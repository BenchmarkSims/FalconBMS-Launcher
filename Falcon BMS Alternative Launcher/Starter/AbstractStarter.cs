using System.IO;
using System.ServiceModel.Syndication;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Starter
{
    public abstract class AbstractStarter
    {
        protected AppRegInfo appReg;
        protected MainWindow mainWindow;

        protected string url;

        public AbstractStarter(AppRegInfo appReg, MainWindow mainWindow)
        {
            this.appReg     = appReg;
            this.mainWindow = mainWindow;
        }

        public abstract void execute(object sender);

        public virtual string getCommandLine()
        {
            string strCmdText = "";
            if (mainWindow.CMD_ACMI.IsOn == true)
                strCmdText += "-acmi ";
            if (mainWindow.CMD_WINDOW.IsOn == true)
                strCmdText += "-window ";
            if (mainWindow.CMD_NOMOVIE.IsOn == true)
                strCmdText += "-nomovie ";
            if (mainWindow.CMD_EF.IsOn == true)
                strCmdText += "-ef ";
            if (mainWindow.CMD_MONO.IsOn == true)
                strCmdText += "-mono ";

            if (mainWindow.VR_SteamVR.IsVisible && mainWindow.VR_SteamVR.IsChecked == true)
                strCmdText += "-vr ";
            else if (mainWindow.VR_OpenXR.IsVisible && mainWindow.VR_OpenXR.IsChecked == true)
                strCmdText += "-xr ";
            else
                strCmdText += "-novr ";

            return strCmdText;
        }

        public void NewAxisFrom433(bool flg)
        {
            if (flg)
            {
                mainWindow.Name_FLIR_Brightness.Visibility = Visibility.Visible;
                mainWindow.Label_FLIR_Brightness.Visibility = Visibility.Visible;
                mainWindow.Axis_FLIR_Brightness.Visibility = Visibility.Visible;
                mainWindow.FLIR_Brightness.Visibility = Visibility.Visible;

                mainWindow.Name_AI_vs_IVC.Visibility = Visibility.Visible;
                mainWindow.Label_AI_vs_IVC.Visibility = Visibility.Visible;
                mainWindow.Axis_AI_vs_IVC.Visibility = Visibility.Visible;
                mainWindow.AI_vs_IVC.Visibility = Visibility.Visible;

                mainWindow.Grid_HSI.Visibility = Visibility.Visible;
                mainWindow.Grid_Altimeter.Visibility = Visibility.Visible;
                mainWindow.Misc_NaturalHeadMovement.Visibility = Visibility.Visible;
            }
            else
            {
                mainWindow.Name_FLIR_Brightness.Visibility = Visibility.Hidden;
                mainWindow.Label_FLIR_Brightness.Visibility = Visibility.Hidden;
                mainWindow.Axis_FLIR_Brightness.Visibility = Visibility.Hidden;
                mainWindow.FLIR_Brightness.Visibility = Visibility.Hidden;

                mainWindow.Name_AI_vs_IVC.Visibility = Visibility.Hidden;
                mainWindow.Label_AI_vs_IVC.Visibility = Visibility.Hidden;
                mainWindow.Axis_AI_vs_IVC.Visibility = Visibility.Hidden;
                mainWindow.AI_vs_IVC.Visibility = Visibility.Hidden;

                mainWindow.Grid_HSI.Visibility = Visibility.Collapsed;
                mainWindow.Grid_Altimeter.Visibility = Visibility.Collapsed;
                mainWindow.Misc_NaturalHeadMovement.Visibility = Visibility.Collapsed;
            }
        }

        public void AVCSince433(bool flg)
        {
            // Hide/show the Avionics Configurator button (and its label) by item Id.
            mainWindow.SetPrimaryLauncherVisible("AVC", flg);
        }

        public void VRsince437(bool flg)
        {
            if (flg)
            {
                mainWindow.Label_VR.Visibility = Visibility.Visible;
                mainWindow.VR_SteamVR.Visibility = Visibility.Visible;
                mainWindow.VR_OpenXR.Visibility = Visibility.Visible;
                mainWindow.VR_NoVR.Visibility = Visibility.Visible;
            }
            else
            {
                mainWindow.Label_VR.Visibility = Visibility.Hidden;
                mainWindow.VR_SteamVR.Visibility = Visibility.Hidden;
                mainWindow.VR_OpenXR.Visibility = Visibility.Hidden;
                mainWindow.VR_NoVR.Visibility = Visibility.Hidden;
            }
        }

        public void NewAxisFrom435(bool flg)
        {
            if (flg)
            {
                mainWindow.Name_ILS_Volume_Knob.Visibility  = Visibility.Visible;
                mainWindow.Label_ILS_Volume_Knob.Visibility = Visibility.Visible;
                mainWindow.Axis_ILS_Volume_Knob.Visibility  = Visibility.Visible;
                mainWindow.ILS_Volume_Knob.Visibility       = Visibility.Visible;
            }
            else
            {
                mainWindow.Name_ILS_Volume_Knob.Visibility  = Visibility.Hidden;
                mainWindow.Label_ILS_Volume_Knob.Visibility = Visibility.Hidden;
                mainWindow.Axis_ILS_Volume_Knob.Visibility  = Visibility.Hidden;
                mainWindow.ILS_Volume_Knob.Visibility       = Visibility.Hidden;
            }
        }

        public void RTTsince435(bool flg)
        {
            // Hide/show both RTT items by Id.
            mainWindow.SetPrimaryLauncherVisible("RTTC", flg);
            mainWindow.SetPrimaryLauncherVisible("RTTS", flg);
        }

    }
}
