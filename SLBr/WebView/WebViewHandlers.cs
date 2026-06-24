/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using CefSharp.Internals;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Protocols;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SLBr.WebView
{
    public static class WebViewManager
    {
        public static CoreWebView2Environment WebView2Environment { get; set; }
        public static CoreWebView2ControllerOptions WebView2ControllerOptions { get; set; }
        public static CoreWebView2ControllerOptions WebView2PrivateControllerOptions { get; set; }
        public static CoreWebView2FindOptions WebView2FindOptions { get; set; }

        public static ChromiumLifeSpanHandler GlobalLifeSpanHandler { get; set; }
        public static ChromiumJsDialogHandler GlobalJsDialogHandler { get; set; }
        public static ChromiumKeyboardHandler GlobalKeyboardHandler { get; set; }
        public static ChromiumPermissionHandler GlobalPermissionHandler { get; set; }
        public static ChromiumDownloadHandler GlobalDownloadHandler { get; set; }
        public static ChromiumContextMenuHandler GlobalContextMenuHandler { get; set; }
        public static ChromiumFindHandler GlobalFindHandler { get; set; }
        public static ChromiumDialogHandler GlobalDialogHandler { get; set; }

        public static List<IWebView> WebViews = [];
        public static Dictionary<IWebBrowser, ChromiumWebView> ChromiumWebViews = [];

        public static WebViewSettings Settings { get; set; }
        public static WebViewRuntimeSettings RuntimeSettings { get; } = new();

        public static WebDownloadManager DownloadManager { get; } = new();

        public static bool IsWebView2Initialized { get; private set; } = false;
        public static bool IsCefInitialized { get; private set; } = false;
        public static bool IsTridentInitialized { get; private set; } = false;

        public static string WebView2Version { get; private set; } = "";

        public static async Task<IWebView> Create(WebEngineType EngineType, List<WebNavigationEntry> Urls, WebViewBrowserSettings _BrowserSettings)
        {
            switch (EngineType)
            {
                case WebEngineType.Chromium:
                    {
                        ChromiumWebView CView = new(Urls, _BrowserSettings);
                        await CView.InitializeAsync();
                        return CView;
                    }
                case WebEngineType.ChromiumEdge:
                    if (!IsWebView2Initialized)
                    {
                        try { WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(); }
                        catch (WebView2RuntimeNotFoundException)
                        {
                            ChromiumWebView CBView = new(Urls, _BrowserSettings);
                            await CBView.InitializeAsync();
                            return CBView;
                        }
                    }
                    ChromiumEdgeWebView EView = new(Urls, _BrowserSettings);
                    await EView.InitializeAsync();
                    return EView;
                case WebEngineType.Trident:
                    return new TridentWebView(Urls, _BrowserSettings);
                default:
                    {
                        ChromiumWebView CView = new(Urls, _BrowserSettings);
                        await CView.InitializeAsync();
                        return CView;
                    }
            }
        }
        /*public static string GetPreferencesString(string _String, string Parents, KeyValuePair<string, object> ObjectPair)
        {
            if (ObjectPair.Value is System.Dynamic.ExpandoObject _Expando)
            {
                foreach (KeyValuePair<string, object> Property in (IDictionary<string, object>)_Expando)
                    _String = $"{GetPreferencesString(_String, Parents + $"[{ObjectPair.Key}]", Property)}";
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

        private static Task<bool>? CEFInitializeTask;
        private static readonly Lock CEFInitializeLock = new();

        public static Task<bool> InitializeCEF()
        {
            lock (CEFInitializeLock)
            {
                if (CEFInitializeTask != null)
                    return CEFInitializeTask;

                CEFInitializeTask = InitializeCEFInternal();
                return CEFInitializeTask;
            }
        }

        private static async Task<bool> InitializeCEFInternal()
        {
            if (IsCefInitialized)
                return true;
            if (string.IsNullOrEmpty(SECRETS.GOOGLE_API_KEY))
            {
                InfoBar GoogleAPIKeyInfoBar = null;
                GoogleAPIKeyInfoBar = new()
                {
                    Title = "Missing API Keys",
                    Description = [new() { Text = "Google API keys are missing. Some functionality of Chromium (CEF) will be disabled." }],
                    Actions = [
                        new() { Text = "Learn more", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent), Foreground = App.Instance.CornflowerBlueColor, Command = new RelayCommand(() => {
                            App.Instance.CloseInfoBar(GoogleAPIKeyInfoBar);
                            App.Instance.CurrentFocusedWindow().NewTab("https://www.chromium.org/developers/how-tos/api-keys/", true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                        }) },
                    ]
                };
                App.Instance.InfoBars.Add(GoogleAPIKeyInfoBar);
            }
            CefSettings ChromiumSettings = new()
            {
                BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName,
                PersistSessionCookies = false,
                LogFile = Settings.LogFile,
                LogSeverity = LogSeverity.Error,
                Locale = Settings.Language,
                AcceptLanguageList = string.Join(",", Settings.Languages),
                JavascriptFlags = Settings.JavaScriptFlags,
                BackgroundColor = 0x000000
            };

            if (Settings.UserDataPath != null)
            {
                ChromiumSettings.CachePath = Path.GetFullPath(Path.Combine(Settings.UserDataPath, "Cache"));
                ChromiumSettings.RootCachePath = Settings.UserDataPath;
            }

            ChromiumSettings.CefCommandLineArgs.Remove("disable-back-forward-cache");
            //NOTE: Resolved in https://github.com/cefsharp/CefSharp/pull/5245
            //ChromiumSettings.AddNoErrorFlag("disable-features", "EnableHangWatcher,GlicActorUi,AutofillActorMode,LensOverlay");
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
                    IsSecure = true,
                    IsDisplayIsolated = true
                });
            }
            //await Task.Delay(20000);
            //TODO: Ineffective, application freezes.
            bool Success = await Cef.InitializeAsync(ChromiumSettings, false);

            if (!Success)
                return false;

            await Cef.UIThreadTaskFactory.StartNew(async delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                //GlobalRequestContext.SetPreference("extensions.ui.developer_mode", true, out _);
                GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !RuntimeSettings.PDFViewer, out _);
                GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !RuntimeSettings.PDFViewer, out _);

                //GlobalRequestContext.SetPreference("browser.theme.color_scheme", 0, out _);
                GlobalRequestContext.SetPreference("compact_mode", true, out _);
                GlobalRequestContext.SetPreference("history.saving_disabled", true, out _);
                GlobalRequestContext.SetPreference("profile.content_settings.enable_cpss.geolocation", false, out _);
                //GlobalRequestContext.SetPreference("accessibility.captions.live_caption_enabled", false, out _);

                GlobalRequestContext.SetPreference("autofill.enabled", false, out _);
                GlobalRequestContext.SetPreference("autofill.profile_enabled", false, out _);
                GlobalRequestContext.SetPreference("autofill.credit_card_enabled", false, out _);

                //TODO: Investigate the absence of "net.happy_eyeballs_v3_enabled" https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/pref_names.h;l=3033?q=HappyEyeballsV3
                /*string _Preferences = string.Empty;
                foreach (KeyValuePair<string, object> e in GlobalRequestContext.GetAllPreferences(true))
                    _Preferences = GetPreferencesString(_Preferences, string.Empty, e);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WriteLines.txt")))
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
                GlobalRequestContext.SetPreference("https_first_balanced_mode_enabled", false, out _);
                GlobalRequestContext.SetPreference("https_first_mode_incognito_enabled", false, out _);
                GlobalRequestContext.SetPreference("https_only_mode_auto_enabled", false, out _);
                GlobalRequestContext.SetPreference("https_only_mode_enabled", false, out _);
                GlobalRequestContext.SetPreference("net.network_prediction_options", Settings.Performance == PerformancePreset.High ? 0 : 2, out _);
                GlobalRequestContext.SetPreference("safebrowsing.enabled", false, out _);

                //GlobalRequestContext.SetPreference("profile.password_manager_enabled", false, out _);
                //GlobalRequestContext.SetPreference("credentials_enable_service", false, out _);

                GlobalRequestContext.SetPreference("browser.enable_spellchecking", RuntimeSettings.SpellCheck, out _);
                //GlobalRequestContext.SetPreference("spellcheck.use_spelling_service", false, out _);
                if (Settings.Languages?.Length != 0)
                {
                    GlobalRequestContext.SetPreference("spellcheck.dictionaries", string.Join(',', Settings.Languages), out _);
                    GlobalRequestContext.SetPreference("intl.accept_languages", string.Join(',', Settings.Languages), out _);
                }
                /*foreach (ContentSettingTypes SettingType in Enum.GetValues<ContentSettingTypes>())
                {
                    switch (SettingType)
                    {
                        case ContentSettingTypes.AutoSelectCertificate:
                        case ContentSettingTypes.DeprecatedPpapiBroker:
                        case ContentSettingTypes.SslCertDecisions:
                        case ContentSettingTypes.AppBanner:
                        case ContentSettingTypes.SiteEngagement:
                        case ContentSettingTypes.UsbChooserData:
                        case ContentSettingTypes.ImportantSiteInfo:
                        case ContentSettingTypes.PermissionAutoblockerData:
                        case ContentSettingTypes.AdsData:
                        case ContentSettingTypes.Midi:
                        case ContentSettingTypes.PasswordProtection:
                        case ContentSettingTypes.MediaEngagement:
                        case ContentSettingTypes.ClientHints:
                        case ContentSettingTypes.DeprecatedAccessibilityEvents:
                        case ContentSettingTypes.BackgroundFetch:
                        case ContentSettingTypes.IntentPickerDisplay:
                        case ContentSettingTypes.SerialChooserData:
                        case ContentSettingTypes.PeriodicBackgroundSync:
                        case ContentSettingTypes.HidChooserData:
                        case ContentSettingTypes.WakeLockScreen:
                        case ContentSettingTypes.WakeLockSystem:
                        case ContentSettingTypes.BluetoothChooserData:
                        case ContentSettingTypes.ClipboardSanitizedWrite:
                        case ContentSettingTypes.SafeBrowsingUrlCheckData:
                        case ContentSettingTypes.PermissionAutorevocationData:
                        case ContentSettingTypes.FileSystemLastPickedDirectory:
                        case ContentSettingTypes.DisplayCapture:
                        case ContentSettingTypes.FileSystemAccessChooserData:
                        case ContentSettingTypes.FederatedIdentitySharing:
                        case ContentSettingTypes.HttpAllowed:
                        case ContentSettingTypes.FormfillMetadata:
                        case ContentSettingTypes.DeprecatedFederatedIdentityActiveSession:
                        case ContentSettingTypes.AutoDarkWebContent:
                        case ContentSettingTypes.RequestDesktopSite:
                        case ContentSettingTypes.NotificationInteractions:
                        case ContentSettingTypes.ReducedAcceptLanguage:
                        case ContentSettingTypes.NotificationPermissionReview://
                        case ContentSettingTypes.PrivateNetworkChooserData://Works for some reason
                        case ContentSettingTypes.FederatedIdentityIdentityProviderSigninStatus:
                        case ContentSettingTypes.RevokedUnusedSitePermissions:
                        case ContentSettingTypes.FederatedIdentityIdentityProviderRegistration:
                        case ContentSettingTypes.HttpsEnforced:
                        case ContentSettingTypes.AllScreenCapture://
                        case ContentSettingTypes.CookieControlsMetadata://
                        case ContentSettingTypes.TpcdHeuristicsGrants://
                        case ContentSettingTypes.TpcdMetadataGrants://
                        case ContentSettingTypes.TpcdTrial://
                        case ContentSettingTypes.TopLevelTpcdTrial://
                        case ContentSettingTypes.TopLevelTpcdOriginTrial://
                        case ContentSettingTypes.SmartCardGuard:
                        case ContentSettingTypes.SmartCardData://
                        case ContentSettingTypes.WebPrinting:
                        case ContentSettingTypes.AutomaticFullscreen://
                        case ContentSettingTypes.SubAppInstallationPrompts://
                        case ContentSettingTypes.SpeakerSelection://
                        case ContentSettingTypes.RevokedAbusiveNotificationPermissions:
                        case ContentSettingTypes.TrackingProtection://
                        case ContentSettingTypes.DisplayMediaSystemAudio://
                        case ContentSettingTypes.StorageAccessHeaderOriginTrial://
                            continue;
                    }
                    ContentSettingValues Value = GlobalRequestContext.GetContentSetting("https://example.com", "https://example.com", SettingType);
                    Debug.WriteLine($"Setting: {SettingType}, Value: {Value}");
                }*/
                //enable_a_ping
                /*ContentSettingValues SettingValue = GlobalRequestContext.GetContentSetting("https://example.com", "https://example.com", ContentSettingTypes.JavaScript);
                if (SettingValue == ContentSettingValues.Allow)
                    Debug.WriteLine("JavaScript is allowed.");
                ICookieManager CookieManager = Cef.GetGlobalCookieManager();
                List<CefSharp.Cookie> CookiesList = await CookieManager.VisitAllCookiesAsync();
                foreach (var Cookie in CookiesList)
                    Debug.WriteLine($"Name: {Cookie.Name}, Value: {Cookie.Value}");*/
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
            return true;
        }

        private static Task<bool>? WebView2InitializeTask;
        private static readonly Lock WebView2InitializeLock = new();

        public static Task<bool> InitializeWebView2()
        {
            lock (WebView2InitializeLock)
            {
                if (WebView2InitializeTask != null)
                    return WebView2InitializeTask;

                WebView2InitializeTask = InitializeWebView2Internal();
                return WebView2InitializeTask;
            }
        }

        private static async Task<bool> InitializeWebView2Internal()
        {
            if (IsWebView2Initialized)
                return true;
            //https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags
            //msWebView2TreatAppSuspendAsDeviceSuspend
            List<CoreWebView2CustomSchemeRegistration> CustomSchemeRegistrations = [];
            foreach (var Scheme in Settings.Schemes.Where(i => i.Key != "*"))
                CustomSchemeRegistrations.Add(new(Scheme.Key) { HasAuthorityComponent = true, TreatAsSecure = true });
            CoreWebView2EnvironmentOptions EnvironmentOptions = new(Settings.BuildFlags(true), Settings.Language, null, false, CustomSchemeRegistrations);
            try { WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(null, EnvironmentOptions); }
            catch (WebView2RuntimeNotFoundException)
            {
                //MessageBox.Show("WebView2 Runtime is not installed. Please install it or disable WebView2.");
                return false;
            }

            EnvironmentOptions.AreBrowserExtensionsEnabled = true;
            EnvironmentOptions.IsCustomCrashReportingEnabled = true;
            EnvironmentOptions.ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay;

            WebView2Environment = await CoreWebView2Environment.CreateAsync(null, Settings.UserDataPath, EnvironmentOptions);

            WebView2ControllerOptions = WebView2Environment.CreateCoreWebView2ControllerOptions();
            if (Settings.UserDataPath == null)
                WebView2ControllerOptions.IsInPrivateModeEnabled = true;
            WebView2ControllerOptions.DefaultBackgroundColor = Color.Black;
            //WebView2ControllerOptions.ProfileName = "Default";

            //WARNING: Enabling AllowHostInputProcessing freezes WebView2 on drag & drop.
            //Related? https://github.com/MicrosoftEdge/WebView2Feedback/issues/5141
            //To no avail https://gist.github.com/ivanjx/b026ba331796e20a717778ae56760e3c, Disabling AllowExternalDrop may help, investigate?

            //Ignore above, keyboard hotkeys remain functional on disabled AllowHostInputProcessing.
            WebView2ControllerOptions.AllowHostInputProcessing = false;

            WebView2PrivateControllerOptions = WebView2Environment.CreateCoreWebView2ControllerOptions();
            WebView2PrivateControllerOptions.IsInPrivateModeEnabled = true;
            WebView2PrivateControllerOptions.DefaultBackgroundColor = Color.Black;
            WebView2PrivateControllerOptions.AllowHostInputProcessing = false;

            WebView2FindOptions = WebView2Environment.CreateFindOptions();
            WebView2FindOptions.ShouldHighlightAllMatches = true;
            WebView2FindOptions.ShouldMatchWord = false;
            WebView2FindOptions.SuppressDefaultFindDialog = true;
            IsWebView2Initialized = true;
            return true;
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

            SetIEFeatureControlKey("FEATURE_SPELLCHECKING", (uint)(RuntimeSettings.SpellCheck ? 1 : 0));
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

        public static async Task<ProtocolResponse> GopherHandler(string Url, string Extra = "", CancellationToken? Token = null)
        {
            try
            {
                GeminiGopherIResponse Response = await Gopher.Fetch(new Uri(Url), CancellationToken: Token ?? CancellationToken.None);
                if (Response.Mime.Contains("application/gopher-menu"))
                    return ProtocolResponse.FromString(TextGopher.NewFormat(Response), "text/html", Response.StatusCode, Response.ErrorCode);
                else
                    return ProtocolResponse.FromBytes(Response.Bytes.ToArray(), Response.Mime, Response.StatusCode, Response.ErrorCode);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception _Exception)
            {
                return ProtocolResponse.FromString($"<h1>Gopher Error</h1><pre>{_Exception.Message}</pre>", "text/html", 0, WebErrorCode.Failed);
            }
        }
        public static async Task<ProtocolResponse> GeminiHandler(string Url, string Extra = "", CancellationToken? Token = null)
        {
            try
            {
                GeminiGopherIResponse Response = await Gemini.Fetch(new Uri(Url), CancellationToken: Token ?? CancellationToken.None);
                if (Response.Mime.Contains("text/gemini"))
                    return ProtocolResponse.FromString(TextGemini.NewFormat(Response), "text/html", Response.StatusCode, Response.ErrorCode);
                else
                    return ProtocolResponse.FromBytes(Response.Bytes.ToArray(), Response.Mime, Response.StatusCode, Response.ErrorCode);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception _Exception)
            {
                return ProtocolResponse.FromString($"<h1>Gemini Error</h1><pre>{_Exception.Message}</pre>", "text/html", 0, WebErrorCode.Failed);
            }
            /*if (Response.SSLStatus.X509Certificate != null)
            {
                MessageBox.Show(Response.SSLStatus.X509Certificate.Subject);
                MessageBox.Show(Response.SSLStatus.X509Certificate.Issuer);
                MessageBox.Show(Response.SSLStatus.X509Certificate.NotBefore.Date.ToShortDateString());
                MessageBox.Show(Response.SSLStatus.X509Certificate.NotAfter.Date.ToShortDateString());
            }*/
        }
        private static Lazy<string[]> SLBrURLs = new(() => ["credits", "newtab", "downloads", "history", "settings", "tetris", "favourites"]);
        public static async Task<ProtocolResponse> SLBrHandler(string Url, string Extra = "", CancellationToken? Token = null)
        {
            try
            {
                string Host = Utils.FastHost(Url);
                if (SLBrURLs.Value.Contains(Host))
                {
                    string Page = Url[(7 + Host.Length)..].TrimStart('/');
                    string FileName = string.IsNullOrWhiteSpace(Page) ? $"{Host}.html" : Page;
                    if (string.IsNullOrWhiteSpace(Page) && Host == "newtab")
                    {
                        if (App.Instance.ReadOnlyInstance)
                            FileName = "guest.html";
                        else if (Extra == "1")
                            FileName = "private.html";
                    }
                    string FilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", FileName);
                    if (File.Exists(FilePath))
                        return ProtocolResponse.FromBytes(File.ReadAllBytes(FilePath), Cef.GetMimeType(Path.GetExtension(FilePath)), 200);
                    else if (App.CustomPageOverlays.TryGetValue(Host, out _))
                        return ProtocolResponse.FromString(string.Format(App.OverlayPagePlaceholder, Host.ToTitleCase()), "text/html", 200);
                }
                return ProtocolResponse.FromString($"<h1>404 Not Found</h1>", "text/html", 404);
            }
            catch (Exception ex)
            {
                return ProtocolResponse.FromString($"<h1>Error</h1><pre>{WebUtility.HtmlEncode(ex.Message)}</pre>", "text/html", 0, WebErrorCode.Failed);
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

        public static bool RegisterOverrideRequest(string Url, byte[] Data, string MimeType = ResourceHandler.DefaultMimeType/*, bool limitedUse = false*/, int Uses = 1, int Error = -1)
        {
            if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? URI))
            {
                RequestOverrideItem Entry = new(Data, MimeType, Uses, Error);
                OverrideRequests.AddOrUpdate(URI.AbsoluteUri, Entry, (k, v) => Entry);
                return true;
            }
            return false;
        }

        public static bool UnregisterOverrideRequest(string Url) => OverrideRequests.TryRemove(Url, out _);
        public static ConcurrentDictionary<string, RequestOverrideItem> OverrideRequests = new(StringComparer.OrdinalIgnoreCase);
    }

    public class RequestOverrideItem(byte[] _Data, string _MimeType, int _Uses = 1, int _Error = -1)
    {
        public byte[] Data = _Data;
        public string MimeType = _MimeType;
        public int Error = _Error;
        public int Uses = _Uses;
    }

    //https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/general-info/ee330720(v=vs.85)
    public enum TridentEmulationVersion: uint
    {
        IE7 = 7000,
        IE8 = 8888,
        IE9 = 9999,
        IE10 = 10001,
        IE11 = 11001,
        Edge = 12001
    }
    public enum PerformancePreset
    {
        Low,
        Default,
        High
    }

    public class WebViewSettings
    {
        public string Language;
        public string[] Languages = [];

        public string? UserDataPath = null;
        public string LogFile;

        public PerformancePreset Performance = PerformancePreset.Default;
        public TridentEmulationVersion TridentVersion = TridentEmulationVersion.IE11;
        public CefRuntimeStyle CefRuntimeStyle = CefRuntimeStyle.Default;

        public string JavaScriptFlags = string.Empty;
        public Dictionary<string, string> Flags = [];
        //https://www.chromium.org/developers/how-tos/run-chromium-with-flags/
        public string BuildFlags(bool IncludeJavaScript = false)
        {
            StringBuilder _StringBuilder = new();
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
        public readonly Dictionary<string, ProtocolHandler> Schemes = [];
        public void RegisterProtocol(string Scheme, ProtocolHandler Handler) => Schemes[Scheme] = Handler;

        public void AddFlag(string Key, string Value) => Flags.Add(Key, Value);

        public void AddFlag(string Value) => Flags.Add(Value, string.Empty);

        public bool GPUAcceleration = true;
        /*public bool PrintPreview = true;*/
    }
    public class WebViewRuntimeSettings
    {
        public bool SpellCheck = true;

        //TODO: WebView2 Profile.DefaultDownloadFolderPath = DownloadFolderPath;
        public string DownloadFolderPath = string.Empty;
        public bool DownloadPrompt = true;
        private bool _PDFViewer = true;
        public bool PDFViewer
        {
            get => _PDFViewer;
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
        public void Updated(WebDownloadItem Item)
        {
            if (!Item.EndTime.HasValue && Item.State == WebDownloadState.InProgress && !App.Instance.LiteMode)
            {
                if (Item.TotalBytes <= 0 || Item.ReceivedBytes >= Item.TotalBytes)
                    Item.CalculatedEndTime = DateTime.Now;
                else
                {
                    DateTime Now = DateTime.Now;
                    double ElapsedSeconds = (Now - Item.LastCheckTime).TotalSeconds;
                    if (ElapsedSeconds > 0)
                    {
                        double BytesPerSecond = (Item.ReceivedBytes - Item.LastReceivedBytes) / ElapsedSeconds;
                        Item.LastCheckTime = Now;
                        Item.LastReceivedBytes = Item.ReceivedBytes;
                        if (BytesPerSecond > 0)
                            Item.CalculatedEndTime = DateTime.Now.AddSeconds((Item.TotalBytes - Item.ReceivedBytes) / BytesPerSecond);
                    }
                }
            }
            DownloadUpdated?.RaiseUIAsync(Item);
        }
        public void Completed(WebDownloadItem Item) => DownloadCompleted?.RaiseUIAsync(Item);

        public void RemoveFileStaging(WebDownloadItem Item)
        {
            if (File.Exists(Item.TempPath))
            {
                switch (Item.State)
                {
                    case WebDownloadState.Canceled:
                        try { File.Delete(Item.TempPath); } catch { }
                        break;
                    case WebDownloadState.Completed:
                        if (Item.TempPath != Item.FullPath)
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(Item.FullPath));
                                File.Move(Item.TempPath, Item.FullPath, true);
                            }
                            catch { }
                        }
                        break;
                }
            }
        }

        private static Lazy<HttpClient> DownloadHttpClient = new(() => HttpClientFactory.Create(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            ConnectTimeout = TimeSpan.FromSeconds(30)
        }));

        public async Task StartDownloadAsync(string Url, string TargetPath, bool ShowDialog, string? DialogFilter = null)
        {
            if (ShowDialog)
            {
                SaveFileDialog SaveDialog = new()
                {
                    FileName = Path.GetFileName(Url),
                    InitialDirectory = WebViewManager.RuntimeSettings.DownloadFolderPath,
                    //Guide on proper file dialog wild cards https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/dd459587(v=vs.95)
                    Filter = string.IsNullOrEmpty(DialogFilter) ? "All Files (*.*)|*.*" : DialogFilter
                };
                if (SaveDialog.ShowDialog() == true)
                    TargetPath = SaveDialog.FileName;
                else
                    return;
            }
            else if (!string.IsNullOrEmpty(TargetPath))
            {
                if (Directory.Exists(TargetPath) || !Path.HasExtension(TargetPath))
                    TargetPath = Path.Combine(TargetPath, Path.GetFileName(Url));
            }
            WebDownloadItem Item = new()
            {
                ID = Guid.NewGuid().ToString(),
                Url = Url,
                FileName = Path.GetFileName(TargetPath),
                FullPath = TargetPath,
                State = WebDownloadState.InProgress
            };

            Started(Item);

            try
            {
                //TODO: Implement pause & resume functionality.
                using HttpResponseMessage Response = await DownloadHttpClient.Value.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);

                Response.EnsureSuccessStatusCode();
                Item.TotalBytes = Response.Content.Headers.ContentLength ?? -1;

                await using FileStream _FileStream = new(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var Stream = await Response.Content.ReadAsStreamAsync();

                var Buffer = new byte[8192];
                int Read;
                while ((Read = await Stream.ReadAsync(Buffer)) > 0)
                {
                    await _FileStream.WriteAsync(Buffer.AsMemory(0, Read));
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

        public void WriteDownload(byte[] Data, string? TargetPath, bool ShowDialog, string? DialogFilter = null)
        {
            if (ShowDialog)
            {
                SaveFileDialog SaveDialog = new()
                {
                    FileName = Path.GetFileName(TargetPath),
                    InitialDirectory = Path.GetDirectoryName(TargetPath),
                    //Guide on proper file dialog wild cards https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/dd459587(v=vs.95)
                    Filter = string.IsNullOrEmpty(DialogFilter) ? "All Files (*.*)|*.*" : DialogFilter
                };
                if (SaveDialog.ShowDialog() == true)
                    TargetPath = SaveDialog.FileName;
                else
                    return;
            }
            WebDownloadItem Item = new()
            {
                ID = Guid.NewGuid().ToString(),
                Url = string.Empty,
                FileName = Path.GetFileName(TargetPath),
                FullPath = TargetPath,
                State = WebDownloadState.InProgress,
                TotalBytes = Data.Length,
                ReceivedBytes = Data.Length
            };
            Started(Item);
            if (Data != null)
            {
                try
                {
                    string? _Directory = Path.GetDirectoryName(TargetPath);
                    if (!Directory.Exists(_Directory))
                        Directory.CreateDirectory(_Directory);
                    File.WriteAllBytes(TargetPath, Data);
                    Item.State = WebDownloadState.Completed;
                }
                catch
                {
                    //TODO: Interrupt reason.
                    Item.State = WebDownloadState.Canceled;
                }
            }
            Completed(Item);
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
                    NewTabRequestEventArgs Args = new(targetUrl, targetDisposition == WindowOpenDisposition.NewBackgroundTab, targetDisposition == WindowOpenDisposition.NewPopup ? new Rect(popupFeatures.X ?? 0, popupFeatures.Y ?? 0, popupFeatures.Width ?? 0, popupFeatures.Height ?? 0) : null);
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
            ScriptDialogEventArgs Args = new(ScriptDialogType.BeforeUnload, Address, messageText, string.Empty, isReload);
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
            ScriptDialogEventArgs Args = new((ScriptDialogType)DialogType, originUrl, messageText, defaultPromptText);
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
                    if (Window is InformationDialogWindow || Window is DynamicDialogWindow || Window is CredentialsDialogWindow)
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
                HotKey? Key = HotKeyManager.HotKeys.FirstOrDefault(i => i.KeyCode == WPFKeyCode && i.Control == HasControl && i.Shift == HasShift && i.Alt == HasAlt);
                if (Key != null)
                    Application.Current?.Dispatcher.BeginInvoke(() => Key.Callback());
            }
            return false;
        }
    }

    public class ChromiumResourceRequestHandler(ChromiumWebView _WebView) : IResourceRequestHandler
    {
        private ChromiumWebView WebView = _WebView;

        private WebResourceResponse? Response;
        private bool Intercept;

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            Dictionary<string, string> Headers = [with(StringComparer.OrdinalIgnoreCase)];
            foreach (string? Key in request.Headers.AllKeys)
                Headers[Key] = request.Headers[Key];
            ResourceRequestEventArgs Args = new(
                request.Url,
                browser.FocusedFrame?.Url ?? string.Empty,
                request.Method,
                request.ResourceType.ToResourceRequestType(),
                Headers
            );
            WebView.RaiseResourceRequest(Args);
            if (Args.Cancel) return CefReturnValue.Cancel;
            if (Args.ModifiedHeaders != null && Args.ModifiedHeaders.Count != 0)
            {
                foreach (var Header in Args.ModifiedHeaders)
                    request.SetHeaderByName(Header.Key, Header.Value, true);
            }
            Intercept = Args.Intercept;
            if (Args.Response != null)
                Response = Args.Response;
            return CefReturnValue.Continue;
        }

        public IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            if (Response != null)
            {
                if (Response.Content.CanSeek)
                    Response.Content.Position = 0;
                ResourceHandler ResourceOverride = ResourceHandler.FromStream(Response.Content, Response.MimeType, true);
                if (Response.StatusCode.HasValue)
                    ResourceOverride.StatusCode = Response.StatusCode.Value;
                if (Response.Headers.IsValueCreated && Response.Headers.Value.Count != 0)
                {
                    foreach (var Header in Response.Headers.Value)
                        ResourceOverride.Headers[Header.Key] = Header.Value;
                }
                return ResourceOverride;
            }
            return null;
        }
        public ICookieAccessFilter GetCookieAccessFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request) => null;
        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl) { }
        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            //WebView.RaiseResourceResponded(new ResourceRespondedResult(request.Url, request.ResourceType.ToResourceRequestType()));
            return false;
        }
        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            if (Intercept)
            {
                string Url = request.Url;
                ResourceRequestType Type = request.ResourceType.ToResourceRequestType();
                int Code = response.StatusCode;
                return new CefResponseInterceptorFilter((Bytes, Length) =>
                {
                    Task.Run(async () =>
                    {
                        MemoryStream _Stream = new(Bytes, 0, Length, false);
                        ResponseInterceptedResult InterceptedResult = new(Url, Type, Code, async (Action) => {
                            using (_Stream)
                            {
                                await Action(_Stream);
                            }
                        });
                        WebView.RaiseResponseIntercepted(InterceptedResult);
                    });
                });
            }
            return null;
        }
        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            WebView.RaiseResourceLoaded(new ResourceLoadedResult(request.Url, status != UrlRequestStatus.Failed && status != UrlRequestStatus.Canceled, receivedContentLength, request.ResourceType.ToResourceRequestType()));
        }
        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            ExternalProtocolEventArgs Args = new(request.Url, frame.Url);
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
            GC.SuppressFinalize(this);
        }
    }

    public class CefResponseInterceptorFilter : IResponseFilter
    {
        private readonly MemoryStream ShadowBuffer = new(32768);
        private readonly Action<byte[], int> OnComplete;

        public CefResponseInterceptorFilter(Action<byte[], int> _OnComplete)
        {
            OnComplete = _OnComplete;
        }

        public bool InitFilter() => true;

        public FilterStatus Filter(Stream? dataIn, out long dataInRead, Stream? dataOut, out long dataOutWritten)
        {
            dataInRead = 0;
            dataOutWritten = 0;
            if (dataIn == null || dataOut == null)
            {
                CompleteAndExtractBytes();
                return FilterStatus.Done;
            }
            int Length = (int)dataIn.Length;
            if (Length == 0)
            {
                CompleteAndExtractBytes();
                return FilterStatus.Done;
            }
            byte[] Buffer = ArrayPool<byte>.Shared.Rent(Length);
            try
            {
                dataInRead = dataIn.Read(Buffer, 0, Length);
                if (dataInRead > 0)
                {
                    dataOut.Write(Buffer, 0, (int)dataInRead);
                    dataOutWritten = dataInRead;
                    ShadowBuffer.Write(Buffer, 0, (int)dataInRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
            if (dataIn.Position >= Length)
            {
                CompleteAndExtractBytes();
                return FilterStatus.Done;
            }
            return FilterStatus.NeedMoreData;
        }

        private void CompleteAndExtractBytes()
        {
            int Length = (int)ShadowBuffer.Length;
            if (Length > 0)
            {
                OnComplete(ShadowBuffer.GetBuffer(), Length);
                ShadowBuffer.SetLength(0);
            }
        }

        public void Dispose() => ShadowBuffer.Dispose();
    }

    public class ChromiumPermissionHandler : IPermissionHandler
    {
        public void OnDismissPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, PermissionRequestResult result) { }

        public bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
        {
            if (callback == null)
                return false;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }

                PermissionRequestedEventArgs Args = new(requestingOrigin, requestedPermissions.ToWebPermission());
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
                PermissionRequestedEventArgs Args = new(requestingOrigin, _ProperPermissionRequestType.ToWebPermission());
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
                else if (Args.State == WebPermissionState.Ask)
                    callback.Continue(PermissionRequestResult.Ignore);
                callback.Dispose();
            }));
            return true;
        }
    }
    public class ChromiumDownloadHandler : IDownloadHandler
    {
        private Dictionary<int, WebDownloadItem> WebDownloadItems = [];
        private Dictionary<int, IDownloadItemCallback> DownloadCallbacks = [];

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod) => true;

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (callback.IsDisposed) return false;
            using (callback)
            {
                string PreferredPath = Path.Combine(WebViewManager.RuntimeSettings.DownloadFolderPath, downloadItem.SuggestedFileName);
                if (WebViewManager.RuntimeSettings.DownloadPrompt)
                {
                    SaveFileDialog SaveDialog = new()
                    {
                        FileName = downloadItem.SuggestedFileName,
                        InitialDirectory = WebViewManager.RuntimeSettings.DownloadFolderPath,
                        Filter = "All Files (*.*)|*.*"
                    };
                    if (SaveDialog.ShowDialog() == true)
                        PreferredPath = SaveDialog.FileName;
                    else
                        return false;
                }
                string TempPath;
                if (Path.GetExtension(PreferredPath) == ".crx")
                    TempPath = PreferredPath;
                else
                    TempPath = PreferredPath + ".part";

                WebDownloadItem Item = new()
                {
                    ID = downloadItem.Id.ToString(),
                    Url = downloadItem.Url,
                    FileName = Path.GetFileName(PreferredPath),
                    FullPath = PreferredPath,
                    TempPath = TempPath,
                    TotalBytes = downloadItem.TotalBytes,
                    State = WebDownloadState.InProgress
                };

                WebDownloadItems[downloadItem.Id] = Item;
                WebViewManager.DownloadManager.Started(Item);
                callback.Continue(TempPath, false);
            }
            /*using (callback)
                callback.Continue(Path.Combine(WebViewManager.Settings.DownloadFolderPath, downloadItem.SuggestedFileName), WebViewManager.Settings.DownloadPrompt);*/
            return true;
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            if (!WebDownloadItems.TryGetValue(downloadItem.Id, out var Item))
                return;
            //Item.Url = downloadItem.Url;
            Item.TotalBytes = downloadItem.TotalBytes;
            Item.ReceivedBytes = downloadItem.ReceivedBytes;
            //TODO: Null provided.
            Item.EndTime = downloadItem.EndTime;

            if (downloadItem.IsInProgress)
            {
                DownloadCallbacks[downloadItem.Id] = callback;
                Item.Pause = () => { DownloadCallbacks[downloadItem.Id].Pause(); };
                Item.Resume = () => { DownloadCallbacks[downloadItem.Id].Resume(); };
                Item.Cancel = () => { DownloadCallbacks[downloadItem.Id].Cancel(); };
                Item.State = WebDownloadState.InProgress;
                WebViewManager.DownloadManager.Updated(Item);
            }
            //TODO: Submit pull request for downloadItem.IsInterrupted & GetInterruptReason.
            //https://cef-builds.spotifycdn.com/docs/148.0/classCefDownloadItem.html

            else if (downloadItem.IsPaused)
            {
                Item.State = WebDownloadState.Paused;
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
            //Both CefSharp & WebView2 suffer from the same spellcheck issue.
            if (parameters.FrameUrl.StartsWith("devtools:"))
                return;
            model.Clear();
            WebViewManager.ChromiumWebViews[chromiumWebBrowser]?.RaiseContextMenu(new WebContextMenuEventArgs
            {
                X = parameters.XCoord,
                Y = parameters.YCoord,
                //TODO: Investigate CefSharp copy link text
                //LinkText = parameters.SelectionText,
                LinkUrl = parameters.LinkUrl,
                FrameUrl = parameters.FrameUrl,
                SelectionText = parameters.SelectionText,
                IsEditable = parameters.IsEditable,
                DictionarySuggestions = parameters.DictionarySuggestions,
                //MisspelledWord = parameters.MisspelledWord,
                SourceUrl = parameters.SourceUrl,
                //SpellCheck = parameters.IsSpellCheckEnabled,
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
            ChromiumWebView _ChromiumWebView = WebViewManager.ChromiumWebViews.FirstOrDefault(i => i.Key.BrowserCore != null && i.Key.BrowserCore.IsSame(browser)).Value;
            //WebViewManager.ChromiumWebViews.TryGetValue(browser, out ChromiumWebView _ChromiumWebView);
            return new ChromiumResourceHandler(Handler, request.Url, _ChromiumWebView?.Settings.Private.ToInt().ToString() ?? string.Empty, _ChromiumWebView);
        }
    }
    public class ChromiumResourceHandler : ResourceHandler
    {
        private readonly ChromiumWebView? WebView;
        private readonly ProtocolHandler Handler;
        private readonly string Url;
        private readonly string Extra;
        private CancellationTokenSource TokenSource;

        public ChromiumResourceHandler(ProtocolHandler _Handler, string _Url, string _Extra, ChromiumWebView? _WebView = null)
        {
            WebView = _WebView;
            Handler = _Handler;
            Url = _Url;
            Extra = _Extra;
        }

        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            TokenSource = new CancellationTokenSource();
            _ = ExecuteRequest(callback, TokenSource.Token);
            return CefReturnValue.ContinueAsync;
        }

        private async Task ExecuteRequest(ICallback Callback, CancellationToken Token)
        {
            try
            {
                var Response = await Handler(Url, Extra, Token).ConfigureAwait(false);

                if (Token.IsCancellationRequested || Callback.IsDisposed)
                    return;

                Stream = new MemoryStream(Response.Data, false);
                MimeType = Response.MimeType;
                StatusCode = Response.StatusCode;
                StatusText = "OK";

                if (!Callback.IsDisposed)
                    Callback.Continue();

                if (Response.ErrorCode != WebErrorCode.None)
                    WebView?.RaiseNavigationError(new NavigationErrorEventArgs(Response.ErrorCode, string.Empty, Url));
            }
            catch (OperationCanceledException) { }
            catch (Exception Exception)
            {
                Stream = new MemoryStream(Encoding.UTF8.GetBytes($"<h1>Protocol error</h1><pre>{WebUtility.HtmlEncode(Exception.Message)}</pre>"), false);
                MimeType = "text/html";
                StatusCode = 500;
                StatusText = "Error";
                if (!Callback.IsDisposed)
                    Callback.Continue();
            }
        }

        public override void Dispose()
        {
            TokenSource?.Cancel();
            TokenSource?.Dispose();
            TokenSource = null;
            base.Dispose();
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
                    ImageTray Picker = new()
                    {
                        FileFilters = acceptFilters,
                        FileExtensions = acceptExtensions,
                        FileDescriptions = acceptDescriptions
                    };
                    if (Picker.ShowDialog() == true && !string.IsNullOrEmpty(Picker.SelectedFilePath))
                        callback.Continue([Picker.SelectedFilePath]);
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

        bool IResourceRequestHandlerFactory.HasHandlers => !WebViewManager.OverrideRequests.IsEmpty;

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
