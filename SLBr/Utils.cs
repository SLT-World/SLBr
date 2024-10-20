using CefSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Windows.Media.Imaging;

namespace SLBr
{
    static class MessageHelper
    {
        public const int WM_COPYDATA = 0x004A;
        /*public const int WM_NCHITTEST = 0x0084;
        public const int WM_SYSTEMMENU = 0xa4;
        public const int WP_SYSTEMMENU = 0x02;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const int HTMAXBUTTON = 9;*/
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

            SendMessage((IntPtr)HWND_BROADCAST, WM_COPYDATA, IntPtr.Zero, CopyDataBuffer);

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

    /*public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_NCRENDERING_ENABLED = 1,              // [get] Is non-client rendering enabled/disabled
        DWMWA_USE_HOSTBACKDROPBRUSH,                // [set] BOOL, Allows the use of host backdrop brushes for the window.
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,         // [set] BOOL, Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,        // [set] WINDOW_CORNER_PREFERENCE, Controls the policy that rounds top-level window corners
        DWMWA_BORDER_COLOR,                         // [set] COLORREF, The color of the thin border around a top-level window
        DWMWA_CAPTION_COLOR,                        // [set] COLORREF, The color of the caption
        DWMWA_TEXT_COLOR,                           // [set] COLORREF, The color of the caption text
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,       // [get] UINT, width of the visible border around a thick frame window
        DWMWA_MICA_EFFECT = 1029,                   // [set] BOOL, undocumented
        DWMWA_SYSTEMBACKDROP_TYPE = 38,             // [set] INT, undocumented
        DWMWA_LAST
    };

    enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO = 0,
        DWMSBT_DISABLE = 1, // None
        DWMSBT_MAINWINDOW = 2, // Mica
        DWMSBT_TRANSIENTWINDOW = 3, // Acrylic
        DWMSBT_TABBEDWINDOW = 4 // Tabbed
    }*/

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
                        return "Win64; x64";
                    else
                        return "Win64; IA64";
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
            //return (RuntimeInformation.ProcessArchitecture == Architecture.Arm || (RuntimeInformation.ProcessArchitecture) == Architecture.Arm64) ? "arm" : "x86";
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    return "x86";
                case Architecture.X64:
                    return "x86";
                case Architecture.Arm:
                case Architecture.Arm64:
                    return "arm";
            }
            return "x86";
            /*if (Environment.Is64BitOperatingSystem)
            {
                if (Environment.Is64BitProcess)
                    return "x86";
                else
                    return "arm";
            }
            else
                return "x86";*/
            //return Environment.Is64BitOperatingSystem ? (Environment.Is64BitProcess ? "x86" : "arm") : "x86";
        }

        /*public static string GetCpuBitness()
        {
            return Environment.Is64BitOperatingSystem ? "64" : "32";
        }*/

        public static string GetPlatformVersion()//https://textslashplain.com/2021/09/21/determining-os-platform-version/
        {
            string[] parts = Environment.OSVersion.Version.ToString().Split('.');
            return parts[0] + "." + parts[1] + "." + parts[2];
        }

        public static string GetOSVersion()
        {
            string[] parts = Environment.OSVersion.Version.ToString().Split('.');
            return parts[0] + "." + parts[1];
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

        public static string BuildUserAgentFromOSAndProduct(string OSInfo, string Product)
        {
            /* Derived from Safari's UA string.
             * This is done to expose our product name in a manner that is maximally compatible with Safari, we hope!!*/
            return $"Mozilla/5.0 ({OSInfo}) AppleWebKit/537.36 (KHTML, like Gecko) {Product} Safari/537.36";
        }
    }

    public static class ClassExtensions
    {
        /*public static bool NewLoadHtml(this IWebBrowser browser, string html, string url, Encoding encoding, int uses = 1, string error = "")
        {
            //if (!(browser.ResourceRequestHandlerFactory is Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory))
            //    throw new Exception("LoadHtml can only be used with the SLBr's IResourceRequestHandlerFactory implementation");
            Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory = (Handlers.ResourceRequestHandlerFactory)browser.ResourceRequestHandlerFactory;
            if (resourceRequestHandlerFactory.RegisterHandler(url, ResourceHandler.GetByteArray(html, encoding), "text/html", uses, error))
            {
                browser.Load(url);
                return true;
            }
            return false;
        }*/
        /*public static bool NewNoLoadHtml(this IWebBrowser browser, string html, string url, Encoding encoding, int uses = 1, string error = "")
        {
            //if (!(browser.ResourceRequestHandlerFactory is Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory))
            //    throw new Exception("LoadHtml can only be used with the SLBr's IResourceRequestHandlerFactory implementation");
            Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory = (Handlers.ResourceRequestHandlerFactory)browser.ResourceRequestHandlerFactory;
            resourceRequestHandlerFactory.RegisterHandler(url, ResourceHandler.GetByteArray(html, encoding), "text/html", uses, error);
            return true;
        }*/

        /*public static int CountChars(this string source, char toFind)
        {
            int count = 0;
            foreach (var c in source.AsSpan())
            {
                if (c == toFind)
                    count++;
            }
            return count;
        }*/
        public static bool ToBool(this bool? self) =>
            self == true;
        /*public static CefState ToCefState(this bool self) =>
            self ? CefState.Enabled : CefState.Disabled;
        public static bool ToBoolean(this CefState self) =>
            self == CefState.Enabled ? true : false;*/
        /*public static FastHashSet<TSource> ToFastHashSet<TSource>(this IEnumerable<TSource> collection) =>
            new FastHashSet<TSource>(collection);*/
        /*public static BitmapSource ToBitmapSource(this DrawingImage source)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new Rect(new Point(0, 0), new Size(source.Width, source.Height)));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }*/

        /*public static bool IsModal(this Window window)
        {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }*/
        /*public static BitmapImage ToBitmapImage(this BitmapSource bitmapsource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapsource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bImg.BeginInit();
            bImg.StreamSource = memoryStream;
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }*/
        public static uint ToUInt(this System.Drawing.Color color) =>
               (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
    }

    public static class Utils
    {
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
            new Random().Next();

        public enum FolderGuids
        {
            Downloads,
            Documents,
            Music,
            Pictures,
            SavedGames,
        }
        private static Guid DownloadsGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        private static Guid DocumentsGuid = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
        private static Guid MusicGuid = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
        private static Guid PicturesGuid = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
        private static Guid SavedGamesGuid = new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");
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
                    case FolderGuids.Documents:
                        _FolderGuid = DocumentsGuid;
                        break;
                    case FolderGuids.Music:
                        _FolderGuid = MusicGuid;
                        break;
                    case FolderGuids.Pictures:
                        _FolderGuid = PicturesGuid;
                        break;
                    case FolderGuids.SavedGames:
                        _FolderGuid = SavedGamesGuid;
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
        /*public static string Between(string Value, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = Value.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = Value.IndexOf(LastString);
            if (Pos2 - Pos1 > -1)
                FinalString = Value.Substring(Pos1, Pos2 - Pos1);
            else
                FinalString = Value.Substring(Pos1);
            return FinalString;
        }*/

        /*public static void LimitMemoryUsage(IntPtr _Process, int MaxMemory)//MB
        {
            int MaxMemoryBytes = MaxMemory * 1024 * 1024;
            SetProcessWorkingSetSize(_Process, MaxMemoryBytes, MaxMemoryBytes);
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);*/

        public static string GetFileExtensionFromUrl(string Url)
        {
            Url = Url.Split('?')[0].Split('/').Last();
            return Url.Contains('.', StringComparison.Ordinal) ? Url.Substring(Url.LastIndexOf('.')) : "";
        }
        
        /*public static bool IsSystemUrl(string Url) =>
            (IsInternalUrl(Url) || Url.StartsWith("ws:") || Url.StartsWith("wss:") || Url.StartsWith("javascript:") || Url.StartsWith("file:") || Url.StartsWith("localhost:") || IsAboutUrl(Url) || Url.StartsWith("view-source:") || Url.StartsWith("devtools:") || Url.StartsWith("data:"));*/
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:", StringComparison.Ordinal) || Url.StartsWith("mailto:", StringComparison.Ordinal) || Url.StartsWith("news:", StringComparison.Ordinal) || Url.StartsWith("feed:", StringComparison.Ordinal);
        /*public static bool IsAboutUrl(string Url) =>
            Url.StartsWith("about:", StringComparison.Ordinal);*/
        public static bool CanCheckSafeBrowsing(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.NavigationPreLoadSubFrame || _ResourceType == ResourceType.NavigationPreLoadMainFrame || _ResourceType == ResourceType.SubFrame;
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.Ping || _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame || _ResourceType == ResourceType.Image;
        /*public static bool CanCheck(TransitionType _TransitionType) =>
            _TransitionType != TransitionType.AutoSubFrame && _TransitionType != TransitionType.Blocked && _TransitionType != TransitionType.FormSubmit;*/
        public static bool IsHttpScheme(string Url) =>
            Url.StartsWith("https:", StringComparison.Ordinal) || Url.StartsWith("http:", StringComparison.Ordinal);
        public static bool IsDomain(string Url) =>
            !Url.StartsWith(".", StringComparison.Ordinal) && Url.Contains(".", StringComparison.Ordinal);
        public static bool IsProtocolNotHttp(string Url)
        {
            if (IsHttpScheme(Url))
                return false;
            int Colon = Url.IndexOf(":", StringComparison.Ordinal);
            if (Colon >= 0)
            {
                int Dot = Url.IndexOf(".", StringComparison.Ordinal);
                if (Dot >= 0)
                    return !(Dot < Colon);
                else
                    return true;
            }
            return false;
        }
        public static bool IsUrl(string Url)
        {
            if (!Url.StartsWith("javascript:", StringComparison.Ordinal) && !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
                return false;
            if (!IsHttpScheme(Url) && !IsProtocolNotHttp(Url) && !IsDomain(Url) && !Url.EndsWith("/", StringComparison.Ordinal))
                return false;
            return true;
        }
        public static bool IsCode(string Url)
        {
            return Url.StartsWith("javascript:", StringComparison.Ordinal) || Url.StartsWith("data:", StringComparison.Ordinal);
        }

        public static string RemovePrefix(string Url, string Prefix, bool CaseSensitive = false, bool Back = false, bool ReturnCaseSensitive = true)
        {
            string NewUrl = CaseSensitive ? Url : Url.ToLower();
            string NewPrefix = CaseSensitive ? Prefix : Prefix.ToLower();
            if (Back ? NewUrl.EndsWith(NewPrefix) : NewUrl.StartsWith(NewPrefix))
            {
                if (ReturnCaseSensitive)
                    return (Back ? Url.Substring(0, Url.Length - Prefix.Length) : Url.Substring(Prefix.Length));
                return (Back ? NewUrl.Substring(0, NewUrl.Length - Prefix.Length) : NewUrl.Substring(Prefix.Length));
            }
            return Url;
        }
        /*public static string RemoveCharsAfterLastChar(string Content, string Prefix, bool KeepPrefix)
        {
            int Index = Content.LastIndexOf(Prefix);
            if (Index >= 0)
                Content = Content.Substring(0, Index + (KeepPrefix ? Prefix.Length : 0));
            return Content;
        }*/
        public static string FilterUrlForBrowser(string Url, string SearchEngineUrl)
        {
            Url = Url.Trim();
            if (Url.Length > 0)
            {
                if (!Url.StartsWith("domain:", StringComparison.Ordinal) && !Url.StartsWith("search:", StringComparison.Ordinal))
                {
                    if (IsProgramUrl(Url))
                    {
                        Process.Start(Url);
                        return Url;
                    }
                    else if (IsUrl(Url))
                        return FixUrl(Url);
                    else
                        Url = "search:" + Url;
                    /*else if (IsUrl(Url))
                        Url = "domain:" + Url;
                    else
                        Url = "search:" + Url;*/
                }
                if (Url.StartsWith("search:", StringComparison.Ordinal))
                {
                    if (SearchEngineUrl.Length == 0)
                        return FixUrl(Uri.EscapeDataString(Url.Substring(7)));
                    else
                        return FixUrl(string.Format(SearchEngineUrl, Uri.EscapeDataString(Url.Substring(7))));
                }
                else if (Url.StartsWith("domain:", StringComparison.Ordinal))
                    return FixUrl(Url.Substring(7));
            }
            return Url;
        }

        public static string Host(string Url, bool RemoveWWW = true)
        {
            string Host = CleanUrl(Url, true, false, true, RemoveWWW);
            if (IsHttpScheme(Url) || Url.StartsWith("file:///", StringComparison.Ordinal))
            {
                if (Url.Length != 0)
                    return Host.Split('/')[0];
            }
            return Host;
        }
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveFragment = true, bool RemoveWWW = false, bool RemoveProtocol = true)
        {
            Url = Url.Trim();
            if (Url.Length == 0)
                return Url;
            if (RemoveParameters)
            {
                int ToRemoveIndex = Url.LastIndexOf("?", StringComparison.Ordinal);
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            if (RemoveFragment)
            {
                int ToRemoveIndex = Url.LastIndexOf("#", StringComparison.Ordinal);
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            if (RemoveProtocol)
            {
                Url = RemovePrefix(Url, "http://");
                Url = RemovePrefix(Url, "https://");
                Url = RemovePrefix(Url, "file:///");
            }
            if (RemoveLastSlash)
                Url = RemovePrefix(Url, "/", false, true);
            if (RemoveWWW)
                Url = RemovePrefix(Url, "www.");
            return Url;
        }
        public static string FixUrl(string Url)
        {
            Url = Url.Trim();
            if (Url.Length == 0)
                return Url;
            if (!IsProtocolNotHttp(Url))
            {
                if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
                    return "https://" + Url;
            }
            return Url;
        }
        public static string ConvertUrlToReadableUrl(IdnMapping _IdnMapping, string Url)
        {
            if (Url.Length == 0)
                return Url;
            try
            {
                return Uri.UnescapeDataString(_IdnMapping.GetUnicode(Url.Trim()));
            } catch { return Url; }
        }
        /*public static bool IsIPAddress(string Host)
        {
            if (Host.Split('.').Length != 4 || Host.Length > 23 || Host.ToLower().IndexOf(".com") > 1)
            {
                return false;
            }
            Host = MethodExtensions.ChopOffAfter(Host, ":");
            IPAddress address;
            return Host.Length <= 15 && IPAddress.TryParse(Host, out address);
        }*/
        /*[DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(ref int connDescription, int ReservedValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool checkInternet()
        {
            int connDescription = default(int);
            return InternetGetConnectedState(ref connDescription, 0);
        }*/
        /*public static bool CheckForInternetConnection(int TimeoutMS = 1500)//, string url = null)
        {
            try
            {
                url = "http://www.gstatic.com/generate_204";
                switch (CultureInfo.InstalledUICulture.Name)
                {
                    case string s when s.StartsWith("fa"):
                        url = "http://www.aparat.com";
                        break;
                    case string s when s.StartsWith("zh"):
                        url = "http://www.baidu.com";
                        break;
                };
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch { return false; }
        }*/
        /*public static bool CheckForInternetConnection(int TimeoutMS = 1500)//, string url = null)
        {
            try
            {
                return new Ping().Send("8.8.8.8", TimeoutMS, new byte[32]).Status == IPStatus.Success;
            }
            catch { return false; }
        }*/
    }

    public class Saving
    {
        string KeySeparator = "<,>";
        string ValueSeparator = "<|>";
        string KeyValueSeparator = "<:>";
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
            if (Has(Key))
                return Data[Key];
            if (Default != "NOTFOUND")
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
            List<string> Contents = new List<string>();
            foreach (KeyValuePair<string, string> KVP in Data)
                Contents.Add(KVP.Key + KeyValueSeparator + KVP.Value);
            File.WriteAllText(SaveFilePath, string.Join(KeySeparator, Contents));
        }
        public void Load()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
            if (!File.Exists(SaveFilePath))
                File.Create(SaveFilePath).Close();
            string[] Contents = File.ReadAllText(SaveFilePath).Split(KeySeparator, StringSplitOptions.None);
            foreach (string Content in Contents)
            {
                if (string.IsNullOrWhiteSpace(Content))
                    continue;
                string[] Values = Content.Split(KeyValueSeparator, 2, StringSplitOptions.None);
                Data[Values[0]] = Values[1];
            }
        }
    }
}
