// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Process _Process = Process.GetCurrentProcess();
            List<Process> Processes = Process.GetProcesses().Where(p =>
                p.ProcessName == _Process.ProcessName && !_Process.HasExited).ToList();

            int count = Processes.Count() - 1;

            if (count > 1)
            {
                //ShowWindow(Processes[0].MainWindowHandle, SW_MAXIMIZE);
                MessageBox.Show("There " + (count > 2 ? "are" : "is") + $" already {count - 1} instance" + (count > 2 ? "s" : "") + " of SLBr running... Relaunch the application if you think something is wrong.");//BUG, Relaunching in SLBr shows this message
                Current.Shutdown();
            }
            /*if (e.Args.Count() > 0)
            {
                MessageBox.Show("You have the latest version.");
                Shutdown();
            }*/

            JumpTask OpenTask = new JumpTask
            {
                Title = "Open",
                Arguments = "",
                //Description = "Open SLBr",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetExecutingAssembly().Location,
                IconResourcePath = Assembly.GetExecutingAssembly().Location
            };
            JumpTask PrivateOpenTask = new JumpTask
            {
                Title = "Open in private mode",
                Arguments = "Private",
                //Description = "No browsing history will be saved, in memory cache will be used (Incognito)",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetExecutingAssembly().Location,
                IconResourcePath = Assembly.GetExecutingAssembly().Location
            };
            JumpTask DeveloperOpenTask = new JumpTask
            {
                Title = "Open in developer mode",
                Arguments = "Developer",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetExecutingAssembly().Location,
                IconResourcePath = Assembly.GetExecutingAssembly().Location
            };
            JumpTask ChromiumOpenTask = new JumpTask
            {
                Title = "Open in chromium mode",
                Arguments = "Chromium",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetExecutingAssembly().Location,
                IconResourcePath = Assembly.GetExecutingAssembly().Location
            };
            JumpTask IEOpenTask = new JumpTask
            {
                Title = "Open in Internet Explorer mode",
                Arguments = "IE",
                //Description = "Access to developer features of SLBr and bypass the i5 processor check",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetExecutingAssembly().Location,
                IconResourcePath = Assembly.GetExecutingAssembly().Location
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
            /*bool IsNewInstance = false;
            SingleInstanceMutex = new Mutex(true, "SLBrSLTBrowser", out IsNewInstance);
            if (!IsNewInstance)
            {
                MessageBox.Show("Already an instance is running...");
                Current.Shutdown();
            }*/
        }
    }
}
