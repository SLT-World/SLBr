// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using CefSharp.DevTools;
using CefSharp.SchemeHandler;
using CefSharp.Wpf;
using CefSharp.Wpf.Rendering.Experimental;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

//TODO: Test out settings tab closing

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Utils.WM_SHOWPAGE)
                //Vulnerability, hackers can call methods and control how to the browser runs, please help out
                //SOLUTION 1: Have a dialog that asks the user whether to proceed the action or ignore it
                //SOLUTION 2: Use a pipeline
                ShowPage();
            return IntPtr.Zero;
        }
        
        public void CreateBrowserTab(string Url, bool Focus = true, int Index = -1, bool NameByUrl = true)
        {
            if (IsIEMode)
                CreateIETab(CreateIEWebBrowser(Url), Focus, Index);
            else
                CreateChromeTab(CreateWebBrowser(Url), Focus, Index, NameByUrl);
        }

        private void ShowAndBrowse(string Url)
        {
            ShowPage();
            if (!IsProcessLoaded)
                return;
            if (Directory.Exists(Url) || File.Exists(Url))
                Url = "file:\\\\\\" + Url;
            CreateBrowserTab(Url);
        }
        private void ShowPage()
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Visible;
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        WindowState = WindowState.Normal;
                        Activate();
                    })
                );
            }
        }
        #region Start
        public enum BuildType
        {
            Offical,/*Standard*/
            Developer
        }

        public static MainWindow Instance;

        private static Guid DownloadsGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        private static Guid DocumentsGuid = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
        private static Guid MusicGuid = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
        private static Guid PicturesGuid = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
        private static Guid SavedGamesGuid = new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

        public static string GetDownloadsPath()
        {
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
            IntPtr pathPtr = IntPtr.Zero;
            try
            {
                SHGetKnownFolderPath(ref DownloadsGuid, 0, IntPtr.Zero, out pathPtr);
                return Marshal.PtrToStringUni(pathPtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }
        public static string GetScreenshotPath()
        {
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
            IntPtr pathPtr = IntPtr.Zero;
            try
            {
                SHGetKnownFolderPath(ref PicturesGuid, 0, IntPtr.Zero, out pathPtr);
                return Path.Combine(Marshal.PtrToStringUni(pathPtr), "Screenshots", "SLBr");
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        public List<string> DefaultSearchEngines = new List<string>() {
            "https://www.ecosia.org/search?q={0}",
            "https://google.com/search?q={0}",
            "https://bing.com/search?q={0}",
            "https://duckduckgo.com/?q={0}",
            "https://search.brave.com/search?q={0}",
            "https://search.yahoo.com/search?p={0}",
            "https://yandex.com/search/?text={0}",
        };
        public string GoogleWeblightUserAgent = "Mozilla/5.0 (Linux; Android 4.2.1; en-us; Nexus 5 Build/JOP40D) AppleWebKit/535.19 (KHTML, like Gecko; googleweblight) Chrome/38.0.1025.166 Mobile Safari/535.19 SLBr/2022.2.22";
        public List<string> SearchEngines;

        public Utils.Saving MainSave;
        public Utils.Saving FavouriteSave;
        public Utils.Saving SearchEnginesSave;
        public Utils.Saving TabsSave;
        public Utils.Saving SearchProviderUrlsSave;
        public Utils.Saving SandboxSave;

        public List<string> SearchProviderUrls = new List<string>();

        List<string> SaveNames = new List<string> { "Save.bin", "Favourites.bin", "SearchEngines.bin", "Tabs.bin", "SearchProviderUrls.bin", "Sandbox.bin" };
        List<Utils.Saving> Saves = new List<Utils.Saving>();

        public string ReleaseVersion;

        string ReleaseYear = "2022";
        string ReleaseMonth = "6";
        string ReleaseDay = "25";

        bool IsInformationSet;
        public string ChromiumVersion;
        public string ExecutableLocation;
        public string UserAgent;
        public string JavascriptVersion;
        //public string Revision;
        public string Bitness;
        public BuildType _BuildType;

        public string ProxyServer = "127.0.0.1:8088";//http://

        public string ApplicationDataPath;
        public string CachePath;
        public string LogPath;
        public string UserDataPath;
        public int RemoteDebuggingPort = 8088;
        
        public int BlockedTrackers;
        public int BlockedAds;

        public bool AddressBoxFocused;
        public bool AddressBoxMouseEnter;

        LifeSpanHandler _LifeSpanHandler;
        DownloadHandler _DownloadHandler;
        RequestHandler _RequestHandler;
        ContextMenuHandler _ContextMenuHandler;
        KeyboardHandler _KeyboardHandler;
        JsDialogHandler _JsDialogHandler;
        JSBindingHandler _JSBindingHandler;
        public Utils.SafeBrowsing _SafeBrowsing;

        SettingsWindow _SettingsWindow;

        public WebClient TinyDownloader;

        string[] Args;
        string NewTabUrl = "Empty00000";
        public bool IsPrivateMode;
        public bool IsDeveloperMode;
        public bool IsChromiumMode;
        public bool IsIEMode;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        private ObservableCollection<Favourite> PrivateFavourites = new ObservableCollection<Favourite>();
        public ObservableCollection<Favourite> Favourites
        {
            get { return PrivateFavourites; }
            set
            {
                PrivateFavourites = value;
                RaisePropertyChanged("Favourites");
            }
        }

        private ObservableCollection<Favourite> PrivateSuggestions = new ObservableCollection<Favourite>();
        public ObservableCollection<Favourite> Suggestions
        {
            get { return PrivateSuggestions; }
            set
            {
                PrivateSuggestions = value;
                RaisePropertyChanged("Suggestions");
            }
        }

        private ObservableCollection<Prompt> PrivatePrompts = new ObservableCollection<Prompt>();
        public ObservableCollection<Prompt> Prompts
        {
            get { return PrivatePrompts; }
            set
            {
                PrivatePrompts = value;
                RaisePropertyChanged("Prompts");
            }
        }

        //int PreviousTabIndex;

        URLScheme SLBrScheme;
        List<Theme> Themes = new List<Theme>();
        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);
        }
        public Theme GetCurrentTheme() =>
            GetTheme(MainSave.Get("Theme"));
        public Theme GetTheme(string Name)
        {
            foreach (Theme _Theme in Themes)
            {
                if (_Theme.Name == Name)
                    return _Theme;
            }
            return Themes[0];
        }
        #region Window
        public MainWindow()
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            source.AddHook(new HwndSourceHook(WndProc));
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 15 });

            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;// Fixed i5 problem with this code

            /*foreach (ManagementObject video in new ManagementObjectSearcher(new SelectQuery("Win32_VideoController")).Get())
            if ((string)video["Name"] == "Intel(R) Iris(R) Xe Graphics" && string.CompareOrdinal((string)video["DriverVersion"], "30.0.100.9667") <= 0)
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                break;
            }*/

            //Intel(R) Iris(R) Xe Graphics
            //Intel(R) Core(TM) i5-1135G7
            if (Utils.HasDebugger())
                _BuildType = BuildType.Developer;
            else
                _BuildType = BuildType.Offical;
            Args = Environment.GetCommandLineArgs();
            if (Args.Length > 1)
            {
                switch (Args[1])
                {
                    case "Private":
                        IsPrivateMode = true;
                        break;
                    case "Developer":
                        IsDeveloperMode = true;
                        break;
                    case "Chromium":
                        IsChromiumMode = true;
                        break;
                    case "IE":
                        IsIEMode = true;
                        break;
                    case "--chromium-flags":
                        break;
                    default:
                        if (Directory.Exists(Args[1]) || File.Exists(Args[1]))
                            NewTabUrl = "file:\\\\\\" + Args[1];
                        else
                            NewTabUrl = Args[1];
                        CreateTabForCommandLineUrl = true;
                        break;
                }
                /*else
                    //if (File.Exists(Args[1]))
                    NewTabUrl = Args[1];*/
            }
            if (!IsDeveloperMode)
                IsDeveloperMode = Utils.HasDebugger();
            //IsIEMode = true;
            //IsPrivateMode = true;
            if (!IsChromiumMode && !IsIEMode)
            {
                // Set Google API keys, used for Geolocation requests sans GPS.  See http://www.chromium.org/developers/how-tos/api-keys
                Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
                //Create a cs file named "SECRETS",
                //add 'public static string GOOGLE_API_KEY = "APIHERE";',
                //replace the "APIHERE" with your own google api key, or leave it empty.
                Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
                //Create a cs file named "SECRETS",
                //add 'public static string GOOGLE_DEFAULT_CLIENT_ID = "IDHERE";',
                //replace the "IDHERE" with your own google client id, or leave it empty.
                Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);
                //Create a cs file named "SECRETS",
                //add 'public static string GOOGLE_DEFAULT_CLIENT_SECRET = "SECRETHERE";',
                //replace the "SECRETHERE" with your own google client secret, or leave it empty.
            }
            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            if (IsPrivateMode)
                ApplicationDataPath = Path.Combine(ApplicationDataPath, "Private");
            CachePath = Path.Combine(ApplicationDataPath, "Cache");
            UserDataPath = Path.Combine(ApplicationDataPath, "User Data");
            LogPath = Path.Combine(ApplicationDataPath, "Debug.log");
            foreach (string Name in SaveNames)
                Saves.Add(new Utils.Saving(true, Name, ApplicationDataPath));
            InitializeComponent();
            Instance = this;
            DateTime CurrentDateTime = DateTime.Now;
            if (string.IsNullOrEmpty(ReleaseYear))
                ReleaseYear = CurrentDateTime.Year.ToString();
            if (string.IsNullOrEmpty(ReleaseMonth))
                ReleaseMonth = CurrentDateTime.Month.ToString();
            if (string.IsNullOrEmpty(ReleaseDay))
                ReleaseDay = CurrentDateTime.Day.ToString();
            ReleaseVersion = $"{ReleaseYear}.{ReleaseMonth}.{ReleaseDay}.0";
            //Assembly.GetExecutingAssembly().GetName().Version = Version.Parse(ReleaseVersion);
            //MessageBox.Show(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Bitness = Environment.Is64BitProcess ? "64" : "36";
            ChromiumVersion = Cef.ChromiumVersion;
            if (!IsIEMode)
                InitializeCEF();
            else
                SetBrowserEmulationVersion();
            /*else
                InitializeIE();*/
            MainSave = Saves[0];
            FavouriteSave = Saves[1];
            SearchEnginesSave = Saves[2];
            TabsSave = Saves[3];
            SearchProviderUrlsSave = Saves[4];
            SandboxSave = Saves[5];
            ExecutableLocation = Assembly.GetExecutingAssembly().Location.Replace("\\", "\\\\");
            TinyDownloader = new WebClient();
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();
        }
        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            try
            {
                if (e.OldDpi.PixelsPerDip != e.NewDpi.PixelsPerDip)
                {
                    Prompt(false, "Dynamic DPI is not utilized for SLBr's use.", false, "", "", "", true, "\xE7BA");
                    //MessageBox.Show("It seems that DPI has been changed. Netbird currently not supporting dynamic DPI. ", "Netbird Warning");
                }
            }
            catch
            {

            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Themes.Add(new Theme("Light", Colors.White, Colors.Black, Colors.Gainsboro, Colors.WhiteSmoke, Colors.Gray));
            Themes.Add(new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), Colors.White, (Color)ColorConverter.ConvertFromString("#36393F"), (Color)ColorConverter.ConvertFromString("#2F3136"), Colors.Gainsboro));
            Themes.Add(new Theme("Cheesy", (Color)ColorConverter.ConvertFromString("#FEE2AE"), Colors.Black, (Color)ColorConverter.ConvertFromString("#FDCD74"), (Color)ColorConverter.ConvertFromString("#FDBC44"), Colors.Orange));
            //Themes.Add(new Theme("Lush Green", (Color)ColorConverter.ConvertFromString("#037D06"), Colors.White, (Color)ColorConverter.ConvertFromString("#308214"), (Color)ColorConverter.ConvertFromString("#308214"), Colors.Gainsboro));
            Themes.Add(new Theme("Lush Green", (Color)ColorConverter.ConvertFromString("#82CC52"), Colors.White, (Color)ColorConverter.ConvertFromString("#77C34F"), (Color)ColorConverter.ConvertFromString("#62B249"), Colors.White));
            Themes.Add(new Theme("Blueprint", (Color)ColorConverter.ConvertFromString("#182539"), Colors.White, (Color)ColorConverter.ConvertFromString("#3F4A61"), (Color)ColorConverter.ConvertFromString("#FFAE42"), Colors.Gainsboro));
            if (SearchEnginesSave.Has("Search_Engine_Count"))
            {
                SearchEngines = new List<string>();
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(SearchEnginesSave.Get("Search_Engine_Count")); i++)
                    {
                        string Url = SearchEnginesSave.Get($"Search_Engine_{i}");
                        if (!SearchEngines.Contains(Url))
                            SearchEngines.Add(Url);
                    }
                }));
            }
            else
                SearchEngines = new List<string>(DefaultSearchEngines);
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!IsPrivateMode && !MainSave.Has("AssociationsSet"))
                {
                    if (Utils.IsAdministrator())
                    {
                        //FileAssociations.EnsureAssociationsSet();
                        Prompt(false, "The required associations for SLBr has been set, thank you for cooperating.");
                        MainSave.Set("AssociationsSet", true.ToString());
                    }
                    else
                        Prompt(false, "SLBr must be opened with administrative permissions to set the required associations.");
                }
                if (!MainSave.Has("Search_Engine"))
                {
                    MainSave.Set("Search_Engine", SearchEngines[IsPrivateMode ? 3 : 0]);
                }
                if (!MainSave.Has("UsedBefore"))
                {
                    if (NewTabUrl == "Empty00000" && !IsPrivateMode)
                    {
                        NewTabUrl = "slbr://about/";
                        CreateTabForCommandLineUrl = true;
                    }
                    MainSave.Set("UsedBefore", true.ToString());
                }
                if (MainSave.Has("PreviousSessionV") && MainSave.Get("PreviousSessionV") != ReleaseVersion)
                {
                    if (NewTabUrl == "Empty00000" && !IsPrivateMode)
                    {
                        NewTabUrl = "slbr://whatsnew/";
                        CreateTabForCommandLineUrl = true;
                    }
                }
                MainSave.Set("PreviousSessionV", ReleaseVersion);
                if (!MainSave.Has("Homepage"))
                    MainSave.Set("Homepage", "slbr://newtab"/*Utils.FixUrl(new Uri(SearchEngines[0]).Host, false)*/);
                if (!MainSave.Has("Theme"))
                    MainSave.Set("Theme", "Dark");
                if (!MainSave.Has("DarkWebpage"))
                    MainSave.Set("DarkWebpage", true.ToString());
                ApplyTheme(GetCurrentTheme());
                if (!MainSave.Has("RestoreTabs"))
                    MainSave.Set("RestoreTabs", (!IsPrivateMode).ToString());
                if (!MainSave.Has("FavouritesBar"))
                    MainSave.Set("FavouritesBar", true.ToString());
                if (!MainSave.Has("AdBlock"))
                    AdBlock(true);
                else
                    AdBlock(bool.Parse(MainSave.Get("AdBlock")));
                if (!MainSave.Has("TrackerBlock"))
                    TrackerBlock(true);
                else
                    TrackerBlock(bool.Parse(MainSave.Get("TrackerBlock")));

                if (!MainSave.Has("BlockedTrackers"))
                    MainSave.Set("BlockedTrackers", "0");
                BlockedTrackers = int.Parse(MainSave.Get("BlockedTrackers"));
                if (!MainSave.Has("BlockedAds"))
                    MainSave.Set("BlockedAds", "0");
                BlockedAds = int.Parse(MainSave.Get("BlockedAds"));

                if (!MainSave.Has("RenderMode"))
                {
                    string _RenderMode = "Hardware";
                    var ProcessorID = Utils.GetProcessorID();
                    foreach (string Processor in HardwareUnavailableProcessors)
                    {
                        if (ProcessorID.Contains(Processor))
                        {
                            _RenderMode = "Software";
                        }
                    }
                    SetRenderMode(_RenderMode, false);
                }
                else
                    SetRenderMode(MainSave.Get("RenderMode"), true);
                if (!MainSave.Has("LiteMode"))
                    MainSave.Set("LiteMode", true.ToString());
                if (!MainSave.Has("DoNotTrack"))
                    MainSave.Set("DoNotTrack", true.ToString());
                if (!MainSave.Has("AutoSuggestions"))
                    SetAutoSuggestions(!IsPrivateMode);
                else
                    SetAutoSuggestions(bool.Parse(MainSave.Get("AutoSuggestions")));
                if (!MainSave.Has("Weblight"))
                    MainSave.Set("Weblight", false.ToString());
                if (!MainSave.Has("VideoPopout"))
                    MainSave.Set("VideoPopout", false.ToString());
                if (!MainSave.Has("SelectedTabIndex"))
                    MainSave.Set("SelectedTabIndex", 0.ToString());
                if (!MainSave.Has("ShowTabs"))
                    MainSave.Set("ShowTabs", true.ToString());

                if (!MainSave.Has("ShowPerformanceMetrics"))
                    MainSave.Set("ShowPerformanceMetrics", true.ToString());

                if (!MainSave.Has("TabUnloading"))
                    MainSave.Set("TabUnloading", true.ToString());
                if (!MainSave.Has("FullAddress"))
                    MainSave.Set("FullAddress", false.ToString());
                if (!MainSave.Has("BlockKeywords"))
                    MainSave.Set("BlockKeywords", false.ToString());
                if (!MainSave.Has("BlockedKeywords"))
                    MainSave.Set("BlockedKeywords", "");
                if (!MainSave.Has("BlockRedirect"))
                    MainSave.Set("BlockRedirect", MainSave.Get("Homepage"));
                if (!MainSave.Has("DownloadPrompt"))
                    MainSave.Set("DownloadPrompt", true.ToString());
                if (!MainSave.Has("FindSearchProvider"))
                    MainSave.Set("FindSearchProvider", false.ToString());
                if (!MainSave.Has("DownloadPath"))
                    MainSave.Set("DownloadPath", GetDownloadsPath());
                if (!MainSave.Has("ScreenshotPath"))
                    MainSave.Set("ScreenshotPath", GetScreenshotPath());

                if (!SandboxSave.Has("Framerate"))
                    SandboxSave.Set("Framerate", "40");
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

                bool RestoreTabs = bool.Parse(MainSave.Get("RestoreTabs"));
                if (RestoreTabs)
                {
                    if (TabsSave.Has("Tab_Count"))
                    {
                        if (int.Parse(TabsSave.Get("Tab_Count")) > 0)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                int Index = int.Parse(MainSave.Get("SelectedTabIndex"));
                                for (int i = 0; i < int.Parse(TabsSave.Get("Tab_Count")); i++)
                                {
                                    string Url = TabsSave.Get($"Tab_{i}").Replace("slbr://renderprocesscrashed/?s=", "").Replace("https://googleweblight.com/?lite_url=", "");
                                    //if (!SearchEngines.Contains(Url))
                                    bool IsSelected = Index == i;
                                    CreateBrowserTab(Url, IsSelected, -1, true);
                                }
                                if (CreateTabForCommandLineUrl)
                                    CreateBrowserTab(NewTabUrl);
                            }));
                        }
                        else
                            CreateBrowserTab(NewTabUrl);
                    }
                    else
                        CreateBrowserTab(NewTabUrl);
                }
                else
                    CreateBrowserTab(NewTabUrl);
                ShowTabs(bool.Parse(MainSave.Get("ShowTabs")));
                if (FavouriteSave.Has("Favourite_Count"))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        for (int i = 0; i < int.Parse(FavouriteSave.Get("Favourite_Count")); i++)
                        {
                            string[] Value = FavouriteSave.Get($"Favourite_{i}", true);
                            Favourites.Add(new Favourite { Name = Value[1], Arguments = $"12<,>{Value[0]}", Address = Value[0] });
                        }
                        if (Favourites.Count == 0 || !bool.Parse(MainSave.Get("FavouritesBar")))
                            FavouriteContainer.Visibility = Visibility.Collapsed;
                        else
                            FavouriteContainer.Visibility = Visibility.Visible;
                    }));
                }
                else
                    FavouriteContainer.Visibility = Visibility.Collapsed;
            }));
            if (SearchProviderUrlsSave.Has("Url_Count"))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(SearchProviderUrlsSave.Get("Url_Count")); i++)
                    {
                        string Url = SearchProviderUrlsSave.Get(i.ToString());
                        if (!SearchProviderUrls.Contains(Url))
                            SearchProviderUrls.Add(Url);
                    }
                }));
            }
            if (IsPrivateMode)
            {
                Prompt(false, NoCacheString);//is being used
                CachePath = string.Empty;
                Title += " Private";
            }
            else if (IsDeveloperMode)
            {
                Prompt(false, DeveloperModeString, false, "", "", "", true, "\xE71C", "180");
                TestsMenuItem.Visibility = Visibility.Visible;
            }
            else if (IsChromiumMode)
                Prompt(false, NoAPIKeysString);
            else if (IsIEMode)
            {
                Prompt(false, IEModeString);
                FindTextBox.Visibility = Visibility.Collapsed;
                SSLGrid.Visibility = Visibility.Collapsed;
                ReaderModeButton.Visibility = Visibility.Collapsed;
            }
            GCTimer = new DispatcherTimer();
            GCTimer.Tick += GCCollect_Tick;
            GCTimer.Interval = new TimeSpan(0, 0, 30);
            GCTimer.Start();
            if (!IsIEMode)
            {
                SuggestionsTimer = new DispatcherTimer();
                SuggestionsTimer.Tick += SuggestionsTimer_Tick;
                SuggestionsTimer.Interval = new TimeSpan(0, 0, 1);

                Inspector = new ChromiumWebBrowser("localhost:8088/json/list");
                ConfigureInspectorBrowser();
                UtilityContainer.Children.Add(Inspector);
                RenderOptions.SetBitmapScalingMode(Inspector, BitmapScalingMode.LowQuality);
                new VideoPopoutWindow("", 0, VideoPopoutWindow.VideoProvider.Youtube);
            }
            if (Utils.CheckForInternetConnection())
            {
                try
                {
                    string VersionInfo = TinyDownloader.DownloadString("https://raw.githubusercontent.com/SLT-World/SLBr/main/Version.txt").Replace("\n", "");
                    if (!VersionInfo.StartsWith(ReleaseVersion))
                        Prompt(false, string.Format(NewUpdateString, VersionInfo), true, "Download", $"24<,>https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", $"https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", true, "\xE896");//SLBr is up to date
                }
                catch { }
            }

            IsProcessLoaded = true;

            FavouritesPanel.ItemsSource = Favourites;
            FavouritesMenu.Collection = Favourites;
            SuggestionsMenu.Collection = Suggestions;
            PromptsPanel.ItemsSource = Prompts;
            if (DateTime.Now.Day == 10 && DateTime.Now.Month == 10)
                ROCNationalDay.Visibility = Visibility.Visible;
            /*SSLToolTip.ApplyTemplate();
            SSLToolTip.IsOpen = true;
            SSLToolTip.Content = "APPLYTEMPLATE";
            SSLToolTip.IsOpen = false;*/
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsProcessLoaded)
            {
                if (GCTimer != null)
                    GCTimer.Stop();
                //if (UnloadAllTabsTimer != null)
                //    UnloadAllTabsTimer.Stop();

                MainSave.Set("BlockedTrackers", BlockedTrackers.ToString());
                MainSave.Set("BlockedAds", BlockedAds.ToString());

                FavouriteSave.Set("Favourite_Count", Favourites.Count.ToString(), false);
                for (int i = 0; i < Favourites.Count; i++)
                    FavouriteSave.Set($"Favourite_{i}", Favourites[i].Arguments.Replace("12<,>", ""), Favourites[i].Name, false);
                FavouriteSave.Save();
                SearchEnginesSave.Set("Search_Engine_Count", SearchEngines.Count.ToString(), false);
                for (int i = 0; i < SearchEngines.Count; i++)
                    SearchEnginesSave.Set($"Search_Engine_{i}", SearchEngines[i], false);
                SearchEnginesSave.Save();
                SearchProviderUrlsSave.Set("Url_Count", SearchProviderUrls.Count.ToString(), false);
                for (int i = 0; i < SearchProviderUrls.Count; i++)
                    SearchProviderUrlsSave.Set(i.ToString(), SearchProviderUrls[i], false);
                SearchProviderUrlsSave.Save();
                bool RestoreTabs = bool.Parse(MainSave.Get("RestoreTabs"));
                if (RestoreTabs)
                {
                    int Count = 0;
                    int SelectedIndex = 0;
                    for (int i = 0; i < Tabs.Items.Count; i++)
                    {
                        //if (i == 0)
                        //    continue;
                        TabItem Tab = (TabItem)Tabs.Items.GetItemAt(i);
                        string Url;
                        if (IsIEMode)
                        {
                            WebBrowser Browser = GetIEBrowser(Tab);
                            if (Browser == null)
                                continue;
                            Url = Browser.Source.AbsoluteUri;
                        }
                        else
                        {
                            ChromiumWebBrowser Browser = GetBrowser(Tab);
                            if (Browser == null)
                                continue;
                            Url = Browser.Address;
                        }
                        TabsSave.Set($"Tab_{Count}", Url, false);
                        if (Tab.IsSelected)
                            SelectedIndex = Count;
                        Count++;
                    }
                    TabsSave.Set("Tab_Count", Count.ToString());
                    MainSave.Set("SelectedTabIndex", SelectedIndex.ToString());
                }
            }
            if (!IsIEMode)
            {
                Inspector.Dispose();
                //UIThreadTimer.Tick -= UIThreadTimer_Tick;
                //UIThreadTimer.Stop();
                Cef.Shutdown();
            }
            //MainSave.Save();
            Application.Current.Shutdown();
        }
        #endregion
        HashSet<string> HardwareUnavailableProcessors = new HashSet<string>
        {
            "Intel(R) Iris(R) Xe Graphics",
            "Intel Iris Xe Integrated GPU",//Intel Iris Xe Integrated GPU(11th Gen)
            "Intel(R) Core(TM) i5"
        };
        string NoHardwareAvailableMessage = "The browser may seem to be unstable when using {0} cards. It is recommended to change from hardware to software rendering under Settings > Performance > Render Mode if you encounter such problem.";
        public void SetRenderMode(string Mode, bool Notify)
        {
            if (Mode == "Hardware")
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                if (Notify)
                {
                    var ProcessorID = Utils.GetProcessorID();
                    foreach (string Processor in HardwareUnavailableProcessors)
                    {
                        if (ProcessorID.Contains(Processor))
                        {
                            Prompt(false, NoHardwareAvailableMessage.Replace("{0}", Processor), false, "", "", "", true, "\xE7BA");
                        }
                    }
                }
            }
            else if (Mode == "Software")
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
            MainSave.Set("RenderMode", Mode);
        }
        private void SuggestionsTimer_Tick(object? sender, EventArgs e)
        {
            SuggestionsTimer.Stop();
            SetSuggestions();
        }

        private DispatcherTimer GCTimer;
        private int UnloadTabsTime;
        private void GCCollect_Tick(object sender, EventArgs e)
        {
            /*if (bool.Parse(MainSave.Get("LiteMode")))
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser != null)
                    _Browser.GetMainFrame().ExecuteJavaScriptAsync("images = document.getElementsByTagName('img');" +
                            "iframes = document.getElementsByTagName('iframe');" +
                            "for (var i = 0; i < images.length; i++) {" +
                                //"if (!images[i].complete) {" +
                                    "images[i].setAttribute('loading', 'lazy');" +
                                //"}" +
                            "};" +
                            "for (var i = 0; i < iframes.length; i++) {" +
                                "iframes[i].setAttribute('loading', 'lazy');" +
                            "};");
            }*/
            if (bool.Parse(MainSave.Get("TabUnloading")))
            {
                if (UnloadTabsTime >= 300)//900
                {
                    int SelectedIndex = Tabs.SelectedIndex;
                    foreach (TabItem Tab in Tabs.Items)
                    {
                        if (!Tab.IsSelected)
                        {
                            int _TabIndex = Tabs.Items.IndexOf(Tab);
                            if (_TabIndex > SelectedIndex + 1 || _TabIndex < SelectedIndex - 1)
                                UnloadTab(Tab, true);//PROBLEM: Look at the UnloadTab code
                        }
                    }
                    UnloadTabsTime = 0;
                }
                else
                    UnloadTabsTime += 30;
            }
            //GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        #region CEF Arguments
        /*private void DisableTouch(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("disable-touch-adjustment", "1");
            settings.CefCommandLineArgs.Add("top-chrome-touch-ui", "disabled");
            settings.CefCommandLineArgs.Add("touch-events", "disabled");
            settings.CefCommandLineArgs.Add("touch-devices", "disabled");
        }*/
        /*private void SetDNSArgs(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-features", "DnsOverHttps<DoHTrial");
            settings.CefCommandLineArgs.Add("force-fieldtrials", "DoHTrial/Group1");
            //settings.CefCommandLineArgs.Add("force-fieldtrial-params", "DoHTrial.Group1:Fallback/true/Templates/https%3A%2F%2Fcloudflare-dns.com%2Fdns-query");
            settings.CefCommandLineArgs.Add("force-fieldtrial-params", "DoHTrial.Group1:Fallback/true/Templates/https%3A%2F%2F1.1.1.1%2Fdns-query");

            //settings.CefCommandLineArgs["enable-features"] += ",DnsOverHttps<DoHTrial";//Cross Site Request

            //settings.CefCommandLineArgs.Add("enable-features", "dns-over-https<DoHTrial");
            //settings.CefCommandLineArgs.Add("force-fieldtrial-params", "DoHTrial.Group1:server/https%3A%2F%2F1.1.1.1%2Fdns-query/method/POST");
        }*/
        private void SetCEFArgs(CefSettings settings)
        {
            SetOffscreenArgs(settings);
            SetNetworkArgs(settings);
            SetRendererArgs(settings);
            SetAPIArgs(settings);
            SetSecurityArgs(settings);
            SetChromiumArgs(settings);
            //SetDNSArgs(settings);
        }
        private void SetChromiumArgs(CefSettings settings)
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
        private void SetOffscreenArgs(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("disable-gpu");
            settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
            settings.CefCommandLineArgs.Add("enable-native-gpu-memory-buffers");

            settings.CefCommandLineArgs.Add("disable-gpu-compositing");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
            settings.CefCommandLineArgs.Add("disable-direct-composition");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync");
            settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");

            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");

            if (IsPrivateMode)
                settings.CefCommandLineArgs.Add("enable-filesystem-in-incognito");
            else if (IsDeveloperMode)
                settings.CefCommandLineArgs.Add("enable-commerce-developer");

            settings.CefCommandLineArgs.Remove("enable-chrome-runtime");
            settings.CefCommandLineArgs.Add("reduce-user-agent");

            //Optimization
            settings.CefCommandLineArgs.Add("enable-low-end-device-mode");
            settings.CefCommandLineArgs.Add("enable-zero-copy");//Enable Zero Copy for Intel
            settings.CefCommandLineArgs.Add("disable-background-mode");

            settings.CefCommandLineArgs.Add("kiosk");

            //settings.CefCommandLineArgs.Add("renderer-process-limit", "25");

            settings.CefCommandLineArgs.Add("enable-simple-cache-backend");

            settings.CefCommandLineArgs.Add("js-flags", "max_old_space_size=1024,lite_mode,optimize_for_size,idle-time-scavenge,lazy");//enable-lazy-source-positions,gc-experiment-background-schedule
            settings.CefCommandLineArgs.Add("disk-cache-size", "5242880");//104857600
            //https://gist.github.com/andrewiggins/68c3165d47769a39eb5ae16e3001d6c6
            //Optimization

            //settings.CefCommandLineArgs.Add("proxy-server", "enable-drdc");

            //settings.CefCommandLineArgs.Add("proxy-server", ProxyServer);
            //settings.CefCommandLineArgs.Add("disable-pinch");
            //settings.CefCommandLineArgs.Add("no-experiments");

            //settings.CefCommandLineArgs.Add("in-process-gpu");//The --in-process-gpu option will run the GPU process as a thread in the main browser process. These processes consume most of the CPU time and the GPU driver crash will likely crash the whole browser, so you probably don't want to use it.
        }
        private void SetRendererArgs(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("enable-reader-mode");
            //if (IsDeveloperMode)
            //    settings.CefCommandLineArgs.Add("show-performance-metrics-hud", "1");

            //Optimization
            //settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas");
            //settings.CefCommandLineArgs.Add("back-forward-cache");

            //settings.CefCommandLineArgs.Add("scroll-unification");

            //settings.CefCommandLineArgs.Add("enable-smooth-scrolling");
            //settings.CefCommandLineArgs.Add("enable-overlay-scrollbar");

            settings.CefCommandLineArgs.Add("disable-threaded-scrolling");
            settings.CefCommandLineArgs.Add("disable-smooth-scrolling");
            settings.CefCommandLineArgs.Add("disable-features", "AsyncWheelEvents,TouchpadAndWheelScrollLatching");

            settings.CefCommandLineArgs.Add("canvas-oop-rasterization");

            //settings.CefCommandLineArgs.Add("off-screen-frame-rate", "40");
            settings.CefCommandLineArgs.Add("off-screen-frame-rate", "10");

            settings.CefCommandLineArgs.Add("enable-tile-compression");

            settings.CefCommandLineArgs.Add("proactive-tab-freeze-and-discard");//75
            settings.CefCommandLineArgs.Add("proactive-tab-freeze");//80
            settings.CefCommandLineArgs.Add("automatic-tab-discarding");

            settings.CefCommandLineArgs.Add("stop-loading-in-background");

            settings.CefCommandLineArgs.Add("expensive-background-timer-throttling");
            settings.CefCommandLineArgs.Add("intensive-wake-up-throttling");

            settings.CefCommandLineArgs.Add("max-tiles-for-interest-area", "64");
            settings.CefCommandLineArgs.Add("default-tile-width", "64");
            settings.CefCommandLineArgs.Add("default-tile-height", "64");
            settings.CefCommandLineArgs.Add("num-raster-threads", "1");

            settings.CefCommandLineArgs.Add("enable-fast-unload");
            //Optimization

            //back-forward-cache
            //enable-de-jelly
            //enable-prerender2
            //enable-dom-distiller
            //settings.CefCommandLineArgs.Add("enable-offline-auto-reload-visible-only");

            settings.CefCommandLineArgs.Add("enable-scroll-anchoring");

            //settings.CefCommandLineArgs.Add("force-effective-connection-type");

            settings.CefCommandLineArgs.Add("disable-surfaces");

            settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes");

            //disable-features="WebAuthenticationUseNativeWinApi"

            //if (IsDeveloperMode)
            //    settings.CefCommandLineArgs.Add("enable-print-preview");//Only for Non-OSR

            //settings.CefCommandLineArgs.Add("enable-use-zoom-for-dsf");
            //settings.CefCommandLineArgs.Add("enable-viewport");
            //settings.CefCommandLineArgs.Add("enable-features", "CastMediaRouteProvider,NetworkServiceInProcess");

            //if (IsDeveloperMode)
            //    settings.CefCommandLineArgs.Add("enable-logging"); //Enable Logging for the Renderer process (will open with a cmd prompt and output debug messages - use in conjunction with setting LogSeverity = LogSeverity.Verbose;)

            //settings.CefCommandLineArgs.Remove("mute-audio");
            //settings.BackgroundColor = Cef.ColorSetARGB(0, 255, 255, 255);

            //settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
        }
        private void SetNetworkArgs(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-tcp-fast-open");
            settings.CefCommandLineArgs.Add("enable-quic");
            settings.CefCommandLineArgs.Add("enable-spdy4");

            settings.CefCommandLineArgs.Add("no-proxy-server");
            //settings.CefCommandLineArgs.Add("winhttp-proxy-resolver");

            settings.CefCommandLineArgs.Add("no-pings");

            settings.CefCommandLineArgs.Add("dns-over-https");

            //settings.CefCommandLineArgs.Add("disable-http2");

            //enable-resource-prefetch
            //settings.CefCommandLineArgs.Add("dns-prefetch-disable");
        }
        private void SetAPIArgs(CefSettings settings)
        {
            //web-sql-access
            settings.CefCommandLineArgs.Add("sanitizer-api");
            settings.CefCommandLineArgs.Add("enable-winrt-geolocation-implementation");

            settings.CefCommandLineArgs.Add("disable-spell-checking");

            /*settings.CefCommandLineArgs.Add("enable-3d-apis", "1");
            settings.CefCommandLineArgs.Add("enable-webgl-draft-extensions", "1");
            settings.CefCommandLineArgs.Add("enable-gpu", "1");
            settings.CefCommandLineArgs.Add("enable-webgl", "1");*/

            //settings.CefCommandLineArgs.Add("disable-3d-apis", "1");
            //settings.CefCommandLineArgs.Add("disable-software-rasterizer");
            SetMediaArgs(settings);
            SetStreamingArgs(settings);
            SetPluginArgs(settings);
            SetVRArgs(settings);
        }
        private void SetSecurityArgs(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("enable-webrtc-hide-local-ips-with-mdns");

            settings.CefCommandLineArgs.Add("disable-domain-reliability");
            settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");

            settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");
            //settings.CefCommandLineArgs.Add("block-insecure-private-network-requests");

            settings.CefCommandLineArgs.Add("ignore-certificate-errors");
            //settings.CefCommandLineArgs.Add("allow-running-insecure-content");

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files");

            settings.CefCommandLineArgs.Add("enable-heavy-ad-intervention");
            settings.CefCommandLineArgs.Add("heavy-ad-privacy-mitigations");

            settings.CefCommandLineArgs.Add("disable-sync");

            settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption");
            //settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption-experiment");

            //settings.CefCommandLineArgs.Add("treat-unsafe-downloads-as-active-content");

            //settings.CefCommandLineArgs.Add("strict-origin-isolation");

            settings.CefCommandLineArgs.Remove("process-per-tab");
            //settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            //settings.CefCommandLineArgs.Add("site-isolation-trial-opt-out");
            settings.CefCommandLineArgs.Add("process-per-site");

            //settings.CefCommandLineArgs.Add("disable-features=IsolateOrigins,process-per-tab,site-per-process,process-per-site");

            //settings.CefCommandLineArgs["disable-features"] += ",SameSiteByDefaultCookies,CookiesWithoutSameSiteMustBeSecure";//Cross Site Request

            if (IsDeveloperMode)
                settings.CefCommandLineArgs.Add("ignore-gpu-blocklist");//Uncomment this if CPU is blacklisted or not utilized for SLBr's use on your device
        }
        private void SetMediaArgs(CefSettings settings)
        {
            //Media: Files, Folders, Images, Videos, Frames, etc...
            settings.CefCommandLineArgs.Add("enable-parallel-downloading");

            settings.CefCommandLineArgs.Add("enable-jxl");
            settings.CefCommandLineArgs.Add("enable-widevine-cdm");

            if (IsDeveloperMode)
            {
                settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");
                settings.CefCommandLineArgs.Add("enable-javascript-harmony");
                settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
                settings.CefCommandLineArgs.Add("web-share");
                //settings.CefCommandLineArgs.Add("enable-portals");
                //settings.CefCommandLineArgs.Add("enable-webgl-developer-extensions");
                //settings.CefCommandLineArgs.Add("webxr-incubations");
                //settings.CefCommandLineArgs.Add("enable-generic-sensor-extra-classes");
                //settings.CefCommandLineArgs.Add("enable-experimental-cookie-features");
            }

            //Optimizations
            settings.CefCommandLineArgs.Add("disable-login-animations");
            settings.CefCommandLineArgs.Add("disable-low-res-tiling");

            settings.CefCommandLineArgs.Add("disable-background-video-track");
            settings.CefCommandLineArgs.Add("zero-copy-video-capture");
            settings.CefCommandLineArgs.Add("enable-lite-video");
            //settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");

            settings.CefCommandLineArgs.Add("force-enable-lite-pages");
            settings.CefCommandLineArgs.Add("enable-lazy-image-loading");
            settings.CefCommandLineArgs.Add("enable-lazy-frame-loading");

            settings.CefCommandLineArgs.Add("subframe-shutdown-delay");
            //Optimizations
        }
        private void SetStreamingArgs(CefSettings settings)
        {
            settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");
            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            settings.CefCommandLineArgs.Add("enable-speech-input");

            //enable-webrtc-capture-multi-channel-audio-processing
            //enable-webrtc-analog-agc-clipping-control
        }
        private void SetVRArgs(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("no-vr-runtime");
            //settings.CefCommandLineArgs.Add("force-webxr-runtime");
        }
        private void SetPluginArgs(CefSettings settings)
        {
            //settings.CefCommandLineArgs.Add("pdf-ocr");
            //settings.CefCommandLineArgs.Add("pdf-xfa-forms");
            //https://bitbucket.org/chromiumembedded/cef/issues/2969/support-chrome-windows-with-cef-callbacks
            settings.CefCommandLineArgs.Add("debug-plugin-loading");
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");
            //enable-ime-service
            //settings.CefCommandLineArgs.Add("disable-extensions");
        }
        #endregion
        DispatcherTimer UIThreadTimer;
        //public bool Set;
        private void UIThreadTimer_Tick(object sender, EventArgs e)
        {
            Cef.DoMessageLoopWork();
        }
        private void InitializeCEF()
        {
            _LifeSpanHandler = new LifeSpanHandler();
            _DownloadHandler = new DownloadHandler();
            _RequestHandler = new RequestHandler();
            _ContextMenuHandler = new ContextMenuHandler();
            _KeyboardHandler = new KeyboardHandler();
            _JsDialogHandler = new JsDialogHandler();
            _JSBindingHandler = new JSBindingHandler();
            InitializeKeyboardHandler();
            if (!IsPrivateMode)
                _SafeBrowsing = new Utils.SafeBrowsing(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"), Environment.GetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID"));
            else
                _SafeBrowsing = new Utils.SafeBrowsing("", "");

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            CefSettings settings = new CefSettings();

            using (var currentProcess = Process.GetCurrentProcess())
                settings.BrowserSubprocessPath = currentProcess.MainModule.FileName;

            //settings.BrowserSubprocessPath = Args[0];

            settings.MultiThreadedMessageLoop = true;

            //settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;
            settings.CommandLineArgsDisabled = true;
            if (!IsChromiumMode)
                settings.UserAgentProduct = $"SLBr/{ReleaseVersion} Chrome/{ChromiumVersion}";
            settings.LogFile = LogPath;
            settings.LogSeverity = LogSeverity.Warning;
            if (IsPrivateMode)
            {
                //InMemory
                settings.CefCommandLineArgs.Add("disable-application-cache");
                settings.CefCommandLineArgs.Add("disable-cache");
                //settings.CefCommandLineArgs.Add("disable-session-storage");
            }
            else
                settings.CachePath = CachePath;
            settings.RemoteDebuggingPort = RemoteDebuggingPort;
            settings.UserDataPath = UserDataPath;

            //TODO: Use this to compress png, this method results in a crash when visiting the chrome web store.
            //Test failed, the image compression code keeps doing the opposite.
            //Instead of decreasing file size and quality, it increased file size and quality...
            //settings.RegisterScheme(new CefCustomScheme
            //{
            //    SchemeName = HTTPSSchemeHandlerFactory.SchemeName,
            //    SchemeHandlerFactory = new HTTPSSchemeHandlerFactory()
            //});


            SetCEFArgs(settings);

            //Enables Uncaught exception handler
            //settings.UncaughtExceptionStackSize = 10;
            
            /*var proxy = ProxyConfig.GetProxyInformation();
            switch (proxy.AccessType)
            {
                case InternetOpenType.Direct:
                    {
                        //Don't use a proxy server, always make direct connections.
                        settings.CefCommandLineArgs.Add("no-proxy-server");
                        break;
                    }
                case InternetOpenType.Proxy:
                    {
                        settings.CefCommandLineArgs.Add("proxy-server", proxy.ProxyAddress);
                        break;
                    }
                case InternetOpenType.PreConfig:
                    {
                        settings.CefCommandLineArgs.Add("proxy-auto-detect");
                        break;
                    }
            }*/

            if (!IsDeveloperMode && !IsChromiumMode)
            {
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "chrome",
                    SchemeHandlerFactory = new BlankSchemeHandlerFactory()
                });
            }
            SLBrScheme = new URLScheme
            {
                Name = "slbr",
                IsStandard = true,
                IsLocal = true,
                IsSecure = true,
                Schemes = new List<URLScheme.Scheme> { new URLScheme.Scheme { PageName = "Urls", FileName = "Urls.html" },
                    new URLScheme.Scheme { PageName = "Blank", FileName = "Blank.html" },
                    new URLScheme.Scheme { PageName = "About", FileName = "About.html" },
                    new URLScheme.Scheme { PageName = "Credits", FileName = "Credits.html" },
                    new URLScheme.Scheme { PageName = "Version", FileName = "Version.html" },
                    new URLScheme.Scheme { PageName = "License", FileName = "License.html" },
                    new URLScheme.Scheme { PageName = "NewTab", FileName = "NewTab.html" },
                    new URLScheme.Scheme { PageName = "WhatsNew", FileName = "WhatsNew.html" },
                    new URLScheme.Scheme { PageName = "CdmSupport", FileName = "CdmSupport.html" },

                    new URLScheme.Scheme { PageName = "Malware", FileName = "Malware.html" },
                    new URLScheme.Scheme { PageName = "Deception", FileName = "Deception.html" },
                    new URLScheme.Scheme { PageName = "RenderProcessCrashed", FileName = "RenderProcessCrashed.html" },
                    new URLScheme.Scheme { PageName = "UnderConstruction", FileName = "UnderConstruction.html" },

                    new URLScheme.Scheme { PageName = "Tetris", FileName = "Tetris.html" },
                    //new URLScheme.Scheme { PageName = "CannotConnect", FileName = "CannotConnect.html" },

                    new URLScheme.Scheme { PageName = "Copy_Icon.svg", FileName = "Copy_Icon.svg" }
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
                    IsStandard = SLBrScheme.IsStandard
                });
            }
            Cef.Initialize(settings);
            if (Environment.OSVersion.Version.Major >= 6)
                Cef.EnableHighDPISupport();
            if (IsPrivateMode)
            {
                Cef.GetGlobalCookieManager().DeleteCookies("", "");
            }
            //UIThreadTimer = new DispatcherTimer();
            //UIThreadTimer.Interval = TimeSpan.FromMilliseconds(1000 / 30);
            //UIThreadTimer.Tick += UIThreadTimer_Tick;
            //UIThreadTimer.Start();
        }
        private void InitializeKeyboardHandler()
        {
            /*_KeyboardHandler.AddKey(Refresh, (int)System.Windows.Forms.Keys.R, true);
            _KeyboardHandler.AddKey(delegate() { CreateTab(CreateWebBrowser()); }, (int)System.Windows.Forms.Keys.T, true);
            _KeyboardHandler.AddKey(delegate () { CloseTab(); }, (int)System.Windows.Forms.Keys.W, true);*/
            _KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(Refresh, (int)Key.F5);
            _KeyboardHandler.AddKey(UseInspector, (int)Key.F12);
        }
        bool CreateTabForCommandLineUrl;
        ChromiumWebBrowser Inspector;

        private const string IEBrowserEmulationKey = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        bool IsProcessLoaded;

        string NewUpdateString = "SLBr {0} is now available, please update SLBr to keep up with the progress.";

        string DeveloperModeString = "Enabled access to developer/experimental features & functionalities of SLBr.";
        string IEModeString = "Javascript will not function properly and much of SLBr's functionalities will be disabled/broken. There will also be unexpected crashes and lots of errors. But hey, have some fun testing out websites and see how broken it gets.";
        string NoCacheString = "No browsing history will be saved, in-memory cache will be used.";
        string NoAPIKeysString = "API Keys are missing. The following functionalities of SLBr will be disabled [SafeBrowsing, Google Sign-in].";
        public bool SetBrowserEmulationVersion()
        {
            bool _Result = false;
            try
            {
                RegistryKey _RegistryKey = Registry.CurrentUser.OpenSubKey(IEBrowserEmulationKey, true);

                if (_RegistryKey != null)
                {
                    string ProgramName = Path.GetFileName(Args[0]);
                    _RegistryKey.SetValue(ProgramName, 11001, RegistryValueKind.DWord);

                    //key.DeleteValue(programName, false);

                    _Result = true;
                }
            }
            catch// (SecurityException)
            {
                // The user does not have the permissions required to read from the registry key.
            }
            /*catch (UnauthorizedAccessException)
            {
                // The user does not have the necessary registry rights.
            }*/

            return _Result;
        }

        public void AdBlock(bool Boolean)
        {
            MainSave.Set("AdBlock", Boolean.ToString());
            if (!IsIEMode)
                _RequestHandler.AdBlock = Boolean;
        }
        public void TrackerBlock(bool Boolean)
        {
            MainSave.Set("TrackerBlock", Boolean.ToString());
            if (!IsIEMode)
                _RequestHandler.TrackerBlock = Boolean;
        }
        public void SetAutoSuggestions(bool Boolean)
        {
            SuggestionsDropdown.Visibility = Boolean ? Visibility.Visible : Visibility.Collapsed;
            MainSave.Set("AutoSuggestions", Boolean.ToString());
            if (IsIEMode)
                SuggestionsDropdown.Visibility = Visibility.Collapsed;
        }

        #region Actions
        public enum Actions
        {
            Undo = 0,
            Redo = 1,
            Refresh = 2,
            Create_Tab = 3,
            Print = 4,
            Source = 5,
            Inspector = 6,
            CloseTab = 7,
            Settings = 8,
            HTMLEditor = 9,
            Home = 10,
            Favourite = 11,
            Navigate = 12,
            FileExplorer = 13,
            DarkTheme = 14,
            Relaunch = 15,
            Reset = 16,
            ZoomIn = 17,
            ZoomOut = 18,
            ResetZoomLevel = 19,
            NewsFeed = 20,
            Pin = 21,
            ClosePrompt = 22,
            Prompt = 23,
            PromptNavigate = 24,
            Screenshot = 25,
            ReaderMode = 26,
        }
        private void Action(Actions _Action, object sender = null, bool Middle = false, string LastValue = "", string Value1 = "", string Value2 = "", string Value3 = "")
        {
            if (!IsProcessLoaded)
                return;
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
                case Actions.Create_Tab:
                    CreateBrowserTab("Empty00000");
                    /*if (IsIEMode)
                        CreateIETab(CreateIEWebBrowser());
                    else
                        CreateTab(CreateWebBrowser());*/
                    break;
                case Actions.Print:
                    Print();
                    break;
                case Actions.Source:
                    ViewSource();
                    break;
                case Actions.Inspector:
                    UseInspector();
                    break;
                case Actions.CloseTab:
                    /*if (sender != null && sender is MenuItem)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            //Control c = (Control)(((ContextMenu)((MenuItem)sender).Parent).PlacementTarget);
                            //throw new Exception(c.GetType().ToString());
                            TabItem SpecifiedTab = null;
                            //if (sender is MenuItem)
                            SpecifiedTab = (TabItem)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget; if (sender is MenuItem)
                                //else if (sender is Button)
                                //{
                                //    Control c = (Control)((Button)sender).Parent;
                                //    throw new Exception(c.GetType().ToString());
                                //}
                                CloseTab(SpecifiedTab);
                        }));
                    }
                    else*/
                    CloseTab();
                    break;
                case Actions.Settings:
                    Settings();
                    break;
                case Actions.HTMLEditor:
                    HTMLEditor();
                    break;
                case Actions.Home:
                    Home();
                    break;
                case Actions.Favourite:
                    Favourite();
                    break;
                case Actions.Navigate:
                    if (sender != null)
                        Navigate(Middle, Value1);
                    /*if (sender is Button)
                        Navigate(((Button)sender).Content.ToString());
                    else if (sender is MenuItem)
                        Navigate(((MenuItem)sender).Header.ToString());*/
                    break;
                case Actions.FileExplorer:
                    if (sender is MenuItem || sender is Button)
                        FileExplorer(Value1);
                    break;
                case Actions.DarkTheme:
                    break;
                case Actions.Relaunch:
                    Relaunch();
                    break;
                case Actions.Reset:
                    Reset();
                    break;
                case Actions.ZoomIn:
                    Reset();
                    break;
                case Actions.ZoomOut:
                    Reset();
                    break;
                case Actions.ResetZoomLevel:
                    Reset();
                    break;
                case Actions.NewsFeed:
                    NewsFeed();
                    break;
                case Actions.Pin:
                    //Pin(sender);
                    break;
                case Actions.ClosePrompt:
                    ClosePrompt(int.Parse(Value1));
                    break;
                case Actions.Prompt:
                    //NewMessage(Value1, Value2, Value3);
                    Prompt(false, Value1, true, Value2, Value3);
                    break;
                case Actions.PromptNavigate:
                    PromptNavigate(int.Parse(LastValue), Value1);
                    break;
                case Actions.Screenshot:
                    Screenshot();
                    break;
                case Actions.ReaderMode:
                    ReaderMode();
                    break;
            }
        }
        public void ReaderMode()
        {
            if (IsIEMode)
            {
                Prompt(false, "Reader mode is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            //if (!Utils.IsHttpScheme(_Browser.Address))
            //{
            //    Prompt(false, "Reader mode cannot be used on non-http urls.", false);
            //    return;
            //}
            /*try
            {
                _Browser.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('html')[0].innerHTML").ContinueWith(t =>
                {
                    if (t.Result != null && t.Result.Result != null)
                    {
                        var result = t.Result.Result.ToString();

                        var _Document = new HtmlDocument();
                        _Document.LoadHtml(result);

                        //sidebar, header, footer, pagetop, nav, toolbar
                        var headers = _Document.DocumentNode.SelectNodes("//header");
                        if (headers != null)
                        {
                            foreach (var tag in headers)
                                tag.Remove();
                        }
                        var footers = _Document.DocumentNode.SelectNodes("//footer");
                        if (footers != null)
                        {
                            foreach (var tag in footers)
                                tag.Remove();
                        }

                        var divs = _Document.DocumentNode.SelectNodes("//div");
                        if (divs != null)
                        {
                            foreach (var tag in divs)
                            {
                                if (tag.Attributes["class"] != null && string.Compare(tag.Attributes["class"].Value, "footer", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    tag.Remove();
                                }
                                else if (tag.Attributes["id"] != null && string.Compare(tag.Attributes["id"].Value, "footer", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    tag.Remove();
                                }
                                else if (tag.Attributes["class"] != null && string.Compare(tag.Attributes["class"].Value, "header", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    tag.Remove();
                                }
                                else if (tag.Attributes["id"] != null && string.Compare(tag.Attributes["id"].Value, "header", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    tag.Remove();
                                }
                            }
                        }

                        string s = _Document.DocumentNode.SelectSingleNode("//body").InnerText;
                        s = Regex.Replace(s, @"[^\u0000-\u007F]+", string.Empty);
                        s = Regex.Replace(s, @"\s\s+", " ");

                        //s = string.Join(
                        //    string.Empty,
                        //    s.Select((x, i) => (
                        //         char.IsUpper(x) && i > 0 &&
                        //         (char.IsUpper(s[i - 1]) || (i < s.Count() - 1 && char.IsLower(s[i + 1])))
                        //    ) ? "<br><br>" + x : x.ToString()));

                        string newtext = "</html></body></pre>" + s + "</pre></body></html>";
                        newtext = newtext.Replace(". ", ".<br>");
                        newtext = newtext.Replace(". ", ".<br>");
                        _Browser.LoadHtml(newtext);
                    }
                });
            }
            catch { }*/

            _Browser.Address = "slbr://underconstruction";

            /*string html = TinyDownloader.DownloadString(_Browser.Address);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string s = doc.DocumentNode.SelectSingleNode("//body").InnerText;
            string newtext = "</html></body></pre>" + s + "</pre></body></html>";
            newtext = newtext.Replace(". ", ".<br>");
            //string newtext = "</html></body></pre>\"" + Regex.Replace(html, @"<(.|\n)*?>", string.Empty) + "\"</pre></body></html>";
            _Browser.LoadHtml(newtext);*/
        }
        public async void Screenshot()
        {
            if (IsIEMode)
            {
                Prompt(false, "The screenshot feature is not supported on Internet Explorer mode.", false);
                return;
            }
            /*else if (!IsDeveloperMode)
            {
                Prompt(false, "The screenshot feature is still an experimental feature.", false);
                return;
            }*/
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            string ScreenshotPath = MainSave.Get("ScreenshotPath");
            if (!Directory.Exists(ScreenshotPath))
                Directory.CreateDirectory(ScreenshotPath);
            using (var _DevToolsClient = _Browser.GetDevToolsClient())
            {
                string Url = $"{Path.Combine(ScreenshotPath, Regex.Replace($"{_Browser.Title}_{DateTime.Now.ToString().Replace("/", "_").Replace(" ", "_").Replace(":", "_")}.jpg", "[^a-zA-Z0-9._]", String.Empty))}";
                var result = await _DevToolsClient.Page.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, null, null, null, false);
                File.WriteAllBytes(Url, result.Data);
                Navigate(true, "file:///////" + Url);
            }
        }
        public Prompt Prompt(bool CloseOnTabSwitch, string Content, bool IncludeButton = false, string ButtonContent = "", string ButtonArguments = "", string ToolTip = "", bool IncludeIcon = false, string IconText = "", string IconRotation = "")
        {
            int Count = Prompts.Count;
            Prompt _Prompt = new Prompt { CloseOnTabSwitch = CloseOnTabSwitch, Content = Content, ButtonVisibility = IncludeButton ? Visibility.Visible : Visibility.Collapsed, ButtonToolTip = ToolTip, ButtonContent = ButtonContent, ButtonTag = ButtonArguments + (ButtonArguments.StartsWith("24") ? $"<,>{Count}" : ""), CloseButtonTag = $"22<,>{Count}", IconVisibility = IncludeIcon ? Visibility.Visible : Visibility.Collapsed, IconText = IconText, IconRotation = IconRotation };
            Prompts.Add(_Prompt);
            return _Prompt;
        }
        public void PromptNavigate(int Index, string Url)
        {
            Navigate(false, Url);
            ClosePrompt(Index);
        }
        public void ClosePrompt(int Index)
        {
            if (Prompts.Count > 0 && Prompts[Index] != null)
            {
                Prompts.RemoveAt(Index);
                foreach (Prompt _Prompt in Prompts)
                {
                    _Prompt.CloseButtonTag = $"22<,>{Prompts.IndexOf(_Prompt)}";
                    if (_Prompt.ButtonTag.StartsWith("24"))
                        _Prompt.ButtonTag = Utils.RemoveCharsAfterLastChar(_Prompt.ButtonTag, "<,>", true) + Prompts.IndexOf(_Prompt).ToString();
                }
            }
        }
        public void ShowTabs(bool Toggle)
        {
            MainSave.Set("ShowTabs", Toggle.ToString());
            TabPanel _TabPanel = (TabPanel)Tabs.Template.FindName("HeaderPanel", Tabs);
            if (Toggle)
            {
                _TabPanel.Visibility = Visibility.Visible;
                if (_SettingsWindow != null)
                {
                    _SettingsWindow.Close();
                    //_SettingsWindow = null;
                    Settings();
                }
            }
            else
            {
                foreach (TabItem Item in Tabs.Items)
                {
                    if (Item.Name == "SLBrSettingsTab")
                    {
                        CloseTab(Item);
                        _SettingsWindow = new SettingsWindow();
                        _SettingsWindow.Show();
                        break;
                    }
                }
                _TabPanel.Visibility = Visibility.Collapsed;
            }
        }
        /*public void Pin(object sender)
        {
            TabItem Tab = GetCurrentTab();
            int TabIndex = Tabs.Items.IndexOf(Tab);
            string CleanedTag = Tab.Tag.ToString().Replace("<,>Unpinned", "").Replace("<,>Pinned", "");
            int Index = 0;
            try
            {
                Index = int.Parse(CleanedTag);
            }
            catch { Index = TabIndex; }
            Button _PinButton = (Button)sender;
            if (CleanedTag != TabIndex.ToString())
            {
                //_PinButton.Content = "\xE840";
                Tabs.Items.Remove(Tab);
                Tabs.Items.Insert(Index, Tab);
                Tab.IsSelected = true;
                //Tab.Tag = $"{TabIndex}<,>Unpinned";
            }
            else
            {
                //_PinButton.Content = "\xE77A";
                Tabs.Items.Remove(Tab);
                Tabs.Items.Insert(1, Tab);
                Tab.IsSelected = true;
                //Tab.Tag = $"{TabIndex}<,>Pinned";
            }
        }*/
        public void NewsFeed()
        {
            new NewsPage().Show();
        }
        public void ResetZoomLevel()
        {
            if (IsIEMode)
            {
                Prompt(false, "Zooming is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            _Browser.SetZoomLevel(0);
            _Browser.ZoomLevel = 0;
        }
        public void ZoomOut()
        {
            if (IsIEMode)
            {
                Prompt(false, "Zooming is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            _Browser.ZoomLevel -= _Browser.ZoomLevelIncrement;
        }
        public void ZoomIn()
        {
            if (IsIEMode)
            {
                Prompt(false, "Zooming is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            _Browser.ZoomLevel += _Browser.ZoomLevelIncrement;
        }
        public void Reset()
        {
            if (MessageBox.Show("Are you sure you want to reset everything? (Restarts Application after resetting)", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            foreach (Utils.Saving Save in Saves)
            {
                string _Path = Save.SaveFilePath;
                if (File.Exists(_Path))
                    File.Delete(_Path);
                Save.Clear();
            }
            Relaunch(false);
        }
        public void Relaunch(bool CallClosingEvent = true)
        {
            if (CallClosingEvent && MessageBox.Show("Are you sure you want to relaunch SLBr? (Everything will be saved)", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            else if (!CallClosingEvent)
                IsProcessLoaded = false;
            //Closing -= Window_Closing;
            Application.Current.Shutdown();
            Process.Start(Application.ResourceAssembly.Location);//DoNotUseIfUsingClickOnce
        }
        public void FileExplorer(string _Path)
        {
            //string FolderPath = System.IO.Path.GetDirectoryName(_Path);
            /*if (Directory.Exists(FolderPath))
            {*/
            _Path = Utils.CleanUrl(_Path).Replace("%20", "");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = "/select, \"" + _Path + "\"",
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
            /*}
            else
            {
                MessageBox.Show(string.Format("{0} Directory does not exist!", FolderPath));
            }*/
        }
        public void Navigate(bool NewTab, string Url)
        {
            if (IsIEMode)
            {
                if (NewTab)
                {
                    CreateIETab(CreateIEWebBrowser(Url), true, Tabs.SelectedIndex + 1);
                    return;
                }
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                _Browser.Navigate(Url);
            }
            else
            {
                if (NewTab)
                {
                    CreateChromeTab(CreateWebBrowser(Url), true, Tabs.SelectedIndex + 1, true);
                    return;
                }
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                _Browser.Address = Utils.FixUrl(Url, bool.Parse(MainSave.Get("Weblight")));
            }
        }
        public void Favourite()
        {
            string Url;
            string Title;
            bool IsLoaded;
            if (IsIEMode)
            {
                //Prompt(false, "The favourite feature is not supported on Internet Explorer mode.", false);
                //return;
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                Url = _Browser.Source.AbsoluteUri;
                Title = ((dynamic)_Browser.Document).Title;
                IsLoaded = _Browser.IsLoaded;
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                Url = _Browser.Address;
                Title = _Browser.Title;
                IsLoaded = _Browser.IsLoaded;
            }
            int FavouriteExistIndex = FavouriteExists(Url);
            if (FavouriteExistIndex != -1)
            {
                Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
                //Dispatcher.Invoke(delegate () { }, DispatcherPriority.Render);
            }
            else if (IsLoaded)
            {
                Favourites.Add(new Favourite { Name = Title, Arguments = $"12<,>{Url}", Address = Url });
                FavouriteButton.Content = "\xEB52";
            }
            if (Favourites.Count == 0 || !bool.Parse(MainSave.Get("FavouritesBar")))
                FavouriteContainer.Visibility = Visibility.Collapsed;
            else
                FavouriteContainer.Visibility = Visibility.Visible;
        }
        public void Home()
        {
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                _Browser.Navigate(MainSave.Get("Homepage"));
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                _Browser.Address = Utils.FixUrl(MainSave.Get("Homepage"), bool.Parse(MainSave.Get("Weblight")));
            }
        }
        public void HTMLEditor()
        {
            if (IsIEMode)
            {
                Prompt(false, "HTML Editor is not supported on Internet Explorer mode.", false);
                return;
            }
            CreateChromeTab(CreateWebBrowser("slbr://htmleditor"));
        }
        public void Settings()
        {
            //new SettingsWindow().ShowDialog();
            if (bool.Parse(MainSave.Get("ShowTabs")))
            {
                if (SettingsPage.Instance != null)
                {
                    foreach (TabItem Item in Tabs.Items)
                    {
                        if (Item.Name == "SLBrSettingsTab")
                        {
                            Tabs.SelectedItem = Item;
                            return;
                        }
                    }
                }

                //int Count = Tabs.Items.Count;
                Frame _Frame = new Frame();
                TabItem Tab = new TabItem()
                {
                    Name = "SLBrSettingsTab",
                    Header = "Settings",
                    Content = _Frame
                };
                _Frame.Content = new SettingsPage();
                Tabs.Items.Insert(Tabs.SelectedIndex + 1, Tab);
                //Tab.Tag = $"{Count}<,>Unpinned";
                Tabs.SelectedItem = Tab;
                /*AddressBox.Text = "SLBr Settings Tab";
                ReloadButton.Content = "\xE72C";
                WebsiteLoadingProgressBar.IsEnabled = false;
                WebsiteLoadingProgressBar.IsIndeterminate = false;*/
                _Frame.LoadCompleted += (sender, args) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        Tab.ApplyTemplate();
                        Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                        System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                        if (_DefaultTabIcon != null && _Image != null)
                        {
                            _DefaultTabIcon.Visibility = Visibility.Visible;
                            _Image.Visibility = Visibility.Collapsed;
                        }
                    }));
                };
            }
            else
            {
                //if (_SettingsWindow == null)
                //{
                if (SettingsPage.Instance != null)
                {
                    _SettingsWindow = new SettingsWindow();
                    _SettingsWindow.Show();
                }
                //}
                //else
                //    MessageBox.Show("An instance of the settings window is already running.");
            }
        }
        public void CloseTab(TabItem SpecifiedTab = null)
        {
            TabItem Tab;
            if (SpecifiedTab != null)
                Tab = SpecifiedTab;
            else
                Tab = GetCurrentTab();
            /*if (_MenuItem != null && !(_MenuItem.DataContext is TabItem))
                throw new Exception();*/
            if (Tabs.Items.Count == 1)
                return;
            //if (Tab.Name == "SLBrSettingsTab")
            //    SettingsPages--;
            //Tabs.SelectedIndex = PreviousTabIndex;
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser(Tab);
                if (_Browser != null)
                    _Browser.Dispose();
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser(Tab);
                if (_Browser != null)
                {
                    /*if (_Browser.BrowserSettings == null)
                    {
                        _Browser.BrowserSettings = new BrowserSettings
                        {
                            BackgroundColor = bool.Parse(MainSave.Get("DarkWebpage")) ? Utils.ColorToUInt(System.Drawing.Color.Black) : Utils.ColorToUInt(System.Drawing.Color.White)
                        };
                    }*/
                    Tab.Content = null;
                    //DisposeBrowser(_Browser);
                    //_Browser.Stop();
                    if (_Browser.BrowserCore != null)
                        _Browser.BrowserCore.CloseBrowser(true);
                    //_Browser.BrowserSettings = null;
                    //if (_Browser.IsBrowserInitialized)
                    _Browser.Dispose();
                }
            }
            Tabs.Items.Remove(Tab);
            //GC.Collect();
        }
        bool IsUtilityContainerOpen;
        public void UseInspector()
        {
            if (IsIEMode)
            {
                Prompt(false, "Developer Tools is not supported on Internet Explorer mode.", false);
                return;
            }
            IsUtilityContainerOpen = UtilityContainer.Visibility == Visibility.Visible;
            /*ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            _Browser.ShowDevTools();*/
            FrameworkElement _Element = (FrameworkElement)Tabs.SelectedContent;
            Thickness _Margin = _Element.Margin;
            if (IsUtilityContainerOpen)
            {
                _Margin.Right = 0;
                Inspector.Address = "about:blank";
            }
            else
            {
                _Margin.Right = 500;
                Inspector.Address = "localhost:8088/json/list";
            }
            _Element.Margin = _Margin;
            //Inspector.GetDevToolsClient().DeviceOrientation.ClearDeviceOrientationOverrideAsync();
            //--load-media-router-component-extension, 0
            UtilityContainer.Visibility = IsUtilityContainerOpen ? Visibility.Collapsed : Visibility.Visible;
            //InspectorSplitter.Visibility = DevToolsContainer.Visibility;
            IsUtilityContainerOpen = !IsUtilityContainerOpen;
            /*if (IsDevToolsOpen)
            {
                Grid.SetColumnSpan(Tabs, 2);
                DevToolsContainer.Children.Clear();
                _Browser.CloseDevTools();
                IsDevToolsOpen = false;
            }
            else
            {
                Grid.SetColumnSpan(Tabs, 1);
                IntPtr Handle = IntPtr.Zero;
                HwndSource _HwndSource = PresentationSource.FromVisual(DevToolsContainer) as HwndSource;
                if (_HwndSource != null)
                {
                    Handle = _HwndSource.Handle;
                }
                var _Rect = GetBoundingBox(DevToolsContainer, this);
                WindowInfo _WindowInfo = new WindowInfo();
                _WindowInfo.SetAsChild(Handle, Convert.ToInt32(_Rect.Left), Convert.ToInt32(_Rect.Top), Convert.ToInt32(_Rect.Right), Convert.ToInt32(_Rect.Bottom));
                _Browser.ShowDevTools(_WindowInfo);
                IsDevToolsOpen = true;
            }*/
        }
        public void ViewSource()
        {
            if (IsIEMode)
            {
                Prompt(false, "Viewing sources is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            CreateChromeTab(CreateWebBrowser($"view-source:{_Browser.Address}"), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
            //_Browser.ViewSource();
        }
        public void Print()
        {
            /*if (IsIEMode)
            {
                NewMessage("Printing is not supported on Internet Explorer mode.", false);
                return;
            }*/
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                _Browser.InvokeScript("eval", "document.execCommand('Print');");
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                _Browser.Print();
            }
        }
        public void Undo()
        {
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                if (_Browser.CanGoBack == true)
                    _Browser.GoBack();
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null || !_Browser.IsBrowserInitialized)
                    return;
                if (_Browser.CanGoBack == true)
                    _Browser.Back();
            }
        }
        public void Redo()
        {
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                if (_Browser.CanGoForward == true)
                    _Browser.GoForward();
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null || !_Browser.IsBrowserInitialized)
                    return;
                if (_Browser.CanGoForward == true)
                    _Browser.Forward();
            }
        }
        public void Refresh()
        {
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                if (_Browser.IsLoaded)
                    _Browser.Refresh();
                else
                    _Browser.InvokeScript("eval", "document.execCommand('Stop');");
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null || !_Browser.IsBrowserInitialized)
                    return;
                if (_Browser.IsLoaded)
                    _Browser.Reload();
                else
                    _Browser.Stop();
            }
        }
        #endregion
        public MenuItem CreateMenuItemForList(string Header, string Tag = "Empty00000", RoutedEventHandler? _RoutedEventHandler = null)
        {
            MenuItem _MenuItem = new MenuItem();
            _MenuItem.FontFamily = new FontFamily("Arial");
            //_MenuItem.Foreground = (SolidColorBrush)Resources["FontBrush"];//new SolidColorBrush(Colors.Black);
            _MenuItem.Header = Header;
            if (Tag != "Empty00000")
                _MenuItem.Tag = Tag;
            if (_RoutedEventHandler != null)
                _MenuItem.Click += _RoutedEventHandler;
            return _MenuItem;
        }
        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            Actions _Action;
            var Target = (FrameworkElement)sender;
            string _Tag = Target.Tag.ToString();
            var Values = _Tag.Split(new string[] { "<,>" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3//, "&lt;,&gt;"
            _Action = (Actions)int.Parse(Values[0]);
            string LastValue = Values.Last();
            Action(_Action, sender, false, LastValue, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }
        public void MiddleMouseButtonAction(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle && e.ButtonState != MouseButtonState.Pressed)
                return;
            Actions _Action;
            var Target = (FrameworkElement)sender;
            string _Tag = Target.Tag.ToString();
            var Values = _Tag.Split(new string[] { "<,>" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3//, "&lt;,&gt;"
            _Action = (Actions)int.Parse(Values[0]);
            string LastValue = Values.Last();
            Action(_Action, sender, true, LastValue, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }
        public void MiddleButtonAction(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;
            Actions _Action;
            var Target = (FrameworkElement)sender;
            string _Tag = Target.Tag.ToString();
            var Values = _Tag.Split(new string[] { "<,>" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3//, "&lt;,&gt;"
            _Action = (Actions)int.Parse(Values[0]);
            string LastValue = Values.Last();
            Action(_Action, sender, true, LastValue, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }

        public int Framerate;
        public CefState Javascript = CefState.Enabled;
        public CefState LoadImages = CefState.Enabled;
        public CefState LocalStorage = CefState.Enabled;
        public CefState Databases = CefState.Enabled;
        public CefState WebGL = CefState.Enabled;

        public void SetSingleSandbox(string ToSandbox, CefState State)
        {
            switch (ToSandbox)
            {
                case "JS":
                    Javascript = State;
                    break;
                case "LI":
                    LoadImages = State;
                    break;
                case "LS":
                    LocalStorage = State;
                    break;
                case "DB":
                    Databases = State;
                    break;
                case "WebGL":
                    WebGL = State;
                    break;
            }
            SandboxSave.Set(ToSandbox, State.ToBoolean().ToString());
            foreach (TabItem Tab in Tabs.Items)
            {
                ChromiumWebBrowser _Browser = GetBrowser(Tab);
                if (_Browser != null)
                {
                    string Url = _Browser.Address;
                    _Browser.Dispose();
                    Tab.Content = null;
                    ApplyToTab(CreateWebBrowser(Url), Tab, true);
                }
            }
            UnloadTabsTime = 0;
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
            SandboxSave.Set("JS", JSState.ToBoolean().ToString());
            SandboxSave.Set("LI", LIState.ToBoolean().ToString());
            SandboxSave.Set("LS", LSState.ToBoolean().ToString());
            SandboxSave.Set("DB", DBState.ToBoolean().ToString());
            SandboxSave.Set("WebGL", WebGLState.ToBoolean().ToString());
            foreach (TabItem Tab in Tabs.Items)
            {
                ChromiumWebBrowser _Browser = GetBrowser(Tab);
                if (_Browser != null)
                {
                    string Url = _Browser.Address;
                    _Browser.Dispose();
                    Tab.Content = null;
                    ApplyToTab(CreateWebBrowser(Url), Tab, true);
                }
            }
            UnloadTabsTime = 0;
        }

        public void CreateChromeTab(ChromiumWebBrowser _Browser, bool Focus = true, int Index = -1, bool NameByUrl = false)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                int Count = Tabs.Items.Count;
                if (Index > -1)
                    Count = Index;
                string TabName = NameByUrl ? Utils.CleanUrl(_Browser.Address).Replace("www.", "") : "New Tab";
                TabItem Tab = new TabItem()
                {
                    Header = $"{TabName}"
                };
                ApplyToTab(_Browser, Tab, true);
                Tabs.Items.Insert(Count/* - 1*/, Tab);
                //Tab.Tag = $"{Count}<,>Unpinned";
                Tab.IsSelected = Focus;
            }));
        }
        public void CreateIETab(WebBrowser _Browser, bool Focus = true, int Index = -1)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                int Count = Tabs.Items.Count;
                if (Index > -1)
                    Count = Index;
                string TabName = /*NameByUrl ? *//*_Browser.Source != null ? _Browser.Source.AbsoluteUri : */"New Tab";
                TabItem Tab = new TabItem()
                {
                    Header = $"{TabName}"
                };
                /*_Browser = */ConfigureIE(_Browser, Tab);
                //Tab.Background = Brushes.Transparent;
                //Tab.FontFamily = new FontFamily("Original");
                Tab.Content = _Browser;
                /*_Browser.SnapsToDevicePixels = true;
                _Browser.UseLayoutRounding = true;*/
                Tabs.Items.Insert(Count/* - 1*/, Tab);
                //Tab.Tag = $"{Count}<,>Unpinned";
                Tab.IsSelected = Focus;
                //Tab.UseLayoutRounding = true;
                Tab.SnapsToDevicePixels = true;
                //NewMessage("You are in SLBr developer mode, you have access to developer features of SLBr.", false);
                /*else if (InternetExplorerMode)
                    NewMessage("All sites in this tab will be opened in Internet Explorere mode.", false);*/
            }));
        }

        private class InspectorObject
        {
            //public string description { get; set; }
            public string devtoolsFrontendUrl { get; set; }
            public string id { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string url { get; set; }
            //public string webSocketDebuggerUrl { get; set; }
        }
        private void ConfigureInspectorBrowser()
        {
            Inspector.BrowserSettings = new BrowserSettings
            {
                WebGl = CefState.Disabled,
                WindowlessFrameRate = 20,
                BackgroundColor = Utils.ColorToUInt(System.Drawing.Color.Black),
            };
            Inspector.AllowDrop = true;
            Inspector.FrameLoadEnd += (sender, args) =>
            {
                if (args.Frame.IsValid && args.Frame.IsMain)
                {
                    //_Browser.GetDevToolsClient().Page.SetAdBlockingEnabledAsync(true);
                    /*Inspector.GetMainFrame().ExecuteJavaScriptAsync(@"console.log(""1"");" +
                        @"const htmlc = document.getElementsByTagName(""HTML"")[0];console.log(""2"");" +
                        @"console.log(htmlc);" +
                        //@"if (htmlc.classList[0] == '-theme-with-dark-background'){" +
                        //@"console.log(""3"");}" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"htmlc.classList.remove('-theme-with-dark-background');" +
                        @"console.log(htmlc.classList);"
                        );*/
                    //Inspector.GetMainFrame().ExecuteJavaScriptAsync("document.getElementById("MyElement").classList.toggle('MyClass');");
                    if (args.Url == "http://localhost:8088/json/list")
                    {
                        Inspector.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('body')[0].innerHTML").ContinueWith(t =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                if (t.Result != null && t.Result.Result != null)
                                {
                                    var result = t.Result.Result.ToString();

                                    var _Document = new HtmlDocument();
                                    _Document.LoadHtml(result);
                                    HtmlNode _Node = _Document.DocumentNode.SelectSingleNode("//pre");
                                    if (_Node != null)
                                    {
                                        string Content = _Node.InnerHtml;
                                        List<InspectorObject> InspectorObjects = JsonConvert.DeserializeObject<List<InspectorObject>>(Content);
                                        foreach (InspectorObject _InspectorObject in InspectorObjects)
                                        {
                                            //if (_InspectorObject.url == GetBrowser().Address)
                                            if (_InspectorObject.type == "page" && _InspectorObject.title == GetBrowser().Title)
                                            {
                                                Inspector.Address = "http://localhost:8088" + _InspectorObject.devtoolsFrontendUrl;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }));
                        });
                    }
                    else
                    {
                        if (MainSave.Get("Theme") == "Dark")
                            Inspector.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(bool.Parse(MainSave.Get("DarkWebpage")));
                        else
                            Inspector.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(false);
                    }
                }
            };
            Inspector.LifeSpanHandler = _LifeSpanHandler;
            Inspector.DownloadHandler = _DownloadHandler;
            Inspector.RequestHandler = _RequestHandler;
            //Inspector.MenuHandler = _ContextMenuHandler;
            Inspector.KeyboardHandler = _KeyboardHandler;
            Inspector.JsDialogHandler = _JsDialogHandler;
            Inspector.StatusMessage += OnWebBrowserStatusMessage;
            Inspector.ZoomLevelIncrement = 0.25;
            Inspector.AllowDrop = true;
        }

        public ChromiumWebBrowser CreateWebBrowser(string Url = "Empty00000")
        {
            if (Url == "Empty00000")
                Url = MainSave.Get("Homepage");
            ChromiumWebBrowser _Browser = new ChromiumWebBrowser(Url);//Configure(GetBrowserFromPool(), Url);
            RenderOptions.SetBitmapScalingMode(_Browser, BitmapScalingMode.LowQuality);//BUG: If the settings menu was opened, all would be blurry until the settings tab is closed
            _Browser.Address = Utils.FixUrl(Url, bool.Parse(MainSave.Get("Weblight")));
            return _Browser;
        }
        public WebBrowser CreateIEWebBrowser(string Url = "Empty00000")
        {
            if (Url == "Empty00000")
                Url = MainSave.Get("Homepage");
            WebBrowser _Browser = new WebBrowser();//Configure(GetBrowserFromPool(), Url);
            _Browser.Navigate(Url);
            return _Browser;
        }
        private void Configure(ChromiumWebBrowser _Browser, TabItem Tab/* = null*/)
        {
            _Browser.JavascriptObjectRepository.Register("slbr", _JSBindingHandler, BindingOptions.DefaultBinder);
            //_Browser.ExecuteScriptAsyncWhenPageLoaded(File.ReadAllText("Resources/JsBinding.js")/*, true*/);
            _Browser.TitleChanged += TitleChanged;
            _Browser.LoadingStateChanged += LoadingStateChanged;
            _Browser.AddressChanged += AddressChanged;
            // Enable touch scrolling - once properly tested this will likely become the default
            //_Browser.IsManipulationEnabled = true;
            _Browser.LifeSpanHandler = _LifeSpanHandler;
            _Browser.DownloadHandler = _DownloadHandler;
            _Browser.RequestHandler = _RequestHandler;
            _Browser.MenuHandler = _ContextMenuHandler;
            _Browser.KeyboardHandler = _KeyboardHandler;
            _Browser.JsDialogHandler = _JsDialogHandler;
            _Browser.StatusMessage += OnWebBrowserStatusMessage;
            _Browser.ZoomLevelIncrement = 0.25;
            _Browser.AllowDrop = true;
            /*_Browser.FrameLoadStart += (sender, args) =>
            {
                if (bool.Parse(MainSave.Get("LiteMode")))
                {
                    args.Frame.ExecuteJavaScriptAsync("let images = document.getElementsByTagName('img');" +
                                "let iframes = document.getElementsByTagName('iframe');" +
                                "for (var i = 0; i < images.length; i++) {" +
                                        //"if (!images[i].complete) {" +
                                        "images[i].setAttribute('loading', 'lazy');" +
                                // "}" +
                                "};" +
                                "for (var i = 0; i < iframes.length; i++) {" +
                                    "iframes[i].setAttribute('loading', 'lazy');" +
                                "};");
                    args.Frame.ExecuteJavaScriptAsync("window.addEventListener('DOMContentLoaded', (event) => {" +
                                "let images = document.getElementsByTagName('img');" +
                                "let iframes = document.getElementsByTagName('iframe');" +
                                "for (var i = 0; i < images.length; i++) {" +
                                        //"if (!images[i].complete) {" +
                                        "images[i].setAttribute('loading', 'lazy');" +
                                // "}" +
                                "};" +
                                "for (var i = 0; i < iframes.length; i++) {" +
                                    "iframes[i].setAttribute('loading', 'lazy');" +
                                "};" +
                            "});");
                }
            };*/
            _Browser.FrameLoadEnd += (sender, args) =>
            {
                if (args.Frame.IsValid && args.Frame.IsMain)
                {
                    //_Browser.GetDevToolsClient().Page.SetAdBlockingEnabledAsync(true);
                    /*Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        args.Frame.ExecuteJavaScriptAsync("const addCSS = s => document.head.appendChild(document.createElement('style')).innerHTML=s;" +
                        "addCSS(\'::-webkit-scrollbar-thumb:hover{background:" + (Resources["ControlFontBrush"] as SolidColorBrush).Color.ToString() + ";}\');" +
                        "addCSS(\'::-webkit-scrollbar-thumb{background:" + (Resources["BorderBrush"] as SolidColorBrush).Color.ToString() + ";}\');" +
                        "addCSS(\'::-webkit-scrollbar-track{background:" + (Resources["PrimaryBrush"] as SolidColorBrush).Color.ToString() + ";}\');");
                    }));*/
                    string ArgsUrl = args.Url;
                    if (!IsInformationSet)
                    {
                        var Response = args.Browser.GetDevToolsClient().Browser.GetVersionAsync();
                        JavascriptVersion = Response.Result.JsVersion;
                        /*Revision = Response.Result.Revision;
                        if (Revision.StartsWith("@"))
                            Revision = Revision.Substring(1);*/
                        UserAgent = Response.Result.UserAgent;
                        IsInformationSet = true;
                    }
                    /*if (bool.Parse(MainSave.Get("LiteMode")))
                    {
                        _Browser.GetMainFrame().ExecuteJavaScriptAsync("let images = document.getElementsByTagName('img');" +
                                "let iframes = document.getElementsByTagName('iframe');" +
                                "for (var i = 0; i < images.length; i++) {" +
                                    //"if (!images[i].complete) {" +
                                    "images[i].setAttribute('loading', 'lazy');" +
                                //"}" +
                                "};" +
                                "for (var i = 0; i < iframes.length; i++) {" +
                                    "iframes[i].setAttribute('loading', 'lazy');" +
                                "};");
                    }*/
                    if (ArgsUrl == "slbr://version/")
                    {
                        args.Frame.ExecuteJavaScriptAsync($"document.getElementById(\"_version\").innerHTML = \"{ReleaseVersion}\";" +
                            $"document.getElementById(\"bit_process\").innerHTML = \"({Bitness}-bit ARM)\";" +
                            $"document.getElementById(\"build_type\").innerHTML = \"({_BuildType} Build)\";" +
                            $"document.getElementById(\"chromium_version\").innerHTML = \"{ChromiumVersion}\";" +
                            //$"document.getElementById(\"_revision\").innerHTML = \"{Revision}\";" +
                            //"document.getElementById(\"_os_type\").innerHTML = \"Windows\";" +
                            $"document.getElementById(\"js_version\").innerHTML = \"{JavascriptVersion}\";" +
                            //$"document.getElementById(\"_useragent\").innerHTML = \"{UserAgent}\";" +
                            $"document.getElementById(\"_command_line\").innerHTML = '{"\"" + ExecutableLocation + "\""}';" +
                            $"document.getElementById(\"_executable_path\").innerHTML = \"{ExecutableLocation.Replace("dll", "exe")}\";" +
                            $"document.getElementById(\"_cache_path\").innerHTML = \"{CachePath.Replace("\\", "\\\\")}\";");
                    }
                    //_Browser.GetDevToolsClient().DeviceOrientation.ClearDeviceOrientationOverrideAsync();
                    // && _Browser.GetDevToolsClient().Network.GetResponseBodyAsync("content-type").Result.Bod
                    //Dark theme cannot be applied in "text/plain, application/json, image/svg+xml"
                    if (args.Browser.IsLoading)
                    {
                        DevToolsClient _DevToolsClient = _Browser.GetDevToolsClient();
                        if (MainSave.Get("Theme") == "Dark")
                        {
                            //_DevToolsClient.Network.EmulateNetworkConditionsAsync(true, 250, 270, 300);
                            _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(bool.Parse(MainSave.Get("DarkWebpage")));
                            //_Browser.GetDevToolsClient().Emulation.SetDefaultBackgroundColorOverrideAsync(Cef.ColorSetARGB(255, 0, 0, 0));
                        }
                        else
                        {
                            _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(false);
                            //_Browser.GetDevToolsClient().Emulation.SetDefaultBackgroundColorOverrideAsync();
                        }
                        if (IsDeveloperMode)
                        {
                            bool ShowPerformanceMetrics = bool.Parse(MainSave.Get("ShowPerformanceMetrics"));
                            //show-performance-metrics-hud
                            _DevToolsClient.Overlay.SetShowWebVitalsAsync(ShowPerformanceMetrics);
                            _DevToolsClient.Overlay.SetShowAdHighlightsAsync(ShowPerformanceMetrics);
                            _DevToolsClient.Overlay.SetShowFPSCounterAsync(ShowPerformanceMetrics);
                            //_DevToolsClient.CSS.GetMediaQueriesAsync();
                            //_DevToolsClient.Emulation.SetCPUThrottlingRateAsync;
                        }
                    }
                    //_Browser.GetDevToolsClient().Emulation.SetCPUThrottlingRateAsync
                    //if (bool.Parse(MainSave.Get("DarkTheme")))
                    //{
                    /*args.Frame.ExecuteJavaScriptAsync("document.documentElement.style.setProperty('filter', 'invert(100%)');" +
                                "const addCSS = s => document.head.appendChild(document.createElement('style')).innerHTML=s;" +
                                "addCSS('iframe, img, svg, video{ filter:invert(100%); }');" +
                                "const all = document.querySelectorAll('*');" +
                                "const restricted = ['iframe', 'img', 'svg', 'video'];" +
                                "all.forEach(tags => {" +
                                    "if (!restricted.includes(tags.tagName.toLowerCase()))" +
                                    "{" +
                                        "if (getComputedStyle(tags).backgroundImage != 'none')" +
                                        "{" +
                                            "tags.style.filter = 'invert(100%)';" +
                                        "}" +
                                    "}" +
                                "});");*/

                    /*args.Frame.ExecuteJavaScriptAsync("document.documentElement.setAttribute('data-theme', 'light');" +
                        "document.documentElement.classList.add(\"light\")" +
                        "document.documentElement.setAttribute('data-force-color-mode', 'light');" +
                        "if (document.documentElement.classList.contains(\"dark\"))" +
                        "document.documentElement.classList.remove(\"dark\")" + 
                        "localStorage.setItem('theme', 'light');" +
                        "localStorage.setItem('color-mode', 'light');");*/
                    /*args.Frame.ExecuteJavaScriptAsync("function detectColorScheme()" +
                        "{" +
                            "var theme = \"light\";" +
                            "if (localStorage.getItem(\"theme\"))" +
                            "{" +
                                "if (localStorage.getItem(\"theme\") == \"dark\")" +
                                "{" +
                                    "var theme = \"dark\";" +
                                "}" +
                            "}" +
                            "else if (!window.matchMedia)" +
                            "{" +
                                "return false; " +
                            "}" +
                            "else if (window.matchMedia(\"(prefers-color-scheme: dark)\").matches) " +
                            "{" +
                                "var theme = \"dark\";" +
                            "}" +
                            "if (theme == \"dark\")" +
                            "{" +
                                "document.documentElement.setAttribute(\"data-theme\", \"dark\");" +
                            "}" +
                        "}" +
                        "detectColorScheme();");*/

                    //args.Frame.ExecuteJavaScriptAsync("localStorage.setItem('theme', 'dark');" +
                    //"document.documentElement.setAttribute('data-theme', 'dark'); ");

                    //args.Frame.ExecuteJavaScriptAsync("document.styleSheets[0].cssRules[0].conditionText = \"(prefers-color-scheme: light)\";");
                    //args.Frame.ExecuteJavaScriptAsync("document.styleSheets[0].insertRule(\"(prefers-color-scheme: light)\", 0);");
                    //}
                    int HttpStatusCode = args.HttpStatusCode;
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (string.IsNullOrEmpty(_Browser.Address))
                            _Browser.Address = Utils.FixUrl(MainSave.Get("Homepage"), bool.Parse(MainSave.Get("Weblight")));
                        if (CanChangeAddressBox())
                            AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? _Browser.Address : Utils.CleanUrl(_Browser.Address);
                        if (HttpStatusCode == 404 && !Utils.CleanUrl(ArgsUrl).StartsWith("web.archive.org"))
                            Prompt(true, "This page is missing, do you want to check if there's a saved version on the Wayback Machine?", true, "Check for saved version", $"24<,>https://web.archive.org/{ArgsUrl}", $"https://web.archive.org/{ArgsUrl}", true, "\xF142");
                        /*else if (Utils.CleanUrl(ArgsUrl).EndsWith(".png") || Utils.CleanUrl(ArgsUrl).EndsWith(".jpeg") || Utils.CleanUrl(ArgsUrl).EndsWith(".webp"))
                            Prompt(true, "You are viewing a image, don't mistake it for a webpage.", false);*/
                        else
                            CloseClosableMessages();
                        try
                        {
                            string Host = Utils.Host(_Browser.Address);//new Uri(_Browser.Address).Host;
                            if (bool.Parse(MainSave.Get("FindSearchProvider")) && !SearchProviderUrls.Contains(Host) && !SearchEngines.Select(item => Utils.Host(_Browser.Address)/*new Uri(item).Host*/).Contains(Host))
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    try
                                    {
                                        _Browser.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('head')[0].innerHTML").ContinueWith(t =>
                                        {
                                            if (t.Result != null && t.Result.Result != null)
                                            {
                                                var result = t.Result.Result.ToString();

                                                string Url = string.Empty;
                                                var _Document = new HtmlDocument();
                                                _Document.LoadHtml(result);
                                                HtmlNode _Node = _Document.DocumentNode.SelectSingleNode("//link[contains(@type, 'application/opensearchdescription+xml')]");
                                                if (_Node != null)
                                                {
                                                    Url = _Node.Attributes["href"].Value;
                                                    SearchProviderUrls.Add(Host);
                                                    if (!Url.StartsWith("//") && Url.StartsWith("/"))
                                                        Url = "http://" + Host + Url;
                                                    else if (Url.StartsWith("//"))
                                                        Url = "http://" + Url.Substring(2);
                                                    if (MessageBox.Show("Set as default search provider?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                                                    {
                                                        if (Path.GetExtension(Url) == ".xml")
                                                        {
                                                            string Content = TinyDownloader.DownloadString(Url);
                                                            _Document.LoadHtml(Content);
                                                            _Node = _Document.DocumentNode.SelectSingleNode("//opensearchdescription").SelectSingleNode("//url");
                                                            string _SearchUrl = _Node.Attributes["template"].Value.Replace("searchTerms", "0");
                                                            SearchEngines.Add(_SearchUrl);
                                                            MainSave.Set("Search_Engine", _SearchUrl);
                                                        }
                                                    }
                                                }
                                            }
                                        });
                                    }
                                    catch { }
                                }));
                            }
                        }
                        catch { }
                        Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                        System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                        if (_DefaultTabIcon != null && _Image != null)
                        {
                            try
                            {
                                var bytes = TinyDownloader.DownloadData("https://www.google.com/s2/favicons?domain=" + Utils.Host(_Browser.Address));//new Uri(_Browser.Address).Host);
                                var ms = new MemoryStream(bytes);

                                var bi = new BitmapImage();
                                bi.BeginInit();
                                bi.StreamSource = ms;
                                bi.EndInit();

                                _DefaultTabIcon.Visibility = Visibility.Collapsed;
                                _Image.Visibility = Visibility.Visible;
                                _Image.Source = bi;
                            }
                            catch
                            {
                                _DefaultTabIcon.Visibility = Visibility.Visible;
                                _Image.Visibility = Visibility.Collapsed;
                            }
                        }
                        if (_Browser.IsLoaded && IsUtilityContainerOpen && Inspector.Address == "http://localhost:8088/json/list")
                            Inspector.Address = "localhost:8088/json/list";

                        string CurrentAddress = Utils.CleanUrl(_Browser.Address, false, true);
                        CurrentAddress = CurrentAddress.Replace("www.", "");
                        if (CurrentAddress.StartsWith("youtu"))
                            VideoPopoutWindow.Instance.SetUp("", 0, VideoPopoutWindow.VideoProvider.Youtube);
                        //_Browser.ExecuteScriptAsync("function C(d,o){v=d.createElement('div');o.parentNode.replaceChild(v,o);}function A(d){for(j=0;t=[\", Interaction.IIf(browser.Address.Contains(\"youtube.com\"), \"'iframe','marquee'\", \"'iframe','embed','marquee'\")), \"][j];++j){o=d.getElementsByTagName(t);for(i=o.length-1;i>=0;i--)C(d,o[i]);}g=d.images;for(k=g.length-1;k>=0;k--)if({'21x21':1,'48x48':1,'60x468':1,'88x31':1,'88x33':1,'88x62':1,'90x30':1,'90x32':1,'90x90':1,'100x30':1,'100x37':1,'100x45':1,'100x50':1,'100x70':1,'100x100':1,'100x275':1,'110x50':1,'110x55':1,'110x60':1,'110x110':1,'120x30':1,'120x60':1,'120x80':1,'120x90':1,'120x120':1,'120x163':1,'120x181':1,'120x234':1,'120x240':1,'120x300':1,'120x400':1,'120x410':1,'120x500':1,'120x600':1,'120x800':1,'125x40':1,'125x60':1,'125x65':1,'125x72':1,'125x80':1,'125x125':1,'125x170':1,'125x250':1,'125x255':1,'125x300':1,'125x350':1,'125x400':1,'125x600':1,'125x800':1,'126x110':1,'130x60':1,'130x65':1,'130x158':1,'130x200':1,'132x70':1,'140x55':1,'140x350':1,'145x145':1,'146x60':1,'150x26':1,'150x60':1,'150x90':1,'150x100':1,'150x150':1,'155x275':1,'155x470':1,'160x80':1,'160x126':1,'160x600':1,'180x30':1,'180x66':1,'180x132':1,'180x150':1,'194x165':1,'200x60':1,'220x100':1,'225x70':1,'230x30':1,'230x33':1,'230x60':1,'234x60':1,'234x68':1,'240x80':1,'240x300':1,'250x250':1,'275x60':1,'280x280':1,'300x60':1,'300x100':1,'300x250':1,'320x50':1,'320x70':1,'336x280':1,'350x300':1,'350x850':1,'360x300':1,'380x112':1,'380x250':1,'392x72':1,'400x40':1,'400x50':1,'425x600':1,'430x225':1,'440x40':1,'464x62':1,'468x16':1,'468x60':1,'468x76':1,'468x120':1,'468x248':1,'470x60':1,'480x400':1,'486x60':1,'545x90':1,'550x5':1,'600x30':1,'720x90':1,'720x300':1,'725x90':1,'728x90':1,'734x96':1,'745x90':1,'750x25':1,'750x100':1,'750x150':1,'850x120':1}[g[k].width+'x'+g[k].height])C(d,g[k]);}A(document);for(f=0;z=frames[f];++f)A(z.document)");
                    }));
                }
            };
            /*_Browser.RequestContext = new RequestContext(_RequestContextSettings);
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                _Browser.RequestContext.SetPreference("enable_do_not_track", true, out _);
                _Browser.RequestContext.SetPreference("browser.enable_do_not_track", true, out _);
                //var success = requestContext.SetPreference("enable_do_not_track", true, out errorMessage);
                //if (!success)
                //{
                //    this.InvokeOnUiThreadIfRequired(() => MessageBox.Show("Unable to set preference enable_do_not_track errorMessage: " + errorMessage));
                //}
                //_Browser.RequestContext.SetPreference("browser.enable_spellchecking", true, out _);
                //_Browser.RequestContext.SetPreference("spellcheck.dictionaries", new List<object> { "en-US" }, out _);
            });*/
            //_Browser.RenderHandler = new CompositionTargetRenderHandler(_Browser, _Browser.DpiScaleFactor, _Browser.DpiScaleFactor);
            //CefSharp.Wpf.Experimental.Compo
            _Browser.BrowserSettings = new BrowserSettings
            {
                Javascript = Javascript,
                ImageLoading = LoadImages,
                LocalStorage = LocalStorage,
                Databases = Databases,
                WebGl = WebGL,
                WindowlessFrameRate = Framerate,
                BackgroundColor = bool.Parse(MainSave.Get("DarkWebpage")) ? Utils.ColorToUInt(System.Drawing.Color.Black) : Utils.ColorToUInt(System.Drawing.Color.White)
            };
            //return _Browser;
        }
        private void ConfigureIE(WebBrowser _Browser, TabItem Tab/* = null*/)
        {
            _Browser.LoadCompleted += delegate (object sender, NavigationEventArgs e) { IE_LoadCompleted(sender, e, Tab); };
            //_Browser.Navigated += (object sender, NavigationEventArgs e) => { IE_Navigated(_Browser, e, Tab); };
            _Browser.Loaded += (object sender, RoutedEventArgs e) => { SuppressIEScriptErrors(_Browser, true); };
            //_Browser.Navigating += webBrowser_Navigating;
            //return _Browser;
        }
        void BrowserChanged(object sender, bool IsSwitchTab = false)
        {
            if (IsIEMode)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    WebBrowser CurrentBrowser = GetIEBrowser();
                    if (CurrentBrowser == null || CurrentBrowser.Source == null)
                        return;
                    if (sender is WebBrowser)
                    {
                        WebBrowser _Browser = sender as WebBrowser;
                        if (CurrentBrowser != _Browser)
                            return;
                    }
                    if (string.IsNullOrEmpty(CurrentBrowser.Source.AbsoluteUri))
                        CurrentBrowser.Navigate(Utils.FixUrl(MainSave.Get("Homepage"), bool.Parse(MainSave.Get("Weblight"))));
                    if (CanChangeAddressBox())
                        AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? CurrentBrowser.Source.AbsoluteUri : Utils.CleanUrl(CurrentBrowser.Source.AbsoluteUri);
                    AddressBox.Tag = CurrentBrowser.Source.AbsoluteUri;
                    /*bool ContinueUrlCheck = true;
                    if (CurrentBrowser.Address.StartsWith("http") || CurrentBrowser.Address.StartsWith("file:"))
                    {
                        string Host = Utils.Host(CurrentBrowser.Address);
                        if (CurrentBrowser.Address.StartsWith("https:"))
                        {
                            SSLSymbol.Text = "\xE72E";
                            SSLToolTip.Content = $"{Host} has a valid SSL certificate";
                            ContinueUrlCheck = false;
                        }
                        else if (CurrentBrowser.Address.StartsWith("http:"))
                        {
                            SSLSymbol.Text = "\xE785";
                            SSLToolTip.Content = $"{Host} doesn't have a valid SSL certificate";
                            ContinueUrlCheck = false;
                        }
                        else if (CurrentBrowser.Address.StartsWith("file:"))
                        {
                            SSLSymbol.Text = "\xE8B7";
                            SSLToolTip.Content = $"Local or shared file";
                            ContinueUrlCheck = false;
                        }
                    }
                    if (ContinueUrlCheck)
                    {
                        if (Utils.IsInteralProtocol(CurrentBrowser.Address))
                        {
                            SSLSymbol.Text = "\xE8BE";
                            SSLToolTip.Content = $"Secure SLBr Page";
                        }
                        else if (Utils.IsProtocolNotHttp(CurrentBrowser.Address))
                        {
                            SSLSymbol.Text = "\xE72E";
                            SSLToolTip.Content = $"{CurrentBrowser.Address} is a protocol and is considered secure";
                        }
                        else
                        {
                            SSLSymbol.Text = "\xE783";
                            SSLToolTip.Content = $"Unknown";
                        }
                    }*/
                    if (!IsSwitchTab)
                    {
                        //CurrentBrowser.Reload();
                        ReloadButton.Content = CurrentBrowser.IsLoaded ? "\xE711" : "\xE72C";
                        WebsiteLoadingProgressBar.IsEnabled = CurrentBrowser.IsLoaded;
                        WebsiteLoadingProgressBar.IsIndeterminate = CurrentBrowser.IsLoaded;
                        /*if (CurrentBrowser.IsLoaded)
                        {
                            ReloadButton.Content = "\xE711";
                            WebsiteLoadingProgressBar.IsEnabled = true;
                            WebsiteLoadingProgressBar.IsIndeterminate = true;
                        }
                        else
                        {
                            ReloadButton.Content = "\xE72C";
                            WebsiteLoadingProgressBar.IsEnabled = false;
                            WebsiteLoadingProgressBar.IsIndeterminate = false;
                        }*/
                        /*if (bool.Parse(MainSave.Get("BlockKeywords")) && MainSave.Get("BlockedKeywords").Length > 0)
                        {
                            string[] BlockedKeywords = MainSave.Get("BlockedKeywords").Split(',');
                            bool ContainsKeyword = false;
                            for (int i = 0; i < BlockedKeywords.Length; i++)
                            {
                                ContainsKeyword = CurrentBrowser.Source.AbsoluteUri.ToLower().Contains(BlockedKeywords[i]) || (CurrentBrowser.Title != null && CurrentBrowser.Title.ToLower().Contains(BlockedKeywords[i]));
                                if (ContainsKeyword)
                                {
                                    CurrentBrowser.Navigate(Utils.FixUrl(MainSave.Get("BlockRedirect"), bool.Parse(MainSave.Get("Weblight"))));
                                    break;
                                }
                            }
                        }*/
                    }
                    else
                    {
                        BackButton.IsEnabled = CurrentBrowser.CanGoBack;
                        ForwardButton.IsEnabled = CurrentBrowser.CanGoForward;
                    }
                    if (FavouriteExists(AddressBox.Tag.ToString()) != -1)
                        FavouriteButton.Content = "\xEB52";
                    else
                        FavouriteButton.Content = "\xEB51";
                    if (HistoryListMenuItem.Items.Count > 0 && ((MenuItem)HistoryListMenuItem.Items[0]).Header.ToString() == CurrentBrowser.Source.AbsoluteUri)
                        return;
                    string Address = Utils.FixUrl(CurrentBrowser.Source.AbsoluteUri, bool.Parse(MainSave.Get("Weblight")));
                    //if (Address.Contains("googleweblight.com/?lite_url=") && bool.Parse(MainSave.Get("Weblight")))
                    //    Address = Address.Replace("googleweblight.com/?lite_url=", "");
                    MenuItem _HistoryMenuItem = CreateMenuItemForList(Address, $"12<,>{Address}", new RoutedEventHandler(ButtonAction));
                    HistoryListMenuItem.Items.Insert(0, _HistoryMenuItem);
                    //if (HistoryListMenuItem.Items.Contains(CurrentBrowser.Address))
                    //{
                    //    HistoryListMenuItem.Items.RemoveAt(HistoryListMenuItem.Items.IndexOf(CurrentBrowser.Address));
                    //}
                    if (HistoryListMenuItem.Items.Count > 25)
                        HistoryListMenuItem.Items.RemoveAt(25);
                }));
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                /*TabItem Tab = Tabs.SelectedItem as TabItem;
                if (Tab == null)
                    return;*/
                ChromiumWebBrowser CurrentBrowser = GetBrowser();
                if (CurrentBrowser == null)
                    return;
                if (sender is ChromiumWebBrowser)
                {
                    ChromiumWebBrowser _Browser = sender as ChromiumWebBrowser;
                    if (CurrentBrowser != _Browser)
                        return;
                }
                if (string.IsNullOrEmpty(CurrentBrowser.Address))
                    CurrentBrowser.Address = Utils.FixUrl(MainSave.Get("Homepage"), bool.Parse(MainSave.Get("Weblight")));
                if (CanChangeAddressBox())
                    AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? CurrentBrowser.Address : Utils.CleanUrl(CurrentBrowser.Address);
                AddressBox.Tag = CurrentBrowser.Address;
                bool ContinueUrlCheck = true;
                if (CurrentBrowser.Address.StartsWith("http") || CurrentBrowser.Address.StartsWith("file:"))
                {
                    //MessageBox.Show(CurrentBrowser.Address);
                    string Host = Utils.Host(CurrentBrowser.Address);//new Uri(CurrentBrowser.Address).Host;//Cef.ParseUrl(CurrentBrowser.Address)
                    if (CurrentBrowser.Address.StartsWith("https:"))
                    {
                        SSLSymbol.Text = "\xE72E";
                        SSLToolTip.Content = $"{Host} has a valid SSL certificate";
                        ContinueUrlCheck = false;
                    }
                    else if (CurrentBrowser.Address.StartsWith("http:"))
                    {
                        SSLSymbol.Text = "\xE785";
                        SSLToolTip.Content = $"{Host} doesn't have a valid SSL certificate";
                        ContinueUrlCheck = false;
                    }
                    else if (CurrentBrowser.Address.StartsWith("file:"))
                    {
                        SSLSymbol.Text = "\xE8B7";//E8A5//E8B7//E7C3
                        SSLToolTip.Content = $"Local or shared file";
                        ContinueUrlCheck = false;
                    }
                }
                if (ContinueUrlCheck)
                {
                    if (Utils.IsInteralProtocol(CurrentBrowser.Address))
                    {
                        SSLSymbol.Text = "\xE8BE";
                        SSLToolTip.Content = $"Secure SLBr Page";
                    }
                    else if (Utils.IsProtocolNotHttp(CurrentBrowser.Address))
                    {
                        SSLSymbol.Text = "\xE72E";
                        SSLToolTip.Content = $"{CurrentBrowser.Address} is a protocol and is considered secure";
                    }
                    else
                    {
                        SSLSymbol.Text = "\xE783";
                        SSLToolTip.Content = $"Unknown";
                    }
                }
                if (!IsSwitchTab)
                {
                    //CurrentBrowser.Reload();
                    if (CurrentBrowser.IsLoading)
                    {
                        ReloadButton.Content = "\xE72C";
                        WebsiteLoadingProgressBar.IsEnabled = false;
                        WebsiteLoadingProgressBar.IsIndeterminate = false;
                    }
                    else
                    {
                        ReloadButton.Content = "\xE711";
                        WebsiteLoadingProgressBar.IsEnabled = true;
                        WebsiteLoadingProgressBar.IsIndeterminate = true;
                    }
                    if (bool.Parse(MainSave.Get("BlockKeywords")) && MainSave.Get("BlockedKeywords").Length > 0)
                    {
                        string[] BlockedKeywords = MainSave.Get("BlockedKeywords").Split(',');
                        bool ContainsKeyword = false;
                        for (int i = 0; i < BlockedKeywords.Length; i++)
                        {
                            ContainsKeyword = CurrentBrowser.Address.ToLower().Contains(BlockedKeywords[i]) || (CurrentBrowser.Title != null && CurrentBrowser.Title.ToLower().Contains(BlockedKeywords[i]));
                            if (ContainsKeyword)
                            {
                                CurrentBrowser.Address = Utils.FixUrl(MainSave.Get("BlockRedirect"), bool.Parse(MainSave.Get("Weblight")));
                                break;
                            }
                        }
                    }
                }
                else
                {
                    BackButton.IsEnabled = CurrentBrowser.CanGoBack;
                    ForwardButton.IsEnabled = CurrentBrowser.CanGoForward;
                    if (IsUtilityContainerOpen)
                        Inspector.Address = "localhost:8088/json/list";
                }
                if (FavouriteExists(AddressBox.Tag.ToString()) != -1)
                    FavouriteButton.Content = "\xEB52";
                else
                    FavouriteButton.Content = "\xEB51";
                if (HistoryListMenuItem.Items.Count > 0 && ((MenuItem)HistoryListMenuItem.Items[0]).Header.ToString() == CurrentBrowser.Address)
                    return;
                string Address = Utils.FixUrl(CurrentBrowser.Address, bool.Parse(MainSave.Get("Weblight")));
                //if (Address.Contains("googleweblight.com/?lite_url=") && bool.Parse(MainSave.Get("Weblight")))
                //    Address = Address.Replace("googleweblight.com/?lite_url=", "");
                MenuItem _HistoryMenuItem = CreateMenuItemForList(Address, $"12<,>{Address}", new RoutedEventHandler(ButtonAction));
                HistoryListMenuItem.Items.Insert(0, _HistoryMenuItem);
                //if (HistoryListMenuItem.Items.Contains(CurrentBrowser.Address))
                //{
                //    HistoryListMenuItem.Items.RemoveAt(HistoryListMenuItem.Items.IndexOf(CurrentBrowser.Address));
                //}
                if (HistoryListMenuItem.Items.Count > 25)
                    HistoryListMenuItem.Items.RemoveAt(25);
            }));
        }
        private void UnloadTab(TabItem Tab, bool IgnoreIfSound = false)
        {
            ChromiumWebBrowser _Browser = GetBrowser(Tab);
            if (_Browser != null && _Browser.IsBrowserInitialized)
            {
                if (IgnoreIfSound)//PROBLEM: This checks if the address is a known music website. I need help on detecting sound.
                {
                    string CleanedAddress = Utils.CleanUrl(_Browser.Address);
                    if (_Browser.GetBrowser().CanGoBack || (CleanedAddress.Contains("youtube.com/watch")
                        || CleanedAddress.Contains("meet.google.com/")
                        || CleanedAddress.Contains("spotify.com/track/")
                        || CleanedAddress.Contains("soundcloud.com")
                        || CleanedAddress.Contains("dailymotion.com/video/")
                        || CleanedAddress.Contains("vimeo.com")
                        || CleanedAddress.Contains("twitch.tv/")
                        || CleanedAddress.Contains("bitchute.com/video/")
                        || CleanedAddress.Contains("ted.com/talks/"))
                        )
                    {
                        return;
                    }
                }
                string Url = _Browser.Address;
                _Browser.Dispose();
                Tab.Content = null;
                ApplyToTab(CreateWebBrowser(Url), Tab, true);
            }
        }
        private void ApplyToTab(ChromiumWebBrowser _Browser, TabItem Tab, bool Configure)
        {
            if (Configure)
                /*_Browser = */
                this.Configure(_Browser, Tab);
            Tab.Content = _Browser;
        }

        #region IE
        private void IE_Loaded(object sender, RoutedEventArgs e)
        {
            var _Browser = sender as WebBrowser;
            _Browser.Loaded -= IE_Loaded;
            SuppressIEScriptErrors(_Browser, true);
        }
        private void SuppressIEScriptErrors(WebBrowser _Browser, bool Hide)
        {
            if (!_Browser.IsLoaded)
            {
                _Browser.Loaded += IE_Loaded; // in case we are too early
                return;
            }
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(_Browser);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
            /*var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            var objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null)
            {
                wb.Loaded += (o, s) => HideIEScriptErrors(wb, hide); //In case we are to early
                return;
            }
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });*/
        }
        private void IE_Navigated(WebBrowser _Browser, NavigationEventArgs e, TabItem _Tab)
        {
        }
        private void IE_LoadCompleted(object sender, NavigationEventArgs e, TabItem Tab)
        {
            WebBrowser _Browser = sender as WebBrowser;
            string ArgsUrl = e.Uri.AbsoluteUri;
            IsInformationSet = true;
            //int HttpStatusCode = e.WebResponse.ContentType;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Tab.Header = bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri);
                if (AddressBox.Text != (bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri)) && _Browser == GetIEBrowser())
                {
                    if (CanChangeAddressBox())
                        AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri);
                    AddressBox.Tag = e.Uri.AbsoluteUri;
                }
                Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                /*if (_DefaultTabIcon != null && _Image != null)
                {
                    try
                    {
                        var bytes = TinyDownloader.DownloadData("https://www.google.com/s2/favicons?domain=" + e.Uri.Host);
                        var ms = new MemoryStream(bytes);

                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.StreamSource = ms;
                        bi.EndInit();

                        _DefaultTabIcon.Visibility = Visibility.Collapsed;
                        _Image.Visibility = Visibility.Visible;
                        _Image.Source = bi;
                    }
                    catch
                    {
                        _DefaultTabIcon.Visibility = Visibility.Visible;
                        _Image.Visibility = Visibility.Collapsed;
                    }
                }*/
                _DefaultTabIcon.Visibility = Visibility.Visible;
                BackButton.IsEnabled = _Browser.CanGoBack;
                ForwardButton.IsEnabled = _Browser.CanGoForward;
            }));
        }
        #endregion
        #region CEF
        private void OnWebBrowserStatusMessage(object sender, StatusMessageEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                StatusBar.Visibility = string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible;
                StatusMessage.Text = e.Value;
            }));
        }
        private void TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                ChromiumWebBrowser _Browser = (ChromiumWebBrowser)sender;
                TabItem Tab = GetCurrentTab();
                ChromiumWebBrowser CurrentBrowser = GetBrowser();
                if (CurrentBrowser != _Browser)
                    return;
                Tab.Header = _Browser.Title.Trim().Length > 0 ? _Browser.Title : Utils.CleanUrl(_Browser.Address);
                if (_Browser.Title.ToLower().Contains("cefsharp.browsersubprocess.exe"))
                {
                    Prompt(true, "CefSharp.BrowserSubprocess.exe is made by CEFSharp, which is a wrapper around the Chromium Embedded Framework, and is a reputable project. A significant number of applications uses it, including SLBr. The process is therefore safe.", true, "Open", "12<,>https://removefile.com/cefsharp-browsersubprocess-exe/");
                }
            }));
        }
        private void AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                //ChromiumWebBrowser _Browser = (ChromiumWebBrowser)sender;
                TabItem Tab = GetCurrentTab();
                ChromiumWebBrowser CurrentBrowser = GetBrowser();
                //if (CurrentBrowser != _Browser)
                //    AddressBox.Tag = _Browser.Address;
                AddressBox.Tag = CurrentBrowser.Address;
            }));
        }
        private void LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                BackButton.IsEnabled = e.CanGoBack;
                ForwardButton.IsEnabled = e.CanGoForward;
            }));
            BrowserChanged(sender);
        }
        #endregion

        public TabItem GetTab(IWebBrowser _Browser)
        {
            foreach (TabItem _Tab in Tabs.Items)
            {
                if (_Tab != null && GetBrowser(_Tab) == _Browser)
                    return _Tab;
            }
            return null;
        }
        private TabItem GetCurrentTab()
        {
            TabItem Tab = Tabs.SelectedItem as TabItem;
            return Tab;
        }
        private ChromiumWebBrowser GetBrowser(TabItem Tab = null)
        {
            if (Tab == null)
                Tab = GetCurrentTab();
            ChromiumWebBrowser _Browser = Tab.Content as ChromiumWebBrowser;
            /*if (_Browser == null)
                return null;*/
            return _Browser;
        }
        private WebBrowser GetIEBrowser(TabItem Tab = null)
        {
            if (Tab == null)
                Tab = GetCurrentTab();
            WebBrowser _Browser = Tab.Content as WebBrowser;
            /*if (_Browser == null)
                return null;*/
            return _Browser;
        }

        int FavouriteExists(string Url)
        {
            int ToReturn = -1;
            string[] FavouriteUrls = Favourites.Select(item => item.Arguments.Replace("12<,>", "")).ToArray();
            for (int i = 0; i < FavouriteUrls.Length; i++)
            {
                if (FavouriteUrls[i] == Url)
                    ToReturn = i;
            }
            return ToReturn;
        }
        bool CanCloseMessage(int Index)
        {
            Prompt _Message = Prompts[Index];
            return _Message.CloseOnTabSwitch;
            /*return (_Message.ButtonContent != null && _Message.ButtonContent == "Check for saved version"
            || _Message.ButtonContent == "Set as default search provider?"
            || _Message.ButtonContent == "Open in file explorer")
            || (_Message.Content != null && _Message.Content.StartsWith("You are viewing a image"));*/
        }
        void CloseClosableMessages()
        {
            for (int i = 0; i < Prompts.Count; i++)
            {
                if (CanCloseMessage(i))
                    ClosePrompt(i);
            }
        }
        #region AddressBox
        private bool CanChangeAddressBox()
        {
            string Text = AddressBox.Text.Trim();
            return !AddressBoxFocused || !Text.Contains(" ");
        }

        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsIEMode)
            {
                WebBrowser _IEBrowser = GetIEBrowser();
                if (_IEBrowser == null)
                    return;
                if (e.Key == Key.Enter && AddressBox.Text.Trim().Length > 0)
                {
                    try
                    {
                        if (AddressBox.Text.StartsWith("search:"))
                            _IEBrowser.Navigate(string.Format(MainSave.Get("Search_Engine"), AddressBox.Text.Substring(7).Trim().Replace(" ", "+")));
                        else if (AddressBox.Text.StartsWith("mailto:"))
                            Process.Start(AddressBox.Text);
                        else if (AddressBox.Text.Contains("."))
                        {
                            if (AddressBox.Text.StartsWith("https://") || AddressBox.Text.StartsWith("http://"))
                                _IEBrowser.Navigate(AddressBox.Text.Trim().Replace(" ", string.Empty));
                            else
                                _IEBrowser.Navigate("http://" + AddressBox.Text.Trim().Replace(" ", string.Empty));
                        }
                        else
                            _IEBrowser.Navigate(string.Format(MainSave.Get("Search_Engine"), AddressBox.Text.Trim().Replace(" ", "+")));
                    }
                    catch { }
                    return;
                }
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            if (AddressBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Enter)
                {
                    /*if (AddressBox.Text.StartsWith("search:"))
                        _Browser.Address = Utils.FixUrl(string.Format(MainSave.Get("Search_Engine"), AddressBox.Text.Substring(7).Trim().Replace(" ", "+")), bool.Parse(MainSave.Get("Weblight")));
                    else if (AddressBox.Text.StartsWith("mailto:"))
                        Process.Start(AddressBox.Text);
                    else if (IsChromiumMode && AddressBox.Text.StartsWith("chrome:"))
                        _Browser.Address = AddressBox.Text;
                    else if (!IsChromiumMode && AddressBox.Text.StartsWith("cef:"))
                        _Browser.Address = "chrome:" + AddressBox.Text.Substring(4);
                    else if (Utils.IsSystemUrl(AddressBox.Text))
                        _Browser.Address = AddressBox.Text;
                    else if (AddressBox.Text.Contains("."))
                    {
                        if (AddressBox.Text.StartsWith("https://") || AddressBox.Text.StartsWith("http://"))
                            _Browser.Address = Utils.FixUrl(AddressBox.Text.Trim().Replace(" ", string.Empty), bool.Parse(MainSave.Get("Weblight")));
                        else
                            _Browser.Address = Utils.FixUrl("http://" + AddressBox.Text.Trim().Replace(" ", string.Empty), bool.Parse(MainSave.Get("Weblight")));
                    }
                    else
                        _Browser.Address = string.Format(MainSave.Get("Search_Engine"), AddressBox.Text.Trim().Replace(" ", "+"));
                    if (AddressBox.Text.ToLower().Contains("cefsharp.browsersubprocess"))
                        MessageBox.Show("cefsharp.browsersubprocess is necessary for the browser engine to function accordingly.");*/
                    string Url = Utils.FilterUrlForBrowser(AddressBox.Text, MainSave.Get("Search_Engine"), bool.Parse(MainSave.Get("Weblight"))/*, IsChromiumMode*/);
                    if (!Utils.IsProgramUrl(Url))
                        _Browser.Address = Url;
                }
                else if (bool.Parse(MainSave.Get("AutoSuggestions")) && (e.Key >= Key.A && e.Key <= Key.Z)/* || (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)*/ && Utils.CheckForInternetConnection(100))
                {
                    SuggestionsTimer.Stop();
                    SuggestionsTimer.Start();
                    //SetSuggestions();
                }
            }
        }
        private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddressBox.Text == (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString())))
                    AddressBox.Text = AddressBox.Tag.ToString();
            }
            catch { }
            AddressBoxFocused = true;
        }
        private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!AddressBoxMouseEnter)
            {
                try
                {
                    if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                        AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString());
                }
                catch { }
            }
            AddressBoxFocused = false;
        }
        private void AddressBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                try
                {
                    if (AddressBox.Text == (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString())))
                        AddressBox.Text = AddressBox.Tag.ToString();
                }
                catch { }
            }
            AddressBoxMouseEnter = true;
        }
        private void AddressBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                try
                {
                    if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                        AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString());
                }
                catch { }
            }
            AddressBoxMouseEnter = false;
        }
        #endregion
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            if (FindTextBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Enter)
                    _Browser.Find(FindTextBox.Text, true, false, true);
            }
            else
            {
                _Browser.StopFinding(true);
            }
        }
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsProcessLoaded)
            {
                CloseClosableMessages();
                BrowserChanged(sender, true);
                TabItem Tab = (TabItem)Tabs.SelectedItem;
                if (Tab != null)
                {
                    /*if (Tab.Tag != null && Tab.Tag.ToString().Contains("Unpinned"))
                        Tab.Tag = Tabs.Items.IndexOf(Tab);*/
                    if (Tab.Name == "SLBrSettingsTab")
                    {
                        if (IsUtilityContainerOpen)
                            UseInspector();
                        AddressBox.Text = string.Empty;
                    }
                    else
                    {
                        //UnloadTabsTime = 0;
                        //PreviousTabIndex = Tabs.SelectedIndex;
                        FrameworkElement _Element = (FrameworkElement)Tab.Content;
                        Thickness _Margin = _Element.Margin;
                        if (IsUtilityContainerOpen)
                            _Margin.Right = 500;
                        else
                            _Margin.Right = 0;
                        _Element.Margin = _Margin;
                        if (PreviousTab != null && PreviousTab != GetCurrentTab())
                        {
                            if (!IsIEMode && bool.Parse(MainSave.Get("VideoPopout")))
                            {
                                var PreviousBrowser = GetBrowser(PreviousTab);
                                if (PreviousBrowser != null)
                                {
                                    string CleanAddress = Utils.CleanUrl(PreviousBrowser.Address, false, true);
                                    string CurrentAddress = Utils.CleanUrl(GetBrowser(Tab).Address, false, true);

                                    CleanAddress = CleanAddress.Replace("www.", "");
                                    CurrentAddress = CurrentAddress.Replace("www.", "");

                                    if (CleanAddress.StartsWith("youtu"))
                                    {
                                        int ToRemoveIndex = CleanAddress.LastIndexOf("&");
                                        if (ToRemoveIndex >= 0)
                                            CleanAddress = CleanAddress.Substring(0, ToRemoveIndex);

                                        CleanAddress = CleanAddress.Replace("youtube.com/watch?v=", "");
                                        CleanAddress = CleanAddress.Replace("youtu.be/", "");
                                        CleanAddress = CleanAddress.Replace("youtube.com/v/", "");
                                        if (CurrentAddress.StartsWith("youtu"))
                                            VideoPopoutWindow.Instance.SetUp("", 0, VideoPopoutWindow.VideoProvider.Youtube);
                                        else
                                            VideoPopoutWindow.Instance.SetUp(CleanAddress, 0, VideoPopoutWindow.VideoProvider.Youtube);
                                    }
                                    else
                                    {
                                        /*int CurrentToRemoveIndex = CurrentAddress.LastIndexOf("&");
                                        if (CurrentToRemoveIndex >= 0)
                                            CurrentAddress = CurrentAddress.Substring(0, CurrentToRemoveIndex);*/
                                        if (CurrentAddress.StartsWith("youtu"))
                                            VideoPopoutWindow.Instance.SetUp("", 0, VideoPopoutWindow.VideoProvider.Youtube);
                                    }
                                }
                            }
                        }
                    }
                    PreviousTab = Tab;
                }
            }
        }
        TabItem PreviousTab;
        private void FavouriteScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            FavouriteScrollViewer.ScrollToHorizontalOffset(FavouriteScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
        }
        DispatcherTimer SuggestionsTimer;
        private void SetSuggestions()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Suggestions.Clear();
                /*for (int i = 0; i < Suggestions.Count; i++)
                {
                    Suggestions.RemoveAt(i);
                }*/
                //SuggestionsMenu.Items.Clear();
                //https://suggestqueries.google.com/complete/search?client=chrome&q=
                //http://suggestqueries.google.com/complete/search?output=firefox&q=
                if (!string.IsNullOrWhiteSpace(AddressBox.Text))
                {
                    string TextToScan = Utils.CleanUrl(AddressBox.Text)/*.Replace("www.", "")*/;
                    var url = new Uri("https://suggestqueries.google.com/complete/search?client=chrome&q=" + TextToScan);
                    //var url = new Uri("https://canary.discord.com/api/v10/gifs/suggest?q=" + TextToScan);
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    var response = (HttpWebResponse)request.GetResponse();
                    var responseText = (new StreamReader(response.GetResponseStream())).ReadToEnd();
                    var items = (from each in responseText.Split(',')
                                 select each.Trim('[', ']', '\"', ':', '{', '}')).ToArray<string>();
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (i < 6)
                        {
                            if (items[i].Contains(AddressBox.Text))
                            {
                                Suggestions.Add(new Favourite { Name = items[i] });
                                //var _MenuItem = new MenuItem();
                                //_MenuItem.FontFamily = new FontFamily();
                                //_MenuItem.Header = items[i];
                                //SuggestionsMenu.Items.Add(_MenuItem);
                            }
                        }
                    }
                }
            }));
        }
    }
}