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
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using Microsoft.Windows.Themes;
using static SLBr.Controls.UrlScheme;
using System.Drawing.Drawing2D;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public enum Actions
    {
        Undo = 0,
        Redo = 1,
        Refresh = 2,
        Navigate = 3,
        CreateTab = 4,
        CloseTab = 5,
        Inspect = 6,
        Favourite = 7,
        SetAudio = 8,
        Settings = 9,
        UnloadTabs = 10,
        SwitchBrowser = 11,
        OpenFileExplorer = 12,
        QRCode = 13,
        SetInspectorDock = 14,
        OpenAsPopupBrowser = 15,
        SizeEmulator = 16,
        ForceUnloadTab = 17,
        OpenNewBrowserPopup = 18,
        ClosePrompt = 19,
        Prompt = 20,
        PromptNavigate = 21,
        SwitchTabAlignment = 22,
    }

    public class BrowserTabItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public BrowserTabItem()
        {
            TabAlignment = MainWindow.Instance.MainSave.Get("TabAlignment");
            DimIconWhenUnloaded = bool.Parse(MainWindow.Instance.MainSave.Get("DimIconsWhenUnloaded"));

            Id = Utils.GenerateRandomId();
            Action = $"5<,>{Id}";
        }

        public string TabAlignment
        {
            get { return _TabAlignment; }
            set
            {
                _TabAlignment = value;
                RaisePropertyChanged("TabAlignment");
            }
        }
        private string _TabAlignment;
        public bool IsUnloaded
        {
            get { return _IsUnloaded; }
            set
            {
                _IsUnloaded = value;
                RaisePropertyChanged("IsUnloaded");
            }
        }
        private bool _IsUnloaded;
        public bool DimIconWhenUnloaded
        {
            get { return _DimIconWhenUnloaded; }
            set
            {
                _DimIconWhenUnloaded = value;
                RaisePropertyChanged("DimIconWhenUnloaded");
            }
        }
        private bool _DimIconWhenUnloaded;
        public string Header
        {
            get { return _Header; }
            set
            {
                _Header = value;
                RaisePropertyChanged("Header");
            }
        }
        private string _Header;
        public BitmapImage Icon
        {
            get { return _Icon; }
            set
            {
                _Icon = value;
                RaisePropertyChanged("Icon");
            }
        }
        private BitmapImage _Icon;
        public string Action
        {
            get { return _Action; }
            set
            {
                _Action = value;
                RaisePropertyChanged("Action");
            }
        }
        private string _Action;
        public UserControl Content { get; set; }
        public int Id
        {
            get { return _Id; }
            set
            {
                _Id = value;
                RefreshCommand = $"2<,>{value}";
                ForceUnloadCommand = $"17<,>{value}";
                AddToFavouritesCommand = $"7<,>{value}";
                MuteCommand = $"8<,>{value}";
                CloseCommand = $"5<,>{value}";
                MuteCommandHeader = "Mute";
                FavouriteCommandHeader = "Add to favourites";
                RaisePropertyChanged("Id");
            }
        }
        private int _Id;

        public Visibility BrowserCommandsVisibility
        {
            get { return _BrowserCommandsVisibility; }
            set
            {
                _BrowserCommandsVisibility = value;
                RaisePropertyChanged("BrowserCommandsVisibility");
            }
        }
        private Visibility _BrowserCommandsVisibility;
        public string RefreshCommand
        {
            get { return _RefreshCommand; }
            set
            {
                _RefreshCommand = value;
                RaisePropertyChanged("RefreshCommand");
            }
        }
        private string _RefreshCommand;
        public string ForceUnloadCommand
        {
            get { return _ForceUnloadCommand; }
            set
            {
                _ForceUnloadCommand = value;
                RaisePropertyChanged("ForceUnloadCommand");
            }
        }
        private string _ForceUnloadCommand;
        public string AddToFavouritesCommand
        {
            get { return _AddToFavouritesCommand; }
            set
            {
                _AddToFavouritesCommand = value;
                RaisePropertyChanged("AddToFavouritesCommand");
            }
        }
        private string _AddToFavouritesCommand;
        public string CloseCommand
        {
            get { return _CloseCommand; }
            set
            {
                _CloseCommand = value;
                RaisePropertyChanged("CloseCommand");
            }
        }
        private string _CloseCommand;
        public string MuteCommand
        {
            get { return _MuteCommand; }
            set
            {
                _MuteCommand = value;
                RaisePropertyChanged("MuteCommand");
            }
        }
        private string _MuteCommand;
        public string FavouriteCommandHeader
        {
            get { return _FavouriteCommandHeader; }
            set
            {
                _FavouriteCommandHeader = value;
                RaisePropertyChanged("FavouriteCommandHeader");
            }
        }
        private string _FavouriteCommandHeader;
        public string MuteCommandHeader
        {
            get { return _MuteCommandHeader; }
            set
            {
                _MuteCommandHeader = value;
                RaisePropertyChanged("MuteCommandHeader");
            }
        }
        private string _MuteCommandHeader;
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public new string Title
        {
            get
            {
                return base.Title;
            }
            set
            {
                base.Title = value;
                WindowChromeTitle.Text = value;
            }
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "\xe923" : "\xe922";
            MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
            //Timeline.SetDesiredFrameRate(, 1);
            /*Dispatcher.Invoke(new Action(() =>
            {
                switch (WindowState)
                {
                    case WindowState.Maximized:
                        ChangeWPFFrameRate(WPFFrameRate);
                        break;
                    case WindowState.Minimized:
                        ChangeWPFFrameRate(1);
                        break;
                    case WindowState.Normal:
                        ChangeWPFFrameRate(WPFFrameRate);
                        break;
                }
            }));*/
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
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
        public List<string> SearchEngines = new List<string>();
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

        public string Username = "Default-User";
        string GlobalApplicationDataPath;
        string UserApplicationDataPath;
        string CachePath;
        string UserDataPath;
        string LogPath;
        string ExecutablePath;

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
        bool IsFullscreen;
        string[] Args;
        public string ReleaseVersion;
        
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
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (item.IsComplete)
                {
                    TaskbarProgress.ProgressValue = 0;
                    CompletedDownloads.Add(new ActionStorage(Path.GetFileName(item.FullPath), "3<,>slbr://downloads/", ""));
                }
                else
                    TaskbarProgress.ProgressValue = (double)item.PercentComplete / 100.0;
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
            /*foreach (BrowserTabItem Tab in Tabs)
            {
                Browser BrowserView = (Browser)Tab.Content;
                if (BrowserView != null)
                    BrowserView.AdBlock(Boolean);
            }*/
        }
        public void TrackerBlock(bool Boolean)
        {
            MainSave.Set("TrackerBlock", Boolean.ToString());
            _RequestHandler.TrackerBlock = Boolean;
            /*foreach (BrowserTabItem Tab in Tabs)
            {
                Browser BrowserView = (Browser)Tab.Content;
                if (BrowserView != null)
                    BrowserView.TrackerBlock(Boolean);
            }*/
        }
        public void SetRenderMode(string Mode, bool Notify)
        {
            if (Mode == "Hardware")
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                //if (Notify)
                //{
                //    var ProcessorID = Utils.GetProcessorID();
                //    foreach (string Processor in HardwareUnavailableProcessors)
                //    {
                //        if (ProcessorID.Contains(Processor))
                //        {
                //            Prompt(false, NoHardwareAvailableMessage.Replace("{0}", Processor), false, "", "", "", true, "\xE7BA");
                //        }
                //    }
                //}
            }
            else if (Mode == "Software")
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            MainSave.Set("RenderMode", Mode);
        }

        #region Initialize
        private void SetIEEmulation(uint Value = 11001)
        {
            SplashScreen.Instance.ReportProgress(16, "Modifying IE Emulation...");
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
                SplashScreen.Instance.ReportProgress(17, "Failed.");
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
            SplashScreen.Instance.ReportProgress(15, "Initializing Internet Explorer features...");
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
            SplashScreen.Instance.ReportProgress(29, "Fetching data...");
            GlobalSave = new Saving("GlobalSave.bin", GlobalApplicationDataPath);
            MainSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            TabsSave = new Saving("Tabs.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            SandboxSave = new Saving("Sandbox.bin", UserApplicationDataPath);
            ExperimentsSave = new Saving("Experiments.bin", UserApplicationDataPath);
            IESave = new Saving("InternetExplorer.bin", UserApplicationDataPath);

            SplashScreen.Instance.ReportProgress(30, "Processing data...");
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
            if (!MainSave.Has("ModernWikipedia"))
                MainSave.Set("ModernWikipedia", true);
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
                SetTabUnloadingTime(5);
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

            if (!MainSave.Has("MSAASampleCount"))
                MainSave.Set("MSAASampleCount", 2);
            if (!MainSave.Has("RendererProcessLimit"))
                MainSave.Set("RendererProcessLimit", 2);
            if (!MainSave.Has("SiteIsolation"))
                MainSave.Set("SiteIsolation", true);

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
                    Themes.Add(new Theme("Auto", (key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0]: Themes[1]));
                }
            }
            catch
            {
                if (MainSave.Get("Theme") == "Auto")
                    MainSave.Set("Theme", "Dark");
            }
            SplashScreen.Instance.ReportProgress(31, "Done.");
        }
        public void SetDimIconsWhenUnloaded(bool Toggle)
        {
            foreach (BrowserTabItem _Tab in Tabs)
                _Tab.DimIconWhenUnloaded = Toggle;
            MainSave.Set("DimIconsWhenUnloaded", true);
        }
        private void InitializeUISaves()
        {
            SplashScreen.Instance.ReportProgress(85, "Processing data...");
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
            SplashScreen.Instance.ReportProgress(81, "Initializing UI...");
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
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (bool.Parse(MainSave.Get("RestoreTabs")) && TabsSave.Has("Tab_Count") && int.Parse(TabsSave.Get("Tab_Count")) > 0)
                {
                    int SelectedIndex = int.Parse(MainSave.Get("SelectedTabIndex"));
                    for (int i = 0; i < int.Parse(TabsSave.Get("Tab_Count")); i++)
                    {
                        string Url = TabsSave.Get($"Tab_{i}");
                        if (Url != "NOTFOUND")
                        {
                            if (Url == "slbr://settings")
                                OpenSettings(false);
                            else
                                NewBrowserTab(Url.Replace("slbr://processcrashed?s=", "").Replace("slbr://processcrashed/?s=", ""), int.Parse(MainSave.Get("DefaultBrowserEngine")));
                        }
                    }
                    BrowserTabs.SelectedIndex = SelectedIndex;
                }
                else
                {
                    if (string.IsNullOrEmpty(NewTabUrl))
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
                    NewBrowserTab(NewTabUrl, int.Parse(MainSave.Get("DefaultBrowserEngine")), true);
            }));
            SplashScreen.Instance.ReportProgress(86, "Done.");
        }
        private void InitializeCEF()
        {
            SplashScreen.Instance.ReportProgress(43, "Initializing LifeSpan Handler...");
            _LifeSpanHandler = new LifeSpanHandler();
            SplashScreen.Instance.ReportProgress(44, "Initializing Download Handler...");
            _DownloadHandler = new DownloadHandler();
            SplashScreen.Instance.ReportProgress(44, "Initializing Request Handler...");
            _RequestHandler = new RequestHandler();
            SplashScreen.Instance.ReportProgress(46, "Initializing Menu Handler...");
            _ContextMenuHandler = new ContextMenuHandler();
            SplashScreen.Instance.ReportProgress(47, "Initializing Keyboard Handler...");
            _KeyboardHandler = new KeyboardHandler();
            SplashScreen.Instance.ReportProgress(48, "Initializing Javascript Dialog Handler...");
            _JsDialogHandler = new JsDialogHandler();
            SplashScreen.Instance.ReportProgress(49, "Initializing private Javascript Handler...");
            _PrivateJsObjectHandler = new PrivateJsObjectHandler();
            SplashScreen.Instance.ReportProgress(50, "Initializing public Javascript Handler...");
            _PublicJsObjectHandler = new PublicJsObjectHandler();
            SplashScreen.Instance.ReportProgress(51, "Initializing QR Code Handler.");
            _PermissionHandler = new PermissionHandler();
            SplashScreen.Instance.ReportProgress(51, "Initializing QR Code Handler.");
            _QRCodeHandler = new QRCodeHandler();
            SplashScreen.Instance.ReportProgress(52, "Done.");

            SplashScreen.Instance.ReportProgress(54, "Applying keyboard shortcuts...");
            _KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(delegate () { Refresh(); }, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(!IsFullscreen); }, (int)Key.F11);
            _KeyboardHandler.AddKey(delegate () { Inspect(); }, (int)Key.F12);
            _KeyboardHandler.AddKey(FindUI, (int)Key.F, true);
            SplashScreen.Instance.ReportProgress(55, "Done.");

            SplashScreen.Instance.ReportProgress(66, "Initializing SafeBrowsing API...");
            _SafeBrowsing = new SafeBrowsingHandler(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"), Environment.GetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID"));
            SplashScreen.Instance.ReportProgress(67, "Done.");

            SplashScreen.Instance.ReportProgress(68, "Processing...");
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

            SplashScreen.Instance.ReportProgress(81, "Registering network protocols...");
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
            SplashScreen.Instance.ReportProgress(82, "Done.");

            SplashScreen.Instance.ReportProgress(83, "Initializing Chromium...");
            Cef.Initialize(settings);
            SplashScreen.Instance.ReportProgress(84, "Chromium initialized.");

            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();

                string errorMessage;
                GlobalRequestContext.SetPreference("enable_do_not_track", bool.Parse(MainSave.Get("DoNotTrack")), out errorMessage);
                GlobalRequestContext.SetPreference("browser.enable_spellchecking", bool.Parse(MainSave.Get("SpellCheck")), out errorMessage);
                GlobalRequestContext.SetPreference("background_mode.enabled", false, out errorMessage);
                GlobalRequestContext.SetPreference("webkit.webprefs.encrypted_media_enabled", true, out errorMessage);
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
            WebView2Environment = await CoreWebView2Environment.CreateAsync(options: new CoreWebView2EnvironmentOptions(
                "--enable-lite-video --enable-lazy-image-loading --enable-gpu-rasterization --remote-debugging-port=9222 --edge-automatic-https " +
                "--enable-parallel-downloading --enable-quic --enable-heavy-ad-intervention --renderer-process-limit=2 --memory-model=low" +
                "--enable-process-per-site --process-per-site --disable-site-per-process --disable-v8-idle-tasks --enable-zero-copy --disable-background-video-track'" +
                "--turn-off-streaming-media-caching-on-battery" +
                ""));
        }
        public CoreWebView2Environment WebView2Environment;
        private void SetCEFFlags(CefSettings settings)
        {
            SplashScreen.Instance.ReportProgress(69, "Applying Chromium optimization features...");
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
            SplashScreen.Instance.ReportProgress(80, "Done.");
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
            settings.CefCommandLineArgs.Add("disable-best-effort-tasks");

            settings.CefCommandLineArgs.Add("aggressive-cache-discard");
            settings.CefCommandLineArgs.Add("enable-simple-cache-backend");
            settings.CefCommandLineArgs.Add("v8-cache-options");
            settings.CefCommandLineArgs.Add("enable-font-cache-scaling");
            settings.CefCommandLineArgs.Add("enable-memory-coordinator");

            settings.CefCommandLineArgs.Add("enable-raw-draw");
            settings.CefCommandLineArgs.Add("disable-oop-rasterization");
            //settings.CefCommandLineArgs.Add("canvas-oop-rasterization");


            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");

            settings.CefCommandLineArgs.Add("memory-model", "low");
            if (MainSave.Get("RendererProcessLimit") != "Unlimited")
                settings.CefCommandLineArgs.Add("renderer-process-limit", MainSave.Get("RendererProcessLimit"));

            //Failed to identify BrowserWrapper in OnContextCreated BrowserId:1

            //settings.CefCommandLineArgs.Remove("process-per-tab");
            //settings.CefCommandLineArgs.Remove("site-per-process");
            //settings.CefCommandLineArgs.Remove("enable-site-per-process");

            //settings.CefCommandLineArgs.Add("process-per-site");
            //settings.CefCommandLineArgs.Add("enable-process-per-site");

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
            try
            {
                settings.CefCommandLineArgs.Add("disable-domain-reliability");
            }
            catch { }
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

        bool Initialized;
        int WPFFrameRate = 30;
        string NewTabUrl = "";
        bool CreateTabForCommandLineUrl;

        public bool DeveloperMode;
        public string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case MessageHelper.WM_COPYDATA:
                    COPYDATASTRUCT _dataStruct = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    string _strMsg = Marshal.PtrToStringUni(_dataStruct.lpData, _dataStruct.cbData / 2);
                    NewBrowserTab(_strMsg, int.Parse(MainSave.Get("DefaultBrowserEngine")), true);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        public BitmapImage GetIcon(string Url)
        {
            if (Utils.IsHttpScheme(Url))
                return new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Utils.CleanUrl(Url, true, true, true, false)));
            return new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", (CurrentTheme.DarkTitleBar ? "White Tab Icon.png" : "Black Tab Icon.png"))));
        }

        public MainWindow()
        {
            InitializeWindow();
        }

        private async void InitializeWindow()
        {
            Instance = this;
            //ReleaseVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ToString();
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            SplashScreen.Instance.ReportProgress(0, "Processing...");
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            source.AddHook(new HwndSourceHook(WndProc));

            Args = Environment.GetCommandLineArgs();
            if (Args.Length > 1)
            {
                foreach (string Flag in Args)
                {
                    SplashScreen.Instance.ReportProgress(1, "Processing command line arguments...");
                    if (Args.ToList().IndexOf(Flag) == 0)
                        continue;
                    else if (Flag.StartsWith("--user="))
                        Username = Flag.Replace("--user=", "").Replace(" ", "-");
                }
                if (Username == "Default-User")
                {
                    Process _otherInstance = Utils.GetAlreadyRunningInstance();
                    if (_otherInstance != null)
                    {
                        MessageHelper.SendDataMessage(_otherInstance, Args[1]);
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }
            await Task.Delay(500);
            //FrameRateProperty = new FrameworkPropertyMetadata { DefaultValue = WPFFrameRate };
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = WPFFrameRate });
            //Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), FrameRateProperty);
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
                        CreateTabForCommandLineUrl = true;
                    }
                }
            }
            if (Username != "Default-User")
            {
                AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30_" + Username + "}";
                SetCurrentProcessExplicitAppUserModelID(AppUserModelID);
            }
            if (!DeveloperMode)
                DeveloperMode = Debugger.IsAttached;
            Application.Current.DispatcherUnhandledException += Window_DispatcherUnhandledException;
            GlobalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
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

            InitializeIE();

            TinyRandom = new Random();
            TinyDownloader = new WebClient();

            InitializeSaves();
            InitializeCEF();
            InitializeEdge();
            InitializeComponent();
            InitializeUISaves();

            //await Task.Delay(500);
            SplashScreen.Instance.ReportProgress(87, "Initializing components...");
            BrowserTabs.ItemsSource = Tabs;

            GCTimer.Tick += GCCollect_Tick;
            GCTimer.Start();
            //await Task.Delay(500);
            SplashScreen.Instance.ReportProgress(99, "Complete.");
            //await Task.Delay(500);
            SplashScreen.Instance.ReportProgress(100, "Showing window...");
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
        private int UnloadTabsTimeIncrement;
        private int TabUnloadingTime;
        public void SetTabUnloadingTime(int Time)
        {
            //MessageBox.Show(Time.ToString());
            TabUnloadingTime = Time * 30;
            UnloadTabsTimeIncrement = 0;
            MainSave.Set("TabUnloadingTime", Time);
        }
        private void GCCollect_Tick(object sender, EventArgs e)
        {
            if (bool.Parse(MainSave.Get("TabUnloading")))
            {
                if (UnloadTabsTimeIncrement >= TabUnloadingTime)
                    UnloadTabs(bool.Parse(MainSave.Get("ShowUnloadedIcon")));
                else
                    UnloadTabsTimeIncrement += 30;
            }
            //GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        public void UnloadTabs(bool ChangeIcon)
        {
            BrowserTabItem SelectedTab = Tabs[BrowserTabs.SelectedIndex];
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (WindowState == WindowState.Minimized || Tab != SelectedTab)
                {
                    Browser BrowserView = GetBrowserView(Tab);
                    if (BrowserView != null)
                        UnloadTab(ChangeIcon, BrowserView);
                }
            }
            UnloadTabsTimeIncrement = 0;
        }
        private void UnloadTab(bool ChangeIcon, Browser BrowserView)
        {
            string CleanedAddress = BrowserView.Address;
            if (BrowserView.BrowserType == 0)
            {
                bool IsBlacklistedSite = CleanedAddress.Contains("youtube.com/watch") || CleanedAddress.Contains("meet.google.com/") || CleanedAddress.Contains("spotify.com/track/")
                        || CleanedAddress.Contains("soundcloud.com") || CleanedAddress.Contains("dailymotion.com/video/") || CleanedAddress.Contains("vimeo.com")
                        || CleanedAddress.Contains("twitch.tv/") || CleanedAddress.Contains("bitchute.com/video/") || CleanedAddress.Contains("ted.com/talks/");
                if (BrowserView.IsAudioMuted || !Utils.IsAudioPlayingInDevice() || !IsBlacklistedSite)
                    BrowserView.Unload(ChangeIcon, Framerate, Javascript, LoadImages, LocalStorage, Databases, WebGL);
            }
            else if (BrowserView.BrowserType == 1)
            {
                if (BrowserView.IsAudioMuted || (BrowserView.Edge.CoreWebView2 != null && (BrowserView.Edge.CoreWebView2.IsMuted || !BrowserView.Edge.CoreWebView2.IsDocumentPlayingAudio)))
                    BrowserView.Unload(ChangeIcon);
            }
            else if (BrowserView.BrowserType == 2)
            {
                BrowserView.Unload(ChangeIcon);
            }
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

        private void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            try
            {
                Browser _BrowserView = GetBrowserView();
                if (_BrowserView != null)
                {
                    V1 = V1.Replace("{CurrentUrl}", _BrowserView.Address);
                    V1 = V1.Replace("{CurrentInspectorUrl}", _BrowserView.Address);
                }
            }
            catch { }
            V1 = V1.Replace("{Homepage}", MainSave.Get("Homepage"));
            switch (_Action)
            {
                case Actions.Undo:
                    Undo(V1);
                    break;
                case Actions.Redo:
                    Redo(V1);
                    break;
                case Actions.Refresh:
                    Refresh(V1);
                    break;
                case Actions.CreateTab:
                    NewBrowserTab(V1, int.Parse(MainSave.Get("DefaultBrowserEngine")), true);
                    break;
                case Actions.CloseTab:
                    CloseBrowserTab(int.Parse(V1));
                    break;
                case Actions.Inspect:
                    Inspect(V1);
                    break;
                case Actions.Settings:
                    OpenSettings(true, BrowserTabs.SelectedIndex + 1);
                    break;
                case Actions.Favourite:
                    Favourite(V1);
                    break;
                case Actions.ForceUnloadTab:
                    ForceUnloadTab(V1);
                    break;
                case Actions.SetAudio:
                    SetAudio(V1);
                    break;
                case Actions.SwitchTabAlignment:
                    SwitchTabAlignment(V1);
                    break;
            }
        }
        public void SwitchTabAlignment(string NewAlignment)
        {
            if (NewAlignment == "Vertical")
            {
                SwitchTabAlignmentButton.ToolTip = "Switch to horizontal tabs";
                SwitchTabAlignmentButton.Tag = "22<,>Horizontal";
                SwitchTabAlignmentButton.Content = "\xE90E";
                BrowserTabs.Style = Resources["VerticalTabControlStyle"] as Style;
            }
            else if (NewAlignment == "Horizontal")
            {
                SwitchTabAlignmentButton.ToolTip = "Switch to vertical tabs";
                SwitchTabAlignmentButton.Tag = "22<,>Vertical";
                SwitchTabAlignmentButton.Content = "\xE90D";
                BrowserTabs.Style = Resources["HorizontalTabControlStyle"] as Style;
            }
            foreach (BrowserTabItem _Tab in Tabs)
                _Tab.TabAlignment = NewAlignment;
            MainSave.Set("TabAlignment", NewAlignment);
        }
        public void SetAudio(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.SetAudio(!_Browser.IsAudioMuted);
        }
        public void ForceUnloadTab(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.Unload(bool.Parse(MainSave.Get("ShowUnloadedIcon")), Framerate, Javascript, LoadImages, LocalStorage, Databases, WebGL);
        }
        public void Favourite(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.Favourite();
        }
        public void Undo(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            if (_Browser.CanGoBack)
                _Browser.Back();
        }
        public void Redo(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            if (_Browser.CanGoForward)
                _Browser.Forward();
        }
        public void Refresh(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
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
                //WindowState = WindowState.Normal;
                //WindowStyle = WindowStyle.None;
                //WindowState = WindowState.Maximized;
                Browser BrowserView = GetBrowserView();
                if (BrowserView != null)
                {
                    BrowserView.CoreContainer.Children.Remove(BrowserView.Chromium);
                    FullscreenContainer.Children.Add(BrowserView.Chromium);
                    Keyboard.Focus(BrowserView.Chromium);

                    if (bool.Parse(MainSave.Get("CoverTaskbarOnFullscreen")))
                    {
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.None;
                    }
                    WindowState = WindowState.Maximized;
                }
                /*foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Collapsed;
                        //BrowserView.Margin = new Thickness(0, -25, 0, 0);
                        //BrowserView.Margin = new Thickness(0, -67, 0, 0);
                        BrowserView.Margin = new Thickness(-239, -32, 0, 0);
                    }
                }*/
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Browser BrowserView = GetBrowserView();
                if (BrowserView != null)
                {
                    FullscreenContainer.Children.Remove(BrowserView.Chromium);
                    BrowserView.CoreContainer.Children.Add(BrowserView.Chromium);
                    Keyboard.Focus(BrowserView.Chromium);
                }
                /*foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Visible;
                        BrowserView.Margin = new Thickness(0, 0, 0, 0);
                    }
                }*/
            }
        }
        public void Inspect(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
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
            _Browser.FindTextBox.SelectAll();
        }
        /*public void NewTab(UserControl Content, string Header, bool IsSelected = false, int Index = -1)
        {
            BrowserTabItem _Tab = new BrowserTabItem { Header = Header, BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = Content;
            _Tab.Id = Utils.GenerateRandomId();
            _Tab.Action = $"5<,>{_Tab.Id}";
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
        }*/
        public void NewBrowserTab(string Url, int BrowserType = 0, bool IsSelected = false, int Index = -1)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            Url = Url.Replace("{Homepage}", MainSave.Get("Homepage"));
            BrowserTabItem _Tab = new BrowserTabItem { Header = Utils.CleanUrl(Url, true, true, true, true), BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = new Browser(Url, BrowserType, _Tab);
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
            //TextOptions.SetTextFormattingMode(_Tab.Content, TextFormattingMode.Display);
        }
        public void OpenSettings(bool IsSelected = false, int Index = -1)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            if (Settings.Instance != null && Settings.Instance.Tab != null)
                SwitchToTab(Settings.Instance.Tab);
            else
            {
                BrowserTabItem _Tab = new BrowserTabItem { Header = "Settings", BrowserCommandsVisibility = Visibility.Collapsed };
                if (Settings.Instance == null)
                    _Tab.Content = new Settings(_Tab);
                else
                {
                    Settings.Instance.Tab = _Tab;
                    _Tab.Content = Settings.Instance;
                }
                if (Index != -1)
                    Tabs.Insert(Index, _Tab);
                else
                    Tabs.Add(_Tab);
                if (IsSelected)
                    SwitchToTab(_Tab);

                //TextOptions.SetTextFormattingMode(_Tab.Content, TextFormattingMode.Display);
                //TextOptions.TextHintingModeProperty.OverrideMetadata(typeof(TextHintingMode), new FrameworkPropertyMetadata { DefaultValue = TextHintingMode.Fixed });
                //TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(TextFormattingMode), new FrameworkPropertyMetadata { DefaultValue = TextFormattingMode.Display });
            }
        }
        public void SwitchToTab(BrowserTabItem _Tab)
        {
            BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
            Browser BrowserView = GetBrowserView(_Tab);
            if (BrowserView != null)
                Keyboard.Focus(BrowserView.Chromium);
        }
        public BrowserTabItem GetBrowserTabWithId(int Id)
        {
            foreach (BrowserTabItem _Tab in Tabs)
            {
                if (_Tab.Id == Id)
                    return _Tab;
            }
            return null;
        }
        public void CloseBrowserTab(int Id)
        {
            //if (Id == -1)
            //    Id = Tabs[BrowserTabs.SelectedIndex].Id;
            BrowserTabItem _Tab = null;
            if (Id == -1)
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(Id);
            if (Tabs.Count > 1)
            {
                bool IsSelected = Id != -1 ? _Tab == Tabs[BrowserTabs.SelectedIndex] : true;
                //MessageBox.Show(Id.ToString());
                Browser BrowserView = GetBrowserView(_Tab);
                if (BrowserView != null)
                    BrowserView.DisposeCore();
                else
                {
                    if (Settings.Instance.Tab == _Tab)
                        Settings.Instance.DisposeCore();
                }
                if (IsSelected)
                {
                    if (BrowserTabs.SelectedIndex > 0)
                        BrowserTabs.SelectedIndex = BrowserTabs.SelectedIndex - 1;
                    else
                        BrowserTabs.SelectedIndex = BrowserTabs.SelectedIndex + 1;
                }
                Tabs.Remove(_Tab);
                if (IsSelected)
                {
                    if (BrowserTabs.SelectedIndex > Tabs.Count - 1)
                        BrowserTabs.SelectedIndex = Tabs.Count - 1;
                }

                //Tabs.RemoveAt(Tabs.IndexOf(_Tab));
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
            int SetDarkTitleBar = _Theme.DarkTitleBar ? 1 : 0;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref SetDarkTitleBar, Marshal.SizeOf(true));

            //int trueValue = 2;
            //DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref trueValue, Marshal.SizeOf(typeof(int)));
            //DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));

            //Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            //Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            //Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            //Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            //Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["UnselectedTabBrushColor"] = _Theme.UnselectedTabColor;
            Resources["ControlFontBrushColor"] = _Theme.ControlFontColor;

            //WindowState = WindowState.Normal;
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (Tab.Content is Browser _Browser)
                    _Browser.ApplyTheme(_Theme);
            }
            //WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowStyle = WindowStyle.SingleBorderWindow;

            CurrentTheme = _Theme;
        }
        public Theme GetTheme(string Name = "")
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
        Theme CurrentTheme;

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
            SandboxSave.Set("JS", JSState.ToBoolean().ToString());//webkit.webprefs.javascript_enabled
            SandboxSave.Set("LI", LIState.ToBoolean().ToString());
            SandboxSave.Set("LS", LSState.ToBoolean().ToString());
            SandboxSave.Set("DB", DBState.ToBoolean().ToString());
            SandboxSave.Set("WebGL", WebGLState.ToBoolean().ToString());
            foreach (BrowserTabItem Tab in Tabs)
            {
                Browser BrowserView = GetBrowserView(Tab);
                if (BrowserView != null)
                    BrowserView.Unload(false, Framerate, JSState, LIState, LSState, DBState, WebGLState);
            }
            UnloadTabsTimeIncrement = 0;
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
            if (Initialized)
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
                            TabsSave.Set($"Tab_{Count}", BrowserView.Address.Replace("slbr://processcrashed?s=", "").Replace("slbr://processcrashed/?s=", "").Replace("slbr://processcrashed/?s=", ""), false);
                            if (i == BrowserTabs.SelectedIndex)
                                SelectedIndex = Count;
                            Count++;
                        }
                        else
                        {
                            if (Settings.Instance.Tab == Tab)
                            {
                                TabsSave.Set($"Tab_{Count}", "slbr://settings", false);
                                if (i == BrowserTabs.SelectedIndex)
                                    SelectedIndex = Count;
                                Count++;
                            }
                        }
                    }
                    TabsSave.Set("Tab_Count", Count.ToString());
                    MainSave.Set("SelectedTabIndex", SelectedIndex.ToString());
                }
            }
            Cef.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(GetTheme());

            if (!DeveloperMode)
            {
                if (Utils.CheckForInternetConnection())
                {
                    try
                    {
                        string VersionInfo = TinyDownloader.DownloadString("https://raw.githubusercontent.com/SLT-World/SLBr/main/Version.txt").Replace("\n", "");
                        if (!VersionInfo.StartsWith(ReleaseVersion))
                            ToastBox.Show(VersionInfo, $"SLBr {VersionInfo} is now available, please update SLBr to keep up with the progress.", 10);
                        //Browser CurrentBrowser = GetBrowserView();
                        //if (CurrentBrowser != null)
                        //    GetBrowserView().Prompt(false, $"SLBr {VersionInfo} is now available, please update SLBr to keep up with the progress.", true, "Download", $"24<,>https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", $"https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", true, "\xE896");//SLBr is up to date
                    }
                    catch { }
                }
            }

            SplashScreen.Instance.Close();
            Initialized = true;
        }

        private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            { 
                BrowserTabItem _CurrentTab = Tabs[BrowserTabs.SelectedIndex];
                Browser BrowserView = GetBrowserView(_CurrentTab);
                if (BrowserView != null)
                    Keyboard.Focus(BrowserView.Chromium);

                Title = _CurrentTab.Header + (Username == "Default-User" ? " - SLBr" : $"- {Username} - SLBr");
            }
            catch
            {
                Title = Username == "Default-User" ? " - SLBr" : $"- {Username} - SLBr";
            }
        }

        /*static void ChangeWPFFrameRate(int FrameRate)
        {
            //Timeline.DesiredFrameRateProperty.DefaultMetadata.DefaultValue = FrameRate;
            FrameRateProperty.DefaultValue = FrameRate;
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), FrameRateProperty);
        }*/

        //static FrameworkPropertyMetadata FrameRateProperty;
    }
}
