using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using SLBr.Pages;
using System.Security.Policy;

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
            /*if (!addressChangedArgs.Address.StartsWith("devtools://devtools/", StringComparison.Ordinal))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _BrowserView.AddNavigationEntry(addressChangedArgs.Address, ((ChromiumWebBrowser)chromiumWebBrowser).Title, _BrowserView.CurrentNavigationEntry);
                    /*string OutputUrl = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, bool.Parse(App.Instance.GlobalSave.Get("FullAddress")) ? addressChangedArgs.Address : Utils.CleanUrl(addressChangedArgs.Address));
                    if (_BrowserView.OmniBox.Text != OutputUrl)
                    {
                        if (_BrowserView.IsOmniBoxModifiable())
                        {
                            _BrowserView.OmniBox.Text = OutputUrl;
                            _BrowserView.OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                        }
                        _BrowserView.OmniBox.Tag = addressChangedArgs.Address;
                    }*/
            //});
            //}
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
            if (urls.Count != 0 && bool.Parse(App.Instance.GlobalSave.Get("Favicons")))
            {
                urls = urls.OrderBy(url => url.EndsWith(".ico") ? 0 : url.EndsWith(".png") ? 1 : 2).ToList();
                //System.Windows.MessageBox.Show(string.Join(" | ", urls));
                App.Current.Dispatcher.Invoke(async () =>
                {
                    if (Utils.GetFileExtensionFromUrl(urls[0]) != ".svg")
                        _BrowserView.Tab.Icon = await App.Instance.SetIcon(urls[0], chromiumWebBrowser.Address);
                    /*foreach (string url in urls)
                    {
                        if (Utils.GetFileExtensionFromUrl(url) != ".svg")
                        {
                            //MessageBox.Show(urls[0]);
                            _BrowserView.Tab.Icon = await App.Instance.SetIcon(urls[0], chromiumWebBrowser.Address);
                            break;
                        }
                    }*/
                });
            }
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Instance.CurrentFocusedWindow().Fullscreen(fullscreen);
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
