// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Windows;
using System.Windows.Threading;

namespace SLBr
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
                MainWindow.Instance.CreateTab(MainWindow.Instance.CreateWebBrowser(targetUrl), true, MainWindow.Instance.Tabs.SelectedIndex + 1, true);
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
            //if (browserControl.GetMainFrame().Url.Equals("devtools://devtools/devtools_app.html"))
            return false;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
            //if (!browser.IsDisposed/* && browser.IsPopup*/)
            /*{
                if (!browser.MainFrame.Url.Equals("devtools://devtools/devtools_app.html"))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        TabItem _Tab = MainWindow.Instance.GetTab(browserControl);
                        if (_Tab != null)
                            MainWindow.Instance.CloseTab(_Tab);
                    }));
                }
            }*/
        }
    }
}
