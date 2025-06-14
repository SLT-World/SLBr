using CefSharp;
using SLBr.Controls;
using SLBr.Pages;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SLBr.Handlers
{
	public class RequestHandler : IRequestHandler
	{
		Browser BrowserView;

        public RequestHandler(Browser _BrowserView = null)
		{
			BrowserView = _BrowserView;
        }

        public long Image_Budget = 2 * 1024 * 1024;
        public long Stylesheet_Budget = 400 * 1024;
        public long Script_Budget = 500 * 1024;
        public long Font_Budget = 300 * 1024;
        public long Frame_Budget = 5;

        /*public long Connection_Budget = 10;

        public bool CanAffordAnotherConnection()
		{
			return Connection_Budget_Used > 0;
		}*/

        public bool IsOverBudget(ResourceType _ResourceType)
		{
            switch (_ResourceType)
            {
                case ResourceType.Image:
                    return Image_Budget <= 0;
                case ResourceType.Stylesheet:
                    return Stylesheet_Budget <= 0;
                case ResourceType.Script:
                    return Script_Budget <= 0;
                case ResourceType.FontResource:
                    return Font_Budget <= 0;
                case ResourceType.SubFrame:
                    return Frame_Budget <= 0;
                default:
                    return false;
            }
        }

        /*public bool CanLoadUnderBudget(ResourceType _ResourceType, long DataLength)
        {
            long Maximum = DataLength;
            switch (_ResourceType) {
            case ResourceType.Image:
                Maximum = 1 * 1024 * 1024;
                break;
            case ResourceType.Stylesheet:
                Maximum = 200 * 1024;
                break;
            case ResourceType.Script:
                Maximum = 50 * 1024;
                break;
            case ResourceType.FontResource:
                Maximum = 100 * 1024;
                break;
            }
            if (DataLength > Maximum) {
                //BLOCKED: max per-file size of " + String::Number(type_max / 1024) + "K exceeded by '" + url.ElidedString() + "', which is " + String::Number(data_length / 1024) + "K"));
                return false;
            }

            bool UnderBudget = true;
            switch (_ResourceType)
            {
                case ResourceType.Image:
                    UnderBudget = (Image_Budget - DataLength) > 0;
                    break;
                case ResourceType.Stylesheet:
                    UnderBudget = (Stylesheet_Budget - DataLength) > 0;
                    break;
                case ResourceType.Script:
                    UnderBudget = (Script_Budget - DataLength) > 0;
                    break;
                case ResourceType.FontResource:
                    UnderBudget = (Font_Budget - DataLength) > 0;
                    break;
            }
            //if (!UnderBudget)
            //BLOCKED: total file type budget exceeded
            
            return UnderBudget;
        }*/

        //https://chromium-review.googlesource.com/c/chromium/src/+/1265506/25/third_party/blink/renderer/core/loader/frame_fetch_context.cc
        public void DeductFromBudget(ResourceType _ResourceType, long DataLength)
        {
            /* Currently do not support budgeting for any of:
             * ResourceType.Object:
             * ResourceType.Prefetch:
             * ResourceType.MainFrame:
             * ResourceType.Media:*/
            switch (_ResourceType)
            {
                case ResourceType.Image:
                    Image_Budget -= DataLength;
                    return;
                case ResourceType.Stylesheet:
                    Stylesheet_Budget -= DataLength;
                    return;
                case ResourceType.Script:
                    Script_Budget -= DataLength;
                    return;
                case ResourceType.FontResource:
                    Font_Budget -= DataLength;
                    return;
                case ResourceType.SubFrame:
                    Frame_Budget -= DataLength;
                    return;
                default:
                    break;
            }
        }

        public void ResetBudgets()
        {
            Image_Budget = 2 * 1024 * 1024;
            Stylesheet_Budget = 400 * 1024;
            Script_Budget = 500 * 1024;
            Font_Budget = 300 * 1024;
            Frame_Budget = 5;
            //Connection_Budget = 10;
        }

        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            /*CredentialsDialogWindow _CredentialsDialogWindow;
            bool DialogResult = false;
            string Username = "";
            string Password = "";
            App.Current.Dispatcher.Invoke(() =>
            {
                _CredentialsDialogWindow = new CredentialsDialogWindow($"Sign in to {host}", "\uec19");
                _CredentialsDialogWindow.Topmost = true;
                DialogResult = _CredentialsDialogWindow.ShowDialog().ToBool();
                Username = _CredentialsDialogWindow.Username;
                Password = _CredentialsDialogWindow.Password;
            });
			if (DialogResult == true)
            {
                callback.Continue(Username, Password);
                return true;
            }
			return false;*/

            App.Current.Dispatcher.Invoke(() =>
            {
                using (callback)
                {
                    CredentialsDialogWindow _CredentialsDialogWindow = new CredentialsDialogWindow($"Sign in to {host}", "\uec19");
                    _CredentialsDialogWindow.Topmost = true;
                    if (_CredentialsDialogWindow.ShowDialog().ToBool())
                        callback.Continue(_CredentialsDialogWindow.Username, _CredentialsDialogWindow.Password);
                    else
                        callback.Cancel();
                }
            });
            return true;
        }
		public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            if (App.Instance.NeverSlowMode && request.TransitionType == TransitionType.AutoSubFrame)
            {
                if (IsOverBudget(ResourceType.SubFrame))
                    return true;
                else
                {
                    DeductFromBudget(ResourceType.SubFrame, 1);
                    return false;
                }
            }
            if (Utils.IsHttpScheme(request.Url))
			{
				if (App.Instance.GoogleSafeBrowsing)
				{
					ResourceRequestHandlerFactory _ResourceRequestHandlerFactory = (ResourceRequestHandlerFactory)chromiumWebBrowser.ResourceRequestHandlerFactory;
                    if (!_ResourceRequestHandlerFactory.Handlers.ContainsKey(request.Url))
                    {
                        SafeBrowsingHandler.ThreatType _ThreatType = App.Instance._SafeBrowsing.GetThreatType(App.Instance._SafeBrowsing.Response(request.Url));
                        if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware || _ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software)
                            _ResourceRequestHandlerFactory.RegisterHandler(request.Url, ResourceHandler.GetByteArray(App.Malware_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                        else if (_ThreatType == SafeBrowsingHandler.ThreatType.Social_Engineering)
                            _ResourceRequestHandlerFactory.RegisterHandler(request.Url, ResourceHandler.GetByteArray(App.Deception_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                    }
				}
			}
            else if (request.Url.StartsWith("chrome:", StringComparison.Ordinal))
            {
                ResourceRequestHandlerFactory _ResourceRequestHandlerFactory = (ResourceRequestHandlerFactory)chromiumWebBrowser.ResourceRequestHandlerFactory;
                if (!_ResourceRequestHandlerFactory.Handlers.ContainsKey(request.Url))
                {
                    bool Block = false;
                    //https://source.chromium.org/chromium/chromium/src/+/main:ios/chrome/browser/shared/model/url/chrome_url_constants.cc
                    switch (request.Url.Substring(9))
                    {
                        case string s when s.StartsWith("settings", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("history", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("downloads", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("flags", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("new-tab-page", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("bookmarks", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("apps", StringComparison.Ordinal):
                            Block = true;
                            break;

                        case string s when s.StartsWith("dino", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("management", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("new-tab-page-third-party", StringComparison.Ordinal):
                            Block = true;
                            break;

                        case string s when s.StartsWith("favicon", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("sandbox", StringComparison.Ordinal):
                            Block = true;
                            break;

                        case string s when s.StartsWith("bookmarks-side-panel.top-chrome", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("customize-chrome-side-panel.top-chrome", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("read-later.top-chrome", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("tab-search.top-chrome", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("tab-strip.top-chrome", StringComparison.Ordinal):
                            Block = true;
                            break;

                        case string s when s.StartsWith("support-tool", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("privacy-sandbox-dialog", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("chrome-signin", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("browser-switch", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("profile-picker", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("search-engine-choice", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("intro", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("sync-confirmation", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("app-settings", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("managed-user-profile-notice", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("reset-password", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("imageburner", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("connection-help", StringComparison.Ordinal):
                            Block = true;
                            break;
                        case string s when s.StartsWith("connection-monitoring-detected", StringComparison.Ordinal):
                            Block = true;
                            break;
                            //cast-feedback
                    }
                    if (Block)
                        _ResourceRequestHandlerFactory.RegisterHandler(request.Url, ResourceHandler.GetByteArray(App.Instance.GenerateCannotConnect(request.Url, CefErrorCode.InvalidUrl, "ERR_INVALID_URL"), Encoding.UTF8), "text/html", -1, "");
                }
            }
            if (frame.IsMain)
            {
                if (BrowserView != null)
                {
                    App.Current.Dispatcher.Invoke(async () =>
                    {
                        BrowserView.Tab.Icon = await App.Instance.SetIcon("", chromiumWebBrowser.Address);
                    });
                }
                if (App.Instance.NeverSlowMode)
                    ResetBudgets();
            }
            return false;
		}
		public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
		{
            //callback.Dispose();
            return true;
		}
		public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
		{
			if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    App.Instance.CurrentFocusedWindow().NewTab(targetUrl, false, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
				});
				return true;
			}
			return false;
		}

		public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
		{
		}

		public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
			if (BrowserView != null)
			{
				if (BrowserView._ResourceRequestHandlerFactory.Handlers.Keys.Contains(request.Url))
					return null;
			}
            return new ResourceRequestHandler(this);
        }

		public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            chromiumWebBrowser.ExecuteScriptAsync(Scripts.ScrollCSS);
            if (bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
                chromiumWebBrowser.ExecuteScriptAsync(Scripts.ScrollScript);
        }

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
		{
            callback.Dispose();
			return false;
		}

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage)
        {
            /*if (browser != null)
			{
				//if (Utils.CheckForInternetConnection())
				//	browser.Reload(true);
				//else
				//{
					App.Current.Dispatcher.Invoke(() =>
					{
                        chromiumWebBrowser.LoadUrl($"slbr://processcrashed?s={chromiumWebBrowser.Address}");
					});
				//}
			}*/
        }
    }
}
