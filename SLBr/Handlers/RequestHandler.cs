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

        public RequestHandler(bool _AdBlock = false, bool _TrackerBlock = false, Browser _BrowserView = null)
		{
			BrowserView = _BrowserView;
			AdBlock = _AdBlock;
			TrackerBlock = _TrackerBlock;
        }

		public bool AdBlock;
		public bool TrackerBlock;

		public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
		{
			CredentialsDialogResult _CredentialsDialogResult = CredentialsDialog.Show($@"Sign in to {host}");
			if (_CredentialsDialogResult.Accepted == true)
            {
                callback.Continue(_CredentialsDialogResult.Username, _CredentialsDialogResult.Password);
                return true;
            }
			return false;
		}
		public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
			if (request.Url.StartsWith("chrome://sandbox"))
                return true;
			if (App.Instance.GoogleSafeBrowsing)
			{
				ResourceRequestHandlerFactory _ResourceRequestHandlerFactory = (ResourceRequestHandlerFactory)chromiumWebBrowser.ResourceRequestHandlerFactory;
				bool CheckSafe = true;
				if (BrowserView != null && _ResourceRequestHandlerFactory.Handlers.ContainsKey(request.Url))
					CheckSafe = false;
				if (CheckSafe && Utils.IsHttpScheme(request.Url))
				{
					string Response = App.Instance._SafeBrowsing.Response(request.Url);
					SafeBrowsingHandler.ThreatType _ThreatType = Utils.CheckForInternetConnection() ? App.Instance._SafeBrowsing.GetThreatType(Response) : SafeBrowsingHandler.ThreatType.Unknown;
					if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware || _ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software)
					{
						if (BrowserView != null)
						{
							if (frame.IsMain)
								chromiumWebBrowser.NewLoadHtml(App.Instance.Malware_Error, request.Url, Encoding.UTF8, false, 1, _ThreatType.ToString());
								//chromiumWebBrowser.NewLoadHtml(App.Instance.Malware_Error, request.Url, Encoding.UTF8, true, 2, _ThreatType.ToString());
						}
						return true;
					}
					else if (_ThreatType == SafeBrowsingHandler.ThreatType.Social_Engineering)
					{
						if (BrowserView != null)
						{
							if (frame.IsMain)
								chromiumWebBrowser.NewLoadHtml(App.Instance.Deception_Error, request.Url, Encoding.UTF8, false, 1, _ThreatType.ToString());
								//chromiumWebBrowser.NewLoadHtml(App.Instance.Deception_Error, request.Url, Encoding.UTF8, true, 2, _ThreatType.ToString());
						}
						return true;
					}
				}
			}
			if (BrowserView != null && frame.IsMain)
			{
				TransitionType _TransitionType = request.TransitionType;
                App.Current.Dispatcher.Invoke(async () =>
                {
                    BrowserView.Tab.Icon = await App.Instance.SetIcon("", chromiumWebBrowser.Address);

					/*if (!_TransitionType.HasFlag(TransitionType.ForwardBack) && !_TransitionType.HasFlag(TransitionType.Reload))
						BrowserView.AddNavigationEntry(chromiumWebBrowser.Address, ((ChromiumWebBrowser)chromiumWebBrowser).Title, BrowserView.CurrentNavigationEntry);*/
				});
				//MessageBox.Show($"{isRedirect.ToString()} {userGesture.ToString()} {request.TransitionType} {request.Url}");
			}
            return false;
		}
		public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
		{
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

        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
		{
            var infoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(originUrl)} to", "", "Store files on this device", "Allow", "Block", "\xE8B7");
            infoWindow.Topmost = true;
            if (infoWindow.ShowDialog() == true)
            {
                callback.Continue(true);
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
            return new ResourceRequestHandler(AdBlock, TrackerBlock);
        }

		public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
		{
			chromiumWebBrowser.ExecuteScriptAsync(
@"function addStyle(styleString) {
	const style = document.createElement('style');
	style.textContent = styleString;
	document.head.append(style);
}
addStyle(`
::-webkit-scrollbar {
	width: 15px;
	border-radius: 10px;
	border: 5px solid transparent;
	background-clip: content-box;
	background-color: whitesmoke;
}
::-webkit-scrollbar-thumb {
	height: 56px;
	border-radius: 10px;
	border: 5px solid transparent;
	background-clip: content-box;
	background-color: gainsboro;
	transition: background-color 0.5s;
}
::-webkit-scrollbar-thumb:hover {
	background-color: gray;
	transition: background-color 0.5s;
}
::-webkit-scrollbar-corner {background-color: transparent;}
`);");
            if (bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
                chromiumWebBrowser.ExecuteScriptAsync(
@"!function(){var s,i,c,a,o={frameRate:150,animationTime:400,stepSize:100,pulseAlgorithm:!0,pulseScale:4,pulseNormalize:1,accelerationDelta:50,accelerationMax:3,keyboardSupport:!0,arrowScroll:50,fixedBackground:!0,excluded:""""},p=o,u=!1,d=!1,l={x:0,y:0},f=!1,m=document.documentElement,h=[],v={left:37,up:38,right:39,down:40,spacebar:32,pageup:33,pagedown:34,end:35,home:36},y={37:1,38:1,39:1,40:1};function b(){if(!f&&document.body){f=!0;var e=document.body,t=document.documentElement,o=window.innerHeight,n=e.scrollHeight;if(m=0<=document.compatMode.indexOf(""CSS"")?t:e,s=e,p.keyboardSupport&&Y(""keydown"",D),top!=self)d=!0;else if(o<n&&(e.offsetHeight<=o||t.offsetHeight<=o)){var r,a=document.createElement(""div"");a.style.cssText=""position:absolute; z-index:-10000; top:0; left:0; right:0; height:""+m.scrollHeight+""px"",document.body.appendChild(a),c=function(){r||(r=setTimeout(function(){u||(a.style.height=""0"",a.style.height=m.scrollHeight+""px"",r=null)},500))},setTimeout(c,10),Y(""resize"",c);if((i=new R(c)).observe(e,{attributes:!0,childList:!0,characterData:!1}),m.offsetHeight<=o){var l=document.createElement(""div"");l.style.clear=""both"",e.appendChild(l)}}p.fixedBackground||u||(e.style.backgroundAttachment=""scroll"",t.style.backgroundAttachment=""scroll"")}}var g=[],S=!1,x=Date.now();function k(d,f,m){var e,t;if(e=0<(e=f)?1:-1,t=0<(t=m)?1:-1,(l.x!==e||l.y!==t)&&(l.x=e,l.y=t,g=[],x=0),1!=p.accelerationMax){var o=Date.now()-x;if(o<p.accelerationDelta){var n=(1+50/o)/2;1<n&&(n=Math.min(n,p.accelerationMax),f*=n,m*=n)}x=Date.now()}if(g.push({x:f,y:m,lastX:f<0?.99:-.99,lastY:m<0?.99:-.99,start:Date.now()}),!S){var r=q(),h=d===r||d===document.body;null==d.$scrollBehavior&&function(e){var t=M(e);if(null==B[t]){var o=getComputedStyle(e,"""")[""scroll-behavior""];B[t]=""smooth""==o}return B[t]}(d)&&(d.$scrollBehavior=d.style.scrollBehavior,d.style.scrollBehavior=""auto"");var w=function(e){for(var t=Date.now(),o=0,n=0,r=0;r<g.length;r++){var a=g[r],l=t-a.start,i=l>=p.animationTime,c=i?1:l/p.animationTime;p.pulseAlgorithm&&(c=F(c));var s=a.x*c-a.lastX>>0,u=a.y*c-a.lastY>>0;o+=s,n+=u,a.lastX+=s,a.lastY+=u,i&&(g.splice(r,1),r--)}h?window.scrollBy(o,n):(o&&(d.scrollLeft+=o),n&&(d.scrollTop+=n)),f||m||(g=[]),g.length?j(w,d,1e3/p.frameRate+1):(S=!1,null!=d.$scrollBehavior&&(d.style.scrollBehavior=d.$scrollBehavior,d.$scrollBehavior=null))};j(w,d,0),S=!0}}function e(e){f||b();var t=e.target;if(e.defaultPrevented||e.ctrlKey)return!0;if(N(s,""embed"")||N(t,""embed"")&&/\.pdf/i.test(t.src)||N(s,""object"")||t.shadowRoot)return!0;var o=-e.wheelDeltaX||e.deltaX||0,n=-e.wheelDeltaY||e.deltaY||0;o||n||(n=-e.wheelDelta||0),1===e.deltaMode&&(o*=40,n*=40);var r=z(t);return r?!!function(e){if(!e)return;h.length||(h=[e,e,e]);e=Math.abs(e),h.push(e),h.shift(),clearTimeout(a),a=setTimeout(function(){try{localStorage.SS_deltaBuffer=h.join("","")}catch(e){}},1e3);var t=120<e&&P(e);return!P(120)&&!P(100)&&!t}(n)||(1.2<Math.abs(o)&&(o*=p.stepSize/120),1.2<Math.abs(n)&&(n*=p.stepSize/120),k(r,o,n),e.preventDefault(),void C()):!d||!W||(Object.defineProperty(e,""target"",{value:window.frameElement}),parent.wheel(e))}function D(e){var t=e.target,o=e.ctrlKey||e.altKey||e.metaKey||e.shiftKey&&e.keyCode!==v.spacebar;document.body.contains(s)||(s=document.activeElement);var n=/^(button|submit|radio|checkbox|file|color|image)$/i;if(e.defaultPrevented||/^(textarea|select|embed|object)$/i.test(t.nodeName)||N(t,""input"")&&!n.test(t.type)||N(s,""video"")||function(e){var t=e.target,o=!1;if(-1!=document.URL.indexOf(""www.youtube.com/watch""))do{if(o=t.classList&&t.classList.contains(""html5-video-controls""))break}while(t=t.parentNode);return o}(e)||t.isContentEditable||o)return!0;if((N(t,""button"")||N(t,""input"")&&n.test(t.type))&&e.keyCode===v.spacebar)return!0;if(N(t,""input"")&&""radio""==t.type&&y[e.keyCode])return!0;var r=0,a=0,l=z(s);if(!l)return!d||!W||parent.keydown(e);var i=l.clientHeight;switch(l==document.body&&(i=window.innerHeight),e.keyCode){case v.up:a=-p.arrowScroll;break;case v.down:a=p.arrowScroll;break;case v.spacebar:a=-(e.shiftKey?1:-1)*i*.9;break;case v.pageup:a=.9*-i;break;case v.pagedown:a=.9*i;break;case v.home:l==document.body&&document.scrollingElement&&(l=document.scrollingElement),a=-l.scrollTop;break;case v.end:var c=l.scrollHeight-l.scrollTop-i;a=0<c?c+10:0;break;case v.left:r=-p.arrowScroll;break;case v.right:r=p.arrowScroll;break;default:return!0}k(l,r,a),e.preventDefault(),C()}function t(e){s=e.target}var n,r,M=(n=0,function(e){return e.uniqueID||(e.uniqueID=n++)}),E={},T={},B={};function C(){clearTimeout(r),r=setInterval(function(){E=T=B={}},1e3)}function H(e,t,o){for(var n=o?E:T,r=e.length;r--;)n[M(e[r])]=t;return t}function z(e){var t=[],o=document.body,n=m.scrollHeight;do{var r=(!1?E:T)[M(e)];if(r)return H(t,r);if(t.push(e),n===e.scrollHeight){var a=O(m)&&O(o)||X(m);if(d&&L(m)||!d&&a)return H(t,q())}else if(L(e)&&X(e))return H(t,e)}while(e=e.parentElement)}function L(e){return e.clientHeight+10<e.scrollHeight}function O(e){return""hidden""!==getComputedStyle(e,"""").getPropertyValue(""overflow-y"")}function X(e){var t=getComputedStyle(e,"""").getPropertyValue(""overflow-y"");return""scroll""===t||""auto""===t}function Y(e,t,o){window.addEventListener(e,t,o||!1)}function A(e,t,o){window.removeEventListener(e,t,o||!1)}function N(e,t){return e&&(e.nodeName||"""").toLowerCase()===t.toLowerCase()}if(window.localStorage&&localStorage.SS_deltaBuffer)try{h=localStorage.SS_deltaBuffer.split("","")}catch(e){}function K(e,t){return Math.floor(e/t)==e/t}function P(e){return K(h[0],e)&&K(h[1],e)&&K(h[2],e)}var $,j=window.requestAnimationFrame||window.webkitRequestAnimationFrame||window.mozRequestAnimationFrame||function(e,t,o){window.setTimeout(e,o||1e3/60)},R=window.MutationObserver||window.WebKitMutationObserver||window.MozMutationObserver,q=($=document.scrollingElement,function(){if(!$){var e=document.createElement(""div"");e.style.cssText=""height:10000px;width:1px;"",document.body.appendChild(e);var t=document.body.scrollTop;document.documentElement.scrollTop,window.scrollBy(0,3),$=document.body.scrollTop!=t?document.body:document.documentElement,window.scrollBy(0,-3),document.body.removeChild(e)}return $});function V(e){var t;return((e*=p.pulseScale)<1?e-(1-Math.exp(-e)):(e-=1,(t=Math.exp(-1))+(1-Math.exp(-e))*(1-t)))*p.pulseNormalize}function F(e){return 1<=e?1:e<=0?0:(1==p.pulseNormalize&&(p.pulseNormalize/=V(1)),V(e))}
try{window.addEventListener(""test"",null,Object.defineProperty({},""passive"",{get:function(){ee=!0}}))}catch(e){}var te=!!ee&&{passive:!1},oe=""onwheel""in document.createElement(""div"")?""wheel"":""mousewheel"";function ne(e){for(var t in e)o.hasOwnProperty(t)&&(p[t]=e[t])}oe&&(Y(oe,e,te),Y(""mousedown"",t),Y(""load"",b)),ne.destroy=function(){i&&i.disconnect(),A(oe,e),A(""mousedown"",t),A(""keydown"",D),A(""resize"",c),A(""load"",b)},window.SmoothScrollOptions&&ne(window.SmoothScrollOptions),""function""==typeof define&&define.amd?define(function(){return ne}):""object""==typeof exports?module.exports=ne:window.SmoothScroll=ne}();
SmoothScroll({ animationTime: 400, stepSize: 100, accelerationDelta: 50, accelerationMax: 3, keyboardSupport: true, arrowScroll: 50, pulseAlgorithm: true, pulseScale: 4, pulseNormalize: 1, touchpadSupport: false, fixedBackground: true, excluded          : '' });");
		}

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
		{
			return false;
		}

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage)
        {
            /*if (browser != null)
			{
				try
				{
					if (Utils.CheckForInternetConnection())
						browser.Reload(true);
					else
					{
						App.Current.Dispatcher.Invoke(() =>
						{
							ChromiumWebBrowser _ChromiumWebBrowser = (ChromiumWebBrowser)browserControl;
							_ChromiumWebBrowser.Address = $"slbr://processcrashed?s={browserControl.Address}";
						});
					}
				}
				catch { }
			}*/
        }
    }
}
