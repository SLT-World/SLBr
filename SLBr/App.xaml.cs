using CefSharp;
using CefSharp.SchemeHandler;
using CefSharp.Wpf.HwndHost;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Handlers;
using SLBr.Pages;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SLBr
{
    public class Extension : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        private string PID;
        public string ID
        {
            get { return PID; }
            set
            {
                PID = value;
                RaisePropertyChanged(nameof(ID));
            }
        }

        private string PName;
        public string Name
        {
            get { return PName; }
            set
            {
                PName = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        private string PPopup;
        public string Popup
        {
            get { return PPopup; }
            set
            {
                PPopup = value;
                RaisePropertyChanged(nameof(Popup));
            }
        }

        private string PVersion;
        public string Version
        {
            get { return PVersion; }
            set
            {
                PVersion = value;
                RaisePropertyChanged(nameof(Version));
            }
        }

        /*private string PManifestVersion;
        public string ManifestVersion
        {
            get { return PManifestVersion; }
            set
            {
                PManifestVersion = value;
                RaisePropertyChanged(nameof(ManifestVersion));
            }
        }

        private string PIcon;
        public string Icon
        {
            get { return PIcon; }
            set
            {
                PIcon = value;
                RaisePropertyChanged(nameof(Icon));
            }
        }*/

        private string PDescription;
        public string Description
        {
            get { return PDescription; }
            set
            {
                PDescription = value;
                RaisePropertyChanged(nameof(Description));
            }
        }

        /*private bool PIsEnabled = true;
        public bool IsEnabled
        {
            get { return PIsEnabled; }
            set
            {
                PIsEnabled = value;
                RaisePropertyChanged(nameof(IsEnabled));
            }
        }*/
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App Instance;

        public List<MainWindow> AllWindows = new List<MainWindow>();
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
        public string UserApplicationWindowsPath;
        public string UserApplicationDataPath;
        public string ExecutablePath;
        public string ExtensionsPath;

        bool AppInitialized;

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
        private ObservableCollection<Extension> PrivateExtensions = new ObservableCollection<Extension>();
        public ObservableCollection<Extension> Extensions
        {
            get { return PrivateExtensions; }
            set
            {
                PrivateExtensions = value;
                Dispatcher.Invoke(() =>
                {
                    switch (int.Parse(GlobalSave.Get("ExtensionButton")))
                    {
                        case 0:
                            foreach (MainWindow _Window in AllWindows)
                            {
                                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                                    BrowserView.ExtensionsButton.Visibility = value.Any() ? Visibility.Visible : Visibility.Collapsed;
                            }
                            break;
                        case 1:
                            foreach (MainWindow _Window in AllWindows)
                            {
                                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                                    BrowserView.ExtensionsButton.Visibility = Visibility.Visible;
                            }
                            break;
                        case 2:
                            foreach (MainWindow _Window in AllWindows)
                            {
                                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                                    BrowserView.ExtensionsButton.Visibility = Visibility.Collapsed;
                            }
                            break;
                    }
                });
                RaisePropertyChanged("Extensions");
            }
        }


        public void AddGlobalHistory(string Url, string Title)
        {
            ActionStorage HistoryEntry = new ActionStorage(Title, $"4<,>{Url}", Url);
            if (GlobalHistory.Contains(HistoryEntry))
                GlobalHistory.Remove(HistoryEntry);
            GlobalHistory.Insert(0, HistoryEntry);
        }
        public Dictionary<int, DownloadItem> Downloads = new Dictionary<int, DownloadItem>();
        public void UpdateDownloadItem(DownloadItem item)
        {
            Downloads[item.Id] = item;
            Dispatcher.Invoke(() =>
            {
                foreach (MainWindow _Window in AllWindows)
                    _Window.TaskbarProgress.ProgressValue = item.IsComplete ? 0 : item.PercentComplete / 100.0;
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Instance = this;
            InitializeApp();
        }

        static Mutex _Mutex;
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public string UserAgent;

        public void LoadExtensions()
        {
            Extensions.Clear();
            if (Directory.Exists(ExtensionsPath))
            {
                var ExtensionsDirectory = Directory.GetDirectories(ExtensionsPath);
                if (ExtensionsDirectory.Length != 0)
                {
                    //ObservableCollection<Extension> _Extensions = new ObservableCollection<Extension>();
                    foreach (var ExtensionParentDirectory in ExtensionsDirectory)
                    {
                        try
                        {
                            string ExtensionDirectory = Directory.EnumerateDirectories(ExtensionParentDirectory).FirstOrDefault();
                            if (Directory.Exists(ExtensionDirectory))
                            {
                                string[] Manifests = Directory.GetFiles(ExtensionDirectory, "manifest.json", SearchOption.TopDirectoryOnly);
                                foreach (string ManifestFile in Manifests)
                                {
                                    JsonElement Manifest = JsonDocument.Parse(File.ReadAllText(ManifestFile)).RootElement;

                                    Extension _Extension = new Extension() { ID = Path.GetFileName(ExtensionParentDirectory), Version = Manifest.GetProperty("version").ToString()/*, ManifestVersion = Manifest.GetProperty("manifest_version").ToString()*/ };

                                    if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                                    {
                                        if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup))
                                            _Extension.Popup = $"chrome-extension://{_Extension.ID}/{ExtensionPopup.GetString()}";
                                        /*else if (ExtensionAction.TryGetProperty("default_icon", out JsonElement defaultIconValue))
                                        {
                                            var firstIcon = defaultIconValue.EnumerateObject().OrderBy(kvp => int.Parse(kvp.Name)).FirstOrDefault();
                                            _Extension.Icon = $"chrome-extension://{ExtensionID}/{firstIcon.Value.GetString()}";
                                        }*/
                                    }
                                    List<string> VarsInMessages = new List<string>();
                                    if (Manifest.TryGetProperty("name", out JsonElement ExtensionName))
                                    {
                                        string Name = ExtensionName.GetString();
                                        if (Name.StartsWith("__MSG_"))
                                            VarsInMessages.Add($"Name<|>{Name}");
                                        else
                                            _Extension.Name = Name;
                                    }
                                    if (Manifest.TryGetProperty("description", out JsonElement ExtensionDescription))
                                    {
                                        string Description = ExtensionDescription.GetString();
                                        if (Description.StartsWith("__MSG_"))
                                            VarsInMessages.Add($"Description<|>{Description}");
                                        else
                                            _Extension.Description = Description;
                                    }

                                    foreach (string Var in VarsInMessages)
                                    {
                                        string _Locale = "en";
                                        string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(ExtensionDirectory, "_locales"));
                                        foreach (string LocaleDirectory in LocalesDirectory)
                                        {
                                            string CompareLocale = Locale.Name.Replace("-", "_");
                                            if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                                            {
                                                _Locale = CompareLocale;
                                                break;
                                            }
                                        }
                                        string[] MessagesFiles = Directory.GetFiles(Path.Combine(ExtensionDirectory, "_locales", _Locale), "messages.json", SearchOption.TopDirectoryOnly);
                                        foreach (string MessagesFile in MessagesFiles)
                                        {
                                            JsonElement Messages = JsonDocument.Parse(File.ReadAllText(MessagesFile)).RootElement;
                                            string[] Vars = Var.Split("<|>");
                                            if (Vars[0] == "Description")
                                            {
                                                _Extension.Description = Messages.GetProperty(Vars[1].Remove(0, 5).Trim('_')).GetProperty("message").ToString();
                                                break;
                                            }
                                            else if (Vars[0] == "Name")
                                            {
                                                _Extension.Name = Messages.GetProperty(Vars[1].Remove(0, 5).Trim('_')).GetProperty("message").ToString();
                                                break;
                                            }
                                        }
                                    }

                                    //_Extension.IsEnabled = true;
                                    Extensions.Add(_Extension);
                                }
                            }
                        }
                        catch { }
                    }
                    //Extensions = _Extensions;
                }
            }
        }

        private void InitializeApp()
        {
            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";
            string CommandLineUrl = "";
            if (Args.Any())
            {
                foreach (string Flag in Args)
                {
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
            _Mutex = new Mutex(true, AppUserModelID);
            if (string.IsNullOrEmpty(CommandLineUrl))
            {
                if (!_Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                if (OtherInstance != null)
                {
                    MessageHelper.SendDataMessage(OtherInstance, CommandLineUrl);
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Set Google API keys. See http://www.chromium.org/developers/how-tos/api-keys
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);

            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            ExtensionsPath = Path.Combine(UserApplicationDataPath, "User Data", "Default", "Extensions");

            UserAgent = UserAgentGenerator.BuildUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}");

            InitializeSaves();
            InitializeUISaves(CommandLineUrl);

            if (Utils.IsAdministrator())
            {
                using (var CheckKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
                {
                    if (CheckKey.GetValue("SLBr") == null)
                    {
                        using (var Key = Registry.ClassesRoot.CreateSubKey("SLBr", true))
                        {
                            Key.SetValue(null, "SLBr Document");
                            Key.SetValue("AppUserModelId", "SLBr");

                            RegistryKey ApplicationRegistry = Key.CreateSubKey("Application", true);
                            ApplicationRegistry.SetValue("AppUserModelId", "SLBr");
                            ApplicationRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                            ApplicationRegistry.SetValue("ApplicationName", "SLBr");
                            ApplicationRegistry.SetValue("ApplicationCompany", "SLT Softwares");
                            ApplicationRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                            ApplicationRegistry.Close();

                            RegistryKey IconRegistry = Key.CreateSubKey("DefaultIcon", true);
                            IconRegistry.SetValue(null, $"{ExecutablePath},0");
                            ApplicationRegistry.Close();

                            RegistryKey CommandRegistry = Key.CreateSubKey("shell\\open\\command", true);
                            CommandRegistry.SetValue(null, $"\"{ExecutablePath}\" \"%1\"");
                            CommandRegistry.Close();
                        }
                        using (var Key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Clients\\StartMenuInternet", true).CreateSubKey("SLBr", true))
                        {
                            if (Key.GetValue(null) as string != "SLBr")
                                Key.SetValue(null, "SLBr");

                            RegistryKey CapabilitiesRegistry = Key.CreateSubKey("Capabilities", true);
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

                            RegistryKey DefaultIconRegistry = Key.CreateSubKey("DefaultIcon", true);
                            DefaultIconRegistry.SetValue(null, $"{ExecutablePath},0");
                            DefaultIconRegistry.Close();

                            RegistryKey CommandRegistry = Key.CreateSubKey("shell\\open\\command", true);
                            CommandRegistry.SetValue(null, $"\"{ExecutablePath}\"");
                            CommandRegistry.Close();
                        }
                        CheckKey.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                    }
                }
            }
            InitializeCEF();
            AppInitialized = true;
        }

        public void DiscordWebhookSendInfo(string Content)
        {
            try { new WebClient().UploadValues(SECRETS.DISCORD_WEBHOOK, new NameValueCollection { { "content", Content }, { "username", "SLBr Diagnostics" } }); }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));

            //e.SetObserved();
            MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //MessageBox.Show(e.ExceptionObject.ToString());
            Exception _Exception = e.ExceptionObject as Exception;
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), _Exception.Message, _Exception.Source, _Exception.TargetSite, _Exception.StackTrace, _Exception.InnerException));

            MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), _Exception.Message, _Exception.Source, _Exception.TargetSite, _Exception.StackTrace, _Exception.InnerException));
        }

        string ExceptionText = @"[SLBr] {0}
[CEF] {1}
[CPU Architecture] {2}

[Message] {3}
[Source] {4}

[Target Site] {5}

[Stack Trace] {6}

[Inner Exception] {7}";
        string ReportExceptionText = @"**Automatic Report**
> - Version: `{0}`
> - CEF Version: `{1}`
> - CPU Architecture: `{2}`
> - Message: ```{3}```
> - Source: `{4} `
> - Target Site: `{5} `

Stack Trace: ```{6} ```

Inner Exception: ```{7} ```";
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));

            MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));
        }
        public int TrackersBlocked;
        public int AdsBlocked;

        public bool AdBlock;
        public bool TrackerBlock;

        //https://chromium-review.googlesource.com/c/chromium/src/+/1265506
        public bool NeverSlowMode;

        public bool SkipAds;
        public string VideoQuality;

        public void SetYouTube(bool _SkipAds, string Quality)
        {
            GlobalSave.Set("SkipAds", _SkipAds.ToString());
            SkipAds = _SkipAds;
            GlobalSave.Set("VideoQuality", Quality);
            VideoQuality = Quality;
        }
        public void SetNeverSlowMode(bool Boolean)
        {
            GlobalSave.Set("NeverSlowMode", Boolean.ToString());
            NeverSlowMode = Boolean;
        }
        public void SetAdBlock(bool Boolean)
        {
            GlobalSave.Set("AdBlock", Boolean.ToString());
            AdBlock = Boolean;
        }
        public void SetTrackerBlock(bool Boolean)
        {
            GlobalSave.Set("TrackerBlock", Boolean.ToString());
            TrackerBlock = Boolean;
        }
        public void SetRenderMode(string Mode, bool Notify)
        {
            RenderOptions.ProcessRenderMode = (Mode == "Hardware") ? RenderMode.Default : RenderMode.SoftwareOnly;
            GlobalSave.Set("RenderMode", Mode);
        }
        public void UpdateTabUnloadingTimer(int Time = -1)
        {
            if (Time != -1)
                GlobalSave.Set("TabUnloadingTime", Time);
            foreach (MainWindow _Window in AllWindows)
                _Window.UpdateUnloadTimer();
        }

        public Dictionary<string, bool> PopupPermissionHosts = new Dictionary<string, bool>();

        private ObservableCollection<ActionStorage> PrivateLanguages = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> Languages
        {
            get { return PrivateLanguages; }
            set
            {
                PrivateLanguages = value;
                RaisePropertyChanged("Languages");
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

        public string GetLocaleIcon(string ISO)
        {
            if (ISO.StartsWith("zh-TW"))
                return "\xe981";
            else if (ISO.StartsWith("zh"))
                return "\xE982";
            else if (ISO.StartsWith("ja"))
                return "\xe985";
            else if (ISO.StartsWith("ko"))
                return "\xe97d";
            else if (ISO.StartsWith("en"))
                return "\xe97e";
            return "\xf2b7";
            //return "\xE8C1";
        }

        private void InitializeSaves()
        {
            GlobalSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            LanguagesSave = new Saving("Languages.bin", UserApplicationDataPath);

            if (!Directory.Exists(UserApplicationWindowsPath))
            {
                Directory.CreateDirectory(UserApplicationWindowsPath);
                WindowsSaves.Add(new Saving($"Window_0.bin", UserApplicationWindowsPath));
            }
            else
            {
                int WindowsSavesCount = Directory.EnumerateFiles(UserApplicationWindowsPath).Count();
                if (WindowsSavesCount != 0)
                {
                    for (int i = 0; i < WindowsSavesCount; i++)
                        WindowsSaves.Add(new Saving($"Window_{i}.bin", UserApplicationWindowsPath));
                }
                else
                    WindowsSaves.Add(new Saving($"Window_0.bin", UserApplicationWindowsPath));
            }

            if (SearchSave.Has("Count") && int.Parse(SearchSave.Get("Count")) != 0)
            {
                for (int i = 0; i < int.Parse(SearchSave.Get("Count")); i++)
                    SearchEngines.Add(SearchSave.Get($"{i}"));
            }
            else
                SearchEngines = new List<string>() {
                    "http://google.com/search?q={0}",
                    "http://bing.com/search?q={0}",
                    "http://www.ecosia.org/search?q={0}",
                    "http://duckduckgo.com/?q={0}",
                    /*"http://search.brave.com/search?q={0}",
                    "http://search.yahoo.com/search?p={0}",
                    "http://yandex.com/search/?text={0}",
                    "https://www.qwant.com/?q="*/
                };

            if (LanguagesSave.Has("Count") && int.Parse(LanguagesSave.Get("Count")) != 0)
            {
                for (int i = 0; i < int.Parse(LanguagesSave.Get("Count")); i++)
                {
                    string ISO = LanguagesSave.Get($"{i}");
                    if (AllLocales.TryGetValue(ISO, out string Name))
                        Languages.Add(new ActionStorage(Name, GetLocaleIcon(ISO), ISO));
                }
                Locale = Languages[int.Parse(LanguagesSave.Get("Selected"))];
            }
            else
            {
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en-US"), GetLocaleIcon("en-US"), "en-US"));
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en"), GetLocaleIcon("en"), "en"));
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
                GlobalSave.Set("SearchEngine", SearchEngines.Find(i => i.Contains("ecosia.org")));

            if (!GlobalSave.Has("Homepage"))
                GlobalSave.Set("Homepage", "slbr://newtab");
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

            //if (!GlobalSave.Has("RestoreTabs"))
            //    GlobalSave.Set("RestoreTabs", true);
            if (!GlobalSave.Has("DownloadFavicons"))
                GlobalSave.Set("DownloadFavicons", true);
            if (!GlobalSave.Has("SmoothScroll"))
                GlobalSave.Set("SmoothScroll", true);

            if (!GlobalSave.Has("ChromiumHardwareAcceleration"))
                GlobalSave.Set("ChromiumHardwareAcceleration", (RenderCapability.Tier >> 16) != 0);
            if (!GlobalSave.Has("ExperimentalFeatures"))
                GlobalSave.Set("ExperimentalFeatures", false);
            if (!GlobalSave.Has("LiteMode"))
                GlobalSave.Set("LiteMode", false);
            if (!GlobalSave.Has("PDFViewerExtension"))
                GlobalSave.Set("PDFViewerExtension", true);

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

            /*if (!GlobalSave.Has("DefaultBrowserEngine"))
                GlobalSave.Set("DefaultBrowserEngine", 0);*/
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                    Themes.Add(new Theme("Auto", (key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
            }
            catch
            {
                Themes.Add(new Theme("Auto", Themes[1]));
            }
        }
        private void InitializeUISaves(string CommandLineUrl = "")
        {
            SetYouTube(bool.Parse(GlobalSave.Get("SkipAds", true.ToString())), GlobalSave.Get("VideoQuality", "Auto"));
            SetNeverSlowMode(bool.Parse(GlobalSave.Get("NeverSlowMode", false.ToString())));
            SetAdBlock(bool.Parse(GlobalSave.Get("AdBlock", true.ToString())));
            SetTrackerBlock(bool.Parse(GlobalSave.Get("TrackerBlock", true.ToString())));
            SetRenderMode(GlobalSave.Get("RenderMode", (RenderCapability.Tier >> 16) == 0 ? "Software" : "Hardware"), true);
            
            if (FavouritesSave.Has("Favourite_Count"))
            {
                for (int i = 0; i < int.Parse(FavouritesSave.Get("Favourite_Count")); i++)
                {
                    string[] Value = FavouritesSave.Get($"Favourite_{i}", true);
                    Favourites.Add(new ActionStorage(Value[1], $"4<,>{Value[0]}", Value[0]));
                }
            }
            SetAppearance(GetTheme(GlobalSave.Get("Theme", "Auto")), GlobalSave.Get("TabAlignment", "Horizontal"), bool.Parse(GlobalSave.Get("HomeButton", true.ToString())), bool.Parse(GlobalSave.Get("TranslateButton", true.ToString())), bool.Parse(GlobalSave.Get("AIButton", true.ToString())), bool.Parse(GlobalSave.Get("ReaderButton", false.ToString())), int.Parse(GlobalSave.Get("ExtensionButton", "0")), int.Parse(GlobalSave.Get("FavouritesBar", "0")));
            if (bool.Parse(GlobalSave.Get("RestoreTabs", true.ToString())))
            {
                for (int t = 0; t < WindowsSaves.Count; t++)
                {
                    MainWindow _Window = new MainWindow();
                    _Window.Show();
                    Saving TabsSave = WindowsSaves[t];
                    if (int.Parse(TabsSave.Get("Count", "0")) > 0)
                    {
                        for (int i = 0; i < int.Parse(TabsSave.Get("Count")); i++)
                        {
                            string Url = TabsSave.Get(i.ToString());
                            //if (Url != "NOTFOUND")
                            _Window.NewTab(Url);
                        }
                        _Window.TabsUI.SelectedIndex = int.Parse(TabsSave.Get("Selected", 0.ToString()));
                    }
                    else
                        _Window.NewTab(GlobalSave.Get("Homepage"));
                    _Window.TabsUI.Visibility = Visibility.Visible;
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
            //return;
            _LifeSpanHandler = new LifeSpanHandler(false);
            _DownloadHandler = new DownloadHandler();
            _RequestHandler = new RequestHandler();
            _LimitedContextMenuHandler = new LimitedContextMenuHandler();
            _ContextMenuHandler = new ContextMenuHandler();
            _KeyboardHandler = new KeyboardHandler();
            _JsDialogHandler = new JsDialogHandler();
            _PrivateJsObjectHandler = new PrivateJsObjectHandler();
            _PermissionHandler = new PermissionHandler();
            _SafeBrowsing = new SafeBrowsingHandler(SECRETS.GOOGLE_API_KEY, SECRETS.GOOGLE_DEFAULT_CLIENT_ID);

            //_KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(delegate () { Refresh(); }, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Refresh(true); }, (int)Key.F5, true);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(); }, (int)Key.F11);
            _KeyboardHandler.AddKey(delegate () { DevTools(); }, (int)Key.F12);
            _KeyboardHandler.AddKey(delegate () { Find(); }, (int)Key.F, true);

            CefSettings Settings = new CefSettings();
            Settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;

            Settings.Locale = Locale.Tooltip;
            Settings.AcceptLanguageList = string.Join(",", Languages.Select(i => i.Tooltip));
            Settings.UserAgentProduct = $"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}";

            string UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
            Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));
            Settings.LogSeverity = LogSeverity.Error;
            Settings.CachePath = Path.GetFullPath(Path.Combine(UserDataPath, "Cache"));
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

            Dictionary<string, string> SLBrURLs = new Dictionary<string, string>
            {
                { "Credits", "Credits.html" },
                { "License", "License.html" },
                { "NewTab", "NewTab.html" },
                { "Downloads", "Downloads.html" },
                { "History", "History.html" },
                { "Settings", "Settings.html" },
                { "Tetris", "Tetris.html" },
                { "WhatsNew", "WhatsNew.html" }
            };
            string SLBrSchemeRootFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            foreach (KeyValuePair<string, string> _Scheme in SLBrURLs)
            {
                Settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "slbr",
                    DomainName = _Scheme.Key.ToLower(),
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(SLBrSchemeRootFolder, hostName: _Scheme.Key.ToLower(), defaultPage: _Scheme.Value),
                    IsSecure = true,
                    IsLocal = true,
                    IsStandard = true,
                    IsCorsEnabled = true
                });
            }

            CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome;
            Cef.Initialize(Settings);
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                bool PDFViewerExtension = bool.Parse(GlobalSave.Get("PDFViewerExtension"));

                /*string _Preferences = "";
                foreach (KeyValuePair<string, object> e in GlobalRequestContext.GetAllPreferences(true))
                    _Preferences = GetPreferencesString(_Preferences, "", e);
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt")))
                    outputFile.Write(_Preferences);*/

                string Error;
                GlobalRequestContext.SetPreference("autofill.credit_card_enabled", false, out Error);
                GlobalRequestContext.SetPreference("autofill.profile_enabled", false, out Error);
                GlobalRequestContext.SetPreference("autofill.enabled", false, out Error);
                GlobalRequestContext.SetPreference("payments.can_make_payment_enabled", false, out Error);
                GlobalRequestContext.SetPreference("credentials_enable_service", false, out Error);

                //GlobalRequestContext.SetPreference("scroll_to_text_fragment_enabled", false, out Error);
                //GlobalRequestContext.SetPreference("url_keyed_anonymized_data_collection.enabled", false, out Error);

                GlobalRequestContext.SetPreference("download_bubble_enabled", false, out Error);
                GlobalRequestContext.SetPreference("download_bubble.partial_view_enabled", false, out Error);
                GlobalRequestContext.SetPreference("download_duplicate_file_prompt_enabled", false, out Error);
                //GlobalRequestContext.SetPreference("profile.default_content_setting_values.automatic_downloads", 1, out Error);

                GlobalRequestContext.SetPreference("shopping_list_enabled", false, out Error);
                GlobalRequestContext.SetPreference("browser_labs_enabled", false, out Error);
                GlobalRequestContext.SetPreference("allow_dinosaur_easter_egg", false, out Error);
                GlobalRequestContext.SetPreference("feedback_allowed", false, out Error);
                GlobalRequestContext.SetPreference("policy.feedback_surveys_enabled", false, out Error);
                GlobalRequestContext.SetPreference("ntp.promo_visible", false, out Error);
                GlobalRequestContext.SetPreference("ntp.shortcust_visible", false, out Error);
                GlobalRequestContext.SetPreference("ntp_snippets.enable", false, out Error);
                GlobalRequestContext.SetPreference("ntp_snippets_by_dse.enable", false, out Error);
                GlobalRequestContext.SetPreference("search.suggest_enabled", false, out Error);
                GlobalRequestContext.SetPreference("side_search.enabled", false, out Error);
                GlobalRequestContext.SetPreference("translate.enabled", false, out Error);
                GlobalRequestContext.SetPreference("history.saving_disabled", false, out Error);
                GlobalRequestContext.SetPreference("media_router.enable_media_router", false, out Error);
                GlobalRequestContext.SetPreference("documentsuggest.enabled", false, out Error);
                GlobalRequestContext.SetPreference("alternate_error_pages.enabled", false, out Error);
                //GlobalRequestContext.SetPreference("https_only_mode_enabled", true, out Error);
                //GlobalRequestContext.SetPreference("enable_do_not_track", bool.Parse(GlobalSave.Get("DoNotTrack")), out errorMessage);
                //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/preloading/preloading_prefs.h
                GlobalRequestContext.SetPreference("net.network_prediction_options", 2, out Error);
                GlobalRequestContext.SetPreference("safebrowsing.enabled", false, out Error);
                //GlobalRequestContext.SetPreference("browser.theme.follows_system_colors", false, out Error);

                GlobalRequestContext.SetPreference("browser.enable_spellchecking", bool.Parse(GlobalSave.Get("SpellCheck")), out Error);
                //GlobalRequestContext.SetPreference("spellcheck.use_spelling_service", false, out Error);
                GlobalRequestContext.SetPreference("spellcheck.dictionaries", Languages.Select(i => i.Tooltip), out Error);
                GlobalRequestContext.SetPreference("intl.accept_languages", Languages.Select(i => i.Tooltip), out Error);

                GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !PDFViewerExtension, out Error);
                GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !PDFViewerExtension, out Error);

                if (bool.Parse(GlobalSave.Get("BlockFingerprint")))
                    GlobalRequestContext.SetPreference("webrtc.ip_handling_policy", "disable_non_proxied_udp", out Error);
                //GlobalRequestContext.SetPreference("profile.content_settings.enable_quiet_permission_ui.geolocation", false, out Error);
            });
            LoadExtensions();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                    BrowserView?.InitializeBrowserComponent();
            }
        }

        /*public string GetPreferencesString(string _String, string Parents, KeyValuePair<string, object> ObjectPair)
        {
            if (ObjectPair.Value is System.Dynamic.ExpandoObject expando)
            {
                foreach (KeyValuePair<string, object> property in (IDictionary<string, object>)expando)
                    _String = $"{GetPreferencesString(_String, Parents + $"[{ObjectPair.Key}]", property)}";
                if (string.IsNullOrEmpty(Parents))
                    _String += "\n";
            }
            else if (ObjectPair.Value is List<object> _List)
                _String += string.Join(", ", _List);
            else
            {
                if (!string.IsNullOrEmpty(Parents))
                    _String += $"{Parents}: ";
                _String += $"{ObjectPair.Key}: {ObjectPair.Value}\n";
            }
            return _String;
        }*/

        public string GenerateCannotConnect(string Url, CefErrorCode ErrorCode, string ErrorText)
        {
            string Host = Utils.Host(Url);
            string HTML = Cannot_Connect_Error.Replace("{Site}", Host).Replace("{Error}", ErrorText);
            switch (ErrorCode)
            {
                case CefErrorCode.ConnectionTimedOut:
                    HTML = HTML.Replace("{Description}", $"{Host} took too long to respond.");
                    break;
                case CefErrorCode.ConnectionReset:
                    HTML = HTML.Replace("{Description}", $"The connection was reset.");
                    break;
                case CefErrorCode.ConnectionFailed:
                    HTML = HTML.Replace("{Description}", $"The connection failed.");
                    break;
                case CefErrorCode.ConnectionRefused:
                    HTML = HTML.Replace("{Description}", $"{Host} refused to connect.");
                    break;
                case CefErrorCode.ConnectionClosed:
                    HTML = HTML.Replace("{Description}", $"{Host} unexpectedly closed the connection.");
                    break;
                case CefErrorCode.InternetDisconnected:
                    HTML = HTML.Replace("{Description}", $"Internet was disconnected.");
                    break;
                case CefErrorCode.NameNotResolved:
                    HTML = HTML.Replace("{Description}", $"The URL entered could not be resolved.");
                    break;
                case CefErrorCode.NetworkChanged:
                    HTML = HTML.Replace("{Description}", $"{Host} took too long to respond.");
                    break;
                case CefErrorCode.CertInvalid:
                case CefErrorCode.CertDateInvalid:
                case CefErrorCode.CertAuthorityInvalid:
                case CefErrorCode.CertCommonNameInvalid:
                    HTML = HTML.Replace("{Description}", $"The connection to {Host} is not private.");
                    break;
                default:
                    HTML = HTML.Replace("{Description}", $"Error Code: {ErrorCode}");
                    break;
            }
            return HTML;
        }

        public string Cannot_Connect_Error = @"<html><head><title>Unable to connect to {Site}</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;};</style></head><body><div id=""content""><h1 class=""icon""></h1><h2 id=""title"">Unable to connect to {Site}</h2><h5 id=""description"">{Description}</h5><h5 id=""error"" style=""margin:0px; color:#646464;"">{Error}</h5></div></body></html>";
        public string Process_Crashed_Error = @"<html><head><title>Process crashed</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;};</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Process Crashed</h2><h5>Process crashed while attempting to load content. Undo / Refresh the page to resolve the problem.</h5><a href=""slbr://newtab"">Return to homepage</a></div></body></html>";
        public string Deception_Error = @"<html><head><title>Site access denied</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;};</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Site Access Denied</h2><h5>The site ahead was detected to contain deceptive content.</h5><a href=""slbr://newtab"">Return to homepage</a></div></body></html>";
        public string Malware_Error = @"<html><head><title>Site access denied</title><style>html{background:darkred;}body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;};</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Site Access Denied</h2><h5>The site ahead was detected to contain unwanted software / malware.</h5><a href=""slbr://newtab"">Return to homepage</a></div></body></html>";

        private void SetCEFFlags(CefSettings Settings)
        {
            SetChromeFlags(Settings);
            SetBackgroundFlags(Settings);
            SetNetworkFlags(Settings);
            SetFrameworkFlags(Settings);
            SetGraphicsFlags(Settings);
            SetMediaFlags(Settings);
            SetSecurityFlags(Settings);
            SetFeatureFlags(Settings);
            //force-gpu-mem-available-mb https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/gpu_switches.cc
            //disable-file-system Disable FileSystem API.
        }

        private void SetChromeFlags(CefSettings Settings)
        {
            //https://source.chromium.org/chromium/chromium/src/+/main:tools/perf/testdata/crossbench_output/speedometer_3.0/speedometer_3.0.json?q=disable-component-update&ss=chromium%2Fchromium%2Fsrc
            //Settings.CefCommandLineArgs.Add("disable-crashpad-for-testing");
            /*Settings.CefCommandLineArgs.Add("disable-sync");
            Settings.CefCommandLineArgs.Add("disable-translate");

            Settings.CefCommandLineArgs.Add("disable-fre");*/
            Settings.CefCommandLineArgs.Add("no-default-browser-check");
            Settings.CefCommandLineArgs.Add("no-first-run");
            //Settings.CefCommandLineArgs.Add("disable-first-run-ui");
            //Settings.CefCommandLineArgs.Add("disable-ntp-most-likely-favicons-from-server");
            //Settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");
            Settings.CefCommandLineArgs.Add("disable-domain-reliability");


            Settings.CefCommandLineArgs.Add("disable-chrome-tracing-computation");
            //Settings.CefCommandLineArgs.Add("disable-scroll-to-text-fragment");

            //Settings.CefCommandLineArgs.Add("disable-ntp-other-sessions-menu");
            Settings.CefCommandLineArgs.Add("disable-default-apps");

            Settings.CefCommandLineArgs.Add("disable-modal-animations");
            //Settings.CefCommandLineArgs.Add("material-design-ink-drop-animation-speed", "fast");

            Settings.CefCommandLineArgs.Add("no-network-profile-warning");

            Settings.CefCommandLineArgs.Add("disable-login-animations");
            Settings.CefCommandLineArgs.Add("disable-stack-profiler");
            Settings.CefCommandLineArgs.Add("disable-system-font-check");
            //Settings.CefCommandLineArgs.Add("disable-infobars");
            Settings.CefCommandLineArgs.Add("disable-breakpad");
            Settings.CefCommandLineArgs.Add("disable-crash-reporter");

            Settings.CefCommandLineArgs.Add("disable-top-sites");
            //Settings.CefCommandLineArgs.Add("disable-minimum-show-duration");
            //Settings.CefCommandLineArgs.Add("disable-startup-promos-for-testing");
            //Settings.CefCommandLineArgs.Add("disable-contextual-search");
            Settings.CefCommandLineArgs.Add("no-service-autorun");
            Settings.CefCommandLineArgs.Add("disable-auto-reload");
            //Settings.CefCommandLineArgs.Add("bypass-account-already-used-by-another-profile-check");

            //Settings.CefCommandLineArgs.Add("metrics-recording-only");

            //Settings.CefCommandLineArgs.Add("disable-cloud-policy-on-signin");

            //Settings.CefCommandLineArgs.Add("disable-dev-shm-usage");
            Settings.CefCommandLineArgs.Add("disable-dinosaur-easter-egg"); //enable-dinosaur-easter-egg-alt-images
            //Settings.CefCommandLineArgs.Add("oobe-skip-new-user-check-for-testing");

            //Settings.CefCommandLineArgs.Add("disable-gaia-services"); // https://source.chromium.org/chromium/chromium/src/+/main:ash/constants/ash_switches.cc
            
            Settings.CefCommandLineArgs.Add("wm-window-animations-disabled");
            Settings.CefCommandLineArgs.Add("animation-duration-scale", "0");
            Settings.CefCommandLineArgs.Add("disable-histogram-customizer");

            //REMOVE MOST CHROMIUM POPUPS
            Settings.CefCommandLineArgs.Add("suppress-message-center-popups");
            Settings.CefCommandLineArgs.Add("disable-prompt-on-repost");
            Settings.CefCommandLineArgs.Add("propagate-iph-for-testing");
            Settings.CefCommandLineArgs.Add("disable-search-engine-choice-screen");
            Settings.CefCommandLineArgs.Add("ash-no-nudges");
            Settings.CefCommandLineArgs.Add("noerrdialogs");
            //Settings.CefCommandLineArgs.Add("hide-crash-restore-bubble");
            //Settings.CefCommandLineArgs.Add("disable-chrome-login-prompt");
        }

        private void SetFrameworkFlags(CefSettings Settings)
        {
            if (!bool.Parse(GlobalSave.Get("LiteMode")))
            {
                Settings.CefCommandLineArgs.Add("enable-webassembly-baseline");
                Settings.CefCommandLineArgs.Add("enable-webassembly-tiering");
                Settings.CefCommandLineArgs.Add("enable-webassembly-lazy-compilation");
                Settings.CefCommandLineArgs.Add("enable-webassembly-memory64");
            }

            if (!bool.Parse(GlobalSave.Get("PDFViewerExtension")))
                Settings.CefCommandLineArgs.Add("disable-pdf-extension");

            if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
            {
                //Settings.CefCommandLineArgs.Add("webtransport-developer-mode");
                Settings.CefCommandLineArgs.Add("enable-experimental-cookie-features");

                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-jspi");

                Settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");

                Settings.CefCommandLineArgs.Add("enable-javascript-harmony");
                Settings.CefCommandLineArgs.Add("enable-javascript-experimental-shared-memory");

                Settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
                //Settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption-experiment");
                //Settings.CefCommandLineArgs.Add("text-box-trim");

                //Settings.CefCommandLineArgs.Add("enable-devtools-experiments");

                Settings.CefCommandLineArgs.Add("enable-webgl-developer-extensions");
                //Settings.CefCommandLineArgs.Add("enable-webgl-draft-extensions");
                //Settings.CefCommandLineArgs.Add("enable-webgpu-developer-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-extension-apis");
            }
        }

        private void SetBackgroundFlags(CefSettings Settings)
        {
            //DISABLES ECOSIA SEARCHBOX
            //Settings.CefCommandLineArgs.Add("headless"); //Run in headless mode without a UI or display server dependencies.

            //https://chromium.googlesource.com/chromium/src/+/refs/tags/77.0.3865.0/third_party/blink/renderer/platform/graphics/dark_mode_settings.h
            Settings.CefCommandLineArgs.Add("dark-mode-settings", "ImagePolicy=1");//,ImageClassifierPolicy=0,InversionAlgorithm=2

            //Disabling site isolation somehow increases memory usage by 10 MB
            /*Settings.CefCommandLineArgs.Add("no-sandbox");
            Settings.CefCommandLineArgs.Add("disable-setuid-sandbox");
            Settings.CefCommandLineArgs.Add("site-isolation-trial-opt-out");
            Settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            Settings.CefCommandLineArgs.Add("isolate-origins", "https://challenges.cloudflare.com");*/

            //Settings.CefCommandLineArgs.Add("enable-raster-side-dark-mode-for-images");

            Settings.CefCommandLineArgs.Add("process-per-site");
            Settings.CefCommandLineArgs.Add("password-store", "basic");

            if (bool.Parse(GlobalSave.Get("LiteMode")))
            {
                //Turns device memory into 0.5
                Settings.CefCommandLineArgs.Add("enable-low-end-device-mode"); //Causes memory to be 20 MB more when minimized, but reduces 80 MB when not minimized

                Settings.CefCommandLineArgs.Add("disable-best-effort-tasks"); //PREVENTS GOOGLE LOGIN

                Settings.CefCommandLineArgs.Add("disable-smooth-scrolling");

                Settings.CefCommandLineArgs.Add("disable-low-res-tiling"); //https://codereview.chromium.org/196473007/

                Settings.CefCommandLineArgs.Add("force-prefers-reduced-motion");
                Settings.CefCommandLineArgs.Add("disable-logging");

                Settings.CefCommandLineArgs.Add("max-web-media-player-count", "1");

                Settings.CefCommandLineArgs.Add("gpu-program-cache-size-kb", $"{128 * 1024}");
                Settings.CefCommandLineArgs.Add("gpu-disk-cache-size-kb", $"{128 * 1024}");

                Settings.CefCommandLineArgs.Add("force-effective-connection-type", "Slow-2G");
                //Settings.CefCommandLineArgs.Add("num-raster-threads", "4"); //RETIRED FLAG
                Settings.CefCommandLineArgs.Add("renderer-process-limit", "4");

            }
            else
            {
                Settings.CefCommandLineArgs.Add("gpu-program-cache-size-kb", $"{2 * 1024 * 1024}");
                Settings.CefCommandLineArgs.Add("gpu-disk-cache-size-kb", $"{2 * 1024 * 1024}");
                //Settings.CefCommandLineArgs.Add("component-updater", "fast-update");
                if (!bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
                    Settings.CefCommandLineArgs.Add("enable-low-res-tiling");
            }

            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/optimization_guide/hints_fetcher_browsertest.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
            Settings.CefCommandLineArgs.Add("disable-fetching-hints-at-navigation-start");
            Settings.CefCommandLineArgs.Add("disable-model-download-verification");
            Settings.CefCommandLineArgs.Add("disable-component-update");
            Settings.CefCommandLineArgs.Add("component-updater", "disable-background-downloads,disable-delta-updates"); //https://source.chromium.org/chromium/chromium/src/+/main:components/component_updater/component_updater_command_line_config_policy.cc

            Settings.CefCommandLineArgs.Add("back-forward-cache");

            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139
            Settings.CefCommandLineArgs.Add("disable-highres-timer");

            //https://github.com/portapps/brave-portable/issues/26
            //https://github.com/chromium/chromium/blob/2ca8c5037021c9d2ecc00b787d58a31ed8fc8bcb/third_party/blink/renderer/bindings/core/v8/v8_cache_options.h
            //Settings.CefCommandLineArgs.Add("v8-cache-options");

            //Settings.CefCommandLineArgs.Add("disable-v8-idle-tasks");

            //Settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes"); //Causes memory to be 100 MB more than if disabled when minimized
            //Settings.CefCommandLineArgs.Add("quick-intensive-throttling-after-loading"); //Causes memory to be 100 MB more than if disabled when minimized
            //Settings.CefCommandLineArgs.Add("intensive-wake-up-throttling-policy", "1");

            //Settings.CefCommandLineArgs.Add("font-cache-shared-handle"); //Increases memory by 5 MB

            Settings.CefCommandLineArgs.Add("disable-mipmap-generation"); // Disables mipmap generation in Skia. Used a workaround for select low memory devices

            Settings.CefCommandLineArgs.Add("enable-parallel-downloading");
        }

        /*NEVER SLOW MODE FLAGS
        // The adapter selecting strategy related to GPUPowerPreference.
        https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/service_utils.cc
        none = WebGPUPowerPreference::kNone
        default-low-power = WebGPUPowerPreference::kDefaultLowPower;
        default-high-performance = WebGPUPowerPreference::kDefaultHighPerformance;
        force-low-power = WebGPUPowerPreference::kForceLowPower;
        force-high-performance = WebGPUPowerPreference::kForceHighPerformance;

        use-webgpu-power-preference = force-low-power

        // Allows explicitly specifying the shader disk cache size for embedded devices. Default value is 6MB. On Android, 2MB is default and 128KB for low-end devices.
        //https://source.chromium.org/chromium/chromium/src/+/main:gpu/config/gpu_preferences.h
        gpu-disk-cache-size-kb = 2 * 1024 * 1024 / Low End Device Mode 128 * 1024

        // Override the maximum framerate as can be specified in calls to getUserMedia. This flag expects a value. Example: --max-gum-fps=17.5
        max-gum-fps

        // Forces the maximum disk space to be used by the disk cache, in bytes.
        disk-cache-size

        // Specifies the max number of bytes that should be used by the skia font cache. If the cache needs to allocate more, skia will purge previous entries.
        skia-font-cache-limit-mb = 100KiB

        // Specifies the max number of bytes that should be used by the skia resource cache. The previous entries are purged from the cache when the memory useage exceeds this limit.
        skia-resource-cache-limit-mb = 100KiB

        // Allows user to override maximum number of active WebGL contexts per renderer process.
        max-active-webgl-contexts

        // Sets the maximium decoded image size limitation.
        max-decoded-image-size-mb = 1MiB

        // Sets the maximum number of WebMediaPlayers allowed per frame.
        max-web-media-player-count

        // Sets the maximum size, in megabytes. The log file can grow to before older data is overwritten. Do not use this flag if you want an unlimited file size.
        net-log-max-size-mb

        // This is only used when we did not set buffer size in trace config and will be used for all trace sessions. If not provided, we will use the default value provided in perfetto_config.cc
        default-trace-buffer-size-limit-in-kb

        // Configures the size of the shared memory buffer used for tracing. Value is provided in kB. Defaults to 4096. Should be a multiple of the SMB page size (currently 32kB on Desktop or 4kB on Android).
        trace-smb-size

        // Sets the maximum GPU memory to use for discardable caches.
        force-gpu-mem-discardable-limit-mb

        // Sets the total amount of memory that may be allocated for GPU resources
        force-gpu-mem-available-mb

        // Sets the maximum texture size in pixels.
        force-max-texture-size

        // Sets the maximum size of the in-memory gpu program cache, in kb //https://source.chromium.org/chromium/chromium/src/+/main:gpu/config/gpu_preferences.h
        gpu-program-cache-size-kb = 2 * 1024 * 1024 / Low End Device Mode 128 * 1024

        // Specifies the heap limit for Vulkan memory. TODO(crbug.com/40161102): Remove this switch.
        vulkan-heap-memory-limit-mb
        // Specifies the sync CPU limit for total Vulkan memory. TODO(crbug.com/40161102): Remove this switch.
        vulkan-sync-cpu-memory-limit-mb

        // Allows explicitly specifying MSE audio/video buffer sizes as megabytes. Default values are 150M for video and 12M for audio.
        mse-audio-buffer-size-limit-mb
        mse-video-buffer-size-limit-mb

        disallow-doc-written-script-loads
        BlinkSettings = "disallowFetchForDocWrittenScriptsInMainFrame=true"
        6 connections per proxy server
        */

        private void SetGraphicsFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("in-process-gpu");
            if (bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
            {
                Settings.CefCommandLineArgs.Add("enable-gpu");
                Settings.CefCommandLineArgs.Add("enable-zero-copy");
                Settings.CefCommandLineArgs.Add("disable-software-rasterizer");
                Settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
                //Settings.CefCommandLineArgs.Add("gpu-rasterization-msaa-sample-count", MainSave.Get("MSAASampleCount"));
                //if (MainSave.Get("AngleGraphicsBackend").ToLower() != "default")
                //    Settings.CefCommandLineArgs.Add("use-angle", MainSave.Get("AngleGraphicsBackend"));
                Settings.CefCommandLineArgs.Add("enable-accelerated-2d-canvas");
                /*if (bool.Parse(GlobalSave.Get("LiteMode")))
                    Settings.CefCommandLineArgs.Add("use-webgpu-power-preference", "force-low-power");
                else
                    Settings.CefCommandLineArgs.Add("use-webgpu-power-preference", "default-low-power");*/
            }
            else
            {
                Settings.CefCommandLineArgs.Add("disable-gpu");
                Settings.CefCommandLineArgs.Add("disable-gpu-compositing");
                Settings.CefCommandLineArgs.Add("disable-gpu-vsync");
                Settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
                Settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-encode");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");
                Settings.CefCommandLineArgs.Add("disable-accelerated-mjpeg-decode");
                Settings.CefCommandLineArgs.Add("disable-video-capture-use-gpu-memory-buffer");
            }
        }

        private void SetNetworkFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("enable-tls13-early-data");

            Settings.CefCommandLineArgs.Add("reduce-accept-language");
            //Settings.CefCommandLineArgs.Add("reduce-transfer-size-updated-ipc");

            //Settings.CefCommandLineArgs.Add("enable-network-information-downlink-max");
            //Settings.CefCommandLineArgs.Add("enable-precise-memory-info");

            Settings.CefCommandLineArgs.Add("enable-quic");
            Settings.CefCommandLineArgs.Add("enable-spdy4");
            Settings.CefCommandLineArgs.Add("enable-ipv6");

            Settings.CefCommandLineArgs.Add("no-proxy-server");
            //Settings.CefCommandLineArgs.Add("winhttp-proxy-resolver");
            Settings.CefCommandLineArgs.Add("no-pings");

            Settings.CefCommandLineArgs.Add("disable-background-networking");
            Settings.CefCommandLineArgs.Add("disable-component-extensions-with-background-pages");
        }

        private void SetSecurityFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("unsafely-disable-devtools-self-xss-warnings");
            Settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");

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
            //Settings.CefCommandLineArgs.Add("enable-canvas-2d-dynamic-rendering-mode-switching");

            Settings.CefCommandLineArgs.Add("autoplay-policy", "user-gesture-required");
            Settings.CefCommandLineArgs.Add("animated-image-resume");
            Settings.CefCommandLineArgs.Add("disable-image-animation-resync");
            Settings.CefCommandLineArgs.Add("disable-checker-imaging");

            //Settings.CefCommandLineArgs.Add("enable-lite-video");
            //Settings.CefCommandLineArgs.Add("lite-video-force-override-decision");

            //FLAG SEEMS TO NOT EXIST BUT IT DOES WORK
            Settings.CefCommandLineArgs.Add("enable-speech-input");

            //BREAKS PERMISSIONS, DO NOT ADD
            //Settings.CefCommandLineArgs.Add("enable-media-stream");

            //Settings.CefCommandLineArgs.Add("enable-media-session-service");

            Settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            //Settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            Settings.CefCommandLineArgs.Add("auto-select-desktop-capture-source", "Entire screen");

            //Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-always");
            //Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");
        }

        private void SetFeatureFlags(CefSettings Settings)
        {
            /*[Blink Settings]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/public/common/web_preferences/web_preferences.h
             * https://chromium.googlesource.com/chromium/blink/+/refs/heads/main/Source/core/frame/Settings.in
             * https://source.chromium.org/chromium/chromium/src/+/main:out/lacros-Debug/gen/third_party/blink/public/mojom/webpreferences/web_preferences.mojom.js
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/core/frame/settings.json5
             * 
             * bypassCSP=true //disable Content Security Policy
             */

            /*[Enums]
             * https://chromium.googlesource.com/chromium/blink/+/master/Source/bindings/core/v8/V8CacheOptions.h
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/public/mojom/webpreferences/web_preferences.mojom
             * https://source.chromium.org/chromium/chromium/src/+/main:net/nqe/effective_connection_type.cc
             */

            /*[enable/disable-features]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/common/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/autofill/core/common/autofill_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/segmentation_platform/public/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/feed/feed_feature_list.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:sandbox/policy/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:ui/base/ui_base_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:android_webview/common/aw_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:content/public/common/content_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/page_image_service/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/scheduler/main_thread/memory_purge_manager.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:content/browser/loader/navigation_url_loader_impl.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/embedder_support/android/util/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/js_injection/renderer/js_communication.cc
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:android_webview/java/src/org/chromium/android_webview/common/ProductionSupportedFlagList.java
             * https://chromium.googlesource.com/chromium/src/+/efa55ec49b91438d5a9c0930ef19038d517914d1
             * 
             * BackForwardCacheWithKeepaliveRequest ReduceGpuPriorityOnBackground ProcessHtmlDataImmediately SetLowPriorityForBeacon
             * ImageService ImageServiceSuggestPoweredImages ImageServiceOptimizationGuideSalientImages
             */

            /*[js-flags]
             * https://chromium.googlesource.com/v8/v8/+/master/src/flags/flag-definitions.h
             * 
             * https://stackoverflow.com/questions/73055564/which-nodejs-v8-flags-for-benchmarking
             * https://stackoverflow.com/questions/48387040/how-do-i-determine-the-correct-max-old-space-size-for-node-js
             * 
             * --always-opt //This does not improve performance, on the contrary; it causes V8 to waste CPU cycles on useless work.
             * --predictable-gc-schedule
             */

            /*[enable/disable-blink-features]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/runtime_enabled_features.json5
             * 
             * NetInfoConstantType, NetInfoDownlinkMax, V8IdleTasks, PrettyPrintJSONDocument
             */

            /*[Others]
             * https://github.com/brave/brave-core/pull/19457
             * https://go.dev/solutions/google/chrome
             * https://news.ycombinator.com/item?id=32741359
             * https://github.com/brave/brave-browser/issues/13
             * https://github.com/brave/brave-core/pull/114/files
             * https://github.com/brave/brave-core/blob/master/app/feature_defaults_unittest.cc
             * https://github.com/brave/brave-browser/issues/3855
             * 
             * https://github.com/brave/brave-browser/wiki/Deviations-from-Chromium-(features-we-disable-or-remove)
             * https://github.com/Alex313031/thorium/blob/main/infra/PATCHES.md
             * https://github.com/search?q=repo%3Airidium-browser%2Firidium-browser+patch&type=commits
             * https://github.com/bromite/bromite/tree/master/build/patches
             * https://github.com/ungoogled-software/ungoogled-chromium/tree/master/patches
             * https://github.com/saiarcot895/chromium-ubuntu-build/tree/master/debian/patches
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:components/network_session_configurator/browser/network_session_configurator_unittest.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:net/quic/quic_context.h
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/pref_names.h
             * https://source.chromium.org/chromium/chromium/src/+/main:components/metrics/metrics_pref_names.cc
             * 
             * https://www.chromium.org/developers/design-documents/network-stack/disk-cache/very-simple-backend/
             */

            //https://source.chromium.org/chromium/chromium/src/+/main:components/network_session_configurator/browser/network_session_configurator.cc
            Settings.CefCommandLineArgs.Add("force-fieldtrials", "SimpleCacheTrial/ExperimentYes/");

            string JsFlags = "--max-old-space-size=512,--optimize-gc-for-battery,--memory-reducer-favors-memory,--efficiency-mode,--battery-saver-mode";// "--always-opt,--gc-global,--gc-experiment-reduce-concurrent-marking-tasks";

            //DEFAULT ENABLED: MemoryPurgeInBackground, stop-in-background
            //ANDROID: InputStreamOptimizations
            string EnableFeatures = "QuickIntensiveWakeUpThrottlingAfterLoading,LowerHighResolutionTimerThreshold,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,ReduceCpuUtilization2,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LowLatencyCanvas2dImageChromium,LowLatencyWebGLImageChromium,NoStatePrefetchHoldback,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,BatterySaverModeAlignWakeUps,RestrictThreadPoolInBackground,IntensiveWakeUpThrottling:grace_period_seconds/5,CheckHTMLParserBudgetLessOften,Canvas2DHibernation,Canvas2DHibernationReleaseTransferMemory,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
            string DisableFeatures = "Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,LoadingPredictorPrefetch,WebBluetooth,MediaRouter,LiveCaption,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints,SegmentationPlatform,WebFontsCacheAwareTimeoutAdaption,SpeculationRulesPrefetchFuture,NavigationPredictor,Prerender2MainFrameNavigation,InstalledApp,BrowsingTopics,Fledge,MemoryCacheStrongReference,Prerender2NoVarySearch,Prerender2,InterestFeedContentSuggestions";

            string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility,SkipPreloadScanning,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
            string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI,DocumentPictureInPictureAPI,Prerender2";

            try
            {
                Settings.CefCommandLineArgs.Add("disable-features", DisableFeatures);
                Settings.CefCommandLineArgs.Add("enable-features", EnableFeatures);
                Settings.CefCommandLineArgs.Add("enable-blink-features", EnableBlinkFeatures);
                Settings.CefCommandLineArgs.Add("disable-blink-features", DisableBlinkFeatures);
            }
            catch
            {
                Settings.CefCommandLineArgs["disable-features"] += "," + DisableFeatures;
                Settings.CefCommandLineArgs["enable-features"] += "," + EnableFeatures;
                Settings.CefCommandLineArgs["enable-blink-features"] += "," + EnableBlinkFeatures;
                Settings.CefCommandLineArgs["disable-blink-features"] += "," + DisableBlinkFeatures;
            }

            Settings.CefCommandLineArgs.Add("blink-settings", "dataSaverEnabled=true,hyperlinkAuditingEnabled=false,lowPriorityIframesThreshold=5,smoothScrollForFindEnabled=true,dnsPrefetchingEnabled=false,doHtmlPreloadScanning=false,disallowFetchForDocWrittenScriptsInMainFrame=true,disallowFetchForDocWrittenScriptsInMainFrameIfEffectively2G=true,disallowFetchForDocWrittenScriptsInMainFrameOnSlowConnections=true");
            
            if (bool.Parse(GlobalSave.Get("LiteMode")))
            {
                //https://github.com/cypress-io/cypress/issues/22622
                //https://issues.chromium.org/issues/40220332
                Settings.CefCommandLineArgs["disable-features"] += ",LogJsConsoleMessages,BoostImagePriority,BoostImageSetLoadingTaskPriority,BoostFontLoadingTaskPriority,BoostVideoLoadingTaskPriority,BoostRenderBlockingStyleLoadingTaskPriority,BoostNonRenderBlockingStyleLoadingTaskPriority";
                Settings.CefCommandLineArgs["enable-features"] += ",ClientHintsSaveData,SaveDataImgSrcset,LowPriorityScriptLoading,LowPriorityAsyncScriptExecution";
                Settings.CefCommandLineArgs["enable-blink-features"] += ",PrefersReducedData";//ForceReduceMotion
                Settings.CefCommandLineArgs["blink-settings"] += ",imageAnimationPolicy=1,prefersReducedTransparency=true";
                JsFlags += ",--max-lazy,--lite-mode,--noexpose_wasm,--optimize-for-size";
            }
            else
            {
                JsFlags += "--enable-experimental-regexp-engine-on-excessive-backtracks,--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt,--experimental-wasm-branch-hinting,--experimental-wasm-instruction-tracing";
                if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
                    JsFlags += ",--experimental-wasm-jspi,--experimental-wasm-memory64,--experimental-wasm-type-reflection";
            }
            Settings.JavascriptFlags = JsFlags;
        }


        public Theme CurrentTheme;
        public Theme GetTheme(string Name = "")
        {
            if (string.IsNullOrEmpty(Name) && CurrentTheme != null)
                return CurrentTheme;
            Theme _Theme = Themes.Find(i => i.Name == Name);
            return _Theme == null ? Themes[0] : _Theme;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CloseSLBr(true);
            base.OnExit(e);
        }

        public void ClearAllData()
        {
            AdsBlocked = 0;
            TrackersBlocked = 0;
            Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
            Cef.GetGlobalRequestContext().ClearHttpAuthCredentialsAsync();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                {
                    if (BrowserView != null && BrowserView.Chromium != null && BrowserView.Chromium.IsBrowserInitialized)
                    {
                        if (BrowserView.Chromium.CanExecuteJavascriptInMainFrame)
                            BrowserView.Chromium.ExecuteScriptAsync("localStorage.clear();sessionStorage.clear();");
                        using (var DevToolsClient = BrowserView.Chromium.GetDevToolsClient())
                        {
                            //https://github.com/cefsharp/CefSharp/issues/1234
                            DevToolsClient.Storage.ClearDataForOriginAsync("*", "all");
                            DevToolsClient.Page.ClearCompilationCacheAsync();
                            DevToolsClient.Page.ResetNavigationHistoryAsync();
                            DevToolsClient.Network.ClearBrowserCookiesAsync();
                            DevToolsClient.Network.ClearBrowserCacheAsync();
                        }
                    }
                }
            }
            var infoWindow = new InformationDialogWindow("Alert", $"Settings", "All browsing data has been cleared", "\ue713");
            infoWindow.Topmost = true;
            infoWindow.ShowDialog();
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

                foreach (FileInfo _File in new DirectoryInfo(UserApplicationWindowsPath).GetFiles())
                    _File.Delete();
                if (bool.Parse(GlobalSave.Get("RestoreTabs")))
                {
                    foreach (MainWindow _Window in AllWindows)
                    {
                        Saving TabsSave = WindowsSaves[AllWindows.IndexOf(_Window)];
                        TabsSave.Clear();

                        for (int i = 0; i < _Window.Tabs.Count; i++)
                        {
                            BrowserTabItem Tab = _Window.Tabs[i];
                            if (Tab.ParentWindow != null)
                                TabsSave.Set(i.ToString(), Tab.Content.Address, false);
                        }
                        TabsSave.Set("Selected", _Window.TabsUI.SelectedIndex.ToString());
                        TabsSave.Set("Count", (_Window.Tabs.Count - 1).ToString());
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
            if (Utils.GetFileExtensionFromUrl(Url) != ".pdf")
            {
                if (Utils.IsHttpScheme(Url))
                    return new BitmapImage(new Uri("http://www.google.com/s2/favicons?sz=24&domain=" + Utils.CleanUrl(Url, true, true, true, false, false)));
                else if (Url.StartsWith("slbr://settings"))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://history"))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://downloads"))
                    return DownloadsTabIcon;
                return TabIcon;
            }
            else
                return PDFTabIcon;
        }

        public async Task<BitmapImage> SetIcon(string IconUrl, string Url = "")
        {
            //if (Utils.IsHttpScheme(IconUrl) && Utils.GetFileExtensionFromUrl(Url) != ".pdf")
            //    return new BitmapImage(new Uri(IconUrl));
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
                    catch { return TabIcon; }
                }
                else if (IconUrl.StartsWith("data:image/"))
                    return Utils.ConvertBase64ToBitmapImage(IconUrl);
                else if (Url.StartsWith("slbr://settings"))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://history"))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://downloads"))
                    return DownloadsTabIcon;
                return TabIcon;
            }
            else
                return PDFTabIcon;
        }
        private async Task<byte[]> DownloadImageDataAsync(string uri)
        {
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", UserAgentGenerator.BuildChromeBrand());
                    _WebClient.Headers.Add("Accept", "image/*;");
                    //_WebClient.Headers.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
                    return await _WebClient.DownloadDataTaskAsync(new Uri(uri));
                }
                catch { return null; }
            }
        }

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
        public void SetAppearance(Theme _Theme, string TabAlignment, bool AllowHomeButton, bool AllowTranslateButton, bool AllowAIButton, bool AllowReaderModeButton, int ShowExtensionButton, int ShowFavouritesBar)
        {
            GlobalSave.Set("TabAlignment", TabAlignment);

            GlobalSave.Set("AIButton", AllowAIButton);
            GlobalSave.Set("TranslateButton", AllowTranslateButton);
            GlobalSave.Set("HomeButton", AllowHomeButton);
            GlobalSave.Set("ReaderButton", AllowReaderModeButton);
            GlobalSave.Set("ExtensionButton", ShowExtensionButton);
            GlobalSave.Set("FavouritesBar", ShowFavouritesBar);

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
                _Window.SetAppearance(_Theme, TabAlignment, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
        }
    }

    public class Theme
    {
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
        public string Name;
        public Color BorderColor;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color GrayColor;
        public Color FontColor;
        public Color IndicatorColor;
        public bool DarkTitleBar;
        public bool DarkWebPage;
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
        Find = 32,

        ZoomIn = 40,
        ZoomOut = 41,
        ZoomReset = 42,
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
}
