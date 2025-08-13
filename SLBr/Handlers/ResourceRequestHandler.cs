using CefSharp;
using SLBr.Controls;
using System.Buffers.Text;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

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
            if (Handler.BrowserView?.DevToolsHost != null)
            {
                //MessageBox.Show($"{frame.Name} {frame.Parent} {frame.IsMain} {frame.Url}");
                //devtools://theme/colors.css?sets=ui,chrome
                if (request.Url.StartsWith("devtools:", StringComparison.Ordinal))
                {
                    if (request.Url.EndsWith("design_system_tokens.css", StringComparison.Ordinal))
                        return ResourceHandler.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(App.Instance.DevToolCSS)), mimeType: "text/css", autoDisposeStream: true);
                    else if (request.Url.EndsWith("devtools.svg", StringComparison.Ordinal))
                        return ResourceHandler.FromFilePath(Path.Combine(App.Instance.ResourcesPath, "SLBr.svg"), mimeType: "image/svg+xml", autoDisposeStream: true);
                }
            }
            return null;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            /*if (request.ResourceType == ResourceType.Script && !request.Url.StartsWith("devtools:", StringComparison.Ordinal))
            {
                return new SafePassthroughFilter("window", "dowin");
            }*/

            return null;
        }

        /* fontawesome.min.js
         * angular.js
         * three.js
         * vue.js
         */
        //https://developers.google.com/speed/libraries/
        //https://burak-vural.medium.com/most-common-javascript-libraries-advices-cae853bc0da
        //https://kinsta.com/blog/javascript-libraries/
        //https://www.keycdn.com/support/javascript-cdn-resources
        /*FastHashSet<CdnEntry> CdnOverrides = new()
        {
            new CdnEntry(
                "ajax/libs/webfont/",//https://ajax.googleapis.com/
                "/webfont.js",
                "webfont\\{0}.js"
            ),
            new CdnEntry(
                "https://stackpath.bootstrapcdn.com/bootstrap/",
                "/js/bootstrap.min.js",
                "bootstrap\\{0}.min.js"
            ),
            new CdnEntry(
                "https://code.jquery.com/jquery-",
                ".min.js",
                "jquery\\{0}.min.js"
            ),
            new CdnEntry(
                "ajax/libs/jqueryui/",//https://ajax.googleapis.com/
                "/jquery-ui.min.js",
                "/jquery-ui\\{0}.min.js"
            ),
            new CdnEntry(
                "ajax/libs/angularjs/",//https://ajax.googleapis.com/
                "/angular.min.js",
                "angular\\{0}.min.js"
            ),
            new CdnEntry(
                "ajax/libs/react/",//https://cdnjs.cloudflare.com/
                "/react.min.js",
                "react\\{0}.min.js"
            ),
            new CdnEntry(//https://bluetriangle.com/blog/js-delivery-optimization-for-web-performance
                "ajax/libs/font-awesome/",//https://cdnjs.cloudflare.com/
                "/css",
                "font-awesome\\{0}.css"
            ),
            //new CdnEntry(
            //    "https://fonts.googleapis.com/css?family=",
            //    "",
            //    "fonts\\google\\{0}.css"
            //)
        };
        
        string? GetLocalCdn(string Url)
        {
            foreach (var Entry in CdnOverrides)
            {
                if (Entry.TryMatch(Url, out var Path))
                    return Path;
            }
            return null;
        }*/

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            //if (request.ResourceType == ResourceType.Prefetch || request.ResourceType == ResourceType.NavigationPreLoadMainFrame || request.ResourceType == ResourceType.NavigationPreLoadSubFrame)
            //    return CefReturnValue.Cancel; Breaks YouTube

            /*if (request.ResourceType == ResourceType.Worker || request.ResourceType == ResourceType.SharedWorker || request.ResourceType == ResourceType.ServiceWorker)
                return CefReturnValue.Cancel;*/
            string Url = request.Url.ToLowerInvariant();

            //MessageBox.Show(Url);
            //if (Url.Contains("disable-devtool"))
            //    return CefReturnValue.Cancel;
            /*if (request.Url.EndsWith("devtools.svg", StringComparison.Ordinal))
                return CefReturnValue.Cancel;*/
            if (Utils.IsHttpScheme(Url))
            {
                /*if (request.ResourceType == ResourceType.Script)
                {
                    string MainHost = Utils.FastHost(browser.MainFrame.Url);
                    string RequestHost = Utils.FastHost(request.Url);
                    if (!RequestHost.EndsWith(MainHost, StringComparison.Ordinal))
                        return CefReturnValue.Cancel;
                }*/
                /*string? CdnOverride = GetLocalCdn(Url);
                if (!string.IsNullOrEmpty(CdnOverride))
                {
                    string CdnFile = Path.Combine(App.Instance.CdnPath, CdnOverride);
                    MessageBox.Show(Url);
                    MessageBox.Show(CdnFile);
                    if (File.Exists(CdnFile))
                        request.Url = CdnFile;
                }*/

                if (!App.Instance.ExternalFonts && request.ResourceType == ResourceType.FontResource)
                    return CefReturnValue.Cancel;
                if (App.Instance.NeverSlowMode)
                {
                    if (Handler.IsOverBudget(request.ResourceType))
                        return CefReturnValue.Cancel;
                    foreach (string Pattern in App.FailedScripts)
                        if (Url.Contains(Pattern))
                            return CefReturnValue.Cancel;
                }
                //https://chromium-review.googlesource.com/c/chromium/src/+/1265506/25/third_party/blink/renderer/platform/loader/fetch/resource_loader.cc
                if (App.Instance.AdBlock == 1)
                {
                    if (browser.FocusedFrame != null && App.Instance.AdBlockAllowList.Has(Utils.FastHost(browser.FocusedFrame.Url)))
                            return CefReturnValue.Continue;
                    if (request.ResourceType == ResourceType.Ping)
                    {
                        App.Instance.TrackersBlocked++;
                        return CefReturnValue.Cancel;
                    }
                    else if (Utils.IsPossiblyAd(request.ResourceType))
                    {
                        string Host = Utils.FastHost(Url);
                        if (App.Ads.Has(Host))// || App.Miners.Has(Host)
                        {
                            App.Instance.AdsBlocked++;
                            return CefReturnValue.Cancel;
                        }
                        else if (App.Analytics.Has(Host))
                        {
                            App.Instance.TrackersBlocked++;
                            return CefReturnValue.Cancel;
                        }
                        else if (request.ResourceType == ResourceType.Script)// || request.ResourceType == ResourceType.Xhr
                        {
                            foreach (string Pattern in App.HasInLink)
                                if (Url.Contains(Pattern))
                                    return CefReturnValue.Cancel;
                        }
                    }
                }

                //https://blog.chromium.org/2024/02/optimizing-safe-browsing-checks-in.html
                /*if (App.Instance.GoogleSafeBrowsing && Utils.CanCheckSafeBrowsing(request.ResourceType))
                {
                    SafeBrowsingHandler.ThreatType _ThreatType = App.Instance._SafeBrowsing.GetThreatType(App.Instance._SafeBrowsing.Response(Url));
                    if (_ThreatType == SafeBrowsingHandler.ThreatType.Malware || _ThreatType == SafeBrowsingHandler.ThreatType.Unwanted_Software || _ThreatType == SafeBrowsingHandler.ThreatType.Social_Engineering)
                        return CefReturnValue.Cancel;
                }*/
                if (App.Instance.LiteMode)
                    request.SetHeaderByName("Save-Data", "on", true);
                //if (App.Instance.MobileView)
                if (Handler.BrowserView.UserAgentBranding)
                {
                    request.SetHeaderByName("User-Agent", App.Instance.UserAgent, true);
                    //WARNING: \r\n SHOULD NOT BE REMOVED, CLOUDFLARE TURNSTILE WILL NOT WORK
                    request.SetHeaderByName("sec-ch-ua", $"\r\n{App.Instance.UserAgentBrandsString}", true);
                    //WARNING: \r\n SHOULD NOT BE REMOVED, CLOUDFLARE TURNSTILE WILL NOT WORK
                }
                //request.SetHeaderByName("Device-Memory", "0.25", true);
                //request.SetHeaderByName("DNT", "1", true);
            }
            return CefReturnValue.Continue;
        }

        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return true;//request.Url.StartsWith("mailto", StringComparison.Ordinal);
        }

        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            if (App.Instance.NeverSlowMode)
            {
                if (status == UrlRequestStatus.Success)
                    Handler.DeductFromBudget(request.ResourceType, receivedContentLength);
                else if (status == UrlRequestStatus.Failed)
                {
                    if (request.ResourceType != ResourceType.Script)
                        return;
                    App.FailedScripts.Add(Utils.CleanUrl(request.Url.ToLowerInvariant(), true, true, true, true, true));
                }
            }
        }

        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
        }

        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            //TODO: Always enable cache regardless of Cache-Control: no-cache
            /*request.SetHeaderByName("Cache-Control", "public, max-age=31536000", true);
            request.Headers.Remove("Pragma");
            request.Headers.Remove("Expires");
            request.Headers.Remove("ETag");*/
            if (App.Instance.AMP && request.ResourceType == ResourceType.MainFrame)
            {
                if (frame.IsMain)
                {
                    string Url = request.Url;
                    browser.GetSourceAsync().ContinueWith(TaskHtml =>
                    {
                        string? AMPUrl = Utils.ParseAMPLink(TaskHtml.Result, Url);
                        if (!string.IsNullOrEmpty(AMPUrl))
                        {
                            chromiumWebBrowser.Stop();
                            chromiumWebBrowser.Load(AMPUrl);
                        }
                    });
                }
            }
            //return !Handler.CanLoadUnderBudget(request.ResourceType, receivedContentLength);

            //if (response.StatusCode == 404)
            //    BrowserView.Prompt($"404, Do you want open the page in the Wayback Machine?", true, "Download", $"24<,>https://web.archive.org/{request.Url}", $"https://web.archive.org/{request.Url}", true, "\xE896");
            return false;
        }
    }
}
