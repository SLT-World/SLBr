using CefSharp;
using System.IO;

namespace SLBr.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            //Doesn't seem to work?
            /*if (App.Instance.GoogleSafeBrowsing)
            {
                bool DownloadFile = true;
                string Response = App.Instance._SafeBrowsing.Response(url);
                SafeBrowsingHandler.ThreatType _ThreatType = App.Instance._SafeBrowsing.GetThreatType(Response);
                if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware)
                {
                    MessageBox.Show("This download contains malware.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    DownloadFile = false;
                }
                else if (_ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software)
                    DownloadFile = MessageBox.Show("This download contains unwanted software. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
                else if (_ThreatType == SafeBrowsingHandler.ThreatType.Potentially_Harmful_Application)
                    DownloadFile = MessageBox.Show("This download can be potentially harmful. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
                return DownloadFile;
            }*/
            return true;
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                using (callback)
                    callback.Continue(Path.Combine(App.Instance.GlobalSave.Get("DownloadPath"), downloadItem.SuggestedFileName), bool.Parse(App.Instance.GlobalSave.Get("DownloadPrompt")));
            }
            return true;
        }

        private Dictionary<int, IDownloadItemCallback> DownloadCallbacks = new Dictionary<int, IDownloadItemCallback>();

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            //App.Instance.UpdateDownloadItem(downloadItem);
            if (downloadItem.IsInProgress)
                DownloadCallbacks[downloadItem.Id] = callback;
            else
            {
                DownloadCallbacks.Remove(downloadItem.Id);
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!browser.HasDocument)
                    {
                        if (browser.IsPopup)
                            browser.GetHost().CloseBrowser(false);
                    }
                });
            }
        }

        public void CancelDownload(int DownloadID)
        {
            if (DownloadCallbacks.TryGetValue(DownloadID, out IDownloadItemCallback _Callback))
                _Callback.Cancel();
        }
    }
}
