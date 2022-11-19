using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Handlers
{
    public class SafeBrowsingHandler
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

        public SafeBrowsingHandler(string API_Key, string Client_Id)
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
                        ""threatTypes"":      [""THREAT_TYPE_UNSPECIFIED"", ""MALWARE"", ""SOCIAL_ENGINEERING"", ""UNWANTED_SOFTWARE"", ""POTENTIALLY_HARMFUL_APPLICATION""],
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

}
