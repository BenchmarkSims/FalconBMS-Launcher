namespace BmsDisplayConfig
{
    //----------------------------------------
    internal interface IDataModel
    {
        bool DirtyFlag { get; set; }
        void LoadDspFileOrDefault();
        void SaveDspFile();

        RectXYWH Get2dUIWindowPlacement();
        RectXYWH Get3dSimWindowPlacement();
        void Set3dSimWindowPlacement(RectXYWH r);
    }

    //----------------------------------------
    internal interface ISystemModel
    {
        RectXYWH GetVirtualDesktopRect();
        uint GetNumMonitors();
        RectXYWH[] GetMonitorRects();
    }
}
