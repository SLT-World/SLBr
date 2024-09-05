using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.UI.ViewManagement.Core;

namespace SLBr.Handlers
{
    public class LimitedContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
            if (parameters.IsEditable)
            {
                if (parameters.DictionarySuggestions.Count != 0 && bool.Parse(App.Instance.GlobalSave.Get("SpellCheck")))
                {
                    switch (parameters.DictionarySuggestions.Count)
                    {
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
                    model.AddItem(CefMenuCommand.AddToDictionary, "Ignore Word");
                    model.AddSeparator();
                }
                model.AddItem((CefMenuCommand)26510, "Emoji");
                model.AddSeparator();
                model.AddItem(CefMenuCommand.Undo, "Undo");
                model.AddItem(CefMenuCommand.Redo, "Redo");
                model.AddSeparator();
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
                    int SuggestionIndex = ((int)CommandID) - (int)CefMenuCommand.SpellCheckSuggestion0;
                    if (SuggestionIndex < Parameters.DictionarySuggestions.Count)
                        Browser.ReplaceMisspelling(Parameters.DictionarySuggestions[SuggestionIndex]);
                    ToReturn = true;
                }
                else if (CommandID == CefMenuCommand.AddToDictionary)
                {
                    Browser.AddWordToDictionary(Parameters.MisspelledWord);
                    ToReturn = true;
                }
                else if (CommandID == (CefMenuCommand)26510)
                    CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
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
            if (model.Count != 0)
            {
                var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;
                var menuItems = GetMenuItemsNew(model).ToList();
                ContextMenuParams Parameters = new ContextMenuParams(parameters);

                webBrowser.Dispatcher.Invoke(() =>
                {
                    var menu = new ContextMenu { IsOpen = true, Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse };
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
            }
            return true;
        }
        void MenuClassToCollection(IEnumerable<MenuClass> Menu, ItemCollection _ItemCollection, IWebBrowser chromiumWebBrowser, IBrowser browser, ContextMenuParams Parameters)
        {
            foreach (MenuClass Item in Menu)
            {
                if (Item.CommandId == CefMenuCommand.NotFound && string.IsNullOrWhiteSpace(Item.Header))
                {
                    _ItemCollection.Add(new Separator());
                    continue;
                }

                var _MenuItem = new MenuItem
                {
                    Header = Item.Header.Replace("&", "_"),
                    Command = GetCommandByCefCommand(chromiumWebBrowser, browser, Item.CommandId, Parameters)
                };
                //_MenuItem.SetResourceReference(Control.ForegroundProperty, "White");
                //_MenuItem.SetResourceReference(Control.StyleProperty, (Style)MainWindow.Instance.Resources["ApplyableMenuItemStyle"]);
                //_MenuItem.SetResourceReference(Control.OverridesDefaultStyleProperty, true);

                if (Item.CommandId == CefMenuCommand.Back)
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoBack;
                else if (Item.CommandId == CefMenuCommand.Forward)
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoForward;
                else if (Item.CommandId == CefMenuCommand.Delete)
                    _MenuItem.IsEnabled = !string.IsNullOrEmpty(Parameters.SelectionText);

                else if (Item.CommandId == CefMenuCommand.Undo)
                    _MenuItem.InputGestureText = "Ctrl+Z";
                else if (Item.CommandId == CefMenuCommand.Redo)
                    _MenuItem.InputGestureText = "Ctrl+Y";
                else if (Item.CommandId == CefMenuCommand.Cut)
                    _MenuItem.InputGestureText = "Ctrl+X";
                else if (Item.CommandId == CefMenuCommand.Copy)
                    _MenuItem.InputGestureText = "Ctrl+C";
                else if (Item.CommandId == CefMenuCommand.Paste)
                    _MenuItem.InputGestureText = "Ctrl+V";
                else if (Item.CommandId == CefMenuCommand.SelectAll)
                    _MenuItem.InputGestureText = "Ctrl+A";
                else if (Item.CommandId == (CefMenuCommand)26510)
                    _MenuItem.InputGestureText = "Win+Period";
                else if (Item.CommandId == CefMenuCommand.SpellCheckSuggestion0 || Item.CommandId == CefMenuCommand.SpellCheckSuggestion1 || Item.CommandId == CefMenuCommand.SpellCheckSuggestion2 || Item.CommandId == CefMenuCommand.SpellCheckSuggestion3 || Item.CommandId == CefMenuCommand.SpellCheckSuggestion4)
                    _MenuItem.Icon = "\uf87b";
                else if (Item.CommandId == CefMenuCommand.AddToDictionary)
                    _MenuItem.Icon = "\uecc9";

                //_MenuItem.SetResourceReference(Control.ForegroundProperty, "White");
                //_MenuItem.SetResourceReference(Control.StyleProperty, (Style)MainWindow.Instance.Resources["ApplyableMenuItemStyle"]);
                //_MenuItem.SetResourceReference(Control.OverridesDefaultStyleProperty, true);
                if (Item.SubMenu != null)
                    MenuClassToCollection(Item.SubMenu, _MenuItem.Items, chromiumWebBrowser, browser, Parameters);
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
                List<MenuClass> SubMenu = new List<MenuClass>();
                if (model.GetSubMenuAt(i) != null)
                    SubMenu = GetMenuItemsNew(model.GetSubMenuAt(i)).ToList();
                yield return new MenuClass(model.GetLabelAt(i), model.GetCommandIdAt(i), model.IsEnabledAt(i), SubMenu);
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
