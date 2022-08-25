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
        //FastHashSet<string> Ads = new FastHashSet<string> { "youtube.com/ads", "google-analytics.com", "analytics.facebook.com", "analytics.pointdrive.linkedin.com", "analytics.pinterest.com", "analytics.tiktok.com", "analytics-sg.tiktok.com", "ads.tiktok.com", "ads-sg.tiktok.com" };
        //FastHashSet<string> FullAdLinks = new FastHashSet<string> {
        //"youtube.com/youtubei/v1/player/ad_break", "youtube.com/youtubei/v1/log_event", "youtube.com/pagead/interaction/", "youtube.com/api/stats/ads", "youtube.com/pagead/paralleladview"
        //, "youtube.com/pagead/adview"/*, "youtube.com/ptracking"*/, "youtube.com/pcs/activeview"
        //};
        /*FastHashSet<string> Urls = new FastHashSet<string> {
            "cse.google.com/adsense"
        };*/
        FastHashSet<string> HasInLink = new FastHashSet<string> {
            "smartadserver.com", "bidswitch.net", "taboola", "amazon-adsystem.com", "survey.min.js", "survey.js", "social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "ad.js", "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "adserver", "smartadserver", "usync.js", "moneybid.js", "miner.js", "prebid", "youtube.com/ptracking", "fls.doubleclick.net", "google.com/ads",
            "advertising.js", "adsense.js", "track", "plusone.js"
        };
        FastHashSet<string> Miners = new FastHashSet<string> {
            "cryptonight.wasm", "deepminer.js", "deepminer.min.js", "coinhive.min.js", "monero-miner.js", "wasmminer.wasm", "wasmminer.js", "cn-asmjs.min.js", "gridcash.js",
            "worker-asmjs.min.js", "miner.js", "webmr4.js", "webmr.js", "webxmr.js",
            "lib/crypta.js", "static/js/tpb.js", "bitrix/js/main/core/core_tasker.js", "bitrix/js/main/core/core_loader.js", "vbb/me0w.js", "lib/crlt.js", "pool/direct.js",
            "plugins/wp-monero-miner-pro", "plugins/ajcryptominer", "plugins/aj-cryptominer",
            "?perfekt=wss://", "?proxy=wss://", "?proxy=ws://"
        };//https://github.com/xd4rker/MinerBlock/blob/master/assets/filters.txt
        FastHashSet<string> Ads = new FastHashSet<string> {
            "pagead2.googlesyndication.com", "tpc.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "ade.googlesyndication.com", "pagead2.googleadservices.com", "adservice.google.com", "googleadservices.com",
            "googleads2.g.doubleclick.net", "googleads3.g.doubleclick.net", "googleads4.g.doubleclick.net", "googleads5.g.doubleclick.net", "googleads6.g.doubleclick.net", "googleads7.g.doubleclick.net", "googleads8.g.doubleclick.net", "googleads9.g.doubleclick.net",
            "doubleclick.net", "ad.doubleclick.net", "cm.g.doubleclick.net", "static.doubleclick.net", "m.doubleclick.net", "mediavisor.doubleclick.net", "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net", "www3.doubleclick.net",
            "ads.google.com",
            "googleads.g.doubleclick.net",
            "gads.pubmatic.com", "ads.pubmatic.com",// "image6.pubmatic.com",
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

            "static.adsafeprotected.com", "pixel.adsafeprotected.com",

            "api.ad.xiaomi.com",
            "sdkconfig.ad.xiaomi.com",
            "sdkconfig.ad.intl.xiaomi.com",
            "globalapi.ad.xiaomi.com",
            "t.adx.opera.com",

            "business.samsungusa.com", "samsungads.com", "ad.samsungadhub.com", "config.samsungads.com", "samsung-com.112.2o7.net",
            "click.oneplus.com", "click.oneplus.cn", "open.oneplus.net",
            "asadcdn.com",
            "ads.yieldmo.com", "match.adsrvr.org", "ads.servenobid.com", "e3.adpushup.com", "c1.adform.net",
            "ib.adnxs.com",
            "sync.smartadserver.com",
            "match.adsrvr.org",
            "scdn.cxense.com",
            "adserver.juicyads.com",
            "a.realsrv.com", "mc.yandex.ru", "a.vdo.ai",
            "dt.adsafeprotected.com",
            "z-na.amazon-adsystem.com", "aax-us-east.amazon-adsystem.com", "fls-na.amazon-adsystem.com", "z-na.amazon-adsystem.com",
            "ads.betweendigital.com", "rtb.adpone.com", "ads.themoneytizer.com", "bidder.criteo.com", "bidder.criteo.com", "bidder.criteo.com",
            "secure-assets.rubiconproject.com", "eus.rubiconproject.com", "fastlane.rubiconproject.com", "pixel.rubiconproject.com", "prebid-server.rubiconproject.com",
            "ids.ad.gt", "powerad.ai", "hb.brainlyads.com", "pixel.quantserve.com", "ads.anura.io", "static.getclicky.com",
            "ad.turn.com", "rtb.mfadsrvr.com", "ad.mrtnsvr.com", "s.ad.smaato.net", "rtb-csync.smartadserver.com", "ssbsync.smartadserver.com",
            "adpush.technoratimedia.com", "pixel.tapad.com", "secure.adnxs.com", "data.adsrvr.org", "px.adhigh.net",
            "epnt.ebay.com", "mb.moatads.com", "ad.adsrvr.org", "a.ad.gt", "pixels.ad.gt", "z.moatads.com", "px.moatads.com", "s.pubmine.com", "px.ads.linkedin.com", "p.adsymptotic.com",
            "btloader.com", "ad-delivery.net", "ad.doubleclick.net",
            "services.vlitag.com", "tag.vlitag.com", "assets.vlitag.com"
        };
        FastHashSet<string> Analytics = new FastHashSet<string> { "google-analytics.com", "ssl.google-analytics.com",
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

            "prebid.media.net", "hbopenbid.pubmatic.com", "prebid.a-mo.net",
            "tpsc-sgc.doubleverify.com", "cdn.doubleverify.com", "onetag-sys.com",
            "id5-sync.com", "bttrack.com", "idsync.rlcdn.com", "u.openx.net", "sync-t1.taboola.com", "x.bidswitch.net", "rtd-tm.everesttech.net", "usermatch.krxd.net", "visitor.omnitagjs.com", "ping.chartbeat.net",
            "sync.outbrain.com", "widgets.outbrain.com",
            "collect.mopinion.com", "pb-server.ezoic.com",
            "demand.trafficroots.com", "sync.srv.stackadapt.com", "sync.ipredictive.com", "analytics.vdo.ai", "tag-api-2-1.ccgateway.net", "sync.search.spotxchange.com",
            "reporting.powerad.ai", "monitor.ebay.com", "beacon.walmart.com", "capture.condenastdigital.com", "a.pub.network"
        };
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var headers = request.Headers;
            //Filter Lists
            bool Continue = true;
            if (bool.Parse(MainWindow.Instance.MainSave.Get("LiteMode")))
            {
                //headers["Save-Data"] = "on";
                headers.Add("Save-Data", "on");
                headers.Add("Device-Memory", "0.25");
                //headers["Device-Memory"] = "0.25";
                //headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.63 Safari/537.36";
            }
            //if (Utils.IsDataOffender(request.ResourceType))
            //{
            //}

            //if (request.Url.StartsWith("ipfs://"))
            //    request.Url = request.Url.Replace("ipfs://", "https://cloudflare-ipfs.com/ipfs/");

            if (Utils.IsPossiblyAd(request.ResourceType))
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
                            {
                                Continue = false;
                                MainWindow.Instance.TrackersBlocked++;
                            }
                        if (Continue)
                        {
                            if (AdBlock)
                                if (Ads.Contains(Host))
                                {
                                    Continue = false;
                                    MainWindow.Instance.AdsBlocked++;
                                }
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
            //int code = response.StatusCode;
            //if (!frame.IsValid || frame.Url != request.Url)
            //    return;
            //if (request.Url.StartsWith("file:///"))
            //{

                //string _Path = request.Url/*.Substring(8)*/;
                //if (File.Exists(_Path) || Directory.Exists(_Path))
                //    return;
                //else
                //{
                //    frame.LoadUrl("slbr://cannotconnect" + "?url=" + request.Url);
                //}

            //}

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
