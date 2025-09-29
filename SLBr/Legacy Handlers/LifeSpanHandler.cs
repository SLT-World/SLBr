using CefSharp;
using CefSharp.Enums;
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
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                string TopLevelOrigin = Utils.GetOrigin(browser.MainFrame.Url);
                ContentSettingValues Value = GlobalRequestContext.GetContentSetting(TopLevelOrigin, TopLevelOrigin, ContentSettingTypes.Popups);
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (targetDisposition == WindowOpenDisposition.NewPopup)
                    {
                        bool Allow = false;
                        if (Value == ContentSettingValues.Allow || Value == ContentSettingValues.SessionOnly)
                            Allow = true;
                        else if (Value == ContentSettingValues.Block)
                            Allow = false;
                        else
                        {
                            InformationDialogWindow InfoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(browser.MainFrame.Url)} to", "Open popup", "\uE8D7", "Allow", "Block", "\xE737");
                            InfoWindow.Topmost = true;
                            Allow = InfoWindow.ShowDialog().ToBool();
                            //if (Value != ContentSettingValues.Ask)
                            //{
                                if (Allow)
                                {
                                    Cef.UIThreadTaskFactory.StartNew(delegate
                                    {
                                        GlobalRequestContext.SetContentSetting(TopLevelOrigin, TopLevelOrigin, ContentSettingTypes.Popups, ContentSettingValues.Allow);
                                    });
                                }
                                else
                                {
                                    Cef.UIThreadTaskFactory.StartNew(delegate
                                    {
                                        GlobalRequestContext.SetContentSetting(TopLevelOrigin, TopLevelOrigin, ContentSettingTypes.Popups, ContentSettingValues.Block);
                                    });
                                }
                            //}
                        }
                        if (Allow)
                            new PopupBrowser(targetUrl, popupFeatures.Width ?? 600, popupFeatures.Height ?? 650).Show();
                    }
                    else
                        App.Instance.CurrentFocusedWindow().NewTab(targetUrl, true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                });
            }
            return true;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            //RuntimeStyle Chrome does not run DoClose
            if (browser.IsPopup)
                return false;
            return true;
        }

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
