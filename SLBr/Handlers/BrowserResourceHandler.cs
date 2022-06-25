using CefSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
{
    public class HTTPSSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "https";

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new WebRequestResourceHandler();
        }
    }
    public class WebRequestResourceHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            //Spawn a Task and immediately return CefReturnValue.ContinueAsync
            /*Task.Run(async () =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url);

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
            });*/
            Task.Run(async () =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url);
                    httpWebRequest.UserAgent = request.Headers["User-Agent"];
                    httpWebRequest.Accept = request.Headers["Accept"];
                    httpWebRequest.Method = request.Method;
                    httpWebRequest.Referer = request.ReferrerUrl;

                    httpWebRequest.Headers.Add(request.Headers);
                    httpWebRequest.Headers.Remove("User-Agent");
                    httpWebRequest.Headers.Remove("Accept");

                    //var postData = request.PostData;

                    var httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse;

                    var receiveStream = httpWebResponse.GetResponseStream();

                    var contentType = new ContentType(httpWebResponse.ContentType);
                    var mimeType = contentType.MediaType;
                    var charSet = contentType.CharSet;
                    var statusCode = httpWebResponse.StatusCode;

                    var memoryStream = new MemoryStream();
                    receiveStream.CopyTo(memoryStream);
                    receiveStream.Dispose();
                    httpWebResponse.Dispose();

                    //    if (mimeType == "image/png" && !MainWindow.Instance.Set)
                    //    {
                    //        byte[] OriginalData = memoryStream.ToArray();
                    //        MainWindow.Instance.Set = true;
                    //        //Bitmap Input = new Bitmap(memoryStream);
                    //        /*Bitmap Output = new Bitmap(Input.Width, Input.Height, PixelFormat.Format8bppIndexed);
                    //        //for (int I = 0; I <= bmp.Width - 1; I++)
                    //        //{
                    //        //    for (int J = 0; J <= bmp.Height - 1; J++)
                    //        //    {
                    //        //        img8.SetPixel(I, J, bmp.GetPixel(I, J));
                    //        //    }
                    //        //}
                    //        for (int y = 0; y < Input.Height; y++)
                    //        {
                    //            for (int x = 0; x < Input.Width; x++)
                    //            {
                    //                Color colorBefore = Output.GetPixel(x, y);
                    //                BitmapData data = Output.LockBits(new Rectangle(0, 0, Output.Width, Output.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    //                byte[] bytes = new byte[data.Height * data.Stride];
                    //                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                    //                bytes[y * data.Stride + x] = 7;
                    //                Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
                    //                Output.UnlockBits(data);
                    //            }
                    //        }
                    //        //Bitmap Resized = Utils.ResizeBitmap(Output, 1, 1);
                    //        //Resized.SetResolution(1, 1);
                    //        //Resized.Save(memoryStream, ImageFormat.Jpeg);
                    //        */

                    //        //BitmapData bmData = Input.LockBits(new Rectangle(0, 0, Input.Width, Input.Height), ImageLockMode.ReadWrite, Input.PixelFormat);
                    //        //Bitmap temp = new Bitmap(bmData.Width, bmData.Height, bmData.Stride, PixelFormat.Format16bppRgb555, bmData.Scan0);
                    //        //Input.UnlockBits(bmData);
                    //        //temp.Save(memoryStream, ImageFormat.Png);

                    //        //using (var image = Image.FromStream(memoryStream))
                    //        //{
                    //        //    var newWidth = (int)(image.Width * 0.1f);
                    //        //    var newHeight = (int)(image.Height * 0.1f);
                    //        //    var thumbnailImg = new Bitmap(newWidth, newHeight);
                    //        //    var thumbGraph = Graphics.FromImage(thumbnailImg);
                    //        //    thumbGraph.CompositingQuality = CompositingQuality.Default;
                    //        //    thumbGraph.SmoothingMode = SmoothingMode.Default;
                    //        //    thumbGraph.InterpolationMode = InterpolationMode.Low;
                    //        //    var imageRectangle = new Rectangle(0, 0, newWidth, newHeight);
                    //        //    thumbGraph.DrawImage(image, imageRectangle);
                    //        //    thumbnailImg.Save(memoryStream, ImageFormat.Png);//image.RawFormat
                    //        //}


                    //        string Url = $"{Path.Combine(@"C:\Users\AI\Desktop", "Comparison1.png")}";
                    //        File.WriteAllBytes(Url, OriginalData.ToArray());
                    //        string Url2 = $"{Path.Combine(@"C:\Users\AI\Desktop", "Comparison2.png")}";
                    //        File.WriteAllBytes(Url2, memoryStream.ToArray());
                    //    }

                    memoryStream.Position = 0;

                    ResponseLength = memoryStream.Length;
                    MimeType = mimeType;
                    Charset = charSet ?? "UTF-8";
                    StatusCode = (int)statusCode;
                    Stream = memoryStream;
                    AutoDisposeStream = true;

                    callback.Continue();
                }
            });
            /*Task.Run(async () =>
            {
                using (callback)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url);
                    httpWebRequest.UserAgent = request.Headers["User-Agent"];
                    httpWebRequest.Accept = request.Headers["Accept"];
                    httpWebRequest.Method = request.Method;
                    httpWebRequest.Referer = request.ReferrerUrl;

                    httpWebRequest.Headers.Add(request.Headers);
                    httpWebRequest.Headers.Remove("User-Agent");
                    httpWebRequest.Headers.Remove("Accept");

                    //var postData = request.PostData;

                    var httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse;

                    var receiveStream = httpWebResponse.GetResponseStream();

                    var contentType = new ContentType(httpWebResponse.ContentType);
                    var mimeType = contentType.MediaType;
                    var charSet = contentType.CharSet;
                    var statusCode = httpWebResponse.StatusCode;

                    var memoryStream = new MemoryStream();
                    receiveStream.CopyTo(memoryStream);
                    receiveStream.Dispose();
                    httpWebResponse.Dispose();

                    memoryStream.Position = 0;

                    ResponseLength = memoryStream.Length;
                    MimeType = mimeType;
                    Charset = charSet ?? "UTF-8";
                    StatusCode = (int)statusCode;
                    Stream = memoryStream;
                    AutoDisposeStream = true;

                    callback.Continue();
                }
            });*/

            return CefReturnValue.ContinueAsync;
        }
    }
}
