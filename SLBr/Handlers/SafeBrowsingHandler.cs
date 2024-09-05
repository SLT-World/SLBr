using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;

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
                //MessageBox.Show(_Data, "Raw Data");
                try
                {
                    if (JsonNode.Parse(_Data)["matches"] is JsonArray Matches)
                    {
                        if (Matches[0]["threatType"].ToString() == "MALWARE")
                            _Type = ThreatType.Malware;
                        else if (Matches[0]["threatType"].ToString() == "UNWANTED_SOFTWARE")
                            _Type = ThreatType.Unwanted_Software;
                        else if (Matches[0]["threatType"].ToString() == "SOCIAL_ENGINEERING")
                            _Type = ThreatType.Social_Engineering;
                        if (_Type == ThreatType.Unknown && Matches.Count > 1)
                        {
                            if (Matches[1]["threatType"].ToString() == "MALWARE")
                                _Type = ThreatType.Malware;
                            else if (Matches[0]["threatType"].ToString() == "UNWANTED_SOFTWARE")
                                _Type = ThreatType.Unwanted_Software;
                            else if (Matches[1]["threatType"].ToString() == "SOCIAL_ENGINEERING")
                                _Type = ThreatType.Social_Engineering;
                        }
                    }
                }
                catch// (Exception ex)
                {
                    //MessageBox.Show($"Error parsing data: {ex.Message}", "Error");
                }
            }
            return _Type;
        }

        public string Response(string Url)
        {
            if (string.IsNullOrEmpty(APIKey))
                return $@"{{}}";
            using (var _HttpClient = new HttpClient())
            {
                string Payload = $@"{{
    ""client"":{{""clientId"":""{ClientId}"",""clientVersion"":""1.0.0""}},
    ""threatInfo"":{{
        ""threatTypes"":[""THREAT_TYPE_UNSPECIFIED"",""MALWARE"",""SOCIAL_ENGINEERING"",""UNWANTED_SOFTWARE"",""POTENTIALLY_HARMFUL_APPLICATION""],
        ""platformTypes"":[""CHROME""],
        ""threatEntryTypes"":[""URL""],
        ""threatEntries"":[{{""url"":""{Utils.CleanUrl(Url, false, false, true, false, false)}""}}]
    }}
}}";//,""WINDOWS"" doesn't seem to work
                try
                {
                    var Response = _HttpClient.PostAsync($"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={APIKey}", new StringContent(Payload, Encoding.Default, "application/json")).Result;
                    return Response.Content.ReadAsStringAsync().Result;
                }
                catch { }
                return "ERROR";
            }
        }
    }
}
