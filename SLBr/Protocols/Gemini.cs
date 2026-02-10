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
        const string LinkSymbol = "=&gt;";
        const string BlockquoteSymbol = "&gt; ";
        private static string FormatTitle(string Input)
        {
            int Level = 0;
            string Template = "<div><u>{0}</u></div>";
            foreach (char Character in Input.Take(3))
            {
                if (Character == '#')
                    Level += 1;
                else
                    break;
            }
            if (Level == 0)
                return Input;
            switch (Level)
            {
                case 1:
                    Template = "<h1>{0}</h1>";
                    break;
                case 2:
                    Template = "<h2>{0}</h2>";
                    break;
                case 3:
                    Template = "<h3>{0}</h3>";
                    break;
            }
            string Heading = Input.Substring(Level).Trim();
            if (Heading.EndsWith("<br/>"))
                Heading = Heading.Substring(0, Heading.Length - "<br/>".Length);
            return string.Format(Template, Heading);
        }
        private static string FormatLink(string Input)
        {
            if (!Input.StartsWith(LinkSymbol))
                return Input;
            Input = Input.Substring(LinkSymbol.Length).Trim();
            if (Input.EndsWith("<br/>"))
                Input = Input[..^"<br/>".Length].Trim();
            int FirstWhitespace = Input.IndexOfAny([' ', '\t']);
            string Url = FirstWhitespace == -1 ? Input : Input[..FirstWhitespace].Trim();
            string Label = FirstWhitespace == -1 ? Input : Input[FirstWhitespace..].Trim();

            if (Url.StartsWith("://")) Url = "gemini" + Url;
            else if (Url.StartsWith("//")) Url = "gemini:" + Url;
            bool _IsSearch = IsSearch(Url, Label);
            if (_IsSearch)
                return $@"<div><a href=""#"" onclick=""geminiSearch('{WebUtility.HtmlEncode(Url)}')"">🔍 {WebUtility.HtmlEncode(Label)}</a></div>";
            return$@"<div><a href=""{WebUtility.HtmlEncode(Url)}"">{WebUtility.HtmlEncode(Label)}</a></div>";
        }
        private static string FormatEmbed(string Input)
        {
            if (!Input.StartsWith(BlockquoteSymbol))
                return Input;
            return $"<div class=\"embed\"><div></div>{Input.Substring(BlockquoteSymbol.Length).Trim()}</div>";
        }
        static bool IsSearch(string Url, string Label)
        {
            if (!string.IsNullOrEmpty(Path.GetExtension(Url)))
                return false;
            Label = Label.ToLowerInvariant();
            return Label.StartsWith("search") || Label.StartsWith("find") || Label.Contains(" search");
        }

        public static string NewFormat(GeminiGopherIResponse Response)
        {
            StringBuilder Builder = new StringBuilder(@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<style>
html {height: 100%;}
body {font-family: 'Segoe UI Light', Tahoma, sans-serif; background: white;}
h1, h2, h3 {margin: 0;}
pre {background: white; border-radius: 5px; padding: 10px;}
.content {background: whitesmoke; border-radius: 10px; margin: 50px; padding: 25px;}
.embed {background: white; border-radius: 5px; padding: 5px;}
.embed > div:first-child {display: inline-block; background: gray; width: 2px; height: 12.5px; margin-right: 5px; margin-left: 5px;}
ul {margin: 0;padding-left: 0;list-style-position: inside;}
</style>
<script>
function geminiSearch(url){let q=prompt(""Search query:"");q&&(window.location.href=url+""?""+encodeURIComponent(q))}
</script>
</head>
<body>
<div class=""content"">
");//WARNING: New line required
            bool IsLiteral = false;
            string Title = "";
            string Input = Encoding.UTF8.GetString(Response.Bytes.ToArray());
            foreach (char Character in Input)
            {
                switch (Character)
                {
                    case '<':
                        Builder.Append("&lt;");
                        continue;
                    case '>':
                        Builder.Append("&gt;");
                        continue;
                    case '\r':
                        continue;
                    case '\n':
                        Builder.Append("<br/>");
                        break;
                }
                Builder.Append(Character);
            }
            Builder.Append("</div></body></html>");

            StringWriter Output = new StringWriter();
            using (StringReader Reader = new StringReader(Regex.Replace(Builder.ToString(), @"[^\u0000-\u007F]+", string.Empty, RegexOptions.Compiled)))
            {
                string Line;
                bool InList = false;
                while ((Line = Reader.ReadLine()) != null)
                {
                    string Lineout = Line;
                    if (!IsLiteral)
                    {
                        if (Line.StartsWith("* "))
                        {
                            if (!InList)
                            {
                                Output.WriteLine("<ul>");
                                InList = true;
                            }
                            Output.WriteLine($"<li>{Line.Substring(2)}</li>");
                            continue;
                        }
                        else if (InList)
                        {
                            Output.WriteLine("</ul>");
                            InList = false;
                        }
                        Lineout = FormatTitle(Lineout);
                        if (string.IsNullOrEmpty(Title) && Lineout.StartsWith("<h1>"))
                            Title = Lineout.Substring(4, Lineout.Length - 9);
                        Lineout = FormatEmbed(Lineout);
                        Lineout = FormatLink(Lineout);
                    }
                    else
                        Lineout = Lineout.Replace("<br/>", string.Empty);
                    if (Line.StartsWith("```"))
                    {
                        if (InList)
                        {
                            Output.WriteLine("</ul>");
                            InList = false;
                        }
                        IsLiteral = !IsLiteral;
                        Lineout = IsLiteral ? "<pre>" : "</pre>";
                    }
                    Output.WriteLine(Lineout);
                }
                if (InList)
                    Output.WriteLine("</ul>");
            }

            string Result = Output.ToString();
            if (!string.IsNullOrEmpty(Title))
                Result = Result.Replace("</head>", $"<title>{WebUtility.HtmlEncode(Title)}</title></head>");
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
        int StatusCode { get; }
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
        public int StatusCode { get; set; } = 200;
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
        static GeminiResponse ErrorResponse(Uri _Uri, char Major, char Minor, string Message, int StatusCode)
        {
            return new GeminiResponse
            {
                CodeMajor = Major,
                CodeMinor = Minor,
                Meta = Message,
                _Uri = _Uri,
                StatusCode = StatusCode,
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
                    Response.Meta = "Request timeout";
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
                /*case '1'://Input required
                    break;*/
                case '2'://Success
                    Response.Mime = Response.Meta;
                    break;
                case '3'://Redirect
                    Uri RedirectUri = Response.Meta.Contains("://") ? new Uri(Response.Meta) : new Uri(HostURL, Response.Meta);
                    //if (redirectUri.Scheme != HostURL.Scheme)
                    //    throw new Exception("Cannot redirect to a URI with a different scheme: " + redirectUri.Scheme);
                    HostURL = RedirectUri;
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

    /*enum GeminiStatus
    {
        INPUT = 10,
        SENSITIVE_INPUT = 11,
        SUCCESS = 20,
        REDIRECT_TEMPORARY = 30,
        REDIRECT_PERMANENT = 31,
        TEMPORARY_FAILURE = 40,
        SERVER_UNAVAILABLE = 41,
        CGI_ERROR = 42,
        PROXY_ERROR = 43,
        SLOW_DOWN = 44,
        PERMANENT_FAILURE = 50,
        NOT_FOUND = 51,
        GONE = 52,
        PROXY_REQUEST_REFUSED = 53,
        BAD_REQUEST = 59,
        CLIENT_CERTIFICATE_REQUIRED = 60,
        CERTIFICATE_NOT_AUTHORISED = 61,
        CERTIFICATE_NOT_VALID = 62
    }*/
}