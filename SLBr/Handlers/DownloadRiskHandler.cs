/*Copyright Â© SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using ProtoBuf;
using SLBr.SafeBrowsing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using static SLBr.SafeBrowsing.ClientDownloadRequest;
using static SLBr.SafeBrowsing.ClientDownloadResponse;

namespace SLBr.Handlers
{
    public enum DownloadSecurityService
    {
        None,
        Google,
        //VirusTotal
        //Windows Defender//Unnecessary
    }

    public enum DownloadVerdict
    {
        Safe,
        Dangerous,
        Uncommon,
        DangerousHost
    }

    //https://source.chromium.org/chromium/chromium/src/+/main:components/safe_browsing/core/common/proto/csd.proto
    //https://github.com/mozilla-firefox/firefox/blob/main/toolkit/components/reputationservice/chromium/chrome/common/safe_browsing/csd.proto
    //https://github.com/mozilla-firefox/firefox/blob/main/toolkit/components/reputationservice/ApplicationReputation.cpp
    public class DownloadRiskHandler
    {
        const string GoogleEndpoint = "https://sb-ssl.google.com/safebrowsing/clientreport/download?key=";
        //const string VirusTotalEndpoint = "";
        private static Lazy<HttpClient> HttpClientInstance = new(() => HttpClientFactory.Create(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        }, TimeSpan.FromSeconds(10)));

        public FastHashSet<ulong> SafeHashes = [];

        public async Task<DownloadVerdict> IsSafe(string ActualPath, string FilePath, string DownloadUrl, DownloadSecurityService Service = DownloadSecurityService.Google, string? Referrer = null, CancellationToken Token = default)
        {
            if (!IsSupportedDownload(FilePath, DownloadUrl))
                return DownloadVerdict.Safe;
            ulong HashKey = BitConverter.ToUInt64(SHA256.HashData(Encoding.UTF8.GetBytes(DownloadUrl)), 0);
            if (SafeHashes.Contains(HashKey))
                return DownloadVerdict.Safe;
            DownloadVerdict Result = DownloadVerdict.Safe;
            switch (Service)
            {
                case DownloadSecurityService.Google:
                    Result = await SBGetDownloadVerdict(GoogleEndpoint + SECRETS.GOOGLE_API_KEY, ActualPath, FilePath, DownloadUrl, Referrer, Token);
                    break;
                    //case DownloadSecurityService.VirusTotal:
                    //TODO: Implement VirusTotal.
                    //break;
            }
            if (Result == DownloadVerdict.Safe)
                SafeHashes.Add(HashKey);
            return Result;
        }

        public static Lazy<FastHashSet<string>> SafeFileExtensions = new(() =>
            [
                with(StringComparer.OrdinalIgnoreCase),
                ".jpg",  ".jpeg", ".mp3",      ".mp4",  ".png",  ".csv",  ".ica",
                ".gif",  ".txt",  ".package",  ".tif",  ".webp", ".mkv",  ".wav",
                ".mov",  ".paf",  ".vbscript", ".ad",   ".inx",  ".isu",  ".job",
                ".rgs",  ".u3p",  ".out",      ".run",  ".bmp",  ".css",  ".ehtml",
                ".flac", ".ico",  ".jfif",     ".m4a",  ".m4v",  ".mpeg", ".mpg",
                ".oga",  ".ogg",  ".ogm",      ".ogv",  ".opus", ".pjp",  ".pjpeg",
                ".svgz", ".text", ".tiff",     ".weba", ".webm", ".xbm",  ".crx"
            ]);

        public bool IsSupportedDownload(string FilePath, string DownloadUrl)
        {
            if (!Utils.IsHttpScheme(DownloadUrl))
                return false;
            if (SafeFileExtensions.Value.Contains(Path.GetExtension(FilePath)))
                return false;
            return true;
        }

        private static async Task<DownloadVerdict> SBGetDownloadVerdict(string Endpoint, string ActualPath, string FilePath, string DownloadUrl, string? Referrer = null, CancellationToken Token = default)
        {
            if (!File.Exists(ActualPath))
                return DownloadVerdict.Safe;
            byte[] LocalHash;
            await using (FileStream _FileStream = File.OpenRead(ActualPath))
            using (SHA256 SHA = SHA256.Create())
            {
                LocalHash = await SHA.ComputeHashAsync(_FileStream, Token);
            }

            FileInfo _FileInfo = new(ActualPath);
            ClientDownloadRequest Request = new()
            {
                Url = DownloadUrl,
                Length = _FileInfo.Length,
                UserInitiated = true,
                FileBasename = Path.GetFileName(FilePath),
                Locale = "en-US",
                download_type = SBGetDownloadTypeNet(FilePath),
                digests = new Digests
                {
                    Sha256 = LocalHash
                }
            };

            Request.Resources.Add(new Resource
            {
                Url = DownloadUrl,
                Referrer = Referrer ?? "",
                Type = ResourceType.DownloadUrl
            });

            using var Stream = new MemoryStream();
            Serializer.Serialize(Stream, Request);
            byte[] RequestBytes = Stream.ToArray();

            using ReadOnlyMemoryContent Content = new(RequestBytes);
            Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            HttpResponseMessage Response = await HttpClientInstance.Value.PostAsync(Endpoint, Content, Token);

            if (!Response.IsSuccessStatusCode)
                return DownloadVerdict.Safe;
            byte[] ResponseBytes = await Response.Content.ReadAsByteArrayAsync(Token);
            if (ResponseBytes == null || ResponseBytes.Length == 0)
                return DownloadVerdict.Safe;
            ClientDownloadResponse ProtoResponse = Serializer.Deserialize<ClientDownloadResponse>(ResponseBytes);
            return ProtoResponse.ToVerdict();
        }

        //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/safe_browsing/download_type_util.cc
        private static DownloadType SBGetDownloadTypeNet(string FilePath)
        {
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                case ".zip":
                    return DownloadType.ZippedExecutable;
                case ".rar":
                    return DownloadType.RarCompressedExecutable;
                case ".7z":
                    return DownloadType.SevenZipCompressedExecutable;
                case ".tar":
                case ".gz":
                case ".bz2":
                case ".xz":
                case ".iso":
                    return DownloadType.Archive;
                case ".pdf":
                case ".doc":
                case ".docx":
                case ".docm":
                case ".docb":
                case ".dot":
                case ".dotm":
                case ".dotx":
                case ".xls":
                case ".xlsb":
                case ".xlt":
                case ".xlm":
                case ".xlsx":
                case ".xldm":
                case ".xltx":
                case ".xltm":
                case ".xla":
                case ".xlam":
                case ".xll":
                case ".xlw":
                case ".ppt":
                case ".pot":
                case ".pps":
                case ".pptx":
                case ".pptm":
                case ".potx":
                case ".potm":
                case ".ppam":
                case ".ppsx":
                case ".ppsm":
                case ".sldx":
                case ".rtf":
                case ".wll":
                    return DownloadType.Document;
            }
            return DownloadType.WinExecutable;
        }
    }
    public static class ProtoUtils
    {
        public static DownloadVerdict ToVerdict(this ClientDownloadResponse Response) => Response.verdict switch
        {
            Verdict.Safe => DownloadVerdict.Safe,
            Verdict.Dangerous => DownloadVerdict.Dangerous,
            Verdict.Uncommon => DownloadVerdict.Uncommon,
            Verdict.DangerousHost => DownloadVerdict.DangerousHost,
            _ => DownloadVerdict.Safe
        };
    }
}