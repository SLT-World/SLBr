// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Net;
using CefSharp.Wpf;
using CefSharp;
using CefSharp.SchemeHandler;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using HtmlAgilityPack;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //bool IsDevToolsOpen;
        #region Start
        public enum BuildType
        {
            Offical,/*Standard*/
            Developer
        }

        public static MainWindow Instance;

        private static Guid FolderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

        public static string GetDownloadsPath()
        {
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
            IntPtr pathPtr = IntPtr.Zero;
            try
            {
                SHGetKnownFolderPath(ref FolderDownloads, 0, IntPtr.Zero, out pathPtr);
                return Marshal.PtrToStringUni(pathPtr);
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
        };
        public string GoogleWeblightUserAgent = "Mozilla/5.0 (Linux; Android 4.2.1; en-us; Nexus 5 Build/JOP40D) AppleWebKit/535.19 (KHTML, like Gecko; googleweblight) Chrome/38.0.1025.166 Mobile Safari/535.19 SLBr/2022.2.22";
        public List<string> SearchEngines;

        public Utils.Saving MainSave;
        public Utils.Saving FavouriteSave;
        public Utils.Saving SearchEnginesSave;
        public Utils.Saving TabsSave;
        public Utils.Saving ATSADSEUrlsSave;

        public int SettingsPages;

        string SchemeName = "slbr";
        string SchemeFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");

        List<string> SchemeDomainNames = new List<string> { "Urls", "Blank", "DevToolsTests", "NewTab", "License", "About"/*, "Dino", "Plans"*/, "SLBrUADetector", "Version", "WhatsNew"/*, "HTMLEditor"*/, "CannotConnect", "CdmSupport", "Copy_Icon.svg", "Malware", "Deception", "SLBr-Urls" };
        //public List<string> SchemeDomainUrls = new List<string>();

        public List<string> ATSADSEUrls = new List<string>();

        List<string> SaveNames = new List<string> { "Save.bin", "Favourites.bin", "SearchEngines.bin", "Tabs.bin", "ATSADSEUrls.bin" };
        List<Utils.Saving> Saves = new List<Utils.Saving>();

        public string ReleaseVersion;

        string ReleaseYear = "2022";
        string ReleaseMonth = "3";
        string ReleaseDay = "13";

        bool IsInformationSet;
        public string ChromiumVersion;
        public string ExecutableLocation;
        public string UserAgent;
        public string JavascriptVersion;
        //public string Revision;
        public string BitProcess;
        public BuildType _BuildType;

        public string ProxyServer = "127.0.0.1:8088";//http://

        public string ApplicationDataPath;
        public string CachePath;
        public string LogPath;
        public int RemoteDebuggingPort = 8088;

        public bool AddressBoxFocused;
        public bool AddressBoxMouseEnter;

        LifeSpanHandler _LifeSpanHandler;
        DownloadHandler _DownloadHandler;
        RequestHandler _RequestHandler;
        ContextMenuHandler _ContextMenuHandler;
        KeyboardHandler _KeyboardHandler;
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

        //public List<Prompt> Prompts = new List<Prompt>();
        //public ObservableCollection<Prompt> PromptsBinding { get { return new ObservableCollection<Prompt>(Prompts); } }

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

        public MainWindow()
        {
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
                    default:
                        NewTabUrl = /*"file:\\\\\\" + */Args[1];
                        break;
                }
                /*else
                    //if (File.Exists(Args[1]))
                    NewTabUrl = Args[1];*/
            }
            if (!IsChromiumMode || !IsIEMode)
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
            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLT World", "SLBr");
            //if (!IsPrivateMode)
            CachePath = Path.Combine(ApplicationDataPath, "Cache");
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
            BitProcess = Environment.Is64BitProcess ? "64" : "36";
            ChromiumVersion = Cef.ChromiumVersion;

            if (!IsIEMode)
            {
                InitializeCEF();

                _LifeSpanHandler = new LifeSpanHandler();
                _DownloadHandler = new DownloadHandler();
                _RequestHandler = new RequestHandler();
                _ContextMenuHandler = new ContextMenuHandler();
                _KeyboardHandler = new KeyboardHandler();
                _JSBindingHandler = new JSBindingHandler();
                InitializeKeyboardHandler();
                _SafeBrowsing = new Utils.SafeBrowsing(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"), Environment.GetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID"));
            }
            /*else
                InitializeIE();*/

            MainSave = Saves[0];
            FavouriteSave = Saves[1];
            SearchEnginesSave = Saves[2];
            TabsSave = Saves[3];
            ATSADSEUrlsSave = Saves[4];
            ExecutableLocation = Assembly.GetExecutingAssembly().Location.Replace("\\", "\\\\");
            TinyDownloader = new WebClient();
        }
        public void InitializeCEF()
        {
            /*if (!File.Exists(LogFile))
                File.Create(LogFile).Close();*/
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            Cef.EnableHighDPISupport();
            var settings = new CefSettings();
            //settings.BackgroundColor = Cef.ColorSetARGB(0, 255, 255, 255);
            //CefSharpSettings.ConcurrentTaskExecution = true; //true
            //CefSharpSettings.FocusedNodeChangedEnabled = true;
            //CefSharpSettings.WcfEnabled = true; //true

            //settings.CefCommandLineArgs.Add("enable-widevine");
            //settings.CefCommandLineArgs.Add("enable-widevine-cdm");

            settings.CommandLineArgsDisabled = true;
            settings.LogFile = LogPath;
            settings.LogSeverity = LogSeverity.Warning;
            settings.CachePath = CachePath;
            settings.CefCommandLineArgs.Add("enable-print-preview");//Only for Non-OSR
            settings.RemoteDebuggingPort = RemoteDebuggingPort;
            
            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-speech-input");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capture");
            settings.CefCommandLineArgs.Add("enable-features", "PdfUnseasoned");
            //settings.CefCommandLineArgs.Add("enable-viewport");
            //settings.CefCommandLineArgs.Add("enable-features", "CastMediaRouteProvider,NetworkServiceInProcess");

            //settings.CefCommandLineArgs.Add("disable-extensions");
            //settings.CefCommandLineArgs.Add("enable-logging"); //Enable Logging for the Renderer process (will open with a cmd prompt and output debug messages - use in conjunction with setting LogSeverity = LogSeverity.Verbose;)
            settings.CefCommandLineArgs.Add("ignore-certificate-errors");

            //OSR Performance
            //settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("disable-direct-composition");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync");
            //settings.CefCommandLineArgs.Add("disable-gpu");
            settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
            settings.CefCommandLineArgs.Add("off-screen-frame-rate", "30");

            //settings.CefCommandLineArgs.Add("proxy-server", ProxyServer);
            settings.CefCommandLineArgs.Add("debug-plugin-loading");
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");
            //settings.CefCommandLineArgs.Add("allow-running-insecure-content");
            settings.CefCommandLineArgs.Add("no-proxy-server");
            settings.CefCommandLineArgs.Add("disable-pinch");
            settings.CefCommandLineArgs.Add("disable-features", "WebUIDarkMode");/*,TouchpadAndWheelScrollLatching,AsyncWheelEvents*/
            //settings.CefCommandLineArgs["disable-features"] += ",SameSiteByDefaultCookies";//Cross Site Request

            settings.CefCommandLineArgs.Add("renderer-process-limit", "1");
            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");
            settings.CefCommandLineArgs.Add("disable-threaded-scrolling");
            settings.CefCommandLineArgs.Add("disable-smooth-scrolling");
            settings.CefCommandLineArgs.Add("disable-surfaces");
            settings.CefCommandLineArgs.Remove("process-per-tab");
            settings.CefCommandLineArgs.Add("disable-site-isolation-trials");

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files");
            //settings.CefCommandLineArgs.Add("disable-features=IsolateOrigins,process-per-tab,site-per-process,process-per-site");
            //settings.CefCommandLineArgs.Add("process-per-site");
            //settings.CefCommandLineArgs.Remove("process-per-site");
            //settings.CefCommandLineArgs.Remove("site-per-process");
            //settings.CefCommandLineArgs.Add("process-per-site-instance");

            //settings.CefCommandLineArgs.Add("disable-3d-apis", "1");
            settings.CefCommandLineArgs.Add("disable-low-res-tiling");
            //settings.CefCommandLineArgs.Add("disable-direct-write");
            //settings.CefCommandLineArgs.Add("allow-sandbox-debugging");
            //settings.CefCommandLineArgs.Add("webview-sandboxed-renderer");
            settings.CefCommandLineArgs.Add("js-flags", "max_old_space_size=1024,lite_mode");
            //settings.CefCommandLineArgs.Add("no-experiments");
            //settings.CefCommandLineArgs.Add("no-vr-runtime");
            //settings.CefCommandLineArgs.Add("in-process-gpu");//The --in-process-gpu option will run the GPU process as a thread in the main browser process. These processes consume most of the CPU time and the GPU driver crash will likely crash the whole browser, so you probably don't wanna use it.

            /*settings.CefCommandLineArgs.Add("flag-switches-begin");
            settings.CefCommandLineArgs.Add("flag-switches-end");*/

            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");

            //Enables Uncaught exception handler
            //settings.UncaughtExceptionStackSize = 10;
            if (!IsChromiumMode)
                settings.UserAgentProduct = $"SLBr/{ReleaseVersion} Chromium/{ChromiumVersion}";
            //settings.CefCommandLineArgs.Remove("mute-audio");
            //else
            //    settings.UserAgentProduct = $"Chromium/{ChromiumVersion}";
            //settings.ChromeRuntime = true;
            //settings.CefCommandLineArgs.Add("enable-chrome-runtime");//https://bitbucket.org/chromiumembedded/cef/issues/2969/support-chrome-windows-with-cef-callbacks
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
            //CefSharpSettings.FocusedNodeChangedEnabled = true;
            //CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            //Cef.EnableHighDPISupport();
            //Load pepper flash player
            //settings.CefCommandLineArgs.Add("ppapi-flash-path", System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"pepflashplayer.dll");
            foreach (string Name in SchemeDomainNames)
            {
                string NewName = $"{Name}.html";
                if (Name.Contains("."))
                    NewName = Name;
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = SchemeName,
                    DomainName = Name.ToLower(),
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                        rootFolder: SchemeFolder,
                        hostName: Name,
                        defaultPage: NewName
                    ),
                    IsSecure = true
                });
                //SchemeDomainUrls.Add($"{SchemeName}://{Name.ToLower()}");
            }
            Cef.Initialize(settings);
        }
        /*public void InitializeIE()
        {
            RegistryKey Regkey = null;
            try
            {
                // For 64 bit machine
                if (Environment.Is64BitOperatingSystem)
                    Regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", true);
                else  //For 32 bit machine
                    Regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", true);

                // If the path is not correct or
                // if the user haven't priviledges to access the registry
                if (Regkey == null)
                {
                    MessageBox.Show("Application Settings Failed - Address Not found");
                    return;
                }

                string FindAppkey = Convert.ToString(Regkey.GetValue("SLBr"));

                // Check if key is already present
                if (FindAppkey == "11001")
                {
                    MessageBox.Show("Required Application Settings Present");
                    Regkey.Close();
                    return;
                }

                // If a key is not present add the key, Key value 8000 (decimal)
                if (string.IsNullOrEmpty(FindAppkey))
                    Regkey.SetValue("SLBr", unchecked((int)0x1F40), RegistryValueKind.DWord);

                // Check for the key after adding
                FindAppkey = Convert.ToString(Regkey.GetValue("SLBr"));

                if (FindAppkey == "11001")
                    MessageBox.Show("Application Settings Applied Successfully");
                else
                    MessageBox.Show("Application Settings Failed, Ref: " + FindAppkey);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Application Settings Failed");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                // Close the Registry
                if (Regkey != null)
                    Regkey.Close();
            }
        }*/
        public void InitializeKeyboardHandler()
        {
            _KeyboardHandler.AddKey(Refresh, (int)System.Windows.Forms.Keys.R, true);
            _KeyboardHandler.AddKey(delegate() { CreateTab(CreateWebBrowser()); }, (int)System.Windows.Forms.Keys.T, true);
            _KeyboardHandler.AddKey(CloseTab, (int)System.Windows.Forms.Keys.W, true);
            _KeyboardHandler.AddKey(Screenshot, (int)System.Windows.Forms.Keys.S, true);
            _KeyboardHandler.AddKey(Refresh, (int)System.Windows.Forms.Keys.F5);
            _KeyboardHandler.AddKey(DevTools, (int)System.Windows.Forms.Keys.F12);
        }

        /*using (var devToolsClient = browser.GetDevToolsClient())
{
    //Get the content size
    var layoutMetricsResponse = await devToolsClient.Page.GetLayoutMetricsAsync();
    var contentSize = layoutMetricsResponse.ContentSize;

    var viewPort = new Viewport()
    {
        Height= contentSize.Height,
        Width = contentSize.Width,
        X = 0,
        Y = 0,
        Scale = 1 
    };

    // https://bugs.chromium.org/p/chromium/issues/detail?id=1198576#c17
    var result = await devToolsClient.Page.CaptureScreenshotAsync(clip: viewPort, fromSurface:true, captureBeyondViewport: true);

    return result.Data;
}v*/
        bool IsProcessLoaded;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    //if (!MainSave.Has("AssociationsSet"))
                    //{
                    //    FileAssociations.EnsureAssociationsSet();
                    //MainSave.Set("AssociationsSet", true.ToString());
                    //}
                    if (!MainSave.Has("Search_Engine"))
                        MainSave.Set("Search_Engine", SearchEngines[0]);
                    if (!MainSave.Has("Homepage"))
                        MainSave.Set("Homepage", "slbr://newtab"/*Utils.FixUrl(new Uri(SearchEngines[0]).Host, false)*/);
                    if (!MainSave.Has("DarkTheme"))
                        MainSave.Set("DarkTheme", false.ToString());
                    else
                        Action(Actions.DarkTheme, null, "", MainSave.Get("DarkTheme"));
                    if (!MainSave.Has("FullAddress"))
                        MainSave.Set("FullAddress", false.ToString());
                    if (!MainSave.Has("BlockKeywords"))
                        MainSave.Set("BlockKeywords", true.ToString());
                    if (!MainSave.Has("BlockedKeywords"))
                        MainSave.Set("BlockedKeywords", "roblox,gacha");
                    if (!MainSave.Has("BlockRedirect"))
                        MainSave.Set("BlockRedirect", MainSave.Get("Homepage"));
                    if (!MainSave.Has("RestoreTabs"))
                        MainSave.Set("RestoreTabs", false.ToString());
                    if (!MainSave.Has("AFDP"))
                        MainSave.Set("AFDP", true.ToString());
                    if (!MainSave.Has("ATSADSE"))
                        MainSave.Set("ATSADSE", true.ToString());
                    if (!MainSave.Has("DownloadPath"))
                        MainSave.Set("DownloadPath", GetDownloadsPath());
                    if (!MainSave.Has("SelectedTabIndex"))
                        MainSave.Set("SelectedTabIndex", 1.ToString());
                    if (!MainSave.Has("HideTabs"))
                        MainSave.Set("HideTabs", false.ToString());
                    if (!MainSave.Has("Weblight"))
                        MainSave.Set("Weblight", false.ToString());
                    if (!MainSave.Has("TabUnloading"))
                        MainSave.Set("TabUnloading", true.ToString());
                    /*if (!MainSave.Has("IsI5Processor"))
                    {
                        if (Utils.GetProcessorID().Contains("i5"))
                        {
                            MainSave.Set("IsI5Processor", true.ToString());
                        }
                        else
                            MainSave.Set("IsI5Processor", false.ToString());
                    }*/
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
                                        string Url = TabsSave.Get($"Tab_{i}");
                                        //if (!SearchEngines.Contains(Url))
                                        bool IsSelected = false;
                                        if (Index == i && MainSave.Has("UsedBefore"))
                                            IsSelected = true;
                                        if (IsIEMode)
                                            CreateIETab(CreateIEWebBrowser(Url), IsSelected);
                                        else
                                            CreateTab(CreateWebBrowser(Url), IsSelected);
                                    }
                                    if (Args.Length > 1 && File.Exists(Args[1]))
                                    {
                                        if (IsIEMode)
                                            CreateIETab(CreateIEWebBrowser(NewTabUrl));
                                        else
                                            CreateTab(CreateWebBrowser(NewTabUrl));
                                    }
                                }));
                            }
                            else
                            {
                                if (IsIEMode)
                                    CreateIETab(CreateIEWebBrowser(NewTabUrl));
                                else
                                    CreateTab(CreateWebBrowser(NewTabUrl));
                            }
                        }
                        else
                        {
                            if (IsIEMode)
                                CreateIETab(CreateIEWebBrowser(NewTabUrl));
                            else
                                CreateTab(CreateWebBrowser(NewTabUrl));
                        }
                    }
                    else
                    {
                        if (IsIEMode)
                            CreateIETab(CreateIEWebBrowser(NewTabUrl));
                        else
                            CreateTab(CreateWebBrowser(NewTabUrl));
                    }
                    HideTabs(bool.Parse(MainSave.Get("HideTabs")));
                    /*if (bool.Parse(MainSave.Get("IsI5Processor")))
                    {
                        if (!IsDeveloperMode || !IsIEMode)
                        {
                            MessageBox.Show("Sorry, SLBr can't support device Intel i5 processors");
                            Application.Current.Shutdown();
                        }
                    }*/
                    /*Cef.UIThreadTaskFactory.StartNew(delegate {
                        var rc = ((Tabs.Items.GetItemAt(1) as TabItem).Content as ChromiumWebBrowser).GetBrowser().GetHost().RequestContext;
                        var v = new Dictionary<string, object>();
                        v["mode"] = "fixed_servers";
                        v["server"] = "socks5://127.0.0.1:9190";
                        string error;
                        bool success = rc.SetPreference("proxy", v, out error);
                    });*/
                }));
            });
            if (FavouriteSave.Has("Favourite_Count"))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(FavouriteSave.Get("Favourite_Count")); i++)
                    {
                        string[] Value = FavouriteSave.Get($"Favourite_{i}", true);
                        //string Url = Value[0];
                        //string Header = Value[1];

                        Favourites.Add(new Favourite { Name = Value[1], Arguments = $"12<,>{Value[0]}" });
                    }
                    if (Favourites.Count == 0)
                        FavouriteContainer.Visibility = Visibility.Collapsed;
                    else
                        FavouriteContainer.Visibility = Visibility.Visible;
                }));
            }
            else
                FavouriteContainer.Visibility = Visibility.Collapsed;
            if (ATSADSEUrlsSave.Has("ATSADSEUrl_Count"))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < int.Parse(ATSADSEUrlsSave.Get("ATSADSEUrl_Count")); i++)
                    {
                        string Url = ATSADSEUrlsSave.Get($"ATSADSEUrl_{i}");
                        if (!ATSADSEUrls.Contains(Url))
                            ATSADSEUrls.Add(Url);
                    }
                }));
            }
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
            if (!IsDeveloperMode)
                IsDeveloperMode = Utils.HasDebugger();
            if (IsPrivateMode)
            {
                Prompt("No browsing history will be saved, in-memory cache will be used.", false);//is being used
                CachePath = string.Empty;
            }
            else if (IsDeveloperMode)
            {
                Prompt("Enabled access to developer/experimental features & functionalities of SLBr.", false, "", "", "", true, "\xE71C", "180");
                TestsMenuItem.Visibility = Visibility.Visible;
            }
            else if (IsChromiumMode)
                Prompt("API Keys are missing. The following functionalities of SLBr will be disabled [SafeBrowsing, Google Sign-in].", false);
            else if (IsIEMode)
            {
                Prompt("Javascript will not function properly and much of SLBr's functionalities will be disabled/broken. There will also be unexpected crashes and lots of errors. But hey, at least you can test out a website and see how bad it's styling gets.", false);
                FindTextBox.Visibility = Visibility.Collapsed;
                SSLGrid.Visibility = Visibility.Collapsed;
            }
            else if (string.IsNullOrEmpty(CachePath))
                Prompt("No browsing history will be saved, in-memory cache will be used.", false);
            IsProcessLoaded = true;
            if (!Utils.HasDebugger() && !IsDeveloperMode && !IsIEMode && !IsChromiumMode && !IsPrivateMode)
            {
                try
                {
                    string VersionInfo = TinyDownloader.DownloadString("https://raw.githubusercontent.com/SLT-World/SLBr/main/Version.txt").Replace("\n", "");
                    if (!VersionInfo.StartsWith(ReleaseVersion))
                    {
                        Prompt($"SLBr {VersionInfo} is now available, please update SLBr to keep up with the progress.", true, "Download", $"24<,>https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", $"https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", true, "\xE896");//SLBr is up to date
                    }
                }
                catch { }
            }
            if (!MainSave.Has("UsedBefore"))
            {
                MainSave.Set("UsedBefore", true.ToString());
                if (!IsIEMode)
                    CreateTab(CreateWebBrowser("slbr://about/"), true);
            }
            FavouritesPanel.ItemsSource = Favourites;
            FavouritesMenu.Collection = Favourites;
            PromptsPanel.ItemsSource = Prompts;
            UnloadAllTabsTimer = new DispatcherTimer();
            UnloadAllTabsTimer.Tick += UnloadAllTabsTimer_Tick;
            UnloadAllTabsTimer.Interval = new TimeSpan(0, 7, 0);
            UnloadAllTabsTimer.Start();
        }

        private void UnloadAllTabsTimer_Tick(object sender, EventArgs e)
        {
            if (!bool.Parse(MainSave.Get("TabUnloading")))
                return;
            foreach (TabItem Tab in Tabs.Items)
            {
                if (!Tab.IsSelected)
                    UnloadTab(Tab, true);//PROBLEM: Look at the UnloadTab code
            }
        }

        /*foreach (ManagementObject video in new ManagementObjectSearcher(new SelectQuery("Win32_VideoController")).Get())
	if ((string)video["Name"] == "Intel(R) Iris(R) Xe Graphics" && string.CompareOrdinal((string)video["DriverVersion"], "30.0.100.9667") <= 0)
	{
		System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
		break;
	}*/

        #region Actions
        public enum Actions
        {
            Undo = 0,
            Redo = 1,
            Refresh = 2,
            Create_Tab = 3,
            Print = 4,
            Source = 5,
            DevTools = 6,
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
        }
        private void Action(Actions _Action, object sender = null, string LastValue = "", string Value1 = "", string Value2 = "", string Value3 = "")
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
                case Actions.Create_Tab:
                    if (IsIEMode)
                        CreateIETab(CreateIEWebBrowser());
                    else
                        CreateTab(CreateWebBrowser());
                    break;
                case Actions.Print:
                    Print();
                    break;
                case Actions.Source:
                    ViewSource();
                    break;
                case Actions.DevTools:
                    DevTools();
                    break;
                case Actions.CloseTab:
                    if (Tabs.Items.Count == 2)
                        break;
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
                        Navigate(Value1);
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
                    //MessageBox.Show($"{Value1}|{Value2}|{Value3},{LastValue}");
                    DarkTheme(bool.Parse(Value1));
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
                    Pin(sender);
                    break;
                case Actions.ClosePrompt:
                    ClosePrompt(int.Parse(Value1));
                    break;
                case Actions.Prompt:
                    //NewMessage(Value1, Value2, Value3);
                    Prompt(Value1, true, Value2, Value3);
                    break;
                case Actions.PromptNavigate:
                    PromptNavigate(int.Parse(LastValue), Value1);
                    break;
                case Actions.Screenshot:
                    Screenshot();
                    break;
            }
        }

        public async void Screenshot()
        {
            if (IsIEMode)
            {
                Prompt("The screenshot feature is not supported on Internet Explorer mode.", false);
                return;
            }
            if (!IsDeveloperMode)
            {
                Prompt("The screenshot feature is still an experimental feature.", false);
                return;
            }
            using (var _DevToolsClient = GetBrowser().GetDevToolsClient())
            {
                string Url = $"{Path.Combine(GetDownloadsPath(), "SLBr_Screenshot_Test.png")}";
                var result = await _DevToolsClient.Page.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Png, null, null, null, false);
                File.WriteAllBytes(Url, result.Data);
                Navigate(Url);
            }
        }
        public void Prompt(string Content, bool IncludeButton = true, string ButtonContent = "", string ButtonArguments = "", string ToolTip = "", bool IncludeIcon = false, string IconText = "", string IconRotation = "")
        {
            int Count = Prompts.Count;
            Prompts.Add(new Prompt { Content = Content, ButtonVisibility = IncludeButton ? Visibility.Visible : Visibility.Collapsed, ButtonToolTip = ToolTip, ButtonContent = ButtonContent, ButtonTag = ButtonArguments + $"<,>{Count}", CloseButtonTag = $"22<,>{Count}", IconVisibility = IncludeIcon ? Visibility.Visible : Visibility.Collapsed, IconText = IconText, IconRotation = IconRotation });
        }
        public void PromptNavigate(int Index, string Url)
        {
            Navigate(Url);
            ClosePrompt(Index);
        }
        public void ClosePrompt(int Index)
        {
            if (Prompts.Count > 0)
            {
                Prompts.RemoveAt(Index);
                foreach (Prompt _Prompt in Prompts)
                {
                    _Prompt.CloseButtonTag = $"22<,>{Prompts.IndexOf(_Prompt)}";
                    _Prompt.ButtonTag = Utils.RemoveCharsAfterLastChar(_Prompt.ButtonTag, "<,>", true) + Prompts.IndexOf(_Prompt).ToString();
                }
            }
            //MessagesPanel.Refresh();
        }
        public void HideTabs(bool Toggle)
        {
            MainSave.Set("HideTabs", Toggle.ToString());
            TabPanel _TabPanel = (TabPanel)Tabs.Template.FindName("HeaderPanel", Tabs);
            if (Toggle)
            {
                foreach (TabItem Item in Tabs.Items)
                {
                    if (Item.Name == "SLBrSettingsTab")
                    {
                        Tabs.SelectedItem = Item;
                        CloseTab();
                        _SettingsWindow = new SettingsWindow();
                        _SettingsWindow.Show();
                        break;
                    }
                }
                _TabPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_SettingsWindow != null)
                {
                    _SettingsWindow.Close();
                    //_SettingsWindow = null;
                    Settings();
                }
                _TabPanel.Visibility = Visibility.Visible;
            }
        }
        public void Pin(object sender)
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
                Tab.Tag = $"{TabIndex}<,>Unpinned";
            }
            else
            {
                //_PinButton.Content = "\xE77A";
                Tabs.Items.Remove(Tab);
                Tabs.Items.Insert(1, Tab);
                Tab.IsSelected = true;
                Tab.Tag = $"{TabIndex}<,>Pinned";
            }
            _PinButton = (Button)Tab.Template.FindName("PinButton", Tab);
            if (_PinButton != null && !_PinButton.Tag.ToString().Contains("ActionSetted"))
            {
                _PinButton.Tag = "21<,>ActionSetted";
                _PinButton.Click += new RoutedEventHandler(ButtonAction);
            }
        }
        public void NewsFeed()
        {
            new NewsPage().Show();
        }
        public void ResetZoomLevel()
        {
            if (IsIEMode)
            {
                Prompt("Zooming is not supported on Internet Explorer mode.", false);
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
                Prompt("Zooming is not supported on Internet Explorer mode.", false);
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
                Prompt("Zooming is not supported on Internet Explorer mode.", false);
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
                /*ATSADSEUrls
                    SearchEngines
                    Favourites*/
            }
            Relaunch(false);
        }
        /*public void ClearCache()
        {
            string _Path = CachePath;
            if (Directory.Exists(_Path))
                Directory.Delete(_Path);
            //Restart();
        }*/
        public void Relaunch(bool CallClosingEvent = true)
        {
            if (CallClosingEvent && MessageBox.Show("Are you sure you want to relaunch SLBr? (Everything will be saved)", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            else if (!CallClosingEvent)
                IsProcessLoaded = false;
                //Closing -= Window_Closing;
            Process.Start(Application.ResourceAssembly.Location);//DoNotUseIfUsingClickOnce
            Application.Current.Shutdown();
        }
        public void DarkTheme(bool Toggle)
        {
            if (Toggle)
            {
                Resources["PrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202225"));
                Resources["FontBrush"] = new SolidColorBrush(Colors.White);
                Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#36393F"));
                Resources["UnselectedTabBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3136"));
                Resources["ControlFontBrush"] = new SolidColorBrush(Colors.Gainsboro);
            }
            else
            {
                Resources["PrimaryBrush"] = new SolidColorBrush(Colors.White);
                Resources["FontBrush"] = new SolidColorBrush(Colors.Black);
                Resources["BorderBrush"] = new SolidColorBrush(Colors.Gainsboro);
                Resources["UnselectedTabBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
                Resources["ControlFontBrush"] = new SolidColorBrush(Colors.Gray);
            }
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
        public void Navigate(string Url)
        {
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser == null)
                    return;
                _Browser.Navigate(Url);
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                _Browser.Address = Utils.FixUrl(Url, bool.Parse(MainSave.Get("Weblight")));
            }
        }
        public void Favourite()
        {
            if (IsIEMode)
            {
                Prompt("The favourite feature is not supported on Internet Explorer mode.", false);
                return;
            }
            TabItem Tab = GetCurrentTab();
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            string Url = _Browser.Address;
            int FavouriteExistIndex = FavouriteExists(Url);
            if (FavouriteExistIndex != -1)
            {
                Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
                //Dispatcher.Invoke(delegate () { }, DispatcherPriority.Render);
            }
            else if (_Browser.IsLoaded)
            {
                Favourites.Add(new Favourite { Name = _Browser.Title, Arguments = $"12<,>{Url}" });
                FavouriteButton.Content = "\xEB52";
            }
            if (Favourites.Count == 0)
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
                Prompt("HTML Editor is not supported on Internet Explorer mode.", false);
                return;
            }
            CreateTab(CreateWebBrowser("slbr://htmleditor"));
        }
        public void Settings()
        {
            //new SettingsWindow().ShowDialog();
            if (bool.Parse(MainSave.Get("HideTabs")))
            {
                //if (_SettingsWindow == null)
                //{
                _SettingsWindow = new SettingsWindow();
                _SettingsWindow.Show();
                //}
                //else
                //    MessageBox.Show("An instance of the settings window is already running.");
            }
            else
            {
                if (SettingsPages > 0)
                {
                    bool FoundSettingsPage = false;
                    foreach (TabItem Item in Tabs.Items)
                    {
                        if (Item.Name == "SLBrSettingsTab")
                        {
                            Tabs.SelectedItem = Item;
                            if (!FoundSettingsPage)
                                FoundSettingsPage = true;
                            break;
                        }
                    }
                    if (!FoundSettingsPage)
                        SettingsPages = 0;
                    return;
                }

                int Count = Tabs.Items.Count;
                Frame _Frame = new Frame();
                TabItem Tab = new TabItem()
                {
                    Name = "SLBrSettingsTab",
                    Header = "Settings",
                    Content = _Frame
                };
                _Frame.Content = new SettingsPage();
                Tabs.Items.Insert(Count/* - 1*/, Tab);
                Tab.Tag = $"{Count}<,>Unpinned";
                Tabs.SelectedItem = Tab;
                AddressBox.Text = string.Empty;
                _Frame.LoadCompleted += (sender, args) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        Tab.ApplyTemplate();
                        Button _Button = (Button)Tab.Template.FindName("CloseTabButton", Tab);
                        if (_Button != null && !_Button.Tag.ToString().Contains("ActionSetted")/* && Tab.ContextMenu.Items[0] != null*/)
                        {
                            /*MenuItem _MenuItem = (MenuItem)Tab.ContextMenu.Items[0];
                            _MenuItem.Tag = "7";
                            _MenuItem.Click += new RoutedEventHandler(ButtonAction);*/
                            _Button.Tag = "7<,>ActionSetted";
                            _Button.Click += new RoutedEventHandler(ButtonAction);
                        }
                        Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                        System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                        if (_DefaultTabIcon != null && _Image != null)
                        {
                            _DefaultTabIcon.Visibility = Visibility.Visible;
                            _Image.Visibility = Visibility.Collapsed;
                        }
                        Button _PinButton = (Button)Tab.Template.FindName("PinButton", Tab);
                        if (_PinButton != null && !_PinButton.Tag.ToString().Contains("ActionSetted"))
                        {
                            _PinButton.Tag = "21<,>ActionSetted";
                            _PinButton.Click += new RoutedEventHandler(ButtonAction);
                        }
                    }));
                };
                SettingsPages++;
            }
        }
        public void CloseTab(/*TabItem SpecifiedTab = null*/)
        {
            TabItem Tab;
            /*if (SpecifiedTab != null)
                Tab = SpecifiedTab;
            else
                return;*/
            Tab = GetCurrentTab();
            /*if (_MenuItem != null && !(_MenuItem.DataContext is TabItem))
                throw new Exception();*/
            if (Tab.Name == "SLBrSettingsTab")
                SettingsPages--;
            if (IsIEMode)
            {
                WebBrowser _Browser = GetIEBrowser();
                if (_Browser != null)
                    _Browser.Dispose();
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser != null)
                {
                    //DisposeBrowser(_Browser);
                    //_Browser.Stop();
                    //_Browser.BrowserCore.CloseBrowser(true);
                    _Browser.Dispose();
                }
            }
            Tabs.Items.Remove(Tab);
        }
        public void DevTools()
        {
            if (IsIEMode)
            {
                Prompt("Developer Tools is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            _Browser.ShowDevTools();
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
                Prompt("Viewing sources is not supported on Internet Explorer mode.", false);
                return;
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            CreateTab(CreateWebBrowser($"view-source:{_Browser.Address}"));
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
                {
                    _Browser.GoBack();
                }
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                if (_Browser.CanGoBack == true)
                {
                    _Browser.Back();
                }
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
                {
                    _Browser.GoForward();
                }
            }
            else
            {
                ChromiumWebBrowser _Browser = GetBrowser();
                if (_Browser == null)
                    return;
                if (_Browser.CanGoForward == true)
                {
                    _Browser.Forward();
                }
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
                if (_Browser == null)
                    return;
                if (_Browser.IsLoaded)
                    _Browser.Reload();
                else
                    _Browser.Stop();
            }
        }
        #endregion
        public void CreateTab(ChromiumWebBrowser _Browser, bool Focus = true, int Index = 0, bool NameByUrl = false)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                int Count = Tabs.Items.Count;
                if (Index > 0)
                    Count = Index;
                string TabName = NameByUrl ? _Browser.Address : "New Tab";
                TabItem Tab = new TabItem()
                {
                    Header = $"{TabName}"
                };
                ApplyToTab(_Browser, Tab, true);
                Tabs.Items.Insert(Count/* - 1*/, Tab);
                Tab.Tag = $"{Count}<,>Unpinned";
                Tab.IsSelected = Focus;
                Tab.UseLayoutRounding = true;
                Tab.SnapsToDevicePixels = true;
            }));
        }
        public void CreateIETab(WebBrowser _Browser, bool Focus = true, int Index = 0)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                int Count = Tabs.Items.Count;
                if (Index > 0)
                    Count = Index;
                string TabName = /*NameByUrl ? */_Browser.Source.AbsoluteUri/* : "New Tab"*/;
                TabItem Tab = new TabItem()
                {
                    //Header = $"Tab {Count}"
                    Header = $"{TabName}"
                };
                _Browser = ConfigureIE(_Browser, Tab);
                //Tab.Background = Brushes.Transparent;
                //Tab.FontFamily = new FontFamily("Original");
                Tab.Content = _Browser;
                /*_Browser.SnapsToDevicePixels = true;
                _Browser.UseLayoutRounding = true;*/
                Tabs.Items.Insert(Count/* - 1*/, Tab);
                Tab.Tag = $"{Count}<,>Unpinned";
                Tab.IsSelected = Focus;
                //Tab.UseLayoutRounding = true;
                    //NewMessage("You are in SLBr developer mode, you have access to developer features of SLBr.", false);
                /*else if (InternetExplorerMode)
                    NewMessage("All sites in this tab will be opened in Internet Explorere mode.", false);*/
            }));
        }

        public void UnloadTab(TabItem Tab, bool IgnoreIfSound = false)
        {
            ChromiumWebBrowser _Browser = GetBrowser(Tab);
            if (_Browser != null && _Browser.IsBrowserInitialized)
            {
                if (IgnoreIfSound)//PROBLEM: This checks if the address is a known music website. I need help on detecting sound.
                {
                    string CleanedAddress = Utils.CleanUrl(_Browser.Address);
                    if (CleanedAddress.Contains("youtube.com/watch")
                        || CleanedAddress.Contains("spotify.com/track/")
                        || CleanedAddress.Contains("soundcloud.com")
                        || CleanedAddress.Contains("dailymotion.com/video/")
                        || CleanedAddress.Contains("vimeo.com")
                        || CleanedAddress.Contains("twitch.tv/")
                        || CleanedAddress.Contains("bitchute.com/video/")
                        || CleanedAddress.Contains("ted.com/talks/")
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
        public void ApplyToTab(ChromiumWebBrowser _Browser, TabItem Tab, bool Configure)
        {
            if (Configure)
                _Browser = this.Configure(_Browser, Tab);
            Tab.Content = _Browser;
        }

        public ChromiumWebBrowser Configure(ChromiumWebBrowser _Browser, TabItem Tab/* = null*/)
        {
            var sett = new BrowserSettings();
            sett.BackgroundColor = Utils.ColorToUInt(System.Drawing.Color.Black);
            _Browser.BrowserSettings = sett;
            _Browser.JavascriptObjectRepository.Register("slbr", _JSBindingHandler, true, BindingOptions.DefaultBinder);
            //_Browser.ExecuteScriptAsyncWhenPageLoaded(File.ReadAllText("Resources/JsBinding.js")/*, true*/);
            _Browser.TitleChanged += TitleChanged;
                _Browser.LoadingStateChanged += LoadingStateChanged;
                // Enable touch scrolling - once properly tested this will likely become the default
                //_Browser.IsManipulationEnabled = true;
                _Browser.LifeSpanHandler = _LifeSpanHandler;
                _Browser.DownloadHandler = _DownloadHandler;
                _Browser.RequestHandler = _RequestHandler;
                _Browser.MenuHandler = _ContextMenuHandler;
                _Browser.KeyboardHandler = _KeyboardHandler;
                _Browser.ZoomLevelIncrement = 0.25;
                _Browser.FrameLoadEnd += (sender, args) =>
                {
                    if (args.Frame.IsValid && args.Frame.IsMain)
                    {
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
                        if (ArgsUrl == "slbr://version/")
                        {
                            args.Frame.ExecuteJavaScriptAsync($"document.getElementById(\"_version\").innerHTML = \"{ReleaseVersion}\";" +
                                $"document.getElementById(\"bit_process\").innerHTML = \"({BitProcess}-bit ARM)\";" +
                                $"document.getElementById(\"build_type\").innerHTML = \"({_BuildType} Build)\";" +
                                $"document.getElementById(\"chromium_version\").innerHTML = \"{ChromiumVersion}\";" +
                                //$"document.getElementById(\"_revision\").innerHTML = \"{Revision}\";" +
                                //"document.getElementById(\"_os_type\").innerHTML = \"Windows\";" +
                                $"document.getElementById(\"js_version\").innerHTML = \"{JavascriptVersion}\";" +
                                //$"document.getElementById(\"_useragent\").innerHTML = \"{UserAgent}\";" +
                                $"document.getElementById(\"_command_line\").innerHTML = '{"\"" + ExecutableLocation + "\""}';" +
                                $"document.getElementById(\"_executable_path\").innerHTML = \"{ExecutableLocation}\";" +
                                $"document.getElementById(\"_cache_path\").innerHTML = \"{CachePath.Replace("\\", "\\\\")}\";");
                        }
                        if (bool.Parse(MainSave.Get("DarkTheme")))
                        {
                            args.Frame.ExecuteJavaScriptAsync("document.documentElement.style.setProperty('filter', 'invert(100%)');" +
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
                                        "});");
                        }
                        int HttpStatusCode = args.HttpStatusCode;
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (HttpStatusCode == 404 && !Utils.CleanUrl(ArgsUrl).StartsWith("web.archive.org"))
                                Prompt("This page is missing, do you want to check if there's a saved version on the Wayback Machine?", true, "Check for saved version", $"24<,>https://web.archive.org/{ArgsUrl}", $"https://web.archive.org/{ArgsUrl}", true, "\xF142");
                            else if (Utils.CleanUrl(ArgsUrl).EndsWith(".png") || Utils.CleanUrl(ArgsUrl).EndsWith(".jpeg") || Utils.CleanUrl(ArgsUrl).EndsWith(".webp"))
                                Prompt("You are viewing a image, don't mistake it for a webpage.", false);
                            else
                                CloseClosableMessages();
                            try
                            {
                                string Host = new Uri(_Browser.Address).Host;
                                if (bool.Parse(MainSave.Get("ATSADSE")) && !ATSADSEUrls.Contains(Host) && !SearchEngines.Select(item => new Uri(item).Host).Contains(Host))
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
                                                        ATSADSEUrls.Add(Host);
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
                            Button _Button = (Button)Tab.Template.FindName("CloseTabButton", Tab);
                            if (_Button != null && !_Button.Tag.ToString().Contains("ActionSetted")/* && Tab.ContextMenu.Items[0] != null*/)
                            {
                                //_Browser.ExecuteScriptAsync("chrome.webRequest.onBeforeRequest.addListener(function(details) { return { cancel: true}; },{ urls:[\"*://*.doubleclick.net/*\"] },[\"blocking\"]);");
                                /*MenuItem _MenuItem = (MenuItem)Tab.ContextMenu.Items[0];
                                _MenuItem.Tag = "7";
                                _MenuItem.Click += new RoutedEventHandler(ButtonAction);*/
                                _Button.Tag = "7<,>ActionSetted";
                                _Button.Click += new RoutedEventHandler(ButtonAction);
                            }
                            Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                            System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                            if (AddressBox.Text != (bool.Parse(MainSave.Get("FullAddress")) ? _Browser.Address : Utils.CleanUrl(_Browser.Address)) && _Browser == GetBrowser())
                            {
                                AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? _Browser.Address : Utils.CleanUrl(_Browser.Address);
                                AddressBox.Tag = _Browser.Address;
                            }
                            if (_DefaultTabIcon != null && _Image != null)
                            {
                                try
                                {
                                    var bytes = TinyDownloader.DownloadData("https://www.google.com/s2/favicons?domain=" + new Uri(_Browser.Address).Host);
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
                            Button _PinButton = (Button)Tab.Template.FindName("PinButton", Tab);
                            if (_PinButton != null && !_PinButton.Tag.ToString().Contains("ActionSetted"))
                            {
                                _PinButton.Tag = "21<,>ActionSetted";
                                _PinButton.Click += new RoutedEventHandler(ButtonAction);
                            }
                            //_Browser.ExecuteScriptAsync("function C(d,o){v=d.createElement('div');o.parentNode.replaceChild(v,o);}function A(d){for(j=0;t=[\", Interaction.IIf(browser.Address.Contains(\"youtube.com\"), \"'iframe','marquee'\", \"'iframe','embed','marquee'\")), \"][j];++j){o=d.getElementsByTagName(t);for(i=o.length-1;i>=0;i--)C(d,o[i]);}g=d.images;for(k=g.length-1;k>=0;k--)if({'21x21':1,'48x48':1,'60x468':1,'88x31':1,'88x33':1,'88x62':1,'90x30':1,'90x32':1,'90x90':1,'100x30':1,'100x37':1,'100x45':1,'100x50':1,'100x70':1,'100x100':1,'100x275':1,'110x50':1,'110x55':1,'110x60':1,'110x110':1,'120x30':1,'120x60':1,'120x80':1,'120x90':1,'120x120':1,'120x163':1,'120x181':1,'120x234':1,'120x240':1,'120x300':1,'120x400':1,'120x410':1,'120x500':1,'120x600':1,'120x800':1,'125x40':1,'125x60':1,'125x65':1,'125x72':1,'125x80':1,'125x125':1,'125x170':1,'125x250':1,'125x255':1,'125x300':1,'125x350':1,'125x400':1,'125x600':1,'125x800':1,'126x110':1,'130x60':1,'130x65':1,'130x158':1,'130x200':1,'132x70':1,'140x55':1,'140x350':1,'145x145':1,'146x60':1,'150x26':1,'150x60':1,'150x90':1,'150x100':1,'150x150':1,'155x275':1,'155x470':1,'160x80':1,'160x126':1,'160x600':1,'180x30':1,'180x66':1,'180x132':1,'180x150':1,'194x165':1,'200x60':1,'220x100':1,'225x70':1,'230x30':1,'230x33':1,'230x60':1,'234x60':1,'234x68':1,'240x80':1,'240x300':1,'250x250':1,'275x60':1,'280x280':1,'300x60':1,'300x100':1,'300x250':1,'320x50':1,'320x70':1,'336x280':1,'350x300':1,'350x850':1,'360x300':1,'380x112':1,'380x250':1,'392x72':1,'400x40':1,'400x50':1,'425x600':1,'430x225':1,'440x40':1,'464x62':1,'468x16':1,'468x60':1,'468x76':1,'468x120':1,'468x248':1,'470x60':1,'480x400':1,'486x60':1,'545x90':1,'550x5':1,'600x30':1,'720x90':1,'720x300':1,'725x90':1,'728x90':1,'734x96':1,'745x90':1,'750x25':1,'750x100':1,'750x150':1,'850x120':1}[g[k].width+'x'+g[k].height])C(d,g[k]);}A(document);for(f=0;z=frames[f];++f)A(z.document)");
                            if (_Browser.GetBrowser().CanGoBack)
                                BackButton.IsEnabled = true;
                            else
                                BackButton.IsEnabled = false;
                            if (_Browser.GetBrowser().CanGoForward)
                                ForwardButton.IsEnabled = true;
                            else
                                ForwardButton.IsEnabled = false;
                        }));
                    }
                };
            return _Browser;
        }
        public WebBrowser ConfigureIE(WebBrowser _Browser, TabItem Tab/* = null*/)
        {
            _Browser.LoadCompleted += delegate (object sender, NavigationEventArgs e) { IE_LoadCompleted(sender, e, Tab); };
            return _Browser;
        }

        private void IE_LoadCompleted(object sender, NavigationEventArgs e, TabItem Tab)
        {
            WebBrowser _Browser = sender as WebBrowser;
                string ArgsUrl = e.Uri.AbsoluteUri;
                if (!IsInformationSet)
                    IsInformationSet = true;
                //int HttpStatusCode = e.WebResponse.ContentType;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (AddressBox.Text != (bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri)) && _Browser == GetIEBrowser())
                    {
                        AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri);
                        AddressBox.Tag = e.Uri.AbsoluteUri;
                    }
                    Button _Button = (Button)Tab.Template.FindName("CloseTabButton", Tab);
                    if (_Button != null && !_Button.Tag.ToString().Contains("ActionSetted")/* && Tab.ContextMenu.Items[0] != null*/)
                    {
                        /*MenuItem _MenuItem = (MenuItem)Tab.ContextMenu.Items[0];
                        _MenuItem.Tag = "7";
                        _MenuItem.Click += new RoutedEventHandler(ButtonAction);*/
                        _Button.Tag = "7<,>ActionSetted";
                        _Button.Click += new RoutedEventHandler(ButtonAction);
                    }
                    Image _Image = (Image)Tab.Template.FindName("Icon", Tab);
                    System.Windows.Shapes.Path _DefaultTabIcon = (System.Windows.Shapes.Path)Tab.Template.FindName("TabIcon", Tab);
                    if (_DefaultTabIcon != null && _Image != null)
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
                    }
                    Button _PinButton = (Button)Tab.Template.FindName("PinButton", Tab);
                    if (_PinButton != null && !_PinButton.Tag.ToString().Contains("ActionSetted"))
                    {
                        _PinButton.Tag = "21<,>ActionSetted";
                        _PinButton.Click += new RoutedEventHandler(ButtonAction);
                    }
                    if (_Browser.CanGoBack)
                        BackButton.Foreground = Resources["ControlFontBrush"] as SolidColorBrush;
                    else
                        BackButton.Foreground = Resources["BorderBrush"] as SolidColorBrush;
                    if (_Browser.CanGoForward)
                        ForwardButton.Foreground = Resources["ControlFontBrush"] as SolidColorBrush;
                    else
                        ForwardButton.Foreground = Resources["BorderBrush"] as SolidColorBrush;
                }));
        }

        /*private Rect GetBoundingBox(FrameworkElement element, Window containerWindow)
        {
            GeneralTransform transform = element.TransformToAncestor(containerWindow);
            Point topLeft = transform.Transform(new Point(0, 0));
            Point bottomRight = transform.Transform(new Point(element.ActualWidth, element.ActualHeight));
            return new Rect(topLeft, bottomRight);
        }*/
        public static MenuItem CreateMenuItemForList(string Header, string Tag = "Empty00000", RoutedEventHandler _RoutedEventHandler = null)
        {
            MenuItem _MenuItem = new MenuItem();
            _MenuItem.FontFamily = new FontFamily("Arial");
            _MenuItem.Foreground = new SolidColorBrush(Colors.Black);
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
            var Values = _Tag.Split(new string[] { "<,>", "&lt;,&gt;" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3
                _Action = (Actions)int.Parse(Values[0]);
                string LastValue = Values.Last();
                Action(_Action, sender, LastValue, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }

        public ChromiumWebBrowser CreateWebBrowser(string Url = "Empty00000")
        {
            if (Url == "Empty00000")
                Url = MainSave.Get("Homepage");
            ChromiumWebBrowser _Browser = new ChromiumWebBrowser(Url);//Configure(GetBrowserFromPool(), Url);
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

        void BrowserChanged(object sender, bool IsSwitchTab = false)
        {
            if (IsIEMode)
                return;
            Dispatcher.BeginInvoke(new Action(delegate
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
                AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? CurrentBrowser.Address : Utils.CleanUrl(CurrentBrowser.Address);
                AddressBox.Tag = CurrentBrowser.Address;
                bool ContinueUrlCheck = true;
                if (CurrentBrowser.Address.StartsWith("http") || CurrentBrowser.Address.StartsWith("file:"))
                {
                    string Host = new Uri(CurrentBrowser.Address).Host;//Cef.ParseUrl(CurrentBrowser.Address)
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
                    if (Utils.IsInternalUrl(CurrentBrowser.Address))
                    {
                        SSLSymbol.Text = "\xE8BE";
                        SSLToolTip.Content = $"Secure SLBr Page";
                    }
                    else if (Utils.IsSystemUrl(CurrentBrowser.Address))
                    {
                        SSLSymbol.Text = "\xE72E";
                        SSLToolTip.Content = $"{CurrentBrowser.Address} is a system scheme and is considered secure";
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
                    if (CurrentBrowser.CanGoBack)
                        BackButton.IsEnabled = true;
                    else
                        BackButton.IsEnabled = false;
                    if (CurrentBrowser.CanGoForward)
                        ForwardButton.IsEnabled = true;
                    else
                        ForwardButton.IsEnabled = false;
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

        //document.querySelectorAll('video').forEach(vid => vid.pause());

        #region CEF
        private void TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ChromiumWebBrowser _Browser = (ChromiumWebBrowser)sender;
                TabItem Tab = GetCurrentTab();
                ChromiumWebBrowser CurrentBrowser = GetBrowser();
                if (CurrentBrowser != _Browser)
                    return;
                Tab.Header = _Browser.Title.Trim().Length > 0 ? _Browser.Title : Utils.CleanUrl(_Browser.Address);
            }));
        }
        private void LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            //Dispatcher.BeginInvoke(new Action(delegate
            //{
            BrowserChanged(sender);
            //}));
        }
        #endregion

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
            return (_Message.ButtonContent != null && _Message.ButtonContent == "Check for saved version"
            || _Message.ButtonContent == "Set as default search provider?"
            || _Message.ButtonContent == "Open in file explorer")
            || (_Message.Content != null && _Message.Content.StartsWith("You are viewing a image"));
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
        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsIEMode)
            {
                WebBrowser _IEBrowser = GetIEBrowser();
                if (_IEBrowser == null)
                    return;
                if (e.Key == Key.Enter && AddressBox.Text.Trim().Length > 0)
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
                    return;
                }
            }
            ChromiumWebBrowser _Browser = GetBrowser();
            if (_Browser == null)
                return;
            if (e.Key == Key.Enter && AddressBox.Text.Trim().Length > 0)
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
                string Url = Utils.FilterUrlForBrowser(AddressBox.Text, MainSave.Get("Search_Engine"), bool.Parse(MainSave.Get("Weblight")), IsChromiumMode);
                if (!Utils.IsProgramUrl(Url))
                    _Browser.Address = Url;
            }
        }
        private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
        {
            //try
            //{
            if (AddressBox.Text == (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString())))
                AddressBox.Text = AddressBox.Tag.ToString();
            //}
            //catch { }
            AddressBoxFocused = true;
        }
        private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!AddressBoxMouseEnter)
            {
                //try
                //{
                if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                    AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString());
                //}
                //catch { }
            }
            AddressBoxFocused = false;
        }
        private void AddressBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                //try
                //{
                if (AddressBox.Text == (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString())))
                    AddressBox.Text = AddressBox.Tag.ToString();
                //}
                //catch { }
            }
            AddressBoxMouseEnter = true;
        }
        private void AddressBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                //try
                //{
                if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                    AddressBox.Text = bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString());
                //}
                //catch { }
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

        DispatcherTimer UnloadAllTabsTimer;

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CloseClosableMessages();
            BrowserChanged(sender, true);
            TabItem Tab = (TabItem)Tabs.SelectedItem;
            if (Tab != null)
            {
                if (Tab.Tag != null && Tab.Tag.ToString().Contains("Unpinned"))
                    Tab.Tag = Tabs.Items.IndexOf(Tab);
                if (Tab.Name == "SLBrSettingsTab")
                    AddressBox.Text = string.Empty;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsProcessLoaded)
            {
                UnloadAllTabsTimer.Stop();
                for (int i = 0; i < Favourites.Count; i++)
                    FavouriteSave.Set($"Favourite_{i}", Favourites[i].Arguments.Replace("12<,>", ""), Favourites[i].Name, false);
                FavouriteSave.Set("Favourite_Count", Favourites.Count.ToString());
                for (int i = 0; i < SearchEngines.Count; i++)
                    SearchEnginesSave.Set($"Search_Engine_{i}", SearchEngines[i], false);
                SearchEnginesSave.Set("Search_Engine_Count", SearchEngines.Count.ToString());
                for (int i = 0; i < ATSADSEUrls.Count; i++)
                    ATSADSEUrlsSave.Set($"ATSADSEUrl_{i}", ATSADSEUrls[i], false);
                ATSADSEUrlsSave.Set("ATSADSEUrl_Count", ATSADSEUrls.Count.ToString());
                bool RestoreTabs = bool.Parse(MainSave.Get("RestoreTabs"));
                if (RestoreTabs)
                {
                    int Count = 0;
                    int SelectedIndex = 1;
                    for (int i = 0; i < Tabs.Items.Count; i++)
                    {
                        if (i == 0)
                            continue;
                        TabItem Tab = (TabItem)Tabs.Items.GetItemAt(i);
                        string Url = "";
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
            Cef.Shutdown();
            //MainSave.Save();
        }
    }
}