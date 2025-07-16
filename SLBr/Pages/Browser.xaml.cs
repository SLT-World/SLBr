using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Page;
using CefSharp.SchemeHandler;
using CefSharp.Wpf.HwndHost;
using SLBr.Controls;
using SLBr.Handlers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using Windows.UI.Notifications;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public BrowserTabItem Tab;

        public ChromiumWebBrowser Chromium;
        public Settings _Settings;
        public Handlers.ResourceRequestHandlerFactory _ResourceRequestHandlerFactory;

        public bool Private = false;

        public Browser(string Url, BrowserTabItem _Tab = null, bool IsPrivate = false)//int _BrowserType = 0, 
        {
            InitializeComponent();
            Tab = _Tab != null ? _Tab : Tab.ParentWindow.GetTab(this);
            Private = IsPrivate;
            Tab.Icon = App.Instance.GetIcon(bool.Parse(App.Instance.GlobalSave.Get("Favicons")) ? Url : "", Private);
            Address = Url;
            SetAudioState(false);
            //BrowserType = _BrowserType;
            FavouritesPanel.ItemsSource = App.Instance.Favourites;
            FavouriteListMenu.ItemsSource = App.Instance.Favourites;
            HistoryListMenu.Collection = App.Instance.History;
            ExtensionsMenu.ItemsSource = App.Instance.Extensions;//ObservableCollection wasn't working for no reason so I turned it into a list
            /*BrowserEmulatorComboBox.Items.Add("Chromium");
            BrowserEmulatorComboBox.Items.Add("Edge");
            BrowserEmulatorComboBox.Items.Add("Internet Explorer");*/
            SetAppearance(App.Instance.CurrentTheme, bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")),App.Instance.GlobalSave.GetInt("ExtensionButton"), App.Instance.GlobalSave.GetInt("FavouritesBar"));

            if (!Private)
            {
                OmniBoxFastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                OmniBoxSmartTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                OmniBoxFastTimer.Tick += OmniBoxFastTimer_Tick;
                OmniBoxSmartTimer.Tick += OmniBoxSmartTimer_Tick;
            }
            //BrowserEmulatorComboBox.SelectionChanged += BrowserEmulatorComboBox_SelectionChanged;
            if (Cef.IsInitialized.ToBool())
                InitializeBrowserComponent();
        }

        public void InitializeBrowserComponent()
        {
            if (Chromium == null && Cef.IsInitialized.ToBool())
                CreateChromium(Address);
            else
                BrowserLoadChanged(Address);
        }

        TextBox OmniTextBox;
        Popup OmniBoxPopup;
        Grid OmniBoxPopupDropDown;
        bool AudioPlaying = false;

        public void SetAudioState(bool? _AudioPlaying = false)
        {
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
            {
                if (_AudioPlaying != null)
                    AudioPlaying = _AudioPlaying.ToBool();
                Tab.ProgressBarVisibility = !Muted && AudioPlaying ? Visibility.Collapsed : Visibility.Visible;
            }
            else
                Tab.ProgressBarVisibility = Visibility.Collapsed;
        }

        public void Favourites_CollectionChanged()
        {
            SetFavouritesBarVisibility();
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add from favourites";
                Tab.FavouriteCommandHeader = "Add from favourites";
            }
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            var Values = ((FrameworkElement)sender).Tag.ToString().Split(new string[] { "<,>" }, StringSplitOptions.None);
            Action((Actions)int.Parse(Values[0]), sender, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }
        public void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
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

                case Actions.CreateTab:
                    if (V2 == "Tab")
                    {
                        BrowserTabItem _Tab = Tab.ParentWindow.GetBrowserTabWithId(int.Parse(V1));
                        Tab.ParentWindow.NewTab(_Tab.Content.Address, true, Tab.ParentWindow.Tabs.IndexOf(_Tab) + 1);
                    }
                    else if (V2 == "Private")
                    {
                        Tab.ParentWindow.NewTab(V1, true, -1, true);
                    }
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
                    Favourite();
                    break;
                case Actions.OpenFileExplorer:
                    OpenFileExplorer(V1);
                    break;
                case Actions.OpenAsPopupBrowser:
                    OpenAsPopupBrowser(V1);
                    break;
                case Actions.SwitchUserPopup:
                    SwitchUserPopup();
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
                    if (Chromium != null && Chromium.IsBrowserInitialized)
                        Chromium.Print();
                    break;
                case Actions.Mute:
                    ToggleMute();
                    break;
                case Actions.Find:
                    Find("");
                    break;

                case Actions.ZoomIn:
                    Zoom(1);
                    break;
                case Actions.ZoomOut:
                    Zoom(-1);
                    break;
                case Actions.ZoomReset:
                    Zoom(0);
                    break;
            }
        }
        RequestHandler _RequestHandler;

        void CreateChromium(string Url)
        {
            if (Chromium != null && !Cef.IsInitialized.ToBool())
                return;
            Address = Url;
            Tab.IsUnloaded = true;
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.ProgressBarVisibility = Visibility.Collapsed;

            Chromium = new ChromiumWebBrowser(Url);
            Chromium.Address = Url;
            Chromium.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            //Chromium.JavascriptObjectRepository.Register("slbr", App.Instance._PublicJsObjectHandler, BindingOptions.DefaultBinder);
            Chromium.LifeSpanHandler = App.Instance._LifeSpanHandler;
            Chromium.DownloadHandler = App.Instance._DownloadHandler;
            _RequestHandler = new RequestHandler(this);
            Chromium.RequestHandler = _RequestHandler;
            Chromium.MenuHandler = App.Instance._ContextMenuHandler;
            Chromium.KeyboardHandler = App.Instance._KeyboardHandler;
            Chromium.JsDialogHandler = App.Instance._JsDialogHandler;
            Chromium.PermissionHandler = App.Instance._PermissionHandler;
            Chromium.DialogHandler = App.Instance._DialogHandler;
            _ResourceRequestHandlerFactory = new Handlers.ResourceRequestHandlerFactory(_RequestHandler);
            Chromium.ResourceRequestHandlerFactory = _ResourceRequestHandlerFactory;
            Chromium.DisplayHandler = new DisplayHandler(this);
            Chromium.AllowDrop = true;
            Chromium.IsManipulationEnabled = true;
            Chromium.UseLayoutRounding = true;
            Color _PrimaryColor = (Color)FindResource("PrimaryBrushColor");
            BrowserSettings _BrowserSettings = new BrowserSettings
            {
                BackgroundColor = (uint)((_PrimaryColor.A << 24) | (_PrimaryColor.R << 16) | (_PrimaryColor.G << 8) | (_PrimaryColor.B << 0)),
                //JavascriptCloseWindows = CefState.Disabled // Not working?
            };
            if (Private)
            {
                _BrowserSettings.LocalStorage = CefState.Disabled;
                _BrowserSettings.Databases = CefState.Disabled;
                _BrowserSettings.JavascriptAccessClipboard = CefState.Disabled;
                _BrowserSettings.JavascriptDomPaste = CefState.Disabled;
                RequestContextSettings ContextSettings = new RequestContextSettings
                {
                    PersistSessionCookies = false,
                    CachePath = null
                };
                RequestContext PrivateRequestContext = new RequestContext(ContextSettings); 
                
                string[] SLBrURLs = ["Credits", "License", "Downloads", "History", "Settings", "Tetris", "WhatsNew"];
                PrivateRequestContext.RegisterSchemeHandlerFactory("slbr", "newtab", new FolderSchemeHandlerFactory(App.Instance.ResourcesPath, hostName: "newtab", defaultPage: "Private.html"));
                foreach (string _Scheme in SLBrURLs)
                {
                    string Lower = _Scheme.ToLower();
                    PrivateRequestContext.RegisterSchemeHandlerFactory("slbr", Lower, new FolderSchemeHandlerFactory(App.Instance.ResourcesPath, hostName: Lower, defaultPage: $"{_Scheme}.html"));
                }
                PrivateRequestContext.RegisterSchemeHandlerFactory("gemini", "", new GeminiSchemeHandlerFactory());
                PrivateRequestContext.RegisterSchemeHandlerFactory("gopher", "", new GopherSchemeHandlerFactory());

                Chromium.RequestContext = PrivateRequestContext;
            }

            Chromium.BrowserSettings = _BrowserSettings;
            Chromium.IsBrowserInitializedChanged += Chromium_IsBrowserInitializedChanged;
            Chromium.FrameLoadStart += Chromium_FrameLoadStart;
            Chromium.LoadingStateChanged += Chromium_LoadingStateChanged;
            Chromium.ZoomLevelIncrement = 0.5f;
            Chromium.TitleChanged += Chromium_TitleChanged;
            Chromium.StatusMessage += Chromium_StatusMessage;
            Chromium.LoadError += Chromium_LoadError;
            Chromium.PreviewMouseWheel += Chromium_PreviewMouseWheel;
            Chromium.JavascriptMessageReceived += Chromium_JavascriptMessageReceived;
            CoreContainer.Visibility = Visibility.Collapsed;
            CoreContainer.Children.Add(Chromium);

            //BrowserEmulatorComboBox.SelectedItem = "Chromium";
        }

        public void UnFocus()
        {
            if (App.Instance.LiteMode && Chromium != null && Chromium.IsBrowserInitialized && !Chromium.IsLoading && Chromium.CanExecuteJavascriptInMainFrame)
            {
                try
                {
                    Utils.RunSafeFireAndForget(async () =>
                    {
                        DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
                        await _DevToolsClient.Page.SetWebLifecycleStateAsync(SetWebLifecycleStateState.Frozen);
                    });
                }
                catch { }
            }
        }

        public void ReFocus()
        {
            if (Tab.IsUnloaded)
            {
                InitializeBrowserComponent();
                if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
                {
                    if (Chromium != null)
                        Chromium.Visibility = Visibility.Collapsed;
                    if (_Settings == null)
                    {
                        _Settings = new Settings(this);
                        CoreContainer.Children.Add(_Settings);
                    }
                    _Settings.Visibility = Visibility.Visible;
                }
                else
                {
                    if (Chromium != null)
                        Chromium.Visibility = Visibility.Visible;
                    if (_Settings != null)
                    {
                        CoreContainer.Children.Remove(_Settings);
                        _Settings?.Dispose();
                        _Settings = null;
                    }
                }
            }
            if (App.Instance.LiteMode && Chromium != null && Chromium.IsBrowserInitialized && !Chromium.IsLoading && Chromium.CanExecuteJavascriptInMainFrame)
            {
                try
                {
                    Utils.RunSafeFireAndForget(async () =>
                    {
                        DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
                        //_DevToolsClient.Network.ResourceChangedPriority;
                        //_DevToolsClient.Network.RequestWillBeSent
                        /*ValidateSetWebLifecycleState(state);
            var dict = new System.Collections.Generic.Dictionary<string, object>();
            dict.Add("state", EnumToString(state));
            return _client.ExecuteDevToolsMethodAsync<DevToolsMethodResponse>("Page.setWebLifecycleState", dict);*/
                        //return _client.ExecuteDevToolsMethodAsync<DevToolsMethodResponse>("Network.clearBrowserCache", dict);
                        await _DevToolsClient.Page.SetWebLifecycleStateAsync(SetWebLifecycleStateState.Active);
                    });
                }
                catch { }
            }
        }

        private void Chromium_FrameLoadStart(object? sender, FrameLoadStartEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                if (Utils.IsHttpScheme(e.Url))
                {
                    e.Browser.ExecuteScriptAsync("window.close=function(){};");//Replacement for DoClose of LifeSpanHandler in RuntimeStyle Chrome
                    e.Browser.ExecuteScriptAsync(Scripts.ShiftContextMenuScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiTamper")))
                    {
                        if (bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect")))
                            e.Browser.ExecuteScriptAsync(Scripts.LateAntiDevtools);
                        if (bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu")))
                            e.Browser.ExecuteScriptAsync(Scripts.ForceContextMenuScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("TextSelection")))
                            e.Browser.ExecuteScriptAsync(Scripts.AllowInteractionScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter")))
                            e.Browser.ExecuteScriptAsync(Scripts.RemoveFilterCSS);
                        if (bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay")))
                            e.Browser.ExecuteScriptAsync(Scripts.RemoveOverlayCSS);
                    }
                }
                else if (e.Url.StartsWith("slbr:", StringComparison.Ordinal))
                    e.Browser.ExecuteScriptAsync(App.InternalJavascriptFunction);
            }
        }

        private void Chromium_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            if (e.Delta != 0)
                Zoom(e.Delta);
        }

        private void Chromium_LoadError(object? sender, LoadErrorEventArgs e)
        {
            if (e.ErrorCode == CefErrorCode.Aborted)
                return;
            Dispatcher.Invoke(() =>
            {
                _ResourceRequestHandlerFactory.RegisterHandler(e.FailedUrl, ResourceHandler.GetByteArray(App.Instance.GenerateCannotConnect(e.FailedUrl, e.ErrorCode, e.ErrorText), Encoding.UTF8), "text/html", 1, "");
                e.Frame.LoadUrl(e.FailedUrl);
            });
        }

        private async void Chromium_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (Chromium.IsBrowserInitialized)
            {
                CoreContainer.Visibility = Visibility.Visible;
                Tab.IsUnloaded = false;
                Tab.BrowserCommandsVisibility = Visibility.Visible;
                if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                    Tab.ProgressBarVisibility = Visibility.Visible;
                using (var DevToolsClient = Chromium.GetDevToolsClient())
                {
                    //DevToolsClient.Network.SetCookieControlsAsync(true, true, true);
                    //if (App.Instance.AdBlock == 2)
                    //    await ToggleEfficientAdBlock(DevToolsClient, App.Instance.AdBlockAllowList.Has(Utils.FastHost(Address)));
                    await ToggleEfficientAdBlock(DevToolsClient, App.Instance.AdBlock == 2);
                    try
                    {
                        //await DevToolsClient.Page.EnableAsync();
                        //await DevToolsClient.Page.SetAdBlockingEnabledAsync(App.Instance.AdBlock != 0);
                        if (!App.Instance.HighPerformanceMode)
                            await DevToolsClient.Page.SetPrerenderingAllowedAsync(false);
                    }
                    catch { }
                    //TODO: CefSharp.DevTools.DevToolsClientException: 'DevTools Client Error :Not attached to an active page'



                    //await DevToolsClient.Preload.DisableAsync();
                    //await DevToolsClient.Network.SetExtraHTTPHeadersAsync
                    if (!bool.Parse(App.Instance.GlobalSave.Get("BlockFingerprint")))
                    {
                        //navigator.userAgentData.getHighEntropyValues(["architecture","model","platform","platformVersion","uaFullVersion"]).then(ua =>{console.log(ua)});
                        await DevToolsClient.Emulation.SetUserAgentOverrideAsync(App.Instance.UserAgent, null, null, App.Instance.UserAgentData);
                    }
                }
            }
        }
        private void Chromium_StatusMessage(object? sender, StatusMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(e.Value))
                    StatusMessage.Text = e.Value;
                StatusBarPopup.IsOpen = !string.IsNullOrEmpty(e.Value);
            });
        }
        private void Chromium_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Tab.Header = Title;
            if (Tab == Tab.ParentWindow.Tabs[Tab.ParentWindow.TabsUI.SelectedIndex])
                Title = Title + App.Instance.Username == "Default" ? " - SLBr" : $" - {App.Instance.Username} - SLBr";
        }

        private void Chromium_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            IDictionary<string, object> Message = e.Message as IDictionary<string, object>;
            if (Message != null && Message.ContainsKey("type"))
            {
                switch (Message["type"].ToString())
                {
                    case "Media":
                        SetAudioState(Message["event"].ToString() == "1");
                        break;
                    case "OpenSearch":
                        App.Instance.SaveOpenSearch(Message["name"].ToString(), Message["url"].ToString());
                        break;
                    case "Internal":
                        switch (Message["function"])
                        {
                            case "Downloads":
                                Chromium.ExecuteScriptAsync($"internal.receive(\"downloads={JsonSerializer.Serialize(App.Instance.Downloads).Replace("\\", "\\\\").Replace("\"", "\\\"")}\")");
                                break;
                            case "History":
                                Chromium.ExecuteScriptAsync($"internal.receive(\"history={JsonSerializer.Serialize(App.Instance.History).Replace("\\", "\\\\").Replace("\"", "\\\"")}\")");
                                break;
                            case "Background":
                                string Url = "";
                                switch (App.Instance.GlobalSave.GetInt("HomepageBackground"))
                                {
                                    case 1:
                                        int BingBackground = App.Instance.GlobalSave.GetInt("BingBackground");
                                        if (BingBackground == 0)
                                        {
                                            try
                                            {
                                                XmlDocument doc = new XmlDocument();
                                                doc.LoadXml(new WebClient().DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                                                Url = "http://www.bing.com/" + doc.SelectSingleNode("/images/image/url").InnerText;
                                            }
                                            catch { }
                                        }
                                        else
                                            Url = "http://bingw.jasonzeng.dev/?index=random";
                                        break;

                                    case 2:
                                        Url = "http://picsum.photos/1920/1080?random";
                                        break;

                                    case 0:
                                        Url = App.Instance.GlobalSave.Get("CustomBackgroundImage");
                                        if (!Utils.IsHttpScheme(Url) && File.Exists(Url))
                                            Url = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(Url))}";
                                        break;
                                }
                                Chromium.ExecuteScriptAsync($"internal.receive(\"background={$"url('{Url}')".Replace("\\", "\\\\").Replace("\"", "\\\"")}\")");
                                break;
                            case "OpenDownload":
                                Process.Start(new ProcessStartInfo("explorer.exe", "/select, \"" + App.Instance.Downloads.GetValueOrDefault((int)Message["variable"]).FullPath + "\"") { UseShellExecute = true });
                                break;
                            case "CancelDownload":
                                Dispatcher.Invoke(() =>
                                {
                                    App.Instance._DownloadHandler.CancelDownload((int)Message["variable"]);
                                });
                                break;
                            case "ClearHistory":
                                Dispatcher.Invoke(App.Instance.History.Clear);
                                break;
                            case "Search":
                                Dispatcher.Invoke(() =>
                                {
                                    Address = Utils.FilterUrlForBrowser(Message["variable"].ToString(), App.Instance.DefaultSearchProvider.SearchUrl);
                                });
                                break;
                        }
                        break;
                    /*case "Extension":
                        App.Instance.LoadExtensions();
                        break;*/
                    case "Notification":
                        var Data = JsonSerializer.Deserialize<List<object>>((string)Message["data"]);
                        if (Data != null && Data.Count == 2)
                        {
                            var ToastXML = new Windows.Data.Xml.Dom.XmlDocument();
                            /*Uri uri = new Uri(e.Frame.Url);
                            string BaseURL = $"{uri.Scheme}://{uri.Host}";
                            var xml = @$"<toast>
                                <visual>
                                    <binding template=""ToastImageAndText04"">
                                        <image id=""1"" src=""{BaseURL}/{notificationWrapper.Body.Icon}""/>
                                        <text id=""1"">{notificationWrapper.Title}</text>
                                        <text id=""2"">{notificationWrapper.Body.Body}</text>
                                        <text id=""3"">{uri.Host}</text>
                                    </binding>
                                </visual>
                            </toast>";*/
                            ToastXML.LoadXml(@$"<toast>
    <visual>
        <binding template=""ToastText04"">
            <text id=""1"">{Data[0].ToString()}</text>
            <text id=""2"">{((IDictionary<string, object>)JsonSerializer.Deserialize<ExpandoObject>(((JsonElement)Data[1]).GetRawText()))["body"].ToString()}</text>
            <text id=""3"">{Utils.Host(e.Frame.Url, false)}</text>
        </binding>
    </visual>
</toast>");
                            ToastNotificationManager.CreateToastNotifier("SLBr").Show(new ToastNotification(ToastXML));
                        }
                        break;
                }
            }
        }

        bool IsCustomTheme = false;
        bool DevToolsAdBlock = false;

        public async Task ToggleEfficientAdBlock(DevToolsClient _DevToolsClient, bool Boolean)
        {
            AdBlockToggleButton.IsEnabled = !Boolean;
            AdBlockContainer.ToolTip = Boolean ? "Whitelist is unavailable in efficient ad block mode." : "";
            if (Boolean && !DevToolsAdBlock)
            {
                await _DevToolsClient.Network.EnableAsync();
                await _DevToolsClient.Network.SetBlockedURLsAsync(App.BlockedAdPatterns);
                DevToolsAdBlock = true;
            }
            else if (!Boolean && DevToolsAdBlock)
            {
                await _DevToolsClient.Network.DisableAsync();
                DevToolsAdBlock = false;
            }
        }

        private void Chromium_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                if (Chromium == null)
                    return;
                IsReaderMode = false;
                Address = Chromium.Address;
                Title = Chromium.Title;
                if (!Chromium.IsBrowserInitialized)
                    return;
                BackButton.IsEnabled = e.CanGoBack;
                ForwardButton.IsEnabled = e.CanGoForward;
                ReloadButton.Content = e.IsLoading ? "\xF78A" : "\xE72C";
                DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
                await _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(App.Instance.CurrentTheme.DarkWebPage);
                if (e.Browser != null && e.Browser.IsValid)
                {
                    if (bool.Parse(App.Instance.GlobalSave.Get("BlockFingerprint")))
                    {
                        switch (App.Instance.GlobalSave.Get("FingerprintLevel"))
                        {
                            case "Balanced":
                                await _DevToolsClient.Emulation.SetHardwareConcurrencyOverrideAsync(12);
                                break;
                            case "Random" or "Strict":
                                //https://data.firefox.com/dashboard/hardware
                                int[] FingerprintHardwareConcurrencies = { 1, 2, 4, 6, 8, 10, 12, 14 };
                                //https://source.chromium.org/chromium/chromium/deps/icu.git/+/chromium/m120:source/data/misc/metaZones.txt
                                string[] FingerprintTimeZones = { "Africa/Monrovia", "Europe/London", "America/New_York", "Asia/Seoul", "Asia/Singapore", "Asia/Taipei" };
                                await _DevToolsClient.Emulation.SetHardwareConcurrencyOverrideAsync(FingerprintHardwareConcurrencies[App.MiniRandom.Next(FingerprintHardwareConcurrencies.Length)]);
                                await _DevToolsClient.Emulation.SetTimezoneOverrideAsync(FingerprintTimeZones[App.MiniRandom.Next(FingerprintTimeZones.Length)]);
                                break;
                            default:
                                break;
                        }
                        //Device Memory 2, 3, 4, 6, 7, 8, 15, 16, 31, 32, 64
                        //await DevToolsClient.Emulation.SetIdleOverrideAsync(true, true); //A permission prompt appears
                        if (App.Instance.GlobalSave.Get("FingerprintLevel") != "Minimal")
                            Chromium.ExecuteScriptAsync(@"Object.defineProperty(navigator,'getBattery',{get:function(){return new Promise((resolve,reject)=>{reject('Battery API is disabled.');});}});Object.defineProperty(navigator,'connection',{get:function(){return null;}});");
                    }
                }

                //if (App.Instance.AdBlock == 2)
                //    await ToggleEfficientAdBlock(_DevToolsClient, App.Instance.AdBlockAllowList.Has(Utils.FastHost(Address)));
                BrowserLoadChanged(Address, e.IsLoading);
                //Chromium.GetDevToolsClient().Emulation.SetEmulatedMediaAsync(null, new List<MediaFeature>() { new MediaFeature() { Name = "prefers-reduced-motion", Value = "reduce" }, new MediaFeature() { Name = "prefers-reduced-data", Value = "reduce" } });

                /*using (var devToolsClient = Chromium.GetDevToolsClient())
                {
                    await devToolsClient.DOM.EnableAsync();
                    await devToolsClient.CSS.EnableAsync();

                    var mediaQueries = await devToolsClient.CSS.SetMediaTextAsync("prefers-reduced-motion", new SourceRange(), "reduce");
                }*/
                if (!e.IsLoading)
                {
                    if (!Address.StartsWith("slbr:", StringComparison.Ordinal))
                    {
                        if (Chromium.CanExecuteJavascriptInMainFrame)
                        {
                            if (Utils.IsHttpScheme(Address))
                            {
                                if (App.Instance.SkipAds)
                                {
                                    /*if (Address.AsSpan().IndexOf("youtube.com", StringComparison.Ordinal) >= 0)
                                    {
                                        Chromium.ExecuteScriptAsync(Scripts.YouTubeHideAdScript);
                                        if (Address.AsSpan().IndexOf("/watch?v=", StringComparison.Ordinal) >= 0)
                                            Chromium.ExecuteScriptAsync(Scripts.YouTubeSkipAdScript);
                                    }*/
                                    if (Address.AsSpan().IndexOf("youtube.com/watch?v=", StringComparison.Ordinal) >= 0)
                                        Chromium.ExecuteScriptAsync(Scripts.YouTubeSkipAdScript);
                                }
                                if (Address.AsSpan().IndexOf("chromewebstore.google.com/detail", StringComparison.Ordinal) >= 0)
                                    Chromium.ExecuteScriptAsync(Scripts.WebStoreScript);
                                //if (App.Instance.LiteMode)
                                //{
                                //Chromium.ExecuteScriptAsync(@"var style = document.createElement('style');style.type ='text/css';style.appendChild(document.createTextNode('*{ transition: none!important;-webkit-transition: none!important; }')); document.getElementsByTagName('head')[0].appendChild(style);");
                                //Chromium.ExecuteScriptAsync(@"Object.defineProperty(navigator.connection, 'saveData', { value: true, writable: false });");
                                //}
                                if (bool.Parse(App.Instance.GlobalSave.Get("WebNotifications")))
                                    /*Chromium.ExecuteScriptAsync(@"const nativeRequestPermission = Notification?.requestPermission?.bind(Notification);
    const nativePermission = Object.getOwnPropertyDescriptor(Notification, 'permission')?.get;
    class Notification {
    constructor(title, options = {}) {
        if(Notification.permission!=='granted'){throw new Error(""Notification permission not granted."");}
            this.onclick = null;
            this.onshow = null;
            this.onclose = null;
            this.onerror = null;
            if(typeof engine !== 'undefined' && typeof engine.postMessage === 'function') {
                let packageSet=new Set();packageSet.add(title).add(options);
                engine.postMessage({type:""Notification"",data:JSON.stringify([...packageSet])});
            }
            setTimeout(() => {if(typeof this.onshow==='function')this.onshow();},0);
            if(Notification.autoClose){setTimeout(()=>this.close(),Notification.autoClose);}
        }
        close(){if(typeof this.onclose==='function')this.onclose();}
        static requestPermission(callback){
            if(nativeRequestPermission){return nativeRequestPermission(callback);}
            else{if(callback)callback('granted');return Promise.resolve('granted');}
        }
        static get permission(){return nativePermission?nativePermission():'granted';}
    }
    Notification.autoClose = 7000;
    window.Notification = Notification;");*/
                                    Chromium.ExecuteScriptAsync(Scripts.NotificationPolyfill);
                                if (bool.Parse(App.Instance.GlobalSave.Get("EnhanceImage")))
                                    Chromium.ExecuteScriptAsync(Scripts.SharpenImageScript);
                                if (!Private && bool.Parse(App.Instance.GlobalSave.Get("OpenSearch")))
                                {
                                    string SiteHost = Utils.FastHost(Address);
                                    if (App.Instance.SearchEngines.Find(i => i.Host == SiteHost) == null)
                                        Chromium.ExecuteScriptAsync(Scripts.OpenSearchScript);
                                }
                            }
                            else if (Address.StartsWith("file:///", StringComparison.Ordinal))
                                Chromium.ExecuteScriptAsync(Scripts.FileScript);
                        }
                        if (!Private)
                            App.Instance.AddHistory(Address, Title);
                    }
                    if (bool.Parse(App.Instance.GlobalSave.Get("TabUnloading")))
                        Chromium.ExecuteScriptAsync(Scripts.TabUnloadScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme")))
                    {
                        JavascriptResponse? Task = await Chromium.EvaluateScriptAsync("document.querySelector('meta[name=\"theme-color\"]')?.content");
                        if (Task.Success && Task.Result is string HexColor)
                        {
                            try
                            {
                                IsCustomTheme = true;
                                Color PrimaryColor = Utils.ParseThemeColor(HexColor);
                                double a = 1 - (0.299 * PrimaryColor.R + 0.587 * PrimaryColor.G + 0.114 * PrimaryColor.B) / 255;
                                Theme SiteTheme = null;
                                if (a < 0.4)
                                {
                                    SiteTheme = new Theme("Temp", App.Instance.Themes[0]);
                                    SiteTheme.FontColor = Colors.Black;
                                    SiteTheme.DarkTitleBar = false;
                                    SiteTheme.DarkWebPage = false;
                                }
                                else if (a < 0.7)
                                {
                                    SiteTheme = new Theme("Temp", App.Instance.Themes[0]);
                                    SiteTheme.FontColor = Colors.White;
                                    SiteTheme.DarkTitleBar = false;
                                    SiteTheme.DarkWebPage = false;
                                    SiteTheme.SecondaryColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Min(255, PrimaryColor.R * 0.95f),
                                        (byte)Math.Min(255, PrimaryColor.G * 0.95f),
                                        (byte)Math.Min(255, PrimaryColor.B * 0.95f));
                                    SiteTheme.BorderColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Min(255, PrimaryColor.R * 0.90f),
                                        (byte)Math.Min(255, PrimaryColor.G * 0.90f),
                                        (byte)Math.Min(255, PrimaryColor.B * 0.90f));
                                    SiteTheme.GrayColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Min(255, PrimaryColor.R * 0.75f),
                                        (byte)Math.Min(255, PrimaryColor.G * 0.75f),
                                        (byte)Math.Min(255, PrimaryColor.B * 0.75f));
                                }
                                else
                                {
                                    SiteTheme = new Theme("Temp", App.Instance.Themes[1]);
                                    SiteTheme.FontColor = Colors.White;
                                    SiteTheme.DarkTitleBar = true;
                                    SiteTheme.DarkWebPage = true;
                                    SiteTheme.SecondaryColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Max(0, PrimaryColor.R * 1.25f),
                                        (byte)Math.Max(0, PrimaryColor.G * 1.25f),
                                        (byte)Math.Max(0, PrimaryColor.B * 1.25f));
                                    SiteTheme.BorderColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Max(0, PrimaryColor.R * 1.35f),
                                        (byte)Math.Max(0, PrimaryColor.G * 1.35f),
                                        (byte)Math.Max(0, PrimaryColor.B * 1.35f));
                                    SiteTheme.GrayColor = Color.FromArgb(PrimaryColor.A,
                                        (byte)Math.Max(0, PrimaryColor.R * 1.95f),
                                        (byte)Math.Max(0, PrimaryColor.G * 1.95f),
                                        (byte)Math.Max(0, PrimaryColor.B * 1.95f));
                                }
                                SiteTheme.PrimaryColor = PrimaryColor;
                                SetAppearance(SiteTheme, AllowHomeButton, AllowTranslateButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(SiteTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(SiteTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(SiteTheme.BorderColor);
                            }
                            catch
                            {
                                IsCustomTheme = false;
                                SetAppearance(App.Instance.CurrentTheme, AllowHomeButton, AllowTranslateButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                            }
                        }
                        else if (IsCustomTheme)
                        {
                            IsCustomTheme = false;
                            SetAppearance(App.Instance.CurrentTheme, AllowHomeButton, AllowTranslateButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                            TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                            _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                            _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                            _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                        }
                    }
                    if (App.Instance.AdBlock != 0)
                        await _DevToolsClient.Storage.RunBounceTrackingMitigationsAsync();
                }
            });
        }

        public bool CanUnload()
        {
            return (Muted || !AudioPlaying) && Chromium != null && Chromium.IsBrowserInitialized;
        }

        public async Task<bool> IsArticle()
        {
            if (Chromium != null && Chromium.IsBrowserInitialized && Chromium.CanExecuteJavascriptInMainFrame)
            {
                var Response = await Chromium.EvaluateScriptAsync(Scripts.ArticleScript);
                if (Response.Success && Response.Result is bool IsArticle)
                    return IsArticle;
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
            string OutputUrl = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(Address));
            if (OmniBox.Text != OutputUrl)
            {
                if (IsOmniBoxModifiable())
                {
                    if (Address.StartsWith("slbr://newtab", StringComparison.Ordinal))
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Visible;
                        OmniBox.Text = "";
                    }
                    else
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                        OmniBox.Text = OutputUrl;
                    }
                    OmniBoxIsDropdown = false;
                }
                OmniBox.Tag = Address;
            }
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add from favourites";
                Tab.FavouriteCommandHeader = "Add from favourites";
            }
            if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
            {
                if (Chromium != null)
                    Chromium.Visibility = Visibility.Collapsed;
                if (_Settings == null)
                {
                    _Settings = new Settings(this);
                    CoreContainer.Children.Add(_Settings);
                }
                _Settings.Visibility = Visibility.Visible;
            }
            else
            {
                if (Chromium != null)
                    Chromium.Visibility = Visibility.Visible;
                if (_Settings != null)
                {
                    CoreContainer.Children.Remove(_Settings);
                    _Settings?.Dispose();
                    _Settings = null;
                }
            }
            SiteInformationPopup.IsOpen = false;
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
            if (IsLoading != null)
            {
                Storyboard LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                if (!IsLoading.ToBool())
                {
                    string SetSiteInfo = "Process";
                    if (_ResourceRequestHandlerFactory.Handlers.TryGetValue(Address, out Handlers.ResourceRequestHandlerFactory.SLBrResourceRequestHandlerFactoryItem Item))
                    {
                        if (!string.IsNullOrEmpty(Item.Error))
                        {
                            if (Item.Error.StartsWith("Malware") || Item.Error.StartsWith("Potentially_Harmful_Application") || Item.Error.StartsWith("Social_Engineering") || Item.Error.StartsWith("Unwanted_Software"))
                                SetSiteInfo = "Danger";
                        }
                    }
                    if (SetSiteInfo == "Process")
                    {
                        if (IsHTTP)
                        {
                            SiteInformationCertificate.Visibility = Visibility.Visible;
                            if (Chromium != null && Chromium.IsBrowserInitialized)
                            {
                                CefSharp.NavigationEntry _NavigationEntry = await Chromium.GetVisibleNavigationEntryAsync();
                                if (_NavigationEntry != null)
                                {
                                    SetSiteInfo = _NavigationEntry.SslStatus.IsSecureConnection ? (_NavigationEntry.HttpStatusCode == 418 ? "Teapot" : "Secure") : "Insecure";
                                    CertificateValidation.Text = _NavigationEntry.SslStatus.IsSecureConnection ? "Certificate is valid" : "Certificate is invalid";
                                    await Cef.UIThreadTaskFactory.StartNew(delegate
                                    {
                                        SslStatus _SSL = Chromium.GetBrowserHost().GetVisibleNavigationEntry().SslStatus;
                                        Dispatcher.Invoke(() =>
                                        {
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
                                            else
                                                CertificateInfo.Visibility = Visibility.Collapsed;
                                        });
                                    });
                                }
                                else
                                {
                                    if (Address.StartsWith("https:", StringComparison.Ordinal))
                                        SetSiteInfo = "Secure";
                                    else
                                        SetSiteInfo = "Insecure";
                                    CertificateInfo.Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                if (Address.StartsWith("https:", StringComparison.Ordinal))
                                    SetSiteInfo = "Secure";
                                else
                                    SetSiteInfo = "Insecure";
                                CertificateInfo.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            if (Address.StartsWith("file:", StringComparison.Ordinal))
                                SetSiteInfo = "File";
                            else if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                                SetSiteInfo = "SLBr";
                            else if (Address.StartsWith("chrome-extension:", StringComparison.Ordinal))
                                SetSiteInfo = "Extension";
                            else
                                SetSiteInfo = "Protocol";
                        }
                    }

                    switch (SetSiteInfo)
                    {
                        case "Secure":
                            SiteInformationIcon.Text = "\xE72E";
                            SiteInformationIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            SiteInformationText.Text = $"Secure";
                            TranslateButton.Visibility = AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE72E";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is secure";
                            break;
                        case "Insecure":
                            SiteInformationIcon.Text = "\xE785";
                            SiteInformationIcon.Foreground = new SolidColorBrush(Colors.Red);
                            SiteInformationText.Text = $"Insecure";
                            TranslateButton.Visibility = AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE785";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(Colors.Red);
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is insecure";
                            break;
                        case "File":
                            SiteInformationIcon.Text = "\xE8B7";
                            SiteInformationIcon.Foreground = new SolidColorBrush(Colors.NavajoWhite);
                            SiteInformationText.Text = $"File";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Visible;
                            SiteInformationPopupIcon.Text = "\xE8B7";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(Colors.NavajoWhite);
                            SiteInformationPopupText.Text = $"Local or shared file";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "SLBr":
                            SiteInformationIcon.Text = "\xF8B0";
                            SiteInformationIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0092FF"));
                            SiteInformationText.Text = $"SLBr";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Visible;
                            SiteInformationPopupIcon.Text = "\xF8B0";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0092FF"));
                            SiteInformationPopupText.Text = $"Secure SLBr page";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Danger":
                            SiteInformationIcon.Text = "\xE730";
                            SiteInformationIcon.Foreground = new SolidColorBrush(Colors.Red);
                            SiteInformationText.Text = $"Danger";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE730";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(Colors.Red);
                            SiteInformationPopupText.Text = $"Dangerous site";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Protocol":
                            SiteInformationIcon.Text = "\xE774";
                            SiteInformationIcon.Foreground = new SolidColorBrush(Colors.CornflowerBlue);
                            SiteInformationText.Text = $"Protocol";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE774";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(Colors.CornflowerBlue);
                            SiteInformationPopupText.Text = $"Network protocol";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Extension":
                            SiteInformationIcon.Text = "\xEA86";
                            SiteInformationIcon.Foreground = new SolidColorBrush(App.Instance.GetTheme().FontColor);
                            SiteInformationText.Text = $"Extension";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xEA86";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(App.Instance.GetTheme().FontColor);
                            SiteInformationPopupText.Text = $"Extension";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Teapot":
                            SiteInformationIcon.Text = "\xEC32";
                            SiteInformationIcon.Foreground = new SolidColorBrush(App.Instance.GetTheme().FontColor);
                            SiteInformationText.Text = $"Teapot";
                            TranslateButton.Visibility = AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            //OpenFileExplorerButton.Visibility = Visibility.Collapsed;

                            SiteInformationPopupIcon.Text = "\xEC32";
                            SiteInformationPopupIcon.Foreground = new SolidColorBrush(App.Instance.GetTheme().FontColor);
                            SiteInformationPopupText.Text = "I'm a teapot";
                            break;
                    }
                    LoadingStoryboard?.Seek(TimeSpan.Zero);
                    LoadingStoryboard?.Stop();
                    if (AllowReaderModeButton && IsHTTP)
                        ReaderModeButton.Visibility = (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed;
                    else
                        ReaderModeButton.Visibility = Visibility.Collapsed;
                }
                else if (SiteInformationText.Text != "Loading")
                {
                    //SiteInformationIcon.Text = "\xED5A";
                    SiteInformationIcon.Text = "\xF16A";
                    SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                    SiteInformationText.Text = "Loading";
                    //SiteInformationPanel.ToolTip = "Loading";
                    //TranslateButton.Visibility = Visibility.Collapsed;
                    LoadingStoryboard?.Begin();
                }
            }
        }

        private string PAddress;
        public string Address
        {
            get
            {
                if (Chromium != null)
                    PAddress = Chromium.Address;
                return PAddress;
            }
            set
            {
                PAddress = value;
                if (Chromium != null)
                    Chromium.Address = value;
            }
        }
        private string PTitle;
        public string Title
        {
            get
            {
                if (Chromium != null)
                    PTitle = Chromium.Title != null && Chromium.Title.Trim().Length > 0 ? Chromium.Title : Utils.CleanUrl(Address);
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
                if (Chromium != null)
                    return Chromium.CanGoBack;
                else
                    return false;
            }
        }
        public bool CanGoForward
        {
            get
            {
                if (Chromium != null)
                    return Chromium.CanGoForward;
                else
                    return false;
            }
        }
        public bool IsLoading
        {
            get
            {
                if (Chromium != null)
                    return Chromium.IsLoading;
                else
                    return false;
            }
        }

        public void Unload()
        {
            SetAudioState(false);
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon")))
                Tab.Icon = App.Instance.UnloadedIcon;
            ToggleSideBar(true);
            if (Chromium != null && Chromium.IsBrowserInitialized)
                Address = Chromium.Address;
            CoreContainer.Children.Clear();
            SideBarCoreContainer.Children.Clear();
            Chromium?.Dispose();
            _Settings?.Dispose();
            _Settings = null;
            Chromium = null;
            Tab.IsUnloaded = true;
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.ProgressBarVisibility = Visibility.Collapsed;
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }
        private void Browser_GotFocus(object sender, RoutedEventArgs e)
        {
            ReFocus();
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            if (Chromium != null && Chromium.IsBrowserInitialized)
                Chromium.Back();
        }
        public void Forward()
        {
            if (!CanGoForward)
                return;
            if (Chromium != null && Chromium.IsBrowserInitialized)
                Chromium.Forward();
        }
        public void Refresh(bool IgnoreCache = false, bool ClearCache = false)
        {
            if (!IsLoading)
            {
                if (ClearCache)
                {
                    using (var DevToolsClient = Chromium.GetDevToolsClient())
                    {
                        DevToolsClient.Page.ClearCompilationCacheAsync();
                        DevToolsClient.Network.ClearBrowserCacheAsync();
                    }
                }
                Reload(IgnoreCache);
            }
            else
                Stop();
        }
        public void Reload(bool IgnoreCache = false)
        {
            if (Chromium != null && Chromium.IsBrowserInitialized)
                Chromium.Reload(IgnoreCache);
        }
        public void Stop()
        {
            if (Chromium != null && Chromium.IsBrowserInitialized)
                Chromium.Stop();
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ActivatePopup(Popup popup)
        {
            SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(popup.Child)).Handle);
        }

        public async void Find(string Text, bool Forward = true, bool FindNext = false)
        {
            if (Text == "")
            {
                var Response = await Chromium.EvaluateScriptAsync("window.getSelection().toString();");
                if (Response.Success && Response.Result != null)
                    Text = Response.Result.ToString();
            }
            FindPopup.IsOpen = true;
            FindTextBox.Text = Text;
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
            PreviousFindButton.IsEnabled = Text.Length > 0;
            NextFindButton.IsEnabled = Text.Length > 0;
            Chromium.Find(Text, Forward, false, FindNext);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            var Values = ((Button)sender).ToolTip.ToString().Split("<,>");
            if (Values[0] == "Close")
                StopFinding(true);
            else if (Values[0] == "Previous")
                Find(FindTextBox.Text, false, true);
            else if (Values[0] == "Next")
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
                Chromium.StopFinding(true);
            }
        }

        private void FindTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
        }

        public void StopFinding(bool ClearSelection = true)
        {
            FindPopup.IsOpen = false;
            Chromium.StopFinding(ClearSelection);
        }
        public void Navigate(string Url)
        {
            Address = Url;
        }
        public void OpenFileExplorer(string Url)
        {
            Process.Start(new ProcessStartInfo { Arguments = "/select, \"" + Url + "\"", FileName = "explorer.exe" });
        }

        private void SwitchUserPopup()
        {
            var infoWindow = new PromptDialogWindow("Prompt", $"Switch Profile", "Enter username for the profile to switch to:", "Default", "\xE77B");
            infoWindow.Topmost = true;
            if (infoWindow.ShowDialog() == true && infoWindow.UserInput != App.Instance.Username)
                Process.Start(new ProcessStartInfo() { FileName = App.Instance.ExecutablePath, Arguments = $"--user={infoWindow.UserInput}" });
            /*Process.Start(new ProcessStartInfo() {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Arguments = "/C choice /C Y /N /D Y /T 1 & START \"\" \"" + App.Instance.ExecutablePath + "\" --user=" + infoWindow.UserInput });*/
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
        private void OpenAsPopupBrowser(string Url)
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

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        bool IsUtilityContainerOpen;
        IWindowInfo SideBarWindowInfo;
        public HwndHoster DevToolsHost;
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
                    if (DevToolsHost != null)
                    {
                        Chromium.BrowserCore.CloseDevTools();
                        DestroyWindow(DevToolsHost.Handle);
                        DevToolsHost?.Dispose();
                        DevToolsHost = null;
                    }
                    SideBarWindowInfo?.Dispose();
                    IsUtilityContainerOpen = false;
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
            if (!ForceClose && IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
                ToggleSideBar(ForceClose);
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
                        SideBarWindowInfo = WindowInfo.Create();
                        SideBarWindowInfo.SetAsChild(DevToolsHost.Handle);
                        if (Chromium != null && Chromium.BrowserCore != null)
                            Chromium.BrowserCore.ShowDevTools(SideBarWindowInfo, XCoord, YCoord);
                    }
                };
            }
            SideBar.Visibility = IsUtilityContainerOpen ? Visibility.Visible : Visibility.Collapsed;
        }
        bool IsReaderMode = false;
        public void ToggleReaderMode()
        {
            IsReaderMode = !IsReaderMode;
            if (IsReaderMode)
            {
                Chromium.ExecuteScriptAsync(Scripts.ReaderScript);
                Chromium.ExecuteScriptAsync($"var style=document.createElement('style');style.innerHTML=`{Scripts.ReaderCSS}`;document.head.appendChild(style);");
            }
            else
                Reload();
        }

        News _NewsFeed;
        public void NewsFeed(bool ForceClose = false)
        {
            if (!ForceClose && IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
                ToggleSideBar(ForceClose);
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                DevToolsToolBar.Visibility = Visibility.Collapsed;
                _NewsFeed = new News(this);
                SideBarCoreContainer.Children.Add(_NewsFeed);
                Grid.SetColumn(_NewsFeed, 1);
                Grid.SetRow(_NewsFeed, 1);

                _NewsFeed.HorizontalAlignment = HorizontalAlignment.Stretch;
                _NewsFeed.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        bool Muted = false;
        public void ToggleMute()
        {
            Muted = !Muted;
            Chromium.ToggleAudioMute();
            MuteMenuItem.Icon = Muted ? "\xe767" : "\xe74f";
            MuteMenuItem.Header = Muted ? "Unmute" : "Mute";
            SetAudioState(null);
        }

        public void Favourite()
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
                PromptDialogWindow InfoWindow = new PromptDialogWindow("Prompt", $"Add Favourite", "Name", Title);
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    App.Instance.Favourites.Add(new ActionStorage(InfoWindow.UserInput, $"4<,>{Address}", Address));
                    FavouriteButton.Content = "\xEB52";
                    FavouriteButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
                    FavouriteButton.ToolTip = "Remove from favourites";
                    Tab.FavouriteCommandHeader = "Remove from favourites";
                }
            }
        }

        public void SetFavouritesBarVisibility()
        {
            if (ShowFavouritesBar == 0)
            {
                if (App.Instance.Favourites.Count == 0)
                {
                    FavouriteScrollViewer.Margin = new Thickness(0);
                    FavouriteContainer.Height = 5;
                }
                else
                {
                    FavouriteScrollViewer.Margin = new Thickness(5, 5, 5, 5);
                    FavouriteContainer.Height = 41.25f;
                }
            }
            else if (ShowFavouritesBar == 1)
            {
                FavouriteScrollViewer.Margin = new Thickness(5, 5, 5, 5);
                FavouriteContainer.Height = 41.25f;
            }
            else if (ShowFavouritesBar == 2)
            {
                FavouriteScrollViewer.Margin = new Thickness(0);
                FavouriteContainer.Height = 5;
            }
        }

        int FavouriteExists(string Url)
        {
            if (App.Instance.Favourites.Count == 0)
                return -1;
            return App.Instance.Favourites.ToList().FindIndex(0, i => i.Tooltip == Url);
        }
        public void Zoom(int Delta)
        {
            if (Delta == 0)
                Chromium.ZoomResetCommand.Execute(null);
            else if (Delta > 0)
                Chromium.ZoomInCommand.Execute(null);
            else if (Delta < 0)
                Chromium.ZoomOutCommand.Execute(null);
        }
        public async void Screenshot()
        {
            try
            {
                int _ScreenshotFormat = App.Instance.GlobalSave.GetInt("ScreenshotFormat");
                string FileExtension = "jpg";
                CaptureScreenshotFormat ScreenshotFormat = CaptureScreenshotFormat.Jpeg;
                if (_ScreenshotFormat == 1)
                {
                    FileExtension = "png";
                    ScreenshotFormat = CaptureScreenshotFormat.Png;
                }
                else if (_ScreenshotFormat == 2)
                {
                    FileExtension = "webp";
                    ScreenshotFormat = CaptureScreenshotFormat.Webp;
                }
                string ScreenshotPath = App.Instance.GlobalSave.Get("ScreenshotPath");
                if (!Directory.Exists(ScreenshotPath))
                    Directory.CreateDirectory(ScreenshotPath);
                DateTime CurrentTime = DateTime.Now;
                string Url = $"{Path.Combine(ScreenshotPath, Regex.Replace($"{Chromium.Title} {CurrentTime.Day}-{CurrentTime.Month}-{CurrentTime.Year} {string.Format("{0:hh:mm tt}", DateTime.Now)}.{FileExtension}", "[^a-zA-Z0-9._ -]", ""))}";
                using (var _DevToolsClient = Chromium.GetDevToolsClient())
                {
                    var ContentSize = await Chromium.GetContentSizeAsync();
                    var Result = await _DevToolsClient.Page.CaptureScreenshotAsync(ScreenshotFormat, null, new Viewport { Width = ContentSize.Width, Height = ContentSize.Height }, null, true, true);
                    File.WriteAllBytes(Url, Result.Data);
                }
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
            catch { }
        }


        private void FavouriteScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            FavouriteScrollViewer.ScrollToHorizontalOffset(FavouriteScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
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
        /*private bool IsMouseOverPopup(Popup popup, Point mousePosition)
        {
            if (popup.Child is FrameworkElement child)
                return new Rect(0, 0, child.ActualWidth, child.ActualHeight).Contains(mousePosition);
            return false;
        }*/

        public void OmniBoxEnter()
        {
            string Url = Utils.FilterUrlForBrowser(OmniBox.Text, App.Instance.DefaultSearchProvider.SearchUrl);
            if (Url.StartsWith("javascript:", StringComparison.Ordinal))
            {
                Chromium.ExecuteScriptAsync(Url.Substring(11));
                OmniBox.Text = OmniBox.Tag.ToString();
            }
            else if (!Utils.IsProgramUrl(Url))
                Address = Url;
            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
            {
                OmniBoxFastTimer.Stop();
                OmniBoxSmartTimer.Stop();
            }
            //Suggestions.Clear();
            OmniBox.IsDropDownOpen = false;
            Keyboard.ClearFocus();
            Chromium.Focus();
        }

        private bool IsOnlyModifierPressed()
        {
            return Keyboard.Modifiers == ModifierKeys.Control ||
                   Keyboard.Modifiers == ModifierKeys.Alt ||
                   Keyboard.Modifiers == ModifierKeys.Windows;
        }

        private bool IsIgnorableKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F24)
                return true;
            return key switch
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

        private void OmniBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (OmniBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                    OmniBoxEnter();
                else
                {
                    if (IsIgnorableKey(e.Key) || IsOnlyModifierPressed())
                        return;
                    Storyboard LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                    LoadingStoryboard?.Seek(TimeSpan.Zero);
                    LoadingStoryboard?.Stop();
                    if (OmniBox.Text.Length != 0)
                    {
                        if (OmniBox.Text.StartsWith("search:", StringComparison.Ordinal))
                        {
                            SiteInformationIcon.Text = "\xE721";
                            SiteInformationText.Text = $"Search";
                            SiteInformationPanel.ToolTip = $"Searching: {OmniBox.Text.Substring(7).Trim()}";
                        }
                        else if (OmniBox.Text.StartsWith("domain:", StringComparison.Ordinal))
                        {
                            SiteInformationIcon.Text = "\xE71B";
                            SiteInformationText.Text = $"Address";
                            SiteInformationPanel.ToolTip = $"Address: {OmniBox.Text.Substring(7).Trim()}";
                        }
                        else if (Utils.IsProgramUrl(OmniBox.Text))
                        {
                            SiteInformationIcon.Text = "\xE756";
                            SiteInformationText.Text = $"Program";
                            SiteInformationPanel.ToolTip = $"Open program: {OmniBox.Text}";
                        }
                        else if (Utils.IsCode(OmniBox.Text))
                        {
                            SiteInformationIcon.Text = "\xE943";
                            SiteInformationText.Text = $"Code";
                            SiteInformationPanel.ToolTip = $"Code: {OmniBox.Text}";
                        }
                        else if (Utils.IsUrl(OmniBox.Text))
                        {
                            SiteInformationIcon.Text = "\xE71B";
                            SiteInformationText.Text = $"Address";
                            SiteInformationPanel.ToolTip = $"Address: {OmniBox.Text}";
                        }
                        else
                        {
                            SiteInformationIcon.Text = "\xE721";
                            SiteInformationText.Text = $"Search";
                            SiteInformationPanel.ToolTip = $"Searching: {OmniBox.Text}";
                        }

                        if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
                        {
                            OmniBoxFastTimer.Stop();
                            OmniBoxSmartTimer.Stop();
                            OmniBoxFastTimer.Start();
                            if (bool.Parse(App.Instance.GlobalSave.Get("SmartSuggestions")))
                                OmniBoxSmartTimer.Start();
                        }
                    }
                    else
                    {
                        SiteInformationIcon.Text = "\xE721";
                        SiteInformationText.Text = $"Search";
                        SiteInformationPanel.ToolTip = $"Searching: {OmniBox.Text}";
                    }
                    SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                    if (OmniBox.IsDropDownOpen)
                    {
                        OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
                        OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
                    }
                }
            }
        }
        public bool IsOmniBoxModifiable()
        {
            return !OmniBoxFocused;
        }

        private void OmniBox_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (OmniBox.IsKeyboardFocusWithin)
            {
                try
                {
                    if (OmniBox.Text == Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(OmniBox.Tag.ToString())))
                        OmniBox.Text = OmniBox.Tag.ToString();
                }
                catch { }
                //OmniTextBox.SelectAll();
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
                    if (Utils.CleanUrl(OmniBox.Text) == Utils.CleanUrl(OmniBox.Tag.ToString()))
                        OmniBox.Text = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(OmniBox.Tag.ToString()));
                }
                catch { }
                OmniBoxBorder.BorderThickness = new Thickness(1);
                OmniBoxBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
                OmniBoxFocused = false;
                if (!OmniBoxHovered)
                    SiteInformationText.Visibility = Visibility.Collapsed;
            }
        }

        bool OmniBoxFocused;
        bool OmniBoxHovered;

        Size MaximizedSize = Size.Empty;
        private void CoreContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size NewSize = new Size(CoreContainerSizeEmulator.ActualWidth, CoreContainerSizeEmulator.ActualHeight);
            if (MaximizedSize == Size.Empty)
                MaximizedSize = NewSize;
            Size Percentage = new Size(NewSize.Width / MaximizedSize.Width, NewSize.Height / MaximizedSize.Height);

            SizeEmulatorColumn1.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorColumn2.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorRow1.MaxHeight = 400 * Percentage.Height;
            SizeEmulatorRow2.MaxHeight = 400 * Percentage.Height;

            SizeEmulatorColumn1.Width = new GridLength(0);
            SizeEmulatorColumn2.Width = new GridLength(0);
            SizeEmulatorRow1.Height = new GridLength(0);
            SizeEmulatorRow2.Height = new GridLength(0);
        }

        bool AllowHomeButton;
        bool AllowTranslateButton;
        bool AllowReaderModeButton;
        int ShowExtensionButton;
        int ShowFavouritesBar;

        public async void SetAppearance(Theme _Theme, bool _AllowHomeButton, bool _AllowTranslateButton, bool _AllowReaderModeButton, int _ShowExtensionButton, int _ShowFavouritesBar)
        {
            AllowHomeButton = _AllowHomeButton;
            AllowTranslateButton = !Private && _AllowTranslateButton;
            AllowReaderModeButton = _AllowReaderModeButton;
            ShowExtensionButton = _ShowExtensionButton;
            ShowFavouritesBar = _ShowFavouritesBar;
            SetFavouritesBarVisibility();
            HomeButton.Visibility = AllowHomeButton ? Visibility.Visible : Visibility.Collapsed;
            if (!IsLoading)
            {
                //MessageBox.Show(Address);
                //MessageBox.Show(CoAddress);
                if (Utils.IsHttpScheme(Address))
                    TranslateButton.Visibility = AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                /*else if (Address.StartsWith("file:", StringComparison.Ordinal))
                    TranslateButton.Visibility = Visibility.Collapsed;
                else if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                    TranslateButton.Visibility = Visibility.Collapsed;*/
                else
                    TranslateButton.Visibility = Visibility.Collapsed;
            }

            if (Chromium != null && Chromium.IsBrowserInitialized)
            {
                Chromium.GetDevToolsClient()?.Emulation.SetAutoDarkModeOverrideAsync(_Theme.DarkWebPage);
                ReaderModeButton.Visibility = AllowReaderModeButton ? (Chromium.CanExecuteJavascriptInMainFrame && (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed) : Visibility.Collapsed;
            }
            else
                ReaderModeButton.Visibility = Visibility.Collapsed;

            if (ShowExtensionButton == 0)
                ExtensionsButton.Visibility = App.Instance.Extensions.Any() ? Visibility.Visible : Visibility.Collapsed;
            else if (ShowExtensionButton == 1)
                ExtensionsButton.Visibility = Visibility.Visible;
            else
                ExtensionsButton.Visibility = Visibility.Collapsed;

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        public void DisposeCore()
        {
            ToggleSideBar(true);
            CoreContainer.Children.Clear();
            SideBarCoreContainer.Children.Clear();
            Chromium?.Dispose();
            _Settings?.Dispose();
            _Settings = null;
            Chromium = null;
            GC.Collect(GC.MaxGeneration);
            GC.SuppressFinalize(this);
        }

        private void InspectorDockDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Action(Actions.SetSideBarDock, null, (3 - SideBarDockDropdown.SelectedIndex).ToString());
        }

        private ObservableCollection<OmniSuggestion> _Suggestions = new ObservableCollection<OmniSuggestion>();
        public ObservableCollection<OmniSuggestion> Suggestions
        {
            get { return _Suggestions; }
            set
            {
                _Suggestions = value;
                RaisePropertyChanged("Suggestions");
            }
        }
        private DispatcherTimer OmniBoxFastTimer;
        private DispatcherTimer OmniBoxSmartTimer;
        bool OmniBoxIsDropdown = false;

        private CancellationTokenSource? SmartSuggestionCancellation;

        private async void OmniBoxFastTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxFastTimer.Stop();
            string CurrentText = OmniBox.Text;
            Suggestions.Clear();
            OmniBox.Text = CurrentText;
            /*string FirstEntryType = "S";
            switch (App.GetSearchType(OmniBox.Text))
            {
                case "Url":
                    FirstEntryType = "W";
                    break;
                case "Program":
                    FirstEntryType = "P";
                    break;
                case "Code":
                    FirstEntryType = "C";
                    break;
                case "File":
                    FirstEntryType = "F";
                    break;
            }*/
            Suggestions.Add(App.GenerateSuggestion(CurrentText, App.GetMiniSearchType(CurrentText)));//FirstEntryType
            try
            {
                string SuggestionsUrl = string.Format(App.Instance.DefaultSearchProvider.SuggestUrl, Uri.EscapeDataString(CurrentText));
                if (SuggestionsUrl != "")
                {
                    string ResponseText = await App.MiniHttpClient.GetStringAsync(SuggestionsUrl);
                    using (JsonDocument Document = JsonDocument.Parse(ResponseText))
                    {
                        foreach (JsonElement Suggestion in Document.RootElement[1].EnumerateArray())
                        {
                            string SuggestionStr = Suggestion.GetString();
                            Suggestions.Add(App.GenerateSuggestion(SuggestionStr, App.GetMiniSearchType(SuggestionStr)));
                        }
                    }
                }
            }
            catch { }
            OmniBox.IsDropDownOpen = Suggestions.Count > 0;
            OmniBoxIsDropdown = true;

            //Keyboard.Focus(OmniBox);
            OmniBox.Focus();
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
        }

        private async void OmniBoxSmartTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxSmartTimer.Stop();
            if (!OmniBox.IsDropDownOpen)
                return;
            string Text = OmniBox.Text.Trim();
            string Type = App.GetSmartType(Text);
            if (Type == "None") return;
            SmartSuggestionCancellation?.Cancel();
            SmartSuggestionCancellation = new CancellationTokenSource();
            var Token = SmartSuggestionCancellation.Token;

            OmniSuggestion Suggestion = await App.Instance.GenerateSmartSuggestion(Text, Type, Token);
            if (!Token.IsCancellationRequested)
            {
                Suggestions.RemoveAt(0);
                Suggestions.Insert(0, Suggestion);
            }
        }

        private void OmniBox_DropDownOpened(object sender, EventArgs e)
        {
            Chromium.Focusable = false;
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);// + 4 + 4
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
        }

        private void OmniBox_DropDownClosed(object sender, EventArgs e)
        {
            Chromium.Focusable = true;
        }

        int CaretIndex = 0;

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Browser_Loaded;
            OmniTextBox = OmniBox.Template.FindName("PART_EditableTextBox", OmniBox) as TextBox;
            OmniTextBox.PreviewKeyDown += (_, __) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CaretIndex = OmniTextBox.CaretIndex;
                }), DispatcherPriority.Input);
            };

            OmniTextBox.GotKeyboardFocus += (sender, args) =>
            {
                args.Handled = true;
                OmniTextBox.CaretIndex = CaretIndex;
                if (!OmniBoxIsDropdown)
                    OmniTextBox.SelectAll();
            };
            OmniBoxPopup = OmniBox.Template.FindName("Popup", OmniBox) as Popup;
            OmniBoxPopupDropDown = OmniBox.Template.FindName("DropDown", OmniBox) as Grid;
            OmniBox.ItemsSource = Suggestions;
            if (Address == "slbr://newtab")
            {
                Keyboard.Focus(OmniBox);
                OmniBox.Focus();
            }
        }

        Window ExtensionWindow;
        private void LoadExtensionPopup(object sender, RoutedEventArgs e)
        {
            Extension _Extension = App.Instance.Extensions.ToList().Find(i => i.ID == ((FrameworkElement)sender).Tag.ToString());
            ExtensionWindow = new Window();
            ChromiumWebBrowser ExtensionBrowser = new ChromiumWebBrowser(_Extension.Popup);
            ExtensionBrowser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            HwndSource _HwndSource = HwndSource.FromHwnd(new WindowInteropHelper(ExtensionWindow).EnsureHandle());
            _HwndSource.AddHook(WndProc);
            ExtensionBrowser.LoadingStateChanged += (s, args) =>
            {
                if (!args.IsLoading)
                    ExtensionBrowser.ExecuteScriptAsync(Scripts.ExtensionScript);
                /*function getBoundingClientRect(element) {
var rect = element.getBoundingClientRect();
    return {
        top: rect.top,
        right: rect.right,
        bottom: rect.bottom,
        left: rect.left,
        width: rect.width + 16,
        height: rect.height + 39,
        x: rect.x,
        y: rect.y
    };
}*/
            };
            int trueValue = 0x01;
            int falseValue = 0x00;
            DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref App.Instance.CurrentTheme.DarkTitleBar ? ref trueValue : ref falseValue, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            ExtensionBrowser.JavascriptMessageReceived += ExtensionBrowser_JavascriptMessageReceived;
            ExtensionBrowser.SnapsToDevicePixels = true;
            ExtensionBrowser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            ExtensionBrowser.DownloadHandler = App.Instance._DownloadHandler;
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
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MOVE = 0xF010;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
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
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        private void ExtensionBrowser_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                dynamic data = e.Message;
                ExtensionWindow.Height = data.height;
                ExtensionWindow.Width = data.width;
            });
        }

        private void SiteInformation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SiteInformationText.Text != "Loading")
                SiteInformationPopup.IsOpen = !SiteInformationPopup.IsOpen;
        }
    }
}
