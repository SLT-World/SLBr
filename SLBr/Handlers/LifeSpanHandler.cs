using CefSharp;
using SLBr.Pages;
using System;
using System.Windows;

namespace SLBr.Handlers
{
    public class LifeSpanHandler : ILifeSpanHandler
    {
        public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName,
            WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo,
            IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.NewBrowserTab(targetUrl, 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
            }));
            //Program.Form.Invoke(new Action(() => Program.Form.newPage(targetUrl)));
            //browser.MainFrame.LoadUrl(targetUrl);
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            return false;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
