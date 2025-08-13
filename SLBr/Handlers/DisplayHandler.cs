using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using SLBr.Pages;

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
        }

        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, Size newSize)
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
            if (urls.Count != 0 && bool.Parse(App.Instance.GlobalSave.Get("Favicons")) && !_BrowserView.Private)
            {
                urls = urls.OrderBy(url => url.EndsWith(".ico", StringComparison.Ordinal) ? 0 : url.EndsWith(".png", StringComparison.Ordinal) ? 1 : 2).ToList();
                if (!urls[0].EndsWith(".svg", StringComparison.Ordinal))
                {
                    App.Current.Dispatcher.Invoke(async () =>
                    {
                        _BrowserView.Tab.Icon = await App.Instance.SetIcon(urls[0], chromiumWebBrowser.Address);
                    });
                }
            }
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Instance.CurrentFocusedWindow().Fullscreen(fullscreen, _BrowserView);
            });
        }

        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress)
        {
            /*App.Current.Dispatcher.Invoke(() =>
            {
                _BrowserView.LoadingBar.IsEnabled = progress != 1;
                _BrowserView.LoadingBar.Value = progress != 1 ? progress : 0;
            });*/
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
