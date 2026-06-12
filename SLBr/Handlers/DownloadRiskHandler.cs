/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.Protobuf;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

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
            ReadOnlySpan<byte> PayloadBytes;
            byte[] RentedBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                ProtobufWriter Writer = new(RentedBuffer);
                Writer.WriteString(1, DownloadUrl);

                Span<byte> DigestsSpace = stackalloc byte[64];
                ProtobufWriter DigestsWriter = new(DigestsSpace);
                DigestsWriter.WriteBytes(1, LocalHash);
                Writer.WriteBytes(2, DigestsWriter.WrittenSpan);

                Writer.WriteInt64(3, _FileInfo.Length);

                Span<byte> ResourceSpace = stackalloc byte[512];
                ProtobufWriter ResourceWriter = new(ResourceSpace);
                ResourceWriter.WriteString(1, DownloadUrl);
                ResourceWriter.WriteEnum(2, 0);
                ResourceWriter.WriteString(4, Referrer ?? "");
                Writer.WriteBytes(4, ResourceWriter.WrittenSpan);

                Writer.WriteBool(6, true);
                Writer.WriteString(9, Path.GetFileName(FilePath));
                int FileDownloadType = SBGetDownloadType(FilePath);
                Writer.WriteEnum(10, FileDownloadType);
                //Writer.WriteString(11, "en-US");
                //TODO: Deliver 22 archived_binary information.
                if (FileDownloadType is 3 or 6 or 11 or 15)
                    Writer.WriteBool(26, false);
                PayloadBytes = Writer.WrittenSpan;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(RentedBuffer);
            }

            using ReadOnlyMemoryContent Content = new(PayloadBytes.ToArray());
            Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            HttpResponseMessage Response = await HttpClientInstance.Value.PostAsync(Endpoint, Content, Token);

            if (!Response.IsSuccessStatusCode)
                return DownloadVerdict.Safe;
            byte[] ResponseBytes = await Response.Content.ReadAsByteArrayAsync(Token);
            if (ResponseBytes == null || ResponseBytes.Length == 0)
                return DownloadVerdict.Safe;
            //System.Diagnostics.Debug.WriteLine("byte[] Bytes = [" + string.Join(", ", ResponseBytes.Select(b => $"0x{b:X2}")) + "];");
            SafeBrowsingDownloadParser.ClientDownloadResponse ProtoResponse = SafeBrowsingDownloadParser.ParseResponse(ResponseBytes);
            return ProtoResponse.ToVerdict();
        }

        //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/safe_browsing/download_type_util.cc
        private static int SBGetDownloadType(string FilePath)
        {
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                case ".zip":
                    return 3;//ZippedExecutable;
                case ".rar":
                    return 11;//RarCompressedExecutable;
                case ".7z":
                    return 15;//SevenZipCompressedExecutable;
                case ".tar":
                case ".gz":
                case ".bz2":
                case ".xz":
                case ".iso":
                    return 6;//Archive;
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
                    return 14;//Document;
            }
            return 0;//WinExecutable;
        }
    }
    public static class ProtoUtils
    {
        public static DownloadVerdict ToVerdict(this SafeBrowsingDownloadParser.ClientDownloadResponse Response) => Response.Verdict switch
        {
            0 => DownloadVerdict.Safe,
            1 => DownloadVerdict.Dangerous,
            2 => DownloadVerdict.Uncommon,
            3 => DownloadVerdict.Dangerous,//PotentiallyUnwanted,
            4 => DownloadVerdict.DangerousHost,
            _ => DownloadVerdict.Safe
        };
    }
}
