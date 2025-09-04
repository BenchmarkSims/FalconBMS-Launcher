using System.Windows;

using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher
{
    public class AppProperties
    {
        private MainWindow mainWindow;
        public int bandWidthDefault = 1024;

        public AppProperties(MainWindow mainWindow)
        {
            Diagnostics.Log("Start Reading Launcher Settings.");

            this.mainWindow = mainWindow;

            // Load Buttons
            mainWindow.CMD_ACMI.IsOn        = Properties.Settings.Default.CMD_ACMI;
            mainWindow.CMD_WINDOW.IsOn      = Properties.Settings.Default.CMD_WINDOW;
            mainWindow.CMD_NOMOVIE.IsOn     = Properties.Settings.Default.CMD_NOMOVIE;
            mainWindow.CMD_EF.IsOn          = Properties.Settings.Default.CMD_EF;
            mainWindow.CMD_MONO.IsOn        = Properties.Settings.Default.CMD_MONO;
            bandWidthDefault                               = Properties.Settings.Default.CMD_BW;
            mainWindow.ApplicationOverride.IsChecked       = Properties.Settings.Default.NoOverride;
            mainWindow.Misc_RollLinkedNWS.IsChecked        = Properties.Settings.Default.Misc_RLNWS;
            mainWindow.Misc_ExMouseLook.IsChecked          = Properties.Settings.Default.Misc_ExMouseLook;
            mainWindow.Misc_SmartScalingOverride.IsChecked = Properties.Settings.Default.Misc_SmartScalingOverride;
            mainWindow.Misc_NaturalHeadMovement.IsChecked  = Properties.Settings.Default.Misc_NaturalHeadMovement;
            mainWindow.Misc_PilotModel.IsChecked           = Properties.Settings.Default.Misc_PilotModel;

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

            mainWindow.CMD_BW.Content               = "BW : " + bandWidthDefault;
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
            Properties.Settings.Default.CMD_BW                    = bandWidthDefault;
            Properties.Settings.Default.NoOverride                = (bool)mainWindow.ApplicationOverride.IsChecked;
            Properties.Settings.Default.Misc_RLNWS                = (bool)mainWindow.Misc_RollLinkedNWS.IsChecked;
            Properties.Settings.Default.Misc_TrackIRZ             = (mainWindow.TrackIRZ_ZoomFOV.IsChecked == true);
            Properties.Settings.Default.Misc_ExMouseLook          = (bool)mainWindow.Misc_ExMouseLook.IsChecked;
            Properties.Settings.Default.Misc_SmartScalingOverride = (bool)mainWindow.Misc_SmartScalingOverride.IsChecked;
            Properties.Settings.Default.Misc_NaturalHeadMovement  = (bool)mainWindow.Misc_NaturalHeadMovement.IsChecked;
            Properties.Settings.Default.Misc_PilotModel           = (bool)mainWindow.Misc_PilotModel.IsChecked;
            Properties.Settings.Default.VR_Option = (bool)mainWindow.VR_SteamVR.IsChecked ? "SteamVR" : (bool)mainWindow.VR_OpenXR.IsChecked ? "OpenXR" : "NoVR";
            Properties.Settings.Default.Misc_bExportRTTTextures   = (mainWindow.RTT_Enable.IsChecked == true);
            Properties.Settings.Default.Save();
        }

        public void CMD_BW_Click()
        {
            bandWidthDefault *= 2;
            if (bandWidthDefault > 10000)
            {
                bandWidthDefault = 512;
            }
            mainWindow.CMD_BW.Content = "BW : " + bandWidthDefault;
        }
    }
}
