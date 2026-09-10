using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher.Windows
{

    public partial class MainWindow
    {
        
        /// <summary>
        /// Normative mapping of logical_axis => { physical_device, physical_axis, dz/sat etc }
        /// </summary>
        internal static Dictionary<LogicalAxis, InGameAxAssgn> s_map_logical_axes
            = new Dictionary<LogicalAxis, InGameAxAssgn>();

        private Label tblabel;
        private Label tblabelab;

        private ProgressBar tbprogressbar;

        private void _UpdateUI_Axes( )
        {
            Debug.Assert(Program.mainWin != null);
            HwndSource hwnd_source = PresentationSource.FromVisual(Program.mainWin) as HwndSource;
            if (hwnd_source == null) return; // can't get DirectInput axis values until hwnd is established

            foreach (LogicalAxis log_axis_id in Enum.GetValues(typeof(LogicalAxis)))
                _UpdateUI_Axes(log_axis_id);
        }
        private void _UpdateUI_Axes( LogicalAxis log_axis_id )
        {
            InGameAxAssgn axis = s_map_logical_axes[log_axis_id];
            if (!axis.IsAssigned()) return;

            // Update text-label and progress-bar visuals.
            tblabel = FindName($"Label_{log_axis_id}") as Label;
            tbprogressbar = FindName($"Axis_{log_axis_id}") as ProgressBar;

            if (tblabel == null || tbprogressbar == null) 
                throw new InvalidProgramException();

            //NB: some logical axes are implicitly inverted.. who knows why
            int invert_mul = (axis.GetInvert() ? -1 : +1);
            switch (log_axis_id)
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
                tbprogressbar.Minimum = CommonConstants.AXISMIN;
                tbprogressbar.Maximum = CommonConstants.AXISMAX;
            }
            else // (invert_mul < 0)
            {
                tbprogressbar.Minimum = -CommonConstants.AXISMAX;
                tbprogressbar.Maximum = CommonConstants.AXISMIN;
            }

            // Get async value of axis.
            var dev_guid = axis.GetJoy().GetInstanceGUID();
            var listener = DirectInputDeviceMap.Singleton.GetListenerForJoystick(dev_guid) as DirectInputListener_Joystick;
            var phys_id = axis.GetPhysicalAxisId();
            var axis_val = listener.GetAxisValue((int)phys_id);

            int adjusted_val = ApplyDeadZone(axis_val, axis.GetDeadzone(), axis.GetSaturation());

            tbprogressbar.Value = adjusted_val * invert_mul;

            //string joyActualName = deviceControl.GetHwDevice(axis.GetDeviceNumber()).DeviceInformation.InstanceName;
            string joyName = "JOY  " + axis.GetDeviceNumber();

            string label_text = joyName + " : " + phys_id.ToString().Replace('_', ' ');
            label_text = label_text.Replace("Axis ", "  ");
            label_text = label_text.Replace("Rotation ", "R");
            label_text = label_text.Replace("Slider 0", "S1");
            label_text = label_text.Replace("Slider 1", "S2");
            tblabel.Content = label_text;

            // Special handling for throttle axes, below..
            if (log_axis_id != LogicalAxis.Throttle && 
                log_axis_id != LogicalAxis.Throttle_Right)
                return;

            tblabelab = FindName($"AB_{log_axis_id}") as Label;
            tblabelab.Visibility = Visibility.Hidden;

            tbprogressbar.Foreground = CommonConstants.LIGHTBLUE;

            InGameAxAssgn throttleAxis = MainWindow.s_map_logical_axes[LogicalAxis.Throttle];
            if (throttleAxis.GetDeviceNumber() >= 0)
            {
                if (!axis.GetInvert() && CommonConstants.AXISMAX + tbprogressbar.Value < GetIDLE() ||
                      axis.GetInvert() && CommonConstants.AXISMIN + tbprogressbar.Value < GetIDLE())
                {
                    tbprogressbar.Foreground = CommonConstants.LIGHTRED;
                    tblabelab.Visibility = Visibility.Visible;
                    tblabelab.Content = "IDLE CUTOFF";
                }
                if (!axis.GetInvert() && CommonConstants.AXISMAX + tbprogressbar.Value > GetAB() ||
                      axis.GetInvert() && CommonConstants.AXISMIN + tbprogressbar.Value > GetAB())
                {
                    tbprogressbar.Foreground = CommonConstants.LIGHTGREEN;
                    tblabelab.Visibility = Visibility.Visible;
                    tblabelab.Content = "AB";
                }
            }

            return;
        }

        private void MainWindow_AxisInputReceived( Guid device_guid, int axis_id, int new_value )
        {
            if (!this.IsActive) return;
            //Debug.WriteLine($"MainWindow_AxisInputReceived({device_guid}, {axis_id}, {new_value})");

            if (s_map_logical_axes == null) return;
            if (s_map_logical_axes.Count == 0) return;

            var joy = deviceControl.GetJoystickMappingForDeviceId(device_guid);
            var log_axis = joy.axis[axis_id].GetLogicalAxis();
            _UpdateUI_Axes(log_axis);

            return;
        }

        /// <summary>
        /// Shows axis output with DEADZONE and SATURATION enabled in BMS
        /// </summary>
        /// <param name="input"></param>
        /// <param name="deadzone"></param>
        /// <param name="saturation"></param>
        /// <returns></returns>
        public static int ApplyDeadZone(int input, AxCurve deadzone, AxCurve saturation)
        {
            float mid16 = (CommonConstants.AXISMAX / 2f);

            float dz16 = 0f;
            switch (deadzone)
            {
                case AxCurve.None:
                    dz16 = 0f;
                    break;
                case AxCurve.Small:
                    dz16 = (CommonConstants.AXISMAX * 0.01f);
                    break;
                case AxCurve.Medium:
                    dz16 = (CommonConstants.AXISMAX * 0.05f);
                    break;
                case AxCurve.Large:
                    dz16 = (CommonConstants.AXISMAX * 0.10f);
                    break;
                default:
                    throw new InvalidProgramException();
            }
            float sat16 = 0f;
            switch (saturation)
            {
                case AxCurve.None:
                    sat16 = 0f;
                    break;
                case AxCurve.Small:
                    sat16 = (CommonConstants.AXISMAX * 0.01f);
                    break;
                case AxCurve.Medium:
                    sat16 = (CommonConstants.AXISMAX * 0.05f);
                    break;
                case AxCurve.Large:
                    sat16 = (CommonConstants.AXISMAX * 0.10f);
                    break;
                default:
                    throw new InvalidProgramException();
            }

            // Curve is 5 segments, at these 4 breakpoints.
            float a = 0 + sat16;
            float b = mid16 - dz16;
            float c = mid16 + dz16;
            float d = CommonConstants.AXISMAX - sat16;

            if (input < a) return 0;
            if (input < b) return input;
            if (input < c) return (int)mid16;
            if (input < d) return input;
            //else
            return CommonConstants.AXISMAX;
        }
        
        /// <summary>
        /// Callback When clicked "Assign" Button. Opens AxisAssignWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Assign_Click(object sender, RoutedEventArgs e)
        {
            //HACK: the button names (in XAML) are the enum value names.
            string whocalledwindow = ((System.Windows.Controls.Button)sender).Name;
            LogicalAxis log_axis = (LogicalAxis)Enum.Parse(typeof(LogicalAxis), whocalledwindow);

            InGameAxAssgn axisAssign = AxisAssignWindow.ShowAxisAssignWindow(this, s_map_logical_axes[log_axis], log_axis);

            // Reset PhysicalAxis previously assigned to same axis
            // In case of axis has been unassigned and saved.
            foreach (var joy in deviceControl.GetJoystickMappings())
                joy.ResetPreviousAxis(log_axis);

            // When axis has been assigned.
            if (axisAssign.IsAssigned())
                axisAssign.GetJoy().axis[(int)axisAssign.GetPhysicalAxisId()]
                    = new AxAssgn(log_axis, axisAssign);

            joyAssign_2_inGameAxis();
            ResetMainWindow_Axis();

            // Save the XML and Key files, after each change user makes.
            MainWindow.deviceControl.SaveXml();
            this.appReg.getOverrideWriter().SaveKeyMapping(MainWindow.s_map_logical_axes, MainWindow.deviceControl);
        }
        
        public void joyAssign_2_inGameAxis()
        {
            foreach (LogicalAxis log_axis in Enum.GetValues(typeof(LogicalAxis)))
                s_map_logical_axes[log_axis] = new InGameAxAssgn();

            foreach (var joy in deviceControl.GetJoystickMappings())
            {
                foreach (PhysicalAxis phys in Enum.GetValues(typeof(PhysicalAxis)))
                {
                    int i = (int)phys;
                    LogicalAxis log_axis = joy.axis[i].GetLogicalAxis();

                    s_map_logical_axes[log_axis] = new InGameAxAssgn(joy, phys, joy.axis[i]);
                }
            }
        }
        
        public void ResetMainWindow_Axis()
        {
            foreach (LogicalAxis log_axis in Enum.GetValues(typeof(LogicalAxis)))
            {
                Label tblabel = FindName($"Label_{log_axis}") as Label;
                ProgressBar tbprogressbar = FindName($"Axis_{log_axis}") as ProgressBar;

                tblabel.Content = log_axis.ToString().Replace("_", " ") + " :";
                tblabel.Content = "";

                tbprogressbar.Value   = CommonConstants.AXISMIN;
                tbprogressbar.Minimum = CommonConstants.AXISMIN;
                tbprogressbar.Maximum = CommonConstants.AXISMAX;
            }

            _UpdateUI_Axes();
        }
        
        public int GetAB()
        {
            InGameAxAssgn axis = s_map_logical_axes[LogicalAxis.Throttle];
            if (axis.IsAssigned())
                return axis.GetJoy().detentPosition.AB;
            //else
            return CommonConstants.AXISMAX;
        }

        public int GetIDLE()
        {
            InGameAxAssgn axis = s_map_logical_axes[LogicalAxis.Throttle];
            if (axis.IsAssigned())
                return axis.GetJoy().detentPosition.IDLE;
            //else
            return CommonConstants.AXISMIN;
        }
    }

}