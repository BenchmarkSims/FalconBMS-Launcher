using System;

namespace FalconBMS.Launcher.Input
{

    public class AxAssgn : ICloneable
    {
        // Member
        protected LogicalAxis logical_axis;
        protected DateTime assgnDate = new DateTime(1998, 12, 12, 12, 0, 0);
        protected bool invert;
        protected AxCurve saturation = 0;
        protected AxCurve deadzone = 0;

        // Property for XML
        public string AxisName { get => logical_axis.ToString();
            set => logical_axis = (LogicalAxis)Enum.Parse(typeof(LogicalAxis), value);
        }
        public DateTime AssgnDate { get => assgnDate;
            set => assgnDate = value;
        }
        public bool Invert { get => invert;
            set => invert = value;
        }
        public AxCurve Saturation { get => saturation;
            set => saturation = value;
        }
        public AxCurve Deadzone { get => deadzone;
            set => deadzone = value;
        }

        // Constructor
        public AxAssgn() { }
        public AxAssgn( LogicalAxis logical_axis, InGameAxAssgn axisassign)
        {
            this.logical_axis = logical_axis;

            assgnDate = DateTime.Now;
            invert = axisassign.GetInvert();
            saturation = axisassign.GetSaturation();
            deadzone = axisassign.GetDeadzone();
        }
        public AxAssgn( LogicalAxis logical_axis, DateTime assgnDate, bool invert, AxCurve saturation, AxCurve deadzone)
        {
            this.logical_axis = logical_axis;

            this.assgnDate = assgnDate;
            this.invert = invert;
            this.saturation = saturation;
            this.deadzone = deadzone;
        }

        // Method
        public LogicalAxis GetLogicalAxis() { return logical_axis; }
        public DateTime GetAssignDate() { return assgnDate; }
        public bool GetInvert() { return invert; }
        public AxCurve GetDeadZone() { return deadzone; }
        public AxCurve GetSaturation() { return saturation; }

        object ICloneable.Clone() => Clone();

        public AxAssgn Clone()
        {
            return new AxAssgn(logical_axis, assgnDate, invert, saturation, deadzone);
        }
    }

    public enum AxCurve
    {
        None,
        Small,
        Medium,
        Large
    }

}
