using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using FalconBMS.Launcher.Input;

using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private List<ButtonStateTracker> _buttonTrackers;
        private bool _isShiftButtonPressed = false;
        private int _keyMappingMaxButtons = SanitizeKeyMappingMaxButtonsValue(Properties.Settings.Default.KeyMappingMaxButtons);

        private static int SanitizeKeyMappingMaxButtonsValue(int maxButtons)
        {
            switch (maxButtons)
            {
                case 16:
                case 32:
                case 64:
                case 128:
                    return maxButtons;
                default:
                    return CommonConstants.DX_MAX_BUTTONS;
            }
        }

        private int GetSelectedKeyMappingMaxButtons()
        {
            ComboBoxItem selectedItem = KeyMappingMaxButtonsDropdown.SelectedItem as ComboBoxItem;
            int maxButtons;
            if (selectedItem != null && Int32.TryParse(selectedItem.Content.ToString(), out maxButtons))
                return maxButtons;

            return CommonConstants.DX_MAX_BUTTONS;
        }

        private int SanitizeKeyMappingMaxButtons(int maxButtons)
        {
            return SanitizeKeyMappingMaxButtonsValue(maxButtons);
        }

        public void UpdateCategoryHeaders()
        {
            List<string> categoryHeaders = new List<string>(12) { "TOP" };
            foreach (string s in deviceControl.GetKeyBindings().categoryHeaderLabels)
                categoryHeaders.Add(s.TrimStart('\x22').TrimEnd('\x22')); // trim off leading/trailing doublequotes

            this.Category.ItemsSource = categoryHeaders.ToArray();
            return;
        }

        public void UpdateDataGridBindingSource()
        {
            foreach (KeyAssgn Assgn in deviceControl.GetKeyBindings().keyAssign)
                Assgn.Visibility = Assgn.GetVisibility();

            KeyMappingGrid.ItemsSource = deviceControl.GetKeyBindings().keyAssign;

            ResetButtonTrackers();
        }

        public void ResetButtonTrackers()
        {
            JoyAssgn[] joyAssgns = deviceControl.GetJoystickMappings();

            _buttonTrackers = new List<ButtonStateTracker>(joyAssgns.Length);
            foreach (JoyAssgn joy in joyAssgns)
                _buttonTrackers.Add(new ButtonStateTracker(joy, _onButtonChanged, _onPovHatChanged));

            return;
        }

        /// <summary>
        /// Initialize Datagrid Columns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Mapping":
                    e.Column.Header = "Mapping";
                    e.Column.DisplayIndex = 0;
                    break;
                case "Key":
                    e.Column.Header = "Key";
                    e.Column.DisplayIndex = 1;
                    break;
                case "Visibility":
                    // Do not show
                    e.Column.DisplayIndex = 2;
                    e.Cancel = true;
                    break;
            }
            Category.SelectedIndex = 0;
            if (!e.PropertyName.Contains("Z_Joy_"))
                return;
            int target = int.Parse(e.PropertyName.Replace("Z_Joy_", ""));
            JoyAssgn[] joyAssgns = deviceControl.GetJoystickMappings();
            if (target >= joyAssgns.Length)
            {
                e.Cancel = true;
                return;
            }
            e.Column.Header = joyAssgns[target].GetSanitizedProductName();
            //e.Column.Width = 128;
            e.Column.DisplayIndex = 3 + target;
        }

        /// <summary>
        /// Unassign keyboard key or joystick button when double clicked a Datagrid cell.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_MouseButtonDoubleClick(object sender, MouseButtonEventArgs e)
        {
            KeyAssgn selectedItem = (KeyAssgn)KeyMappingGrid.SelectedItem;
            if (selectedItem == null)
                return;
            if (KeyMappingGrid.CurrentColumn == null)
                return;
            if (selectedItem.GetVisibility() != "White")
                return;
            if (selectedItem.GetCallback() == CommonConstants.SIMDONOTHING)
                return;

            KeyMappingWindow.ShowKeyMappingWindow(this, deviceControl, selectedItem, _keyMappingMaxButtons);

            KeyMappingGrid.Items.Refresh();
            KeyMappingGrid.UnselectAllCells();
        }

        /// <summary>
        /// Check your keyboard/joysticks button behaviour every 60 frames per seconds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void MainWindowKeyMapping_HandleTimerTick()
        {
            // Don't burn CPU displaying input events, if our window is inactive.
            Window activeWin = Program.activeWin;
            if (!activeWin.IsActive) return;

            directInputDevice.GetCurrentKeyboardState();
            for (int i = 1; i < 238; i++)
                if (directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                    KeyMappingGrid_KeyDown();
            
            JumptoAssignedKey();
        }

        /// <summary>
        /// You pressed a joystick button to search which callback is it assigned to? OK let's go there.
        /// </summary>
        public void JumptoAssignedKey()
        {
            // Invoke _onButtonChanged, _onPovHatChanged callbacks, below.
            try
            {
                foreach (ButtonStateTracker tracker in _buttonTrackers)
                    tracker.PollUpdate();
            }
            catch (Exception ex)
            {
                // Typically, InputLostException from DirectInput.
                Diagnostics.Log(ex);
            }

            return;
        }

        private void _onButtonChanged(JoyAssgn joy, int buttonId, bool newState)
        {
            System.Diagnostics.Debug.WriteLine($"_onButtonChanged({joy.GetSanitizedProductName()}, {buttonId}, {newState})");

            if (buttonId >= _keyMappingMaxButtons)
                return;

            if (newState == true) // button pressed
            {
                // Lookup callback - perhaps shifted.
                int mappingBehavior = (int)Behaviour.Press;
                if (_isShiftButtonPressed) mappingBehavior += (int)Pinky.Shift;

                string target = joy.dx[buttonId].assign[mappingBehavior].GetCallback();

                // If target is dx-shift callback, set flag for subsequent button presses.
                if (target == "SimHotasPinkyShift" || target == "SimHotasShift")
                    _isShiftButtonPressed = true;

                // Update UI.
                if (String.IsNullOrEmpty(target) || target == CommonConstants.SIMDONOTHING)
                {
                    Label_AssgnStatus.Content =
                        "DX" + (buttonId + 1) +
                        " (" + joy.GetSanitizedProductName() + ")";
                }
                else
                {
                    // If we have a row for this mapping, jump to it and highlight it.
                    KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

                    Label_AssgnStatus.Content =
                        "DX" + (buttonId + 1) +
                        " (" + joy.GetSanitizedProductName() + ")" + " / " +
                        ((row != null) ? row.Mapping : target);

                    if (row != null)
                    {
                        KeyMappingGrid.ScrollIntoView(row);
                        KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(row);
                    }
                }
            }
            else // button released
            {
                // Look for a release-behavior callback - perhaps shifted.
                int mappingBehavior = (int)Behaviour.Release;
                if (_isShiftButtonPressed) mappingBehavior += (int)Pinky.Shift;

                string target = joy.dx[buttonId].assign[mappingBehavior].GetCallback();

                if (!String.IsNullOrEmpty(target) && target != CommonConstants.SIMDONOTHING)
                {
                    // If we have a row for this mapping, jump to it and highlight it.
                    KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

                    Label_AssgnStatus.Content =
                        "DX" + (buttonId + 1) + ".RELEASE" +
                        " (" + joy.GetSanitizedProductName() + ")" + " / " +
                        ((row != null) ? row.Mapping : target);

                    if (row != null)
                    {
                        KeyMappingGrid.ScrollIntoView(row);
                        KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(row);
                    }
                }

                // If the press-mode callback is dx-shift, clear the shift-flag.
                string target0 = joy.dx[buttonId].assign[0].GetCallback();
                if (target0 == "SimHotasPinkyShift" || target0 == "SimHotasShift")
                    _isShiftButtonPressed = false;
            }

            return;
        }

        private void _onPovHatChanged(JoyAssgn joy, int povhatId, int newDirection)
        {
            System.Diagnostics.Debug.WriteLine($"_onPovHatChanged({joy.GetSanitizedProductName()}, {povhatId}, {newDirection})");

            if (newDirection < 0) return;

            // Show UI feedback, and currently mapped callback, if any -- including support for dx-shift mapping.
            string target = joy.pov[povhatId].direction[newDirection].GetCallback(_isShiftButtonPressed ? Pinky.Shift : Pinky.UnShift);

            string dirlabel = PovAssgn.GetDirectionLabel(newDirection);

            if (String.IsNullOrEmpty(target) || target == CommonConstants.SIMDONOTHING)
            {
                Label_AssgnStatus.Content =
                    "POV" + (povhatId + 1) + "." + dirlabel +
                    " (" + joy.GetSanitizedProductName() + ")";
                return;
            }

            // If we have a row for this mapping, jump to it and highlight it.
            KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

            Label_AssgnStatus.Content =
                "POV" + (povhatId + 1) + "." + dirlabel + 
                " (" + joy.GetSanitizedProductName() + ")" + " / " +
                ((row != null) ? row.Mapping : target);

            if (row != null)
            {
                KeyMappingGrid.ScrollIntoView(row);
                KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(row);
            }

            return;
        }

        /// <summary>
        /// You pressed keyboard keys? I will check which key was pressed with Shift/Ctrl/Alt.
        /// </summary>
        private void KeyMappingGrid_KeyDown()
        {
            if (SearchBox.IsSelectionActive)
                return;
            if (SearchBox.IsFocused)
                return;
            if (SearchBox.IsKeyboardFocused)
                return;

            bool Shift = false;
            bool Ctrl = false;
            bool Alt = false;

            int catchedScanCode = 0;

            directInputDevice.GetCurrentKeyboardState();

            for (int i = 1; i < CommonConstants.KEYBOARD_KEYLENGTH; i++)
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

            KeyAssgn keytmp = KeyFile.ParseKeyfileLine(@"SimDoNothing -1 0 0xFFFFFFFF 0 0 0 -1 ""nothing""");
            keytmp.SetKeyboard(catchedScanCode, Shift, Ctrl, Alt);
            Label_AssgnStatus.Content = "INPUT " + keytmp.GetKeyAssignmentStatus();

            // If the key assignment was found, jump to the mapping for it and highlight it.
            KeyAssgn key = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetKeyAssignmentStatus() == keytmp.GetKeyAssignmentStatus());
            if (key != null)
            {
                Label_AssgnStatus.Content += "\t/" + key.Mapping;

                KeyMappingGrid.UpdateLayout();
                KeyMappingGrid.ScrollIntoView(key);
                KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(key);
            }
        }

        /// <summary>
        /// So this was... keyboard, I suppose.
        /// </summary>
        DirectInputKeyboard directInputDevice = new DirectInputKeyboard();
        class DirectInputKeyboard
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

        /// <summary>
        /// Let's jump to a category you have selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeyMappingGrid.Items == null || KeyMappingGrid.Items.Count == 0)
                return;

            int selectedIndex = Category.SelectedIndex;

            if (selectedIndex <= 0)
            {
                // Go to top.
                KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[0]);
                KeyMappingGrid.UpdateLayout();
                return;
            }
            --selectedIndex; //nb: offset for "TOP" element zero

            string[] catLabels = deviceControl.GetKeyBindings().categoryHeaderLabels;
            if (selectedIndex >= catLabels.Length)
                return;

            string cat = catLabels[selectedIndex];

            int i = 0;
            foreach (KeyAssgn keys in deviceControl.GetKeyBindings().keyAssign)
            {
                if (i >= KeyMappingGrid.Items.Count)
                    break;

                if (0 == String.CompareOrdinal(keys.GetKeyDescription(), cat))
                {
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[KeyMappingGrid.Items.Count - 1]);
                    KeyMappingGrid.UpdateLayout();
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[i]);
                    return;
                }
                i++;
            }
            Diagnostics.Log("Unable to locate selected category in keyfile: " + cat);
            return;
        }

        /// <summary>
        /// User has changed the text in the search box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            KeyMappingGrid.UnselectAllCells();
            string filter = SearchBox.Text.Trim().ToLower();
            bool isFilterEmpty = string.IsNullOrWhiteSpace(filter);

            // Enable the category selector only if there's no search filter active.
            Category.IsEnabled = isFilterEmpty;

            KeyMappingGrid.Items.Filter = x => isFilterEmpty || ((KeyAssgn)x).Mapping.Trim().ToLower().Contains(filter);
        }

        private void KeyMappingMaxButtonsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(KeyMappingMaxButtonsDropdown.SelectedItem is ComboBoxItem))
                return;

            _keyMappingMaxButtons = SanitizeKeyMappingMaxButtons(GetSelectedKeyMappingMaxButtons());
            Properties.Settings.Default.KeyMappingMaxButtons = _keyMappingMaxButtons;
            Properties.Settings.Default.Save();

            if (appReg != null && deviceControl != null)
                appReg.getOverrideWriter().SaveConfigOverrides(MainWindow.inGameAxis, deviceControl);
        }

        private void ProfileSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deviceControl == null) 
                return;

            ComboBoxItem selectedItem = ProfileSelect.SelectedItem as ComboBoxItem;
            if (selectedItem == null) 
                return;

            string newTag = selectedItem.Tag as string;
            if (newTag == null) return;

            switch (newTag)
            {
                case "F16":
                    if (DeviceControl.avionicsProfile == null) return;//no change
                    deviceControl.UpdateAvionicsProfile(null);//default is F16
                    break;
                case "F15":
                    if (DeviceControl.avionicsProfile == CommonConstants.F15_TAG) return;//no change
                    deviceControl.UpdateAvionicsProfile(CommonConstants.F15_TAG);
                    break;
            }

            UpdateCategoryHeaders();

            UpdateDataGridBindingSource();
            return;
        }

    }
}