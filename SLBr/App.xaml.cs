using CefSharp.BrowserSubprocess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                SelfHost.Main(args);
                return;
            }
        }
    }
}
