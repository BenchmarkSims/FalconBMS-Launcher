using System.IO;
using System.ServiceModel.Syndication;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Starter
{
    public class Starter433 : AbstractStarter
    {
        public Starter433(AppRegInfo appReg, MainWindow mainWindow) : base(appReg, mainWindow)
        {
            NewAxisFrom433(true);
            AVCSince433(true);
            RTTsince435(false);
            NewAxisFrom435(false);
            VRsince437(false);

            mainWindow.Version_Number.Content = "4.33";
        }

        public override void execute(object sender)
        {
            System.Diagnostics.Process process;
            switch (((System.Windows.Controls.Button)sender).Name)
            {
                case "Launch_UPD":
                    process = Utils.LaunchProcess(appReg.GetInstallDir() + "/Updater.exe");
                    mainWindow.Close();
                    break;

                case "Launch_BMS_Large":
                    string strCmdText = getCommandLine();

                    // OVERRIDE SETTINGS.
                    mainWindow.executeOverride();

                    string bmsExe = appReg.GetInstallDir() + "/Bin/x64/Falcon BMS.exe";
                    if (File.Exists(bmsExe) == false)
                        return;

                    process = Utils.LaunchProcess(bmsExe, strCmdText);
                    MainWindow.bmsHasBeenLaunched = true;

                    if (Properties.Settings.Default.KeepLauncherOpen)
                    {
                        // Stay open: minimize while BMS runs, then restore on exit
                        mainWindow.minimizeWindowUntilProcessEnds(process);
                    }
                    else
                    {
                        // Default behavior: close after launching BMS
                        mainWindow.Close();
                    }
                    break;

                case "Launch_CFG":
                    process = Utils.LaunchProcess(appReg.GetInstallDir() + "/Config.exe");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_IVCC":
                    string ivcClientCmd = appReg.GetInstallDir() + "/Bin/x86/IVC/IVC Client.exe";
                    string ivcClientCwd = appReg.GetInstallDir() + "/Bin/x86/IVC";
                    Utils.LaunchProcess(ivcClientCmd, args: null, ivcClientCwd);
                    break;
                case "Launch_IVCS":
                    Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x86/IVC/IVC Server.exe");
                    break;
                case "Launch_AVC":
                    process = Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x86/Avionics Configurator.exe", args:null, cwd: appReg.GetInstallDir() + "/Bin/x86/");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_EDIT":
                    Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x86/Editor.exe");
                    break;
            }
        }
    }

}
