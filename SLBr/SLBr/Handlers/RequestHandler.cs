// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace SLBr
{
    public class RequestHandler : IRequestHandler
	{
		public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
		{
			return false;
		}

		public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
		{
			if (request.Url.Contains("roblox.com"))
				return true;
			//if (request.Url != frame.Url)
			//	return true;
			if (Utils.CanCheck(request.TransitionType) && !Utils.IsProtocolNotHttp(request.Url) && !Utils.IsProgramUrl(request.Url))//(isRedirect || userGesture || frame.IsMain)
			{
				string Response = MainWindow.Instance._SafeBrowsing.Response(request.Url.Replace("https://googleweblight.com/?lite_url=", ""));
				Utils.SafeBrowsing.ThreatType _ThreatType = Utils.CheckForInternetConnection() ? MainWindow.Instance._SafeBrowsing.GetThreatType(Response) : Utils.SafeBrowsing.ThreatType.Unknown;
				if (_ThreatType == Utils.SafeBrowsing.ThreatType.Malware || _ThreatType == Utils.SafeBrowsing.ThreatType.Unwanted_Software)
					//chromiumWebBrowser.LoadHtml(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "Malware.html")), request.Url);
					frame.LoadUrl("slbr://malware"/* + "?url=" + request.Url*/);
				else if (_ThreatType == Utils.SafeBrowsing.ThreatType.Social_Engineering)
					frame.LoadUrl("slbr://deception"/* + "?url=" + request.Url*/);
				/*else
                {
					if (bool.Parse(MainWindow.Instance.MainSave.Get("Weblight")) && !request.Url.Contains("googleweblight.com/?lite_url="))
                    {
						frame.LoadUrl(Utils.FixUrl(request.Url, true));
                    }
				}*/
				//if (request.Url.EndsWith(".pdf"))
				//	frame.LoadUrl(request.Url + "#toolbar=0");
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
				Application.Current.Dispatcher.BeginInvoke(new Action(delegate
				{
					MainWindow.Instance.CreateTab(MainWindow.Instance.CreateWebBrowser(targetUrl), false, MainWindow.Instance.Tabs.SelectedIndex + 1, true);
				}));
				return true;
			}
   //         else
			//{
			//	if (Utils.IsAboutUrl(targetUrl))
			//	{
			//		MessageBox.Show("slbr://" + targetUrl.Substring(6));
			//		frame.LoadUrl("slbr://" + targetUrl.Substring(6));
			//		//return true;
			//	}
			//}
			return false;
		}

		public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
		{
		}
		
		public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
		{
			callback.Continue(true);
			return true;
		}

		public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
		{
			browserControl.Load("slbr://renderprocesscrashed");
		}

		public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
		{
		}

		public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			var _ResourceRequestHandler = new ResourceRequestHandler();
			return _ResourceRequestHandler;
		}

		public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
		{
			return false;
		}

        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }
    }
}
