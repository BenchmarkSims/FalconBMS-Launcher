using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher.Windows
{

    public partial class AxisAssignWindow
    {
        public static InGameAxAssgn ShowAxisAssignWindow( Window owner, InGameAxAssgn axisAssign, LogicalAxis log_axis )
        {
            AxisAssignWindow ownWindow = new AxisAssignWindow(owner, axisAssign, log_axis);
            Program.ShowDialogAndMakeActive(ownWindow);

            axisAssign = ownWindow._curr_axis_assgn;
            return axisAssign;
        }

        private AxisAssignWindow(Window owner, InGameAxAssgn axisAssign, LogicalAxis log_axis)
        {
            InitializeComponent();
            this.Owner = owner;

            this._logical_axis_id = log_axis;
            this._curr_axis_assgn = axisAssign;
        }

        private List<DirectInputListener> _input_listeners;
        private bool _awaiting_input = true;

        private LogicalAxis _logical_axis_id;
        private InGameAxAssgn _curr_axis_assgn;

        // Local data-model for this dialog:
        private InGameAxAssgn _dlg_axis_assgn;

        // Timer only for flashing UI elements.
        private DispatcherTimer _timer;
        bool _flash_on = true;

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
                listener.AxisInputReceived += AxisAssignWindow_AxisInputReceived;
                listener.AxisCoarseInputReceived += AxisAssignWindow_AxisCoarseInputReceived;
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
                listener.AxisInputReceived -= AxisAssignWindow_AxisInputReceived;
                listener.AxisCoarseInputReceived -= AxisAssignWindow_AxisCoarseInputReceived;
            }
        }

        private void AxisAssignWindow_AxisInputReceived( Guid device_guid, int axis_id, int new_value )
        {
            if (!this.IsActive) return;
            //Debug.WriteLine($"AxisAssignWindow_AxisInputReceived({device_guid}, {axis_id}, {new_value})");

            if (_awaiting_input) return;

            var joy = _dlg_axis_assgn.GetJoy();
            if (device_guid != joy.GetInstanceGUID()) return;
            if ((PhysicalAxis)axis_id != _dlg_axis_assgn.GetPhysicalAxisId()) return;

            UpdateUI(new_value);
            return;
        }

        private void AxisAssignWindow_AxisCoarseInputReceived( Guid device_guid, int axis_id, int new_value )
        {
            if (!this.IsActive) return;
            if (_dlg_axis_assgn == null) return;
            //Debug.WriteLine($"AxisAssignWindow_AxisCoarseInputReceived({device_guid}, {axis_id}, {new_value})");

            if (!_awaiting_input) return;

            // Engage this device and axis.
            var joy = MainWindow.deviceControl.GetJoystickMappingForDeviceId(device_guid);

            _dlg_axis_assgn = new InGameAxAssgn(
                joy, (PhysicalAxis)axis_id,
                _dlg_axis_assgn.GetInvert(),
                _dlg_axis_assgn.GetDeadzone(),
                _dlg_axis_assgn.GetSaturation()
                );

            _awaiting_input = false;
            AssignedJoystick.Visibility = Visibility.Visible;

            // Update UI.
            UpdateUI(new_value);

            Retry.Content = "RETRY";
            Retry.Visibility = Visibility.Visible;

            if (_logical_axis_id == LogicalAxis.Throttle)
            {
                SetAB.Visibility = Visibility.Visible;
                Idle.Visibility = Visibility.Visible;
            }

            return;
        }

        private void AssignWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Retry.Visibility = Visibility.Hidden;
            SetAB.Visibility = Visibility.Hidden;
            Idle.Visibility = Visibility.Hidden;

            check_ABIDLE.Visibility = Visibility.Hidden;

            Label_AxisName.Content = _logical_axis_id.ToString().Replace("_", " ");

            _awaiting_input = true;
            AssignedJoystick.Content = "   AWAITING INPUTS";
            AssignedJoystick.Visibility = Visibility.Visible;

            switch (_logical_axis_id)
            {
                case LogicalAxis.Roll:
                    DirectionDecrease.Content = "Roll Left";
                    DirectionIncrease.Content = "Roll Right";
                    break;
                case LogicalAxis.Trim_Roll:
                    DirectionDecrease.Content = "Roll Left";
                    DirectionIncrease.Content = "Roll Right";
                    break;
                case LogicalAxis.Pitch:
                    DirectionDecrease.Content = "Pitch Down";
                    DirectionIncrease.Content = "Pitch Up";
                    break;
                case LogicalAxis.Trim_Pitch:
                    DirectionDecrease.Content = "Pitch Down";
                    DirectionIncrease.Content = "Pitch Up";
                    break;
                case LogicalAxis.Yaw:
                case LogicalAxis.Trim_Yaw:
                    DirectionDecrease.Content = "Yaw Left";
                    DirectionIncrease.Content = "Yaw Right";
                    break;
                case LogicalAxis.Throttle:
                case LogicalAxis.Throttle_Right:
                    DirectionDecrease.Content = "Afterward";
                    DirectionIncrease.Content = "Forward";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.Toe_Brake:
                case LogicalAxis.Toe_Brake_Right:
                    DirectionDecrease.Content = "Release";
                    DirectionIncrease.Content = "Apply";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.Radar_Antenna_Elevation:
                    DirectionDecrease.Content = "Elevation Down";
                    DirectionIncrease.Content = "Elevation Up";
                    break;
                case LogicalAxis.Cursor_X:
                    DirectionDecrease.Content = "Cursor Left";
                    DirectionIncrease.Content = "Cursor Right";
                    break;
                case LogicalAxis.Cursor_Y:
                    DirectionDecrease.Content = "Cursor Afterward";
                    DirectionIncrease.Content = "Cursor Forward";
                    break;
                case LogicalAxis.Range_Knob:
                    DirectionDecrease.Content = "Clock Wise";
                    DirectionIncrease.Content = "Counter CW";
                    break;
                case LogicalAxis.HMS_Brightness:
                case LogicalAxis.FLIR_Brightness:
                case LogicalAxis.HUD_Brightness:
                case LogicalAxis.Reticle_Depression:
                    DirectionDecrease.Content = "Dark";
                    DirectionIncrease.Content = "Bright";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.Intercom:
                case LogicalAxis.COMM_Channel_1:
                case LogicalAxis.COMM_Channel_2:
                case LogicalAxis.MSL_Volume:
                case LogicalAxis.Threat_Volume:
                case LogicalAxis.AI_vs_IVC:
                    DirectionDecrease.Content = "Volume Down";
                    DirectionIncrease.Content = "Volume Up";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.FOV:
                    DirectionDecrease.Content = "Narrow";
                    DirectionIncrease.Content = "Wide";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.Camera_Distance:
                    DirectionDecrease.Content = "Close";
                    DirectionIncrease.Content = "Leave";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
                case LogicalAxis.HSI_Course_Knob:
                case LogicalAxis.HSI_Heading_Knob:
                case LogicalAxis.Altimeter_Knob:
                    DirectionDecrease.Content = "Decrease";
                    DirectionIncrease.Content = "Increase";
                    DeadZone.Visibility = Visibility.Collapsed;
                    Label_DeadZone.Visibility = Visibility.Collapsed;
                    break;
            }

            AxisValueProgress.Value   = CommonConstants.AXISMIN;
            AxisValueProgress.Minimum = CommonConstants.AXISMIN;
            AxisValueProgress.Maximum = CommonConstants.AXISMAX;

            Reset();
            return;
        }

        public void Reset()
        {
            AxisValueProgress.Value = CommonConstants.AXISMIN;

            _curr_axis_assgn = MainWindow.s_map_logical_axes[_logical_axis_id];

            if (_curr_axis_assgn.IsAssigned())
            {
                _awaiting_input = false;
                Retry.Content = "CLEAR";
                Retry.Visibility = Visibility.Visible;

                if (_logical_axis_id == LogicalAxis.Throttle)
                {
                    SetAB.Visibility = Visibility.Visible;
                    Idle.Visibility = Visibility.Visible;
                }
            }

            Saturation.SelectedIndex = (int)_curr_axis_assgn.GetSaturation();
            DeadZone.SelectedIndex = (int)_curr_axis_assgn.GetDeadzone();
            Invert.IsChecked = _curr_axis_assgn.GetInvert();

            _dlg_axis_assgn = new InGameAxAssgn( //TODO: copy ctor?
                joy: _curr_axis_assgn.GetJoy(), 
                phys_axis: _curr_axis_assgn.GetPhysicalAxisId(),
                invert: _curr_axis_assgn.GetInvert(),
                deadzone: _curr_axis_assgn.GetDeadzone(),
                saturation: _curr_axis_assgn.GetSaturation()
                );
            return;
        }

        private void InvertAxisDisp( int axis_val )
        {
            //NB: some logical axes are implicitly inverted.. who knows why
            int invert_mul = (_dlg_axis_assgn.GetInvert() ? -1 : +1);
            switch (_logical_axis_id) //TODO: dedupe this with MainWindow_AxisInputReceived
            {
                case LogicalAxis.Throttle:
                case LogicalAxis.Throttle_Right:
                case LogicalAxis.Toe_Brake:
                case LogicalAxis.Toe_Brake_Right:
                case LogicalAxis.Intercom:
                case LogicalAxis.COMM_Channel_1:
                case LogicalAxis.COMM_Channel_2:
                case LogicalAxis.MSL_Volume:
                case LogicalAxis.Threat_Volume:
                case LogicalAxis.AI_vs_IVC:
                case LogicalAxis.ILS_Volume_Knob:
                    invert_mul *= -1;
                    break;
            }

            if (invert_mul > 0)
            {
                AxisValueProgress.Minimum = CommonConstants.AXISMIN;
                AxisValueProgress.Maximum = CommonConstants.AXISMAX;
            }
            else // (invert_mul < 0)
            {
                AxisValueProgress.Minimum = -CommonConstants.AXISMAX;
                AxisValueProgress.Maximum = CommonConstants.AXISMIN;
            }

            int adjusted_val = MainWindow.ApplyDeadZone(axis_val, _dlg_axis_assgn.GetDeadzone(), _dlg_axis_assgn.GetSaturation());

            AxisValueProgress.Value = adjusted_val * invert_mul;
            return;
        }

        private void UpdateUI( int axis_val )
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;

            var joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            InvertAxisDisp(axis_val);
            
            AssignedJoystick.Content = "   "
                + _dlg_axis_assgn.GetPhysicalAxisId().ToString().Replace('_', ' ') + " : "
                + joy.GetSanitizedProductName();

            if (_logical_axis_id != LogicalAxis.Throttle && _logical_axis_id != LogicalAxis.Throttle_Right)
                return;

            // Throttle specific stuff..
            AxisValueProgress.Foreground = CommonConstants.LIGHTBLUE;
            check_ABIDLE.Visibility = Visibility.Hidden;
            var ab = _dlg_axis_assgn.GetJoy().detentPosition.AB;
            var idle = _dlg_axis_assgn.GetJoy().detentPosition.IDLE;
            if ((Invert.IsChecked == false && CommonConstants.AXISMAX + AxisValueProgress.Value < idle) || 
                (Invert.IsChecked == true && CommonConstants.AXISMIN + AxisValueProgress.Value < idle))
            {
                AxisValueProgress.Foreground = CommonConstants.LIGHTRED;
                check_ABIDLE.Visibility = Visibility.Visible;
                check_ABIDLE.Content = "IDLE CUTOFF";
            }
            if ((Invert.IsChecked == false && CommonConstants.AXISMAX + AxisValueProgress.Value > ab) || 
                (Invert.IsChecked == true && CommonConstants.AXISMIN + AxisValueProgress.Value > ab) )
            {
                AxisValueProgress.Foreground = CommonConstants.LIGHTGREEN;
                check_ABIDLE.Visibility = Visibility.Visible;
                check_ABIDLE.Content = "AB";
            }
            return;
        }

        private void Timer_Tick( object sender, EventArgs e )
        {
            if (!_awaiting_input) return;

            _flash_on = (!_flash_on);

            if (_flash_on)
                AssignedJoystick.Visibility = Visibility.Visible;
            else
                AssignedJoystick.Visibility = Visibility.Hidden;

            return;
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            _awaiting_input = true;
            AssignedJoystick.Content = "   AWAITING INPUTS";
            AssignedJoystick.Visibility = Visibility.Visible;

            AxisValueProgress.Minimum = CommonConstants.AXISMIN;
            AxisValueProgress.Maximum = CommonConstants.AXISMAX;
            AxisValueProgress.Value   = CommonConstants.AXISMIN;

            Retry.Visibility = Visibility.Hidden;
            SetAB.Visibility = Visibility.Hidden;
            Idle.Visibility  = Visibility.Hidden;

            return;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;

            if (_awaiting_input)
            {
                _curr_axis_assgn = new InGameAxAssgn();
            }
            else
            {
                _curr_axis_assgn = _dlg_axis_assgn;
            }

            Close();
            return;
        }

        private void Invert_Click( object sender, RoutedEventArgs e )
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;
            JoyAssgn joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            _dlg_axis_assgn.SetInvert(this.Invert.IsChecked == true);
            return;
        }

        private void DeadZone_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;
            JoyAssgn joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            _dlg_axis_assgn.SetDeadzone((AxCurve)this.DeadZone.SelectedIndex);
            return;
        }

        private void Saturation_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;
            JoyAssgn joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            _dlg_axis_assgn.SetSaturation((AxCurve)this.Saturation.SelectedIndex);
            return;
        }

        private void SetAB_Click(object sender, RoutedEventArgs e)
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;
            JoyAssgn joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            var buffer = DirectInputDeviceMap.Singleton.GetListenerForJoystick(joy.GetInstanceGUID()) as DirectInputListener_Joystick;
            int axis_val = buffer.GetAxisValue((int)_dlg_axis_assgn.GetPhysicalAxisId());

            //NB: this looks backward but throttle axis is one of the implicitly inverted ones.. so it's double-backward.
            if (_dlg_axis_assgn.GetInvert())
                axis_val = CommonConstants.AXISMIN + axis_val;
            else
                axis_val = CommonConstants.AXISMAX - axis_val;

            axis_val = Math.Min(Math.Max(CommonConstants.AXISMIN, axis_val), CommonConstants.AXISMAX);

            joy.detentPosition.AB = axis_val;
            return;
        }

        private void SetIDLE_Click(object sender, RoutedEventArgs e)
        {
            if (_awaiting_input) return;
            if (_dlg_axis_assgn == null) return;
            JoyAssgn joy = _dlg_axis_assgn.GetJoy();
            if (joy == null) return;

            var buffer = DirectInputDeviceMap.Singleton.GetListenerForJoystick(joy.GetInstanceGUID()) as DirectInputListener_Joystick;
            int axis_val = buffer.GetAxisValue((int)_dlg_axis_assgn.GetPhysicalAxisId());

            //NB: this looks backward but throttle axis is one of the implicitly inverted ones.. so it's double-backward.
            if (_dlg_axis_assgn.GetInvert())
                axis_val = CommonConstants.AXISMIN + axis_val;
            else
                axis_val = CommonConstants.AXISMAX - axis_val;

            axis_val = Math.Min(Math.Max(CommonConstants.AXISMIN, axis_val), CommonConstants.AXISMAX);

            joy.detentPosition.IDLE = axis_val;
            return;
        }
    }
}
