// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.CreateTab(MainWindow.Instance.CreateWebBrowser(targetUrl));
            }));
            //Program.Form.Invoke(new Action(() => Program.Form.newPage(targetUrl)));
            //browser.MainFrame.LoadUrl(targetUrl);
            newBrowser = null;
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
            //
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            //if (browserControl.GetMainFrame().Url.Equals("devtools://devtools/devtools_app.html"))
            return false;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
            //nothing
        }
    }
}
