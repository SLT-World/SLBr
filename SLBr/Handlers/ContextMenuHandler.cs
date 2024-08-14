using CefSharp;
using CefSharp.Wpf.HwndHost;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.UI.ViewManagement.Core;

namespace SLBr.Handlers
{
    public class ContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
            if (parameters.IsEditable)
            {
                if (bool.Parse(App.Instance.GlobalSave.Get("SpellCheck")))
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
                if (!string.IsNullOrEmpty(parameters.SelectionText))
                {
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.Find, $"Search \"{parameters.SelectionText}\" in new tab");
                }
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
                if (parameters.MediaType != ContextMenuMediaType.Image)//parameters.MediaType == ContextMenuMediaType.None
                {
                    model.AddItem(CefMenuCommand.Back, "Back");
                    model.AddItem(CefMenuCommand.Forward, "Forward");
                    model.AddItem(CefMenuCommand.Reload, "Refresh");

                    model.AddSeparator();
                    model.AddItem((CefMenuCommand)26501, "Save as");
                    model.AddItem(CefMenuCommand.Print, "Print");
                    model.AddSeparator();

                    model.AddItem((CefMenuCommand)26506, "Screenshot");

                    IMenuModel ZoomSubMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Zoom");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26507, "Zoom in");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26508, "Zoom out");
                    ZoomSubMenuModel.AddItem((CefMenuCommand)26509, "Reset");
                    /*model.AddSeparator();
                    model.AddItem((CefMenuCommand)26507, "Zoom");
                    model.AddItem((CefMenuCommand)26508, "Zoom out");
                    model.AddItem((CefMenuCommand)26509, "Reset zoom");*/

                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.ViewSource, "View page source");
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
                    model.AddItem(CefMenuCommand.Copy, "Copy image");
                    model.AddItem((CefMenuCommand)26505, "Copy image link");
                    model.AddItem((CefMenuCommand)26501, "Save image as");
                    //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
                }
            }
            /*model.AddSeparator();
            model.AddItem(CefMenuCommand.NotFound, "Cancel");*/
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
                else if (CommandID == CefMenuCommand.Find)
                {
                    string SelectedText = Parameters.SelectionText;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        App.Instance.CurrentFocusedWindow().NewTab(Utils.FixUrl(string.Format(App.Instance.GlobalSave.Get("SearchEngine"), SelectedText)), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1); ;
                        ToReturn = true;
                    });
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
                if (Parameters.MediaType != ContextMenuMediaType.Image)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (CommandID == (CefMenuCommand)26503)
                        {
                            App.Instance.CurrentFocusedWindow().DevTools("", Parameters.XCoord, Parameters.YCoord);
                            ToReturn = true;
                        }
                        else if (CommandID == (CefMenuCommand)26501)
                        {
                            Browser.StartDownload(WebBrowser.Address);
                            ToReturn = true;
                        }
                        else if (CommandID == (CefMenuCommand)26506)
                        {
                            App.Instance.CurrentFocusedWindow().Screenshot();
                            ToReturn = true;
                        }
                        else if (CommandID == (CefMenuCommand)26507)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(1);
                            ToReturn = true;
                        }
                        else if (CommandID == (CefMenuCommand)26508)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(-1);
                            ToReturn = true;
                        }
                        else if (CommandID == (CefMenuCommand)26509)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(0);
                            ToReturn = true;
                        }
                    });
                }
                else
                {
                    if (CommandID == CefMenuCommand.Copy)
                    {
                        DownloadAndCopyImage(Parameters.SourceUrl);
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
                    Command = GetCommandByCefCommand(chromiumWebBrowser, browser, item.CommandId, Parameters)
                };

                if (item.CommandId == CefMenuCommand.Back)
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoBack;
                else if (item.CommandId == CefMenuCommand.Forward)
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoForward;
                else if (item.CommandId == CefMenuCommand.Delete)
                    _MenuItem.IsEnabled = !string.IsNullOrEmpty(Parameters.SelectionText);

                else if (item.CommandId == CefMenuCommand.Undo)
                    _MenuItem.InputGestureText = "Ctrl+Z";
                else if (item.CommandId == CefMenuCommand.Redo)
                    _MenuItem.InputGestureText = "Ctrl+Y";
                else if (item.CommandId == CefMenuCommand.Cut)
                    _MenuItem.InputGestureText = "Ctrl+X";
                else if (item.CommandId == CefMenuCommand.Copy)
                    _MenuItem.InputGestureText = "Ctrl+C";
                else if (item.CommandId == CefMenuCommand.Paste)
                    _MenuItem.InputGestureText = "Ctrl+V";
                else if (item.CommandId == CefMenuCommand.SelectAll)
                    _MenuItem.InputGestureText = "Ctrl+A";
                else if (item.CommandId == (CefMenuCommand)26510)
                    _MenuItem.InputGestureText = "Win+Period";
                else if (item.Header.StartsWith("Search"))
                    _MenuItem.Icon = "\uF6Fa";

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
                    SubMenu = GetMenuItemsNew(model.GetSubMenuAt(i)).ToList();
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
                disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
