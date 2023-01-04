﻿using System.IO;
using System.Linq;
using FalconBMS.Launcher.Windows;
using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Input
{
    public class DeviceControl
    {
        // Member
        public DeviceList devList;
        public Device[] joyStick;
        public JoyAssgn[] joyAssign;
        //public AxAssgn mouseWheelAssign = new AxAssgn();
        public JoyAssgn mouse = new JoyAssgn();

        /// <summary>
        /// Get Devices.
        /// </summary>
        public DeviceControl()
        { }

        /// <summary>
        /// Get Devices.
        /// </summary>
        public DeviceControl(AppRegInfo appReg)
        {
            // Make Joystick Instances.
            devList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);
            joyStick = new Device[devList.Count];
            joyAssign = new JoyAssgn[devList.Count];

            System.Xml.Serialization.XmlSerializer serializer;
            StreamReader sr;
            string fileName;
            string stockFileName;
            int i = 0;

            foreach (DeviceInstance dev in devList)
            {
                joyStick[i] = new Device(dev.InstanceGuid);
                joyAssign[i] = new JoyAssgn(joyStick[i]);

                joyAssign[i].SetDeviceInstance(dev);

                fileName = appReg.GetInstallDir() + "/User/Config/Setup.v100." + joyAssign[i].GetProductName().Replace("/", "-")
                + " {" + joyAssign[i].GetInstanceGUID().ToString().ToUpper() + "}.xml";

                // Load existing .xml files.
                if (File.Exists(fileName))
                {
                    serializer = new System.Xml.Serialization.XmlSerializer(typeof(JoyAssgn));
                    sr = new StreamReader(fileName, new System.Text.UTF8Encoding(false));
                    joyAssign[i].Load((JoyAssgn)serializer.Deserialize(sr));
                    sr.Close();
                }
                else
                {
                    stockFileName = Directory.GetCurrentDirectory() 
                        + "/Stock/Setup.v100." 
                        + joyAssign[i].GetProductName().Replace("/", "-")
                        + " {Stock}.xml";
                    if (!File.Exists(stockFileName))
                    {
                        stockFileName = appReg.GetInstallDir() + "/Launcher"
                            + "/Stock/Setup.v100."
                            + joyAssign[i].GetProductName().Replace("/", "-")
                            + " {Stock}.xml";
                    }
                    if (File.Exists(stockFileName))
                    {
                        File.Copy(stockFileName, fileName);

                        serializer = new System.Xml.Serialization.XmlSerializer(typeof(JoyAssgn));
                        sr = new StreamReader(fileName, new System.Text.UTF8Encoding(false));
                        joyAssign[i].Load((JoyAssgn)serializer.Deserialize(sr));
                        sr.Close();
                    }
                }
                joyAssign[i].SetDeviceInstance(dev);
                i += 1;
            }
            
            // Load MouseWheel .xml file.
            serializer = new System.Xml.Serialization.XmlSerializer(typeof(AxAssgn));
            fileName = appReg.GetInstallDir() + "/User/Config/Setup.v100.Mousewheel.xml";
            if (File.Exists(fileName))
            {
                sr = new StreamReader(fileName, new System.Text.UTF8Encoding(false));
                mouse.LoadAx((AxAssgn)serializer.Deserialize(sr));
                sr.Close();
            }
        }

        public void SortDevice()
        {
            joyAssign = joyAssign.OrderByDescending(j => j.GetAssignedNumber()).ToArray();
        }

        public int GetAB(AxisName name)
        {
            return joyAssign[((InGameAxAssgn)MainWindow.inGameAxis[name.ToString()]).GetDeviceNumber()].detentPosition.GetAB();
        }

        public int GetIDLE(AxisName name)
        {
            return joyAssign[((InGameAxAssgn)MainWindow.inGameAxis[name.ToString()]).GetDeviceNumber()].detentPosition.GetIDLE();
        }
    }
}
