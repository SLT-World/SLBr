// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp.BrowserSubprocess;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /*[DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOWNORMAL = 1;*/

        //private static Mutex SingleInstanceMutex;

        //static Mutex mutex = new Mutex(true, "{SLBrSLTBrowser-SLT-WORLD-BROWSER-SLBr}");
        [STAThread]
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                SelfHost.Main(args);
                return;
            }
            /*if (mutex.WaitOne(TimeSpan.Zero, true))
                mutex.ReleaseMutex();
            else
            {
                MessageBox.Show("An instance SLBr is already running...");
                Utils.PostMessage(
                    (IntPtr)Utils.HWND_BROADCAST,
                    Utils.WM_SHOWPAGE,
                    IntPtr.Zero,
                    IntPtr.Zero);
                Current.Shutdown();
            }*/


            /*bool IsNewInstance = false;
            SingleInstanceMutex = new Mutex(true, "SLBrSLTBrowser", out IsNewInstance);
            if (!IsNewInstance)
            {
                MessageBox.Show("Already an instance is running...");
                Current.Shutdown();
            }*/


            /*Process _Process = Process.GetCurrentProcess();
            List<Process> Processes = Process.GetProcesses().Where(p =>
                p.ProcessName == _Process.ProcessName && !_Process.HasExited).ToList();

            int count = Processes.Count() - 1;

            if (count > 1)
            {
                //ShowWindow(Processes[0].MainWindowHandle, SW_MAXIMIZE);
                MessageBox.Show("There " + (count > 2 ? "are" : "is") + $" already {count - 1} instance" + (count > 2 ? "s" : "") + " of SLBr running... Relaunch the application if you think something is wrong.");//BUG, Relaunching in SLBr shows this message
                Current.Shutdown();
            }*/


            /*if (e.Args.Count() > 0)
            {
                MessageBox.Show("You have the latest version.");
                Shutdown();
            }*/
            string ExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            //string IconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources/SLBr.ico");
            JumpTask OpenTask = new JumpTask
            {
                Title = "Open",
                Arguments = "",
                //Description = "Open SLBr",
                CustomCategory = "Actions",
                ApplicationPath = ExecutablePath,
                IconResourcePath = ExecutablePath
            };
            JumpTask PrivateOpenTask = new JumpTask
            {
                Title = "Open in private mode",
                Arguments = "--private",
                //Description = "No browsing history will be saved, in memory cache will be used (Incognito)",
                CustomCategory = "Actions",
                ApplicationPath = ExecutablePath,
                IconResourcePath = ExecutablePath
            };
            JumpTask DeveloperOpenTask = new JumpTask
            {
                Title = "Open in developer mode",
                Arguments = "--developer",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = ExecutablePath,
                IconResourcePath = ExecutablePath
            };
            JumpTask ChromiumOpenTask = new JumpTask
            {
                Title = "Open in chromium mode",
                Arguments = "--chromium",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = ExecutablePath,
                IconResourcePath = ExecutablePath
            };
            JumpTask IEOpenTask = new JumpTask
            {
                Title = "Open in Internet Explorer mode",
                Arguments = "--ie",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = ExecutablePath,
                IconResourcePath = ExecutablePath
            };

            JumpList jumpList = new JumpList();
            jumpList.JumpItems.Add(OpenTask);
            jumpList.JumpItems.Add(PrivateOpenTask);
            jumpList.JumpItems.Add(DeveloperOpenTask);
            jumpList.JumpItems.Add(ChromiumOpenTask);
            jumpList.JumpItems.Add(IEOpenTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

            JumpList.SetJumpList(Current, jumpList);
        }
    }
}
