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

    public interface GeminiGopherIResponse
    {
        List<byte> Bytes { get; }
        string Mime { get; }
        Uri _Uri { get; }
        string _Encoding { get; }
    }
    public struct GeminiResponse : GeminiGopherIResponse
    {
        public char CodeMajor;
        public char CodeMinor;
        public string Meta;
        public Uri _Uri { get; set; }
        public List<byte> Bytes { get; set; }
        public string Mime { get; set; }
        public string _Encoding { get; set; }

        public GeminiResponse(Stream responseStream, Uri uri)
        {
            byte[] statusText = { (byte)'4', (byte)'1' };
            var statusBytes = responseStream.Read(statusText, 0, 2);
            //if (statusBytes != 2)
            //    throw new Exception("malformed Gemini response - no status");

            var status = Encoding.UTF8.GetChars(statusText);
            CodeMajor = status[0];
            CodeMinor = status[1];

            byte[] space = { 0 };
            var spaceBytes = responseStream.Read(space, 0, 1);
            //if (spaceBytes != 1 || space[0] != (byte)' ')
            //    throw new Exception("malformed Gemini response - missing space after status");

            List<byte> metaBuffer = new List<byte>();
            byte[] tempMetaBuffer = { 0 };
            byte currentChar;
            while (responseStream.Read(tempMetaBuffer, 0, 1) == 1)
            {
                currentChar = tempMetaBuffer[0];
                if (currentChar == (byte)'\r')
                {
                    responseStream.Read(tempMetaBuffer, 0, 1);
                    currentChar = tempMetaBuffer[0];
                    //if (currentChar != (byte)'\n')
                    //    throw new Exception("malformed Gemini header - missing LF after CR");
                    break;
                }
                metaBuffer.Add(currentChar);
            }

            Meta = Encoding.UTF8.GetString(metaBuffer.ToArray());
            Bytes = new List<byte>();
            Mime = "text/gemini";
            _Encoding = "UTF-8";
            _Uri = uri;
        }
        public override string ToString() =>
            $"{CodeMajor}{CodeMinor}: {Meta}";
    }
    public class Gemini
    {
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            var expireDate = DateTime.Parse(certificate.GetExpirationDateString());
            if (expireDate < DateTime.Now)
                return false;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
                return true;
            return false;
        }
        public static bool AlwaysAccept(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        static GeminiResponse ReadMessage(SslStream SSLStream, Uri URI, int MaxSize, int AbandonAfterSeconds)
        {
            byte[] Buffer = new byte[2048];
            int Bytes = -1;

            var AbandonTime = DateTime.Now.AddSeconds(AbandonAfterSeconds);
            GeminiResponse Response = new GeminiResponse(SSLStream, URI);

            Bytes = SSLStream.Read(Buffer, 0, Buffer.Length);
            var maxSizeBytes = MaxSize * 1024;
            while (Bytes != 0)
            {
                Response.Bytes.AddRange(Buffer.Take(Bytes));
                Bytes = SSLStream.Read(Buffer, 0, Buffer.Length);

                if (Response.Bytes.Count > maxSizeBytes)
                    return Response;
                //throw new Exception("Abort due to resource exceeding max size (" + MaxSize + "Kb)");
                if (DateTime.Now >= AbandonTime)
                    return Response;
                //throw new Exception("Abort due to resource exceeding time limit (" + AbandonAfterSeconds + " seconds)");
            }
            return Response;
        }
        public static GeminiGopherIResponse Fetch(Uri HostURL, X509Certificate2 ClientCertificate = null, string proxy = "", bool Insecure = false, int AbandonReadSizeKb = 2048, int AbandonReadTimes = 5)
        {
            int refetchCount = 0;
        Refetch:
            //if (refetchCount >= 5)
            //    throw new Exception(string.Format("Too many redirects!"));
            refetchCount += 1;

            var ServerHost = HostURL.Host;
            int Port = HostURL.Port;
            if (Port == -1)
                Port = 1965;

            if (proxy.Length > 0)
            {
                var proxySplit = proxy.Split(':');
                ServerHost = proxySplit[0];
                Port = int.Parse(proxySplit[1]);
            }

            TcpClient _Client;
            try
            {
                _Client = new TcpClient(ServerHost, Port);
            }
            catch { return null; }

            RemoteCertificateValidationCallback _Callback = new RemoteCertificateValidationCallback(ValidateServerCertificate);
            if (Insecure)
                _Callback = new RemoteCertificateValidationCallback(AlwaysAccept);

            SslStream SSLStream = new SslStream(_Client.GetStream(), false, _Callback, null);

            var certs = new X509CertificateCollection();
            if (ClientCertificate != null)
                certs.Add(ClientCertificate);

            SSLStream.AuthenticateAsClient(ServerHost, certs, SslProtocols.Tls12, !Insecure);

            byte[] Message = Encoding.UTF8.GetBytes(HostURL.AbsoluteUri + "\r\n");
            GeminiResponse resp = new GeminiResponse();
            try
            {
                SSLStream.ReadTimeout = AbandonReadTimes * 1000;

                SSLStream.Write(Message, 0, Message.Count());
                SSLStream.Flush();
                resp = ReadMessage(SSLStream, HostURL, AbandonReadSizeKb, AbandonReadTimes);
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

            switch (resp.CodeMajor)
            {
                case '1':
                    break;
                case '2':
                    resp.Mime = resp.Meta;
                    break;
                case '3':
                    Uri redirectUri;
                    redirectUri = resp.Meta.Contains("://") ? new Uri(resp.Meta) : new Uri(HostURL, resp.Meta);
                    //if (redirectUri.Scheme != HostURL.Scheme)
                    //    throw new Exception("Cannot redirect to a URI with a different scheme: " + redirectUri.Scheme);
                    HostURL = redirectUri;
                    goto Refetch;
                case '4':
                case '5':
                case '6':
                    resp.Bytes = Encoding.UTF8.GetBytes(resp.ToString()).ToList();
                    break;
                //default:
                //    throw new Exception(string.Format("Invalid response code {0}", resp.CodeMajor));
            }

            return resp;
        }
    }
}