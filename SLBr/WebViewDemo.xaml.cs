using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for WebViewDemo.xaml
    /// </summary>
    public partial class WebViewDemo : Window
    {
        public IWebView CurrentWebView;

        public WebViewDemo()
        {
            InitializeComponent();
            HotKeyManager.HotKeys.Add(new HotKey(Reload, (int)Key.F5, false, false, false));
            WebViewManager.DownloadManager.DownloadStarted += CurrentWebView_DownloadStarted;
            WebViewManager.DownloadManager.DownloadUpdated += CurrentWebView_DownloadUpdated;
            WebViewManager.DownloadManager.DownloadCompleted += CurrentWebView_DownloadCompleted;
        }

        void Reload()
        {
            CurrentWebView?.Refresh();
        }

        private void IE_Click(object sender, RoutedEventArgs e)
        {
            SwapWebView(WebEngineType.Trident);
        }

        private void Edge_Click(object sender, RoutedEventArgs e)
        {
            SwapWebView(WebEngineType.ChromiumEdge);
        }

        private void Cef_Click(object sender, RoutedEventArgs e)
        {
            SwapWebView(WebEngineType.Chromium);
        }

        private void SwapWebView(WebEngineType EngineType)
        {
            //MessageBox.Show(((WebView2)CurrentWebView.Control).ZoomFactor.ToString());
            CurrentWebView?.Dispose();
            WebViewBrowserSettings Settings = new WebViewBrowserSettings()
            {
                JavaScriptMessage = false
            };
            CurrentWebView = EngineType switch
            {
                WebEngineType.Chromium => new ChromiumWebView("https://permission.site/", Settings),
                WebEngineType.ChromiumEdge => new ChromiumEdgeWebView("https://permission.site/", Settings),
                WebEngineType.Trident => new TridentWebView("https://permission.site/", Settings)
                //https://commons.wikimedia.org/wiki/Example_images
                //https://www.w3schools.com/js/js_popup.asp
                //https://www.thinkbroadband.com/download
            };
            AttachContextMenu(CurrentWebView, CurrentWebView.Control);

            CurrentWebView.IsBrowserInitializedChanged += CurrentWebView_IsBrowserInitializedChanged;
            CurrentWebView.TitleChanged += CurrentWebView_TitleChanged;
            CurrentWebView.LoadingStateChanged += CurrentWebView_LoadingStateChanged;
            CurrentWebView.FaviconChanged += CurrentWebView_FaviconChanged;
            CurrentWebView.NewWindowRequested += CurrentWebView_NewWindowRequested;
            CurrentWebView.PermissionRequested += CurrentWebView_PermissionRequested;
            CurrentWebView.StatusMessage += CurrentWebView_StatusMessage;
            CurrentWebView.FullscreenChanged += CurrentWebView_FullscreenChanged;
            CurrentWebView.FindResult += CurrentWebView_FindResult;
            CurrentWebView.JavaScriptMessageReceived += CurrentWebView_JavaScriptMessageReceived;
            /*CurrentWebView.BeforeNavigation += (s, e) =>
            {
                MessageBoxResult Result = MessageBox.Show("Go to " + e.Url, "", MessageBoxButton.YesNo);
                if (Result == MessageBoxResult.No)
                    e.Cancel = true;
            };*/
            CurrentWebView.AuthenticationRequested += CurrentWebView_AuthenticationRequested;
            CurrentWebView.ScriptDialogOpened += (s, e) =>
            {
                if (e.DialogType == ScriptDialogType.Alert)
                {
                    MessageBox.Show($"Custom handler: {e.Text}", "Alert from " + e.Url);
                    e.Handled = true;
                    e.Result = true;
                }
                else if (e.DialogType == ScriptDialogType.Confirm)
                {
                    var Result = MessageBox.Show($"Custom handler: {e.Text}", "Confirm from " + e.Url, MessageBoxButton.YesNo);
                    e.Handled = true;
                    e.Result = Result == MessageBoxResult.Yes;
                }
                else if (e.DialogType == ScriptDialogType.Prompt)
                {
                    var Result = MessageBox.Show($"Custom handler: {e.Text}", "Prompt from " + e.Url, MessageBoxButton.YesNo);
                    e.Handled = true;
                    e.Result = Result == MessageBoxResult.Yes;
                    e.PromptResult = "Char Aznable";
                }
                else if (e.DialogType == ScriptDialogType.BeforeUnload)
                {
                    var Result = MessageBox.Show("Before Unload", "Reload site?" + e.Url, MessageBoxButton.YesNo);
                    e.Handled = true;
                    e.Result = Result == MessageBoxResult.Yes;
                }
            };
            CurrentWebView.ExternalProtocolRequested += CurrentWebView_ExternalProtocolRequested;
            CurrentWebView.NavigationError += CurrentWebView_NavigationError;

            CurrentWebView.ResourceLoaded += CurrentWebView_ResourceLoaded;
            WebViewHost.Content = CurrentWebView.Control;
        }

        private void CurrentWebView_ResourceLoaded(object? sender, ResourceLoadedResult e)
        {
            //MessageBox.Show($"[{e.Url}] {e.ReceivedContentLength} {e.ResourceRequestType}");
        }

        private void CurrentWebView_NavigationError(object? sender, NavigationErrorEventArgs e)
        {
            //MessageBox.Show($"Code: {e.ErrorCode}, Text: {e.ErrorText}");
        }

        private void CurrentWebView_ExternalProtocolRequested(object? sender, ExternalProtocolEventArgs e)
        {
            //ms-settings: Settings
            //ms-photos: Photos
            //ms-settings-screenrotation:
            //
            string text = "Launching External URI Scheme";
            text += " to ";
            text += e.Url;
            text += "\n";
            text += "Do you want to grant permission?";
            string caption = "Launching External URI Scheme request";
            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage icnMessageBox = MessageBoxImage.None;
            MessageBoxResult Result = MessageBox.Show(text, caption, btnMessageBox, icnMessageBox);
            if (Result == MessageBoxResult.Yes)
                e.Launch = true;
        }

        private void CurrentWebView_AuthenticationRequested(object? sender, WebAuthenticationRequestedEventArgs e)
        {
            //https://jigsaw.w3.org/HTTP/Basic/
            MessageBoxResult Result = MessageBox.Show("Authentication " + e.Url, "", MessageBoxButton.YesNo);
            if (Result == MessageBoxResult.No)
                e.Cancel = true;
            else
            {
                e.Username = "guest";
                e.Password = "guest";
            }
        }

        /*private void CurrentWebView_ResourceRequested(object? sender, ResourceRequestEventArgs e)
        {
            MessageBox.Show(e.Method);
            MessageBox.Show(e.Url);
            MessageBox.Show(e.FocusedUrl);
        }*/

        private void CurrentWebView_JavaScriptMessageReceived(object? sender, string e)
        {
            MessageBox.Show(e);
        }

        private void CurrentWebView_ResourceResponded(object? sender, string e)
        {
            MessageBox.Show(e);
        }

        private void CurrentWebView_FindResult(object? sender, FindResult e)
        {
            //MessageBox.Show($"{e.ActiveMatch}/{e.MatchCount}");
        }

        private void CurrentWebView_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
        }

        private void AttachContextMenu(IWebView webView, FrameworkElement hostElement)
        {
            webView.ContextMenuRequested += (s, e) =>
            {
                var menu = new ContextMenu();
                foreach (WebContextMenuType i in Enum.GetValues(typeof(WebContextMenuType)))
                {
                    if (e.MenuType.HasFlag(i))
                    {
                        menu.Items.Add(new MenuItem
                        {
                            Header = i.ToString(),
                            Command = new RelayCommand(_ => { })
                        });
                    }
                }
                menu.Items.Add(new Separator());
                
                menu.Items.Add(new MenuItem
                {
                    Icon = "\uE76B",
                    Header = e.MediaType.ToString(),
                    Command = new RelayCommand(_ => { })
                });
                if (!string.IsNullOrEmpty(e.SelectionText))
                {
                    menu.Items.Add(new MenuItem
                    {
                        Icon = "\uE76B",
                        Header = "Copy",
                        Command = new RelayCommand(_ => webView.Copy())
                    });
                }
                menu.Items.Add(new MenuItem
                {
                    Icon = "\uE76B",
                    Header = "Reload",
                    Command = new RelayCommand(_ => webView.Refresh())
                });
                if (!string.IsNullOrEmpty(e.LinkUrl))
                {
                    menu.Items.Add(new MenuItem
                    {
                        Icon = "\uE76B",
                        Header = "Open link in new tab",
                        Command = new RelayCommand(_ => MessageBox.Show(e.LinkUrl))
                    });
                }
                menu.PlacementTarget = hostElement;
                menu.IsOpen = true;
            };
        }

        private void CurrentWebView_DownloadCompleted(WebDownloadItem Item)
        {
            CurrentDownload = null;
            DownloadStatus.Text = $"{App.FormatBytes(Item.TotalBytes)} - {Item.State.ToString()}";
            DownloadProgressBar.Value = 1;
        }

        private void CurrentWebView_DownloadUpdated(WebDownloadItem Item)
        {
            DownloadProgressBar.Value = Item.Progress;
            if (Item.TotalBytes > 0)
                DownloadStatus.Text = App.FormatBytes(Item.ReceivedBytes, false) + "/" + App.FormatBytes(Item.TotalBytes) + " - Downloading";
            else
            {
                DownloadStatus.Text = App.FormatBytes(Item.ReceivedBytes) + " - Downloading";
                DownloadProgressBar.IsIndeterminate = true;
            }
        }

        private void CurrentWebView_DownloadStarted(WebDownloadItem Item)
        {
            CurrentDownload = Item;
            DownloadText.Text = Item.FileName;
        }

        WebDownloadItem? CurrentDownload;

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDownload?.Resume();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDownload?.Pause();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDownload?.Cancel();
        }

        private void CurrentWebView_FullscreenChanged(object? sender, bool e)
        {
            if (e)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
                WindowStyle = WindowStyle.SingleBorderWindow;
        }

        private void CurrentWebView_StatusMessage(object? sender, string e)
        {
            Status.Text = e;
        }

        private void CurrentWebView_PermissionRequested(object? sender, PermissionRequestedEventArgs e)
        {
            var Result = MessageBox.Show(e.Kind.ToString(), "Accept", MessageBoxButton.YesNoCancel);
            if (Result == MessageBoxResult.Yes)
                e.State = WebPermissionState.Allow;
            else if (Result == MessageBoxResult.No)
                e.State = WebPermissionState.Deny;
            else if (Result == MessageBoxResult.Cancel)
                e.State = WebPermissionState.Default;
        }

        private void CurrentWebView_NewWindowRequested(NewWindowRequest e)
        {
            CurrentWebView.Address = e.Url;
            //MessageBox.Show(Url);
        }

        private byte[] DownloadImageDataAsync(string Url)
        {
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", UserAgentGenerator.BuildChromeBrand());
                    _WebClient.Headers.Add("Accept", "image/*;");
                    return _WebClient.DownloadData(Url);
                }
                catch { return null; }
            }
        }
        private void CurrentWebView_FaviconChanged(object? sender, string e)
        {
            if (string.IsNullOrEmpty(e))
                return;
            byte[] ImageData = DownloadImageDataAsync(e);
            if (ImageData != null)
            {
                try
                {
                    BitmapImage Bitmap = new BitmapImage();
                    using (MemoryStream Stream = new MemoryStream(ImageData))
                    {
                        Bitmap.BeginInit();
                        Bitmap.StreamSource = Stream;
                        Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        Bitmap.EndInit();
                        if (Bitmap.CanFreeze)
                            Bitmap.Freeze();
                    }
                    Icon = Bitmap;
                }
                catch { }
            }
        }

        private void CurrentWebView_LoadingStateChanged(object? sender, bool e)
        {
            //CurrentWebView.ZoomFactor = 1;
            AddressBar.Text = CurrentWebView.Address;
            BackButton.IsEnabled = CurrentWebView.CanGoBack;
            ForwardButton.IsEnabled = CurrentWebView.CanGoForward;
            ReloadButton.Content = e ? "\xF78A" : "\xE72C";
            if (!e)
            {
                CurrentWebView.Find("a", false, false, false);
                //CurrentWebView.ExecuteScript("engine.postMessage(\"Hello from JS\");"); //Cef
                //CurrentWebView.ExecuteScript("window.chrome.webview.postMessage(\"Hello from JS\");"); //Edge
                //CurrentWebView.ExecuteScript("window.external.postMessage(\"Hello from JS\");"); //IE
                /*Dispatcher.BeginInvoke(async () =>
                {
                    MessageBox.Show("1");
                    MessageBox.Show("Source 2" + await CurrentWebView.GetSourceAsync());
                    MessageBox.Show("2");
                });*/
            }
            CurrentWebView.CallDevToolsAsync("Page.setPrerenderingAllowed", new
            {
                isAllowed = false
            });
            CurrentWebView.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
            {
                enabled = true
            });
            /*CurrentWebView.CallDevToolsAsync("Emulation.setHardwareConcurrencyOverride", new
            {
                hardwareConcurrency = 11
            });*/
            //Emulation.setDataSaverOverride
        }

        private void CurrentWebView_TitleChanged(object? sender, string e)
        {
            Title = e;
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(AddressBar.Text))
            {
                var url = AddressBar.Text.Trim();
                if (!Utils.IsUrl(url))
                    url = "https://" + url;

                CurrentWebView.Navigate(url);
            }
        }

        void ToggleMute()
        {
            CurrentWebView.IsMuted = !CurrentWebView.IsMuted;
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
            Action((Actions)int.Parse(Values[0]), sender, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }
        public void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            V1 = V1.Replace("{CurrentUrl}", CurrentWebView.Address).Replace("{Homepage}", "https://google.com/");

            switch (_Action)
            {
                case Actions.Undo:
                    CurrentWebView.Back();
                    break;
                case Actions.Redo:
                    CurrentWebView.Forward();
                    break;
                case Actions.Refresh:
                    CurrentWebView.Refresh();
                    break;
                case Actions.Navigate:
                    CurrentWebView.Navigate(V1);
                    break;

                case Actions.Print:
                    CurrentWebView.Print();
                    break;
                case Actions.Mute:
                    ToggleMute();
                    break;
                /*case Actions.Find:
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
                    break;*/
            }
        }

        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var Result = await CurrentWebView.TakeScreenshotAsync(WebScreenshotFormat.PNG);
            File.WriteAllBytes("screenshot.png", Result);
            Process.Start(new ProcessStartInfo("screenshot.png") { UseShellExecute = true });
        }
    }
}
