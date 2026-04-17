using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using FalconBMS.Launcher.Input;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Override
{
    public class OverrideSettingFor436 : OverrideSettingFor435
    {
        public OverrideSettingFor436(MainWindow mainWindow, AppRegInfo appReg) : base(mainWindow, appReg)
        {
        }

        protected override int GetButtonsPerDevice()
        {
            switch (Properties.Settings.Default.KeyMappingMaxButtons)
            {
                case 16:
                case 32:
                case 64:
                case 128:
                    return Properties.Settings.Default.KeyMappingMaxButtons;
                default:
                    return CommonConstants.DX_MAX_BUTTONS;
            }
        }

        protected override void OverrideHotasPinkyShiftMagnitude(StreamWriter cfg, DeviceControl deviceControl)
        {
            cfg.Write(
                "set g_nHotasPinkyShiftMagnitude "
                + deviceControl.GetJoystickMappings().Length * GetButtonsPerDevice()
                + " " + CommonConstants.CFGOVERRIDECOMMENT_NEW + "\r\n");
        }

        protected override void OverrideButtonsPerDevice(StreamWriter cfg, DeviceControl deviceControl)
        {
            cfg.Write(
                "set g_nButtonsPerDevice "
                + GetButtonsPerDevice()
                + " " + CommonConstants.CFGOVERRIDECOMMENT_NEW + "\r\n");
        }
    }
}
