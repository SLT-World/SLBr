using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using CefSharp.Wpf.HwndHost;
using CefSharp;
using System.Diagnostics;
using SLBr.Handlers;
using System.Windows.Interop;
using SLBr.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Reflection;
using CefSharp.SchemeHandler;
using Microsoft.Win32;
using System.Collections.Specialized;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class BrowserTabItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public string Header
        {
            get { return _Header; }
            set
            {
                _Header = value;
                RaisePropertyChanged("Header");
            }
        }
        public string _Header;
        public BitmapImage Icon
        {
            get { return _Icon; }
            set
            {
                _Icon = value;
                RaisePropertyChanged("Icon");
            }
        }
        public BitmapImage _Icon;
        public UserControl Content { get; set; }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        #region Variables
        public static MainWindow Instance;
        public ObservableCollection<BrowserTabItem> Tabs = new ObservableCollection<BrowserTabItem>();
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
        public List<string> SearchEngines;
        List<Theme> Themes = new List<Theme>();

        public Saving GlobalSave;
        public Saving MainSave;
        public Saving FavouritesSave;
        public Saving TabsSave;
        public Saving SearchSave;
        public Saving StatisticsSave;
        public Saving SandboxSave;
        public Saving ExperimentsSave;
        public Saving IESave;

        string Username = "Default-User";
        string GlobalApplicationDataPath;
        string UserApplicationDataPath;
        string CachePath;
        string UserDataPath;
        string LogPath;
        string ExecutablePath;

        public Random TinyRandom;
        public WebClient TinyDownloader;
        public LifeSpanHandler _LifeSpanHandler;
        public DownloadHandler _DownloadHandler;
        public RequestHandler _RequestHandler;
        public ContextMenuHandler _ContextMenuHandler;
        public KeyboardHandler _KeyboardHandler;
        public JsDialogHandler _JsDialogHandler;
        public PrivateJsObjectHandler _PrivateJsObjectHandler;
        public PublicJsObjectHandler _PublicJsObjectHandler;
        public QRCodeHandler _QRCodeHandler;
        public SafeBrowsing _SafeBrowsing;
        public int TrackersBlocked;
        public int AdsBlocked;
        bool IsFullscreen;
        string[] Args;
        public string ReleaseVersion = "2022.9.5.0";
        
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
        }
        HashSet<string> HardwareUnavailableProcessors = new HashSet<string>
        {
            "Intel(R) Iris(R) Xe Graphics",
            "Intel Iris Xe Integrated GPU",//Intel Iris Xe Integrated GPU(11th Gen)
            "Intel(R) Core(TM) i5"
        };
        #endregion

        public void SetRenderMode(string Mode, bool Notify)
        {
            if (Mode == "Hardware")
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                /*if (Notify)
                {
                    var ProcessorID = Utils.GetProcessorID();
                    foreach (string Processor in HardwareUnavailableProcessors)
                    {
                        if (ProcessorID.Contains(Processor))
                        {
                            Prompt(false, NoHardwareAvailableMessage.Replace("{0}", Processor), false, "", "", "", true, "\xE7BA");
                        }
                    }
                }*/
            }
            else if (Mode == "Software")
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            MainSave.Set("RenderMode", Mode);
        }

        #region Initialize
        private void SetIEEmulation(uint Value = 11001)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
            {
                if (key.GetValue("SLBr.exe") == null)
                key.SetValue("SLBr.exe", Value, RegistryValueKind.DWord);
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
            GlobalSave = new Saving("GlobalSave.bin", GlobalApplicationDataPath);
            MainSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            TabsSave = new Saving("Tabs.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            SandboxSave = new Saving("Sandbox.bin", UserApplicationDataPath);
            ExperimentsSave = new Saving("Experiments.bin", UserApplicationDataPath);
            IESave = new Saving("InternetExplorer.bin", UserApplicationDataPath);

            if (SearchSave.Has("Search_Engine_Count"))
            {
                SearchEngines = new List<string>();
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(SearchSave.Get("Search_Engine_Count")); i++)
                    {
                        string Url = SearchSave.Get($"Search_Engine_{i}");
                        if (!SearchEngines.Contains(Url))
                            SearchEngines.Add(Url);
                    }
                }));
            }
            else
                SearchEngines = new List<string>(DefaultSearchEngines);
            if (!MainSave.Has("FullAddress"))
                MainSave.Set("FullAddress", false);
            if (!MainSave.Has("ModernWikipedia"))
                MainSave.Set("ModernWikipedia", true);
            if (!MainSave.Has("Search_Engine"))
                MainSave.Set("Search_Engine", DefaultSearchEngines[0]);

            if (!MainSave.Has("Homepage"))
                MainSave.Set("Homepage", "slbr://newtab");
            if (!MainSave.Has("Theme"))
                MainSave.Set("Theme", "Dark");
            if (!StatisticsSave.Has("BlockedTrackers"))
                StatisticsSave.Set("BlockedTrackers", "0");
            TrackersBlocked = int.Parse(StatisticsSave.Get("BlockedTrackers"));
            if (!StatisticsSave.Has("BlockedAds"))
                StatisticsSave.Set("BlockedAds", "0");
            AdsBlocked = int.Parse(StatisticsSave.Get("BlockedAds"));

            if (!MainSave.Has("TabUnloading"))
                MainSave.Set("TabUnloading", true.ToString());
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

            if (!IESave.Has("IESuppressErrors"))
                IESave.Set("IESuppressErrors", true);

            if (!MainSave.Has("RestoreTabs"))
                MainSave.Set("RestoreTabs", true);
            if (!MainSave.Has("SelectedTabIndex"))
                MainSave.Set("SelectedTabIndex", 0);

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

            if (!ExperimentsSave.Has("HardwareAcceleration"))
                ExperimentsSave.Set("HardwareAcceleration", true);
            if (!ExperimentsSave.Has("LowEndDeviceMode"))
                ExperimentsSave.Set("LowEndDeviceMode", false);
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


            if (!MainSave.Has("BackgroundImage"))
                MainSave.Set("BackgroundImage", "Unsplash");
            if (!MainSave.Has("CustomBackgroundImage"))
                MainSave.Set("CustomBackgroundImage", "");
            Themes.Add(new Theme("Light", Colors.White, Colors.Black, Colors.Gainsboro, Colors.WhiteSmoke, Colors.Gray));
            Themes.Add(new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), Colors.White, (Color)ColorConverter.ConvertFromString("#36393F"), (Color)ColorConverter.ConvertFromString("#2F3136"), Colors.Gainsboro, true, true));
        }
        private void InitializeUISaves()
        {
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
                SetRenderMode(_RenderMode, false);
            }
            else
                SetRenderMode(MainSave.Get("RenderMode"), true);

            if (FavouritesSave.Has("Favourite_Count"))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(FavouritesSave.Get("Favourite_Count")); i++)
                    {
                        string[] Value = FavouritesSave.Get($"Favourite_{i}", true);
                        Favourites.Add(new ActionStorage(Value[1], $"3<,>{Value[0]}", Value[0]));
                    }
                }));
            }
            if (bool.Parse(MainSave.Get("RestoreTabs")) && TabsSave.Has("Tab_Count") && int.Parse(TabsSave.Get("Tab_Count")) > 0)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    int SelectedIndex = int.Parse(MainSave.Get("SelectedTabIndex"));
                    for (int i = 0; i < int.Parse(TabsSave.Get("Tab_Count")); i++)
                    {
                        string Url = TabsSave.Get($"Tab_{i}").Replace("slbr://processcrashed/?s=", "");
                        if (Url != "NOTFOUND")
                            NewBrowserTab(Url, 0);
                    }
                    BrowserTabs.SelectedIndex = SelectedIndex;
                }));
            }
            else
            {
                NewTabUrl = MainSave.Get("Homepage");
                CreateTabForCommandLineUrl = true;
            }
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
                NewBrowserTab(NewTabUrl, 0);
        }
        private void InitializeCEF()
        {
            _LifeSpanHandler = new LifeSpanHandler();
            _DownloadHandler = new DownloadHandler();
            _RequestHandler = new RequestHandler();
            _ContextMenuHandler = new ContextMenuHandler();
            _KeyboardHandler = new KeyboardHandler();
            _JsDialogHandler = new JsDialogHandler();
            _PrivateJsObjectHandler = new PrivateJsObjectHandler();
            _PublicJsObjectHandler = new PublicJsObjectHandler();
            _QRCodeHandler = new QRCodeHandler();

            _KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(Refresh, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(!IsFullscreen); }, (int)Key.F11);
            _KeyboardHandler.AddKey(Inspect, (int)Key.F12);
            _KeyboardHandler.AddKey(FindUI, (int)Key.F, true);

            _SafeBrowsing = new SafeBrowsing(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"), Environment.GetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID"));

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            CefSettings settings = new CefSettings();
            SetCEFFlags(settings);
            using (var currentProcess = Process.GetCurrentProcess())
                settings.BrowserSubprocessPath = currentProcess.MainModule.FileName;

            //settings.BrowserSubprocessPath = Args[0];
            //settings.ChromeRuntime = true;

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
                Schemes = new List<UrlScheme.Scheme> {
                    new UrlScheme.Scheme { PageName = "Credits", FileName = "Credits.html" },
                    new UrlScheme.Scheme { PageName = "License", FileName = "License.html" },
                    new UrlScheme.Scheme { PageName = "NewTab", FileName = "NewTab.html" },
                    new UrlScheme.Scheme { PageName = "Downloads", FileName = "Downloads.html" },
                    new UrlScheme.Scheme { PageName = "History", FileName = "History.html" },

                    new UrlScheme.Scheme { PageName = "Malware", FileName = "Malware.html" },
                    new UrlScheme.Scheme { PageName = "Deception", FileName = "Deception.html" },
                    new UrlScheme.Scheme { PageName = "ProcessCrashed", FileName = "ProcessCrashed.html" },
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

            Cef.Initialize(settings);
        }
        private void SetCEFFlags(CefSettings settings)
        {
            SetDefaultFlags(settings);
            SetBackgroundFlags(settings);
            SetUIFlags(settings);
            SetGPUFlags(settings);
            SetNetworkFlags(settings);
            SetSecurityFlags(settings);
            SetMediaFlags(settings);
            SetFrameworkFlags(settings);
            if (DeveloperMode)
            {
                SetDeveloperFlags(settings);
            }
            SetStreamingFlags(settings);
            SetExtensionFlags(settings);
            SetListedFlags(settings);

            SetChromiumFlags(settings);
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

            if (bool.Parse(ExperimentsSave.Get("LowEndDeviceMode")))
                settings.CefCommandLineArgs.Add("enable-low-end-device-mode");

            settings.CefCommandLineArgs.Add("media-cache-size", "262144000");
            settings.CefCommandLineArgs.Add("disk-cache-size", "104857600");

            if (bool.Parse(ExperimentsSave.Get("AutoplayUserGestureRequired")))
                settings.CefCommandLineArgs.Add("autoplay-policy", "user-gesture-required");
            else
                settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
        }
        private void SetBackgroundFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-ipc-flooding-protection");

            settings.CefCommandLineArgs.Add("disable-background-mode");

            settings.CefCommandLineArgs.Add("disable-highres-timer");
            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139
            settings.CefCommandLineArgs.Add("disable-best-effort-tasks");

            settings.CefCommandLineArgs.Add("enable-simple-cache-backend");
            settings.CefCommandLineArgs.Add("v8-cache-options");
            settings.CefCommandLineArgs.Add("enable-font-cache-scaling");
            settings.CefCommandLineArgs.Add("enable-memory-coordinator");

            settings.CefCommandLineArgs.Add("enable-raw-draw");
            settings.CefCommandLineArgs.Add("disable-oop-rasterization");
            //settings.CefCommandLineArgs.Add("canvas-oop-rasterization");

            settings.CefCommandLineArgs.Add("memory-model=low");

            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");

            settings.CefCommandLineArgs.Add("renderer-process-limit", "2");

            settings.CefCommandLineArgs.Remove("process-per-tab");
            settings.CefCommandLineArgs.Add("process-per-site");

            settings.CefCommandLineArgs.Add("enable-tile-compression");

            settings.CefCommandLineArgs.Add("automatic-tab-discarding");
            settings.CefCommandLineArgs.Add("stop-loading-in-background");

            settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes");
            settings.CefCommandLineArgs.Add("quick-intensive-throttling-after-loading");
            settings.CefCommandLineArgs.Add("expensive-background-timer-throttling");
            settings.CefCommandLineArgs.Add("intensive-wake-up-throttling");

            settings.CefCommandLineArgs.Add("max-tiles-for-interest-area", "64");
            settings.CefCommandLineArgs.Add("default-tile-width", "64");
            settings.CefCommandLineArgs.Add("default-tile-height", "64");
            //settings.CefCommandLineArgs.Add("num-raster-threads", "1");

            settings.CefCommandLineArgs.Add("enable-fast-unload");

            settings.CefCommandLineArgs.Add("calculate-native-win-occlusion");
            settings.CefCommandLineArgs.Add("enable-winrt-geolocation-implementation");
        }
        private void SetUIFlags(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("enable-print-preview");
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

            if (bool.Parse(ExperimentsSave.Get("HardwareAcceleration")))
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
            //commandLine.AppendSwitch("off-screen-rendering-enabled");
            if (bool.Parse(ExperimentsSave.Get("HardwareAcceleration")))
            {
                settings.CefCommandLineArgs.Add("ignore-gpu-blocklist");
                settings.CefCommandLineArgs.Add("enable-gpu");
                settings.CefCommandLineArgs.Add("enable-zero-copy");
                settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
                //settings.CefCommandLineArgs.Add("gpu-rasterization-msaa-sample-count", "0");
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
            }

            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
        }
        private void SetNetworkFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-webrtc-hide-local-ips-with-mdns");

            settings.CefCommandLineArgs.Add("enable-tcp-fast-open");
            settings.CefCommandLineArgs.Add("enable-quic");
            settings.CefCommandLineArgs.Add("enable-spdy4");
            settings.CefCommandLineArgs.Add("enable-brotli");

            settings.CefCommandLineArgs.Add("no-proxy-server");
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
            settings.CefCommandLineArgs.Add("disable-domain-reliability");
            settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");

            settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");

            settings.CefCommandLineArgs.Add("ignore-certificate-errors");

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            //settings.CefCommandLineArgs.Add("allow-file-access-from-files");

            settings.CefCommandLineArgs.Add("enable-heavy-ad-intervention");
            settings.CefCommandLineArgs.Add("heavy-ad-privacy-mitigations");
            
            settings.CefCommandLineArgs.Add("tls13-variant");

            //settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption");
        }
        private void SetMediaFlags(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-parallel-downloading");

            settings.CefCommandLineArgs.Add("enable-jxl");

            settings.CefCommandLineArgs.Add("disable-login-animations");
            settings.CefCommandLineArgs.Add("disable-low-res-tiling");

            settings.CefCommandLineArgs.Add("disable-background-video-track");
            settings.CefCommandLineArgs.Add("zero-copy-video-capture");
            settings.CefCommandLineArgs.Add("enable-lite-video");
            settings.CefCommandLineArgs.Add("lite-video-force-override-decision");
            settings.CefCommandLineArgs.Add("enable-av1-decoder");
            //settings.CefCommandLineArgs.Add("enable-hdr");

            if (bool.Parse(ExperimentsSave.Get("HardwareAcceleration")))
            {
                settings.CefCommandLineArgs.Add("d3d11-video-decoder");
                settings.CefCommandLineArgs.Add("enable-accelerated-video-decode");
                settings.CefCommandLineArgs.Add("enable-accelerated-mjpeg-decode");
            }
            else
            {
                settings.CefCommandLineArgs.Add("disable-accelerated-video");
                settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");
            }

            settings.CefCommandLineArgs.Add("force-enable-lite-pages");
            settings.CefCommandLineArgs.Add("enable-lazy-image-loading");
            settings.CefCommandLineArgs.Add("enable-lazy-frame-loading");

            settings.CefCommandLineArgs.Add("subframe-shutdown-delay");
        }
        private void SetFrameworkFlags(CefSettings settings)
        {
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
            settings.CefCommandLineArgs.Add("enable-experimental-webassembly-features");
            settings.CefCommandLineArgs.Add("enable-experimental-webassembly-stack-switching");
            settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");
            settings.CefCommandLineArgs.Add("enable-experimental-canvas-features");
            settings.CefCommandLineArgs.Add("enable-javascript-harmony");
            settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
            settings.CefCommandLineArgs.Add("enable-devtools-experiments");
            //settings.CefCommandLineArgs.Add("web-share");
            settings.CefCommandLineArgs.Add("webui-branding-update");
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

            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("enable-media-session-service");
            settings.CefCommandLineArgs.Add("use-fake-device-for-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            settings.CefCommandLineArgs.Add("enable-speech-input");
            settings.CefCommandLineArgs.Add("allow-http-screen-capture");
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
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");

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
                settings.CefCommandLineArgs.Add("disable-features", "WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching");
                settings.CefCommandLineArgs.Add("enable-features", "AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics");
                settings.CefCommandLineArgs.Add("enable-blink-features", "NeverSlowMode,SkipAd,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics");
                
            }
            catch
            {
                //settings.CefCommandLineArgs["js-flags"] += ",--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt--enable-one-shot-optimization,--enable-experimental-regexp-engine-on-excessive-backtracks,--no-sparkplug,--experimental-flush-embedded-blob-icache,--turbo-fast-api-calls,--gc-experiment-reduce-concurrent-marking-tasks,--lazy-feedback-allocation,--gc-global,--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--expose-gc,--max_old_space_size=512,--optimize-for-size,--idle-time-scavenge,--lazy";
                settings.CefCommandLineArgs["disable-features"] += ",WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching";
                settings.CefCommandLineArgs["enable-features"] += ",AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics";
                settings.CefCommandLineArgs["enable-blink-featuress"] += ",NeverSlowMode,SkipAd,LazyInitializeMediaControls,LazyFrameLoading,LazyFrameVisibleLoadTimeMetrics,LazyImageLoading,LazyImageVisibleLoadTimeMetrics";
            }
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

        string NewTabUrl = "";
        bool CreateTabForCommandLineUrl;

        bool DeveloperMode;
        public MainWindow()
        {
            Instance = this;
            if (Username != "Default-User")
                SetCurrentProcessExplicitAppUserModelID("{ab11da56-fbdf-4678-916e-67e165b21f30_" + Username + "}");

            string RegPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "SLBr.reg");
            /*if (Utils.IsAdministrator())
            {
                string SLBrExecutablePath = Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe");
                File.WriteAllText(RegPath, SLBrRegTemplate.Replace("{SLBr}", SLBrExecutablePath));
            }*/

            Args = Environment.GetCommandLineArgs();
            if (Args.Length > 1)
            {
                foreach (string Flag in Args)
                {
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
                        if (Directory.Exists(Flag) || File.Exists(Flag))
                            NewTabUrl = "file:\\\\\\" + Args[1];
                        else
                            NewTabUrl = Flag;
                        CreateTabForCommandLineUrl = true;
                    }
                }
            }
            if (!DeveloperMode)
                DeveloperMode = Debugger.IsAttached;
            if (!DeveloperMode)
                Application.Current.DispatcherUnhandledException += Window_DispatcherUnhandledException;
            GlobalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            CachePath = Path.Combine(UserApplicationDataPath, "Cache");
            UserDataPath = Path.Combine(UserApplicationDataPath, "User Data");
            LogPath = Path.Combine(UserApplicationDataPath, "Errors.log");
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            // Set Google API keys, used for Geolocation requests sans GPS.  See http://www.chromium.org/developers/how-tos/api-keys
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            //create cs file named SECRETS, add string variable GOOGLE_API_KEY with value of google api key
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            //add string variable GOOGLE_DEFAULT_CLIENT_ID with value of google client id
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);
            //add string variable GOOGLE_DEFAULT_CLIENT_SECRET with value of google client secret

            if (Utils.IsAdministrator())
            {
                using (var key = Registry.ClassesRoot.CreateSubKey("SLBr", true))
                {
                    key.SetValue(null, "SLBr");
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
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Clients\\StartMenuInternet", true).CreateSubKey("SLBr", true))
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
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
                {
                    key.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                }
                //Process regeditProcess = Process.Start("regedit.exe", "/s \"" + RegPath + "\"");
                //regeditProcess.WaitForExit();
            }

            InitializeIE();

            TinyRandom = new Random();
            TinyDownloader = new WebClient();

            InitializeSaves();
            InitializeCEF();
            InitializeUISaves();

            InitializeComponent();

            BrowserTabs.ItemsSource = Tabs;

            GCTimer.Tick += GCCollect_Tick;
            GCTimer.Start();
        }

        public void DiscordWebhookSendInfo(string Content)
        {
            try
            {
                TinyDownloader.UploadValues(SECRETS.DISCORD_WEBHOOK, new NameValueCollection
                {
                    { "content", Content },
                    { "username", "SLBr Diagnostics" }
                });
            }
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
            if (!DeveloperMode)
                CloseSLBr();
        }

        private DispatcherTimer GCTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 30) };
        private int UnloadTabsTime;
        private void GCCollect_Tick(object sender, EventArgs e)
        {
            if (bool.Parse(MainSave.Get("TabUnloading")))
            {
                if (UnloadTabsTime >= 150)
                    UnloadTabs();
                else
                    UnloadTabsTime += 30;
            }
            //GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        public void UnloadTabs()
        {
            BrowserTabItem SelectedTab = Tabs[BrowserTabs.SelectedIndex];
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (Tab != SelectedTab)
                {
                    Browser BrowserView = GetBrowserView(Tab);
                    if (BrowserView != null)
                        UnloadTab(BrowserView, true);
                }
            }
            UnloadTabsTime = 0;
        }
        private void UnloadTab(Browser BrowserView, bool IgnoreIfSound = true)
        {
            if (IgnoreIfSound)//PROBLEM: This checks if the address is a known music website. I need help on detecting sound.
            {
                string CleanedAddress = Utils.CleanUrl(BrowserView.Address, true, true, true, true);
                if ((CleanedAddress.StartsWith("youtube.com/watch")
                    || CleanedAddress.StartsWith("meet.google.com/")
                    || CleanedAddress.StartsWith("spotify.com/track/")
                    || CleanedAddress.StartsWith("soundcloud.com")
                    || CleanedAddress.StartsWith("dailymotion.com/video/")
                    || CleanedAddress.StartsWith("vimeo.com")
                    || CleanedAddress.StartsWith("twitch.tv/")
                    || CleanedAddress.StartsWith("bitchute.com/video/")
                    || CleanedAddress.StartsWith("ted.com/talks/")
                    ) && !BrowserView.IsAudioMuted)
                    return;
            }
            BrowserView.Unload(Framerate, Javascript, LoadImages, LocalStorage, Databases, WebGL);
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == null)
                    return;
                Actions _Action;
                var Target = (FrameworkElement)sender;
                string _Tag = Target.Tag.ToString();
                var Values = _Tag.Split(new string[] { "<,>" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3//, "&lt;,&gt;"
                _Action = (Actions)int.Parse(Values[0]);
                string LastValue = Values.Last();
                Action(_Action, sender, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
            }
            catch { }
        }

        public enum Actions
        {
            Undo = 0,
            Redo = 1,
            Refresh = 2,
            Navigate = 3,
            CreateTab = 4,
            CloseTab = 5,
            Inspect = 6,
        }
        private void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            switch (_Action)
            {
                case Actions.Undo:
                    Undo();
                    break;
                case Actions.Redo:
                    Redo();
                    break;
                case Actions.Refresh:
                    Refresh();
                    break;
                case Actions.CreateTab:
                    NewBrowserTab(V1, 0, true);
                    break;
                case Actions.CloseTab:
                    CloseCurrentBrowserTab();
                    break;
                case Actions.Inspect:
                    Inspect();
                    break;
                /*case Actions.Create_Tab:
                    CreateBrowserTab("Empty00000");
                    break;
                case Actions.CloseTab:
                    CloseTab();
                    break;*/
            }
        }
        public void Undo()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            if (_Browser.CanGoBack)
                _Browser.Back();
        }
        public void Redo()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            if (_Browser.CanGoForward)
                _Browser.Forward();
        }
        public void Refresh()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            if (!_Browser.IsLoading)
                _Browser.Reload();
            else
                _Browser.Stop();
        }
        public void Navigate(string Url)
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            _Browser.Navigate(Url);
        }
        public void Fullscreen(bool Fullscreen)
        {
            IsFullscreen = Fullscreen;
            if (Fullscreen)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Collapsed;
                        BrowserView.Margin = new Thickness(0, -25, 0, 0);
                    }
                }
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Visible;
                        BrowserView.Margin = new Thickness(0, 0, 0, 0);
                    }
                }
            }
        }
        public void Inspect()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            _Browser.Inspect();
        }
        public void FindUI()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            Keyboard.Focus(_Browser.FindTextBox);
        }
        public void NewBrowserTab(string Url, int BrowserType = 0, bool IsSelected = false, int Index = -1)
        {
            Url = Url.Replace("{Homepage}", MainSave.Get("Homepage"));
            BrowserTabItem _Tab = new BrowserTabItem { Header = Utils.CleanUrl(Url, true, true, true, true) };
            _Tab.Content = new Browser(Url, BrowserType, _Tab);
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
        }
        public void Settings(bool IsSelected = false, int Index = -1)
        {
            BrowserTabItem _Tab = new BrowserTabItem { Header = "Settings" };
            _Tab.Content = new Settings();
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
        }
        public void CloseCurrentBrowserTab()
        {
            if (Tabs.Count > 1)
            {
                Browser BrowserView = GetBrowserView(Tabs[BrowserTabs.SelectedIndex]);
                if (BrowserView != null)
                    BrowserView.DisposeCore();
                Tabs.RemoveAt(BrowserTabs.SelectedIndex);
            }
            else
            {
                CloseSLBr();
                Application.Current.Shutdown();
            }
        }
        public void Screenshot()
        {
            Browser BrowserView = GetBrowserView(Tabs[BrowserTabs.SelectedIndex]);
            if (BrowserView != null)
                BrowserView.Screenshot();
        }

        public void Zoom(int Delta)
        {
            Browser BrowserView = GetBrowserView(Tabs[BrowserTabs.SelectedIndex]);
            if (BrowserView != null)
                BrowserView.Zoom(Delta);
        }

        public BrowserTabItem GetTab(Browser _Control = null)
        {
            if (_Control != null)
            {
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    if (GetBrowserView(_Tab) == _Control)
                        return _Tab;
                }
                return null;
            }
            else
                return Tabs[BrowserTabs.SelectedIndex];
        }
        public UserControl GetUserControlView(BrowserTabItem Tab = null)
        {
            if (Tab != null)
                return Tab.Content;
            else
                return GetTab().Content;
        }
        public Browser GetBrowserView(BrowserTabItem Tab = null)
        {
            return GetUserControlView(Tab) as Browser;
        }
        public void ApplyTheme(Theme _Theme)
        {
            bool SetDarkTitleBar = _Theme.DarkTitleBar;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref SetDarkTitleBar, Marshal.SizeOf(true));

            Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);

            //WindowState = WindowState.Normal;
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (Tab.Content is Browser _Browser)
                    _Browser.ApplyTheme(_Theme);
            }
            //WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        public Theme GetTheme(string Name = "")
        {
            if (string.IsNullOrEmpty(Name))
                Name = MainSave.Get("Theme");
            foreach (Theme _Theme in Themes)
            {
                if (_Theme.Name == Name)
                    return _Theme;
            }
            return Themes[0];
        }
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

        public int Framerate;
        public CefState Javascript = CefState.Enabled;
        public CefState LoadImages = CefState.Enabled;
        public CefState LocalStorage = CefState.Enabled;
        public CefState Databases = CefState.Enabled;
        public CefState WebGL = CefState.Enabled;

        public void SetSandbox(int _Framerate, CefState JSState, CefState LIState, CefState LSState, CefState DBState, CefState WebGLState)
        {
            Framerate = _Framerate;
            Javascript = JSState;
            LoadImages = LIState;
            LocalStorage = LSState;
            Databases = DBState;
            WebGL = WebGLState;
            SandboxSave.Set("Framerate", _Framerate.ToString());
            SandboxSave.Set("JS", JSState.ToBoolean().ToString());
            SandboxSave.Set("LI", LIState.ToBoolean().ToString());
            SandboxSave.Set("LS", LSState.ToBoolean().ToString());
            SandboxSave.Set("DB", DBState.ToBoolean().ToString());
            SandboxSave.Set("WebGL", WebGLState.ToBoolean().ToString());
            foreach (BrowserTabItem Tab in Tabs)
            {
                Browser BrowserView = GetBrowserView(Tab);
                if (BrowserView != null)
                    BrowserView.Unload(Framerate, JSState, LIState, LSState, DBState, WebGLState);
            }
            UnloadTabsTime = 0;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseSLBr();
            Application.Current.Shutdown();
        }
        public void CloseSLBr()
        {
            if (GCTimer != null)
                GCTimer.Stop();

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

            TabsSave.Clear();
            if (bool.Parse(MainSave.Get("RestoreTabs")))
            {
                int Count = 0;
                int SelectedIndex = 0;
                for (int i = 0; i < Tabs.Count; i++)
                {
                    BrowserTabItem Tab = Tabs[i];
                    Browser BrowserView = GetBrowserView(Tab);
                    if (BrowserView != null)
                    {
                        TabsSave.Set($"Tab_{Count}", ((Browser)Tab.Content).Address, false);
                        if (i == BrowserTabs.SelectedIndex)
                            SelectedIndex = Count;
                        Count++;
                    }
                }
                TabsSave.Set("Tab_Count", Count.ToString());
                MainSave.Set("SelectedTabIndex", SelectedIndex.ToString());
            }
            Cef.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(GetTheme());
        }
    }
}
