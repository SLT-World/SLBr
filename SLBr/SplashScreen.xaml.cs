using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public static SplashScreen Instance;

        MainWindow _Window;
        //BackgroundWorker ProgressWorker;

        public SplashScreen()
        {
            Instance = this;
            InitializeComponent();
            Icon.Source = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "SLBr.ico")));

            /*ProgressWorker = new BackgroundWorker();
            ProgressWorker.WorkerReportsProgress = true;
            ProgressWorker.DoWork += ProgressWorker_DoWork;
            ProgressWorker.ProgressChanged += ProgressWorker_ProgressChanged;
            ProgressWorker.RunWorkerCompleted += ProgressWorker_RunWorkerCompleted;
            ProgressWorker.RunWorkerAsync();*/

            //new MainWindow();
            /*_Window = new MainWindow(ProgressWorker);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
            };*/
        }

        public void ReportProgress(int ProgressPercentage, string UserState)
        {
            LoadingProgress.Value = ProgressPercentage;
            ProgressStatus.Text = UserState;

            if (ProgressPercentage == 100)
            {
                //App.Instance.CurrentFocusedWindow().Show();
                Close();
            }
        }

        /*private void ProgressWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _Window.Show();
        }

        private void ProgressWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            LoadingProgress.Value = e.ProgressPercentage;
            //Thread.Sleep(1000);
            ProgressStatus.Text = (string)e.UserState;
        }

        private void ProgressWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            //ProgressWorker.ReportProgress(0, "Processing...");
            for (int i = 0; i < 1000; i++)
            {
                //ProgressWorker.ReportProgress(i, $"Processing interation {i}...");
                //Thread.Sleep(i);
            }
            //ProgressWorker.ReportProgress(100, "Done processing.");
        }*/
    }
}
