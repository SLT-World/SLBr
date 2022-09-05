using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace SLBr
{
    public static class ClassExtensions
    {
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
        /*public static string[] Split(this string value, string[] seperator)
        {
            return value.Split(seperator, StringSplitOptions.None);
        }*/
    }

    public static class Utils
    {
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
        public static uint ColorToUInt(Color color) =>
               (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));

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
        public static bool IsPossiblyAd(ResourceType _ResourceType) =>
            _ResourceType == ResourceType.Xhr || _ResourceType == ResourceType.Image || _ResourceType == ResourceType.Media || _ResourceType == ResourceType.Script || _ResourceType == ResourceType.SubFrame;
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
                        if (IsAboutUrl(SubstringUrl))
                            Url = FixUrl(SubstringUrl.Replace("about://", "slbr://").Replace("about:", "slbr://"));
                        else
                            Url = FixUrl(SubstringUrl);
                    }
                    else if (IsHttpScheme(SubstringUrl))
                        Url = FixUrl(SubstringUrl);
                    else
                        Url = FixUrl(SubstringUrl);
                }
                if (ContinueCheck && Url.StartsWith("search:"))
                    Url = FixUrl(string.Format(SearchEngineUrl, Url.Substring(7)));
            }
            return Url;
        }

        public static string Host(string Url, bool RemoveWWW = true, bool _CleanUrl = true)
        {
            string Host = _CleanUrl ? CleanUrl(Url) : Url;
            if (RemoveWWW)
                Host = Host.Replace("www.", "");
            return Host.Split('/')[0];
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
}
