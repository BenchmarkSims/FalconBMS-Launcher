using System.Collections.Generic;

namespace FalconBMS.Launcher.Windows
{
    public enum LauncherRoute { Primary, ThirdParty }

    public sealed class LauncherItem
    {
        public string Id { get; set; }         // e.g., "WDP"
        public string Label { get; set; }      // e.g., "Weapon Delivery Planner"
        public string SubLabel { get; set; }   // optional second line
        public string IconPath { get; set; }   // e.g., "/Resources/WDP.png"
        public LauncherRoute Route { get; set; }
    }

    public static class LauncherCatalog
    {
        // Map the image location
        private const string Pack = "pack://application:,,,/FalconBMS_Alternative_Launcher;component/Resources/";

        // Primary strip (9 columns)
        public static IReadOnlyList<LauncherItem> PrimaryStrip => _primaryStrip;
        private static readonly List<LauncherItem> _primaryStrip = new List<LauncherItem>
        {
            new LauncherItem { 
                Id="UPD",  
                Label="Update",
                IconPath=$"{Pack}UPD.png",
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="CFG",  
                Label="Config",
                IconPath=$"{Pack}CFG.png",
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="MMC",  
                Label="Display Config",
                IconPath=$"{Pack}BmsMultiMon.png",
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="RTTC", 
                Label="RTT Client",
                IconPath=$"{Pack}RTTClient64.png",
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="RTTS", 
                Label="RTT Server",
                IconPath=$"{Pack}RTTServer64.png",     
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="IVCC", 
                Label="IVC Client", 
                SubLabel="(MP Radio)",
                IconPath=$"{Pack}IVCC.png", 
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="IVCS", 
                Label="IVC Server", 
                SubLabel="(MP Radio)",
                IconPath=$"{Pack}IVCS.png", 
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="AVC",
                Label="Avionics",
                SubLabel="Configurator",
                IconPath=$"{Pack}AVC.png",
                Route=LauncherRoute.Primary
            },
            new LauncherItem { 
                Id="EDIT",
                Label="Editor",
                IconPath=$"{Pack}EDIT.png",
                Route=LauncherRoute.Primary
            },
        };

        // Third-party strip (5 columns)
        public static IReadOnlyList<LauncherItem> ThirdPartyStrip => _thirdPartyStrip;
        private static readonly List<LauncherItem> _thirdPartyStrip = new List<LauncherItem>
        {
            new LauncherItem { 
                Id="WDP",
                Label="Weapon Delivery Planner",
                IconPath=$"{Pack}WDP.png",
                Route=LauncherRoute.ThirdParty 
            },
            new LauncherItem { 
                Id="MC", 
                Label="Mission Commander",
               IconPath=$"{Pack}MC.png", 
                Route=LauncherRoute.ThirdParty
            },
            new LauncherItem { 
                Id="WC",
                Label="Weather Commander",
               IconPath=$"{Pack}WC.png", 
                Route=LauncherRoute.ThirdParty 
            },
            new LauncherItem { 
                Id="F4WX",
                Label="F4Wx Real Weather",
                IconPath=$"{Pack}F4WX.png",
                Route=LauncherRoute.ThirdParty
            },
            new LauncherItem { 
                Id="F4RADAR", 
                Label="F4 RADAR C2 (AWACS) Tool",
               IconPath=$"{Pack}F4RADAR.png", 
                Route=LauncherRoute.ThirdParty 
            },
        };
    }
}
