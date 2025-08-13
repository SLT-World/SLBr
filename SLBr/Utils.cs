using CefSharp;
using CefSharp.DevTools.CSS;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Foundation.Collections;
using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace SLBr
{
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
            return "";
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

        public static string BuildChromeBrand()
        {
            return $"Chrome/{Cef.ChromiumVersion.Split('.')[0]}.0.0.0";
        }

        public static string BuildOSCpuInfo()
        {
            return BuildOSCpuInfoFromOSVersionAndCpuType(GetOSVersion(), BuildCPUInfo());
        }

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

        public static string BuildUserAgentFromProduct(string Product)
        {
            return BuildUserAgentFromOSAndProduct(/*GetUserAgentPlatform()+*/BuildOSCpuInfo(), Product);
        }

        public static string BuildMobileUserAgentFromProduct(string Product)
        {
            return BuildUserAgentFromOSAndProduct("Linux; Android 10; K", Product + " Mobile");
        }

        public static string BuildUserAgentFromOSAndProduct(string OSInfo, string Product)
        {
            /* Derived from Safari's UA string.
             * This is done to expose our product name in a manner that is maximally compatible with Safari, we hope!!*/
            return $"Mozilla/5.0 ({OSInfo}) AppleWebKit/537.36 (KHTML, like Gecko) {Product} Safari/537.36";
        }
    }

    public static class ClassExtensions
    {
        public static MColor ToMediaColor(this DColor color) =>
            MColor.FromArgb(color.A, color.R, color.G, color.B);
        public static bool ToBool(this bool? self) =>
            self == true;
        public static int ToInt(this bool self) =>
            self == true ? 1 : 0;
        /*public static uint ToUInt(this System.Drawing.Color color) =>
               (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));*/
    }

    public static class Utils
    {
        public static string? ParseAMPLink(string HTML, string BaseUrl)
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
        }

        /*public static string? GetAMPUrl(string Url)
        {
            using (HttpClient Client = new HttpClient())
            {
                string Payload = $"{{\"urls\":\"{Url}\"}}";
                try
                {
                    Client.DefaultRequestHeaders.Add("X-Goog-Api-Key", SECRETS.AMPs[App.MiniRandom.Next(SECRETS.AMPs.Count)]);
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
            Client.DefaultRequestHeaders.Add("X-Goog-Api-Key", SECRETS.AMPs[App.MiniRandom.Next(SECRETS.AMPs.Count)]);
            var Response = await Client.PostAsJsonAsync(App.AMPEndpoint, new { urls = new[] { Url } });
            if (!Response.IsSuccessStatusCode) return null;

            var Json = Response.Content.ReadFromJsonAsync<JsonElement>().Result;
            if (!Json.TryGetProperty("ampUrls", out var AMPUrls) || AMPUrls.GetArrayLength() == 0)
                return null;
            //cdnAmpUrl
            //return AMPUrls[0].GetProperty("ampUrl").GetString();
        }*/

        public static async Task RunSafeAsync(Func<Task> TaskFunction)
        {
            try
            {
                await TaskFunction().ConfigureAwait(false);
            }
            catch { }
        }

        public static void RunSafeFireAndForget(Func<Task> TaskFunction)
        {
            Task.Run(() => RunSafeAsync(TaskFunction));
        }

        /*public static bool FastBool(string Value)
        {
            ReadOnlySpan<char> Span = Value.AsSpan();
            return Span.Length == 4 &&
                   (Span[0] == 'T') &&
                   (Span[1] == 'r') &&
                   (Span[2] == 'u') &&
                   (Span[3] == 'e');
        }*/

        public static MColor ParseThemeColor(string ColorString)
        {
            if (ColorString.StartsWith("rgb", StringComparison.Ordinal))
            {
                var Numbers = Regex.Matches(ColorString, @"\d+").Cast<Match>().Select(m => byte.Parse(m.Value)).ToArray();

                byte r = Numbers[0];
                byte g = Numbers[1];
                byte b = Numbers[2];

                return MColor.FromRgb(r, g, b);
            }
            else
            {
                return ColorTranslator.FromHtml(ColorString).ToMediaColor();
            }
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);

        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }

        public static BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            int base64Start = base64String.IndexOf("base64,", StringComparison.Ordinal);
            if (base64String.StartsWith("data:image/", StringComparison.Ordinal) && base64Start != -1)
                base64String = base64String.Substring(base64Start + 7);
            using (MemoryStream _Stream = new MemoryStream(Convert.FromBase64String(base64String)))
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


        /*public static Process GetMutexOwner(string mutexName)
        {
            IntPtr mutexHandle = OpenMutex(0x001F0001, false, mutexName);
            if (mutexHandle == IntPtr.Zero)
                return null;

            uint processId = 0;
            GetSecurityInfo(mutexHandle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, out processId, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            CloseHandle(mutexHandle);

            return Process.GetProcessById((int)processId);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenMutex(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("advapi32.dll")]
        private static extern int GetSecurityInfo(IntPtr handle, SE_OBJECT_TYPE objectType, SECURITY_INFORMATION securityInfo, out uint ownerSid, IntPtr owner, IntPtr group, IntPtr dacl);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private enum SE_OBJECT_TYPE
        {
            SE_KERNEL_OBJECT = 0
        }

        [Flags]
        public enum SECURITY_INFORMATION
        {
            OWNER_SECURITY_INFORMATION = 0x00000001
        }*/

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
            //if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
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

        public static bool IsAdministrator() =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

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
            return (CN ?? "", O ?? "");
        }

        public static string GetFileExtension(string Url)
        {
            ReadOnlySpan<char> Span = Url.AsSpan();
            int Query = Span.IndexOf("?", StringComparison.Ordinal);
            if (Query >= 0)
                Span = Span[..Query];

            int Slash = Span.LastIndexOf("/", StringComparison.Ordinal);
            if (Slash >= 0)
                Span = Span[(Slash + 1)..];

            int Dot = Span.LastIndexOf(".", StringComparison.Ordinal);
            return Dot >= 0 ? Span[Dot..].ToString() : string.Empty;
        }
        
        /*public static bool IsSystemUrl(string Url) =>
            (IsInternalUrl(Url) || Url.StartsWith("ws:") || Url.StartsWith("wss:") || Url.StartsWith("javascript:") || Url.StartsWith("file:") || Url.StartsWith("localhost:") || IsAboutUrl(Url) || Url.StartsWith("view-source:") || Url.StartsWith("devtools:") || Url.StartsWith("data:"));*/
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:", StringComparison.Ordinal) || Url.StartsWith("mailto:", StringComparison.Ordinal) || Url.StartsWith("news:", StringComparison.Ordinal) || Url.StartsWith("feed:", StringComparison.Ordinal);
        /*public static bool IsAboutUrl(string Url) =>
            Url.StartsWith("about:", StringComparison.Ordinal);*/
        //public static bool CanCheckSafeBrowsing(ResourceType _ResourceType) =>
        //    _ResourceType == ResourceType.NavigationPreLoadSubFrame || _ResourceType == ResourceType.NavigationPreLoadMainFrame || _ResourceType == ResourceType.SubFrame;
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
             _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame || _ResourceType == ResourceType.Image;
        /*public static bool CanCheck(TransitionType _TransitionType) =>
            _TransitionType != TransitionType.AutoSubFrame && _TransitionType != TransitionType.Blocked && _TransitionType != TransitionType.FormSubmit;*/
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
            Url.StartsWith("javascript:", StringComparison.Ordinal) || Url.StartsWith("data:", StringComparison.Ordinal) || Url.StartsWith("blob:", StringComparison.Ordinal);


        public static string EscapeDataString(string Input)
        {
            var _StringBuilder = new StringBuilder(Input.Length + 8);
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


        public static string RemovePrefix(string Input, string Prefix, bool CaseSensitive = false, bool FromEnd = false, bool ReturnCaseSensitive = true)
        {
            ReadOnlySpan<char> InputSpan = Input.AsSpan();
            ReadOnlySpan<char> PrefixSpan = Prefix.AsSpan();

            if (FromEnd)
            {
                if (InputSpan.Length < PrefixSpan.Length) return Input;
                var Tail = InputSpan[^PrefixSpan.Length..];
                bool Match = CaseSensitive
                    ? Tail.SequenceEqual(PrefixSpan)
                    : Tail.Equals(PrefixSpan, StringComparison.OrdinalIgnoreCase);
                return Match ? InputSpan[..^PrefixSpan.Length].ToString() : Input;
            }
            else
            {
                if (InputSpan.Length < PrefixSpan.Length) return Input;
                var Head = InputSpan[..PrefixSpan.Length];
                bool Match = CaseSensitive
                    ? Head.SequenceEqual(PrefixSpan)
                    : Head.Equals(PrefixSpan, StringComparison.OrdinalIgnoreCase);
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
                    try
                    {
                        ProcessStartInfo _ProcessStartInfo = new(Url)
                        {
                            UseShellExecute = true,
                        };
                        Process.Start(_ProcessStartInfo);
                    }
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
        public static string FastHost(string Url)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            ReadOnlySpan<char> Span = Url.AsSpan();

            int Protocol = Span.IndexOf("://", StringComparison.Ordinal);
            if (Protocol >= 0)
                Span = Span[(Protocol + 3)..];

            if (Span.StartsWith("www.", StringComparison.Ordinal))
                Span = Span[4..];

            int Separator = Span.IndexOfAny('/', '?', '#');
            if (Separator >= 0)
                Span = Span[..Separator];

            return Span.ToString();
        }
        public static string GetOrigin(string Url)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;

            ReadOnlySpan<char> Span = Url.AsSpan();

            int SchemeEnd = Span.IndexOf("://", StringComparison.Ordinal);
            if (SchemeEnd < 0)
                return Url;

            ReadOnlySpan<char> Scheme = Span[..(SchemeEnd + 3)];

            Span = Span[(SchemeEnd + 3)..];

            int Separator = Span.IndexOfAny('/', '?', '#');
            if (Separator >= 0)
                Span = Span[..Separator];

            return string.Concat(Scheme, Span);
        }
        public static string Host(string Url, bool RemoveWWW = true)
        {
            string Host = CleanUrl(Url, true, false, true, RemoveWWW);
            if (IsHttpScheme(Url) || Url.StartsWith("file:///", StringComparison.Ordinal))
            {
                int SlashIndex = Host.IndexOf('/', StringComparison.Ordinal);
                return SlashIndex >= 0 ? Host.Substring(0, SlashIndex) : Host;
            }
            return Host;
        }
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveFragment = true, bool RemoveWWW = false, bool RemoveProtocol = true)
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
            if (RemoveWWW)
                Url = RemovePrefix(Url, "www.");
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
}
