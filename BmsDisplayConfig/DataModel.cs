using System;
using System.Collections.Generic;
using System.IO;

namespace BmsDisplayConfig
{
    //--------------------------------------------------------------
    // Model class encapsulating the "d3d11.dsp" file

    internal class DataModel : IDataModel
    {
        bool _isDirty = false;

        DisplayOptionsFile _dspData = DisplayOptionsFile.FromDefaults();

        public bool DirtyFlag
        {
            get => _isDirty;
            set => _isDirty = value;
        }

        public RectXYWH Get2dUIWindowPlacement()
        {
            return RectXYWH.FromXYWH(
                _dspData.UiDisplayMode.Left,
                _dspData.UiDisplayMode.Top,
                (int)_dspData.UiDisplayMode.Width,
                (int)_dspData.UiDisplayMode.Height);
        }

        public RectXYWH Get3dSimWindowPlacement()
        {
            return RectXYWH.FromXYWH(
                _dspData.SimDisplayMode.Left,
                _dspData.SimDisplayMode.Top,
                (int)_dspData.SimDisplayMode.Width,
                (int)_dspData.SimDisplayMode.Height);
        }

        public void Set3dSimWindowPlacement(RectXYWH r)
        {
            if (r.Equals(this.Get3dSimWindowPlacement())) return;

            _dspData.SimDisplayMode.Left = r.x;
            _dspData.SimDisplayMode.Top = r.y;
            _dspData.SimDisplayMode.Width = (uint)r.width;
            _dspData.SimDisplayMode.Height = (uint)r.height;

            _isDirty = true;
        }

        public void LoadDspFileOrDefault()
        {
            Logger.WriteLine("DataModel.LoadDspFileOrDefault");

            _isDirty = false;

            string dspPath = ConfigFileProbe.FindDspFilePath();
            Logger.WriteLine($"..dspPath: {dspPath}");

            if (File.Exists(dspPath))
                _dspData = DisplayOptionsFile.Deserialize(dspPath);
            else
                _dspData = DisplayOptionsFile.FromDefaults();
        }

        public void SaveDspFile()
        {
            Logger.WriteLine("DataModel.SaveDspFile");

            string dspPath = ConfigFileProbe.FindDspFilePath();
            _dspData.Serialize(dspPath);
            _isDirty = false;

            Logger.WriteLine("..ok");
        }
    }

    //----------------------------------------
    internal class DataModel_Test : IDataModel
    {
        bool _isDirty = false;

        public bool DirtyFlag 
        { 
            get => _isDirty;
            set => _isDirty = value;
        }

        public RectXYWH Get2dUIWindowPlacement()
        {
            const int width = 1920, height = 1080;
            return RectXYWH.FromXYWH(100, 100, 100 + width, 100 + height);
        }

        public RectXYWH Get3dSimWindowPlacement()
        {
            const int width = 2560, height = 1440;
            return RectXYWH.FromXYWH(-2560, 0, width, height);
        }

        public void Set3dSimWindowPlacement(RectXYWH r)
        {
            if (r.Equals(this.Get3dSimWindowPlacement())) return;

            _isDirty = true;
        }

        public void LoadDspFileOrDefault()
        {
            //NOP
            _isDirty = false;
        }

        public void SaveDspFile()
        {
            //NOP
            _isDirty = false;
        }
    }
}
