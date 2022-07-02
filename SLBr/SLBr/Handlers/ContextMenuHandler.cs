// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SLBr
{
    class ContextMenuHandler : IContextMenuHandler
    {
        //TODO: HAVE CUSTOM CONTEXT MENU STYLE LIKE MS EDGE

        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
            {
                if (!string.IsNullOrEmpty(parameters.SelectionText))
                {
                    if (Utils.IsHttpScheme(parameters.SelectionText) || Utils.IsProtocolNotHttp(parameters.SelectionText))
                    {
                        //model.Clear();
                        model.AddItem((CefMenuCommand)26501, "Open in new tab");
                        model.AddSeparator();
                        model.AddItem(CefMenuCommand.NotFound, "Cancel");
                    }
                    else
                    {
                        model.AddItem((CefMenuCommand)26502, "Search for text in new tab");
                        model.AddSeparator();
                        model.AddItem(CefMenuCommand.NotFound, "Cancel");
                        /*model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                        model.AddSeparator();
                        model.Clear();
                        model.AddItem(CefMenuCommand.Copy, "Copy");
                        model.AddItem((CefMenuCommand)26501, "Save as");*/
                    }
                }
                else
                {
                    model.Remove(CefMenuCommand.Print);
                    model.Remove(CefMenuCommand.ViewSource);
                    model.AddItem(CefMenuCommand.Reload, "Refresh");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26503, "Zoom In");
                    model.AddItem((CefMenuCommand)26504, "Zoom Out");
                    model.AddItem((CefMenuCommand)26505, "Reset Zoom Level");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26506, "Screenshot");
                    model.AddSeparator();
                    /*IMenuModel _EditSubMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Edit");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Undo, "Undo");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Redo, "Redo");
                    _EditSubMenuModel.AddSeparator();
                    _EditSubMenuModel.AddItem(CefMenuCommand.Cut, "Cut");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Copy, "Copy");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Paste, "Paste");
                    _EditSubMenuModel.AddItem(CefMenuCommand.Delete, "Delete");
                    _EditSubMenuModel.AddSeparator();
                    _EditSubMenuModel.AddItem(CefMenuCommand.SelectAll, "Select All");
                    model.AddSeparator();*/
                    model.AddItem((CefMenuCommand)26507, "Search page on SafeBrowsing");
                    model.AddItem((CefMenuCommand)26508, "View page source");
                    model.AddItem((CefMenuCommand)26509, "Inspect");
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.NotFound, "Cancel");
                }
            }
            else
            {
                model.Clear();
                model.AddItem(CefMenuCommand.Copy, "Copy");
                model.AddItem((CefMenuCommand)26510, "Save as");
                model.AddSeparator();
                model.AddItem(CefMenuCommand.NotFound, "Cancel");
                //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
            }
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            if (commandId == CefMenuCommand.NotFound)
                return false;
            bool ToReturn = false;
            if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
            {
                string SelectedText = parameters.SelectionText;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (!string.IsNullOrEmpty(SelectedText))
                    {
                        if (Utils.IsHttpScheme(SelectedText) || Utils.IsProtocolNotHttp(SelectedText))
                        {
                            if (commandId == (CefMenuCommand)26501)
                            {
                                MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser(SelectedText), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
                                ToReturn = true;
                            }
                        }
                        else
                        {
                            if (commandId == (CefMenuCommand)26502) {
                                MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser(string.Format(MainWindow.Instance.MainSave.Get("Search_Engine"), SelectedText.Trim().Replace(" ", "+"))), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
                                ToReturn = true;
                            }
                            /*model.AddItem(CefMenuCommand.NotFound, "Close Menu");
                            model.AddSeparator();
                            model.Clear();
                            model.AddItem(CefMenuCommand.Copy, "Copy");
                            model.AddItem((CefMenuCommand)26501, "Save as");*/
                        }
                    }
                    else
                    {
                        if (commandId == (CefMenuCommand)26503)
                        {
                            MainWindow.Instance.ZoomIn();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26504)
                        {
                            MainWindow.Instance.ZoomOut();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26505)
                        {
                            MainWindow.Instance.ResetZoomLevel();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26506)
                        {
                            MainWindow.Instance.Screenshot();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26507)
                        {
                            MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser($"https://transparencyreport.google.com/safe-browsing/search?url={chromiumWebBrowser.Address}"));
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26508)
                        {
                            MainWindow.Instance.ViewSource();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26509)
                        {
                            MainWindow.Instance.UseInspector();
                            ToReturn = true;
                        }
                    }
                }));
            }
            else
            {
                if (commandId == CefMenuCommand.Copy)
                {
                    Clipboard.SetText(parameters.SourceUrl);
                    ToReturn = true;
                }
                else if (commandId == (CefMenuCommand)26510)
                {
                    chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                    ToReturn = true;
                }
                /*else if (commandId == (CefMenuCommand)26502)
                {
                    chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                    //string DownloadPath = MainWindow.Instance.MainSave.Get("DownloadPath");
                    //string FileName = Path.Combine(DownloadPath, Path.GetFileName(parameters.SourceUrl));
                    Process.Start(FileName);
                }*/
            }
            return ToReturn;
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
            var _Browser = (ChromiumWebBrowser)chromiumWebBrowser;

            _Browser.Dispatcher.Invoke(() =>
            {
                _Browser.ContextMenu = null;
            });
        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            //callback.Cancel();
            //return false;
            var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;

            //IMenuModel is only valid in the context of this method, so need to read the values before invoking on the UI thread
            var menuItems = GetMenuItems(model).ToList();

            string SelectedText = parameters.SelectionText;
            webBrowser.Dispatcher.Invoke(() =>
            {
                var menu = new ContextMenu
                {
                    IsOpen = true
                };

                RoutedEventHandler handler = null;

                handler = (s, e) =>
                {
                    menu.Closed -= handler;

                    if (!callback.IsDisposed)
                    {
                        callback.Cancel();
                    }
                };

                menu.Closed += handler;
                menu.Style = (Style)MainWindow.Instance.Resources["ContextMenuStyle"];

                foreach (var item in menuItems)
                {
                    if (item.Item2 == CefMenuCommand.NotFound && string.IsNullOrWhiteSpace(item.Item1))
                    {
                        menu.Items.Add(new Separator());
                        continue;
                    }

                    menu.Items.Add(new MenuItem
                    {
                        Header = item.Item1.Replace("&", "_"),
                        IsEnabled = item.Item3,
                        Command = new RelayCommand(() =>
                        {
                            switch (item.Item2)
                            {
                                case CefMenuCommand.Back:
                                    {
                                        browser.GoBack();
                                        break;
                                    }
                                case CefMenuCommand.Forward:
                                    {
                                        browser.GoForward();
                                        break;
                                    }
                                case CefMenuCommand.Cut:
                                    {
                                        browser.FocusedFrame.Cut();
                                        break;
                                    }
                                case CefMenuCommand.Copy:
                                    {
                                        if (parameters.MediaType != ContextMenuMediaType.Image)
                                            Clipboard.SetText(parameters.SourceUrl);
                                        else
                                            browser.FocusedFrame.Copy();
                                        break;
                                    }
                                case CefMenuCommand.Paste:
                                    {
                                        browser.FocusedFrame.Paste();
                                        break;
                                    }
                                case CefMenuCommand.Print:
                                    {
                                        browser.GetHost().Print();
                                        break;
                                    }
                                case CefMenuCommand.ViewSource:
                                    {
                                        browser.FocusedFrame.ViewSource();
                                        break;
                                    }
                                case CefMenuCommand.Undo:
                                    {
                                        browser.FocusedFrame.Undo();
                                        break;
                                    }
                                case CefMenuCommand.StopLoad:
                                    {
                                        browser.StopLoad();
                                        break;
                                    }
                                case CefMenuCommand.SelectAll:
                                    {
                                        browser.FocusedFrame.SelectAll();
                                        break;
                                    }
                                case CefMenuCommand.Redo:
                                    {
                                        browser.FocusedFrame.Redo();
                                        break;
                                    }
                                case CefMenuCommand.Find:
                                    {
                                        browser.GetHost().Find(parameters.SelectionText, true, false, false);
                                        break;
                                    }
                                case CefMenuCommand.AddToDictionary:
                                    {
                                        browser.GetHost().AddWordToDictionary(parameters.MisspelledWord);
                                        break;
                                    }
                                case CefMenuCommand.Reload:
                                    {
                                        browser.Reload();
                                        break;
                                    }
                                case CefMenuCommand.ReloadNoCache:
                                    {
                                        browser.Reload(ignoreCache: true);
                                        break;
                                    }
                                case (CefMenuCommand)26501:
                                    {
                                        MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser(SelectedText), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
                                        break;
                                    }
                                case (CefMenuCommand)26502:
                                    {
                                        MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser(string.Format(MainWindow.Instance.MainSave.Get("Search_Engine"), SelectedText.Trim().Replace(" ", "+"))), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
                                        break;
                                    }
                                case (CefMenuCommand)26503:
                                    {
                                        MainWindow.Instance.ZoomIn();
                                        break;
                                    }
                                case (CefMenuCommand)26504:
                                    {
                                        MainWindow.Instance.ZoomOut();
                                        break;
                                    }
                                case (CefMenuCommand)26505:
                                    {
                                        MainWindow.Instance.ResetZoomLevel();
                                        break;
                                    }
                                case (CefMenuCommand)26506:
                                    {
                                        MainWindow.Instance.Screenshot();
                                        break;
                                    }
                                case (CefMenuCommand)26507:
                                    {
                                        MainWindow.Instance.CreateChromeTab(MainWindow.Instance.CreateWebBrowser($"https://transparencyreport.google.com/safe-browsing/search?url={chromiumWebBrowser.Address}"), true, MainWindow.Instance.Tabs.SelectedIndex + 1);
                                        break;
                                    }
                                case (CefMenuCommand)26508:
                                    {
                                        MainWindow.Instance.ViewSource();
                                        break;
                                    }
                                case (CefMenuCommand)26509:
                                    {
                                        MainWindow.Instance.UseInspector();
                                        break;
                                    }
                                case (CefMenuCommand)26510:
                                    {
                                        chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                                        break;
                                    }
                            }
                        })
                    });
                }
                webBrowser.ContextMenu = menu;
            });

            return true;
        }

        private static IEnumerable<Tuple<string, CefMenuCommand, bool>> GetMenuItems(IMenuModel model)
        {
            for (var i = 0; i < model.Count; i++)
            {
                var header = model.GetLabelAt(i);
                var commandId = model.GetCommandIdAt(i);
                var isEnabled = model.IsEnabledAt(i);
                yield return new Tuple<string, CefMenuCommand, bool>(header, commandId, isEnabled);
            }
        }
    }
}
