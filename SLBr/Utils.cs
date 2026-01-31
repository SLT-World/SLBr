using CefSharp;
using CefSharp.Wpf.HwndHost;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinRT;
using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace SLBr
{
    static class DllUtils
    {
        /*[DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommand uCmd);

        public enum GetWindowCommand : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }*/

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, WindowDisplayAffinity affinity);

        public enum WindowDisplayAffinity : uint
        {
            WDA_NONE = 0x00000000,
            WDA_MONITOR = 0x00000001
            //WDA_EXCLUDEFROMCAPTURE = 0x00000011
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        public const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, int style, int x, int y, int width, int height, IntPtr hwndParent, IntPtr hMenu, IntPtr hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        public static extern bool DestroyWindow(IntPtr hwnd);
        
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeft, int nTop, int nRight, int nBottom);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public static string GetWindowTextRaw(IntPtr hWnd)
        {
            StringBuilder Builder = new(512);
            GetWindowText(hWnd, Builder, Builder.Capacity);
            return Builder.ToString();
        }

        public const int SW_SHOWNA = 8;
        public const int SW_HIDE = 0;
        //public const int SW_SHOWNORMAL = 1;
        //public const int SW_SHOWMINIMIZED = 2;
        //public const int SW_SHOWMAXIMIZED = 3;
        //public const int SW_SHOWNOACTIVATE = 4;
        //public const int SW_SHOW = 5;
        //public const int SW_MINIMIZE = 6;
        //public const int SW_SHOWDEFAULT = 10;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_CHILD = 0x40000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_SYSMENU = 0x00080000;

        //public const int WS_EX_LAYERED = 0x80000;
        //public const int WS_EX_NOACTIVATE = 0x08000000;
        //public const int WS_EX_DLGMODALFRAME = 0x00000001;
        //public const int WS_EX_CLIENTEDGE = 0x00000200;
        //public const int WS_EX_STATICEDGE = 0x00020000;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const int SWP_NOMOVE = 0x0002;

        public const int HOST_ID = 0x00000002;

        public const int WM_SIZE = 0x0005;
        public const int WM_SETFOCUS = 0x0007;
        public const int WM_MOUSEACTIVATE = 0x0021;
        //public const int WS_OVERLAPPED = 0x00000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;
        //public const int WS_DISABLED = 0x08000000;
        //public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        //public const int WS_BORDER = 0x00800000;
        //public const int WS_DLGFRAME = 0x00400000;
        //public const int WS_VSCROLL = 0x00200000;
        //public const int WS_HSCROLL = 0x00100000;
        //public const int WS_GROUP = 0x00020000;
        //public const int WS_TABSTOP = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;

        //public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

        //public const int WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU;

        /*public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_ACCEPTFILES = 0x00000010;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_MDICHILD = 0x00000040;*/
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        /*public const int WS_EX_WINDOWEDGE = 0x00000100;
        public const int WS_EX_CONTEXTHELP = 0x00000400;
        public const int WS_EX_RIGHT = 0x00001000;
        public const int WS_EX_LEFT = 0x00000000;
        public const int WS_EX_RTLREADING = 0x00002000;
        public const int WS_EX_LTRREADING = 0x00000000;
        public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
        public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;
        public const int WS_EX_CONTROLPARENT = 0x00010000;*/
        public const int WS_EX_APPWINDOW = 0x00040000;

        /*public const int WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
        public const int WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;*/

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;

        public const uint WM_CLOSE = 0x0010;

        public const int WM_COPYDATA = 0x004A;
        public const int HWND_BROADCAST = 0xffff;
    }
    static class MessageHelper
    {
        public static void SendDataMessage(Process targetProcess, string msg)
        {
            IntPtr StringMessageBuffer = Marshal.StringToHGlobalUni(msg);

            COPYDATASTRUCT CopyData = new()
            {
                dwData = IntPtr.Zero,
                lpData = StringMessageBuffer,
                cbData = msg.Length * 2
            };
            IntPtr CopyDataBuffer = IntPtrAlloc(CopyData);

            DllUtils.SendMessage(DllUtils.HWND_BROADCAST, DllUtils.WM_COPYDATA, IntPtr.Zero, CopyDataBuffer);

            Marshal.FreeHGlobal(CopyDataBuffer);
            Marshal.FreeHGlobal(StringMessageBuffer);
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

    [Flags]
    public enum DwmWindowAttribute : uint
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_MICA_EFFECT = 1029
    }

    public static class UserAgentGenerator //https://source.chromium.org/chromium/chromium/src/+/main:content/common/user_agent.cc
    {
        //public static string FrozenUserAgentTemplate = "Mozilla/5.0 ({0}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{1}.0.0.0 Safari/537.36";

        /*public static string GetUserAgentPlatform()
        {
            return "";
        }*/

        /*public static string GetUnifiedPlatform()
        {
            return "Windows NT 10.0; Win64; x64";
        }*/

        /*// Inaccurately named for historical reasons
        public static string GetWebKitVersion()
        {
            return string.Format("537.36 ({0})", CHROMIUM_GIT_REVISION);
        }

        public static string GetChromiumGitRevision()
        {
            return CHROMIUM_GIT_REVISION;
        }*/

        /*[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process2([In] IntPtr hProcess, [Out] ImageFileMachine pProcessMachine, [Out, Optional] ImageFileMachine pNativeMachine);*/

        /*[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern unsafe bool IsWow64Process2(IntPtr hProcess, ImageFileMachine* pProcessMachine, [Optional] ImageFileMachine* pNativeMachine);

        public static unsafe void IsWow64Process2(SafeHandle hProcess, out ImageFileMachine pProcessMachine, out ImageFileMachine pNativeMachine)
        {
            bool hProcessAddRef = false;
            try
            {
                fixed (ImageFileMachine* pProcessMachineLocal = &pProcessMachine)
                {
                    fixed (ImageFileMachine* pNativeMachineLocal = &pNativeMachine)
                    {
                        IntPtr hProcessLocal;
                        if (hProcess is object)
                        {
                            hProcess.DangerousAddRef(ref hProcessAddRef);
                            hProcessLocal = hProcess.DangerousGetHandle();
                            if (IsWow64Process2(hProcessLocal, pProcessMachineLocal, pNativeMachineLocal))
                                return;
                            else throw new Win32Exception();
                        }
                        else
                            throw new ArgumentNullException(nameof(hProcess));
                    }
                }
            }
            finally
            {
                if (hProcessAddRef)
                    hProcess.DangerousRelease();
            }
        }*/

        /*[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

        public static bool IsWow64()
        {
            //if (Environment.OSVersion.Version.Major >= 6 || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
            //{
            if (!IsWow64Process(Process.GetCurrentProcess().Handle, out bool RetVal))
                return false;
            return RetVal;
            //}
            //return false;
        }*/

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        public static bool IsIA64()
        {
            /*IsWow64Process2(Process.GetCurrentProcess().SafeHandle, out ImageFileMachine pProcessMachine, out ImageFileMachine pNativeMachine);
            //pProcessMachine.ToString() // IMAGE_FILE_MACHINE_UNKNOWN
            return pNativeMachine == ImageFileMachine.IA64;*/
            IsWow64Process2(Process.GetCurrentProcess().Handle, out ushort pProcessMachine, out ushort pNativeMachine);
            return pNativeMachine == 512;
        }

        //https://gist.github.com/BinToss/aa6a269f5eb58088425cdb5a2341e14e
        //http://zuga.net/articles/cs-is64bitprocess-vs-iswow64process/

        /*public enum ImageFileMachine : ushort
        {
            AXP64 = 644,
            I386 = 332,
            IA64 = 512,
            AMD64 = 34404,
            UNKNOWN = 0,
            TARGET_HOST = 1,
            R3000 = 354,
            R4000 = 358,
            R10000 = 360,
            WCEMIPSV2 = 361,
            ALPHA = 388,
            SH3 = 418,
            SH3DSP = 419,
            SH3E = 420,
            SH4 = 422,
            SH5 = 424,
            ARM = 448,
            THUMB = 450,
            ARMNT = 452,
            AM33 = 467,
            POWERPC = 496,
            POWERPCFP = 497,
            MIPS16 = 614,
            ALPHA64 = 644,
            MIPSFPU = 870,
            MIPSFPU16 = 1126,
            TRICORE = 1312,
            CEF = 3311,
            EBC = 3772,
            M32R = 36929,
            ARM64 = 43620,
            CEE = 49390,
        }*/

        /*private static bool? PIsWindows11OrGreater;

        internal static bool IsWindows11OrGreater
        {
            get
            {
                if (PIsWindows11OrGreater.HasValue)
                    return PIsWindows11OrGreater.Value;
                PIsWindows11OrGreater = Environment.OSVersion.Version >= new Version(10, 0, 22000);
                return PIsWindows11OrGreater.Value;
            }
        }*/

        public static string BuildCPUInfo()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                if (!Environment.Is64BitProcess) //IsWow64()
                    return "WOW64";
                else
                {
                    if (IsIA64())
                        return "Win64; IA64";
                    else
                        return "Win64; x64";
                }

                /*if (Environment.Is64BitProcess)
                    return "Win64; x64";
                else if (IsIA64())
                    return "Win64; IA64";
                else if (IsWow64())
                    return "WOW64";*/
            }
            return string.Empty;
        }

        public static string GetCPUArchitecture()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm:
                case Architecture.Arm64:
                    return "arm";
                default:
                    return "x86";
            }
        }

        public static string GetPlatformVersion()//https://textslashplain.com/2021/09/21/determining-os-platform-version/
        {
            string[] Parts = Environment.OSVersion.Version.ToString().Split('.');
            return Parts[0] + "." + Parts[1] + "." + Parts[2];
        }

        public static string GetOSVersion()
        {
            string[] Parts = Environment.OSVersion.Version.ToString().Split('.');
            return Parts[0] + "." + Parts[1];
        }

        public static string BuildChromeBrand() =>
            $"Chrome/{Cef.ChromiumVersion.Split('.')[0]}.0.0.0";

        public static string BuildOSCpuInfo() =>
            BuildOSCpuInfoFromOSVersionAndCpuType(GetOSVersion(), BuildCPUInfo());

        public static string BuildOSCpuInfoFromOSVersionAndCpuType(string OSVersion, string CPUType)
        {
            if (CPUType.Length == 0)
                return string.Format("Windows NT {0}", OSVersion);
            else
                return string.Format("Windows NT {0}; {1}", OSVersion, CPUType);
        }

        /*public static string GetReducedUserAgent(string major_version)
        {
            return string.Format(FrozenUserAgentTemplate, GetUnifiedPlatform(), major_version);
        }

        public static string BuildUnifiedPlatformUserAgentFromProduct(string product)
        {
            return BuildUserAgentFromOSAndProduct(GetUnifiedPlatform(), product);
        }*/

        public static string BuildUserAgentFromProduct(string Product) =>
            BuildUserAgentFromOSAndProduct(/*GetUserAgentPlatform()+*/BuildOSCpuInfo(), Product);

        public static string BuildMobileUserAgentFromProduct(string Product) =>
            BuildUserAgentFromOSAndProduct("Linux; Android 10; K", Product + " Mobile");

        public static string BuildUserAgentFromOSAndProduct(string OSInfo, string Product) =>
            $"Mozilla/5.0 ({OSInfo}) AppleWebKit/537.36 (KHTML, like Gecko) {Product} Safari/537.36";
            /* Derived from Safari's UA string.
             * This is done to expose our product name in a manner that is maximally compatible with Safari, we hope!!*/
    }

    public static class ClassExtensions
    {
        public static MColor ToMediaColor(this DColor color) =>
            MColor.FromArgb(color.A, color.R, color.G, color.B);
        public static bool ToBool(this bool? self) =>
            self == true;
        public static int ToInt(this bool self) =>
            self == true ? 1 : 0;
        public static string Cut(this string Self, int MaxLength, bool AddEllipsis = false)
        {
            if (Self.Length <= MaxLength)
                return Self;
            return string.Concat(Self.AsSpan(0, MaxLength - (AddEllipsis ? 3 : 0)), AddEllipsis ? "..." : string.Empty);
        }
        /*public static uint ToUInt(this System.Drawing.Color color) =>
               (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));*/

        public static void AddNoErrorFlag(this CefSettings Settings, string Key, string Value)
        {
            try { Settings.CefCommandLineArgs.Add(Key, Value); } catch { }
        }

        public static void AddNoErrorFlag(this CefSettings Settings, string Value)
        {
            try { Settings.CefCommandLineArgs.Add(Value); } catch { }
        }
    }

    public static partial class Utils
    {
        public static Brush GetContrastBrush(Color BackgroundColor) =>
            (0.299 * BackgroundColor.R + 0.587 * BackgroundColor.G + 0.114 * BackgroundColor.B) / 255 > 0.6 ? Brushes.Black : Brushes.White;
        public static void OpenFileExplorer(string Url) =>
            Process.Start(new ProcessStartInfo { Arguments = $"/select, \"{Url}\"", FileName = "explorer.exe" });

        public static async void DownloadAndCopyImage(string ImageUrl)
        {
            try
            {
                using (HttpClient _HttpClient = new())
                {
                    byte[] ImageData = await _HttpClient.GetByteArrayAsync(ImageUrl);
                    if (ImageData != null)
                    {
                        using (MemoryStream stream = new(ImageData))
                        {
                            BitmapImage Bitmap = new();
                            Bitmap.BeginInit();
                            Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            Bitmap.StreamSource = stream;
                            Bitmap.EndInit();
                            if (Bitmap.CanFreeze)
                                Bitmap.Freeze();

                            Clipboard.SetImage(Bitmap);
                        }
                    }
                }
            }
            catch { }
        }

        public static bool IsEmptyOrWhiteSpace([NotNullWhen(false)] string? Value)
        {
            if (Value == null || Value.Length == 0)
                return true;
            for (int i = 0; i < Value.Length; i++)
                if (!char.IsWhiteSpace(Value[i])) return false;
            return true;
        }

        public static void RaiseUIAsync(this EventHandler Handler, object? Sender)
        {
            Application.Current?.Dispatcher.BeginInvoke(() => Handler?.Invoke(Sender, null));
        }
        public static void RaiseUIAsync<T>(this EventHandler<T> Handler, object? Sender, T Args)
        {
            Application.Current?.Dispatcher.BeginInvoke(() => Handler?.Invoke(Sender, Args));
        }
        public static void RaiseUIAsync<T>(this Action<T> Handler, T Args)
        {
            Application.Current?.Dispatcher.BeginInvoke(() => Handler?.Invoke(Args));
        }

        public static string SanitizeFileName(string Name)
        {
            foreach (char Char in Path.GetInvalidFileNameChars())
                Name = Name.Replace(Char, '_');
            return Name;
        }

        public static string ResolveUrl(string BaseUrl, string RelativeAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(RelativeAbsolutePath)) return BaseUrl;
            if (Uri.TryCreate(RelativeAbsolutePath, UriKind.Absolute, out var _Absolute)) return _Absolute.ToString();
            return new Uri(new Uri(BaseUrl), RelativeAbsolutePath).ToString();
        }

        public static string CapitalizeAllFirstCharacters(string Input)
        {
            if (string.IsNullOrEmpty(Input))
                return Input;
            string[] Words = Input.Split(' ');
            for (int i = 0; i < Words.Length; i++)
            {
                if (!string.IsNullOrEmpty(Words[i]))
                    Words[i] = char.ToUpper(Words[i][0]) + Words[i].Substring(1);
            }
            return string.Join(" ", Words);
        }

        /*public static string? ParseAMPLink(string HTML, string BaseUrl)
        {
            if (string.IsNullOrEmpty(HTML) || string.IsNullOrEmpty(BaseUrl))
                return null;
            Match _Match = App.AMPRegex.Match(HTML);
            if (!_Match.Success)
                return null;
            string Href = _Match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(Href))
                return null;
            if (IsHttpScheme(Href))
                return Href;
            int SlashIndex = BaseUrl.IndexOf("/", 8);
            string Origin = SlashIndex > 0 ? BaseUrl.Substring(0, SlashIndex) : BaseUrl;
            if (Href.StartsWith("/"))
                return Origin + Href;
            else
            {
                int LastSlash = BaseUrl.LastIndexOf("/");
                if (LastSlash >= 0)
                    return BaseUrl.Substring(0, LastSlash + 1) + Href;
                else
                    return BaseUrl + "/" + Href;
            }
        }*/

        public static string? GetAMPUrl(string Url)
        {
            using (HttpClient Client = new())
            {
                string Payload = $"{{\"urls\":\"{Url}\"}}";
                try
                {
                    Client.DefaultRequestHeaders.Add("X-Goog-Api-Key", SECRETS.AMP_API_KEY);
                    var Response = Client.PostAsync(App.AMPEndpoint, new StringContent(Payload, Encoding.Default, "application/json")).Result;
                    if (!Response.IsSuccessStatusCode) return null;
                    var Json = Response.Content.ReadFromJsonAsync<JsonElement>().Result;
                    if (!Json.TryGetProperty("ampUrls", out var AMPUrls) || AMPUrls.GetArrayLength() == 0)
                        return null;
                    return AMPUrls[0].GetProperty("ampUrl").GetString();
                }
                catch { }
                return null;
            }

            //using HttpClient Client = new HttpClient();
            /*Client.DefaultRequestHeaders.Add("X-Goog-Api-Key", SECRETS.AMPs[App.MiniRandom.Next(SECRETS.AMPs.Count)]);
            var Response = await Client.PostAsJsonAsync(App.AMPEndpoint, new { urls = new[] { Url } });
            if (!Response.IsSuccessStatusCode) return null;

            var Json = Response.Content.ReadFromJsonAsync<JsonElement>().Result;
            if (!Json.TryGetProperty("ampUrls", out var AMPUrls) || AMPUrls.GetArrayLength() == 0)
                return null;*/
            //cdnAmpUrl
            //return AMPUrls[0].GetProperty("ampUrl").GetString();
        }

        /*public static async Task RunSafeAsync(Func<Task> TaskFunction)
        {
            try
            {
                await TaskFunction().ConfigureAwait(false);
            }
            catch { }
        }*/

        /*public static void RunSafeFireAndForget(Func<Task> TaskFunction)
        {
            Task.Run(() => RunSafeAsync(TaskFunction));
        }*/

        public static MColor ParseThemeColor(string ColorString)
        {
            if (ColorString.StartsWith("rgb"))
            {
                var Numbers = Regex.Matches(ColorString, @"\d+").Cast<Match>().Select(m => byte.Parse(m.Value)).ToArray();
                return MColor.FromRgb(Numbers[0], Numbers[1], Numbers[2]);
            }
            else
                return System.Drawing.ColorTranslator.FromHtml(ColorString).ToMediaColor();
        }

        public static void ColorToHSV(MColor _Color, out double Hue, out double Saturation, out double Value)
        {
            double R = _Color.R / 255.0;
            double G = _Color.G / 255.0;
            double B = _Color.B / 255.0;

            double Maximum = Math.Max(R, Math.Max(G, B));
            double Minimum = Math.Min(R, Math.Min(G, B));
            double Delta = Maximum - Minimum;

            Hue = 0;
            if (Delta != 0)
            {
                if (Maximum == R)
                    Hue = 60 * (((G - B) / Delta) % 6);
                else if (Maximum == G)
                    Hue = 60 * (((B - R) / Delta) + 2);
                else
                    Hue = 60 * (((R - G) / Delta) + 4);
            }
            if (Hue < 0) Hue += 360;

            Saturation = (Maximum == 0) ? 0 : Delta / Maximum;
            Value = Maximum;
        }

        public static MColor ColorFromHSV(double Hue, double Saturation, double Value)
        {
            int Hi = Convert.ToInt32(Math.Floor(Hue / 60)) % 6;
            double F = Hue / 60 - Math.Floor(Hue / 60);

            Value *= 255;
            byte V = (byte)Value;
            byte P = (byte)(Value * (1 - Saturation));
            byte Q = (byte)(Value * (1 - F * Saturation));
            byte T = (byte)(Value * (1 - (1 - F) * Saturation));

            return Hi switch
            {
                0 => MColor.FromRgb(V, T, P),
                1 => MColor.FromRgb(Q, V, P),
                2 => MColor.FromRgb(P, V, T),
                3 => MColor.FromRgb(P, Q, V),
                4 => MColor.FromRgb(T, P, V),
                _ => MColor.FromRgb(V, P, Q),
            };
        }

        public static string ColorToHex(MColor _Color) =>
            $"#{_Color.R:X2}{_Color.G:X2}{_Color.B:X2}";

        public static MColor HexToColor(string Hex)
        {
            try
            {
                return (MColor)ColorConverter.ConvertFromString(Hex);
            }
            catch
            {
                return Colors.Black;
            }
        }

        public static double GetHue(MColor _Color) =>
            DColor.FromArgb(_Color.R, _Color.G, _Color.B).GetHue();

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable() =>
            InternetGetConnectedState(out int Description, 0);

        public static void SaveImage(BitmapSource Bitmap, string FilePath)
        {
            using (FileStream _FileStream = new(FilePath, FileMode.Create))
            {
                PngBitmapEncoder PNGEncoder = new();
                PNGEncoder.Frames.Add(BitmapFrame.Create(Bitmap));
                PNGEncoder.Save(_FileStream);
            }
        }

        public static BitmapImage ConvertBase64ToBitmapImage(string Base64)
        {
            int Base64Start = Base64.IndexOf("base64,");
            if (Base64.StartsWith("data:image/") && Base64Start != -1)
                Base64 = Base64.Substring(Base64Start + 7);
            using (MemoryStream _Stream = new(Convert.FromBase64String(Base64)))
            {
                BitmapImage _Bitmap = new();
                _Bitmap.BeginInit();
                _Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                _Bitmap.StreamSource = _Stream;
                _Bitmap.EndInit();
                if (_Bitmap.CanFreeze)
                    _Bitmap.Freeze();
                return _Bitmap;
            }
        }

        public static Process GetAlreadyRunningInstance(Process CurrentProcess)
        {
            Process[] AllProcesses = Process.GetProcessesByName(CurrentProcess.ProcessName);
            for (int i = 0; i < AllProcesses.Length; i++)
            {
                if (AllProcesses[i].Id != CurrentProcess.Id)
                    return AllProcesses[i];
            }
            return null;
        }

        public static int GenerateRandomId() =>
            App.MiniRandom.Next();

        public enum FolderGuids
        {
            Downloads,
            Pictures
        }
        private static Guid DownloadsGuid = new("374DE290-123F-4565-9164-39C4925E467B");
        private static Guid PicturesGuid = new("33E28130-4E1E-4676-835A-98395C3BC3BB");
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);
        public static string GetFolderPath(FolderGuids FolderGuid)
        {
            IntPtr PathPtr = IntPtr.Zero;
            try
            {
                Guid _FolderGuid = new();
                switch (FolderGuid)
                {
                    case FolderGuids.Downloads:
                        _FolderGuid = DownloadsGuid;
                        break;
                    case FolderGuids.Pictures:
                        _FolderGuid = PicturesGuid;
                        break;
                }
                SHGetKnownFolderPath(ref _FolderGuid, 0, IntPtr.Zero, out PathPtr);
                return Marshal.PtrToStringUni(PathPtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(PathPtr);
            }
        }

        static Guid DTM_IID = new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        [GeneratedComInterface]
        [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal partial interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow(IntPtr appWindow, ref Guid riid);

            void ShowShareUIForWindow(IntPtr appWindow);
        }

        public static void Share(nint HWND, string Title, Uri Url)
        {
            IDataTransferManagerInterop DataTransferManagerFactory = Windows.ApplicationModel.DataTransfer.DataTransferManager.As<IDataTransferManagerInterop>();
            var _DataTransferManager = MarshalInterface<Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(DataTransferManagerFactory.GetForWindow(HWND, ref DTM_IID));
            _DataTransferManager.DataRequested += (sender, args) =>
            {
                args.Request.Data.Properties.Title = Title;
                args.Request.Data.SetUri(Url);
                args.Request.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
            };
            DataTransferManagerFactory.ShowShareUIForWindow(HWND);
        }

        //public static bool IsAdministrator() =>
        //    new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static (string, string) ParseCertificateIssue(string Certificate)
        {
            ReadOnlySpan<char> Span = Certificate.AsSpan().Trim();
            string CN = string.Empty;
            string O = string.Empty;
            while (!Span.IsEmpty)
            {
                int Comma = Span.IndexOf(",");
                ReadOnlySpan<char> Part = Comma >= 0 ? Span[..Comma] : Span;
                int Equal = Part.IndexOf("=");
                if (Equal > 0)
                {
                    ReadOnlySpan<char> Key = Part[..Equal].Trim();
                    ReadOnlySpan<char> Value = Part[(Equal + 1)..].Trim();
                    if (Key.Equals("CN", StringComparison.Ordinal))
                        CN = Value.ToString();
                    else if (Key.Equals("O", StringComparison.Ordinal))
                        O = Value.ToString();
                }
                Span = Comma >= 0 ? Span[(Comma + 1)..].TrimStart() : default;
            }
            return (CN, O);
        }

        public static string GetScheme(string Url)
        {
            int SchemeSeparatorIndex = Url.IndexOf(':');
            if (SchemeSeparatorIndex != -1)
                return Url.Substring(0, SchemeSeparatorIndex);
            return string.Empty;
        }

        public static string GetFileExtension(string Url)
        {
            ReadOnlySpan<char> Span = Url.AsSpan();
            int Query = Span.IndexOf("?");
            if (Query >= 0)
                Span = Span[..Query];

            int Hash = Span.IndexOf("#");
            if (Hash >= 0)
                Span = Span[..Hash];

            int Slash = Span.LastIndexOf("/");
            if (Slash >= 0)
                Span = Span[(Slash + 1)..];

            int Dot = Span.LastIndexOf(".");
            return Dot >= 0 ? Span[Dot..].ToString() : string.Empty;
        }
        
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:") || Url.StartsWith("mailto:") || Url.StartsWith("news:") || Url.StartsWith("feed:");
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
             _ResourceType is ResourceType.Xhr or ResourceType.Media or ResourceType.Script or ResourceType.SubFrame or ResourceType.Image;
        public static bool IsPossiblyAd(ResourceRequestType _ResourceType) =>
             _ResourceType is ResourceRequestType.XMLHTTPRequest or ResourceRequestType.Media or ResourceRequestType.Script or ResourceRequestType.SubFrame or ResourceRequestType.Image;
        public static bool IsHttpScheme(string Url) =>
            Url.StartsWith("https:") || Url.StartsWith("http:");
        
        public static bool IsDomain(string Url)
        {
            try
            {
                string Host = new IdnMapping().GetAscii(FastHost(Url));
                int LastDot = Host.LastIndexOf('.');
                if (LastDot <= 0 || LastDot == Host.Length - 1)
                    return false;
                string SLD = Host[..LastDot];
                foreach (char _Char in SLD)
                    //INFO: Underscores are allowed in Chromium "a_b.com"
                    if (!char.IsLetterOrDigit(_Char) && _Char != '_' && _Char != '-' && _Char != '.')
                        return false;
                string TLD = Host[(LastDot + 1)..];
                if (IsAlphabeticalTLD(TLD))
                    return true;
                if (IsPunycodeTLD(TLD))
                    return true;
            }
            catch { }
            return false;
        }
        public static bool IsAlphabeticalTLD(string TLD)
        {
            foreach (char _Char in TLD)
                if (!char.IsLetter(_Char))
                    return false;
            return TLD.Length >= 2;
        }
        //https://xn--j1ay.xn--p1ai/
        public static bool IsPunycodeTLD(string TLD)
        {
            if (!TLD.StartsWith("xn--"))
                return false;
            if (TLD.Length <= 4)
                return false;
            for (int i = 4; i < TLD.Length; i++)
            {
                char _Char = TLD[i];
                if (!(char.IsLetterOrDigit(_Char) || _Char == '-'))
                    return false;
            }
            return true;
        }
        public static bool IsProtocolNotHttp(string Url) =>
            !IsHttpScheme(Url) && IsProtocol(Url);
        public static bool IsProtocol(string Url)
        {
            int Colon = Url.IndexOf(':');
            if (Colon < 1)
                return false;
            int Dot = Url.IndexOf('.');
            return Dot < 0 || Colon < Dot;
        }
        //TODO: Validate domain TLDs from https://data.iana.org/TLD/tlds-alpha-by-domain.txt
        public static bool IsUrl(string Url)
        {
            if (IsCode(Url))
                return false;
            /*if (Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))// && !Uri.IsWellFormedUriString(Uri.EscapeDataString(Url), UriKind.RelativeOrAbsolute))
                return true;*/

            ReadOnlySpan<char> Scheme = string.Empty;

            Url = CleanUrl(Url, true, true, true, false, false);

            ReadOnlySpan<char> _Span = Url.AsSpan();
            int Protocol = _Span.IndexOf(":///");
            if (Protocol >= 0)
            {
                Scheme = _Span[..Protocol];
                _Span = _Span[(Protocol + 4)..];
            }
            else
            {
                Protocol = _Span.IndexOf("://");
                if (Protocol >= 0)
                {
                    Scheme = _Span[..Protocol];
                    _Span = _Span[(Protocol + 3)..];
                }
                else
                {
                    Protocol = _Span.IndexOf(":");
                    if (Protocol >= 0)
                    {
                        Scheme = _Span[..Protocol];
                        _Span = _Span[(Protocol + 1)..];
                    }
                }
            }

            int HostEnd = _Span.IndexOfAny('/', '?', '#');
            ReadOnlySpan<char> Host = HostEnd >= 0 ? _Span[..HostEnd] : _Span;

            if (Protocol == -1 || Scheme is "http" or "https")
                return IsDomain(Host.ToString());
            else
                return true;

            //TODO: Validate path?
            /*if (HostEnd >= 0)
            {
                ReadOnlySpan<char> _Path = _Span[HostEnd..];
            }*/
        }
        public static bool IsCode(string Url) =>
            Url.StartsWith("javascript:") || Url.StartsWith("view-source:") || Url.StartsWith("localhost:") || Url.StartsWith("data:") || Url.StartsWith("blob:");
        public static bool IsCustomScheme(string Url) =>
            !IsHttpScheme(Url) && !IsCode(Url) && IsProtocol(Url);
        public static bool IsProprietaryCodec(string Extension) =>
            Extension is ".mp4" or ".m4a" or ".aac" or ".m4v" or ".mov" or ".mp3" or ".wma" or ".wmv";


        public static string EscapeDataString(string Input)
        {
            StringBuilder _StringBuilder = new(Input.Length + 8);
            foreach (char Character in Input)
            {
                if ((Character >= 'A' && Character <= 'Z') || (Character >= 'a' && Character <= 'z') || (Character >= '0' && Character <= '9') || Character == '-' || Character == '_' || Character == '.' || Character == '~')
                    _StringBuilder.Append(Character);
                else
                    _StringBuilder.Append('%').Append(((int)Character).ToString("X2"));
            }
            return _StringBuilder.ToString();
        }

        public static string UnescapeDataString(string Input)
        {
            Span<char> Buffer = stackalloc char[Input.Length];
            int j = 0;
            for (int i = 0; i < Input.Length;)
            {
                if (Input[i] == '%' && i + 2 < Input.Length && byte.TryParse(Input.AsSpan(i + 1, 2), NumberStyles.HexNumber, null, out byte b))
                {
                    Buffer[j++] = (char)b;
                    i += 3;
                }
                else
                    Buffer[j++] = Input[i++];
            }
            return new string(Buffer[..j]);
        }

        public static string GenerateSID()
        {
            string TimePart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("x");
            byte[] RandomBytes = new byte[8];
            RandomNumberGenerator.Fill(RandomBytes);
            string RandomPart = BitConverter.ToString(RandomBytes).Replace("-", "").ToLower();
            return (TimePart + RandomPart).Substring(0, 16);
        }

        public static string GetProtocolAppName(string Protocol)
        {
            Protocol = Protocol.ToLowerInvariant();
            string? Name = GetRegistryProtocol(Registry.CurrentUser, @"Software\Classes\" + Protocol);
            if (Name != null)
                return Name;
            Name = GetRegistryProtocol(Registry.ClassesRoot, Protocol);
            if (Name != null)
                return Name;
            return Protocol;
        }


        public static string GetRegistryProtocol(RegistryKey Root, string SubKeyPath)
        {
            try
            {
                using RegistryKey? Key = Root.OpenSubKey(SubKeyPath);
                if (Key == null)
                    return null;
                using RegistryKey? CommandKey = Key.OpenSubKey(@"shell\open\command");
                string Command = (string)CommandKey?.GetValue(null);
                if (!string.IsNullOrWhiteSpace(Command))
                {
                    string _ExecutablePath = Command.Split('"')[Command.StartsWith('"') ? 1 : 0];
                    if (File.Exists(_ExecutablePath))
                    {
                        FileVersionInfo Info = FileVersionInfo.GetVersionInfo(_ExecutablePath);
                        return Info.ProductName ?? Info.FileDescription ?? Path.GetFileNameWithoutExtension(_ExecutablePath);
                    }
                }
            }
            catch { }
            return null;
        }
        public static string GetProtocolName(string Protocol)
        {
            return Protocol switch
            {
                "ms-settings" => "Windows Settings",
                "ms-photos" => "Microsoft Photos",
                "ms-store" => "Microsoft Store",
                "mailto" => "Mail",
                "tel" => "Phone",
                _ => GetProtocolAppName(Protocol)
            };
        }

        public static string RemovePrefix(string Input, string Prefix, bool CaseSensitive = false, bool FromEnd = false)
        {
            ReadOnlySpan<char> InputSpan = Input.AsSpan();
            ReadOnlySpan<char> PrefixSpan = Prefix.AsSpan();

            if (FromEnd)
            {
                if (InputSpan.Length < PrefixSpan.Length) return Input;
                var Tail = InputSpan[^PrefixSpan.Length..];
                bool Match = CaseSensitive ? Tail.SequenceEqual(PrefixSpan) : Tail.Equals(PrefixSpan, StringComparison.OrdinalIgnoreCase);
                return Match ? InputSpan[..^PrefixSpan.Length].ToString() : Input;
            }
            else
            {
                if (InputSpan.Length < PrefixSpan.Length) return Input;
                var Head = InputSpan[..PrefixSpan.Length];
                bool Match = CaseSensitive ? Head.SequenceEqual(PrefixSpan) : Head.Equals(PrefixSpan, StringComparison.OrdinalIgnoreCase);
                return Match ? InputSpan[PrefixSpan.Length..].ToString() : Input;
            }
        }
        public static string FilterUrlForBrowser(string Url, string SearchEngineUrl)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return Url;

            Url = Url.Trim();

            if (!Url.StartsWith("domain:") && !Url.StartsWith("search:"))
            {
                if (IsProgramUrl(Url))
                {
                    try { Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true }); }
                    catch { }
                    return Url;
                }
                if (IsCode(Url))
                    return Url;
                if (IsUrl(Url))
                    return FixUrl(Url);
                Url = "search:" + Url;
            }
            ReadOnlySpan<char> Span = Url.AsSpan();
            if (Span.StartsWith("search:"))
            {
                ReadOnlySpan<char> Query = Span[7..];
                string Encoded = EscapeDataString(Query.ToString());
                return string.IsNullOrEmpty(SearchEngineUrl) ? FixUrl(Encoded) : FixUrl(string.Format(SearchEngineUrl, Encoded));
            }
            if (Span.StartsWith("domain:"))
                return FixUrl(Span[7..].ToString());
            return Url;
        }
        public static string FastHost(string Url, bool RemoveTrivialSubdomain = true)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            ReadOnlySpan<char> Span = Url.AsSpan();

            int Protocol = Span.IndexOf("://");
            if (Protocol >= 0)
                Span = Span[(Protocol + 3)..];

            if (RemoveTrivialSubdomain)
            {
                if (Span.StartsWith("www.") && CanRemoveTrivialSubdomain(Span[4..]))
                    Span = Span[4..];
                else if (Span.StartsWith("m.") && CanRemoveTrivialSubdomain(Span[2..]))
                    Span = Span[2..];
            }

            int Separator = Span.IndexOfAny('/', '?', '#');
            if (Separator >= 0)
                Span = Span[..Separator];

            return Span.ToString();
        }
        public static string Host(string Url, bool RemoveTrivialSubdomain = true)
        {
            string Host = CleanUrl(Url, true, false, true, RemoveTrivialSubdomain);
            if (IsHttpScheme(Url) || Url.StartsWith("file:///"))
            {
                int SlashIndex = Host.IndexOf('/');
                return SlashIndex >= 0 ? Host.Substring(0, SlashIndex) : Host;
            }
            return Host;
        }
        public static string HostOnlyHTTP(string Url, bool RemoveTrivialSubdomain = true)
        {
            string Host = CleanUrl(Url, true, true, true, RemoveTrivialSubdomain, false);
            if (IsHttpScheme(Url))
            {
                int Protocol = Host.IndexOf("://");
                if (Protocol >= 0)
                    Host = Host[(Protocol + 3)..];
                int SlashIndex = Host.IndexOf('/');
                return SlashIndex >= 0 ? Host.Substring(0, SlashIndex) : Host;
            }
            return Host;
        }
        //TODO: Switch to public suffix list detection
        public static bool CanRemoveTrivialSubdomain(ReadOnlySpan<char> Host)
        {
            int Dots = 0;
            foreach (char _Char in Host)
                if (_Char == '.') Dots++;
            return Dots >= 1;//Switch to 2 to support "www.co.uk"
        }
        public static bool CanRemoveTrivialSubdomain(string Host)
        {
            int Dots = 0;
            foreach (char _Char in Host)
                if (_Char == '.') Dots++;
            return Dots >= 1;
        }
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveFragment = true, bool RemoveTrivialSubdomain = false, bool RemoveProtocol = true)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return Url;
            ReadOnlySpan<char> Span = Url.AsSpan().Trim();
            if (RemoveParameters)
            {
                int ToRemoveIndex = Span.LastIndexOf("?");
                if (ToRemoveIndex >= 0)
                    Span = Span[..ToRemoveIndex];
            }
            if (RemoveFragment)
            {
                int ToRemoveIndex = Span.LastIndexOf("#");
                if (ToRemoveIndex >= 0)
                    Span = Span[..ToRemoveIndex];
            }

            Url = Span.ToString();

            if (RemoveProtocol)
            {
                Url = RemovePrefix(Url, "http://");
                Url = RemovePrefix(Url, "https://");
                Url = RemovePrefix(Url, "file:///");
            }
            if (RemoveLastSlash && Url.Length > 0 && Url[^1] == '/')
                Url = Url[..^1];

            if (RemoveTrivialSubdomain)
            {
                if (Url.StartsWith("www.") && CanRemoveTrivialSubdomain(Url[4..]))
                    Url = Url[4..];
                else if (Url.StartsWith("m.") && CanRemoveTrivialSubdomain(Url[2..]))
                    Url = Url[2..];
            }
            return Url;
        }
        public static string FixUrl(string Url)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return Url;
            return IsProtocol(Url) ? Url : "https://" + Url;
        }

        private static readonly Dictionary<char, char> DeceptiveCharMap = new()
        {
            //Cyrillic
            ['а'] = 'a',
            ['А'] = 'A',
            ['В'] = 'B',
            ['е'] = 'e',
            ['Е'] = 'E',
            ['к'] = 'k',
            ['К'] = 'K',
            ['М'] = 'M',
            ['Н'] = 'H',
            ['о'] = 'o',
            ['О'] = 'O',
            ['р'] = 'p',
            ['Р'] = 'P',
            ['с'] = 'c',
            ['С'] = 'C',
            ['Т'] = 'T',
            ['у'] = 'y',
            ['У'] = 'Y',
            ['х'] = 'x',
            ['Х'] = 'X',
            ['ӏ'] = 'l',

            ['в'] = 'b',
            ['д'] = 'n',
            ['л'] = 'n',
            ['п'] = 'n',
            ['ѕ'] = 's',
            ['ѵ'] = 'v',
            ['і'] = 'i',
            ['ј'] = 'j',
            ['ѡ'] = 'w',

            //Greek
            ['Α'] = 'A',
            ['β'] = 'B',
            ['Β'] = 'B',
            ['Ε'] = 'E',
            ['Ζ'] = 'Z',
            ['Η'] = 'H',
            ['Ι'] = 'I',
            ['Κ'] = 'K',
            ['Μ'] = 'M',
            ['Ν'] = 'N',
            ['Ο'] = 'O',
            ['Ρ'] = 'P',
            ['Τ'] = 'T',
            ['Υ'] = 'Y',
            ['Χ'] = 'X',

            ['α'] = 'a',
            ['ε'] = 'e',
            ['η'] = 'n',
            ['ι'] = 'i',
            ['κ'] = 'k',
            ['μ'] = 'u',
            ['ν'] = 'v',
            ['ο'] = 'o',
            ['ρ'] = 'p',
            ['τ'] = 't',
            ['υ'] = 'u',
            ['χ'] = 'x',
            ['γ'] = 'y',
            ['δ'] = 'd',
            ['λ'] = 'l',
            ['ξ'] = 'x',
            ['σ'] = 'o',
            ['ς'] = 'o',
            ['ω'] = 'w',
            ['ϲ'] = 'c',

            ['ı'] = 'i',
            ['ł'] = 'l',
            ['đ'] = 'd',
            ['ħ'] = 'h',
            ['ŧ'] = 't',
            ['Ɩ'] = 'l',
            ['Ɨ'] = 'I',
            ['Ɵ'] = 'O',

            ['ℓ'] = 'l',
            ['℮'] = 'e',
            ['ℴ'] = 'o',
            ['K'] = 'K',
            ['Å'] = 'A',
        };

        /*TODO: Investigate usage of https://www.unicode.org/Public/security/8.0.0/confusables.txt
         * https://www.unicode.org/reports/tr36/confusables.txt
         * https://github.com/wanderingstan/Confusables
         * https://medium.com/grindr-engineering/confusable-character-detection-in-erlang-98aa47abc9ab
         * https://www.unicode.org/reports/tr39/
         */
        public static string BuildTextSkeleton(string Text)
        {
            Text = Text.Normalize(NormalizationForm.FormD);
            StringBuilder Builder = new(Text.Length);
            foreach (char _Char in Text)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(_Char) == UnicodeCategory.NonSpacingMark)
                    continue;
                if (_Char <= 127)
                    Builder.Append(_Char);
                else if (DeceptiveCharMap.TryGetValue(_Char, out char Mapped))
                    Builder.Append(Mapped);
                else
                    Builder.Append(_Char);
            }
            return Builder.ToString();
        }
    }

    public class Saving
    {
        const string KeySeparator = "<,>";
        const string ValueSeparator = "<|>";
        const string KeyValueSeparator = "<:>";
        Dictionary<string, string> Data = [];
        //Dictionary<string, object> Data = [];
        public string SaveFolderPath;
        public string SaveFilePath;

        public Saving(string FileName, string FolderPath)
        {
            SaveFolderPath = FolderPath;
            SaveFilePath = Path.Combine(SaveFolderPath, FileName);
            Load();
        }

        public bool Has(string Key) =>
            Data.ContainsKey(Key);
        public void Remove(string Key) =>
            Data.Remove(Key);

        public void Set(string Key, string Value) =>
            Data[Key] = Value;
        public void Set(string Key, bool Value) =>
            Data[Key] = Value.ToString();
        public void Set(string Key, double Value) =>
            Data[Key] = Value.ToString();
        public void Set(string Key, int Value) =>
            Data[Key] = Value.ToString();
        public void Set(string Key, float Value) =>
            Data[Key] = Value.ToString();
        /*public void Set(string Key, object Value) =>
            Data[Key] = Value.ToString();*/

        public void Set(string Key, params string[] Items)
        {
            Set(Key, string.Join(ValueSeparator, Items));
        }

        /*public void Set<T>(string Key, T Value)
        {
            Data[Key] = Value;
        }
        public T Get<T>(string Key, T Default = default)
        {
            if (Data.TryGetValue(Key, out var Cached))
                return (T)Cached;
            if (Data.TryGetValue(Key, out var _String))
            {
                try
                {
                    T Value = (T)Convert.ChangeType(_String, typeof(T), CultureInfo.InvariantCulture);
                    Data[Key] = Value;
                    return Value;
                }
                catch { }
            }

            Set(Key, Default);
            Data[Key] = Default;
            return Default;
        }*/

        public string Get(string Key, string Default = "NOTFOUND")
        {
            if (Data.TryGetValue(Key, out var value))
                return value;
            if (Default != "NOTFOUND")
                Set(Key, Default);
            return Default;
        }

        public int GetInt(string Key, int Default = -1)
        {
            if (Data.TryGetValue(Key, out string StrValue))
            {
                if (int.TryParse(StrValue, out int IntValue))
                    return IntValue;
            }
            if (Default != -1)
                Set(Key, Default);
            return Default;
        }

        public string[] Get(string Key, bool UseListParameter) =>
            Get(Key).Split(ValueSeparator, StringSplitOptions.None);
        public void Clear() =>
            Data.Clear();
        public string Save()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
            if (!File.Exists(SaveFilePath))
                File.Create(SaveFilePath).Close();

            StringBuilder Builder = new StringBuilder(Data.Count * 32);
            foreach (KeyValuePair<string, string> Entry in Data)
                Builder.Append(Entry.Key).Append(KeyValueSeparator).Append(Entry.Value).Append(KeySeparator);
            string Content = Builder.ToString();
            File.WriteAllText(SaveFilePath, Content);
            return Content;
        }
        public void Load()
        {
            if (!File.Exists(SaveFilePath))
                return;
            Process(File.ReadAllText(SaveFilePath));
        }

        public void Process(string Content)
        {
            Data.Clear();
            if (string.IsNullOrWhiteSpace(Content))
                return;
            foreach (var Entry in Content.Split(KeySeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] Values = Entry.Split(KeyValueSeparator, 2, StringSplitOptions.None);
                if (Values.Length == 2)
                    Data[Values[0]] = Values[1];
            }
        }
    }
    public class Favourite : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        [JsonPropertyName("children")]
        public ObservableCollection<Favourite> Children { get; set; }

        [JsonPropertyName("name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_Name))
                    return Url;
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged();
            }
        }
        private string _Name;

        [JsonPropertyName("type")]
        public string Type
        {
            get => _Type;
            set
            {
                _Type = value;
                RaisePropertyChanged();
            }
        }
        private string _Type;

        [JsonPropertyName("url")]
        public string Url
        {
            get => _Url;
            set
            {
                _Url = value;
                RaisePropertyChanged();
            }
        }
        private string _Url;
    }

    public class BookmarksManager
    {
        public class Bookmarks
        {
            [JsonPropertyName("roots")]
            public BookmarkRoots Roots { get; set; }
        }
        public class BookmarkRoots
        {
            [JsonPropertyName("bookmark_bar")]
            public Favourite Bookmarks { get; set; }
        }

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static Bookmarks Import(string _Path)
        {
            return JsonSerializer.Deserialize<Bookmarks>(File.ReadAllText(_Path), JsonOptions)!;
        }
    }
}
