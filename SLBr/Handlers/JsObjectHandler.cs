using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Handlers
{
    public class JsObjectHandler
    {
        public string SearchProviderPrefix() =>
            MainWindow.Instance.MainSave.Get("Search_Engine");
        public int BlockedAds() =>
            MainWindow.Instance.AdsBlocked;
        public int BlockedTrackers() =>
            MainWindow.Instance.TrackersBlocked;

        public string Downloads()
        {
            return JsonConvert.SerializeObject(MainWindow.Instance.Downloads);
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
