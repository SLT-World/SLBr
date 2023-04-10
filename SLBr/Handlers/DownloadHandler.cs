using CefSharp;
using CefSharp.DevTools.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        //public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        //public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            bool DownloadFile = true;
            string Response = App.Instance._SafeBrowsing.Response(url);
            SafeBrowsingHandler.ThreatType _ThreatType = Utils.CheckForInternetConnection() ? App.Instance._SafeBrowsing.GetThreatType(Response) : SafeBrowsingHandler.ThreatType.Unknown;
            if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware)
                DownloadFile = MessageBox.Show("This download contains malware. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
                //MessageBox.Show($"This download contains malware. Download is blocked.", "Warning");

            else if (_ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software)
                DownloadFile = MessageBox.Show("This download contains unwanted software. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

            else if (_ThreatType == SafeBrowsingHandler.ThreatType.Potentially_Harmful_Application)
                DownloadFile = MessageBox.Show("This download can be potentially harmful. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
            return DownloadFile;
        }

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            //OnBeforeDownloadFired?.Invoke(this, downloadItem);

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    string DownloadFilePath = Path.Combine(App.Instance.MainSave.Get("DownloadPath"), downloadItem.SuggestedFileName);
                    App.Instance.UpdateDownloadItem(downloadItem);
                    callback.Continue(DownloadFilePath, bool.Parse(App.Instance.MainSave.Get("DownloadPrompt")));
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            //OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            App.Instance.UpdateDownloadItem(downloadItem);
            if (downloadItem.IsInProgress)
            {
                if (App.Instance.CanceledDownloads.Contains(downloadItem.Id))
                    callback.Cancel();
            }
        }
    }
}
