using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace SLBr.Protocols
{
    public class TextGopher
    {
        bool is_literal = false;
        private static string FormatLineAsLink(string input, int count)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            if (!Char.IsNumber(input.First()))
                return input;
            string remainder = input.Substring(1).Trim().Replace("<p>", "i").Replace("</p>", "0");
            int firstSlash = remainder.IndexOfAny(new char[] { '/' });
            string url;
            string host = "";
            string port = "";
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
            int firstWhitespace = url.IndexOfAny(new char[] { '\t' });
            if (firstWhitespace != -1)
            {
                string hostport = url.Substring(firstWhitespace).Trim();
                url = url.Substring(0, firstWhitespace).Trim();
                int firstURLWhitespace = hostport.IndexOfAny(new char[] { '\t' });
                if (firstURLWhitespace != -1)
                {
                    host = hostport.Substring(0, firstURLWhitespace).Trim();
                    port = hostport.Substring(firstURLWhitespace).Trim();
                    url = $"{host}:{port}" + url;
                }
            }

            if (url.StartsWith("://", StringComparison.Ordinal))
                url = "gopher" + url;
            else if (url.StartsWith("//", StringComparison.Ordinal))
                url = "gopher:" + url;
            else if (!url.StartsWith("gopher://", StringComparison.Ordinal))
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
                    "body {font-family: 'Segoe UI Light', Tahoma, sans-serif; background: repeating-linear-gradient(45deg, black, transparent 100px);}" +
                    "h1, h2, h3, p {margin: 0;}" +
                    "pre {background: white; border-radius: 5px; padding: 10px;}" +
                    ".Content {background: whitesmoke; border-radius: 10px; margin: 50px; padding: 25px;}" +
                    ".Embed {background: white; border-radius: 5px; padding: 5px;}" +
                    "</style><body><div class=\"Content\">\r\n");
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

    public struct GopherResponse : GeminiGopherIResponse
    {
        public List<byte> Bytes { get; set; }
        public string Mime { get; set; }
        public string _Encoding { get; set; }
        public Uri _Uri { get; set; }

        public GopherResponse(List<byte> buffer, int bytes, Uri uri)
        {
            int pyldStart = 0;
            int pyldLen = bytes - pyldStart;
            byte[] metaraw = buffer.ToArray();
            Bytes = buffer.Skip(pyldStart).Take(pyldLen).ToList();
            Mime = "application/octet-stream";
            _Encoding = "UTF-8";
            _Uri = uri;
        }
    }

    public class Gopher
    {
        static GopherResponse ReadMessage(Stream _Stream, Uri URI, int axSize, int AbandonAfterSeconds)
        {
            byte[] Buffer = new byte[2048];
            int Bytes = -1;

            var AbandonTime = DateTime.Now.AddSeconds(AbandonAfterSeconds);
            var MaxSizeBytes = axSize * 1024;

            Bytes = _Stream.Read(Buffer, 0, Buffer.Length);
            GopherResponse Resp = new GopherResponse(Buffer.ToList(), Bytes, URI);

            while (Bytes != 0)
            {
                Bytes = _Stream.Read(Buffer, 0, Buffer.Length);
                Resp.Bytes.AddRange(Buffer.Take(Bytes));

                if (Resp.Bytes.Count > MaxSizeBytes)
                    throw new Exception("Abort due to resource exceeding max size (" + axSize + "Kb)");
                if (DateTime.Now >= AbandonTime)
                    throw new Exception("Abort due to resource exceeding time limit (" + AbandonAfterSeconds + " seconds)");
            }
            return Resp;
        }

        private static string GetMime(Uri URI)//, string GopherType
        {
            var Response = "text/html";//"application/octet-stream";
            var ExtensionFull = Path.GetExtension(URI.AbsolutePath);
            string Extension = (ExtensionFull.Length > 0) ? ExtensionFull.Substring(1) : "";
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
                    /*case "pdf":
                        res = "application/pdf";
                        break;
                    case "doc":
                        res = "application/msword";
                        break;
                    case "ps":
                        res = "application/postscript";
                        break;

                    case "zip":
                        res = "application/zip";
                        break;*/
                    default:
                        Response = $"application/{Extension}";
                        break;
                }
            }
            /*switch (GopherType)
            {
                case "H":
                    res = "text/plain";
                    break;
            }*/
            //MessageBox.Show(GopherType);
            return Response;
        }


        public static GeminiGopherIResponse Fetch(Uri HostURL, int AbandonReadSizeKb = 2048, int AbandonReadTimes = 5)
        {
            int Port = HostURL.Port;
            if (Port == -1)
                Port = 70;

            TcpClient _Client;
            try
            {
                _Client = new TcpClient(HostURL.Host, Port);
            }
            catch
            {
                return null;
            }

            Stream Stream = _Client.GetStream();

            //var GopherType = "1";
            var TrimmedUrl = HostURL.AbsolutePath;

            if (HostURL.AbsolutePath.Length > 1)
            //{
                //GopherType = TrimmedUrl[1].ToString();
                TrimmedUrl = TrimmedUrl.Substring(1);
            //}

            byte[] Message = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(TrimmedUrl) + "\r\n");
            Stream.Write(Message, 0, Message.Count());
            Stream.Flush();
            GopherResponse Response = ReadMessage(Stream, HostURL, AbandonReadSizeKb, AbandonReadTimes);
            _Client.Close();

            Response.Mime = GetMime(HostURL);//, gopherType);

            return Response;
        }
    }
}
