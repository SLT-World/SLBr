// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SLBr
{
    public class ResourceRequestHandler : IResourceRequestHandler
    {
        bool AdBlock;
        bool TrackerBlock;
        public ResourceRequestHandler(bool _AdBlock, bool _TrackerBlock)
        {
            AdBlock = _AdBlock;
            TrackerBlock = _TrackerBlock;
        }

        public void Dispose()
        {
            GC.Collect();
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
        HashSet<string> HasInLink = new HashSet<string> {
            "smartadserver.com", "bidswitch.net", "taboola", "amazon-adsystem.com", "survey.min.js", "survey.js", "social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js", "ad.js", "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "adserver", "smartadserver", "usync.js", "moneybid.js", "miner.js", "prebid", "youtube.com/ptracking"
        };
        /*HashSet<string> Urls = new HashSet<string> {
            "cse.google.com/adsense"
        };*/
        HashSet<string> Ads = new HashSet<string> {
            "pagead2.googlesyndication.com", "tpc.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "ade.googlesyndication.com", "pagead2.googleadservices.com", "adservice.google.com", "googleadservices.com",
            "doubleclick.net", "ad.doubleclick.net", "cm.g.doubleclick.net", "static.doubleclick.net", "m.doubleclick.net", "mediavisor.doubleclick.net", "googleads.g.doubleclick.net", "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net", "www3.doubleclick.net", "ads.google.com",
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
            "yandexadexchange.net", "adsdk.yandex.ru",

            //ads.yieldmo.com
            //match.adsrvr.org

            "api.ad.xiaomi.com",
            "sdkconfig.ad.xiaomi.com",
            "sdkconfig.ad.intl.xiaomi.com",
            "globalapi.ad.xiaomi.com",
            "t.adx.opera.com",

            "business.samsungusa.com", "samsungads.com", "ad.samsungadhub.com", "config.samsungads.com", "samsung-com.112.2o7.net",
            "click.oneplus.com", "click.oneplus.cn", "open.oneplus.net",
            "asadcdn.com",
            "static.adsafeprotected.com",
            "ib.adnxs.com",
            "sync.smartadserver.com",
            "match.adsrvr.org",
            "scdn.cxense.com",
            "adserver.juicyads.com",
            "a.realsrv.com", "mc.yandex.ru", "a.vdo.ai",
            //"dt.adsafeprotected.com",
            "z-na.amazon-adsystem.com", "aax-us-east.amazon-adsystem.com", "fls-na.amazon-adsystem.com", "z-na.amazon-adsystem.com",
            "ads.betweendigital.com", "rtb.adpone.com", "ads.themoneytizer.com", "bidder.criteo.com", "bidder.criteo.com", "bidder.criteo.com",
            "secure-assets.rubiconproject.com", "eus.rubiconproject.com", "fastlane.rubiconproject.com", "pixel.rubiconproject.com",
            "ids.ad.gt", "powerad.ai", "hb.brainlyads.com", "pixel.quantserve.com", "ads.anura.io", "static.getclicky.com",
            "ad.turn.com", "rtb.mfadsrvr.com", "ad.mrtnsvr.com", "s.ad.smaato.net", "rtb-csync.smartadserver.com", "ssbsync.smartadserver.com", "pixel.tapad.com", "secure.adnxs.com", "data.adsrvr.org", "px.adhigh.net"
        };
        HashSet<string> Analytics = new HashSet<string> { "google-analytics.com", "ssl.google-analytics.com",
            "stats.wp.com",
            "analytics.facebook.com", "pixel.facebook.com",
            "analytics.tiktok.com", "analytics-sg.tiktok.com",
            "analytics.pinterest.com", "widgets.pinterest.com", "log.pinterest.com", "trk.pinterest.com",
            "analytics.pointdrive.linkedin.com",
            "analyticsengine.s3.amazonaws.com", "affiliationjs.s3.amazonaws.com",
            "analytics.mobile.yandex.net", "appmetrica.yandex.com", "extmaps-api.yandex.net",
            "analytics.yahoo.com", "ups.analytics.yahoo.com",
            "metrics.apple.com",
            "hotjar.com", "static.hotjar.com", "api-hotjar.com",
            "mouseflow.com", "a.mouseflow.com",
            "freshmarketer.com",
            "notify.bugsnag.com", "sessions.bugsnag.com", "api.bugsnag.com", "app.bugsnag.com",
            "browser.sentry-cdn.com", "app.getsentry.com",
            "id5-sync.com", "bttrack.com", "idsync.rlcdn.com", "u.openx.net", "sync-t1.taboola.com", "x.bidswitch.net", "rtd-tm.everesttech.net", "usermatch.krxd.net", "visitor.omnitagjs.com", "ping.chartbeat.net",

            "luckyorange.com", "cdn.luckyorange.com", "w1.luckyorange.com", "upload.luckyorange.net", "cs.luckyorange.net", "settings.luckyorange.net",

            "data.mistat.xiaomi.com",
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
            "logbak.hicloud.com",

            "smetrics.samsung.com", "nmetrics.samsung.com", "analytics-api.samsunghealthcn.com",
            "securemetrics.apple.com", "supportmetrics.apple.com", "metrics.icloud.com", "metrics.mzstatic.com",
            "sync.outbrain.com", "widgets.outbrain.com",
            "collect.mopinion.com", "pb-server.ezoic.com",
            "demand.trafficroots.com", "sync.srv.stackadapt.com", "sync.ipredictive.com", "analytics.vdo.ai", "tag-api-2-1.ccgateway.net", "sync.search.spotxchange.com",
            "reporting.powerad.ai"
        };
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var headers = request.Headers;
            //Filter Lists
            bool Continue = true;
            //if (request.TransitionType == TransitionType.AutoSubFrame)
            //    Continue = false;
            if (request.ResourceType == ResourceType.MainFrame || request.ResourceType == ResourceType.SubFrame || request.ResourceType == ResourceType.Image)
            {
                if (bool.Parse(MainWindow.Instance.MainSave.Get("LiteMode")))
                {
                    headers["Save-Data"] = "on";
                    headers["Device-Memory"] = "0.25";
                }
            }
            if (request.ResourceType == ResourceType.Xhr || request.ResourceType == ResourceType.Script || request.ResourceType == ResourceType.Image || request.ResourceType == ResourceType.SubFrame)
            {
                if (AdBlock || TrackerBlock)
                {
                    string CleanedUrl = Utils.CleanUrl(request.Url, true, true);//false if check full path
                    string Host = Utils.Host(CleanedUrl, true, false);
                    if (request.ResourceType == ResourceType.Script || request.ResourceType == ResourceType.Xhr)
                    {
                        foreach (string Script in HasInLink)
                        {
                            if (CleanedUrl.Contains(Script.ToLower()))
                                Continue = false;
                        }
                        if (bool.Parse(MainWindow.Instance.MainSave.Get("DoNotTrack")))
                            headers["DNT"] = "1";
                    }
                    /*if (Scripts.Contains(Path.GetFileName(CleanedUrl)))
                        Continue = false;
                    else */
                    /*if (request.ResourceType == ResourceType.Script)
                    {
                        foreach (string Script in Scripts)
                        {
                            if (CleanedUrl.Contains(Script.ToLower()))
                                return CefReturnValue.Cancel;
                        }
                    }*/
                    if (Continue)
                    {
                        if (TrackerBlock)
                            if (Analytics.Contains(Host))
                                Continue = false;
                        if (Continue)
                        {
                            if (AdBlock)
                                if (Ads.Contains(Host))
                                    Continue = false;
                        }
                    }
                }
            }
            request.Headers = headers;
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

            //else if (code == 0 || code == 444 || (code >= 500 && code <= 599))
            //    frame.LoadUrl("slbr://cannotconnect" + "?url=" + request.Url);
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
