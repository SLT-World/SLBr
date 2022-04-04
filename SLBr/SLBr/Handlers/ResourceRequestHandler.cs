// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace SLBr
{
    public class ResourceRequestHandler : IResourceRequestHandler
    {
        public void Dispose()
        {
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
        /*List<string> AdProviders = new List<string> { "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net",
            "www.googletagservices.com",
            "ads.google.com",
            "googleadservices.com", "pagead2.googleadservices.com",
            "gads.pubmatic.com", "ads.pubmatic.com",
            "tpc.googlesyndication.com", "pagead2.googlesyndication.com", "googleads.g.doubleclick.net" };*/
        //List<string> Ads = new List<string> { "youtube.com/ads", "google-analytics.com", "analytics.facebook.com", "analytics.pointdrive.linkedin.com", "analytics.pinterest.com", "analytics.tiktok.com", "analytics-sg.tiktok.com", "ads.tiktok.com", "ads-sg.tiktok.com" };
        //HashSet<string> FullAdLinks = new HashSet<string> {
            //"youtube.com/youtubei/v1/player/ad_break", "youtube.com/youtubei/v1/log_event", "youtube.com/pagead/interaction/", "youtube.com/api/stats/ads", "youtube.com/pagead/paralleladview"
            //, "youtube.com/pagead/adview"/*, "youtube.com/ptracking"*/, "youtube.com/pcs/activeview"
        //};
        HashSet<string> Ads = new HashSet<string> {
            "pagead2.googlesyndication.com", "tpc.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "ade.googlesyndication.com", "pagead2.googleadservices.com", "adservice.google.com", "googleadservices.com",
            "doubleclick.net", "ad.doubleclick.net", "static.doubleclick.net", "m.doubleclick.net", "mediavisor.doubleclick.net", "googleads.g.doubleclick.net", "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net",
            "gads.pubmatic.com", "ads.pubmatic.com",
            "ads.facebook.com", "an.facebook.com",
            "ads.youtube.com", "youtube.cleverads.vn"/*, "yt3.ggpht.com"*/,
            "ads.tiktok.com", "ads-sg.tiktok.com",
            "ads.reddit.com", "d.reddit.com", "rereddit.com", "events.redditmedia.com",
            "ads-twitter.com", "static.ads-twitter.com", "ads-api.twitter.com", "advertising.twitter.com",
            "ads.pinterest.com", "ads-dev.pinterest.com", 
            "adtago.s3.amazonaws.com", "advice-ads.s3.amazonaws.com", "advertising-api-eu.amazon.com", "c.amazon-adsystem.com", "s.amazon-adsystem.com",
            "ads.linkedin.com",
            "static.media.net", "media.net", "adservetx.media.net",
            "media.fastclick.net", "cdn.fastclick.net", 
            "global.adserver.yahoo.com", "ads.yahoo.com", "ads.yap.yahoo.com",
            "yandexadexchange.net", "adsdk.yandex.ru"//,

            //ads.yieldmo.com
            //match.adsrvr.org

            /*"api.ad.xiaomi.com",
            "sdkconfig.ad.xiaomi.com",
            "sdkconfig.ad.intl.xiaomi.com",
            "globalapi.ad.xiaomi.com",*/
        };
        HashSet<string> Analytics = new HashSet<string> { "google-analytics.com", "ssl.google-analytics.com",
            "stats.wp.com",
            "analytics.facebook.com", "pixel.facebook.com",
            "analytics.tiktok.com", "analytics-sg.tiktok.com",
            "analytics.pinterest.com", "widgets.pinterest.com", "log.pinterest.com", "trk.pinterest.com",
            "analytics.pointdrive.linkedin.com",
            "analyticsengine.s3.amazonaws.com", "affiliationjs.s3.amazonaws.com",
            "analytics.mobile.yandex.net", "appmetrica.yandex.com", "extmaps-api.yandex.net",
            "analytics.yahoo.com",
            "metrics.apple.com",
            "hotjar.com", "static.hotjar.com", "api-hotjar.com",
            "mouseflow.com", "a.mouseflow.com",
            "freshmarketer.com"//,

            /*"data.mistat.xiaomi.com",
            "data.mistat.intl.xiaomi.com",
            "data.mistat.india.xiaomi.com",
            "data.mistat.rus.xiaomi.com",
            "tracking.miui.com",
            "sa.api.intl.miui.com",
            "tracking.intl.miui.com",
            "tracking.india.miui.com",
            "tracking.rus.miui.com",

            "metrics1.data.hicloud.com",
            "metrics5.data.hicloud.com",
            "logservice.hicloud.com",
            "logservice1.hicloud.com",
            "metrics-dra.dt.hicloud.com",
            "logbak.hicloud.com"*/
        };
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            bool Continue = true;
            /*string Address = "BRUHHHHH";
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Address = chromiumWebBrowser.Address;
            }));
            MessageBox.Show($"{Utils.Host(request.Url)},{Utils.Host(Address)}");
                if (Utils.Host(request.Url) != Utils.Host(Address))
                Continue = false;*/
            //if (request.TransitionType == TransitionType.AutoSubFrame)
            //    Continue = false;
            if (request.ResourceType == ResourceType.Xhr || request.ResourceType == ResourceType.Script || request.ResourceType == ResourceType.Image || request.ResourceType == ResourceType.SubFrame)
            {
                //Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                //{
                string CleanedUrl = Utils.CleanUrl(request.Url, true, true);//false if check full path
                /*if (FullAdLinks.Contains(CleanedUrl))
                    Continue = false;
                if (Continue)
                {*/
                string Host = Utils.Host(CleanedUrl, true, false);
                if (Analytics.Contains(Host))
                    Continue = false;
                else if (Ads.Contains(Host))
                    Continue = false;
                //}

                    //if (request.Url.Contains("/pagead/"))
                    //    return CefReturnValue.Cancel;
                    /*foreach (string Ad in Analytics)
                    {
                        if (Utils.CleanUrl(request.Url).Contains(Ad))
                            return CefReturnValue.Cancel;
                    }*/
                    //MessageBox.Show(request.TransitionType.ToString() + "," + request.ResourceType.ToString());
                //}));
            }
            return Continue ? CefReturnValue.Continue : CefReturnValue.Cancel;
        }

        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return true;
        }

        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            int code = response.StatusCode;
            if (!frame.IsValid || frame.Url != request.Url)
                return;
            if (request.Url.StartsWith("file:///"))
            {
                //string _Path = request.Url/*.Substring(8)*/;
                //if (File.Exists(_Path) || Directory.Exists(_Path))
                //    return;
                //else
                //{
                //    frame.LoadUrl("slbr://cannotconnect" + "?url=" + request.Url);
                //}
            }
            /*else if (code == 404)
            {
                string Url = request.Url;
                if (!Utils.CleanUrl(Url).StartsWith("web.archive.org"))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        MainWindow.Instance.NewMessage("This page is missing, do you want to check if there's a saved version on the Wayback Machine?", $"https://web.archive.org/{Url}", "Check for saved version");*/
                    //dynamic Data = JObject.Parse(MainWindow.Instance.TinyDownloader.DownloadString($"https://archive.org/wayback/available?url={request.Url}"));
                    /*try
                    {
                        dynamic archived_snapshots = Data.archived_snapshots;
                        archived_snapshots.
                    }
                    catch { }*/
                    /*}));
                }
                //frame.LoadUrl("slbr://notfound" + "?url=" + request.Url);
            }*/
            //    else if (request.Url.StartsWith("file:///"))
            //    {
            //        string _Path = request.Url.Substring(8);
            //        if (!File.Exists(_Path))
            //            frame.LoadUrl("slbr://notfound" + "?url=" + _Path);
            //    }
            else if (code == 0 || code == 444 || (code >= 500 && code <= 599))
                frame.LoadUrl("slbr://cannotconnect" + "?url=" + request.Url);
        }

        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
        }

        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return false;
        }
    }
}
