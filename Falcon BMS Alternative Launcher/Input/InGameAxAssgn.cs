using System;
using System.Diagnostics;
using FalconBMS.Launcher.Windows;

namespace FalconBMS.Launcher.Input
{

    public class InGameAxAssgn
    {
        protected JoyAssgn joy = null; //null == unassigned
        protected PhysicalAxis phys_axis_id = (PhysicalAxis)(-1);

        protected bool invert;
        protected AxCurve saturation = AxCurve.None;
        protected AxCurve deadzone = AxCurve.None;
        protected DateTime assgnDate = new DateTime(1998, 12, 12, 12, 0, 0);

        public InGameAxAssgn() { }

        public InGameAxAssgn(JoyAssgn joy, PhysicalAxis phys_axis, AxAssgn axis)
        {
            this.joy = joy;
            this.phys_axis_id = phys_axis;

            invert = axis.GetInvert();
            saturation = axis.GetSaturation();
            deadzone = axis.GetDeadZone();
            assgnDate = axis.GetAssignDate();
        }

        public InGameAxAssgn(JoyAssgn joy, PhysicalAxis phys_axis, bool invert, AxCurve deadzone, AxCurve saturation)
        {
            this.joy = joy;
            this.phys_axis_id = phys_axis;

            this.invert = invert;
            this.deadzone = deadzone;
            this.saturation = saturation;
            this.assgnDate = DateTime.UtcNow;
        }

        public bool IsAssigned()
        {
            Debug.Assert((joy != null) == ((int)phys_axis_id >= 0));
            return (this.joy != null);
        }

        public int GetDeviceNumber() 
        {
            return MainWindow.deviceControl.GetDeviceNumberForJoy(joy);
        }

        public JoyAssgn GetJoy()
        {
            return joy;
        }
        public PhysicalAxis GetPhysicalAxisId() { return phys_axis_id; }
        public bool GetInvert() { return invert; }
        public AxCurve GetDeadzone() { return deadzone; }
        public AxCurve GetSaturation() { return saturation; }
        public DateTime getDate() { return assgnDate; }

        public void SetInvert(bool invert)
        {
            this.invert = invert;
            this.joy.axis[(int)phys_axis_id].Invert = invert;
        }
        public void SetDeadzone( AxCurve dz )
        {
            this.deadzone = dz;
            this.joy.axis[(int)phys_axis_id].Deadzone = dz;
        }
        public void SetSaturation( AxCurve sat )
        {
            this.saturation = sat;
            this.joy.axis[(int)phys_axis_id].Saturation = sat;
        }
    }

}
