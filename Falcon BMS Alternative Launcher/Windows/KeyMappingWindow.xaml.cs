using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher.Windows
{

    public partial class KeyMappingWindow
    {
        private DeviceControl _deviceControlRef;

        private List<DirectInputListener> _input_listeners;
        private bool _awaiting_input = true;

        private KeyFile _keyFile;
        private KeyAssgn _selected_callback;

        private KeyAssgn _tmpKeyboard;
        private JoyAssgn[] _tmpJoyAssgns;

        private DispatcherTimer _timer;
        bool _flash_on = true;

        public static void ShowKeyMappingWindow( Window owner, DeviceControl deviceControl, KeyAssgn selectedCallback )
        {
            KeyMappingWindow ownWindow = new KeyMappingWindow(deviceControl, selectedCallback);
            Program.ShowDialogAndMakeActive(ownWindow);
        }

        private KeyMappingWindow(DeviceControl deviceControl, KeyAssgn selectedCallback)
        {
            InitializeComponent();

            this._selected_callback = selectedCallback;

            this._deviceControlRef = deviceControl;
            this._keyFile = deviceControl.GetKeyBindings();

            CallbackName.Content = selectedCallback.GetKeyDescription();

            CurrentlyMapped.Visibility = Visibility.Hidden;

            string selectedCallbackName = selectedCallback.GetCallback();
            this.Select_PinkyShift.IsEnabled = !(selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift");
            this.Select_DX_Release.IsEnabled = !(selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift");

            CloneTempDialogData();
            UpdateUI();
        }

        protected override void OnInitialized( EventArgs e )
        {
            base.OnInitialized(e);

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Start();

            SubscribeToDirectInputEvents();
        }

        internal void SubscribeToDirectInputEvents( )
        {
            var dev_map = DirectInputDeviceMap.Singleton;

            _input_listeners = new List<DirectInputListener>
            {
                dev_map.GetListenerForKeyboard()
            };
            foreach (Guid g in dev_map.GetDeviceInstanceGuids())
            {
                _input_listeners.Add(dev_map.GetListenerForJoystick(g));
            }

            foreach (var listener in _input_listeners)
            {
                listener.KeyboardInputReceived += KeyMappingWindow_KeyboardInputReceived;

                listener.PovInputReceived += KeyMappingWindow_PovInputReceived;
                listener.ButtonInputReceived += KeyMappingWindow_ButtonInputReceived;
            }

            return;
        }

        protected override void OnClosing( CancelEventArgs e )
        {
            base.OnClosing(e);

            // Stop timers.
            _timer.Stop();

            // Unsubscribe the DirectInput listeners.
            foreach (var listener in _input_listeners)
            {
                listener.KeyboardInputReceived -= KeyMappingWindow_KeyboardInputReceived;

                listener.PovInputReceived -= KeyMappingWindow_PovInputReceived;
                listener.ButtonInputReceived -= KeyMappingWindow_ButtonInputReceived;
            }
        }

        private void UpdateUI( )
        {
            if (_tmpKeyboard == null) return;

            var sb = new StringBuilder(500);
            sb.Append(_tmpKeyboard.GetKeyAssignmentStatus());
            if (sb.Length > 0) sb.Append("; ");

            for (int i = 0; i < _tmpJoyAssgns.Length; i++)
                sb.Append(_tmpKeyboard.ReadJoyAssignment(_tmpJoyAssgns[i], i));

            if (sb.Length > 0)
            {
                _awaiting_input = false;
                AwaitingInputs.Visibility = Visibility.Hidden;
            }
            else
            {
                _awaiting_input = true;
                AwaitingInputs.Visibility = Visibility.Visible;
            }
            MappedButton.Content = sb.ToString();

            return;
        }

        private void KeyMappingWindow_ButtonInputReceived( Guid device_guid, int button_id, bool new_state )
        {
            //Debug.WriteLine($"KeyMappingWindow_ButtonInputReceived({device_guid}, {button_id}, {new_state})");

            _awaiting_input = false;
            AwaitingInputs.Visibility = Visibility.Hidden;

            var joy = _GetTmpJoyForDeviceGuid(device_guid);
            Debug.Assert(joy != null);

            string selectedCallbackName = _selected_callback.GetCallback();

            // First, determine if button is mapped to a dx-shift callback; auto-set togglebutton accordingly.
            string currCallback0 = joy.dx[button_id].assign[0].GetCallback();
            if (currCallback0 == "SimHotasPinkyShift" || currCallback0 == "SimHotasShift")
            {
                this.Select_PinkyShift.IsOn = new_state;
                return;
            }

            // Nothing else to do, if this is a release.
            if (new_state == false)
                return;

            // Show UI feedback, and currently mapped callback, if any -- incl consideration for shift- and release-modes.
            Pinky pinky = (this.Select_PinkyShift.IsOn == true ? Pinky.Shift : Pinky.UnShift);
            Behaviour behaviour = (this.Select_DX_Release.IsOn == true ? Behaviour.Release : Behaviour.Press);
            Invoke action = (this.Select_DX_Release.IsOn == true ? Invoke.Down : Invoke.Default);

            int mappingBehavior = (int)pinky + (int)behaviour;
            string currCallback = joy.dx[button_id].assign[mappingBehavior].GetCallback();

            if (String.IsNullOrEmpty(currCallback) ||
                currCallback == CommonConstants.SIMDONOTHING ||
                currCallback == selectedCallbackName)
            {
                this.CurrentlyMapped.Visibility = Visibility.Hidden;
            }
            else
            {
                KeyAssgn row = _keyFile.LookupCallback(currCallback);
                string currCallbackDescr = row != null ? row.GetKeyDescription() : currCallback;

                string pressOrRelease = (this.Select_DX_Release.IsOn == true) ? "release" : "press";
                this.CurrentlyMapped.Text = $"Button {pressOrRelease} currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional DX button assignment.
            joy.dx[button_id].Assign(selectedCallbackName, pinky, behaviour, action, _selected_callback.GetSoundID());

            // Special-case for dx-shift: also assign shift layer to same callback.
            if (selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift")
                joy.dx[button_id].Assign(selectedCallbackName, Pinky.Shift, Behaviour.Press, Invoke.Default, _selected_callback.GetSoundID());

            UpdateUI();
            return;
        }

        private void KeyMappingWindow_PovInputReceived( Guid device_guid, int povhat_id, int new_direction )
        {
            //Debug.WriteLine($"KeyMappingWindow_PovInputReceived({device_guid}, {povhat_id}, {new_direction})");

            _awaiting_input = false;
            AwaitingInputs.Visibility = Visibility.Hidden;

            // Nothing to do, if -1 (pov hat released/centered).
            if (new_direction < 0) return;

            var joy = _GetTmpJoyForDeviceGuid(device_guid);
            Debug.Assert(joy != null);

            // Show UI feedback, and currently mapped callback, if any -- incl support for dx-shift mappings.
            Pinky pinky = (this.Select_PinkyShift.IsOn == true ? Pinky.Shift : Pinky.UnShift);

            string currently_assigned_callback = joy.pov[povhat_id].GetCurrentCallback(new_direction, pinky);
            string selected_callback = _selected_callback.GetCallback();

            if (String.IsNullOrEmpty(currently_assigned_callback) ||
                currently_assigned_callback == CommonConstants.SIMDONOTHING ||
                currently_assigned_callback == selected_callback)
            {
                this.CurrentlyMapped.Visibility = Visibility.Hidden;
            }
            else
            {
                KeyAssgn row = _keyFile.LookupCallback(currently_assigned_callback);
                string currCallbackDescr = row != null ? row.GetKeyDescription() : currently_assigned_callback;

                this.CurrentlyMapped.Text = $"POV-hat direction currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional POV-direction assignment.
            joy.pov[povhat_id].Assign(new_direction, selected_callback, pinky, _selected_callback.GetSoundID());

            UpdateUI();
            return;
        }

        private void KeyMappingWindow_KeyboardInputReceived( int key_scancode, int mod_flags, bool new_state )
        {
            //Debug.WriteLine($"KeyMappingWindow_KeyboardInputReceived({key_scancode}, {mod_flags}, {new_state})");

            _awaiting_input = false;
            AwaitingInputs.Visibility = Visibility.Hidden;

            //TODO: blocklist for QWERTY, see also https://github.com/gyroplatter/FalconBMS.Launcher/issues/10

            Pinky pinkyStatus = (Select_PinkyShift.IsOn == true) ? Pinky.Shift : Pinky.UnShift;

            // Determine if this input is already mapped to another callback, and display warning/hint if so.
            KeyAssgn currCallbackAssgn = _keyFile.ReverseLookupKeyboardInput(key_scancode, mod_flags);
            if (currCallbackAssgn == null || currCallbackAssgn.GetCallback() == "SimDoNothing")
            {
                this.CurrentlyMapped.Visibility = Visibility.Hidden;
            }
            else
            {
                string currCallbackDescr = "Keyboard input currently bound to:\r\n" + currCallbackAssgn.GetKeyDescription();

                this.CurrentlyMapped.Text = currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Assign to temp model.
            if (pinkyStatus == Pinky.UnShift)
                _tmpKeyboard.SetKeyboard(key_scancode, mod_flags);
            if (pinkyStatus == Pinky.Shift)
                _tmpKeyboard.SetKeyboardComboPrefix(key_scancode, mod_flags);//TODO: support adding new key-combo prefixes

            UpdateUI();
            return;
        }

        private void CloneTempDialogData()
        {
            _tmpKeyboard = _selected_callback.Clone();

            JoyAssgn[] joyAssgns = _deviceControlRef.GetJoystickMappings();
            _tmpJoyAssgns = new JoyAssgn[joyAssgns.Length];

            for (int i = 0; i < joyAssgns.Length; i++)
            {
                JoyAssgn tmpjoy = joyAssgns[i].MakeTempCloneForKeyMappingDialog();
                _tmpJoyAssgns[i] = tmpjoy;
            }
            return;
        }

        JoyAssgn _GetTmpJoyForDeviceGuid( Guid device_guid )
        {
            foreach (var tmpjoy in _tmpJoyAssgns)
            {
                if (tmpjoy.GetInstanceGUID() == device_guid)
                    return tmpjoy;
            }
            throw new KeyNotFoundException();
        }

        private void Timer_Tick( object sender, EventArgs e )
        {
            if (!_awaiting_input) return;

            _flash_on = (!_flash_on);

            if (_flash_on)
                AwaitingInputs.Visibility = Visibility.Visible;
            else
                AwaitingInputs.Visibility = Visibility.Hidden;

            return;
        }

        private void ClearDX_Click(object sender, RoutedEventArgs e)
        {
            // Make fresh clones. //BUGBUG: does this also wipe any unsaved changes to key binding?
            CloneTempDialogData();

            // Remove the current DX button/povhat bindings.
            string targetCallback = _selected_callback.GetCallback();

            foreach (JoyAssgn tmpjoy in _tmpJoyAssgns)
                tmpjoy.UnassigntargetCallback(targetCallback);

            _awaiting_input = false;
            AwaitingInputs.Visibility = Visibility.Hidden;
            UpdateUI();
        }

        private void ClearKey_Click(object sender, RoutedEventArgs e)
        {
            _tmpKeyboard.UnassignKeyboard();

            _awaiting_input = false;
            AwaitingInputs.Visibility = Visibility.Hidden;
            UpdateUI();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            JoyAssgn[] joyAssgns = _deviceControlRef.GetJoystickMappings();

            for (int i = 0; i < _tmpJoyAssgns.Length; i++)
            {
                joyAssgns[i].CopyButtonsAndHatsFromCurrentProfile(_tmpJoyAssgns[i]);
            }
            _selected_callback.CopyOtherKeyAssgn(_tmpKeyboard);

            // Unassign the previous mapping that was assigned to this key/key combo.
            KeyAssgn oldKey = _keyFile.keyAssign.FirstOrDefault(x => x != _selected_callback && x.GetKeyAssignmentStatus() == _selected_callback.GetKeyAssignmentStatus());
            if (oldKey != null)
            {
                oldKey.UnassignKeyboard();
            }

            // Save the XML and Key files, after each change user makes.
            _deviceControlRef.SaveXml();
            Program.mainWin.appReg.getOverrideWriter().SaveKeyMapping(MainWindow.s_map_logical_axes, _deviceControlRef);

            Close();
        }

    }

}
