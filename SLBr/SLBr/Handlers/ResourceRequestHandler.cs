// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            /*try
            {
                if (Utils.CanCheck(request.TransitionType))
                {
                    if (Utils.CleanUrl(request.Url).Contains("facebook.com"))
                        request.SetHeaderByName("user-agent", MainWindow.Instance.UserAgent.Replace($"Chromium", "Chrome"), true);
                    else if (Utils.CleanUrl(request.Url).Contains("web.whatsapp.com"))
                        request.SetHeaderByName("user-agent", MainWindow.Instance.UserAgent.Replace($"Chromium", "Chrome"), true);
                    else if (Utils.CleanUrl(request.Url).Contains("mail.google.com") || Utils.CleanUrl(request.Url).Contains("web.whatsapp.com"))
                        request.SetHeaderByName("user-agent", MainWindow.Instance.UserAgent.Replace($"SLBr {MainWindow.Instance.ReleaseVersion}", "").Replace($"Chromium", "Chrome"), true);
                }
            }
            catch { }*/
            //Chrome Web Store Experiment
            //request.SetHeaderByName("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36 Edg/98.0.1108.62", false);
            //request.SetHeaderByName("sec-ch-ua", "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"98\", \"Microsoft Edge\";v=\"98\"", false);
            //request.SetHeaderByName("cookie", "1P_JAR=2022-02-28-01; NID=511=WF7xnRY3fmPoA8KW27PerWOTOjQrw1MMhOIK3-B1hIT0zDunpoEaSN5p4x0sTx7gPBuJ8UlVHtDvHbZTqtDop0fPvTQYKwaVDK-DfHKzTYn2BhDLM_Xmw2E6LY_G89d2WO1xYeOmfV-0vvixbyJonM-OtnhNMHyLMoAS7nDtItdu8bSI92GP_Nk_fCjldDyD0KminbX_bpVF; __utma=73091649.1413177246.1600596546.1645953634.1646110777.3; __utmc=73091649; __utmz=73091649.1646110777.3.2.utmcsr=bing|utmccn=(organic)|utmcmd=organic|utmctr=(not provided); __utmt=1; __utmb=73091649.91.9.1646112576592", false);
            return CefReturnValue.Continue;
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
