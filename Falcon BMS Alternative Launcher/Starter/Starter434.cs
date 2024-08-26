using System.IO;
using System.ServiceModel.Syndication;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Starter
{
    public class Starter434 : AbstractStarter
    {
        public Starter434(AppRegInfo appReg, MainWindow mainWindow) : base(appReg, mainWindow)
        {
            Bandwidth(false);
            NewAxisFrom433(true);
            AVCSince433(true);
            DISXuntil434(true);
            RTTsince435(false);
            NewAxisFrom435(false);
            VRsince437(false);

            mainWindow.Version_Number.Content = "4.34";
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
                    process = Utils.LaunchProcess(bmsExe, strCmdText);
                    MainWindow.bmsHasBeenLaunched = true;
                    mainWindow.Close();
                    break;
                case "Launch_CFG":
                    process = Utils.LaunchProcess(appReg.GetInstallDir() + "/Config.exe");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_DISX":
                    Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x86/Display Extraction.exe");
                    break;
                case "Launch_IVCC":
                    string ivcClientCmd = appReg.GetInstallDir() + "/Bin/x64/IVC/IVC Client.exe";
                    string ivcClientCwd = appReg.GetInstallDir() + "/Bin/x64/IVC";
                    Utils.LaunchProcess(ivcClientCmd, args: null, ivcClientCwd);
                    break;
                case "Launch_IVCS":
                    Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x64/IVC/IVC Server.exe");
                    break;
                case "Launch_AVC":
                    process = Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x86/Avionics Configurator.exe", args:null, cwd: appReg.GetInstallDir() + "/Bin/x86/");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_EDIT":
                    Utils.LaunchProcess(appReg.GetInstallDir() + "/Bin/x64/Editor.exe");
                    break;
            }
        }
    }

}
