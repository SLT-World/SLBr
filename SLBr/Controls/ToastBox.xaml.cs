using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for ToastBox.xaml
    /// </summary>
    public partial class ToastBox : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();

        private const int DEFAULT_TIMER = 10;
        private const string DEFAULT_TITLE = "Notification";

        public ToastBox() { }

        public ToastBox(string message)
            : this(message, DEFAULT_TITLE, DEFAULT_TIMER) { }
        public ToastBox(string message, string title)
            : this(message, title, DEFAULT_TIMER) { }

        public ToastBox(string _Title, string _Description, int Delay, Theme _Theme = null)
        {
            InitializeComponent();
            timer.Tick += new EventHandler(CloseToast);

            //MessageTitle.Text = _Title;
            Description.Text = _Description;
            timer.Interval = new TimeSpan(0, 0, Delay);
            timer.Start();
            var Monitor = MonitorMethods.MonitorFromWindow(new WindowInteropHelper(MainWindow.Instance).EnsureHandle(), MonitorMethods.MONITOR_DEFAULTTONEAREST);
            
            if (Monitor != IntPtr.Zero)
            {
                var MonitorInfo = new MonitorMethods.NativeMonitorInfo();
                MonitorMethods.GetMonitorInfo(Monitor, MonitorInfo);
                //var desktopWorkingArea = SystemParameters.WorkArea;
                var desktopWorkingArea = MonitorInfo.Monitor;
                Left = desktopWorkingArea.Right - this.Width - 2.5;
                Top = desktopWorkingArea.Bottom - this.Height - 37.5;// + 10
            }
            
            if (_Theme == null)
                _Theme = MainWindow.Instance.GetTheme();
            ApplyTheme(_Theme);
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
        private void ToastClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void CloseToast(object sender, EventArgs e)
        {
            Close();
        }
        public static void Show(string _Title, string _Description, int Delay, Theme _Theme = null)
        {
            new ToastBox(_Title, _Description, Delay, _Theme).Show();
        }
    }
}
