/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.SafeBrowsing;
using System.Net;
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
        private static HttpClient? HttpClientInstance;

        public static ThreatType SBv5GetThreatType(SearchHashesResponse Response, byte[] LocalHash)
        {
            //TODO: Investigate Chrome billing warning.
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
                        return ThreatType.Malware;
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
        /*public static async Task<SearchHashesResponse> SBv5Response(byte[] LocalHash, string Endpoint)
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
                    
                var Response = await HttpClientInstance.GetAsync(Endpoint + $"&hashPrefixes={Uri.EscapeDataString(Convert.ToBase64String(LocalHash, 0, 4))}");
                if (Response.IsSuccessStatusCode)
                {
                    byte[] Bytes = await Response.Content.ReadAsByteArrayAsync();
                    if (Bytes.Length > 0)
                        return SearchHashesResponse.Parser.ParseFrom(Bytes);
                }
            }
            catch { }
            return new SearchHashesResponse();
        }*/
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
                    if (JsonNode.Parse(Data)?["matches"] is JsonArray Matches && Matches.Count > 0)
                    {
                        foreach (JsonNode? Match in Matches)
                        {
                            if (Match == null)
                                continue;
                            string? Threat = Match["threatType"]?.ToString();
                            if (Threat == null)
                                continue;
                            switch (Threat)
                            {
                                case "MALWARE":
                                    return ThreatType.Malware;
                                case "POTENTIALLY_HARMFUL_APPLICATION":
                                    return ThreatType.Malware;
                                case "SOCIAL_ENGINEERING":
                                    return ThreatType.Social_Engineering;
                                case "UNWANTED_SOFTWARE":
                                    return ThreatType.Unwanted_Software;
                            }
                            continue;
                        }
                    }
                }
                catch { }
            }
            return ThreatType.Unknown;
        }
        /*public static async Task<string> SBv4Response(string Url, string Endpoint)
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
            //""THREAT_TYPE_UNSPECIFIED""
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
                var Response = await HttpClientInstance.PostAsync(Endpoint, new StringContent(Payload, Encoding.UTF8, "application/json"));
                return await Response.Content.ReadAsStringAsync();
            }
            catch { }
            return "{}";
        }*/
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

        /*public static async Task<ThreatType> PTCheck(string Url, string APIKey)
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
                var Response = await HttpClientInstance.SendAsync(Request);
                var ResponseText = await Response.Content.ReadAsStringAsync();

                var Result = XDocument.Parse(ResponseText).Root?.Element("results")?.Element("url0");
                if (Result != null)
                {
                    bool InDatabase = Result.Element("in_database")?.Value == "true";
                    bool Verified = Result.Element("verified")?.Value == "true";
                    if (InDatabase && Verified)
                        return Result.Element("valid")?.Value == "true" ? ThreatType.Social_Engineering : ThreatType.Unknown;
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
        }*/
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

        /*public static async Task<ThreatType> IsSafe(string Url, SecurityService Service = SecurityService.Google)
        {
            switch (Service)
            {
                case SecurityService.Google:
                    byte[] LocalHash = SHA256.HashData(Encoding.UTF8.GetBytes(Utils.CleanUrl(Url, true, false, true, false, true)));
                    return SBv5GetThreatType(await SBv5Response(LocalHash, GoogleEndpoint + SECRETS.GOOGLE_API_KEY), LocalHash);
                case SecurityService.Yandex:
                    return SBv4GetThreatType(await SBv4Response(Url, YandexEndpoint + SECRETS.YANDEX_API_KEY));
                case SecurityService.PhishTank:
                    return await PTCheck(Url, SECRETS.PHISHTANK_API_KEY);
            }
            return ThreatType.Unknown;
        }*/

        public static ThreatType IsSafe(string Url, SecurityService Service = SecurityService.Google)
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
