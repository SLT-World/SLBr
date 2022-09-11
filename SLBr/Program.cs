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
            Cef.EnableHighDPISupport();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
                return SelfHost.Main(args);
            else
            {
                App.Main();

                Cef.Shutdown();
                return Environment.ExitCode;
            }
        }
    }
}
