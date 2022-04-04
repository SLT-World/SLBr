using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SLBr_Lite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Args;
        public MainWindow(string Address)
        {
            Initialize(false);
            Browser.Address = Address;
        }
        public MainWindow()
        {
            Initialize();
        }
        void Initialize(bool DoInitializeCEF = true)
        {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;// Fixed i5 problem with this code
            Args = Environment.GetCommandLineArgs();
            if (DoInitializeCEF)
                InitializeCEF();
            InitializeComponent();
            TinyDownloader = new WebClient();
            Browser.LifeSpanHandler = new LifeSpanHandler();
        }

        void InitializeCEF()
        {
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            CefSharpSettings.ShutdownOnExit = true;
            Cef.EnableHighDPISupport();
            var settings = new CefSettings();

            settings.UserAgentProduct = $"SLBr/2022Lite Chrome/{Cef.ChromiumVersion}";

            settings.RemoteDebuggingPort = 8089;
            if (Args.Length > 1)
            {
                if (Args[1].StartsWith("--"))
                {
                    Cef.Initialize(settings);
                    return;
                }
            }

            //settings.CefCommandLineArgs.Add("enable-widevine");
            //settings.CefCommandLineArgs.Add("enable-widevine-cdm");

            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capture");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            settings.CefCommandLineArgs.Add("enable-speech-input");
            settings.CefCommandLineArgs.Add("enable-features", "PdfUnseasoned");
            //settings.CefCommandLineArgs.Add("enable-viewport");
            //settings.CefCommandLineArgs.Add("enable-features", "CastMediaRouteProvider,NetworkServiceInProcess");

            //settings.CefCommandLineArgs.Add("disable-extensions");
            settings.CefCommandLineArgs.Add("ignore-certificate-errors");

            settings.CefCommandLineArgs.Add("disable-direct-composition");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync");

            //settings.CefCommandLineArgs.Add("disable-gpu");
            //settings.CefCommandLineArgs.Add("disable-software-rasterizer");

            settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
            settings.CefCommandLineArgs.Add("off-screen-frame-rate", "30");

            settings.CefCommandLineArgs.Add("debug-plugin-loading");
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");
            settings.CefCommandLineArgs.Add("no-proxy-server");
            settings.CefCommandLineArgs.Add("disable-pinch");
            //settings.CefCommandLineArgs.Add("disable-features", "WebUIDarkMode");/*,TouchpadAndWheelScrollLatching,AsyncWheelEvents*/
            //settings.CefCommandLineArgs["disable-features"] += ",SameSiteByDefaultCookies";//Cross Site Request

            settings.CefCommandLineArgs.Add("renderer-process-limit", "1");
            settings.CefCommandLineArgs.Add("multi-threaded-message-loop");
            settings.CefCommandLineArgs.Add("disable-threaded-scrolling");
            settings.CefCommandLineArgs.Add("disable-smooth-scrolling");
            settings.CefCommandLineArgs.Add("disable-surfaces");
            settings.CefCommandLineArgs.Remove("process-per-tab");
            settings.CefCommandLineArgs.Add("disable-site-isolation-trials");

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files");
            //settings.CefCommandLineArgs.Add("disable-features=IsolateOrigins,process-per-tab,site-per-process,process-per-site");
            //settings.CefCommandLineArgs.Add("process-per-site");
            //settings.CefCommandLineArgs.Remove("process-per-site");
            //settings.CefCommandLineArgs.Remove("site-per-process");
            //settings.CefCommandLineArgs.Add("process-per-site-instance");

            //settings.CefCommandLineArgs.Add("disable-3d-apis", "1");
            settings.CefCommandLineArgs.Add("disable-low-res-tiling");
            //settings.CefCommandLineArgs.Add("disable-direct-write");
            //settings.CefCommandLineArgs.Add("allow-sandbox-debugging");
            //settings.CefCommandLineArgs.Add("webview-sandboxed-renderer");
            settings.CefCommandLineArgs.Add("js-flags", "max_old_space_size=524,lite_mode");
            //settings.CefCommandLineArgs.Add("no-experiments");
            //settings.CefCommandLineArgs.Add("no-vr-runtime");
            //settings.CefCommandLineArgs.Add("in-process-gpu");//The --in-process-gpu option will run the GPU process as a thread in the main browser process. These processes consume most of the CPU time and the GPU driver crash will likely crash the whole browser, so you probably don't wanna use it.

            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "chrome",
                SchemeHandlerFactory = new SchemeHandlerFactory()
            });
            Cef.Initialize(settings);
        }

        bool AddressBoxFocused;
        bool AddressBoxMouseEnter;
        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && AddressBox.Text.Trim().Length > 0)
            {
                string Url = FilterForBrowser(AddressBox.Text);
                Browser.Address = Url;
            }
        }
        private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddressBox.Text == CleanUrl(Browser.Address))
                    AddressBox.Text = Browser.Address;
            }
            catch { }
            AddressBoxFocused = true;
        }
        private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CleanUrl(AddressBox.Text) == CleanUrl(Browser.Address))
                    AddressBox.Text = CleanUrl(Browser.Address);
            }
            catch { }
            AddressBoxFocused = false;
        }
        private void AddressBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                try
                {
                    if (AddressBox.Text == CleanUrl(Browser.Address))
                        AddressBox.Text = Browser.Address;
                }
                catch { }
            }
            AddressBoxMouseEnter = true;
        }
        private void AddressBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!AddressBoxFocused)
            {
                try
                {
                    if (CleanUrl(AddressBox.Text) == CleanUrl(Browser.Address))
                        AddressBox.Text = CleanUrl(Browser.Address);
                }
                catch { }
            }
            AddressBoxMouseEnter = false;
        }

        string SearchEngineQuery = "https://google.com/search?q={0}";

        public string FilterForBrowser(string Text)
        {
            if (Text.Trim().Length > 0)
            {
                if (!Text.StartsWith("domain:") && !Text.StartsWith("search:"))
                {
                    if ((Text.Contains(":") || Text.Contains(".")) && !Text.Contains(" "))
                        Text = "domain:" + Text;
                    else
                        Text = "search:" + Text;
                }
                bool ContinueCheck = true;
                if (Text.StartsWith("domain:"))
                {
                    ContinueCheck = false;
                    string SubstringUrl = Text.Substring(7);
                    Text = FixUrl(SubstringUrl);
                }
                if (ContinueCheck && Text.StartsWith("search:"))
                    Text = FixUrl(string.Format(SearchEngineQuery, Text.Substring(7)));

                //if (Url.ToLower().Contains("cefsharp.browsersubprocess"))
                //    MessageBox.Show("cefsharp.browsersubprocess is necessary for the browser engine to function accordingly.");
            }
            return Text;
        }

        public static string CleanUrl(string Url)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            int ToRemoveIndex = Url.LastIndexOf("?");
            if (ToRemoveIndex >= 0)
                Url = Url.Substring(0, ToRemoveIndex);
            else
            {
                ToRemoveIndex = Url.LastIndexOf("#");
                if (ToRemoveIndex >= 0)
                    Url = Url.Substring(0, ToRemoveIndex);
            }
            Url = RemovePrefix(Url, "http://");
            Url = RemovePrefix(Url, "https://");
            Url = RemovePrefix(Url, "file:///");
            Url = RemovePrefix(Url, "/", false, true);
            return Url;
        }
        public static string FixUrl(string Url)
        {
            if (string.IsNullOrEmpty(Url))
                return Url;
            Url = Url.Trim();
            Url = Url.Replace(" ", "%20");
            if ((!Url.StartsWith("https://") && !Url.StartsWith("http://")) && !Url.StartsWith("chrome://"))
                Url = "https://" + Url;
            return Url;
        }
        public static string RemovePrefix(string Url, string Prefix, bool CaseSensitive = false, bool Back = false, bool ReturnCaseSensitive = true)
        {
            /*if (Url.Length >= Prefix.Length)
            {*/
            string NewUrl = CaseSensitive ? Url : Url.ToLower();
            string CaseSensitiveUrl = Url;
            string NewPrefix = CaseSensitive ? Prefix : Prefix.ToLower();
            if ((Back ? NewUrl.EndsWith(NewPrefix) : NewUrl.StartsWith(NewPrefix)))
            {
                if (ReturnCaseSensitive)
                    return (Back ? CaseSensitiveUrl.Substring(0, CaseSensitiveUrl.Length - Prefix.Length) : CaseSensitiveUrl.Substring(Prefix.Length));
                return (Back ? NewUrl.Substring(0, NewUrl.Length - Prefix.Length) : NewUrl.Substring(Prefix.Length));
            }
            //}
            return Url;
        }

        WebClient TinyDownloader;

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            if (args.Frame.IsValid && args.Frame.IsMain)
            {
                //_Browser.GetDevToolsClient().Page.SetAdBlockingEnabledAsync(true);
                /*Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    args.Frame.ExecuteJavaScriptAsync("const addCSS = s => document.head.appendChild(document.createElement('style')).innerHTML=s;" +
                    "addCSS(\'::-webkit-scrollbar-thumb:hover{background:" + (Resources["ControlFontBrush"] as SolidColorBrush).Color.ToString() + ";}\');" +
                    "addCSS(\'::-webkit-scrollbar-thumb{background:" + (Resources["BorderBrush"] as SolidColorBrush).Color.ToString() + ";}\');" +
                    "addCSS(\'::-webkit-scrollbar-track{background:" + (Resources["PrimaryBrush"] as SolidColorBrush).Color.ToString() + ";}\');");
                }));*/
                string Address = args.Url;
                int HttpStatusCode = args.HttpStatusCode;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        var bytes = TinyDownloader.DownloadData("https://www.google.com/s2/favicons?domain=" + new Uri(Address).Host);
                        var ms = new MemoryStream(bytes);

                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.StreamSource = ms;
                        bi.EndInit();

                        WebsiteFavicon.Source = bi;
                    }
                    catch
                    {
                        WebsiteFavicon.Source = null;
                    }
                    //_Browser.ExecuteScriptAsync("function C(d,o){v=d.createElement('div');o.parentNode.replaceChild(v,o);}function A(d){for(j=0;t=[\", Interaction.IIf(browser.Address.Contains(\"youtube.com\"), \"'iframe','marquee'\", \"'iframe','embed','marquee'\")), \"][j];++j){o=d.getElementsByTagName(t);for(i=o.length-1;i>=0;i--)C(d,o[i]);}g=d.images;for(k=g.length-1;k>=0;k--)if({'21x21':1,'48x48':1,'60x468':1,'88x31':1,'88x33':1,'88x62':1,'90x30':1,'90x32':1,'90x90':1,'100x30':1,'100x37':1,'100x45':1,'100x50':1,'100x70':1,'100x100':1,'100x275':1,'110x50':1,'110x55':1,'110x60':1,'110x110':1,'120x30':1,'120x60':1,'120x80':1,'120x90':1,'120x120':1,'120x163':1,'120x181':1,'120x234':1,'120x240':1,'120x300':1,'120x400':1,'120x410':1,'120x500':1,'120x600':1,'120x800':1,'125x40':1,'125x60':1,'125x65':1,'125x72':1,'125x80':1,'125x125':1,'125x170':1,'125x250':1,'125x255':1,'125x300':1,'125x350':1,'125x400':1,'125x600':1,'125x800':1,'126x110':1,'130x60':1,'130x65':1,'130x158':1,'130x200':1,'132x70':1,'140x55':1,'140x350':1,'145x145':1,'146x60':1,'150x26':1,'150x60':1,'150x90':1,'150x100':1,'150x150':1,'155x275':1,'155x470':1,'160x80':1,'160x126':1,'160x600':1,'180x30':1,'180x66':1,'180x132':1,'180x150':1,'194x165':1,'200x60':1,'220x100':1,'225x70':1,'230x30':1,'230x33':1,'230x60':1,'234x60':1,'234x68':1,'240x80':1,'240x300':1,'250x250':1,'275x60':1,'280x280':1,'300x60':1,'300x100':1,'300x250':1,'320x50':1,'320x70':1,'336x280':1,'350x300':1,'350x850':1,'360x300':1,'380x112':1,'380x250':1,'392x72':1,'400x40':1,'400x50':1,'425x600':1,'430x225':1,'440x40':1,'464x62':1,'468x16':1,'468x60':1,'468x76':1,'468x120':1,'468x248':1,'470x60':1,'480x400':1,'486x60':1,'545x90':1,'550x5':1,'600x30':1,'720x90':1,'720x300':1,'725x90':1,'728x90':1,'734x96':1,'745x90':1,'750x25':1,'750x100':1,'750x150':1,'850x120':1}[g[k].width+'x'+g[k].height])C(d,g[k]);}A(document);for(f=0;z=frames[f];++f)A(z.document)");
                }));
            }
        }

        private void Browser_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (string.IsNullOrEmpty(Browser.Address))
                    Browser.Address = "https://google.com/";
                AddressBox.Text = CleanUrl(Browser.Address);
            }));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack == true)
                Browser.Back();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoForward == true)
                Browser.Forward();
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                BackButton.IsEnabled = e.CanGoBack;
                ForwardButton.IsEnabled = e.CanGoForward;
            }));
        }
    }
}
