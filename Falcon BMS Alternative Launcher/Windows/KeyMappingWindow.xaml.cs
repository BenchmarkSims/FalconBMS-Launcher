using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
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
        private DeviceControl _deviceControlRef;

        private KeyFile _keyFile;
        private KeyAssgn _selectedCallback;

        private KeyAssgn _tmpKeyboard;
        private JoyAssgn[] _tmpJoyAssgns;
        private List<ButtonStateTracker> _buttonTrackers = new List<ButtonStateTracker>();

        private DirectInputKeyboard _directInputDevice = new DirectInputKeyboard();

        private int _tickNextUIFlush1 = Environment.TickCount;
        private int _tickNextUIFlush2 = Environment.TickCount;
        private int _maxButtonsToShow = CommonConstants.DX_MAX_BUTTONS;

        public KeyMappingWindow(DeviceControl deviceControl, KeyAssgn selectedCallback)
        {
            InitializeComponent();

            this._selectedCallback = selectedCallback;

            this._deviceControlRef = deviceControl;
            this._keyFile = deviceControl.GetKeyBindings();

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

        private int GetSelectedMaxButtons()
        {
            ComboBoxItem selectedItem = MaxButtonsDropdown.SelectedItem as ComboBoxItem;
            int maxButtons;
            if (selectedItem != null && Int32.TryParse(selectedItem.Content.ToString(), out maxButtons))
                return maxButtons;

            return CommonConstants.DX_MAX_BUTTONS;
        }

        private void MaxButtonsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _maxButtonsToShow = GetSelectedMaxButtons();
            ShowAssignedStatus();
        }

        private void CloneTempDialogData()
        {
            _tmpKeyboard = _selectedCallback.Clone();

            JoyAssgn[] joyAssgns = _deviceControlRef.GetJoystickMappings();

            _tmpJoyAssgns = new JoyAssgn[joyAssgns.Length];
            _buttonTrackers = new List<ButtonStateTracker>(joyAssgns.Length);

            for (int i = 0; i < joyAssgns.Length; i++)
            {
                JoyAssgn tmpjoy = joyAssgns[i].MakeTempCloneForKeyMappingDialog();
                _tmpJoyAssgns[i] = tmpjoy;
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
            //JoyAssgn[] joyAssgns = deviceControlRef.GetJoystickMappings();

            var sb = new StringBuilder(500);
            sb.Append(_tmpKeyboard.GetKeyAssignmentStatus());
            if (sb.Length > 0) sb.Append("; ");

            for (int i = 0; i < _tmpJoyAssgns.Length; i++)
                sb.Append(_tmpKeyboard.ReadJoyAssignment(i, _tmpJoyAssgns, _maxButtonsToShow));

            string currentKeyAndButtons = sb.ToString();
            MappedButton.Content = currentKeyAndButtons;

            if (currentKeyAndButtons.Length == 0)
            {
                if (Environment.TickCount > _tickNextUIFlush1)
                    AwaitingInputs.Content = "";
                if (Environment.TickCount > _tickNextUIFlush2)
                {
                    AwaitingInputs.Content = "   AWAITING INPUTS";

                    _tickNextUIFlush1 = Environment.TickCount + CommonConstants.FLUSHTIME1;
                    _tickNextUIFlush2 = Environment.TickCount + CommonConstants.FLUSHTIME2;
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
            // Invoke _onButtonChanged, _onPovHatChanged callbacks, below.
            foreach (ButtonStateTracker tracker in _buttonTrackers)
                tracker.PollUpdate();

            return;
        }

        private void _onButtonChanged(JoyAssgn tmpjoy, int buttonId, bool newState)
        {
            System.Diagnostics.Debug.WriteLine($"KMW::_onButtonChanged({tmpjoy.GetSanitizedProductName()}, {buttonId}, {newState})");

            if (buttonId >= _maxButtonsToShow)
                return;

            string selectedCallbackName = _selectedCallback.GetCallback();

            // First, determine if button is mapped to a dx-shift callback; auto-set togglebutton accordingly.
            string currCallback0 = tmpjoy.dx[buttonId].assign[0].GetCallback();
            if (currCallback0 == "SimHotasPinkyShift" || currCallback0 == "SimHotasShift")
            {
                this.Select_PinkyShift.IsOn = newState;
                return;
            }

            // Nothing else to do, if this is a release.
            if (newState == false)
                return;

            // Show UI feedback, and currently mapped callback, if any -- incl consideration for shift- and release-modes.
            Pinky pinky = (this.Select_PinkyShift.IsOn == true ? Pinky.Shift : Pinky.UnShift);
            Behaviour behaviour = (this.Select_DX_Release.IsOn == true ? Behaviour.Release : Behaviour.Press);
            Invoke action = (this.Select_DX_Release.IsOn == true ? Invoke.Down : Invoke.Default);

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
                KeyAssgn row = _keyFile.LookupCallback(currCallback);
                string currCallbackDescr = row != null ? row.GetKeyDescription() : currCallback;

                string pressOrRelease = (this.Select_DX_Release.IsOn == true) ? "release" : "press";
                this.CurrentlyMapped.Text = $"Button {pressOrRelease} currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional DX button assignment.
            tmpjoy.dx[buttonId].Assign(selectedCallbackName, pinky, behaviour, action, _selectedCallback.GetSoundID());

            // Special-case for dx-shift: also assign shift layer to same callback.
            if (selectedCallbackName == "SimHotasPinkyShift" || selectedCallbackName == "SimHotasShift")
                tmpjoy.dx[buttonId].Assign(selectedCallbackName, Pinky.Shift, Behaviour.Press, Invoke.Default, _selectedCallback.GetSoundID());

            return;
        }

        private void _onPovHatChanged(JoyAssgn tmpjoy, int povhatId, int newDirection)
        {
            System.Diagnostics.Debug.WriteLine($"KMW::_onPovHatChanged({tmpjoy.GetSanitizedProductName()}, {povhatId}, {newDirection})");

            string selectedCallbackName = _selectedCallback.GetCallback();

            // Nothing to do, if this is a release.
            if (newDirection < 0) return;

            // Show UI feedback, and currently mapped callback, if any -- incl support for dx-shift mappings.
            Pinky pinky = (this.Select_PinkyShift.IsOn == true ? Pinky.Shift : Pinky.UnShift);

            string currCallback = tmpjoy.pov[povhatId].GetCurrentCallback(newDirection, pinky);

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

                this.CurrentlyMapped.Text = $"POV-hat direction currently bound to:\r\n" + currCallbackDescr;
                this.CurrentlyMapped.Visibility = Visibility.Visible;
            }

            // Construct provisional POV-direction assignment.
            System.Diagnostics.Debug.WriteLine("Assigning pov hat");
            tmpjoy.pov[povhatId].Assign(newDirection, selectedCallbackName, pinky, _selectedCallback.GetSoundID());

            return;
        }

        private void KeyboardButtonMonitor()
        {
            _directInputDevice.GetCurrentKeyboardState();
            for (int i = 1; i < CommonConstants.KEYBOARD_KEYLENGTH; i++)
                if (_directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                    HandleKeyDown();
        }

        private void HandleKeyDown()
        {
            bool Shift = false;
            bool Ctrl = false;
            bool Alt = false;
            int catchedScanCode = 0;
            _directInputDevice.GetCurrentKeyboardState();
            for (int i = 1; i < 238; i++)
            {
                if (_directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
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

            Pinky pinkyStatus = (Select_PinkyShift.IsOn == true) ? Pinky.Shift : Pinky.UnShift;

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
                _tmpKeyboard.SetKeyboard(catchedScanCode, Shift, Ctrl, Alt);
            if (pinkyStatus == Pinky.Shift)
                _tmpKeyboard.Setkeycombo(catchedScanCode, Shift, Ctrl, Alt);
        }

        private void ClearDX_Click(object sender, RoutedEventArgs e)
        {
            // Make fresh clones, and button-trackers.
            CloneTempDialogData();

            // Remove the current DX button/povhat bindings.
            string targetCallback = _selectedCallback.GetCallback();

            foreach (JoyAssgn tmpjoy in _tmpJoyAssgns)
                tmpjoy.UnassigntargetCallback(targetCallback);
        }

        private void ClearKey_Click(object sender, RoutedEventArgs e)
        {
            _tmpKeyboard.UnassignKeyboard();
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
            JoyAssgn[] joyAssgns = _deviceControlRef.GetJoystickMappings();

            for (int i = 0; i < _tmpJoyAssgns.Length; i++)
            {
                joyAssgns[i].CopyButtonsAndHatsFromCurrentProfile(_tmpJoyAssgns[i]);
            }
            _selectedCallback.CopyOtherKeyAssgn(_tmpKeyboard);

            // Unassign the previous mapping that was assigned to this key/key combo.
            KeyAssgn oldKey = _keyFile.keyAssign.FirstOrDefault(x => x != _selectedCallback && x.GetKeyAssignmentStatus() == _selectedCallback.GetKeyAssignmentStatus());
            if (oldKey != null)
            {
                oldKey.UnassignKeyboard();
            }

            // Save the XML and Key files, after each change user makes.
            _deviceControlRef.SaveXml();
            Program.mainWin.appReg.getOverrideWriter().SaveKeyMapping(MainWindow.inGameAxis, _deviceControlRef);

            Close();
        }

    }
}
