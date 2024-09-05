using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Xml;

namespace SLBr.Handlers
{
    public class PrivateJsObjectHandler
    {
        public string SearchProviderPrefix() =>
            App.Instance.GlobalSave.Get("SearchEngine");

        public string GetBackground()
        {
            string Url = "";
            string BackgroundImage = App.Instance.GlobalSave.Get("HomepageBackground");

            if (BackgroundImage == "Bing")
            {
                string BingBackground = App.Instance.GlobalSave.Get("BingBackground");
                if (BingBackground == "Image of the day")
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(new WebClient().DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                        Url = @"http://www.bing.com/" + doc.SelectSingleNode(@"/images/image/url").InnerText;
                    }
                    catch { }
                }
                else if (BingBackground == "Random")
                    Url = "http://bingw.jasonzeng.dev/?index=random";
            }
            else if (BackgroundImage == "Picsum")
                Url = "http://picsum.photos/1920/1080?random";
            else if (BackgroundImage == "Custom")
            {
                Url = App.Instance.GlobalSave.Get("CustomBackgroundImage");
                if (!Utils.IsHttpScheme(Url) && File.Exists(Url))
                    return $"url('data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(Url))}')";
            }
            return $"url('{Url}')";
        }

        public string Downloads()
        {
            return JsonSerializer.Serialize(App.Instance.Downloads);
        }
        public string History()
        {
            return JsonSerializer.Serialize(App.Instance.GlobalHistory);
        }
        public void ClearAllHistory()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Instance.GlobalHistory.Clear();
            });
        }
        public bool OpenDownload(int DownloadId)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "/select, \"" + App.Instance.Downloads.GetValueOrDefault(DownloadId).FullPath + "\"") { UseShellExecute = true });
            return true;
        }
        public bool CancelDownload(int DownloadId)
        {
            if (!App.Instance.CanceledDownloads.Contains(DownloadId))
                App.Instance.CanceledDownloads.Add(DownloadId);
            return true;
        }
    }
}
