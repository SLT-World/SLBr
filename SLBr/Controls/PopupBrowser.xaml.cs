using CefSharp;
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
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

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

            ApplyTheme(App.Instance.CurrentTheme);
            _Browser = new ChromiumWebBrowser();
            _Browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
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
            _Browser.UseLayoutRounding = true;

            _Browser.BrowserSettings = new BrowserSettings
            {
                BackgroundColor = System.Drawing.Color.Black.ToUInt()
            };

            WebContent.Children.Add(_Browser);
            //RenderOptions.SetBitmapScalingMode(_Browser, BitmapScalingMode.LowQuality);

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
            if (!e.Browser.IsValid || e.IsLoading)
                return;
            Dispatcher.Invoke(() =>
            {
                Icon = new BitmapImage(new Uri("http://www.google.com/s2/favicons?sz=24&domain=" + Utils.CleanUrl(_Browser.Address, true, true, true, false, false)));
                _Browser.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(CurrentTheme.DarkWebPage);
            });
        }

        public void ApplyTheme(Theme _Theme)
        {
            CurrentTheme = _Theme;

            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (_Theme.DarkTitleBar)
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Title = e.NewValue + App.Instance.Username == "Default" ? " - SLBr" : $" - {App.Instance.Username} - SLBr";
        }
    }
}
