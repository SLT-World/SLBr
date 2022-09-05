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
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);

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
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            MainWindow.Instance.UpdateDownloadItem(downloadItem);
            if (downloadItem.IsInProgress && MainWindow.Instance.CanceledDownloads.Contains(downloadItem.Id))
                callback.Cancel();
            /*MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (IsComplete)
                {
                    MainWindow.Instance.DownloadContainer.Visibility = Visibility.Collapsed;

                    MenuItem DownloadMenuItem = MainWindow.Instance.CreateMenuItemForList(Path.GetFileName(downloadItem.SuggestedFileName), $"13<,>{downloadItem.SuggestedFileName}", new RoutedEventHandler(MainWindow.Instance.ButtonAction));
                    MainWindow.Instance.DownloadListMenuItem.Items.Insert(0, DownloadMenuItem);
                    if (MainWindow.Instance.DownloadListMenuItem.Items.Count > 10)
                        MainWindow.Instance.DownloadListMenuItem.Items.RemoveAt(10);
                }
                else
                {
                    if (IsCancelled)
                    {
                        MainWindow.Instance.DownloadContainer.Visibility = Visibility.Collapsed;
                        MainWindow.Instance.DownloadProgressText.Text = "Cancelled";
                    }
                    else if (IsInProgress)
                    {
                        string FileName = Path.GetFileName(downloadItem.FullPath);
                        MainWindow.Instance.DownloadContainer.Visibility = Visibility.Visible;
                        MainWindow.Instance.DownloadFileName.Text = FileName;
                        //{
                        MainWindow.Instance.DownloadProgressText.Visibility = Visibility.Visible;
                        MainWindow.Instance.DownloadProgressBar.Visibility = Visibility.Visible;
                        MainWindow.Instance.DownloadOpenFileButton.Visibility = Visibility.Collapsed;
                        MainWindow.Instance.DownloadProgressText.Text = $"{downloadItem.PercentComplete}% Complete";//{downloadItem.CurrentSpeed} bytes ()*//*{(downloadItem.EndTime - downloadItem.StartTime).Value.TotalSeconds} seconds left.{downloadItem.ReceivedBytes}/{downloadItem.TotalBytes} bytes,
                        MainWindow.Instance.DownloadProgressBar.Value = downloadItem.PercentComplete;
                    }
                }
            }));*/
        }

        /*public event EventHandler<DownloadItem> OnBeforeDownloadFired;

        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (downloadItem.IsValid)
            {
                Console.WriteLine("== File information ========================");
                Console.WriteLine(" File URL: {0}", downloadItem.Url);
                Console.WriteLine(" Suggested FileName: {0}", downloadItem.SuggestedFileName);
                Console.WriteLine(" MimeType: {0}", downloadItem.MimeType);
                Console.WriteLine(" Content Disposition: {0}", downloadItem.ContentDisposition);
                Console.WriteLine(" Total Size: {0}", downloadItem.TotalBytes);
                Console.WriteLine("============================================");
            }

            OnBeforeDownloadFired?.Invoke(this, downloadItem);

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    callback.Continue(
                        downloadItem.SuggestedFileName,
                        showDialog: true
                    );
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            if (downloadItem.IsValid)
            {
                if (downloadItem.IsInProgress && (downloadItem.PercentComplete != 0))
                {
                    Console.WriteLine(
                        "Current Download Speed: {0} bytes ({1}%)",
                        downloadItem.CurrentSpeed,
                        downloadItem.PercentComplete
                    );
                }

                if (downloadItem.IsComplete)
                    Console.WriteLine("The download has been finished!");
            }
        }*/
    }
}
