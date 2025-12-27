using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lpszClass, string lpszWindow);

        public static string GetWindowTextRaw(IntPtr hWnd)
        {
            StringBuilder Builder = new(512);
            GetWindowText(hWnd, Builder, Builder.Capacity);
            return Builder.ToString();
        }

        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_CHILD = 0x40000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_SYSMENU = 0x00080000;

        public const int WS_EX_DLGMODALFRAME = 0x00000001;
        public const int WS_EX_CLIENTEDGE = 0x00000200;
        public const int WS_EX_STATICEDGE = 0x00020000;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const int WS_OVERLAPPED = 0x00000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_DISABLED = 0x08000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_BORDER = 0x00800000;
        public const int WS_DLGFRAME = 0x00400000;
        public const int WS_VSCROLL = 0x00200000;
        public const int WS_HSCROLL = 0x00100000;
        public const int WS_GROUP = 0x00020000;
        public const int WS_TABSTOP = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;

        public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

        public const int WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU;

        public const int WS_CHILDWINDOW = WS_CHILD;

        public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_ACCEPTFILES = 0x00000010;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_MDICHILD = 0x00000040;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_WINDOWEDGE = 0x00000100;
        public const int WS_EX_CONTEXTHELP = 0x00000400;
        public const int WS_EX_RIGHT = 0x00001000;
        public const int WS_EX_LEFT = 0x00000000;
        public const int WS_EX_RTLREADING = 0x00002000;
        public const int WS_EX_LTRREADING = 0x00000000;
        public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
        public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;
        public const int WS_EX_CONTROLPARENT = 0x00010000;
        public const int WS_EX_APPWINDOW = 0x00040000;

        public const int WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
        public const int WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;

        public const uint WM_CLOSE = 0x0010;
    }
    static class MessageHelper
    {
        public const int WM_COPYDATA = 0x004A;
        public const int HWND_BROADCAST = 0xffff;

        [DllImport("user32", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public static void SendDataMessage(Process targetProcess, string msg)
        {
            IntPtr StringMessageBuffer = Marshal.StringToHGlobalUni(msg);

            COPYDATASTRUCT CopyData = new COPYDATASTRUCT();
            CopyData.dwData = IntPtr.Zero;
            CopyData.lpData = StringMessageBuffer;
            CopyData.cbData = msg.Length * 2;
            IntPtr CopyDataBuffer = IntPtrAlloc(CopyData);

            SendMessage(HWND_BROADCAST, WM_COPYDATA, IntPtr.Zero, CopyDataBuffer);

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
            return Self.Substring(0, MaxLength - (AddEllipsis ? 3 : 0)) + (AddEllipsis ? "..." : string.Empty);
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

    public static class Utils
    {
        public static async void DownloadAndCopyImage(string ImageUrl)
        {
            try
            {
                using (var _HttpClient = new HttpClient())
                {
                    byte[] ImageData = await _HttpClient.GetByteArrayAsync(ImageUrl);
                    if (ImageData != null)
                    {
                        using (MemoryStream stream = new MemoryStream(ImageData))
                        {
                            BitmapImage Bitmap = new BitmapImage();
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
            int SlashIndex = BaseUrl.IndexOf("/", 8, StringComparison.Ordinal);
            string Origin = SlashIndex > 0 ? BaseUrl.Substring(0, SlashIndex) : BaseUrl;
            if (Href.StartsWith("/", StringComparison.Ordinal))
                return Origin + Href;
            else
            {
                int LastSlash = BaseUrl.LastIndexOf("/", StringComparison.Ordinal);
                if (LastSlash >= 0)
                    return BaseUrl.Substring(0, LastSlash + 1) + Href;
                else
                    return BaseUrl + "/" + Href;
            }
        }*/

        public static string? GetAMPUrl(string Url)
        {
            using (HttpClient Client = new HttpClient())
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
            if (ColorString.StartsWith("rgb", StringComparison.Ordinal))
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

            Value = Value * 255;
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
            using (FileStream _FileStream = new FileStream(FilePath, FileMode.Create))
            {
                PngBitmapEncoder PNGEncoder = new PngBitmapEncoder();
                PNGEncoder.Frames.Add(BitmapFrame.Create(Bitmap));
                PNGEncoder.Save(_FileStream);
            }
        }

        public static BitmapImage ConvertBase64ToBitmapImage(string Base64)
        {
            int base64Start = Base64.IndexOf("base64,", StringComparison.Ordinal);
            if (Base64.StartsWith("data:image/", StringComparison.Ordinal) && base64Start != -1)
                Base64 = Base64.Substring(base64Start + 7);
            using (MemoryStream _Stream = new MemoryStream(Convert.FromBase64String(Base64)))
            {
                BitmapImage _Bitmap = new BitmapImage();
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
        private static Guid DownloadsGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        private static Guid PicturesGuid = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);
        public static string GetFolderPath(FolderGuids FolderGuid)
        {
            IntPtr PathPtr = IntPtr.Zero;
            try
            {
                Guid _FolderGuid = new Guid();
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

        //public static bool IsAdministrator() =>
        //    new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static (string, string) ParseCertificateIssue(string Certificate)
        {
            ReadOnlySpan<char> Span = Certificate.AsSpan().Trim();
            string CN = null;
            string O = null;
            while (!Span.IsEmpty)
            {
                int Comma = Span.IndexOf(",", StringComparison.Ordinal);
                ReadOnlySpan<char> Part = Comma >= 0 ? Span[..Comma] : Span;
                int Equal = Part.IndexOf("=", StringComparison.Ordinal);
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
            return (CN ?? string.Empty, O ?? string.Empty);
        }

        public static string GetScheme(string Url)
        {
            int SchemeSeparatorIndex = Url.IndexOf(":");
            if (SchemeSeparatorIndex != -1)
                return Url.Substring(0, SchemeSeparatorIndex);
            return string.Empty;
        }

        public static string GetFileExtension(string Url)
        {
            ReadOnlySpan<char> Span = Url.AsSpan();
            int Query = Span.IndexOf("?", StringComparison.Ordinal);
            if (Query >= 0)
                Span = Span[..Query];

            int Hash = Span.IndexOf("#", StringComparison.Ordinal);
            if (Hash >= 0)
                Span = Span[..Hash];

            int Slash = Span.LastIndexOf("/", StringComparison.Ordinal);
            if (Slash >= 0)
                Span = Span[(Slash + 1)..];

            int Dot = Span.LastIndexOf(".", StringComparison.Ordinal);
            return Dot >= 0 ? Span[Dot..].ToString() : string.Empty;
        }
        
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:", StringComparison.Ordinal) || Url.StartsWith("mailto:", StringComparison.Ordinal) || Url.StartsWith("news:", StringComparison.Ordinal) || Url.StartsWith("feed:", StringComparison.Ordinal);
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
             _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame || _ResourceType == ResourceType.Image;
        public static bool IsPossiblyAd(ResourceRequestType _ResourceType) =>
             _ResourceType == ResourceRequestType.XMLHTTPRequest || _ResourceType == ResourceRequestType.Media || _ResourceType == ResourceRequestType.Script || _ResourceType == ResourceRequestType.SubFrame || _ResourceType == ResourceRequestType.Image;
        public static bool IsHttpScheme(string Url) =>
            Url.StartsWith("https:", StringComparison.Ordinal) || Url.StartsWith("http:", StringComparison.Ordinal);
        public static bool IsDomain(string Url) =>
            !Url.StartsWith(".", StringComparison.Ordinal) && Url.IndexOf(".", StringComparison.Ordinal) > 0;
        public static bool IsProtocolNotHttp(string Url) =>
            !IsHttpScheme(Url) && IsProtocol(Url);
        public static bool IsProtocol(string Url)
        {
            int Colon = Url.IndexOf(":", StringComparison.Ordinal);
            if (Colon < 0)
                return false;
            int Dot = Url.IndexOf(".", StringComparison.Ordinal);
            return Dot < 0 || Colon < Dot;
        }
        public static bool IsUrl(string Url)
        {
            if (IsCode(Url))
                return false;
            if (!Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
                return false;
            return IsProtocol(Url) || IsDomain(Url) || Url.EndsWith("/", StringComparison.Ordinal);
        }
        public static bool IsCode(string Url) =>
            Url.StartsWith("javascript:", StringComparison.Ordinal) || Url.StartsWith("view-source:", StringComparison.Ordinal) || Url.StartsWith("localhost:", StringComparison.Ordinal) || Url.StartsWith("data:", StringComparison.Ordinal) || Url.StartsWith("blob:", StringComparison.Ordinal);
        public static bool IsCustomScheme(string Url) =>
            !IsHttpScheme(Url) && !IsCode(Url) && IsProtocol(Url);
        public static bool IsProprietaryCodec(string Extension) =>
            Extension == ".mp4" || Extension == ".m4a" || Extension == ".aac" || Extension == ".m4v" || Extension == ".mov" || Extension == ".mp3" || Extension == ".wma" || Extension == ".wmv";


        public static string EscapeDataString(string Input)
        {
            StringBuilder _StringBuilder = new StringBuilder(Input.Length + 8);
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
            long unixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string timePart = unixTimeMs.ToString("x");

            byte[] randomBytes = new byte[8];
            RandomNumberGenerator.Fill(randomBytes);

            string randomPart = BitConverter.ToString(randomBytes).Replace("-", "").ToLower();
            return (timePart + randomPart).Substring(0, 16);
        }

        public static string RemovePrefix(string Input, string Prefix, bool CaseSensitive = false, bool FromEnd = false, bool ReturnCaseSensitive = true)
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

            if (!Url.StartsWith("domain:", StringComparison.Ordinal) && !Url.StartsWith("search:", StringComparison.Ordinal))
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
            if (Span.StartsWith("search:", StringComparison.Ordinal))
            {
                ReadOnlySpan<char> Query = Span[7..];
                string Encoded = EscapeDataString(Query.ToString());
                return string.IsNullOrEmpty(SearchEngineUrl) ? FixUrl(Encoded) : FixUrl(string.Format(SearchEngineUrl, Encoded));
            }
            if (Span.StartsWith("domain:", StringComparison.Ordinal))
                return FixUrl(Span[7..].ToString());
            return Url;
        }
        public static string FastHost(string Url, bool RemoveTrivialSubdomain = true)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            ReadOnlySpan<char> Span = Url.AsSpan();

            int Protocol = Span.IndexOf("://", StringComparison.Ordinal);
            if (Protocol >= 0)
                Span = Span[(Protocol + 3)..];

            if (RemoveTrivialSubdomain)
            {
                if (Span.StartsWith("www.", StringComparison.Ordinal))
                    Span = Span[4..];
                if (Span.StartsWith("m.", StringComparison.Ordinal))
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
            if (IsHttpScheme(Url) || Url.StartsWith("file:///", StringComparison.Ordinal))
            {
                int SlashIndex = Host.IndexOf('/', StringComparison.Ordinal);
                return SlashIndex >= 0 ? Host.Substring(0, SlashIndex) : Host;
            }
            return Host;
        }
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveFragment = true, bool RemoveTrivialSubdomain = false, bool RemoveProtocol = true)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return Url;
            ReadOnlySpan<char> Span = Url.AsSpan().Trim();
            if (RemoveParameters)
            {
                int ToRemoveIndex = Span.LastIndexOf("?", StringComparison.Ordinal);
                if (ToRemoveIndex >= 0)
                    Span = Span[..ToRemoveIndex];
            }
            if (RemoveFragment)
            {
                int ToRemoveIndex = Span.LastIndexOf("#", StringComparison.Ordinal);
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
                Url = RemovePrefix(Url, "www.");
                Url = RemovePrefix(Url, "m.");
            }
            return Url;
        }
        public static string FixUrl(string Url)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return Url;
            return IsProtocol(Url) ? Url : "https://" + Url;
        }
        public static string ConvertUrlToReadableUrl(IdnMapping _IdnMapping, string Url)
        {
            if (Url.Length == 0)
                return Url;
            try { return UnescapeDataString(_IdnMapping.GetUnicode(Url)); }
            catch { return Url; }
        }
    }

    public class Saving
    {
        const string KeySeparator = "<,>";
        const string ValueSeparator = "<|>";
        const string KeyValueSeparator = "<:>";
        Dictionary<string, string> Data = new Dictionary<string, string>();
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

        public void Set(string Key, string Value, bool _Save = true)
        {
            Data[Key] = Value;
            if (_Save)
                Save();
        }
        public void Set(string Key, bool Value, bool _Save = true)
        {
            Data[Key] = Value.ToString();
            if (_Save)
                Save();
        }
        public void Set(string Key, int Value, bool _Save = true)
        {
            Data[Key] = Value.ToString();
            if (_Save)
                Save();
        }
        public void Set(string Key, float Value, bool _Save = true)
        {
            Data[Key] = Value.ToString();
            if (_Save)
                Save();
        }

        public void Set(string Key, string Value_1, string Value_2, bool _Save = true)
        {
            Set(Key, string.Join(ValueSeparator, Value_1, Value_2), _Save);
        }
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
        public void Save()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
            if (!File.Exists(SaveFilePath))
                File.Create(SaveFilePath).Close();

            using StreamWriter Writer = new StreamWriter(SaveFilePath, false);
            foreach (KeyValuePair<string, string> Entry in Data)
            {
                Writer.Write(Entry.Key);
                Writer.Write(KeyValueSeparator);
                Writer.Write(Entry.Value);
                Writer.Write(KeySeparator);
            }
            /*List<string> Contents = new List<string>();
            foreach (KeyValuePair<string, string> KVP in Data)
                Contents.Add(KVP.Key + KeyValueSeparator + KVP.Value);
            File.WriteAllText(SaveFilePath, string.Join(KeySeparator, Contents));*/
        }
        public void Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                Directory.CreateDirectory(SaveFolderPath);
                File.Create(SaveFilePath).Close();
                return;
            }
            var Content = File.ReadAllText(SaveFilePath);
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

    /*public class ChromiumBookmarkManager
    {
        public class Bookmarks
        {
            public BookmarkRoots roots { get; set; }
        }
        public class BookmarkRoots
        {
            public Bookmark bookmark_bar { get; set; }
        }
        public class Bookmark
        {
            public List<Bookmark> children { get; set; }
            public string date_added { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string url { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static Bookmarks Import(string _Path)
        {
            string FileContent = File.ReadAllText(_Path);
            return Decode<Bookmarks>(FileContent);
        }

        public static string Encode<T>(T data)
        {
            return JsonSerializer.Serialize(data, JsonOptions);
        }

        public static T Decode<T>(string jsonData)
        {
            return JsonSerializer.Deserialize<T>(jsonData, JsonOptions)!;
        }
    }*/
}
