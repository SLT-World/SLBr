// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
{
    class ContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
            {
                if (!string.IsNullOrEmpty(parameters.SelectionText))
                {
                    model.AddItem((CefMenuCommand)26501, "Search for text in new tab");
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                    /*model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                    model.AddSeparator();
                    model.Clear();
                    model.AddItem(CefMenuCommand.Copy, "Copy");
                    model.AddItem((CefMenuCommand)26501, "Save as");*/
                }
                else
                {
                    model.Remove(CefMenuCommand.Print);
                    model.Remove(CefMenuCommand.ViewSource);
                    model.AddItem(CefMenuCommand.Reload, "Refresh");
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26501, "Zoom In");
                    model.AddItem((CefMenuCommand)26502, "Zoom Out");
                    model.AddItem((CefMenuCommand)26503, "Reset Zoom Level");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26506, "Take a screenshot");
                    model.AddSeparator();
                    IMenuModel _EditSubMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Edit");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Undo, "Undo");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Redo, "Redo");
                    _EditSubMenuModel.AddSeparator();
                    _EditSubMenuModel.AddItem(CefMenuCommand.Cut, "Cut");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Copy, "Copy");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Paste, "Paste");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Delete, "Delete");
                    _EditSubMenuModel.AddSeparator();
                    _EditSubMenuModel.AddItem(CefMenuCommand.SelectAll, "Select All");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26507, "Search page on SafeBrowsing");
                    model.AddItem((CefMenuCommand)26504, "View page source");
                    model.AddItem((CefMenuCommand)26505, "Inspect");
                }
            }
            else
            {
                model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                model.AddSeparator();
                model.Clear();
                model.AddItem(CefMenuCommand.Copy, "Copy");
                model.AddItem((CefMenuCommand)26501, "Save as");
                //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
            }
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            if (commandId == CefMenuCommand.NotFound)
                return false;
            if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
            {
                string SelectedText = parameters.SelectionText;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (!string.IsNullOrEmpty(SelectedText))
                    {
                        if (commandId == (CefMenuCommand)26501)
                            MainWindow.Instance.CreateTab(MainWindow.Instance.CreateWebBrowser(string.Format(MainWindow.Instance.MainSave.Get("Search_Engine"), SelectedText.Trim().Replace(" ", "+"))));
                    }
                    else
                    {
                        if (commandId == (CefMenuCommand)26501)
                            MainWindow.Instance.ZoomIn();
                        else if (commandId == (CefMenuCommand)26502)
                            MainWindow.Instance.ZoomOut();
                        else if (commandId == (CefMenuCommand)26503)
                            MainWindow.Instance.ResetZoomLevel();
                        else if (commandId == (CefMenuCommand)26504)
                            MainWindow.Instance.ViewSource();
                        else if (commandId == (CefMenuCommand)26505)
                            MainWindow.Instance.DevTools();
                        else if (commandId == (CefMenuCommand)26506)
                            MainWindow.Instance.Screenshot();
                        else if (commandId == (CefMenuCommand)26507)
                            MainWindow.Instance.CreateTab(MainWindow.Instance.CreateWebBrowser($"https://transparencyreport.google.com/safe-browsing/search?url={chromiumWebBrowser.Address}"));
                    }
                }));
            }
            else
            {
                if (commandId == CefMenuCommand.Copy)
                    Clipboard.SetText(parameters.SourceUrl);
                else if (commandId == (CefMenuCommand)26501)
                    chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                /*else if (commandId == (CefMenuCommand)26502)
                {
                    chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                    //string DownloadPath = MainWindow.Instance.MainSave.Get("DownloadPath");
                    //string FileName = Path.Combine(DownloadPath, Path.GetFileName(parameters.SourceUrl));
                    Process.Start(FileName);
                }*/
            }
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
            /*var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;

            webBrowser.Dispatcher.Invoke(() =>
            {
                webBrowser.ContextMenu = null;
            });*/
        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            //callback.Cancel();
            return false;
        }
    }
}
