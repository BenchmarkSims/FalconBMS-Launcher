﻿using System.IO;
using System.ServiceModel.Syndication;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Starter
{
    public class Starter436Internal : AbstractStarter
    {
        public Starter436Internal(AppRegInfo appReg, MainWindow mainWindow) : base(appReg, mainWindow)
        {
            Bandwidth(false);
            NewAxisFrom433(true);
            PlatformChangeSince433(AvailablePlatform.X64);
            AVCSince433(true);
            DISXuntil434(false);
            RTTsince435(true);
            NewAxisFrom435(true);
            VRsince437(true);

            mainWindow.Version_Number.Content = "4.36 I";
        }

        public override void execute(object sender, bool flg)
        {
            System.Diagnostics.Process process;
            switch (((System.Windows.Controls.Button)sender).Name)
            {
                case "Launch_BMS":
                case "Launch_BMS_Large":
                    string strCmdText = getCommandLine();

                    // OVERRIDE SETTINGS.
                    mainWindow.executeOverride();

                    string appPlatform = appReg.GetInstallDir() + "/Bin/x86//Hub.exe";
                    process = System.Diagnostics.Process.Start(appPlatform, strCmdText);
                    if (flg)
                        mainWindow.minimizeWindowUntilProcessEnds(process);
                    else
                        mainWindow.Close();
                    break;
                case "Launch_CFG":
                    process = System.Diagnostics.Process.Start(appReg.GetInstallDir() + "/Config.exe");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_RTTC":
                    Directory.SetCurrentDirectory(appReg.GetInstallDir() + "/Tools/RTTRemote/");
                    System.Diagnostics.Process.Start("RTTClient64.exe");
                    break;
                case "Launch_RTTS":
                    Directory.SetCurrentDirectory(appReg.GetInstallDir() + "/Tools/RTTRemote/");
                    System.Diagnostics.Process.Start("RTTServer64.exe");
                    break;
                case "Launch_IVCC":
                    Directory.SetCurrentDirectory(appReg.GetInstallDir() + "/Bin/x64/IVC/");
                    System.Diagnostics.Process.Start("IVC Client.exe");
                    break;
                case "Launch_IVCS":
                    System.Diagnostics.Process.Start(appReg.GetInstallDir() + "/Bin/x64/IVC/IVC Server.exe");
                    break;
                case "Launch_AVC":
                    Directory.SetCurrentDirectory(appReg.GetInstallDir() + "/Bin/x86/");
                    process = System.Diagnostics.Process.Start("Avionics Configurator.exe");
                    mainWindow.minimizeWindowUntilProcessEnds(process);
                    break;
                case "Launch_EDIT":
                    System.Diagnostics.Process.Start(appReg.GetInstallDir() + "/Bin/x64/Editor.exe");
                    break;
            }
        }
    }

}
