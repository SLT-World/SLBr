using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.CacheStorage;
using CefSharp.DevTools.Debugger;
using CefSharp.DevTools.Network;
using CefSharp.DevTools.Page;
using CefSharp.Wpf.HwndHost;
using CSCore.Tags.ID3.Frames;
using HtmlAgilityPack;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using SLBr.Controls;
using SLBr.Handlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Blink.xaml
    /// </summary>
    public partial class Browser : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        private class InspectorObject
        {
            public string devtoolsFrontendUrl { get; set; }
            public string type { get; set; }
            public string url { get; set; }
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

        Saving MainSave;
        IdnMapping _IdnMapping;
        public BrowserTabItem Tab;

        BrowserSettings _BrowserSettings;

        bool IsUtilityContainerOpen;

        public int BrowserType;
        public ChromiumWebBrowser Chromium;
        public ChromiumWebBrowser ChromiumInspector;
        public WebView2 Edge;
        public WebView2 EdgeInspector;
        IEWebBrowser IE;
        string StartupUrl;
        //IEWebBrowser IEInspector;

        //RequestHandler _RequestHandler;
        public Prompt ErrorPrompt;

        /*public void AdBlock(bool Boolean)
        {
            MainSave.Set("AdBlock", Boolean.ToString());
            _RequestHandler.AdBlock = Boolean;
        }
        public void TrackerBlock(bool Boolean)
        {
            MainSave.Set("TrackerBlock", Boolean.ToString());
            _RequestHandler.TrackerBlock = Boolean;
        }*/

        public Browser(string Url, int _BrowserType = 0, BrowserTabItem _Tab = null, BrowserSettings CefBrowserSettings = null)
        {
            InitializeComponent();
            StartupUrl = Url;
            MainSave = MainWindow.Instance.MainSave;
            _IdnMapping = MainWindow.Instance._IdnMapping;
            BrowserType = _BrowserType;
            FavouritesPanel.ItemsSource = MainWindow.Instance.Favourites;
            FavouriteListMenu.ItemsSource = MainWindow.Instance.Favourites;
            HistoryListMenu.ItemsSource = MainWindow.Instance.History;
            DownloadListMenu.ItemsSource = MainWindow.Instance.CompletedDownloads;
            PromptsPanel.ItemsSource = Prompts;
            BrowserEmulatorComboBox.Items.Add("Chromium");
            BrowserEmulatorComboBox.Items.Add("Edge");
            BrowserEmulatorComboBox.Items.Add("Internet Explorer");
            ApplyTheme(MainWindow.Instance.GetTheme());
            if (MainWindow.Instance.Favourites.Count == 0)
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 0);
                FavouriteContainer.Height = 1;
            }
            else
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 5);
                FavouriteContainer.Height = 33;
            }
            if (_Tab != null)
                Tab = _Tab;
            else
                Tab = MainWindow.Instance.GetTab(this);
            //_RequestHandler = new RequestHandler(this);
            Tab.Icon = MainWindow.Instance.GetIcon(Url);
            if (BrowserType == 0)
                CreateChromium(Url, CefBrowserSettings);
            if (BrowserType == 1)
                CreateEdge(Url);
            else if (BrowserType == 2)
                CreateIE(Url);
            SuggestionsTimer = new DispatcherTimer();
            SuggestionsTimer.Tick += SuggestionsTimer_Tick;
            SuggestionsTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            BrowserEmulatorComboBox.SelectionChanged += BrowserEmulatorComboBox_SelectionChanged;
            //Prompt(false, "Initialized", true, "Download", $"24<,>https://github.com/SLT-World/SLBr/releases/latest", $"https://github.com/SLT-World/SLBr/releases/latest", true, "\xE899");
        }

        void CreateChromium(string Url, BrowserSettings CefBrowserSettings = null)
        {
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.IsUnloaded = true;
            Chromium = new ChromiumWebBrowser();
            Chromium.JavascriptObjectRepository.Register("internal", MainWindow.Instance._PrivateJsObjectHandler, BindingOptions.DefaultBinder);
            Chromium.JavascriptObjectRepository.Register("slbr", MainWindow.Instance._PublicJsObjectHandler, BindingOptions.DefaultBinder);
            Chromium.Address = Url;
            Chromium.LifeSpanHandler = MainWindow.Instance._LifeSpanHandler;
            Chromium.DownloadHandler = MainWindow.Instance._DownloadHandler;
            Chromium.RequestHandler = MainWindow.Instance._RequestHandler;
            Chromium.MenuHandler = MainWindow.Instance._ContextMenuHandler;
            Chromium.KeyboardHandler = MainWindow.Instance._KeyboardHandler;
            Chromium.JsDialogHandler = MainWindow.Instance._JsDialogHandler;
            Chromium.PermissionHandler = MainWindow.Instance._PermissionHandler;
            Chromium.DisplayHandler = new DisplayHandler(this);
            //Chromium.AudioHandler = new AudioHandler();
            Chromium.AllowDrop = true;
            Chromium.IsManipulationEnabled = true;
            if (CefBrowserSettings != null)
                _BrowserSettings = CefBrowserSettings;
            else
            {
                _BrowserSettings = new BrowserSettings
                {
                    WindowlessFrameRate = MainWindow.Instance.Framerate,
                    Javascript = MainWindow.Instance.Javascript,
                    ImageLoading = MainWindow.Instance.LoadImages,
                    LocalStorage = MainWindow.Instance.LocalStorage,
                    Databases = MainWindow.Instance.Databases,
                    WebGl = MainWindow.Instance.WebGL,
                    BackgroundColor = System.Drawing.Color.Black.ToUInt()
                };
            }
            Chromium.BrowserSettings = _BrowserSettings;
            Chromium.IsBrowserInitializedChanged += Chromium_IsBrowserInitializedChanged;
            Chromium.JavascriptMessageReceived += Chromium_JavascriptMessageReceived;
            Chromium.LoadingStateChanged += Chromium_LoadingStateChanged;
            Chromium.ZoomLevelIncrement = 0.5f;
            Chromium.FrameLoadEnd += Chromium_FrameLoadEnd;
            Chromium.TitleChanged += Chromium_TitleChanged;
            Chromium.StatusMessage += Chromium_StatusMessage;
            Chromium.LoadError += Chromium_LoadError;

            CoreContainer.Children.Add(Chromium);

            ChromiumInspector = new ChromiumWebBrowser("about:blank");
            ChromiumInspector.Address = "about:blank";
            ChromiumInspector.BrowserSettings = new BrowserSettings
            {
                WebGl = CefState.Disabled,
                WindowlessFrameRate = 20,
                BackgroundColor = System.Drawing.Color.Black.ToUInt()
            };
            ChromiumInspector.AllowDrop = true;
            ChromiumInspector.FrameLoadEnd += (sender, args) =>
            {
                if (args.Frame.IsValid && args.Frame.IsMain)
                {
                    if (args.Url == "http://localhost:8089/json/list")
                    {
                        ChromiumInspector.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('body')[0].getElementsByTagName('pre')[0].innerHTML").ContinueWith(t =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                if (t.Result != null && t.Result.Result != null)
                                {
                                    List<InspectorObject> InspectorObjects = JsonConvert.DeserializeObject<List<InspectorObject>>(t.Result.Result.ToString());
                                    foreach (InspectorObject _InspectorObject in InspectorObjects)
                                    {
                                        if (_InspectorObject.type == "page" && _InspectorObject.url == Chromium.Address)
                                        {
                                            ChromiumInspector.Address = "http://localhost:8089" + _InspectorObject.devtoolsFrontendUrl.Replace("inspector.html", "devtools_app.html");
                                            //ChromiumInspector.Address = "http://localhost:8089" + (MainWindow.Instance.DeveloperMode ? _InspectorObject.devtoolsFrontendUrl : _InspectorObject.devtoolsFrontendUrl.Replace("inspector.html", "devtools_app.html"));
                                            //devtools_app.html?can_dock=true
                                            //ChromiumInspector.Address = "http://localhost:8089" + _InspectorObject.devtoolsFrontendUrl;
                                            break;
                                        }
                                    }
                                }
                            }));
                        });
                    }
                    else
                    {
                        ChromiumInspector.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(MainWindow.Instance.GetTheme().DarkWebPage ? bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebPage")) : false);
                    }
                }
            };
            ChromiumInspector.LifeSpanHandler = MainWindow.Instance._LifeSpanHandler;
            //ChromiumInspector.DownloadHandler = MainWindow.Instance._DownloadHandler;
            ChromiumInspector.RequestHandler = MainWindow.Instance._RequestHandler;
            ChromiumInspector.KeyboardHandler = MainWindow.Instance._KeyboardHandler;
            ChromiumInspector.JsDialogHandler = MainWindow.Instance._JsDialogHandler;
            //ChromiumInspector.StatusMessage += OnWebBrowserStatusMessage;
            InspectorCoreContainer.Children.Add(ChromiumInspector);

            RenderOptions.SetBitmapScalingMode(Chromium, BitmapScalingMode.LowQuality);
            RenderOptions.SetBitmapScalingMode(ChromiumInspector, BitmapScalingMode.LowQuality);

            SwitchBrowserButton.Tag = "11<,>IE";
            SwitchBrowserButton.ToolTip = "Internet Explorer mode";
            SwitchBrowserText.Text = "e";
            BrowserEmulatorComboBox.SelectedItem = "Chromium";
        }

        private void Chromium_LoadError(object? sender, LoadErrorEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                string Host = Utils.Host(e.FailedUrl);
                switch (e.ErrorText)
                {
                    case "ERR_CONNECTION_TIMED_OUT":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description={Host} took too long to respond.";
                        break;
                    case "ERR_CONNECTION_RESET":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description=The connection was reset.";
                        break;
                    case "ERR_CONNECTION_REFUSED":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description={Host} refused to connect.";
                        break;
                    case "ERR_CONNECTION_CLOSED":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description={Host} unexpectedly closed the connection.";
                        break;

                    case "ERR_INTERNET_DISCONNECTED":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description=Internet was disconnected.";
                        break;
                    case "ERR_NAME_NOT_RESOLVED":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description=The URL entered could not be resolved.";
                        break;
                    case "ERR_NETWORK_CHANGED":
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description=The connection to {Host} was interrupted by a change in the network connected.";
                        break;
                    case "ERR_ABORTED":
                        break;
                    default:
                        Chromium.Address = $"slbr://cannotconnect?site={e.FailedUrl}&error={e.ErrorText}&description=Error Code: {e.ErrorCode}";
                        break;
                }
            }));
        }

        private void Chromium_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            Tab.BrowserCommandsVisibility = Chromium.IsBrowserInitialized ? Visibility.Visible : Visibility.Collapsed;
            Tab.IsUnloaded = false;
        }

        private void Chromium_StatusMessage(object? sender, StatusMessageEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                //StatusBar.Visibility = string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible;
                StatusBarPopup.IsOpen = !string.IsNullOrEmpty(e.Value);
                StatusMessage.Text = e.Value;
            }));
        }

        void CreateIE(string Url, bool SwitchToEdge = false)
        {
            Tab.IsUnloaded = true;
            IE = new IEWebBrowser(Url);
            IE.BrowserCore.Loaded += IE_Loaded;
            IE.BrowserCore.Navigating += IE_Navigating;
            IE.BrowserCore.Navigated += IE_Navigated;
            CoreContainer.Children.Add(IE.BrowserCore);

            if (SwitchToEdge)
            {
                SwitchBrowserButton.Tag = "11<,>Edge";
                SwitchBrowserButton.ToolTip = "Edge mode";
                SwitchBrowserText.Text = "e";
            }
            else
            {
                SwitchBrowserButton.Tag = "11<,>Chromium";
                SwitchBrowserButton.ToolTip = "Chromium mode";
                SwitchBrowserText.Text = "c";
            }
            BrowserEmulatorComboBox.SelectedItem = "Internet Explorer";

            //IEInspector = new IEWebBrowser("about:blank");
            //InspectorContainer.Children.Add(IEInspector.BrowserCore);
        }
        async void CreateEdge(string Url)
        {
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.IsUnloaded = true;
            Edge = new WebView2();
            Edge.DefaultBackgroundColor = System.Drawing.Color.Black;
            Edge.CoreWebView2InitializationCompleted += Edge_CoreWebView2InitializationCompleted;
            Edge.EnsureCoreWebView2Async(MainWindow.Instance.WebView2Environment);
            Edge.Source = new Uri(Url);
            //Chromium.JavascriptObjectRepository.Register("internal", MainWindow.Instance._PrivateJsObjectHandler, BindingOptions.DefaultBinder);
            //Chromium.JavascriptObjectRepository.Register("slbr", MainWindow.Instance._PublicJsObjectHandler, BindingOptions.DefaultBinder);
            //Chromium.LifeSpanHandler = MainWindow.Instance._LifeSpanHandler;
            //Chromium.DownloadHandler = MainWindow.Instance._DownloadHandler;
            //Chromium.RequestHandler = MainWindow.Instance._RequestHandler;
            //Chromium.MenuHandler = MainWindow.Instance._ContextMenuHandler;
            //Chromium.KeyboardHandler = MainWindow.Instance._KeyboardHandler;
            //Chromium.JsDialogHandler = MainWindow.Instance._JsDialogHandler;
            //Chromium.PermissionHandler = MainWindow.Instance._PermissionHandler;
            //Chromium.DisplayHandler = new DisplayHandler(this);
            //Chromium.AudioHandler = new AudioHandler();
            Edge.AllowDrop = true;
            Edge.IsManipulationEnabled = true;

            CoreContainer.Children.Add(Edge);

            EdgeInspector = new WebView2();
            EdgeInspector.CoreWebView2InitializationCompleted += EdgeInspector_CoreWebView2InitializationCompleted;
            EdgeInspector.DefaultBackgroundColor = System.Drawing.Color.Black;
            EdgeInspector.EnsureCoreWebView2Async(MainWindow.Instance.WebView2Environment);
            EdgeInspector.Source = new Uri("about:blank");
            EdgeInspector.AllowDrop = true;
            InspectorCoreContainer.Children.Add(EdgeInspector);

            RenderOptions.SetBitmapScalingMode(Edge, BitmapScalingMode.LowQuality);
            RenderOptions.SetBitmapScalingMode(EdgeInspector, BitmapScalingMode.LowQuality);

            SwitchBrowserButton.Tag = "11<,>IE";
            SwitchBrowserButton.ToolTip = "Internet Explorer mode";
            SwitchBrowserText.Text = "e";
            BrowserEmulatorComboBox.SelectedItem = "Edge";
        }

        private void EdgeInspector_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (EdgeInspector.Source.AbsoluteUri == "http://localhost:9222/json/list")
                {
                    EdgeInspector.ExecuteScriptAsync("document.getElementsByTagName('body')[0].getElementsByTagName('pre')[0].innerHTML").ContinueWith(t =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            string Html = t.Result.Trim('"').Replace("\\n", "").Replace("\\\"", "\"");
                            //Html = Regex.Replace(Html, @"\r\n?|\n", "");
                            List<InspectorObject> InspectorObjects = JsonConvert.DeserializeObject<List<InspectorObject>>(Html);
                            foreach (InspectorObject _InspectorObject in InspectorObjects)
                            {
                                if (_InspectorObject.type == "page" && _InspectorObject.url == Edge.Source.AbsoluteUri)
                                {
                                    EdgeInspector.CoreWebView2.Navigate("http://localhost:9222" + _InspectorObject.devtoolsFrontendUrl.Replace("inspector.html", "devtools_app.html"));
                                    break;
                                }
                            }
                        }));
                    });
                }
                /*else
                {
                    ChromiumInspector.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(MainWindow.Instance.GetTheme().DarkWebPage ? bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebPage")) : false);
                }*/
            }
            catch { }
        }
        private void EdgeInspector_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            EdgeInspector.CoreWebView2.NavigationCompleted += EdgeInspector_NavigationCompleted;
            EdgeInspector.CoreWebView2.Navigate("about:blank");
        }
        private void Edge_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            Edge.CoreWebView2.Settings.AreDevToolsEnabled = true;
            Edge.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Edge.CoreWebView2.WebMessageReceived += Edge_WebMessageReceived;
            Edge.CoreWebView2.NavigationStarting += Edge_NavigationStarting;
            Edge.CoreWebView2.NavigationCompleted += Edge_NavigationCompleted;
            //Edge.CoreWebView2.FrameNavigationStarting += Edge_FrameNavigationStarting;
            //Chromium.LoadingStateChanged += Chromium_LoadingStateChanged;
            //Chromium.ZoomLevelIncrement = 0.5f;
            //Edge.CoreWebView2.FrameCreated += Edge_FrameCreated;
            Edge.CoreWebView2.DocumentTitleChanged += Edge_TitleChanged;
            Edge.CoreWebView2.StatusBarTextChanged += Edge_StatusBarTextChanged;
            Edge.CoreWebView2.NewWindowRequested += Edge_NewWindowRequested;
            Tab.BrowserCommandsVisibility = Visibility.Visible;
            Tab.IsUnloaded = false;
        }

        /*private void Edge_FrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string CleanedUrl = Utils.CleanUrl(e.Uri, true, true, true, true);//false if check full path
            string Host = Utils.Host(CleanedUrl, true);
            if (Analytics.Contains(Host))
            {
                e.Cancel = true;
                MainWindow.Instance.TrackersBlocked++;
            }
            else if (Ads.Contains(Host))
            {
                e.Cancel = true;
                MainWindow.Instance.AdsBlocked++;
            }
        }*/

        private void Edge_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            //if (e.WindowFeatures. == WindowOpenDisposition.CurrentTab)
            //    browser.MainFrame.LoadUrl(targetUrl);
            //else
            {
                string _Source = e.Uri;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    //if (e.NewWindow.targetDisposition == WindowOpenDisposition.NewPopup)
                    //    new PopupBrowser(e.NewWindow.Source, -1, -1).Show();
                    //else
                        MainWindow.Instance.NewBrowserTab(_Source, 1, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                }));
            }
            e.Handled = true;
        }

        private void Edge_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                BrowserLoadChanged(Edge.Source.AbsoluteUri);

                if (IsUtilityContainerOpen && !EdgeInspector.Source.AbsoluteUri.StartsWith("http://localhost:9222/"))
                    EdgeInspector.Source = new Uri("http://localhost:9222/json/list");
                Tab.Header = Title;
                ReloadButton.Content = "\xE72C";
                WebsiteLoadingProgressBar.IsEnabled = false;
                WebsiteLoadingProgressBar.IsIndeterminate = false;
                BackButton.IsEnabled = Edge.CanGoBack;
                ForwardButton.IsEnabled = Edge.CanGoForward;

                string OutputUrl = Utils.ConvertUrlToReadableUrl(_IdnMapping, bool.Parse(MainSave.Get("FullAddress")) ? Edge.Source.AbsoluteUri : Utils.CleanUrl(Edge.Source.AbsoluteUri));
                if (AddressBox.Text != OutputUrl)
                {
                    if (CanChangeAddressBox())
                    {
                        AddressBox.Text = OutputUrl;
                        AddressBoxPlaceholder.Text = "";
                    }
                    AddressBox.Tag = Edge.Source.AbsoluteUri;
                }
            }));
        }
        private void Edge_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsUtilityContainerOpen && !EdgeInspector.Source.AbsoluteUri.StartsWith("http://localhost:9222/"))
                    EdgeInspector.Source = new Uri("http://localhost:9222/json/list");
                ReloadButton.Content = "\xE711";
                WebsiteLoadingProgressBar.IsEnabled = true;
                WebsiteLoadingProgressBar.IsIndeterminate = true;
                BackButton.IsEnabled = Edge.CanGoBack;
                ForwardButton.IsEnabled = Edge.CanGoForward;
            }));
        }
        private void Edge_StatusBarTextChanged(object? sender, object e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                //StatusBar.Visibility = string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible;
                StatusBarPopup.IsOpen = !string.IsNullOrEmpty(Edge.CoreWebView2.StatusBarText);
                StatusMessage.Text = Edge.CoreWebView2.StatusBarText;
            }));
        }
        private void Edge_TitleChanged(object? sender, object e)
        {
            Tab.Header = Title;
            if (Tab == MainWindow.Instance.Tabs[MainWindow.Instance.BrowserTabs.SelectedIndex])
                MainWindow.Instance.Title = Title + " - SLBr";
        }
        private void Edge_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            object[] objArray = JsonConvert.DeserializeObject<object[]>(e.WebMessageAsJson.ToString());
            MessageOptions options = JsonConvert.DeserializeObject<MessageOptions>(objArray[1].ToString(), new ImageConverter());
            ToastBox.Show(options.tag, options.body, 10);
            MessageQueue.Enqueue(new object[] { objArray[0], options });
        }

        private void IE_Loaded(object sender, RoutedEventArgs e)
        {
            IE.BrowserCore.Loaded -= IE_Loaded;
            SuppressIEScriptErrors(bool.Parse(MainWindow.Instance.IESave.Get("IESuppressErrors")));
        }
        private void SuppressIEScriptErrors(bool Hide)
        {
            if (!IE.BrowserCore.IsLoaded)
            {
                IE.BrowserCore.Loaded += IE_Loaded;
                return;
            }
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(IE.BrowserCore);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        public Queue<object[]> MessageQueue = new Queue<object[]>();
        private void Chromium_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            object[] objArray = JsonConvert.DeserializeObject<object[]>(e.Message.ToString());
            MessageOptions options = JsonConvert.DeserializeObject<MessageOptions>(objArray[1].ToString(), new ImageConverter());
            ToastBox.Show(options.tag, options.body, 10);
            MessageQueue.Enqueue(new object[] { objArray[0], options });
        }
        private void Chromium_FrameLoadEnd(object? sender, FrameLoadEndEventArgs e)
        {
            if (bool.Parse(MainSave.Get("WebNotifications")))
                Chromium.ExecuteScriptAsync(@"(function(){ class Notification {
                    static permission = 'granted';
                    static maxActions = 2;
                    static name = 'Notification';
                    constructor(title, options) {
                        let packageSet = new Set();
                        packageSet.add(title).add(options);
                        let json_package = JSON.stringify([...packageSet]);
                        CefSharp.PostMessage(json_package);
                        //alert(title);
                    }
                    static requestPermission() {
                        return new Promise((res, rej) => {
                            res('granted');
                        })
                    }
                };
                window.Notification = Notification;
                })();");
            //Chromium.ExecuteScriptAsync(@"Object.defineProperty(navigator.connection, ""saveData"", { get: function() { return true; } });");

            //Chromium.ExecuteScriptAsync("window.navigator.vendor = \"SLT World\"");
            //Chromium.ExecuteScriptAsync("window.navigator.deviceMemory = 0.25");
            if (e.Frame.IsValid && e.Frame.IsMain)
            {
                string ArgsUrl = e.Url;
                int HttpStatusCode = e.HttpStatusCode;
                if (ErrorPrompt != null)
                    ClosePrompt(ErrorPrompt);
                if (HttpStatusCode == 404 && !ArgsUrl.StartsWith("https://web.archive.org/web/"))
                {
                    ErrorPrompt = Prompt($"{HttpStatusCode}, Do you want open the page in the Wayback Machine?", "Open", $"21<,>https://web.archive.org/{ArgsUrl}", $"https://web.archive.org/{ArgsUrl}", "\xE8FF");
                }
                if (HttpStatusCode == 444 || (HttpStatusCode >= 500 && HttpStatusCode <= 599))
                {
                    ErrorPrompt = Prompt($"{HttpStatusCode}, Cannot connect to server.", "", $"", $"", "\xE8FF");
                }
                else if (HttpStatusCode == 418)
                {
                    ErrorPrompt = Prompt($"I'm a teapot");
                }
            }
        }

        public void Unload(bool ChangeIcon, BrowserSettings _BrowserSettings = null)
        {
            if (BrowserType == 0)
            {
                string Url = Chromium.Address;
                
                //if (ChangeIcon && Tab.Icon != null)
                if (ChangeIcon && Chromium.IsBrowserInitialized)
                    Tab.Icon = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "Green Sustainable Icon.png")));
                CoreContainer.Children.Clear();
                InspectorCoreContainer.Children.Clear();
                Chromium.Dispose();
                ChromiumInspector.Dispose();
                CreateChromium(Url, _BrowserSettings);
            }
            else if (BrowserType == 1)
            {
                string Url = Edge.Source.AbsoluteUri;
                //if (ChangeIcon && Tab.Icon != null)
                if (ChangeIcon && Edge.CoreWebView2 != null)
                    Tab.Icon = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "Green Sustainable Icon.png")));
                CoreContainer.Children.Clear();
                InspectorCoreContainer.Children.Clear();
                Edge.Dispose();
                EdgeInspector.Dispose();
                CreateEdge(Url);
            }
            /*else if (BrowserType == 2)
            {
                string Url = IE.BrowserCore.Source.AbsoluteUri;
                if (ChangeIcon && Tab.Icon != null)
                    Tab.Icon = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "Green Sustainable Icon.png")));
                CoreContainer.Children.Clear();
                InspectorCoreContainer.Children.Clear();
                IE.Dispose();
                //IEInspector.Dispose();
                CreateIE(Url);
            }*/
            GC.Collect();
        }
        public void Unload(bool ChangeIcon, int _Framerate, CefState JSState, CefState LIState, CefState LSState, CefState DBState, CefState WebGLState)
        {
            if (BrowserType == 0)
            {
                _BrowserSettings = new BrowserSettings
                {
                    WindowlessFrameRate = _Framerate,
                    Javascript = JSState,
                    ImageLoading = LIState,
                    LocalStorage = LSState,
                    Databases = DBState,
                    WebGl = WebGLState,
                    BackgroundColor = System.Drawing.Color.Black.ToUInt()
                };
                Unload(ChangeIcon, _BrowserSettings);
            }
            else
                Unload(ChangeIcon);
        }
        /*private void Chromium_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CtrlKeyDown)
            {
                if (e.Delta > 0)// && Chromium.ZoomLevel <= MaximumZoomLevel
                    Chromium.ZoomInCommand.Execute(null);
                else if (e.Delta < 0)// && Chromium.ZoomLevel >= MinimumZoomLevel
                    Chromium.ZoomOutCommand.Execute(null);
            }
        }*/

        public Prompt Prompt(string Content, string ButtonContent = "", string ButtonArguments = "", string ToolTip = "", string IconText = "", string IconRotation = "")
        {
            int Count = Prompts.Count;
            Prompt _Prompt = new Prompt { Content = Content, ButtonVisibility = !string.IsNullOrEmpty(ButtonContent) ? Visibility.Visible : Visibility.Collapsed, ButtonToolTip = ToolTip, ButtonContent = ButtonContent, ButtonTag = ButtonArguments + (ButtonArguments.StartsWith("21") ? $"<,>{Count}" : ""), CloseButtonTag = $"19<,>{Count}", IconVisibility = !string.IsNullOrEmpty(IconText) ? Visibility.Visible : Visibility.Collapsed, IconText = IconText, IconRotation = IconRotation };
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Prompts.Add(_Prompt);
            }));
            return _Prompt;
        }
        public void ClosePrompt(int Index)
        {
            if (Prompts.Count > 0 && Prompts[Index] != null)
            {
                Prompts.RemoveAt(Index);
                for (int i = 0; i < Prompts.Count; i++)
                {
                    Prompt _Prompt = Prompts[i];
                    _Prompt.CloseButtonTag = $"19<,>{Prompts.IndexOf(_Prompt)}";
                    if (_Prompt.ButtonTag.StartsWith("20"))
                        _Prompt.ButtonTag = Utils.RemoveCharsAfterLastChar(_Prompt.ButtonTag, "<,>", true) + Prompts.IndexOf(_Prompt).ToString();
                }
            }
        }
        public void ClosePrompt(Prompt _PromptToClose)
        {
            if (Prompts.Count > 0)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    Prompts.Remove(_PromptToClose);
                }));
                for (int i = 0; i < Prompts.Count; i++)
                {
                    Prompt _Prompt = Prompts[i];
                    _Prompt.CloseButtonTag = $"19<,>{Prompts.IndexOf(_Prompt)}";
                    if (_Prompt.ButtonTag.StartsWith("20"))
                        _Prompt.ButtonTag = Utils.RemoveCharsAfterLastChar(_Prompt.ButtonTag, "<,>", true) + Prompts.IndexOf(_Prompt).ToString();
                }
                //foreach (Prompt _Prompt in Prompts)
                //{
                //    _Prompt.CloseButtonTag = $"19<,>{Prompts.IndexOf(_Prompt)}";
                //    if (_Prompt.ButtonTag.StartsWith("20"))
                //        _Prompt.ButtonTag = Utils.RemoveCharsAfterLastChar(_Prompt.ButtonTag, "<,>", true) + Prompts.IndexOf(_Prompt).ToString();
                //}
            }
        }

        private void IE_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Tab.BrowserCommandsVisibility = Visibility.Visible;
            Tab.IsUnloaded = false;
            if (IsUtilityContainerOpen)
            {
                try
                {
                    dynamic document = IE.BrowserCore.Document;
                    dynamic script = document.createElement("script");
                    script.type = @"text/javascript";
                    script.src = @"https://lupatec.eu/getfirebug/firebug-lite-compressed.js#startOpened=true,disableWhenFirebugActive=false";
                    document.head.appendChild(script); // Dynamic property head does not exist.
                }
                catch { };
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IE.BrowserCore.Source == null)
                    return;
                BrowserLoadChanged(IE.BrowserCore.Source.AbsoluteUri);
                Tab.Header = Title;
                ReloadButton.Content = "\xE72C";
                WebsiteLoadingProgressBar.IsEnabled = false;
                WebsiteLoadingProgressBar.IsIndeterminate = false;
                BackButton.IsEnabled = IE.BrowserCore.CanGoBack;
                ForwardButton.IsEnabled = IE.BrowserCore.CanGoForward;

                string OutputUrl = Utils.ConvertUrlToReadableUrl(_IdnMapping, bool.Parse(MainSave.Get("FullAddress")) ? e.Uri.AbsoluteUri : Utils.CleanUrl(e.Uri.AbsoluteUri));
                if (AddressBox.Text != OutputUrl)
                {
                    if (CanChangeAddressBox())
                    {
                        AddressBox.Text = OutputUrl;
                        AddressBoxPlaceholder.Text = "";
                    }
                    AddressBox.Tag = e.Uri.AbsoluteUri;
                }
            }));
        }
        private void IE_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReloadButton.Content = "\xE711";
                WebsiteLoadingProgressBar.IsEnabled = true;
                WebsiteLoadingProgressBar.IsIndeterminate = true;
                BackButton.IsEnabled = IE.BrowserCore.CanGoBack;
                ForwardButton.IsEnabled = IE.BrowserCore.CanGoForward;
            }));
        }

        void BrowserLoadChanged(string Address)
        {
            QRCodePopup.IsOpen = false;
            MainWindow.Instance.AddHistory(Address);
            string Host = Utils.Host(Address);
            Tab.Icon = MainWindow.Instance.GetIcon(Address);

            if (Address.StartsWith("https:"))
            {
                SSLSymbol.Text = "\xE72E";
                SSLSymbol.Foreground = new SolidColorBrush(Colors.LimeGreen);
                SSLToolTip.Content = $"Connection to {Host} is secure";
                QRCodeButton.Visibility = Visibility.Visible;
                TranslateButton.Visibility = Visibility.Visible;
                OpenFileExplorerButton.Visibility = Visibility.Collapsed;
            }
            else if (Address.StartsWith("http:"))
            {
                SSLSymbol.Text = "\xE785";
                SSLSymbol.Foreground = new SolidColorBrush(Colors.Red);
                SSLToolTip.Content = $"Connection to {Host} is not secure";
                QRCodeButton.Visibility = Visibility.Visible;
                TranslateButton.Visibility = Visibility.Visible;
                OpenFileExplorerButton.Visibility = Visibility.Collapsed;
            }
            else if (Address.StartsWith("file:"))
            {
                SSLSymbol.Text = "\xE8B7";
                SSLSymbol.Foreground = new SolidColorBrush(Colors.NavajoWhite);
                SSLToolTip.Content = $"Local or shared file";
                QRCodeButton.Visibility = Visibility.Collapsed;
                TranslateButton.Visibility = Visibility.Collapsed;
                OpenFileExplorerButton.Visibility = Visibility.Visible;
            }
            else
            {
                SSLSymbol.Text = "\xE774";
                SSLSymbol.Foreground = new SolidColorBrush(Colors.CornflowerBlue);
                SSLToolTip.Content = $"Network protocol";
                QRCodeButton.Visibility = Visibility.Collapsed;
                TranslateButton.Visibility = Visibility.Collapsed;
                OpenFileExplorerButton.Visibility = Visibility.Collapsed;
            }
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                Tab.FavouriteCommandHeader = "Add from favourites";
            }
            if (MainWindow.Instance.Favourites.Count == 0)
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 0);
                FavouriteContainer.Height = 1;
            }
            else
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 5);
                FavouriteContainer.Height = 33;
            }
        }

        private void Chromium_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Tab.Header = Title;
            if (Tab == MainWindow.Instance.Tabs[MainWindow.Instance.BrowserTabs.SelectedIndex])
                MainWindow.Instance.Title = Title + " - SLBr";
        }
        private void Chromium_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!Chromium.IsBrowserInitialized)
                    return;
                BrowserLoadChanged(Chromium.Address);

                if (!Chromium.IsLoading)
                {
                    if (IsUtilityContainerOpen && !ChromiumInspector.Address.StartsWith("http://localhost:8089/"))
                        ChromiumInspector.Address = "localhost:8089/json/list";
                        //if (IsUtilityContainerOpen && !ChromiumInspector.Address.StartsWith("http://localhost:8089/devtools/"))
                    if (Chromium.Address.EndsWith("github.com/SLT-World/SLBr"))
                        ToastBox.Show("", "Please support SLBr by giving a star to the project.", 10);
                }
                else
                {
                    if (IsUtilityContainerOpen && !ChromiumInspector.Address.StartsWith("http://localhost:8089/"))
                        ChromiumInspector.Address = "localhost:8089/json/list";
                    if (Chromium.Address.StartsWith("slbr:"))
                        Chromium.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"internal\");");
                    Chromium.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"slbr\");");
                }
                ReloadButton.Content = e.IsLoading ? "\xE711" : "\xE72C";
                //WebsiteLoadingProgressBar.IsEnabled = e.IsLoading;
                //WebsiteLoadingProgressBar.IsIndeterminate = e.IsLoading;
                BackButton.IsEnabled = e.CanGoBack;
                ForwardButton.IsEnabled = e.CanGoForward;
                DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
                _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(MainWindow.Instance.GetTheme().DarkWebPage ? bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebPage")) : false);
            }));
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
        public void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            V1 = V1.Replace("{CurrentUrl}", Address);
            if (BrowserType == 0)
                V1 = V1.Replace("{CurrentInspectorUrl}", ChromiumInspector.Address);
            else if (BrowserType == 1)
                V1 = V1.Replace("{CurrentInspectorUrl}", EdgeInspector.Source.AbsoluteUri);
            V1 = V1.Replace("{Homepage}", MainSave.Get("Homepage"));
            switch (_Action)
            {
                case Actions.Undo:
                    Back();
                    break;
                case Actions.Redo:
                    Forward();
                    break;
                case Actions.Refresh:
                    Refresh();
                    break;
                case Actions.Navigate:
                    Navigate(V1);
                    break;
                case Actions.CreateTab:
                    MainWindow.Instance.NewBrowserTab(V1, 0, true);
                    break;
                case Actions.CloseTab:
                    MainWindow.Instance.CloseBrowserTab(int.Parse(V1));
                    break;
                case Actions.Inspect:
                    Inspect();
                    break;
                case Actions.Favourite:
                    Favourite();
                    break;
                case Actions.SetAudio:
                    SetAudio(!IsAudioMuted);
                    break;
                case Actions.Settings:
                    MainWindow.Instance.OpenSettings(true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                    break;
                case Actions.UnloadTabs:
                    MainWindow.Instance.UnloadTabs(true);
                    break;
                case Actions.SwitchBrowser:
                    SwitchBrowser(V1);
                    break;
                case Actions.OpenFileExplorer:
                    OpenFileExplorer(V1);
                    break;
                case Actions.QRCode:
                    QRCode(V1);
                    break;
                case Actions.SetInspectorDock:
                    SetInspectorDock(int.Parse(V1));
                    break;
                case Actions.OpenAsPopupBrowser:
                    OpenAsPopupBrowser(V1);
                    break;
                case Actions.SizeEmulator:
                    SizeEmulator();
                    break;
                case Actions.OpenNewBrowserPopup:
                    OpenNewBrowserPopup();
                    break;
                case Actions.ClosePrompt:
                    ClosePrompt(int.Parse(V1));
                    break;
                case Actions.Prompt:
                    //NewMessage(Value1, Value2, Value3);
                    Prompt(V1, V2, V3);
                    break;
                case Actions.PromptNavigate:
                    Navigate(V1);
                    ClosePrompt(int.Parse(V2));
                    break;
            }
        }

        private void OpenNewBrowserPopup()
        {
            var infoWindow = new PromptDialogWindow("Prompt", $"New SLBr window", "Enter username", "Default-User");
            infoWindow.Topmost = true;

            if (infoWindow.ShowDialog() == true)
            {
                //MainWindow.Instance.CloseSLBr();
                ProcessStartInfo Info = new ProcessStartInfo();
                Info.Arguments = "/C choice /C Y /N /D Y /T 1 & START \"\" \"" + Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe") + "\" --user=" + infoWindow.UserInput;
                Info.WindowStyle = ProcessWindowStyle.Hidden;
                Info.CreateNoWindow = true;
                Info.FileName = "cmd.exe";
                Process.Start(Info);
                //Process.GetCurrentProcess().Kill();
            }
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
        private void SetInspectorDock(int DockID)
        {
            switch (DockID)
            {
                case 0:
                    Grid.SetColumn(InspectorContainer, 2);
                    Grid.SetRow(InspectorContainer, 1);
                    InspectorContainer.Height = Double.NaN;
                    InspectorContainer.Width = 600;
                    InspectorContainer.BorderThickness = new Thickness(1, 0, 0, 0);
                    break;
                case 1:
                    Grid.SetColumn(InspectorContainer, 0);
                    Grid.SetRow(InspectorContainer, 1);
                    InspectorContainer.Height = Double.NaN;
                    InspectorContainer.Width = 600;
                    InspectorContainer.BorderThickness = new Thickness(0, 0, 1, 0);
                    break;
                case 2:
                    Grid.SetColumn(InspectorContainer, 1);
                    Grid.SetRow(InspectorContainer, 2);
                    InspectorContainer.Height = 300;
                    InspectorContainer.Width = Double.NaN;
                    InspectorContainer.BorderThickness = new Thickness(0, 1, 0, 0);
                    break;
                case 3:
                    Grid.SetColumn(InspectorContainer, 1);
                    Grid.SetRow(InspectorContainer, 0);
                    InspectorContainer.Height = 300;
                    InspectorContainer.Width = Double.NaN;
                    InspectorContainer.BorderThickness = new Thickness(0, 0, 0, 1);
                    break;
            }
        }

        private void BrowserEmulatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox _ComboBox = (ComboBox)sender;
            SwitchBrowser(_ComboBox.SelectedValue.ToString());
        }
        public void SwitchBrowser(string NewBrowserName)
        {
            int NewBrowserType = 0;
            if (NewBrowserName == "Chromium")
                NewBrowserType = 0;
            else if (NewBrowserName == "IE")
                NewBrowserType = 2;
            else if (NewBrowserName == "Internet Explorer")
                NewBrowserType = 2;
            else if (NewBrowserName == "Edge")
                NewBrowserType = 1;
            else if (NewBrowserName == "Microsoft Edge")
                NewBrowserType = 1;
            CoreContainer.Children.Clear();
            InspectorCoreContainer.Children.Clear();
            string Url = Address;
            if (NewBrowserType == 0)//Chromium
            {
                if (BrowserType == 2)
                {
                    IE.Dispose();
                    //IEInspector.Dispose();
                }
                else if (BrowserType == 1)
                {
                    Edge.Dispose();
                    EdgeInspector.Dispose();
                }
                CreateChromium(Url);
            }
            else if (NewBrowserType == 1)//Edge
            {
                if (BrowserType == 2)
                {
                    IE.Dispose();
                    //IEInspector.Dispose();
                }
                else if (BrowserType == 0)
                {
                    Chromium.Dispose();
                    ChromiumInspector.Dispose();
                }
                CreateEdge(Url);
            }
            else if (NewBrowserType == 2)//IE
            {
                if (BrowserType == 0)
                {
                    Chromium.Dispose();
                    ChromiumInspector.Dispose();
                }
                else if (BrowserType == 1)
                {
                    Edge.Dispose();
                    EdgeInspector.Dispose();
                }
                CreateIE(Url, BrowserType == 1);
            }
            BrowserType = NewBrowserType;
        }

        public string Address
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.Address;
                else if (BrowserType == 1)
                    return Edge.Source.AbsoluteUri;
                else if (BrowserType == 2)
                    try { return IE.BrowserCore.Source.AbsoluteUri; }
                    catch { return StartupUrl; }
                return "????";
            }
            set
            {
                if (BrowserType == 0)
                    Chromium.Address = value;
                else if (BrowserType == 1)
                    Edge.Source = new Uri(value);
                else if (BrowserType == 2)
                    IE.Navigate(value);
            }
        }
        public string Title
        {
            get {
                if (BrowserType == 0)
                    return Chromium.Title != null && Chromium.Title.Trim().Length > 0 ? Chromium.Title : Utils.CleanUrl(Chromium.Address);
                else if (BrowserType == 1)
                    return Edge.CoreWebView2.DocumentTitle != null && Edge.CoreWebView2.DocumentTitle.Trim().Length > 0 ? Edge.CoreWebView2.DocumentTitle : Utils.CleanUrl(Edge.Source.AbsoluteUri);
                else if (BrowserType == 2)
                    try { return (string)IE.BrowserCore.InvokeScript("eval", "document.title.toString()"); }
                    catch { return Utils.CleanUrl(IE.BrowserCore.Source.AbsoluteUri); }
                return "????";
            }
        }
        public bool CanGoBack
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.CanGoBack;
                else if (BrowserType == 1)
                    return Edge.CanGoBack;
                else if (BrowserType == 2)
                    return IE.BrowserCore.CanGoBack;
                return true;
            }
        }
        public bool CanGoForward
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.CanGoForward;
                else if (BrowserType == 1)
                    return Edge.CanGoForward;
                else if (BrowserType == 2)
                    return IE.BrowserCore.CanGoForward;
                return true;
            }
        }
        public bool IsLoading
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.IsLoading;
                else if (BrowserType == 1)
                    return false;
                else if (BrowserType == 2)
                    return IE.IsLoading;
                return false;
            }
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            if (BrowserType == 0)
                Chromium.Back();
            else if (BrowserType == 1)
                Edge.GoBack();
            else if (BrowserType == 2)
                IE.BrowserCore.GoBack();
        }
        public void Forward()
        {
            if (!CanGoForward)
                return;
            if (BrowserType == 0)
                Chromium.Forward();
            else if (BrowserType == 1)
                Edge.GoForward();
            else if (BrowserType == 2)
                IE.BrowserCore.GoForward();
        }
        public void Refresh()
        {
            if (!IsLoading)
                Reload();
            else
                Stop();
        }
        public void Reload()
        {
            try
            {
                if (BrowserType == 0)
                    Chromium.Reload();
                else if (BrowserType == 1)
                    Edge.Reload();
                else if (BrowserType == 2)
                    IE.BrowserCore.Navigate(IE.BrowserCore.Source.AbsoluteUri);
            }
            catch { }
            //IE.BrowserCore.Refresh();
        }
        public void Stop()
        {
            try
            {
                if (BrowserType == 0)
                Chromium.Stop();
            else if (BrowserType == 1)
                Edge.Stop();
            else if (BrowserType == 2)
                IE.BrowserCore.InvokeScript("eval", "document.execCommand('Stop');");
            }
            catch { }
        }
        public void Find(string Text)
        {
            if (BrowserType == 0)
                Chromium.Find(Text, true, false, true);
            //else// if (BrowserType == 2)
            //    IE.BrowserCore.;
        }
        public void StopFinding(bool ClearSelection = true)
        {
            if (BrowserType == 0)
                Chromium.StopFinding(ClearSelection);
            //else// if (BrowserType == 2)
            //    IE.BrowserCore.;
        }
        public void Navigate(string Url)
        {
            if (BrowserType == 0)
                Chromium.Address = Url;
            else if (BrowserType == 0)
                Edge.Source = new Uri(Url);
            else if (BrowserType == 2)
                IE.Navigate(Url);
        }
        public void OpenFileExplorer(string Url)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = "/select, \"" + Url + "\"",
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }
        public void Inspect()
        {
            if (BrowserType == 0)
            {
                IsUtilityContainerOpen = InspectorContainer.Visibility == Visibility.Visible;
                if (IsUtilityContainerOpen)
                    ChromiumInspector.Address = "about:blank";
                else
                    ChromiumInspector.Address = "localhost:8089/json/list";
                InspectorContainer.Visibility = IsUtilityContainerOpen ? Visibility.Collapsed : Visibility.Visible;
                IsUtilityContainerOpen = !IsUtilityContainerOpen;

                if (ActiveSizeEmulation)
                    SizeEmulator();
            }
            else if (BrowserType == 1)
            {
                IsUtilityContainerOpen = InspectorContainer.Visibility == Visibility.Visible;
                if (IsUtilityContainerOpen)
                    EdgeInspector.CoreWebView2.Navigate("about:blank");
                else
                    EdgeInspector.CoreWebView2.Navigate("http://localhost:9222/json/list");
                InspectorContainer.Visibility = IsUtilityContainerOpen ? Visibility.Collapsed : Visibility.Visible;
                IsUtilityContainerOpen = !IsUtilityContainerOpen;

                if (ActiveSizeEmulation)
                    SizeEmulator();
            }
            else if (BrowserType == 2)
            {
                IsUtilityContainerOpen = !IsUtilityContainerOpen;
                IE.BrowserCore.Navigate(IE.BrowserCore.Source.AbsoluteUri);
            }
            //Inspector.GetDevToolsClient().DeviceOrientation.ClearDeviceOrientationOverrideAsync();
            //--load-media-router-component-extension, 0
        }
        public void Favourite()
        {
            /*string Url;
            string Title;
            bool IsLoaded;
            Url = Address;
            Title = this.Title;
            IsLoaded = !IsLoading;*/
            int FavouriteExistIndex = FavouriteExists(Address);
            if (FavouriteExistIndex != -1)
            {
                MainWindow.Instance.Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
            else if (!IsLoading)
            {
                MainWindow.Instance.Favourites.Add(new ActionStorage(this.Title, $"3<,>{Address}", Address));
                FavouriteButton.Content = "\xEB52";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            if (MainWindow.Instance.Favourites.Count == 0)
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 0);
                FavouriteContainer.Height = 1;
            }
            else
            {
                ToolBarPanel.Margin = new Thickness(5, 5, 5, 5);
                FavouriteContainer.Height = 33;
            }
        }
        int FavouriteExists(string Url)
        {
            int ToReturn = -1;
            string[] FavouriteUrls = MainWindow.Instance.Favourites.Select(item => item.Tooltip).ToArray();
            for (int i = 0; i < FavouriteUrls.Length; i++)
            {
                if (FavouriteUrls[i] == Url)
                    ToReturn = i;
            }
            return ToReturn;
        }
        public void SetAudio(bool Muted)
        {
            try
            {
                if (BrowserType == 0)
                {
                    Chromium.BrowserCore.GetHost().SetAudioMuted(Muted);
                    MuteAudioButton.Content = Muted ? "\xE74F" : "\xE767";
                    Tab.MuteCommandHeader = Muted ? "Unmute" : "Mute";
                }
                else if (BrowserType == 0)
                {
                    Edge.CoreWebView2.IsMuted = Muted;
                    MuteAudioButton.Content = Muted ? "\xE74F" : "\xE767";
                    Tab.MuteCommandHeader = Muted ? "Unmute" : "Mute";
                }
            }
            catch { }
            IsAudioMuted = Muted;
        }
        //int InZoom = 0;
        //int OutZoom = 0;
        public void Zoom(int Delta)
        {
            if (BrowserType == 0)
            {
                /*if (Delta == 0)
                {
                    Chromium.ZoomLevel += OutZoom * Chromium.ZoomLevelIncrement;
                    OutZoom = 0;
                    Chromium.ZoomLevel -= InZoom * Chromium.ZoomLevelIncrement;
                    InZoom = 0;
                }
                else if (Delta > 0)
                {
                    Chromium.ZoomLevel += Chromium.ZoomLevelIncrement;
                    InZoom++;
                    if (OutZoom != 0)
                        OutZoom--;
                }
                else if (Delta < 0)
                {
                    Chromium.ZoomLevel -= Chromium.ZoomLevelIncrement;
                    OutZoom++;
                    if (InZoom != 0)
                        InZoom--;
                }*/
                if (Delta == 0)
                    Chromium.ZoomResetCommand.Execute(null);
                else if (Delta > 0)
                    Chromium.ZoomInCommand.Execute(null);
                else if (Delta < 0)
                    Chromium.ZoomOutCommand.Execute(null);
            }
        }
        public async void Screenshot()
        {
            try
            {
                if (BrowserType == 0)
                {
                    string ScreenshotPath = MainSave.Get("ScreenshotPath");
                    if (!Directory.Exists(ScreenshotPath))
                        Directory.CreateDirectory(ScreenshotPath);
                    string _ScreenshotFormat = MainWindow.Instance.MainSave.Get("ScreenshotFormat");
                    string FileExtension = "jpg";
                    CaptureScreenshotFormat ScreenshotFormat = CaptureScreenshotFormat.Jpeg;
                    if (_ScreenshotFormat == "Png")
                    {
                        FileExtension = "png";
                        ScreenshotFormat = CaptureScreenshotFormat.Png;
                    }
                    else if (_ScreenshotFormat == "WebP")
                    {
                        FileExtension = "webp";
                        ScreenshotFormat = CaptureScreenshotFormat.Webp;
                    }
                    DateTime CurrentTime = DateTime.Now;
                    string Url = $"{Path.Combine(ScreenshotPath, Regex.Replace($"{Chromium.Title} {CurrentTime.Day}-{CurrentTime.Month}-{CurrentTime.Year} {string.Format("{0:hh:mm tt}", DateTime.Now)}.{FileExtension}", "[^a-zA-Z0-9._ -]", ""))}";
                    using (var _DevToolsClient = Chromium.GetDevToolsClient())
                    {
                        var result = await _DevToolsClient.Page.CaptureScreenshotAsync(ScreenshotFormat, null, null, null, false);
                        File.WriteAllBytes(Url, result.Data);
                        //File.SetAttributes(Url, FileAttributes.Normal);
                        //Navigate(true, "file:///////" + Url);
                    }
                    Process.Start(new ProcessStartInfo(Url)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }
        public void QRCode(string Url)
        {
            if (!QRCodePopup.IsOpen)
                QRCodeImage.Source = MainWindow.Instance._QRCodeHandler.GenerateQRCode(Url).ToImageSource();
            QRCodePopup.IsOpen = !QRCodePopup.IsOpen;
        }
        public bool IsAudioMuted;

        private void FavouriteScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            FavouriteScrollViewer.ScrollToHorizontalOffset(FavouriteScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
        }

        public void ApplyTheme(Theme _Theme)
        {
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
        }

        bool AddressBoxFocused;
        bool AddressBoxMouseEnter;
        public bool CanChangeAddressBox()
        {
            string Text = AddressBox.Text.Trim();
            return !AddressBoxFocused || !Text.Contains(" ");
        }
        private void AddressBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (AddressBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                {
                    Keyboard.ClearFocus();
                    string Url = Utils.FilterUrlForBrowser(AddressBox.Text, MainSave.Get("Search_Engine"));
                    if (!Utils.IsProgramUrl(Url))
                        Address = Url;
                    AddressBoxPlaceholder.Text = "";
                }
                else if ((e.Key == Key.Back || e.Key == Key.Delete) || (e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.D0) && (e.Key <= Key.D9) || (e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9))
                {
                    SuggestionsTimer.Stop();
                    SuggestionsTimer.Start();
                    /*string CurrentText = AddressBox.Text;
                    if (!(e.Key == Key.Back || e.Key == Key.Delete))
                    {
                        if ((e.Key >= Key.A) && (e.Key <= Key.Z))
                            CurrentText += (char)((int)'a' + (int)(e.Key - Key.A));
                        else if ((e.Key >= Key.D0) && (e.Key <= Key.D9))
                            CurrentText += (char)((int)'0' + (int)(e.Key - Key.D0));
                        else if ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9))
                            CurrentText += (char)((int)'0' + (int)(e.Key - Key.NumPad0));
                    }
                    string TextToScan = CurrentText.ToLower();
                    var responseText = MainWindow.Instance.TinyDownloader.DownloadString("https://suggestqueries.google.com/complete/search?client=chrome&gl=US&q=" + TextToScan);
                    var items = (from each in responseText.Split(',') select each.Trim('[', ']', '\"', ':', '{', '}')).ToArray<string>();
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items.Length > 1)
                        {
                            string Suggestion = items[1].Trim().Replace("\"", "").Replace("[", "").Replace("]", "");
                            AddressBoxPlaceholder.Text = Suggestion.Contains(TextToScan) ? Suggestion : "";
                        }
                        else
                            AddressBoxPlaceholder.Text = "";
                    }*/
                }
                AddressBoxPlaceholder.Text = "";
            }
            else
            {
                AddressBoxPlaceholder.Text = "";
            }
        }
        private void SuggestionsTimer_Tick(object? sender, EventArgs e)
        {
            SuggestionsTimer.Stop();
            try
            {
                if (!bool.Parse(MainWindow.Instance.MainSave.Get("SearchSuggestions")) || Utils.IsUrl(AddressBox.Text))
                {
                    AddressBoxPlaceholder.Text = "";
                    return;
                }
                string CurrentText = AddressBox.Text;
                /*if (!(e.Key == Key.Back || e.Key == Key.Delete))
                {
                    if ((e.Key >= Key.A) && (e.Key <= Key.Z))
                        CurrentText += (char)((int)'a' + (int)(e.Key - Key.A));
                    else if ((e.Key >= Key.D0) && (e.Key <= Key.D9))
                        CurrentText += (char)((int)'0' + (int)(e.Key - Key.D0));
                    else if ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9))
                        CurrentText += (char)((int)'0' + (int)(e.Key - Key.NumPad0));
                }*/
                string TextToScan = CurrentText.ToLower();
                var responseText = MainWindow.Instance.TinyDownloader.DownloadString("https://suggestqueries.google.com/complete/search?client=chrome&gl=US&q=" + TextToScan);
                var items = (from each in responseText.Split(',') select each.Trim('[', ']', '\"', ':', '{', '}')).ToArray<string>();
                for (int i = 0; i < items.Length; i++)
                {
                    if (items.Length > 1)
                    {
                        string Suggestion = items[1].Trim().Replace("\"", "").Replace("[", "").Replace("]", "");
                        AddressBoxPlaceholder.Text = Suggestion != TextToScan && Suggestion.Contains(TextToScan) ? Suggestion : "";
                    }
                    else
                        AddressBoxPlaceholder.Text = "";
                }
            }
            catch { }
        }
        DispatcherTimer SuggestionsTimer;
        private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddressBox.Text == Utils.ConvertUrlToReadableUrl(_IdnMapping, (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString()))))
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
                    AddressBoxPlaceholder.Text = "";
                    if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                        AddressBox.Text = Utils.ConvertUrlToReadableUrl(MainWindow.Instance._IdnMapping, bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString()));
                }
                catch { }
            }
            //SetSuggestions();
            AddressBoxFocused = false;
        }
        private void AddressBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                try
                {
                    if (AddressBox.Text == Utils.ConvertUrlToReadableUrl(MainWindow.Instance._IdnMapping, (bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString()))))
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
                    AddressBoxPlaceholder.Text = "";
                    if (Utils.CleanUrl(AddressBox.Text) == Utils.CleanUrl(AddressBox.Tag.ToString()))
                        AddressBox.Text = Utils.ConvertUrlToReadableUrl(MainWindow.Instance._IdnMapping, bool.Parse(MainSave.Get("FullAddress")) ? AddressBox.Tag.ToString() : Utils.CleanUrl(AddressBox.Tag.ToString()));
                }
                catch { }
            }
            //SetSuggestions();
            AddressBoxMouseEnter = false;
        }
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (FindTextBox.Text.Trim().Length > 0)
                    Find(FindTextBox.Text);
                else
                    StopFinding();
            }
        }

        public void DisposeCore()
        {
            CoreContainer.Children.Clear();
            InspectorCoreContainer.Children.Clear();
            if (BrowserType == 0)
            {
                Chromium.Dispose();
                ChromiumInspector.Dispose();
            }
            else if (BrowserType == 1)
            {
                Edge.Dispose();
                EdgeInspector.Dispose();
            }
            else if (BrowserType == 2)
            {
                IE.BrowserCore.Dispose();
                //IEInspector.BrowserCore.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        Size MaximizedSize = Size.Empty;
        Size PreviousSize = Size.Empty;
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

            //if (PreviousSize == NewSize)
            //{
                SizeEmulatorColumn1.Width = new GridLength(0);
                SizeEmulatorColumn2.Width = new GridLength(0);
                SizeEmulatorRow1.Height = new GridLength(0);
                SizeEmulatorRow2.Height = new GridLength(0);
            //}

            PreviousSize = NewSize;
            //ToastBox.Show("", NewSize.ToString() + $" {Percentage}", 10);

            //if (SizeEmulatorColumn1.Width.Value > SizeEmulatorColumn1.MaxWidth)
            //    SizeEmulatorColumn1.Width = new GridLength(SizeEmulatorColumn1.MaxWidth);
            //if (SizeEmulatorColumn2.Width.Value > SizeEmulatorColumn2.MaxWidth)
            //    SizeEmulatorColumn2.Width = new GridLength(SizeEmulatorColumn2.MaxWidth);
            //if (SizeEmulatorRow1.Height.Value > SizeEmulatorRow1.MaxHeight)
            //    SizeEmulatorRow1.Height = new GridLength(SizeEmulatorRow1.MaxHeight);
            //if (SizeEmulatorRow2.Height.Value > SizeEmulatorRow2.MaxHeight)
            //    SizeEmulatorRow2.Height = new GridLength(SizeEmulatorRow2.MaxHeight);
            //MessageBox.Show(NewSize.ToString() + $" {Percentage}");
        }

        /*private void CoreContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return;
            try
            {
                Zoom(e.Delta);
                e.Handled = true;
            }
            catch (Exception) { }
        }*/
    }
}
