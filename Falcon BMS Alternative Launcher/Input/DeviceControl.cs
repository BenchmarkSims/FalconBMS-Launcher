using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

using Microsoft.DirectX.DirectInput;

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

        private List<Device> hwDevices;
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
            DeviceList devList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);

            this.suppressList = new DeviceSuppressList();

            hwDevices = new List<Device>(devList.Count);
            joyAssign = new List<JoyAssgn>(devList.Count);
            
            string pathToUserXml;
            string pathToStockXml;

            int i = 0;
            foreach (DeviceInstance dev in devList)
            {
                if (suppressList.IsDeviceSuppressed(dev.InstanceGuid) ||
                    suppressList.IsDeviceSuppressed(dev.ProductGuid))
                    continue;

                Device hwdev = new Device(dev.InstanceGuid);
                JoyAssgn joy = new JoyAssgn(hwdev);
                hwDevices.Add(hwdev);
                joyAssign.Add(joy);

                pathToUserXml = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.SETUPV100 + joy.GetProductFileName()
                + " {" + joy.GetInstanceGUID().ToString().ToUpper() + "}.xml";

                // Load existing .xml files.
                if (File.Exists(pathToUserXml))
                {
                    joy.LoadAxesButtonsAndHatsFrom(pathToUserXml);
                }
                else
                {
                    pathToStockXml = Directory.GetCurrentDirectory() 
                        + CommonConstants.STOCKFOLDER + CommonConstants.SETUPV100
                        + joy.GetProductFileName()
                        + CommonConstants.STOCKXML;
                    if (!File.Exists(pathToStockXml))
                    {
                        pathToStockXml = appReg.GetInstallDir() + CommonConstants.LAUNCHERFOLDER
                            + CommonConstants.STOCKFOLDER + CommonConstants.SETUPV100
                            + joy.GetProductFileName()
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
            
            // Load key bindings from keyfiles.
            LoadKeyBindingsFromUserOrStockKeyfiles(appReg);
        }

        public bool DeviceListNeedsRefresh()
        {
            var tmpDevices = new List<Device>();
            DeviceList devList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);
            foreach (DeviceInstance dev in devList)
            {
                if (suppressList.IsDeviceSuppressed(dev.InstanceGuid) ||
                    suppressList.IsDeviceSuppressed(dev.ProductGuid))
                    continue;

                tmpDevices.Add(new Device(dev.InstanceGuid));
            }

            if (tmpDevices.Count != this.hwDevices.Count) return true;

            for (int i = 0; i < tmpDevices.Count; ++i)
            {
                if (tmpDevices[i].DeviceInformation.InstanceGuid !=
                    this.hwDevices[i].DeviceInformation.InstanceGuid)
                    return true;
            }

            return false;
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

        public Device[] GetHwDeviceList()
        {
            return this.hwDevices.ToArray();
        }

        public Device GetHwDevice(int i)
        {
            return this.hwDevices[i];
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
                    string fileName = this.appReg.GetInstallDir() + CommonConstants.CONFIGFOLDER + CommonConstants.SETUPV100 + joy.GetProductFileName()
                    + " {" + joy.GetInstanceGUID().ToString().ToUpper() + "}.xml";

                    using (StreamWriter sw = Utils.CreateUtf8TextWihoutBom(fileName))
                        serializer.Serialize(sw, joy);

                    // QUICKFIX: Save a duplicate copy in a safe space, to guard against possibility of user (accidentally or
                    // purposefully) running an older AL against 4.37.3 or later install -- this will silently delete the
                    // user's F-15 profile from the XML files!
                    if (!Directory.Exists(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER))
                        Directory.CreateDirectory(this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER);

                    string backupPath = this.appReg.GetInstallDir() + CommonConstants.BACKUPFOLDER + 
                        CommonConstants.SETUPV100 + joy.GetProductFileName() + " {" + joy.GetInstanceGUID().ToString().ToUpper() + "}.xml";
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
