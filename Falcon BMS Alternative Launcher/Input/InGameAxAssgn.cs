﻿using System;
using FalconBMS.Launcher.Windows;
using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Input
{
    public class InGameAxAssgn
    {
        protected JoyAssgn joy;      // DeviceNumber(-2=MouseWheel)
        protected int phyAxNum = -1;    // PhysicalAxisNumber
                                        // 0=X 1=Y 2=Z 3=Rx 4=Ry 5=Rz 6=Slider0 7=Slider1
        protected bool invert;
        protected AxCurve saturation = AxCurve.None;
        protected AxCurve deadzone = AxCurve.None;
        protected DateTime assgnDate = new DateTime(1998, 12, 12, 12, 0, 0);

        public InGameAxAssgn() { }

        public InGameAxAssgn(JoyAssgn joy, int phyAxNum, AxAssgn axis)
        {
            this.joy = joy;
            this.phyAxNum = phyAxNum;
            invert = axis.GetInvert();
            saturation = axis.GetSaturation();
            deadzone = axis.GetDeadZone();
            assgnDate = axis.GetAssignDate();
        }

        public InGameAxAssgn(JoyAssgn joy, int phyAxNum, bool invert, AxCurve deadzone, AxCurve saturation)
        {
            this.joy = joy;
            this.phyAxNum = phyAxNum;
            this.invert = invert;
            this.deadzone = deadzone;
            this.saturation = saturation;
        }

        public bool IsAssigned()
        {
            return GetDeviceNumber() != CommonConstants.JOYNUMUNASSIGNED;
        }
        public bool IsJoyAssigned()
        {
            return GetDeviceNumber() > CommonConstants.JOYNUMUNASSIGNED;
        }

        public int GetDeviceNumber() 
        {
            for (int i = 0; i < MainWindow.deviceControl.GetJoystickMappings().Length; i++)
                if (MainWindow.deviceControl.GetJoystickMappings()[i] == joy)
                    return i;

            return CommonConstants.JOYNUMUNASSIGNED;
        }
        public Device GetDevice()
        {
            for (int i = 0; i < MainWindow.deviceControl.GetJoystickMappings().Length; i++)
                if (MainWindow.deviceControl.GetJoystickMappings()[i] == joy)
                    return joy.GetDevice();
            return null;
        }
        public JoyAssgn GetJoy()
        {
            return joy;
        }
        public int GetPhysicalNumber() { return phyAxNum; }
        public bool GetInvert() { return invert; }
        public AxCurve GetDeadzone() { return deadzone; }
        public AxCurve GetSaturation() { return saturation; }
        public DateTime getDate() { return assgnDate; }
    }
}
