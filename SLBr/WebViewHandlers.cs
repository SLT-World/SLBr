using CefSharp;
using CefSharp.Internals;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Protocols;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SLBr
{
    public static class WebViewManager
    {
        public static CoreWebView2Environment WebView2Environment { get; set; }
        public static CoreWebView2ControllerOptions WebView2ControllerOptions { get; set; }
        public static CoreWebView2ControllerOptions WebView2PrivateControllerOptions { get; set; }
        public static CoreWebView2WebResourceResponse WebView2CancelResponse { get; set; }
        public static CoreWebView2FindOptions WebView2FindOptions { get; set; }

        public static ChromiumLifeSpanHandler GlobalLifeSpanHandler { get; set; }
        public static ChromiumJsDialogHandler GlobalJsDialogHandler { get; set; }
        public static ChromiumKeyboardHandler GlobalKeyboardHandler { get; set; }
        public static ChromiumPermissionHandler GlobalPermissionHandler { get; set; }
        public static ChromiumDownloadHandler GlobalDownloadHandler { get; set; }
        public static ChromiumContextMenuHandler GlobalContextMenuHandler { get; set; }
        public static ChromiumFindHandler GlobalFindHandler { get; set; }
        public static ChromiumDialogHandler GlobalDialogHandler { get; set; }

        public static List<IWebView> WebViews = new List<IWebView>();
        public static Dictionary<IWebBrowser, ChromiumWebView> ChromiumWebViews = new Dictionary<IWebBrowser, ChromiumWebView>();

        public static WebViewSettings Settings { get; set; }
        public static WebViewRuntimeSettings RuntimeSettings { get; } = new WebViewRuntimeSettings();

        public static WebDownloadManager DownloadManager { get; } = new WebDownloadManager();

        public static bool IsWebView2Initialized { get; private set; } = false;
        public static bool IsCefInitialized { get; private set; } = false;
        public static bool IsTridentInitialized { get; private set; } = false;

        public static string WebView2Version { get; private set; } = "";

        public static IWebView Create(WebEngineType EngineType, string Url, WebViewBrowserSettings _BrowserSettings)
        {
            switch (EngineType)
            {
                case WebEngineType.Chromium:
                    return new ChromiumWebView(Url, _BrowserSettings);
                case WebEngineType.ChromiumEdge:
                    if (!IsWebView2Initialized)
                    {
                        try { WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(); }
                        catch (WebView2RuntimeNotFoundException) { return new ChromiumWebView(Url, _BrowserSettings); }
                    }
                    return new ChromiumEdgeWebView(Url, _BrowserSettings);
                case WebEngineType.Trident:
                    return new TridentWebView(Url, _BrowserSettings);
                default:
                    return new ChromiumWebView(Url, _BrowserSettings);
            }
        }
        /*public static string GetPreferencesString(string _String, string Parents, KeyValuePair<string, object> ObjectPair)
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

        public static void InitializeCEF()
        {
            if (IsCefInitialized)
                return;
            CefSettings ChromiumSettings = new CefSettings();
            ChromiumSettings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;

            ChromiumSettings.CachePath = Path.GetFullPath(Path.Combine(Settings.UserDataPath, "Cache"));
            ChromiumSettings.RootCachePath = Settings.UserDataPath;
            ChromiumSettings.PersistSessionCookies = false;

            ChromiumSettings.LogFile = Settings.LogFile;
            ChromiumSettings.LogSeverity = LogSeverity.Error;

            ChromiumSettings.Locale = Settings.Language;
            ChromiumSettings.AcceptLanguageList = string.Join(",", Settings.Languages);
            ChromiumSettings.JavascriptFlags = Settings.JavaScriptFlags;

            ChromiumSettings.CefCommandLineArgs.Remove("disable-back-forward-cache");
            foreach (var Flag in Settings.Flags)
                ChromiumSettings.AddNoErrorFlag(Flag.Key, Flag.Value);

            CefSharpSettings.RuntimeStyle = Settings.CefRuntimeStyle;
            foreach (var Scheme in Settings.Schemes)
            {
                ChromiumSettings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = Scheme.Key,
                    SchemeHandlerFactory = new ChromiumProtocolHandlerFactory(Scheme.Value),
                    IsStandard = true,
                    IsSecure = true
                });
            }
            Cef.Initialize(ChromiumSettings);

            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                string _;
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !RuntimeSettings.PDFViewer, out _);
                GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !RuntimeSettings.PDFViewer, out _);

                GlobalRequestContext.SetPreference("compact_mode", true, out _);
                GlobalRequestContext.SetPreference("history.saving_disabled", true, out _);
                GlobalRequestContext.SetPreference("profile.content_settings.enable_cpss.geolocation", false, out _);
                //GlobalRequestContext.SetPreference("accessibility.captions.live_caption_enabled", false, out _);

                GlobalRequestContext.SetPreference("autofill.enabled", false, out _);
                GlobalRequestContext.SetPreference("autofill.profile_enabled", false, out _);
                GlobalRequestContext.SetPreference("autofill.credit_card_enabled", false, out _);

                /*string _Preferences = string.Empty;
                foreach (KeyValuePair<string, object> e in GlobalRequestContext.GetAllPreferences(true))
                    _Preferences = GetPreferencesString(_Preferences, string.Empty, e);
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt")))
                    outputFile.Write(_Preferences);*/

                GlobalRequestContext.SetPreference("download_bubble.partial_view_enabled", false, out _);

                GlobalRequestContext.SetPreference("shopping_list_enabled", false, out _);
                GlobalRequestContext.SetPreference("browser_labs_enabled", false, out _);
                GlobalRequestContext.SetPreference("allow_dinosaur_easter_egg", false, out _);
                GlobalRequestContext.SetPreference("feedback_allowed", false, out _);
                GlobalRequestContext.SetPreference("policy.feedback_surveys_enabled", false, out _);
                GlobalRequestContext.SetPreference("policy.built_in_ai_apis_enabled", false, out _);

                GlobalRequestContext.SetPreference("ntp.promo_visible", false, out _);
                GlobalRequestContext.SetPreference("ntp.shortcust_visible", false, out _);
                GlobalRequestContext.SetPreference("ntp_snippets.enable", false, out _);
                GlobalRequestContext.SetPreference("ntp_snippets_by_dse.enable", false, out _);
                GlobalRequestContext.SetPreference("search.suggest_enabled", false, out _);
                GlobalRequestContext.SetPreference("side_search.enabled", false, out _);
                GlobalRequestContext.SetPreference("translate.enabled", false, out _);
                GlobalRequestContext.SetPreference("alternate_error_pages.enabled", false, out _);
                GlobalRequestContext.SetPreference("https_first_balanced_mode_enabled", true, out _);
                GlobalRequestContext.SetPreference("net.network_prediction_options", Settings.Performance == PerformancePreset.High ? 0 : 2, out _);
                GlobalRequestContext.SetPreference("safebrowsing.enabled", false, out _);

                GlobalRequestContext.SetPreference("browser.enable_spellchecking", Settings.SpellCheck, out _);
                //GlobalRequestContext.SetPreference("spellcheck.use_spelling_service", false, out _);
                if (Settings.Languages?.Length != 0)
                {
                    GlobalRequestContext.SetPreference("spellcheck.dictionaries", string.Join(',', Settings.Languages), out _);
                    GlobalRequestContext.SetPreference("intl.accept_languages", string.Join(',', Settings.Languages), out _);
                }
                //enable_a_ping
            });

            GlobalLifeSpanHandler = new ChromiumLifeSpanHandler();
            GlobalJsDialogHandler = new ChromiumJsDialogHandler();
            GlobalKeyboardHandler = new ChromiumKeyboardHandler();
            GlobalPermissionHandler = new ChromiumPermissionHandler();
            GlobalDownloadHandler = new ChromiumDownloadHandler();
            GlobalContextMenuHandler = new ChromiumContextMenuHandler();
            GlobalFindHandler = new ChromiumFindHandler();
            GlobalDialogHandler = new ChromiumDialogHandler();
            IsCefInitialized = true;
        }

        public static async void InitializeWebView2()
        {
            if (IsWebView2Initialized)
                return;
            //https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags
            //msWebView2TreatAppSuspendAsDeviceSuspend
            List<CoreWebView2CustomSchemeRegistration> CustomSchemeRegistrations = new List<CoreWebView2CustomSchemeRegistration>();
            foreach (var Scheme in Settings.Schemes.Where(i => i.Key != "*"))
                CustomSchemeRegistrations.Add(new CoreWebView2CustomSchemeRegistration(Scheme.Key) { HasAuthorityComponent = true, TreatAsSecure = true });
            CoreWebView2EnvironmentOptions EnvironmentOptions = new CoreWebView2EnvironmentOptions(Settings.BuildFlags(true), Settings.Language, null, false, CustomSchemeRegistrations);

            try { WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(null, EnvironmentOptions); }
            catch (WebView2RuntimeNotFoundException)
            {
                //MessageBox.Show("WebView2 Runtime is not installed. Please install it or disable WebView2.");
                return;// false;
            }

            EnvironmentOptions.AreBrowserExtensionsEnabled = true;
            EnvironmentOptions.IsCustomCrashReportingEnabled = true;
            EnvironmentOptions.ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay;

            WebView2Environment = await CoreWebView2Environment.CreateAsync(null, Settings.UserDataPath, EnvironmentOptions);

            WebView2ControllerOptions = WebView2Environment.CreateCoreWebView2ControllerOptions();
            WebView2ControllerOptions.DefaultBackgroundColor = Color.Black;
            //WebView2ControllerOptions.ProfileName = "Default";
            WebView2ControllerOptions.AllowHostInputProcessing = true;

            WebView2PrivateControllerOptions = WebView2Environment.CreateCoreWebView2ControllerOptions();
            WebView2PrivateControllerOptions.DefaultBackgroundColor = Color.Black;
            WebView2PrivateControllerOptions.AllowHostInputProcessing = true;
            WebView2PrivateControllerOptions.IsInPrivateModeEnabled = true;

            WebView2CancelResponse = WebView2Environment.CreateWebResourceResponse(Stream.Null, 403, "Blocked", "Content-Type: text/plain");

            WebView2FindOptions = WebView2Environment.CreateFindOptions();
            WebView2FindOptions.ShouldHighlightAllMatches = true;
            WebView2FindOptions.ShouldMatchWord = false;
            WebView2FindOptions.SuppressDefaultFindDialog = true;
            IsWebView2Initialized = true;
        }
        public static void DeleteWebView2HighDPIRegistry()
        {
            try
            {
                using (RegistryKey? Key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true))
                {
                    if (Key == null)
                        return;
                    string[] ValueNames = Key.GetValueNames();
                    foreach (string ValueName in ValueNames)
                    {
                        if (ValueName.EndsWith("msedgewebview2.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            object Value = Key.GetValue(ValueName);
                            if (Value != null && Value.ToString().Contains("HIGHDPIAWARE"))
                                Key.DeleteValue(ValueName);
                        }
                    }
                }
            }
            catch { }
        }

        public static void InitializeTrident()
        {
            if (IsTridentInitialized)
                return;
            //https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/general-info/ee330720(v=vs.85)
            SetIEFeatureControlKey("FEATURE_BROWSER_EMULATION", (uint)Settings.TridentVersion);
            SetIEFeatureControlKey("FEATURE_GPU_RENDERING", (uint)(Settings.GPUAcceleration ? 1 : 0));
            SetIEFeatureControlKey("FEATURE_ALLOW_HIGHFREQ_TIMERS", (uint)(Settings.Performance != PerformancePreset.Low ? 1 : 0));

            //SetIEFeatureControlKey("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", 1);
            SetIEFeatureControlKey("FEATURE_AJAX_CONNECTIONEVENTS", 1);
            SetIEFeatureControlKey("FEATURE_NINPUT_LEGACYMODE", 0);

            SetIEFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", 1);

            SetIEFeatureControlKey("FEATURE_BLOCK_LMZ_IMG", 1);
            SetIEFeatureControlKey("FEATURE_BLOCK_LMZ_OBJECT", 1);
            SetIEFeatureControlKey("FEATURE_BLOCK_LMZ_SCRIPT", 1);

            SetIEFeatureControlKey("FEATURE_SPELLCHECKING", (uint)(Settings.SpellCheck ? 1 : 0));
            SetIEFeatureControlKey("FEATURE_VIEWLINKEDWEBOC_IS_UNSAFE", 1);
            SetIEFeatureControlKey("FEATURE_BLOCK_CROSS_PROTOCOL_FILE_NAVIGATION", 1);
            SetIEFeatureControlKey("FEATURE_RESTRICT_ABOUT_PROTOCOL_IE7", 1);
            SetIEFeatureControlKey("FEATURE_SHOW_APP_PROTOCOL_WARN_DIALOG", 1);
            SetIEFeatureControlKey("FEATURE_IFRAME_MAILTO_THRESHOLD", 1);
            SetIEFeatureControlKey("FEATURE_LOCALMACHINE_LOCKDOWN", 1);
            //SetIEFeatureControlKey("FEATURE_MIME_HANDLING", 1);
            //SetIEFeatureControlKey("FEATURE_RESTRICT_ACTIVEXINSTALL", 1);

            SetIEFeatureControlKey("FEATURE_UNC_SAVEDFILECHECK", 1);
            SetIEFeatureControlKey("FEATURE_DISABLE_TELNET_PROTOCOL", 1);
            IsTridentInitialized = true;
        }
        private static void SetIEFeatureControlKey(string Feature, uint Value)
        {
            using (var Key = Registry.CurrentUser.CreateSubKey(string.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", Feature), RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                Key?.SetValue("SLBr.exe", Value, RegistryValueKind.DWord);
                Key?.Close();
            }
        }

        public static ProtocolResponse GopherHandler(string Url, string Extra = "")
        {
            GeminiGopherIResponse Response = Gopher.Fetch(new Uri(Url));
            if (Response == null)
                return ProtocolResponse.FromString($"<h1>404 Not Found</h1>", "text/html");
            //return ProtocolResponse.FromString("<h1>Failed to fetch resource</h1>", "text/html");
            return ProtocolResponse.FromString(TextGopher.NewFormat(Response), Response.Mime.Contains("application/gopher-menu") ? "text/html" : Response.Mime);
        }
        public static ProtocolResponse GeminiHandler(string Url, string Extra = "")
        {
            GeminiGopherIResponse Response = Gemini.Fetch(new Uri(Url));
            if (Response == null)
                return ProtocolResponse.FromString($"<h1>404 Not Found</h1>", "text/html");
            //return ProtocolResponse.FromString("<h1>Failed to fetch resource</h1>", "text/html");
            return ProtocolResponse.FromString(TextGemini.NewFormat(Response), Response.Mime.Contains("text/gemini") ? "text/html" : Response.Mime);
        }
        public static ProtocolResponse SLBrHandler(string Url, string Extra = "")
        {
            try
            {
                string ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
                Uri _Uri = new Uri(Url);
                string Host = _Uri.Host.ToLower();
                string Page = _Uri.AbsolutePath.TrimStart('/');

                string[] SLBrURLs = ["credits", "newtab", "downloads", "history", "settings", "tetris"];
                if (SLBrURLs.Contains(Host))
                {
                    string FileName = string.IsNullOrWhiteSpace(Page) ? $"{Host}.html" : Page;
                    if (Extra == "1" && Host == "newtab")
                        FileName = string.IsNullOrWhiteSpace(Page) ? $"private.html" : Page;
                    string FilePath = Path.Combine(ResourcesPath, FileName);
                    if (!File.Exists(FilePath))
                        return ProtocolResponse.FromString($"<h1>404 Not Found</h1>", "text/html");
                    string MimeType = Utils.GetFileExtension(FilePath) switch
                    {
                        ".html" => "text/html",
                        ".htm" => "text/html",
                        ".js" => "application/javascript",
                        ".css" => "text/css",
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        ".svg" => "image/svg+xml",
                        ".ico" => "image/x-icon",
                        _ => "application/octet-stream"
                    };
                    return ProtocolResponse.FromBytes(File.ReadAllBytes(FilePath), MimeType);
                }
                return ProtocolResponse.FromString($"<h1>404 Not Found</h1>", "text/html");
            }
            catch (Exception ex)
            {
                return ProtocolResponse.FromString(
                    $"<h1>Error</h1><pre>{System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>",
                    "text/html"
                );
            }
        }
        public static ProtocolResponse OverrideHandler(string Url, string Extra = "")
        {
            if (OverrideRequests.TryGetValue(Url, out RequestOverrideItem Item))
            {
                if (Item.Uses != -1)
                {
                    Item.Uses -= 1;
                    if (Item.Uses == 0)
                        OverrideRequests.Remove(Url, out Item);
                }
                return ProtocolResponse.FromBytes(Item.Data, Item.MimeType);
            }
            return null;
        }

        public static bool RegisterOverrideRequest(string Url, byte[] Data, string MimeType = ResourceHandler.DefaultMimeType/*, bool limitedUse = false*/, int Uses = 1, string Error = "")
        {
            if (Uri.TryCreate(Url, UriKind.Absolute, out Uri URI))
            {
                RequestOverrideItem Entry = new RequestOverrideItem(Data, MimeType, Uses, Error);
                OverrideRequests.AddOrUpdate(URI.AbsoluteUri, Entry, (k, v) => Entry);
                return true;
            }
            return false;
        }

        public static bool UnregisterOverrideRequest(string Url)
        {
            return OverrideRequests.TryRemove(Url, out _);
        }
        public static ConcurrentDictionary<string, RequestOverrideItem> OverrideRequests = new ConcurrentDictionary<string, RequestOverrideItem>(StringComparer.OrdinalIgnoreCase);
    }

    public class RequestOverrideItem
    {
        public byte[] Data;
        public string MimeType;
        public string Error;
        public int Uses;

        public RequestOverrideItem(byte[] _Data, string _MimeType, int _Uses = 1, string _Error = "")
        {
            Data = _Data;
            MimeType = _MimeType;
            Uses = _Uses;
            Error = _Error;
        }
    }

    public enum TridentEmulationVersion: uint
    {
        IE7 = 7000,
        IE8 = 8888,
        IE9 = 9999,
        IE10 = 10001,
        IE11 = 11001,
        //Edge = 12001
    }
    public enum PerformancePreset
    {
        Low,
        Default,
        High
    }

    public class WebViewSettings
    {
        /*public WebViewSettings()
        {
            WebViewManager.Settings = this;
        }*/
        
        public string Language;
        public string[] Languages = Array.Empty<string>();

        public string UserDataPath;
        public string LogFile;
        public string DownloadFolderPath = string.Empty;
        public bool DownloadPrompt = true;

        public PerformancePreset Performance = PerformancePreset.Default;
        public TridentEmulationVersion TridentVersion = TridentEmulationVersion.IE11;
        public CefRuntimeStyle CefRuntimeStyle = CefRuntimeStyle.Default;
        public bool SpellCheck = true;

        public string JavaScriptFlags = string.Empty;
        public Dictionary<string, string> Flags = new();
        //https://www.chromium.org/developers/how-tos/run-chromium-with-flags/
        public string BuildFlags(bool IncludeJavaScript = false)
        {
            StringBuilder _StringBuilder = new StringBuilder();
            foreach (var Flag in Flags)
            {
                if (string.IsNullOrWhiteSpace(Flag.Value))
                    _StringBuilder.Append($"--{Flag.Key} ");
                else
                    _StringBuilder.Append($"--{Flag.Key}={Flag.Value.Replace("\"", "\\\"")} ");
            }
            if (IncludeJavaScript)
                _StringBuilder.Append($"--js-flags=\"{JavaScriptFlags.Replace("\"", "\\\"")}\"");
            return _StringBuilder.ToString().Trim();
        }
        public readonly Dictionary<string, ProtocolHandler> Schemes = new();
        public void RegisterProtocol(string Scheme, ProtocolHandler Handler) => Schemes[Scheme] = Handler;

        public void AddFlag(string Key, string Value) => Flags.Add(Key, Value);

        public void AddFlag(string Value) => Flags.Add(Value, string.Empty);

        public bool GPUAcceleration = true;
        /*public bool PrintPreview = true;*/
    }
    public class WebViewRuntimeSettings
    {
        private bool _PDFViewer = true;
        public bool PDFViewer
        {
            get { return _PDFViewer; }
            set
            {
                _PDFViewer = value;
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !value, out string _);
                        GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !value, out string _);
                    });
                }
            }
        }
    }

    public class WebDownloadManager
    {
        public event Action<WebDownloadItem> DownloadStarted;
        public event Action<WebDownloadItem> DownloadUpdated;
        public event Action<WebDownloadItem> DownloadCompleted;

        public void Started(WebDownloadItem Item) => DownloadStarted?.RaiseUIAsync(Item);
        public void Updated(WebDownloadItem Item) => DownloadUpdated?.RaiseUIAsync(Item);
        public void Completed(WebDownloadItem Item) => DownloadCompleted?.RaiseUIAsync(Item);

        //private readonly Dictionary<string, WebDownloadItem> ActiveDownloads = new();

        public async Task StartDownloadAsync(string Url, string TargetPath, bool ShowDialog, string DialogFilter = "")
        {
            if (ShowDialog)
            {
                SaveFileDialog SaveDialog = new SaveFileDialog();
                SaveDialog.Filter = string.IsNullOrEmpty(DialogFilter) ? "All Files (*.*)|*.*" : DialogFilter;
                //https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/dd459587(v=vs.95) Guide on proper file dialog wild cards
                SaveDialog.InitialDirectory = WebViewManager.Settings.DownloadFolderPath;
                SaveDialog.FileName = Path.GetFileName(Url);
                if (SaveDialog.ShowDialog() == true)
                    TargetPath = SaveDialog.FileName;
                else
                    return;
            }
            var Item = new WebDownloadItem
            {
                ID = Guid.NewGuid().ToString(),
                Url = Url,
                FileName = Path.GetFileName(TargetPath),
                FullPath = TargetPath,
                State = WebDownloadState.InProgress
            };

            //ActiveDownloads[Item.ID] = Item;
            Started(Item);

            try
            {
                using var Client = new HttpClient();
                using var Response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);

                Response.EnsureSuccessStatusCode();
                Item.TotalBytes = Response.Content.Headers.ContentLength ?? -1;

                await using FileStream _FileStream = new FileStream(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var Stream = await Response.Content.ReadAsStreamAsync();

                var Buffer = new byte[8192];
                int Read;
                while ((Read = await Stream.ReadAsync(Buffer, 0, Buffer.Length)) > 0)
                {
                    await _FileStream.WriteAsync(Buffer, 0, Read);
                    Item.ReceivedBytes += Read;
                    Updated(Item);
                }

                Item.State = WebDownloadState.Completed;
                Completed(Item);
            }
            catch
            {
                Item.State = WebDownloadState.Canceled;
                Completed(Item);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> CommandHandler;
        private readonly Func<object, bool> CanExecuteHandler;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> _CommandHandler, Func<object, bool> _CanExecuteHandler = null)
        {
            CommandHandler = _CommandHandler;
            CanExecuteHandler = _CanExecuteHandler;
        }
        public RelayCommand(Action _CommandHandler, Func<bool> _CanExecuteHandler = null) : this(_ => _CommandHandler(), _CanExecuteHandler == null ? null : new Func<object, bool>(_ => _CanExecuteHandler()))
        {
        }

        public void Execute(object parameter)
        {
            CommandHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteHandler == null || CanExecuteHandler(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
    }

    public class ChromiumLifeSpanHandler : ILifeSpanHandler
    {
        public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName,
            WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo,
            IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            if (targetDisposition == WindowOpenDisposition.NewPictureInPicture)
                return false;
            else
            {
                if (targetDisposition == WindowOpenDisposition.CurrentTab)
                    browser.MainFrame.LoadUrl(targetUrl);
                else
                {
                    NewTabRequestEventArgs Args = new NewTabRequestEventArgs(targetUrl, targetDisposition == WindowOpenDisposition.NewBackgroundTab, targetDisposition == WindowOpenDisposition.NewPopup ? new Rect(popupFeatures.X ?? 0, popupFeatures.Y ?? 0, popupFeatures.Width ?? 0, popupFeatures.Height ?? 0) : null);
                    WebViewManager.ChromiumWebViews[browserControl]?.NotifyNewTabRequested(Args);

                    //CefSharp doesn't seem to support the ability to set something like "var myWindow = window.open("", "MsgWindow", "width=200,height=100");"
                    /*if (Args.WebView is ChromiumWebView ChromiumWebView)
                        newBrowser = (ChromiumWebBrowser)ChromiumWebView.Control;
                    else
                        newBrowser = null;*/
                }
            }
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            MessageBox.Show("Hoi");
            if (browser.IsPopup)
                return false;
            return true;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
    public class ChromiumJsDialogHandler : IJsDialogHandler
    {
        public bool OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
        {
            string Address = string.Empty;
            Application.Current?.Dispatcher.Invoke(() => Address = chromiumWebBrowser.Address);
            var Args = new ScriptDialogEventArgs(ScriptDialogType.BeforeUnload, Address, messageText, "", isReload);
            WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaiseScriptDialog(Args);
            if (Args.Handled)
            {
                callback.Continue(Args.Result);
                return true;
            }
            callback.Continue(true);
            return true;
        }

        public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        public bool OnJSDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, CefJsDialogType DialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            var Args = new ScriptDialogEventArgs((ScriptDialogType)DialogType, originUrl, messageText, defaultPromptText);
            WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaiseScriptDialog(Args);
            if (Args.Handled)
            {
                if (DialogType == CefJsDialogType.Prompt)
                    callback.Continue(Args.Result, Args.PromptResult);
                else
                    callback.Continue(Args.Result);
                return true;
            }

            suppressMessage = true;
            return false;
        }

        public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (Window Window in Application.Current.Windows)
                {
                    if (Window is InformationDialogWindow || Window is PromptDialogWindow)
                        Window.Close();
                }
            });
        }
    }
    public class ChromiumKeyboardHandler : IKeyboardHandler
    {
        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey) => true;
        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            if (type == KeyType.RawKeyDown)
            {
                bool HasControl = modifiers == CefEventFlags.ControlDown;
                bool HasShift = modifiers == CefEventFlags.ShiftDown;
                bool HasAlt = modifiers == CefEventFlags.AltDown;
                int WPFKeyCode = (int)KeyInterop.KeyFromVirtualKey(windowsKeyCode);
                foreach (HotKey Key in HotKeyManager.HotKeys)
                {
                    if (Key.KeyCode == WPFKeyCode && Key.Control == HasControl && Key.Shift == HasShift && Key.Alt == HasAlt)
                    {
                        Application.Current?.Dispatcher.Invoke(() => Key.Callback());
                        break;
                    }
                }
            }
            return false;
        }
    }
    public class ChromiumResourceRequestHandler : IResourceRequestHandler
    {
        private ChromiumWebView WebView;
        public ChromiumResourceRequestHandler(ChromiumWebView _WebView)
        {
            WebView = _WebView;
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            ResourceRequestEventArgs Args = new ResourceRequestEventArgs(request.Url, browser.FocusedFrame == null ? string.Empty : browser.FocusedFrame.Url, request.Method, request.ResourceType.ToResourceRequestType(), new Dictionary<string, string>());
            //request.Headers.AllKeys.ToDictionary(k => k, k => request.Headers[k])
            WebView.RaiseResourceRequest(Args);
            if (Args.Cancel)
                return CefReturnValue.Cancel;
            if (Args.ModifiedHeaders != null && Args.ModifiedHeaders.Count != 0)
            {
                foreach (var Header in Args.ModifiedHeaders)
                    request.SetHeaderByName(Header.Key, Header.Value, true);
            }
            return CefReturnValue.Continue;
        }

        public IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request) => null;
        public ICookieAccessFilter GetCookieAccessFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request) => null;
        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl) { }
        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            WebView.RaiseResourceResponded(new ResourceRespondedResult(request.Url, request.ResourceType.ToResourceRequestType()));
            return false;
        }
        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response) => null;
        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            WebView.RaiseResourceLoaded(new ResourceLoadedResult(request.Url, status != UrlRequestStatus.Failed && status != UrlRequestStatus.Canceled, receivedContentLength, request.ResourceType.ToResourceRequestType()));
        }
        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            ExternalProtocolEventArgs Args = new ExternalProtocolEventArgs(request.Url);
            WebView.RaiseExternalProtocolRequested(Args);
            if (Args.Launch)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = request.Url,
                    UseShellExecute = true
                });
            }
            return true;
        }
        public void Dispose()
        {
            GC.Collect(GC.MaxGeneration);
            GC.SuppressFinalize(this);
        }
    }
    public class ChromiumPermissionHandler : IPermissionHandler
    {
        public void OnDismissPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, PermissionRequestResult result)
        {
        }

        public bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
        {
            if (callback == null)
                return false;
            //WebPermissionKind _ProperPermissionRequestType = (MediaAccessPermissionType)requestedPermissions;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                /*bool NoPermission = true;
                MediaAccessPermissionType AllowedPermissions = MediaAccessPermissionType.None;
                foreach (MediaAccessPermissionType SinglePermission in Enum.GetValues(typeof(MediaAccessPermissionType)))
                {
                    if (requestedPermissions.HasFlag(SinglePermission))
                    {
                        WebPermissionKind WebPermission = SinglePermission switch
                        {
                            MediaAccessPermissionType.AudioCapture => WebPermissionKind.MicStream,
                            MediaAccessPermissionType.VideoCapture => WebPermissionKind.CameraStream,
                            MediaAccessPermissionType.DesktopAudioCapture => WebPermissionKind.RecordAudio,
                            MediaAccessPermissionType.DesktopVideoCapture => WebPermissionKind.ScreenShare,
                            _ => WebPermissionKind.None
                        };
                        if (WebPermission == WebPermissionKind.None)
                            continue;
                        var Args = new PermissionRequestedEventArgs(requestingOrigin, WebPermission);
                        WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaisePermissionRequested(Args);
                        if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                        {
                            callback.Dispose();
                            return;
                        }
                        if (Args.State == WebPermissionState.Allow)
                        {
                            NoPermission = false;
                            AllowedPermissions |= SinglePermission;
                        }
                    }
                }
                if (NoPermission)
                    callback.Cancel();
                else
                    callback.Continue(AllowedPermissions);*/


                var Args = new PermissionRequestedEventArgs(requestingOrigin, requestedPermissions.ToWebPermission());
                WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaisePermissionRequested(Args);
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                if (Args.State == WebPermissionState.Allow)
                    callback.Continue(requestedPermissions);
                else if (Args.State == WebPermissionState.Deny)
                    callback.Cancel();
                callback.Dispose();
            }));
            return true;
        }

        //CefSharp's PermissionRequestType enum isn't synced with CEF's
        //https://github.com/chromiumembedded/cef/blob/master/include/internal/cef_types.h
        //https://github.com/cefsharp/CefSharp/blob/master/CefSharp/Enums/PermissionRequestType.cs
        public enum FixedPermissionRequestType : uint
        {
            None = 0,
            ArSession = 1 << 0,
            CameraPanTiltZoom = 1 << 1,
            CameraStream = 1 << 2,
            CapturedSurfaceControl = 1 << 3,
            Clipboard = 1 << 4,
            TopLevelStorageAccess = 1 << 5,
            DiskQuota = 1 << 6,
            LocalFonts = 1 << 7,
            Geolocation = 1 << 8,
            HandTracking = 1 << 9,
            IdentityProvider = 1 << 10,
            IdleDetection = 1 << 11,
            MicStream = 1 << 12,
            MidiSysex = 1 << 13,
            MultipleDownloads = 1 << 14,
            Notifications = 1 << 15,
            KeyboardLock = 1 << 16,
            PointerLock = 1 << 17,
            ProtectedMediaIdentifier = 1 << 18,
            RegisterProtocolHandler = 1 << 19,
            StorageAccess = 1 << 20,
            VrSession = 1 << 21,
            WebAppInstallation = 1 << 22,
            WindowManagement = 1 << 23,
            FileSystemAccess = 1 << 24,
            LocalNetworkAccess = 1 << 25
        }

        public bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
        {
            if (callback == null)
                return false;
            FixedPermissionRequestType _ProperPermissionRequestType = (FixedPermissionRequestType)requestedPermissions;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                /*PermissionRequestResult Result = PermissionRequestResult.Ignore;
                PermissionRequestType AllowedPermissions = PermissionRequestType.None;
                foreach (FixedPermissionRequestType SinglePermission in Enum.GetValues(typeof(FixedPermissionRequestType)))
                {
                    if (_ProperPermissionRequestType.HasFlag(SinglePermission))
                    {
                        WebPermissionKind WebPermission = SinglePermission switch
                        {
                            FixedPermissionRequestType.ArSession => WebPermissionKind.ArSession,
                            FixedPermissionRequestType.CameraPanTiltZoom => WebPermissionKind.CameraPanTiltZoom,
                            FixedPermissionRequestType.CameraStream => WebPermissionKind.CameraStream,
                            FixedPermissionRequestType.CapturedSurfaceControl => WebPermissionKind.CapturedSurfaceControl,
                            FixedPermissionRequestType.Clipboard => WebPermissionKind.Clipboard,
                            FixedPermissionRequestType.TopLevelStorageAccess => WebPermissionKind.TopLevelStorageAccess,
                            FixedPermissionRequestType.DiskQuota => WebPermissionKind.DiskQuota,
                            FixedPermissionRequestType.LocalFonts => WebPermissionKind.LocalFonts,
                            FixedPermissionRequestType.Geolocation => WebPermissionKind.Geolocation,
                            FixedPermissionRequestType.IdentityProvider => WebPermissionKind.IdentityProvider,
                            FixedPermissionRequestType.IdleDetection => WebPermissionKind.IdleDetection,
                            FixedPermissionRequestType.MicStream => WebPermissionKind.MicStream,
                            FixedPermissionRequestType.MidiSysex => WebPermissionKind.MidiSysex,
                            FixedPermissionRequestType.MultipleDownloads => WebPermissionKind.MultipleDownloads,
                            FixedPermissionRequestType.Notifications => WebPermissionKind.Notifications,
                            FixedPermissionRequestType.KeyboardLock => WebPermissionKind.KeyboardLock,
                            FixedPermissionRequestType.PointerLock => WebPermissionKind.PointerLock,
                            FixedPermissionRequestType.ProtectedMediaIdentifier => WebPermissionKind.ProtectedMediaIdentifier,
                            FixedPermissionRequestType.RegisterProtocolHandler => WebPermissionKind.RegisterProtocolHandler,
                            FixedPermissionRequestType.StorageAccess => WebPermissionKind.StorageAccess,
                            FixedPermissionRequestType.VrSession => WebPermissionKind.VrSession,
                            FixedPermissionRequestType.WebAppInstallation => WebPermissionKind.WebAppInstallation,
                            FixedPermissionRequestType.WindowManagement => WebPermissionKind.WindowManagement,
                            FixedPermissionRequestType.FileSystemAccess => WebPermissionKind.FileSystemAccess,
                            FixedPermissionRequestType.LocalNetworkAccess => WebPermissionKind.LocalNetworkAccess,
                            _ => WebPermissionKind.None
                        };
                        if (WebPermission == WebPermissionKind.None)
                            continue;
                        var Args = new PermissionRequestedEventArgs(requestingOrigin, WebPermission);
                        WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaisePermissionRequested(Args);
                        if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                        {
                            callback.Dispose();
                            return;
                        }
                        if (Args.State == WebPermissionState.Allow)
                        {
                            Result = PermissionRequestResult.Accept;
                            AllowedPermissions |= (PermissionRequestType)SinglePermission;
                        }
                        else if (Args.State == WebPermissionState.Deny)
                            Result = PermissionRequestResult.Deny;
                    }
                }
                callback.Continue(Result);
                callback.Dispose();*/
                /*WebPermissionKind WebPermission = SinglePermission switch
                {
                    FixedPermissionRequestType.ArSession => WebPermissionKind.ArSession,
                    FixedPermissionRequestType.CameraPanTiltZoom => WebPermissionKind.CameraPanTiltZoom,
                    FixedPermissionRequestType.CameraStream => WebPermissionKind.CameraStream,
                    FixedPermissionRequestType.CapturedSurfaceControl => WebPermissionKind.CapturedSurfaceControl,
                    FixedPermissionRequestType.Clipboard => WebPermissionKind.Clipboard,
                    FixedPermissionRequestType.TopLevelStorageAccess => WebPermissionKind.TopLevelStorageAccess,
                    FixedPermissionRequestType.DiskQuota => WebPermissionKind.DiskQuota,
                    FixedPermissionRequestType.LocalFonts => WebPermissionKind.LocalFonts,
                    FixedPermissionRequestType.Geolocation => WebPermissionKind.Geolocation,
                    FixedPermissionRequestType.IdentityProvider => WebPermissionKind.IdentityProvider,
                    FixedPermissionRequestType.IdleDetection => WebPermissionKind.IdleDetection,
                    FixedPermissionRequestType.MicStream => WebPermissionKind.MicStream,
                    FixedPermissionRequestType.MidiSysex => WebPermissionKind.MidiSysex,
                    FixedPermissionRequestType.MultipleDownloads => WebPermissionKind.MultipleDownloads,
                    FixedPermissionRequestType.Notifications => WebPermissionKind.Notifications,
                    FixedPermissionRequestType.KeyboardLock => WebPermissionKind.KeyboardLock,
                    FixedPermissionRequestType.PointerLock => WebPermissionKind.PointerLock,
                    FixedPermissionRequestType.ProtectedMediaIdentifier => WebPermissionKind.ProtectedMediaIdentifier,
                    FixedPermissionRequestType.RegisterProtocolHandler => WebPermissionKind.RegisterProtocolHandler,
                    FixedPermissionRequestType.StorageAccess => WebPermissionKind.StorageAccess,
                    FixedPermissionRequestType.VrSession => WebPermissionKind.VrSession,
                    FixedPermissionRequestType.WebAppInstallation => WebPermissionKind.WebAppInstallation,
                    FixedPermissionRequestType.WindowManagement => WebPermissionKind.WindowManagement,
                    FixedPermissionRequestType.FileSystemAccess => WebPermissionKind.FileSystemAccess,
                    FixedPermissionRequestType.LocalNetworkAccess => WebPermissionKind.LocalNetworkAccess,
                    _ => WebPermissionKind.None
                };*/
                //if (WebPermission == WebPermissionKind.None)
                //    continue;
                var Args = new PermissionRequestedEventArgs(requestingOrigin, _ProperPermissionRequestType.ToWebPermission());
                WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaisePermissionRequested(Args);
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                if (Args.State == WebPermissionState.Allow)
                    callback.Continue(PermissionRequestResult.Accept);
                else if (Args.State == WebPermissionState.Deny)
                    callback.Continue(PermissionRequestResult.Deny);
                callback.Dispose();

                /*var Args = new PermissionRequestedEventArgs(requestingOrigin, _ProperPermissionRequestType);
                WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaisePermissionRequested(Args);
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                callback.Continue(Args.State.ToCefPermissionState());
                callback.Dispose();*/
            }));
            return true;
        }
    }
    public class ChromiumDownloadHandler : IDownloadHandler
    {
        private Dictionary<int, WebDownloadItem> WebDownloadItems = new Dictionary<int, WebDownloadItem>();
        private Dictionary<int, IDownloadItemCallback> DownloadCallbacks = new Dictionary<int, IDownloadItemCallback>();

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod) => true;

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            WebDownloadItem Item = new WebDownloadItem
            {
                ID = downloadItem.Id.ToString(),
                Url = downloadItem.Url,
                FileName = downloadItem.SuggestedFileName,
                FullPath = Path.Combine(WebViewManager.Settings.DownloadFolderPath, downloadItem.SuggestedFileName),
                TotalBytes = downloadItem.TotalBytes,
                State = WebDownloadState.InProgress
            };
            WebDownloadItems[downloadItem.Id] = Item;

            WebViewManager.DownloadManager.Started(Item);
            if (!callback.IsDisposed)
            {
                using (callback)
                    callback.Continue(Path.Combine(WebViewManager.Settings.DownloadFolderPath, downloadItem.SuggestedFileName), WebViewManager.Settings.DownloadPrompt);
            }
            return true;
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            if (!WebDownloadItems.TryGetValue(downloadItem.Id, out var Item))
            {
                Item = new WebDownloadItem { ID = downloadItem.Id.ToString() };
                WebDownloadItems[downloadItem.Id] = Item;
            }

            if (string.IsNullOrEmpty(Item.FileName))
            {
                Item.Url = downloadItem.Url;
                Item.FileName = Path.GetFileName(Item.FullPath);
            }
            Item.TotalBytes = downloadItem.TotalBytes;
            Item.ReceivedBytes = downloadItem.ReceivedBytes;
            Item.Pause = () => { DownloadCallbacks[downloadItem.Id].Pause(); };
            Item.Resume = () => { DownloadCallbacks[downloadItem.Id].Resume(); };
            Item.Cancel = () => { DownloadCallbacks[downloadItem.Id].Cancel(); };
            Item.FullPath = downloadItem.FullPath;

            if (downloadItem.IsInProgress)
            {
                DownloadCallbacks[downloadItem.Id] = callback;
                Item.State = WebDownloadState.InProgress;
                WebViewManager.DownloadManager.Updated(Item);
            }
            else
            {
                //WARNING: Keep this warning path otherwise the open downloads wouldn't work
                DownloadCallbacks.Remove(downloadItem.Id);
                if (downloadItem.IsCancelled)
                    Item.State = WebDownloadState.Canceled;
                else if (downloadItem.IsComplete)
                    Item.State = WebDownloadState.Completed;
                WebViewManager.DownloadManager.Completed(Item);
            }
        }
    }
    public class ChromiumContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            if (parameters.FrameUrl.StartsWith("devtools:", StringComparison.Ordinal))
                return;
            //Looks like CefSharp suffers the same issue of spellcheck seen in WebView2
            /*for (int i = 0; i < model.Count; i++)
            {
                var commandId = model.GetCommandIdAt(i);
                var label = model.GetLabelAt(i);
                var type = model.GetTypeAt(i);
                var enabled = model.IsEnabledAt(i);
                var visible = model.IsVisibleAt(i);

                MessageBox.Show($"[{i}] {commandId} | {label} | {type} | Enabled={enabled} | Visible={visible}");

                // If it's a submenu, recurse
                var subMenu = model.GetSubMenuAt(i);
                if (subMenu != null)
                {
                    for (int j = 0; j < subMenu.Count; j++)
                    {
                        var subCommandId = subMenu.GetCommandIdAt(j);
                        var subLabel = subMenu.GetLabelAt(j);
                        MessageBox.Show($"    Sub[{j}] {subCommandId} | {subLabel}");
                    }
                }
            }*/
            model.Clear();
            WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaiseContextMenu(new WebContextMenuEventArgs
            {
                X = parameters.XCoord,
                Y = parameters.YCoord,
                LinkUrl = parameters.LinkUrl,
                FrameUrl = parameters.FrameUrl,
                SelectionText = parameters.SelectionText,
                IsEditable = parameters.IsEditable,
                DictionarySuggestions = parameters.DictionarySuggestions,
                MisspelledWord = parameters.MisspelledWord,
                SourceUrl = parameters.SourceUrl,
                SpellCheck = parameters.IsSpellCheckEnabled,
                MediaType = parameters.MediaType.ToWebContextMenuMediaType(),
                MenuType = parameters.TypeFlags.ToWebContextMenuType()
            });
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags) => false;

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame) { }
        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback) => false;
    }
    public class ChromiumProtocolHandlerFactory : ISchemeHandlerFactory
    {
        private readonly ProtocolHandler Handler;

        public ChromiumProtocolHandlerFactory(ProtocolHandler _Handler)
        {
            Handler = _Handler;
        }

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            ChromiumWebView ChromiumWebView = null;
            foreach (var KeyValue in WebViewManager.ChromiumWebViews)
            {
                if (KeyValue.Key.BrowserCore != null && KeyValue.Key.BrowserCore.IsSame(browser))
                {
                    ChromiumWebView = KeyValue.Value;
                    break;
                }
            }
            ProtocolResponse Response = Handler(request.Url, ChromiumWebView?.Settings.Private.ToInt().ToString() ?? string.Empty);
            return ResourceHandler.FromByteArray(Response.Data, Response.MimeType);
        }
    }
    public class ChromiumFindHandler : IFindHandler
    {
        public void OnFindResult(IWebBrowser chromiumWebBrowser, IBrowser browser, int identifier, int count, CefSharp.Structs.Rect selectionRect, int activeMatchOrdinal, bool finalUpdate)
        {
            WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.SetFindResult(activeMatchOrdinal, count);
        }
    }
    public class ChromiumDialogHandler : IDialogHandler
    {
        bool IDialogHandler.OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, IReadOnlyCollection<string> acceptFilters, IReadOnlyCollection<string> acceptExtensions, IReadOnlyCollection<string> acceptDescriptions, IFileDialogCallback callback)
        {
            return OnFileDialog(chromiumWebBrowser, browser, mode, title, defaultFilePath, acceptFilters, acceptExtensions, acceptDescriptions, callback);
        }
        protected virtual bool OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, IReadOnlyCollection<string> acceptFilters, IReadOnlyCollection<string> acceptExtensions, IReadOnlyCollection<string> acceptDescriptions, IFileDialogCallback callback)
        {
            if (mode == CefFileDialogMode.Open && bool.Parse(App.Instance.GlobalSave.Get("QuickImage")) && acceptFilters.FirstOrDefault() == "image/*")
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ImageTray Picker = new ImageTray();
                    Picker.FileFilters = acceptFilters;
                    Picker.FileExtensions = acceptExtensions;
                    Picker.FileDescriptions = acceptDescriptions;
                    if (Picker.ShowDialog() == true && !string.IsNullOrEmpty(Picker.SelectedFilePath))
                        callback.Continue(new List<string> { Picker.SelectedFilePath });
                    else
                        callback.Cancel();
                });
                return true;
            }
            return false;
        }
    }

    public class OverrideResourceRequestHandlerFactory : IResourceRequestHandlerFactory
    {
        ChromiumWebView WebView;
        public OverrideResourceRequestHandlerFactory(ChromiumWebView _Handler)
        {
            WebView = _Handler;
        }

        bool IResourceRequestHandlerFactory.HasHandlers
        {
            get { return WebViewManager.OverrideRequests.Count > 0; }
        }

        IResourceRequestHandler IResourceRequestHandlerFactory.GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }

        protected virtual IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            try
            {
                if (WebViewManager.OverrideRequests.TryGetValue(request.Url, out RequestOverrideItem Entry))
                {
                    if (Entry.Uses != -1)
                    {
                        Entry.Uses -= 1;
                        if (Entry.Uses == 0)
                            WebViewManager.OverrideRequests.TryRemove(request.Url, out Entry);
                    }
                    return new InMemoryResourceRequestHandler(Entry.Data, Entry.MimeType);
                }
                return new ChromiumResourceRequestHandler(WebView);
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}
