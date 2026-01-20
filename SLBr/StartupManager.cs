using Microsoft.Win32;
using System.Diagnostics;

namespace SLBr
{
    public static class StartupManager
    {
        private const string RegistryRunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void EnableStartup()
        {
            string KeyName = $"SLBr-{App.Instance.CurrentProfile}";
            string Arguments = $"--background --user={App.Instance.CurrentProfile}";
            using RegistryKey Key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            Key.SetValue(KeyName, $"\"{Process.GetCurrentProcess().MainModule.FileName}\" {Arguments}");
        }

        public static void DisableStartup()
        {
            string KeyName = $"SLBr-{App.Instance.CurrentProfile}";
            using RegistryKey Key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            Key.DeleteValue(KeyName, false);
        }

        /*public static bool IsStartupEnabled()
        {
            string KeyName = App.Instance.Username == "Default" ? "SLBr" : $"SLBr-{App.Instance.Username}";
            using RegistryKey Key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, false);
            return Key?.GetValue(KeyName) != null;
        }*/
    }
}
