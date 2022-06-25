using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Threading;
using System.ComponentModel;
using System.IO.Compression;
using System.Diagnostics;

namespace SLBr_Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum OSPlatforms
        {
            Windows,
            MacOS,
            Linux,
        }
        public enum MacOSBuildPlatforms
        {
            Universal,
            Silicon,
            Intel
        }
        public enum ProcessStatus
        {
            Complete,
            Downloading,
            //Updating,
            Deleting
        }

        private ProcessStatus CurrentStatus
        {
            get => _CurrentStatus;
            set
            {
                _CurrentStatus = value;
                switch (_CurrentStatus)
                {
                    case ProcessStatus.Complete:
                        Status.Text = $"[OS: {CurrentOS}, Platform: {CurrentMacOSBuildPlatform}] {_CurrentStatus}...";
                        break;
                    case ProcessStatus.Downloading:
                        Status.Text = $"[OS: {CurrentOS}, Platform: {CurrentMacOSBuildPlatform}] {_CurrentStatus}... {ProgressPercentage}%";
                        break;
                    //case ProcessStatus.Updating:
                    //    Status.Text = $"[OS: {CurrentOS}, Platform: {CurrentMacOSBuildPlatform}] {CurrentStatus}... {ProgressPercentage}%";
                    //    break;
                    case ProcessStatus.Deleting:
                        Status.Text = $"[OS: {CurrentOS}, Platform: {CurrentMacOSBuildPlatform}] {_CurrentStatus}...";
                        break;
                    default:
                        break;
                }
            }
        }
        internal ProcessStatus _CurrentStatus;

        bool IsBusy;

        OSPlatforms CurrentOS;
        MacOSBuildPlatforms CurrentMacOSBuildPlatform = MacOSBuildPlatforms.Intel;

        string ReleasesRSSFeed = "https://github.com/SLT-World/SLBr/releases.atom";
        //string DownloadTemplate = "https://github.com/SLT-World/SLBr/releases/tag/{0}/{0}.zip";
        string OldDownloadTemplate = "https://github.com/SLT-World/SLBr/releases/download/{0}/SLBr_Portable_{0}.zip";

        string WindowsDownloadTemplate = "https://github.com/SLT-World/SLBr/releases/download/{0}/{0}_Windows.zip";
        string LinuxDownloadTemplate = "https://github.com/SLT-World/SLBr/releases/download/{0}/{0}_Linux.zip";
        string MacOSDownloadTemplate = "https://github.com/SLT-World/SLBr/releases/download/{0}/{0}_Mac.zip";

        string DownloadPathTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "{0}");
        string DownloadPath;
        string DownloadZip;

        WebClient _WebClient = new WebClient();

        int ProgressPercentage
        {
            get { return _ProgressPercentage; }
            set
            {
                _ProgressPercentage = value;
                _ProgressBar.Value = value;
            }
        }
        internal int _ProgressPercentage;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }
        public MainWindow()
        {
            InitializeComponent();

            //HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            //source.AddHook(new HwndSourceHook(WndProc));
            //Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 15 });
            VersionSave = new Utils.Saving(true, "VersionInfo.bin");
        }

        string LocalVersion;
        string LatestVersion;

        Utils.Saving VersionSave;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Style s = new Style();
            //s.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            //Transitions.ItemContainerStyle = s;

            if (!VersionSave.Has("LocalVersion"))
                LocalVersion = "0.0.0.0";
            else
                LocalVersion = VersionSave.Get("LocalVersion");

            ApplicationComboBox.Items.Add("SLBr");
            //ApplicationComboBox.Items.Add("SLBr Lite");
            ApplicationComboBox.SelectedIndex = 0;

            OSPlatformComboBox.Items.Add("Windows");
            OSPlatformComboBox.Items.Add("MacOS");
            OSPlatformComboBox.Items.Add("Linux");
            OSPlatformComboBox.SelectedIndex = 0;

            SetupBuildPlatformSelection(OSPlatforms.Windows);

            _WebClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            _WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);

            ReleaseFeedXML = _WebClient.DownloadString(ReleasesRSSFeed);

            var _Document = new HtmlDocument();
            _Document.LoadHtml(ReleaseFeedXML);

            HtmlNodeCollection xnl = _Document.DocumentNode.SelectNodes("//feed/entry");
            HtmlNode _Node = xnl[0];
            LatestVersion = _Node.ChildNodes["title"].InnerText;

            Title.Text = $"SLBr {LatestVersion} - Released";
            if (LatestVersion != LocalVersion)
            {
                //MessageBox.Show($"Update {LatestReleaseVersion} is recommended...");
            }
            else
            {
                Transitions.SelectedItem = LaunchTransition;
            }
        }

        string ReleaseFeedXML;
        private void WebClient_DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                File.Delete(DownloadZip);
                _WebClient.Dispose();
                _WebClient = new WebClient();
                _WebClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                _WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);
                return;
            }
            CurrentStatus = ProcessStatus.Complete;
            IsBusy = false;
            LocalVersion = LatestVersion;
            ZipFile.ExtractToDirectory(DownloadZip, DownloadPath, true);
            File.Delete(DownloadZip);
            Transitions.SelectedItem = LaunchTransition;
        }
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressPercentage = (int)e.ProgressPercentage;
            CurrentStatus = ProcessStatus.Downloading;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _WebClient.CancelAsync();
            VersionSave.Set("LocalVersion", LocalVersion);
        }

        private void StartDownload()
        {
            if (Utils.CheckForInternetConnection())
            {
                Transitions.SelectedItem = ProgressTransition;
                IsBusy = true;
                _WebClient.DownloadFileAsync(new Uri(string.Format(OldDownloadTemplate, LatestVersion)), (string)ApplicationComboBox.SelectedItem + ".zip");
            }
            else
                MessageBox.Show("Sorry, a stable internet connection is required to install SLBr.");
        }

        private void SetupBuildPlatformSelection(OSPlatforms _OSPlatforms)
        {
            BuildPlatformComboBox.Items.Clear();
            switch (_OSPlatforms)
            {
                case OSPlatforms.Windows:
                    BuildPlatformComboBox.Visibility = Visibility.Collapsed;
                    //BuildPlatformComboBox.Items.Add("ARM");
                    //BuildPlatformComboBox.Items.Add("x64");
                    //BuildPlatformComboBox.Items.Add("x86");
                    break;
                case OSPlatforms.MacOS:
                    BuildPlatformComboBox.Visibility = Visibility.Visible;
                    BuildPlatformComboBox.Items.Add("Universal");//Both
                    BuildPlatformComboBox.Items.Add("Silicon");//AArach64
                    BuildPlatformComboBox.Items.Add("Intel");//86_64
                    BuildPlatformComboBox.SelectedIndex = 0;
                    break;
                case OSPlatforms.Linux:
                    BuildPlatformComboBox.Visibility = Visibility.Collapsed;
                    //BuildPlatformComboBox.Items.Add("ARM");
                    //BuildPlatformComboBox.Items.Add("ARM64");
                    //BuildPlatformComboBox.Items.Add("x64");
                    break;
            }
            CurrentOS = _OSPlatforms;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            //OSPlatforms OS = IndexToOS(OSPlatformComboBox.SelectedIndex);
            CurrentMacOSBuildPlatform = CurrentOS == OSPlatforms.MacOS ? IndexToMacOSBuildPlatform(BuildPlatformComboBox.SelectedIndex) : MacOSBuildPlatforms.Intel;
            StartDownload();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsBusy)
                Close();
            else
            {
                if (MessageBox.Show("Application is busy... Close Application?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    Close();
                //MessageBox.Show("Are you sure you want to quit while the process is busy?");
            }
        }

        private void OSPlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBox _ComboBox = sender as ComboBox;
            SetupBuildPlatformSelection(IndexToOS(OSPlatformComboBox.SelectedIndex));
        }

        private MacOSBuildPlatforms IndexToMacOSBuildPlatform(int Index) =>
            (MacOSBuildPlatforms)Index;

        private OSPlatforms IndexToOS(int Index) =>
            (OSPlatforms)Index;
        private OSPlatforms StringToOS(string OS)
        {
            switch (OS)
            {
                case "Windows":
                    return OSPlatforms.Windows;
                case "MacOS":
                    return OSPlatforms.MacOS;
                case "Linux":
                    return OSPlatforms.Linux;
                default:
                    return OSPlatforms.Windows;
            }
        }

        private void ApplicationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DownloadPath = string.Format(DownloadPathTemplate, (string)ApplicationComboBox.SelectedItem);
            DownloadZip = DownloadPath + ".zip";
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo(Path.Combine(DownloadPath, "SLBr.exe"));
            StartInfo.WorkingDirectory = Path.Combine(DownloadPath);
            Process.Start(StartInfo);
        }
    }
}
