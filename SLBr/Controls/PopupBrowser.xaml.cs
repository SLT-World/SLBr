using CefSharp;
using CefSharp.DevTools;
using CefSharp.Wpf.HwndHost;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for PopupBrowser.xaml
    /// </summary>
    public partial class PopupBrowser : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        ChromiumWebBrowser _Browser;
        Theme CurrentTheme;
        string InitialAddress;
        public PopupBrowser(string _Address, int _Width, int _Height)
        {
            InitializeComponent();
            InitialAddress = _Address;
            if (_Width != -1)
                Width = _Width;
            if (_Height != -1)
                Height = _Height;
        }

        private void Browser_StatusMessage(object? sender, StatusMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(e.Value))
                    StatusMessage.Text = e.Value;
                StatusBarPopup.IsOpen = !string.IsNullOrEmpty(e.Value);
            });
        }

        private void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!_Browser.IsBrowserInitialized)
                return;
            Dispatcher.Invoke(() =>
            {
                if (!e.IsLoading)
                {
                    Icon = new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Utils.Host(_Browser.Address)));
                    DevToolsClient _DevToolsClient = _Browser.GetDevToolsClient();
                    _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(CurrentTheme.DarkWebPage);
                }
            });
        }

        public void ApplyTheme(Theme _Theme)
        {
            CurrentTheme = _Theme;
            int SetDarkTitleBar = _Theme.DarkTitleBar ? 1 : 0;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref SetDarkTitleBar, Marshal.SizeOf(true));
            
            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Title = e.NewValue + App.Instance.Username == "Default" ? " - SLBr" : $" - {App.Instance.Username} - SLBr";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(App.Instance.CurrentTheme);
            _Browser = new ChromiumWebBrowser();
            _Browser.Address = InitialAddress;

            _Browser.LifeSpanHandler = App.Instance._LifeSpanHandler;
            _Browser.DownloadHandler = App.Instance._DownloadHandler;
            _Browser.RequestHandler = App.Instance._RequestHandler;
            _Browser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            //_Browser.KeyboardHandler = MainWindow.Instance._KeyboardHandler;
            //_Browser.JsDialogHandler = MainWindow.Instance._JsDialogHandler;

            _Browser.TitleChanged += Browser_TitleChanged;
            _Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            _Browser.ZoomLevelIncrement = 0.5f;
            _Browser.StatusMessage += Browser_StatusMessage;
            _Browser.AllowDrop = true;
            _Browser.IsManipulationEnabled = true;

            _Browser.BrowserSettings = new BrowserSettings
            {
                /*WindowlessFrameRate = App.Instance.Framerate,
                Javascript = App.Instance.Javascript,
                ImageLoading = App.Instance.LoadImages,
                LocalStorage = App.Instance.LocalStorage,
                Databases = App.Instance.Databases,
                WebGl = App.Instance.WebGL,*/
                BackgroundColor = System.Drawing.Color.Black.ToUInt()
            };

            WebContent.Children.Add(_Browser);
            RenderOptions.SetBitmapScalingMode(_Browser, BitmapScalingMode.LowQuality);
            _Browser.UseLayoutRounding = true;
        }
    }
}
