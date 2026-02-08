using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace SLBr.Protocols
{
    public class TextGemini
    {
        private static string FormatLineAsTitle(string input)
        {
            int level = 0;
            string template = "<div><u>{0}</u></div>";

            foreach (char c in input.Take(3))
            {
                if (c == '#')
                    level += 1;
                else
                    break;
            }
            if (level == 0)
                return input;

            switch (level)
            {
                case 1:
                    template = "<h1>{0}</h1>";
                    break;
                case 2:
                    template = "<h2>{0}</h2>";
                    break;
                case 3:
                    template = "<h3>{0}</h3>";
                    break;
            }

            string heading = input.Substring(level).Trim();
            if (heading.EndsWith("<br/>"))
                heading = heading.Substring(0, heading.Length - "<br/>".Length);

            return string.Format(template, heading);
        }
        private static string FormatLineAsLink(string input, int count)
        {
            const string linkSym = "=&gt;";

            if (!input.StartsWith(linkSym)) { return input; }
            char[] whitespace = [' ', '\t'];

            string remainder = input.Substring(linkSym.Length).Trim();
            int firstWhitespace = remainder.IndexOfAny(whitespace);
            string url;
            string label;

            if (remainder.EndsWith("<br/>"))
            {
                remainder = remainder.Substring(0, remainder.Length - "<br/>".Length).Trim();
            }

            if (firstWhitespace == -1)
            {
                url = remainder;
                label = remainder;
            }
            else
            {
                url = remainder.Substring(0, firstWhitespace).Trim();
                label = remainder.Substring(firstWhitespace).Trim();
            }

            if (url.StartsWith("://")) { url = "gemini" + url; }
            else if (url.StartsWith("//")) { url = "gemini:" + url; }
            return $"<div><a href=\"{url}\">{label}</a></div>";
            //return $"<div>[{count}] <a href=\"{url}\">{label}</a></div>";
        }
        private static string FormatLineAsEmbed(string input)
        {
            if (!input.StartsWith("&gt; "))
                return input;
            string template = "<div class=\"embed\"><div style=\"display: inline-block; background: gray; width: 2px; height: 12.5px; margin-right: 5px; margin-left: 5px;\"></div>{0}</div>";
            input = input.Replace("&gt; ", string.Empty);
            return string.Format(template, input);
        }

        public static string NewFormat(GeminiGopherIResponse Response)//, bool IsRaw = false)
        {
            bool is_literal = false;
            string title = "";
            string input = Encoding.UTF8.GetString(Response.Bytes.ToArray());
            StringBuilder sb = new StringBuilder(@"<!DOCTYPE html>
<html>
<head>
    <style>
html {height: 100%;}
body {font-family: 'Segoe UI Light', Tahoma, sans-serif; background: white;}
h1, h2, h3 {margin: 0;}
pre {background: white; border-radius: 5px; padding: 10px;}
.content {background: whitesmoke; border-radius: 10px; margin: 50px; padding: 25px;}
.embed {background: white; border-radius: 5px; padding: 5px;}
    </style>
</head>
<body>
    <div class=""content"">
");
            foreach (char c in input)
            {
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
                    case '\n':
                        sb.Append("<br/>");
                        break;
                }
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

                    //if (!IsRaw)
                    //{
                    if (!is_literal)
                    {
                        lineout = FormatLineAsTitle(lineout);
                        if (string.IsNullOrEmpty(title) && lineout.StartsWith("<h1>"))
                            title = lineout.Substring(4, lineout.Length - 9);
                        lineout = FormatLineAsEmbed(lineout);
                        if (lineout.StartsWith("=&gt;"))
                            LinkCount++;
                        lineout = FormatLineAsLink(lineout, LinkCount);
                    }
                    else
                        lineout = lineout.Replace("<br/>", string.Empty);
                    if (line.StartsWith("```"))
                    {
                        is_literal = !is_literal;

                        if (is_literal)
                            lineout = "<pre>";
                        else
                            lineout = "</pre>";
                    }
                    //}
                    output.WriteLine(lineout);
                }
            }

            string Result = output.ToString();
            if (!string.IsNullOrEmpty(title))
                Result = Result.Replace("</head>", $"<title>{WebUtility.HtmlEncode(title)}</title>\r\n</head>");
            return Result;
        }
    }
    public class WebSSLStatus
    {
        public X509Certificate2 X509Certificate { get; set; }
        public SslPolicyErrors PolicyErrors { get; set; }
        public bool IsSecure => X509Certificate != null && PolicyErrors == SslPolicyErrors.None;
    }
    public interface GeminiGopherIResponse
    {
        List<byte> Bytes { get; }
        string Mime { get; }
        Uri _Uri { get; }
        string _Encoding { get; }
        WebSSLStatus SSLStatus { get; }
    }

    public class GeminiResponse : GeminiGopherIResponse
    {
        public char CodeMajor;
        public char CodeMinor;
        public string Meta;
        public Uri _Uri { get; set; }
        public List<byte> Bytes { get; set; } = new List<byte>();
        public string Mime { get; set; } = "text/gemini";
        public string _Encoding { get; set; } = "UTF-8";
        public WebSSLStatus SSLStatus { get; set; }

        public async Task SetResponseHeader(Stream _Stream)
        {
            byte[] StatusText = { (byte)'4', (byte)'1' };
            await _Stream.ReadAsync(StatusText, 0, 2);
            //if (await _Stream.ReadAsync(StatusText, 0, 2) != 2)
            //    throw new Exception("Malformed Gemini response (no status)");

            var Status = Encoding.UTF8.GetChars(StatusText);
            CodeMajor = Status[0];
            CodeMinor = Status[1];

            int Space = _Stream.ReadByte();
            if (Space != ' ')
                return;
            //throw new Exception("Malformed Gemini response (missing space)");

            List<byte> MetaBuffer = [];
            int Byte;
            while ((Byte = _Stream.ReadByte()) != -1)
            {
                if (Byte == '\r')
                {
                    _Stream.ReadByte();
                    break;
                }
                MetaBuffer.Add((byte)Byte);
            }
            Meta = Encoding.UTF8.GetString(MetaBuffer.ToArray());
        }

        public override string ToString() =>
            $"{CodeMajor}{CodeMinor}: {Meta}";
    }
    public class Gemini
    {
        static GeminiResponse ErrorResponse(Uri _Uri, char Major, char Minor, string Message)
        {
            return new GeminiResponse
            {
                CodeMajor = Major,
                CodeMinor = Minor,
                Meta = Message,
                _Uri = _Uri,
                Mime = "text/plain",
                SSLStatus = new WebSSLStatus() { PolicyErrors = SslPolicyErrors.None },
                Bytes = Encoding.UTF8.GetBytes($"{Major}{Minor}: {Message}").ToList()
            };
        }

        static async Task ReadMessage(GeminiResponse Response, SslStream _Stream, int MaxSize, int AbandonAfterSeconds)
        {
            await Response.SetResponseHeader(_Stream);
            if (Response.CodeMajor == '4' || Response.CodeMajor == '5')
                return;
            DateTime AbandonTime = DateTime.Now.AddSeconds(AbandonAfterSeconds);

            byte[] Buffer = new byte[2048];
            int Bytes = await _Stream.ReadAsync(Buffer);
            var MaxSizeBytes = MaxSize * 1024;
            while (Bytes != 0)
            {
                Response.Bytes.AddRange(Buffer.Take(Bytes));
                Bytes = await _Stream.ReadAsync(Buffer);

                if (Response.Bytes.Count > MaxSizeBytes)
                {
                    Response.CodeMajor = '4';
                    Response.CodeMinor = '3';
                    Response.Meta = "Resource too large";
                    break;
                }
                //throw new Exception("Abort due to resource exceeding max size (" + MaxSize + "Kb)");
                else if (DateTime.Now >= AbandonTime)
                {
                    Response.CodeMajor = '4';
                    Response.CodeMinor = '4';
                    Response.Meta = "Read timeout";
                    break;
                }
                //throw new Exception("Abort due to resource exceeding time limit (" + AbandonAfterSeconds + " seconds)");
            }
        }
        public static async Task<GeminiGopherIResponse> Fetch(Uri HostURL, X509Certificate2 ClientCertificate = null, string Proxy = "", bool Insecure = false, int AbandonReadSizeKb = 2048, int AbandonReadTimes = 5)
        {
            int RefetchCount = 0;
        Refetch:
            if (RefetchCount >= 5)
                return ErrorResponse(HostURL, '5', '1', "Too many redirects");
            RefetchCount += 1;

            var ServerHost = HostURL.Host;
            int Port = HostURL.Port;
            if (Port == -1)
                Port = 1965;

            if (Proxy.Length > 0)
            {
                string[] ProxyValues = Proxy.Split(':');
                ServerHost = ProxyValues[0];
                Port = int.Parse(ProxyValues[1]);
            }
            TcpClient _Client = new TcpClient();
            try
            {
                await _Client.ConnectAsync(ServerHost, Port);
            }
            catch (SocketException ex)
            {
                return ErrorResponse(HostURL, '5', '1', $"Network error: {ex.Message}");
            }

            WebSSLStatus SSLStatus = new WebSSLStatus();

            RemoteCertificateValidationCallback _Callback = (Sender, Certificate, Chain, Errors) =>
            {
                if (Certificate is X509Certificate2 Certificate2)
                    SSLStatus.X509Certificate = new X509Certificate2(Certificate2);
                SSLStatus.PolicyErrors = Errors;

                if (Insecure)
                    return true;
                if (Errors == SslPolicyErrors.None)
                    return true;

                DateTime ExpirationDate = DateTime.Parse(Certificate.GetExpirationDateString());
                if (ExpirationDate < DateTime.Now)
                    return false;

                return Errors == SslPolicyErrors.RemoteCertificateChainErrors;
            };
            SslStream SSLStream = new SslStream(_Client.GetStream(), false, _Callback, null);

            var Certificates = new X509CertificateCollection();
            if (ClientCertificate != null)
                Certificates.Add(ClientCertificate);

            try
            {
                await SSLStream.AuthenticateAsClientAsync(ServerHost, Certificates, SslProtocols.Tls12, !Insecure);
            }
            catch (AuthenticationException ex)
            {
                return ErrorResponse(HostURL, '5', '9', $"TLS authentication failed: {ex.Message}");
            }


            GeminiResponse Response = new GeminiResponse() { _Uri = HostURL, SSLStatus = SSLStatus };
            byte[] Message = Encoding.UTF8.GetBytes(HostURL.AbsoluteUri + "\r\n");
            try
            {
                SSLStream.ReadTimeout = AbandonReadTimes * 1000;

                await SSLStream.WriteAsync(Message, 0, Message.Count());
                await SSLStream.FlushAsync();
                await ReadMessage(Response, SSLStream, AbandonReadSizeKb, AbandonReadTimes);
            }
            catch
            {
                SSLStream.Close();
                _Client.Close();
                //throw new Exception(Error.Message);
            }
            finally
            {
                SSLStream.Close();
                _Client.Close();
                SSLStream.Dispose();
                _Client.Dispose();
            }

            switch (Response.CodeMajor)
            {
                case '1'://Input required
                    break;
                case '2'://Success
                    Response.Mime = Response.Meta;
                    break;
                case '3'://Redirect
                    Uri redirectUri;
                    redirectUri = Response.Meta.Contains("://") ? new Uri(Response.Meta) : new Uri(HostURL, Response.Meta);
                    //if (redirectUri.Scheme != HostURL.Scheme)
                    //    throw new Exception("Cannot redirect to a URI with a different scheme: " + redirectUri.Scheme);
                    HostURL = redirectUri;
                    goto Refetch;
                case '4'://Temporary failure
                case '5'://Permanent failure
                case '6'://Client certificate required
                    if (Response.Bytes.Count == 0)
                        Response.Bytes = Encoding.UTF8.GetBytes(Response.ToString()).ToList();
                    break;
                //default:
                //    throw new Exception(string.Format("Invalid response code {0}", resp.CodeMajor));
            }

            return Response;
        }
    }
}