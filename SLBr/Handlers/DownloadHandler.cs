using CefSharp;
using System.IO;
using System.Windows;

namespace SLBr.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            bool DownloadFile = true;
            if (App.Instance.GoogleSafeBrowsing)
            {
                string Response = App.Instance._SafeBrowsing.Response(url);
                SafeBrowsingHandler.ThreatType _ThreatType = Utils.CheckForInternetConnection() ? App.Instance._SafeBrowsing.GetThreatType(Response) : SafeBrowsingHandler.ThreatType.Unknown;
                if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware)
                {
                    MessageBox.Show("This download contains malware.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    DownloadFile = false;
                }
                else if (_ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software)
                    DownloadFile = MessageBox.Show("This download contains unwanted software. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
                else if (_ThreatType == SafeBrowsingHandler.ThreatType.Potentially_Harmful_Application)
                    DownloadFile = MessageBox.Show("This download can be potentially harmful. Are you sure you want to proceed with the download?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
            }
            return DownloadFile;
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    string DownloadFilePath = Path.Combine(App.Instance.GlobalSave.Get("DownloadPath"), downloadItem.SuggestedFileName);
                    App.Instance.UpdateDownloadItem(downloadItem);
                    callback.Continue(DownloadFilePath, bool.Parse(App.Instance.GlobalSave.Get("DownloadPrompt")));
                }
            }
            return true;
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            App.Instance.UpdateDownloadItem(downloadItem);

            App.Current.Dispatcher.Invoke(() =>
            {
                if (downloadItem.IsInProgress)
                {
                    if (App.Instance.CanceledDownloads.Contains(downloadItem.Id))
                        callback.Cancel();
                }
                else
                {
                    if (!browser.HasDocument)
                    {
                        if (browser.IsPopup)
                            browser.GetHost().CloseBrowser(false);
                        /*else
                        {
                            foreach (MainWindow _Window in App.Instance.AllWindows)
                            {
                                foreach (BrowserTabItem _Tab in _Window.Tabs)
                                {
                                    Browser BrowserView = _Window.GetBrowserView(_Tab);
                                    if (BrowserView?.Chromium == (ChromiumWebBrowser)chromiumWebBrowser)
                                    {
                                        _Window.CloseTab(_Tab.ID, _Window.ID);
                                        return;
                                    }
                                }
                            }
                        }*/
                    }
                }
            });
        }
    }
}
