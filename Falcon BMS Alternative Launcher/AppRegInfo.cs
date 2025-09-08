using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

using Microsoft.Win32;

using FalconBMS.Launcher.Windows;
using FalconBMS.Launcher.Override;
using FalconBMS.Launcher.Starter;
using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher
{
    public class AppRegInfo
    {
        // Member
        private string regName = "SOFTWARE\\Wow6432Node\\Benchmark Sims\\Falcon BMS 4.37";

        private BMS_Version bms_Version = BMS_Version.BMS435;

        private OverrideSetting overRideSetting;

        private AbstractStarter starter;

        private string installDir;
        private string exeDir;
        private string currentTheater;
        private string pilotCallsign;

        private bool has16kTerrainTilesInNeedOfProcessing;
        private bool hasLowFreeSpaceOnDrive;

        private MainWindow mainWindow;

        public string theaterOwnConfig = "";

        // Method
        public string GetInstallDir() { return installDir; }
        public string GetCurrentTheater() { return currentTheater; }
        public string GetPilotCallsign() { return pilotCallsign; }

        public bool Has16kTerrainTilesInNeedOfProcessing() { return has16kTerrainTilesInNeedOfProcessing; }
        public bool HasLowFreeSpaceOnDrive() { return hasLowFreeSpaceOnDrive; }

        public OverrideSetting getOverrideWriter() { return overRideSetting; }
        public BMS_Version getBMSVersion() { return bms_Version; }
        public AbstractStarter getLauncher() { return starter; }

        // This will list teh available BMS versions to the launcher list.
        public string[] availableBMSVersions =
        {
            "Falcon BMS 4.38",
            "Falcon BMS 4.38 (Internal)",
            "Falcon BMS 4.37.8 (Internal)",
            "Falcon BMS 4.37.7 (Internal)",
            "Falcon BMS 4.37.6 (Internal)",
            "Falcon BMS 4.37.5 (Internal)",
            "Falcon BMS 4.37 (Internal)",
            "Falcon BMS 4.37",
            "Falcon BMS 4.36",
            "Falcon BMS 4.35",
            "Falcon BMS 4.34",
            "Falcon BMS 4.33",
            "Falcon BMS 4.32"
        };

        public AppRegInfo(MainWindow window)
        {
            this.mainWindow = window;

            Diagnostics.Log("Start Reading Registry.");

            // Enumerate the available versions, and populate the listbox in a more deterministic, reliable sort-order.
            var foundVersions = new List<string>(10);
            foreach (string version in availableBMSVersions)
            {
                if (Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Benchmark Sims\\" + version, writable:false) == null)
                    continue;

                if (BMSExists(version))
                    foundVersions.Add(version);
            }
            if (foundVersions.Count == 0)
            {
                MessageBox.Show(window, "Unable to locate any BMS installation(s)!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Sort order: most recent releases on top.
            foundVersions.Sort((a, b) => { return -1 * String.CompareOrdinal(a, b); });

            // Data-bind the list to the UI control.
            mainWindow.ListBox_BMS.ItemsSource = foundVersions;

            string selectedVersion = null;

            // If we have a saved pref, and it's available, select it.
            if (!string.IsNullOrEmpty(Properties.Settings.Default.BMS_Version))
            {
                int idx = foundVersions.IndexOf(Properties.Settings.Default.BMS_Version);
                if (idx >= 0)
                {
                    selectedVersion = Properties.Settings.Default.BMS_Version;
                    mainWindow.ListBox_BMS.SelectedIndex = idx;
                }
            }

            // If no previously saved pref is found, select the latest and greatest.
            if (string.IsNullOrEmpty(selectedVersion))
            {
                selectedVersion = foundVersions[0];
                mainWindow.ListBox_BMS.SelectedIndex = 0;
            }

            UpdateSelectedBMSVersion(selectedVersion);

            Diagnostics.Log("Finished Reading Registry.");
            return;
        }

        public void UpdateSelectedBMSVersion(string version)
        {
            InitOverriderAndStarterFor(version);

            //TODO: refactor this innocent-looking boolean getter, which has side-effects to init many member fields for
            // the currently selected version.. in the meantime, we must call it again now that we have selectedVersion.
            BMSExists(version);

            // Hack: try to help users with some sanity-checks, before launching into 16K conversion.
            string warning_text = @"WARNING: Found 16K folder containing unprocessed terrain textures -- BMS must run in 2D until the conversion process is complete." +
                "\n\n" +
                @"This conversion process will take several minutes, and require 21 Gb of free drive space.";

            if (this.Has16kTerrainTilesInNeedOfProcessing())
            {
                if (this.HasLowFreeSpaceOnDrive())
                    warning_text += "\n\n" + @"WARNING: drive currently has LOW FREE SPACE!";

                MessageBox.Show(mainWindow, warning_text, "16K Terrain - Texture Convesion Needed", MessageBoxButton.OK, MessageBoxImage.Warning);
                mainWindow.CMD_MONO.IsOn = true;
                mainWindow.CMD_MONO.IsEnabled = false;
            }
            else
            {
                mainWindow.CMD_MONO.IsEnabled = true;
            }

            return;
        }

        public bool BMSExists(string version)
        {
            string regName64 = "SOFTWARE\\Wow6432Node\\Benchmark Sims\\" + version;

            RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName64, writable:false);
            if (rk == null) return false;

            this.regName = regName64;

            this.installDir = (string)rk.GetValue("baseDir");
            if (String.IsNullOrEmpty(installDir)) return false;
            if (!Directory.Exists(installDir)) return false;

            this.currentTheater = ReadCurrentTheater();
            this.pilotCallsign = ReadPilotCallsign();

            //if (platform == Platform.OS_64bit)
            exeDir = installDir + "\\bin\\x64\\Falcon BMS.exe";
            if (!File.Exists(exeDir)) return false;

            FileVersionInfo exe_version_info = FileVersionInfo.GetVersionInfo(exeDir);
            Diagnostics.Log(exe_version_info.ToString());

            int major = exe_version_info.FileMajorPart;
            int minor = exe_version_info.FileMinorPart;
            int build = exe_version_info.FileBuildPart;
            int patch = exe_version_info.FilePrivatePart;

            this.has16kTerrainTilesInNeedOfProcessing = false;
            if (major == 4 && minor == 38 && build == 1 && patch >= 3099)
                ScanFor16K();

            this.hasLowFreeSpaceOnDrive = false;
            if (this.has16kTerrainTilesInNeedOfProcessing)
                CheckDriveFreeSpace();

            return true;
        }

        private void ScanFor16K()
        {
            Diagnostics.Log("BMS version is 4.38.1 - scanning for 16K textures..");

            this.has16kTerrainTilesInNeedOfProcessing = false;

            string dataDir = this.installDir + @"\Data";
            foreach (string d in Directory.GetDirectories(dataDir, "16K", SearchOption.AllDirectories))
            {
                foreach (string f in Directory.GetFiles(d, "*.dds", SearchOption.TopDirectoryOnly))
                {
                    if (DdsFileHeader.FileResolutionIs16Kx16K(f))
                    {
                        Diagnostics.Log("Found one or more 16K dds files: " + f);
                        this.has16kTerrainTilesInNeedOfProcessing = true;
                        return;
                    }
                }
            }
        }

        private void CheckDriveFreeSpace()
        {
            Diagnostics.Log("BMS version is 4.38.1 - checking free space on BMS drive..");

            this.hasLowFreeSpaceOnDrive = false;

            string drive_root = Path.GetPathRoot(this.installDir);

            DriveInfo di = new System.IO.DriveInfo(drive_root);
            long free_space = di.AvailableFreeSpace;

            // 16K texture conversion process will require ~22 Gb of free space.
            const long space_reqd = 30_000_000_000L;
            this.hasLowFreeSpaceOnDrive = (free_space < space_reqd);
            return;
        }

        public void ChangeCfgPath()
        {
            try
            {
                RegistryKey regkeyCFG = Registry.CurrentUser.CreateSubKey("SOFTWARE\\F4Patch\\Settings", writable:true);
                regkeyCFG.SetValue("F4Exe", Path.Combine(installDir, "Launcher.exe"));
                regkeyCFG.Close();
            }
            catch (Exception exCFG)
            {
                Diagnostics.Log(exCFG);
                return;
            }
        }

        public void InitOverriderAndStarterFor(string version)
        {
            switch (version)
            {
                case "Falcon BMS 4.38":
                    bms_Version = BMS_Version.BMS438;
                    overRideSetting = new OverrideSettingFor438(this.mainWindow, this);
                    starter = new Starter438(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.38 (Internal)":
                    bms_Version     = BMS_Version.BMS438I;
                    overRideSetting = new OverrideSettingFor438(this.mainWindow, this);
                    starter         = new Starter438Internal(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.37.6 (Internal)":
                    bms_Version = BMS_Version.BMS437I;
                    overRideSetting = new OverrideSettingFor437(this.mainWindow, this);
                    starter = new Starter437Internal(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.37.5 (Internal)":
                    bms_Version = BMS_Version.BMS437I;
                    overRideSetting = new OverrideSettingFor437(this.mainWindow, this);
                    starter = new Starter437Internal(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.37 (Internal)":
                    bms_Version     = BMS_Version.BMS437I;
                    overRideSetting = new OverrideSettingFor437(this.mainWindow, this);
                    starter         = new Starter437Internal(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.37":
                    bms_Version     = BMS_Version.BMS437;
                    overRideSetting = new OverrideSettingFor437(this.mainWindow, this);
                    starter         = new Starter437(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.36":
                    bms_Version     = BMS_Version.BMS436;
                    overRideSetting = new OverrideSettingFor436(this.mainWindow, this);
                    starter         = new Starter436(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.35":
                    bms_Version     = BMS_Version.BMS435;
                    overRideSetting = new OverrideSettingFor435(this.mainWindow, this);
                    starter         = new Starter435(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.34":
                    bms_Version     = BMS_Version.BMS434U1;
                    overRideSetting = new OverrideSettingFor434U1(this.mainWindow, this);
                    starter         = new Starter434(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.33":
                    bms_Version     = BMS_Version.BMS433;
                    overRideSetting = new OverrideSettingFor433(this.mainWindow, this);
                    starter         = new Starter433(this, this.mainWindow);
                    break;
                case "Falcon BMS 4.32":
                    bms_Version     = BMS_Version.BMS432;
                    overRideSetting = new OverrideSettingFor432(this.mainWindow, this);
                    starter         = new Starter432(this, this.mainWindow);
                    break;
                default:
                    bms_Version = BMS_Version.UNDEFINED;
                    Properties.Settings.Default.BMS_Version = null;
                    throw new ArgumentOutOfRangeException(); // Just to be explicit.
            }
        }

        public string ReadPilotCallsign()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName, writable: false))
            {
                byte[] bits = (byte[])(rk.GetValue("PilotCallsign"));
                if (bits == null || bits[0] == 0x00) return "Viper";

                // Guard against possibility of embedded nullchars, and missing nullterm.
                int n = Array.IndexOf<byte>(bits, 0x00);
                if (n == -1) n = bits.Length;

                return Encoding.ASCII.GetString(bits, 0, n);
            }
        }

        public string ReadPilotName()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName, writable: false))
            {
                byte[] bits = (byte[])(rk.GetValue("PilotName"));
                if (bits == null || bits[0] == 0x00) return "Joe Pilot";

                // Guard against possibility of embedded nullchars, and missing nullterm.
                int n = Array.IndexOf<byte>(bits, 0x00);
                if (n == -1) n = bits.Length;

                return Encoding.ASCII.GetString(bits, 0, n);
            }
        }

        public bool IsUniqueNameDefined()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName, writable:false))
            {
                if (rk == null) return false;

                if (rk.GetValue("PilotCallsign") == null)
                    return false;
                if (rk.GetValue("PilotName") == null)
                    return false;
                if (ReadPilotCallsign() == "Viper")
                    return false;
                if (ReadPilotName() == "Joe Pilot")
                    return false;

                return true;
            }
        }

        public void ChangeName(string callSign, string pilotName)
        {
            pilotCallsign = callSign;

            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName, writable: true))
            {
                if (rk == null)
                    return;

                byte[] buffer = new byte[12];
                byte[] callsignBytes = Encoding.ASCII.GetBytes(callSign);

                int n = Math.Min(callsignBytes.Length, buffer.Length);
                Array.Copy(callsignBytes, buffer, n);

                rk.SetValue("PilotCallsign", _AllocZeroPaddedBuffer(callSign, 12));

                rk.SetValue("PilotName", _AllocZeroPaddedBuffer(pilotName, 20));
            }

            byte[] _AllocZeroPaddedBuffer(string _s, int _len)
            {
                byte[] buffer = new byte[_len];
                byte[] ascii = Encoding.ASCII.GetBytes(_s);

                int n = Math.Min(ascii.Length, buffer.Length);
                Array.Copy(ascii, buffer, n);
                return buffer;
            }

            return;
        }

        public string ReadCurrentTheater()
        {
            using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(regName, writable: false))
            {
                string s = (string)(regkey.GetValue("curTheater"));
                if (String.IsNullOrEmpty(s)) return "Korea KTO";

                return s;
            }
        }

        public void ChangeTheater(ComboBox combobox)
        {
            if (combobox.SelectedIndex == -1)
                return;

            RegistryKey rk = Registry.LocalMachine.OpenSubKey(regName, writable:true);
            if (rk == null)
                return;

            rk.SetValue("curTheater", combobox.Items[combobox.SelectedIndex].ToString());
            rk.Close();

            //switch ((string)mainWindow.Dropdown_TheaterList.SelectedItem)
            //{
            //    case "Israel":
            //        mainWindow.Launch_TheaterConfig.Visibility = Visibility.Visible;
            //        theaterOwnConfig = GetInstallDir() + "\\Data\\Add-On Israel\\Israel Theater Settings.exe";
            //        return;
            //    case "Ikaros":
            //        mainWindow.Launch_TheaterConfig.Visibility = Visibility.Visible;
            //        theaterOwnConfig = GetInstallDir() + "\\Data\\Add-On Ikaros\\Ikaros Settings.exe";
            //        return;
            //    default:
            //        mainWindow.Launch_TheaterConfig.Visibility = Visibility.Collapsed;
            //        break;
            //}
            //mainWindow.Launch_TheaterConfig.Visibility = Visibility.Hidden;
        }
    }

    public enum BMS_Version
    {
        UNDEFINED,
        BMS432,
        BMS433,
        BMS433U1,
        BMS434,
        BMS434U1,
        BMS435,
        BMS436I,
        BMS436,
        BMS437I,
        BMS437,
        BMS438I,
        BMS438
    }
}
