/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.SafeBrowsing;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace SLBr.Handlers
{
    public class WebRiskHandler
    {
        public enum SecurityService
        {
            None,
            Google,
            Yandex,
            PhishTank
        }

        public enum ThreatType
        {
            Unknown,
            Malware,
            Social_Engineering,
            Unwanted_Software,
        }

        const string GoogleEndpoint = "https://safebrowsing.googleapis.com/v5/hashes:search?key=";
        const string YandexEndpoint = "https://sba.yandex.net/v4/threatMatches:find?key=";
        const string PhishTankEndpoint = "https://checkurl.phishtank.com/checkurl/";

        public ThreatType SBv5GetThreatType(SearchHashesResponse Response, byte[] LocalHash)
        {
            //MessageBox.Show($"Full hashes returned: {Response.FullHashes.Count}");
            //MessageBox.Show($"Local Hash: {BitConverter.ToString(LocalHash)}");
            foreach (FullHash _FullHash in Response.FullHashes)
            {
                //byte[] ServerHash = _FullHash.FullHash_.ToByteArray();
                //MessageBox.Show($"Server Hash: {BitConverter.ToString(ServerHash)}");
                if (!_FullHash.FullHash_.Span.SequenceEqual(LocalHash))
                    continue;
                var Detail = _FullHash.FullHashDetails.FirstOrDefault();
                if (Detail == null)
                    continue;
                return Detail.ThreatType switch
                {
                    SafeBrowsing.ThreatType.Malware => ThreatType.Malware,
                    SafeBrowsing.ThreatType.SocialEngineering => ThreatType.Social_Engineering,
                    SafeBrowsing.ThreatType.UnwantedSoftware => ThreatType.Unwanted_Software,
                    _ => ThreatType.Unknown
                };
            }
            //MessageBox.Show("No match found.");
            return ThreatType.Unknown;
        }
        public SearchHashesResponse SBv5Response(byte[] LocalHash, string Endpoint)
        {
            using (HttpClient Client = new())
            {
                try
                {
                    
                    var Response = Client.GetAsync(Endpoint + $"&hashPrefixes={Uri.EscapeDataString(Convert.ToBase64String(LocalHash.AsSpan(0, 4).ToArray()))}").Result;
                    //MessageBox.Show($"Status: {Response.StatusCode}");
                    if (Response.IsSuccessStatusCode)
                    {
                        byte[] Bytes = Response.Content.ReadAsByteArrayAsync().Result;
                        //MessageBox.Show($"Response Size: {Bytes.Length} bytes");
                        if (Bytes.Length > 0)
                            return SearchHashesResponse.Parser.ParseFrom(Bytes);
                    }
                }
                catch { }
                return new SearchHashesResponse();
            }
        }


        public ThreatType SBv4GetThreatType(string Data)
        {
            if (Data.Length > 2)
            {
                try
                {
                    if (JsonNode.Parse(Data)?["matches"] is JsonArray Matches)
                    {
                        string FirstThreatType = Matches[0]["threatType"].ToString();
                        if (FirstThreatType == "MALWARE")
                            return ThreatType.Malware;
                        else if (FirstThreatType == "UNWANTED_SOFTWARE")
                            return ThreatType.Unwanted_Software;
                        else if (FirstThreatType == "SOCIAL_ENGINEERING")
                            return ThreatType.Social_Engineering;
                        /*else if (FirstThreatType == "POTENTIALLY_HARMFUL_APPLICATION")
                            return ThreatType.Potentially_Harmful_Application;*/
                        else if (Matches.Count > 1)
                        {
                            string SecondThreatType = Matches[1]["threatType"].ToString();
                            if (SecondThreatType == "MALWARE")
                                return ThreatType.Malware;
                            else if (SecondThreatType == "UNWANTED_SOFTWARE")
                                return ThreatType.Unwanted_Software;
                            else if (SecondThreatType == "SOCIAL_ENGINEERING")
                                return ThreatType.Social_Engineering;
                            /*else if (SecondThreatType == "POTENTIALLY_HARMFUL_APPLICATION")
                                return ThreatType.Potentially_Harmful_Application;*/
                        }
                    }
                }
                catch// (Exception ex)
                {
                    //MessageBox.Show($"Error parsing data: {ex.Message}", "Error");
                }
            }
            return ThreatType.Unknown;
        }
        public string SBv4Response(string Url, string Endpoint)
        {
            using (HttpClient Client = new())
            {
                //""THREAT_TYPE_UNSPECIFIED"",""POTENTIALLY_HARMFUL_APPLICATION""
                string Payload = $@"{{
    ""client"":{{""clientId"":""{SECRETS.GOOGLE_DEFAULT_CLIENT_ID}"",""clientVersion"":""1.0.0""}},
    ""threatInfo"":{{
        ""threatTypes"":[""MALWARE"",""SOCIAL_ENGINEERING"",""UNWANTED_SOFTWARE""],
        ""platformTypes"":[""CHROME""],
        ""threatEntryTypes"":[""URL""],
        ""threatEntries"":[{{""url"":""{Utils.CleanUrl(Url, false, false, true, false, false)}""}}]
    }}
}}";
                try
                {
                    var Response = Client.PostAsync(Endpoint, new StringContent(Payload, Encoding.Default, "application/json")).Result;
                    return Response.Content.ReadAsStringAsync().Result;
                }
                catch { }
                return "{}";
            }
        }

        public ThreatType PTCheck(string Url, string APIKey)
        {
            FormUrlEncodedContent Data = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("url", Url),
                new KeyValuePair<string, string>("format", "xml"),
                new KeyValuePair<string, string>("app_key", APIKey)
            ]);

            using (HttpClient Client = new HttpClient())
            {
                Client.DefaultRequestHeaders.Add("User-Agent", "phishtank/slbr");

                var Response = Client.PostAsync(PhishTankEndpoint, Data).GetAwaiter().GetResult();
                var ResponseText = Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                try
                {
                    XDocument XML = XDocument.Parse(ResponseText);
                    var Result = XML.Root?.Element("results")?.Element("url0");
                    if (Result != null)
                    {
                        bool InDatabase = Result.Element("in_database")?.Value == "true";
                        bool Verified = Result.Element("verified")?.Value == "true";
                        //string DetailPage = Result.Element("phish_detail_page")?.Value;

                        if (InDatabase && Verified)
                            return Result.Element("valid")?.Value == "true" ? ThreatType.Social_Engineering : ThreatType.Unknown;
                        //MessageBox.Show("PhishTank URL: " + DetailPage);
                        else
                            return ThreatType.Unknown;
                    }
                    else
                        return ThreatType.Unknown;
                }
                catch
                {
                    return ThreatType.Unknown;
                }
            }
        }

        public ThreatType IsSafe(string Url, SecurityService Service = SecurityService.Google)
        {
            switch (Service)
            {
                case SecurityService.Google:
                    byte[] LocalHash = SHA256.HashData(Encoding.UTF8.GetBytes(Utils.CleanUrl(Url, true, false, true, false, true)));
                    return SBv5GetThreatType(SBv5Response(LocalHash, GoogleEndpoint + SECRETS.GOOGLE_API_KEY), LocalHash);
                case SecurityService.Yandex:
                    return SBv4GetThreatType(SBv4Response(Url, YandexEndpoint + SECRETS.YANDEX_API_KEY));
                case SecurityService.PhishTank:
                    return PTCheck(Url, SECRETS.PHISHTANK_API_KEY);
            }
            return ThreatType.Unknown;
        }
    }
}
