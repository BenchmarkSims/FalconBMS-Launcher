using System;
using System.Text.RegularExpressions;

using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Input
{
    internal struct AntiHisteresisFilter_Int32 //can't do math on generics :(
    {
        int _min_delta;

        int _latest_val;
        int _latest_delta;

        public AntiHisteresisFilter_Int32( int min_delta, int init_val )
        {
            _min_delta = min_delta;

            _latest_val = init_val;
            _latest_delta = 0;
        }

        public int LatestValue => _latest_val;

        public int UpdateValue( int val )
        {
            int delta = (val - _latest_val);

            // if d > 1% range, update
            if (Math.Abs(delta) > _min_delta)
            {
                _latest_val = val;
                _latest_delta = delta;
                return _latest_val;
            }

            // if d in same direction as prev_d, update
            if (Math.Sign(delta) == Math.Sign(_latest_delta))
            {
                _latest_val = val;
                _latest_delta = delta;
                return _latest_val;
            }

            return _latest_val;
        }
    }

    internal static class DirectInputHelper
    {
        public static Guid GetDeviceProductGuid( Guid instance_guid ) // aka the "PIDVID" guid
        {
            using (Device dev = new Device(instance_guid))
            {
                return dev.DeviceInformation.ProductGuid;
            }
        }

        public static string GetDeviceProductName( Guid instance_guid, bool sanitized = false )
        {
            using (Device dev = new Device(instance_guid))
            {
                string s = dev.DeviceInformation.ProductName;
                if (sanitized) return _SanitizeProductName(s);
                else return s;
            }
        }
        static string _SanitizeProductName( string s )
        {
            // Redact any chars not appropriate for filenames.  Does NOT trim leading/trailing space.
            return Regex.Replace(s, @"[^A-Za-z0-9\~\`\[\]\{\}\-_\=\'\x20]", String.Empty);
        }

        public static string GetDeviceShortName( Guid instance_guid )
        {
            using (Device dev = new Device(instance_guid))
            {
                string s = dev.DeviceInformation.ProductGuid.ToString();
                s = s.Substring(4, 4);

                switch (s)
                {
                    case "044F": s = "TM"; break;
                    case "231D": s = "VKB"; break;
                    case "3344": s = "Virpil"; break;
                    case "4098": s = "Winwing"; break;
                    case "046D": s = "Logitech"; break;
                    case "06A3": s = "Saitek"; break;
                    case "045E": s = "MS"; break;
                    case "068E": s = "CH"; break;
                    default: s = "Joy"; break;
                }
                return s;
            }
        }

        public static string GetKeyboardKeyText( int scancode )
        {
            Key key = (Key)scancode;
            return key.ToString();
        }
    }

}