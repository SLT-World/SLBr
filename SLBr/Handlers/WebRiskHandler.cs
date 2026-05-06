/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.SafeBrowsing;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        private static HttpClient? HttpClientInstance;

        public FastHashSet<ulong> SafeHashes = [];

        public static ThreatType SBv5GetThreatType(SearchHashesResponse Response, byte[] LocalHash)
        {
            foreach (FullHash _FullHash in Response.FullHashes)
            {
                if (!_FullHash.FullHash_.Span.SequenceEqual(LocalHash))
                    continue;
                FullHashDetail? Detail = _FullHash.FullHashDetails.FirstOrDefault();
                if (Detail == null)
                    continue;
                switch (Detail.ThreatType)
                {
                    case SafeBrowsing.ThreatType.Malware:
                    case SafeBrowsing.ThreatType.PotentiallyHarmfulApplication:
                        return ThreatType.Malware;
                    case SafeBrowsing.ThreatType.SocialEngineering:
                        return ThreatType.Social_Engineering;
                    case SafeBrowsing.ThreatType.UnwantedSoftware:
                        return ThreatType.Unwanted_Software;
                }
                continue;
            }
            return ThreatType.Unknown;
        }
        public static SearchHashesResponse SBv5Response(byte[] LocalHash, string Endpoint)
        {
            HttpClientInstance ??= new(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            })
            {
                Timeout = TimeSpan.FromSeconds(10),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
            try
            {
                var Response = HttpClientInstance.GetAsync(Endpoint + $"&hashPrefixes={Uri.EscapeDataString(Convert.ToBase64String(LocalHash, 0, 4))}").Result;
                if (Response.IsSuccessStatusCode)
                {
                    byte[] Bytes = Response.Content.ReadAsByteArrayAsync().Result;
                    if (Bytes.Length > 0)
                        return SearchHashesResponse.Parser.ParseFrom(Bytes);
                }
            }
            catch { }
            return new SearchHashesResponse();
        }


        public static ThreatType SBv4GetThreatType(string Data)
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
                                    case "POTENTIALLY_HARMFUL_APPLICATION":
                                        return ThreatType.Malware;
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
        public static string SBv4Response(string Url, string Endpoint)
        {
            HttpClientInstance ??= new(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            })
            {
                Timeout = TimeSpan.FromSeconds(10),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
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
                var Response = HttpClientInstance.PostAsync(Endpoint, new StringContent(Payload, Encoding.UTF8, "application/json")).Result;
                return Response.Content.ReadAsStringAsync().Result;
            }
            catch { }
            return "{}";
        }

        public static ThreatType PTCheck(string Url, string APIKey)
        {
            HttpClientInstance ??= new(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            })
            {
                Timeout = TimeSpan.FromSeconds(10),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
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
                var Response = HttpClientInstance.SendAsync(Request).Result;
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


        public ThreatType IsSafe(string Url, SecurityService Service = SecurityService.Google)
        {
            byte[] LocalHash = SHA256.HashData(Encoding.UTF8.GetBytes(Utils.CleanUrl(Url, true, false, true, false, true)));
            ulong HashKey = BitConverter.ToUInt64(LocalHash, 0);
            if (SafeHashes.Contains(HashKey))
                return ThreatType.Unknown;
            ThreatType Result = ThreatType.Unknown;
            switch (Service)
            {
                case SecurityService.Google:
                    Result = SBv5GetThreatType(SBv5Response(LocalHash, GoogleEndpoint + SECRETS.GOOGLE_API_KEY), LocalHash);
                    break;
                case SecurityService.Yandex:
                    //TODO: Implement Yandex Hash-based check.
                    //https://yandex.com/dev/safebrowsing/doc/en/concepts/url-hash
                    //https://yandex.com/dev/safebrowsing/doc/en/concepts/update-fullhashes-find
                    //https://yandex.com/dev/safebrowsing/doc/en/
                    Result = SBv4GetThreatType(SBv4Response(Url, YandexEndpoint + SECRETS.YANDEX_API_KEY));
                    break;
                case SecurityService.PhishTank:
                    Result = PTCheck(Url, SECRETS.PHISHTANK_API_KEY);
                    break;
            }
            if (Result == ThreatType.Unknown)
                SafeHashes.Add(HashKey);
            return Result;
        }
    }
}
