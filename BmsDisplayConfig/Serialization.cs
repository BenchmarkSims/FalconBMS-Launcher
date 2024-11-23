using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BmsDisplayConfig
{
    internal static class ConfigFileProbe
    {
        public static string FindDspFilePath()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string pathToExe = assembly.Location;

            string exeDir = Path.GetDirectoryName(pathToExe);

            string cfgDir = Path.Combine(exeDir, "User/Config");
            if (Directory.Exists(cfgDir))
            {
                return Path.Combine(cfgDir, "d3d11.dsp");
            }

            string baseDir = Directory.GetParent(exeDir).FullName;
            cfgDir = Path.Combine(baseDir, "User/Config");
            if (Directory.Exists(cfgDir))
            {
                return Path.Combine(cfgDir, "d3d11.dsp");
            }

#if DEBUG
            cfgDir = @"C:\Falcon BMS 4.37.5 (Internal)\User\Config";
            if (Directory.Exists(cfgDir))
            {
                return Path.Combine(cfgDir, "d3d11.dsp");
            }
#endif

            throw new DirectoryNotFoundException($"Unable to locate /User/Config subfolder, relative to \x22{pathToExe}\x22");
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DisplayOptionsFile
    {
        public DisplayMode3D UiDisplayMode;
        public DisplayMode3D SimDisplayMode;

        private bool b1, b2, b3, b4, b5, b6, b7, b8; // legacy values, all hardcoded true in modern codebase

        public static DisplayOptionsFile FromDefaults()
        {
            var dsp = new DisplayOptionsFile();
            dsp.Initialize();
            return dsp;
        }

        void Initialize()
        {
            UiDisplayMode.Fullscreen = false;
            UiDisplayMode.WindowBorders = false;

            UiDisplayMode.AdapterIdx = 0;
            UiDisplayMode.OutputIdx = 0;

            UiDisplayMode.Left = 100;
            UiDisplayMode.Top = 100;
            UiDisplayMode.Width = 1920;
            UiDisplayMode.Height = 1080;

            UiDisplayMode.RefreshRateNumerator = 60;
            UiDisplayMode.RefreshRateDenominator = 1;

            UiDisplayMode.MsaaSampleCount = 1;
            UiDisplayMode.MsaaQualityLevel = 0;

            UiDisplayMode.ScalingBehavior = 0;//unspecified
            UiDisplayMode.ScanLineOrdering = 1;//progressive
            UiDisplayMode.SurfaceFormat = 0x57; //32-bit ARGB

            UiDisplayMode.BackBufferCount = 2;
            UiDisplayMode.Vsync = 0;

            SimDisplayMode = UiDisplayMode;//bitwise copy

            b1 = b2 = b3 = b4 = b5 = b6 = b7 = b8 = true;
        }

        public static DisplayOptionsFile Deserialize(string dspPath)
        {
            if (!File.Exists(dspPath))
                return FromDefaults();

            using (FileStream fs = File.OpenRead(dspPath))
            {
                DisplayOptionsFile dsp = new DisplayOptionsFile();
                dsp.Initialize();

                BinaryReader br = new BinaryReader(fs);

                dsp.UiDisplayMode.Fullscreen = br.ReadBoolean();
                dsp.UiDisplayMode.WindowBorders = br.ReadBoolean();
                br.ReadByte();//padding
                br.ReadByte();//padding
                dsp.UiDisplayMode.AdapterIdx = br.ReadUInt32();
                dsp.UiDisplayMode.OutputIdx = br.ReadUInt32();
                dsp.UiDisplayMode.Left = br.ReadInt32();
                dsp.UiDisplayMode.Top = br.ReadInt32();
                dsp.UiDisplayMode.Width = br.ReadUInt32();
                dsp.UiDisplayMode.Height = br.ReadUInt32();
                dsp.UiDisplayMode.RefreshRateNumerator = br.ReadUInt32();
                dsp.UiDisplayMode.RefreshRateDenominator = br.ReadUInt32();
                dsp.UiDisplayMode.MsaaSampleCount = br.ReadUInt32();
                dsp.UiDisplayMode.MsaaQualityLevel = br.ReadUInt32();
                dsp.UiDisplayMode.ScalingBehavior = br.ReadUInt32();
                dsp.UiDisplayMode.ScanLineOrdering = br.ReadUInt32();
                dsp.UiDisplayMode.SurfaceFormat = br.ReadUInt32();
                dsp.UiDisplayMode.BackBufferCount = br.ReadUInt32();
                dsp.UiDisplayMode.Vsync = br.ReadUInt32();

                dsp.SimDisplayMode.Fullscreen = br.ReadBoolean();
                dsp.SimDisplayMode.WindowBorders = br.ReadBoolean();
                br.ReadByte();//padding
                br.ReadByte();//padding
                dsp.SimDisplayMode.AdapterIdx = br.ReadUInt32();
                dsp.SimDisplayMode.OutputIdx = br.ReadUInt32();
                dsp.SimDisplayMode.Left = br.ReadInt32();
                dsp.SimDisplayMode.Top = br.ReadInt32();
                dsp.SimDisplayMode.Width = br.ReadUInt32();
                dsp.SimDisplayMode.Height = br.ReadUInt32();
                dsp.SimDisplayMode.RefreshRateNumerator = br.ReadUInt32();
                dsp.SimDisplayMode.RefreshRateDenominator = br.ReadUInt32();
                dsp.SimDisplayMode.MsaaSampleCount = br.ReadUInt32();
                dsp.SimDisplayMode.MsaaQualityLevel = br.ReadUInt32();
                dsp.SimDisplayMode.ScalingBehavior = br.ReadUInt32();
                dsp.SimDisplayMode.ScanLineOrdering = br.ReadUInt32();
                dsp.SimDisplayMode.SurfaceFormat = br.ReadUInt32();
                dsp.SimDisplayMode.BackBufferCount = br.ReadUInt32();
                dsp.SimDisplayMode.Vsync = br.ReadUInt32();

                dsp.b1 = br.ReadBoolean();
                dsp.b2 = br.ReadBoolean();
                dsp.b3 = br.ReadBoolean();
                dsp.b4 = br.ReadBoolean();
                dsp.b5 = br.ReadBoolean();
                dsp.b6 = br.ReadBoolean();
                dsp.b7 = br.ReadBoolean();
                dsp.b8 = br.ReadBoolean();

                return dsp;
            }
        }

        public void Serialize(string dspPath)
        {
            using (FileStream fs = File.Create(dspPath))
            {
                BinaryWriter bw = new BinaryWriter(fs);

                bw.Write((bool)this.UiDisplayMode.Fullscreen);
                bw.Write((bool)this.UiDisplayMode.WindowBorders);
                bw.Write((byte)0);//padding
                bw.Write((byte)0);//padding
                bw.Write((uint)this.UiDisplayMode.AdapterIdx);
                bw.Write((uint)this.UiDisplayMode.OutputIdx);
                bw.Write((int)this.UiDisplayMode.Left);
                bw.Write((int)this.UiDisplayMode.Top);
                bw.Write((uint)this.UiDisplayMode.Width);
                bw.Write((uint)this.UiDisplayMode.Height);
                bw.Write((uint)this.UiDisplayMode.RefreshRateNumerator);
                bw.Write((uint)this.UiDisplayMode.RefreshRateDenominator);
                bw.Write((uint)this.UiDisplayMode.MsaaSampleCount);
                bw.Write((uint)this.UiDisplayMode.MsaaQualityLevel);
                bw.Write((uint)this.UiDisplayMode.ScalingBehavior);
                bw.Write((uint)this.UiDisplayMode.ScanLineOrdering);
                bw.Write((uint)this.UiDisplayMode.SurfaceFormat);
                bw.Write((uint)this.UiDisplayMode.BackBufferCount);
                bw.Write((uint)this.UiDisplayMode.Vsync);

                bw.Write((bool)this.SimDisplayMode.Fullscreen);
                bw.Write((bool)this.SimDisplayMode.WindowBorders);
                bw.Write((byte)0);//padding
                bw.Write((byte)0);//padding
                bw.Write((uint)this.SimDisplayMode.AdapterIdx);
                bw.Write((uint)this.SimDisplayMode.OutputIdx);
                bw.Write((int)this.SimDisplayMode.Left);
                bw.Write((int)this.SimDisplayMode.Top);
                bw.Write((uint)this.SimDisplayMode.Width);
                bw.Write((uint)this.SimDisplayMode.Height);
                bw.Write((uint)this.SimDisplayMode.RefreshRateNumerator);
                bw.Write((uint)this.SimDisplayMode.RefreshRateDenominator);
                bw.Write((uint)this.SimDisplayMode.MsaaSampleCount);
                bw.Write((uint)this.SimDisplayMode.MsaaQualityLevel);
                bw.Write((uint)this.SimDisplayMode.ScalingBehavior);
                bw.Write((uint)this.SimDisplayMode.ScanLineOrdering);
                bw.Write((uint)this.SimDisplayMode.SurfaceFormat);
                bw.Write((uint)this.SimDisplayMode.BackBufferCount);
                bw.Write((uint)this.SimDisplayMode.Vsync);

                bw.Write((bool)this.b1);
                bw.Write((bool)this.b2);
                bw.Write((bool)this.b3);
                bw.Write((bool)this.b4);
                bw.Write((bool)this.b5);
                bw.Write((bool)this.b6);
                bw.Write((bool)this.b7);
                bw.Write((bool)this.b8);
            }
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct DisplayMode3D
    {
        public bool Fullscreen;
        public bool WindowBorders;
        //byte _padding1;
        //byte _padding2;
        public uint AdapterIdx;
        public uint OutputIdx;
        public int Left;
        public int Top;
        public uint Width;
        public uint Height;
        public uint RefreshRateNumerator;
        public uint RefreshRateDenominator;
        public uint MsaaSampleCount;
        public uint MsaaQualityLevel;
        public uint ScalingBehavior;
        public uint ScanLineOrdering;
        public uint SurfaceFormat; //0x57
        public uint BackBufferCount;
        public uint Vsync;
    }

}
