using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SLBr.Protocols
{
    public class TextGopher
    {
        private static string FormatLineAsLink(string input, int count)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            if (!char.IsNumber(input.First()))
                return input;
            string remainder = input.Substring(1).Trim().Replace("<p>", "i").Replace("</p>", "0");
            int firstSlash = remainder.IndexOfAny(['/']);
            string url;
            string host = string.Empty;
            string port = string.Empty;
            string label;

            if (remainder.EndsWith("+"))
            {
                remainder = remainder.Substring(0, remainder.Length - 1).Trim();
            }

            if (firstSlash == -1)
            {
                label = remainder;
                url = remainder;
            }
            else
            {
                label = remainder.Substring(0, firstSlash).Trim();
                url = remainder.Substring(firstSlash).Trim();
            }
            int firstWhitespace = url.IndexOfAny(['\t']);
            if (firstWhitespace != -1)
            {
                string hostport = url.Substring(firstWhitespace).Trim();
                url = url.Substring(0, firstWhitespace).Trim();
                int firstURLWhitespace = hostport.IndexOfAny(['\t']);
                if (firstURLWhitespace != -1)
                {
                    host = hostport.Substring(0, firstURLWhitespace).Trim();
                    port = hostport.Substring(firstURLWhitespace).Trim();
                    url = $"{host}:{port}" + url;
                }
            }

            if (url.StartsWith("://"))
                url = "gopher" + url;
            else if (url.StartsWith("//"))
                url = "gopher:" + url;
            else if (!url.StartsWith("gopher://"))
                url = "gopher://" + url;
            return $"<div><a href=\"{url}\">{label}</a></div>";
            //return $"<div>[{count}] <a href=\"{url}\">{label}</a></div>";
        }

        public static string NewFormat(GeminiGopherIResponse Response, bool IsRaw = false)
        {
            if (Response.Mime != "text/html")
                return Encoding.UTF8.GetString(Response.Bytes.ToArray());
            else
            {
                string input = Encoding.UTF8.GetString(Response.Bytes.ToArray());
                StringBuilder sb = new StringBuilder("<!DOCTYPE html><html>\r\n");
                sb.Append("<style>" +
                    "html {height: 100%;}" +
                    "body {font-family: 'Segoe UI Light', Tahoma, sans-serif; background: white;}" +
                    "h1, h2, h3, p {margin: 0;}" +
                    "pre {background: white; border-radius: 5px; padding: 10px;}" +
                    ".content {background: whitesmoke; border-radius: 10px; margin: 50px; padding: 25px;}" +
                    ".embed {background: white; border-radius: 5px; padding: 5px;}" +
                    "</style><body><div class=\"content\">\r\n");
                bool ConstructingText = false;
                foreach (char c in input)
                {
                    bool AppendC = true;
                    switch (c)
                    {
                        case '<':
                            sb.Append("&lt;");
                            continue;
                        case '>':
                            sb.Append("&gt;");
                            continue;
                        case '\r':
                            continue;
                        /*case '\n':
                            sb.Append("<br/>");
                            break;*/
                        case 'i':
                            if (!ConstructingText)
                            {
                                ConstructingText = true;
                                sb.Append("<p>");
                                AppendC = false;
                            }
                            break;
                        case '0':
                            if (ConstructingText)
                            {
                                ConstructingText = false;
                                sb.Append("</p>");
                                AppendC = false;
                            }
                            break;
                    }
                    if (AppendC)
                        sb.Append(c);
                }
                sb.Append("\r\n</div></body></html>");

                int LinkCount = -1;
                StringWriter output = new StringWriter();
                using (StringReader reader = new StringReader(Regex.Replace(sb.ToString(), @"[^\u0000-\u007F]+", string.Empty, RegexOptions.Compiled)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string lineout = line;

                        if (!IsRaw)
                        {
                            lineout = lineout.Replace("\tfake\t(NULL)\t", "<br/>");
                            lineout = FormatLineAsLink(lineout, LinkCount);
                        }
                        output.WriteLine(lineout);
                    }
                }
                return output.ToString();
            }
        }
    }

    public class GopherResponse : GeminiGopherIResponse
    {
        public List<byte> Bytes { get; set; }
        public string Mime { get; set; } = "application/octet-stream";
        public string _Encoding { get; set; } = "UTF-8";
        public Uri _Uri { get; set; }
        public WebSSLStatus SSLStatus { get; set; }
    }

    public class Gopher
    {
        static GopherResponse ErrorResponse(Uri _Uri, string Message)
        {
            return new()
            {
                _Uri = _Uri,
                Mime = "text/plain",
                SSLStatus = new WebSSLStatus { PolicyErrors = SslPolicyErrors.None },
                Bytes = Encoding.UTF8.GetBytes(Message).ToList(),
            };
        }

        static async Task ReadMessage(GopherResponse Response, Stream _Stream, int MaxSize, int AbandonAfterSeconds)
        {
            DateTime AbandonTime = DateTime.Now.AddSeconds(AbandonAfterSeconds);

            byte[] Buffer = new byte[2048];
            int Bytes = await _Stream.ReadAsync(Buffer);
            var MaxSizeBytes = MaxSize * 1024;

            Response.Bytes = Buffer.Take(Bytes).ToList();

            while (Bytes != 0)
            {
                Bytes = await _Stream.ReadAsync(Buffer);
                Response.Bytes.AddRange(Buffer.Take(Bytes));

                if (Response.Bytes.Count > MaxSizeBytes)
                {
                    //Response.CodeMajor = '4';
                    //Response.CodeMinor = '3';
                    Response.Bytes = Encoding.UTF8.GetBytes("Resource too large").ToList();
                    break;
                }
                else if (DateTime.Now >= AbandonTime)
                {
                    //Response.CodeMajor = '4';
                    //Response.CodeMinor = '4';
                    Response.Bytes = Encoding.UTF8.GetBytes("Read timeout").ToList();
                    break;
                }
            }
        }

        private static string GetMime(Uri URI)
        {
            var Response = "text/html";
            var ExtensionFull = Path.GetExtension(URI.AbsolutePath);
            string Extension = (ExtensionFull.Length > 0) ? ExtensionFull.Substring(1) : string.Empty;
            if (ExtensionFull.Length > 0)
            {
                switch (Extension)
                {
                    case "txt":
                        Response = "text/plain";
                        break;
                    case "jpg":
                        Response = "image/jpeg";
                        break;
                    case "gif":
                    case "png":
                    case "bmp":
                    case "jpeg":
                        Response = "image/" + Extension;
                        break;
                    case "mp3":
                        Response = "audio/mpeg";
                        break;
                    case "wav":
                    case "ogg":
                    case "flac":
                        Response = "audio/" + Extension;
                        break;
                    default:
                        Response = $"application/{Extension}";
                        break;
                }
            }
            return Response;
        }


        public static async Task<GeminiGopherIResponse> Fetch(Uri HostURL, int AbandonReadSizeKb = 2048, int AbandonReadTimes = 5)
        {
            int Port = HostURL.Port;
            if (Port == -1)
                Port = 70;

            TcpClient _Client = new TcpClient();
            try
            {
                await _Client.ConnectAsync(HostURL.Host, Port);
            }
            catch (SocketException ex)
            {
                return ErrorResponse(HostURL, $"Network error: {ex.Message}");
            }

            Stream Stream = _Client.GetStream();

            var TrimmedUrl = HostURL.AbsolutePath;
            if (HostURL.AbsolutePath.Length > 1)
                TrimmedUrl = TrimmedUrl.Substring(1);

            byte[] Message = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(TrimmedUrl) + "\r\n");
            await Stream.WriteAsync(Message, 0, Message.Count());
            await Stream.FlushAsync();
            GopherResponse Response = new GopherResponse() { _Uri = HostURL, Mime = GetMime(HostURL), SSLStatus = new WebSSLStatus { PolicyErrors = SslPolicyErrors.None } };
            await ReadMessage(Response, Stream, AbandonReadSizeKb, AbandonReadTimes);
            _Client.Close();

            return Response;
        }
    }
}
