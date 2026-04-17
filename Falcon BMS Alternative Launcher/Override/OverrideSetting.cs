using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FalconBMS.Launcher.Input;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Override
{
    /// <summary>
    /// Writer for setting Override
    /// </summary>
    public class OverrideSetting
    {
        protected MainWindow mainWindow;
        protected AppRegInfo appReg;

        /// <summary>
        /// Writer for setting Override
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="appReg"></param>
        public OverrideSetting(MainWindow mainWindow, AppRegInfo appReg)
        {
            this.mainWindow = mainWindow;
            this.appReg = appReg;
        }

        /// <summary>
        /// Execute setting override.
        /// </summary>
        /// <param name="inGameAxis"></param>
        /// <param name="deviceControl"></param>
        /// <param name="keyFile"></param>
        /// <param name="visualAcuity"></param>
        public void Execute(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            if (!Directory.Exists(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                Directory.CreateDirectory(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

            SaveAxisMapping(inGameAxis, deviceControl);
            SaveJoystickCal(inGameAxis, deviceControl);
            SaveDeviceSorting(deviceControl);
            SaveConfigfile(inGameAxis, deviceControl);
            SaveKeyMapping(inGameAxis, deviceControl);
            //SavePlcLbk();
            SavePop();
        }

        protected virtual void SaveConfigfile(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            Diagnostics.Log("Ammending Falcon BMS User.cfg..", Diagnostics.LogLevels.Info);

            using (StreamWriter cfgUser = OverwriteCfg(CommonConstants.USERCFGFILE))
            {
                cfgUser.WriteLine(CommonConstants.CFGOVERRIDECOMMENTLINE);

                OverrideButtonsPerDevice(cfgUser, deviceControl);
                OverrideHotasPinkyShiftMagnitude(cfgUser, deviceControl);
                OverridePovDeviceIDs(cfgUser, inGameAxis);

                ApplyVROverrides(cfgUser);
                ApplyMiscOverrides(cfgUser);
            }
        }

        public void SaveConfigOverrides(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            if (!Directory.Exists(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                Directory.CreateDirectory(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

            SaveConfigfile(inGameAxis, deviceControl);
        }

        private StreamWriter OverwriteCfg(string fname)
        {
            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + fname;
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + fname;

            if (!File.Exists(fbackupname) & File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            // Read existing contents, modulo the lines we've added in the past.
            List<string> lines = new List<string>(500);
            using (StreamReader reader = new StreamReader(filename, Encoding.UTF8))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;

                    if (line.Contains(CommonConstants.CFGOVERRIDECOMMENT_OLD))
                        continue;
                    if (line.Contains(CommonConstants.CFGOVERRIDECOMMENT_NEW))
                        continue;
                    if (line.Contains(CommonConstants.CFGOVERRIDECOMMENTLINE))
                        break; // read no more below this cutoff line

                    // Trim leading/trailing whitespace, collapse consecutive whitespace chars, and replace unicode smart-doublequotes.
                    string lineTrimmed = line.Trim();
                    if (lineTrimmed.StartsWith("set"))
                    {
                        line = lineTrimmed;
                        while (line.Contains("  "))
                            line = line.Replace("  ", " "); //consecutive spaces break BMS parser (as of 4.37)
                        while (line.Contains("\x201C") || line.Contains("\x201D")) //unicode smart-doublequotes
                            line = line.Replace("\x201C", "\x0022").Replace("\x201D", "\x0022");
                    }

                    lines.Add(line);
                }
            }

            // Recreate file contents; keep handle open.
            StreamWriter writer = Utils.CreateUtf8TextWihoutBom(filename);
            writer.NewLine = Environment.NewLine;

            foreach (string line in lines)
                writer.WriteLine(line);

            return writer;
        }

        private static string NormalizeCfgSetLine(string line)
        {
            string lineTrimmed = line.Trim();
            if (!lineTrimmed.StartsWith("set"))
                return null;

            string normalizedLine = lineTrimmed;
            while (normalizedLine.Contains("  "))
                normalizedLine = normalizedLine.Replace("  ", " "); // consecutive spaces break BMS parser (as of 4.37)
            while (normalizedLine.Contains("\x201C") || normalizedLine.Contains("\x201D"))
                normalizedLine = normalizedLine.Replace("\x201C", "\x0022").Replace("\x201D", "\x0022");

            return normalizedLine;
        }

        protected virtual void OverridePovDeviceIDs(StreamWriter cfg, Hashtable inGameAxis) { }

        protected virtual void OverrideHotasPinkyShiftMagnitude(StreamWriter cfg, DeviceControl deviceControl) { }

        protected virtual int GetButtonsPerDevice()
        {
            return CommonConstants.DX_MAX_BUTTONS_LEGACY;
        }

        protected virtual void OverrideButtonsPerDevice(StreamWriter cfg, DeviceControl deviceControl)
        {
            cfg.Write(
                "set g_nButtonsPerDevice "
                + GetButtonsPerDevice()
                + " " + CommonConstants.CFGOVERRIDECOMMENT_OLD + "\r\n");
        }

        protected virtual void ApplyVROverrides(StreamWriter cfg)
        {
            bool isVR = false;
            if (mainWindow.VR_SteamVR.IsVisible && mainWindow.VR_SteamVR.IsChecked == true)
                isVR = true;
            if (mainWindow.VR_OpenXR.IsVisible && mainWindow.VR_OpenXR.IsChecked == true)
                isVR = true;

            if (!isVR) return;

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.VRCFGFILE;
            if (!File.Exists(filename)) return;

            List<string> lines = File.ReadAllLines(filename, Encoding.UTF8).ToList();
            int overrideStart = lines.FindIndex(x => x.Contains(CommonConstants.CFGOVERRIDECOMMENTLINE));
            IEnumerable<string> vrLines = (overrideStart >= 0) ? lines.Skip(overrideStart + 1) : lines;

            foreach (string rawLine in vrLines)
            {
                if (String.IsNullOrWhiteSpace(rawLine))
                    continue;
                if (rawLine.Contains(CommonConstants.CFGOVERRIDECOMMENT_OLD))
                    continue;
                if (rawLine.Contains(CommonConstants.CFGOVERRIDECOMMENT_NEW))
                    continue;
                if (rawLine.Contains(CommonConstants.CFGOVERRIDECOMMENTLINE))
                    continue;

                string normalizedSetLine = NormalizeCfgSetLine(rawLine);
                if (String.IsNullOrEmpty(normalizedSetLine))
                    continue;

                cfg.WriteLine(normalizedSetLine + " " + CommonConstants.CFGOVERRIDECOMMENT_NEW);
            }
            return;
        }

        protected virtual void ApplyMiscOverrides(StreamWriter cfg)
        {
            var miscFlags = new (string key, int value)[]
            {
                ("g_bExportRTTTextures", (mainWindow.RTT_Enable?.IsChecked == true) ? 1 : 0),
            };

            foreach (var (key, value) in miscFlags)
                cfg.WriteLine($"set {key} {value}" + " " + CommonConstants.CFGOVERRIDECOMMENT_NEW);
        }

        protected void SaveDeviceSorting(DeviceControl deviceControl)
        {
            Diagnostics.Log("Overwriting DeviceSorting.txt..", Diagnostics.LogLevels.Info);

            // BMS overwrites DeviceSorting.txt if was written in UTF-8.
            string filename = appReg.GetInstallDir() + "/User/Config/DeviceSorting.txt";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "DeviceSorting.txt";
            if (!File.Exists(fbackupname) & File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            using (StreamWriter sw = Utils.CreateUtf8TextWihoutBom(filename))
            {
                sw.NewLine = Environment.NewLine;

                foreach (JoyAssgn joy in deviceControl.GetJoystickMappings())
                    sw.WriteLine(joy.GetDeviceSortingLine());
            }
        }

        public virtual void SaveKeyMapping(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            Diagnostics.Log("Emitting BMS - Auto.key and BMS - Auto-F15ABCD.key..", Diagnostics.LogLevels.Info);

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.BMS_AUTO + ".key";
            string filenameF15 = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.BMS_AUTO + "-F15ABCD.key";

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            if (File.Exists(filenameF15))
                File.SetAttributes(filenameF15, File.GetAttributes(filenameF15) & ~FileAttributes.ReadOnly);

            //HACK: Fetch F16 and F15 profile separately, explicitly
            string temp = DeviceControl.avionicsProfile;
            try
            {
                deviceControl.UpdateAvionicsProfile(null);
                WriteKeyLines(filename, inGameAxis,
                    deviceControl.GetKeyBindings(),
                    deviceControl.GetJoystickMappings());
                deviceControl.UpdateAvionicsProfile(CommonConstants.F15_TAG);
                WriteKeyLines(filenameF15, inGameAxis,
                    deviceControl.GetKeyBindings(),
                    deviceControl.GetJoystickMappings());
            }
            finally
            {
                deviceControl.UpdateAvionicsProfile(temp);
            }

            // QUICKFIX: Save a duplicate copy in a safe space, to guard against possibility of user (accidentally or
            // purposefully) running an older AL against 4.37.3 or later install.
            if (!Directory.Exists(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                Directory.CreateDirectory(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

            string backupPath = this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER +
                CommonConstants.BMS_AUTO + ".key";
            File.Copy(filename, backupPath, overwrite: true);

            string backupPathF15 = this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER +
                CommonConstants.BMS_AUTO + "-F15ABCD.key";
            File.Copy(filenameF15, backupPathF15, overwrite: true);

            return;
        }

        protected virtual void WriteKeyLines(string filename, Hashtable inGameAxis, KeyFile keyFile, JoyAssgn[] joyAssgns)
        {
            int buttonsPerDevice = GetButtonsPerDevice();

            using (StreamWriter sw = Utils.CreateUtf8TextWihoutBom(filename))
            {
                sw.NewLine = "\n"; // probably not necessary, but for consistency with existing keyfile serialization code that hardcodes "\n" everywhere

                for (int i = 0; i < keyFile.keyAssign.Length; i++)
                    sw.Write(keyFile.keyAssign[i].GetKeyLine());

                for (int i = 0; i < joyAssgns.Length; i++)
                {
                    InGameAxAssgn rollAxis = (InGameAxAssgn)inGameAxis[AxisName.Roll.ToString()];

                    sw.Write(joyAssgns[i].GetKeyLineDX(i, joyAssgns.Length, buttonsPerDevice));
                    // PRIMARY DEVICE POV
                    if (rollAxis.GetDeviceNumber() == i)
                        sw.Write(joyAssgns[i].GetKeyLinePOV(0, 0));
                }
            }
        }

        /// <summary>
        /// Overwrite callsign.pop file.
        /// </summary>
        protected virtual void SavePop()
        {
        }

        /// <summary>
        /// As the name inplies...
        /// </summary>
        protected void SaveAxisMapping(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            Diagnostics.Log("Overwriting AxisMapping.dat..", Diagnostics.LogLevels.Info);

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + "axismapping.dat";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "axismapping.dat";

            if (!File.Exists(fbackupname) & File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            FileStream fs = new FileStream
                (filename, FileMode.Create, FileAccess.Write);

            byte[] bs;

            InGameAxAssgn pitchAxis = (InGameAxAssgn)inGameAxis[AxisName.Pitch.ToString()];

            if (pitchAxis.GetDeviceNumber() > CommonConstants.JOYNUMUNASSIGNED)
            {
                bs = new byte[] 
                {
                    (byte)(pitchAxis.GetDeviceNumber() + CommonConstants.JOYNUMOFFSET),
                    0x00, 0x00, 0x00
                };
                fs.Write(bs, 0, bs.Length);

                bs = deviceControl.GetJoystickMappings()[pitchAxis.GetDeviceNumber()].GetInstanceGUID().ToByteArray();
                fs.Write(bs, 0, bs.Length);

                bs = new byte[] { (byte)deviceControl.GetJoystickMappings().Length, 0x00, 0x00, 0x00 };
                fs.Write(bs, 0, bs.Length);
            }
            else
            {
                bs = new byte[] 
                {
                    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
                bs[20] = (byte)deviceControl.GetJoystickMappings().Length;
                fs.Write(bs, 0, bs.Length);
            }

            AxisName[] localAxisMappingList = getAxisMappingList();

            foreach (AxisName nme in localAxisMappingList)
            {
                InGameAxAssgn currentAxis = (InGameAxAssgn)inGameAxis[nme.ToString()];

                if (!currentAxis.IsAssigned() || isRollLinkedNWSEnabled(nme))
                {
                    bs = new byte[] 
                    {
                        0xFF, 0xFF, 0xFF, 0xFF,
                        0xFF, 0xFF, 0xFF, 0xFF,
                        0x64, 0x00, 0x00, 0x00,
                        0xFF, 0xFF, 0xFF, 0xFF
                    };
                    fs.Write(bs, 0, bs.Length);
                    continue;
                }
                if (currentAxis.IsJoyAssigned() && 
                    !isRollLinkedNWSEnabled(nme))
                {
                    bs = new byte[] 
                    {
                        (byte)(currentAxis.GetDeviceNumber() + CommonConstants.JOYNUMOFFSET),
                        0x00, 0x00, 0x00
                    };
                    fs.Write(bs, 0, bs.Length);
                    bs = new byte[] 
                    {
                        (byte)currentAxis.GetPhysicalNumber(),
                        0x00, 0x00, 0x00
                    };
                    fs.Write(bs, 0, bs.Length);
                }

                bs = isRollLinkedNWSEnabled(nme) ?
                    new byte[] { 0x00, 0x00, 0x00, 0x00 } :
                    GetAxDeadZoneByte(currentAxis.GetDeadzone());

                fs.Write(bs, 0, bs.Length);

                bs = isRollLinkedNWSEnabled(nme) ? 
                    new byte[] { 0x00, 0x00, 0x00, 0x00 } :
                    GetAxSaturationByte(currentAxis.GetSaturation());

                fs.Write(bs, 0, bs.Length);
            }
            fs.Close();
        }

        private byte[] GetAxDeadZoneByte(AxCurve axCurve)
        {
            var bs = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            switch (axCurve)
            {
                case AxCurve.None:
                    bs = new byte[] { 0x00, 0x00, 0x00, 0x00 };
                    break;
                case AxCurve.Small:
                    bs = new byte[] { 0x64, 0x00, 0x00, 0x00 };
                    break;
                case AxCurve.Medium:
                    bs = new byte[] { 0xF4, 0x01, 0x00, 0x00 };
                    break;
                case AxCurve.Large:
                    bs = new byte[] { 0xE8, 0x03, 0x00, 0x00 };
                    break;
            }
            return bs;
        }

        private byte[] GetAxSaturationByte(AxCurve axCurve)
        { 
            var bs = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            switch (axCurve)
            {
                case AxCurve.None:
                    bs = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    break;
                case AxCurve.Small:
                    bs = new byte[] { 0x1C, 0x25, 0x00, 0x00 };
                    break;
                case AxCurve.Medium:
                    bs = new byte[] { 0x28, 0x23, 0x00, 0x00 };
                    break;
                case AxCurve.Large:
                    bs = new byte[] { 0x34, 0x21, 0x00, 0x00 };
                    break;
            }
            return bs;
        }

        /// <summary>
        /// As the name implies...
        /// </summary>
        protected virtual void SaveJoystickCal(Hashtable inGameAxis, DeviceControl deviceControl)
        {
            Diagnostics.Log("Overwriting Joystick.cal..", Diagnostics.LogLevels.Info);

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + "joystick.cal";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "joystick.cal";

            if (!File.Exists(fbackupname) & File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            FileStream fs = new FileStream
                (filename, FileMode.Create, FileAccess.Write);

            byte[] bs = new byte[] { 0x00 };

            AxisName[] localJoystickCalList = appReg.getOverrideWriter().getJoystickCalList();

            foreach (AxisName nme in localJoystickCalList)
            {
                InGameAxAssgn currentAxis = (InGameAxAssgn)inGameAxis[nme.ToString()];

                SetJoyCalDefaultByte(ref bs);

                if (currentAxis.IsAssigned() && !isRollLinkedNWSEnabled(nme))
                {
                    bs[12] = 0x01;

                    if (nme == AxisName.Throttle && currentAxis.IsJoyAssigned())
                    {
                        double iAB = deviceControl.GetJoystickMappings()[currentAxis.GetDeviceNumber()].detentPosition.GetAB();
                        double iIdle = deviceControl.GetJoystickMappings()[currentAxis.GetDeviceNumber()].detentPosition.GetIDLE();

                        iAB = iAB * CommonConstants.BINAXISMAX / CommonConstants.AXISMAX;
                        iIdle = iIdle * CommonConstants.BINAXISMAX / CommonConstants.AXISMAX;

                        InGameAxAssgn axis = (InGameAxAssgn)MainWindow.inGameAxis[nme.ToString()];
                        if (axis.GetInvert() == false)
                        {
                            iAB = CommonConstants.BINAXISMAX - iAB;
                            iIdle = CommonConstants.BINAXISMAX - iIdle;
                        }

                        byte[] ab = BitConverter.GetBytes((int)iAB).Reverse().ToArray();
                        byte[] idle = BitConverter.GetBytes((int)iIdle).Reverse().ToArray();

                        bs[1] = ab[2];
                        bs[5] = idle[2];
                    }
                }
                if (currentAxis.GetInvert())
                {
                    SetJoyCalInvertByte(ref bs);
                }
                fs.Write(bs, 0, bs.Length);
            }
            fs.Close();
        }

        protected virtual void SetJoyCalDefaultByte(ref byte[] bs)
        {
            //NB: this is overridden to return 24 bytes, in OverrideSettingsFor435 and later.
            bs = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x98, 0x3A, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };
        }

        protected virtual void SetJoyCalInvertByte(ref byte[] bs)
        {
            //NB: this is overridden to write byte offset [21], in OverrideSettingsFor435 and later.
            bs[20] = 0x01;
        }

        protected bool isRollLinkedNWSEnabled(AxisName nme)
        {
            return mainWindow.Misc_RollLinkedNWS.IsChecked == true && ( nme == AxisName.Yaw );
        }

        public virtual AxisName[] getAxisMappingList() { return axisMappingList; }
        public virtual AxisName[] getJoystickCalList() { return joystickCalList; }

        /// <summary>
        /// Axis information order for AxisMapping.dat
        /// </summary>
        private AxisName[] axisMappingList = { };

        /// <summary>
        /// Axis information order for JoyStick.cal
        /// </summary>
        private AxisName[] joystickCalList = { };
    }
    

}
