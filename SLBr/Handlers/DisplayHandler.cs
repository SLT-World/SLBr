using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SLBr.Handlers
{
    public class DisplayHandler : IDisplayHandler
    {
        Browser _BrowserView;
        public DisplayHandler(Browser BrowserView)
        {
            _BrowserView = BrowserView;
        }

        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                string OutputUrl = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, bool.Parse(App.Instance.MainSave.Get("FullAddress")) ? addressChangedArgs.Address : Utils.CleanUrl(addressChangedArgs.Address));
                if (_BrowserView.AddressBox.Text != OutputUrl)
                {
                    if (_BrowserView.CanChangeAddressBox())
                    {
                        _BrowserView.AddressBox.Text = OutputUrl;
                        _BrowserView.AddressBoxPlaceholder.Text = "";
                    }
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
            /*Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                foreach (string x in urls)
                {
                    _BrowserView.Tab.Icon = new BitmapImage(new Uri(x));
                }
            }));*/
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                App.Instance.CurrentFocusedWindow().Fullscreen(fullscreen);
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
