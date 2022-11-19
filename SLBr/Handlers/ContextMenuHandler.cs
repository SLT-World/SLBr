using CefSharp;
using CefSharp.Wpf.HwndHost;
using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
                //if (bool.Parse(MainWindow.Instance.MainSave.Get("SpellCheck")))
                //{
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
                //}
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

                    model.AddSeparator();
                    IMenuModel ZoomSubMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Zoom");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26507, "Increment");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26508, "Decrement");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26509, "Default");
                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26501, "Save as");
                    model.AddItem(CefMenuCommand.Print, "Print");
                    model.AddItem((CefMenuCommand)26506, "Take screenshot");
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
        //26506 Take screenshot
        //26507 Zoom default
        //26508 Zoom out
        //26509 Zoom in

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
                        else if (commandId == (CefMenuCommand)26503)
                        {
                            MainWindow.Instance.Inspect();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26501)
                        {
                            chromiumWebBrowser.StartDownload(chromiumWebBrowser.Address);
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26506)
                        {
                            MainWindow.Instance.Screenshot();
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26507)
                        {
                            MainWindow.Instance.Zoom(1);
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26508)
                        {
                            MainWindow.Instance.Zoom(-1);
                            ToReturn = true;
                        }
                        else if (commandId == (CefMenuCommand)26509)
                        {
                            MainWindow.Instance.Zoom(0);
                            ToReturn = true;
                        }
                    }));
                }
                else
                {
                    if (commandId == CefMenuCommand.Copy)
                    {
                        try { Clipboard.SetDataObject(new Bitmap(new MemoryStream(MainWindow.Instance.TinyDownloader.DownloadData(parameters.SourceUrl)))); }
                        catch { Clipboard.SetText(parameters.SourceUrl); }
                        ToReturn = true;
                    }
                    else if (commandId == (CefMenuCommand)26505)
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

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            if (model.Count == 0) return true;
            if (MainWindow.Instance.GetTheme().DarkTitleBar == true) return false;

            //return false;
            var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;
            var menuItems = GetMenuItemsNew(model).ToList();

            ContextMenuParams Parameters = new ContextMenuParams(parameters);

            webBrowser.Dispatcher.Invoke(() =>
            {
                var menu = new ContextMenu { IsOpen = true };
                RoutedEventHandler handler = null;
                handler = (s, e) =>
                {
                    menu.Closed -= handler;
                    if (!callback.IsDisposed)
                        callback.Cancel();
                };

                menu.Closed += handler;
                menu.Style = (Style)MainWindow.Instance.Resources["ContextMenuStyle"];
                menu.OverridesDefaultStyle = true;

                MenuClassToCollection(menuItems, menu.Items, browser, Parameters);
                /*foreach (var item in menuItems)
                {
                    if (item.CommandId == CefMenuCommand.NotFound && string.IsNullOrWhiteSpace(item.Header))
                    {
                        menu.Items.Add(new Separator());
                        continue;
                    }

                    menu.Items.Add(new MenuItem
                    {
                        Header = item.Header.Replace("&", "_"),
                        IsEnabled = item.IsEnabled,
                        Command = GetCommandByCefCommand(browser, item.CommandId, Parameters),
                        Items = item.SubMenu
                    });
                }*/
                webBrowser.ContextMenu = menu;
            });
            //callback.Cancel();
            Parameters.Dispose();
            return true;
        }

        void MenuClassToCollection(IEnumerable<MenuClass> Menu, ItemCollection _ItemCollection, IBrowser browser, ContextMenuParams Parameters)
        {
            foreach (var item in Menu)
            {
                if (item.CommandId == CefMenuCommand.NotFound && string.IsNullOrWhiteSpace(item.Header))
                {
                    _ItemCollection.Add(new Separator());
                    continue;
                }

                var _MenuItem = new MenuItem
                {
                    Header = item.Header.Replace("&", "_"),
                    IsEnabled = item.IsEnabled,
                    Command = GetCommandByCefCommand(browser, item.CommandId, Parameters),
                };
                if (item.SubMenu != null)
                {
                    //MessageBox.Show(item.SubMenu.ToString());
                    //MessageBox.Show(item.SubMenu.ToString());
                    MenuClassToCollection(item.SubMenu, _MenuItem.Items, browser, Parameters);
                }
                _ItemCollection.Add(_MenuItem);
            }
        }

        public ICommand GetCommandByCefCommand(IBrowser browser, CefMenuCommand _Command, IContextMenuParams Parameters)
        {
            return new RelayCommand(() =>
            {
                bool ToStop = false;
                if (Parameters.IsEditable)
                {

                }
                else if (!string.IsNullOrEmpty(Parameters.SelectionText))
                {
                    string SelectedText = Parameters.SelectionText;
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (Utils.IsUrl(SelectedText))
                        {
                            if (_Command == (CefMenuCommand)26502)
                            {
                                MainWindow.Instance.NewBrowserTab(SelectedText, 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                                ToStop = true;
                            }
                        }
                        else
                        {
                            if (_Command == CefMenuCommand.Find)
                            {
                                MainWindow.Instance.NewBrowserTab(Utils.FixUrl(string.Format(MainWindow.Instance.MainSave.Get("Search_Engine"), SelectedText)), 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1); ;
                                ToStop = true;
                            }
                        }
                    }));
                }
                else
                {
                    if (Parameters.MediaType != ContextMenuMediaType.Image)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (_Command == (CefMenuCommand)26504)
                            {
                                MainWindow.Instance.NewBrowserTab($"view-source:{browser.MainFrame.Url}", 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26503)
                            {
                                MainWindow.Instance.Inspect();
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26501)
                            {
                                browser.StartDownload(browser.MainFrame.Url);
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26506)
                            {
                                MainWindow.Instance.Screenshot();
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26507)
                            {
                                MainWindow.Instance.Zoom(1);
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26508)
                            {
                                MainWindow.Instance.Zoom(-1);
                                ToStop = true;
                            }
                            else if (_Command == (CefMenuCommand)26509)
                            {
                                MainWindow.Instance.Zoom(0);
                                ToStop = true;
                            }
                        }));
                    }
                    else
                    {
                        if (_Command == CefMenuCommand.Copy)
                        {
                            try { Clipboard.SetDataObject(new Bitmap(new MemoryStream(MainWindow.Instance.TinyDownloader.DownloadData(Parameters.SourceUrl)))); }
                            catch { Clipboard.SetText(Parameters.SourceUrl); }
                            ToStop = true;
                        }
                        else if (_Command == (CefMenuCommand)26505)
                        {
                            Clipboard.SetText(Parameters.SourceUrl);
                            ToStop = true;
                        }
                        else if (_Command == (CefMenuCommand)26501)
                        {
                            browser.StartDownload(Parameters.SourceUrl);
                            ToStop = true;
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
                if (!ToStop)
                {
                    switch (_Command)
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
                                browser.GetHost().Find(Parameters.SelectionText, true, false, false);
                                break;
                            }
                        case CefMenuCommand.AddToDictionary:
                            {
                                browser.GetHost().AddWordToDictionary(Parameters.MisspelledWord);
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
                    }
                }
            });
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
            var _Browser = (ChromiumWebBrowser)chromiumWebBrowser;
            _Browser.Dispatcher.Invoke(() =>
            {
                _Browser.ContextMenu = null;
            });
        }

        private static IEnumerable<MenuClass> GetMenuItemsNew(IMenuModel model)
        {
            for (var i = 0; i < model.Count; i++)
            {
                var header = model.GetLabelAt(i);
                var commandId = model.GetCommandIdAt(i);
                var isEnabled = model.IsEnabledAt(i);
                List<MenuClass> SubMenu = new List<MenuClass>();
                if (model.GetSubMenuAt(i) != null)
                {
                    SubMenu = GetMenuItemsNew(model.GetSubMenuAt(i)).ToList();
                    /*foreach (MenuClass _Menu in SubMenu)
                    {
                        MessageBox.Show(_Menu.Header);
                    }*/
                }
                yield return new MenuClass(header, commandId, isEnabled, SubMenu);
            }
        }

        class MenuClass
        {
            public MenuClass(string _Header, CefMenuCommand _CommandId, bool _IsEnabled, IEnumerable<MenuClass> _SubMenu)
            {
                Header = _Header;
                CommandId = _CommandId;
                IsEnabled = _IsEnabled;
                SubMenu = _SubMenu;
            }

            public string Header;
            public CefMenuCommand CommandId;
            public bool IsEnabled;
            public IEnumerable<MenuClass> SubMenu { get; set; }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> commandHandler;
        private readonly Func<object, bool> canExecuteHandler;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> commandHandler, Func<object, bool> canExecuteHandler = null)
        {
            this.commandHandler = commandHandler;
            this.canExecuteHandler = canExecuteHandler;
        }
        public RelayCommand(Action commandHandler, Func<bool> canExecuteHandler = null)
            : this(_ => commandHandler(), canExecuteHandler == null ? null : new Func<object, bool>(_ => canExecuteHandler()))
        {
        }

        public void Execute(object parameter)
        {
            commandHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return
                canExecuteHandler == null ||
                canExecuteHandler(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public class RelayCommand<T> : RelayCommand
    {
        public RelayCommand(Action<T> commandHandler, Func<T, bool> canExecuteHandler = null)
            : base(o => commandHandler(o is T t ? t : default(T)), canExecuteHandler == null ? null : new Func<object, bool>(o => canExecuteHandler(o is T t ? t : default(T))))
        {
        }
    }

    class ContextMenuParams : IContextMenuParams
    {
        public ContextMenuParams(IContextMenuParams Parameters)
        {
            YCoord = Parameters.YCoord;
            XCoord = Parameters.XCoord;
            TypeFlags = Parameters.TypeFlags;
            LinkUrl = Parameters.LinkUrl;
            UnfilteredLinkUrl = Parameters.UnfilteredLinkUrl;
            SourceUrl = Parameters.SourceUrl;
            HasImageContents = Parameters.HasImageContents;
            PageUrl = Parameters.PageUrl;
            FrameUrl = Parameters.FrameUrl;
            FrameCharset = Parameters.FrameCharset;
            MediaType = Parameters.MediaType;
            MediaStateFlags = Parameters.MediaStateFlags;
            SelectionText = Parameters.SelectionText;
            MisspelledWord = Parameters.MisspelledWord;
            DictionarySuggestions = Parameters.DictionarySuggestions;
            IsEditable = Parameters.IsEditable;
            IsSpellCheckEnabled = Parameters.IsSpellCheckEnabled;
            EditStateFlags = Parameters.EditStateFlags;
            IsCustomMenu = Parameters.IsCustomMenu;
            IsDisposed = Parameters.IsDisposed;
        }

        private bool disposedValue;

        public int YCoord { get; set; }

        public int XCoord { get; set; }

        public ContextMenuType TypeFlags { get; set; }

        public string LinkUrl { get; set; }

        public string UnfilteredLinkUrl { get; set; }

        public string SourceUrl { get; set; }

        public bool HasImageContents { get; set; }

        public string PageUrl { get; set; }

        public string FrameUrl { get; set; }

        public string FrameCharset { get; set; }

        public ContextMenuMediaType MediaType { get; set; }

        public ContextMenuMediaState MediaStateFlags { get; set; }

        public string SelectionText { get; set; }

        public string MisspelledWord { get; set; }

        public List<string> DictionarySuggestions { get; set; }

        public bool IsEditable { get; set; }

        public bool IsSpellCheckEnabled { get; set; }

        public ContextMenuEditState EditStateFlags { get; set; }

        public bool IsCustomMenu { get; set; }

        public bool IsDisposed { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ContextMenuParams()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
