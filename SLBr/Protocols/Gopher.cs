using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SLBr.Protocols
{
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
            this.Bytes = buffer.Skip(pyldStart).Take(pyldLen).ToList();

            this.Mime = "application/octet-stream";
            this._Encoding = "UTF-8";
            this._Uri = uri;
        }

    }

    public class Gopher
    {
        const int DefaultPort = 70;

        static GopherResponse ReadMessage(Stream stream, Uri uri, int maxSize, int abandonAfterSeconds)
        {
            byte[] buffer = new byte[2048];
            int bytes = -1;

            var abandonTime = DateTime.Now.AddSeconds((double)abandonAfterSeconds);
            var maxSizeBytes = maxSize * 1024;      //Kb to Bytes

            bytes = stream.Read(buffer, 0, buffer.Length);
            GopherResponse resp = new GopherResponse(buffer.ToList(), bytes, uri);

            while (bytes != 0)
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
                resp.Bytes.AddRange(buffer.Take(bytes));

                if (resp.Bytes.Count > maxSizeBytes)
                {
                    throw new Exception("Abort due to resource exceeding max size (" + maxSize + "Kb)");
                }

                if (DateTime.Now >= abandonTime)
                {
                    throw new Exception("Abort due to resource exceeding time limit (" + abandonAfterSeconds + " seconds)");
                }
            }

            return resp;
        }

        private static string GetMime(Uri uri, string gopherType)
        {
            var res = "application/octet-stream";
            var ext = Path.GetExtension(uri.AbsolutePath);

            if (ext.Length > 0)
            {
                ext = ext.Substring(1);
            }
            else
            {
                ext = "";
            }

            switch (gopherType)
            {
                case "1":
                case "7":
                    res = "application/gopher-menu";
                    break;
                case "0":
                case "3":
                    res = "text/plain";
                    break;
                case "h":
                    res = "text/html";
                    break;
                case "g":
                    res = "image/gif";
                    break;
                case "4":
                case "5":
                    //binhex and dos binaries
                    break;
                case "I":
                case "s":
                case "d":
                case "9":
                    switch (ext)
                    {
                        case "jpg":
                            res = "image/jpeg";
                            break;
                        case "gif":
                        case "png":
                        case "bmp":
                        case "jpeg":
                            res = "image/" + ext;   //for these, the extension is same as sub type
                            break;
                        case "mp3":
                            res = "audio/mpeg";
                            break;
                        case "wav":
                        case "ogg":
                        case "flac":
                            res = "audio/" + ext;   //for these, the extension is same as sub type
                            break;
                        case "pdf":
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
                            break;

                    }
                    break;
            }

            return (res);
        }


        public static GeminiGopherIResponse Fetch(Uri hostURL, int abandonReadSizeKb = 2048, int abandonReadTimeS = 5)
        {
            int port = hostURL.Port;
            if (port == -1) { port = DefaultPort; }

            TcpClient client;
            try
            {
                client = new TcpClient(hostURL.Host, port);
            }
            catch// (Exception e)
            {
                //throw e;
                return null;
            }

            Stream stream = client.GetStream();

            var gopherType = "1";
            var trimmedUrl = hostURL.AbsolutePath;

            if (hostURL.AbsolutePath.Length > 1)
            {
                gopherType = trimmedUrl[1].ToString();      //e.g. extract "0" from "/0foo" or "/0/bar"

                //remove first two parts
                trimmedUrl = trimmedUrl.Substring(2);
            }

            var usePath = Uri.UnescapeDataString(trimmedUrl);        //we need to unescape any escaped characters like %20 back to space etc

            byte[] messsage = Encoding.UTF8.GetBytes(usePath + "\r\n");
            stream.Write(messsage, 0, messsage.Count());
            stream.Flush();
            GopherResponse resp = ReadMessage(stream, hostURL, abandonReadSizeKb, abandonReadTimeS);
            client.Close();

            resp.Mime = GetMime(hostURL, gopherType);

            return resp;
        }
    }
}
