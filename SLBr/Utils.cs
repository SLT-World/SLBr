using CefSharp;
using CSCore.CoreAudioAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.WebRequestMethods;

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
        public static T DeepCopy<T>(this T self)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var serialized = JsonConvert.SerializeObject(self, settings);
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }
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
        public static Bitmap ToBitmap(this BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
        public static BitmapImage ToImageSource(this Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
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
        /*public static string[] Split(this string value, string[] seperator)
        {
            return value.Split(seperator, StringSplitOptions.None);
        }*/
    }

    public static class Utils
    {
        public static MMDevice GetDefaultRenderDevice()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
        }

        public static bool IsAudioPlayingInDevice(MMDevice device = null)
        {
            if (device == null)
                device = GetDefaultRenderDevice();
            using (var meter = AudioMeterInformation.FromDevice(device))
            {
                return meter.PeakValue > 0.0000001 && meter.PeakValue < 1;
            }

        }
        public static Process GetAlreadyRunningInstance()
        {
            Process _currentProc = Process.GetCurrentProcess();
            Process[] _allProcs = Process.GetProcessesByName(_currentProc.ProcessName);

            for (int i = 0; i < _allProcs.Length; i++)
            {
                if (_allProcs[i].Id != _currentProc.Id)
                    return _allProcs[i];
            }
            return null;
        }
        public static bool CheckInstancesUsingMutex()
        {
            Mutex _appMutex = new Mutex(false, App.Instance.AppUserModelID);
            if (!_appMutex.WaitOne(1000))
                return true;
            return false;
        }
        public static bool CheckInstancesFromRunningProcesses()
        {
            Process[] _Processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            return _Processes.Length > 1;
        }

        public static int GenerateRandomId()
        {
            Random rnd1 = new Random();
            return rnd1.Next();
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

        public static DrawingImage Utf32ToDrawingImage(int SymbolCode, double SymbolSize = 16, System.Windows.Media.FontFamily _FontFamily = null)
        {
            if (_FontFamily == null)
                _FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            var textBlock = new TextBlock
            {
                FontFamily = _FontFamily,
                Text = char.ConvertFromUtf32(SymbolCode)
            };

            var brush = new VisualBrush
            {
                Visual = textBlock,
                Stretch = Stretch.Uniform
            };

            var drawing = new GeometryDrawing
            {
                Brush = brush,
                Geometry = new RectangleGeometry(
                    new Rect(0, 0, SymbolSize, SymbolSize))
            };

            return new DrawingImage(drawing);
        }

        public static bool IsAdministrator() =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string GetProcessorID()
        {
            ManagementClass mgt = new ManagementClass("Win32_Processor");
            ManagementObjectCollection procs = mgt.GetInstances();
            foreach (ManagementObject item in procs)
                return item.Properties["Name"].Value.ToString();
            return "Unknown";
        }
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

        public static string ToMobileWiki(string Url)
        {
            string CleanedUrl = CleanUrl(Url, false, false, false, true);
            string OutputUrl = Url;
            if (!CleanedUrl.StartsWith("wikipedia.org") && CleanedUrl.Contains(".wikipedia.org") && !CleanedUrl.Contains(".m.wikipedia.org"))
                OutputUrl = Url.Replace(".wikipedia.org", ".m.wikipedia.org");
            else if (!CleanedUrl.StartsWith("wikimedia.org") && CleanedUrl.Contains(".wikimedia.org") && !CleanedUrl.Contains(".m.wikimedia.org") && !CleanedUrl.StartsWith("donate.wikimedia.org") && !CleanedUrl.StartsWith("upload.wikimedia.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikimedia.org", ".m.wikimedia.org");
            else if (CleanedUrl.StartsWith("mediawiki.org"))
                OutputUrl = Url.Replace("www.", "").Replace("mediawiki.org", "m.mediawiki.org");
            else if (CleanedUrl.StartsWith("wikidata.org"))
                OutputUrl = Url.Replace("www.", "").Replace("wikidata.org", "m.wikidata.org");
            else if (!CleanedUrl.StartsWith("wikibooks.org") && CleanedUrl.Contains(".wikibooks.org") && !CleanedUrl.Contains(".m.wikibooks.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikibooks.org", ".m.wikibooks.org");
            else if (!CleanedUrl.StartsWith("wikinews.org") && CleanedUrl.Contains(".wikinews.org") && !CleanedUrl.Contains(".m.wikinews.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikinews.org", ".m.wikinews.org");
            else if (!CleanedUrl.StartsWith("wiktionary.org") && CleanedUrl.Contains(".wiktionary.org") && !CleanedUrl.Contains(".m.wiktionary.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wiktionary.org", ".m.wiktionary.org");
            else if (!CleanedUrl.StartsWith("wikiquote.org") && CleanedUrl.Contains(".wikiquote.org") && !CleanedUrl.Contains(".m.wikiquote.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikiquote.org", ".m.wikiquote.org");
            else if (!CleanedUrl.StartsWith("wikiversity.org") && CleanedUrl.Contains(".wikiversity.org") && !CleanedUrl.Contains(".m.wikiversity.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikiversity.org", ".m.wikiversity.org");
            else if (!CleanedUrl.StartsWith("wikivoyage.org") && CleanedUrl.Contains(".wikivoyage.org") && !CleanedUrl.Contains(".m.wikivoyage.org"))
                OutputUrl = Url.Replace("www.", "").Replace(".wikivoyage.org", ".m.wikivoyage.org");
            else if (CleanedUrl.StartsWith("wikisource.org"))
                OutputUrl = Url.Replace("www.", "").Replace("wikisource.org", "m.wikisource.org");
            return OutputUrl;
        }

        /*public static bool IsSystemUrl(string Url) =>
            (IsInternalUrl(Url) || Url.StartsWith("ws:") || Url.StartsWith("wss:") || Url.StartsWith("javascript:") || Url.StartsWith("file:") || Url.StartsWith("localhost:") || IsAboutUrl(Url) || Url.StartsWith("view-source:") || Url.StartsWith("devtools:") || Url.StartsWith("data:"));*/
        public static bool IsProgramUrl(string Url) =>
            Url.StartsWith("callto:") || Url.StartsWith("mailto:") || Url.StartsWith("news:") || Url.StartsWith("feed:");
        public static bool IsAboutUrl(string Url) =>
            Url.StartsWith("about:");
        public static bool CanCheckSafeBrowsing(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.NavigationPreLoadSubFrame || _ResourceType == ResourceType.NavigationPreLoadMainFrame || _ResourceType == ResourceType.MainFrame || _ResourceType == ResourceType.SubFrame;
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame;
        public static bool CanCheck(TransitionType _TransitionType) =>
            _TransitionType != TransitionType.AutoSubFrame && _TransitionType != TransitionType.Blocked && _TransitionType != TransitionType.FormSubmit;
        public static bool IsHttpScheme(string Url) =>
            Url.StartsWith("https:") || Url.StartsWith("http:");
        public static bool IsDomain(string Url) =>
            !Url.StartsWith(".") && Url.Contains(".");
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
            if (!IsHttpScheme(Url) && !IsProtocolNotHttp(Url) && !IsDomain(Url) && !Url.EndsWith("/"))
                return false;
            return true;
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
                    {
                        //if (IsAboutUrl(SubstringUrl))
                        //    Url = FixUrl(SubstringUrl.Replace("about://", "slbr://").Replace("about:", "slbr://"));
                        //else
                            Url = FixUrl(SubstringUrl);
                    }
                    else if (IsHttpScheme(SubstringUrl))
                        Url = FixUrl(SubstringUrl);
                    else
                        Url = FixUrl(SubstringUrl);
                }
                if (ContinueCheck && Url.StartsWith("search:"))
                    Url = FixUrl(string.Format(SearchEngineUrl, Url.Substring(7)));

                if (Url.EndsWith("youtube.com/watch?v="))
                    Url = "https://www.youtube.com/watch?v=KMU0tzLwhbE";
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
        public static string CleanUrl(string Url, bool RemoveParameters = false, bool RemoveLastSlash = true, bool RemoveAnchor = true, bool RemoveWWW = false)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            if (RemoveParameters)
            {
                int ToRemoveIndex = Url.LastIndexOf("?");
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            if (RemoveAnchor)
            {
                int ToRemoveIndex = Url.LastIndexOf("#");
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            Url = RemovePrefix(Url, "http://");
            Url = RemovePrefix(Url, "https://");
            Url = RemovePrefix(Url, "file:///");
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
            Url = Url.Trim().Replace(" ", "%20");
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

    public class ImageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = reader.Value.ToString();
            var index = reader.Value.ToString().IndexOf("base64,");
            if (index == -1)
            {
                try
                {
                    //Task<byte[]> task = App.ImageDownloaderObj.GetImageBytesAsync(new Uri(str));
                    //task.Wait(500);
                    //return task.Result;
                    return new byte[] { };
                }
                catch { return new byte[] { }; }
            }
            else
            {
                var m = new MemoryStream(Convert.FromBase64String(str.Substring(index + 7)));
                return m.ToArray();
            }
            //return (Bitmap)Image.FromStream(m);
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Bitmap bmp = (Bitmap)value;
            MemoryStream m = new MemoryStream();
            bmp.Save(m, System.Drawing.Imaging.ImageFormat.Png);

            writer.WriteValue(Convert.ToBase64String(m.ToArray()));
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
            //Load();
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
        public string Get(string Key)
        {
            if (Has(Key))
                return Data[Key];
            return "NOTFOUND";
        }
        public string[] Get(string Key, bool UseListParameter) =>
            Get(Key).Split(new[] { ValueSeparator }, StringSplitOptions.None);
        public void Clear() =>
            Data.Clear();
        public void Save()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);

            if (!System.IO.File.Exists(SaveFilePath))
                System.IO.File.Create(SaveFilePath).Close();
            FastHashSet<string> Contents = new FastHashSet<string>();
            foreach (KeyValuePair<string, string> KVP in Data)
                Contents.Add(KVP.Key + KeyValueSeparator + KVP.Value);
            System.IO.File.WriteAllText(SaveFilePath, string.Join(KeySeparator, Contents));
        }
        public void Load()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
            if (!System.IO.File.Exists(SaveFilePath))
                System.IO.File.Create(SaveFilePath).Close();

            FastHashSet<string> Contents = System.IO.File.ReadAllText(SaveFilePath).Split(new string[] { KeySeparator }, StringSplitOptions.None).ToFastHashSet();
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
