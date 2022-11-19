using CefSharp;
using CefSharp.DevTools;
using CefSharp.Wpf.HwndHost;
using SLBr.Handlers;
using System;
using System.Runtime.InteropServices;
using System.Security.Policy;
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
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                //StatusBar.Visibility = string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible;
                StatusBarPopup.IsOpen = !string.IsNullOrEmpty(e.Value);
                StatusMessage.Text = e.Value;
            }));
        }

        private void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!_Browser.IsBrowserInitialized)
                return;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Icon = new BitmapImage(new Uri("https://www.google.com/s2/favicons?sz=24&domain=" + Utils.Host(_Browser.Address)));
                DevToolsClient _DevToolsClient = _Browser.GetDevToolsClient();
                _DevToolsClient.Emulation.SetAutoDarkModeOverrideAsync(CurrentTheme.DarkWebPage ? bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebPage")) : false);
            }));
        }

        public void ApplyTheme(Theme _Theme)
        {
            CurrentTheme = _Theme;
            int SetDarkTitleBar = _Theme.DarkTitleBar ? 1 : 0;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref SetDarkTitleBar, Marshal.SizeOf(true));
            
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

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Title = $"{e.NewValue} - SLBr";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(MainWindow.Instance.GetTheme());
            _Browser = new ChromiumWebBrowser();
            _Browser.Address = InitialAddress;

            _Browser.LifeSpanHandler = MainWindow.Instance._LifeSpanHandler;
            _Browser.DownloadHandler = MainWindow.Instance._DownloadHandler;
            _Browser.RequestHandler = new RequestHandler();
            //_Browser.MenuHandler = MainWindow.Instance._ContextMenuHandler;
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
                WindowlessFrameRate = MainWindow.Instance.Framerate,
                Javascript = MainWindow.Instance.Javascript,
                ImageLoading = MainWindow.Instance.LoadImages,
                LocalStorage = MainWindow.Instance.LocalStorage,
                Databases = MainWindow.Instance.Databases,
                WebGl = MainWindow.Instance.WebGL,
                BackgroundColor = System.Drawing.Color.Black.ToUInt()
            };

            WebContent.Children.Add(_Browser);
            RenderOptions.SetBitmapScalingMode(_Browser, BitmapScalingMode.LowQuality);
        }
    }
}
