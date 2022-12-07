﻿using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using FalconBMS.Launcher.Input;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Override
{
    public class OverrideSettingFor437 : OverrideSettingFor436
    {
        public OverrideSettingFor437(MainWindow mainWindow, AppRegInfo appReg) : base(mainWindow, appReg)
        {
        }

        protected override void SaveConfigfile(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            string filename = appReg.GetInstallDir() + "/User/Config/falcon bms.cfg";
            string fbackupname = appReg.GetInstallDir() + "/User/Config/Backup/falcon bms.cfg";
            if (!File.Exists(fbackupname) & File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            StreamReader cReader = new StreamReader
                (filename, Encoding.Default);
            string stResult = "";
            while (cReader.Peek() >= 0)
            {
                string stBuffer = cReader.ReadLine();
                if (stBuffer.Contains("// SETUP OVERRIDE"))
                    continue;
                stResult += stBuffer + "\r\n";
            }
            cReader.Close();

            StreamWriter cfg = new StreamWriter
                (filename, false, Encoding.GetEncoding("shift_jis"));
            cfg.Write(stResult);
            cfg.Write("set g_nButtonsPerDevice " + CommonConstants.DX128
                + "          // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nHotasPinkyShiftMagnitude " + deviceControl.joyAssign.Length * CommonConstants.DX128
                + "          // SETUP OVERRIDE\r\n");
            cfg.Write("set g_bHotasDgftSelfCancel " + Convert.ToInt32(mainWindow.Misc_OverrideSelfCancel.IsChecked)
                + "          // SETUP OVERRIDE\r\n");
            cfg.Write("set g_b3DClickableCursorAnchored " + Convert.ToInt32(mainWindow.Misc_MouseCursorAnchor.IsChecked)
                + "          // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nFakeVROption " + Convert.ToInt32(mainWindow.Misc_VR.IsChecked)
                + "          // SETUP OVERRIDE\r\n");
            if (((InGameAxAssgn)inGameAxis["Roll"]).GetDeviceNumber() == ((InGameAxAssgn)inGameAxis["Throttle"]).GetDeviceNumber())
            {
                cfg.Close();
                return;
            }
            cfg.Write("set g_nNumOfPOVs 2      // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nPOV1DeviceID " + (((InGameAxAssgn)inGameAxis["Roll"]).GetDeviceNumber() + 2) + "   // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nPOV1ID 0         // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nPOV2DeviceID " + (((InGameAxAssgn)inGameAxis["Throttle"]).GetDeviceNumber() + 2) + "   // SETUP OVERRIDE\r\n");
            cfg.Write("set g_nPOV2ID 0         // SETUP OVERRIDE\r\n");
            cfg.Close();
        }
    }
}
