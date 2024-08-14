using CefSharp;
using CefSharp.BrowserSubprocess;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SLBr
{
    static class MessageHelper
    {
        public const int WM_COPYDATA = 0x004A;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_SYSTEMMENU = 0xa4;
        public const int WP_SYSTEMMENU = 0x02;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const int HTMAXBUTTON = 9;
        public const int HWND_BROADCAST = 0xffff;

        [DllImport("user32", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public static void SendDataMessage(Process targetProcess, string msg)
        {
            IntPtr _stringMessageBuffer = Marshal.StringToHGlobalUni(msg);

            COPYDATASTRUCT _copyData = new COPYDATASTRUCT();
            _copyData.dwData = IntPtr.Zero;
            _copyData.lpData = _stringMessageBuffer;
            _copyData.cbData = msg.Length * 2;
            IntPtr _copyDataBuff = IntPtrAlloc(_copyData);

            SendMessage((IntPtr)HWND_BROADCAST, WM_COPYDATA, IntPtr.Zero, _copyDataBuff);

            Marshal.FreeHGlobal(_copyDataBuff);
            Marshal.FreeHGlobal(_stringMessageBuffer);
        }

        private static IntPtr IntPtrAlloc<T>(T param)
        {
            IntPtr retval = Marshal.AllocHGlobal(Marshal.SizeOf(param));
            Marshal.StructureToPtr(param, retval, false);
            return retval;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");
            MinimizeMemory();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                //MessageBox.Show(string.Join("|",args));
                return SelfHost.Main(args);
            }
            else
            {
                App.Main();

                Cef.Shutdown();
                return Environment.ExitCode;
            }
        }

        private static void MinimizeMemory()
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            //CurrentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            //SetCpuAffinity(0x0001);
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                SetProcessWorkingSetSize(CurrentProcess.Handle, -1, -1);
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        /*[DllImport("kernel32.dll")]
        private static extern bool SetProcessAffinityMask(IntPtr handle, IntPtr affinity);

        public static void SetCpuAffinity(int affinityMask)
        {
            Process process = Process.GetCurrentProcess();
            SetProcessAffinityMask(process.Handle, (IntPtr)affinityMask);
        }*/
    }
}
