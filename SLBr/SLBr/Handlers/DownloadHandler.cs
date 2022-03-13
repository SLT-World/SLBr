﻿// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
using CefSharp;
//using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SLBr
{
    class DownloadHandler : IDownloadHandler
    {
        bool _allowDownload = true;

        float DownloadUpdatePeriod = 2.5f;
        float DownloadUpdateTime;

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    if (_allowDownload)
                    {
                        bool Download = false;
                        if (bool.Parse(MainWindow.Instance.MainSave.Get("AFDP")))
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            saveFileDialog.InitialDirectory = MainWindow.Instance.MainSave.Get("DownloadPath");
                            saveFileDialog.FileName = downloadItem.SuggestedFileName;
                            saveFileDialog.Filter = "|*" + System.IO.Path.GetExtension(saveFileDialog.FileName);

                            if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName != "")
                            {
                                downloadItem.SuggestedFileName = saveFileDialog.FileName;
                                Download = true;
                            }
                            else
                                downloadItem.IsCancelled = _allowDownload;
                        }
                        else
                        {
                            downloadItem.SuggestedFileName = Path.Combine(MainWindow.Instance.MainSave.Get("DownloadPath"), Path.GetFileName(downloadItem.SuggestedFileName));
                            Download = true;
                        }
                        if (Download)
                        {
                            MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                string FileName = Path.GetFileName(downloadItem.SuggestedFileName);
                                MainWindow.Instance.DownloadContainer.Visibility = Visibility.Visible;
                                MainWindow.Instance.DownloadFileName.Text = FileName;
                                //MainWindow.Instance.DownloadProgressBar.Maximum = downloadItem.TotalBytes;
                                System.Windows.Controls.MenuItem DownloadMenuItem = MainWindow.CreateMenuItemForList(Path.GetFileName(downloadItem.SuggestedFileName), $"13<,>{downloadItem.SuggestedFileName}", new RoutedEventHandler(MainWindow.Instance.ButtonAction));
                                MainWindow.Instance.DownloadListMenuItem.Items.Insert(0, DownloadMenuItem);
                                if (MainWindow.Instance.DownloadListMenuItem.Items.Count > 10)
                                    MainWindow.Instance.DownloadListMenuItem.Items.RemoveAt(10);
                            }));
                        }
                    }
                    else
                        downloadItem.IsCancelled = !_allowDownload;

                    callback.Continue(downloadItem.SuggestedFileName, showDialog: false);
                }

            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (downloadItem.IsComplete)
                {
                    MainWindow.Instance.DownloadProgressText.Visibility = Visibility.Collapsed;
                    MainWindow.Instance.DownloadProgressBar.Visibility = Visibility.Collapsed;
                    //browser.CloseBrowser(true);
                    MainWindow.Instance.DownloadOpenFileButton.Visibility = Visibility.Visible;
                    MainWindow.Instance.DownloadOpenFileButton.Tag = $"13<,>{downloadItem.FullPath}";
                }
                else
                {
                    if ((DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds > DownloadUpdateTime)
                    {
                        MainWindow.Instance.DownloadProgressText.Visibility = Visibility.Visible;
                        MainWindow.Instance.DownloadProgressBar.Visibility = Visibility.Visible;
                        MainWindow.Instance.DownloadOpenFileButton.Visibility = Visibility.Collapsed;
                        if (downloadItem.IsCancelled)
                            MainWindow.Instance.DownloadProgressText.Text = "Cancelled";
                        else if (downloadItem.IsInProgress)
                        {
                            MainWindow.Instance.DownloadProgressText.Text = $"{downloadItem.CurrentSpeed} bytes ({downloadItem.PercentComplete}%)";/*{(downloadItem.EndTime - downloadItem.StartTime).Value.TotalSeconds} seconds left.{downloadItem.ReceivedBytes}/{downloadItem.TotalBytes} bytes, */
                            MainWindow.Instance.DownloadProgressBar.Value = downloadItem.PercentComplete;
                            //MainWindow.Instance.DownloadProgressBar.Value = downloadItem.ReceivedBytes;
                            DownloadUpdateTime = ((float)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds) + DownloadUpdatePeriod;
                        }

                    }
                }
            }));
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