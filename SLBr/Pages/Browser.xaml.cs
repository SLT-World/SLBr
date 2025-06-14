using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Emulation;
using CefSharp.DevTools.Page;
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

        //BrowserSettings _BrowserSettings;

        public ChromiumWebBrowser Chromium;
        public Settings _Settings;
        public Handlers.ResourceRequestHandlerFactory _ResourceRequestHandlerFactory;

        public Browser(string Url, BrowserTabItem _Tab = null)//int _BrowserType = 0, 
        {
            InitializeComponent();
            Tab = _Tab != null ? _Tab : Tab.ParentWindow.GetTab(this);
            Tab.Icon = App.Instance.GetIcon(bool.Parse(App.Instance.GlobalSave.Get("Favicons")) ? Url : "");
            Address = Url;
            SetAudioState(false);
            //BrowserType = _BrowserType;
            InitializeBrowserComponent();
            FavouritesPanel.ItemsSource = App.Instance.Favourites;
            FavouriteListMenu.ItemsSource = App.Instance.Favourites;
            HistoryListMenu.ItemsSource = App.Instance.History;
            ExtensionsMenu.ItemsSource = App.Instance.Extensions;//ObservableCollection wasn't working for no reason so I turned it into a list
            /*BrowserEmulatorComboBox.Items.Add("Chromium");
            BrowserEmulatorComboBox.Items.Add("Edge");
            BrowserEmulatorComboBox.Items.Add("Internet Explorer");*/
            App.Instance.Favourites.CollectionChanged += Favourites_CollectionChanged;
            SetAppearance(App.Instance.CurrentTheme, bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")),int.Parse(App.Instance.GlobalSave.Get("ExtensionButton")), int.Parse(App.Instance.GlobalSave.Get("FavouritesBar")));

            OmniBoxTimer = new DispatcherTimer();
            OmniBoxTimer.Tick += OmniBoxTimer_Tick;
            OmniBoxTimer.Interval = TimeSpan.FromMilliseconds(200);
            //BrowserEmulatorComboBox.SelectionChanged += BrowserEmulatorComboBox_SelectionChanged;
        }

        public void InitializeBrowserComponent(bool First = true)
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

        private void Favourites_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
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
            /*if (sender == null)
                return;*/
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

                case Actions.AIChat:
                    AIChat();
                    break;
                case Actions.AIChatFeature:
                    AIChatFeature(int.Parse(V1));
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
            if (Chromium != null && (!Cef.IsInitialized.ToBool()))
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
                BackgroundColor = (uint)((_PrimaryColor.A << 24) | (_PrimaryColor.R << 16) | (_PrimaryColor.G << 8) | (_PrimaryColor.B << 0))
            };
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

            //RenderOptions.SetBitmapScalingMode(Chromium, BitmapScalingMode.LowQuality);

            //BrowserEmulatorComboBox.SelectedItem = "Chromium";
        }

        public void ReFocus()
        {
            InitializeBrowserComponent(false);
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

        private void Chromium_FrameLoadStart(object? sender, FrameLoadStartEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                    Chromium.ExecuteScriptAsync(App.InternalJavascriptFunction);
            });
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

        private void Chromium_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (Chromium.IsBrowserInitialized)
            {
                CoreContainer.Visibility = Visibility.Visible;
                Tab.IsUnloaded = false;
                Tab.BrowserCommandsVisibility = Visibility.Visible;
                if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                    Tab.ProgressBarVisibility = Visibility.Visible;
                Chromium.Focus();
                using (var DevToolsClient = Chromium.GetDevToolsClient())
                {
                    DevToolsClient.Page.SetPrerenderingAllowedAsync(false);
                    DevToolsClient.Preload.DisableAsync();
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
                                switch (App.Instance.GlobalSave.Get("HomepageBackground"))
                                {
                                    case "Bing":
                                        string BingBackground = App.Instance.GlobalSave.Get("BingBackground");
                                        if (BingBackground == "Image of the day")
                                        {
                                            try
                                            {
                                                XmlDocument doc = new XmlDocument();
                                                doc.LoadXml(new WebClient().DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                                                Url = "http://www.bing.com/" + doc.SelectSingleNode("/images/image/url").InnerText;
                                            }
                                            catch { }
                                        }
                                        else if (BingBackground == "Random")
                                            Url = "http://bingw.jasonzeng.dev/?index=random";
                                        break;

                                    case "Picsum":
                                        Url = "http://picsum.photos/1920/1080?random";
                                        break;

                                    case "Custom":
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
                                    Address = Utils.FilterUrlForBrowser(Message["variable"].ToString(), App.Instance.GlobalSave.Get("SearchEngine"));
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

        private void Chromium_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
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
                if (e.Browser.IsValid)
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
                                List<int> FingerprintHardwareConcurrencies = new List<int>() { 1, 2, 4, 6, 8, 10, 12, 14 };
                                //https://source.chromium.org/chromium/chromium/deps/icu.git/+/chromium/m120:source/data/misc/metaZones.txt
                                List<string> FingerprintTimeZones = new List<string>() { "Africa/Monrovia", "Europe/London", "America/New_York", "Asia/Seoul", "Asia/Singapore", "Asia/Taipei" };
                                Random _Random = new Random();
                                await _DevToolsClient.Emulation.SetHardwareConcurrencyOverrideAsync(FingerprintHardwareConcurrencies[_Random.Next(FingerprintHardwareConcurrencies.Count)]);
                                await _DevToolsClient.Emulation.SetTimezoneOverrideAsync(FingerprintTimeZones[_Random.Next(FingerprintTimeZones.Count)]);
                                break;
                            default:
                                break;
                        }
                        //Hardware Concurrency 1, 2, 4, 6, 8, 10, 12, 14
                        //Device Memory 2, 3, 4, 6, 7, 8, 15, 16, 31, 32, 64
                        if (App.Instance.GlobalSave.Get("FingerprintLevel") != "Minimal")
                            Chromium.ExecuteScriptAsync(@"Object.defineProperty(navigator,'getBattery',{get:function(){return new Promise((resolve,reject)=>{reject('Battery API is disabled.');});}});Object.defineProperty(navigator,'connection',{get:function(){return null;}});");
                    }
                    else
                    {
                        var Brands = new List<UserAgentBrandVersion>
                        {
                            new UserAgentBrandVersion
                            {
                                Brand = "SLBr",
                                Version = App.Instance.ReleaseVersion.Split('.')[0]
                            },
                            new UserAgentBrandVersion
                            {
                                Brand = "Chromium",
                                Version = Cef.ChromiumVersion.Split('.')[0]
                            }
                        };
                        var _UserAgentMetadata = new UserAgentMetadata
                        {
                            Brands = Brands,
                            Architecture = UserAgentGenerator.GetCPUArchitecture(),
                            Model = "",
                            Platform = "Windows",
                            PlatformVersion = UserAgentGenerator.GetPlatformVersion(),//https://textslashplain.com/2021/09/21/determining-os-platform-version/
                            FullVersion = Cef.ChromiumVersion,
                            Mobile = false
                        };
                        /*var _UserAgentMetadata = new UserAgentMetadata
                        {
                            Brands = Brands,
                            Architecture = "arm",
                            Model = "Nexus 7",
                            Platform = "Android",
                            PlatformVersion = "6.0.1",
                            FullVersion = Cef.ChromiumVersion,
                            Mobile = true
                        };*/
                        //navigator.userAgentData.getHighEntropyValues(["architecture","model","platform","platformVersion","uaFullVersion"]).then(ua =>{console.log(ua)});
                        await _DevToolsClient.Emulation.SetUserAgentOverrideAsync(App.Instance.UserAgent, null, null, _UserAgentMetadata);
                    }
                }
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
                                    if (Address.AsSpan().IndexOf("youtube.com", StringComparison.Ordinal) >= 0)
                                    {
                                        Chromium.ExecuteScriptAsync(Scripts.YouTubeHideAdScript);
                                        if (Address.AsSpan().IndexOf("/watch?v=", StringComparison.Ordinal) >= 0)
                                            Chromium.ExecuteScriptAsync(Scripts.YouTubeSkipAdScript);
                                    }
                                }
                                if (Address.AsSpan().IndexOf("chromewebstore.google.com/detail", StringComparison.Ordinal) >= 0)
                                    Chromium.ExecuteScriptAsync(Scripts.WebStoreScript);
                                //if (bool.Parse(App.Instance.GlobalSave.Get("LiteMode")))
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
                            }
                            else if (Address.StartsWith("file:///", StringComparison.Ordinal))
                                Chromium.ExecuteScriptAsync(Scripts.FileScript);
                        }
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
                                SetAppearance(SiteTheme, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(SiteTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(SiteTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(SiteTheme.BorderColor);
                            }
                            catch
                            {
                                IsCustomTheme = false;
                                SetAppearance(App.Instance.CurrentTheme, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                            }
                        }
                        else if (IsCustomTheme)
                        {
                            IsCustomTheme = false;
                            SetAppearance(App.Instance.CurrentTheme, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
                            TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                            _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                            _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                            _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                        }
                    }
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
            //SetFavouritesBarVisibility();
            AIChatButton.Visibility = AllowAIButton ? Visibility.Visible : Visibility.Collapsed;
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
                        if (Utils.IsHttpScheme(Address))
                        {
                            SiteInformationCertificate.Visibility = Visibility.Visible;
                            if (Chromium != null && Chromium.IsBrowserInitialized)
                            {
                                CefSharp.NavigationEntry _NavigationEntry = await Chromium.GetVisibleNavigationEntryAsync();
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
                                else if (Address.StartsWith("http:", StringComparison.Ordinal))
                                    SetSiteInfo = "Insecure";
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
                            if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
                                AIChatButton.Visibility = Visibility.Collapsed;
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
                    if (AllowReaderModeButton && Utils.IsHttpScheme(Address))
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
        HwndHoster Host;
        public void ToggleSideBar(bool ForceClose = false)
        {
            AIChatToolBar.Visibility = Visibility.Collapsed;
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
                    AIChatBrowser?.Dispose();
                    AIChatBrowser = null;
                    if (Host != null)
                    {
                        Chromium.BrowserCore.CloseDevTools();
                        DestroyWindow(Host.Handle);
                        Host?.Dispose();
                        Host = null;
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
            if (!ForceClose && IsUtilityContainerOpen && ((AIChatBrowser != null && !AIChatBrowser.IsDisposed) || (_NewsFeed != null) || (Host != null)))
                ToggleSideBar(ForceClose);
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                AIChatToolBar.Visibility = Visibility.Collapsed;
                DevToolsToolBar.Visibility = Visibility.Visible;
                Host = new HwndHoster();
                SideBarCoreContainer.Children.Add(Host);
                Grid.SetColumn(Host, 1);
                Grid.SetRow(Host, 1);

                Host.HorizontalAlignment = HorizontalAlignment.Stretch;
                Host.VerticalAlignment = VerticalAlignment.Stretch;

                Host.Loaded += (s, args) =>
                {
                    if (Host != null)
                    {
                        SideBarWindowInfo = WindowInfo.Create();
                        SideBarWindowInfo.SetAsChild(Host.Handle);
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
            if (!ForceClose && IsUtilityContainerOpen && ((AIChatBrowser != null && !AIChatBrowser.IsDisposed) || (_NewsFeed != null) || (Host != null)))
                ToggleSideBar(ForceClose);
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                AIChatToolBar.Visibility = Visibility.Collapsed;
                DevToolsToolBar.Visibility = Visibility.Collapsed;
                _NewsFeed = new News(this);
                SideBarCoreContainer.Children.Add(_NewsFeed);
                Grid.SetColumn(_NewsFeed, 1);
                Grid.SetRow(_NewsFeed, 1);

                _NewsFeed.HorizontalAlignment = HorizontalAlignment.Stretch;
                _NewsFeed.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        ChromiumWebBrowser AIChatBrowser;
        public void AIChat(bool ForceClose = false)
        {
            if (!ForceClose && IsUtilityContainerOpen && ((AIChatBrowser != null && !AIChatBrowser.IsDisposed) || (_NewsFeed != null) || (Host != null)))
                ToggleSideBar(ForceClose);
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                AIChatToolBar.Visibility = Visibility.Visible;
                DevToolsToolBar.Visibility = Visibility.Collapsed;
                AIChatBrowser = new ChromiumWebBrowser();
                AIChatBrowser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
                AIChatBrowser.LifeSpanHandler = new LifeSpanHandler(true);
                AIChatBrowser.DownloadHandler = App.Instance._DownloadHandler;
                AIChatBrowser.MenuHandler = App.Instance._LimitedContextMenuHandler;
                AIChatBrowser.AllowDrop = true;
                AIChatBrowser.IsManipulationEnabled = true;
                AIChatBrowser.UseLayoutRounding = true;

                Color _PrimaryColor = (Color)FindResource("PrimaryBrushColor");
                AIChatBrowser.BrowserSettings = new BrowserSettings
                {
                    WebGl = CefState.Disabled,
                    BackgroundColor = (uint)((_PrimaryColor.A << 24) | (_PrimaryColor.R << 16) | (_PrimaryColor.G << 8) | (_PrimaryColor.B << 0))
                };
                AIChatBrowser.ZoomLevelIncrement = 0.5f;
                //RenderOptions.SetBitmapScalingMode(AIChatBrowser, BitmapScalingMode.LowQuality);

                SideBarCoreContainer.Children.Add(AIChatBrowser);
                Grid.SetColumn(AIChatBrowser, 1);
                Grid.SetRow(AIChatBrowser, 1);

                AIChatBrowser.HorizontalAlignment = HorizontalAlignment.Stretch;
                AIChatBrowser.VerticalAlignment = VerticalAlignment.Stretch;

                AIChatFeature(0);
                AIChatBrowser.LoadingStateChanged += AIChatBrowser_LoadingStateChanged;
                AIChatBrowser.IsBrowserInitializedChanged += AIChatBrowser_IsBrowserInitializedChanged;
                AIChatBrowser.Visibility = Visibility.Collapsed;
            }
        }


        private void AIChatBrowser_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (AIChatBrowser.IsBrowserInitialized)
                AIChatBrowser.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync($"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{Cef.ChromiumVersion} Safari/537.36 Edg/{Cef.ChromiumVersion}");
        }

        private void AIChatBrowser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (AIChatBrowser == null)
                return;
            Dispatcher.Invoke(() =>
            {
                AIChatBrowser.Visibility = Visibility.Collapsed;
            });
            if (!AIChatBrowser.IsBrowserInitialized)
                return;
            if (!e.IsLoading)
            {
                Color _PrimaryColor = (Color)FindResource("PrimaryBrushColor");
                string PrimaryHex = $"#{_PrimaryColor.R:X2}{_PrimaryColor.G:X2}{_PrimaryColor.B:X2}{_PrimaryColor.A:X2}";
                Color _SecondaryColor = (Color)FindResource("SecondaryBrushColor");
                string SecondaryHex = $"#{_SecondaryColor.R:X2}{_SecondaryColor.G:X2}{_SecondaryColor.B:X2}{_SecondaryColor.A:X2}";
                Color _BorderColor = (Color)FindResource("BorderBrushColor");
                string BorderHex = $"#{_BorderColor.R:X2}{_BorderColor.G:X2}{_BorderColor.B:X2}{_BorderColor.A:X2}";
                Color _GrayColor = (Color)FindResource("GrayBrushColor");
                string GrayHex = $"#{_GrayColor.R:X2}{_GrayColor.G:X2}{_GrayColor.B:X2}{_GrayColor.A:X2}";
                Color _FontColor = (Color)FindResource("FontBrushColor");
                string FontHex = $"#{_FontColor.R:X2}{_FontColor.G:X2}{_FontColor.B:X2}{_FontColor.A:X2}";
                Color _IndicatorColor = (Color)FindResource("IndicatorBrushColor");
                string IndicatorHex = $"#{_IndicatorColor.R:X2}{_IndicatorColor.G:X2}{_IndicatorColor.B:X2}{_IndicatorColor.A:X2}";

                string ChatJS = $@"const CSSVariables=`body {{
    --cib-border-radius-circular: 5px;
    --cib-border-radius-extra-large: 5px;
    --cib-border-radius-large: 5px;
    --cib-comp-thread-host-border-radius: 5px;
    --cib-color-background-surface-app-primary: {PrimaryHex};
    --cib-color-background-surface-solid-base: {PrimaryHex};
    --cib-color-background-surface-solid-primary: {PrimaryHex};
    --cib-color-background-surface-app-secondary: {SecondaryHex};
    --cib-color-background-surface-solid-secondary: {PrimaryHex};
    --cib-color-background-surface-solid-quaternary: {PrimaryHex};
    --cib-color-fill-neutral-solid-primary: {PrimaryHex};
    --cib-color-fill-subtle-tertiary: {BorderHex};
    --cib-color-stroke-neutral-quarternary: {BorderHex};
    --cib-color-background-surface-card-primary: {SecondaryHex};
    --cib-color-background-surface-card-secondary: {SecondaryHex};
    --cib-color-background-surface-card-tertiary: {BorderHex};
    --cib-color-foreground-neutral-tertiary: {GrayHex};
    --cib-color-background-system-caution-primary: #FF8800;
    background: {PrimaryHex} !important;
    --cib-color-foreground-accent-primary: {IndicatorHex};
    --cib-color-fill-accent-gradient-primary: {IndicatorHex};
    --cib-color-stroke-accent-primary: {IndicatorHex};
    --cib-color-fill-accent-gradient-customized-primary: {IndicatorHex};
}}
::-webkit-scrollbar {{
    background: {PrimaryHex} !important;
}}
.surface {{
    border: 1px solid var(--cib-color-background-surface-card-tertiary) !important;
    box-shadow: none !important;
}}
button[is=""cib-button""] {{
    height: 35px !important;
    width: 35px !important;
}}
.text-message-content[user] div {{
    background: var(--cib-color-fill-accent-gradient-primary) !important;
    border-radius: 5px !important;
    padding: 15px !important;
    text-align: right !important;
}}
:host([source=user]) .header {{
    align-self: flex-end !important;
}}
:host([source=user]) {{
    align-items: flex-end;
}}`;
const style = document.createElement('style');
style.type = 'text/css';
style.innerHTML = CSSVariables;
document.head.appendChild(style);
var elements = document.querySelectorAll('.b_wlcmDesc');
elements.forEach(function(element) {{
    element.parentNode.removeChild(element);
}});
var elements = document.querySelectorAll('.b_wlcmName');
elements.forEach(function(element) {{
    element.parentNode.removeChild(element);
}});";

                string ComposeJS = $@"const CSSVariables=`::-webkit-scrollbar {{
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper .sidebar {{
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper .child {{
    background: {SecondaryHex} !important;
    border: 1px solid {BorderHex} !important;
    box-shadow: none !important;
}}
.zpcarousel .wt_cont {{
    border-radius: 5px !important;
}}
.zpcarousel .slide .cimg_cont {{
    border-radius: 5px !important;
}}
.uds_coauthor_wrapper .button {{
    border-radius: 5px !important;
}}
.uds_coauthor_wrapper .tag {{
    border-radius: 5px !important;
    color: white !important;
    border-color: {BorderHex} !important;
    background: {SecondaryHex} !important;
    color: {FontHex} !important;
}}
.uds_coauthor_wrapper .tag.selected {{
    outline: 2px solid {IndicatorHex} !important;
    outline-offset: -2px;
}}
.uds_coauthor_wrapper .tag:hover:not(.selected) {{
    background: {BorderHex} !important;
}}
.uds_coauthor_wrapper textarea {{
    border-radius: 5px !important;
    box-shadow: none !important;
    border: 1px solid {BorderHex} !important;
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper #letter_counter {{
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper textarea:focus-visible {{
    border: 1px solid {IndicatorHex} !important
}}
.uds_coauthor_wrapper .option-section {{
    border-radius: 5px !important;
    border-color: {BorderHex} !important;
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper .format-option .illustration {{
    border-radius: 5px !important;
    border: 1px solid {BorderHex} !important;
    background: {SecondaryHex} !important;
}}
.uds_coauthor_wrapper .format-option.selected .illustration {{
    outline: 2px solid {IndicatorHex} !important;
    outline-offset: -2px;
}}
.uds_coauthor_wrapper .format-option.selected .illustration svg>path {{
    fill-opacity: 1 !important;
    opacity: 1 !important;
}}
.uds_coauthor_wrapper .format-option:hover:not(.selected) .illustration {{
    background: {BorderHex} !important;
}}
.uds_coauthor_wrapper #custom_tone_plus_button svg>path, .uds_coauthor_wrapper #custom_tone_edit_button svg>path, .uds_coauthor_wrapper #add_change_suggestion_button svg>path {{
    fill: {FontHex} !important;
}}
.uds_coauthor_wrapper #custom_tone_add_button, .uds_coauthor_wrapper #custom_tone_save_button, .uds_coauthor_wrapper #custom_tone_add_button:not(.disabled) span:hover, .uds_coauthor_wrapper #custom_tone_save_button:not(.disabled) span:hover, .uds_coauthor_wrapper #submit_change_suggestion_button, .uds_coauthor_wrapper #submit_change_suggestion_button:not(.disabled) span:hover, .uds_coauthor_wrapper input {{
    box-shadow: none !important;
    border: 1px solid {BorderHex} !important;
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper input:focus {{
    box-shadow: none !important;
    border-left: 1px solid {IndicatorHex} !important;
    border-right: 0px;
    border-top: 1px solid {IndicatorHex} !important;
    border-bottom: 1px solid {IndicatorHex} !important;
}}
.uds_coauthor_wrapper #custom_tone_add_button, .uds_coauthor_wrapper #custom_tone_save_button, .uds_coauthor_wrapper #change_suggestions_input, .uds_coauthor_wrapper #submit_change_suggestion_button {{
    box-shadow: none !important;
    border-left: 0px;
    border-right: 1px solid {IndicatorHex} !important;
    border-top: 1px solid {IndicatorHex} !important;
    border-bottom: 1px solid {IndicatorHex} !important;
}}
.uds_coauthor_wrapper .preview-options {{
    background: {SecondaryHex} !important;
    right: 10px !important;
    width: auto !important;
    border-radius: 5px !important;
    border: 1px solid {BorderHex};
    margin-bottom: 10px !important;
}}
.uds_coauthor_wrapper .preview-options .item.disabled:hover {{
    background: transparent !important;
}}
.uds_coauthor_wrapper .preview-options .item:nth-child(2) {{
    margin-left: 0px !important;
}}
.uds_coauthor_wrapper .preview-options .item {{
    width: 35px !important;
    height: 35px !important;
    border-radius: 5px !important;
    margin-left: 5px;
}}
.uds_coauthor_wrapper .preview-options .item div {{
    align-items: center;
    align-self: center;
    display: flex;
}}
.uds_coauthor_wrapper .change-suggestion.tag {{
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper #compose_button:not(.disabled) {{
    background: {IndicatorHex} !important;
}}
.uds_coauthor_wrapper .button.disabled {{
    outline: 1px solid {BorderHex} !important;
    outline-offset: -1px;
    background: {SecondaryHex} !important;
    color: {GrayHex} !important;
    cursor: not-allowed !important;
}}`;
const style = document.createElement('style');
style.type = 'text/css';
style.innerHTML = CSSVariables;
document.head.appendChild(style);
const insertButton = document.querySelector('#insert_button');
if (insertButton)
    insertButton.remove();";
                /*string ComposeJS = $@"
const CSSVariables = `
::-webkit-scrollbar {{
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper .sidebar {{
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper .child {{
    background: {SecondaryHex} !important;
    border: 1px solid {BorderHex} !important;
    box-shadow: none !important;
}}

.zpcarousel .wt_cont {{
    border-radius: 5px !important;
}}

.zpcarousel .slide .cimg_cont {{
    border-radius: 5px !important;
}}

.uds_coauthor_wrapper .button {{
    border-radius: 5px !important;
}}

.uds_coauthor_wrapper .tag {{
    border-radius: 5px !important;
    color: white !important;
    border-color: {BorderHex} !important;
    background: {SecondaryHex} !important;
    color: {FontHex} !important;
}}

.uds_coauthor_wrapper .tag.selected {{
    background: linear-gradient(130deg, #2870EA 20%, #1B4AEF 77.5%) !important;
    border-color: #3399FF !important;
    color: white !important;
}}
.uds_coauthor_wrapper .tag.selected svg>path {{
    fill: white !important;
}}
.uds_coauthor_wrapper .tag:hover:not(.selected) {{
    background: {BorderHex} !important;
}}

.uds_coauthor_wrapper textarea {{
    border-radius: 5px !important;
    box-shadow: none !important;
    border: 1px solid {BorderHex} !important;
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper #letter_counter {{
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper textarea:focus-visible {{
    border: 1px solid #3399FF !important
}}

.uds_coauthor_wrapper .option-section {{
    border-radius: 5px !important;
    border-color: {BorderHex} !important;
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper .format-option .illustration {{
    border-radius: 5px !important;
    border: 1px solid {BorderHex} !important;
    background: {SecondaryHex} !important;
}}
.uds_coauthor_wrapper .format-option.selected .illustration {{
    border-color: #3399FF !important;
    background: linear-gradient(130deg, #2870EA 20%, #1B4AEF 77.5%) !important;
}}
.uds_coauthor_wrapper .format-option.selected .illustration svg>path {{
    fill-opacity: 1 !important;
    opacity: 1 !important;
    fill: white !important;
}}
.uds_coauthor_wrapper .format-option:hover:not(.selected) .illustration {{
    background: {BorderHex} !important;
}}

.uds_coauthor_wrapper #custom_tone_plus_button:not(.selected) svg>path, .uds_coauthor_wrapper #custom_tone_edit_button:not(.selected) svg>path, .uds_coauthor_wrapper #add_change_suggestion_button:not(.selected) svg>path {{
    fill: {FontHex} !important;
}}

.uds_coauthor_wrapper #custom_tone_add_button, .uds_coauthor_wrapper #custom_tone_save_button, .uds_coauthor_wrapper #custom_tone_add_button:not(.disabled) span:hover, .uds_coauthor_wrapper #custom_tone_save_button:not(.disabled) span:hover, .uds_coauthor_wrapper #submit_change_suggestion_button, .uds_coauthor_wrapper #submit_change_suggestion_button:not(.disabled) span:hover, .uds_coauthor_wrapper input {{
    box-shadow: none !important;
    border: 1px solid {BorderHex} !important;
    background: {PrimaryHex} !important;
}}
.uds_coauthor_wrapper input:focus {{
    box-shadow: none !important;
    border-left: 1px solid #3399FF !important;
    border-right: 0px;
    border-top: 1px solid #3399FF !important;
    border-bottom: 1px solid #3399FF !important;
}}
.uds_coauthor_wrapper #custom_tone_add_button, .uds_coauthor_wrapper #custom_tone_save_button, .uds_coauthor_wrapper #change_suggestions_input, .uds_coauthor_wrapper #submit_change_suggestion_button {{
    box-shadow: none !important;
    border-left: 0px;
    border-right: 1px solid #3399FF !important;
    border-top: 1px solid #3399FF !important;
    border-bottom: 1px solid #3399FF !important;
}}
.uds_coauthor_wrapper .preview-options {{
    background: {SecondaryHex} !important;
    right: 10px !important;
    width: auto !important;
    border-radius: 5px !important;
    border: 1px solid {BorderHex};
    margin-bottom: 10px !important;
}}

.uds_coauthor_wrapper .preview-options .item.disabled:hover {{
    background: transparent !important;
}}

.uds_coauthor_wrapper .preview-options .item:nth-child(2) {{
    margin-left: 0px !important;
}}
.uds_coauthor_wrapper .preview-options .item {{
    width: 35px !important;
    height: 35px !important;
    border-radius: 5px !important;
    margin-left: 5px;
}}

.uds_coauthor_wrapper .preview-options .item div {{
    align-items: center;
    align-self: center;
    display: flex;
}}

.uds_coauthor_wrapper .change-suggestion.tag {{
    background: {PrimaryHex} !important;
}}

.uds_coauthor_wrapper #compose_button {{
    background: linear-gradient(130deg, #2870EA 20%, #1B4AEF 77.5%) !important;
}}
`;

const style = document.createElement('style');
style.type = 'text/css';
style.innerHTML = CSSVariables;
document.head.appendChild(style);

const insertButton = document.querySelector('#insert_button');
if (insertButton) {{
    insertButton.remove();
}}";*/
                /*
                .primary-row button > * {{
                    --icon-size: 20px !important;
                }}
                .primary-row .description {{
                    height: 35px !important;
                    align-content: center !important;
                }}`;*/
                Dispatcher.Invoke(() =>
                {
                    AIChatBrowser.Visibility = Visibility.Visible;
                    if (AIChatBrowser.Address.StartsWith("https://edgeservices.bing.com/edgesvc/compose", StringComparison.Ordinal))
                        AIChatBrowser.ExecuteScriptAsync(ComposeJS);
                    else
                    {
                        AIChatBrowser.ExecuteScriptAsync(ChatJS);
                        TaskScheduler syncContextScheduler = (SynchronizationContext.Current != null) ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current;
                        Task.Factory.StartNew(() => Thread.Sleep(500))
                        .ContinueWith((t) =>
                        {
                            AIChatBrowser.ExecuteScriptAsync(@"var elements=document.querySelectorAll('.b_wlcmLogo');
elements.forEach(function(logo){
    if (logo.shadowRoot){
        const defsElement=logo.shadowRoot.querySelector(""defs"");
        if (defsElement){
            defsElement.innerHTML = `<radialGradient id='b' cx='0' cy='0' r='1' gradientUnits='userSpaceOnUse' gradientTransform='matrix(-18.09451 -22.11268 20.79145 -17.01336 58.88 31.274)'>
                    <stop offset='0.0955758' stop-color='#00AEFF'></stop>
                    <stop offset='0.773185' stop-color='#2253CE'></stop>
                    <stop offset='1' stop-color='#0736C4'></stop>
                </radialGradient>
                <radialGradient id='c' cx='0' cy='0' r='1' gradientUnits='userSpaceOnUse' gradientTransform='rotate(43.896 -55.288 43.754) scale(25.1554 24.7085)'>
                    <stop stop-color='#FFB657'></stop>
                    <stop offset='0.633728' stop-color='#FF5F3D'></stop>
                    <stop offset='0.923392' stop-color='#C02B3C'></stop>
                </radialGradient>
                <radialGradient id='f' cx='0' cy='0' r='1' gradientUnits='userSpaceOnUse' gradientTransform='matrix(-22.94248 56.77892 -69.48524 -28.07668 64.343 17.504)'>
                    <stop offset='0.0661714' stop-color='#8C48FF'></stop>
                    <stop offset='0.5' stop-color='#F2598A'></stop>
                    <stop offset='0.895833' stop-color='#FFB152'></stop>
                </radialGradient>
                <linearGradient id='d' x1='13.637' y1='3.994' x2='21.673' y2='52.601' gradientUnits='userSpaceOnUse'>
                    <stop offset='0.156162' stop-color='#0D91E1'></stop>
                    <stop offset='0.487484' stop-color='#52B471'></stop>
                    <stop offset='0.652394' stop-color='#98BD42'></stop>
                    <stop offset='0.937361' stop-color='#FFC800'></stop>
                </linearGradient>
                <linearGradient id='e' x1='20.452' y1='3.994' x2='22.389' y2='49.999' gradientUnits='userSpaceOnUse'>
                    <stop stop-color='#3DCBFF'></stop>
                    <stop offset='0.246674' stop-color='#0588F7' stop-opacity='0'></stop>
                </linearGradient>
                <linearGradient id='g' x1='66.24' y1='20.254' x2='63.34' y2='38.021' gradientUnits='userSpaceOnUse'>
                    <stop offset='0.0581535' stop-color='#F8ADFA'></stop>
                    <stop offset='0.708063' stop-color='#A86EDD' stop-opacity='0'></stop>
                </linearGradient>
                <clipPath id='a'>
                    <path fill='#fff' d='M0 0h72v72H0z'></path>
                </clipPath>`;
        }
    }
});
function applyStyles(){
    const applyCSS=(element,css)=>{const style=document.createElement('style');style.textContent=css;element.appendChild(style);};
    document.querySelectorAll('.b_wlcmPersName').forEach(function(element){element.parentNode.removeChild(element);});
    document.querySelectorAll('.b_wlcmPersDesc').forEach(function(element){element.parentNode.removeChild(element);});
    document.querySelectorAll('.b_wlcmPersAuthorText').forEach(function(element){element.parentNode.removeChild(element);});
    document.querySelectorAll('.b_wlcmCont').forEach(function(element){if (element.id=='b_sydWelcomeTemplate_'){document.querySelectorAll('.b_ziCont').forEach(function(element){element.parentNode.removeChild(element);});}});
    const cibChatTurns=document.querySelector('.cib-serp-main').shadowRoot.querySelector('#cib-conversation-main').shadowRoot.querySelectorAll('cib-chat-turn');
    cibChatTurns.forEach(cibChatTurn=>{
            const cibMessageGroups=cibChatTurn.shadowRoot.querySelectorAll('cib-message-group');
                cibMessageGroups.forEach(cibMessageGroup => {
            const cibMessageGroupShadowRoot=cibMessageGroup.shadowRoot;
            const header=cibMessageGroupShadowRoot.querySelector('.header');
            const cibMessage=cibMessageGroupShadowRoot.querySelector('cib-message');
            if (cibMessage){
                const cibShared=cibMessage.shadowRoot.querySelector('cib-shared');
                const footer=cibMessage.shadowRoot.querySelector('.footer');
                if (cibShared){
                    applyCSS(cibMessage.shadowRoot,`:host([source=user]) .text-message-content div{
    background:var(--cib-color-fill-accent-gradient-primary) !important;
    border-radius:5px !important;
    padding:15px !important;
    text-align:right !important;
    align-self:flex-end;
    color:white !important;
}
:host([source=user]) .text-message-content[user] img{margin-inline-end:0px !important;margin-inline-start:auto !important;}
:host([source=user]) .footer{align-self:flex-end !important;}`);
                }
                if (footer){
                    const cibMessageActions=footer.querySelector('cib-message-actions');
                    if (cibMessageActions){
                        const cibMessageActionsShadowRoot=cibMessageActions.shadowRoot;
                        const container = cibMessageActionsShadowRoot.querySelector('.container');
                        if (container){
                            const searchButton=container.querySelector('#search-on-bing-button');
                            if (searchButton)
                                searchButton.remove();
                        }
                    }
                }
            }
            if (header)
                applyCSS(cibMessageGroupShadowRoot,`:host([source=user]) .header{align-self:flex-end !important;} :host([source=user]){align-items:flex-end;}`);
            });
    });
}
applyStyles();
new MutationObserver(applyStyles).observe(document.body,{attributes:true,childList:true,subtree:true});");

                            /*if (AIChatBrowser.Address.Contains("mobfull,moblike"))
                                AIChatBrowser.ExecuteScriptAsync(@"const LateStyle=document.createElement('style');
LateStyle.type='text/css';
LateStyle.innerHTML=`.zero_state_item{border-radius:5px !important;}`;
document.querySelector('.cib-serp-main').shadowRoot.querySelector('#cib-conversation-main').shadowRoot.querySelector('cib-welcome-container').shadowRoot.querySelector('.zero_state_wrap').shadowRoot.appendChild(LateStyle);
document.querySelector('.cib-serp-main').shadowRoot.querySelector('#cib-conversation-main').shadowRoot.querySelector('cib-welcome-container').shadowRoot.querySelector('.zero_state_wrap').shadowRoot.querySelector('.hello_text').innerHTML=""Suggestions"";
document.querySelector('.cib-serp-main').shadowRoot.querySelector('#cib-conversation-main').shadowRoot.querySelector('cib-welcome-container').shadowRoot.querySelector('.preview-container').remove();");*/
                        }, syncContextScheduler);
                    }
                });
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

        public void AIChatFeature(int Feature)
        {
            if (AIChatBrowser != null)
            {
                switch (Feature)
                {
                    case 0:
                        string AIChatAddress = "http://edgeservices.bing.com/edgesvc/chat?udsframed=1&form=SHORUN&clientscopes=chat,noheader,channelstable";
                        if (App.Instance.CurrentTheme.DarkWebPage)
                            AIChatAddress += "&darkschemeovr=1";
                        AIChatBrowser.Address = AIChatAddress;
                        break;
                    /*case 1:
                        string AIMobAddress = "http://edgeservices.bing.com/edgesvc/chat?udsframed=1&form=SHORUN&clientscopes=chat,noheader,mobfull,moblike";
                        if (App.Instance.CurrentTheme.DarkWebPage)
                            AIMobAddress += "&darkschemeovr=1";
                        AIChatBrowser.Address = AIMobAddress;
                        break;*/
                    case 2:
                        string AIComposeAddress = "http://edgeservices.bing.com/edgesvc/compose?udsframed=1&clientscopes=chat,noheader";
                        if (App.Instance.CurrentTheme.DarkWebPage)
                            AIComposeAddress += "&darkschemeovr=1";
                        AIChatBrowser.Address = AIComposeAddress;
                        break;
                }
            }
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
                var infoWindow = new PromptDialogWindow("Prompt", $"Add Favourite", "Name", Title);
                infoWindow.Topmost = true;
                if (infoWindow.ShowDialog() == true)
                {
                    App.Instance.Favourites.Add(new ActionStorage(infoWindow.UserInput, $"4<,>{Address}", Address));
                    FavouriteButton.Content = "\xEB52";
                    FavouriteButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
                    FavouriteButton.ToolTip = "Remove from favourites";
                    Tab.FavouriteCommandHeader = "Remove from favourites";
                }
            }
            SetFavouritesBarVisibility();
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
                string _ScreenshotFormat = App.Instance.GlobalSave.Get("ScreenshotFormat");
                string FileExtension = "jpg";
                CaptureScreenshotFormat ScreenshotFormat = CaptureScreenshotFormat.Jpeg;
                if (_ScreenshotFormat == "PNG")
                {
                    FileExtension = "png";
                    ScreenshotFormat = CaptureScreenshotFormat.Png;
                }
                else if (_ScreenshotFormat == "WebP")
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
                    var contentSize = await Chromium.GetContentSizeAsync();
                    var result = await _DevToolsClient.Page.CaptureScreenshotAsync(ScreenshotFormat, null, new Viewport { Width = contentSize.Width, Height = contentSize.Height }, null, true, true);
                    File.WriteAllBytes(Url, result.Data);
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

        private void OmniBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (OmniBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                {
                    string Url = Utils.FilterUrlForBrowser(OmniBox.Text, App.Instance.GlobalSave.Get("SearchEngine"));
                    if (Url.StartsWith("javascript:", StringComparison.Ordinal))
                    {
                        Chromium.ExecuteScriptAsync(Url.Substring(11));
                        OmniBox.Text = OmniBox.Tag.ToString();
                    }
                    else if (!Utils.IsProgramUrl(Url))
                        Address = Url;
                    Keyboard.ClearFocus();
                    Chromium.Focus();
                }
                else
                {
                    if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftShift || e.Key == Key.RightShift)
                        return;
                    if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Shift)
                        return;

                    if (e.Key == Key.Back || !char.IsControl((char)KeyInterop.VirtualKeyFromKey(e.Key)))
                    {
                        Storyboard LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                        LoadingStoryboard?.Seek(TimeSpan.Zero);
                        LoadingStoryboard?.Stop();
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
                        SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");

                        PreviousOmniBoxText = OmniTextBox.Text;
                        CaretIndex = OmniTextBox.CaretIndex;
                        SelectionStart = OmniTextBox.SelectionStart;
                        SelectionLength = OmniTextBox.SelectionLength;
                        OmniBoxTimer.Stop();
                        OmniBoxTimer.Start();
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
                OmniTextBox.SelectAll();
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
        bool AllowAIButton;
        bool AllowReaderModeButton;
        int ShowExtensionButton;
        int ShowFavouritesBar;

        public async void SetAppearance(Theme _Theme, bool _AllowHomeButton, bool _AllowTranslateButton, bool _AllowAIButton, bool _AllowReaderModeButton, int _ShowExtensionButton, int _ShowFavouritesBar)
        {
            AllowHomeButton = _AllowHomeButton;
            AllowTranslateButton = _AllowTranslateButton;
            AllowAIButton = _AllowAIButton;
            AllowReaderModeButton = _AllowReaderModeButton;
            ShowExtensionButton = _ShowExtensionButton;
            ShowFavouritesBar = _ShowFavouritesBar;
            SetFavouritesBarVisibility();
            HomeButton.Visibility = AllowHomeButton ? Visibility.Visible : Visibility.Collapsed;
            AIChatButton.Visibility = AllowAIButton ? Visibility.Visible : Visibility.Collapsed;
            if (!IsLoading)
            {
                //MessageBox.Show(Address);
                //MessageBox.Show(CoAddress);
                if (Utils.IsHttpScheme(Address))
                    TranslateButton.Visibility = AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                else if (Address.StartsWith("file:", StringComparison.Ordinal))
                    TranslateButton.Visibility = Visibility.Collapsed;
                else if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                {
                    TranslateButton.Visibility = Visibility.Collapsed;
                    if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
                        AIChatButton.Visibility = Visibility.Collapsed;
                }
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

        private ObservableCollection<string> _Suggestions = new ObservableCollection<string>();
        public ObservableCollection<string> Suggestions
        {
            get { return _Suggestions; }
            set
            {
                _Suggestions = value;
                RaisePropertyChanged("Suggestions");
            }
        }
        private DispatcherTimer OmniBoxTimer;
        string PreviousOmniBoxText;
        int CaretIndex;
        int SelectionStart;
        int SelectionLength;

        private async void OmniBoxTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxTimer.Stop();
            string Text = OmniBox.Text;
            Suggestions.Clear();
            if (!bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
            {
                OmniBox.IsDropDownOpen = false;
                return;
            }
            OmniBox.Text = Text;
            if (OmniBox.Text.Trim().Length > 0)
            {
                try
                {
                    string SuggestionSource = App.Instance.GlobalSave.Get("SuggestionsSource");
                    string SuggestionsUrl = "";
                    if (SuggestionSource == "Google")
                        SuggestionsUrl = "http://suggestqueries.google.com/complete/search?client=chrome&output=toolbar&q=" + OmniBox.Text;
                    else if (SuggestionSource == "Bing")
                        SuggestionsUrl = "http://api.bing.com/osjson.aspx?query=" + OmniBox.Text;
                    else if (SuggestionSource == "Brave Search")
                        SuggestionsUrl = "http://search.brave.com/api/suggest?q=" + OmniBox.Text;
                    //else if (SuggestionSource == "Ecosia")
                    //    SuggestionsUrl = "http://ac.ecosia.org/autocomplete?type=list&q=" + OmniBox.Text;
                    else if (SuggestionSource == "DuckDuckGo")
                        SuggestionsUrl = "http://duckduckgo.com/ac/?type=list&q=" + OmniBox.Text;
                    else if (SuggestionSource == "Yahoo")
                        SuggestionsUrl = "http://ff.search.yahoo.com/gossip?output=fxjson&command=" + OmniBox.Text;
                    else if (SuggestionSource == "Wikipedia")
                        SuggestionsUrl = "http://en.wikipedia.org/w/api.php?action=opensearch&format=json&search=" + OmniBox.Text;

                    HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(SuggestionsUrl);
                    try
                    {
                        HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                        string ResponseText = new StreamReader(Response.GetResponseStream()).ReadToEnd();
                        using (JsonDocument Document = JsonDocument.Parse(ResponseText))
                        {
                            foreach (JsonElement Suggestion in Document.RootElement[1].EnumerateArray())
                                Suggestions.Add(Suggestion.GetString());
                        }
                    }
                    catch { }
                    OmniBox.IsDropDownOpen = OmniBox.Text.Trim().Length > 0 && Suggestions.Count > 0;
                }
                catch { }
            }
            Keyboard.Focus(OmniBox);
            OmniBox.Focus();
        }

        private void OmniBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
                OmniBox.Text = e.AddedItems[0].ToString();
            /*Storyboard LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
            LoadingStoryboard?.Seek(TimeSpan.Zero);
            LoadingStoryboard?.Stop();
            if (OmniBox.Text.StartsWith("search:"))
            {
                SiteInformationIcon.Text = "\xE721";
                SiteInformationText.Text = $"Search";
                SiteInformationPanel.ToolTip = $"Searching: {OmniBox.Text.Substring(7).Trim()}";
            }
            else if (OmniBox.Text.StartsWith("domain:"))
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
            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");*/

            string Url = Utils.FilterUrlForBrowser(OmniBox.Text, App.Instance.GlobalSave.Get("SearchEngine"));
            if (Url.StartsWith("javascript:", StringComparison.Ordinal))
            {
                Chromium.ExecuteScriptAsync(Url.Substring(11));
                OmniBox.Text = OmniBox.Tag.ToString();
            }
            else if (!Utils.IsProgramUrl(Url))
                Address = Url;
            Keyboard.ClearFocus();
            Chromium.Focus();
        }

        private void OmniBox_DropDownOpened(object sender, EventArgs e)
        {
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);// + 4 + 4
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
            OmniTextBox.Text = PreviousOmniBoxText;
            if (SelectionLength == 0)
            {
                OmniTextBox.Select(0, 0);
                OmniTextBox.CaretIndex = CaretIndex;
            }
            else
                OmniTextBox.Select(SelectionStart, SelectionLength);
        }

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Browser_Loaded;
            OmniTextBox = OmniBox.Template.FindName("PART_EditableTextBox", OmniBox) as TextBox;
            OmniBoxPopup = OmniBox.Template.FindName("Popup", OmniBox) as Popup;
            OmniBoxPopupDropDown = OmniBox.Template.FindName("DropDown", OmniBox) as Grid;
            OmniBox.ItemsSource = Suggestions;
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
