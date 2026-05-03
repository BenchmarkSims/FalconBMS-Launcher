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

        private MainWindow mainWindow;

        public string theaterOwnConfig = "";
        private string currentExeFourPartVersion = "";

        // Method
        public string GetInstallDir() { return installDir; }
        public string GetCurrentTheater() { return currentTheater; }
        public string GetPilotCallsign() { return pilotCallsign; }

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
                if (Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Benchmark Sims\\" + version, writable: false) == null)
                    continue;

                if (BMSExists(version))
                    foundVersions.Add(version);
            }
            if (foundVersions.Count == 0)
            {
                MessageBox.Show(window, "Unable to locate any BMS installation(s)!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Sort order: most recent releases on top (by the registry-key strings).
            foundVersions.Sort((a, b) => { return -1 * String.CompareOrdinal(a, b); });

            // Build the display list in the SAME order (Key = real key, Value = pretty label)
            var sortedItems = new List<KeyValuePair<string, string>>(foundVersions.Count);
            foreach (var v in foundVersions)
            {
                // Ensure exeDir is set for this version
                BMSExists(v);

                // Read the real version parts from the EXE
                int major = 0, minor = 0, build = 0, patch = 0;
                try
                {
                    var vi = FileVersionInfo.GetVersionInfo(exeDir);
                    major = vi.FileMajorPart;   // ex: 4
                    minor = vi.FileMinorPart;   // ex: 38
                    build = vi.FileBuildPart;   // ex: 1
                    patch = vi.FilePrivatePart; // ex: 3236
                }
                catch
                {
                    // If anything goes wrong, leave zeros
                }

                bool isInternal = v.EndsWith("(Internal)", StringComparison.Ordinal);

                string label;
                if (isInternal)
                {
                    // Internal: {major}.{minor}.{build} Build {patch} (Internal)
                    label = $"Falcon BMS {major}.{minor}.{build} (Internal Build {patch})";
                }
                else
                {
                    // Non-Internal: {major}.{minor}.{build}
                    label = $"Falcon BMS {major}.{minor}.{build}";
                }

                sortedItems.Add(new KeyValuePair<string, string>(v, label));
            }

            // Bind pretty labels but keep real keys via SelectedValue
            mainWindow.ListBox_BMS.ItemsSource = sortedItems;
            mainWindow.ListBox_BMS.DisplayMemberPath = "Value";
            mainWindow.ListBox_BMS.SelectedValuePath = "Key";

            string selectedVersion = null;

            // If we have a saved pref, and it's available, select it.
            if (!string.IsNullOrEmpty(Properties.Settings.Default.BMS_Version))
            {
                int idx = foundVersions.IndexOf(Properties.Settings.Default.BMS_Version);
                if (idx >= 0)
                {
                    selectedVersion = Properties.Settings.Default.BMS_Version;
                    // Select by value (real registry key)
                    mainWindow.ListBox_BMS.SelectedValue = selectedVersion;
                }
            }

            // If no previously saved pref is found, select the latest and greatest.
            if (string.IsNullOrEmpty(selectedVersion))
            {
                selectedVersion = foundVersions[0];
                // Select by value (real registry key)
                mainWindow.ListBox_BMS.SelectedValue = selectedVersion;
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

            currentExeFourPartVersion = $"{major}.{minor}.{build}.{patch}";

            return true;
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
