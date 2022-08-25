using CefSharp;
using CefSharp.Callback;
using SLBr.Protocols;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
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

                        callback.Continue();
                    }
                });
                return CefReturnValue.ContinueAsync;
            }
            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }
    public class WeblightSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://googleweblight.com/?lite_url=google.com");//request.Url.Replace("weblight://", 
                    httpWebRequest.Method = "GET";
                    //httpWebRequest.ContentType = "application/x-www-form-urlencoded";
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

                    callback.Continue();
                }
            });
            return CefReturnValue.ContinueAsync;
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
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipfs://", "https://cloudflare-ipfs.com/ipfs/"));

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
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url.Replace("ipns://", "https://cloudflare-ipfs.com/ipns/"));

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
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(TextGemini.NewFormat(resp.Bytes.ToArray(), request.Url.EndsWith("?raw=true"))));

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
    public class BlankSchemeHandler : IResourceHandler, IDisposable
    {
        //private static string appPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\";

        //private string mimeType;
        //private Stream stream;

        public void Cancel()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            //responseLength = stream != null ? stream.Length : 0;
            responseLength = 0;
            redirectUrl = null;

            //response.StatusCode = (int)HttpStatusCode.OK;
            //response.StatusText = "OK";
            //response.MimeType = mimeType;
        }

        public bool Open(IRequest request, out bool handleRequest, ICallback callback)
        {
            //uri = new Uri(request.Url);
            //fileName = uri.AbsolutePath;

            /*Task.Factory.StartNew(() => {
                using (callback)
                {
                    FileStream fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                    mimeType = ResourceHandler.GetMimeType(Path.GetExtension(fileName));
                    stream = fStream;
                    callback.Continue();
                }
            });*/

            callback.Dispose();

            handleRequest = true;
            return false;
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            return false;
        }

        public bool Read(Stream dataOut, out int bytesRead, IResourceReadCallback callback)
        {
            bytesRead = -1;
            return false;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            callback.Dispose();

            /*if (stream == null)
            {
                bytesRead = 0;
                return false;
            }*/
            bytesRead = 0;
            return false;

            //Data out represents an underlying buffer (typically 32kb in size).
            /*var buffer = new byte[dataOut.Length];
            bytesRead = stream.Read(buffer, 0, buffer.Length);

            dataOut.Write(buffer, 0, buffer.Length);

            return bytesRead > 0;*/
        }

        public bool Skip(long bytesToSkip, out long bytesSkipped, IResourceSkipCallback callback)
        {
            bytesSkipped = -2;
            return false;
        }
    }
}
