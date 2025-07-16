using CefSharp;
using CefSharp.BrowserSubprocess;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;

namespace SLBr
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            //https://github.com/dotnet/runtime/issues/93914
            //https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector
            /*Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");*/

            /*string Username = "Default";
            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--user=", StringComparison.Ordinal))
                {
                    Username = Flag.Replace("--user=", "").Replace(" ", "-");
                    System.Windows.MessageBox.Show(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username, "Save.bin")).Contains("Performance<:>2").ToString());
                }
            }
            */
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
            MinimizeMemory();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
                return SelfHost.Main(args);
            else
            {
                /*DispatcherTimer FlushTimer = new DispatcherTimer();
                FlushTimer.Interval = new TimeSpan(500);
                FlushTimer.Tick += FlushTimer_Tick;*/
                BackgroundWorker Worker = new BackgroundWorker();
                Worker.DoWork += Worker_DoWork;
                Worker.RunWorkerAsync();
                App.Main();

                Cef.Shutdown();
                return Environment.ExitCode;
            }
        }
        private static void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            OptimizeMemoryUsage();
        }
        private static void OptimizeMemoryUsage()
        {
            while (true)
            {
                try
                {
                    FlushMemory();
                    MinimizeFootprint();
                }
                finally
                {
                    Thread.Sleep(60000);
                }
            }
        }

        /*[DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        static extern bool SetProcessWorkingSetSize32(IntPtr pProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        static extern bool SetProcessWorkingSetSize64(IntPtr pProcess, long dwMinimumWorkingSetSize, long dwMaximumWorkingSetSize);*/

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        private static void FlushMemory()
        {
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            /*if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (IntPtr.Size == 8)
                    SetProcessWorkingSetSize64(Process.GetCurrentProcess().Handle, -1, -1);
                else
                    SetProcessWorkingSetSize32(Process.GetCurrentProcess().Handle, -1, -1);
            }*/
        }

        private static void MinimizeFootprint()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        /*private static void FlushTimer_Tick(object? sender, EventArgs e)
        {
            Process[] SubProcesses = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process SubProcess in SubProcesses)
            {
                int CurrentMemoryUsage = (int)SubProcess.WorkingSet64;
                int CurrentMemoryMB = (CurrentMemoryUsage / 1024 / 1024);
                int MaxMemoryUsage = (int)Math.Round(CurrentMemoryMB * 0.3f);
                Utils.LimitMemoryUsage(SubProcess.Handle, MaxMemoryUsage);
            }
            Utils.LimitMemoryUsage(Process.GetCurrentProcess().Handle, 10);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Chromium.ExecuteScriptAsync("window.gc();");
        }*/

        private static void MinimizeMemory()
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            //SetCpuAffinity(0x0001);
            /*CurrentProcess.PriorityBoostEnabled = false;
            CurrentProcess.PriorityClass = ProcessPriorityClass.Idle;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;*/

            //GC.Collect(GC.MaxGeneration);
            //GC.WaitForPendingFinalizers();
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT) //It will only run on Windows regardless
            SetProcessWorkingSetSize(CurrentProcess.Handle, -1, -1);
            //EmptyWorkingSet(CurrentProcess.Handle);
        }

        /*[DllImport("kernel32.dll")]
        private static extern bool SetProcessAffinityMask(IntPtr handle, IntPtr affinity);

        public static void SetCpuAffinity(int affinityMask)
        {
            Process process = Process.GetCurrentProcess();
            SetProcessAffinityMask(process.Handle, (IntPtr)affinityMask);
        }*/
    }
}
