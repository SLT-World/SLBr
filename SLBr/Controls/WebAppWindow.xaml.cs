using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SLBr.Controls
{
    public partial class WebAppWindow : Window
    {
        public static void SetTaskbarIcon(Window _Window, string IconPath)
        {
            if (File.Exists(IconPath))
            {
                var bitmap = new BitmapImage(new Uri(IconPath, UriKind.RelativeOrAbsolute));
                _Window.TaskbarItemInfo = new TaskbarItemInfo
                {
                    Overlay = bitmap
                };
            }
        }

        public ChromiumWebBrowser Browser;
        WebAppManifest Manifest;
        bool DarkMode;
        public WebAppWindow(WebAppManifest _Manifest)
        {
            Manifest = _Manifest;
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(Manifest?.ThemeColor))
            {
                DarkMode = DarkTheme(Utils.HexToColor(Manifest.ThemeColor));
                ApplyTheme(DarkMode);
            }

            Dispatcher.BeginInvoke(() =>
            {
                string AppsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SLBr", "Apps");
                string ID = Utils.SanitizeFileName(Manifest.StartUrl);
                string ImagePath = Path.Combine(AppsFolder, $"{ID}.ico");
                Icon = BitmapFrame.Create(new Uri(ImagePath, UriKind.RelativeOrAbsolute));
                SetTaskbarIcon(this, ImagePath);
            });

            Browser = new ChromiumWebBrowser();
            Browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            Browser.Address = Manifest.StartUrl;

            Browser.LifeSpanHandler = new WebAppLifeSpanHandler(this);
            Browser.RequestHandler = new WebAppRequestHandler(this);

            Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
            Browser.TitleChanged += Browser_TitleChanged;
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            Browser.ZoomLevelIncrement = 0.5f;
            Browser.AllowDrop = true;
            Browser.IsManipulationEnabled = true;
            Browser.UseLayoutRounding = true;

            Browser.BrowserSettings = new BrowserSettings
            {
                BackgroundColor = 0x000000,
                ChromeStatusBubble = CefState.Disabled,
                ChromeZoomBubble = CefState.Disabled,
            };
            WebContent.Visibility = Visibility.Collapsed;
            WebContent.Children.Add(Browser);
            int trueValue = 0x01;
            DllUtils.DwmSetWindowAttribute(HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle()).Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        private void Browser_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (Browser.IsBrowserInitialized)
                WebContent.Visibility = Visibility.Visible;
        }

        private void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!e.Browser.IsValid)
                return;
            e.Browser.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(DarkMode);
        }

        public bool DarkTheme(Color BaseColor)
        {
            double a = 1 - (0.299 * BaseColor.R + 0.587 * BaseColor.G + 0.114 * BaseColor.B) / 255;
            return a >= 0.7;
        }

        public void ApplyTheme(bool Dark)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (Dark)
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Title = e.NewValue + $" - {Manifest.Name}";
        }
    }

    public class WebAppRequestHandler : IRequestHandler
    {
        public WebAppWindow WebApp;

        public WebAppRequestHandler(WebAppWindow _WebApp)
        {
            WebApp = _WebApp;
        }

        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            WebApp.Dispatcher.Invoke(() =>
            {
                using (callback)
                {
                    CredentialsDialogWindow _CredentialsDialogWindow = new CredentialsDialogWindow($"Sign in to {host}", "\uec19");
                    _CredentialsDialogWindow.Topmost = true;
                    if (_CredentialsDialogWindow.ShowDialog().ToBool())
                        callback.Continue(_CredentialsDialogWindow.Username, _CredentialsDialogWindow.Password);
                    else
                        callback.Cancel();
                }
            });
            return true;
        }
        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            return false;
        }
        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            return true;
        }
        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
            {
                WebApp.Browser.Address = targetUrl;
                return true;
            }
            return false;
        }

        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return new WebAppResourceRequestHandler();
        }

        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            //chromiumWebBrowser.ExecuteScriptAsync(Scripts.ScrollCSS);
            //if (!App.Instance.LiteMode && bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
            //chromiumWebBrowser.ExecuteScriptAsync(Scripts.ScrollScript);
        }

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            callback.Dispose();
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage)
        {
        }
    }
    public class WebAppResourceRequestHandler : IResourceRequestHandler
    {
        public WebAppResourceRequestHandler()
        {
        }

        public void Dispose()
        {
            GC.Collect(GC.MaxGeneration);
            GC.SuppressFinalize(this);
        }

        public ICookieAccessFilter GetCookieAccessFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return null;
        }

        public IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return null;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return null;
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            return CefReturnValue.Continue;
        }

        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return true;
        }

        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
        }

        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
        }

        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return false;
        }
    }
    /*public class WebAppDownloadHandler : IDownloadHandler
    {
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                using (callback)
                    callback.Continue(Path.Combine(App.Instance.GlobalSave.Get("DownloadPath"), downloadItem.SuggestedFileName), true);
            }
            return true;
        }

        private Dictionary<int, IDownloadItemCallback> DownloadCallbacks = new Dictionary<int, IDownloadItemCallback>();

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
        }
    }*/
    public class WebAppLifeSpanHandler : ILifeSpanHandler
    {
        public WebAppWindow WebApp;
        public WebAppLifeSpanHandler(WebAppWindow _WebApp)
        {
            WebApp = _WebApp;
        }

        public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName,
            WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo,
            IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            /*if (targetDisposition == WindowOpenDisposition.CurrentTab)
                browser.MainFrame.LoadUrl(targetUrl);
            else
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                string TargetOrigin = Utils.GetOrigin(targetUrl);
                string TopLevelOrigin = Utils.GetOrigin(browser.MainFrame.Url);
                ContentSettingValues Value = GlobalRequestContext.GetContentSetting(TopLevelOrigin, TopLevelOrigin, ContentSettingTypes.Popups);
                WebApp.Dispatcher.Invoke(() =>
                {
                    if (targetDisposition != WindowOpenDisposition.NewPopup)
                        browser.MainFrame.LoadUrl(targetUrl);
                });
            }*/

            if (targetDisposition != WindowOpenDisposition.NewPopup)
                browser.MainFrame.LoadUrl(targetUrl);
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            return false;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
