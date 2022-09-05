using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SLBr.Handlers
{
    public class ContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            if (parameters.IsEditable)
            {
                model.Clear();
                switch (parameters.DictionarySuggestions.Count)
                {
                    case 0:
                        break;
                    case 1:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddSeparator();
                        break;
                    case 2:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion1, parameters.DictionarySuggestions[1]);
                        model.AddSeparator();
                        break;
                    case 3:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion1, parameters.DictionarySuggestions[1]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion2, parameters.DictionarySuggestions[2]);
                        model.AddSeparator();
                        break;
                    case 4:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion1, parameters.DictionarySuggestions[1]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion2, parameters.DictionarySuggestions[2]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion3, parameters.DictionarySuggestions[3]);
                        model.AddSeparator();
                        break;
                    case 5:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion1, parameters.DictionarySuggestions[1]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion2, parameters.DictionarySuggestions[2]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion3, parameters.DictionarySuggestions[3]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion4, parameters.DictionarySuggestions[4]);
                        model.AddSeparator();
                        break;
                    default:
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion0, parameters.DictionarySuggestions[0]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion1, parameters.DictionarySuggestions[1]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion2, parameters.DictionarySuggestions[2]);
                        model.AddItem(CefMenuCommand.SpellCheckSuggestion3, parameters.DictionarySuggestions[3]);
                        model.AddSeparator();
                        break;
                }
                model.AddItem(CefMenuCommand.Cut, "Cut");
                model.AddItem(CefMenuCommand.Copy, "Copy");
                model.AddItem(CefMenuCommand.Paste, "Paste");
                model.AddItem(CefMenuCommand.Delete, "Delete");
                model.AddItem(CefMenuCommand.SelectAll, "Select All");
            }
            else if (!string.IsNullOrEmpty(parameters.SelectionText))
            {
                model.Clear();
                if (Utils.IsUrl(parameters.SelectionText))
                {
                    model.AddItem((CefMenuCommand)26502, "Open in new tab");
                    model.AddItem(CefMenuCommand.Copy, "Copy");
                }
                else
                {
                    model.AddItem(CefMenuCommand.Find, $"Search \"{parameters.SelectionText}\" in new tab");
                    model.AddItem(CefMenuCommand.Copy, "Copy");
                }
            }
            else
            {
                if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
                {
                    model.Remove(CefMenuCommand.Print);
                    model.Remove(CefMenuCommand.ViewSource);
                    model.AddItem(CefMenuCommand.Reload, "Refresh");
                    /*model.AddSeparator();
                    model.AddItem((CefMenuCommand)26503, "Zoom In");
                    model.AddItem((CefMenuCommand)26504, "Zoom Out");
                    model.AddItem((CefMenuCommand)26505, "Reset Zoom Level");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26506, "Screenshot");
                    model.AddItem((CefMenuCommand)26511, "Translate to English");*/
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.Print, "Print");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26504, "View page source");
                    model.AddItem((CefMenuCommand)26503, "Inspect");
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
                    /*model.AddItem((CefMenuCommand)26508, "View page source");
                    model.AddItem((CefMenuCommand)26509, "Inspect");
                    model.AddSeparator();*/
                }
                else
                {
                    model.Clear();
                    model.AddItem(CefMenuCommand.Copy, "Copy");
                    model.AddItem((CefMenuCommand)26505, "Copy address");
                    model.AddItem((CefMenuCommand)26501, "Save as");
                    //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
                }
            }
            model.AddSeparator();
            model.AddItem(CefMenuCommand.NotFound, "Cancel");
        }

        //Save as 26501
        //26502 Open in new Tab
        //26503 Inspector
        //26504 View source
        //26505 Copy Image Url

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            if (commandId == CefMenuCommand.NotFound)
                return false;
            bool ToReturn = false;
            if (parameters.IsEditable)
            {
                    
            }
            else if (!string.IsNullOrEmpty(parameters.SelectionText))
            {
                string SelectedText = parameters.SelectionText;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (Utils.IsUrl(SelectedText))
                    {
                        if (commandId == (CefMenuCommand)26502)
                        {
                            MainWindow.Instance.NewBrowserTab(SelectedText, 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                            ToReturn = true;
                        }
                    }
                    else
                    {
                        if (commandId == CefMenuCommand.Find)
                        {
                            MainWindow.Instance.NewBrowserTab(Utils.FixUrl(string.Format(MainWindow.Instance.MainSave.Get("Search_Engine"), SelectedText)), 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1); ;
                            ToReturn = true;
                        }
                    }
                }));
            }
            else
            {
                if (parameters.MediaType != ContextMenuMediaType.Image)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (commandId == (CefMenuCommand)26504)
                        {
                            MainWindow.Instance.NewBrowserTab($"view-source:{chromiumWebBrowser.Address}", 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                            ToReturn = true;
                        }
                        if (commandId == (CefMenuCommand)26503)
                        {
                            MainWindow.Instance.Inspect();
                            ToReturn = true;
                        }
                    }));
                }
                else
                {
                    if (commandId == CefMenuCommand.Copy)
                    {
                        try
                        {
                            Clipboard.SetDataObject(new Bitmap(new MemoryStream(MainWindow.Instance.TinyDownloader.DownloadData(parameters.SourceUrl))));
                        }
                        catch
                        {
                            Clipboard.SetText(parameters.SourceUrl);
                        }
                        ToReturn = true;
                    }
                    if (commandId == (CefMenuCommand)26505)
                    {
                        Clipboard.SetText(parameters.SourceUrl);
                        ToReturn = true;
                    }
                    else if (commandId == (CefMenuCommand)26501)
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
            return false;
        }
    }
}
