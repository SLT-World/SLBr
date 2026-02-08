using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace SLBr.Protocols
{
    public class TextGopher
    {
        public static string NewFormat(GeminiGopherIResponse Response)
        {
            if (Response.Mime != "text/html")
                return Encoding.UTF8.GetString(Response.Bytes.ToArray());
            else
            {
                var Items = Parse(Encoding.UTF8.GetString(Response.Bytes.ToArray()));
                //TODO: Investigate gopher search functionality.
                var Builder = new StringBuilder(@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<style>
html {height: 100%;}
* {white-space: pre;}
body {font-family: Consolas, monospace; background: white;}
.menu {display: flex;flex-direction: column;gap: 5px;background: whitesmoke; border-radius: 10px; width: 900px; max-width: 100%; margin: 50px auto; padding: 25px;}
</style>
<script>
function gopherSearch(e,r,t){let c=prompt(""Search query:"");if(!c)return;let h=""gopher://""+e+"":""+r+""/""+t.replace(/^\/+/,"""")+""	""+encodeURIComponent(c);window.location.href=h}
</script>
</head>
<body>
<div class=""menu"">");
                foreach (var Item in Items)
                {
                    Builder.Append("<div>");
                    string Url = $"gopher://{Item.Host}:{Item.Port}/{Item.Selector.TrimStart('/')}";
                    Builder.Append(Item.Type switch
                    {
                        '0' => $"📄 <a href=\"{Url}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        '1' => $"📁 <a href=\"{Url}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        '7' => $@"🔍 <a href=""#"" onclick=""gopherSearch('{WebUtility.HtmlEncode(Item.Host)}','{Item.Port}','{WebUtility.HtmlEncode(Item.Selector)}')"">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        '9' => $"📦 <a href=\"{Url}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        'g' => $"🖼️ <a href=\"{Url}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        'I' => $"🖼️ <a href=\"{Url}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        'h' => $"🌐 <a href=\"{Item.Selector}\">{WebUtility.HtmlEncode(Item.Label)}</a>",
                        'i' => $"   <span>{WebUtility.HtmlEncode(Item.Label)}</span>",
                        _ => $"❓ {WebUtility.HtmlEncode(Item.Label)}"
                    });
                    Builder.Append("</div>\n");
                }

                Builder.Append("</div></body></html>");
                return Builder.ToString();
            }
        }
        static IEnumerable<(char Type, string Label, string Selector, string Host, int Port)> Parse(string Text)
        {
            using StringReader Reader = new(Text);
            string Line;
            //WARNING: Do not add ` ?? ""`.
            while ((Line = Reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(Line))
                    continue;
                var Parts = Line.Substring(1).Split('\t');
                if (Parts.Length < 4)
                    continue;
                yield return (Line[0], Parts[0], Parts[1], Parts[2], int.TryParse(Parts[3], out var p) ? p : 70);
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
