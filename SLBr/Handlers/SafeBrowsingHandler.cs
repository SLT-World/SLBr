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
            //Potentially_Harmful_Application,
            Social_Engineering,
            Unwanted_Software,
        }
        /*enum PlatformType
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
        }*/

        string APIKey;
        string ClientID;

        public SafeBrowsingHandler(string _APIKey, string _ClientID)
        {
            APIKey = _APIKey;
            ClientID = _ClientID;
        }

        public ThreatType GetThreatType(string Data)
        {
            ThreatType _Type = ThreatType.Unknown;
            if (Data.Length > 2)
            {
                try
                {
                    if (JsonNode.Parse(Data)["matches"] is JsonArray Matches)
                    {
                        string FirstThreatType = Matches[0]["threatType"].ToString();
                        if (FirstThreatType == "MALWARE")
                            _Type = ThreatType.Malware;
                        else if (FirstThreatType == "UNWANTED_SOFTWARE")
                            _Type = ThreatType.Unwanted_Software;
                        else if (FirstThreatType == "SOCIAL_ENGINEERING")
                            _Type = ThreatType.Social_Engineering;
                        /*else if (FirstThreatType == "POTENTIALLY_HARMFUL_APPLICATION")
                            _Type = ThreatType.Potentially_Harmful_Application;*/
                        else if (Matches.Count > 1)
                        {
                            string SecondThreatType = Matches[1]["threatType"].ToString();
                            if (SecondThreatType == "MALWARE")
                                _Type = ThreatType.Malware;
                            else if (SecondThreatType == "UNWANTED_SOFTWARE")
                                _Type = ThreatType.Unwanted_Software;
                            else if (SecondThreatType == "SOCIAL_ENGINEERING")
                                _Type = ThreatType.Social_Engineering;
                            /*else if (SecondThreatType == "POTENTIALLY_HARMFUL_APPLICATION")
                                _Type = ThreatType.Potentially_Harmful_Application;*/
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
                return "{}";
            using (HttpClient Client = new HttpClient())
            {
                //,""POTENTIALLY_HARMFUL_APPLICATION""
                string Payload = $@"{{
    ""client"":{{""clientId"":""{ClientID}"",""clientVersion"":""1.0.0""}},
    ""threatInfo"":{{
        ""threatTypes"":[""THREAT_TYPE_UNSPECIFIED"",""MALWARE"",""SOCIAL_ENGINEERING"",""UNWANTED_SOFTWARE""],
        ""platformTypes"":[""CHROME""],
        ""threatEntryTypes"":[""URL""],
        ""threatEntries"":[{{""url"":""{Utils.CleanUrl(Url, false, false, true, false, false)}""}}]
    }}
}}";
                try
                {
                    var Response = Client.PostAsync($"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={APIKey}", new StringContent(Payload, Encoding.Default, "application/json")).Result;
                    return Response.Content.ReadAsStringAsync().Result;
                }
                catch { }
                return "ERROR";
            }
        }
    }
}
