using CefSharp;
using CefSharp.SchemeHandler;
using CefSharp.Wpf.HwndHost;
using Microsoft.Win32;
using SLBr.Handlers;
using SLBr.Pages;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App Instance;
        public Random TinyRandom;
        public WebClient TinyDownloader;

        public List<MainWindow> AllWindows = new List<MainWindow>();

        public List<string> DefaultSearchEngines = new List<string>() {
            "https://google.com/search?q={0}",
            "https://bing.com/search?q={0}",
            "https://duckduckgo.com/?q={0}",
            "https://search.brave.com/search?q={0}",
            "https://www.ecosia.org/search?q={0}",
            /*"https://search.yahoo.com/search?p={0}",
            "https://yandex.com/search/?text={0}",*/
        };
        public List<string> SearchEngines = new List<string>();
        public List<Theme> Themes = new List<Theme>()
        {
            new Theme("Light", Colors.White, Colors.WhiteSmoke, Colors.Gainsboro, Colors.Gray, Colors.Black, (Color)ColorConverter.ConvertFromString("#3399FF"), false, false),
            new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), (Color)ColorConverter.ConvertFromString("#2F3136"), (Color)ColorConverter.ConvertFromString("#36393F"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3399FF"), true, true),
            new Theme("Purple", (Color)ColorConverter.ConvertFromString("#191025"), (Color)ColorConverter.ConvertFromString("#251C31"), (Color)ColorConverter.ConvertFromString("#2B2237"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#934CFE"), true, true),
            new Theme("Green", (Color)ColorConverter.ConvertFromString("#163C2C"), (Color)ColorConverter.ConvertFromString("#18352B"), (Color)ColorConverter.ConvertFromString("#1A3029"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3AE872"), true, true)
        };

        public IdnMapping _IdnMapping = new IdnMapping();

        public Saving GlobalSave;
        public Saving FavouritesSave;
        public Saving SearchSave;
        public Saving StatisticsSave;
        public Saving LanguagesSave;

        public List<Saving> WindowsSaves = new List<Saving>();


        public string Username = "Default";
        string GlobalApplicationDataPath;
        string UserApplicationDataPath;
        public string UserApplicationWindowsPath;
        string CachePath;
        string UserDataPath;
        string LogPath;
        public string ExecutablePath;

        bool AppInitialized;
        public string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";

        public string ChromiumRevision;
        public string ChromiumJSVersion;




        //https://data.firefox.com/dashboard/hardware
        public List<int> FingerprintHardwareConcurrencies = new List<int>() { 1, 2, 4, 6, 8, 10, 12, 14 };
        //https://source.chromium.org/chromium/chromium/deps/icu.git/+/chromium/m120:source/data/misc/metaZones.txt
        public List<string> FingerprintTimeZones = new List<string>() { "Africa/Monrovia", "Europe/London", "America/New_York", "Asia/Seoul", "Asia/Singapore", "Asia/Taipei" };




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
        private ObservableCollection<ActionStorage> PrivateGlobalHistory = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> GlobalHistory
        {
            get { return PrivateGlobalHistory; }
            set
            {
                PrivateGlobalHistory = value;
                RaisePropertyChanged("GlobalHistory");
            }
        }
        private ObservableCollection<ActionStorage> PrivateCompletedDownloads = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> CompletedDownloads
        {
            get { return PrivateCompletedDownloads; }
            set
            {
                PrivateCompletedDownloads = value;
                Dispatcher.Invoke(() =>
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
                });
                RaisePropertyChanged("CompletedDownloads");
            }
        }
        public void AddGlobalHistory(string Url, string Title)
        {
            List<string> Urls = GlobalHistory.Select(i => i.Tooltip).ToList();
            if (Urls.Contains(Url))
                GlobalHistory.RemoveAt(Urls.IndexOf(Url));
            GlobalHistory.Insert(0, new ActionStorage(Title, $"4<,>{Url}", Url));
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
            Dispatcher.Invoke(() =>
            {
                if (item.IsComplete)
                    CompletedDownloads.Add(new ActionStorage(Path.GetFileName(item.FullPath), "4<,>slbr://downloads/", ""));
                foreach (MainWindow _Window in AllWindows)
                {
                    _Window.TaskbarProgress.ProgressValue = item.IsComplete ? 0 : (double)item.PercentComplete / 100.0;
                }
            });
        }

        string[] Args;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Instance = this;
            Args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            InitializeApp();
        }

        static Mutex mutex;
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private void InitializeApp()
        {
            string CommandLineUrl = "";
            if (Args.Length > 0)
            {
                foreach (string Flag in Args)
                {
                    /*if (Flag == "--dev")
                        DeveloperMode = true;*/
                    if (Flag.StartsWith("--user="))
                    {
                        Username = Flag.Replace("--user=", "").Replace(" ", "-");
                        if (Username != "Default")
                            AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30-" + Username + "}";
                    }
                    else
                    {
                        if (Flag.StartsWith("--"))
                            continue;
                        CommandLineUrl = Flag;
                    }
                }
            }
            SetCurrentProcessExplicitAppUserModelID(AppUserModelID);
            mutex = new Mutex(true, AppUserModelID);

            if (string.IsNullOrEmpty(CommandLineUrl))
            {
                if (!mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                Process CurrentProcess = Process.GetCurrentProcess();
                Process _otherInstance = Utils.GetAlreadyRunningInstance(CurrentProcess);
                if (_otherInstance != null)
                {
                    MessageHelper.SendDataMessage(_otherInstance, CommandLineUrl);
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }



            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Set Google API keys, used for Geolocation requests sans GPS. See http://www.chromium.org/developers/how-tos/api-keys
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);

            GlobalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
            CachePath = Path.GetFullPath(Path.Combine(UserDataPath, "Cache"));
            LogPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            TinyRandom = new Random();
            TinyDownloader = new WebClient();

            InitializeSaves();
            InitializeCEF();
            InitializeUISaves(CommandLineUrl);

            if (Utils.IsAdministrator())
            {
                using (var checkkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
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
                            ApplicationRegistry.SetValue("ApplicationCompany", "SLT Softwares");
                            ApplicationRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                            ApplicationRegistry.Close();

                            RegistryKey IconRegistry = key.CreateSubKey("DefaultIcon", true);
                            IconRegistry.SetValue(null, $"{ExecutablePath},0");
                            ApplicationRegistry.Close();

                            RegistryKey CommandRegistry = key.CreateSubKey("shell\\open\\command", true);
                            CommandRegistry.SetValue(null, $"\"{ExecutablePath}\" \"%1\"");
                            CommandRegistry.Close();
                        }
                        using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Clients\\StartMenuInternet", true).CreateSubKey("SLBr", true))
                        {
                            if (key.GetValue(null) as string != "SLBr")
                                key.SetValue(null, "SLBr");

                            RegistryKey CapabilitiesRegistry = key.CreateSubKey("Capabilities", true);
                            CapabilitiesRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                            CapabilitiesRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                            CapabilitiesRegistry.SetValue("ApplicationName", $"SLBr");
                            RegistryKey StartMenuRegistry = CapabilitiesRegistry.CreateSubKey("Startmenu", true);
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
                            CommandRegistry.SetValue(null, $"\"{ExecutablePath}\"");
                            CommandRegistry.Close();
                        }
                        checkkey.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                    }
                }
            }

            AppInitialized = true;
        }

        public void DiscordWebhookSendInfo(string Content)
        {
            try { TinyDownloader.UploadValues(SECRETS.DISCORD_WEBHOOK, new NameValueCollection { { "content", Content }, { "username", "SLBr Diagnostics" } }); }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo($"**Automatic Report**\n" +
                $"> - Version: `{ReleaseVersion}`\n> \n" +
                $"> - Message: ```{e.Exception.Message}```\n> \n" +
                $"> - Source: `{e.Exception.Source} `\n" +
                $"> - Target Site: `{e.Exception.TargetSite} `\n\n" +
                $"Stack Trace: ```{e.Exception.StackTrace} ```\n" +
                $"Inner Exception: ```{e.Exception.InnerException} ```");

            string Text = $"[SLBr] {ReleaseVersion}\n\n" +
                $"[Message] {e.Exception.Message}\n" +
                $"[Source] {e.Exception.Source}\n\n" +
                $"[Target Site] {e.Exception.TargetSite}\n\n" +
                $"[Stack Trace] {e.Exception.StackTrace}\n\n" +
                $"[Inner Exception] {e.Exception.InnerException}";
            //e.SetObserved();
            MessageBox.Show(Text);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception _E = e.ExceptionObject as Exception;
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo($"**Automatic Report**\n" +
                $"> - Version: `{ReleaseVersion}`\n> \n" +
                $"> - Message: ```{_E.Message}```\n> \n" +
                $"> - Source: `{_E.Source} `\n" +
                $"> - Target Site: `{_E.TargetSite} `\n\n" +
                $"Stack Trace: ```{_E.StackTrace} ```\n" +
                $"Inner Exception: ```{_E.InnerException} ```");

            string Text = $"[SLBr] {ReleaseVersion}\n\n" +
                $"[Message] {_E.Message}\n" +
                $"[Source] {_E.Source}\n\n" +
                $"[Target Site] {_E.TargetSite}\n\n" +
                $"[Stack Trace] {_E.StackTrace}\n\n" +
                $"[Inner Exception] {_E.InnerException}";
            MessageBox.Show(Text);
        }
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo($"**Automatic Report**\n" +
                $"> - Version: `{ReleaseVersion}`\n> \n" +
                $"> - Message: ```{e.Exception.Message}```\n> \n" +
                $"> - Source: `{e.Exception.Source} `\n" +
                $"> - Target Site: `{e.Exception.TargetSite} `\n\n" +
                $"Stack Trace: ```{e.Exception.StackTrace} ```\n" +
                $"Inner Exception: ```{e.Exception.InnerException} ```");

            string Text = $"[SLBr] {ReleaseVersion}\n\n" +
                $"[Message] {e.Exception.Message}\n" +
                $"[Source] {e.Exception.Source}\n\n" +
                $"[Target Site] {e.Exception.TargetSite}\n\n" +
                $"[Stack Trace] {e.Exception.StackTrace}\n\n" +
                $"[Inner Exception] {e.Exception.InnerException}";
            MessageBox.Show(Text);
        }
        public int TrackersBlocked;
        public int AdsBlocked;
        public void AdBlock(bool Boolean)
        {
            GlobalSave.Set("AdBlock", Boolean.ToString());
            _RequestHandler.AdBlock = Boolean;
            foreach (MainWindow Window in AllWindows)
            {
                foreach (BrowserTabItem Browser in Window.Tabs)
                {
                    Browser BrowserView = Window.GetBrowserView(Browser);
                    if (BrowserView != null)
                    {
                        if (BrowserView.Chromium != null)
                            ((RequestHandler)BrowserView.Chromium.RequestHandler).AdBlock = Boolean;
                    }
                }
            }
        }
        public void TrackerBlock(bool Boolean)
        {
            GlobalSave.Set("TrackerBlock", Boolean.ToString());
            _RequestHandler.TrackerBlock = Boolean;
            foreach (MainWindow Window in AllWindows)
            {
                foreach (BrowserTabItem Browser in Window.Tabs)
                {
                    Browser BrowserView = Window.GetBrowserView(Browser);
                    if (BrowserView != null)
                    {
                        if (BrowserView.Chromium != null)
                            ((RequestHandler)BrowserView.Chromium.RequestHandler).TrackerBlock = Boolean;
                    }
                }
            }
        }
        public void SetRenderMode(string Mode, bool Notify)
        {
            if (Mode == "Hardware")
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            else if (Mode == "Software")
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            GlobalSave.Set("RenderMode", Mode);
        }
        public void UpdateTabUnloadingTimer(int Time = -1)
        {
            if (Time != -1)
                GlobalSave.Set("TabUnloadingTime", Time);
            foreach (MainWindow _Window in AllWindows)
                _Window.UpdateUnloadTimer();
                //_Window.GCTimer.Interval = new TimeSpan(0, int.Parse(GlobalSave.Get("TabUnloadingTime")), 0);
        }

        private ObservableCollection<ActionStorage> PrivateLanguages = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> Languages
        {
            get { return PrivateLanguages; }
            set
            {
                PrivateLanguages = value;
                RaisePropertyChanged("CompletedDownloads");
            }
        }
        public ActionStorage Locale;

        public Dictionary<string, string> AllLocales = new Dictionary<string, string>
        {
            { "af", "Afrikaans" },
            { "af-ZA", "Afrikaans (South Africa)" },

            { "ar", "Arabic" },
            { "ar-SA", "Arabic (Saudi Arabia)" },
            { "ar-EG", "Arabic (Egypt)" },
            { "ar-MA", "Arabic (Morocco)" },
            { "ar-LB", "Arabic (Lebanon)" },

            { "az", "Azerbaijani" },
            { "az-AZ", "Azerbaijani (Azerbaijan)" },

            { "bg", "Bulgarian" },
            { "bg-BG", "Bulgarian (Bulgaria)" },

            { "bn", "Bengali" },
            { "bn-BD", "Bengali (Bangladesh)" },
            { "bn-IN", "Bengali (India)" },

            { "cs", "Czech" },
            { "cs-CZ", "Czech (Czech Republic)" },

            { "da", "Danish" },
            { "da-DK", "Danish (Denmark)" },

            { "de", "German" },
            { "de-DE", "German (Germany)" },
            { "de-CH", "German (Switzerland)" },

            { "el", "Greek" },
            { "el-GR", "Greek (Greece)" },

            { "en", "English" },
            { "en-US", "English (United States)" },
            { "en-GB", "English (United Kingdom)" },
            { "en-CA", "English (Canada)" },
            { "en-AU", "English (Australia)" },

            { "es", "Spanish" },
            { "es-ES", "Spanish (Spain)" },
            { "es-MX", "Spanish (Mexico)" },
            { "es-AR", "Spanish (Argentina)" },

            { "fa", "Persian" },
            { "fa-IR", "Persian (Iran)" },

            { "fi", "Finnish" },
            { "fi-FI", "Finnish (Finland)" },

            { "fr", "French" },
            { "fr-FR", "French (France)" },
            { "fr-CA", "French (Canada)" },

            { "he", "Hebrew" },
            { "he-IL", "Hebrew (Israel)" },

            { "hi", "Hindi" },
            { "hi-IN", "Hindi (India)" },

            { "hu", "Hungarian" },
            { "hu-HU", "Hungarian (Hungary)" },

            { "id", "Indonesian" },
            { "id-ID", "Indonesian (Indonesia)" },

            { "it", "Italian" },
            { "it-IT", "Italian (Italy)" },
            { "it-CH", "Italian (Switzerland)" },

            { "ja", "Japanese" },
            { "ja-JP", "Japanese (Japan)" },

            { "ko", "Korean" },
            { "ko-KR", "Korean (South Korea)" },

            { "ms", "Malay" },
            { "ms-MY", "Malay (Malaysia)" },

            { "nl", "Dutch" },
            { "nl-NL", "Dutch (Netherlands)" },

            { "pl", "Polish" },
            { "pl-PL", "Polish (Poland)" },

            { "pt", "Portuguese" },
            { "pt-PT", "Portuguese (Portugal)" },
            { "pt-BR", "Portuguese (Brazil)" },

            { "ro", "Romanian" },
            { "ro-RO", "Romanian (Romania)" },

            { "ru", "Russian" },
            { "ru-RU", "Russian (Russia)" },

            { "sv", "Swedish" },
            { "sv-SE", "Swedish (Sweden)" },

            { "th", "Thai" },
            { "th-TH", "Thai (Thailand)" },

            { "tr", "Turkish" },
            { "tr-TR", "Turkish (Turkey)" },

            { "uk", "Ukrainian" },
            { "uk-UA", "Ukrainian (Ukraine)" },

            { "vi", "Vietnamese" },
            { "vi-VN", "Vietnamese (Vietnam)" },

            { "zh", "Chinese" },
            { "zh-CN", "Chinese (Simplified, China)" },
            { "zh-TW", "Chinese (Traditional, Taiwan)" },
            { "zh-HK", "Chinese (Hong Kong)" },

            { "zu", "Zulu" },
            { "zu-ZA", "Zulu (South Africa)" },
        };

        private void InitializeSaves()
        {
            GlobalSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            LanguagesSave = new Saving("Languages.bin", UserApplicationDataPath);

            if (!Directory.Exists(UserApplicationWindowsPath))
                Directory.CreateDirectory(UserApplicationWindowsPath);
            int WindowsSavesCount = Directory.EnumerateFiles(UserApplicationWindowsPath).Count();
            if (WindowsSavesCount != 0)
            {
                for (int i = 0; i < WindowsSavesCount; i++)
                    WindowsSaves.Add(new Saving($"Window_{i}.bin", UserApplicationWindowsPath));
            }
            else
                WindowsSaves.Add(new Saving($"Window_0.bin", UserApplicationWindowsPath));

            if (SearchSave.Has("Count"))
            {
                if (int.Parse(SearchSave.Get("Count")) == 0)
                    SearchEngines = new List<string>(DefaultSearchEngines);
                else
                {
                    for (int i = 0; i < int.Parse(SearchSave.Get("Count")); i++)
                    {
                        string Url = SearchSave.Get($"{i}");
                        if (!SearchEngines.Contains(Url))
                            SearchEngines.Add(Url);
                    }
                }
            }
            else
                SearchEngines = new List<string>(DefaultSearchEngines);

            if (LanguagesSave.Has("Count"))
            {
                if (int.Parse(LanguagesSave.Get("Count")) == 0)
                {
                    Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en-US"), "", "en-US"));
                    Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en"), "", "en"));
                    Locale = Languages[0];
                }
                else
                {
                    for (int i = 0; i < int.Parse(LanguagesSave.Get("Count")); i++)
                    {
                        string ISO = LanguagesSave.Get($"{i}");
                        if (AllLocales.TryGetValue(ISO, out string Name))
                            Languages.Add(new ActionStorage(Name, "", ISO));
                    }
                    Locale = Languages[int.Parse(LanguagesSave.Get("Selected"))];
                }
            }
            else
            {
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en-US"), "", "en-US"));
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en"), "", "en"));
                Locale = Languages[0];
            }

            SetGoogleSafeBrowsing(bool.Parse(GlobalSave.Get("GoogleSafeBrowsing", false.ToString())));

            if (!GlobalSave.Has("SearchSuggestions"))
                GlobalSave.Set("SearchSuggestions", true);
            if (!GlobalSave.Has("SuggestionsSource"))
                GlobalSave.Set("SuggestionsSource", "Google");
            if (!GlobalSave.Has("SpellCheck"))
                GlobalSave.Set("SpellCheck", true);
            if (!GlobalSave.Has("SearchEngine"))
                GlobalSave.Set("SearchEngine", DefaultSearchEngines.Find(i => i.StartsWith("https://www.ecosia.org")));

            if (!GlobalSave.Has("Homepage"))
                GlobalSave.Set("Homepage", "slbr://newtab");
            if (!GlobalSave.Has("Theme"))
                GlobalSave.Set("Theme", "Auto");
            TrackersBlocked = int.Parse(StatisticsSave.Get("BlockedTrackers", "0"));
            AdsBlocked = int.Parse(StatisticsSave.Get("BlockedAds", "0"));

            if (!GlobalSave.Has("TabUnloading"))
                GlobalSave.Set("TabUnloading", true.ToString());
            if (!GlobalSave.Has("ShowUnloadProgress"))
                GlobalSave.Set("ShowUnloadProgress", false.ToString());
            if (!GlobalSave.Has("DimUnloadedIcon"))
                GlobalSave.Set("DimUnloadedIcon", true);
            if (!GlobalSave.Has("ShowUnloadedIcon"))
                GlobalSave.Set("ShowUnloadedIcon", true);
            UpdateTabUnloadingTimer(int.Parse(GlobalSave.Get("TabUnloadingTime", "10")));
            /*if (!GlobalSave.Has("IPFS"))
                GlobalSave.Set("IPFS", true.ToString());
            if (!GlobalSave.Has("Wayback"))
                GlobalSave.Set("Wayback", true.ToString());
            if (!GlobalSave.Has("Gemini"))
                GlobalSave.Set("Gemini", true.ToString());
            if (!GlobalSave.Has("Gopher"))
                GlobalSave.Set("Gopher", true.ToString());*/
            if (!GlobalSave.Has("DownloadPrompt"))
                GlobalSave.Set("DownloadPrompt", true.ToString());
            if (!GlobalSave.Has("DownloadPath"))
                GlobalSave.Set("DownloadPath", Utils.GetFolderPath(Utils.FolderGuids.Downloads));
            if (!GlobalSave.Has("ScreenshotPath"))
                GlobalSave.Set("ScreenshotPath", Path.Combine(Utils.GetFolderPath(Utils.FolderGuids.Pictures), "Screenshots", "SLBr"));

            if (!GlobalSave.Has("SendDiagnostics"))
                GlobalSave.Set("SendDiagnostics", true);
            if (!GlobalSave.Has("WebNotifications"))
                GlobalSave.Set("WebNotifications", true);
            if (!GlobalSave.Has("AdaptiveTheme"))
                GlobalSave.Set("AdaptiveTheme", false);

            if (!GlobalSave.Has("ScreenshotFormat"))
                GlobalSave.Set("ScreenshotFormat", "Jpeg");

            /*if (!IESave.Has("IESuppressErrors"))
                IESave.Set("IESuppressErrors", true);*/

            if (!GlobalSave.Has("RestoreTabs"))
                GlobalSave.Set("RestoreTabs", true);
            if (!GlobalSave.Has("SmoothScroll"))
                GlobalSave.Set("SmoothScroll", true);

            /*if (!SandboxSave.Has("Framerate"))
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
            WebGL = bool.Parse(SandboxSave.Get("WebGL")).ToCefState();*/

            if (!GlobalSave.Has("ChromiumHardwareAcceleration"))
                GlobalSave.Set("ChromiumHardwareAcceleration", true);
            if (!GlobalSave.Has("ExperimentalFeatures"))
                GlobalSave.Set("ExperimentalFeatures", false);
            if (!GlobalSave.Has("LiteMode"))
                GlobalSave.Set("LiteMode", false);
            if (!GlobalSave.Has("PDFViewerExtension"))
                GlobalSave.Set("PDFViewerExtension", true);

            /*if (!GlobalSave.Has("AngleGraphicsBackend"))
                GlobalSave.Set("AngleGraphicsBackend", "Default");
            if (!GlobalSave.Has("MSAASampleCount"))
                GlobalSave.Set("MSAASampleCount", 2);
            if (!GlobalSave.Has("RendererProcessLimit"))
                GlobalSave.Set("RendererProcessLimit", 2);
            if (!GlobalSave.Has("SiteIsolation"))
                GlobalSave.Set("SiteIsolation", true);
            if (!GlobalSave.Has("SkipLowPriorityTasks"))
                GlobalSave.Set("SkipLowPriorityTasks", true);
            if (!GlobalSave.Has("PrintRaster"))
                GlobalSave.Set("PrintRaster", true);
            if (!GlobalSave.Has("Prerender"))
                GlobalSave.Set("Prerender", true);
            if (!GlobalSave.Has("SpeculativePreconnect"))
                GlobalSave.Set("SpeculativePreconnect", true);
            if (!GlobalSave.Has("PrefetchDNS"))
                GlobalSave.Set("PrefetchDNS", true);*/

            if (!GlobalSave.Has("HomepageBackground"))
                GlobalSave.Set("HomepageBackground", "Custom");
            if (!GlobalSave.Has("BingBackground"))
                GlobalSave.Set("BingBackground", "Image of the day");
            if (!GlobalSave.Has("CustomBackgroundQuery"))
                GlobalSave.Set("CustomBackgroundQuery", "");
            if (!GlobalSave.Has("CustomBackgroundImage"))
                GlobalSave.Set("CustomBackgroundImage", "");

            if (!GlobalSave.Has("BlockFingerprint"))
                GlobalSave.Set("BlockFingerprint", false);
            if (!GlobalSave.Has("FingerprintLevel"))
                GlobalSave.Set("FingerprintLevel", "Minimal");

            if (!GlobalSave.Has("FlagEmoji"))
                GlobalSave.Set("FlagEmoji", false);

            /*if (!GlobalSave.Has("DefaultBrowserEngine"))
                GlobalSave.Set("DefaultBrowserEngine", 0);*/
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                    Themes.Add(new Theme("Auto", (key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
            }
            catch
            {
                if (GlobalSave.Get("Theme") == "Auto")
                    GlobalSave.Set("Theme", "Dark");
            }
            SetAppearance(GetTheme(GlobalSave.Get("Theme", "Auto")), GlobalSave.Get("TabAlignment", "Horizontal"), bool.Parse(GlobalSave.Get("HomeButton", true.ToString())), bool.Parse(GlobalSave.Get("TranslateButton", true.ToString())), bool.Parse(GlobalSave.Get("AIButton", true.ToString())), bool.Parse(GlobalSave.Get("ReaderButton", false.ToString())));
        }
        private void InitializeUISaves(string CommandLineUrl = "")
        {
            AdBlock(bool.Parse(GlobalSave.Get("AdBlock", true.ToString())));
            TrackerBlock(bool.Parse(GlobalSave.Get("TrackerBlock", true.ToString())));

            if (!GlobalSave.Has("RenderMode"))
            {
                int renderingTier = RenderCapability.Tier >> 16;
                string  _RenderMode = renderingTier == 0 ? "Software" : "Hardware";
                SetRenderMode(_RenderMode, false);
            }
            else
                SetRenderMode(GlobalSave.Get("RenderMode"), true);
            
            if (FavouritesSave.Has("Favourite_Count"))
            {
                for (int i = 0; i < int.Parse(FavouritesSave.Get("Favourite_Count")); i++)
                {
                    string[] Value = FavouritesSave.Get($"Favourite_{i}", true);
                    Favourites.Add(new ActionStorage(Value[1], $"4<,>{Value[0]}", Value[0]));
                }
            }
            if (bool.Parse(GlobalSave.Get("RestoreTabs")))
            {
                for (int t = 0; t < WindowsSaves.Count; t++)
                {
                    Saving TabsSave = WindowsSaves[t];
                    MainWindow _Window = new MainWindow();
                    if (int.Parse(TabsSave.Get("Count", "0")) > 0)
                    {
                        int SelectedIndex = int.Parse(TabsSave.Get("Selected", 0.ToString()));
                        for (int i = 0; i < int.Parse(TabsSave.Get("Count")); i++)
                        {
                            string Url = TabsSave.Get(i.ToString());
                            if (Url != "NOTFOUND")
                                _Window.NewTab(Url);
                        }
                        _Window.TabsUI.SelectedIndex = SelectedIndex;
                    }
                    else
                        _Window.NewTab(GlobalSave.Get("Homepage"));
                    _Window.Show();
                }
            }
            if (!string.IsNullOrEmpty(CommandLineUrl))
                CurrentFocusedWindow().NewTab(CommandLineUrl, true);
        }

        public MainWindow CurrentFocusedWindow()
        {
            var focusedWindow = AllWindows.FirstOrDefault(w => w.IsFocused);
            if (focusedWindow != null) return focusedWindow;

            var activeWindow = AllWindows.FirstOrDefault(w => w.IsActive);
            if (activeWindow != null) return activeWindow;

            var maximizedWindow = AllWindows.FirstOrDefault(w => w.WindowState == WindowState.Maximized);
            if (maximizedWindow != null) return maximizedWindow;

            var normalWindow = AllWindows.FirstOrDefault(w => w.WindowState == WindowState.Normal);
            if (normalWindow != null) return normalWindow;
            return null;
        }

        /*public MainWindow CurrentFocusedWindow()
        {
            foreach (MainWindow _Window in AllWindows)
                if (_Window.IsFocused || _Window.IsActive || _Window.WindowState == WindowState.Maximized || _Window.WindowState == WindowState.Normal) return _Window;
            return null;
        }*/

        public void Refresh(bool IgnoreCache = false)
        {
            CurrentFocusedWindow().Refresh("", IgnoreCache);
        }
        public void Fullscreen()
        {
            CurrentFocusedWindow().Fullscreen(!CurrentFocusedWindow().IsFullscreen);
        }
        public void DevTools(string Id = "")
        {
            CurrentFocusedWindow().DevTools(Id);
        }
        public void Find(string Text = "")
        {
            CurrentFocusedWindow().Find(Text);
        }
        public void Screenshot()
        {
            CurrentFocusedWindow().Screenshot();
        }
        public void NewWindow()
        {
            MainWindow _Window = new MainWindow();
            _Window.Show();
            _Window.NewTab(GlobalSave.Get("Homepage"), true);
        }

        public LifeSpanHandler _LifeSpanHandler;
        public DownloadHandler _DownloadHandler;
        public LimitedContextMenuHandler _LimitedContextMenuHandler;
        public RequestHandler _RequestHandler;
        public ContextMenuHandler _ContextMenuHandler;
        public KeyboardHandler _KeyboardHandler;
        public JsDialogHandler _JsDialogHandler;
        public PermissionHandler _PermissionHandler;
        public PrivateJsObjectHandler _PrivateJsObjectHandler;
        public SafeBrowsingHandler _SafeBrowsing;

        public string ReleaseVersion;

        private void InitializeCEF()
        {
            _LifeSpanHandler = new LifeSpanHandler(false);
            _DownloadHandler = new DownloadHandler();
            _RequestHandler = new RequestHandler();
            _LimitedContextMenuHandler = new LimitedContextMenuHandler();
            _ContextMenuHandler = new ContextMenuHandler();
            _KeyboardHandler = new KeyboardHandler();
            _JsDialogHandler = new JsDialogHandler();
            _PrivateJsObjectHandler = new PrivateJsObjectHandler();
            _PermissionHandler = new PermissionHandler();

            //_KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(delegate () { Refresh(); }, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Refresh(true); }, (int)Key.F5, true);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(); }, (int)Key.F11);
            _KeyboardHandler.AddKey(delegate () { DevTools(); }, (int)Key.F12);
            _KeyboardHandler.AddKey(delegate () { Find(); }, (int)Key.F, true);

            _SafeBrowsing = new SafeBrowsingHandler(SECRETS.GOOGLE_API_KEY, SECRETS.GOOGLE_DEFAULT_CLIENT_ID);

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            CefSettings Settings = new CefSettings();
            //Settings.EnablePrintPreview();

            Settings.WindowlessRenderingEnabled = false;
            Settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;

            Settings.Locale = Locale.Tooltip;
            Settings.AcceptLanguageList = string.Join(",", Languages.Select(i => i.Tooltip));
            Settings.MultiThreadedMessageLoop = true;
            Settings.CommandLineArgsDisabled = true;
            Settings.UserAgentProduct = $"SLBr/{ReleaseVersion} Chrome/{Cef.ChromiumVersion}";
            Settings.LogFile = LogPath;
            Settings.LogSeverity = LogSeverity.Error;
            Settings.CachePath = CachePath;
            Settings.RootCachePath = UserDataPath;

            SetCEFFlags(Settings);

            /*Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gemini",
                SchemeHandlerFactory = new GeminiSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gopher",
                SchemeHandlerFactory = new GopherSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "wayback",
                SchemeHandlerFactory = new WaybackSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipfs",
                SchemeHandlerFactory = new IPFSSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipns",
                SchemeHandlerFactory = new IPNSSchemeHandlerFactory()
            });*/
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
                    new Scheme { PageName = "Settings", FileName = "SettingsPlacebo.html" },
                    new Scheme { PageName = "Tetris", FileName = "Tetris.html" },
                    new Scheme { PageName = "WhatsNew", FileName = "WhatsNew.html" }

                    /*new Scheme { PageName = "Malware", FileName = "Malware.html" },
                    new Scheme { PageName = "Deception", FileName = "Deception.html" },
                    new Scheme { PageName = "ProcessCrashed", FileName = "ProcessCrashed.html" },
                    new Scheme { PageName = "CannotConnect", FileName = "CannotConnect.html" }*/
                }
            };
            string SLBrSchemeRootFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SLBrScheme.RootFolder);
            foreach (var Scheme in SLBrScheme.Schemes)
            {
                Settings.RegisterScheme(new CefCustomScheme
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

            bool ChromeRuntime = false;
            Settings.ChromeRuntime = ChromeRuntime;
            CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome;

            //Alloy_To_Chrome_Migration.Execute(Settings);
            Cef.Initialize(Settings);
            bool SpellCheck = bool.Parse(GlobalSave.Get("SpellCheck"));
            bool PDFViewerExtension = bool.Parse(GlobalSave.Get("PDFViewerExtension"));
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                /*string _Preferences = "";
                foreach (KeyValuePair<string, object> e in GlobalRequestContext.GetAllPreferences(true))
                {
                    _Preferences = GetPreferencesString(_Preferences, "", e);
                }
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt")))
                {
                    outputFile.Write(_Preferences);
                }*/

                string Error;
                
                if (ChromeRuntime)
                {
                    GlobalRequestContext.SetPreference("browser_labs_enabled", false, out Error);
                    GlobalRequestContext.SetPreference("allow_dinosaur_easter_egg", false, out Error);
                    GlobalRequestContext.SetPreference("download_bubble_enabled", false, out Error);
                    GlobalRequestContext.SetPreference("feedback_allowed", false, out Error);
                    GlobalRequestContext.SetPreference("ntp.promo_visible", false, out Error);
                    GlobalRequestContext.SetPreference("ntp.shortcust_visible", false, out Error);
                    GlobalRequestContext.SetPreference("ntp_snippets.enable", false, out Error);
                    GlobalRequestContext.SetPreference("ntp_snippets_by_dse.enable", false, out Error);
                    GlobalRequestContext.SetPreference("search.suggest_enabled", false, out Error);
                    GlobalRequestContext.SetPreference("side_search.enabled", false, out Error);
                    GlobalRequestContext.SetPreference("shopping_list_enabled.enabled", false, out Error);
                    //GlobalRequestContext.SetPreference("https_only_mode_enabled", true, out Error);
                }
                //GlobalRequestContext.SetPreference("enable_do_not_track", bool.Parse(GlobalSave.Get("DoNotTrack")), out errorMessage);
                GlobalRequestContext.SetPreference("net.network_prediction_options", 1, out Error);

                GlobalRequestContext.SetPreference("safebrowsing.enabled", false, out Error);
                GlobalRequestContext.SetPreference("browser.theme.follows_system_colors", false, out Error);

                GlobalRequestContext.SetPreference("browser.enable_spellchecking", SpellCheck, out Error);
                GlobalRequestContext.SetPreference("spellcheck.dictionaries", Languages.Select(i => i.Tooltip), out Error);
                GlobalRequestContext.SetPreference("background_mode.enabled", false, out Error);

                GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !PDFViewerExtension, out Error);
                GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !PDFViewerExtension, out Error);

                //GlobalRequestContext.SetPreference("profile.default_content_setting_values.automatic_downloads", 1, out Error);

                GlobalRequestContext.SetPreference("download_bubble.partial_view_enabled", false, out Error);

                if (bool.Parse(GlobalSave.Get("BlockFingerprint")))
                    GlobalRequestContext.SetPreference("webrtc.ip_handling_policy", "disable_non_proxied_udp", out Error);

                //GlobalRequestContext.SetPreference("profile.content_settings.enable_quiet_permission_ui.geolocation", false, out Error);

                //profile.block_third_party_cookies
                //cefSettings.CefCommandLineArgs.Add("ssl-version-min", "tls1.2");

                //webkit.webprefs.encrypted_media_enabled : True

                //GlobalRequestContext.SetPreference("extensions.storage.garbagecollect", true, out errorMessage);
            });
        }

        public string GetPreferencesString(string _String, string Parents, KeyValuePair<string, object> ObjectPair)
        {
            if (ObjectPair.Value is ExpandoObject expando)
            {
                foreach (KeyValuePair<string, object> property in (IDictionary<string, object>)expando)
                {
                    _String = $"{GetPreferencesString(_String, Parents + $"[{ObjectPair.Key}]", property)}";
                }
                if (string.IsNullOrEmpty(Parents))
                    _String += "\n";
            }
            else if (ObjectPair.Value is List<object> _List)
            {
                _String += string.Join(", ", _List);
            }
            else
            {
                if (!string.IsNullOrEmpty(Parents))
                    _String += $"{Parents}: ";
                _String += $"{ObjectPair.Key}: {ObjectPair.Value}\n";
            }
            return _String;
        }

        public string Cannot_Connect_Error = @"<!DOCTYPE html>
<html>
<head>
    <title>Unable to connect to {Site}</title>
    <style>
        body { text-align: center; width: 100%; margin: 0px; font-family: 'Segoe UI', Tahoma, sans-serif; }
        #content { width: 100%; margin-top: 140px; }
        .icon { font-family: 'Segoe Fluent Icons'; font-size: 150px; user-select: none; }
        a { color: skyblue; text-decoration: none; };
    </style>
</head>
<body>
    <div id=""content"">
        <h1 class=""icon""></h1>
        <h2 id=""title"">Unable to connect to {Site}</h2>
        <h5 id=""description"">
            {Description}
        </h5>
        <h5 id=""error"" style=""margin: 0 0 0 0; color: #646464;"">
            {Error}
        </h5>
    </div>
</body>
</html>";
        public string Process_Crashed_Error = @"<!DOCTYPE html>
<html>
<head>
    <title>Process crashed</title>
    <style>
        body { text-align: center; width: 100%; margin: 0px; font-family: 'Segoe UI', Tahoma, sans-serif; }
        #content { width: 100%; margin-top: 140px; }
        .icon { font-family: 'Segoe Fluent Icons'; font-size: 150px; user-select: none; }
        a { color: skyblue; text-decoration: none; };
    </style>
</head>
<body>
    <div id=""content"">
        <h1 class=""icon""></h1>
        <h2>Process Crashed</h2>
        <h5>Process crashed while attempting to load content. Undo / Refresh the page to resolve the problem.</h5>
        <a href=""slbr://newtab"">Return to homepage</a>
    </div>
</body>
</html>";
        public string Deception_Error = @"<!DOCTYPE html>
<html>
<head>
    <title>Site access denied</title>
    <style>
        body { text-align: center; width: 100%; margin: 0px; font-family: 'Segoe UI', Tahoma, sans-serif; }
        #content { width: 100%; margin-top: 140px; }
        .icon { font-family: 'Segoe Fluent Icons'; font-size: 150px; user-select: none; }
        a { color: skyblue; text-decoration: none; };
    </style>
</head>
<body>
    <div id=""content"">
        <h1 class=""icon""></h1>
        <h2>Site Access Denied</h2>
        <h5>The site ahead was detected to contain deceptive content.</h5>
        <a href=""slbr://newtab"">Return to homepage</a>
    </div>
</body>
</html>";
        public string Malware_Error = @"<!DOCTYPE html>
<html>
<head>
    <title>Site access denied</title>
    <style>
        html { background: darkred; }
        body { text-align: center; width: 100%; margin: 0px; font-family: 'Segoe UI', Tahoma, sans-serif; }
        #content { width: 100%; margin-top: 140px; }
        .icon { font-family: 'Segoe Fluent Icons'; font-size: 150px; user-select: none; }
        a { color: skyblue; text-decoration: none; };
    </style>
</head>
<body>
    <div id=""content"">
        <h1 class=""icon""></h1>
        <h2>Site Access Denied</h2>
        <h5>The site ahead was detected to contain unwanted software / malware.</h5>
        <a href=""slbr://newtab"">Return to homepage</a>
    </div>
</body>
</html>";

        private void SetCEFFlags(CefSettings Settings)
        {
            SetChromeFlags(Settings);
            SetBackgroundFlags(Settings);
            SetNetworkFlags(Settings);
            SetUIFlags(Settings);
            SetFrameworkFlags(Settings);
            SetGraphicsFlags(Settings);
            SetMediaFlags(Settings);
            SetSecurityFlags(Settings);
            SetJavascriptFlags(Settings);
            //force-gpu-mem-available-mb https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/gpu_switches.cc
            //disable-file-system Disable FileSystem API.
        }

        private void SetChromeFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("disable-fre");
            Settings.CefCommandLineArgs.Add("no-default-browser-check");
            Settings.CefCommandLineArgs.Add("no-first-run");
            Settings.CefCommandLineArgs.Add("disable-first-run-ui");
            Settings.CefCommandLineArgs.Add("disable-ntp-most-likely-favicons-from-server");
            Settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");
            Settings.CefCommandLineArgs.Add("disable-domain-reliability");
            Settings.CefCommandLineArgs.Add("hide-crash-restore-bubble");
            Settings.CefCommandLineArgs.Add("disable-chrome-login-prompt");
            Settings.CefCommandLineArgs.Add("disable-chrome-tracing-computation");
            Settings.CefCommandLineArgs.Add("no-network-profile-warning");
            Settings.CefCommandLineArgs.Add("disable-login-animations");
            Settings.CefCommandLineArgs.Add("disable-search-engine-choice-screen");

            Settings.CefCommandLineArgs.Add("disable-ntp-other-sessions-menu");
            Settings.CefCommandLineArgs.Add("disable-default-apps");
            Settings.CefCommandLineArgs.Add("metrics-recording-only");
            //Settings.CefCommandLineArgs.Add("disable-cloud-policy-on-signin");

            //Settings.CefCommandLineArgs.Add("disable-translate");
            Settings.CefCommandLineArgs.Add("disable-dinosaur-easter-egg"); //enable-dinosaur-easter-egg-alt-images
            //Settings.CefCommandLineArgs.Add("oobe-skip-new-user-check-for-testing");

            //Settings.CefCommandLineArgs.Add("disable-gaia-services"); // https://source.chromium.org/chromium/chromium/src/+/main:ash/constants/ash_switches.cc
        }

        private void SetFrameworkFlags(CefSettings Settings)
        {
            if (!bool.Parse(GlobalSave.Get("LiteMode")))
            {
                Settings.CefCommandLineArgs.Add("enable-wasm");
                Settings.CefCommandLineArgs.Add("enable-webassembly");
                Settings.CefCommandLineArgs.Add("enable-asm-webassembly");
                Settings.CefCommandLineArgs.Add("enable-webassembly-threads");
                Settings.CefCommandLineArgs.Add("enable-webassembly-baseline");
                Settings.CefCommandLineArgs.Add("enable-webassembly-tiering");
                Settings.CefCommandLineArgs.Add("enable-webassembly-lazy-compilation");
                Settings.CefCommandLineArgs.Add("enable-webassembly-streaming");
                Settings.CefCommandLineArgs.Add("enable-webassembly-memory64");
            }

            if (!bool.Parse(GlobalSave.Get("PDFViewerExtension")))
                Settings.CefCommandLineArgs.Add("disable-pdf-extension");
            else
            {
                Settings.CefCommandLineArgs.Add("pdf-use-skia-renderer");
                Settings.CefCommandLineArgs.Add("pdf-oopif");
                Settings.CefCommandLineArgs.Add("pdf-portfolio");
                Settings.CefCommandLineArgs.Add("pdf-ink2");
            }

            if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
            {
                Settings.CefCommandLineArgs.Add("enable-experimental-cookie-features");

                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-jspi");

                Settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");

                Settings.CefCommandLineArgs.Add("enable-javascript-harmony");
                Settings.CefCommandLineArgs.Add("enable-javascript-experimental-shared-memory");

                Settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
                Settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption-experiment");
                Settings.CefCommandLineArgs.Add("text-box-trim");

                Settings.CefCommandLineArgs.Add("enable-devtools-experiments");

                Settings.CefCommandLineArgs.Add("enable-webgl-developer-extensions");
                Settings.CefCommandLineArgs.Add("enable-webgl-draft-extensions");
                Settings.CefCommandLineArgs.Add("enable-webgpu-developer-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-extension-apis");
                Settings.CefCommandLineArgs.Add("enable-devtools-experiments");

                //settings.CefCommandLineArgs.Add("webxr-incubations");
                //settings.CefCommandLineArgs.Add("enable-generic-sensor-extra-classes");
            }
        }

        private void SetBackgroundFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("enable-finch-seed-delta-compression");
            //https://chromium.googlesource.com/chromium/src/+/refs/tags/77.0.3865.0/third_party/blink/renderer/platform/graphics/dark_mode_settings.h
            Settings.CefCommandLineArgs.Add("dark-mode-settings", "ImagePolicy=1");
            Settings.CefCommandLineArgs.Add("component-updater", "fast-update");


            /*Settings.CefCommandLineArgs.Add("site-isolation-trial-opt-out");
            Settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            Settings.CefCommandLineArgs.Add("renderer-process-limit", "2");
            Settings.CefCommandLineArgs.Add("isolate-origins", "https://challenges.cloudflare.com");*/
            //Settings.CefCommandLineArgs.Add("enable-raster-side-dark-mode-for-images");

            Settings.CefCommandLineArgs.Add("disable-adpf"); //https://source.chromium.org/chromium/chromium/src/+/main:components/viz/common/switches.cc
            Settings.CefCommandLineArgs.Add("ignore-autocomplete-off-autofill"); //https://source.chromium.org/chromium/chromium/src/+/main:components/autofill/core/common/autofill_switches.cc

            Settings.CefCommandLineArgs.Add("process-per-site");

            if (bool.Parse(GlobalSave.Get("LiteMode")))
            {
                //Turns device memory into 0.5
                Settings.CefCommandLineArgs.Add("enable-low-end-device-mode");//Causes memory to be 20 MB more when minimized, but reduces 80 MB when not minimized

                Settings.CefCommandLineArgs.Add("disable-smooth-scrolling");
                Settings.CefCommandLineArgs.Add("disable-prefetch");
                Settings.CefCommandLineArgs.Add("disable-preconnect");
                Settings.CefCommandLineArgs.Add("dns-prefetch-disable");
                Settings.CefCommandLineArgs.Add("disable-image-animation");

                //Settings.CefCommandLineArgs.Add("disable-background-timer-throttling");
                Settings.CefCommandLineArgs.Add("force-low-power-gpu");
                Settings.CefCommandLineArgs.Add("disable-low-res-tiling"); // https://codereview.chromium.org/196473007/

                Settings.CefCommandLineArgs.Add("force-prefers-reduced-motion");
                //Settings.CefCommandLineArgs.Add("use-mobile-user-agent");

                //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_switches.cc
                //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/optimization_guide/hints_fetcher_browsertest.cc
                //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
                Settings.CefCommandLineArgs.Add("disable-fetching-hints-at-navigation-start");
                Settings.CefCommandLineArgs.Add("disable-model-download-verification");
            }
            else if (!bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
                Settings.CefCommandLineArgs.Add("enable-low-res-tiling");
            //else
            Settings.CefCommandLineArgs.Add("expensive-background-timer-throttling");

            Settings.CefCommandLineArgs.Add("memory-model", "low");
            Settings.CefCommandLineArgs.Add("back-forward-cache");

            Settings.CefCommandLineArgs.Add("disable-highres-timer");
            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139

            //Settings.CefCommandLineArgs.Add("disable-best-effort-tasks"); //PREVENTS GOOGLE LOGIN, DO NOT ADD

            Settings.CefCommandLineArgs.Add("aggressive-cache-discard");
            Settings.CefCommandLineArgs.Add("enable-simple-cache-backend");
            Settings.CefCommandLineArgs.Add("use-simple-cache-backend");

            //https://github.com/portapps/brave-portable/issues/26
            //https://github.com/chromium/chromium/blob/2ca8c5037021c9d2ecc00b787d58a31ed8fc8bcb/third_party/blink/renderer/bindings/core/v8/v8_cache_options.h
            //Settings.CefCommandLineArgs.Add("v8-cache-options");

            Settings.CefCommandLineArgs.Add("enable-font-cache-scaling");
            Settings.CefCommandLineArgs.Add("enable-memory-coordinator");

            //Settings.CefCommandLineArgs.Add("stale-while-revalidate");


            Settings.CefCommandLineArgs.Add("disable-background-mode");
            Settings.CefCommandLineArgs.Add("stop-loading-in-background");
            Settings.CefCommandLineArgs.Add("disable-v8-idle-tasks");


            //Settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes"); //Causes memory to be 100 MB more than if disabled when minimized
            //Settings.CefCommandLineArgs.Add("quick-intensive-throttling-after-loading"); //Causes memory to be 100 MB more than if disabled when minimized

            Settings.CefCommandLineArgs.Add("intensive-wake-up-throttling");
            Settings.CefCommandLineArgs.Add("align-wakeups");

            Settings.CefCommandLineArgs.Add("calculate-native-win-occlusion");
            //Settings.CefCommandLineArgs.Add("disable-backgrounding-occluded-windows");
            //Settings.CefCommandLineArgs.Add("enable-winrt-geolocation-implementation");

            Settings.CefCommandLineArgs.Add("disable-mipmap-generation"); // Disables mipmap generation in Skia. Used a workaround for select low memory devices


            Settings.CefCommandLineArgs.Add("enable-parallel-downloading");

            Settings.CefCommandLineArgs.Add("subframe-shutdown-delay");
            Settings.CefCommandLineArgs.Add("enable-fast-unload");
        }

        private void SetUIFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("autoplay-policy", "user-gesture-required");

            Settings.CefCommandLineArgs.Add("enable-vulkan");
            Settings.CefCommandLineArgs.Add("enable-smooth-scrolling");

            Settings.CefCommandLineArgs.Add("enable-raw-draw");
            Settings.CefCommandLineArgs.Add("disable-oop-rasterization");
            Settings.CefCommandLineArgs.Add("canvas-oop-rasterization");

            //Settings.CefCommandLineArgs.Add("prerender", "disabled");
        }

        private void SetGraphicsFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("in-process-gpu");
            if (bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
            {
                Settings.CefCommandLineArgs.Add("ignore-gpu-blocklist");
                Settings.CefCommandLineArgs.Add("enable-gpu");
                Settings.CefCommandLineArgs.Add("enable-zero-copy");
                Settings.CefCommandLineArgs.Add("disable-software-rasterizer");
                Settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
                //Settings.CefCommandLineArgs.Add("gpu-rasterization-msaa-sample-count", MainSave.Get("MSAASampleCount"));
                //if (MainSave.Get("AngleGraphicsBackend").ToLower() != "default")
                //    Settings.CefCommandLineArgs.Add("use-angle", MainSave.Get("AngleGraphicsBackend"));
                Settings.CefCommandLineArgs.Add("enable-accelerated-2d-canvas");
            }
            else
            {
                Settings.CefCommandLineArgs.Add("disable-gpu");
                Settings.CefCommandLineArgs.Add("disable-d3d11");
                Settings.CefCommandLineArgs.Add("disable-gpu-compositing");
                Settings.CefCommandLineArgs.Add("disable-direct-composition");
                Settings.CefCommandLineArgs.Add("disable-gpu-vsync");
                Settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
                Settings.CefCommandLineArgs.Add("reduce-gpu-priority-on-background");
                Settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas");
            }
        }

        private void SetNetworkFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("reduce-user-agent-platform-oscpu");
            Settings.CefCommandLineArgs.Add("reduce-accept-language");
            Settings.CefCommandLineArgs.Add("reduce-transfer-size-updated-ipc");
            Settings.CefCommandLineArgs.Add("enable-fingerprinting-protection-blocklist");

            Settings.CefCommandLineArgs.Add("enable-ipc-flooding-protection");

            Settings.CefCommandLineArgs.Add("enable-webrtc-hide-local-ips-with-mdns");
            //Settings.CefCommandLineArgs.Add("force-webrtc-ip-handling-policy");

            Settings.CefCommandLineArgs.Add("enable-quic");
            Settings.CefCommandLineArgs.Add("enable-ipv6");
            Settings.CefCommandLineArgs.Add("enable-spdy4");
            Settings.CefCommandLineArgs.Add("enable-http2");
            Settings.CefCommandLineArgs.Add("enable-tcp-fast-open");
            Settings.CefCommandLineArgs.Add("enable-brotli");


            Settings.CefCommandLineArgs.Add("no-proxy-server");
            //Settings.CefCommandLineArgs.Add("winhttp-proxy-resolver");
            Settings.CefCommandLineArgs.Add("no-pings");
            Settings.CefCommandLineArgs.Add("dns-over-https");

            Settings.CefCommandLineArgs.Add("disable-background-networking");
            Settings.CefCommandLineArgs.Add("disable-component-extensions-with-background-pages");
        }

        private void SetSecurityFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");
            Settings.CefCommandLineArgs.Add("tls13-variant");

            Settings.CefCommandLineArgs.Add("http-cache-partitioning");
            Settings.CefCommandLineArgs.Add("partitioned-cookies");

            if (bool.Parse(GlobalSave.Get("BlockFingerprint")))
            {
                Settings.UserAgentProduct = "";
                if (GlobalSave.Get("FingerprintLevel") == "Strict")
                {
                    Settings.CefCommandLineArgs.Add("disable-reading-from-canvas");
                    Settings.CefCommandLineArgs.Add("disable-webgl");
                    Settings.CefCommandLineArgs.Add("disable-extensions");
                }
                //Settings.CefCommandLineArgs.Remove("reduce-accept-language");
                //Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
            }
        }

        private void SetMediaFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("enable-jxl");

            Settings.CefCommandLineArgs.Add("disable-background-video-track");
            Settings.CefCommandLineArgs.Add("enable-lite-video");
            Settings.CefCommandLineArgs.Add("lite-video-force-override-decision");
            Settings.CefCommandLineArgs.Add("enable-av1-decoder");


            //https://chromium.googlesource.com/chromium/src/+/ae847bd2f43f6209fdc49be21c9a3ab967b8b27a/components/previews/README
            Settings.CefCommandLineArgs.Add("force-enable-lite-pages");
            Settings.CefCommandLineArgs.Add("force-effective-connection-type", "Slow-2G");
            Settings.CefCommandLineArgs.Add("ignore-previews-blocklist");
            Settings.CefCommandLineArgs.Add("enable-lazy-image-loading");
            Settings.CefCommandLineArgs.Add("enable-lazy-frame-loading");

            Settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption");
            Settings.CefCommandLineArgs.Add("enable-widevine");
            Settings.CefCommandLineArgs.Add("enable-widevine-cdm");


            if (bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
            {
                Settings.CefCommandLineArgs.Add("d3d11-video-decoder");
                Settings.CefCommandLineArgs.Add("enable-accelerated-video-decode");
                Settings.CefCommandLineArgs.Add("enable-accelerated-mjpeg-decode");
                Settings.CefCommandLineArgs.Add("enable-vp9-kSVC-decode-acceleration");
                Settings.CefCommandLineArgs.Add("enable-vaapi-av1-decode-acceleration");
                Settings.CefCommandLineArgs.Add("enable-vaapi-jpeg-image-decode-acceleration");
                Settings.CefCommandLineArgs.Add("enable-vaapi-webp-image-decode-accelerationn");
                Settings.CefCommandLineArgs.Add("enable-vbr-encode-acceleration");
                Settings.CefCommandLineArgs.Add("zero-copy-tab-capture");
                Settings.CefCommandLineArgs.Add("zero-copy-video-capture");
            }
            else
            {
                Settings.CefCommandLineArgs.Add("disable-accelerated-video");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-encode");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");
            }

            Settings.CefCommandLineArgs.Add("use-winrt-midi-api");

            Settings.CefCommandLineArgs.Add("enable-speech-api");
            Settings.CefCommandLineArgs.Add("enable-speech-input");
            Settings.CefCommandLineArgs.Add("enable-speech-dispatcher");
            Settings.CefCommandLineArgs.Add("enable-voice-input");

            //BREAKS PERMISSIONS, DO NOT ADD
            //Settings.CefCommandLineArgs.Add("enable-media-stream");

            Settings.CefCommandLineArgs.Add("enable-media-session-service");
            //Settings.CefCommandLineArgs.Add("use-fake-device-for-media-stream");
            //Settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");

            Settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            Settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            Settings.CefCommandLineArgs.Add("auto-select-desktop-capture-source");

            //Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-always");
            Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");
        }

        private void SetJavascriptFlags(CefSettings Settings)
        {
            //https://github.com/brave/brave-core/pull/19457
            string JsFlags = "--always-opt,--max-lazy,--enable-one-shot-optimization,--enable-experimental-regexp-engine-on-excessive-backtracks,--experimental-flush-embedded-blob-icache,--turbo-fast-api-calls,--gc-experiment-reduce-concurrent-marking-tasks,--lazy-feedback-allocation,--gc-global,--expose-gc,--max_old_space_size=512,--idle-time-scavenge,--lazy";
            try
            {
                Settings.CefCommandLineArgs.Add("disable-features", "OptimizationHintsFetchingSRP,PrintCompositorLPAC,NavigationPredictor,PreloadSystemFonts,Prerender2,InterestFeedContentSuggestions,WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching");
                Settings.CefCommandLineArgs.Add("enable-features", "BackForwardCacheMemoryControls,ReduceGpuPriorityOnBackground,BatterySaverModeAlignWakeUps,BatterySaverModeRenderTuning,AutomaticLazyFrameLoadingToEmbeds,AutomaticLazyFrameLoadingToAds,LightweightNoStatePrefetch,ParallelDownloading,MidiManagerWinrt,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LazyImageLoading,EnableTLS13EarlyData,AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading");
                Settings.CefCommandLineArgs.Add("enable-blink-features", "SvgTransformOptimization,InvisibleSVGAnimationThrottling,FreezeFramesOnVisibility,SkipPreloadScanning,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading");
                Settings.CefCommandLineArgs.Add("disable-blink-features", "Prerender2");

                //--enable-blink-features, V8IdleTasks, PrettyPrintJSONDocument, PrefersReducedData, ForceReduceMotion
            }
            catch
            {
                Settings.CefCommandLineArgs["disable-features"] += ",OptimizationHintsFetchingSRP,PrintCompositorLPAC,NavigationPredictor,PreloadSystemFonts,Prerender2,InterestFeedContentSuggestions,WinUseBrowserSpellChecker,AsyncWheelEvents,TouchpadAndWheelScrollLatching";
                Settings.CefCommandLineArgs["enable-features"] += ",BackForwardCacheMemoryControls,ReduceGpuPriorityOnBackground,BatterySaverModeAlignWakeUps,BatterySaverModeRenderTuning,AutomaticLazyFrameLoadingToEmbeds,AutomaticLazyFrameLoadingToAds,LightweightNoStatePrefetch,ParallelDownloading,MidiManagerWinrt,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LazyImageLoading,EnableTLS13EarlyData,AsmJsToWebAssembly,WebAssembly,WebAssemblyStreaming,ThrottleForegroundTimers,IntensiveWakeUpThrottling:grace_period_seconds/10,OptOutZeroTimeoutTimersFromThrottling,AllowAggressiveThrottlingWithWebSocket,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
                Settings.CefCommandLineArgs["enable-blink-features"] += ",SvgTransformOptimization,InvisibleSVGAnimationThrottling,FreezeFramesOnVisibility,SkipPreloadScanning,NeverSlowMode,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
                Settings.CefCommandLineArgs["disable-blink-features"] += ",Prerender2";
            }
            Settings.CefCommandLineArgs.Add("blink-settings", "smoothScrollForFindEnabled=true,dnsPrefetchingEnabled=false,doHtmlPreloadScanning=false,disallowFetchForDocWrittenScriptsInMainFrameIfEffectively2G=true,disallowFetchForDocWrittenScriptsInMainFrameOnSlowConnections=true");

            //enable-features LightweightNoStatePrefetch https://source.chromium.org/chromium/chromium/src/+/main:content/public/common/content_features.cc
            //enable-blink-features  https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/runtime_enabled_features.json5
            //blink-settings  https://source.chromium.org/chromium/chromium/src/+/main:out/lacros-Debug/gen/third_party/blink/public/mojom/webpreferences/web_preferences.mojom.js


            //if (bool.Parse(ExperimentsSave.Get("WebAssembly")))
            /*else
                JsFlags += ",--noexpose_wasm";*/

            //Disables WebAssembly
            if (bool.Parse(GlobalSave.Get("LiteMode")))
            {
                //https://github.com/cypress-io/cypress/issues/22622
                //https://issues.chromium.org/issues/40220332
                Settings.CefCommandLineArgs["disable-features"] += ",OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
                Settings.CefCommandLineArgs["enable-blink-features"] += ",PrefersReducedData";
                JsFlags += ",--lite-mode,--optimize-for-size,--noexpose_wasm";
            }
            else
            {
                JsFlags += ",--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt,--experimental-wasm-branch-hinting,--experimental-wasm-instruction-tracing";
                if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
                    JsFlags += ",--experimental-wasm-jspi,--experimental-wasm-memory64,--experimental-wasm-type-reflection";
            }

            /*if (bool.Parse(ExperimentsSave.Get("V8Sparkplug")))
                JsFlags += ",--sparkplug";
            else
                JsFlags += ",--no-sparkplug";*/
            Settings.JavascriptFlags = JsFlags;
        }


        public Theme CurrentTheme;
        public Theme GetTheme(string Name = "")
        {
            if (string.IsNullOrEmpty(Name))
            {
                if (CurrentTheme != null)
                    return CurrentTheme;
            }
            foreach (Theme _Theme in Themes)
            {
                if (_Theme.Name == Name)
                    return _Theme;
            }
            return Themes[0];
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CloseSLBr(false);
            base.OnExit(e);
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

                SearchSave.Set("Count", SearchEngines.Count.ToString(), false);
                for (int i = 0; i < SearchEngines.Count; i++)
                    SearchSave.Set($"{i}", SearchEngines[i], false);
                SearchSave.Save();

                LanguagesSave.Set("Count", Languages.Count.ToString(), false);
                LanguagesSave.Set("Selected", Languages.IndexOf(Locale), false);
                for (int i = 0; i < Languages.Count; i++)
                    LanguagesSave.Set($"{i}", Languages[i].Tooltip, false);
                LanguagesSave.Save();

                if (bool.Parse(GlobalSave.Get("RestoreTabs")))
                {
                    foreach (FileInfo file in new DirectoryInfo(UserApplicationWindowsPath).GetFiles())
                        file.Delete();
                    foreach (MainWindow _Window in AllWindows)
                    {
                        Saving TabsSave = WindowsSaves[AllWindows.IndexOf(_Window)];
                        TabsSave.Clear();

                        int Count = 0;
                        int SelectedIndex = 0;
                        for (int i = 0; i < _Window.Tabs.Count; i++)
                        {
                            BrowserTabItem Tab = _Window.Tabs[i];
                            if (Tab.ParentWindow != null)
                            {
                                Browser BrowserView = _Window.GetBrowserView(Tab);
                                TabsSave.Set($"{Count}", BrowserView.Address, false);
                                if (i == _Window.TabsUI.SelectedIndex)
                                    SelectedIndex = Count;
                                Count++;
                            }
                        }
                        TabsSave.Set("Count", Count.ToString());
                        TabsSave.Set("Selected", SelectedIndex.ToString());
                    }
                }
            }
            if (ExecuteCloseEvents)
            {
                for (int i = 0; i < AllWindows.Count; i++)
                    AllWindows[i].Close();
            }
            AppInitialized = false;
            Cef.Shutdown();
            Shutdown();
        }

        public BitmapImage TabIcon;
        public BitmapImage PDFTabIcon;
        public BitmapImage SettingsTabIcon;
        public BitmapImage HistoryTabIcon;
        public BitmapImage DownloadsTabIcon;
        public BitmapImage UnloadedIcon;

        public BitmapImage GetIcon(string Url)
        {
            if (Utils.IsHttpScheme(Url) && Utils.GetFileExtensionFromUrl(Url) != ".pdf")
                return new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Utils.CleanUrl(Url, true, true, true, false, false)));
            if (Url.StartsWith("slbr://settings"))
                return SettingsTabIcon;
            else if (Url.StartsWith("slbr://history"))
                return HistoryTabIcon;
            else if (Url.StartsWith("slbr://downloads"))
                return DownloadsTabIcon;
            return Utils.GetFileExtensionFromUrl(Url) == ".pdf" ? PDFTabIcon : TabIcon;
        }

        public async Task<BitmapImage> SetIcon(string IconUrl, string Url = "")
        {
            //if (Utils.IsHttpScheme(IconUrl) && Utils.GetFileExtensionFromUrl(Url) != ".pdf")
            //    return new BitmapImage(new Uri(IconUrl));
            try
            {
                if (Utils.GetFileExtensionFromUrl(Url) != ".pdf")
                {
                    if (Utils.IsHttpScheme(IconUrl))
                    {
                        try
                        {
                            byte[] imageData = await DownloadImageDataAsync(IconUrl);
                            if (imageData != null)
                            {
                                BitmapImage bitmap = new BitmapImage();
                                using (MemoryStream stream = new MemoryStream(imageData))
                                {
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = stream;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                }
                                return bitmap;
                            }
                            else
                                return TabIcon;
                        }
                        catch
                        {
                            return TabIcon;
                        }
                    }
                    else if (IconUrl.StartsWith("data:image/"))
                        return Utils.ConvertBase64ToBitmapImage(IconUrl);
                }
            }
            catch { }

            if (Url.StartsWith("slbr://settings"))
                return SettingsTabIcon;
            else if (Url.StartsWith("slbr://history"))
                return HistoryTabIcon;
            else if (Url.StartsWith("slbr://downloads"))
                return DownloadsTabIcon;
            return Utils.GetFileExtensionFromUrl(Url) == ".pdf" ? PDFTabIcon : TabIcon;
        }
        private async Task<byte[]> DownloadImageDataAsync(string uri)
        {
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", $"Chrome/{Cef.ChromiumVersion}");
                    _WebClient.Headers.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
                    return await _WebClient.DownloadDataTaskAsync(new Uri(uri));
                }
                catch { return null; }
            }
        }


        /*public BitmapImage SetIcon(string IconUrl, string Url = "")
        {
            //if (Utils.IsHttpScheme(IconUrl) && Utils.GetFileExtensionFromUrl(Url) != ".pdf")
            //    return new BitmapImage(new Uri(IconUrl));
            if (Utils.IsHttpScheme(IconUrl) && Utils.GetFileExtensionFromUrl(Url) != ".pdf")
            {
                try
                {
                    byte[] imageData = DownloadImageData(IconUrl);
                    if (imageData != null)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(imageData))
                        {
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                        return bitmap;
                    }
                    else
                        return TabIcon;
                }
                catch
                {
                    return TabIcon;
                }
            }

            if (Url.StartsWith("slbr://settings"))
                return SettingsTabIcon;
            else if (Url.StartsWith("slbr://history"))
                return HistoryTabIcon;
            else if (Url.StartsWith("slbr://downloads"))
                return DownloadsTabIcon;
            return Utils.GetFileExtensionFromUrl(Url) == ".pdf" ? PDFTabIcon : TabIcon;
        }

        private byte[] DownloadImageData(string uri)
        {
            //using (WebClient _WebClient = new WebClient())
            //{
            try
            {
                TinyDownloader.Headers.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{Cef.ChromiumVersion} Safari/537.36");
                TinyDownloader.Headers.Add("Accept", "image/webp,image/apng,image/*,*//*;q=0.8");
                TinyDownloader.Headers.Add("Referer", "https://www.ecosia.org/");
                TinyDownloader.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                return TinyDownloader.DownloadData(new Uri(uri));
            }
            catch { return null; }
            //}
        }*/

        public bool GoogleSafeBrowsing;

        public void SetGoogleSafeBrowsing(bool Toggle)
        {
            GoogleSafeBrowsing = Toggle;
            GlobalSave.Set("GoogleSafeBrowsing", Toggle);
        }

        public void SetDimUnloadedIcon(bool Toggle)
        {
            GlobalSave.Set("DimUnloadedIcon", Toggle);
            foreach (MainWindow _Window in AllWindows)
                _Window.SetDimUnloadedIcon(Toggle);
        }
        public void SetAppearance(Theme _Theme, string TabAlignment, bool AllowHomeButton, bool AllowTranslateButton, bool AllowAIButton, bool AllowReaderModeButton)
        {
            GlobalSave.Set("TabAlignment", TabAlignment);

            GlobalSave.Set("AIButton", AllowAIButton);
            GlobalSave.Set("TranslateButton", AllowTranslateButton);
            GlobalSave.Set("HomeButton", AllowHomeButton);
            GlobalSave.Set("ReaderButton", AllowReaderModeButton);

            CurrentTheme = _Theme;
            GlobalSave.Set("Theme", CurrentTheme.Name);

            int IconSize = 40;
            int DPI = 95;
            TextBlock textBlock = new TextBlock
            {
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                Text = "\uEC6C",
                Width = IconSize,
                Height = IconSize,
                FontSize = IconSize,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = CurrentTheme.DarkWebPage ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                Margin = new Thickness(1, 2, 0, 0)
            };
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                TabIcon = bitmapImage;
            }

            textBlock.Text = "\uEA90";
            renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                PDFTabIcon = bitmapImage;
            }

            textBlock.Text = "\uE713";
            renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                SettingsTabIcon = bitmapImage;
            }

            textBlock.Text = "\ue81c";
            renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                HistoryTabIcon = bitmapImage;
            }

            textBlock.Text = "\ue896";
            renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                DownloadsTabIcon = bitmapImage;
            }

            textBlock.Text = "\uEC0A";
            textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3AE872"));
            renderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            textBlock.Measure(new Size(IconSize, IconSize));
            textBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            renderBitmap.Render(textBlock);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                UnloadedIcon = bitmapImage;
            }

            foreach (MainWindow _Window in AllWindows)
                _Window.SetAppearance(_Theme, TabAlignment, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton);
        }
    }


    public class Theme : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void RaisePropertyChanged(string Name) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));

        #endregion
        public Theme(string _Name, Theme BaseTheme)
        {
            Name = _Name;
            PrimaryColor = BaseTheme.PrimaryColor;
            SecondaryColor = BaseTheme.SecondaryColor;
            BorderColor = BaseTheme.BorderColor;
            GrayColor = BaseTheme.GrayColor;
            FontColor = BaseTheme.FontColor;
            IndicatorColor = BaseTheme.IndicatorColor;

            DarkTitleBar = BaseTheme.DarkTitleBar;
            DarkWebPage = BaseTheme.DarkWebPage;
        }
        public Theme(string _Name, Color _PrimaryColor, Color _SecondaryColor, Color _BorderColor, Color _GrayColor, Color _FontColor, Color _IndicatorColor, bool _DarkTitleBar = false, bool _DarkWebPage = false)
        {
            Name = _Name;
            PrimaryColor = _PrimaryColor;
            SecondaryColor = _SecondaryColor;
            BorderColor = _BorderColor;
            GrayColor = _GrayColor;
            FontColor = _FontColor;
            IndicatorColor = _IndicatorColor;

            DarkTitleBar = _DarkTitleBar;
            DarkWebPage = _DarkWebPage;
        }
        public string Name
        {
            get { return DName; }
            set
            {
                DName = value;
                RaisePropertyChanged("Name");
            }
        }
        public Color BorderColor
        {
            get { return B; }
            set
            {
                B = value;
                RaisePropertyChanged("BorderColor");
            }
        }
        public Color PrimaryColor
        {
            get { return P; }
            set
            {
                P = value;
                RaisePropertyChanged("PrimaryColor");
            }
        }
        public Color SecondaryColor
        {
            get { return SC; }
            set
            {
                SC = value;
                RaisePropertyChanged("SecondaryColor");
            }
        }
        public Color GrayColor
        {
            get { return GC; }
            set
            {
                GC = value;
                RaisePropertyChanged("GrayColor");
            }
        }
        public Color FontColor
        {
            get { return F; }
            set
            {
                F = value;
                RaisePropertyChanged("FontColor");
            }
        }
        public Color IndicatorColor
        {
            get { return I; }
            set
            {
                I = value;
                RaisePropertyChanged("IndicatorColor");
            }
        }
        public bool DarkTitleBar
        {
            get { return DTB; }
            set
            {
                DTB = value;
                RaisePropertyChanged("DarkTitleBar");
            }
        }
        public bool DarkWebPage
        {
            get { return DWP; }
            set
            {
                DWP = value;
                RaisePropertyChanged("DarkWebPage");
            }
        }

        private string DName { get; set; }
        private Color P { get; set; }
        private Color F { get; set; }
        private Color B { get; set; }
        private Color I { get; set; }
        private Color SC { get; set; }
        private Color GC { get; set; }
        private bool DTB { get; set; }
        private bool DWP { get; set; }
    }

    public class UrlScheme
    {
        public string Name;
        public string RootFolder = "Resources";
        public List<Scheme> Schemes;
        public bool IsStandard = false;
        public bool IsSecure = true;
        public bool IsLocal = true;
        public bool IsCorsEnabled = false;
    }

    public class Scheme
    {
        public string PageName;
        public string FileName;
    }

    public enum Actions
    {
        Exit = 0,
        Undo = 1,
        Redo = 2,
        Refresh = 3,
        Navigate = 4,

        CreateTab = 5,
        CloseTab = 6,
        NewWindow = 7,
        UnloadTab = 8,

        DevTools = 9,
        SizeEmulator = 10,
        SetSideBarDock = 11,

        Favourite = 12,
        OpenFileExplorer = 13,
        OpenAsPopupBrowser = 14,
        SwitchUserPopup = 15,

        ReaderMode = 17,

        //SwitchBrowser = 11,

        AIChat = 20,
        AIChatFeature = 21,
        CloseSideBar = 22,
        NewsFeed = 23,

        Print = 30,
        Mute = 31,
    }

    public class ActionStorage : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public ActionStorage(string _Name, string _Arguments, string _Tooltip, bool _Toggle = false)
        {
            Name = _Name;
            Arguments = _Arguments;
            Tooltip = _Tooltip;
            Toggle = _Toggle;
        }

        public string Name
        {
            get { return DName; }
            set
            {
                DName = value;
                RaisePropertyChanged("Name");
            }
        }
        public string Arguments
        {
            get { return DArguments; }
            set
            {
                DArguments = value;
                RaisePropertyChanged("Arguments");
            }
        }
        public string Tooltip
        {
            get { return DTooltip; }
            set
            {
                DTooltip = value;
                RaisePropertyChanged("Tooltip");
            }
        }
        public bool Toggle
        {
            get { return DToggle; }
            set
            {
                DToggle = value;
                RaisePropertyChanged("Toggle");
            }
        }

        private string DName { get; set; }
        private string DArguments { get; set; }
        private string DTooltip { get; set; }
        private bool DToggle { get; set; }
    }

    public class Notification
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("tag")]
        public int Tag { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("dir")]
        public string Dir { get; set; }
    }

    public class NotificationWrapper
    {
        public string Title { get; set; }
        public Notification Body { get; set; }
    }

    public static class Alloy_To_Chrome_Migration
    {
        private static readonly List<string> FoldersToMigrate = new List<string>() { "Cache", "Code Cache", "DawnGraphiteCache", "DawnWebGPUCache", "GPUCache", "Local Storage", "Network", "Session Storage", "Shared Dictionary" };
        private static readonly List<string> FilesToMigrate = new List<string>() { "LOCK", "LOG", "Visited Links" };
        private const string AlloyStateFilename = "LocalPrefs.json";
        private const string ChromeStateFileName = "Local State";
        public static void Execute(CefSettings settings)
        {
            var CachePath = settings.CachePath;
            try
            {
                string alloyStateFile = Path.Combine(CachePath, AlloyStateFilename);
                string chromeStateFile = Path.Combine(CachePath, ChromeStateFileName);
                if (settings.ChromeRuntime && File.Exists(alloyStateFile) && !File.Exists(chromeStateFile))
                {
                    File.Move(alloyStateFile, chromeStateFile);

                    var defaultDir = Path.Combine(CachePath, "Default");
                    Directory.CreateDirectory(defaultDir);

                    foreach (var migrationFolderName in FoldersToMigrate)
                    {
                        var migrationFolder = Path.Combine(CachePath, migrationFolderName);
                        if (Directory.Exists(migrationFolder))
                            Directory.Move(migrationFolder, Path.Combine(defaultDir, migrationFolderName));
                    }
                    foreach (var migrationFileName in FilesToMigrate)
                    {
                        var migrationFile = Path.Combine(CachePath, migrationFileName);
                        if (File.Exists(migrationFile))
                            File.Move(migrationFile, Path.Combine(defaultDir, migrationFileName));
                    }
                }
            }
            catch { }
        }
    }
}
