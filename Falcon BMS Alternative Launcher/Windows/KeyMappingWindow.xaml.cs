using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using FalconBMS.Launcher.Input;

using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Windows
{
    /// <summary>
    /// Interaction logic for KeyMappingWindow.xaml
    /// </summary>
    public partial class KeyMappingWindow : ITimerSink
    {
        private DeviceControl deviceControlRef;

        private KeyFile       keyFile;
        private KeyAssgn      selectedCallback;

        private KeyAssgn   tmpKeyboard;
        private JoyAssgn[] tmpJoyStick;
        private List<ButtonStateTracker> _buttonTrackers = new List<ButtonStateTracker>();

        private DirectInputKeyboard directInputDevice = new DirectInputKeyboard();

        private int TickCount_NextUIFlush1;
        private int TickCount_NextUIFlush2;

        public KeyMappingWindow(DeviceControl deviceControl, KeyAssgn selectedCallback)
        {
            InitializeComponent();

            this.selectedCallback = selectedCallback;

            this.deviceControlRef = deviceControl;
            this.keyFile = deviceControl.GetKeyBindings();

            CallbackName.Content = selectedCallback.GetKeyDescription();

            CurrentlyMapped.Visibility = Visibility.Hidden;

            string selectedCallbackName = selectedCallback.GetCallback();
            this.Select_PinkyShift.IsEnabled = !(selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift");
            this.Select_DX_Release.IsEnabled = !(selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift");

            CloneTempDialogData();
        }

        public static void ShowKeyMappingWindow(Window owner, DeviceControl deviceControl, KeyAssgn selectedCallback)
        {
            KeyMappingWindow ownWindow = new KeyMappingWindow(deviceControl, selectedCallback);
            Program.ShowDialogAndMakeActive(ownWindow);
        }

        private void CloneTempDialogData()
        {
            tmpKeyboard = selectedCallback.Clone();

            JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            tmpJoyStick = new JoyAssgn[joyAssgns.Length];
            _buttonTrackers = new List<ButtonStateTracker>(joyAssgns.Length);

            for (int i = 0; i < joyAssgns.Length; i++)
            {
                JoyAssgn tmpjoy = joyAssgns[i].MakeTempCloneForKeyMappingDialog();
                tmpJoyStick[i] = tmpjoy;
                _buttonTrackers.Add(new ButtonStateTracker(tmpjoy, _onButtonChanged, _onPovHatChanged));
            }
            return;
        }

        void ITimerSink.HandleTimerTick()
        {
            try
            {
                KeyboardButtonMonitor();
                JoystickButtonMonitor();
                ShowAssignedStatus();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }

        private void ShowAssignedStatus()
        {
            JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            var sb = new StringBuilder(500);
            sb.Append(tmpKeyboard.GetKeyAssignmentStatus());
            if (sb.Length > 0) sb.Append("; ");

            for (int i = 0; i < joyAssgns.Length; i++)
                sb.Append(tmpKeyboard.ReadJoyAssignment(i, tmpJoyStick));

            string currentKeyAndButtons = sb.ToString();
            MappedButton.Content = currentKeyAndButtons;

            if (currentKeyAndButtons.Length == 0)
            {
                if (Environment.TickCount > TickCount_NextUIFlush1)
                    AwaitingInputs.Content = "";
                if (Environment.TickCount > TickCount_NextUIFlush2)
                {
                    AwaitingInputs.Content = "   AWAITING INPUTS";

                    TickCount_NextUIFlush1 = Environment.TickCount + CommonConstants.FLUSHTIME1;
                    TickCount_NextUIFlush2 = Environment.TickCount + CommonConstants.FLUSHTIME2;
                }
            }
            else
            {
                AwaitingInputs.Content = "";
            }

            return;
        }

        private void JoystickButtonMonitor()
        {
            JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            // Invoke _onButtonChanged, _onPovHatChanged callbacks, below.
            foreach (ButtonStateTracker tracker in _buttonTrackers)
                tracker.PollUpdate();

            return;
        }

        private void _onButtonChanged(JoyAssgn tmpjoy, int buttonId, bool newState)
        {
            System.Diagnostics.Debug.WriteLine($"KMW::_onButtonChanged({tmpjoy.GetProductName()}, {buttonId}, {newState})");

            string selectedCallbackName = selectedCallback.GetCallback();

            // First, determine if button is mapped to a dx-shift callback; auto-set togglebutton accordingly.
            string currCallback0 = tmpjoy.dx[buttonId].assign[0].GetCallback();
            if (currCallback0 == "SimHotasPinkyShift" || currCallback0 == "SimHotasShift")
            {
                this.Select_PinkyShift.IsChecked = newState;
                return;
            }

            // Nothing else to do, if this is a release.
            if (newState == false)
                return;

            // Show UI feedback, and currently mapped callback, if any -- incl consideration for shift- and release-modes.
            Pinky pinky = (this.Select_PinkyShift.IsChecked == true ? Pinky.Shift : Pinky.UnShift);
            Behaviour behaviour = (this.Select_DX_Release.IsChecked == true ? Behaviour.Release : Behaviour.Press);
            Invoke action = (this.Select_DX_Release.IsChecked == true ? Invoke.Down : Invoke.Default);

            int mappingBehavior = (int)pinky + (int)behaviour;
            string currCallback = tmpjoy.dx[buttonId].assign[mappingBehavior].GetCallback();

            if (String.IsNullOrEmpty(currCallback) || 
                currCallback == CommonConstants.SIMDONOTHING || 
                currCallback == selectedCallbackName)
            {
                this.CurrentlyMapped.Visibility = Visibility.Hidden;
            }
            else
            {
                KeyAssgn row = keyFile.LookupCallback(currCallback);
                string currCallbackDescr = row != null ? row.GetKeyDescription() : currCallback;

                string pressOrRelease = (this.Select_DX_Release.IsChecked == true) ? "release" : "press";
                this.CurrentlyMapped.Text = $"Button {pressOrRelease} currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional DX button assignment.
            tmpjoy.dx[buttonId].Assign(selectedCallbackName, pinky, behaviour, Invoke.Default, selectedCallback.GetSoundID());

            // Special-case for dx-shift: also assign shift layer to same callback.
            if (selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift")
                tmpjoy.dx[buttonId].Assign(selectedCallbackName, Pinky.Shift, Behaviour.Press, Invoke.Default, selectedCallback.GetSoundID());

            return;
        }

        private void _onPovHatChanged(JoyAssgn tmpjoy, int povhatId, int newDirection)
        {
            System.Diagnostics.Debug.WriteLine($"KMW::_onPovHatChanged({tmpjoy.GetProductName()}, {povhatId}, {newDirection})");

            string selectedCallbackName = selectedCallback.GetCallback();

            // Nothing to do, if this is a release.
            if (newDirection < 0) return;

            // Show UI feedback, and currently mapped callback, if any -- incl support for dx-shift mappings.
            Pinky pinky = (this.Select_PinkyShift.IsChecked == true ? Pinky.Shift : Pinky.UnShift);

            string currCallback = tmpjoy.pov[povhatId].GetCurrentCallback(newDirection, pinky);

            if (String.IsNullOrEmpty(currCallback) || 
                currCallback == CommonConstants.SIMDONOTHING || 
                currCallback == selectedCallbackName)
            {
                this.CurrentlyMapped.Visibility = Visibility.Hidden;
            }
            else
            {
                KeyAssgn row = keyFile.LookupCallback(currCallback);
                string currCallbackDescr = row != null ? row.GetKeyDescription() : currCallback;

                this.CurrentlyMapped.Text = $"POV-hat direction currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional POV-direction assignment.
            System.Diagnostics.Debug.WriteLine("Assigning pov hat");
            tmpjoy.pov[povhatId].Assign(newDirection, selectedCallbackName, pinky, selectedCallback.GetSoundID());

            return;
        }

        private void KeyboardButtonMonitor()
        {
            directInputDevice.GetCurrentKeyboardState();
            for (int i = 1; i < CommonConstants.KEYBOARD_KEYLENGTH; i++)
                if (directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                    HandleKeyDown();
        }

        private void HandleKeyDown()
        {
            bool Shift = false;
            bool Ctrl = false;
            bool Alt = false;
            int catchedScanCode = 0;
            directInputDevice.GetCurrentKeyboardState();
            for (int i = 1; i < 238; i++)
            {
                if (directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                {
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftShift |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightShift)
                    {
                        Shift = true;
                        continue;
                    }
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftControl |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightControl)
                    {
                        Ctrl = true;
                        continue;
                    }
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftAlt |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightAlt)
                    {
                        Alt = true;
                        continue;
                    }
                    catchedScanCode = i;
                }
            }
            if (catchedScanCode == 0)
                return;

            //QWERTY comm menu avoid.
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.Q && !Shift && !Ctrl && !Alt)
                return;
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.W && !Shift && !Ctrl && !Alt)
                return;
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.E && !Shift && !Ctrl && !Alt)
                return;
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.R && !Shift && !Ctrl && !Alt)
                return;
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.T && !Shift && !Ctrl && !Alt)
                return;
            if ((Microsoft.DirectX.DirectInput.Key)catchedScanCode == Microsoft.DirectX.DirectInput.Key.Y && !Shift && !Ctrl && !Alt)
                return;

            Pinky pinkyStatus = (Select_PinkyShift.IsChecked == true) ? Pinky.Shift : Pinky.UnShift;

            // Determine if this input is already mapped to another callback, and display warning/hint if so.
            KeyAssgn currCallbackAssgn = _keyFile.ReverseLookupKeyboardInput(catchedScanCode, Shift, Ctrl, Alt);
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
                tmpKeyboard.SetKeyboard(catchedScanCode, Shift, Ctrl, Alt);
            if (pinkyStatus == Pinky.Shift)
                tmpKeyboard.Setkeycombo(catchedScanCode, Shift, Ctrl, Alt);
        }

        private class NeutralButtons
        {
            public byte[] buttons { get; set; }
            public int[] povs { get; set; }

            public NeutralButtons(JoyAssgn joyStick)
            {
                buttons = joyStick.GetButtons();
                povs = joyStick.GetPointOfView();
            }
        }

        private void ClearDX_Click(object sender, RoutedEventArgs e)
        {
            JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            for (int i = 0; i < joyAssgns.Length; i++)
            {
                tmpJoyStick[i] = joyAssgns[i].MakeTempCloneForKeyMappingDialog();
            }
            string target = tmpKeyboard.GetCallback();
            foreach (JoyAssgn joy in tmpJoyStick)
                joy.UnassigntargetCallback(target);
        }

        private void ClearKey_Click(object sender, RoutedEventArgs e)
        {
            tmpKeyboard.UnassignKeyboard();
        }

        private class DirectInputKeyboard
        {
            Device device;
            KeyboardState keyState;
            public KeyboardState KeyboardState => keyState;

            public DirectInputKeyboard()
            {
                device = new Device(SystemGuid.Keyboard);
                device.Acquire();
            }
            public void GetCurrentKeyboardState()
            {
                keyState = device.GetCurrentKeyboardState();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            for (int i = 0; i < tmpJoyStick.Length; i++)
            {
                joyAssgns[i].CopyButtonsAndHatsFromCurrentProfile(tmpJoyStick[i]);
            }
            selectedCallback.CopyOtherKeyAssgn(tmpKeyboard);

            // Unassign the previous mapping that was assigned to this key/key combo.
            KeyAssgn oldKey = keyFile.keyAssign.FirstOrDefault(x => x != selectedCallback && x.GetKeyAssignmentStatus() == selectedCallback.GetKeyAssignmentStatus());
            if (oldKey != null)
            {
                oldKey.UnassignKeyboard();
            }

            // Save the XML and Key files, after each change user makes.
            deviceControlRef.SaveXml();
            Program.mainWin.appReg.getOverrideWriter().SaveKeyMapping(MainWindow.inGameAxis, deviceControlRef);

            Close();
        }

    }
}
