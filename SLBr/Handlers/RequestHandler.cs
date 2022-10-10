using CefSharp;
using CefSharp.Wpf.HwndHost;
using SLBr.Controls;
using SLBr.Pages;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace SLBr.Handlers
{
	public class RequestHandler : IRequestHandler
	{
		public bool AdBlock;
		public bool TrackerBlock;

		//string Username;
		//string Password;

		public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
		{
			bool _Handled = false;

			//Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			//{
			CredentialsDialogResult _CredentialsDialogResult = CredentialsDialog.Show($@"Sign in to {host}");
			if (_CredentialsDialogResult.Accepted == true)
            {
                callback.Continue(_CredentialsDialogResult.Username, _CredentialsDialogResult.Password);
                _Handled = true;
            }
			//CredentialsDialogResult _CredentialsDialogResult = CredentialsDialog.Show($"Sign in to {host}");
			//if (_CredentialsDialogResult.Accepted == true)
			//{
			//    callback.Continue(_CredentialsDialogResult.Username, _CredentialsDialogResult.Password);
			//    _Handled = true;
			//}
			//}));
			//MessageBoxResult dlg = MessageBox.Show("test", "e", MessageBoxButton.OK);

			//if (dlg == MessageBoxResult.OK)
			//{
			//	_Handled = true;
			//}

			return _Handled;
		}

		public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
		{
			if (bool.Parse(MainWindow.Instance.MainSave.Get("ModernWikipedia")))
			{
				if (request.Url.Contains("wik"))
				{
					string OutputUrl = Utils.ToMobileWiki(request.Url);
					if (OutputUrl != request.Url)
						frame.LoadUrl(Utils.ToMobileWiki(request.Url));
				}
			}
			if (Utils.IsHttpScheme(request.Url))
			{
				string Response = MainWindow.Instance._SafeBrowsing.Response(request.Url);
				SafeBrowsing.ThreatType _ThreatType = Utils.CheckForInternetConnection() ? MainWindow.Instance._SafeBrowsing.GetThreatType(Response) : SafeBrowsing.ThreatType.Unknown;
				if (_ThreatType == SafeBrowsing.ThreatType.Malware || _ThreatType == SafeBrowsing.ThreatType.Unwanted_Software)
					frame.LoadUrl("slbr://malware");
				else if (_ThreatType == SafeBrowsing.ThreatType.Social_Engineering)
					frame.LoadUrl("slbr://deception");
				//if (request.Url.EndsWith(".pdf"))
				//	frame.LoadUrl(request.Url + "#toolbar=0");
			}
			//MessageBox.Show(request.Url);
			else if (request.Url.StartsWith("chrome://sandbox"))
				return true;
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
					MainWindow.Instance.NewBrowserTab(targetUrl, 0, false, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
				}));
				return true;
			}
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
			try
			{
				if (Utils.CheckForInternetConnection())
					browser.Reload(true);
				else
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(delegate
					{
						ChromiumWebBrowser _ChromiumWebBrowser = (ChromiumWebBrowser)browserControl;
						_ChromiumWebBrowser.Address = $"slbr://processcrashed?s={browserControl.Address}";
					}));
				}
			}
			catch { }
		}

		public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
		{
		}

		public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			return new ResourceRequestHandler(AdBlock, TrackerBlock);
		}

		public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
		{
		}

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
		{
			return false;
		}
    }
}
