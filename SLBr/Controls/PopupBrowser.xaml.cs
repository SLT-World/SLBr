using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.UI.ViewManagement.Core;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for PopupBrowser.xaml
    /// </summary>
    public partial class PopupBrowser : Window
    {
        public IWebView WebView;

        public PopupBrowser(string _Address, int _Width, int _Height)
        {
            InitializeComponent();
            if (_Width != -1)
                Width = _Width;
            if (_Height != -1)
                Height = _Height;
            ApplyTheme(App.Instance.CurrentTheme);

            WebViewBrowserSettings Settings = new WebViewBrowserSettings()
            {
                JavaScriptMessage = false
            };

            WebView = WebViewManager.Create((WebEngineType)App.Instance.GlobalSave.GetInt("WebEngine"), [new(true, _Address)], Settings);
            WebView.StatusMessage += WebView_StatusMessage;
            WebView.LoadingStateChanged += WebView_LoadingStateChanged;
            WebView.TitleChanged += WebView_TitleChanged;
            WebView.IsBrowserInitializedChanged += WebView_IsBrowserInitializedChanged;
            WebView.FaviconChanged += WebView_FaviconChanged;
            WebView.ResourceRequested += WebView_ResourceRequested;
            WebView.ContextMenuRequested += WebView_ContextMenuRequested;
            /*_Browser.LifeSpanHandler = App.Instance._LifeSpanHandler;
            _Browser.RequestHandler = App.Instance._RequestHandler;
            _Browser.ResourceRequestHandlerFactory = new Handlers.ResourceRequestHandlerFactory(App.Instance._RequestHandler);
            _Browser.DownloadHandler = App.Instance._DownloadHandler;
            _Browser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            //_Browser.JsDialogHandler = MainWindow.Instance._JsDialogHandler;*/

            WebView.Control.AllowDrop = true;
            WebView.Control.IsManipulationEnabled = true;
            WebView.Control.UseLayoutRounding = true;

            WebContent.Visibility = Visibility.Collapsed;
            WebContent.Children.Add(WebView.Control);
            int trueValue = 0x01;
            DllUtils.DwmSetWindowAttribute(HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle()).Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        private void WebView_ContextMenuRequested(object? sender, WebContextMenuEventArgs e)
        {
            bool IsPageMenu = true;
            ContextMenu BrowserMenu = new ContextMenu();
            foreach (WebContextMenuType i in Enum.GetValues(typeof(WebContextMenuType)))
            {
                if (e.MenuType.HasFlag(i))
                {
                    if (BrowserMenu.Items.Count != 0 && BrowserMenu.Items[BrowserMenu.Items.Count - 1].GetType() == typeof(MenuItem))
                        BrowserMenu.Items.Add(new Separator());
                    if (i == WebContextMenuType.Link)
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open in new tab", Command = new RelayCommand(_ => App.Instance.CurrentFocusedWindow().NewTab(e.LinkUrl, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy link", Command = new RelayCommand(_ => Clipboard.SetText(e.LinkUrl)) });
                    }
                    else if (i == WebContextMenuType.Selection && !e.IsEditable)
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => App.Instance.CurrentFocusedWindow().NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText)), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+C", Icon = "\ue8c8", Header = "Copy", Command = new RelayCommand(_ => Clipboard.SetText(e.SelectionText)) });
                        BrowserMenu.Items.Add(new Separator());
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select all", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                    }
                    else if (i == WebContextMenuType.Media)
                    {
                        IsPageMenu = false;
                        if (e.MediaType == WebContextMenuMediaType.Image)
                        {
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\xe8b9",
                                Header = "Copy image",
                                Command = new RelayCommand(_ => {
                                    try { Utils.DownloadAndCopyImage(e.SourceUrl); }
                                    catch { Clipboard.SetText(e.SourceUrl); }
                                })
                            });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy image link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save image as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\uF6Fa",
                                Header = "Search image",
                                Command = new RelayCommand(_ => {

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
                                    App.Instance.CurrentFocusedWindow().NewTab(Url, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                                })
                            });
                        }
                        else if (e.MediaType == WebContextMenuMediaType.Video)
                        {
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy video link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save video as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uee49", Header = "Picture in picture", Command = new RelayCommand(_ => WebView?.ExecuteScript("(async()=>{let playingVideo=Array.from(document.querySelectorAll('video')).find(v=>!v.paused&&!v.ended&&v.readyState>2);if (!playingVideo){playingVideo=document.querySelector('video');}if (playingVideo&&document.pictureInPictureEnabled){await playingVideo.requestPictureInPicture();}})();")) });
                        }
                    }
                }
            }
            if (e.IsEditable)
            {
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
                if (!string.IsNullOrEmpty(e.SelectionText))
                {
                    BrowserMenu.Items.Add(new Separator());
                    BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => App.Instance.CurrentFocusedWindow().NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText)), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                }
            }
            else if (IsPageMenu && e.MediaType == WebContextMenuMediaType.None)
            {
                BrowserMenu.Items.Add(new MenuItem { IsEnabled = WebView.CanGoBack, Icon = "\uE76B", Header = "Back", Command = new RelayCommand(_ => WebView?.Back()) });
                BrowserMenu.Items.Add(new MenuItem { IsEnabled = WebView.CanGoForward, Icon = "\uE76C", Header = "Forward", Command = new RelayCommand(_ => WebView?.Forward()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE72C", Header = "Refresh", Command = new RelayCommand(_ => WebView?.Refresh()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save as", Command = new RelayCommand(_ => WebView?.Download(WebView.Address)) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE749", Header = "Print", Command = new RelayCommand(_ => WebView?.Print()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select all", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                BrowserMenu.Items.Add(new Separator());

                BrowserMenu.Items.Add(new MenuItem { IsEnabled = Utils.IsHttpScheme(e.FrameUrl), Icon = "\uE8C1", Header = "Translate", Command = new RelayCommand(_ => WebView.Navigate($"https://translate.google.com/translate?sl=auto&tl=en&hl=en&u={e.FrameUrl}")) });
                
                BrowserMenu.Items.Add(new Separator());

                MenuItem AdvancedSubMenuModel = new MenuItem { Icon = "\uec7a", Header = "Advanced" };
                AdvancedSubMenuModel.Items.Add(new MenuItem { IsEnabled = Utils.IsHttpScheme(e.FrameUrl), Icon = "\ue943", Header = "View source", Command = new RelayCommand(_ => App.Instance.CurrentFocusedWindow().NewTab($"view-source:{e.FrameUrl}", true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                BrowserMenu.Items.Add(AdvancedSubMenuModel);
            }
            BrowserMenu.PlacementTarget = WebView?.Control;
            BrowserMenu.IsOpen = true;
        }

        private void WebView_ResourceRequested(object? sender, ResourceRequestEventArgs e)
        {
            if (!App.Instance.ExternalFonts && e.ResourceRequestType == ResourceRequestType.Font)
            {
                e.Cancel = true;
                return;
            }
        }

        private void WebView_FaviconChanged(object? sender, string e)
        {
            Icon = App.Instance.GetIcon(e);
        }

        private void WebView_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (WebView.IsBrowserInitialized)
                WebContent.Visibility = Visibility.Visible;
        }

        private void WebView_TitleChanged(object? sender, string e)
        {
            Title = e +  " - SLBr";
        }

        private async void WebView_LoadingStateChanged(object? sender, LoadingStateResult e)
        {
            await WebView.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
            {
                enabled = App.Instance.CurrentTheme.DarkWebPage
            });
        }

        private void WebView_StatusMessage(object? sender, string e)
        {
            if (!string.IsNullOrEmpty(e))
                StatusMessage.Text = e;
            StatusBarPopup.IsOpen = !string.IsNullOrEmpty(e);
        }

        public void ApplyTheme(Theme _Theme)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (_Theme.DarkTitleBar)
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }
    }
}
