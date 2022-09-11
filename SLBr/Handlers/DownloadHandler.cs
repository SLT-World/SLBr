using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        //public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        //public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            //OnBeforeDownloadFired?.Invoke(this, downloadItem);

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    string DownloadFilePath = Path.Combine(MainWindow.Instance.MainSave.Get("DownloadPath"), downloadItem.SuggestedFileName);
                    MainWindow.Instance.UpdateDownloadItem(downloadItem);
                    callback.Continue(DownloadFilePath, bool.Parse(MainWindow.Instance.MainSave.Get("DownloadPrompt")));
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            //OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            MainWindow.Instance.UpdateDownloadItem(downloadItem);
            if (downloadItem.IsInProgress && MainWindow.Instance.CanceledDownloads.Contains(downloadItem.Id))
                callback.Cancel();
        }
    }
}
