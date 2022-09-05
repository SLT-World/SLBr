using CefSharp;
using CefSharp.BrowserSubprocess;
using System;

namespace SLBr
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");
            // Determine if we need to run the CEFSharp browser subprocess.
            Cef.EnableHighDPISupport();
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
