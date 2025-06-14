using CefSharp;
using CefSharp.DevTools.IO;
using SLBr.Protocols;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace SLBr.Handlers
{
    /*public class IPFSSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipfs://", "https://cloudflare-ipfs.com/ipfs/"));

                    var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    var receiveStream = httpWebResponse.GetResponseStream();
                    var mime = httpWebResponse.ContentType;

                    var stream = new MemoryStream();
                    receiveStream.CopyTo(stream);
                    httpWebResponse.Close();

                    stream.Position = 0;
                    ResponseLength = stream.Length;
                    MimeType = mime;
                    StatusCode = (int)HttpStatusCode.OK;
                    Stream = stream;

                    callback.Continue();
                }
            });
            return CefReturnValue.ContinueAsync;
        }
    }
    public class IPNSSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipns://", "https://cloudflare-ipfs.com/ipns/"));

                    var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    var receiveStream = httpWebResponse.GetResponseStream();
                    var mime = httpWebResponse.ContentType;

                    var stream = new MemoryStream();
                    receiveStream.CopyTo(stream);
                    httpWebResponse.Close();

                    stream.Position = 0;
                    ResponseLength = stream.Length;
                    MimeType = mime;
                    StatusCode = (int)HttpStatusCode.OK;
                    Stream = stream;

                    callback.Continue();
                }
            });
            return CefReturnValue.ContinueAsync;
        }
    }*/
    public class GeminiSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            GeminiGopherIResponse Response = Gemini.Fetch(new Uri(Utils.CleanUrl(request.Url)));
            if (Response != null)
            {
                bool Raw = request.Url.EndsWith("?raw=true");
                Stream = new MemoryStream(Raw ? Response.Bytes.ToArray() : Encoding.UTF8.GetBytes(TextGemini.NewFormat(Response)));

                MimeType = Raw ? "text/plain" : Response.Mime.Contains("text/gemini") ? "text/html" : Response.Mime;

                callback.Continue();
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
    public class GopherSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            GeminiGopherIResponse Response = Gopher.Fetch(new Uri(Utils.CleanUrl(request.Url)));
            if (Response != null)
            {
                //bool Raw = request.Url.EndsWith("?raw=true");
                Stream = new MemoryStream(/*Raw ? Response.Bytes.ToArray() : */Encoding.UTF8.GetBytes(TextGopher.NewFormat(Response)));

                MimeType = /*Raw ? "text/plain" : */Response.Mime.Contains("application/gopher-menu") ? "text/html" : Response.Mime;

                callback.Continue();
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
}
