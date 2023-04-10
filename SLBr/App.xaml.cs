using CefSharp;
using CefSharp.BrowserSubprocess;
using CefSharp.DevTools.Database;
using CefSharp.SchemeHandler;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Handlers;
using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static SLBr.Controls.UrlScheme;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        #region Variables
        public static App Instance;
        public List<MainWindow> AllWindows = new List<MainWindow>();
        public IdnMapping _IdnMapping = new IdnMapping();

        public List<string> DefaultSearchEngines = new List<string>() {
            "https://www.ecosia.org/search?q={0}",
            "https://google.com/search?q={0}",
            "https://bing.com/search?q={0}",
            "https://search.brave.com/search?q={0}",
            /*"https://slsearch.cf/search?q={0}",
            "https://duckduckgo.com/?q={0}",
            "https://search.yahoo.com/search?p={0}",
            "https://yandex.com/search/?text={0}",*/
        };
        public List<string> SearchEngines = new List<string>();
        public List<Theme> Themes = new List<Theme>();


        public List<Saving> TabsSaves = new List<Saving>();

        public Saving GlobalSave;
        public Saving MainSave;
        public Saving FavouritesSave;
        //public Saving TabsSave;
        public Saving SearchSave;
        public Saving StatisticsSave;
        public Saving SandboxSave;
        public Saving ExperimentsSave;
        public Saving IESave;

        public string Username = "Default-User";
        string GlobalApplicationDataPath;
        string UserApplicationDataPath;
        public string UserApplicationWindowsPath;
        string CachePath;
        string UserDataPath;
        string LogPath;
        string ExecutablePath;

        

        public string ChromiumJSVersion;
        public string ChromiumRevision;
        public string EdgeJSVersion;
        public string EdgeRevision;

        public Random TinyRandom;
        public WebClient TinyDownloader;
        public LifeSpanHandler _LifeSpanHandler;
        public RequestHandler _RequestHandler;
        public DownloadHandler _DownloadHandler;
        public ContextMenuHandler _ContextMenuHandler;
        public KeyboardHandler _KeyboardHandler;
        public JsDialogHandler _JsDialogHandler;
        public PermissionHandler _PermissionHandler;
        public PrivateJsObjectHandler _PrivateJsObjectHandler;
        public PublicJsObjectHandler _PublicJsObjectHandler;
        public QRCodeHandler _QRCodeHandler;
        public SafeBrowsingHandler _SafeBrowsing;

        public int TrackersBlocked;
        public int AdsBlocked;
        string[] Args;
        public string ReleaseVersion;

        bool AppInitialized;
        string NewTabUrl = "";
        bool CreateTabForCommandLineUrl;
        int WPFFrameRate = 30;

        public bool DeveloperMode;
        public string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        private ObservableCollection<ActionStorage> PrivateFavourites = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> Favourites
        {
            get { return PrivateFavourites; }
            set
            {
                PrivateFavourites = value;
                RaisePropertyChanged("Favourites");
            }
        }
        private ObservableCollection<ActionStorage> PrivateHistory = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> History
        {
            get { return PrivateHistory; }
            set
            {
                PrivateHistory = value;
                RaisePropertyChanged("History");
            }
        }
        private ObservableCollection<ActionStorage> PrivateCompletedDownloads = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> CompletedDownloads
        {
            get { return PrivateCompletedDownloads; }
            set
            {
                PrivateCompletedDownloads = value;
                App.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    foreach (MainWindow _Window in AllWindows)
                    {
                        foreach (BrowserTabItem _Tab in _Window.Tabs)
                        {
                            Browser _Browser = ((Browser)_Tab.Content);
                            if (_Browser != null)
                                _Browser.OpenDownloadsButton.Visibility = Visibility.Visible;
                        }
                    }
                }));
            RaisePropertyChanged("CompletedDownloads");
            }
        }
        public void AddHistory(string Url)
        {
            List<string> Urls = History.Select(i => i.Name).ToList();
            if (Urls.Contains(Url))
                History.RemoveAt(Urls.IndexOf(Url));
            History.Insert(0, new ActionStorage(Url, $"3<,>{Url}", Utils.Host(Url)));
        }
        private Dictionary<int, DownloadItem> PrivateDownloads = new Dictionary<int, DownloadItem>();
        public Dictionary<int, DownloadItem> Downloads
        {
            get { return PrivateDownloads; }
            set
            {
                PrivateDownloads = value;
                RaisePropertyChanged("Downloads");
            }
        }
        public FastHashSet<int> CanceledDownloads = new FastHashSet<int>();
        public void UpdateDownloadItem(DownloadItem item)
        {
            Downloads[item.Id] = item;
            Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (item.IsComplete)
                {
                    //TaskbarProgress.ProgressValue = 0;
                    CompletedDownloads.Add(new ActionStorage(Path.GetFileName(item.FullPath), "3<,>slbr://downloads/", ""));
                }
                //else
                //    TaskbarProgress.ProgressValue = (double)item.PercentComplete / 100.0;

                foreach (MainWindow _Window in AllWindows)
                {
                    _Window.TaskbarProgress.ProgressValue = item.IsComplete ? 0 : (double)item.PercentComplete / 100.0;
                }
            }));
        }
        HashSet<string> HardwareUnavailableProcessors = new HashSet<string>
        {
            "Intel(R) Iris(R) Xe Graphics",
            "Intel Iris Xe Integrated GPU",//Intel Iris Xe Integrated GPU(11th Gen)
            "Intel(R) Core(TM) i5"
        };
        #endregion

        public void AdBlock(bool Boolean)
        {
            MainSave.Set("AdBlock", Boolean.ToString());
            _RequestHandler.AdBlock = Boolean;
        }
        public void TrackerBlock(bool Boolean)
        {
            MainSave.Set("TrackerBlock", Boolean.ToString());
            _RequestHandler.TrackerBlock = Boolean;
        }
        public void SetRenderMode(string Mode, bool Notify)
        {
            if (Mode == "Hardware")
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
            else if (Mode == "Software")
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            MainSave.Set("RenderMode", Mode);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Instance = this;
            Args = Environment.GetCommandLineArgs();
            //MessageBox.Show(string.Join(",", Args));
            if (Args.Length > 0 && Args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                SelfHost.Main(Args);
                return;
            }
            InitializeApp();
        }
        private async void InitializeApp()
        {
            new SplashScreen();
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = WPFFrameRate });
            SplashScreen.Instance.ReportProgress(0, "Processing...");

            if (Args.Length > 1)
            {
                foreach (string Flag in Args)
                {
                    SplashScreen.Instance.ReportProgress(1, "Processing command line arguments...");
                    if (Args.ToList().IndexOf(Flag) == 0)
                        continue;
                    if (Flag == "--dev")
                        DeveloperMode = true;
                    else if (Flag.StartsWith("--user="))
                        Username = Flag.Replace("--user=", "").Replace(" ", "-");
                    else if (Flag.StartsWith("--chromium-flags="))
                        continue;
                    else
                    {
                        if (Flag.StartsWith("--"))
                            continue;
                        //if (Directory.Exists(Flag) || File.Exists(Flag))
                        //    NewTabUrl = "file:\\\\\\" + Args[1];
                        //else
                        NewTabUrl = Flag;
                        //MessageBox.Show(Flag);
                        CreateTabForCommandLineUrl = true;
                    }
                }
                if (Username == "Default-User")
                {
                    Process _otherInstance = Utils.GetAlreadyRunningInstance();
                    if (_otherInstance != null)
                    {
                        MessageHelper.SendDataMessage(_otherInstance, Args[1]);
                        //ShutdownMode = ShutdownMode.OnLastWindowClose;
                        Shutdown(1);
                        return;
                    }
                }
            }
            if (!DeveloperMode)
                DeveloperMode = Debugger.IsAttached;
            Current.DispatcherUnhandledException += Window_DispatcherUnhandledException;
            GlobalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            CachePath = Path.Combine(UserApplicationDataPath, "Cache");
            UserDataPath = Path.Combine(UserApplicationDataPath, "User Data");
            LogPath = Path.Combine(UserApplicationDataPath, "Errors.log");
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            SplashScreen.Instance.ReportProgress(2, "Registering client API keys...");
            // Set Google API keys, used for Geolocation requests sans GPS. See http://www.chromium.org/developers/how-tos/api-keys
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            //create cs file named SECRETS, add string variable GOOGLE_API_KEY with value of google api key
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            //add string variable GOOGLE_DEFAULT_CLIENT_ID with value of google client id
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);
            //add string variable GOOGLE_DEFAULT_CLIENT_SECRET with value of google client secret

            //if (Utils.IsAdministrator())
            {
                SplashScreen.Instance.ReportProgress(3, "Registering application into registry...");
                try
                {
                    using (var checkkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))//LocalMachine
                    {
                        if (checkkey.GetValue("SLBr") == null)
                        {
                            using (var key = Registry.ClassesRoot.CreateSubKey("SLBr", true))
                            {
                                key.SetValue(null, "SLBr Document");
                                key.SetValue("AppUserModelId", "SLBr");

                                RegistryKey ApplicationRegistry = key.CreateSubKey("Application", true);
                                ApplicationRegistry.SetValue("AppUserModelId", "SLBr");
                                ApplicationRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                                ApplicationRegistry.SetValue("ApplicationName", "SLBr");
                                ApplicationRegistry.SetValue("ApplicationCompany", "SLT World");
                                ApplicationRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                                ApplicationRegistry.Close();

                                RegistryKey IconRegistry = key.CreateSubKey("DefaultIcon", true);
                                IconRegistry.SetValue(null, $"{ExecutablePath},0");
                                ApplicationRegistry.Close();

                                RegistryKey CommandRegistry = key.CreateSubKey("shell\\open\\command", true);
                                CommandRegistry.SetValue(null, $"\"{ExecutablePath}\" \"%1\"");
                                CommandRegistry.Close();
                            }
                            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Clients\\StartMenuInternet", true).CreateSubKey("SLBr", true))//LocalMachine
                            {
                                if (key.GetValue(null) as string != "SLBr")
                                    key.SetValue(null, "SLBr");

                                RegistryKey CapabilitiesRegistry = key.CreateSubKey("Capabilities", true);
                                CapabilitiesRegistry.SetValue("ApplicationDescription", "SLBr is a open source browser that prioritizes a faster web");
                                CapabilitiesRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                                CapabilitiesRegistry.SetValue("ApplicationName", $"SLBr");
                                RegistryKey StartMenuRegistry = CapabilitiesRegistry.CreateSubKey("StartMenu", true);
                                StartMenuRegistry.SetValue("StartMenuInternet", "SLBr");
                                StartMenuRegistry.Close();

                                RegistryKey FileAssociationsRegistry = CapabilitiesRegistry.CreateSubKey("FileAssociations", true);
                                FileAssociationsRegistry.SetValue(".xhtml", "SLBr");
                                FileAssociationsRegistry.SetValue(".xht", "SLBr");
                                FileAssociationsRegistry.SetValue(".shtml", "SLBr");
                                FileAssociationsRegistry.SetValue(".html", "SLBr");
                                FileAssociationsRegistry.SetValue(".htm", "SLBr");
                                FileAssociationsRegistry.SetValue(".pdf", "SLBr");
                                FileAssociationsRegistry.SetValue(".svg", "SLBr");
                                FileAssociationsRegistry.SetValue(".webp", "SLBr");
                                FileAssociationsRegistry.Close();

                                RegistryKey URLAssociationsRegistry = CapabilitiesRegistry.CreateSubKey("URLAssociations", true);
                                URLAssociationsRegistry.SetValue("http", "SLBr");
                                URLAssociationsRegistry.SetValue("https", "SLBr");
                                URLAssociationsRegistry.Close();

                                CapabilitiesRegistry.Close();

                                RegistryKey DefaultIconRegistry = key.CreateSubKey("DefaultIcon", true);
                                DefaultIconRegistry.SetValue(null, $"{ExecutablePath},0");
                                DefaultIconRegistry.Close();

                                RegistryKey CommandRegistry = key.CreateSubKey("shell\\open\\command", true);
                                CommandRegistry.SetValue(null, $"\"{ExecutablePath}\" \"%1\"");
                                CommandRegistry.Close();
                            }
                            //using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))//LocalMachine
                            //{
                            checkkey.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                            //}
                        }
                    }
                }
                catch
                {
                    SplashScreen.Instance.ReportProgress(4, "Failed.");
                }
                //Process regeditProcess = Process.Start("regedit.exe", "/s \"" + RegPath + "\"");
                //regeditProcess.WaitForExit();
            }

            TinyRandom = new Random();
            TinyDownloader = new WebClient();
            SplashScreen.Instance.ReportProgress(87, "Initializing components...");
            InitializeSaves();
            InitializeCEF();
            InitializeEdge();
            InitializeIE();

            await Task.Delay(50);
            SplashScreen.Instance.ReportProgress(87, "Initializing UI components...");
            InitializeUISaves();

            SplashScreen.Instance.ReportProgress(99, "Complete.");
            SplashScreen.Instance.ReportProgress(100, "Showing window...");
            SwitchTabAlignment(MainSave.Get("TabAlignment"));
            AppInitialized = true;
        }

        public void DiscordWebhookSendInfo(string Content)
        {
            try { TinyDownloader.UploadValues(SECRETS.DISCORD_WEBHOOK, new NameValueCollection{ { "content", Content }, { "username", "SLBr Diagnostics" } }); }
            catch { }
        }
        private void Window_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (bool.Parse(MainSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo($"[SLBr] {ReleaseVersion}\n" +
                    $"[Message] {e.Exception.Message}\n" +
                    $"[Source] {e.Exception.Source}\n" +
                    $"[Target Site] {e.Exception.TargetSite}\n" +
                    $"[Stack Trace] ```{e.Exception.StackTrace}```\n" +
                    $"[Inner Exception] ```{e.Exception.InnerException}```"
                    );
            MessageBox.Show(e.Exception.Message + "|" + e.Exception.Source + "|" + e.Exception.TargetSite + "|" + e.Exception.StackTrace + "|" + e.Exception.InnerException);
            //if (!DeveloperMode)
            //    CloseSLBr();
        }

        public int TabUnloadingTime;
        public void SetTabUnloadingTime(int Time)
        {
            //MessageBox.Show(Time.ToString());
            TabUnloadingTime = Time * 30;
            foreach (MainWindow _Window in AllWindows)
                _Window.UnloadTabsTimeIncrement = 0;
            MainSave.Set("TabUnloadingTime", Time);
        }
        public void SetSandbox(int _Framerate, CefState JSState, CefState LIState, CefState LSState, CefState DBState, CefState WebGLState)
        {
            Framerate = _Framerate;
            Javascript = JSState;
            LoadImages = LIState;
            LocalStorage = LSState;
            Databases = DBState;
            WebGL = WebGLState;
            SandboxSave.Set("Framerate", _Framerate.ToString());
            SandboxSave.Set("JS", JSState.ToBoolean().ToString());//webkit.webprefs.javascript_enabled
            SandboxSave.Set("LI", LIState.ToBoolean().ToString());
            SandboxSave.Set("LS", LSState.ToBoolean().ToString());
            SandboxSave.Set("DB", DBState.ToBoolean().ToString());
            SandboxSave.Set("WebGL", WebGLState.ToBoolean().ToString());
            foreach (MainWindow _Window in AllWindows)
                _Window.SetSandbox(_Framerate, JSState, LIState, LSState, DBState, WebGLState);
        }

        public Theme GetTheme(string Name)
        {
            if (string.IsNullOrEmpty(Name))
            {
                if (CurrentTheme != null)
                    return CurrentTheme;
                else
                    Name = MainSave.Get("Theme");
            }
            foreach (Theme _Theme in Themes)
            {
                if (_Theme.Name == Name)
                    return _Theme;
            }
            return Themes[0];
        }
        public Theme CurrentTheme;

        public int Framerate;
        public CefState Javascript = CefState.Enabled;
        public CefState LoadImages = CefState.Enabled;
        public CefState LocalStorage = CefState.Enabled;
        public CefState Databases = CefState.Enabled;
        public CefState WebGL = CefState.Enabled;

        public void NewWindow()
        {
            MainWindow _Window = new MainWindow();
            _Window.Show();
            _Window.NewBrowserTab(MainSave.Get("Homepage"), int.Parse(MainSave.Get("DefaultBrowserEngine")), true);
        }

        public void SetDimIconsWhenUnloaded(bool Toggle)
        {
            foreach (MainWindow _Window in AllWindows)
                _Window.SetDimIconsWhenUnloaded(Toggle);
            MainSave.Set("DimIconsWhenUnloaded", true);
        }
        public void SwitchTabAlignment(string NewAlignment)
        {
            foreach (MainWindow _Window in AllWindows)
                _Window.SwitchTabAlignment(NewAlignment);
            MainSave.Set("TabAlignment", NewAlignment);
        }
        #region Initialize
        private void SetIEEmulation(uint Value = 11001)
        {
            //SplashScreen.Instance.ReportProgress(16, "Modifying IE Emulation...");
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
                {
                    if (key.GetValue("SLBr.exe") == null)
                        key.SetValue("SLBr.exe", Value, RegistryValueKind.DWord);
                }
            }
            catch
            {
                //SplashScreen.Instance.ReportProgress(17, "Failed.");
            }
        }
        private void SetFeatureControlKey(string Feature, uint Value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(string.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", Feature), RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue("SLBr.exe", Value, RegistryValueKind.DWord);
            }
        }
        private void InitializeIE()
        {
            //SplashScreen.Instance.ReportProgress(15, "Initializing Internet Explorer features...");
            SetIEEmulation();
            /*SetFeatureControlKey("FEATURE_BROWSER_EMULATION", 11001); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
            SetFeatureControlKey("FEATURE_GPU_RENDERING", 1);
            SetFeatureControlKey("FEATURE_DISABLE_LEGACY_COMPRESSION", 0);
            SetFeatureControlKey("FEATURE_ALLOW_HIGHFREQ_TIMERS", 1);
            SetFeatureControlKey("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", 1);

            SetFeatureControlKey("FEATURE_DOMSTORAGE", 1);
            SetFeatureControlKey("FEATURE_WEBSOCKET", 1);
            SetFeatureControlKey("FEATURE_XMLHTTP", 1);

            SetFeatureControlKey("FEATURE_AJAX_CONNECTIONEVENTS", 1);
            SetFeatureControlKey("FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS", 1);
            SetFeatureControlKey("FEATURE_IVIEWOBJECTDRAW_DMLT9_WITH_GDI  ", 0);
            SetFeatureControlKey("FEATURE_LOCALMACHINE_LOCKDOWN", 0);
            SetFeatureControlKey("FEATURE_BLOCK_LMZ_IMG", 1);
            SetFeatureControlKey("FEATURE_BLOCK_LMZ_OBJECT", 1);
            SetFeatureControlKey("FEATURE_BLOCK_LMZ_SCRIPT", 1);
            SetFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", 1);
            SetFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", 1);
            SetFeatureControlKey("FEATURE_SPELLCHECKING", 1);
            SetFeatureControlKey("FEATURE_STATUS_BAR_THROTTLING", 1);
            SetFeatureControlKey("FEATURE_TABBED_BROWSING", 1);
            SetFeatureControlKey("FEATURE_VALIDATE_NAVIGATE_URL", 1);
            SetFeatureControlKey("FEATURE_WEBOC_DOCUMENT_ZOOM", 1);
            SetFeatureControlKey("FEATURE_WEBOC_POPUPMANAGEMENT", 0);
            SetFeatureControlKey("FEATURE_WEBOC_MOVESIZECHILD", 1);
            SetFeatureControlKey("FEATURE_ADDON_MANAGEMENT", 0);
            SetFeatureControlKey("FEATURE_WINDOW_RESTRICTIONS ", 0);*/
        }
        private void InitializeSaves()
        {
            //SplashScreen.Instance.ReportProgress(29, "Fetching data...");
            GlobalSave = new Saving("GlobalSave.bin", GlobalApplicationDataPath);
            MainSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            //TabsSave = new Saving("Tabs.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            SandboxSave = new Saving("Sandbox.bin", UserApplicationDataPath);
            ExperimentsSave = new Saving("Experiments.bin", UserApplicationDataPath);
            IESave = new Saving("InternetExplorer.bin", UserApplicationDataPath);

            if (!Directory.Exists(UserApplicationWindowsPath))
                Directory.CreateDirectory(UserApplicationWindowsPath);
            int WindowsSavesCount = Directory.EnumerateFiles(UserApplicationWindowsPath).Count();
            if (WindowsSavesCount != 0)
            {
                for (int i = 0; i < WindowsSavesCount; i++)
                    TabsSaves.Add(new Saving($"Window_{i}_Tabs.bin", UserApplicationWindowsPath));
            }
            else
                TabsSaves.Add(new Saving($"Window_0_Tabs.bin", UserApplicationWindowsPath));

            //SplashScreen.Instance.ReportProgress(30, "Processing data...");
            if (SearchSave.Has("Search_Engine_Count"))
            {
                if (int.Parse(SearchSave.Get("Search_Engine_Count")) == 0)
                    SearchEngines = new List<string>(DefaultSearchEngines);
                else
                {
                    for (int i = 0; i < int.Parse(SearchSave.Get("Search_Engine_Count")); i++)
                    {
                        string Url = SearchSave.Get($"Search_Engine_{i}");
                        if (!SearchEngines.Contains(Url))
                            SearchEngines.Add(Url);
                    }
                }
            }
            else
                SearchEngines = new List<string>(DefaultSearchEngines);
            if (!MainSave.Has("FullAddress"))
                MainSave.Set("FullAddress", false);
            if (!MainSave.Has("MobileWikipedia"))
                MainSave.Set("MobileWikipedia", false);
            if (!MainSave.Has("SpellCheck"))
                MainSave.Set("SpellCheck", true);
            if (!MainSave.Has("Search_Engine"))
                MainSave.Set("Search_Engine", DefaultSearchEngines[0]);

            if (!MainSave.Has("Homepage"))
                MainSave.Set("Homepage", "slbr://newtab");
            if (!MainSave.Has("Theme"))
                MainSave.Set("Theme", "Auto");
            if (!StatisticsSave.Has("BlockedTrackers"))
                StatisticsSave.Set("BlockedTrackers", "0");
            TrackersBlocked = int.Parse(StatisticsSave.Get("BlockedTrackers"));
            if (!StatisticsSave.Has("BlockedAds"))
                StatisticsSave.Set("BlockedAds", "0");
            AdsBlocked = int.Parse(StatisticsSave.Get("BlockedAds"));
            if (!ExperimentsSave.Has("RedirectAJAXToCDNJS"))
                ExperimentsSave.Set("RedirectAJAXToCDNJS", false.ToString());

            if (!MainSave.Has("TabUnloading"))
                MainSave.Set("TabUnloading", true.ToString());
            if (!MainSave.Has("TabUnloadingTime"))
                SetTabUnloadingTime(15);
            else
                SetTabUnloadingTime(int.Parse(MainSave.Get("TabUnloadingTime")));
            if (!MainSave.Has("IPFS"))
                MainSave.Set("IPFS", true.ToString());
            if (!MainSave.Has("Wayback"))
                MainSave.Set("Wayback", true.ToString());
            if (!MainSave.Has("Gemini"))
                MainSave.Set("Gemini", true.ToString());
            if (!MainSave.Has("Gopher"))
                MainSave.Set("Gopher", true.ToString());
            if (!MainSave.Has("DownloadPrompt"))
                MainSave.Set("DownloadPrompt", true.ToString());
            if (!MainSave.Has("DownloadPath"))
                MainSave.Set("DownloadPath", Utils.GetFolderPath(Utils.FolderGuids.Downloads));
            if (!MainSave.Has("ScreenshotPath"))
                MainSave.Set("ScreenshotPath", Path.Combine(Utils.GetFolderPath(Utils.FolderGuids.Pictures), "Screenshots", "SLBr"));

            if (!MainSave.Has("SendDiagnostics"))
                MainSave.Set("SendDiagnostics", true);
            if (!MainSave.Has("WebNotifications"))
                MainSave.Set("WebNotifications", true);

            if (!MainSave.Has("CoverTaskbarOnFullscreen"))
                MainSave.Set("CoverTaskbarOnFullscreen", true);

            if (!MainSave.Has("ScreenshotFormat"))
                MainSave.Set("ScreenshotFormat", "Jpeg");

            if (!IESave.Has("IESuppressErrors"))
                IESave.Set("IESuppressErrors", true);

            if (!MainSave.Has("RestoreTabs"))
                MainSave.Set("RestoreTabs", true);

            if (!SandboxSave.Has("Framerate"))
                SandboxSave.Set("Framerate", "60");
            Framerate = int.Parse(SandboxSave.Get("Framerate"));
            if (!SandboxSave.Has("JS"))
                SandboxSave.Set("JS", true.ToString());
            Javascript = bool.Parse(SandboxSave.Get("JS")).ToCefState();
            if (!SandboxSave.Has("LI"))
                SandboxSave.Set("LI", true.ToString());
            LoadImages = bool.Parse(SandboxSave.Get("LI")).ToCefState();
            if (!SandboxSave.Has("LS"))
                SandboxSave.Set("LS", true.ToString());
            LocalStorage = bool.Parse(SandboxSave.Get("LS")).ToCefState();
            if (!SandboxSave.Has("DB"))
                SandboxSave.Set("DB", true.ToString());
            Databases = bool.Parse(SandboxSave.Get("DB")).ToCefState();
            if (!SandboxSave.Has("WebGL"))
                SandboxSave.Set("WebGL", true.ToString());
            WebGL = bool.Parse(SandboxSave.Get("WebGL")).ToCefState();

            if (!ExperimentsSave.Has("DeveloperMode"))
                ExperimentsSave.Set("DeveloperMode", false);
            //int renderingTier = RenderCapability.Tier >> 16;
            if (!ExperimentsSave.Has("ChromiumHardwareAcceleration"))
                ExperimentsSave.Set("ChromiumHardwareAcceleration", true);//renderingTier == 0
            if (!ExperimentsSave.Has("ChromeRuntime"))
                ExperimentsSave.Set("ChromeRuntime", false);
            if (!ExperimentsSave.Has("LowEndDeviceMode"))
                ExperimentsSave.Set("LowEndDeviceMode", true);
            if (!ExperimentsSave.Has("PDFViewerExtension"))
                ExperimentsSave.Set("PDFViewerExtension", true);
            if (!ExperimentsSave.Has("AutoplayUserGestureRequired"))
                ExperimentsSave.Set("AutoplayUserGestureRequired", true);
            if (!ExperimentsSave.Has("SmoothScrolling"))
                ExperimentsSave.Set("SmoothScrolling", true);
            if (!ExperimentsSave.Has("WebAssembly"))
                ExperimentsSave.Set("WebAssembly", true);
            if (!ExperimentsSave.Has("V8LiteMode"))
                ExperimentsSave.Set("V8LiteMode", false);
            if (!ExperimentsSave.Has("V8Sparkplug"))
                ExperimentsSave.Set("V8Sparkplug", false);

            if (!MainSave.Has("SearchSuggestions"))
                MainSave.Set("SearchSuggestions", true);
            if (!MainSave.Has("DarkWebPage"))
                MainSave.Set("DarkWebPage", true);
            if (!MainSave.Has("DoNotTrack"))
                MainSave.Set("DoNotTrack", false);

            if (!MainSave.Has("AngleGraphicsBackend"))
                MainSave.Set("AngleGraphicsBackend", "Default");
            if (!MainSave.Has("MSAASampleCount"))
                MainSave.Set("MSAASampleCount", 2);
            if (!MainSave.Has("RendererProcessLimit"))
                MainSave.Set("RendererProcessLimit", 2);
            if (!MainSave.Has("SiteIsolation"))
                MainSave.Set("SiteIsolation", true);
            if (!MainSave.Has("SkipLowPriorityTasks"))
                MainSave.Set("SkipLowPriorityTasks", true);
            if (!MainSave.Has("PrintRaster"))
                MainSave.Set("PrintRaster", true);
            if (!MainSave.Has("Prerender"))
                MainSave.Set("Prerender", true);
            if (!MainSave.Has("SpeculativePreconnect"))
                MainSave.Set("SpeculativePreconnect", true);
            if (!MainSave.Has("PrefetchDNS"))
                MainSave.Set("PrefetchDNS", true);

            if (!MainSave.Has("BackgroundImage"))
                MainSave.Set("BackgroundImage", "Lorem Picsum");
            if (!MainSave.Has("CustomBackgroundQuery"))
                MainSave.Set("CustomBackgroundQuery", "");
            if (!MainSave.Has("CustomBackgroundImage"))
                MainSave.Set("CustomBackgroundImage", "");

            if (!MainSave.Has("DefaultBrowserEngine"))
                MainSave.Set("DefaultBrowserEngine", 0);

            if (bool.Parse(ExperimentsSave.Get("DeveloperMode")))
                DeveloperMode = true;

            Themes.Add(new Theme("Light", Colors.White, Colors.Black, Colors.Gainsboro, Colors.WhiteSmoke, Colors.Gray));
            Themes.Add(new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), Colors.White, (Color)ColorConverter.ConvertFromString("#36393F"), (Color)ColorConverter.ConvertFromString("#2F3136"), Colors.Gainsboro, true, true));
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                {
                    //MessageBox.Show((key.GetValue("SystemUsesLightTheme") as int? == 1).ToString());
                    //if (key.GetValue("AppsUseLightTheme") as uint? == 1)
                    Themes.Add(new Theme("Auto", (key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
                }
            }
            catch
            {
                if (MainSave.Get("Theme") == "Auto")
                    MainSave.Set("Theme", "Dark");
            }
            ApplyTheme(GetTheme(MainSave.Get("Theme")));
            //SplashScreen.Instance.ReportProgress(31, "Done.");
        }
        private void InitializeUISaves()
        {
            //SplashScreen.Instance.ReportProgress(85, "Processing data...");
            if (!MainSave.Has("TabAlignment"))
                SwitchTabAlignment("Horizontal");
            else
                SwitchTabAlignment(MainSave.Get("TabAlignment"));
            if (!MainSave.Has("DimIconsWhenUnloaded"))
                SetDimIconsWhenUnloaded(true);
            if (!MainSave.Has("ShowUnloadedIcon"))
                MainSave.Set("ShowUnloadedIcon", false);

            if (!MainSave.Has("AdBlock"))
                AdBlock(true);
            else
                AdBlock(bool.Parse(MainSave.Get("AdBlock")));
            if (!MainSave.Has("TrackerBlock"))
                TrackerBlock(true);
            else
                TrackerBlock(bool.Parse(MainSave.Get("TrackerBlock")));

            if (!MainSave.Has("RenderMode"))
            {
                string _RenderMode = "Hardware";
                var ProcessorID = Utils.GetProcessorID();
                foreach (string Processor in HardwareUnavailableProcessors)
                {
                    if (ProcessorID.Contains(Processor))
                        _RenderMode = "Software";
                }
                if (_RenderMode != "Software")
                {
                    int renderingTier = RenderCapability.Tier >> 16;
                    _RenderMode = renderingTier == 0 ? "Software" : _RenderMode;
                }
                SetRenderMode(_RenderMode, false);
            }
            else
                SetRenderMode(MainSave.Get("RenderMode"), true);
            //SplashScreen.Instance.ReportProgress(81, "Initializing UI...");
            if (FavouritesSave.Has("Favourite_Count"))
            {
                Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(FavouritesSave.Get("Favourite_Count")); i++)
                    {
                        string[] Value = FavouritesSave.Get($"Favourite_{i}", true);
                        Favourites.Add(new ActionStorage(Value[1], $"3<,>{Value[0]}", Value[0]));
                    }
                }));
            }
            Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (bool.Parse(MainSave.Get("RestoreTabs")))
                {
                    foreach (Saving TabsSave in TabsSaves)
                    {
                        if (TabsSave.Has("Tab_Count") && int.Parse(TabsSave.Get("Tab_Count")) > 0)
                        {
                            MainWindow _Window = new MainWindow();
                            if (!TabsSave.Has("SelectedTabIndex"))
                                TabsSave.Set("SelectedTabIndex", 0);
                            int SelectedIndex = int.Parse(TabsSave.Get("SelectedTabIndex"));
                            for (int i = 0; i < int.Parse(TabsSave.Get("Tab_Count")); i++)
                            {
                                string Url = TabsSave.Get($"Tab_{i}");
                                if (Url != "NOTFOUND")
                                {
                                    if (Url == "slbr://settings")
                                        _Window.OpenSettings(false);
                                    else
                                        _Window.NewBrowserTab(Url.Replace("slbr://processcrashed?s=", "").Replace("slbr://processcrashed/?s=", ""), int.Parse(MainSave.Get("DefaultBrowserEngine")));
                                }
                            }
                            _Window.BrowserTabs.SelectedIndex = SelectedIndex;
                            _Window.Show();
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(NewTabUrl))
                                NewTabUrl = MainSave.Get("Homepage");
                            CreateTabForCommandLineUrl = true;
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(NewTabUrl))
                        NewTabUrl = MainSave.Get("Homepage");
                    CreateTabForCommandLineUrl = true;
                }
                if (AllWindows.Count == 0)
                    new MainWindow().Show();
                if (!MainSave.Has("UsedBefore"))
                {
                    if (string.IsNullOrEmpty(NewTabUrl) || NewTabUrl == MainSave.Get("Homepage"))
                    {
                        NewTabUrl = "https://github.com/SLT-World/SLBr";
                        CreateTabForCommandLineUrl = true;
                    }
                    MainSave.Set("UsedBefore", true.ToString());
                }
                if (CreateTabForCommandLineUrl)
                    AllWindows[0].NewBrowserTab(NewTabUrl, int.Parse(MainSave.Get("DefaultBrowserEngine")), true);
            }));
            //SplashScreen.Instance.ReportProgress(86, "Done.");
        }

        public MainWindow CurrentFocusedWindow()
        {
            foreach (MainWindow _Window in AllWindows)
            {
                if (_Window.IsActive || _Window.IsFocused || _Window.WindowState == WindowState.Maximized || _Window.WindowState == WindowState.Normal) return _Window;
            }
            return null;
        }

        public void Refresh()
        {
            CurrentFocusedWindow().Refresh();
        }
        public void Fullscreen()
        {
            CurrentFocusedWindow().Fullscreen(CurrentFocusedWindow().IsFullscreen);
        }
        public void Inspect(string Id = "")
        {
            CurrentFocusedWindow().Inspect(Id);
        }
        public void FindUI()
        {
            CurrentFocusedWindow().FindUI();
        }
        public void Screenshot()
        {
            CurrentFocusedWindow().Screenshot();
        }
        private void InitializeCEF()
        {
            //SplashScreen.Instance.ReportProgress(43, "Initializing LifeSpan Handler...");
            _LifeSpanHandler = new LifeSpanHandler();
            //SplashScreen.Instance.ReportProgress(44, "Initializing Download Handler...");
            _DownloadHandler = new DownloadHandler();
            //SplashScreen.Instance.ReportProgress(44, "Initializing Request Handler...");
            _RequestHandler = new RequestHandler();
            //SplashScreen.Instance.ReportProgress(46, "Initializing Menu Handler...");
            _ContextMenuHandler = new ContextMenuHandler();
            //SplashScreen.Instance.ReportProgress(47, "Initializing Keyboard Handler...");
            _KeyboardHandler = new KeyboardHandler();
            //SplashScreen.Instance.ReportProgress(48, "Initializing Javascript Dialog Handler...");
            _JsDialogHandler = new JsDialogHandler();
            //SplashScreen.Instance.ReportProgress(49, "Initializing private Javascript Handler...");
            _PrivateJsObjectHandler = new PrivateJsObjectHandler();
            //SplashScreen.Instance.ReportProgress(50, "Initializing public Javascript Handler...");
            _PublicJsObjectHandler = new PublicJsObjectHandler();
            //SplashScreen.Instance.ReportProgress(51, "Initializing QR Code Handler.");
            _PermissionHandler = new PermissionHandler();
            //SplashScreen.Instance.ReportProgress(51, "Initializing QR Code Handler.");
            _QRCodeHandler = new QRCodeHandler();
            //SplashScreen.Instance.ReportProgress(52, "Done.");

            //SplashScreen.Instance.ReportProgress(54, "Applying keyboard shortcuts...");
            _KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(delegate () { Refresh(); }, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(); }, (int)Key.F11);
            _KeyboardHandler.AddKey(delegate () { Inspect(); }, (int)Key.F12);
            _KeyboardHandler.AddKey(FindUI, (int)Key.F, true);
            //SplashScreen.Instance.ReportProgress(55, "Done.");

            //SplashScreen.Instance.ReportProgress(66, "Initializing SafeBrowsing API...");
            _SafeBrowsing = new SafeBrowsingHandler(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"), Environment.GetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID"));
            //SplashScreen.Instance.ReportProgress(67, "Done.");

            //SplashScreen.Instance.ReportProgress(68, "Processing...");
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            CefSettings settings = new CefSettings();
            //settings.WindowlessRenderingEnabled = false;
            //settings.EnablePrintPreview();

            SetCEFFlags(settings);

            //if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Utility Service.exe")))
            //settings.BrowserSubprocessPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Utility Service.exe");
            //settings.BrowserSubprocessPath = "E:\\Visual Studio\\SLBr\\Utility Service\\bin\\Debug\\net6.0-windows\\Utility Service.exe";
            //else
            settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;
            //using (var currentProcess = Process.GetCurrentProcess())
            //settings.BrowserSubprocessPath = currentProcess.MainModule.FileName;

            //settings.BrowserSubprocessPath = Args[0];
            if (bool.Parse(ExperimentsSave.Get("ChromeRuntime")))
                settings.ChromeRuntime = true;
            //settings.CefCommandLineArgs.Add("enable-chrome-runtime");

            Cef.EnableHighDPISupport();

            settings.Locale = "en-US";
            settings.MultiThreadedMessageLoop = true;
            settings.CommandLineArgsDisabled = true;
            settings.UserAgentProduct = $"SLBr/{ReleaseVersion} Chrome/{Cef.ChromiumVersion}";
            settings.LogFile = LogPath;
            settings.LogSeverity = LogSeverity.Error;
            settings.CachePath = CachePath;
            settings.RemoteDebuggingPort = 8089;
            settings.UserDataPath = UserDataPath;

            //SplashScreen.Instance.ReportProgress(81, "Registering network protocols...");
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gemini",
                SchemeHandlerFactory = new GeminiSchemeHandlerFactory()
            });
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gopher",
                SchemeHandlerFactory = new GopherSchemeHandlerFactory()
            });
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "wayback",
                SchemeHandlerFactory = new WaybackSchemeHandlerFactory()
            });
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipfs",
                SchemeHandlerFactory = new IPFSSchemeHandlerFactory()
            });
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipns",
                SchemeHandlerFactory = new IPNSSchemeHandlerFactory()
            });
            UrlScheme SLBrScheme = new UrlScheme
            {
                Name = "slbr",
                IsStandard = true,
                IsLocal = true,
                IsSecure = true,
                IsCorsEnabled = true,
                Schemes = new List<Scheme> {
                    new Scheme { PageName = "Credits", FileName = "Credits.html" },
                    new Scheme { PageName = "License", FileName = "License.html" },
                    new Scheme { PageName = "NewTab", FileName = "NewTab.html" },
                    new Scheme { PageName = "Downloads", FileName = "Downloads.html" },
                    new Scheme { PageName = "History", FileName = "History.html" },

                    new Scheme { PageName = "Malware", FileName = "Malware.html" },
                    new Scheme { PageName = "Deception", FileName = "Deception.html" },
                    new Scheme { PageName = "ProcessCrashed", FileName = "ProcessCrashed.html" },
                    new Scheme { PageName = "CannotConnect", FileName = "CannotConnect.html" },
                    new Scheme { PageName = "Tetris", FileName = "Tetris.html" },
                }
            };
            string SLBrSchemeRootFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SLBrScheme.RootFolder);
            foreach (var Scheme in SLBrScheme.Schemes)
            {
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = SLBrScheme.Name,
                    DomainName = Scheme.PageName.ToLower(),
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                        rootFolder: SLBrSchemeRootFolder,
                        hostName: Scheme.PageName,
                        defaultPage: Scheme.FileName
                    ),
                    IsSecure = SLBrScheme.IsSecure,
                    IsLocal = SLBrScheme.IsLocal,
                    IsStandard = SLBrScheme.IsStandard,
                    IsCorsEnabled = SLBrScheme.IsCorsEnabled
                });
            }
            //SplashScreen.Instance.ReportProgress(82, "Done.");

            //SplashScreen.Instance.ReportProgress(83, "Initializing Chromium...");
            Cef.Initialize(settings);
            //SplashScreen.Instance.ReportProgress(84, "Chromium initialized.");

            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();

                string errorMessage;
                GlobalRequestContext.SetPreference("enable_do_not_track", bool.Parse(MainSave.Get("DoNotTrack")), out errorMessage);
                GlobalRequestContext.SetPreference("browser.enable_spellchecking", bool.Parse(MainSave.Get("SpellCheck")), out errorMessage);
                GlobalRequestContext.SetPreference("background_mode.enabled", false, out errorMessage);
                // GlobalRequestContext.SetPreference("webkit.webprefs.encrypted_media_enabled", true, out errorMessage);
                //plugins.always_open_pdf_externally
                //profile.block_third_party_cookies
                //printing.enabled
                //settings.force_google_safesearch
                //cefSettings.CefCommandLineArgs.Add("ssl-version-min", "tls1.2");

                //webkit.webprefs.default_fixed_font_size : 13
                //webkit.webprefs.default_font_size : 16
                //webkit.webprefs.loads_images_automatically
                //webkit.webprefs.javascript_enabled
                //webkit.webprefs.encrypted_media_enabled : True

                //GlobalRequestContext.SetPreference("extensions.storage.garbagecollect", true, out errorMessage);

                //webrtc.multiple_routes_enabled
                //webrtc.nonproxied_udp_enabled
                //var doNotTrack = (bool)GlobalRequestContext.GetAllPreferences(true)["enable_do_not_track"];

                //MessageBox.Show("DoNotTrack: " + doNotTrack);
            });
        }
        private async void InitializeEdge()
        {
            string availableVersion = null;
            try
            {
                availableVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (WebView2RuntimeNotFoundException)
            {
            }
            if (availableVersion != null && CoreWebView2Environment.CompareBrowserVersions(availableVersion, "100.0.0.0") >= 0)
            {
                WebView2Environment = await CoreWebView2Environment.CreateAsync(options: new CoreWebView2EnvironmentOptions(
                    "--enable-lite-video --enable-lazy-image-loading --enable-gpu-rasterization --remote-debugging-port=9222 --edge-automatic-https " +
                    "--enable-parallel-downloading --enable-quic --enable-heavy-ad-intervention --renderer-process-limit=2 --memory-model=low" +
                    "--enable-process-per-site --process-per-site --disable-site-per-process --disable-v8-idle-tasks --enable-zero-copy --disable-background-video-track'" +
                    "--turn-off-streaming-media-caching-on-battery --remote-allow-origins=http://localhost:9222" +
                    ""));
            }
            else
            {
                System.Console.WriteLine("Minimum version not found.");
            }
        }
        public CoreWebView2Environment WebView2Environment;
        private void SetCEFFlags(CefSettings settings)
        {
            //SplashScreen.Instance.ReportProgress(69, "Applying Chromium optimization features...");
            SetDefaultFlags(settings);
            SetBackgroundFlags(settings);
            SetUIFlags(settings);
            SetGPUFlags(settings);
            SetNetworkFlags(settings);
            SetSecurityFlags(settings);
            SetMediaFlags(settings);
            SetFrameworkFlags(settings);
            if (DeveloperMode)
                SetDeveloperFlags(settings);
            SetStreamingFlags(settings);
            SetExtensionFlags(settings);
            SetListedFlags(settings);

            SetChromiumFlags(settings);
            //SplashScreen.Instance.ReportProgress(80, "Done.");
        }
        private void SetChromiumFlags(CefSettings settings)
        {
            string[] ChromiumFlags = new string[0];
            //var _Args = new string[1] { "--chromium-flags=\"" +
            //    "autoplay-policy=\"no-user-gesture-required\"" +
            //    "\"" };
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--chromium-flags"))
                {
                    ChromiumFlags = Flag.Remove(Flag.Length - 1, 1).Replace("--chromium-flags=\"", "").Split(' ');
                    break;
                }
            }
            foreach (string ChromiumFlag in ChromiumFlags)
            {
                string[] Values = ChromiumFlag.Replace("\"", "").Split('=');
                settings.CefCommandLineArgs.Remove(Values[0]);
                if (Values.Length > 1)
                    settings.CefCommandLineArgs.Add(Values[0], Values[1]);
                else
                    settings.CefCommandLineArgs.Add(Values[0]);
            }
        }
        private void SetDefaultFlags(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("enable-chrome-runtime");

            settings.CefCommandLineArgs.Add("remote-allow-origins", "http://localhost:8089");

            if (bool.Parse(ExperimentsSave.Get("LowEndDeviceMode")))
                settings.CefCommandLineArgs.Add("enable-low-end-device-mode");

            settings.CefCommandLineArgs.Add("media-cache-size", "262144000");
            settings.CefCommandLineArgs.Add("disk-cache-size", "104857600");//104857600

            if (bool.Parse(ExperimentsSave.Get("AutoplayUserGestureRequired")))
                settings.CefCommandLineArgs.Add("autoplay-policy", "user-gesture-required");
            else
                settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
        }
        private void SetBackgroundFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-finch-seed-delta-compression");

            settings.CefCommandLineArgs.Add("battery-saver-mode-available");
            settings.CefCommandLineArgs.Add("high-efficiency-mode-available");

            settings.CefCommandLineArgs.Add("enable-ipc-flooding-protection");

            settings.CefCommandLineArgs.Add("disable-background-mode");

            settings.CefCommandLineArgs.Add("disable-highres-timer");
            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139
            if (bool.Parse(MainSave.Get("SkipLowPriorityTasks")))
                settings.CefCommandLineArgs.Add("disable-best-effort-tasks");

            settings.CefCommandLineArgs.Add("aggressive-cache-discard");
            settings.CefCommandLineArgs.Add("enable-simple-cache-backend");
            settings.CefCommandLineArgs.Add("v8-cache-options");
            settings.CefCommandLineArgs.Add("enable-font-cache-scaling");
            settings.CefCommandLineArgs.Add("enable-memory-coordinator");
            
            settings.CefCommandLineArgs.Add("stale-while-revalidate");

            settings.CefCommandLineArgs.Add("enable-raw-draw");
            settings.CefCommandLineArgs.Add("disable-oop-rasterization");
            //settings.CefCommandLineArgs.Add("canvas-oop-rasterization");


            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");

            settings.CefCommandLineArgs.Add("memory-model", "low");
            if (MainSave.Get("RendererProcessLimit") != "Unlimited")
                settings.CefCommandLineArgs.Add("renderer-process-limit", MainSave.Get("RendererProcessLimit"));

            if (bool.Parse(MainSave.Get("SiteIsolation")) == false)
            {
                settings.CefCommandLineArgs.Add("site-isolation-trial-opt-out");
                settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            }

            settings.CefCommandLineArgs.Add("enable-tile-compression");

            settings.CefCommandLineArgs.Add("automatic-tab-discarding");
            settings.CefCommandLineArgs.Add("stop-loading-in-background");

            settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes");
            settings.CefCommandLineArgs.Add("quick-intensive-throttling-after-loading");
            settings.CefCommandLineArgs.Add("expensive-background-timer-throttling");
            settings.CefCommandLineArgs.Add("intensive-wake-up-throttling");
            settings.CefCommandLineArgs.Add("align-wakeups");

            settings.CefCommandLineArgs.Add("max-tiles-for-interest-area", "64");
            settings.CefCommandLineArgs.Add("default-tile-width", "64");
            settings.CefCommandLineArgs.Add("default-tile-height", "64");
            //settings.CefCommandLineArgs.Add("num-raster-threads", "1");

            settings.CefCommandLineArgs.Add("enable-fast-unload");

            settings.CefCommandLineArgs.Add("calculate-native-win-occlusion");
            //settings.CefCommandLineArgs.Add("disable-backgrounding-occluded-windows");
            settings.CefCommandLineArgs.Add("enable-winrt-geolocation-implementation");

            settings.CefCommandLineArgs.Add("disable-v8-idle-tasks");
            settings.CefCommandLineArgs.Add("disable-mipmap-generation");
        }
        private void SetUIFlags(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("custom-devtools-frontend", "https://source.chromium.org/chromium/chromium/src/+/main:out/Debug/gen/third_party/devtools-frontend/src");
            settings.CefCommandLineArgs.Add("disable-pinch");

            settings.CefCommandLineArgs.Add("deferred-font-shaping");
            settings.CefCommandLineArgs.Add("enable-auto-disable-accessibility");
            settings.CefCommandLineArgs.Add("enable-print-preview");
            //settings.CefCommandLineArgs.Add("enable-pixel-canvas-recording");

            settings.CefCommandLineArgs.Add("enable-draw-occlusion");

            settings.CefCommandLineArgs.Add("enable-canvas-2d-dynamic-rendering-mode-switching");

            settings.CefCommandLineArgs.Add("enable-vulkan");
            settings.CefCommandLineArgs.Add("disable-usb-keyboard-detect");

            settings.CefCommandLineArgs.Add("force-renderer-accessibility");

            if (bool.Parse(ExperimentsSave.Get("SmoothScrolling")))
                settings.CefCommandLineArgs.Add("enable-smooth-scrolling");
            else
                settings.CefCommandLineArgs.Add("disable-smooth-scrolling");
            settings.CefCommandLineArgs.Add("enable-scroll-prediction");

            settings.CefCommandLineArgs.Add("enable-scroll-anchoring");
            settings.CefCommandLineArgs.Add("enable-skip-redirecting-entries-on-back-forward-ui");

            if (bool.Parse(ExperimentsSave.Get("ChromiumHardwareAcceleration")))
            {
                settings.CefCommandLineArgs.Add("enable-accelerated-2d-canvas");
            }
            else
            {
                settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas");
            }
        }
        private void SetGPUFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("in-process-gpu");
            //commandLine.AppendSwitch("off-screen-rendering-enabled");
            if (bool.Parse(ExperimentsSave.Get("ChromiumHardwareAcceleration")))
            {
                settings.CefCommandLineArgs.Add("ignore-gpu-blocklist");
                settings.CefCommandLineArgs.Add("enable-gpu");
                settings.CefCommandLineArgs.Add("enable-zero-copy");
                settings.CefCommandLineArgs.Add("disable-software-rasterizer");
                settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
                settings.CefCommandLineArgs.Add("gpu-rasterization-msaa-sample-count", MainSave.Get("MSAASampleCount"));
                if (MainSave.Get("AngleGraphicsBackend").ToLower() != "default")
                    settings.CefCommandLineArgs.Add("use-angle", MainSave.Get("AngleGraphicsBackend"));
                settings.CefCommandLineArgs.Add("enable-native-gpu-memory-buffers");
            }
            else
            {
                settings.CefCommandLineArgs.Add("disable-gpu");
                settings.CefCommandLineArgs.Add("disable-d3d11");
                settings.CefCommandLineArgs.Add("disable-gpu-compositing");
                settings.CefCommandLineArgs.Add("disable-direct-composition");
                settings.CefCommandLineArgs.Add("disable-gpu-vsync");
                settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
                settings.CefCommandLineArgs.Add("reduce-gpu-priority-on-background");
            }

            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
        }
        private void SetNetworkFlags(CefSettings settings)
        {
            if (!bool.Parse(MainSave.Get("PrintRaster")))
                settings.CefCommandLineArgs.Add("print-raster", "disabled");

            if (!bool.Parse(MainSave.Get("Prerender")))
                settings.CefCommandLineArgs.Add("prerender", "disabled");

            if (!bool.Parse(MainSave.Get("SpeculativePreconnect")))
                settings.CefCommandLineArgs.Add("disable-preconnect");

            if (!bool.Parse(MainSave.Get("PrefetchDNS")))
                settings.CefCommandLineArgs.Add("dns-prefetch-disable");

            settings.CefCommandLineArgs.Add("enable-webrtc-hide-local-ips-with-mdns");

            settings.CefCommandLineArgs.Add("enable-tcp-fast-open");
            settings.CefCommandLineArgs.Add("enable-quic");
            settings.CefCommandLineArgs.Add("enable-spdy4");
            settings.CefCommandLineArgs.Add("enable-brotli");

            settings.CefCommandLineArgs.Add("disable-domain-reliability");
            settings.CefCommandLineArgs.Add("no-proxy-server");
            settings.CefCommandLineArgs.Add("no-proxy");
            //settings.CefCommandLineArgs.Add("winhttp-proxy-resolver");
            settings.CefCommandLineArgs.Add("no-pings");
            settings.CefCommandLineArgs.Add("dns-over-https");

            settings.CefCommandLineArgs.Add("http-cache-partitioning");
            settings.CefCommandLineArgs.Add("partitioned-cookies");

            settings.CefCommandLineArgs.Add("disable-background-networking");
            settings.CefCommandLineArgs.Add("disable-component-extensions-with-background-pages");
            //settings.CefCommandLineArgs.Add("dns-prefetch-disable");
        }
        private void SetSecurityFlags(CefSettings settings)
        {
            //try
            //{
            //    settings.CefCommandLineArgs.Add("disable-domain-reliability");
            //}
            //catch { }
            settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");

            settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");

            settings.CefCommandLineArgs.Add("ignore-certificate-errors");

            //settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            //settings.CefCommandLineArgs.Add("allow-file-access-from-files");

            settings.CefCommandLineArgs.Add("enable-heavy-ad-intervention");
            settings.CefCommandLineArgs.Add("heavy-ad-privacy-mitigations");

            settings.CefCommandLineArgs.Add("tls13-variant");

            //settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption");
        }
        private void SetMediaFlags(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("allow-file-access-from-files");
            //settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("enable-parallel-downloading");

            settings.CefCommandLineArgs.Add("enable-jxl");

            settings.CefCommandLineArgs.Add("disable-login-animations");

            settings.CefCommandLineArgs.Add("disable-background-video-track");
            settings.CefCommandLineArgs.Add("enable-lite-video");
            settings.CefCommandLineArgs.Add("lite-video-force-override-decision");
            settings.CefCommandLineArgs.Add("enable-av1-decoder");

            if (bool.Parse(ExperimentsSave.Get("ChromiumHardwareAcceleration")))
            {
                settings.CefCommandLineArgs.Add("d3d11-video-decoder");
                settings.CefCommandLineArgs.Add("enable-accelerated-video-decode");
                settings.CefCommandLineArgs.Add("enable-accelerated-mjpeg-decode");
                settings.CefCommandLineArgs.Add("enable-vp9-kSVC-decode-acceleration");
                settings.CefCommandLineArgs.Add("enable-vaapi-av1-decode-acceleration");
                settings.CefCommandLineArgs.Add("enable-vaapi-jpeg-image-decode-acceleration");
                settings.CefCommandLineArgs.Add("enable-vaapi-webp-image-decode-accelerationn");
                settings.CefCommandLineArgs.Add("enable-vbr-encode-acceleration");
                settings.CefCommandLineArgs.Add("zero-copy-tab-capture");
                settings.CefCommandLineArgs.Add("zero-copy-video-capture");
            }
            else
            {
                settings.CefCommandLineArgs.Add("disable-low-res-tiling");

                settings.CefCommandLineArgs.Add("disable-accelerated-video");
                settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");
            }

            settings.CefCommandLineArgs.Add("force-enable-lite-pages");
            settings.CefCommandLineArgs.Add("enable-lazy-image-loading");
            settings.CefCommandLineArgs.Add("enable-lazy-frame-loading");

            settings.CefCommandLineArgs.Add("subframe-shutdown-delay");

            settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");
            //settings.CefCommandLineArgs.Add("enable-hdr");

            //settings.CefCommandLineArgs.Add("optimization-target-prediction");
            //settings.CefCommandLineArgs.Add("optimization-guide-model-downloading");
        }
        private void SetFrameworkFlags(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("use-angle", "opengl");

            //settings.CefCommandLineArgs.Add("use-angle", "gl");
            //settings.CefCommandLineArgs.Add("use-gl", "desktop");

            settings.CefCommandLineArgs.Add("accessible-pdf-form");
            //settings.CefCommandLineArgs.Add("pdf-ocr");
            //settings.CefCommandLineArgs.Add("pdf-xfa-forms");

            settings.CefCommandLineArgs.Add("enable-widevine-cdm");

            if (bool.Parse(ExperimentsSave.Get("WebAssembly")))
            {
                settings.CefCommandLineArgs.Add("enable-wasm");
                settings.CefCommandLineArgs.Add("enable-webassembly");
                settings.CefCommandLineArgs.Add("enable-asm-webassembly");
                settings.CefCommandLineArgs.Add("enable-webassembly-threads");
                settings.CefCommandLineArgs.Add("enable-webassembly-baseline");
                settings.CefCommandLineArgs.Add("enable-webassembly-tiering");
                settings.CefCommandLineArgs.Add("enable-webassembly-lazy-compilation");
                settings.CefCommandLineArgs.Add("enable-webassembly-streaming");
            }
            //settings.CefCommandLineArgs.Add("no-vr-runtime");
            //settings.CefCommandLineArgs.Add("force-webxr-runtime");
        }
        private void SetDeveloperFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-experimental-extension-apis");
            settings.CefCommandLineArgs.Add("enable-experimental-webassembly-features");
            settings.CefCommandLineArgs.Add("enable-experimental-webassembly-stack-switching");
            settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");
            settings.CefCommandLineArgs.Add("enable-experimental-canvas-features");
            settings.CefCommandLineArgs.Add("enable-javascript-harmony");
            settings.CefCommandLineArgs.Add("enable-javascript-experimental-shared-memory");
            settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
            settings.CefCommandLineArgs.Add("enable-devtools-experiments");
            //settings.CefCommandLineArgs.Add("web-share");
            //settings.CefCommandLineArgs.Add("enable-portals");
            settings.CefCommandLineArgs.Add("enable-webgl-developer-extensions");
            settings.CefCommandLineArgs.Add("webxr-incubations");
            settings.CefCommandLineArgs.Add("enable-generic-sensor-extra-classes");
            settings.CefCommandLineArgs.Add("enable-experimental-cookie-features");
        }
        private void SetStreamingFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-speech-api");
            settings.CefCommandLineArgs.Add("enable-voice-input");

            //settings.CefCommandLineArgs.Add("enable-media-stream");//Removed for permission prompt

            settings.CefCommandLineArgs.Add("enable-media-session-service");
            //settings.CefCommandLineArgs.Add("use-fake-device-for-media-stream");
            //settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");

            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            settings.CefCommandLineArgs.Add("enable-speech-input");
            //settings.CefCommandLineArgs.Add("allow-http-screen-capture");//HTTP is not allowed to use screen capture
            settings.CefCommandLineArgs.Add("auto-select-desktop-capture-source");
            settings.CefCommandLineArgs.Add("enable-speech-dispatcher");

            //settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-always");
            //settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");

            //settings.CefCommandLineArgs.Add("media-session-webrtc");

            //enable-webrtc-capture-multi-channel-audio-processing
            //enable-webrtc-analog-agc-clipping-control
        }
        private void SetExtensionFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("debug-plugin-loading");
            //settings.CefCommandLineArgs.Add("disable-plugins-discovery");

            if (!bool.Parse(ExperimentsSave.Get("PDFViewerExtension")))
                settings.CefCommandLineArgs.Add("disable-pdf-extension");
        }
        private void SetListedFlags(CefSettings settings)
        {
            //https://chromium.googlesource.com/chromium/src.git/+/refs/heads/main/gin/v8_initializer.cc
            //https://github.com/v8/v8/blob/9.4.117/src/flags/flag-definitions.h
            //https://medium.com/@yanguly/sparkplug-v8-baseline-javascript-compiler-758a7bc96e84
            //js-flag --lite_mode disables Wasm and some other stuff but makes SLBr 30 mb lighter
            //--turboprop, TurboProp is a faster and lighter version of TurboFan with some heavy optimisations turned off
            //Replace TurboFan with TurboProp completely with --turboprop-as-toptier
            //--sparkplug, Sparkplug is a non-optimising JavaScript compiler
            string JsFlags = "--enable-one-shot-optimization,--enable-experimental-regexp-engine-on-excessive-backtracks,--experimental-flush-embedded-blob-icache,--turbo-fast-api-calls,--gc-experiment-reduce-concurrent-marking-tasks,--lazy-feedback-allocation,--gc-global,--expose-gc,--max_old_space_size=512,--optimize-for-size,--idle-time-scavenge,--lazy";
            try
            {
                //TurnOffStreamingMediaCachingAlways,TurnOffStreamingMediaCachingOnBattery,

                //IsolateOrigins,site-per-process,
                settings.CefCommandLineArgs.Add("disable-features", "WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching");
                settings.CefCommandLineArgs.Add("enable-features", "MidiManagerWinrt,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LazyImageLoading,EnableTLS13EarlyData,LegacyTLSEnforced,AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics");
                settings.CefCommandLineArgs.Add("enable-blink-features", "NeverSlowMode,SkipAd,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics");

            }
            catch
            {
                //settings.CefCommandLineArgs["js-flags"] += ",--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt--enable-one-shot-optimization,--enable-experimental-regexp-engine-on-excessive-backtracks,--no-sparkplug,--experimental-flush-embedded-blob-icache,--turbo-fast-api-calls,--gc-experiment-reduce-concurrent-marking-tasks,--lazy-feedback-allocation,--gc-global,--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--expose-gc,--max_old_space_size=512,--optimize-for-size,--idle-time-scavenge,--lazy";
                settings.CefCommandLineArgs["disable-features"] += ",WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching";
                settings.CefCommandLineArgs["enable-features"] += ",MidiManagerWinrt,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LazyImageLoading,EnableTLS13EarlyData,LegacyTLSEnforced,AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics";
                settings.CefCommandLineArgs["enable-blink-featuress"] += ",NeverSlowMode,SkipAd,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics";
            }
            if (bool.Parse(MainSave.Get("SiteIsolation")) == false)
                settings.CefCommandLineArgs["disable-features"] += ",IsolateOrigins,site-per-process";
            if (bool.Parse(ExperimentsSave.Get("WebAssembly")))
                JsFlags += ",--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt";
            else
                JsFlags += ",--noexpose_wasm";
            if (bool.Parse(ExperimentsSave.Get("V8LiteMode")))
                JsFlags += ",--lite-mode";
            if (bool.Parse(ExperimentsSave.Get("V8Sparkplug")))
                JsFlags += ",--sparkplug";
            else
                JsFlags += ",--no-sparkplug";
            settings.JavascriptFlags = JsFlags;
        }
        #endregion


        public void ApplyTheme(Theme _Theme)
        {
            CurrentTheme = _Theme;
            MainSave.Set("Theme", _Theme.Name);
            foreach (MainWindow _Window in AllWindows)
                _Window.ApplyTheme(_Theme);
        }

        public void CloseSLBr(bool ExecuteCloseEvents = true)
        {
            if (AppInitialized)
            {
                StatisticsSave.Set("BlockedTrackers", TrackersBlocked.ToString());
                StatisticsSave.Set("BlockedAds", AdsBlocked.ToString());

                FavouritesSave.Clear();
                FavouritesSave.Set("Favourite_Count", Favourites.Count.ToString(), false);
                for (int i = 0; i < Favourites.Count; i++)
                    FavouritesSave.Set($"Favourite_{i}", Favourites[i].Tooltip, Favourites[i].Name, false);
                FavouritesSave.Save();

                SearchSave.Set("Search_Engine_Count", SearchEngines.Count.ToString(), false);
                for (int i = 0; i < SearchEngines.Count; i++)
                    SearchSave.Set($"Search_Engine_{i}", SearchEngines[i], false);
                SearchSave.Save();

                if (bool.Parse(MainSave.Get("RestoreTabs")))
                {
                    /*if (TabsSaves.Count < AllWindows.Count)
                    {
                        for (int i = 0; i < AllWindows.Count; i++)
                        {
                            if (TabsSaves.Count - 1 < i)
                            if (TabsSaves.Count < AllWindows.Count)
                            {
                                TabsSaves.Add(new Saving($"Window_{i}_Tabs.bin", UserApplicationWindowsPath));
                            }
                        }
                    }*/
                    foreach (FileInfo file in new DirectoryInfo(UserApplicationWindowsPath).GetFiles())
                        file.Delete();
                    foreach (MainWindow _Window in AllWindows)
                    {
                        //MessageBox.Show(_Window.Title);
                        Saving TabsSave = TabsSaves[AllWindows.IndexOf(_Window)];
                        TabsSave.Clear();

                        int Count = 0;
                        int SelectedIndex = 0;
                        for (int i = 0; i < _Window.Tabs.Count; i++)
                        {
                            //MessageBox.Show(_Window.Tabs[i].Header);
                            BrowserTabItem Tab = _Window.Tabs[i];
                            Browser BrowserView = _Window.GetBrowserView(Tab);
                            if (BrowserView != null)
                            {
                                //.Replace("slbr://processcrashed?s=", "").Replace("slbr://processcrashed/?s=", "")
                                TabsSave.Set($"Tab_{Count}", BrowserView.Address.Replace("slbr://processcrashed/?s=", ""), false);
                                if (i == _Window.BrowserTabs.SelectedIndex)
                                    SelectedIndex = Count;
                                Count++;
                            }
                            else
                            {
                                if (Settings.Instance.Tab == Tab)
                                {
                                    TabsSave.Set($"Tab_{Count}", "slbr://settings", false);
                                    if (i == _Window.BrowserTabs.SelectedIndex)
                                        SelectedIndex = Count;
                                    Count++;
                                }
                            }
                        }
                        TabsSave.Set("Tab_Count", Count.ToString());
                        TabsSave.Set("SelectedTabIndex", SelectedIndex.ToString());
                    }
                }
            }
            if (ExecuteCloseEvents)
            {
                foreach (MainWindow _Window in AllWindows)
                    _Window.ExecuteCloseEvent();
            }
            Cef.Shutdown();
            Current.Shutdown();
            AppInitialized = false;
        }

        public BitmapImage GetIcon(string Url/*, Theme _Theme = null*/)
        {
            //if (_Theme == null)
            //    _Theme = App.Instance.CurrentTheme;
            if (Utils.IsHttpScheme(Url))
                return new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Utils.CleanUrl(Url, true, true, true, false)));
            return new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", (App.Instance.CurrentTheme.DarkTitleBar ? "White Tab Icon.png" : "Black Tab Icon.png"))));
        }
    }
}
