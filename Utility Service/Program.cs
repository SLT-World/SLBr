using CefSharp;
using CefSharp.BrowserSubprocess;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Utility_Service
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string text);

        [STAThread]
        private static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");
            Cef.EnableHighDPISupport();

            //var handle = GetConsoleWindow();
            //ShowWindow(handle, SW_HIDE);

            //MessageBox.Show(string.Join(",", args));
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                string WindowText = "Browser";
                string _Type = args[0].Replace("--type=", "");
                if (_Type == "gpu-process")
                    WindowText = "GPU Process";
                    //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "GPU Process");
                if (_Type == "renderer")
                    WindowText = "Renderer";
                    //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Renderer");
                else if (_Type == "utility")
                {
                    string _UtilitySubType = args[1].Replace("--utility-sub-type=", "");
                    if (_UtilitySubType.EndsWith("NetworkService"))
                        WindowText = "Utility: Network Service";
                    //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Utility: Network Service");
                    else if (_UtilitySubType.EndsWith("StorageService"))
                        WindowText = "Utility: Storage Service";
                    //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Utility: Storage Service");
                    else if (_UtilitySubType.EndsWith("AudioService"))
                        WindowText = "Utility: Audio Service";
                    //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Utility: Audio Service");
                }
                SetWindowText(Process.GetCurrentProcess().MainWindowHandle, WindowText);
                //var window = new Window()
                //{
                //    Width = 0,
                //    Height = 0,
                //    //AllowsTransparency = true,
                //    //Opacity = 0.0,
                //    WindowStyle = WindowStyle.None,
                //    ShowInTaskbar = false,
                //    //ShowActivated = false,
                //    //Visibility = Visibility.Hidden,
                //    Title = WindowText
                //};
                //window.Show();

                //--type
                //[utility]
                //--utility-sub-type
                //storage.mojom.StorageService
                //network.mojom.NetworkService
                //audio.mojom.AudioService

                //gpu-process
                //utility
                //renderer
                //MessageBox.Show(args[0]);
                return SelfHost.Main(args);
            }
            else
                return Environment.ExitCode;
        }
    }
}
