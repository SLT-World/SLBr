using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Handlers
{
    public class QRCodeHandler
    {
        public Bitmap GenerateQRCode(string Value, int Width = 300, int Height = 300)
        {
            var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chs={1}x{2}&chl={0}", Value, Width, Height);
            WebResponse response = default(WebResponse);
            Stream remoteStream = default(Stream);
            StreamReader readStream = default(StreamReader);
            WebRequest request = WebRequest.Create(url);
            response = request.GetResponse();
            remoteStream = response.GetResponseStream();
            readStream = new StreamReader(remoteStream);
            Bitmap Output = new Bitmap(Image.FromStream(remoteStream));
            response.Close();
            remoteStream.Close();
            readStream.Close();
            return Output;
            //img.Save("E:/QRCode/" + "iewrferreg" + ".png");
        }
    }
}
