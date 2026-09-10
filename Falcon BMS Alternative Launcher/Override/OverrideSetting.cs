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
        public void Execute( Dictionary<LogicalAxis, InGameAxAssgn> axis_map, DeviceControl deviceControl)
        {
            if (!Directory.Exists(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                Directory.CreateDirectory(appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

            SaveAxisMapping(axis_map, deviceControl);
            SaveJoystickCal(axis_map, deviceControl);
            SaveDeviceSorting(deviceControl);
            SaveConfigfile(axis_map, deviceControl);
            SaveKeyMapping(axis_map, deviceControl);
            //SavePlcLbk();
            SavePop();
        }

        protected virtual void SaveConfigfile( Dictionary<LogicalAxis, InGameAxAssgn> axis_map, DeviceControl deviceControl)
        {
            Diagnostics.Log("Ammending Falcon BMS User.cfg..", Diagnostics.LogLevels.Info);

            using (StreamWriter cfgUser = OverwriteCfg(CommonConstants.USERCFGFILE))
            {
                cfgUser.WriteLine(CommonConstants.CFGOVERRIDECOMMENTLINE);

                OverrideButtonsPerDevice(cfgUser, deviceControl);
                OverrideHotasPinkyShiftMagnitude(cfgUser, deviceControl);
                OverridePovDeviceIDs(cfgUser, axis_map);

                ApplyVROverrides(cfgUser);
                ApplyMiscOverrides(cfgUser);
            }
        }

        private StreamWriter OverwriteCfg(string fname)
        {
            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + fname;
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + fname;

            if (!File.Exists(fbackupname) && File.Exists(filename))
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

        protected virtual void OverridePovDeviceIDs(StreamWriter cfg, Dictionary<LogicalAxis, InGameAxAssgn> axis_map ) { }

        protected virtual void OverrideHotasPinkyShiftMagnitude(StreamWriter cfg, DeviceControl deviceControl) { }

        protected virtual void OverrideButtonsPerDevice(StreamWriter cfg, DeviceControl deviceControl)
        {
            cfg.Write(
                "set g_nButtonsPerDevice "
                + CommonConstants.DX_MAX_BUTTONS_LEGACY
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

            using (StreamReader reader = new StreamReader(filename, Encoding.UTF8))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;

                    //TODO: refactor to remove duplicated code .. actually this whole VR-override feature should be ported over to native BMS.
                    string lineTrimmed = line.Trim();
                    if (lineTrimmed.Length == 0) continue;

                    if (lineTrimmed.StartsWith("set"))
                    {
                        line = lineTrimmed;
                        while (line.Contains("  "))
                            line = line.Replace("  ", " "); //consecutive spaces break BMS parser (as of 4.37)
                        while (line.Contains("\x201C") || line.Contains("\x201D")) //unicode smart-doublequotes
                            line = line.Replace("\x201C", "\x0022").Replace("\x201D", "\x0022");
                    }

                    cfg.WriteLine(line + CommonConstants.CFGOVERRIDECOMMENT_OLD);
                }
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
            if (!File.Exists(fbackupname) && File.Exists(filename))
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

        public virtual void SaveKeyMapping(Dictionary<LogicalAxis, InGameAxAssgn> axis_map, DeviceControl deviceControl)
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
                WriteKeyLines(filename, axis_map,
                    deviceControl.GetKeyBindings(),
                    deviceControl.GetJoystickMappings());
                deviceControl.UpdateAvionicsProfile(CommonConstants.F15_TAG);
                WriteKeyLines(filenameF15, axis_map,
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

        protected virtual void WriteKeyLines(string filename, Dictionary<LogicalAxis, InGameAxAssgn> axis_map, KeyFile keyFile, JoyAssgn[] joyAssgns)
        {
            using (StreamWriter sw = Utils.CreateUtf8TextWihoutBom(filename))
            {
                sw.NewLine = "\n"; // probably not necessary, but for consistency with existing keyfile serialization code that hardcodes "\n" everywhere

                for (int i = 0; i < keyFile.keyAssign.Length; i++)
                    sw.Write(keyFile.keyAssign[i].GetKeyLine());

                for (int i = 0; i < joyAssgns.Length; i++)
                {
                    InGameAxAssgn rollAxis = axis_map[LogicalAxis.Roll];

                    sw.Write(joyAssgns[i].GetKeyLineDX(i, joyAssgns.Length));
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

        protected void SaveAxisMapping(Dictionary<LogicalAxis, InGameAxAssgn> axis_map, DeviceControl deviceControl)
        {
            Diagnostics.Log("Overwriting AxisMapping.dat..", Diagnostics.LogLevels.Info);

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + "axismapping.dat";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "axismapping.dat";

            if (!File.Exists(fbackupname) && File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            FileStream fs = new FileStream
                (filename, FileMode.Create, FileAccess.Write);

            byte[] bs;

            InGameAxAssgn pitchAxis = axis_map[LogicalAxis.Pitch];

            if (pitchAxis.IsAssigned())
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

            LogicalAxis[] localAxisMappingList = getAxisMappingList();

            foreach (LogicalAxis log_axis in localAxisMappingList)
            {
                InGameAxAssgn currentAxis = axis_map[log_axis];

                if (!currentAxis.IsAssigned() || isYawAxisWithRollLinkedNWS(log_axis))
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
                if (currentAxis.IsAssigned() && !isYawAxisWithRollLinkedNWS(log_axis))
                {
                    bs = new byte[] 
                    {
                        (byte)(currentAxis.GetDeviceNumber() + CommonConstants.JOYNUMOFFSET),
                        0x00, 0x00, 0x00
                    };
                    fs.Write(bs, 0, bs.Length);
                    bs = new byte[] 
                    {
                        (byte)currentAxis.GetPhysicalAxisId(),
                        0x00, 0x00, 0x00
                    };
                    fs.Write(bs, 0, bs.Length);
                }

                bs = isYawAxisWithRollLinkedNWS(log_axis) ?
                    new byte[] { 0x00, 0x00, 0x00, 0x00 } :
                    GetAxDeadZoneByte(currentAxis.GetDeadzone());

                fs.Write(bs, 0, bs.Length);

                bs = isYawAxisWithRollLinkedNWS(log_axis) ? 
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
        protected virtual void SaveJoystickCal( Dictionary<LogicalAxis, InGameAxAssgn> axis_map, DeviceControl deviceControl)
        {
            Diagnostics.Log("Overwriting Joystick.cal..", Diagnostics.LogLevels.Info);

            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + "joystick.cal";
            string fbackupname = appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + "joystick.cal";

            if (!File.Exists(fbackupname) && File.Exists(filename))
                File.Copy(filename, fbackupname, true);

            if (File.Exists(filename))
                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

            FileStream fs = new FileStream
                (filename, FileMode.Create, FileAccess.Write);

            byte[] bs = new byte[] { 0x00 };

            LogicalAxis[] localJoystickCalList = appReg.getOverrideWriter().getJoystickCalList();

            foreach (LogicalAxis log_axis in localJoystickCalList)
            {
                InGameAxAssgn currentAxis = axis_map[log_axis];

                SetJoyCalDefaultByte(ref bs);

                if (currentAxis.IsAssigned() && !isYawAxisWithRollLinkedNWS(log_axis))
                {
                    bs[12] = 0x01;

                    if (log_axis == LogicalAxis.Throttle && currentAxis.IsAssigned())
                    {
                        double iAB = deviceControl.GetJoystickMappings()[currentAxis.GetDeviceNumber()].detentPosition.AB;
                        double iIdle = deviceControl.GetJoystickMappings()[currentAxis.GetDeviceNumber()].detentPosition.IDLE;

                        iAB = iAB * CommonConstants.BINAXISMAX / CommonConstants.AXISMAX;
                        iIdle = iIdle * CommonConstants.BINAXISMAX / CommonConstants.AXISMAX;

                        InGameAxAssgn axis = MainWindow.s_map_logical_axes[log_axis];
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

        protected bool isYawAxisWithRollLinkedNWS(LogicalAxis log_axis)
        {
            return mainWindow.Misc_RollLinkedNWS.IsChecked == true && ( log_axis == LogicalAxis.Yaw );
        }

        public virtual LogicalAxis[] getAxisMappingList() { return axisMappingList; }
        public virtual LogicalAxis[] getJoystickCalList() { return joystickCalList; }

        /// <summary>
        /// Axis information order for AxisMapping.dat
        /// </summary>
        private LogicalAxis[] axisMappingList = { };

        /// <summary>
        /// Axis information order for JoyStick.cal
        /// </summary>
        private LogicalAxis[] joystickCalList = { };
    }
    

}
