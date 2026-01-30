using CefSharp;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SLBr.Controls;
using SLBr.Handlers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement.Core;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl, IDisposable
    {
        public BrowserTabItem Tab;

        public IWebView WebView;
        public IPageOverlay? PageOverlay;
        public UserControl? PageOverlayControl
        {
            get => PageOverlay as UserControl;
        }

        public bool Private = false;
        public bool UserAgentBranding = true;

        public ObservableCollection<InfoBar> LocalInfoBars = [];
        public ObservableCollection<InfoBar> VisibleInfoBars = [];

        Storyboard LoadingStoryboard;

        public Browser(string Url, BrowserTabItem _Tab = null, bool IsPrivate = false)
        {
            InitializeComponent();
            Tab = _Tab ?? Tab.ParentWindow.GetTab(this);
            Private = IsPrivate;
            SetIcon(App.Instance.GetIcon(bool.Parse(App.Instance.GlobalSave.Get("Favicons")) ? Url : string.Empty, Private));
            Address = Url;
            SetAudioState(false);
            DownloadsPopup.ItemsSource = App.Instance.VisibleDownloads;
            FavouritesPanel.ItemsSource = App.Instance.Favourites;
            FavouriteListMenu.Collection = App.Instance.Favourites;
            HistoryListMenu.Collection = App.Instance.History;
            ExtensionsMenu.ItemsSource = App.Instance.Extensions;//ObservableCollection wasn't working so turned it into a list
            InfoBarList.ItemsSource = VisibleInfoBars;
            SetAppearance(App.Instance.CurrentTheme);

            if (!Private)
            {
                OmniBoxFastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                OmniBoxSmartTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                OmniBoxFastTimer.Tick += OmniBoxFastTimer_Tick;
                OmniBoxSmartTimer.Tick += OmniBoxSmartTimer_Tick;
            }
            LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
            SiteInformationText.Text = "Loading";
            LoadingStoryboard?.Begin();
            TranslateComboBox.ItemsSource = App.Instance.LocaleNames;
            TranslateComboBox.SelectedValue = App.Instance.Locale.Name;
            LocalInfoBars.CollectionChanged += LocalInfoBars_CollectionChanged;
            SyncInfobars();
            InitializeBrowserComponent();
        }

        private void LocalInfoBars_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SyncInfobars();
        }

        public void SyncInfobars()
        {
            VisibleInfoBars.Clear();
            foreach (InfoBar Bar in App.Instance.InfoBars)
                VisibleInfoBars.Add(Bar);
            foreach (InfoBar Bar in LocalInfoBars)
                VisibleInfoBars.Add(Bar);
        }

        public void InitializeBrowserComponent()
        {
            if (WebView == null)
                CreateWebView(Address, (WebEngineType)App.Instance.GlobalSave.GetInt("WebEngine"));
            else
                BrowserLoadChanged(Address);
        }

        TextBox OmniTextBox;
        Popup OmniBoxPopup;
        Grid OmniBoxPopupDropDown;
        bool AudioPlaying = false;

        private void WebView_AudioPlayingChanged(object? sender, EventArgs e)
        {
            SetAudioState(WebView.AudioPlaying);
        }

        public void SetAudioState(bool? _AudioPlaying = false)
        {
            if (_AudioPlaying != null)
                AudioPlaying = _AudioPlaying.Value;
            if (WebIcon != null)
                SetIcon(WebIcon);
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                Tab.ProgressBarVisibility = (Muted || !AudioPlaying) ? Visibility.Visible : Visibility.Collapsed;
            else
                Tab.ProgressBarVisibility = Visibility.Collapsed;
        }

        public void Favourites_CollectionChanged()
        {
            SetFavouritesBarVisibility();
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = App.Instance.FavouriteColor;
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
        }

        public void UpdateSLBr(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowUpdateInfoBar();
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
            Action((Actions)int.Parse(Values[0]), (Values.Length > 1) ? Values[1] : string.Empty, (Values.Length > 2) ? Values[2] : string.Empty, (Values.Length > 3) ? Values[3] : string.Empty);
        }
        public void Action(Actions _Action, string V1 = "", string V2 = "", string V3 = "")
        {
            V1 = V1.Replace("{CurrentUrl}", Address).Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));

            switch (_Action)
            {
                case Actions.Exit:
                    App.Instance.CloseSLBr(true);
                    break;
                case Actions.Undo:
                    Back();
                    break;
                case Actions.Redo:
                    Forward();
                    break;
                case Actions.Refresh:
                    Refresh();
                    break;
                case Actions.HardRefresh:
                    Refresh(true);
                    break;
                case Actions.ClearCacheHardRefresh:
                    Refresh(true, true);
                    break;
                case Actions.Navigate:
                    Navigate(V1);
                    break;
                case Actions.Share:
                    Share();
                    break;

                case Actions.CreateTab:
                    if (V2 == "Tab")
                    {
                        BrowserTabItem _Tab = Tab.ParentWindow.GetBrowserTabWithId(int.Parse(V1));
                        Tab.ParentWindow.NewTab(_Tab.Content.Address, true, Tab.ParentWindow.Tabs.IndexOf(_Tab) + 1, Private, _Tab.TabGroup);
                    }
                    else if (V2 == "Private")
                        Tab.ParentWindow.NewTab(V1, true, -1, true);
                    else
                        Tab.ParentWindow.NewTab(V1, true);
                    break;
                case Actions.CloseTab:
                    Tab.ParentWindow.CloseTab(int.Parse(V1), int.Parse(V2));
                    break;
                case Actions.NewWindow:
                    App.Instance.NewWindow();
                    break;

                case Actions.DevTools:
                    DevTools();
                    break;
                case Actions.SizeEmulator:
                    SizeEmulator();
                    break;
                case Actions.SetSideBarDock:
                    SetSideBarDock(int.Parse(V1));
                    break;
                case Actions.Favourite:
                    FavouriteAction();
                    break;
                case Actions.OpenFileExplorer:
                    Utils.OpenFileExplorer(V1);
                    break;
                case Actions.OpenAsPopupBrowser:
                    OpenAsPopupBrowser(V1);
                    break;
                case Actions.SwitchUserPopup:
                    App.Instance.SwitchUserPopup();
                    break;
                case Actions.ReaderMode:
                    ToggleReaderMode();
                    break;

                case Actions.CloseSideBar:
                    ToggleSideBar(true);
                    break;
                case Actions.NewsFeed:
                    NewsFeed();
                    break;

                case Actions.Print:
                    WebView?.Print();
                    break;
                case Actions.Mute:
                    ToggleMute();
                    break;
                case Actions.Find:
                    Find(string.Empty);
                    break;

                /*case Actions.ZoomIn:
                    Zoom(1);
                    break;
                case Actions.ZoomOut:
                    Zoom(-1);
                    break;
                case Actions.ZoomReset:
                    Zoom(0);
                    break;*/
                case Actions.InstallWebApp:
                    Dispatcher.Invoke(async () =>
                    {
                        if (App.Instance.AvailableWebAppManifests.ContainsKey(CurrentWebAppManifestUrl))
                            CurrentWebAppManifest = App.Instance.AvailableWebAppManifests.GetValueOrDefault(CurrentWebAppManifestUrl);
                        else
                        {
                            CurrentWebAppManifest = await WebAppHandler.FetchManifestAsync(Address, CurrentWebAppManifestUrl);
                            if (CurrentWebAppManifest != null)
                                App.Instance.AvailableWebAppManifests.Add(CurrentWebAppManifestUrl, CurrentWebAppManifest);
                        }
                        if (CurrentWebAppManifest != null)
                        {
                            InformationDialogWindow InfoWindow = new("Information", $"Install {CurrentWebAppManifest.ShortName ?? CurrentWebAppManifest.Name}", "This site can be installed as an application.", "\ueb3b", "Install", "Cancel");
                            InfoWindow.Topmost = true;
                            if (InfoWindow.ShowDialog() == true)
                                await WebAppHandler.Install(CurrentWebAppManifest);
                        }
                    });
                    break;
                case Actions.QR:
                    if (V1 == "0")
                    {
                        QRBitmap ??= new QRSaveBitmapImage(App.MiniQREncoder.Encode(Address)) { ModuleSize = 5, QuietZone = 10 }.CreateQRCodeBitmap();
                        QRImage.Source = QRBitmap;
                        QRButton.OpenPopup();
                    }
                    else if (V1 == "1")
                    {
                        if (QRBitmap != null)
                            Clipboard.SetImage(QRBitmap);
                    }
                    else
                    {
                        if (QRBitmap != null)
                        {
                            try
                            {
                                string ScreenshotPath = App.Instance.GlobalSave.Get("ScreenshotPath");
                                if (!Directory.Exists(ScreenshotPath))
                                    Directory.CreateDirectory(ScreenshotPath);
                                string Url = Path.Combine(ScreenshotPath, Utils.SanitizeFileName(Address) + ".png");
                                Utils.SaveImage(QRBitmap, Url);
                                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
                            }
                            catch { }
                        }
                    }
                    break;
                case Actions.SwitchWebEngine:
                    switch (V1)
                    {
                        case "0":
                            if (WebView?.Engine == WebEngineType.Chromium)
                            {
                                InformationDialogWindow InfoWindow = new("Information", "Already using Chromium web engine", "This tab is already running with the Chromium web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.Chromium);
                            break;
                        case "1":
                            if (WebView?.Engine == WebEngineType.ChromiumEdge)
                            {
                                InformationDialogWindow InfoWindow = new("Information", "Already using Edge web engine", "This tab is already running with the Edge web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            string? AvailableVersion = null;
                            try
                            {
                                AvailableVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                            }
                            catch (WebView2RuntimeNotFoundException)
                            {
                                InformationDialogWindow InfoWindow = new("Error", "WebView2 Runtime Unavailable", "Microsoft Edge WebView2 Runtime is not installed on your device.", "\ue7f9", "Download", "Cancel");
                                InfoWindow.Topmost = true;
                                if (InfoWindow.ShowDialog() == true)
                                    Tab.ParentWindow.NewTab("https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/", true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.ChromiumEdge);
                            break;
                        case "2":
                            if (WebView?.Engine == WebEngineType.Trident)
                            {
                                InformationDialogWindow InfoWindow = new("Information", "Already using Trident web engine", "This tab is already running with the Trident web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.Trident);
                            break;
                        case "-1":
                            WebEngineType Engine = (WebEngineType)App.Instance.GlobalSave.GetInt("WebEngine");
                            if (WebView?.Engine == Engine)
                                break;
                            DisposeBrowserCore();
                            CreateWebView(Address, Engine);
                            break;
                    }
                    break;
                case Actions.Translate:
                    Translate(V1 == "1");
                    break;
                case Actions.CreateGroup:
                    Tab.ParentWindow.CreateGroup();
                    break;
            }
        }
        public WriteableBitmap? QRBitmap = null;

        void CreateWebView(string Url, WebEngineType Engine)
        {
            if (WebView != null)
                return;
            Address = Url;
            Tab.IsUnloaded = true;
            Tab.ProgressBarVisibility = Visibility.Collapsed;
            switch (Engine)
            {
                case WebEngineType.Chromium:
                    WebEngineButtonIcon.Text = "\x2600";
                    break;
                case WebEngineType.ChromiumEdge:
                    WebEngineButtonIcon.Text = "\x2601";
                    break;
                case WebEngineType.Trident:
                    WebEngineButtonIcon.Text = "\x2602";
                    break;
            }

            WebViewBrowserSettings WebViewSettings = new()
            {
                Private = Private,
                AudioListener = !App.Instance.LiteMode
            };

            WebView = WebViewManager.Create(Engine, Url, WebViewSettings);

            WebView?.Control.AllowDrop = true;
            WebView?.Control.IsManipulationEnabled = true;
            WebView?.Control.UseLayoutRounding = true;

            WebView?.IsBrowserInitializedChanged += WebView_IsBrowserInitializedChanged;
            //WebView?.Control.PreviewMouseWheel += Chromium_PreviewMouseWheel;

            WebView?.FaviconChanged += WebView_FaviconChanged;
            WebView?.AuthenticationRequested += WebView_AuthenticationRequested;
            WebView?.BeforeNavigation += WebView_BeforeNavigation;
            WebView?.ContextMenuRequested += WebView_ContextMenuRequested;
            WebView?.ExternalProtocolRequested += WebView_ExternalProtocolRequested;
            //WebView?.FindResult += WebView_FindResult;
            WebView?.FrameLoadStart += WebView_FrameLoadStart;
            WebView?.FullscreenChanged += WebView_FullscreenChanged;
            WebView?.JavaScriptMessageReceived += WebView_JavaScriptMessageReceived;
            WebView?.LoadingStateChanged += WebView_LoadingStateChanged;
            WebView?.NavigationError += WebView_NavigationError;
            WebView?.NewTabRequested += WebView_NewTabRequested;
            WebView?.PermissionRequested += WebView_PermissionRequested;
            WebView?.ResourceLoaded += WebView_ResourceLoaded;
            WebView?.ResourceRequested += WebView_ResourceRequested;
            //WebView?.ResourceResponded += WebView_ResourceResponded;
            WebView?.ScriptDialogOpened += WebView_ScriptDialogOpened;
            WebView?.StatusMessage += WebView_StatusMessage;
            WebView?.TitleChanged += WebView_TitleChanged;
            WebView?.AudioPlayingChanged += WebView_AudioPlayingChanged;

            CoreContainer.Visibility = Visibility.Collapsed;
            CoreContainer.Children.Add(WebView?.Control);
            //Chromium.Visibility = Visibility.Collapsed;//VIDEO

            /*Tab.ParentWindow.WindowState = WindowState.Normal;//VIDEO
            Tab.ParentWindow.WindowStyle = WindowStyle.None;//VIDEO
            Tab.ParentWindow.WindowState = WindowState.Maximized;//VIDEO*/
        }

        private async void WebView_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (WebView != null && WebView.IsBrowserInitialized)
            {
                CoreContainer.Visibility = Visibility.Visible;
                Tab.IsUnloaded = false;
                if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                    Tab.ProgressBarVisibility = Visibility.Visible;
                if (bool.Parse(App.Instance.GlobalSave.Get("NetworkLimit")))
                {
                    float Bandwidth = float.Parse(App.Instance.GlobalSave.Get("Bandwidth"));
                    LimitNetwork(0, Bandwidth, Bandwidth);
                }
                await ToggleEfficientAdBlock(App.Instance.AdBlock == 2);
                UserAgentBranding = !Private;
                if (UserAgentBranding)
                {
                    await WebView?.CallDevToolsAsync("Emulation.setUserAgentOverride", new
                    {
                        userAgent = App.Instance.UserAgent,
                        userAgentMetadata = App.Instance.UserAgentData
                    });
                    await WebView?.CallDevToolsAsync("Network.setUserAgentOverride", new
                    {
                        userAgent = App.Instance.UserAgent,
                        userAgentMetadata = App.Instance.UserAgentData
                    });
                }
                if (App.Instance.LiteMode)
                {
                    await WebView?.CallDevToolsAsync("Emulation.setDataSaverOverride", new
                    {
                        dataSaverEnabled = true
                    });
                }
            }
        }

        private void WebView_TitleChanged(object? sender, string e)
        {
            Tab.Header = e;
            if (Tab == Tab.ParentWindow.GetTab())
                Title = e + " - SLBr";
        }

        private void WebView_StatusMessage(object? sender, string e)
        {
            if (string.IsNullOrEmpty(e))
                StatusBarPopup.IsOpen = false;
            else
            {
                StatusMessage.Text = e;
                StatusBarPopup.IsOpen = true;
            }
        }

        private void WebView_ScriptDialogOpened(object? sender, ScriptDialogEventArgs e)
        {
            if (e.DialogType == ScriptDialogType.Alert)
            {
                InformationDialogWindow InfoWindow = new("Alert", $"{Utils.Host(e.Url)}", e.Text);
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
            else if (e.DialogType == ScriptDialogType.Confirm)
            {
                InformationDialogWindow InfoWindow = new("Confirmation", $"{Utils.Host(e.Url)}", e.Text, string.Empty, "OK", "Cancel");
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
            else if (e.DialogType == ScriptDialogType.Prompt)
            {
                DynamicDialogWindow _DynamicDialogWindow = new("Prompt", Utils.Host(e.Url),
                    new List<InputField>
                    {
                        new InputField { Name = e.Text, IsRequired = false, Type = DialogInputType.Text, Value = e.DefaultPrompt }
                    },
                    "\ue946"
                );
                _DynamicDialogWindow.Topmost = true;
                e.Handled = true;
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    e.PromptResult = _DynamicDialogWindow.InputFields[0].Value;
                    e.Result = true;
                }
                else
                    e.Result = false;
            }
            else if (e.DialogType == ScriptDialogType.BeforeUnload)
            {
                InformationDialogWindow InfoWindow = new("Warning", e.IsReload ? "Reload site?" : "Leave site?", "You may lose unsaved changes. Do you want to continue?", string.Empty, e.IsReload ? "Reload" : "Leave", "Cancel");
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
        }

        public ConcurrentDictionary<string, bool> HostCache = new(StringComparer.Ordinal);

        public long Image_Budget = 2 * 1024 * 1024;
        public long Stylesheet_Budget = 400 * 1024;
        public long Script_Budget = 500 * 1024;
        public long Font_Budget = 300 * 1024;
        public long Frame_Budget = 5;

        public void ResetBudgets()
        {
            Image_Budget = 2 * 1024 * 1024;
            Stylesheet_Budget = 400 * 1024;
            Script_Budget = 500 * 1024;
            Font_Budget = 300 * 1024;
            Frame_Budget = 5;
        }

        public bool IsOverBudget(ResourceRequestType _ResourceType)
        {
            switch (_ResourceType)
            {
                case ResourceRequestType.Image:
                    return Image_Budget <= 0;
                case ResourceRequestType.Stylesheet:
                    return Stylesheet_Budget <= 0;
                case ResourceRequestType.Script:
                    return Script_Budget <= 0;
                case ResourceRequestType.Font:
                    return Font_Budget <= 0;
                case ResourceRequestType.SubFrame:
                    return Frame_Budget <= 0;
                default:
                    return false;
            }
        }

        public void DeductFromBudget(ResourceRequestType _ResourceType, long DataLength)
        {
            switch (_ResourceType)
            {
                case ResourceRequestType.Image:
                    Image_Budget -= DataLength;
                    return;
                case ResourceRequestType.Stylesheet:
                    Stylesheet_Budget -= DataLength;
                    return;
                case ResourceRequestType.Script:
                    Script_Budget -= DataLength;
                    return;
                case ResourceRequestType.Font:
                    Font_Budget -= DataLength;
                    return;
                case ResourceRequestType.SubFrame:
                    Frame_Budget -= DataLength;
                    return;
                default:
                    break;
            }
        }

        private void WebView_ResourceRequested(object? sender, ResourceRequestEventArgs e)
        {
            if (!Utils.IsHttpScheme(e.Url))
            {
                e.Cancel = false;
                return;
            }
            if (!App.Instance.ExternalFonts && e.ResourceRequestType == ResourceRequestType.Font)
            {
                e.Cancel = true;
                return;
            }
            if (App.Instance.NeverSlowMode)
            {
                if (IsOverBudget(e.ResourceRequestType))
                {
                    e.Cancel = true;
                    return;
                }
                foreach (string Pattern in App.FailedScripts)
                {
                    if (e.Url.Contains(Pattern))
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            if (App.Instance.AdBlock == 1)
            {
                bool ContinueAdBlock = true;
                if (string.IsNullOrEmpty(e.FocusedUrl))
                {
                    string Host = Utils.FastHost(e.FocusedUrl);
                    if (App.Instance.AdBlockAllowList.Has(Host))
                    {
                        e.Cancel = false;
                        ContinueAdBlock = false;
                    }
                }
                if (ContinueAdBlock)
                {
                    if (e.ResourceRequestType == ResourceRequestType.Ping)
                    {
                        Interlocked.Increment(ref App.Instance.TrackersBlocked);
                        e.Cancel = true;
                        return;
                    }
                    else if (Utils.IsPossiblyAd(e.ResourceRequestType))
                    {
                        string Host = Utils.FastHost(e.Url);
                        bool Cached = HostCache.TryGetValue(Host, out bool Blocked);
                        if (Blocked)
                        {
                            e.Cancel = true;
                            return;
                        }
                        if (!Cached)
                        {
                            if (App.Ads.Has(Host))
                            {
                                Interlocked.Increment(ref App.Instance.AdsBlocked);
                                HostCache[Host] = true;
                                e.Cancel = true;
                                return;
                            }
                            else if (App.Analytics.Has(Host))
                            {
                                Interlocked.Increment(ref App.Instance.TrackersBlocked);
                                HostCache[Host] = true;
                                e.Cancel = true;
                                return;
                            }
                            HostCache[Host] = false;
                        }
                        if (e.ResourceRequestType == ResourceRequestType.Script)
                        {
                            if (App.HasInLinkRegex.IsMatch(e.Url))
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                }
            }

            if (App.Instance.LiteMode)
                e.ModifiedHeaders.Add("Save-Data", "on");
            if (UserAgentBranding)
            {
                //TODO: Fix turnstile issue, UA changes not applied in WebView2.
                e.ModifiedHeaders.Add("User-Agent", App.Instance.UserAgent);
                e.ModifiedHeaders.Add("Sec-Ch-Ua", App.Instance.UserAgentBrandsString);
            }
            if (ProprietaryCodecsInfoBar == null && WebView.Engine == WebEngineType.Chromium && e.ResourceRequestType == ResourceRequestType.Media && Utils.IsProprietaryCodec(Utils.GetFileExtension(e.Url)))
            {
                if (bool.Parse(App.Instance.GlobalSave.Get("WarnCodec")))
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ProprietaryCodecsInfoBar = new()
                        {
                            Icon = "\xea69",
                            Title = "Proprietary Codecs Detected",
                            Description = "This site is trying to play media using formats not supported by Chromium (CEF). Do you want to switch to the Edge (WebView2) engine?",
                            Actions = [
                                new() { Text = "Switch", Background = (SolidColorBrush)FindResource("IndicatorBrush"), Foreground = App.Instance.WhiteColor, Command = new RelayCommand(() => { CloseInfoBar(ProprietaryCodecsInfoBar); Action(Actions.SwitchWebEngine, "1"); }) },
                                new() { Text = "Do not ask again", Command = new RelayCommand(async () => { CloseInfoBar(WaybackInfoBar); App.Instance.GlobalSave.Set("WarnCodec", false); }) }
                            ]
                        };
                        LocalInfoBars.Add(ProprietaryCodecsInfoBar);
                    });
                }
            }
        }

        private void WebView_ResourceLoaded(object? sender, ResourceLoadedResult e)
        {
            if (App.Instance.NeverSlowMode)
            {
                if (e.Success)
                    DeductFromBudget(e.ResourceRequestType, e.ReceivedContentLength);
                else// if (status == UrlRequestStatus.Failed)
                {
                    if (e.ResourceRequestType != ResourceRequestType.Script)
                        return;
                    App.FailedScripts.Add(Utils.CleanUrl(e.Url, true, true, true, true, true));
                }
            }
        }

        private void WebView_BeforeNavigation(object? sender, BeforeNavigationEventArgs e)
        {
            if (e.IsMainFrame)
            {
                if (WebView.Engine == WebEngineType.Chromium && !WebViewManager.OverrideRequests.ContainsKey(e.Url))
                {
                    if (Utils.IsHttpScheme(e.Url))
                    {
                        if (!Private && App.Instance.WebRiskService != WebRiskHandler.SecurityService.None && Utils.GetFileExtension(e.Url) != ".pdf")
                        {
                            WebRiskHandler.ThreatType _ThreatType = App.Instance._WebRiskHandler.IsSafe(e.Url, App.Instance.WebRiskService);
                            if (_ThreatType is WebRiskHandler.ThreatType.Malware or WebRiskHandler.ThreatType.Unwanted_Software)
                                WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Malware_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                            else if (_ThreatType == WebRiskHandler.ThreatType.Social_Engineering)
                                WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Deception_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                        }
                    }
                    else if (e.Url.StartsWith("chrome:"))
                    {
                        bool Block = false;
                        //https://source.chromium.org/chromium/chromium/src/+/main:ios/chrome/browser/shared/model/url/chrome_url_constants.cc
                        switch (e.Url.Substring(9))
                        {
                            case string s when s.StartsWith("settings"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("history"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("downloads"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("flags"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("new-tab-page"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("bookmarks"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("apps"):
                                Block = true;
                                break;

                            case string s when s.StartsWith("dino"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("management"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("new-tab-page-third-party"):
                                Block = true;
                                break;

                            case string s when s.StartsWith("favicon"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("sandbox"):
                                Block = true;
                                break;

                            case string s when s.StartsWith("bookmarks-side-panel.top-chrome"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("customize-chrome-side-panel.top-chrome"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("read-later.top-chrome"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("tab-search.top-chrome"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("tab-strip.top-chrome"):
                                Block = true;
                                break;

                            case string s when s.StartsWith("support-tool"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("privacy-sandbox-dialog"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("chrome-signin"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("browser-switch"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("profile-picker"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("intro"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("sync-confirmation"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("app-settings"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("managed-user-profile-notice"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("reset-password"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("connection-help"):
                                Block = true;
                                break;
                            case string s when s.StartsWith("connection-monitoring-detected"):
                                Block = true;
                                break;
                        }
                        if (Block)
                            WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.GenerateCannotConnect(e.Url, -300, "ERR_INVALID_URL"), Encoding.UTF8), "text/html", -1, string.Empty);
                    }
                }

                if (!Private && App.Instance.AMP && Utils.IsHttpScheme(e.Url) && WebView.Engine != WebEngineType.Trident && !WebViewManager.OverrideRequests.ContainsKey(e.Url))
                {
                    string? AMPUrl = Utils.GetAMPUrl(e.Url);
                    if (AMPUrl != null && AMPUrl != e.Url)
                    {
                        WebView.Stop();
                        Address = AMPUrl;
                        return;
                    }
                }
                //WARNING: Do not remove BeginInvoke, otherwise it will somehow freeze CefSharp
                Dispatcher.BeginInvoke(async () =>
                {
                    if (App.Instance.NeverSlowMode)
                        ResetBudgets();
                    HostCache.Clear();
                    QRBitmap = null;
                    SetIcon(await App.Instance.SetIcon(string.Empty, e.Url, Private));
                });
            }
        }

        BitmapImage? WebIcon;

        void SetIcon(BitmapImage Image, bool Force = false)
        {
            WebIcon = Image;
            Tab.Icon = Force ? Image : ((Muted || !AudioPlaying) ? Image : App.Instance.AudioIcon);
        }

        private void WebView_PermissionRequested(object? sender, PermissionRequestedEventArgs e)
        {
            string Permissions = string.Empty;
            string PermissionIcons = string.Empty;
            foreach (WebPermissionKind Option in Enum.GetValues<WebPermissionKind>())
            {
                if (e.Kind.HasFlag(Option) && Option != WebPermissionKind.None)
                {
                    switch (Option)
                    {
                        /*case ProperPermissionRequestType.AccessibilityEvents:
                            Permissions += "Respond to Accessibility Events";
                            break;*/
                        case WebPermissionKind.ArSession:
                            Permissions += "Use your camera to create a 3D map of your surroundings";
                            PermissionIcons += "\xE809";
                            break;
                        case WebPermissionKind.CameraPanTiltZoom:
                            Permissions += "Move your camera";
                            PermissionIcons += "\xE714";
                            break;
                        case WebPermissionKind.CameraStream:
                            Permissions += "Use your camera";
                            PermissionIcons += "\xE714";
                            break;
                        case WebPermissionKind.CapturedSurfaceControl:
                            Permissions += "Scroll and zoom the contents of your shared tab";
                            PermissionIcons += "\xec6c";
                            break;
                        case WebPermissionKind.Clipboard:
                            Permissions += "See text and images in clipboard";
                            PermissionIcons += "\xF0E3";
                            break;
                        case WebPermissionKind.TopLevelStorageAccess:
                            Permissions += "Access cookies and site Data";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.DiskQuota:
                            Permissions += "Store files on this device";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.LocalFonts:
                            Permissions += "Use your computer fonts";
                            PermissionIcons += "\xE8D2";
                            break;
                        case WebPermissionKind.Geolocation:
                            Permissions += "Know your location";
                            PermissionIcons += "\xECAF";
                            break;
                        case WebPermissionKind.IdentityProvider:
                            Permissions += "Use your accounts to login to websites";
                            PermissionIcons += "\xef58";
                            break;
                        case WebPermissionKind.IdleDetection:
                            Permissions += "Know when you're actively using this device";
                            PermissionIcons += "\xEA6C";
                            break;
                        case WebPermissionKind.MicStream:
                            Permissions += "Use your microphone";
                            PermissionIcons += "\xE720";
                            break;
                        case WebPermissionKind.MidiSysex:
                            Permissions += "Use your MIDI devices";
                            PermissionIcons += "\xEC4F";
                            break;
                        case WebPermissionKind.MultipleDownloads:
                            Permissions += "Download multiple files";
                            PermissionIcons += "\xE896";
                            break;
                        case WebPermissionKind.Notifications:
                            Permissions += "Show notifications";
                            PermissionIcons += "\xEA8F";
                            break;
                        case WebPermissionKind.KeyboardLock:
                            Permissions += "Lock and use your keyboard";
                            PermissionIcons += "\xf26b";
                            break;
                        case WebPermissionKind.PointerLock:
                            Permissions += "Lock and use your mouse";
                            PermissionIcons += "\xf271";
                            break;
                        case WebPermissionKind.ProtectedMediaIdentifier:
                            Permissions += "Know your unique device identifier";
                            PermissionIcons += "\xef3f";
                            break;
                        case WebPermissionKind.RegisterProtocolHandler:
                            Permissions += "Open web links";
                            PermissionIcons += "\xE71B";
                            break;
                        case WebPermissionKind.StorageAccess:
                            Permissions += "Access cookies and site Data";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.VrSession:
                            Permissions += "Use your virtual reality devices";
                            PermissionIcons += "\xEC94";
                            break;
                        case WebPermissionKind.WindowManagement:
                            Permissions += "Manage windows on all your displays";
                            PermissionIcons += "\xE737";
                            break;
                        case WebPermissionKind.FileSystemAccess:
                            Permissions += "Access file system";
                            PermissionIcons += "\xEC50";
                            break;
                        case WebPermissionKind.ScreenShare:
                            Permissions += "Share your screen\n";
                            PermissionIcons += "\xE7F4\n";
                            break;
                        case WebPermissionKind.RecordAudio:
                            Permissions += "Capture desktop audio\n";
                            PermissionIcons += "\xE7F3\n";
                            break;
                    }
                    Permissions += "\n";
                    PermissionIcons += "\n";
                }
            }

            Permissions = Permissions.TrimEnd('\n');
            PermissionIcons = PermissionIcons.TrimEnd('\n');
            if (string.IsNullOrEmpty(Permissions))
                Permissions = e.Kind.ToString();

            InformationDialogWindow InfoWindow = new("Permission", $"Allow {Utils.Host(e.Url)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
            InfoWindow.Topmost = true;

            bool? Result = InfoWindow.ShowDialog();
            if (Result == true)
                e.State = WebPermissionState.Allow;
            else
                e.State = WebPermissionState.Deny;
        }

        private void WebView_NewTabRequested(object? sender, NewTabRequestEventArgs e)
        {
            if (e.Popup.HasValue)
            {
                //TODO: Add popup permission check
                int Width = (int)e.Popup.Value.Width;
                if (Width == 0)
                    Width = 600;
                int Height = (int)e.Popup.Value.Height;
                if (Height == 0)
                    Height = 650;
                PopupBrowser Popup = new(e.Url, Width, Height);
                Popup.Show();
                if (e.Popup.Value.Left != 0)
                    Popup.Left = e.Popup.Value.Left;
                if (e.Popup.Value.Top != 0)
                    Popup.Top = e.Popup.Value.Top;
                e.WebView = Popup.WebView;
            }
            else
                e.WebView = Tab.ParentWindow.NewTab(e.Url, !e.Background, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private ? Private : Private, Tab.TabGroup);
        }

        private void WebView_NavigationError(object? sender, NavigationErrorEventArgs e)
        {
            //https://github.com/brave/brave-core/blob/master/components/brave_wayback_machine/brave_wayback_machine_tab_helper.cc
            if (WaybackInfoBar == null && e.ErrorCode is 404 or 408 or 410 or 451 or 500 or 502 or 503 or 504 or 509 or 520 or 521 or 523 or 524 or 525 or 526 && Utils.IsHttpScheme(e.Url) && bool.Parse(App.Instance.GlobalSave.Get("WaybackInfoBar")) && Utils.FastHost(e.Url) != "web.archive.org")
            {
                Dispatcher.BeginInvoke(async () =>
                {
                    WaybackInfoBar = new()
                    {
                        Icon = "\xf384",
                        IconForeground = App.Instance.OrangeColor,
                        Title = "Page Missing",
                        Description = "Do you want to check if a snapshot is available on the Wayback Machine?",
                        Actions = [
                            new() { Text = "Check", Background = (SolidColorBrush)FindResource("IndicatorBrush"), Foreground = App.Instance.WhiteColor, Command = new RelayCommand(async () => {
                                WaybackInfoBar.Actions[0].IsEnabled = false;
                                try
                                {
                                    using (HttpClient Client = new())
                                    {
                                        Client.DefaultRequestHeaders.Add("User-Agent", App.Instance.UserAgent);
                                        string Json = await Client.GetStringAsync($"https://brave-api.archive.org/wayback/available?url={WebUtility.UrlEncode(e.Url)}");
                                        CloseInfoBar(WaybackInfoBar);
                                        using JsonDocument Document = JsonDocument.Parse(Json);
                                        if (Document.RootElement.TryGetProperty("archived_snapshots", out JsonElement Snapshots) && Snapshots.TryGetProperty("closest", out JsonElement Closest) && Closest.TryGetProperty("available", out JsonElement Available) && Available.GetBoolean() && Closest.TryGetProperty("url", out var Url))
                                            Navigate(Url.GetString());
                                    }
                                }
                                catch { CloseInfoBar(WaybackInfoBar); }
                            }) },
                            new() { Text = "Do not ask again", Command = new RelayCommand(async () => {
                                CloseInfoBar(WaybackInfoBar);
                                App.Instance.GlobalSave.Set("WaybackInfoBar", false);
                            }) }
                        ]
                    };
                    LocalInfoBars.Add(WaybackInfoBar);
                });
            }
            if (WebView.Engine == WebEngineType.ChromiumEdge && e.ErrorText == "Unknown") //For Edge's SmartScreen error page
                return;
            if (WebView.Engine == WebEngineType.Trident && e.ErrorCode == -2146697203) //Custom protocols in IE
                return;
            WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.GenerateCannotConnect(e.Url, e.ErrorCode, e.ErrorText), Encoding.UTF8), "text/html", 1);
            Navigate(e.Url);
        }

        private async void SetDarkMode(bool IsDarkModeEnabled)
        {
            if (App.Instance.SmartDarkMode)
            {
                App.Instance.Dispatcher.BeginInvoke(async () =>
                {
                    if (WebView.CanExecuteJavascript)
                    {
                        var RequireForceDarkMode = await WebView?.EvaluateScriptAsync(Scripts.CheckNativeDarkModeScript);
                        await WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
                        {
                            enabled = RequireForceDarkMode == "1"
                        });
                    }
                    else
                    {
                        EventHandler<bool>? DelayHandler = null;
                        DelayHandler = async (sender, args) =>
                        {
                            if (WebView.CanExecuteJavascript)
                            {
                                WebView.LoadingStateChanged -= DelayHandler;
                                var RequireForceDarkMode = await WebView?.EvaluateScriptAsync(Scripts.CheckNativeDarkModeScript);
                                await WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
                                {
                                    enabled = RequireForceDarkMode == "1"
                                });
                            }
                        };
                        WebView.LoadingStateChanged += DelayHandler;
                    }
                });
            }
            await WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
            {
                enabled = IsDarkModeEnabled
            });
        }

        private async void WebView_LoadingStateChanged(object? sender, bool e)
        {
            if (WebView == null || !WebView.IsBrowserInitialized)
                return;
            if (Address.StartsWith("slbr:"))
                WebView?.ExecuteScript(Scripts.InternalScript);
            BackButton.IsEnabled = CanGoBack;
            ForwardButton.IsEnabled = CanGoForward;
            ReloadButton.Content = IsLoading ? "\xF78A" : "\xE72C";
            SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);

            CurrentWebAppManifest = null;
            CurrentWebAppManifestUrl = string.Empty;
            InstallWebAppButton.Visibility = Visibility.Collapsed;
            if (WaybackInfoBar != null)
            {
                CloseInfoBar(WaybackInfoBar);
                WaybackInfoBar = null;
            }
            BrowserLoadChanged(Address, IsLoading);
            if (!IsLoading)
            {
                if (!Private)
                    App.Instance.AddHistory(Address, Title);
                if (!App.Instance.LiteMode && bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
                    WebView.ExecuteScript(Scripts.ScrollScript);
                if (!Address.StartsWith("slbr:"))
                {
                    if (WebView.CanExecuteJavascript)
                    {
                        if (Utils.IsHttpScheme(Address))
                        {
                            if (App.Instance.SkipAds && Address.Contains("youtube.com/watch?v="))
                                WebView?.ExecuteScript(Scripts.YouTubeSkipAdScript);
                            if (Address.Contains("chromewebstore.google.com/detail"))
                                WebView?.ExecuteScript(Scripts.WebStoreScript);
                            if (bool.Parse(App.Instance.GlobalSave.Get("WebNotifications")))
                                WebView?.ExecuteScript(Scripts.NotificationPolyfill);
                            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("OpenSearch")))
                            {
                                string SiteHost = Utils.FastHost(Address);
                                if (App.Instance.SearchEngines.Any(i => i.Host == SiteHost))
                                    WebView?.ExecuteScript(Scripts.OpenSearchScript);
                            }

                            if (bool.Parse(App.Instance.GlobalSave.Get("WebApps")))
                            {
                                var (Installable, ManifestUrl) = await IsInstallableAsync();
                                if (Installable)
                                {
                                    InstallWebAppButton.Visibility = Visibility.Visible;
                                    CurrentWebAppManifestUrl = ManifestUrl;
                                }
                            }
                        }
                        else if (Address.StartsWith("file:///"))
                        {
                            if (Address.EndsWith('/'))
                            {
                                if (Directory.Exists(Uri.UnescapeDataString(Address.AsSpan(8)).Replace('/', '\\')))
                                    WebView?.ExecuteScript(Scripts.FileScript);
                            }
                        }
                    }
                    if (bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme")))
                    {
                        try
                        {
                            string CustomThemeColor = string.Empty;
                            string? Task = await WebView?.EvaluateScriptAsync("document.querySelector('meta[name=\"theme-color\"]')?.content");
                            if (Task != null)
                                CustomThemeColor = Task;
                            if (!string.IsNullOrEmpty(CustomThemeColor))
                            {
                                IsCustomTheme = true;
                                Color PrimaryColor = Utils.ParseThemeColor(CustomThemeColor);

                                Theme SiteTheme = App.Instance.GenerateTheme(PrimaryColor);
                                SetAppearance(SiteTheme);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(SiteTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(SiteTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(SiteTheme.BorderColor);
                            }
                            else if (IsCustomTheme)
                            {
                                IsCustomTheme = false;
                                SetAppearance(App.Instance.CurrentTheme);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private async void HandleInternalMessage(Dictionary<string, object> Message)
        {
            if (!Message.TryGetValue("function", out object? Value))
                return;
            switch (Value?.ToString())
            {
                case "background":
                    string Url = string.Empty;
                    try
                    {
                        switch (App.Instance.GlobalSave.GetInt("HomepageBackground"))
                        {
                            case 0:
                                Url = App.Instance.GlobalSave.Get("CustomBackgroundImage");
                                if (!Utils.IsHttpScheme(Url) && File.Exists(Url))
                                    Url = $"Data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(Url))}";
                                break;
                            case 1:
                                int BingBackground = App.Instance.GlobalSave.GetInt("BingBackground");
                                if (BingBackground == 0)
                                {
                                    try
                                    {
                                        XmlDocument Doc = new();
                                        Doc.LoadXml(new WebClient().DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                                        Url = "http://www.bing.com/" + Doc.SelectSingleNode("/images/image/url").InnerText;
                                    }
                                    catch { }
                                }
                                else
                                    Url = "http://bingw.jasonzeng.dev/?index=random";
                                break;
                            case 2:
                                Url = "http://picsum.photos/1920/1080?random";
                                break;
                            case 3:
                                using (var Client = new HttpClient())
                                {
                                    string Json = await Client.GetStringAsync("https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY");
                                    using var Document = JsonDocument.Parse(Json);
                                    var Root = Document.RootElement;
                                    if (App.Instance.HighPerformanceMode && Root.TryGetProperty("hdurl", out var HDUrl))
                                        Url = HDUrl.GetString() ?? string.Empty;
                                    else if (Root.TryGetProperty("url", out var _Url))
                                        Url = _Url.GetString() ?? string.Empty;
                                }
                                break;
                        }
                    }
                    catch { }
                    if (!string.IsNullOrEmpty(Url))
                        WebView?.ExecuteScript($"document.documentElement.style.backgroundImage = \"url('{Url}')\";");
                    break;

                case "Search":
                    Address = Utils.FilterUrlForBrowser(Message["variable"]?.ToString() ?? string.Empty, App.Instance.DefaultSearchProvider.SearchUrl);
                    break;
            }
        }

        private void WebView_JavaScriptMessageReceived(object? sender, string e)
        {
            if (string.IsNullOrWhiteSpace(e))
                return;
            Dictionary<string, object>? Message;
            try
            {
                Message = JsonSerializer.Deserialize<Dictionary<string, object>>(e);
            }
            catch { return; }
            if (Message == null || !Message.TryGetValue("type", out object? Value))
                return;

            switch (Value.ToString())
            {
                case "OpenSearch":
                    App.Instance.SaveOpenSearch(Message["name"]?.ToString()!, Message["url"]?.ToString()!);
                    break;
                case "Internal":
                    if (Address.StartsWith("slbr:"))
                        HandleInternalMessage(Message);
                    break;
                case "Notification":
                    try
                    {
                        var DataJson = Message["data"]?.ToString();
                        if (string.IsNullOrWhiteSpace(DataJson))
                            return;
                        var Data = JsonSerializer.Deserialize<List<object>>(DataJson);
                        if (Data != null && Data.Count == 2)
                        {
                            var ToastXML = new Windows.Data.Xml.Dom.XmlDocument();
                            ToastXML.LoadXml(@$"<toast>
    <visual>
        <binding template=""ToastText04"">
            <text id=""1"">{Data[0]}</text>
            <text id=""2"">{((IDictionary<string, object>)JsonSerializer.Deserialize<ExpandoObject>(((JsonElement)Data[1]).GetRawText()))["body"]}</text>
            <text id=""3"">{Utils.Host(Address, false)}</text>
        </binding>
    </visual>
</toast>");
                            ToastNotificationManager.CreateToastNotifier("SLBr").Show(new ToastNotification(ToastXML));
                        }
                    }
                    catch { }
                    break;
            }
        }

        private void WebView_FullscreenChanged(object? sender, bool e)
        {
            Tab.ParentWindow.Fullscreen(e, this);
        }

        private void WebView_FrameLoadStart(object? sender, string e)
        {
            if (Utils.IsHttpScheme(e))
            {
                WebView?.ExecuteScript(Scripts.AntiCloseScript);//Replacement for DoClose of LifeSpanHandler in RuntimeStyle Chrome
                WebView?.ExecuteScript(Scripts.ShiftContextMenuScript);
                if (bool.Parse(App.Instance.GlobalSave.Get("AntiTamper")))
                {
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiFullscreen")))
                        WebView?.ExecuteScript(Scripts.AntiFullscreenScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect")))
                        WebView?.ExecuteScript(Scripts.LateAntiDevtoolsScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu")))
                        WebView?.ExecuteScript(Scripts.ForceContextMenuScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("TextSelection")))
                        WebView?.ExecuteScript(Scripts.AllowInteractionScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter")))
                        WebView?.ExecuteScript(Scripts.RemoveFilterCSS);
                    if (bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay")))
                        WebView?.ExecuteScript(Scripts.RemoveOverlayCSS);
                }
                if (bool.Parse(App.Instance.GlobalSave.Get("ForceLazy")))
                    WebView?.ExecuteScript(Scripts.ForceLazyLoad);
            }
        }

        /*private void WebView_FindResult(object? sender, FindResult e)
        {
        }*/

        private void WebView_ExternalProtocolRequested(object? sender, ExternalProtocolEventArgs e)
        {
            string ProtocolName = Utils.GetProtocolName(Utils.GetScheme(e.Url));
            string Host = Utils.FastHost(e.Origin);
            InformationDialogWindow InfoWindow = new("Warning", $"Open {ProtocolName}", $"{(Host.Length == 0 ? "A website" : Host)} is requesting to open this application.", string.Empty, "Open", "Cancel");
            InfoWindow.Topmost = true;
            e.Launch = InfoWindow.ShowDialog() == true;
        }

        private async void WebView_ContextMenuRequested(object? sender, WebContextMenuEventArgs e)
        {
            bool IsPageMenu = true;
            ContextMenu BrowserMenu = new ContextMenu();
            foreach (WebContextMenuType i in Enum.GetValues<WebContextMenuType>())
            {
                if (e.MenuType.HasFlag(i))
                {
                    if (BrowserMenu.Items.Count != 0 && BrowserMenu.Items[BrowserMenu.Items.Count - 1].GetType() == typeof(MenuItem))
                        BrowserMenu.Items.Add(new Separator());
                    if (i == WebContextMenuType.Link)
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open link in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.LinkUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy link", Command = new RelayCommand(_ => Clipboard.SetText(e.LinkUrl)) }); 
                        BrowserMenu.Items.Add(new MenuItem { IsEnabled = !string.IsNullOrEmpty(e.LinkText?.Trim()), Icon = "\ue8c8", Header = "Copy link text", Command = new RelayCommand(_ => Clipboard.SetText(e.LinkText)) });
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\ue72d", Header = "Share link", Command = new RelayCommand(_ => Share(e.LinkUrl)) });
                    }
                    else if (i == WebContextMenuType.Selection && !e.IsEditable && !string.IsNullOrEmpty(e.SelectionText.ReplaceLineEndings("").Trim()))
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.ReplaceLineEndings("").Trim().Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText.ReplaceLineEndings("").Trim())), true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private)) });
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+C", Icon = "\ue8c8", Header = "Copy", Command = new RelayCommand(_ => Clipboard.SetText(e.SelectionText)) });
                        BrowserMenu.Items.Add(new Separator());
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select all", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                    }
                    else if (i == WebContextMenuType.Media)
                    {
                        if (e.MediaType == WebContextMenuMediaType.Image)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open image in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.SourceUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save image as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\xe8b9",
                                Header = "Copy image",
                                Command = new RelayCommand(_ =>
                                {
                                    try { Utils.DownloadAndCopyImage(e.SourceUrl); }
                                    catch { Clipboard.SetText(e.SourceUrl); }
                                })
                            });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy image link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\uF6Fa",
                                Header = "Search image",
                                Command = new RelayCommand(_ =>
                                {
                                    string Url = string.Empty;
                                    switch (App.Instance.GlobalSave.GetInt("ImageSearch"))
                                    {
                                        case 0:
                                            Url = $"https://lens.google.com/uploadbyurl?url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 1:
                                            Url = $"https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 2:
                                            Url = $"https://yandex.com/images/search?rpt=imageview&url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 3:
                                            Url = $"https://tineye.com/search?url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                    }
                                    Tab.ParentWindow.NewTab(Url, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup);
                                })
                            });
                        }
                        else if (e.MediaType == WebContextMenuMediaType.Video)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open video in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.SourceUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save video as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy video link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uee49", Header = "Picture in picture", Command = new RelayCommand(_ => WebView?.ExecuteScript("(async()=>{let playingVideo=Array.from(document.querySelectorAll('video')).find(v=>!v.paused&&!v.ended&&v.readyState>2);if (!playingVideo){playingVideo=document.querySelector('video');}if (playingVideo&&document.pictureInPictureEnabled){await playingVideo.requestPictureInPicture();}})();")) });
                        }
                        else if (e.MediaType == WebContextMenuMediaType.Audio)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open audio in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.SourceUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save audio as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy audio link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                        }
                        //TODO: Canvas handling
                        /*else if (e.MediaType == WebContextMenuMediaType.Canvas)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save image as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\xe8b9",
                                Header = "Copy image",
                                Command = new RelayCommand(_ => {
                                    try { Utils.DownloadAndCopyImage(e.SourceUrl); }
                                    catch { Clipboard.SetText(e.SourceUrl); }
                                })
                            });
                        }*/
                    }
                }
            }
            if (e.IsEditable)
            {
                //Doesn't work at all
                /*if (e.SpellCheck && e.DictionarySuggestions.Count != 0)
                {
                    foreach (string Suggestion in e.DictionarySuggestions)
                    {
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uf87b", Header = Suggestion, Command = new RelayCommand(_ =>
                        {
                            if (WebView is ChromiumWebView ChromiumWebView)
                                ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().ReplaceMisspelling(Suggestion);
                        })});
                    }
                    BrowserMenu.Items.Add(new MenuItem { Icon = "\ue82e", Header = "Add to dictionary", Command = new RelayCommand(_ =>
                    {
                        if (WebView is ChromiumWebView ChromiumWebView)
                            ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().AddWordToDictionary(e.MisspelledWord);
                    }) });
                    BrowserMenu.Items.Add(new Separator());
                }*/
                if (!string.IsNullOrEmpty(e.SelectionText) && !e.SelectionText.Contains(' ') && bool.Parse(App.Instance.GlobalSave.Get("SpellCheck")))
                {
                    List<(string Word, List<string> Suggestions)> Results = await App.Instance.SpellCheck(e.SelectionText);
                    if (Results.Count != 0)
                    {
                        int Count = 0;

                        MenuItem? SuggestionsSubMenuModel = null;
                        foreach ((string Word, List<string> Suggestions) in Results)
                        {
                            foreach (string Suggestion in Suggestions)
                            {
                                if (Count < 3)
                                {
                                    BrowserMenu.Items.Add(new MenuItem
                                    {
                                        Icon = "\uf87b",
                                        Header = Suggestion,
                                        Command = new RelayCommand(_ =>
                                        {
                                            if (WebView is ChromiumWebView ChromiumWebView)
                                                ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().ReplaceMisspelling(Suggestion);
                                        })
                                    });
                                }
                                else
                                {
                                    SuggestionsSubMenuModel ??= new MenuItem { Icon = "\ue82d", Header = "More" };
                                    SuggestionsSubMenuModel.Items.Add(new MenuItem
                                    {
                                        Icon = "\uf87b",
                                        Header = Suggestion,
                                        Command = new RelayCommand(_ =>
                                        {
                                            if (WebView is ChromiumWebView ChromiumWebView)
                                                ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().ReplaceMisspelling(Suggestion);
                                        })
                                    });
                                }
                                Count++;
                            }
                        }
                        if (SuggestionsSubMenuModel != null)
                            BrowserMenu.Items.Add(SuggestionsSubMenuModel);
                        BrowserMenu.Items.Add(new Separator());
                    }
                }

                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Win+Period", Icon = "\ue76e", Header = "Emoji", Command = new RelayCommand(_ => CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji)) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+Z", Icon = "\ue7a7", Header = "Undo", Command = new RelayCommand(_ => WebView?.Undo()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+Y", Icon = "\ue7a6", Header = "Redo", Command = new RelayCommand(_ => WebView?.Redo()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+X", Icon = "\ue8c6", Header = "Cut", Command = new RelayCommand(_ => WebView?.Cut()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+C", Icon = "\ue8c8", Header = "Copy", Command = new RelayCommand(_ => WebView?.Copy()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+V", Icon = "\ue77f", Header = "Paste", Command = new RelayCommand(_ => WebView?.Paste()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue74d", Header = "Delete", Command = new RelayCommand(_ => WebView?.Delete()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select all", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                if (!string.IsNullOrEmpty(e.SelectionText.ReplaceLineEndings("").Trim()))
                {
                    BrowserMenu.Items.Add(new Separator());
                    BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.ReplaceLineEndings("").Trim().Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText.ReplaceLineEndings("").Trim())), true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                }
            }
            else if (IsPageMenu)// && e.MediaType == WebContextMenuMediaType.None)
            {
                StackPanel TopMenuStack = new() { Orientation = Orientation.Horizontal };
                TopMenuStack.Children.Add(new MenuItem { IsEnabled = WebView.CanGoBack, Icon = "\uE76B", ToolTip = "Back", Command = new RelayCommand(_ => WebView?.Back()), Template = (ControlTemplate)FindResource("IconMenuItemTemplate") });

                TopMenuStack.Children.Add(new MenuItem { IsEnabled = WebView.CanGoForward, Icon = "\uE76C", ToolTip = "Forward", Command = new RelayCommand(_ => WebView?.Forward()), Template = (ControlTemplate)FindResource("IconMenuItemTemplate") });
                TopMenuStack.Children.Add(new MenuItem { Icon = "\uE72C", ToolTip = "Refresh", Command = new RelayCommand(_ => WebView?.Refresh()), Template = (ControlTemplate)FindResource("IconMenuItemTemplate") });
                if (FavouriteExists(Address) != -1)
                    TopMenuStack.Children.Add(new MenuItem { Icon = "\xEB52", ToolTip = "Remove from favourites", Foreground = App.Instance.FavouriteColor, Command = new RelayCommand(_ => FavouriteAction()), Template = (ControlTemplate)FindResource("IconMenuItemTemplate") });
                else
                    TopMenuStack.Children.Add(new MenuItem { Icon = "\xEB51", ToolTip = "Add to favourites", Command = new RelayCommand(_ => FavouriteAction()), Template = (ControlTemplate)FindResource("IconMenuItemTemplate") });

                BrowserMenu.Items.Add(new MenuItem { Template = (ControlTemplate)FindResource("EmptyMenuItemTemplate"), Focusable = false, Header = TopMenuStack });

                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save as", Command = new RelayCommand(_ => WebView?.SaveAs()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE749", Header = "Print", Command = new RelayCommand(_ => WebView?.Print()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue72d", Header = "Share", Command = new RelayCommand(_ => Share()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select all", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                BrowserMenu.Items.Add(new Separator());


                MenuItem ToolsSubMenuModel = new() { Icon = "\ue821", Header = "More tools" };
                ToolsSubMenuModel.Items.Add(new MenuItem { IsEnabled = !IsLoading && !Address.StartsWith("slbr:"), Icon = "\uE8C1", Header = $"Translate to {TranslateComboBox.SelectedValue}", Command = new RelayCommand(_ => Translate()) });
                ToolsSubMenuModel.Items.Add(new MenuItem { Icon = "\uE924", Header = "Screenshot", Command = new RelayCommand(_ => Screenshot()) });
                ToolsSubMenuModel.Items.Add(new MenuItem { Icon = "\ue72d", Header = "Share", Command = new RelayCommand(_ => Share()) });
                BrowserMenu.Items.Add(ToolsSubMenuModel);

                /*MenuItem ZoomSubMenuModel = new MenuItem { Icon = "\ue71e", Header = "Zoom" };
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue8a3", Header = "Zoom in", Command = new RelayCommand(_ => Zoom(1)) });
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue71f", Header = "Zoom out", Command = new RelayCommand(_ => Zoom(-1)) });
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue72c", Header = "Reset", Command = new RelayCommand(_ => Zoom(0)) });
                menu.Items.Add(ZoomSubMenuModel);*/

                MenuItem AdvancedSubMenuModel = new() { Icon = "\uec7a", Header = "Advanced" };
                AdvancedSubMenuModel.Items.Add(new MenuItem { Icon = "\uec7a", Header = "Inspect", Command = new RelayCommand(_ => DevTools()) });
                AdvancedSubMenuModel.Items.Add(new MenuItem { Icon = "\ue943", Header = "View source", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab($"view-source:{e.FrameUrl}", true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private, Tab.TabGroup)) });
                BrowserMenu.Items.Add(AdvancedSubMenuModel);
            }
            BrowserMenu.PlacementTarget = WebView?.Control;
            BrowserMenu.IsOpen = true;
        }

        private void WebView_AuthenticationRequested(object? sender, WebAuthenticationRequestedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CredentialsDialogWindow _CredentialsDialogWindow = new($"Sign in to {Utils.FastHost(e.Url)}", "\uec19");
                _CredentialsDialogWindow.Topmost = true;
                if (_CredentialsDialogWindow.ShowDialog().ToBool())
                {
                    e.Username = _CredentialsDialogWindow.Username;
                    e.Password = _CredentialsDialogWindow.Password;
                }
                else
                    e.Cancel = true;
            });
        }

        private async void WebView_FaviconChanged(object? sender, string e)
        {
            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("Favicons")))
                SetIcon(await App.Instance.SetIcon(e, Address, Private));
        }

        public async void UnFocus()
        {
            OmniBoxFastTimer?.Stop();
            OmniBoxSmartTimer?.Stop();
            //SLBr seems to freeze when switching from a loaded tab with devtools to an unloaded tab
            //DevTools(true);
            if (App.Instance.LiteMode)
            {
                if (WebView != null && WebView.Engine == WebEngineType.ChromiumEdge && WebView.IsBrowserInitialized)
                {
                    WebView?.CallDevToolsAsync("Page.setWebLifecycleState", new
                    {
                        state = "frozen"
                    });
                }
            }
            if (WebView2DevToolsHWND != IntPtr.Zero)
                UpdateDevToolsPosition();
            /*else
            {
                if (WebView != null && WebView.IsBrowserInitialized && Tab.Preview == null)// && Tab == Tab.ParentWindow.GetTab())
                {
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        BitmapImage _BitmapImage = new();

                        var Width = (int)CoreContainerSizeEmulator.ActualWidth;
                        var Height = (int)CoreContainerSizeEmulator.ActualHeight;
                        using (MemoryStream Stream = new(await WebView.TakeScreenshotAsync(WebScreenshotFormat.JPEG, new Size { Height = Height, Width = Width })))
                        {
                            _BitmapImage.BeginInit();
                            _BitmapImage.DecodePixelWidth = 275;
                            _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            _BitmapImage.StreamSource = Stream;
                            _BitmapImage.EndInit();
                            _BitmapImage.Freeze();
                        }
                        Tab.Preview = _BitmapImage;
                        SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);
                    }, DispatcherPriority.Render);
                }
            }*/
        }

        public void ReFocus()
        {
            if (Tab.IsUnloaded)
            {
                InitializeBrowserComponent();
                if (Address.StartsWith("slbr://") && App.CustomPageOverlays.TryGetValue(Utils.FastHost(Address), out Type OverlayType))
                {
                    WebView?.Control?.Visibility = Visibility.Collapsed;
                    if (PageOverlay != null && PageOverlay.GetType() != OverlayType)
                    {
                        CoreContainer.Children.Remove(PageOverlayControl);
                        PageOverlay?.Dispose();
                        PageOverlay = null;
                    }
                    if (PageOverlay == null)
                    {
                        PageOverlay = (IPageOverlay)Activator.CreateInstance(OverlayType, this)!;
                        CoreContainer.Children.Add(PageOverlayControl);
                    }
                    PageOverlayControl?.Visibility = Visibility.Visible;
                }
                else
                {
                    WebView?.Control?.Visibility = Visibility.Visible;//VIDEO
                    if (PageOverlay != null)
                    {
                        CoreContainer.Children.Remove(PageOverlayControl);
                        PageOverlay?.Dispose();
                        PageOverlay = null;
                    }
                }
            }
            else
            {
                //Looks like both Chromium engines have issues now
                if (WebView?.Engine == WebEngineType.ChromiumEdge)
                {
                    //Warning: WebView2 somehow forgets the auto dark mode after a while
                    SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);
                    if (WebView2DevToolsHWND != IntPtr.Zero)
                        UpdateDevToolsPosition();
                    if (App.Instance.LiteMode && WebView != null && WebView.IsBrowserInitialized)
                    {
                        WebView?.CallDevToolsAsync("Page.setWebLifecycleState", new
                        {
                            state = "active"
                        });
                    }
                }
                else if (WebView?.Engine == WebEngineType.Chromium)
                    SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);
            }
        }

        public async void LimitNetwork(int LatencyMs, double DownloadLimitMbps, double UploadLimitMbps)
        {
            //DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
            //_DevToolsClient.Network.EnableAsync();
            await WebView?.CallDevToolsAsync("Network.enable");
            //_DevToolsClient.Network.EmulateNetworkConditionsAsync(false, LatencyMs, DownloadThroughput, UploadThroughput, CefSharp.DevTools.Network.ConnectionType.Wifi);
            // Mbps to bytes per second

            //TODO: Switch to Network.emulateNetworkConditionsByRule for https://issues.chromium.org/issues/40434685
            //Network.emulateNetworkConditions is going to be deprecated
            await WebView?.CallDevToolsAsync("Network.emulateNetworkConditions", new
            {
                offline = false,
                latency = LatencyMs,
                downloadThroughput = DownloadLimitMbps * 125000,
                uploadThroughput = UploadLimitMbps * 125000,
                connectionType = "wifi",
            });
        }

        bool IsCustomTheme = false;
        bool DevToolsAdBlock = false;

        public async Task ToggleEfficientAdBlock(bool Boolean)
        {
            AdBlockToggleButton.IsEnabled = !Boolean;
            AdBlockContainer.ToolTip = Boolean ? "Whitelist is unavailable in efficient ad block mode." : string.Empty;
            if (Boolean && !DevToolsAdBlock)
            {
                await WebView?.CallDevToolsAsync("Network.enable");
                await WebView?.CallDevToolsAsync("Network.setBlockedURLs", new { urls = App.BlockedAdPatterns });
                DevToolsAdBlock = true;
            }
            else if (!Boolean && DevToolsAdBlock)
            {
                await WebView?.CallDevToolsAsync("Network.setBlockedURLs", new { urls = Array.Empty<string>() });
                await WebView?.CallDevToolsAsync("Network.disable");
                DevToolsAdBlock = false;
            }
        }

        WebAppManifest? CurrentWebAppManifest;
        string CurrentWebAppManifestUrl;

        public async Task<(bool Installable, string ManifestUrl)> IsInstallableAsync()
        {
            if (WebView != null && WebView.CanExecuteJavascript)
            {
                try
                {
                    string? Response = await WebView.EvaluateScriptAsync(Scripts.DetectPWA);
                    if (string.IsNullOrEmpty(Response))
                        return (false, string.Empty);

                    using var Document = JsonDocument.Parse(Response);
                    string Manifest = Document.RootElement.GetProperty("manifest").GetString();
                    bool ServiceWorker = Document.RootElement.GetProperty("service_worker").GetBoolean();

                    return (!string.IsNullOrEmpty(Manifest) && ServiceWorker, Manifest);
                }
                catch { }
            }
            return (false, string.Empty);
        }

        public bool CanUnload()
        {
            return (Muted || !AudioPlaying) && WebView != null && WebView.IsBrowserInitialized;
        }

        public async Task<bool> IsArticle()
        {
            if (WebView != null && WebView.IsBrowserInitialized && WebView.CanExecuteJavascript)
            {
                try
                {
                    var Response = await WebView?.EvaluateScriptAsync(Scripts.ArticleScript);
                    if (Response != null)
                        return bool.Parse(Response);
                }
                catch { return false; }
            }
            return false;
        }

        private void AdBlockToggleButton_Click(object sender, RoutedEventArgs e)
        {
            string Host = Utils.FastHost(Address);
            if (AdBlockToggleButton.IsChecked.ToBool())
                App.Instance.AdBlockAllowList.Remove(Host);
            else
                App.Instance.AdBlockAllowList.Add(Host);
        }

        async void BrowserLoadChanged(string Address, bool? IsLoading = null)
        {
            if (OmniBox.Text != Address)
            {
                OmniBox.Tag = Address;
                if (IsOmniBoxModifiable())
                {
                    if (Address.StartsWith("slbr://newtab"))
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Visible;
                        OmniBoxText = string.Empty;
                        OmniBox.Text = string.Empty;
                    }
                    else
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                        OmniBoxText = Address;
                        OmniBox.Text = Address;
                    }

                    OmniBoxIsDropdown = false;
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    SetOverlayDisplay(App.Instance.TrimURL, App.Instance.HomographProtection);
                }
            }
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = App.Instance.FavouriteColor;
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }

            if (Address.StartsWith("slbr://") && App.CustomPageOverlays.TryGetValue(Utils.FastHost(Address), out Type OverlayType))
            {
                WebView?.Control?.Visibility = Visibility.Collapsed;
                if (PageOverlay != null && PageOverlay.GetType() != OverlayType)
                {
                    CoreContainer.Children.Remove(PageOverlayControl);
                    PageOverlay?.Dispose();
                    PageOverlay = null;
                }
                if (PageOverlay == null)
                {
                    PageOverlay = (IPageOverlay)Activator.CreateInstance(OverlayType, this)!;
                    CoreContainer.Children.Add(PageOverlayControl);
                }
                PageOverlayControl?.Visibility = Visibility.Visible;
            }
            else
            {
                WebView?.Control?.Visibility = Visibility.Visible;//VIDEO
                if (PageOverlay != null)
                {
                    CoreContainer.Children.Remove(PageOverlayControl);
                    PageOverlay?.Dispose();
                    PageOverlay = null;
                }
            }
            bool IsHTTP = Utils.IsHttpScheme(Address);
            if (IsHTTP && App.Instance.AdBlock != 0)
            {
                string Host = Utils.FastHost(Address);
                AdBlockHostText.Text = Host;
                AdBlockToggleButton.IsChecked = !App.Instance.AdBlockAllowList.Has(Host);
                AdBlockContainer.Visibility = Visibility.Visible;
            }
            else
                AdBlockContainer.Visibility = Visibility.Collapsed;
            if (Translated)
                Translated = false;
            if (IsReaderMode)
                IsReaderMode = false;
            Tab.Preview = null;
            if (IsLoading != null)
            {
                SiteInformationIcon.FontFamily = App.Instance.IconFont;
                SiteInformationPopupIcon.FontFamily = App.Instance.IconFont;
                LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                bool IsLoadingBool = IsLoading.ToBool();
                if (App.Instance.AllowTranslateButton)
                    TranslateButton.IsEnabled = !IsLoadingBool;
                if (!IsLoadingBool)
                {
                    string SetSiteInfo = string.Empty;
                    if (WebViewManager.OverrideRequests.TryGetValue(Address, out RequestOverrideItem Item))
                    {
                        if (!string.IsNullOrEmpty(Item.Error))
                        {
                            if (Item.Error.StartsWith("Malware") || Item.Error.StartsWith("Potentially_Harmful_Application") || Item.Error.StartsWith("Social_Engineering") || Item.Error.StartsWith("Unwanted_Software"))
                                SetSiteInfo = "Danger";
                        }
                    }
                    if (string.IsNullOrEmpty(SetSiteInfo))
                    {
                        CertificateInfo.Visibility = Visibility.Collapsed;
                        if (IsHTTP)
                        {
                            if (WebView != null && WebView.IsBrowserInitialized)
                            {
                                if (WebView.IsSecure)
                                {
                                    SiteInformationCertificate.Visibility = Visibility.Visible;
                                    SetSiteInfo = "Secure";
                                    if (WebView is ChromiumWebView ChromiumView)
                                    {
                                        NavigationEntry _NavigationEntry = await ((ChromiumWebBrowser)ChromiumView.Control).GetVisibleNavigationEntryAsync();
                                        if (_NavigationEntry != null)
                                        {
                                            /*if (_NavigationEntry.HttpStatusCode == 0)
                                            {
                                                WebView?.Stop();
                                                WebView?.Navigate(WebView?.Address);
                                                return;
                                            }*/
                                            if (_NavigationEntry.HttpStatusCode == 418)
                                                SetSiteInfo = "Teapot";
                                            //TODO: Implement teapot easter egg on WebView2 & Trident web engines

                                            CertificateValidation.Text = _NavigationEntry.SslStatus.IsSecureConnection ? "Certificate is valid" : "Certificate is invalid";
                                            SslStatus _SSL = _NavigationEntry.SslStatus;
                                            if (_SSL.X509Certificate != null)
                                            {
                                                CertificateInfo.Visibility = Visibility.Visible;
                                                var IssuedTo = Utils.ParseCertificateIssue(_SSL.X509Certificate.Subject);
                                                IssueToCommonName.Text = IssuedTo.Item1;
                                                IssueToCompany.Text = IssuedTo.Item2;
                                                var IssuedBy = Utils.ParseCertificateIssue(_SSL.X509Certificate.Issuer);
                                                IssueByCommonName.Text = IssuedBy.Item1;
                                                IssueByCompany.Text = IssuedBy.Item2;
                                                CertificateStart.Text = _SSL.X509Certificate.NotBefore.Date.ToShortDateString();
                                                CertificateEnd.Text = _SSL.X509Certificate.NotAfter.Date.ToShortDateString();
                                            }
                                        }
                                    }
                                }
                                else
                                    SetSiteInfo = "Insecure";
                            }
                            else
                            {
                                if (Address.StartsWith("https:"))
                                    SetSiteInfo = "Secure";
                                else
                                    SetSiteInfo = "Insecure";
                            }
                        }
                        else
                        {
                            if (Address.StartsWith("file:"))
                                SetSiteInfo = "File";
                            else if (Address.StartsWith("slbr:"))
                                SetSiteInfo = "SLBr";
                            else if (Address.StartsWith("chrome-extension:"))
                                SetSiteInfo = "Extension";
                            else
                                SetSiteInfo = "Protocol";
                        }
                    }
                    switch (SetSiteInfo)
                    {
                        case "Secure":
                            SiteInformationIcon.Text = "\xE72E";
                            SiteInformationIcon.Foreground = App.Instance.LimeGreenColor;
                            SiteInformationText.Text = "Secure";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE72E";
                            SiteInformationPopupIcon.Foreground = App.Instance.LimeGreenColor;
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is secure";
                            break;
                        case "Insecure":
                            SiteInformationIcon.Text = "\xE785";
                            SiteInformationIcon.Foreground = App.Instance.RedColor;
                            SiteInformationText.Text = "Insecure";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE785";
                            SiteInformationPopupIcon.Foreground = App.Instance.RedColor;
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is insecure";
                            break;
                        case "File":
                            SiteInformationIcon.Text = "\xE8B7";
                            SiteInformationIcon.Foreground = App.Instance.NavajoWhiteColor;
                            SiteInformationText.Text = "File";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE8B7";
                            SiteInformationPopupIcon.Foreground = App.Instance.NavajoWhiteColor;
                            SiteInformationPopupText.Text = "Local or shared file";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "SLBr":
                            SiteInformationIcon.Text = "\u2603";
                            SiteInformationIcon.FontFamily = App.Instance.SLBrFont;
                            SiteInformationIcon.Foreground = App.Instance.SLBrColor;
                            SiteInformationText.Text = "SLBr";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\u2603";
                            SiteInformationPopupIcon.FontFamily = App.Instance.SLBrFont;
                            SiteInformationPopupIcon.Foreground = App.Instance.SLBrColor;
                            SiteInformationPopupText.Text = "Secure SLBr page";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Danger":
                            SiteInformationIcon.Text = "\xE730";
                            SiteInformationIcon.Foreground = App.Instance.RedColor;
                            SiteInformationText.Text = "Danger";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE730";
                            SiteInformationPopupIcon.Foreground = App.Instance.RedColor;
                            SiteInformationPopupText.Text = "Dangerous site";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Protocol":
                            SiteInformationIcon.Text = "\xE774";
                            SiteInformationIcon.Foreground = App.Instance.CornflowerBlueColor;
                            SiteInformationText.Text = "Protocol";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE774";
                            SiteInformationPopupIcon.Foreground = App.Instance.CornflowerBlueColor;
                            SiteInformationPopupText.Text = "Network protocol";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Extension":
                            SiteInformationIcon.Text = "\xEA86";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationText.Text = "Extension";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xEA86";
                            SiteInformationPopupIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationPopupText.Text = "Extension";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Teapot":
                            SiteInformationIcon.Text = "\xEC32";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationText.Text = "Teapot";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xEC32";
                            SiteInformationPopupIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationPopupText.Text = "I'm a teapot";
                            break;
                    }
                    LoadingStoryboard?.Seek(TimeSpan.Zero);
                    LoadingStoryboard?.Stop();
                    if (App.Instance.AllowReaderModeButton && IsHTTP)
                        ReaderModeButton.Visibility = (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed;
                    else
                        ReaderModeButton.Visibility = Visibility.Collapsed;
                    SiteInformationPopupButton.IsEnabled = true;
                    //TODO: Investigate movement to Unfocus for optimization
                    if (App.Instance.TabPreview && WebView != null && WebView.IsBrowserInitialized && Tab.Preview == null && Tab == Tab.ParentWindow.GetTab() && WebView?.Control?.Visibility != Visibility.Collapsed)
                    {
                        BitmapImage _BitmapImage = new();

                        var Width = (int)CoreContainerSizeEmulator.ActualWidth;
                        var Height = (int)CoreContainerSizeEmulator.ActualHeight;
                        using (MemoryStream Stream = new(await WebView.TakeScreenshotAsync(WebScreenshotFormat.JPEG, new Size { Height = Height, Width = Width })))
                        {
                            _BitmapImage.BeginInit();
                            _BitmapImage.DecodePixelWidth = 275;
                            _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            _BitmapImage.StreamSource = Stream;
                            _BitmapImage.EndInit();
                            _BitmapImage.Freeze();
                        }
                        Tab.Preview = _BitmapImage;
                        SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);
                    }
                }
                else if (SiteInformationText.Text != "Loading")
                {
                    SiteInformationIcon.Text = "\xF16A";
                    SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                    SiteInformationText.Text = "Loading";
                    LoadingStoryboard?.Begin();
                    SiteInformationPopupButton.IsEnabled = false;
                }
            }
        }

        private string PAddress;
        public string Address
        {
            get
            {
                if (WebView != null)
                    PAddress = WebView?.Address;
                return PAddress;
            }
            set
            {
                PAddress = value;
                WebView?.Address = value;
            }
        }
        private string PTitle;
        public string Title
        {
            get
            {
                if (WebView != null)
                    PTitle = WebView?.Title != null && WebView?.Title.Trim().Length > 0 ? WebView?.Title : Utils.CleanUrl(Address);
                return PTitle;
            }
            set
            {
                PTitle = value;
            }
        }
        public bool CanGoBack
        {
            get
            {
                return WebView?.CanGoBack ?? false;
            }
        }
        public bool CanGoForward
        {
            get
            {
                return WebView?.CanGoForward ?? false;
            }
        }
        public bool IsLoading
        {
            get
            {
                return WebView?.IsLoading ?? false;
            }
        }

        public void Unload()
        {
            SetAudioState(false);
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon")))
                SetIcon(App.Instance.UnloadedIcon, true);
            DisposeBrowserCore();
            Tab.IsUnloaded = true;
            Tab.ProgressBarVisibility = Visibility.Collapsed;
            Tab.Preview = null;
        }
        private void Browser_GotFocus(object sender, RoutedEventArgs e)
        {
            ReFocus();
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            WebView?.Back();
        }
        public void Forward()
        {
            if (!CanGoForward)
                return;
            WebView?.Forward();
        }
        public void Refresh(bool IgnoreCache = false, bool ClearCache = false)
        {
            if (!IsLoading)
                WebView?.Refresh(IgnoreCache, ClearCache);
            else
                Stop();
        }
        public void Stop()
        {
            WebView?.Stop();
        }

        public void Share(string? Url = null)
        {
            if (Uri.TryCreate(Url ?? Address, UriKind.Absolute, out Uri? _Uri))
            {
                if (Url != null)
                    Utils.Share(Tab.ParentWindow.WindowInterop.EnsureHandle(), "Shared link", _Uri);
                else
                    Utils.Share(Tab.ParentWindow.WindowInterop.EnsureHandle(), Title.Length != 0 ? Title : "Shared link", _Uri);
            }
        }

        public static void ActivatePopup(Popup popup)
        {
            DllUtils.SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(popup.Child)).Handle);
        }

        public async void Find(string Text, bool Forward = true, bool FindNext = false)
        {
            if (string.IsNullOrEmpty(Text))
            {
                try
                {
                    var Response = await WebView?.EvaluateScriptAsync("window.getSelection().toString();");
                    if (Response != null)
                        Text = Response.Trim('"');
                }
                catch { }
            }
            Text = Text.ReplaceLineEndings("");
            FindPopup.IsOpen = true;
            FindTextBox.Text = Text;
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
            PreviousFindButton.IsEnabled = Text.Length > 0;
            NextFindButton.IsEnabled = Text.Length > 0;
            WebView?.Find(Text, Forward, false, FindNext);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            string Value = ((Button)sender).ToolTip.ToString()!;
            if (Value == "Close")
                StopFind();
            else if (Value == "Previous")
                Find(FindTextBox.Text, false, true);
            else if (Value == "Next")
                Find(FindTextBox.Text, true, true);
        }

        private void FindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (FindTextBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                    Find(FindTextBox.Text, true, true);
                else
                    Find(FindTextBox.Text);
                PreviousFindButton.IsEnabled = true;
                NextFindButton.IsEnabled = true;
            }
            else
            {
                PreviousFindButton.IsEnabled = false;
                NextFindButton.IsEnabled = false;
                WebView?.StopFind();
            }
        }

        private void FindTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
        }

        public void StopFind()
        {
            FindPopup.IsOpen = false;
            WebView?.StopFind();
        }
        public void Navigate(string Url)
        {
            Address = Url;
        }

        bool ActiveSizeEmulation;
        private void SizeEmulator()
        {
            SizeEmulatorColumn1.Width = new GridLength(0);
            SizeEmulatorColumn2.Width = new GridLength(0);
            SizeEmulatorRow1.Height = new GridLength(0);
            SizeEmulatorRow2.Height = new GridLength(0);
            SizeEmulatorColumnSplitter1.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorColumnSplitter2.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorRowSplitter1.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorRowSplitter2.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            ActiveSizeEmulation = !ActiveSizeEmulation;
        }
        private static void OpenAsPopupBrowser(string Url)
        {
            new PopupBrowser(Url, -1, -1).Show();
        }
        private void SetSideBarDock(int DockID)
        {
            SideBarColumnLeft.Width = new GridLength(0);
            SideBarColumnRight.Width = new GridLength(0);
            SideBarRowBottom.Height = new GridLength(0);
            SideBarRowTop.Height = new GridLength(0);
            if (IsLoaded)
            {
                SideBarRowSplitterBottom.Visibility = Visibility.Collapsed;
                SideBarRowSplitterTop.Visibility = Visibility.Collapsed;
                SideBarColumnSplitterLeft.Visibility = Visibility.Collapsed;
                SideBarColumnSplitterRight.Visibility = Visibility.Collapsed;
            }
            if (!IsInitialized)
                DockID = 0;
            switch (DockID)
            {
                case 0://RIGHT
                    if (IsLoaded)
                        SideBarColumnSplitterRight.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 2);
                    Grid.SetRow(SideBar, 1);
                    SideBarColumnRight.MinWidth = 200;
                    SideBarColumnRight.Width = new GridLength(500);
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarRowTop.MinHeight = 0;
                    SideBarRowBottom.MinHeight = 0;
                    SideBar.BorderThickness = new Thickness(1, 0, 0, 0);
                    break;
                case 1://LEFT
                    if (IsLoaded)
                        SideBarColumnSplitterLeft.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 0);
                    Grid.SetRow(SideBar, 1);
                    SideBarColumnLeft.MinWidth = 200;
                    SideBarColumnLeft.Width = new GridLength(500);
                    SideBarColumnRight.MinWidth = 0;
                    SideBarRowTop.MinHeight = 0;
                    SideBarRowBottom.MinHeight = 0;
                    SideBar.BorderThickness = new Thickness(0, 0, 1, 0);
                    break;
                case 2://BOTTOM
                    if (IsLoaded)
                        SideBarRowSplitterBottom.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 1);
                    Grid.SetRow(SideBar, 2);
                    SideBarRowBottom.MinHeight = 200;
                    SideBarRowBottom.Height = new GridLength(350);
                    SideBarRowTop.MinHeight = 0;
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarColumnRight.MinWidth = 0;
                    SideBar.BorderThickness = new Thickness(0, 1, 0, 0);
                    break;
                case 3://TOP
                    if (IsLoaded)
                        SideBarRowSplitterTop.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 1);
                    Grid.SetRow(SideBar, 0);
                    SideBarRowTop.MinHeight = 200;
                    SideBarRowTop.Height = new GridLength(350);
                    SideBarRowBottom.MinHeight = 0;
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarColumnRight.MinWidth = 0;
                    SideBar.BorderThickness = new Thickness(0, 0, 0, 1);
                    break;
            }
            if (!IsInitialized)
            {
                SideBarColumnRight.MinWidth = 0;
                SideBarColumnRight.Width = new GridLength(0);
            }
        }

        bool IsUtilityContainerOpen;
        IWindowInfo SideBarWindowInfo;
        public HwndHoster DevToolsHost;
        public IntPtr WebView2DevToolsHWND = IntPtr.Zero;
        public void ToggleSideBar(bool ForceClose = false)
        {
            DevToolsToolBar.Visibility = Visibility.Collapsed;
            if (IsUtilityContainerOpen || ForceClose)
            {
                SideBarColumnLeft.MinWidth = 0;
                SideBarColumnRight.MinWidth = 0;
                SideBarRowBottom.MinHeight = 0;
                SideBarRowTop.MinHeight = 0;
                SideBarColumnLeft.Width = new GridLength(0);
                SideBarColumnRight.Width = new GridLength(0);
                SideBarRowBottom.Height = new GridLength(0);
                SideBarRowTop.Height = new GridLength(0);
                if (IsUtilityContainerOpen)
                {
                    SideBarCoreContainer.Children.Clear();
                    _NewsFeed = null;
                    if (WebView2DevToolsHWND != IntPtr.Zero)
                    {
                        App.Instance.WebView2DevTools.Remove(WebView2DevToolsHWND);
                        DllUtils.PostMessage(WebView2DevToolsHWND, DllUtils.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        WebView2DevToolsHWND = IntPtr.Zero;
                    }
                    if (DevToolsHost != null)
                    {
                        if (WebView is ChromiumWebView ChromiumView)
                            ((ChromiumWebBrowser)ChromiumView.Control)?.BrowserCore.CloseDevTools();
                        DevToolsHost.Dispose();
                        DevToolsHost = null;
                    }
                    SideBarWindowInfo?.Dispose();
                    SideBarWindowInfo = null;
                    IsUtilityContainerOpen = false;
                    GC.Collect();
                }
            }
            else
            {
                SetSideBarDock(3 - SideBarDockDropdown.SelectedIndex);
                IsUtilityContainerOpen = true;
            }
            SideBar.Visibility = IsUtilityContainerOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        public void DevTools(bool ForceClose = false, int XCoord = 0, int YCoord = 0)
        {
            if (!ForceClose)
            {
                if (IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
                {
                    ToggleSideBar(ForceClose);
                    return;
                }
                if (WebView.Engine == WebEngineType.Trident)
                {
                    LocalInfoBars.Add(new() { Title = "Inspector Unavailable", Description = "Trident webview does not support an inspector tool." });
                    return;
                }
            }
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                DevToolsToolBar.Visibility = Visibility.Visible;
                DevToolsHost = new HwndHoster();
                SideBarCoreContainer.Children.Add(DevToolsHost);
                Grid.SetColumn(DevToolsHost, 1);
                Grid.SetRow(DevToolsHost, 1);

                DevToolsHost.HorizontalAlignment = HorizontalAlignment.Stretch;
                DevToolsHost.VerticalAlignment = VerticalAlignment.Stretch;

                DevToolsHost.Loaded += (s, args) =>
                {
                    if (DevToolsHost != null)
                    {
                        if (WebView is ChromiumWebView ChromiumView)
                        {
                            SideBarWindowInfo = WindowInfo.Create();
                            SideBarWindowInfo.SetAsChild(DevToolsHost.Handle);
                            ((ChromiumWebBrowser)ChromiumView.Control)?.BrowserCore?.ShowDevTools(SideBarWindowInfo, XCoord, YCoord);
                        }
                        else if (WebView is ChromiumEdgeWebView EdgeWebView)
                        {
                            Dispatcher.BeginInvoke(async () =>
                            {
                                ((WebView2)EdgeWebView.Control).CoreWebView2.OpenDevToolsWindow();
                                await Task.Delay(600);
                                string DevToolsName = $"DevTools - {Utils.CleanUrl(Address, false, false, false, false, true)}";
                                DllUtils.EnumWindows((hWnd, lParam) =>
                                {
                                    DllUtils.GetWindowThreadProcessId(hWnd, out uint pid);
                                    Process _Process = Process.GetProcessById((int)pid);
                                    if (_Process.ProcessName.Contains("msedgewebview2", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (DllUtils.IsWindowVisible(hWnd) && DllUtils.GetWindowTextRaw(hWnd) == DevToolsName && !App.Instance.WebView2DevTools.Contains(hWnd))
                                        {
                                            WebView2DevToolsHWND = hWnd;
                                            return false;
                                        }
                                    }
                                    return true;
                                }, IntPtr.Zero);
                                if (WebView2DevToolsHWND != IntPtr.Zero)
                                {
                                    App.Instance.WebView2DevTools.Add(WebView2DevToolsHWND);

                                    int DevToolsWindowStyle = DllUtils.GetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_STYLE);
                                    DevToolsWindowStyle &= ~(DllUtils.WS_CAPTION | DllUtils.WS_THICKFRAME | DllUtils.WS_SYSMENU | DllUtils.WS_MINIMIZEBOX | DllUtils.WS_MAXIMIZEBOX);
                                    DevToolsWindowStyle |= DllUtils.WS_POPUP;
                                    DllUtils.SetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_STYLE, DevToolsWindowStyle);

                                    int DevToolsWindowExStyle = DllUtils.GetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_EXSTYLE);
                                    DevToolsWindowExStyle &= ~DllUtils.WS_EX_APPWINDOW;
                                    DevToolsWindowExStyle |= DllUtils.WS_EX_TOOLWINDOW;
                                    DllUtils.SetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_EXSTYLE, DevToolsWindowExStyle);
                                    
                                    UpdateDevToolsPosition();
                                    DevToolsHost.SizeChanged += (s, e) => UpdateDevToolsPosition();
                                }
                            });
                        }
                    }
                };
            }
            SideBar.Visibility = IsUtilityContainerOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateDevToolsPosition()
        {
            if (WebView2DevToolsHWND == IntPtr.Zero) return;
            if (Tab.ParentWindow.WindowState == WindowState.Minimized || Tab.ParentWindow.GetTab() != Tab)
            {
                DllUtils.SetWindowRgn(WebView2DevToolsHWND, DllUtils.CreateRectRgn(0, 0, 0, 0), true);
                DllUtils.ShowWindow(WebView2DevToolsHWND, DllUtils.SW_HIDE);
                return;
            }
            DllUtils.ShowWindow(WebView2DevToolsHWND, DllUtils.SW_SHOWNA);
            Point TopLeft = DevToolsHost.PointToScreen(new Point(0, 0));
            DllUtils.SetWindowPos(WebView2DevToolsHWND, new IntPtr(-1), (int)TopLeft.X - 7, (int)TopLeft.Y - 30, (int)DevToolsHost.ActualWidth + 14, (int)DevToolsHost.ActualHeight + 37, DllUtils.SWP_NOACTIVATE | DllUtils.SWP_SHOWWINDOW | DllUtils.SWP_FRAMECHANGED);
            DllUtils.SetWindowRgn(WebView2DevToolsHWND, DllUtils.CreateRectRgn(0, 30, (int)DevToolsHost.ActualWidth + 14, (int)DevToolsHost.ActualHeight + 37), true);
        }

        bool PIsReaderMode = false;
        bool IsReaderMode
        {
            get { return PIsReaderMode; }
            set
            {
                PIsReaderMode = value;
                if (value)
                    Dispatcher.BeginInvoke(() => ReaderModeButton.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.IndicatorColor));
                else
                    Dispatcher.BeginInvoke(() => ReaderModeButton.ClearValue(ForegroundProperty));
            }
        }
        public async void ToggleReaderMode()
        {
            IsReaderMode = !IsReaderMode;
            if (IsReaderMode)
                WebView?.ExecuteScript(Scripts.ReaderModeScript);
            else
                Refresh();
        }

        NewsPage? _NewsFeed;
        public void NewsFeed(bool ForceClose = false)
        {
            if (!ForceClose && IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
            {
                ToggleSideBar(ForceClose);
                return;
            }
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                DevToolsToolBar.Visibility = Visibility.Collapsed;
                _NewsFeed = new NewsPage(this);
                SideBarCoreContainer.Children.Add(_NewsFeed);
                Grid.SetColumn(_NewsFeed, 1);
                Grid.SetRow(_NewsFeed, 1);

                _NewsFeed.HorizontalAlignment = HorizontalAlignment.Stretch;
                _NewsFeed.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        bool Muted => WebView?.IsMuted ?? false;
        public void ToggleMute()
        {
            WebView?.IsMuted = !WebView.IsMuted;
            MuteMenuItem.Icon = Muted ? "\xe767" : "\xe74f";
            MuteMenuItem.Header = Muted ? "Unmute" : "Mute";
            SetAudioState(null);
        }

        public void FavouriteAction()
        {
            int FavouriteExistIndex = FavouriteExists(Address);
            if (FavouriteExistIndex != -1)
            {
                App.Instance.Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
            else if (!IsLoading)
            {
                DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Add Favourite",
                    new List<InputField>
                    {
                        new InputField { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Title },
                        new InputField { Name = "URL", IsRequired = true, Type = DialogInputType.Text, Value = Address },
                    },
                    "\ueb51"
                );
                _DynamicDialogWindow.Topmost = true;
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    string URL = _DynamicDialogWindow.InputFields[1].Value.Trim();
                    App.Instance.Favourites.Add(new Favourite() { Type = "url", Url = URL, Name = _DynamicDialogWindow.InputFields[0].Value });
                    if (URL == Address)
                    {
                        FavouriteButton.Content = "\xEB52";
                        FavouriteButton.Foreground = App.Instance.FavouriteColor;
                        FavouriteButton.ToolTip = "Remove from favourites";
                        Tab.FavouriteCommandHeader = "Remove from favourites";
                    }
                }
            }
        }

        public void SetFavouritesBarVisibility()
        {
            if (App.Instance.ShowFavouritesBar == 0)
            {
                if (App.Instance.Favourites.Count == 0)
                {
                    FavouriteScrollViewer.Margin = new Thickness(0);
                    FavouriteContainer.Height = 5;
                }
                else
                {
                    FavouriteScrollViewer.Margin = new Thickness(5);
                    FavouriteContainer.Height = 41.25f;
                }
            }
            else if (App.Instance.ShowFavouritesBar == 1)
            {
                FavouriteScrollViewer.Margin = new Thickness(5);
                FavouriteContainer.Height = 41.25f;
            }
            else if (App.Instance.ShowFavouritesBar == 2)
            {
                FavouriteScrollViewer.Margin = new Thickness(0);
                FavouriteContainer.Height = 5;
            }
        }

        static int FavouriteExists(string Url)
        {
            if (App.Instance.Favourites.Count == 0)
                return -1;
            return App.Instance.Favourites.ToList().FindIndex(0, i => i.Url == Url);
        }
        /*public void Zoom(int Delta)
        {
            if (WebView == null)
                return;
            if (Delta == 0)
                WebView?.ZoomFactor = 1;
            //{
                //MessageBox.Show(WebView?.ZoomFactor.ToString());
            //}
            //Chromium.ZoomResetCommand.Execute(null);
            else if (Delta > 0)
                WebView?.ZoomFactor += 0.1;
            //Chromium.ZoomInCommand.Execute(null);
            else if (Delta < 0)
                WebView?.ZoomFactor -= 0.1;
            //Chromium.ZoomOutCommand.Execute(null);
        }*/
        public async void Screenshot()
        {
            if (WebView == null)
                return;
            try
            {
                int _ScreenshotFormat = App.Instance.GlobalSave.GetInt("ScreenshotFormat");
                string FileExtension = "jpg";
                WebScreenshotFormat ScreenshotFormat = WebScreenshotFormat.JPEG;
                if (_ScreenshotFormat == 1)
                {
                    FileExtension = "png";
                    ScreenshotFormat = WebScreenshotFormat.PNG;
                }
                string ScreenshotPath = App.Instance.GlobalSave.Get("ScreenshotPath");
                DateTime CurrentTime = DateTime.Now;
                string Url = Path.Combine(ScreenshotPath, Regex.Replace($"{WebView?.Title} {CurrentTime.Day}-{CurrentTime.Month}-{CurrentTime.Year} {string.Format("{0:hh:mm tt}", DateTime.Now)}.{FileExtension}", "[^a-zA-Z0-9._ -]", string.Empty));
                File.WriteAllBytes(Url, await WebView?.TakeScreenshotAsync(ScreenshotFormat));
                SetDarkMode(App.Instance.CurrentTheme.DarkWebPage);
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
            catch { }
        }

        bool PrivateTranslate = false;
        bool Translated
        {
            get {  return PrivateTranslate; }
            set
            {
                PrivateTranslate = value;
                if (value)
                    Dispatcher.BeginInvoke(() => TranslateButton.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.IndicatorColor));
                else
                    Dispatcher.BeginInvoke(() => TranslateButton.ClearValue(ForegroundProperty));
            }
        }

        public async void Translate(bool Original = false)
        {
            if (Original)
            {
                TranslateButton.ClosePopup();
                if (Translated == true)
                    Refresh();
                return;
            }
            string Texts = await WebView.EvaluateScriptAsync(Scripts.GetTranslationText);
            List<string> TranslatedTexts = null;
            if (TranslateButton.Visibility == Visibility.Visible)
                TranslateButton.OpenPopup();
            await Dispatcher.BeginInvoke(() => TranslateLoadingPanel.Visibility = Visibility.Visible);
            List<string> AllTexts = JsonSerializer.Deserialize<List<string>>(Texts);
            string TargetLanguage = App.Instance.AllLocales.First(i => i.Value == TranslateComboBox.SelectedValue).Key;
            switch (App.Instance.GlobalSave.GetInt("TranslationProvider"))
            {
                case 0:
                    IEnumerable<List<string>> GBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = [];
                    List<Task<List<string>>> GBatchTasks = [];

                    foreach (List<string> Batch in GBatches)
                    {
                        GBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, SECRETS.GOOGLE_TRANSLATE_ENDPOINT))
                            {
                                TranslateRequest.Headers.Add("Origin", "https://www.google.com");
                                TranslateRequest.Headers.Add("Accept", "*/*");
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new object[] { new object[] { Batch, "auto", TargetLanguage }, "te_lib" }), Encoding.UTF8, "application/json+protobuf");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<object> Json = JsonSerializer.Deserialize<List<object>>(Data);
                                if (Json == null || Json.Count == 0)
                                    return new List<string>();
                                if (Json[0] is not JsonElement Element || Element.ValueKind != JsonValueKind.Array)
                                    return new List<string>();
                                return Element.EnumerateArray().Select(e => HttpUtility.HtmlDecode(e.GetString())).ToList()!;
                            }
                        }));
                    }

                    var GResults = await Task.WhenAll(GBatchTasks);
                    foreach (List<string> BatchResult in GResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                case 1:
                    IEnumerable<List<string>> MBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = [];
                    List<Task<List<string>>> MBatchTasks = [];

                    foreach (List<string> Batch in MBatches)
                    {
                        MBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new(HttpMethod.Post, string.Format(SECRETS.MICROSOFT_TRANSLATE_ENDPOINT, TargetLanguage)))
                            {
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(Batch), Encoding.UTF8, "application/json");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<string> Result = [];
                                try
                                {
                                    using JsonDocument Document = JsonDocument.Parse(Data);
                                    foreach (JsonElement Item in Document.RootElement.EnumerateArray())
                                    {
                                        if (Item.TryGetProperty("translations", out JsonElement TranslationsElement))
                                        {
                                            foreach (JsonElement TranslationElement in TranslationsElement.EnumerateArray())
                                            {
                                                if (TranslationElement.TryGetProperty("text", out JsonElement TextElement))
                                                    Result.Add(TextElement.GetString() ?? "");
                                            }
                                        }
                                    }
                                    return Result;
                                }
                                catch
                                {
                                    return new List<string>();
                                }
                            }
                        }));
                    }

                    var MResults = await Task.WhenAll(MBatchTasks);
                    foreach (List<string> BatchResult in MResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                case 2:
                    string SourceLanguage = "";
                    try
                    {
                        using (HttpRequestMessage LanguageDetectRequest = new(HttpMethod.Get, string.Format(SECRETS.YANDEX_LANGUAGE_DETECTION_ENDPOINT, $"{Utils.GenerateSID()}-0-0", HttpUtility.UrlEncode(AllTexts.First()))))
                        {
                            var Response = await App.MiniHttpClient.SendAsync(LanguageDetectRequest);
                            if (!Response.IsSuccessStatusCode)
                            {
                                Dispatcher.BeginInvoke(() => {
                                    TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                    LocalInfoBars.Add(new() { Title = "Translation Unavailable", Description = "Unable to translate website." });
                                });
                                return;
                            }
                            string Data = await Response.Content.ReadAsStringAsync();
                            using var Document = JsonDocument.Parse(Data);
                            if (Document.RootElement.TryGetProperty("lang", out JsonElement LanguageElement))
                                SourceLanguage = LanguageElement.GetString() ?? "en";
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(() => {
                            TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                            LocalInfoBars.Add(new() { Title = "Translation Unavailable", Description = "Unable to translate website." });
                        });
                        return;
                    }
                    IEnumerable<List<string>> Batches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 16).Select(g => g.Select(x => x.t).ToList());

                    TargetLanguage = TargetLanguage.Split('-').First();
                    string YandexUserAgent = UserAgentGenerator.BuildUserAgentFromProduct("YaBrowser/25.2.0.0");
                    TranslatedTexts = [];
                    List<Task<List<string>>> BatchTasks = [];

                    foreach (List<string> Batch in Batches)
                    {
                        BatchTasks.Add(Task.Run(async () =>
                        {
                            List<string> EncodedTexts = Batch.Select(t => "text=" + HttpUtility.UrlEncode(t)).ToList();
                            string TextParameters = string.Join("&", EncodedTexts);
                            using (HttpRequestMessage TranslateRequest = new(HttpMethod.Get, string.Format(SECRETS.YANDEX_ENDPOINT, $"{Utils.GenerateSID()}-0-0", $"{SourceLanguage}-{TargetLanguage}", TextParameters)))
                            {
                                TranslateRequest.Headers.Add("User-Agent", YandexUserAgent);
                                try
                                {
                                    var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                    Response.EnsureSuccessStatusCode();

                                    string Data = await Response.Content.ReadAsStringAsync();
                                    if (JsonDocument.Parse(Data).RootElement.TryGetProperty("text", out JsonElement TranslatedTexts))
                                        return TranslatedTexts.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                }
                                catch
                                {
                                    return new List<string>();
                                }
                            }
                            return new List<string>();
                        }));
                    }

                    var Results = await Task.WhenAll(BatchTasks);
                    foreach (List<string> BatchResult in Results)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                case 3:
                    TargetLanguage = TargetLanguage switch
                    {
                        "zh" => "zh-Hans",
                        "zh-CN" => "zh-Hans",
                        "zh-TW" => "zh-Hant",
                        "zh-HK" => "zh-Hant",
                        _ => TargetLanguage
                    };

                    IEnumerable<List<string>> LBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = [];
                    List<Task<List<string>>> LBatchTasks = [];

                    foreach (List<string> Batch in LBatches)
                    {
                        LBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new(HttpMethod.Post, SECRETS.LINGVANEX_ENDPOINT))
                            {
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new
                                {
                                    target = TargetLanguage,
                                    q = Batch
                                }), Encoding.UTF8, "application/json");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<string> Result = [];

                                try
                                {
                                    using var Document = JsonDocument.Parse(Data);
                                    if (Document.RootElement.TryGetProperty("translatedText", out JsonElement TranslatedText))
                                    {
                                        foreach (var item in TranslatedText.EnumerateArray())
                                            Result.Add(item.GetString() ?? "");
                                    }
                                }
                                catch { }
                                return Result;
                            }
                        }));
                    }

                    var LResults = await Task.WhenAll(LBatchTasks);
                    foreach (List<string> BatchResult in LResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
            }
            if (TranslatedTexts == null || TranslatedTexts.Count == 0)
            {
                Dispatcher.BeginInvoke(() => {
                    TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                    LocalInfoBars.Add(new() { Title = "Translation Unavailable", Description = "Unable to translate website." });
                });
                return;
            }
            WebView.ExecuteScript(string.Format(Scripts.SetTranslationText, JsonSerializer.Serialize(TranslatedTexts)));
            Translated = true;
            Dispatcher.BeginInvoke(() => {
                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                TranslateButton.ClosePopup();
            });
            
        }

        private void OmniBoxContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            SiteInformationText.Visibility = Visibility.Visible;
            OmniBoxHovered = true;
        }

        private void OmniBoxContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!OmniBoxFocused)
                SiteInformationText.Visibility = Visibility.Collapsed;
            OmniBoxHovered = false;
        }

        public void OmniBoxEnter()
        {
            SmartSuggestionCancellation?.Cancel();
            OmniBoxFastTimer?.Stop();
            OmniBoxSmartTimer?.Stop();
            string SearchUrl = App.Instance.DefaultSearchProvider.SearchUrl;
            if (OmniBoxOverrideSearch != null && !string.IsNullOrEmpty(OmniBoxOverrideSearch.SearchUrl))
                SearchUrl = OmniBoxOverrideSearch.SearchUrl;
            string Url = Utils.FilterUrlForBrowser(OmniBox.Text, SearchUrl);
            if (Url.StartsWith("javascript:"))
            {
                WebView?.ExecuteScript(Url.Substring(11));
                OmniBoxText = OmniBox.Tag.ToString();
                OmniBox.Text = OmniBox.Tag.ToString();
            }
            else if (!Utils.IsProgramUrl(Url))
                Address = Url;
            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
            {
                OmniBoxFastTimer?.Stop();
                OmniBoxSmartTimer?.Stop();
            }
            OmniBox.IsDropDownOpen = false;
            Keyboard.ClearFocus();
            WebView?.Control?.Focus();
            if (OmniBoxOverrideSearch != null)
            {
                OmniBoxStatus.Visibility = Visibility.Collapsed;
                OmniBoxOverrideSearch = null;
                if (OmniBox.Text.Length != 0)
                {
                    foreach (SearchProvider Search in App.Instance.SearchEngines)
                    {
                        if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                            OmniBoxStatusText.Text = $"Search {Search.Name}";
                            OmniBoxStatus.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                SetTemporarySiteInformation();
            }
        }

        private static bool IsIgnorableKey(Key Key)
        {
            if (Key >= Key.F1 && Key <= Key.F24)
                return true;
            return Key switch
            {
                Key.Escape or Key.PrintScreen or Key.Pause or Key.Scroll
                or Key.Insert or Key.Delete or Key.Home or Key.End
                or Key.PageUp or Key.PageDown or Key.Up or Key.Down
                or Key.Left or Key.Right or Key.CapsLock or Key.NumLock
                or Key.Tab or Key.LWin or Key.RWin
                or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt => true,
                _ => false
            };
        }

        SearchProvider? OmniBoxOverrideSearch;

        private void OmniBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            OmniBoxSelectionByMouse = false;
            if (e.Key == Key.Tab)
            {
                if (OmniBoxOverrideSearch != null)
                {
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = null;
                    if (OmniBox.Text.Length != 0)
                    {
                        foreach (SearchProvider Search in App.Instance.SearchEngines)
                        {
                            if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                            {
                                OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                                OmniBoxStatusText.Text = $"Search {Search.Name}";
                                OmniBoxStatus.Visibility = Visibility.Visible;
                                break;
                            }
                        }
                    }
                    SetTemporarySiteInformation();
                    ShowOmniBoxSuggestions();
                    e.Handled = true;
                }
                else if (OmniBoxStatus.Visibility == Visibility.Visible)
                {
                    OmniBoxText = string.Empty;
                    OmniBox.Text = string.Empty;
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    string SelectedProvider = OmniBoxStatus.Tag.ToString()!;
                    OmniBoxOverrideSearch = SelectedProvider.StartsWith("S") ? App.Instance.AllSystemSearchEngines[int.Parse(SelectedProvider[1..])] : App.Instance.SearchEngines[int.Parse(SelectedProvider)];
                    SetTemporarySiteInformation();
                    ShowOmniBoxSuggestions();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Back && OmniBox.Text.Length == 0)
            {
                if (OmniBoxOverrideSearch != null)
                {
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = null;
                    SetTemporarySiteInformation();
                    ShowOmniBoxSuggestions();
                    e.Handled = true;
                }
            }
        }

        private void OmniBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (OmniBox.Text.Trim().Length > 0)
                    OmniBoxEnter();
            }
            else
            {
                if (IsIgnorableKey(e.Key) || Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Windows)
                    return;
                LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                LoadingStoryboard?.Seek(TimeSpan.Zero);
                LoadingStoryboard?.Stop();
                SetTemporarySiteInformation();
                if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ShowOmniBoxSuggestions();
                    }, DispatcherPriority.Background);
                }
                if (OmniBox.IsDropDownOpen)
                {
                    OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
                    OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
                }
            }
        }
        public bool IsOmniBoxModifiable()
        {
            return !OmniBoxFocused;
        }
        public void SetTemporarySiteInformation()
        {
            SiteInformationIcon.FontFamily = App.Instance.IconFont;
            if (OmniBoxOverrideSearch == null)
            {
                if (OmniBox.Text.StartsWith("search:"))
                {
                    SiteInformationIcon.Text = "\xE721";
                    SiteInformationText.Text = "Search";
                    SiteInformationPopupButton.ToolTip = $"Searching: {OmniBox.Text.Substring(7).Trim()}";
                }
                else if (OmniBox.Text.StartsWith("domain:"))
                {
                    SiteInformationIcon.Text = "\xE71B";
                    SiteInformationText.Text = "Address";
                    SiteInformationPopupButton.ToolTip = $"Address: {OmniBox.Text.Substring(7).Trim()}";
                }
                else if (Utils.IsProgramUrl(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE756";
                    SiteInformationText.Text = "Program";
                    SiteInformationPopupButton.ToolTip = $"Open program: {OmniBox.Text}";
                }
                else if (Utils.IsCode(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE943";
                    SiteInformationText.Text = "Code";
                    SiteInformationPopupButton.ToolTip = $"Code: {OmniBox.Text}";
                }
                else if (Utils.IsUrl(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE71B";
                    SiteInformationText.Text = "Address";
                    SiteInformationPopupButton.ToolTip = $"Address: {OmniBox.Text}";
                }
                else
                {
                    SiteInformationIcon.Text = "\xE721";
                    SiteInformationText.Text = "Search";
                    SiteInformationPopupButton.ToolTip = $"Searching: {OmniBox.Text}";
                }
                SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
            }
            else
            {
                if (OmniBoxOverrideSearch.Host == "__Program__")
                {
                    switch (OmniBoxOverrideSearch.Name)
                    {
                        case "Tabs":
                            SiteInformationIcon.Text = "\xec6c";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            break;
                        case "History":
                            SiteInformationIcon.Text = "\xe81c";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            break;
                        case "Favourites":
                            SiteInformationIcon.Text = "\xeb51";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            break;
                        default:
                            SiteInformationIcon.Text = "\xED37";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            break;
                    }
                }
                else
                {
                    SiteInformationIcon.Text = "\xED37";
                    SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                }
                SiteInformationText.Text = OmniBoxOverrideSearch.Name;
                SiteInformationPopupButton.ToolTip = $"Searching {OmniBoxOverrideSearch.Name}: {OmniBox.Text.Trim()}";
            }
        }

        private void OmniBox_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (OmniBox.IsKeyboardFocusWithin)
            {
                OmniBoxOverlayText.Visibility = Visibility.Collapsed;
                OmniBox.Opacity = 1;

                OmniBoxBorder.BorderThickness = new Thickness(2);
                OmniBoxBorder.BorderBrush = (SolidColorBrush)FindResource("IndicatorBrush");
                OmniBoxFocused = true;
                OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                if (!OmniBoxHovered)
                    SiteInformationText.Visibility = Visibility.Visible;
            }
            else
            {
                OmniBox.IsDropDownOpen = false;
                try
                {
                    if (OmniBox.Text.Trim().Length == 0)
                        OmniBoxPlaceholder.Visibility = Visibility.Visible;
                    if (OmniBox.Text == OmniBox.Tag.ToString())
                        SetOverlayDisplay(App.Instance.TrimURL, App.Instance.HomographProtection);
                }
                catch { }
                OmniBoxBorder.BorderThickness = new Thickness(1);
                OmniBoxBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
                OmniBoxFocused = false;
                if (!OmniBoxHovered)
                    SiteInformationText.Visibility = Visibility.Collapsed;
            }
        }

        //Protection against homograph attacks https://www.xudongz.com/blog/2017/idn-phishing/
        //TODO: Add "Did you mean apple.com?", visit xn--80ak6aa92e.com in Chrome to see the error page.
        public void SetOverlayDisplay(bool TruncateURL = true, bool HighlightSuspicious = true)
        {
            OmniBoxOverlayText.Inlines.Clear();
            OmniBoxOverlayText.Visibility = Visibility.Visible;
            OmniBox.Opacity = 0;
            if (OmniBox.Tag is not string Url || string.IsNullOrWhiteSpace(Url) || Url.StartsWith("slbr://newtab"))
            {
                OmniBoxOverlayText.Text = string.Empty;
                return;
            }

            Url = App.Instance._IdnMapping.GetUnicode(Utils.CleanUrl(Url, false, TruncateURL, TruncateURL, TruncateURL && SiteInformationText.Text != "Danger", false));
            SolidColorBrush GrayBrush = (SolidColorBrush)FindResource("GrayBrush");

            string Scheme = Utils.GetScheme(Url);

            ReadOnlySpan<char> _Span = Url.AsSpan();

            switch (Scheme)
            {
                case "https":
                    if (TruncateURL)
                        _Span = _Span[8..];
                    else
                        OmniBoxOverlayText.Inlines.Add(new Run(Scheme) { Foreground = App.Instance.GreenColor });
                    break;
                case "http":
                    if (TruncateURL)
                        _Span = _Span[7..];
                    else
                        OmniBoxOverlayText.Inlines.Add(new Run(Scheme) { Foreground = App.Instance.RedColor });
                    break;
                case "slbr":
                    OmniBoxOverlayText.Inlines.Add(new Run(Scheme) { Foreground = App.Instance.SLBrColor });
                    break;
                case "file":
                    OmniBoxOverlayText.Inlines.Add(new Run(TruncateURL ? Url[8..] : Url) { Foreground = GrayBrush });
                    return;
                case "gopher":
                case "gemini":
                    OmniBoxOverlayText.Inlines.Add(new Run(Scheme) { Foreground = GrayBrush });
                    break;
                default:
                    OmniBoxOverlayText.Inlines.Add(new Run(Url) { Foreground = GrayBrush });
                    return;
            }

            int Protocol = _Span.IndexOf("://");
            if (Protocol >= 0)
            {
                OmniBoxOverlayText.Inlines.Add(new Run("://") { Foreground = GrayBrush });
                _Span = _Span[(Protocol + 3)..];
            }

            //www.com, m.com
            if (_Span.StartsWith("www.") && Utils.CanRemoveTrivialSubdomain(_Span[4..]))
            {
                if (!TruncateURL)
                    OmniBoxOverlayText.Inlines.Add(new Run("www.") { Foreground = GrayBrush });
                _Span = _Span[4..];
            }
            else if (_Span.StartsWith("m.") && Utils.CanRemoveTrivialSubdomain(_Span[2..]))
            {
                if (!TruncateURL)
                    OmniBoxOverlayText.Inlines.Add(new Run("m.") { Foreground = GrayBrush });
                _Span = _Span[2..];
            }

            int HostEnd = _Span.IndexOfAny('/', '?', '#');
            ReadOnlySpan<char> Host = HostEnd >= 0 ? _Span[..HostEnd] : _Span;

            SolidColorBrush FontBrush = (SolidColorBrush)FindResource("FontBrush");
            if (HighlightSuspicious)
            {
                //vvikipedia.com
                //xn--micrsoft-qbh.com
                //xn--l-7sba6dbr.com
                //xn--80ak6aa92e.com
                //xn--pple-zld.com
                int HostIndex = 0;
                StringBuilder HostBuffer = new();
                bool IsNormal = false;

                while (HostIndex < Host.Length)
                {
                    char _Char = Host[HostIndex];
                    if (_Char > 127 && char.IsLetter(_Char))
                    {
                        if (IsNormal)
                        {
                            if (HostBuffer.Length != 0)
                            {
                                OmniBoxOverlayText.Inlines.Add(new Run(HostBuffer.ToString()) { Foreground = IsNormal ? FontBrush : App.Instance.OrangeColor });
                                HostBuffer.Clear();
                            }
                            IsNormal = false;
                        }
                        HostBuffer.Append(_Char);
                        HostIndex++;
                        continue;
                    }

                    bool Matched = false;
                    foreach (string Confusable in App.URLConfusables)
                    {
                        //WARNING: Do not remove StringComparison.Ordinal
                        if (HostIndex + Confusable.Length <= Host.Length && Host.Slice(HostIndex, Confusable.Length).Equals(Confusable.AsSpan(), StringComparison.Ordinal))
                        {
                            if (IsNormal)
                            {
                                if (HostBuffer.Length != 0)
                                {
                                    OmniBoxOverlayText.Inlines.Add(new Run(HostBuffer.ToString()) { Foreground = IsNormal ? FontBrush : App.Instance.OrangeColor });
                                    HostBuffer.Clear();
                                }
                                IsNormal = false;
                            }
                            HostBuffer.Append(Confusable);
                            HostIndex += Confusable.Length;
                            Matched = true;
                            break;
                        }
                    }

                    if (!Matched)
                    {
                        if (!IsNormal)
                        {
                            if (HostBuffer.Length != 0)
                            {
                                OmniBoxOverlayText.Inlines.Add(new Run(HostBuffer.ToString()) { Foreground = IsNormal ? FontBrush : App.Instance.OrangeColor });
                                HostBuffer.Clear();
                            }
                            IsNormal = true;
                        }
                        HostBuffer.Append(_Char);
                        HostIndex++;
                    }
                }
                if (HostBuffer.Length != 0)
                {
                    OmniBoxOverlayText.Inlines.Add(new Run(HostBuffer.ToString()) { Foreground = IsNormal ? FontBrush : App.Instance.OrangeColor });
                    HostBuffer.Clear();
                }
            }
            else
                OmniBoxOverlayText.Inlines.Add(new Run(Host.ToString()) { Foreground = FontBrush });

            if (HostEnd >= 0)
            {
                ReadOnlySpan<char> _Path = _Span[HostEnd..];
                OmniBoxOverlayText.Inlines.Add(new Run(Utils.UnescapeDataString(_Path.ToString())) { Foreground = GrayBrush });
            }
        }

        bool OmniBoxFocused;
        bool OmniBoxHovered;

        Size MaximizedSize = Size.Empty;
        private void CoreContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size NewSize = new(CoreContainerSizeEmulator.ActualWidth, CoreContainerSizeEmulator.ActualHeight);
            if (MaximizedSize == Size.Empty)
                MaximizedSize = NewSize;
            Size Percentage = new(NewSize.Width / MaximizedSize.Width, NewSize.Height / MaximizedSize.Height);

            SizeEmulatorColumn1.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorColumn2.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorRow1.MaxHeight = 400 * Percentage.Height;
            SizeEmulatorRow2.MaxHeight = 400 * Percentage.Height;

            SizeEmulatorColumn1.Width = new GridLength(0);
            SizeEmulatorColumn2.Width = new GridLength(0);
            SizeEmulatorRow1.Height = new GridLength(0);
            SizeEmulatorRow2.Height = new GridLength(0);
        }

        public async void SetAppearance(Theme _Theme)
        {
            SetFavouritesBarVisibility();
            HomeButton.Visibility = App.Instance.AllowHomeButton ? Visibility.Visible : Visibility.Collapsed;
            QRButton.Visibility = App.Instance.AllowQRButton ? Visibility.Visible : Visibility.Collapsed;
            WebEngineButton.Visibility = App.Instance.AllowWebEngineButton ? Visibility.Visible : Visibility.Collapsed;
            if (!IsLoading)
                TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton && !Address.StartsWith("slbr:") ? Visibility.Visible : Visibility.Collapsed;

            if (WebView != null && WebView.IsBrowserInitialized)
            {
                SetDarkMode(_Theme.DarkWebPage);
                ReaderModeButton.Visibility = App.Instance.AllowReaderModeButton ? (WebView.CanExecuteJavascript && (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed) : Visibility.Collapsed;
            }
            else
                ReaderModeButton.Visibility = Visibility.Collapsed;
            _NewsFeed?.ApplyTheme(_Theme);

            if (App.Instance.ShowExtensionButton == 0)
                ExtensionsButton.Visibility = App.Instance.Extensions.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (App.Instance.ShowExtensionButton == 1)
                ExtensionsButton.Visibility = Visibility.Visible;
            else
                ExtensionsButton.Visibility = Visibility.Collapsed;

            if (App.Instance.TabAlignment == 1)
            {
                WebContainer.Margin = new Thickness(App.Instance.VerticalTabWidth, 0, 0, 0);
                WebContainerBorder.BorderThickness = new Thickness(1, 0, 0, 0);
                NewTabButton.Visibility = Visibility.Visible;
            }

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        private bool Disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                if (Disposing)
                {
                    DisposeBrowserCore();
                    LoadingStoryboard.Remove();
                    DownloadsPopup.ItemsSource = null;
                    FavouritesPanel.ItemsSource = null;
                    FavouriteListMenu.Collection = null;
                    HistoryListMenu.Collection = null;
                    ExtensionsMenu.ItemsSource = null;
                    InfoBarList.ItemsSource = null;
                }
                Disposed = true;
            }
        }

        ~Browser()
        {
            Dispose(false);
        }

        public void DisposeBrowserCore()
        {
            SmartSuggestionCancellation?.Cancel();
            OmniBoxFastTimer?.Stop();
            OmniBoxSmartTimer?.Stop();

            ToggleSideBar(true);
            CoreContainer.Children.Clear();
            SideBarCoreContainer.Children.Clear();

            if (WebView != null)
            {
                IWebView DisposingWebView = WebView;
                WebView = null;
                //WARNING: Prevent renavigation.
                if (DisposingWebView.IsBrowserInitialized)
                    Address = DisposingWebView.Address;
                DisposingWebView?.IsBrowserInitializedChanged -= WebView_IsBrowserInitializedChanged;
                //DisposingWebView?.Control.PreviewMouseWheel -= Chromium_PreviewMouseWheel;

                DisposingWebView?.FaviconChanged -= WebView_FaviconChanged;
                DisposingWebView?.AuthenticationRequested -= WebView_AuthenticationRequested;
                DisposingWebView?.BeforeNavigation -= WebView_BeforeNavigation;
                DisposingWebView?.ContextMenuRequested -= WebView_ContextMenuRequested;
                DisposingWebView?.ExternalProtocolRequested -= WebView_ExternalProtocolRequested;
                //DisposingWebView?.FindResult -= WebView_FindResult;
                DisposingWebView?.FrameLoadStart -= WebView_FrameLoadStart;
                DisposingWebView?.FullscreenChanged -= WebView_FullscreenChanged;
                DisposingWebView?.JavaScriptMessageReceived -= WebView_JavaScriptMessageReceived;
                DisposingWebView?.LoadingStateChanged -= WebView_LoadingStateChanged;
                DisposingWebView?.NavigationError -= WebView_NavigationError;
                DisposingWebView?.NewTabRequested -= WebView_NewTabRequested;
                DisposingWebView?.PermissionRequested -= WebView_PermissionRequested;
                DisposingWebView?.ResourceLoaded -= WebView_ResourceLoaded;
                DisposingWebView?.ResourceRequested -= WebView_ResourceRequested;
                //DisposingWebView?.ResourceResponded -= WebView_ResourceResponded;
                DisposingWebView?.ScriptDialogOpened -= WebView_ScriptDialogOpened;
                DisposingWebView?.StatusMessage -= WebView_StatusMessage;
                DisposingWebView?.TitleChanged -= WebView_TitleChanged;

                DisposingWebView?.Dispose();
            }
            PageOverlay?.Dispose();
            PageOverlay = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void InspectorDockDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Action(Actions.SetSideBarDock, (3 - SideBarDockDropdown.SelectedIndex).ToString());
        }

        private ObservableCollection<OmniSuggestion> Suggestions = [];
        private DispatcherTimer OmniBoxFastTimer;
        private DispatcherTimer OmniBoxSmartTimer;
        bool OmniBoxIsDropdown = false;

        private CancellationTokenSource? SmartSuggestionCancellation;

        public void ShowOmniBoxSuggestions()
        {
            string CurrentText = OmniBox.Text;
            if (Suggestions.FirstOrDefault()?.Display == CurrentText)
                return;
            OmniBoxStatus.Visibility = Visibility.Collapsed;
            Suggestions.Clear();
            string ProcessedText = CurrentText.Trim();
            if (!string.IsNullOrEmpty(ProcessedText))
            {
                SolidColorBrush Color = (SolidColorBrush)FindResource("FontBrush");
                SolidColorBrush LinkColor = (SolidColorBrush)FindResource("IndicatorBrush");
                string FirstType = App.GetMiniSearchType(ProcessedText);
                if (FirstType == "W")
                {
                    Suggestions.Add(App.GenerateSuggestion(ProcessedText, FirstType, LinkColor, "- Visit", null, OmniBoxOverrideSearch));
                    Suggestions.Add(App.GenerateSuggestion(ProcessedText, "S", Color, "- Search", $"search:{ProcessedText}", OmniBoxOverrideSearch));
                }
                else
                    Suggestions.Add(App.GenerateSuggestion(ProcessedText, FirstType, Color, "", null, OmniBoxOverrideSearch));
                try
                {
                    SmartSuggestionCancellation?.Cancel();
                    
                    OmniBoxSmartTimer?.Stop();
                    if (OmniBoxOverrideSearch?.Host == "__Program__")
                    {
                        ProcessedText = ProcessedText.ToLowerInvariant();
                        switch (OmniBoxOverrideSearch.Name)
                        {
                            case "Tabs":
                                List<BrowserTabItem> TabCollection = [];
                                foreach (MainWindow _Window in App.Instance.AllWindows)
                                {
                                    TabCollection.AddRange(_Window.Tabs.Where(i => i.Type == BrowserTabType.Navigation && i.Content != null && (i.Header.ToLowerInvariant().Contains(CurrentText) || (i.Content?.Address.ToLowerInvariant().Contains(CurrentText) ?? false))).ToList());
                                    if (TabCollection.Count >= 10)
                                        break;
                                }
                                foreach (BrowserTabItem Entry in TabCollection.Take(10))
                                    Suggestions.Add(App.GenerateSuggestion(Entry.Header, "T", LinkColor, $"- {Entry.Content?.Address}", Entry.Content?.Address, OmniBoxOverrideSearch, Entry.ID.ToString()));
                                break;
                            case "History":
                                List<ActionStorage> HistoryCollection = App.Instance.History.Where(i => (i.Name?.ToLowerInvariant().Contains(CurrentText) ?? false) || (i.Tooltip?.ToLowerInvariant().Contains(CurrentText) ?? false)).ToList();
                                foreach (ActionStorage Entry in HistoryCollection.Take(10))
                                    Suggestions.Add(App.GenerateSuggestion(Entry.Name, "W", LinkColor, $"- {Entry.Tooltip}", Entry.Tooltip, OmniBoxOverrideSearch));
                                break;
                            case "Favourites":
                                List<Favourite> FavouritesCollection = App.Instance.Favourites.Where(i => i.Type == "url" && ((i.Name?.ToLowerInvariant().Contains(CurrentText) ?? false) || (i.Url?.ToLowerInvariant().Contains(CurrentText) ?? false))).ToList();
                                foreach (Favourite Entry in FavouritesCollection.Take(10))
                                    Suggestions.Add(App.GenerateSuggestion(Entry.Name, "W", LinkColor, $"- {Entry.Url}", Entry.Url, OmniBoxOverrideSearch));
                                break;
                        }
                    }
                    else if (ProcessedText.Length <= 60)
                    {
                        OmniBoxFastTimer.Start();
                        if (OmniBoxOverrideSearch == null && bool.Parse(App.Instance.GlobalSave.Get("SmartSuggestions")))
                            OmniBoxSmartTimer.Start();
                    }
                }
                catch { }
                if (OmniBoxOverrideSearch == null)
                {
                    if (OmniBox.Text.StartsWith("@"))
                    {
                        foreach (SearchProvider Search in App.Instance.AllSystemSearchEngines)
                        {
                            if (OmniBox.Text.Length > 1)
                            {
                                if (Search.Name.StartsWith(OmniBox.Text[1..], StringComparison.OrdinalIgnoreCase))
                                {
                                    OmniSuggestion Suggestion = new() { ProviderOverride = Search, Text = $"@{Search.Name.ToLower()}", Display = $"@{Search.Name.ToLower()}", Color = Color, SubText = $"- Search {Search.Name}" };
                                    switch (Search.Name)
                                    {
                                        case "Tabs":
                                            Suggestion.Icon = "\xec6c";
                                            break;
                                        case "History":
                                            Suggestion.Icon = "\xe81c";
                                            break;
                                        case "Favourites":
                                            Suggestion.Icon = "\xeb51";
                                            break;
                                    }
                                    Suggestions.Add(Suggestion);
                                    OmniBoxStatus.Tag = "S" + App.Instance.AllSystemSearchEngines.IndexOf(Search);
                                    OmniBoxStatusText.Text = $"Search {Search.Name}";
                                    OmniBoxStatus.Visibility = Visibility.Visible;
                                    break;
                                }
                            }
                            else
                            {
                                OmniSuggestion Suggestion = new() { ProviderOverride = Search, Text = $"@{Search.Name.ToLower()}", Display = $"@{Search.Name.ToLower()}", Color = Color, SubText = $"- Search {Search.Name}" };
                                switch (Search.Name)
                                {
                                    case "Tabs":
                                        Suggestion.Icon = "\xec6c";
                                        break;
                                    case "History":
                                        Suggestion.Icon = "\xe81c";
                                        break;
                                    case "Favourites":
                                        Suggestion.Icon = "\xeb51";
                                        break;
                                }
                                Suggestions.Add(Suggestion);
                            }
                        }
                    }
                    if (OmniBoxStatus.Visibility == Visibility.Collapsed)
                    {
                        foreach (SearchProvider Search in App.Instance.SearchEngines)
                        {
                            if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                            {
                                OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                                OmniBoxStatusText.Text = $"Search {Search.Name}";
                                OmniBoxStatus.Visibility = Visibility.Visible;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                SmartSuggestionCancellation?.Cancel();
                OmniBoxFastTimer?.Stop();
                OmniBoxSmartTimer?.Stop();
                if (OmniBoxOverrideSearch != null && OmniBoxOverrideSearch.Host == "__Program__")
                {
                    SolidColorBrush LinkColor = (SolidColorBrush)FindResource("IndicatorBrush");
                    switch (OmniBoxOverrideSearch.Name)
                    {
                        case "Tabs":
                            break;
                        case "History":
                            List<ActionStorage> HistoryCollection = App.Instance.History.ToList();
                            foreach (ActionStorage Entry in HistoryCollection.Take(10))
                                Suggestions.Add(App.GenerateSuggestion(Entry.Name, "W", LinkColor, $"- {Entry.Tooltip}", Entry.Tooltip, OmniBoxOverrideSearch));
                            break;
                        case "Favourites":
                            List<Favourite> FavouritesCollection = App.Instance.Favourites.Where(i => i.Type == "url").ToList();
                            foreach (Favourite Entry in FavouritesCollection.Take(10))
                                Suggestions.Add(App.GenerateSuggestion(Entry.Name, "W", LinkColor, $"- {Entry.Url}", Entry.Url, OmniBoxOverrideSearch));
                            break;
                    }
                }
            }

            OmniBox.IsDropDownOpen = Suggestions.Count > 0;
            OmniBoxIsDropdown = true;

            OmniBox.Focus();
            if (OmniBox.IsDropDownOpen)
            {
                OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
                OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
            }
        }

        private bool OmniBoxSelectionByMouse;

        private void OmniBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!OmniBox.IsDropDownOpen || e.AddedItems.Count == 0)
                return;
            if (OmniBox.SelectedItem is not OmniSuggestion Suggestion)
                return;
            if (OmniBoxSelectionByMouse)
            {
                if (Suggestion.ProviderOverride == OmniBoxOverrideSearch)
                {
                    if (OmniBoxOverrideSearch != null && OmniBoxOverrideSearch.Host == "__Program__" && OmniBoxOverrideSearch.Name == "Tabs")
                    {
                        App.Instance.SwitchTab(int.Parse(Suggestion.Hidden));
                    }
                    else if (Suggestion.Text.Trim().Length > 0)
                    {
                        OmniBoxText = Suggestion.Text;
                        OmniBox.Text = Suggestion.Text;
                        OmniBoxEnter();
                    }
                }
                else
                {
                    OmniBoxText = string.Empty;
                    OmniBox.Text = string.Empty;
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = Suggestion.ProviderOverride;
                    SetTemporarySiteInformation();
                    ShowOmniBoxSuggestions();
                }
            }
            else
            {
                if (Suggestion.ProviderOverride != OmniBoxOverrideSearch)
                {
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = Suggestion.ProviderOverride;
                    SetTemporarySiteInformation();
                }
            }
            OmniBoxSelectionByMouse = false;
        }

        private async void OmniBoxFastTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxFastTimer?.Stop();
            if (!OmniBox.IsDropDownOpen)
                return;
            string CurrentText = OmniBox.Text.Trim();
            SolidColorBrush Color = (SolidColorBrush)FindResource("FontBrush");
            SolidColorBrush LinkColor = (SolidColorBrush)FindResource("IndicatorBrush");
            try
            {
                string SuggestionsUrl = string.Format(OmniBoxOverrideSearch?.SuggestUrl ?? App.Instance.DefaultSearchProvider.SuggestUrl, Uri.EscapeDataString(CurrentText));
                if (!string.IsNullOrEmpty(SuggestionsUrl))
                {
                    string ResponseText = await App.MiniHttpClient.GetStringAsync(SuggestionsUrl);
                    using (JsonDocument Document = JsonDocument.Parse(ResponseText))
                    {
                        foreach (JsonElement Suggestion in Document.RootElement[1].EnumerateArray())
                        {
                            string SuggestionStr = Suggestion.GetString();
                            string SuggestionType = App.GetMiniSearchType(SuggestionStr);
                            Suggestions.Add(App.GenerateSuggestion(SuggestionStr, SuggestionType, SuggestionType == "W" ? LinkColor : Color, "", null, OmniBoxOverrideSearch));
                        }
                    }
                }
            }
            catch { }
        }

        private async void OmniBoxSmartTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxSmartTimer?.Stop();
            if (!OmniBox.IsDropDownOpen)
                return;
            string Text = OmniBox.Text.Trim();
            string Type = App.GetSmartType(Text);
            if (Type == "None")
                return;
            SmartSuggestionCancellation?.Cancel();
            SmartSuggestionCancellation = new CancellationTokenSource();
            var Token = SmartSuggestionCancellation.Token;
            SolidColorBrush Color = (SolidColorBrush)FindResource("FontBrush");
            OmniSuggestion Suggestion = await App.Instance.GenerateSmartSuggestion(Text, Type, Color);
            if (!Token.IsCancellationRequested)
            {
                Suggestions.RemoveAt(0);
                Suggestions.Insert(0, Suggestion);
            }
        }

        private void OmniBox_DropDownOpened(object sender, EventArgs e)
        {
            WebView?.Control.Focusable = false;
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);// + 4 + 4
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
        }

        private void OmniBox_DropDownClosed(object sender, EventArgs e)
        {
            OmniBoxSelectionByMouse = false;
            WebView?.Control.Focusable = true;
        }

        int OmniBoxCaretIndex = 0;
        int OmniBoxSelectionStart = 0;
        int OmniBoxSelectionLength = 0;
        string OmniBoxText = string.Empty;

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Browser_Loaded;
            OmniTextBox = OmniBox.Template.FindName("PART_EditableTextBox", OmniBox) as TextBox;
            OmniTextBox.PreviewKeyDown += (sender, args) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    OmniBoxText = OmniTextBox.Text;
                    OmniBoxCaretIndex = OmniTextBox.CaretIndex;
                    OmniBoxSelectionStart = OmniTextBox.SelectionStart;
                    OmniBoxSelectionLength = OmniTextBox.SelectionLength;
                }), DispatcherPriority.Input);
            };

            OmniTextBox.GotKeyboardFocus += (sender, args) =>
            {
                args.Handled = true;
                OmniTextBox.Text = OmniBoxText;
                OmniTextBox.CaretIndex = OmniBoxCaretIndex;
                OmniTextBox.SelectionStart = OmniBoxSelectionStart;
                OmniTextBox.SelectionLength = OmniBoxSelectionLength;
                if (!OmniBoxIsDropdown)
                    OmniTextBox.SelectAll();
            };
            OmniBoxPopup = OmniBox.Template.FindName("Popup", OmniBox) as Popup;
            OmniBoxPopupDropDown = OmniBox.Template.FindName("DropDown", OmniBox) as Grid;
            OmniBoxPopupDropDown.PreviewMouseLeftButtonDown += (_, __) =>
            {
                OmniBoxSelectionByMouse = true;
            };
            OmniBox.ItemsSource = Suggestions;
            if (Address == "slbr://newtab" || Address == "about:blank")
            {
                Keyboard.Focus(OmniBox);
                OmniBox.Focus();
            }
        }

        Window ExtensionWindow;
        private void LoadExtensionPopup(object sender, RoutedEventArgs e)
        {
            //TODO: Use PopupBrowser instead
            Extension _Extension = App.Instance.Extensions.FirstOrDefault(i => i.ID == ((FrameworkElement)sender).Tag.ToString());
            if (_Extension == null)
                return;
            ExtensionWindow = new Window();
            ChromiumWebBrowser ExtensionBrowser = new(_Extension.Popup);
            ExtensionBrowser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            HwndSource _HwndSource = HwndSource.FromHwnd(new WindowInteropHelper(ExtensionWindow).EnsureHandle());
            _HwndSource.AddHook(WndProc);
            ExtensionBrowser.LoadingStateChanged += (s, args) =>
            {
                if (!args.IsLoading)
                    ExtensionBrowser.ExecuteScriptAsync(Scripts.ExtensionScript);
            };
            int trueValue = 0x01;
            int falseValue = 0x00;
            DllUtils.DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref App.Instance.CurrentTheme.DarkTitleBar ? ref trueValue : ref falseValue, Marshal.SizeOf(typeof(int)));
            DllUtils.DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            ExtensionBrowser.JavascriptMessageReceived += ExtensionBrowser_JavascriptMessageReceived;
            ExtensionBrowser.SnapsToDevicePixels = true;
            //ExtensionBrowser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            //ExtensionBrowser.DownloadHandler = App.Instance._DownloadHandler;
            //TODO
            ExtensionBrowser.AllowDrop = true;
            ExtensionBrowser.IsManipulationEnabled = true;
            ExtensionWindow.Content = ExtensionBrowser;
            ExtensionWindow.Title = _Extension.Name + " - Extension";
            ExtensionWindow.ResizeMode = ResizeMode.NoResize;
            ExtensionWindow.SizeChanged += ExtensionWindow_SizeChanged;
            ExtensionWindow.MaxHeight = 700;
            ExtensionWindow.MaxWidth = 700;
            ExtensionWindow.ShowDialog();
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DllUtils.WM_SYSCOMMAND:
                    int Command = wParam.ToInt32() & 0xfff0;
                    if (Command == DllUtils.SC_MOVE)
                        handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        private void ExtensionWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Rect WorkArea = SystemParameters.WorkArea;
            ExtensionWindow.Left = (WorkArea.Width - ExtensionWindow.Width) / 2 + WorkArea.Left;
            ExtensionWindow.Top = (WorkArea.Height - ExtensionWindow.Height) / 2 + WorkArea.Top;
        }

        private void ExtensionBrowser_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                dynamic data = e.Message;
                ExtensionWindow.Height = data.height;
                ExtensionWindow.Width = data.width;
            });
        }

        private void DownloadCancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Instance.Downloads.GetValueOrDefault(((FrameworkElement)sender).Tag.ToString())?.Cancel();
            }
            catch { }
        }

        private void DownloadOpenButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{App.Instance.Downloads.GetValueOrDefault(((FrameworkElement)sender).Tag.ToString()).FullPath}\"") { UseShellExecute = true });
        }

        private void FavouriteButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
            {
                if (e.ChangedButton == MouseButton.Left)
                    Navigate(Favourite.Url);
                else if (e.ChangedButton == MouseButton.Middle)
                    Tab.ParentWindow.NewTab(Favourite.Url, false, -1, Private);
            }
        }

        private void DownloadsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{App.Instance.GlobalSave.Get("DownloadPath")}\"") { UseShellExecute = true });
        }

        private void FavouriteAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem _MenuItem && _MenuItem.DataContext is Favourite Favourite)
            {
                string Action = _MenuItem.Header.ToString();
                if (Action == "Edit")
                {
                    List<InputField> Inputs = [
                        new InputField { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Name },
                    ];
                    if (Favourite.Type == "url")
                        Inputs.Add(new InputField { Name = "URL", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Url });
                    DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Edit Favourite", Inputs, "\ue70f");
                    _DynamicDialogWindow.Topmost = true;
                    if (_DynamicDialogWindow.ShowDialog() == true)
                    {
                        Favourite.Name = _DynamicDialogWindow.InputFields[0].Value;
                        if (Favourite.Type == "url")
                            Favourite.Url = _DynamicDialogWindow.InputFields[1].Value.Trim();
                    }
                }
                else if (Action == "Delete")
                    App.Instance.Favourites.Remove(Favourite);
            }
        }

        private void CloseInfoBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InfoBar Bar)
                CloseInfoBar(Bar);
        }

        private void CloseInfoBar(InfoBar Bar)
        {
            LocalInfoBars.Remove(Bar);
            App.Instance.InfoBars.Remove(Bar);
            if (Bar == ProprietaryCodecsInfoBar)
                ProprietaryCodecsInfoBar = null;
            else if (Bar == WaybackInfoBar)
                WaybackInfoBar = null;
        }

        InfoBar? ProprietaryCodecsInfoBar;
        InfoBar? WaybackInfoBar;
    }
}