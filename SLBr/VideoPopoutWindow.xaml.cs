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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for VideoPopoutWindow.xaml
    /// </summary>
    public partial class VideoPopoutWindow : Window
    {
        public static VideoPopoutWindow Instance;

        public string Code;
        public int StartSeconds;
        public VideoProvider Provider;
        public enum VideoProvider
        {
            Youtube
        }
        public VideoPopoutWindow(string _Code, int _StartSeconds, VideoProvider _VideoProvider)
        {
            Instance = this;
            MainWindow.Instance.SetBrowserEmulationVersion();
            InitializeComponent();
            SetUp(_Code, _StartSeconds, _VideoProvider, false);
        }
        public void SetUp(string _Code, int _StartSeconds, VideoProvider _VideoProvider, bool LoadBrowser = true)
        {
            bool BoolHide = _Code == "" || _Code == Code;
            Code = _Code;
            StartSeconds = _StartSeconds;
            Provider = _VideoProvider;
            if (BoolHide)
                HideWindow();
            else
            {
                Show();
                if (LoadBrowser)
                    WebBrowserControl.NavigateToString(GetVideoEmbed(Code, StartSeconds, Provider));
            }
        }
        private static string GetVideoEmbed(string _Code, int _StartSeconds, VideoProvider _Provider)
        {
            var _StringBuilder = new StringBuilder();

            const string YOUTUBE_URL = @"http://www.youtube-nocookie.com/embed/";

            _StringBuilder.Append("<html>");
            _StringBuilder.Append("    <head>");
            _StringBuilder.Append("        <meta content='IE=Edge' http-equiv='X-UA-Compatible'/>");
            _StringBuilder.Append("    </head>");
            _StringBuilder.Append("    <body style=\"margin: 0;\">");
            switch (_Provider)
            {
                case VideoProvider.Youtube:
                    _StringBuilder.Append($"    <iframe style=\"height: 200px; width: 400px; display: block; border:none;\" src=\"{YOUTUBE_URL + _Code}?start={_StartSeconds}&autoplay=1&modestbranding=1\" frameborder=\"0\" allow=\"autoplay; encrypted-media\"></iframe>");//controls=0&
                    break;
            }
            _StringBuilder.Append("    </body>");
            _StringBuilder.Append("</html>");

            return _StringBuilder.ToString();
        }
        #region Window
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
            WebBrowserControl.NavigateToString(GetVideoEmbed(Code, StartSeconds, Provider));
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
        private void HideWindow()
        {
            WebBrowserControl.Navigate("about:blank");
            GC.Collect();
            Hide();
        }
        #endregion
    }
}
