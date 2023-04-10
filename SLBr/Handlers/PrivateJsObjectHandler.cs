using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SLBr.Handlers
{
    public class PrivateJsObjectHandler
    {
        List<string> UnsplashAPI = new List<string> {
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
            App.Instance.MainSave.Get("Search_Engine");
        public int BlockedAds() =>
            App.Instance.AdsBlocked;
        public int BlockedTrackers() =>
            App.Instance.TrackersBlocked;

        public string GetBackground()
        {
            string Url = "";
            {
                string CustomBackgroundQuery = App.Instance.MainSave.Get("CustomBackgroundQuery");
                string BackgroundImage = App.Instance.MainSave.Get("BackgroundImage");
                if (BackgroundImage == "Unsplash")
                {
                    if (string.IsNullOrEmpty(CustomBackgroundQuery))
                        Url = UnsplashAPI[App.Instance.TinyRandom.Next(UnsplashAPI.Count)];
                    else
                        Url = "https://source.unsplash.com/1920x1080/?" + CustomBackgroundQuery;
                }
                else if (BackgroundImage == "Bing image of the day")
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(App.Instance.TinyDownloader.DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                        Url = @"http://www.bing.com/" + doc.SelectSingleNode(@"/images/image/url").InnerText;
                    }
                    catch { }
                }
                else if (BackgroundImage == "Lorem Picsum")
                    Url = "https://picsum.photos/1920/1080";
                else if (BackgroundImage == "Custom")
                    Url = App.Instance.MainSave.Get("CustomBackgroundImage");
            }
            return "url('" + Url + "')";
            //return 
        }

        public string Downloads()
        {
            return JsonConvert.SerializeObject(App.Instance.Downloads);
        }
        public string History()
        {
            return JsonConvert.SerializeObject(App.Instance.History.Reverse());
        }
        public bool CancelDownload(int DownloadId)
        {
            if (!App.Instance.CanceledDownloads.Contains(DownloadId))
                App.Instance.CanceledDownloads.Add(DownloadId);
            return true;
        }

        public string SayHello(string name) { return $"Hello {name}!"; }
    }
}
