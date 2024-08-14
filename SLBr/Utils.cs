using CefSharp;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SLBr
{
    public enum DWMWINDOWATTRIBUTE
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
    }

    public static class MonitorMethods
    {
        public const Int32 MONITOR_DEFAULTTOPRIMERTY = 0x00000001;
        public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;


        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);


        [DllImport("user32.dll")]
        public static extern Boolean GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);


        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct NativeRectangle
        {
            public Int32 Left;
            public Int32 Top;
            public Int32 Right;
            public Int32 Bottom;


            public NativeRectangle(Int32 left, Int32 top, Int32 right, Int32 bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class NativeMonitorInfo
        {
            public Int32 Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
            public NativeRectangle Monitor;
            public NativeRectangle Work;
            public Int32 Flags;
        }
    }

    public static class ClassExtensions
    {
        public static bool NewLoadHtml(this IWebBrowser browser, string html, string url, Encoding encoding, bool limitedUse = false, int uses = 1, string error = "")
        {
            if (!(browser.ResourceRequestHandlerFactory is Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory))
                throw new Exception("LoadHtml can only be used with the SLBr's IResourceRequestHandlerFactory implementation");
            if (resourceRequestHandlerFactory.RegisterHandler(url, ResourceHandler.GetByteArray(html, encoding), "text/html", limitedUse, uses, error))
            {
                browser.Load(url);
                return true;
            }
            return false;
        }
        public static bool NewNoLoadHtml(this IWebBrowser browser, string html, string url, Encoding encoding, bool limitedUse = false, int uses = 1, string error = "")
        {
            if (!(browser.ResourceRequestHandlerFactory is Handlers.ResourceRequestHandlerFactory resourceRequestHandlerFactory))
                throw new Exception("LoadHtml can only be used with the SLBr's IResourceRequestHandlerFactory implementation");
            resourceRequestHandlerFactory.RegisterHandler(url, ResourceHandler.GetByteArray(html, encoding), "text/html", limitedUse, uses, error);
            return true;
        }

        public static int CountChars(this string source, char toFind)
        {
            int count = 0;
            foreach (var c in source.AsSpan())
            {
                if (c == toFind)
                    count++;
            }
            return count;
        }
        public static bool ToBool(this bool? self) =>
            self == null || self == false ? false : true;
        public static CefState ToCefState(this bool self) =>
            self ? CefState.Enabled : CefState.Disabled;
        public static bool ToBoolean(this CefState self) =>
            self == CefState.Enabled ? true : false;
        public static FastHashSet<TSource> ToFastHashSet<TSource>(this IEnumerable<TSource> collection) =>
            new FastHashSet<TSource>(collection);
        public static BitmapSource ToBitmapSource(this DrawingImage source)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }

        public static bool IsModal(this Window window)
        {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }
        public static BitmapImage ToBitmapImage(this BitmapSource bitmapsource)
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
        }
        public static uint ToUInt(this System.Drawing.Color color) =>
               (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
    }

    public static class Utils
    {
        public static BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            const string base64Prefix = "data:image/";
            int base64Start = base64String.IndexOf("base64,");
            if (base64String.StartsWith(base64Prefix) && base64Start != -1)
                base64String = base64String.Substring(base64Start + 7);

            byte[] imageBytes = Convert.FromBase64String(base64String);

            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
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


        public static Process GetMutexOwner(string mutexName)
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
        }

        public static int GenerateRandomId()
        {
            int E = new Random().Next();
            return E;
        }

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
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
            IntPtr pathPtr = IntPtr.Zero;
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
                SHGetKnownFolderPath(ref _FolderGuid, 0, IntPtr.Zero, out pathPtr);
                return Marshal.PtrToStringUni(pathPtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        public static bool IsAdministrator() =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string Between(string Value, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = Value.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = Value.IndexOf(LastString);
            if (Pos2 - Pos1 > -1)
                FinalString = Value.Substring(Pos1, Pos2 - Pos1);
            else
                FinalString = Value.Substring(Pos1);
            return FinalString;
        }

        public static string GetFileExtensionFromUrl(string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : "";
        }
        
        /*public static bool IsSystemUrl(string Url) =>
            (IsInternalUrl(Url) || Url.StartsWith("ws:") || Url.StartsWith("wss:") || Url.StartsWith("javascript:") || Url.StartsWith("file:") || Url.StartsWith("localhost:") || IsAboutUrl(Url) || Url.StartsWith("view-source:") || Url.StartsWith("devtools:") || Url.StartsWith("data:"));*/
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:") || Url.StartsWith("mailto:") || Url.StartsWith("news:") || Url.StartsWith("feed:");
        public static bool IsAboutUrl(string Url) =>
            Url.StartsWith("about:");
        public static bool CanCheckSafeBrowsing(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.NavigationPreLoadSubFrame || _ResourceType == ResourceType.NavigationPreLoadMainFrame || _ResourceType == ResourceType.SubFrame;
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.Ping || _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame || _ResourceType == ResourceType.Image;
        public static bool CanCheck(TransitionType _TransitionType) =>
            _TransitionType != TransitionType.AutoSubFrame && _TransitionType != TransitionType.Blocked && _TransitionType != TransitionType.FormSubmit;
        public static bool IsHttpScheme(string Url) =>
            Url.StartsWith("https:") || Url.StartsWith("http:");
        public static bool IsDomain(string Url)
        {
            return !Url.StartsWith(".") && Url.Contains(".");
        }
        public static bool IsProtocolNotHttp(string Url)
        {
            if (IsHttpScheme(Url))
                return false;
            if (Url.Contains(":"))
            {
                if (Url.Contains("."))
                {
                    if (Url.IndexOf(".") < Url.IndexOf(":"))
                        return false;
                    else
                        return true;
                }
                else
                    return true;
            }
            return false;
        }
        public static bool IsUrl(string Url)
        {
            if (!Url.StartsWith("javascript:") && !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
                return false;
            if (!IsHttpScheme(Url) && !IsProtocolNotHttp(Url) && !IsDomain(Url) && !Url.EndsWith("/"))
                return false;
            return true;
        }
        public static bool IsCode(string Url)
        {
            if (Url.StartsWith("javascript:") || Url.StartsWith("data:"))
                return true;
            return false;
        }

        public static string RemovePrefix(string Url, string Prefix, bool CaseSensitive = false, bool Back = false, bool ReturnCaseSensitive = true)
        {
            string NewUrl = CaseSensitive ? Url : Url.ToLower();
            string CaseSensitiveUrl = Url;
            string NewPrefix = CaseSensitive ? Prefix : Prefix.ToLower();
            if ((Back ? NewUrl.EndsWith(NewPrefix) : NewUrl.StartsWith(NewPrefix)))
            {
                if (ReturnCaseSensitive)
                    return (Back ? CaseSensitiveUrl.Substring(0, CaseSensitiveUrl.Length - Prefix.Length) : CaseSensitiveUrl.Substring(Prefix.Length));
                return (Back ? NewUrl.Substring(0, NewUrl.Length - Prefix.Length) : NewUrl.Substring(Prefix.Length));
            }
            return Url;
        }
        public static string RemoveCharsAfterLastChar(string Content, string Prefix, bool KeepPrefix)
        {
            int Index = Content.LastIndexOf(Prefix);
            if (Index >= 0)
                Content = Content.Substring(0, Index + (KeepPrefix ? Prefix.Length : 0));
            return Content;
        }
        public static string FilterUrlForBrowser(string Url, string SearchEngineUrl)
        {
            Url = Url.Trim();
            if (Url.Length > 0)
            {
                if (!Url.StartsWith("domain:") && !Url.StartsWith("search:"))
                {
                    if (IsProgramUrl(Url))
                    {
                        Process.Start(Url);
                        return Url;
                    }
                    else if (IsUrl(Url))
                        Url = "domain:" + Url;
                    else
                        Url = "search:" + Url;
                }
                bool ContinueCheck = true;
                if (Url.StartsWith("domain:"))
                {
                    ContinueCheck = false;
                    string SubstringUrl = Url.Substring(7);
                    if (IsProtocolNotHttp(SubstringUrl))
                        Url = FixUrl(SubstringUrl);
                    else if (IsHttpScheme(SubstringUrl))
                        Url = FixUrl(SubstringUrl);
                    else
                        Url = FixUrl(SubstringUrl);
                }
                if (ContinueCheck && Url.StartsWith("search:"))
                {
                    if (!string.IsNullOrEmpty(SearchEngineUrl))
                        Url = FixUrl(string.Format(SearchEngineUrl, Uri.EscapeDataString(Url.Substring(7))));
                    else
                        Url = FixUrl(Uri.EscapeDataString(Url.Substring(7)));
                }
            }
            return Url;
        }

        public static string Host(string Url, bool RemoveWWW = true)
        {
            string Host = CleanUrl(Url, true, false, true, RemoveWWW);
            if (!string.IsNullOrEmpty(Url))
                return Host.Split('/')[0];
            return Host;
        }
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveFragment = true, bool RemoveWWW = false, bool RemoveProtocol = true)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            if (RemoveParameters)
            {
                int ToRemoveIndex = Url.LastIndexOf("?");
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            if (RemoveFragment)
            {
                int ToRemoveIndex = Url.LastIndexOf("#");
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
            if (string.IsNullOrEmpty(Url))
                return Url;
            Url = Url.Trim();
            if (!IsProtocolNotHttp(Url))
            {
                if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
                    Url = "https://" + Url;
            }
            return Url;
        }
        public static string ConvertUrlToReadableUrl(IdnMapping _IdnMapping, string Url)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            try
            {
                return Uri.UnescapeDataString(_IdnMapping.GetUnicode(Url.Trim()));
            } catch { return Url; }
        }
        public static bool CheckForInternetConnection(int timeoutMs = 1500, string url = null)
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
        }
    }

    public class Saving
    {
        string KeySeparator = "<,>";
        string ValueSeparator = "<|>";
        string KeyValueSeparator = "<:>";
        Dictionary<string, string> Data = new Dictionary<string, string>();
        public string SaveFolderPath;
        public string SaveFilePath;
        public bool UseContinuationIndex;

        public Saving(string FileName, string FolderPath)
        {
            SaveFolderPath = FolderPath;
            SaveFilePath = Path.Combine(SaveFolderPath, FileName);
            Load();
        }

        public bool Has(string Key, bool IsValue = false)
        {
            if (IsValue)
                return Data.ContainsValue(Key);
            return Data.ContainsKey(Key);
        }
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
            string Value = string.Join(ValueSeparator, Value_1, Value_2);
            Set(Key, Value, _Save);
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
            Get(Key).Split(new[] { ValueSeparator }, StringSplitOptions.None);
        public void Clear() =>
            Data.Clear();
        public void Save()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);

            if (!File.Exists(SaveFilePath))
                File.Create(SaveFilePath).Close();
            FastHashSet<string> Contents = new FastHashSet<string>();
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

            FastHashSet<string> Contents = File.ReadAllText(SaveFilePath).Split(new string[] { KeySeparator }, StringSplitOptions.None).ToFastHashSet();
            foreach (string Content in Contents)
            {
                if (string.IsNullOrWhiteSpace(Content))
                    continue;
                string[] Values = Content.Split(new string[] { KeyValueSeparator }, 2, StringSplitOptions.None);
                Data[Values[0]] = Values[1];
            }
        }
    }
}
