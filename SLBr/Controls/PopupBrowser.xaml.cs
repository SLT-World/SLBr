using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

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
            _Browser.RequestHandler = App.Instance._RequestHandler;
            _Browser.ResourceRequestHandlerFactory = new Handlers.ResourceRequestHandlerFactory(App.Instance._RequestHandler);
            _Browser.DownloadHandler = App.Instance._DownloadHandler;
            _Browser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            //_Browser.JsDialogHandler = MainWindow.Instance._JsDialogHandler;

            _Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
            _Browser.TitleChanged += Browser_TitleChanged;
            _Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            _Browser.ZoomLevelIncrement = 0.5f;
            _Browser.StatusMessage += Browser_StatusMessage;
            _Browser.AllowDrop = true;
            _Browser.IsManipulationEnabled = true;
            _Browser.UseLayoutRounding = true;

            _Browser.BrowserSettings = new BrowserSettings
            {
                BackgroundColor = 0x000000
            };
            WebContent.Visibility = Visibility.Collapsed;
            WebContent.Children.Add(_Browser);
            int trueValue = 0x01;
            DwmSetWindowAttribute(HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle()).Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        private void Browser_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (_Browser.IsBrowserInitialized)
                WebContent.Visibility = Visibility.Visible;
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
            if (!e.Browser.IsValid)
                return;
            e.Browser.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(CurrentTheme.DarkWebPage);
            Dispatcher.Invoke(() =>
            {
                Icon = App.Instance.GetIcon(_Browser.Address);
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
