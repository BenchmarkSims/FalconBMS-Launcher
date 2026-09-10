using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Input
{

    internal class DirectInputDeviceMap : IDisposable
    {
        static DirectInputDeviceMap s_singleton = new DirectInputDeviceMap();
        public static DirectInputDeviceMap Singleton => s_singleton;

        Dictionary<Guid, DirectInputListener> _listener_map;
        List<Guid> _ordered_keys;

        private DirectInputDeviceMap( )
        {
            _ordered_keys = new List<Guid>();
            _listener_map = new Dictionary<Guid, DirectInputListener>();

            RefreshDeviceList();
        }

        public bool RefreshDeviceList( )
        {
            Guid[] prev_keys = _ordered_keys.ToArray();
            _ordered_keys.Clear();

            _ordered_keys.Add(SystemGuid.Keyboard);

            DeviceList devs = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);
            foreach (DeviceInstance dev in devs)
            {
                Guid g = dev.InstanceGuid;
                _ordered_keys.Add(g);
            }

            // Remove any old/gone devices and shutdown their listeners.
            List<Guid> keys_to_remove = new List<Guid>();
            foreach (var x in _listener_map.Keys)
            {
                if (!_ordered_keys.Contains(x))
                {
                    _listener_map[x].Shutdown();
                    keys_to_remove.Add(x);//can't modify collection while enumerating it!
                }
            }
            foreach (var g in keys_to_remove)
                _listener_map.Remove(g);

            bool updated = (!_ordered_keys.SequenceEqual(prev_keys));
            return updated;
        }

        public IEnumerable<Guid> GetDeviceInstanceGuids( bool include_keybd=false )
        {
            if (include_keybd)
                return _ordered_keys.ToArray();
            else
                return _ordered_keys.Where(x => !x.Equals(SystemGuid.Keyboard));

            //List<Guid> tmp = new List<Guid>();
            //foreach (var x in _ordered_keys)
            //    if (x != SystemGuid.Keyboard) tmp.Add(x);

            //return tmp.ToArray();
        }

        public DirectInputListener GetListenerForKeyboard( )
        {
            Guid device_guid = SystemGuid.Keyboard;
            return _GetListenerForDevice(device_guid, is_keyboard:true);
        }

        public DirectInputListener GetListenerForJoystick( Guid device_guid )
        {
            return _GetListenerForDevice(device_guid);
        }

        DirectInputListener _GetListenerForDevice( Guid device_guid, bool is_keyboard=false )
        {
            if (!_ordered_keys.Contains(device_guid))
                throw new ArgumentException();

            if (_listener_map.ContainsKey(device_guid))
                return _listener_map[device_guid];

            // Else spawn a listener thread for this device.
            DirectInputListener listener;
            if (is_keyboard) listener = new DirectInputListener_Keyboard(device_guid);
            else listener = new DirectInputListener_Joystick(device_guid);

            _listener_map.Add(device_guid, listener);
            return listener;
        }

        public void Dispose( )
        {
            ShutdownAllListeners();
            GC.SuppressFinalize(this);
        }

        public void ShutdownAllListeners( )
        {
            foreach (var key in _ordered_keys)
            {
                var listener = _listener_map[key];
                listener.Shutdown();
                _listener_map.Remove(key);
            }
            _ordered_keys.Clear();

            return;
        }
    }

    public abstract class DirectInputListener : IDisposable
    {
        protected Device _hw_device;
        protected Guid _hw_device_guid;

        AutoResetEvent _autoreset_event;
        volatile bool _shutdown_signal = false;

        Thread _listener_thread;
        Dispatcher _dispatcher;

        public event KeyboardInputCallback KeyboardInputReceived;
        public delegate void KeyboardInputCallback( int key_scancode, int mod_flags, bool new_state );
        protected virtual void OnKeyboardInput( int key_scancode, int mod_flags, bool new_state )
        {
            KeyboardInputReceived?.Invoke(key_scancode, mod_flags, new_state);
        }

        public event ButtonInputCallback ButtonInputReceived;
        public delegate void ButtonInputCallback( Guid device_guid, int button_id, bool new_state );
        protected virtual void OnButtonInput( Guid device_guid, int button_id, bool new_state )
        {
            ButtonInputReceived?.Invoke(device_guid, button_id, new_state);
        }

        public event PovInputCallback PovInputReceived;
        public delegate void PovInputCallback( Guid device_guid, int povhat_id, int new_direction );
        protected virtual void OnPovInput( Guid device_guid, int povhat_id, int new_direction )
        {
            PovInputReceived?.Invoke(device_guid, povhat_id, new_direction);
        }

        public event AxisInputCallback AxisInputReceived;
        public delegate void AxisInputCallback( Guid device_guid, int axis_id, int new_value );
        protected virtual void OnAxisInput( Guid device_guid, int axis_id, int new_value )
        {
            AxisInputReceived?.Invoke(device_guid, axis_id, new_value);
        }

        public event AxisInputCallback AxisCoarseInputReceived; // only report changes of +/-5%, for detecting intentional movement on axis-mapping dialog
        protected virtual void OnAxisCoarseInput( Guid device_guid, int axis_id, int new_value )
        {
            AxisCoarseInputReceived?.Invoke(device_guid, axis_id, new_value);
        }

        public DirectInputListener( Guid device_guid )
        {
            _hw_device = new Device(device_guid);
            _hw_device_guid = _hw_device.DeviceInformation.InstanceGuid;

            // Set props for buffered (queued) input, non-exclusive, and acquire the device.
            Debug.Assert(Program.mainWin != null);
            HwndSource hwnd_source = PresentationSource.FromVisual(Program.mainWin) as HwndSource;
            Debug.Assert(hwnd_source != null);

            _hw_device.SetCooperativeLevel(hwnd_source.Handle,
                CooperativeLevelFlags.NonExclusive |
                CooperativeLevelFlags.Background);

            _hw_device.Properties.BufferSize = 16;
            _autoreset_event = new AutoResetEvent(initialState: false);
            _hw_device.SetEventNotification(_autoreset_event);

            _hw_device.Acquire();

            // Start the listener thread.
            _dispatcher = Dispatcher.CurrentDispatcher;

            _listener_thread = new Thread(_ListenerLoop);
            _listener_thread.IsBackground = true;
            _listener_thread.Name = $"DirectInputListener_{device_guid}";
            _listener_thread.Start();
        }

        public Device HwDevice => _hw_device;

        public void Shutdown( )
        {
            // Stop the listener thread.
            _shutdown_signal = true;
            _autoreset_event.Set();
            _listener_thread.Join();

            // Release the device.
            _hw_device.Unacquire();
            _hw_device.Dispose();

            _autoreset_event.Dispose();
            return;
        }
        void IDisposable.Dispose( )
        {
            Shutdown();
            return;
        }

        private void _ListenerLoop( )
        {
            while (!_shutdown_signal)
            {
                _autoreset_event.WaitOne();
                if (_shutdown_signal) return;

                try
                {
                    var bdc = _hw_device.GetBufferedData();
                    if (bdc == null) continue;

                    foreach (var bd_ in bdc)
                    {
                        var bd = bd_ as BufferedData;
                        //Debug.WriteLine($"Data:{bd.Data}, Offset:{bd.Offset}, ButtonPressedData:{bd.ButtonPressedData}");

                        // Thunk to the UI thread, then let derived keyboard/joystick classes parse the BufferedData.
                        _dispatcher.BeginInvoke(new System.Action(( ) =>
                        {
                            HandleBufferData(bd);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    // Device lost/unacquired? Buffer full? Log and ignore.
                    Diagnostics.Log(ex);
                    continue;
                }
            }
        }

        protected abstract void HandleBufferData( BufferedData bd);
    }

    public class DirectInputListener_Keyboard : DirectInputListener
    {
        int _modifier_flags = 0;

        public DirectInputListener_Keyboard( Guid device_guid  ) : base( device_guid )
        { }

        protected override void HandleBufferData( BufferedData bd )
        {
            Key scancode = (Key)bd.Offset;
            switch (scancode)
            {
                case Key.LeftShift:
                case Key.LeftControl:
                case Key.LeftAlt:
                case Key.RightShift:
                case Key.RightControl:
                case Key.RightAlt:
                    _HandleModifierKey(bd);
                    return;
            }

            bool pressed = (bd.ButtonPressedData != 0);
            OnKeyboardInput((int)scancode, _modifier_flags, pressed);
            return;
        }

        void _HandleModifierKey( BufferedData bd )
        {
            Key scancode = (Key)bd.Offset;
            bool pressed = (bd.ButtonPressedData != 0);

            if (scancode == Key.LeftShift || scancode == Key.RightShift)
                _SetModifierBit(0, pressed);
            if (scancode == Key.LeftControl || scancode == Key.RightControl)
                _SetModifierBit(1, pressed);
            if (scancode == Key.LeftAlt|| scancode == Key.RightAlt)
                _SetModifierBit(2, pressed);

            return;
        }

        void _SetModifierBit( int bitnum, bool pressed )
        {
            int mask = (1 << bitnum);
            if (pressed)
                _modifier_flags |= mask;
            else
                _modifier_flags &= ~mask;
            return;
        }
    }

    public class DirectInputListener_Joystick : DirectInputListener
    {
        AntiHisteresisFilter_Int32[] _axis_cache = new AntiHisteresisFilter_Int32[8];
        int[] _axis_cache_coarse = new int[8];

        public DirectInputListener_Joystick( Guid device_guid ) : base(device_guid)
        {
            _InitAxisCache();
        }

        //TODO: let DirectInput apply the sz/sat math for us?
        //public void UpdateDeadZone( float deadzone );
        //public void UpdateSaturation( float saturation );

        public int GetAxisValue( int axis )
        { 
            return _axis_cache[axis].LatestValue;
        }

        private void _InitAxisCache( )
        {
            _hw_device.Poll();
            var curr_state  = _hw_device.CurrentJoystickState;

            int[] tmp = new int[8]
            {
                curr_state.X,
                curr_state.Y,
                curr_state.Z,
                curr_state.Rx,
                curr_state.Ry,
                curr_state.Ry,
                curr_state.GetSlider()[0],
                curr_state.GetSlider()[1]
            };
            for (int i = 0; i < tmp.Length; ++i)
            {
                _axis_cache[i] = new AntiHisteresisFilter_Int32(500, tmp[i]);
                _axis_cache_coarse[i] = tmp[i];
            }

            return;
        }

        // Offsets into the DIJOYSTATE2 struct: https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee416628(v=vs.85)
        const int c_buffer_offset_axes = 0;
        const int c_buffer_offset_povhats = 32;
        const int c_buffer_offset_buttons = 48;

        protected override void HandleBufferData( BufferedData bd )
        {
            if (c_buffer_offset_buttons <= bd.Offset && bd.Offset < c_buffer_offset_buttons + 128)
                _HandleButtonData(bd);
            else if (c_buffer_offset_povhats <= bd.Offset && bd.Offset < c_buffer_offset_povhats + 16)
                _HandlePovHatData(bd);
            else if (c_buffer_offset_axes <= bd.Offset && bd.Offset < c_buffer_offset_axes + 32)
                _HandleAxisData(bd);
            // else, ignore
            return;
        }

        private void _HandleAxisData( BufferedData bd )
        {
            int axis_id = (bd.Offset - c_buffer_offset_axes) / sizeof(Int32);
            int axis_val = bd.Data;

            axis_val &= 0x0000FFFF;//clamp to [0-65535]

            int last_val = _axis_cache[axis_id].LatestValue; //anti-histeresis filter
            int filtered_val = _axis_cache[axis_id].UpdateValue(axis_val);
            if (filtered_val != last_val)
            {
                OnAxisInput(_hw_device_guid, axis_id, axis_val);

                // Detect coarse changes (+/-5%, for detecting intentional movement on axis-mapping dialog).
                int delta = axis_val - _axis_cache_coarse[axis_id];
                if (Math.Abs(delta) > 5000)
                {
                    _axis_cache_coarse[axis_id] = axis_val;
                    OnAxisCoarseInput(_hw_device_guid, axis_id, axis_val);
                }
            }

            return;
        }

        private void _HandlePovHatData( BufferedData bd )
        {
            int pov_id = bd.Offset - c_buffer_offset_povhats;
            int pov_raw_val = bd.Data;

            // DirectInput transmits pov-hat direction in compass-degrees x100 -- we divide by 4500 to get range [-1|0-7].
            int eightway = (pov_raw_val <= 7 ? pov_raw_val : pov_raw_val / CommonConstants.POV45);

            OnPovInput(_hw_device_guid, pov_id, eightway);
            return;
        }

        private void _HandleButtonData( BufferedData bd )
        {
            int button_id = bd.Offset - c_buffer_offset_buttons;
            int button_val = (bd.Data & 0x80) >> 7; // only the high-bit of the low-byte is meaningful
            OnButtonInput(_hw_device_guid, button_id, button_val != 0);
            return;
        }
    }

}//n
