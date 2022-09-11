using CefSharp;
using CefSharp.DevTools;
using CefSharp.Wpf.HwndHost;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SLBr.Controls;
using SLBr.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public partial class Browser : UserControl
    {
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

        Saving MainSave;
        IdnMapping _IdnMapping;
        BrowserTabItem Tab;

        BrowserSettings _BrowserSettings;

        bool IsUtilityContainerOpen;

        public Browser(string Url, int _BrowserType, BrowserTabItem _Tab = null, BrowserSettings CefBrowserSettings = null)
        {
            InitializeComponent();
            MainSave = MainWindow.Instance.MainSave;
            _IdnMapping = MainWindow.Instance._IdnMapping;
            BrowserType = _BrowserType;
            FavouritesPanel.ItemsSource = MainWindow.Instance.Favourites;
            FavouriteListMenu.ItemsSource = MainWindow.Instance.Favourites;
            HistoryListMenu.ItemsSource = MainWindow.Instance.History;
            ApplyTheme(MainWindow.Instance.GetTheme());
            if (MainWindow.Instance.Favourites.Count == 0)
                FavouriteContainer.Visibility = Visibility.Collapsed;
            else
                FavouriteContainer.Visibility = Visibility.Visible;
            if (BrowserType == 0)
                CreateChromium(Url, CefBrowserSettings);
            else// if (BrowserType == 2)
                CreateIE(Url);
            if (_Tab != null)
                Tab = _Tab;
            else
                Tab = MainWindow.Instance.GetTab(this);
            SuggestionsTimer = new DispatcherTimer();
            SuggestionsTimer.Tick += SuggestionsTimer_Tick;
            SuggestionsTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
        }

        void CreateChromium(string Url, BrowserSettings CefBrowserSettings = null)
        {
            Chromium = new ChromiumWebBrowser(Url);
            Chromium.Address = Url;
            Chromium.JavascriptObjectRepository.Register("internal", MainWindow.Instance._PrivateJsObjectHandler, BindingOptions.DefaultBinder);
            Chromium.JavascriptObjectRepository.Register("slbr", MainWindow.Instance._PublicJsObjectHandler, BindingOptions.DefaultBinder);
            Chromium.IsManipulationEnabled = true;
            Chromium.LifeSpanHandler = MainWindow.Instance._LifeSpanHandler;
            Chromium.DownloadHandler = MainWindow.Instance._DownloadHandler;
            Chromium.RequestHandler = MainWindow.Instance._RequestHandler;
            Chromium.MenuHandler = MainWindow.Instance._ContextMenuHandler;
            Chromium.KeyboardHandler = MainWindow.Instance._KeyboardHandler;
            Chromium.JsDialogHandler = MainWindow.Instance._JsDialogHandler;
            Chromium.DisplayHandler = new DisplayHandler(this);
            Chromium.JavascriptMessageReceived += Chromium_JavascriptMessageReceived;
            Chromium.AllowDrop = true;
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
                    BackgroundColor = Utils.ColorToUInt(System.Drawing.Color.Black)
                };
            }
            Chromium.BrowserSettings = _BrowserSettings;
            Chromium.TitleChanged += Chromium_TitleChanged;
            Chromium.LoadingStateChanged += Chromium_LoadingStateChanged;
            Chromium.ZoomLevelIncrement = 0.5f;
            Chromium.FrameLoadEnd += Chromium_FrameLoadEnd;
            Chromium.StatusMessage += Chromium_StatusMessage;
            CoreContainer.Children.Add(Chromium);

            ChromiumInspector = new ChromiumWebBrowser("about:blank");
            ChromiumInspector.Address = "about:blank";
            ChromiumInspector.BrowserSettings = new BrowserSettings
            {
                WebGl = CefState.Disabled,
                WindowlessFrameRate = 20,
                BackgroundColor = Utils.ColorToUInt(System.Drawing.Color.Black)
            };
            ChromiumInspector.AllowDrop = true;
            ChromiumInspector.FrameLoadEnd += (sender, args) =>
            {
                if (args.Frame.IsValid && args.Frame.IsMain)
                {
                    if (args.Url == "http://localhost:8089/json/list")
                    {
                        ChromiumInspector.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('body')[0].innerHTML").ContinueWith(t =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                if (t.Result != null && t.Result.Result != null)
                                {
                                    var _Document = new HtmlDocument();
                                    _Document.LoadHtml(t.Result.Result.ToString());
                                    HtmlNode _Node = _Document.DocumentNode.SelectSingleNode("//pre");
                                    if (_Node != null)
                                    {
                                        List<InspectorObject> InspectorObjects = JsonConvert.DeserializeObject<List<InspectorObject>>(_Node.InnerHtml);
                                        foreach (InspectorObject _InspectorObject in InspectorObjects)
                                        {
                                            if (_InspectorObject.type == "page" && _InspectorObject.url == Chromium.Address)
                                            {
                                                ChromiumInspector.Address = "http://localhost:8089" + _InspectorObject.devtoolsFrontendUrl;
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
            InspectorContainer.Children.Add(ChromiumInspector);

            RenderOptions.SetBitmapScalingMode(Chromium, BitmapScalingMode.LowQuality);
            RenderOptions.SetBitmapScalingMode(ChromiumInspector, BitmapScalingMode.LowQuality);
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

        void CreateIE(string Url)
        {
            IE = new IEWebBrowser(Url);
            IE.BrowserCore.Loaded += IE_Loaded;
            IE.BrowserCore.Navigating += IE_Navigating;
            IE.BrowserCore.Navigated += IE_Navigated;
            CoreContainer.Children.Add(IE.BrowserCore);

            IEInspector = new IEWebBrowser("about:blank");
            InspectorContainer.Children.Add(IEInspector.BrowserCore);
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
            //Chromium.ExecuteScriptAsync("window.navigator.vendor = \"SLT World\"");
            //Chromium.ExecuteScriptAsync("window.navigator.deviceMemory = 0.25");
            //if (e.Frame.IsValid && e.Frame.IsMain)
        }

        public void Unload(BrowserSettings _BrowserSettings = null)
        {
            if (BrowserType == 0)
            {
                string Url = Chromium.Address;
                CoreContainer.Children.Clear();
                InspectorContainer.Children.Clear();
                Chromium.Dispose();
                ChromiumInspector.Dispose();
                CreateChromium(Url, _BrowserSettings);
            }
            else
            {
                string Url = IE.BrowserCore.Source.AbsoluteUri;
                CoreContainer.Children.Clear();
                InspectorContainer.Children.Clear();
                IE.Dispose();
                IEInspector.Dispose();
                CreateIE(Url);
            }
            GC.Collect();
        }
        public void Unload(int _Framerate, CefState JSState, CefState LIState, CefState LSState, CefState DBState, CefState WebGLState)
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
                    BackgroundColor = Utils.ColorToUInt(System.Drawing.Color.Black)
                };
                Unload(_BrowserSettings);
            }
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

        private void IE_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (IsUtilityContainerOpen)
            {
                try
                {
                    dynamic document = IE.BrowserCore.Document;
                    dynamic script = document.createElement("script");
                    script.type = @"text/javascript";
                    script.src = @"https://lupatec.eu/getfirebug/firebug-lite-compressed.js#startOpened=false,disableWhenFirebugActive=false";
                    document.head.appendChild(script); // Dynamic property head does not exist.
                }
                catch { };
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
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
                        AddressBox.Text = OutputUrl;
                    AddressBox.Tag = e.Uri.AbsoluteUri;
                }
            }));
        }
        private void IE_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
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
            Tab.Icon = new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Host));
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
                FavouriteButton.Content = "\xEB52";
            else
                FavouriteButton.Content = "\xEB51";
            if (MainWindow.Instance.Favourites.Count == 0)
                FavouriteContainer.Visibility = Visibility.Collapsed;
            else
                FavouriteContainer.Visibility = Visibility.Visible;
        }

        private void Chromium_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Tab.Header = Title;
        }
        private void Chromium_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!Chromium.IsBrowserInitialized)
                    return;
                BrowserLoadChanged(Chromium.Address);

                DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
                _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(MainWindow.Instance.GetTheme().DarkWebPage ? bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebPage")) : false);
                ReloadButton.Content = e.IsLoading ? "\xE711" : "\xE72C";
                //WebsiteLoadingProgressBar.IsEnabled = e.IsLoading;
                //WebsiteLoadingProgressBar.IsIndeterminate = e.IsLoading;
                if (!Chromium.IsLoading)
                {
                    if (IsUtilityContainerOpen && !ChromiumInspector.Address.StartsWith("http://localhost:8089/devtools/"))
                        ChromiumInspector.Address = "localhost:8089/json/list";
                    if (Chromium.Address == "https://github.com/SLT-World/SLBr" || Chromium.Address == "https://github.com/SLT-World/SLBr/")
                        ToastBox.Show("", "Please support SLBr by giving a star to the project.", 10);
                }
                else
                {
                    if (Chromium.Address.StartsWith("slbr:"))
                        Chromium.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"internal\");");
                    Chromium.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"slbr\");");
                    Chromium.ExecuteScriptAsync(@"function addStyle(styleString) {
                                                        const style = document.createElement('style');
                                                        style.textContent = styleString;
                                                        document.head.append(style);
                                                    }

                                                    addStyle(`
                                                        ::-webkit-scrollbar{width: 17.5px;}
                                                    `);

                                                    addStyle(`
                                                        ::-webkit-scrollbar-thumb{background:gainsboro;}
                                                    `);

                                                    addStyle(`
                                                        ::-webkit-scrollbar-thumb:hover{background:lightgray;}
                                                    `);

                                                    addStyle(`
                                                        ::-webkit-scrollbar-track{background:whitesmoke;}
                                                    `);");
                }
                BackButton.IsEnabled = e.CanGoBack;
                ForwardButton.IsEnabled = e.CanGoForward;
            }));
        }

        public int BrowserType;
        ChromiumWebBrowser Chromium;
        IEWebBrowser IE;
        ChromiumWebBrowser ChromiumInspector;
        IEWebBrowser IEInspector;

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
        public enum Actions
        {
            Undo = 0,
            Redo = 1,
            Refresh = 2,
            Navigate = 3,
            CreateTab = 4,
            CloseTab = 5,
            Inspect = 6,
            Favourite = 7,
            SetAudio = 8,
            Settings = 9,
            UnloadTabs = 10,
            SwitchBrowser = 11,
            OpenFileExplorer = 12,
            QRCode = 13,
        }
        private void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            V1 = V1.Replace("{CurrentUrl}", Address);
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
                    MainWindow.Instance.CloseCurrentBrowserTab();
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
                    MainWindow.Instance.Settings(true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                    break;
                case Actions.UnloadTabs:
                    MainWindow.Instance.UnloadTabs();
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
            }
        }

        public void SwitchBrowser(string NewBrowserName)
        {
            int NewBrowserType = 0;
            if (NewBrowserName == "Chromium")
                NewBrowserType = 0;
            else if (NewBrowserName == "IE")
                NewBrowserType = 2;
            BrowserType = NewBrowserType;
            if (NewBrowserType == 0)
            {
                string Url = IE.BrowserCore.Source.AbsoluteUri;
                CoreContainer.Children.Clear();
                InspectorContainer.Children.Clear();
                IE.Dispose();
                IEInspector.Dispose();
                CreateChromium(Url);
                SwitchBrowserButton.Tag = "11<,>IE";
                SwitchBrowserButton.ToolTip = "Internet Explorer mode";
                SwitchBrowserText.Text = "e";
            }
            else
            {
                string Url = Chromium.Address;
                CoreContainer.Children.Clear();
                InspectorContainer.Children.Clear();
                Chromium.Dispose();
                ChromiumInspector.Dispose();
                CreateIE(Url);
                SwitchBrowserButton.Tag = "11<,>Chromium";
                SwitchBrowserButton.ToolTip = "Chromium mode";
                SwitchBrowserText.Text = "c";
            }
        }

        public string Address
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.Address;
                else// if (BrowserType == 2)
                    return IE.BrowserCore.Source.AbsoluteUri;
            }
            set
            {
                if (BrowserType == 0)
                    Chromium.Address = value;
                else// if (BrowserType == 2)
                    IE.Navigate(value);
            }
        }
        public string Title
        {
            get {
                if (BrowserType == 0)
                    return Chromium.Title.Trim().Length > 0 ? Chromium.Title : Utils.CleanUrl(Chromium.Address);
                else// if (BrowserType == 2)
                    try
                    {
                        return (string)IE.BrowserCore.InvokeScript("eval", "document.title.toString()");
                    }
                    catch { return Utils.CleanUrl(IE.BrowserCore.Source.AbsoluteUri); }
            }
        }
        public bool CanGoBack
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.CanGoBack;
                else// if (BrowserType == 2)
                    return IE.BrowserCore.CanGoBack;
            }
        }
        public bool CanGoForward
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.CanGoForward;
                else// if (BrowserType == 2)
                    return IE.BrowserCore.CanGoForward;
            }
        }
        public bool IsLoading
        {
            get
            {
                if (BrowserType == 0)
                    return Chromium.IsLoading;
                else// if (BrowserType == 2)
                    return IE.IsLoading;
            }
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            if (BrowserType == 0)
                Chromium.Back();
            else// if (BrowserType == 2)
                IE.BrowserCore.GoBack();
        }
        public void Forward()
        {
            if (!CanGoForward)
                return;
            if (BrowserType == 0)
                Chromium.Forward();
            else// if (BrowserType == 2)
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
            if (BrowserType == 0)
                Chromium.Reload();
            else// if (BrowserType == 2)
                IE.BrowserCore.Navigate(IE.BrowserCore.Source.AbsoluteUri);
            //IE.BrowserCore.Refresh();
        }
        public void Stop()
        {
            if (BrowserType == 0)
                Chromium.Stop();
            else// if (BrowserType == 2)
                IE.BrowserCore.InvokeScript("eval", "document.execCommand('Stop');");
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
            else// if (BrowserType == 2)
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
            }
            else
            {
                IsUtilityContainerOpen = !IsUtilityContainerOpen;
                IE.BrowserCore.Navigate(IE.BrowserCore.Source.AbsoluteUri);
            }
            //Inspector.GetDevToolsClient().DeviceOrientation.ClearDeviceOrientationOverrideAsync();
            //--load-media-router-component-extension, 0
        }
        public void Favourite()
        {
            string Url;
            string Title;
            bool IsLoaded;
            Url = Address;
            Title = this.Title;
            IsLoaded = !IsLoading;
            int FavouriteExistIndex = FavouriteExists(Url);
            if (FavouriteExistIndex != -1)
            {
                MainWindow.Instance.Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
            }
            else if (IsLoaded)
            {
                MainWindow.Instance.Favourites.Add(new ActionStorage(Title, $"3<,>{Url}", Url));
                FavouriteButton.Content = "\xEB52";
            }
            if (MainWindow.Instance.Favourites.Count == 0)
                FavouriteContainer.Visibility = Visibility.Collapsed;
            else
                FavouriteContainer.Visibility = Visibility.Visible;
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
            if (BrowserType == 0)
            {
                Chromium.BrowserCore.GetHost().SetAudioMuted(Muted);
                MuteAudioButton.Content = Muted ? "\xE74F" : "\xE767";
            }
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
            if (BrowserType == 0)
            {
                string ScreenshotPath = MainSave.Get("ScreenshotPath");
                if (!Directory.Exists(ScreenshotPath))
                    Directory.CreateDirectory(ScreenshotPath);
                using (var _DevToolsClient = Chromium.GetDevToolsClient())
                {
                    DateTime CurrentTime = DateTime.Now;
                    string Url = $"{Path.Combine(ScreenshotPath, Regex.Replace($"{Chromium.Title} {CurrentTime.Day}-{CurrentTime.Month}-{CurrentTime.Year} {string.Format("{0:hh:mm tt}", DateTime.Now)}.jpg", "[^a-zA-Z0-9._ -]", ""))}";
                    var result = await _DevToolsClient.Page.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, null, null, null, false);
                    File.WriteAllBytes(Url, result.Data);
                    //Navigate(true, "file:///////" + Url);
                }
            }
        }
        public void QRCode(string Url)
        {
            if (!QRCodePopup.IsOpen)
                QRCodeImage.Source = Utils.BitmapToImageSource(MainWindow.Instance._QRCodeHandler.GenerateQRCode(Url));
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
            Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);
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
            InspectorContainer.Children.Clear();
            if (BrowserType == 0)
            {
                Chromium.Dispose();
                ChromiumInspector.Dispose();
            }
            else// if (BrowserType == 2)
            {
                IE.BrowserCore.Dispose();
                IEInspector.BrowserCore.Dispose();
            }
            GC.SuppressFinalize(this);
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
