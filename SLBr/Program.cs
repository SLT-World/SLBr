using CefSharp;
using CefSharp.BrowserSubprocess;
using CefSharp.Internals;
using System;
using System.Diagnostics;
using System.IO;
//using System.Diagnostics;
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

        [DllImport("user32", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public static void SendDataMessage(Process targetProcess, string msg)
        {
            //Copy the string message to a global memory area in unicode format
            IntPtr _stringMessageBuffer = Marshal.StringToHGlobalUni(msg);

            //Prepare copy data structure
            COPYDATASTRUCT _copyData = new COPYDATASTRUCT();
            _copyData.dwData = IntPtr.Zero;
            _copyData.lpData = _stringMessageBuffer;
            _copyData.cbData = msg.Length * 2;//Number of bytes required for marshalling this string as a series of unicode characters
            IntPtr _copyDataBuff = IntPtrAlloc(_copyData);

            //Send message to the other process
            SendMessage(targetProcess.MainWindowHandle, WM_COPYDATA, IntPtr.Zero, _copyDataBuff);

            Marshal.FreeHGlobal(_copyDataBuff);
            Marshal.FreeHGlobal(_stringMessageBuffer);
        }

        // Allocate a pointer to an arbitrary structure on the global heap.
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
        public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
        public int cbData;       // The count of bytes in the message.
        public IntPtr lpData;    // The address of the message.
    }

    internal static class Program
    {
        //[DllImport("user32.dll")]
        //static extern int SetWindowText(IntPtr hWnd, string text);
        [STAThread]
        private static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");
            Cef.EnableHighDPISupport();
            //MessageBox.Show(string.Join(",", args));
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
            {
                //Process.GetCurrentProcess().ProcessName;
                //gpu-process
                //utility
                //renderer
                //SetWindowText(Process.GetCurrentProcess().MainWindowHandle, args[0].Replace("--type=", ""));
                //MessageBox.Show(args[0]);
                //Process.Start("E:\\Visual Studio\\SLBr\\Utility Service\\bin\\Debug\\net6.0-windows\\Utility Service.exe", args);
                return SelfHost.Main(args);
            }
            else
            {
                App.Main();

                Cef.Shutdown();
                return Environment.ExitCode;
            }
        }
    }
}
