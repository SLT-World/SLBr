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
    public class ContextMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams Parameters, IMenuModel model)
        {
            model.Clear();
            if (Parameters.IsEditable)
            {
                if (Parameters.IsSpellCheckEnabled && Parameters.DictionarySuggestions.Count != 0)
                {
                    switch (Parameters.DictionarySuggestions.Count)
                    {
                        case 1:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            break;
                        case 2:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion1, Parameters.DictionarySuggestions[1]);
                            break;
                        case 3:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion1, Parameters.DictionarySuggestions[1]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion2, Parameters.DictionarySuggestions[2]);
                            break;
                        case 4:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion1, Parameters.DictionarySuggestions[1]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion2, Parameters.DictionarySuggestions[2]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion3, Parameters.DictionarySuggestions[3]);
                            break;
                        case 5:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion1, Parameters.DictionarySuggestions[1]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion2, Parameters.DictionarySuggestions[2]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion3, Parameters.DictionarySuggestions[3]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion4, Parameters.DictionarySuggestions[4]);
                            break;
                        default:
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion0, Parameters.DictionarySuggestions[0]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion1, Parameters.DictionarySuggestions[1]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion2, Parameters.DictionarySuggestions[2]);
                            model.AddItem(CefMenuCommand.SpellCheckSuggestion3, Parameters.DictionarySuggestions[3]);
                            break;
                    }
                    model.AddItem(CefMenuCommand.AddToDictionary, "Ignore Word");
                    model.AddSeparator();
                }
                model.AddItem(MenuEmoji, "Emoji");
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
                if (!string.IsNullOrEmpty(Parameters.SelectionText))
                {
                    model.AddSeparator();
                    model.AddItem(CefMenuCommand.Find, $"Search \"{Parameters.SelectionText}\" in new tab");
                }
            }
            else if (!string.IsNullOrEmpty(Parameters.LinkUrl))
            {
                model.AddItem(MenuOpenNewTab, "Open in new tab");
                model.AddItem(MenuCopyURL, "Copy link");
            }
            else if (!string.IsNullOrEmpty(Parameters.SelectionText))
            {
                model.AddItem(CefMenuCommand.Find, $"Search \"{Parameters.SelectionText}\" in new tab");
                model.AddItem(CefMenuCommand.Copy, "Copy");
                model.AddSeparator();
                model.AddItem(CefMenuCommand.SelectAll, "Select All");
            }
            else
            {
                if (Parameters.MediaType == ContextMenuMediaType.None)
                {
                    model.AddItem(CefMenuCommand.Back, "Back");
                    model.AddItem(CefMenuCommand.Forward, "Forward");
                    model.AddItem(CefMenuCommand.Reload, "Refresh");

                    model.AddSeparator();
                    model.AddItem(MenuSaveAs, "Save as");
                    model.AddItem(CefMenuCommand.Print, "Print");
                    model.AddItem(CefMenuCommand.SelectAll, "Select All");
                    model.AddSeparator();

                    model.AddItem(MenuTranslate, "Translate");
                    model.AddItem(MenuScreenshot, "Screenshot");

                    IMenuModel ZoomSubMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Zoom");
                    ZoomSubMenuModel.AddItem(MenuZoomIn, "Zoom in");
                    ZoomSubMenuModel.AddItem(MenuZoomOut, "Zoom out");
                    ZoomSubMenuModel.AddItem(MenuZoomReset, "Reset");
                    /*model.AddSeparator();
                    model.AddItem((CefMenuCommand)26507, "Zoom");
                    model.AddItem((CefMenuCommand)26508, "Zoom out");
                    model.AddItem((CefMenuCommand)26509, "Reset zoom");*/

                    model.AddSeparator();

                    IMenuModel AdvancedMenuModel = model.AddSubMenu(CefMenuCommand.NotFound, "Advanced");
                    AdvancedMenuModel.AddItem(CefMenuCommand.ViewSource, "View source");
                    AdvancedMenuModel.AddItem(MenuInspector, "Inspect");

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
                else if (Parameters.MediaType == ContextMenuMediaType.Image)
                {
                    model.AddItem(CefMenuCommand.Copy, "Copy image");
                    model.AddItem(MenuCopyURL, "Copy image link");
                    model.AddItem(MenuSaveAs, "Save image as");
                    //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
                }
                else if (Parameters.MediaType == ContextMenuMediaType.Video)
                {
                    model.AddItem(MenuCopyURL, "Copy video link");
                    model.AddItem(MenuSaveAs, "Save video as");
                    //if (Parameters.MediaStateFlags == ContextMenuMediaState.CanPictureInPicture)
                    model.AddItem(MenuPictureInPicture, "Picture in picture");
                }
            }
        }

        CefMenuCommand MenuSaveAs = (CefMenuCommand)26501;
        CefMenuCommand MenuOpenNewTab = (CefMenuCommand)26502;
        CefMenuCommand MenuInspector = (CefMenuCommand)26503;
        CefMenuCommand MenuCopyURL = (CefMenuCommand)26504;
        CefMenuCommand MenuScreenshot = (CefMenuCommand)26505;
        CefMenuCommand MenuEmoji = (CefMenuCommand)26506;
        CefMenuCommand MenuTranslate = (CefMenuCommand)26507;
        CefMenuCommand MenuPictureInPicture = (CefMenuCommand)26508;

        CefMenuCommand MenuZoomReset = (CefMenuCommand)26510;
        CefMenuCommand MenuZoomIn = (CefMenuCommand)26511;
        CefMenuCommand MenuZoomOut = (CefMenuCommand)26512;

        private async void DownloadAndCopyImage(string ImageUrl)
        {
            try
            {
                using (var HttpClient = new HttpClient())
                {
                    byte[] ImageData = await HttpClient.GetByteArrayAsync(ImageUrl);
                    if (ImageData != null)
                    {
                        using (MemoryStream stream = new MemoryStream(ImageData))
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
            catch { }
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
                else if (CommandID == CefMenuCommand.Find)
                {
                    string SelectedText = Parameters.SelectionText;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        App.Instance.CurrentFocusedWindow().NewTab(Utils.FixUrl(string.Format(App.Instance.GlobalSave.Get("SearchEngine"), SelectedText)), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
                    });
                    ToReturn = true;
                }
                else if (CommandID == MenuEmoji)
                    CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            }
            else if (!string.IsNullOrEmpty(Parameters.LinkUrl))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (CommandID == MenuOpenNewTab)
                    {
                        App.Instance.CurrentFocusedWindow().NewTab(Parameters.LinkUrl, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
                        ToReturn = true;
                    }
                    else if (CommandID == MenuCopyURL)
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
                if (Parameters.MediaType == ContextMenuMediaType.None)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (CommandID == MenuInspector)
                        {
                            App.Instance.CurrentFocusedWindow().DevTools("");//, Parameters.XCoord, Parameters.YCoord);
                            ToReturn = true;
                        }
                        else if (CommandID == MenuSaveAs)
                        {
                            Browser.StartDownload(WebBrowser.Address);
                            ToReturn = true;
                        }
                        else if (CommandID == MenuTranslate)
                        {
                            Pages.Browser BrowserView = App.Instance.CurrentFocusedWindow().GetTab().Content;
                            BrowserView.Address = $"https://translate.google.com/translate?sl=auto&tl=en&hl=en&u={BrowserView.Address}";
                            ToReturn = true;
                        }
                        else if (CommandID == MenuScreenshot)
                        {
                            App.Instance.CurrentFocusedWindow().Screenshot();
                            ToReturn = true;
                        }
                        else if (CommandID == MenuZoomIn)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(1);
                            ToReturn = true;
                        }
                        else if (CommandID == MenuZoomOut)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(-1);
                            ToReturn = true;
                        }
                        else if (CommandID == MenuZoomReset)
                        {
                            App.Instance.CurrentFocusedWindow().Zoom(0);
                            ToReturn = true;
                        }
                    });
                }
                else if (Parameters.MediaType == ContextMenuMediaType.Image)
                {
                    if (CommandID == CefMenuCommand.Copy)
                    {
                        try { DownloadAndCopyImage(Parameters.SourceUrl); }
                        catch { Clipboard.SetText(Parameters.SourceUrl); }
                        ToReturn = true;
                    }
                    else if (CommandID == MenuCopyURL)
                    {
                        Clipboard.SetText(Parameters.SourceUrl);
                        ToReturn = true;
                    }
                    else if (CommandID == MenuSaveAs)
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
                else if (Parameters.MediaType == ContextMenuMediaType.Video)
                {
                    if (CommandID == MenuCopyURL)
                    {
                        Clipboard.SetText(Parameters.SourceUrl);
                        ToReturn = true;
                    }
                    else if (CommandID == MenuSaveAs)
                    {
                        Browser.StartDownload(Parameters.SourceUrl);
                        ToReturn = true;
                    }
                    else if (CommandID == MenuPictureInPicture)
                    {
                        Browser.ExecuteScriptAsync("(async()=>{let playingVideo=Array.from(document.querySelectorAll('video')).find(v=>!v.paused&&!v.ended&&v.readyState>2);if (!playingVideo){playingVideo=document.querySelector('video');}if (playingVideo&&document.pictureInPictureEnabled){await playingVideo.requestPictureInPicture();}})();");
                        ToReturn = true;
                    }
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

                if (Item.CommandId == CefMenuCommand.Back)
                {
                    _MenuItem.Icon = "\uE76B";
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoBack;
                }
                else if (Item.CommandId == CefMenuCommand.Forward)
                {
                    _MenuItem.Icon = "\uE76C";
                    _MenuItem.IsEnabled = chromiumWebBrowser.CanGoForward;
                }
                else if (Item.CommandId == CefMenuCommand.Reload)
                    _MenuItem.Icon = "\uE72C";

                else if (Item.CommandId == CefMenuCommand.Print)
                    _MenuItem.Icon = "\uE749";
                else if (Item.CommandId == CefMenuCommand.ViewSource)
                    _MenuItem.Icon = "\ue943";

                else if (Item.Header == "Advanced")
                    _MenuItem.Icon = "\uec7a";

                else if (Item.CommandId == MenuInspector)
                    _MenuItem.Icon = "\uec7a";
                //_MenuItem.Icon = "\ue929";
                else if (Item.CommandId == MenuSaveAs)
                    _MenuItem.Icon = "\ue792";
                else if (Item.CommandId == MenuScreenshot)
                    _MenuItem.Icon = "\uE924";
                else if (Item.CommandId == MenuZoomIn)
                    _MenuItem.Icon = "\ue8a3";
                else if (Item.CommandId == MenuZoomOut)
                    _MenuItem.Icon = "\ue71f";
                else if (Item.CommandId == MenuZoomReset)
                    _MenuItem.Icon = "\ue72c";
                else if (Item.CommandId == MenuPictureInPicture)
                    _MenuItem.Icon = "\uee49";
                else if (Item.CommandId == MenuTranslate)
                {
                    _MenuItem.Icon = "\uE8C1";
                    _MenuItem.IsEnabled = Utils.IsHttpScheme(browser.MainFrame.Url);
                }

                else if (Item.CommandId == MenuOpenNewTab)
                    _MenuItem.Icon = "\uE8A7";
                else if (Item.CommandId == MenuCopyURL)
                    _MenuItem.Icon = "\ue71b";

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
                else if (Item.CommandId == CefMenuCommand.Delete)
                    _MenuItem.IsEnabled = !string.IsNullOrEmpty(Parameters.SelectionText);

                else if (Item.CommandId == MenuEmoji)
                    _MenuItem.InputGestureText = "Win+Period";

                else if (Item.Header.StartsWith("Search", StringComparison.Ordinal))
                    _MenuItem.Icon = "\uF6Fa";
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

    public class RelayCommand : ICommand
    {
        private readonly Action<object> CommandHandler;
        private readonly Func<object, bool> CanExecuteHandler;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> _CommandHandler, Func<object, bool> _CanExecuteHandler = null)
        {
            CommandHandler = _CommandHandler;
            CanExecuteHandler = _CanExecuteHandler;
        }
        public RelayCommand(Action commandHandler, Func<bool> canExecuteHandler = null) : this(_ => commandHandler(), canExecuteHandler == null ? null : new Func<object, bool>(_ => canExecuteHandler()))
        {
        }

        public void Execute(object parameter)
        {
            CommandHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteHandler == null || CanExecuteHandler(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
    }

    /*public class RelayCommand<T> : RelayCommand
    {
        public RelayCommand(Action<T> commandHandler, Func<T, bool> canExecuteHandler = null) : base(o => commandHandler(o is T t ? t : default(T)), canExecuteHandler == null ? null : new Func<object, bool>(o => canExecuteHandler(o is T t ? t : default(T))))
        {
        }
    }*/

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
