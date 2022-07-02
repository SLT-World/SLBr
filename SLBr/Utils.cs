// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Management;
using System.Drawing;
using CefSharp;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Imaging;

namespace SLBr
{
    public static class Extensions
    {
        public static CefState ToCefState(this bool self)
        {
            return self ? CefState.Enabled : CefState.Disabled;
        }
        public static bool ToBoolean(this CefState self)
        {
            return self == CefState.Enabled ? true : false;
        }
        public static T DeepCopy<T>(this T self)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var serialized = JsonConvert.SerializeObject(self, settings);
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }
        public static FastHashSet<TSource> ToFastHashSet<TSource>(this IEnumerable<TSource> collection) =>
            new FastHashSet<TSource>(collection);
        /*public static string[] Split(this string value, string[] seperator)
        {
            return value.Split(seperator, StringSplitOptions.None);
        }*/
    }
    public class Utils
    {
        public const int HWND_BROADCAST = 0xffff;
        public static readonly int WM_SHOWPAGE = RegisterWindowMessage("WM_SHOWPAGE");
        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

        /*public enum Theme
        {
            Light,
            Dark
        }

        public static Theme GetWindowsTheme()
        {
            int value;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                value = (int)key.GetValue("AppsUseLightTheme");
            }

            if (value == 0)
                return Theme.Dark;
            else
                return Theme.Light;
        }*/
        public static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            //Bitmap result = new Bitmap(width, height);
            Bitmap result = new Bitmap(bmp, new Size(width, height));
            /*using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }*/

            return result;
        }
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static uint ColorToUInt(Color color) =>
            (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        public static bool HasDebugger() =>
            Debugger.IsAttached;
        /*public static bool IsSystemUrl(string Url) =>
            (IsInternalUrl(Url) || Url.StartsWith("ws:") || Url.StartsWith("wss:") || Url.StartsWith("javascript:") || Url.StartsWith("file:") || Url.StartsWith("localhost:") || IsAboutUrl(Url) || Url.StartsWith("view-source:") || Url.StartsWith("devtools:") || Url.StartsWith("data:"));*/
        public static bool IsProgramUrl(string Url) =>
            (Url.StartsWith("callto:") || Url.StartsWith("mailto:") || Url.StartsWith("news:") || Url.StartsWith("feed:"));
        public static bool IsInteralProtocol(string Url) =>
            Url.StartsWith("slbr:");
        /*public static bool CanSchemeWithSpace(string Url) =>
            Url.StartsWith("javascript:") || Url.StartsWith("ws:") || Url.StartsWith("wss:");*/
        public static bool IsAboutUrl(string Url) =>
            Url.StartsWith("about:");
        /*public static bool IsBrowserUrlScheme(string Url) =>//NoHttps//WithCEFandChrome
            IsSystemUrl(Url) || IsProgramUrl(Url) || IsInternalUrl(Url) || IsAboutUrl(Url) || IsChromeScheme(Url);*/
        public static bool IsHttpScheme(string Url) =>//NoHttps
            Url.StartsWith("https:") || Url.StartsWith("http:");
        /*public static bool IsChromeScheme(string Url) =>
            Url.StartsWith("cef:") || Url.StartsWith("chrome:");*/
        public static bool IsDataOffender(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.MainFrame || _ResourceType == ResourceType.Image || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.FontResource;
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Image || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame;
        public static bool CanCheck(TransitionType _TransitionType) =>
            _TransitionType != TransitionType.AutoSubFrame && _TransitionType != TransitionType.Blocked && _TransitionType != TransitionType.FormSubmit;
        public static bool IsProtocolNotHttp(string Url)
        {
            if (IsHttpScheme(Url))
                return false;
            if (Url.Contains(":"))
            {
                if (Url.Contains("."))
                {
                    if (Url.IndexOf(".") < Url.IndexOf(":"))
                    {
                        return false;
                    }
                    else
                        return true;
                }
                else
                    return true;
            }
            return false;
        }

        public static string RemovePrefix(string Url, string Prefix, bool CaseSensitive = false, bool Back = false, bool ReturnCaseSensitive = true)
        {
            /*if (Url.Length >= Prefix.Length)
            {*/
            string NewUrl = CaseSensitive ? Url : Url.ToLower();
            string CaseSensitiveUrl = Url;
            string NewPrefix = CaseSensitive ? Prefix : Prefix.ToLower();
            if ((Back ? NewUrl.EndsWith(NewPrefix) : NewUrl.StartsWith(NewPrefix)))
            {
                if (ReturnCaseSensitive)
                    return (Back ? CaseSensitiveUrl.Substring(0, CaseSensitiveUrl.Length - Prefix.Length) : CaseSensitiveUrl.Substring(Prefix.Length));
                return (Back ? NewUrl.Substring(0, NewUrl.Length - Prefix.Length) : NewUrl.Substring(Prefix.Length));
            }
            //}
            return Url;
        }
        public static string RemoveCharsAfterLastChar(string Content, string Prefix, bool KeepPrefix)
        {
            int Index = Content.LastIndexOf(Prefix);
            if (Index >= 0)
                Content = Content.Substring(0, Index + (KeepPrefix ? Prefix.Length : 0));
            return Content;
        }
        public static string FilterUrlForBrowser(string Url, string SearchEngineUrl, bool Weblight/*, bool IsChromiumMode*/)
        {
            Url = Url.Trim();
            if (Url.Length > 0)
            {
                //MessageBox.Show($"{Url},{!Url.StartsWith("domain:")},{!Url.StartsWith("search:")}");
                if (!Url.StartsWith("domain:") && !Url.StartsWith("search:"))
                {
                    if (IsProgramUrl(Url))
                    {
                        Process.Start(Url);
                        return Url;
                    }
                    /*else if (!IsChromiumMode && Url.StartsWith("cef:"))
                        Url = "domain:chrome:" + Url.Substring(4);*/
                    else if ((Url.Contains(".") || IsProtocolNotHttp(Url)/* || IsSystemUrl(Url) || (IsChromiumMode && Url.StartsWith("chrome:"))) && (!Url.Contains(" ") || CanSchemeWithSpace(Url)*/))
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
                        if (IsAboutUrl(SubstringUrl))
                            Url = FixUrl(SubstringUrl.Replace("about://", "slbr://").Replace("about:", "slbr://"), false);
                        else
                            Url = FixUrl(SubstringUrl, false);
                    }
                    else if (IsHttpScheme(SubstringUrl))
                        Url = FixUrl(SubstringUrl, Weblight);
                    else
                    {
                        /*if (IsProtocolNotHttp(SubstringUrl))
                        {
                            Url = "search:" + SubstringUrl;
                            ContinueCheck = true;
                        }
                        else*/
                            Url = FixUrl(SubstringUrl, Weblight);
                    }
                }
                if (ContinueCheck && Url.StartsWith("search:"))
                    Url = FixUrl(string.Format(SearchEngineUrl, Url.Substring(7)), Weblight);

                //if (Url.ToLower().Contains("cefsharp.browsersubprocess"))
                //    MessageBox.Show("cefsharp.browsersubprocess is necessary for the browser engine to function accordingly.");
            }
            return Url;
        }

        public static string Host(string Url, bool RemoveWWW = true, bool CleanUrl = true)
        {
            string Host = CleanUrl ? Utils.CleanUrl(Url) : Url;
            if (RemoveWWW)
                Host = Host.Replace("www.", "");
            return Host.Split('/')[0];
        }
        public static string CleanUrl(string Url, bool RemoveValues = true, bool RemoveLastSlash = true)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            /*if (Url.StartsWith("chrome://"))
            {
                if (!MainWindow.Instance.IsChromiumMode)
                    Url = "cef" + Url.Substring(6);
            }*/
            if (RemoveValues)
            {
                int ToRemoveIndex = Url.LastIndexOf("?");
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
                else
                {
                    ToRemoveIndex = Url.LastIndexOf("#");
                    if (ToRemoveIndex >= 0)
                        Url = Url.Substring(0, ToRemoveIndex);
                }
                /*if (Url.EndsWith(".pdf#toolbar=0"))
                    Url = Utils.RemovePrefix(Url, "#toolbar=0", false, true);*/
            }
            Url = RemovePrefix(Url, "http://");
            Url = RemovePrefix(Url, "https://");
            Url = RemovePrefix(Url, "file:///");
            //if (Url.Replace(new Uri(FixUrl(Url, false, false)).Host, "").Contains("/"))
            if (RemoveLastSlash)
                Url = RemovePrefix(Url, "/", false, true);
            return Url;
        }
        public static string FixUrl(string Url, bool Weblight)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;

            Url = Url.Trim();

            Url = Url.Replace(" ", "%20");
            if (!IsProtocolNotHttp(Url)/*!IsSystemUrl(Url) && !Url.StartsWith("chrome:") && !Url.StartsWith("cef:")*/)
            {
                if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
                    Url = "https://" + Url;
                //Use HTTPS for Incomplete URLs
            }
            if (Weblight && IsHttpScheme(Url)/* && !IsSystemUrl(Url)*/)
                Url = "https://googleweblight.com/?lite_url=" + Url;
            return Url;
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
            catch
            {
                return false;
            }
        }

        public class SafeBrowsing
        {
            public enum ThreatType
            {
                Unknown,
                Malware,
                Potentially_Harmful_Application,
                Social_Engineering,
                Unwanted_Software,
            }
            enum PlatformType
            {
                Unknown,
                All,
                Android,
                Any,
                Chrome,
                Ios,
                Linux,
                MacOS,
                Windows,
            }
            enum ThreatEntryType
            {
                Unknown,
                Executable,
                IpAddressRange,
                Url
            }

            string Payload;

            string APIKey;
            string ClientId;

            public SafeBrowsing(string API_Key, string Client_Id)
            {
                APIKey = API_Key;
                ClientId = Client_Id;
            }

            public ThreatType GetThreatType(string _Data)
            {
                ThreatType _Type = ThreatType.Unknown;
                if (_Data.Length > 2)
                {
                    dynamic Data = JObject.Parse(_Data);
                    try
                    {
                        dynamic Matches = Data.matches;
                        if (Matches[0].threatType == "MALWARE")
                            _Type = ThreatType.Malware;
                        if (Matches[0].threatType == "UNWANTED_SOFTWARE")
                            _Type = ThreatType.Unwanted_Software;
                        else if (Matches[0].threatType == "SOCIAL_ENGINEERING")
                            _Type = ThreatType.Social_Engineering;
                        if (_Type == ThreatType.Unknown)
                        {
                            if (Matches[1].threatType == "MALWARE")
                                _Type = ThreatType.Malware;
                            if (Matches[0].threatType == "UNWANTED_SOFTWARE")
                                _Type = ThreatType.Unwanted_Software;
                            else if (Matches[1].threatType == "SOCIAL_ENGINEERING")
                                _Type = ThreatType.Social_Engineering;
                        }
                    }
                    catch { }
                }
                return _Type;
            }

            public string Response(string Url)
            {
                if (APIKey == string.Empty)
                {
                    Payload = $@"{{}}";
                    return Payload;
                }
                using (var _HttpClient = new HttpClient())
                {
                    Payload = $@"{{
                            ""client"": {{
                            ""clientId"": ""{ClientId}"",
                            ""clientVersion"": ""1.0.0""
                        }},
                        ""threatInfo"": {{
                        ""threatTypes"":      [""MALWARE"", ""SOCIAL_ENGINEERING"", ""UNWANTED_SOFTWARE""],
                        ""platformTypes"":    [""CHROME"", ""WINDOWS""],
                        ""threatEntryTypes"": [""URL""],
                        ""threatEntries"": [
                            {{""url"": ""{Url}""}}
                            ]
                            }}
                        }}";

                    var Content = new StringContent(Payload, Encoding.Default, "application/json");
                    string ResultContent = "";
                    try
                    {
                        var Response = _HttpClient.PostAsync($"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={APIKey}", Content).Result;
                        ResultContent = Response.Content.ReadAsStringAsync().Result;
                    }
                    catch
                    {
                        ResultContent = "";
                    }
                    Payload = string.Empty;
                    return ResultContent;
                }
            }
        }

        public static string GetProcessorID()
        {
            ManagementClass mgt = new ManagementClass("Win32_Processor");
            ManagementObjectCollection procs = mgt.GetInstances();
            foreach (ManagementObject item in procs)
                return item.Properties["Name"].Value.ToString();
            /*var mbs = new ManagementObjectSearcher("Select ProcessorId From Win32_processor");
            ManagementObjectCollection mbsList = mbs.Get();
            string id = "";
            foreach (ManagementObject mo in mbsList)
            {
                id = mo["ProcessorId"].ToString();
                break;
            }
            return id;*/
            return "Unknown";
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

            public Saving(bool Custom = false, string FileName = "Save2.bin", string FolderPath = "EXECUTINGASSEMBLYFOLDERPATHUTILSSAVING")
            {
                if (Custom)
                {
                    if (FolderPath != "EXECUTINGASSEMBLYFOLDERPATHUTILSSAVING")
                        SaveFolderPath = FolderPath;
                    else
                        SaveFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    SaveFilePath = Path.Combine(SaveFolderPath, FileName);
                }
                else
                    SaveFilePath = Path.Combine(SaveFolderPath, "Save.bin");
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
            public void Set(string Key, string Value_1, string Value_2, bool _Save = true)
            {
                string Value = string.Join(ValueSeparator, Value_1, Value_2);
                Set(Key, Value, _Save);
            }
            public string Get(string Key)
            {
                //Load();
                if (Has(Key))
                    return Data[Key];
                
                return "NOTFOUND";
            }
            public string[] Get(string Key, bool UseListParameter)
            {
                return Get(Key).Split(new[] { ValueSeparator }, StringSplitOptions.None);
            }
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

                //FastHashSet<string> Contents = new FastHashSet<string>(File.ReadAllText(SaveFilePath).Split(new string[] { KeySeparator }, StringSplitOptions.None));
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

        public class ChromiumBookmarkManager
        {
            public class Bookmarks
            {
                public string checksum { get; set; }
                public Bookmark roots { get; set; }
            }
            public class Bookmark
            {
                public Metadata bookmark_bar { get; set; }
            }
            public class Metadata
            {
                public List<Metadata> children { get; set; }
                public string date_added { get; set; }
                public string id { get; set; }
                public string name { get; set; }
                public string type { get; set; }
                public string url { get; set; }
            }

            public static Bookmarks Import<T>(string _Path)
            {
                string FileContent = File.ReadAllText(_Path);
                return (Bookmarks)Decode<Bookmarks>(FileContent);
            }
            public static string Encode<T>(T Data)
            {
                return JsonConvert.SerializeObject(Data);
            }
            public static Object Decode<T>(string JsonData)
            {
                return JsonConvert.DeserializeObject<T>(JsonData);
            }
        }

        /*public class AdManager
        {
            public string EasyList;
            public AdManager()
            {
                EasyList = MainWindow.Instance.TinyDownloader.DownloadString()
            }

            public bool IsAd(string Url)
            {

            }
        }*/
    }
}