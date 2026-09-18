using System;

namespace FalconBMS.Launcher.Input
{

    public class AxAssgn : ICloneable
    {
        // Member
        protected LogicalAxis? logical_axis = null;

        protected bool invert;
        protected AxCurve saturation = 0;
        protected AxCurve deadzone = 0;

        // Property for XML
        public string AxisName
        {
            get
            {
                return logical_axis.HasValue ? logical_axis.ToString() : String.Empty;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    logical_axis = null;
                else
                    logical_axis = (LogicalAxis)Enum.Parse(typeof(LogicalAxis), value);
            }
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
        public AxAssgn() { } // for xml deserialization

        public AxAssgn( LogicalAxis? logical_axis, InGameAxAssgn axisassign)
        {
            this.logical_axis = logical_axis;

            invert = axisassign.GetInvert();
            saturation = axisassign.GetSaturation();
            deadzone = axisassign.GetDeadzone();
        }
        public AxAssgn( LogicalAxis? logical_axis, bool invert, AxCurve saturation, AxCurve deadzone)
        {
            this.logical_axis = logical_axis;

            this.invert = invert;
            this.saturation = saturation;
            this.deadzone = deadzone;
        }

        // Method
        public LogicalAxis? GetLogicalAxis() { return logical_axis; }
        public bool GetInvert() { return invert; }
        public AxCurve GetDeadZone() { return deadzone; }
        public AxCurve GetSaturation() { return saturation; }

        object ICloneable.Clone() => Clone();

        public AxAssgn Clone()
        {
            return new AxAssgn(logical_axis, invert, saturation, deadzone);
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
