using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SLBr.Handlers
{
    public class LimitedContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
            if (parameters.IsEditable)
            {
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
                model.AddSeparator();
                model.AddItem(CefMenuCommand.SelectAll, "Select All");
            }
            else if (!string.IsNullOrEmpty(parameters.LinkUrl))
            {
                model.AddItem((CefMenuCommand)26502, "Open in new tab");
                model.AddItem(CefMenuCommand.Copy, "Copy link");
            }
            else if (!string.IsNullOrEmpty(parameters.SelectionText))
            {
                model.AddItem(CefMenuCommand.Find, $"Search \"{parameters.SelectionText}\" in new tab");
                model.AddItem(CefMenuCommand.Copy, "Copy");
            }
            else
            {
                if (parameters.MediaType == ContextMenuMediaType.Image)
                {
                    model.AddItem(CefMenuCommand.Copy, "Copy image");
                    model.AddItem((CefMenuCommand)26505, "Copy image link");
                    model.AddItem((CefMenuCommand)26501, "Save image as");
                }
            }
        }

        private async void DownloadAndCopyImage(string imageUrl)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    byte[] imageData = await client.GetByteArrayAsync(imageUrl);
                    if (imageData != null)
                    {
                        using (MemoryStream stream = new MemoryStream(imageData))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            Clipboard.SetImage(bitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy image: {ex.Message}");
            }
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return ExecuteContextMenu(chromiumWebBrowser, browser, commandId, parameters);
        }

        public bool ExecuteContextMenu(IWebBrowser WebBrowser, IBrowser Browser, CefMenuCommand CommandID, IContextMenuParams Parameters)
        {
            if (CommandID == CefMenuCommand.NotFound)
                return false;
            bool ToReturn = false;
            if (Parameters.IsEditable)
            {
                if (CommandID >= CefMenuCommand.SpellCheckSuggestion0 && CommandID <= CefMenuCommand.SpellCheckSuggestion4)
                {
                    int sugestionIndex = ((int)CommandID) - (int)CefMenuCommand.SpellCheckSuggestion0;
                    if (sugestionIndex < Parameters.DictionarySuggestions.Count)
                    {
                        var suggestion = Parameters.DictionarySuggestions[sugestionIndex];
                        Browser.ReplaceMisspelling(suggestion);
                    }
                    ToReturn = true;
                }
            }
            else if (!string.IsNullOrEmpty(Parameters.LinkUrl))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (CommandID == (CefMenuCommand)26502)
                    {
                        App.Instance.CurrentFocusedWindow().NewTab(Parameters.LinkUrl, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
                        ToReturn = true;
                    }
                    else if (CommandID == CefMenuCommand.Copy)
                    {
                        Clipboard.SetText(Parameters.LinkUrl);
                        ToReturn = true;
                    }
                });
            }
            else if (!string.IsNullOrEmpty(Parameters.SelectionText))
            {
                string SelectedText = Parameters.SelectionText;
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (CommandID == CefMenuCommand.Find)
                    {
                        App.Instance.CurrentFocusedWindow().NewTab(Utils.FixUrl(string.Format(App.Instance.GlobalSave.Get("SearchEngine"), SelectedText)), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1); ;
                        ToReturn = true;
                    }
                });
            }
            else
            {
                if (Parameters.MediaType == ContextMenuMediaType.Image)
                {
                    if (CommandID == CefMenuCommand.Copy)
                    {
                        try { DownloadAndCopyImage(Parameters.SourceUrl); }
                        catch { Clipboard.SetText(Parameters.SourceUrl); }
                        ToReturn = true;
                    }
                    else if (CommandID == (CefMenuCommand)26505)
                    {
                        Clipboard.SetText(Parameters.SourceUrl);
                        ToReturn = true;
                    }
                    else if (CommandID == (CefMenuCommand)26501)
                    {
                        Browser.StartDownload(Parameters.SourceUrl);
                        ToReturn = true;
                    }
                    /*else if (commandId == (CefMenuCommand)26502)
                    {
                        chromiumWebBrowser.StartDownload(parameters.SourceUrl);
                        //string DownloadPath = MainWindow.Instance.GlobalSave.Get("DownloadPath");
                        //string FileName = Path.Combine(DownloadPath, Path.GetFileName(parameters.SourceUrl));
                        Process.Start(FileName);
                    }*/
                }
            }
            if (!ToReturn)
            {
                switch (CommandID)
                {
                    case CefMenuCommand.Back:
                        {
                            Browser.GoBack();
                            break;
                        }
                    case CefMenuCommand.Forward:
                        {
                            Browser.GoForward();
                            break;
                        }
                    case CefMenuCommand.StopLoad:
                        {
                            Browser.StopLoad();
                            break;
                        }
                    case CefMenuCommand.Reload:
                        {
                            Browser.Reload();
                            break;
                        }
                    case CefMenuCommand.ReloadNoCache:
                        {
                            Browser.Reload(ignoreCache: true);
                            break;
                        }

                    case CefMenuCommand.Undo:
                        {
                            Browser.FocusedFrame.Undo();
                            break;
                        }
                    case CefMenuCommand.Redo:
                        {
                            Browser.FocusedFrame.Redo();
                            break;
                        }
                    case CefMenuCommand.Cut:
                        {
                            Browser.FocusedFrame.Cut();
                            break;
                        }
                    case CefMenuCommand.Copy:
                        {
                            Browser.FocusedFrame.Copy();
                            break;
                        }
                    case CefMenuCommand.Paste:
                        {
                            Browser.FocusedFrame.Paste();
                            break;
                        }
                    case CefMenuCommand.Delete:
                        {
                            Browser.FocusedFrame.Delete();
                            break;
                        }
                    case CefMenuCommand.SelectAll:
                        {
                            Browser.FocusedFrame.SelectAll();
                            break;
                        }

                    case CefMenuCommand.Print:
                        {
                            Browser.GetHost().Print();
                            break;
                        }
                    case CefMenuCommand.ViewSource:
                        {
                            App.Instance.CurrentFocusedWindow().NewTab($"view-source:{WebBrowser.Address}", true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
                            //Browser.FocusedFrame.ViewSource();
                            break;
                        }
                    case CefMenuCommand.Find:
                        {
                            Browser.GetHost().Find(Parameters.SelectionText, true, false, false);
                            break;
                        }
                    case CefMenuCommand.AddToDictionary:
                        {
                            Browser.GetHost().AddWordToDictionary(Parameters.MisspelledWord);
                            break;
                        }
                }
            }
            return ToReturn;
        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            if (model.Count == 0) return true;
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
                menu.OverridesDefaultStyle = true;

                MenuClassToCollection(menuItems, menu.Items, chromiumWebBrowser, browser, Parameters);
                webBrowser.ContextMenu = menu;
            });
            Parameters.Dispose();
            return true;
        }
        void MenuClassToCollection(IEnumerable<MenuClass> Menu, ItemCollection _ItemCollection, IWebBrowser chromiumWebBrowser, IBrowser browser, ContextMenuParams Parameters)
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
                    Command = GetCommandByCefCommand(chromiumWebBrowser, browser, item.CommandId, Parameters)
                };
                //_MenuItem.SetResourceReference(Control.ForegroundProperty, "White");
                //_MenuItem.SetResourceReference(Control.StyleProperty, (Style)MainWindow.Instance.Resources["ApplyableMenuItemStyle"]);
                //_MenuItem.SetResourceReference(Control.OverridesDefaultStyleProperty, true);
                if (item.SubMenu != null)
                    MenuClassToCollection(item.SubMenu, _MenuItem.Items, chromiumWebBrowser, browser, Parameters);
                _ItemCollection.Add(_MenuItem);
            }
        }

        public ICommand GetCommandByCefCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, CefMenuCommand _Command, IContextMenuParams Parameters)
        {
            return new RelayCommand(() =>
            {
                ExecuteContextMenu(chromiumWebBrowser, browser, _Command, Parameters);
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
}
