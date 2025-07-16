using Microsoft.Win32;
using System.Diagnostics;

namespace SLBr
{
    public static class StartupManager
    {
        private const string RegistryRunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void EnableStartup()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            key.SetValue("SLBr", $"\"{Process.GetCurrentProcess().MainModule.FileName}\" --background");
        }

        public static void DisableStartup()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            key.DeleteValue("SLBr", false);
        }

        /*public static bool IsStartupEnabled()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, false);
            return key?.GetValue("SLBr") != null;
        }*/
    }
}
