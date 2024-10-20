using CefSharp;
using SLBr.Controls;
using SLBr.Pages;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

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
                            _ResourceRequestHandlerFactory.RegisterHandler(request.Url, ResourceHandler.GetByteArray(App.Instance.Malware_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                        else if (_ThreatType == SafeBrowsingHandler.ThreatType.Social_Engineering)
                            _ResourceRequestHandlerFactory.RegisterHandler(request.Url, ResourceHandler.GetByteArray(App.Instance.Deception_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                    }
				}
			}
            else if (request.Url.StartsWith("chrome://"))
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
                    //TransitionType _TransitionType = request.TransitionType;
                    App.Current.Dispatcher.Invoke(async () =>
                    {
                        BrowserView.Tab.Icon = await App.Instance.SetIcon("", chromiumWebBrowser.Address);
                    });
                }
                //if (App.Instance.NeverSlowMode)
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
			chromiumWebBrowser.ExecuteScriptAsync(@"var style=document.createElement('style');style.textContent=`::-webkit-scrollbar {width:15px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color: whitesmoke;}::-webkit-scrollbar-thumb {height:56px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color: gainsboro;transition:background-color 0.5s;}::-webkit-scrollbar-thumb:hover{background-color:gray;transition:background-color 0.5s;}::-webkit-scrollbar-corner{background-color:transparent;}`;document.head.append(style);");
            if (bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
                chromiumWebBrowser.ExecuteScriptAsync(@"!function(){var s,i,c,a,o={frameRate:150,animationTime:400,stepSize:100,pulseAlgorithm:!0,pulseScale:4,pulseNormalize:1,accelerationDelta:50,accelerationMax:3,keyboardSupport:!0,arrowScroll:50,fixedBackground:!0,excluded:""""},p=o,u=!1,d=!1,l={x:0,y:0},f=!1,m=document.documentElement,h=[],v={left:37,up:38,right:39,down:40,spacebar:32,pageup:33,pagedown:34,end:35,home:36},y={37:1,38:1,39:1,40:1};function b(){if(!f&&document.body){f=!0;var e=document.body,t=document.documentElement,o=window.innerHeight,n=e.scrollHeight;if(m=0<=document.compatMode.indexOf(""CSS"")?t:e,s=e,p.keyboardSupport&&Y(""keydown"",D),top!=self)d=!0;else if(o<n&&(e.offsetHeight<=o||t.offsetHeight<=o)){var r,a=document.createElement(""div"");a.style.cssText=""position:absolute; z-index:-10000; top:0; left:0; right:0; height:""+m.scrollHeight+""px"",document.body.appendChild(a),c=function(){r||(r=setTimeout(function(){u||(a.style.height=""0"",a.style.height=m.scrollHeight+""px"",r=null)},500))},setTimeout(c,10),Y(""resize"",c);if((i=new R(c)).observe(e,{attributes:!0,childList:!0,characterData:!1}),m.offsetHeight<=o){var l=document.createElement(""div"");l.style.clear=""both"",e.appendChild(l)}}p.fixedBackground||u||(e.style.backgroundAttachment=""scroll"",t.style.backgroundAttachment=""scroll"")}}var g=[],S=!1,x=Date.now();function k(d,f,m){var e,t;if(e=0<(e=f)?1:-1,t=0<(t=m)?1:-1,(l.x!==e||l.y!==t)&&(l.x=e,l.y=t,g=[],x=0),1!=p.accelerationMax){var o=Date.now()-x;if(o<p.accelerationDelta){var n=(1+50/o)/2;1<n&&(n=Math.min(n,p.accelerationMax),f*=n,m*=n)}x=Date.now()}if(g.push({x:f,y:m,lastX:f<0?.99:-.99,lastY:m<0?.99:-.99,start:Date.now()}),!S){var r=q(),h=d===r||d===document.body;null==d.$scrollBehavior&&function(e){var t=M(e);if(null==B[t]){var o=getComputedStyle(e,"""")[""scroll-behavior""];B[t]=""smooth""==o}return B[t]}(d)&&(d.$scrollBehavior=d.style.scrollBehavior,d.style.scrollBehavior=""auto"");var w=function(e){for(var t=Date.now(),o=0,n=0,r=0;r<g.length;r++){var a=g[r],l=t-a.start,i=l>=p.animationTime,c=i?1:l/p.animationTime;p.pulseAlgorithm&&(c=F(c));var s=a.x*c-a.lastX>>0,u=a.y*c-a.lastY>>0;o+=s,n+=u,a.lastX+=s,a.lastY+=u,i&&(g.splice(r,1),r--)}h?window.scrollBy(o,n):(o&&(d.scrollLeft+=o),n&&(d.scrollTop+=n)),f||m||(g=[]),g.length?j(w,d,1e3/p.frameRate+1):(S=!1,null!=d.$scrollBehavior&&(d.style.scrollBehavior=d.$scrollBehavior,d.$scrollBehavior=null))};j(w,d,0),S=!0}}function e(e){f||b();var t=e.target;if(e.defaultPrevented||e.ctrlKey)return!0;if(N(s,""embed"")||N(t,""embed"")&&/\.pdf/i.test(t.src)||N(s,""object"")||t.shadowRoot)return!0;var o=-e.wheelDeltaX||e.deltaX||0,n=-e.wheelDeltaY||e.deltaY||0;o||n||(n=-e.wheelDelta||0),1===e.deltaMode&&(o*=40,n*=40);var r=z(t);return r?!!function(e){if(!e)return;h.length||(h=[e,e,e]);e=Math.abs(e),h.push(e),h.shift(),clearTimeout(a),a=setTimeout(function(){try{localStorage.SS_deltaBuffer=h.join("","")}catch(e){}},1e3);var t=120<e&&P(e);return!P(120)&&!P(100)&&!t}(n)||(1.2<Math.abs(o)&&(o*=p.stepSize/120),1.2<Math.abs(n)&&(n*=p.stepSize/120),k(r,o,n),e.preventDefault(),void C()):!d||!W||(Object.defineProperty(e,""target"",{value:window.frameElement}),parent.wheel(e))}function D(e){var t=e.target,o=e.ctrlKey||e.altKey||e.metaKey||e.shiftKey&&e.keyCode!==v.spacebar;document.body.contains(s)||(s=document.activeElement);var n=/^(button|submit|radio|checkbox|file|color|image)$/i;if(e.defaultPrevented||/^(textarea|select|embed|object)$/i.test(t.nodeName)||N(t,""input"")&&!n.test(t.type)||N(s,""video"")||function(e){var t=e.target,o=!1;if(-1!=document.URL.indexOf(""www.youtube.com/watch""))do{if(o=t.classList&&t.classList.contains(""html5-video-controls""))break}while(t=t.parentNode);return o}(e)||t.isContentEditable||o)return!0;if((N(t,""button"")||N(t,""input"")&&n.test(t.type))&&e.keyCode===v.spacebar)return!0;if(N(t,""input"")&&""radio""==t.type&&y[e.keyCode])return!0;var r=0,a=0,l=z(s);if(!l)return!d||!W||parent.keydown(e);var i=l.clientHeight;switch(l==document.body&&(i=window.innerHeight),e.keyCode){case v.up:a=-p.arrowScroll;break;case v.down:a=p.arrowScroll;break;case v.spacebar:a=-(e.shiftKey?1:-1)*i*.9;break;case v.pageup:a=.9*-i;break;case v.pagedown:a=.9*i;break;case v.home:l==document.body&&document.scrollingElement&&(l=document.scrollingElement),a=-l.scrollTop;break;case v.end:var c=l.scrollHeight-l.scrollTop-i;a=0<c?c+10:0;break;case v.left:r=-p.arrowScroll;break;case v.right:r=p.arrowScroll;break;default:return!0}k(l,r,a),e.preventDefault(),C()}function t(e){s=e.target}var n,r,M=(n=0,function(e){return e.uniqueID||(e.uniqueID=n++)}),E={},T={},B={};function C(){clearTimeout(r),r=setInterval(function(){E=T=B={}},1e3)}function H(e,t,o){for(var n=o?E:T,r=e.length;r--;)n[M(e[r])]=t;return t}function z(e){var t=[],o=document.body,n=m.scrollHeight;do{var r=(!1?E:T)[M(e)];if(r)return H(t,r);if(t.push(e),n===e.scrollHeight){var a=O(m)&&O(o)||X(m);if(d&&L(m)||!d&&a)return H(t,q())}else if(L(e)&&X(e))return H(t,e)}while(e=e.parentElement)}function L(e){return e.clientHeight+10<e.scrollHeight}function O(e){return""hidden""!==getComputedStyle(e,"""").getPropertyValue(""overflow-y"")}function X(e){var t=getComputedStyle(e,"""").getPropertyValue(""overflow-y"");return""scroll""===t||""auto""===t}function Y(e,t,o){window.addEventListener(e,t,o||!1)}function A(e,t,o){window.removeEventListener(e,t,o||!1)}function N(e,t){return e&&(e.nodeName||"""").toLowerCase()===t.toLowerCase()}if(window.localStorage&&localStorage.SS_deltaBuffer)try{h=localStorage.SS_deltaBuffer.split("","")}catch(e){}function K(e,t){return Math.floor(e/t)==e/t}function P(e){return K(h[0],e)&&K(h[1],e)&&K(h[2],e)}var $,j=window.requestAnimationFrame||window.webkitRequestAnimationFrame||window.mozRequestAnimationFrame||function(e,t,o){window.setTimeout(e,o||1e3/60)},R=window.MutationObserver||window.WebKitMutationObserver||window.MozMutationObserver,q=($=document.scrollingElement,function(){if(!$){var e=document.createElement(""div"");e.style.cssText=""height:10000px;width:1px;"",document.body.appendChild(e);var t=document.body.scrollTop;document.documentElement.scrollTop,window.scrollBy(0,3),$=document.body.scrollTop!=t?document.body:document.documentElement,window.scrollBy(0,-3),document.body.removeChild(e)}return $});function V(e){var t;return((e*=p.pulseScale)<1?e-(1-Math.exp(-e)):(e-=1,(t=Math.exp(-1))+(1-Math.exp(-e))*(1-t)))*p.pulseNormalize}function F(e){return 1<=e?1:e<=0?0:(1==p.pulseNormalize&&(p.pulseNormalize/=V(1)),V(e))}
try{window.addEventListener(""test"",null,Object.defineProperty({},""passive"",{get:function(){ee=!0}}))}catch(e){}var te=!!ee&&{passive:!1},oe=""onwheel""in document.createElement(""div"")?""wheel"":""mousewheel"";function ne(e){for(var t in e)o.hasOwnProperty(t)&&(p[t]=e[t])}oe&&(Y(oe,e,te),Y(""mousedown"",t),Y(""load"",b)),ne.destroy=function(){i&&i.disconnect(),A(oe,e),A(""mousedown"",t),A(""keydown"",D),A(""resize"",c),A(""load"",b)},window.SmoothScrollOptions&&ne(window.SmoothScrollOptions),""function""==typeof define&&define.amd?define(function(){return ne}):""object""==typeof exports?module.exports=ne:window.SmoothScroll=ne}();
SmoothScroll({animationTime:400,stepSize:100,accelerationDelta:50,accelerationMax:3,keyboardSupport:true,arrowScroll:50,pulseAlgorithm:true,pulseScale:4,pulseNormalize:1,touchpadSupport:false,fixedBackground:true,excluded:''});");
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
