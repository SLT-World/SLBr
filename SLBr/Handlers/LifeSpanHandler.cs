using CefSharp;
using SLBr.Controls;
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
            int _Width = popupFeatures.Width != null ? (int)popupFeatures.Width : 600;
            int _Height = popupFeatures.Height != null ? (int)popupFeatures.Height : 650;
            newBrowser = null;
            if (targetDisposition == WindowOpenDisposition.CurrentTab)
                browser.MainFrame.LoadUrl(targetUrl);
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (targetDisposition == WindowOpenDisposition.NewPopup)
                        new PopupBrowser(targetUrl, _Width, _Height).Show();
                    else
                        App.Instance.CurrentFocusedWindow().NewBrowserTab(targetUrl, 0, true, App.Instance.CurrentFocusedWindow().BrowserTabs.SelectedIndex + 1);
                }));
            }
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            //MessageBox.Show("Close");
            if (browser.IsPopup)
                return false;
            return true;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
