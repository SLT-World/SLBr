using CefSharp;
using SLBr.Protocols;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Handlers
{
    public class WaybackSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            if (bool.Parse(MainWindow.Instance.MainSave.Get("Wayback")))
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        try
                        {
                            string Year = request.Url.Contains("year=") ? Utils.Between(request.Url, "year=", "&") : "2000";
                            string Url = request.Url.Replace($"#year={Year}", "").Replace($"?year={Year}", "").Replace($"&year={Year}", "");
                            var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url.Replace("wayback://", $"http://theoldnet.com/get?year={Year}&noscripts=true&decode=true&url="));

                            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                            // Get the stream associated with the response.
                            var receiveStream = httpWebResponse.GetResponseStream();
                            var mime = httpWebResponse.ContentType;

                            var stream = new MemoryStream();
                            receiveStream.CopyTo(stream);
                            httpWebResponse.Close();

                            //Reset the stream position to 0 so the stream can be copied into the underlying unmanaged buffer
                            stream.Position = 0;

                            //Populate the response values - No longer need to implement GetResponseHeaders (unless you need to perform a redirect)
                            ResponseLength = stream.Length;
                            MimeType = mime.Replace("; charset=iso-8859-1", "");
                            StatusCode = (int)HttpStatusCode.OK;
                            Stream = stream;
                        }
                        catch { }

                        callback.Continue();
                    }
                });
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
    public class IPFSSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            if (bool.Parse(MainWindow.Instance.MainSave.Get("IPFS")))
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        try
                        {//https://cloudflare-ipfs.com/ipfs/
                            var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipfs://", "https://cf-ipfs.com/ipfs/"));

                            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                            // Get the stream associated with the response.
                            var receiveStream = httpWebResponse.GetResponseStream();
                            var mime = httpWebResponse.ContentType;

                            var stream = new MemoryStream();
                            receiveStream.CopyTo(stream);
                            httpWebResponse.Close();

                            //Reset the stream position to 0 so the stream can be copied into the underlying unmanaged buffer
                            stream.Position = 0;

                            //Populate the response values - No longer need to implement GetResponseHeaders (unless you need to perform a redirect)
                            ResponseLength = stream.Length;
                            MimeType = mime;
                            StatusCode = (int)HttpStatusCode.OK;
                            Stream = stream;
                        }
                        catch { }

                        callback.Continue();
                    }
                });
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
    public class IPNSSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            if (bool.Parse(MainWindow.Instance.MainSave.Get("IPFS")))
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        try
                        {
                            var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipns://", "https://cf-ipfs.com/ipns/"));

                            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                            // Get the stream associated with the response.
                            var receiveStream = httpWebResponse.GetResponseStream();
                            var mime = httpWebResponse.ContentType;

                            var stream = new MemoryStream();
                            receiveStream.CopyTo(stream);
                            httpWebResponse.Close();

                            //Reset the stream position to 0 so the stream can be copied into the underlying unmanaged buffer
                            stream.Position = 0;

                            //Populate the response values - No longer need to implement GetResponseHeaders (unless you need to perform a redirect)
                            ResponseLength = stream.Length;
                            MimeType = mime;
                            StatusCode = (int)HttpStatusCode.OK;
                            Stream = stream;
                        }
                        catch { }

                        callback.Continue();
                    }
                });
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
    public class GeminiSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            if (bool.Parse(MainWindow.Instance.MainSave.Get("Gemini")))
            {
                var uri = new Uri(Utils.CleanUrl(request.Url));
                //try
                //{
                GeminiGopherIResponse resp = Gemini.Fetch(uri);
                if (resp != null)
                {
                    //MessageBox.Show(GeminiSchemeHandlerFactory.Instance.TextGeminiInstance.Format(resp.Bytes.ToArray()));
                    //Stream = new MemoryStream(Encoding.UTF8.GetBytes(GeminiSchemeHandlerFactory.Instance.TextGeminiInstance.Format(resp.Bytes.ToArray())));
                    string Html = "";
                    string NoParameterAddress = Utils.CleanUrl(request.Url, true, false, true, true);
                    if (NoParameterAddress.EndsWith(".gmi") || NoParameterAddress.EndsWith("/") || NoParameterAddress.Substring(NoParameterAddress.LastIndexOf('/') + 1).CountChars('.') == 0)
                        Html = TextGemini.NewFormat(resp.Bytes.ToArray(), request.Url.EndsWith("?raw=true"));
                    else
                        Html = TextGemini.DirectlyToString(resp.Bytes.ToArray());
                    //if (NoParameterAddress.EndsWith(".html") || NoParameterAddress.EndsWith(".txt") || NoParameterAddress.EndsWith(".xml") || NoParameterAddress.EndsWith(".htm") || NoParameterAddress.EndsWith(".css"))
                    //    Html = TextGemini.DirectlyToString(resp.Bytes.ToArray());
                    //else
                    //    Html = TextGemini.NewFormat(resp.Bytes.ToArray(), request.Url.EndsWith("?raw=true"));
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(Html));

                    MimeType = resp.Mime.Contains("text/gemini") ? "text/html" : resp.Mime;

                    callback.Continue();
                    return CefReturnValue.ContinueAsync;
                }
            }
            //}
            //catch
            //{
            callback.Dispose();
            return CefReturnValue.Cancel;
            //}
        }
    }
    public class GopherSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            if (bool.Parse(MainWindow.Instance.MainSave.Get("Gopher")))
            {
                var uri = new Uri(Utils.CleanUrl(request.Url));
                //try
                //{
                GeminiGopherIResponse resp = Gopher.Fetch(uri);
                if (resp != null)
                {
                    //MessageBox.Show(GeminiSchemeHandlerFactory.Instance.TextGeminiInstance.Format(resp.Bytes.ToArray()));
                    //Stream = new MemoryStream(Encoding.UTF8.GetBytes(GeminiSchemeHandlerFactory.Instance.TextGeminiInstance.Format(resp.Bytes.ToArray())));
                    Stream = new MemoryStream(resp.Bytes.ToArray());

                    MimeType = resp.Mime.Contains("application/gopher-menu") ? "text/html" : resp.Mime;

                    callback.Continue();
                    return CefReturnValue.ContinueAsync;
                }
            }
            //}
            //catch
            //{
            callback.Dispose();
            return CefReturnValue.Cancel;
            //}
        }
    }
}
