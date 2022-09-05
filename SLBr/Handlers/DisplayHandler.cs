using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr.Handlers
{
    public class DisplayHandler : IDisplayHandler
    {
        Browser _BrowserView;
        public DisplayHandler(Browser BrowserView)
        {
            _BrowserView = BrowserView;
        }

        //Setter HideTabSetter = new Setter(UIElement.VisibilityProperty, Visibility.Collapsed);
        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                string OutputUrl = Utils.ConvertUrlToReadableUrl(MainWindow.Instance._IdnMapping, bool.Parse(MainWindow.Instance.MainSave.Get("FullAddress")) ? addressChangedArgs.Address : Utils.CleanUrl(addressChangedArgs.Address));
                if (_BrowserView.AddressBox.Text != OutputUrl)
                {
                    if (_BrowserView.CanChangeAddressBox())
                        _BrowserView.AddressBox.Text = OutputUrl;
                    _BrowserView.AddressBox.Tag = addressChangedArgs.Address;
                }
            }));
        }

        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.Structs.Size newSize)
        {
            return false;
        }

        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs)
        {
            return false;
        }

        public bool OnCursorChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
            return false;
        }

        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls)
        {
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Fullscreen(fullscreen);
            }));
        }

        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                _BrowserView.WebsiteLoadingProgressBar.IsEnabled = progress != 1;
                _BrowserView.WebsiteLoadingProgressBar.Value = progress != 1 ? progress : 0;
            }));
        }

        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs)
        {
        }

        public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs)
        {
        }

        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text)
        {
            return false;
        }
    }
}
