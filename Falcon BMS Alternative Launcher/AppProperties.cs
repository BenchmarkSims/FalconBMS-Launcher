using System.Windows;

using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher
{
    public class AppProperties
    {
        private MainWindow mainWindow;

        public AppProperties(MainWindow mainWindow)
        {
            Diagnostics.Log("Start Reading Launcher Settings.");

            this.mainWindow = mainWindow;

            int initialMaxButtons = SanitizeKeyMappingMaxButtons(Properties.Settings.Default.KeyMappingMaxButtons);
            if (Properties.Settings.Default.KeyMappingMaxButtons != initialMaxButtons)
            {
                Properties.Settings.Default.KeyMappingMaxButtons = initialMaxButtons;
                Properties.Settings.Default.Save();
            }

            // Load Buttons
            mainWindow.CMD_ACMI.IsOn        = Properties.Settings.Default.CMD_ACMI;
            mainWindow.CMD_WINDOW.IsOn      = Properties.Settings.Default.CMD_WINDOW;
            mainWindow.CMD_NOMOVIE.IsOn     = Properties.Settings.Default.CMD_NOMOVIE;
            mainWindow.CMD_EF.IsOn          = Properties.Settings.Default.CMD_EF;
            mainWindow.CMD_MONO.IsOn        = Properties.Settings.Default.CMD_MONO;

            mainWindow.ApplicationOverride.IsChecked       = Properties.Settings.Default.NoOverride;

            mainWindow.Misc_RollLinkedNWS.IsChecked        = Properties.Settings.Default.Misc_RLNWS;
            mainWindow.Misc_ExMouseLook.IsChecked          = Properties.Settings.Default.Misc_ExMouseLook;
            mainWindow.Misc_SmartScalingOverride.IsChecked = Properties.Settings.Default.Misc_SmartScalingOverride;
            mainWindow.Misc_NaturalHeadMovement.IsChecked  = Properties.Settings.Default.Misc_NaturalHeadMovement;
            mainWindow.Misc_PilotModel.IsChecked           = Properties.Settings.Default.Misc_PilotModel;

            SelectKeyMappingMaxButtons(initialMaxButtons);

            // Button Status Default
            if (Properties.Settings.Default.VR_Option == "SteamVR")
            {
                mainWindow.VR_NoVR.IsChecked = false;
                mainWindow.VR_SteamVR.IsChecked = true;
                mainWindow.VR_OpenXR.IsChecked = false;
            }
            else
            if (Properties.Settings.Default.VR_Option == "OpenXR")
            {
                mainWindow.VR_NoVR.IsChecked = false;
                mainWindow.VR_SteamVR.IsChecked = false;
                mainWindow.VR_OpenXR.IsChecked = true;
            }
            else
            {
                mainWindow.VR_NoVR.IsChecked = true;
                mainWindow.VR_SteamVR.IsChecked = false;
                mainWindow.VR_OpenXR.IsChecked = false;
            }

            if (Properties.Settings.Default.Misc_bExportRTTTextures)
            {
                mainWindow.RTT_Enable.IsChecked = true;
                mainWindow.RTT_Disable.IsChecked = false;
            }
            else
            {
                mainWindow.RTT_Disable.IsChecked = true;
                mainWindow.RTT_Enable.IsChecked = false;
            }

            if (Properties.Settings.Default.Misc_TrackIRZ)
            {
                mainWindow.TrackIRZ_ZoomFOV.IsChecked = true;
                mainWindow.TrackIRZ_HeadForward.IsChecked = false;
            }
            else
            {
                mainWindow.TrackIRZ_HeadForward.IsChecked = true;
                mainWindow.TrackIRZ_ZoomFOV.IsChecked = false;
            }

            mainWindow.AB_Throttle.Visibility       = Visibility.Hidden;
            mainWindow.AB_Throttle_Right.Visibility = Visibility.Hidden;

            Diagnostics.Log("Finished Reading Launcher Settings.");
        }

        public void SaveUISetup()
        {
            Properties.Settings.Default.CMD_ACMI                  = (bool)mainWindow.CMD_ACMI.IsOn;
            Properties.Settings.Default.CMD_WINDOW                = (bool)mainWindow.CMD_WINDOW.IsOn;
            Properties.Settings.Default.CMD_NOMOVIE               = (bool)mainWindow.CMD_NOMOVIE.IsOn;
            Properties.Settings.Default.CMD_EF                    = (bool)mainWindow.CMD_EF.IsOn;
            Properties.Settings.Default.CMD_MONO                  = (bool)mainWindow.CMD_MONO.IsOn;

            Properties.Settings.Default.NoOverride                = (bool)mainWindow.ApplicationOverride.IsChecked;
            
            Properties.Settings.Default.Misc_RLNWS                = (bool)mainWindow.Misc_RollLinkedNWS.IsChecked;
            Properties.Settings.Default.Misc_TrackIRZ             = (mainWindow.TrackIRZ_ZoomFOV.IsChecked == true);
            Properties.Settings.Default.Misc_ExMouseLook          = (bool)mainWindow.Misc_ExMouseLook.IsChecked;
            Properties.Settings.Default.Misc_SmartScalingOverride = (bool)mainWindow.Misc_SmartScalingOverride.IsChecked;
            Properties.Settings.Default.Misc_NaturalHeadMovement  = (bool)mainWindow.Misc_NaturalHeadMovement.IsChecked;
            Properties.Settings.Default.Misc_PilotModel           = (bool)mainWindow.Misc_PilotModel.IsChecked;
            Properties.Settings.Default.VR_Option = (bool)mainWindow.VR_SteamVR.IsChecked ? "SteamVR" : (bool)mainWindow.VR_OpenXR.IsChecked ? "OpenXR" : "NoVR";
            Properties.Settings.Default.Misc_bExportRTTTextures   = (mainWindow.RTT_Enable.IsChecked == true);
            Properties.Settings.Default.KeyMappingMaxButtons      = GetSelectedKeyMappingMaxButtons();
            Properties.Settings.Default.Save();
        }

        private void SelectKeyMappingMaxButtons(int maxButtons)
        {
            int sanitizedMaxButtons = SanitizeKeyMappingMaxButtons(maxButtons);

            for (int i = 0; i < mainWindow.KeyMappingMaxButtonsDropdown.Items.Count; i++)
            {
                System.Windows.Controls.ComboBoxItem comboItem = mainWindow.KeyMappingMaxButtonsDropdown.Items[i] as System.Windows.Controls.ComboBoxItem;
                if (comboItem == null)
                    continue;

                int itemValue;
                if (int.TryParse(comboItem.Content.ToString(), out itemValue) && itemValue == sanitizedMaxButtons)
                {
                    mainWindow.KeyMappingMaxButtonsDropdown.SelectedIndex = i;
                    return;
                }
            }
        }

        private int GetSelectedKeyMappingMaxButtons()
        {
            System.Windows.Controls.ComboBoxItem selectedItem = mainWindow.KeyMappingMaxButtonsDropdown.SelectedItem as System.Windows.Controls.ComboBoxItem;
            int maxButtons;
            if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out maxButtons))
                return SanitizeKeyMappingMaxButtons(maxButtons);

            return 128;
        }

        private static int SanitizeKeyMappingMaxButtons(int maxButtons)
        {
            switch (maxButtons)
            {
                case 16:
                case 32:
                case 64:
                case 128:
                    return maxButtons;
                default:
                    return 128;
            }
        }
    }
}
