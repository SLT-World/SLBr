using CefSharp;
using CefSharp.Callback;
using System;
using System.IO;
using System.Net;

namespace SLBr
{
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
