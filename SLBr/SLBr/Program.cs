using CefSharp.BrowserSubprocess;
using System;
using CefSharp.Wpf;
using CefSharp;

namespace SLBr
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // Determine if we need to run the CEFSharp browser subprocess.
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                return SelfHost.Main(args);
            }
            else
            {
                // Run the regular application.

                // Run the program with WPF GUI.
                App.Main();

                // Shutdown CEFSharp.
                Cef.Shutdown();
                return Environment.ExitCode;
            }
        }
    }
}
