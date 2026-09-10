using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Diagnostics;

namespace FalconBMS.Launcher.Input
{

    public class DeviceControl
    {
        // For keys, hats and buttons, this field tracks which airframe/avionics-profile we're viewing and modifying.
        internal static string avionicsProfile = null; // null => F16 (default); or "F15ABCD"

        // Members
        private AppRegInfo appReg;

        private KeyFile keyFileDefaultF16;
        private KeyFile keyFileF15ABCD;

        private List<JoyAssgn> joyAssign;

        private DeviceSuppressList suppressList;

        public static DeviceControl EnumerateAttachedDevicesAndLoadXml(AppRegInfo appReg)
        {
            return new DeviceControl(appReg);
        }

        private DeviceControl(AppRegInfo appReg)
        {
            this.appReg = appReg;

            // Make Joystick Instances.
            var joy_ids = DirectInputDeviceMap.Singleton.GetDeviceInstanceGuids(include_keybd:false);

            this.suppressList = new DeviceSuppressList();

            joyAssign = new List<JoyAssgn>(16);

            string pathToUserXml;
            string pathToStockXml;

            int i = 0;
            foreach (Guid joy_id in joy_ids)
            {
                Guid pidvid = DirectInputHelper.GetDeviceProductGuid(joy_id);
                if (suppressList.IsDeviceSuppressed(joy_id) ||
                    suppressList.IsDeviceSuppressed(pidvid))
                {
                    Diagnostics.Log($"Ignoring suppressed device: pidvid {pidvid}; instance {joy_id}", Diagnostics.LogLevels.Info);
                    continue;
                }

                string name = DirectInputHelper.GetDeviceProductName(joy_id, sanitized: true);
                Diagnostics.Log($"Found device: {name}; pidvid {pidvid}; instance {joy_id}", Diagnostics.LogLevels.Info);

                JoyAssgn joy = new JoyAssgn(joy_id);
                joyAssign.Add(joy);

                pathToUserXml = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.SETUPV100 + name
                + " {" + joy_id.ToString().ToUpper() + "}.xml";

                // Load existing .xml files.
                if (File.Exists(pathToUserXml))
                {
                    joy.LoadAxesButtonsAndHatsFrom(pathToUserXml);
                }
                else
                {
                    pathToStockXml = Directory.GetCurrentDirectory() 
                        + CommonConstants.STOCKFOLDER + CommonConstants.SETUPV100
                        + joy.GetSanitizedProductName()
                        + CommonConstants.STOCKXML;
                    if (!File.Exists(pathToStockXml))
                    {
                        pathToStockXml = appReg.GetInstallDir() + CommonConstants.LAUNCHERFOLDER
                            + CommonConstants.STOCKFOLDER + CommonConstants.SETUPV100
                            + joy.GetSanitizedProductName()
                            + CommonConstants.STOCKXML;
                    }
                    if (File.Exists(pathToStockXml))
                    {
                        File.Copy(pathToStockXml, pathToUserXml);
                        joy.LoadAxesButtonsAndHatsFrom(pathToUserXml);
                    }
                }

                i += 1;
            }
            return;
        }

        public void LoadKeyBindingsFromUserOrStockKeyfiles(AppRegInfo appReg)
        {
            // First load Auto keys, then fail over to Full keys.
            string filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDERBACKSLASH + CommonConstants.BMS_AUTO + ".key";
            if (!File.Exists (filename))
                filename = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDERBACKSLASH + CommonConstants.BMS_FULL + ".key"; //initial load/fallback
            if (!File.Exists(filename))
                filename = CommonConstants.BMS_FULL + ".key"; //fallback to cwd

            string filenameF15 = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDERBACKSLASH + CommonConstants.BMS_AUTO + "-F15ABCD.key";
            if (!File.Exists(filenameF15))
                filenameF15 = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDERBACKSLASH + CommonConstants.BMS_FULL + "-F15ABCD.key"; //initial load/fallback
            if (!File.Exists(filenameF15))
                filenameF15 = CommonConstants.BMS_FULL + "-F15ABCD.key"; //fallback to cwd

            this.keyFileDefaultF16 = new KeyFile(filename);
            this.keyFileF15ABCD = new KeyFile(filenameF15);
        }

        public void ImportKeyfileIntoCurrentProfile(string filename)
        {
            if (string.IsNullOrEmpty(avionicsProfile))
            {
                this.keyFileDefaultF16 = new KeyFile(filename);
                return;
            }

            switch (avionicsProfile)
            {
                case CommonConstants.F15_TAG:
                    this.keyFileF15ABCD = new KeyFile(filename);
                    return;
            }
            throw new ArgumentException("avionicsProfile");
        }

        public KeyFile GetKeyBindings()
        {
            if (string.IsNullOrEmpty(avionicsProfile))
                return keyFileDefaultF16;

            switch (avionicsProfile)
            {
                case CommonConstants.F15_TAG:
                    return keyFileF15ABCD;
            }
            throw new ArgumentException("avionicsProfile");
        }

        public JoyAssgn[] GetJoystickMappings()
        {
            return joyAssign.ToArray();
        }

        public JoyAssgn GetJoystickMappingForDeviceId(Guid device_guid)
        {
            foreach (JoyAssgn joy in joyAssign)
                if (joy.GetInstanceGUID() == device_guid)
                    return joy;

            throw new KeyNotFoundException();
        }

        //NB: Used for AxisMapping.dat/Joystick.cal serializaiton.
        public int GetDeviceNumberForJoy(JoyAssgn joy)
        {
            if (joy == null) return -1;

            for (int i = 0; i < joyAssign.Count; ++i)
                if (joyAssign[i] == joy) return i;

            Debug.Assert(false); //Unexpected: non-null joy, but not found in deviceControl list?
            return -1;
        }

        public void UpdateAvionicsProfile(string profile)
        {
            DeviceControl.avionicsProfile = profile;

            foreach (JoyAssgn joy in joyAssign)
                joy.SelectAvionicsProfile(profile);

            return;
        }

        public void SaveXml()
        {
            //HACK: Ensure generic F16 bindings are saved at the root level, for back-compat.
            string currentProfile = DeviceControl.avionicsProfile;
            try
            {
                this.UpdateAvionicsProfile(null);//generic F16

                XmlSerializer serializer = new XmlSerializer(typeof(JoyAssgn));
                foreach (JoyAssgn joy in this.joyAssign)
                {
                    string fileName = this.appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.SETUPV100 + joy.GetSanitizedProductName()
                    + " {" + joy.GetInstanceGUID().ToString().ToUpper() + "}.xml";

                    using (StreamWriter sw = Utils.CreateUtf8TextWihoutBom(fileName))
                        serializer.Serialize(sw, joy);

                    // QUICKFIX: Save a duplicate copy in a safe space, to guard against possibility of user (accidentally or
                    // purposefully) running an older AL against 4.37.3 or later install -- this will silently delete the
                    // user's F-15 profile from the XML files!
                    if (!Directory.Exists(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                        Directory.CreateDirectory(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

                    string backupPath = this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + 
                        CommonConstants.SETUPV100 + joy.GetSanitizedProductName() + " {" + joy.GetInstanceGUID().ToString().ToUpper() + "}.xml";
                    File.Copy(fileName, backupPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
            }
            finally
            {
                this.UpdateAvionicsProfile(currentProfile);
            }
        }

    }

}
