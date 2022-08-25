using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr.Protocols
{
    public class TextGemini
    {
        bool is_literal = false;
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
            char[] whitespace = new char[] { ' ', '\t' };

            string remainder = input.Substring(linkSym.Length).Trim();
            int firstWhitespace = remainder.IndexOfAny(whitespace);
            string url;
            string label;

            // Remove newlines
            if (remainder.EndsWith("<br/>"))
            {
                remainder = remainder.Substring(0, remainder.Length - "<br/>".Length).Trim();
            }

            // Seperate into URL and label
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

            // Default protocol is "gemini"
            if (url.StartsWith("://")) { url = "gemini" + url; }
            else if (url.StartsWith("//")) { url = "gemini:" + url; }

            return $"<div>[{count}] <a href=\"{url}\">{label}</a></div>";
        }
        private static string FormatLineAsEmbed(string input)
        {
            if (!input.StartsWith("&gt; "))
                return input;

            string template = "<div class=\"Embed\"><div style=\"display: inline-block; background: gray; width: 2px; height: 12.5px; margin-right: 5px; margin-left: 5px;\"></div>{0}</div>";

            input = input.Replace("&gt; ", "");

            return string.Format(template, input);
        }

        public static string NewFormat(byte[] rawinput, bool IsRaw = false)
        {
            bool is_literal = false;

            string input = Encoding.UTF8.GetString(rawinput);
            StringBuilder sb = new StringBuilder("<!DOCTYPE html><html>\r\n");
            sb.Append("<style>" +
                "body {font-family: 'Segoe UI Light', Tahoma, sans-serif; background: repeating-linear-gradient(45deg, black, transparent 100px);}" +
                "h1, h2, h3 {margin: 0;}" +
                "pre {background: white; border-radius: 10px; padding: 10px;}" +
                ".Content {background: whitesmoke; border-radius: 10px; margin: 50px; padding: 25px;}" +
                ".Embed {background: white; border-radius: 10px; padding: 5px;}" +
                "</style><body><div class=\"Content\">\r\n");
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

                    if (!IsRaw)
                    {
                        if (!is_literal)
                        {
                            lineout = FormatLineAsTitle(lineout);
                            lineout = FormatLineAsEmbed(lineout);
                            //MessageBox.Show(lineout);
                            if (lineout.StartsWith("=&gt;"))
                            {
                                //    lineout = lineout.Replace("=>", $"[{LinkCount}]");
                                LinkCount++;
                            }
                            lineout = FormatLineAsLink(lineout, LinkCount);
                        }
                        else
                            lineout = lineout.Replace("<br/>", "");
                        if (line.StartsWith("```"))
                        {
                            is_literal = !is_literal;

                            if (is_literal)
                                lineout = "<pre>";
                            else
                                lineout = "</pre>";
                        }
                    }
                    output.WriteLine(lineout);
                }
            }

            return output.ToString();
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
            if (statusBytes != 2)
                throw new Exception("malformed Gemini response - no status");

            var status = Encoding.UTF8.GetChars(statusText);
            CodeMajor = status[0];
            CodeMinor = status[1];

            byte[] space = { 0 };
            var spaceBytes = responseStream.Read(space, 0, 1);
            if (spaceBytes != 1 || space[0] != (byte)' ')
                throw new Exception("malformed Gemini response - missing space after status");

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
                    if (currentChar != (byte)'\n')
                        throw new Exception("malformed Gemini header - missing LF after CR");
                    break;
                }
                metaBuffer.Add(currentChar);
            }

            var meta = Encoding.UTF8.GetString(metaBuffer.ToArray());

            this.Meta = meta;
            Bytes = new List<byte>();
            this.Mime = "text/gemini";
            this._Encoding = "UTF-8";
            this._Uri = uri;

        }
        public override string ToString()
        {
            return $"{CodeMajor}{CodeMinor}: {Meta}";
        }
    }
    public class Gemini
    {
        private static Hashtable certificateErrors = new Hashtable();
        const int DefaultPort = 1965;
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
        static GeminiResponse ReadMessage(SslStream sslStream, Uri uri, int maxSize, int abandonAfterSeconds)
        {
            byte[] buffer = new byte[2048];
            int bytes = -1;

            var abandonTime = DateTime.Now.AddSeconds((double)abandonAfterSeconds);
            GeminiResponse resp = new GeminiResponse(sslStream, uri);

            bytes = sslStream.Read(buffer, 0, buffer.Length);
            var maxSizeBytes = maxSize * 1024;
            while (bytes != 0)
            {
                resp.Bytes.AddRange(buffer.Take(bytes));
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                if (resp.Bytes.Count > maxSizeBytes)
                    throw new Exception("Abort due to resource exceeding max size (" + maxSize + "Kb)");
                if (DateTime.Now >= abandonTime)
                    throw new Exception("Abort due to resource exceeding time limit (" + abandonAfterSeconds + " seconds)");
            }
            return resp;
        }
        public static GeminiGopherIResponse Fetch(Uri HostURL, X509Certificate2 clientCertificate = null, string proxy = "", bool insecure = false, int abandonReadSizeKb = 2048, int abandonReadTimeS = 5)
        {
            int refetchCount = 0;
        Refetch:
            if (refetchCount >= 5)
                throw new Exception(string.Format("Too many redirects!"));
            refetchCount += 1;

            var ServerHost = HostURL.Host;
            int Port = HostURL.Port;
            if (Port == -1) { Port = DefaultPort; }

            if (proxy.Length > 0)
            {
                var proxySplit = proxy.Split(':');
                ServerHost = proxySplit[0];
                Port = int.Parse(proxySplit[1]);
            }

            TcpClient client;
            try
            {
                client = new TcpClient(ServerHost, Port);
            }
            catch (Exception e)
            {
                return null;
            }

            RemoteCertificateValidationCallback callback = new RemoteCertificateValidationCallback(ValidateServerCertificate);
            if (insecure)
                callback = new RemoteCertificateValidationCallback(AlwaysAccept);

            SslStream sslStream = new SslStream(client.GetStream(), false, callback, null);

            var certs = new X509CertificateCollection();
            if (clientCertificate != null)
                certs.Add(clientCertificate);

            sslStream.AuthenticateAsClient(ServerHost, certs, SslProtocols.Tls12, !insecure);

            byte[] messsage = Encoding.UTF8.GetBytes(HostURL.AbsoluteUri + "\r\n");
            GeminiResponse resp = new GeminiResponse();
            try
            {
                sslStream.ReadTimeout = abandonReadTimeS * 1000;

                sslStream.Write(messsage, 0, messsage.Count());
                sslStream.Flush();
                resp = ReadMessage(sslStream, HostURL, abandonReadSizeKb, abandonReadTimeS);
            }
            catch (Exception err)
            {
                sslStream.Close();
                client.Close();
                throw new Exception(err.Message);
            }
            finally
            {
                sslStream.Close();
                client.Close();

                sslStream.Dispose();
                client.Dispose();
            }

            switch (resp.CodeMajor)
            {
                case '1': // Text input
                    break;
                case '2': // OK
                    resp.Mime = resp.Meta;      //set the mime as the meta response **TBD parse this into media type/encoding etc
                    break;
                case '3': // Redirect
                    Uri redirectUri;
                    if (resp.Meta.Contains("://"))
                        redirectUri = new Uri(resp.Meta);
                    else
                        redirectUri = new Uri(HostURL, resp.Meta);
                    if (redirectUri.Scheme != HostURL.Scheme)
                        throw new Exception("Cannot redirect to a URI with a different scheme: " + redirectUri.Scheme);
                    HostURL = redirectUri;

                    goto Refetch;
                case '4': // Temporary failure
                case '5': // Permanent failure
                case '6': // Client cert required
                    resp.Bytes = Encoding.UTF8.GetBytes(resp.ToString()).ToList();
                    break;
                default:
                    throw new Exception(string.Format("Invalid response code {0}", resp.CodeMajor));
            }

            return resp;
        }
    }
}
