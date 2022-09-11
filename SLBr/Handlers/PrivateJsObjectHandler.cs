using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SLBr.Handlers
{
    public class PrivateJsObjectHandler
    {
        List<string> UnsplashAPI = new List<string> { //"https://picsum.photos/1920/1080",
        //"https://source.unsplash.com/1920x1080/?space",
        "https://source.unsplash.com/1920x1080/?nasa",
        "https://source.unsplash.com/1920x1080/?mountain-range",
        "https://source.unsplash.com/1920x1080/?mountain-peak",
        "https://source.unsplash.com/1920x1080/?mounts",
        "https://source.unsplash.com/1920x1080/?landscape",
        "https://source.unsplash.com/1920x1080/?landmark",
        "https://source.unsplash.com/1920x1080/?cloud",
        "https://source.unsplash.com/1920x1080/?archipelago",
            };

        public string SearchProviderPrefix() =>
            MainWindow.Instance.MainSave.Get("Search_Engine");
        public int BlockedAds() =>
            MainWindow.Instance.AdsBlocked;
        public int BlockedTrackers() =>
            MainWindow.Instance.TrackersBlocked;

        public string GetBackground()
        {
            string Url = "";
            {
                string BackgroundImage = MainWindow.Instance.MainSave.Get("BackgroundImage");
                if (BackgroundImage == "Unsplash")
                    Url = UnsplashAPI[MainWindow.Instance.TinyRandom.Next(UnsplashAPI.Count)];
                else if (BackgroundImage == "Bing image of the day")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(MainWindow.Instance.TinyDownloader.DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                    Url = @"http://www.bing.com/" + doc.SelectSingleNode(@"/images/image/url").InnerText;
                }
                else if (BackgroundImage == "Custom")
                    Url = MainWindow.Instance.MainSave.Get("CustomBackgroundImage");
            }
            return "url('" + Url + "')";
            //return 
        }

        public string Downloads()
        {
            return JsonConvert.SerializeObject(MainWindow.Instance.Downloads);
        }
        public string History()
        {
            return JsonConvert.SerializeObject(MainWindow.Instance.History.Reverse());
        }
        public bool CancelDownload(int DownloadId)
        {
            if (!MainWindow.Instance.CanceledDownloads.Contains(DownloadId))
                MainWindow.Instance.CanceledDownloads.Add(DownloadId);
            return true;
        }

        public string SayHello(string name) { return $"Hello {name}!"; }
    }
}
