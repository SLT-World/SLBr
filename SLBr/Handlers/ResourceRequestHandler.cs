using CefSharp;
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
            return null;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return null;
        }
        //"ad.js" causes reddit to go weird
        FastHashSet<string> HasInLink = new FastHashSet<string> {
            "smartadserver.com", "bidswitch.net", "taboola", "amazon-adsystem.com", "survey.min.js", "survey.js", "social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "adserver", "smartadserver", "usync.js", "moneybid.js", "miner.js", "prebid", "fls.doubleclick.net",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",

            "google.com/ads", "play.google.com/log"/*, "youtube.com/ptracking", "youtube.com/pagead/adview", "youtube.com/api/stats/ads", "youtube.com/pagead/interaction",*/
        };
        FastHashSet<string> MinersFiles = new FastHashSet<string> {
            "cryptonight.wasm", "deepminer.js", "deepminer.min.js", "coinhive.min.js", "monero-miner.js", "wasmminer.wasm", "wasmminer.js", "cn-asmjs.min.js", "gridcash.js",
            "worker-asmjs.min.js", "miner.js", "webmr4.js", "webmr.js", "webxmr.js",
            "lib/crypta.js", "static/js/tpb.js", "bitrix/js/main/core/core_tasker.js", "bitrix/js/main/core/core_loader.js", "vbb/me0w.js", "lib/crlt.js", "pool/direct.js",
            "plugins/wp-monero-miner-pro", "plugins/ajcryptominer", "plugins/aj-cryptominer",
            "?perfekt=wss://", "?proxy=wss://", "?proxy=ws://"
        };//https://github.com/xd4rker/MinerBlock/blob/master/assets/filters.txt
        FastHashSet<string> Miners = new FastHashSet<string> {
            "coin-hive.com", "coin-have.com", "adminer.com", "ad-miner.com", "coinminerz.com", "coinhive-manager.com", "coinhive.com", "prometheus.phoenixcoin.org", "coinhiveproxy.com", "jsecoin.com", "crypto-loot.com", "cryptonight.wasm", "cloudflare.solutions"
        };//https://v.firebog.net/hosts/static/w3kbl.txt
        FastHashSet<string> Ads = new FastHashSet<string> {
            "ads.google.com", "pagead2.googlesyndication.com", "afs.googlesyndication.com", "tpc.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "ade.googlesyndication.com", "pagead2.googleadservices.com", "adservice.google.com", "googleadservices.com",
            "app-measurement.com", "googleads2.g.doubleclick.net", "googleads3.g.doubleclick.net", "googleads4.g.doubleclick.net", "googleads5.g.doubleclick.net", "googleads6.g.doubleclick.net", "googleads7.g.doubleclick.net", "googleads8.g.doubleclick.net", "googleads9.g.doubleclick.net",
            "doubleclick.net", "stats.g.doubleclick.net", "ad.doubleclick.net", "ads.doubleclick.net", "ad.mo.doubleclick.net", "ad-g.doubleclick.net", "cm.g.doubleclick.net", "static.doubleclick.net", "m.doubleclick.net", "mediavisor.doubleclick.net", "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net", "www3.doubleclick.net",
            "secure-ds.serving-sys.com", "s.innovid.com", "innovid.com", "dts.innovid.com",
            "googleads.g.doubleclick.net", "pagead.l.doubleclick.net", "g.doubleclick.net", "fls.doubleclick.net",
            "gads.pubmatic.com", "ads.pubmatic.com", "ogads-pa.clients6.google.com",// "image6.pubmatic.com",
            "ads.facebook.com", "an.facebook.com",
            "cdn.snigelweb.com", "cdn.connectad.io", //For w3schools
            "pool.admedo.com", "c.pub.network",
            "media.ethicalads.io",
            "ad.youtube.com", "ads.youtube.com", "youtube.cleverads.vn",
            "prod.di.api.cnn.io", "get.s-onetag.com", "assets.bounceexchange.com", "gn-web-assets.api.bbc.com", "pub.doubleverify.com",
            "events.reddit.com",
            "ads.tiktok.com", "ads-sg.tiktok.com", "ads.adthrive.com", "ads-api.tiktok.com", "business-api.tiktok.com",
            "ads.reddit.com", "d.reddit.com", "rereddit.com", "events.redditmedia.com",
            "ads-twitter.com", "static.ads-twitter.com", "ads-api.twitter.com", "advertising.twitter.com",
            "ads.pinterest.com", "ads-dev.pinterest.com",
            "adtago.s3.amazonaws.com", "advice-ads.s3.amazonaws.com", "advertising-api-eu.amazon.com", "c.amazon-adsystem.com", "s.amazon-adsystem.com", "amazonclix.com",
            "ads.linkedin.com",
            "static.media.net", "media.net", "adservetx.media.net",
            "media.fastclick.net", "cdn.fastclick.net",
            "global.adserver.yahoo.com", "advertising.yahoo.com", "ads.yahoo.com", "ads.yap.yahoo.com", "adserver.yahoo.com", "partnerads.ysm.yahoo.com", "adtech.yahooinc.com", "advertising.yahooinc.co",
            "api-adservices.apple.com", "advertising.apple.com", "tr.iadsdk.apple.com",
            "yandexadexchange.net", "adsdk.yandex.ru", "advertising.yandex.ru",

            "ads30.adcolony.com", "adc3-launch.adcolony.com", "events3alt.adcolony.com", "wd.adcolony.com",
            "adm.hotjar.com",
            "webview.unityads.unity3d.com",
            "files.adform.net",
            "static.adsafeprotected.com", "pixel.adsafeprotected.com",
            "api.ad.xiaomi.com", "sdkconfig.ad.xiaomi.com", "sdkconfig.ad.intl.xiaomi.com", "globalapi.ad.xiaomi.com",
            "adsfs.oppomobile.com", "adx.ads.oppomobile.com", "ck.ads.oppomobile.com", "data.ads.oppomobile.com",
            "t.adx.opera.com",
            "bdapi-ads.realmemobile.com", "bdapi-in-ads.realmemobile.com",
            "business.samsungusa.com", "samsungads.com", "ad.samsungadhub.com", "config.samsungads.com", "samsung-com.112.2o7.net",
            "click.oneplus.com", "click.oneplus.cn", "open.oneplus.net",
            "asadcdn.com",
            "ads.yieldmo.com", "match.adsrvr.org", "ads.servenobid.com", "e3.adpushup.com", "c1.adform.net",
            "ib.adnxs.com",
            "sync.smartadserver.com", "ad.a-ads.com",
            "cdn.carbonads.com", "px.ads.linkedin.com",
            "match.adsrvr.org",
            "scdn.cxense.com",
            "acdn.adnxs.com",
            "js.adscale.de",
            "gum.criteo.com",
            "js.hsadspixel.net",
            "adserver.juicyads.com",
            "a.realsrv.com", "mc.yandex.ru", "a.vdo.ai", "adfox.yandex.ru", "adfstat.yandex.ru", "offerwall.yandex.net",
            "ads.msn.com", "adnxs.com", "adnexus.net", "bingads.microsoft.com",
            "dt.adsafeprotected.com",
            "amazonaax.com", "z-na.amazon-adsystem.com", "aax-us-east.amazon-adsystem.com", "fls-na.amazon-adsystem.com", "z-na.amazon-adsystem.com",
            "ads.betweendigital.com", "rtb.adpone.com", "ads.themoneytizer.com", "bidder.criteo.com", "bidder.criteo.com", "bidder.criteo.com",
            "secure-assets.rubiconproject.com", "eus.rubiconproject.com", "fastlane.rubiconproject.com", "pixel.rubiconproject.com", "prebid-server.rubiconproject.com",
            "ids.ad.gt", "powerad.ai", "hb.brainlyads.com", "pixel.quantserve.com", "ads.anura.io", "static.getclicky.com",
            "ad.turn.com", "rtb.mfadsrvr.com", "ad.mrtnsvr.com", "s.ad.smaato.net", "rtb-csync.smartadserver.com", "ssbsync.smartadserver.com",
            "adpush.technoratimedia.com", "pixel.tapad.com", "secure.adnxs.com", "data.adsrvr.org", "px.adhigh.net",
            "epnt.ebay.com", "yt.moatads.com", "pixel.moatads.com", "mb.moatads.com", "ad.adsrvr.org", "a.ad.gt", "pixels.ad.gt", "z.moatads.com", "px.moatads.com", "s.pubmine.com", "px.ads.linkedin.com", "p.adsymptotic.com",
            "btloader.com", "ad-delivery.net",
            "services.vlitag.com", "tag.vlitag.com", "assets.vlitag.com",
            "adserver.snapads.com", "euw.adserver.snapads.com", "euc.adserver.snapads.com", "usc.adserver.snapads.com", "ase.adserver.snapads.com",
            "cdn.adsafeprotected.com",
            "rp.liadm.com",

            "h.seznam.cz", "d.seznam.cz", "ssp.seznam.cz",
            "cdn.performax.cz", "dale.performax.cz", "chip.performax.cz"
        };
        FastHashSet<string> Analytics = new FastHashSet<string> { "ssl-google-analytics.l.google.com", "www-google-analytics.l.google.com", "www-googletagmanager.l.google.com", "analytic-google.com", "google-analytics.com", "ssl.google-analytics.com",
            "stats.wp.com",
            "analytics.google.com", "click.googleanalytics.com",
            "analytics.facebook.com", "pixel.facebook.com",
            "analytics.tiktok.com", "analytics-sg.tiktok.com", "log.byteoversea.com",
            "analytics.pinterest.com", "widgets.pinterest.com", "log.pinterest.com", "trk.pinterest.com",
            "analytics.pointdrive.linkedin.com",
            "analyticsengine.s3.amazonaws.com", "affiliationjs.s3.amazonaws.com", "analytics.s3.amazonaws.com",
            "analytics.mobile.yandex.net", "appmetrica.yandex.com", "extmaps-api.yandex.net", "appmetrica.yandex.ru", "metrika.yandex.ru",
            "analytics.yahoo.com", "ups.analytics.yahoo.com", "analytics.query.yahoo.com", "log.fc.yahoo.com", "geo.yahoo.com", "udc.yahoo.com", "udcm.yahoo.com", "gemini.yahoo.com",
            "metrics.apple.com",
            "surveys.hotjar.com", "insights.hotjar.com", "identify.hotjar.com", "careers.hotjar.com", "script.hotjar.com",
            "mouseflow.com", "a.mouseflow.com", "o2.mouseflow.com", "cdn.mouseflow.com", "cdn-test.mouseflow.com", "tools.mouseflow.com",
            "freshmarketer.com",
            "notify.bugsnag.com", "sessions.bugsnag.com", "api.bugsnag.com", "app.bugsnag.com",
            "browser.sentry-cdn.com", "app.getsentry.com",
            "stats.gc.apple.com", "iadsdk.apple.com",
            "collector.github.com",
            "cloudflareinsights.com",

            "auction.unityads.unity3d.com", "config.unityads.unity3d.com",

            "openbid.pubmatic.com", "prebid.media.net", "hbopenbid.pubmatic.com",
            "collector.cdp.cnn.com", "smetrics.cnn.com", "mybbc-analytics.files.bbci.co.uk", "a1.api.bbc.co.uk", "xproxy.api.bbc.com",
            "uk-script.dotmetrics.net", "rm-script.dotmetrics.net", "scripts.webcontentassessor.com",
            "collector.brandmetrics.com", "sb.scorecardresearch.com",
            "queue.simpleanalyticscdn.com",
            "cdn.permutive.com", "api.permutive.com",

            "luckyorange.com", "api.luckyorange.com", "realtime.luckyorange.com", "cdn.luckyorange.com", "w1.luckyorange.com", "upload.luckyorange.net", "cs.luckyorange.net", "settings.luckyorange.net",


            "smetrics.samsung.com", "nmetrics.samsung.com", "analytics-api.samsunghealthcn.com",
            "iot-eu-logser.realme.com", "iot-logser.realme.com",
            "securemetrics.apple.com", "supportmetrics.apple.com", "metrics.icloud.com", "metrics.mzstatic.com", "books-analytics-events.apple.com", "weather-analytics-events.apple.com", "notes-analytics-events.apple.com",

            "tr.snapchat.com", "sc-analytics.appspot.com", "app-analytics.snapchat.com",
            "crashlogs.whatsapp.net",

            "click.a-ads.com",
            "static.criteo.net",
            "www.clarity.ms",
            "u.clarity.ms",
            "claritybt.freshmarketer.com",

            "data.mistat.xiaomi.com",
            "data.mistat.intl.xiaomi.com",
            "data.mistat.india.xiaomi.com",
            "data.mistat.rus.xiaomi.com",
            "tracking.miui.com",
            "sa.api.intl.miui.com",
            "tracking.intl.miui.com",
            "tracking.india.miui.com",
            "tracking.rus.miui.com",

            "metrics.data.hicloud.com",
            "metrics1.data.hicloud.com",
            "metrics2.data.hicloud.com",
            "metrics5.data.hicloud.com",
            "logservice.hicloud.com",
            "logservice1.hicloud.com",
            "metrics-dra.dt.hicloud.com",
            "logbak.hicloud.com",
            "grs.hicloud.com",

            "s.cdn.turner.com",
            "logx.optimizely.com",
            "signal-metrics-collector-beta.s-onetag.com",
            "connect-metrics-collector.s-onetag.com",
            "ping.chartbeat.net",
            "logs.browser-intake-datadoghq.com",
            "onsiterecs.api.boomtrain.com",
            "adx.adform.net",

            "b.6sc.co",
            "api.bounceexchange.com", "events.bouncex.net",
            "assets.adobedtm.com",
            "static.chartbeat.com",
            "dsum-sec.casalemedia.com",

            "aa.agkn.com",
            "material.anonymised.io",
            "static.anonymised.io",
            "experience.tinypass.com",
            "dn.tinypass.com",
            "dw-usr.userreport.com",
            "capture-api.reachlocalservices.com",
            "capture-api.reachlocalservices.com",
            "discovery.evvnt.com",
            "mab.chartbeat.com",
            "sync.sharethis.com",
            "bcp.crwdcntrl.net",

            "prebid.a-mo.net",
            "tpsc-sgc.doubleverify.com", "cdn.doubleverify.com", "onetag-sys.com",
            "id5-sync.com", "bttrack.com", "idsync.rlcdn.com", "u.openx.net", "sync-t1.taboola.com", "x.bidswitch.net", "rtd-tm.everesttech.net", "usermatch.krxd.net", "visitor.omnitagjs.com", "ping.chartbeat.net",
            "sync.outbrain.com", "widgets.outbrain.com",
            "collect.mopinion.com", "pb-server.ezoic.com",
            "hb.adscale.de",
            "demand.trafficroots.com", "sync.srv.stackadapt.com", "sync.ipredictive.com", "analytics.vdo.ai", "tag-api-2-1.ccgateway.net", "sync.search.spotxchange.com",
            "reporting.powerad.ai", "monitor.ebay.com", "beacon.walmart.com", "capture.condenastdigital.com", "a.pub.network"
        };

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            //MessageBox.Show(Utils.Host(frame.Url));
            if (Utils.IsHttpScheme(request.Url))
            {
                //https://chromium-review.googlesource.com/c/chromium/src/+/1265506/25/third_party/blink/renderer/platform/loader/fetch/resource_loader.cc
                if (App.Instance.NeverSlowMode && Handler.IsOverBudget(request.ResourceType))
                    return CefReturnValue.Cancel;
                if ((App.Instance.AdBlock || App.Instance.TrackerBlock) && !Utils.Host(frame.Url).EndsWith("ecosia.org", StringComparison.Ordinal))
                {
                    if (request.ResourceType == ResourceType.Ping)
                    {
                        App.Instance.TrackersBlocked++;
                        return CefReturnValue.Cancel;
                    }
                    else if (Utils.IsPossiblyAd(request.ResourceType))
                    {
                        string CleanedUrl = Utils.CleanUrl(request.Url, true, true, true, true);
                        if (request.ResourceType == ResourceType.Script || request.ResourceType == ResourceType.Xhr)
                        {
                            foreach (string Script in HasInLink)
                            {
                                if (CleanedUrl.AsSpan().IndexOf(Script, StringComparison.Ordinal) >= 0)
                                    return CefReturnValue.Cancel;
                            }
                        }
                        string Host = Utils.Host(CleanedUrl, true);
                        if (App.Instance.TrackerBlock && Analytics.Contains(Host))
                        {
                            App.Instance.TrackersBlocked++;
                            return CefReturnValue.Cancel;
                        }
                        else if (App.Instance.AdBlock && (Ads.Contains(Host) || Miners.Contains(Host)))
                        {
                            App.Instance.AdsBlocked++;
                            return CefReturnValue.Cancel;
                        }
                    }
                }
                if (App.Instance.GoogleSafeBrowsing && Utils.CanCheckSafeBrowsing(request.ResourceType))
                {
                    string Response = App.Instance._SafeBrowsing.Response(request.Url);
                    SafeBrowsingHandler.ThreatType _ThreatType = App.Instance._SafeBrowsing.GetThreatType(Response);
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
