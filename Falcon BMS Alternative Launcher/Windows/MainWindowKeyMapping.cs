using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher.Windows
{

    public partial class MainWindow
    {
        private bool _isShiftButtonPressed = false;

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

            KeyMappingWindow.ShowKeyMappingWindow(this, deviceControl, selectedItem);

            KeyMappingGrid.Items.Refresh();
            KeyMappingGrid.UnselectAllCells();
        }


        private void MainWindow_KeyboardInputReceived( int key_scancode, int shift_flags, bool new_state )
        {
            if (!this.IsActive) return;
            //Debug.WriteLine($"MainWindow_KeyboardInputReceived({key_scancode}, {shift_flags}, {new_state})");

            KeyAssgn keytmp = KeyFile.ParseKeyfileLine(@"SimDoNothing -1 0 0xFFFFFFFF 0 0 0 -1 ""nothing""");
            keytmp.SetKeyboard(key_scancode, shift_flags);
            string keytmp_assgn_text = keytmp.GetKeyAssignmentStatus();

            Label_AssgnStatus.Content = "INPUT " + keytmp_assgn_text;

            // If the key assignment was found, jump to the mapping for it and highlight it.
            KeyAssgn key = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetKeyAssignmentStatus() == keytmp_assgn_text);
            if (key != null)
            {
                Label_AssgnStatus.Content += "\t/" + key.Mapping;

                KeyMappingGrid.Items.Refresh();
                KeyMappingGrid.UpdateLayout();
                KeyMappingGrid.ScrollIntoView(key);
                KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(key);
            }

            return;
        }

        private void MainWindow_ButtonInputReceived( Guid device_guid, int button_id, bool new_state )
        {
            if (!this.IsActive) return;
            //Debug.WriteLine($"MainWindow_ButtonInputReceived({device_guid}, {button_id}, {new_state})");

            if (new_state)
                MainWindow_ButtonInputReceived_Press(device_guid, button_id);
            else
                MainWindow_ButtonInputReceived_Release(device_guid, button_id);
            return;
        }
        void MainWindow_ButtonInputReceived_Press( Guid device_guid, int button_id)
        {
            JoyAssgn joy = deviceControl.GetJoystickMappingForDeviceId(device_guid);

            // Lookup callback - perhaps shifted.
            int mappingBehavior = (int)Behaviour.Press;
            if (_isShiftButtonPressed) mappingBehavior += (int)Pinky.Shift;

            string target = joy.dx[button_id].assign[mappingBehavior].GetCallback();

            // If target is dx-shift callback, set flag for subsequent button presses.
            if (target == "SimHotasPinkyShift" || target == "SimHotasShift")
                _isShiftButtonPressed = true;

            // Update UI.
            if (String.IsNullOrEmpty(target) || target == CommonConstants.SIMDONOTHING)
            {
                Label_AssgnStatus.Content =
                    "DX" + (button_id + 1) +
                    " (" + joy.GetSanitizedProductName() + ")";
            }
            else
            {
                // If we have a row for this mapping, jump to it and highlight it.
                KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

                Label_AssgnStatus.Content =
                    "DX" + (button_id + 1) +
                    " (" + joy.GetSanitizedProductName() + ")" + " / " +
                    ((row != null) ? row.Mapping : target);

                if (row != null)
                {
                    Debug.Assert(KeyMappingGrid.Items.IndexOf(row) >= 0);

                    KeyMappingGrid.ScrollIntoView(row);
                    KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(row);
                }
            }
        }
        void MainWindow_ButtonInputReceived_Release( Guid device_guid, int button_id)
        {
            JoyAssgn joy = deviceControl.GetJoystickMappingForDeviceId(device_guid);

            // Look for a release-behavior callback - perhaps shifted.
            int mappingBehavior = (int)Behaviour.Release;
            if (_isShiftButtonPressed) mappingBehavior += (int)Pinky.Shift;

            string target = joy.dx[button_id].assign[mappingBehavior].GetCallback();

            if (!String.IsNullOrEmpty(target) && target != CommonConstants.SIMDONOTHING)
            {
                // If we have a row for this mapping, jump to it and highlight it.
                KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

                Label_AssgnStatus.Content =
                    "DX" + (button_id + 1) + ".RELEASE" +
                    " (" + joy.GetSanitizedProductName() + ")" + " / " +
                    ((row != null) ? row.Mapping : target);

                if (row != null)
                {
                    KeyMappingGrid.ScrollIntoView(row);
                    KeyMappingGrid.SelectedIndex = KeyMappingGrid.Items.IndexOf(row);
                }
            }

            // If the press-mode callback is dx-shift, clear the shift-flag.
            string target0 = joy.dx[button_id].assign[0].GetCallback();
            if (target0 == "SimHotasPinkyShift" || target0 == "SimHotasShift")
                _isShiftButtonPressed = false;

        }

        private void MainWindow_PovInputReceived( Guid device_guid, int povhat_id, int new_direction )
        {
            if (!this.IsActive) return;
            //Debug.WriteLine($"MainWindow_PovInputReceived({device_guid}, {povhat_id}, {new_direction})");

            if (new_direction < 0) return; // -1 means hat is centered

            JoyAssgn joy = deviceControl.GetJoystickMappingForDeviceId(device_guid);

            // Show UI feedback, and currently mapped callback, if any -- including support for dx-shift mapping.
            string target = joy.pov[povhat_id].direction[new_direction].GetCallback(_isShiftButtonPressed ? Pinky.Shift : Pinky.UnShift);

            string dirlabel = PovAssgn.GetDirectionLabel(new_direction);

            // Update UI.
            if (String.IsNullOrEmpty(target) || target == CommonConstants.SIMDONOTHING)
            {
                Label_AssgnStatus.Content =
                    "POV" + (povhat_id + 1) + "." + dirlabel +
                    " (" + joy.GetSanitizedProductName() + ")";
                return;
            }

            // If we have a row for this mapping, jump to it and highlight it.
            KeyAssgn row = deviceControl.GetKeyBindings().keyAssign.FirstOrDefault(x => x.GetCallback() == target);

            Label_AssgnStatus.Content =
                "POV" + (povhat_id + 1) + "." + dirlabel +
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