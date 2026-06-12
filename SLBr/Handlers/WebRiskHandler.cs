/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.Protobuf;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace SLBr.Handlers
{
    public enum WebSecurityService
    {
        None,
        Google,
        Yandex,
        PhishTank
    }

    public enum ThreatType : int
    {
        Unknown = 0,
        Malware = 1,
        Social_Engineering = 2,
        Unwanted_Software = 3,
        Potentially_Harmful_Application = 4,
        //Malicious_Binary = 7,
        //Subresource_Filter = 13,
        Trick_To_Bill = 15,
        //Abusive_Experience_Violation = 20,
        //Better_Ads_Violation = 21,
        //Notification_Abuse = 24,
    }

    //https://source.chromium.org/chromium/chromium/src/+/main:components/safe_browsing/core/common/proto/safebrowsingv5.proto
    public class WebRiskHandler
    {
        const string GoogleEndpoint = "https://safebrowsing.googleapis.com/v5/hashes:search?key=";
        const string YandexEndpoint = "https://sba.yandex.net/v4/threatMatches:find?key=";
        const string PhishTankEndpoint = "https://checkurl.phishtank.com/checkurl/";
        private static Lazy<HttpClient> HttpClientInstance = new(() => HttpClientFactory.Create(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        }, TimeSpan.FromSeconds(10)));

        public FastHashSet<ulong> SafeHashes = [];

        private static ThreatType SBv5GetThreatType(SafeBrowsingV5Parser.SearchHashesResponse Response, byte[] LocalHash)
        {
            foreach (SafeBrowsingV5Parser.FullHash _FullHash in Response.FullHashes)
            {
                if (!_FullHash.HashBytes.SequenceEqual(LocalHash))
                    continue;
                foreach (int Threat in _FullHash.ThreatTypes)
                {
                    switch (Threat)
                    {
                        case 0: return ThreatType.Unknown;
                        case 1: return ThreatType.Malware;
                        case 2: return ThreatType.Social_Engineering;
                        case 3: return ThreatType.Unwanted_Software;
                        case 4: return ThreatType.Potentially_Harmful_Application;
                        /*case 7: return ThreatType.Malicious_Binary;
                        case 13: return ThreatType.Subresource_Filter;*/
                        case 15: return ThreatType.Trick_To_Bill;
                            /*case 20: return ThreatType.Abusive_Experience_Violation;
                            case 21: return ThreatType.Better_Ads_Violation;
                            case 24: return ThreatType.Notification_Abuse;*/
                    }
                }
            }
            return ThreatType.Unknown;
        }
        private static SafeBrowsingV5Parser.SearchHashesResponse SBv5Response(byte[] LocalHash, string Endpoint)
        {
            try
            {
                var Response = HttpClientInstance.Value.GetAsync($"{Endpoint}&hashPrefixes={Uri.EscapeDataString(Convert.ToBase64String(LocalHash, 0, 4).AsSpan())}").Result;
                if (Response.IsSuccessStatusCode)
                {
                    byte[] Bytes = Response.Content.ReadAsByteArrayAsync().Result;
                    if (Bytes != null && Bytes.Length > 0)
                        return SafeBrowsingV5Parser.ParseResponse(Bytes);
                }
            }
            catch { }
            return new SafeBrowsingV5Parser.SearchHashesResponse();
        }


        private static ThreatType SBv4GetThreatType(string Data)
        {
            if (Data.Length > 2)
            {
                try
                {
                    using JsonDocument Document = JsonDocument.Parse(Data);
                    if (Document.RootElement.TryGetProperty("matches", out JsonElement Matches) && Matches.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement Match in Matches.EnumerateArray())
                        {
                            if (Match.TryGetProperty("threatType", out JsonElement ThreatElement))
                            {
                                switch (ThreatElement.GetString())
                                {
                                    case "MALWARE":
                                        return ThreatType.Malware;
                                    case "POTENTIALLY_HARMFUL_APPLICATION":
                                        return ThreatType.Potentially_Harmful_Application;
                                    case "SOCIAL_ENGINEERING":
                                        return ThreatType.Social_Engineering;
                                    case "UNWANTED_SOFTWARE":
                                        return ThreatType.Unwanted_Software;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            return ThreatType.Unknown;
        }
        private static string SBv4Response(string Url, string Endpoint)
        {
            //TODO: Investigate Yandex clientId & clientVersion.
            string Payload = $@"{{
    ""client"":{{""clientId"":""{SECRETS.GOOGLE_DEFAULT_CLIENT_ID}"",""clientVersion"":""1.0.0""}},
    ""threatInfo"":{{
        ""threatTypes"":[""MALWARE"",""POTENTIALLY_HARMFUL_APPLICATION"",""SOCIAL_ENGINEERING"",""UNWANTED_SOFTWARE""],
        ""platformTypes"":[""CHROME""],
        ""threatEntryTypes"":[""URL""],
        ""threatEntries"":[{{""url"":""{Utils.CleanUrl(Url, false, false, true, false, false)}""}}]
    }}
}}";
            try
            {
                var Response = HttpClientInstance.Value.PostAsync(Endpoint, new StringContent(Payload, Encoding.UTF8, "application/json")).Result;
                return Response.Content.ReadAsStringAsync().Result;
            }
            catch { }
            return "{}";
        }

        private static ThreatType PTCheck(string Url, string APIKey)
        {
            FormUrlEncodedContent Data = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("url", Url),
                new KeyValuePair<string, string>("format", "xml"),
                new KeyValuePair<string, string>("app_key", APIKey)
            ]);
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Post, PhishTankEndpoint) { Content = Data };
                Request.Headers.UserAgent.ParseAdd("phishtank/slbr");
                var Response = HttpClientInstance.Value.SendAsync(Request).Result;
                var ResponseText = Response.Content.ReadAsStringAsync().Result;

                var Result = XDocument.Parse(ResponseText).Root?.Element("results")?.Element("url0");
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


        public ThreatType IsSafe(string Url, WebSecurityService Service = WebSecurityService.Google)
        {
            byte[] LocalHash = SHA256.HashData(Encoding.UTF8.GetBytes(Utils.CleanUrl(Url, true, false, true, false, true)));
            ulong HashKey = BitConverter.ToUInt64(LocalHash, 0);
            if (SafeHashes.Contains(HashKey))
                return ThreatType.Unknown;
            ThreatType Result = ThreatType.Unknown;
            switch (Service)
            {
                case WebSecurityService.Google:
                    Result = SBv5GetThreatType(SBv5Response(LocalHash, GoogleEndpoint + SECRETS.GOOGLE_API_KEY), LocalHash);
                    break;
                case WebSecurityService.Yandex:
                    //TODO: Implement Yandex Hash-based check.
                    //https://yandex.com/dev/safebrowsing/doc/en/concepts/url-hash
                    //https://yandex.com/dev/safebrowsing/doc/en/concepts/update-fullhashes-find
                    //https://yandex.com/dev/safebrowsing/doc/en/
                    Result = SBv4GetThreatType(SBv4Response(Url, YandexEndpoint + SECRETS.YANDEX_API_KEY));
                    break;
                case WebSecurityService.PhishTank:
                    Result = PTCheck(Url, SECRETS.PHISHTANK_API_KEY);
                    break;
            }
            if (Result == ThreatType.Unknown)
                SafeHashes.Add(HashKey);
            return Result;
        }
    }
}
