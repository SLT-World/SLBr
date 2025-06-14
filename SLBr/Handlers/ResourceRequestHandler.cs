using CefSharp;

namespace SLBr.Handlers
{
    public class ResourceRequestHandler : IResourceRequestHandler
    {
        RequestHandler Handler;

        public ResourceRequestHandler(RequestHandler _Handler)
        {
            Handler = _Handler;
        }

        public void Dispose()
        {
            GC.Collect(GC.MaxGeneration);
            GC.SuppressFinalize(this);
        }

        public ICookieAccessFilter GetCookieAccessFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return null;
        }

        public IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return null;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return null;
        }
        
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            if (Utils.IsHttpScheme(request.Url))
            {
                //https://chromium-review.googlesource.com/c/chromium/src/+/1265506/25/third_party/blink/renderer/platform/loader/fetch/resource_loader.cc
                if (App.Instance.NeverSlowMode && Handler.IsOverBudget(request.ResourceType))
                    return CefReturnValue.Cancel;
                if (App.Instance.AdBlock || App.Instance.TrackerBlock)
                {
                    /*if (Utils.Host(browser.FocusedFrame.Url or request.ReferrerUrl).EndsWith("ecosia.org", StringComparison.Ordinal))
                        return CefReturnValue.Continue;*/
                    if (request.ResourceType == ResourceType.Ping)
                    {
                        App.Instance.TrackersBlocked++;
                        return CefReturnValue.Cancel;
                    }
                    else if (Utils.IsPossiblyAd(request.ResourceType))
                    {
                        string Host = Utils.FastHost(request.Url);
                        if (App.Instance.TrackerBlock && App.Analytics.Has(Host))
                        {
                            App.Instance.TrackersBlocked++;
                            return CefReturnValue.Cancel;
                        }
                        else if (App.Instance.AdBlock && (App.Ads.Has(Host) || App.Miners.Has(Host)))
                        {
                            App.Instance.AdsBlocked++;
                            return CefReturnValue.Cancel;
                        }
                        if (request.ResourceType == ResourceType.Script || request.ResourceType == ResourceType.Xhr)
                        {
                            if (App.HasInLink.Find(request.Url).Any())
                                return CefReturnValue.Cancel;
                            /*string CleanedUrl = Utils.CleanUrl(request.Url, true, true, true, true);
                            if (App.HasInLink.Any(script => CleanedUrl.Contains(script, StringComparison.Ordinal)))
                                return CefReturnValue.Cancel;*/
                        }
                    }
                }
                if (App.Instance.GoogleSafeBrowsing && Utils.CanCheckSafeBrowsing(request.ResourceType))
                {
                    SafeBrowsingHandler.ThreatType _ThreatType = App.Instance._SafeBrowsing.GetThreatType(App.Instance._SafeBrowsing.Response(request.Url));
                    if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware || _ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software || _ThreatType == SafeBrowsingHandler.ThreatType.Social_Engineering)
                        return CefReturnValue.Cancel;
                }
                if (bool.Parse(App.Instance.GlobalSave.Get("LiteMode")))
                    request.SetHeaderByName("Save-Data", "on", true);
                    //request.SetHeaderByName("Device-Memory", "0.25", true);
                //request.SetHeaderByName("DNT", "1", true);
            }
            return CefReturnValue.Continue;
        }

        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return true;
        }

        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            if (status == UrlRequestStatus.Success && App.Instance.NeverSlowMode)
                Handler.DeductFromBudget(request.ResourceType, receivedContentLength);
        }

        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
        }

        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            //return !Handler.CanLoadUnderBudget(request.ResourceType, receivedContentLength);

            //if (response.StatusCode == 404)
            //    BrowserView.Prompt($"404, Do you want open the page in the Wayback Machine?", true, "Download", $"24<,>https://web.archive.org/{request.Url}", $"https://web.archive.org/{request.Url}", true, "\xE896");
            return false;
        }
    }
}
