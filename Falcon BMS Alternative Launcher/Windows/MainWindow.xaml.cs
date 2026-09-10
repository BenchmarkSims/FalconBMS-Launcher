using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

using MahApps.Metro.Controls;

using FalconBMS.Launcher.Input;

namespace FalconBMS.Launcher.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        static SteamVR steamVR = new SteamVR();

        public MainWindow()
        {
            try
            {
                //RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }

        public static DeviceControl deviceControl;

        public AppRegInfo appReg;

        private AppProperties appProperties;

        internal static bool bmsHasBeenLaunched = false;

        private DispatcherTimer DeviceScanTimer;
        List<DirectInputListener> _input_listeners;

        protected override void OnInitialized(EventArgs e)
        {
            // Ensure base window object is fully initialized, before proceeding.
            base.OnInitialized(e);

            // Site ourselves as the app's main window.. this is a bit of a codesmell but necessary for more determinstic use of MessageBox and other dialogs.
            Program.mainWin = this;

            Diagnostics.Log("Post_OnInitialized.");

            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                System.Version ver = asm.GetName().Version;

                string versionLabel = "FalconBMS Launcher v" + ver.ToString();
                AL_Version_Number.Content = versionLabel;

                Diagnostics.Log(versionLabel);

                System.Threading.ThreadPool.QueueUserWorkItem(_ThreadPool_UpdateRss, this.Dispatcher);
            }
            catch (Exception expass)
            {
                Diagnostics.Log(expass);
            }

            try
            {
                appProperties = new AppProperties(this);
                appReg = new AppRegInfo(this);
                InitDevices();

                if (appReg.getBMSVersion() == BMS_Version.UNDEFINED)
                {
                    Diagnostics.Log("Failed to find BMS installation.");
                    Diagnostics.ShowErrorMsgbox("Could Not Find BMS!");
                    Close();
                    return;
                }

                StartVR();
            }
            catch (Exception exclose)
            {
                Diagnostics.Log(exclose);
                Diagnostics.ShowErrorMsgbox(exclose);
                Close();
                return;
            }
            Diagnostics.Log("Post_OnInitialized complete.");
        }

        // Deferred init for things that require HWND interop.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Setup hook for WM_DEVICECHANGED.
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(_WndProc);

            // Begin listening to buffered (queued) DirectInput messages.
            var dev_map = DirectInputDeviceMap.Singleton;
            dev_map.RefreshDeviceList();

            SubscribeToDirectInputEvents();

            return;
        }

        internal void SubscribeToDirectInputEvents( )
        {
            var dev_map = DirectInputDeviceMap.Singleton;

            if (_input_listeners != null)
            {
                // First unsub from existing listeners.
                foreach (var listener in _input_listeners)
                {
                    listener.KeyboardInputReceived -= MainWindow_KeyboardInputReceived;

                    listener.AxisInputReceived -= MainWindow_AxisInputReceived;
                    listener.PovInputReceived -= MainWindow_PovInputReceived;
                    listener.ButtonInputReceived -= MainWindow_ButtonInputReceived;
                }
            }

            // Re-establish listeners for keybd and all joysticks.
            _input_listeners = new List<DirectInputListener>
            {
                dev_map.GetListenerForKeyboard()
            };
            foreach (Guid g in dev_map.GetDeviceInstanceGuids(include_keybd:false))
            {
                _input_listeners.Add(dev_map.GetListenerForJoystick(g));
            }

            foreach (var listener in _input_listeners)
            {
                listener.KeyboardInputReceived += MainWindow_KeyboardInputReceived;

                listener.AxisInputReceived += MainWindow_AxisInputReceived;
                listener.PovInputReceived += MainWindow_PovInputReceived;
                listener.ButtonInputReceived += MainWindow_ButtonInputReceived;
            }

            return;
        }

        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            // Handle WM_DEVICECHANGED notification.
            const int WM_DEVICECHANGED = 0x0219;
            const int DBT_DEVNODES_CHANGED = 7;

            switch (msg)
            {
                case WM_DEVICECHANGED:
                    Diagnostics.Log($"WM_DEVICECHANGE: {wParam}, {lParam}", Diagnostics.LogLevels.Info);
                    if ((int)wParam == DBT_DEVNODES_CHANGED)
                    {
                        // This WM notif tends to arrive in bursts -- use a one-shot timer to rescan devices after 500ms timeout.
                        if (DeviceScanTimer != null)
                            DeviceScanTimer.Stop();

                        DeviceScanTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, 
                            DeviceScanTimer_Tick, this.Dispatcher);
                    }
                break;
            }

            return (IntPtr)1;//TRUE
        }

        private void _ThreadPool_UpdateRss(object state)
        {
            //NB: We are on a background threadpool thread -- no interaction with UI elements allowed!
            try
            {
                Dispatcher thisDispatcher = (Dispatcher)state;

                RSSReader.Read("https://www.falcon-bms.com/rss.xml", "https://www.falcon-bms.com");
                RSSReader.Read("https://www.falcon-lounge.com/news/feed/", "https://www.falcon-lounge.com");

                Diagnostics.Log("Completed RSS fetch on background-thread.");

                // Schedule remaining work via PostMessage, back on the UI-thread's message queue.
                thisDispatcher.BeginInvoke((Action)delegate { _Post_UpdateRss(); });
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                return;
            }
        }

        private void _Post_UpdateRss()
        {
            if (appReg == null)
                return;

            if (this.News == null)
                return;

            RSSReader.Write(News);
            Diagnostics.Log("RSS update finished.");
        }

        private void StartVR()
        {
            Diagnostics.Log("Start VR Check.");

            if ((bool)VR_SteamVR.IsVisible)
                if ((bool)VR_SteamVR.IsChecked)
                    steamVR.Start();

            Diagnostics.Log("Finished VR Check.");
        }

        private void InitDevices()
        {
            Diagnostics.Log("Start Init -- detect Theaters, load Key file, enum Devices and load XMLs");

            ReloadTheatersKeysJoysAndXml();

            Diagnostics.Log("Finished Init");
        }

        private void ReloadTheatersKeysJoysAndXml()
        {
            try
            {
                // Read Theater List
                TheaterList.PopulateAndSave(appReg, Dropdown_TheaterList);

                appReg.ChangeCfgPath();

                // Enum devices and load xml files.
                deviceControl = DeviceControl.EnumerateAttachedDevicesAndLoadXml(appReg);
                UpdateInGameAxisMapping();

                // Read key file(s)
                deviceControl.LoadKeyBindingsFromUserOrStockKeyfiles(appReg);

                // Update category headers, and data-binding -- the grid displays both keyfile records (rows) and joys (columns).
                UpdateCategoryHeaders();
                UpdateDataGridBindingSource();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        private void UpdateInGameAxisMapping()
        {
            try
            {
                // Reset All Axis Settings
                joyAssign_2_inGameAxis();
                ResetMainWindow_Axis();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        private void DeviceScanTimer_Tick(object sender, EventArgs e)
        {
            this.DeviceScanTimer.Stop();
            this.DeviceScanTimer = null;

            Diagnostics.Log("Scanning for new/removed devices..");
            var dev_map = DirectInputDeviceMap.Singleton;
            bool updated = dev_map.RefreshDeviceList();

            try
            {
                if (updated)
                {
                    Diagnostics.Log("Device list refreshed - RELOADING", Diagnostics.LogLevels.Info);
                    ReloadTheatersKeysJoysAndXml();

                    SubscribeToDirectInputEvents(); //un-sub and re-sub to events, as needed
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }

            return;
        }

        /// <summary>
        /// Execute when quiting this app.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            DirectInputDeviceMap.Singleton.ShutdownAllListeners();

            try
            {
                if (appReg == null)
                    return;

                // Save UI Properties(Like Button Status).
                appProperties.SaveUISetup();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                return;
            }
        }
        
        /// <summary>
        /// Execute/Stop timer event when changing top TAB menu (Launcher/AxisAssign/KeyMapping).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!e.Source.Equals(LargeTab))
                    return;

                if (LargeTab.SelectedIndex == 1)
                {
                    this._UpdateUI_Axes();
                }

                if (LargeTab.SelectedIndex == 2)
                {
                    KeyMappingGrid.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }
        
        /// <summary>
        /// Rewrite Theater setting in the registry and Show/Hide Theater own config icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dropdown_TheaterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                appReg.ChangeTheater(Dropdown_TheaterList);
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }
        
        /// <summary>
        /// Launch Theater own config.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Launch_TheaterConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Utils.LaunchProcess(appReg.theaterOwnConfig);
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }
        
        /// <summary>
        /// Open BMS Docs and Manuals.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenDocs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Utils.LaunchAppOrBrowserUrl(appReg.GetInstallDir() + "/Docs");
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        /// <summary>
        ///  Launch BMS utilities.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (appReg.IsUniqueNameDefined() == false)
                {
                    CallsignWindow.ShowCallsignWindow(appReg);

                    if (appReg.IsUniqueNameDefined() == false)
                        return;
                }

                appReg.getLauncher().execute(sender);
            }
            catch (FileNotFoundException ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        /// <summary>
        /// OverrideSettings.
        /// </summary>
        public void executeOverride()
        {
            try
            {
                // throw new Exception("An exception occurs.");
                if (ApplicationOverride.IsChecked == true)
                {
                    if (Properties.Settings.Default.FirstTimeNonOverride)
                    {
                        string textMessage = "You are about to launch BMS without applying setup-overrides from AxisAssign and KeyMapping section.";
                        MessageBoxResult mbr = MessageBox.Show(Program.mainWin, textMessage, "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (mbr != MessageBoxResult.Yes) return;

                        Properties.Settings.Default.FirstTimeNonOverride = false;
                    }
                }
                else
                {
                    appReg.getOverrideWriter().Execute(s_map_logical_axes, deviceControl);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }
        public void minimizeWindowUntilProcessEnds(System.Diagnostics.Process process)
        {
            process.Exited += window_Normal;
            process.EnableRaisingEvents = true;
            WindowState = WindowState.Minimized;
        }

        private void window_Normal(object sender, EventArgs e)
        {
            this.Invoke(() => { WindowState = WindowState.Normal; });
        }

        /// <summary>
        /// Launch third party utilities.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Launch_Third(object sender, RoutedEventArgs e)
        {
            try
            {
                string target       = "";
                string downloadlink = "";
                string installexe   = "";

                switch (((Button)sender).Name)
                {
                    case "Launch_WDP":
                        target = "\\WeaponDeliveryPlanner.exe";
                        downloadlink = "http://www.weapondeliveryplanner.nl/";
                        installexe = Properties.Settings.Default.Third_WDP + target;
                        if (File.Exists(installexe) == false)
                        {
                            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

                            fbd.Description = "Select Install Directory";
                            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                            fbd.ShowNewFolderButton = false;
                            System.Windows.Forms.DialogResult dirResult = fbd.ShowDialog();

                            installexe = fbd.SelectedPath + target;
                            if (File.Exists(installexe))
                                Properties.Settings.Default.Third_WDP = fbd.SelectedPath;
                            else
                            {
                                Utils.LaunchAppOrBrowserUrl(downloadlink);
                                return;
                            }
                        }
                        Utils.LaunchProcess(installexe);
                        break;
                    case "Launch_MC":
                        target = "\\Mission Commander.exe";
                        downloadlink = "http://www.weapondeliveryplanner.nl/";
                        installexe = Properties.Settings.Default.Third_MC + target;
                        if (File.Exists(installexe) == false)
                        {
                            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

                            fbd.Description = "Select Install Directory";
                            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                            fbd.ShowNewFolderButton = false;
                            System.Windows.Forms.DialogResult dirResult = fbd.ShowDialog();

                            installexe = fbd.SelectedPath + target;
                            if (File.Exists(installexe))
                                Properties.Settings.Default.Third_MC = fbd.SelectedPath;
                            else
                            {
                                Utils.LaunchAppOrBrowserUrl(downloadlink);
                                return;
                            }
                        }
                        Utils.LaunchProcess(installexe);
                        break;
                    case "Launch_WC":
                        target = "\\Weather Commander.exe";
                        downloadlink = "http://www.weapondeliveryplanner.nl/";
                        installexe = Properties.Settings.Default.Third_WC + target;
                        if (File.Exists(installexe) == false)
                        {
                            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

                            fbd.Description = "Select Install Directory";
                            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                            fbd.ShowNewFolderButton = false;
                            System.Windows.Forms.DialogResult dirResult = fbd.ShowDialog();

                            installexe = fbd.SelectedPath + target;
                            if (File.Exists(installexe))
                                Properties.Settings.Default.Third_WC = fbd.SelectedPath;
                            else
                            {
                                Utils.LaunchAppOrBrowserUrl(downloadlink);
                                return;
                            }
                        }
                        Utils.LaunchProcess(installexe);
                        break;
                    case "Launch_F4WX":
                        target = "\\F4Wx.exe";
                        downloadlink = "https://forum.falcon-bms.com/topic/8267/f4wx-real-weather-converter";
                        installexe = Properties.Settings.Default.Third_F4WX + target;
                        if (File.Exists(installexe) == false)
                        {
                            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog
                            {
                                Description = "Select Install Directory",
                                RootFolder = Environment.SpecialFolder.MyComputer,
                                ShowNewFolderButton = false
                            };

                            System.Windows.Forms.DialogResult dirResult = fbd.ShowDialog();

                            installexe = fbd.SelectedPath + target;
                            if (File.Exists(installexe))
                                Properties.Settings.Default.Third_F4WX = fbd.SelectedPath;
                            else
                            {
                                Utils.LaunchAppOrBrowserUrl(downloadlink);
                                return;
                            }
                        }
                        Utils.LaunchProcess(installexe);
                        break;
                    case "Launch_F4RADAR":
                        downloadlink = "https://forum.falcon-bms.com/topic/18356/f4radar-lightweight-standalone-radar-application";
                        installexe = Properties.Settings.Default.Third_F4RADAR;
                        if (File.Exists(installexe) == false)
                        {
                            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog
                            {
                                Description = "Select Install Directory",
                                RootFolder = Environment.SpecialFolder.MyComputer,
                                ShowNewFolderButton = false
                            };

                            System.Windows.Forms.DialogResult dirResult = fbd.ShowDialog();

                            installexe = fbd.SelectedPath + target;
                            if (File.Exists(installexe))
                                Properties.Settings.Default.Third_F4WX = fbd.SelectedPath;
                            else
                            {
                                Utils.LaunchAppOrBrowserUrl(downloadlink);
                                return;
                            }
                        }
                        Utils.LaunchProcess(installexe);
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        /// <summary>
        /// Change label color when mouse enters one of launcher icons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseEnterLauncher(object sender, EventArgs e)
        {
            try
            {
                string nme = ((Button)sender).Name;

                if (nme.Contains("Launch_"))
                {
                    if (nme.Contains("Launch_TheaterConfig"))
                        return;
                    Button tbButton = FindName(nme) as Button;
                    if (tbButton == null)
                        return;
                    tbButton.BorderBrush = CommonConstants.LIGHTBLUE;
                    tbButton.BorderThickness = new Thickness(1);

                    nme = nme.Replace("Launch_", "");
                    Label tblabel = FindName("Label_" + nme) as Label;
                    if (tblabel == null)
                        return;
                    tblabel.Foreground = CommonConstants.BLUEILUM;
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }

        /// <summary>
        /// Reset label color when mouse leaves a launcher icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseLeaveLauncher(object sender, EventArgs e)
        {
            try
            {
                string nme = ((Button)sender).Name;

                if (nme.Contains("Launch_"))
                {
                    if (nme.Contains("Launch_TheaterConfig"))
                        return;
                    Button tbButton = FindName(nme) as Button;
                    if (tbButton == null)
                        return;
                    tbButton.BorderThickness = new Thickness(0);

                    nme = nme.Replace("Launch_", "");
                    Label tblabel = FindName("Label_" + nme) as Label;
                    if (tblabel == null)
                        return;
                    tblabel.Foreground = CommonConstants.WHITEILUM;
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }
        
        /// <summary>
        /// Allow user to drag the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left &&
                e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void WDP_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Utils.LaunchAppOrBrowserUrl("http://www.weapondeliveryplanner.nl/");
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }

        private void Serfoss2003_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Utils.LaunchAppOrBrowserUrl("https://apps.dtic.mil/docs/citations/ADA414893");
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
            }
        }

        private void ListBox_BMS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!e.Source.Equals(ListBox_BMS))
                    return;
                if (appReg == null)
                    return;

                if (this.ListBox_BMS.SelectedIndex < 0)
                    return;

                var version = ListBox_BMS.SelectedValue as string; // real registry key, e.g. "Falcon BMS 4.38"
                if (string.IsNullOrEmpty(version)) return;

                Properties.Settings.Default.BMS_Version = version;

                appReg.UpdateSelectedBMSVersion(version);

                ReloadTheatersKeysJoysAndXml();
            }
            catch (Exception ex)
            {
                Diagnostics.Log(ex);
                Diagnostics.ShowErrorMsgbox(ex);
                Close();
            }
        }

        private void Misc_VR_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)VR_SteamVR.IsChecked)
                steamVR.Start();
            else
                steamVR.Stop();
        }

        private void ImportKeyfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult mbr = MessageBox.Show(this, 
                "WARNING -- selecting a new key file will erase and replace all keyboard " +
                "bindings, in the currently selected profile.\r\n\r\nProceed with caution!", 
                "Import Key File - WARNING", 
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (mbr != MessageBoxResult.OK) return;

            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.InitialDirectory = appReg.GetInstallDir() + CommonConstants.CONFIGFOLDERBACKSLASH;
            ofd.Filter = "Key files (*.key)|*.key|All files (*.*)|*.*";

            bool? ans2 = ofd.ShowDialog(this);
            if (ans2 != true) return;

            string newKeyfilePath = ofd.FileName;

            if (false == File.Exists(newKeyfilePath))
            {
                MessageBox.Show(this, "File not found: "+newKeyfilePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (false == KeyFile.ValidateKeyfileLines(newKeyfilePath))
            {
                MessageBox.Show(this,
                    "Key file contains one or more incorrectly formed lines -- please see error log at \n\n" +
                    "\"%LocalAppData%\\Benchmark_Sims\\Launcher_Log.txt\" \n\n" +
                    "for a complete list.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            deviceControl.ImportKeyfileIntoCurrentProfile(newKeyfilePath);
            UpdateCategoryHeaders();
            UpdateDataGridBindingSource();
            return;
        }

    }
}
