﻿using CefSharp;
using SLBr.Controls;

namespace SLBr.Handlers
{
    public class LifeSpanHandler : ILifeSpanHandler
    {
        bool IsSideBar = false;
        public LifeSpanHandler(bool _IsSideBar)
        {
            IsSideBar = _IsSideBar;
        }

        public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName,
            WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo,
            IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            if (targetDisposition == WindowOpenDisposition.CurrentTab)
            {
                if (IsSideBar)
                    App.Instance.CurrentFocusedWindow().GetTab().Content.Address = targetUrl;
                else
                    browser.MainFrame.LoadUrl(targetUrl);
            }
            else
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    //try
                    //{
                        if (targetDisposition == WindowOpenDisposition.NewPopup)
                        {
                            bool Allow = false;
                            string Host = Utils.Host(targetUrl);
                            if (!App.Instance.PopupPermissionHosts.ContainsKey(Host))
                            {
                                var infoWindow = new InformationDialogWindow("Permission", $"Allow {Host} to", "Open popup", "\uE8D7", "Allow", "Block", "\xE737");
                                infoWindow.Topmost = true;
                                Allow = infoWindow.ShowDialog().ToBool();
                                App.Instance.PopupPermissionHosts.Add(Host, Allow);
                            }
                            else
                                App.Instance.PopupPermissionHosts.TryGetValue(Host, out Allow);
                            if (Allow)
                                new PopupBrowser(targetUrl, popupFeatures.Width != null ? (int)popupFeatures.Width : 600, popupFeatures.Height != null ? (int)popupFeatures.Height : 650).Show();
                        }
                        else
                            App.Instance.CurrentFocusedWindow().NewTab(targetUrl, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
                    //}
                    //catch { }
                });
            }
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            if (browser.IsPopup)
                return false;
            return true;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
