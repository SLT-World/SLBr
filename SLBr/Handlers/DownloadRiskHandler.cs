/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using Google.Protobuf;
using SafeBrowsing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using static SafeBrowsing.ClientDownloadRequest.Types;

namespace SLBr.Handlers
{
    public enum DownloadSecurityService
    {
        None,
        Google,
        //VirusTotal
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
        HttpClient HttpClientInstance;

        public FastHashSet<ulong> SafeHashes = [];

        public async Task<DownloadVerdict> IsSafe(string FilePath, string DownloadUrl, DownloadSecurityService Service = DownloadSecurityService.Google, string? Referrer = null, CancellationToken Token = default)
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
                    Result = await SBGetDownloadVerdict(GoogleEndpoint + SECRETS.GOOGLE_API_KEY, FilePath, DownloadUrl, Referrer, Token);
                    break;
                    //case DownloadSecurityService.VirusTotal:
                    //TODO: Implement VirusTotal.
                    //break;
            }
            if (Result == DownloadVerdict.Safe)
                SafeHashes.Add(HashKey);
            return Result;
        }

        public static FastHashSet<string> SafeFileExtensions;

        public bool IsSupportedDownload(string FilePath, string DownloadUrl)
        {
            SafeFileExtensions ??= [
                ".jpg",  ".jpeg", ".mp3",      ".mp4",  ".png",  ".csv",  ".ica",
                ".gif",  ".txt",  ".package",  ".tif",  ".webp", ".mkv",  ".wav",
                ".mov",  ".paf",  ".vbscript", ".ad",   ".inx",  ".isu",  ".job",
                ".rgs",  ".u3p",  ".out",      ".run",  ".bmp",  ".css",  ".ehtml",
                ".flac", ".ico",  ".jfif",     ".m4a",  ".m4v",  ".mpeg", ".mpg",
                ".oga",  ".ogg",  ".ogm",      ".ogv",  ".opus", ".pjp",  ".pjpeg",
                ".svgz", ".text", ".tiff",     ".weba", ".webm", ".xbm"
            ];
            if (!Utils.IsHttpScheme(DownloadUrl))
                return false;
            if (SafeFileExtensions.Contains(Path.GetExtension(FilePath).ToLowerInvariant()))
                return false;
            return true;
        }

        private async Task<DownloadVerdict> SBGetDownloadVerdict(string Endpoint, string FilePath, string DownloadUrl, string? Referrer = null, CancellationToken Token = default)
        {
            if (!File.Exists(FilePath))
                return DownloadVerdict.Safe;
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
            byte[] LocalHash;
            await using (FileStream _FileStream = File.OpenRead(FilePath))
            using (SHA256 SHA = SHA256.Create())
            {
                LocalHash = await SHA.ComputeHashAsync(_FileStream, Token);
            }
            FileInfo _FileInfo = new(FilePath);
            ClientDownloadRequest Request = new()
            {
                Url = DownloadUrl,
                Length = _FileInfo.Length,
                UserInitiated = true,
                FileBasename = _FileInfo.Name,
                Locale = "en-US",
                DownloadType = SBGetDownloadType(FilePath),
                Digests = new Digests
                {
                    Sha256 = ByteString.CopyFrom(LocalHash)
                }
            };

            Request.Resources.Add(new Resource
            {
                Url = DownloadUrl,
                Referrer = Referrer ?? "",
                Type = ResourceType.DownloadUrl
            });

            using ByteArrayContent Content = new(Request.ToByteArray());
            Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            HttpResponseMessage Response = await HttpClientInstance.PostAsync(Endpoint, Content, Token);

            if (!Response.IsSuccessStatusCode)
                return DownloadVerdict.Safe;
            byte[] ResponseBytes = await Response.Content.ReadAsByteArrayAsync(Token);
            ClientDownloadResponse ProtoResponse = ClientDownloadResponse.Parser.ParseFrom(ResponseBytes);
            return ProtoResponse.ToVerdict();
        }

        //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/safe_browsing/download_type_util.cc
        private static DownloadType SBGetDownloadType(string FilePath)
        {
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                /*case ".exe":
                case ".msi":
                case ".cab":
                    return WinExecutable;*/

                /*TODO ZIP & RAR:*
                 * Do not send a ClientDownloadRequest for archive files unless they contain either executables or archives.
                 */
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
                case ".crx":
                    return DownloadType.ChromeExtension;
                /*case ".dmg":
                case ".img":
                case ".iso":
                case ".pkg":
                case ".mpkg":
                case ".smi":
                case ".app":
                case ".cdr":
                case ".dmgpart":
                case ".dvdr":
                case ".dart":
                case ".dc42":
                case ".diskcopy42":
                case ".imgpart":
                case ".ndif":
                case ".udif":
                case ".toast":
                case ".sparsebundle":
                case ".sparseimage":
                    return DownloadType.MacExecutable;*/
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
                /*case ".apk":
                case ".apkm":
                    return DownloadType.AndroidApk;*/
            }
            return DownloadType.WinExecutable;
            //return DownloadType.SampledUnsupportedFile;
        }
    }
    public static class ProtoUtils
    {
        public static DownloadVerdict ToVerdict(this ClientDownloadResponse Response)
        {
            return Response.Verdict switch
            {
                ClientDownloadResponse.Types.Verdict.Safe => DownloadVerdict.Safe,
                ClientDownloadResponse.Types.Verdict.Dangerous => DownloadVerdict.Dangerous,
                ClientDownloadResponse.Types.Verdict.Uncommon => DownloadVerdict.Uncommon,
                ClientDownloadResponse.Types.Verdict.DangerousHost => DownloadVerdict.DangerousHost,
                _ => DownloadVerdict.Safe
            };
        }
    }
}
