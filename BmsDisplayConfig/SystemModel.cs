using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BmsDisplayConfig
{
    //--------------------------------------------------------------
    // Model class encapsulating the multi-mon API

    internal class SystemModel : ISystemModel
    {
        public RectXYWH GetVirtualDesktopRect()
        {
            Logger.WriteLine("SystemModel.GetVirtualDesktopRect");

            Win32.Rect r1 = Win32.User32.GetVirtualDesktopRect();
            RectXYWH r2 = RectXYWH.FromLTRB(r1);
            Logger.WriteLine($"..desktopRect: {r2}");
            return r2;
        }

        public uint GetNumMonitors()
        {
            return Win32.User32.GetNumMonitors();
        }

        public RectXYWH[] GetMonitorRects()
        {
            Logger.WriteLine("SystemModel.GetMonitorRects");

            uint n = GetNumMonitors();
            Logger.WriteLine($"..numMonitors: {n}");

            Win32.Rect[] absRects = Win32.User32.EnumerateMonitors();
            Debug.Assert(n == absRects.Length);
            Logger.WriteLine($"..numMonitorsEnumerated: {absRects.Length}");

            var relRects = new List<RectXYWH>();
            foreach (Win32.Rect r in absRects)
                relRects.Add(RectXYWH.FromLTRB(r));

            return relRects.ToArray();
        }
    }

    //----------------------------------------
    internal class SystemModel_Test : ISystemModel
    {
        public RectXYWH GetVirtualDesktopRect()
        {
            // 5 monitors .. 3x 1440p horizontally, with 2x 1080 below
            RectXYWH rect = RectXYWH.FromXYWH(-2560, 0, 2560*3, 1440+1080);
            return rect;
        }

        public uint GetNumMonitors()
        {
            return 5;
        }

        public RectXYWH[] GetMonitorRects()
        {
            return new RectXYWH[] {
                RectXYWH.FromXYWH(2560, 0, 2560, 1440),
                RectXYWH.FromXYWH(0, 0, 2560, 1440),
                RectXYWH.FromXYWH(-2560, 0, 2560, 1440),
                RectXYWH.FromXYWH(2560/2-1920, 1440, 1920, 1080),
                RectXYWH.FromXYWH(2560/2, 1440, 1920, 1080)
            };
        }
    }
}

//--------------------------------------------------------------
namespace Win32
{
    //----------------------------------------
    internal struct Rect
    {
        public int left, top, right, bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public int GetWidth() { return this.right - this.left; }
        public int GetHeight() { return this.bottom - this.top; }

        public override string ToString() => $"{{ L:{left},T:{top},R:{right},B:{bottom} }}";
    }

    //--------------------------------------------------------------
    // Managed wrappers
    //
    internal static class User32
    {
        //----------------------------------------
        public static Rect GetVirtualDesktopRect()
        {
            const int SM_XVIRTUALSCREEN = 76;
            const int SM_YVIRTUALSCREEN = 77;
            const int SM_CXVIRTUALSCREEN = 78;
            const int SM_CYVIRTUALSCREEN = 79;

            int left = _Interop_User32.GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = _Interop_User32.GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = _Interop_User32.GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = _Interop_User32.GetSystemMetrics(SM_CYVIRTUALSCREEN);

            int right = left + width;
            int bottom = top + height;
            return new Rect(left, top, right, bottom);
        }

        //----------------------------------------
        public static uint GetNumMonitors()
        {
            const int SM_CMONITORS = 80;
            int numMon = _Interop_User32.GetSystemMetrics(SM_CMONITORS);
            return (uint)numMon;
        }

        //----------------------------------------
        public static Rect[] EnumerateMonitors()
        {
            List<Rect> list = new List<Rect>();

            GCHandle gch = GCHandle.Alloc(list, GCHandleType.Weak);
            IntPtr pList = (IntPtr)gch;
            try
            {
                bool ok = _Interop_User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _MonitorEnumProc, pList);
                if (!ok) throw new Win32Exception("EnumDisplayMonitors");
            }
            finally
            {
                gch.Free();
            }

            return list.ToArray();
        }

        private static bool _MonitorEnumProc(IntPtr hMon, IntPtr hdc, ref Rect rect, IntPtr lParam)
        {
            BmsDisplayConfig.Logger.WriteLine("User32._MonitorEnumProc");

            var list = GCHandle.FromIntPtr(lParam).Target as List<Rect>;

            Rect r2 = new Rect(rect.left, rect.top, rect.right, rect.bottom);
            BmsDisplayConfig.Logger.WriteLine($"..rect: {r2}");
            list.Add(r2);

            return true;
        }

        //--------------------------------------------------------------
        // Interop declarations
        //
        static class _Interop_User32
        {
            [DllImport("User32.dll")]
            public extern static int GetSystemMetrics(int m);

            [DllImport("User32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
            public extern static bool EnumDisplayMonitors(IntPtr hdc, ref Rect lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
            [DllImport("User32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
            public extern static bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)] [return: MarshalAs(UnmanagedType.Bool)]
            public delegate bool MonitorEnumProc(IntPtr hMon, IntPtr hdc, ref Rect pRect, IntPtr lParam);
        }
    }
}