﻿using CefSharp;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SLBr.Controls;
using SLBr.Handlers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement.Core;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl, IDisposable
    {
        public BrowserTabItem Tab;

        public IWebView WebView;
        public Settings _Settings;

        public bool Private = false;
        public bool UserAgentBranding = true;

        Storyboard LoadingStoryboard;

        public Browser(string Url, BrowserTabItem _Tab = null, bool IsPrivate = false) 
        {
            InitializeComponent();
            Tab = _Tab ?? Tab.ParentWindow.GetTab(this);
            Private = IsPrivate;
            SetIcon(App.Instance.GetIcon(bool.Parse(App.Instance.GlobalSave.Get("Favicons")) ? Url : string.Empty, Private));
            Address = Url;
            SetAudioState(false);
            DownloadsPopup.ItemsSource = App.Instance.VisibleDownloads;
            FavouritesPanel.ItemsSource = App.Instance.Favourites;
            FavouriteListMenu.ItemsSource = App.Instance.Favourites;
            HistoryListMenu.Collection = App.Instance.History;
            ExtensionsMenu.ItemsSource = App.Instance.Extensions;//ObservableCollection wasn't working so turned it into a list
            SetAppearance(App.Instance.CurrentTheme);

            if (!Private)
            {
                OmniBoxFastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                OmniBoxSmartTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                OmniBoxFastTimer.Tick += OmniBoxFastTimer_Tick;
                OmniBoxSmartTimer.Tick += OmniBoxSmartTimer_Tick;
            }
            LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
            //SiteInformationIcon.Text = "\xF16A";
            //SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
            SiteInformationText.Text = "Loading";
            LoadingStoryboard?.Begin();
            TranslateComboBox.ItemsSource = App.Instance.LocaleNames;
            TranslateComboBox.SelectedValue = App.Instance.Locale.Name;
            InitializeBrowserComponent();
        }

        public void InitializeBrowserComponent()
        {
            if (WebView == null)
                CreateWebView(Address, (WebEngineType)App.Instance.GlobalSave.GetInt("WebEngine"));
            else
                BrowserLoadChanged(Address);
        }

        TextBox OmniTextBox;
        Popup OmniBoxPopup;
        Grid OmniBoxPopupDropDown;
        bool AudioPlaying = false;

        private void WebView_AudioPlayingChanged(object? sender, EventArgs e)
        {
            SetAudioState(WebView.AudioPlaying);
        }

        public void SetAudioState(bool? _AudioPlaying = false)
        {
            if (_AudioPlaying != null)
                AudioPlaying = _AudioPlaying.Value;
            if (WebIcon != null)
                SetIcon(WebIcon);
            //QRButton.Foreground = _AudioPlaying.ToBool() ? App.Instance.GreenColor : App.Instance.RedColor;
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                Tab.ProgressBarVisibility = (Muted || !AudioPlaying) ? Visibility.Visible : Visibility.Collapsed;
            else
                Tab.ProgressBarVisibility = Visibility.Collapsed;
            //MessageBox.Show(_AudioPlaying.ToString());
        }

        public void Favourites_CollectionChanged()
        {
            SetFavouritesBarVisibility();
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = App.Instance.FavouriteColor;
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
        }

        public void UpdateSLBr(object sender, RoutedEventArgs e)
        {
            InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Update Available", "A newer version of SLBr is ready for download.", "\ue895", "Download", "Dismiss");
            InfoWindow.Topmost = true;
            if (InfoWindow.ShowDialog() == true)
                App.Instance.Update();
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
            Action((Actions)int.Parse(Values[0]), sender, (Values.Length > 1) ? Values[1] : string.Empty, (Values.Length > 2) ? Values[2] : string.Empty, (Values.Length > 3) ? Values[3] : string.Empty);
        }
        public void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            V1 = V1.Replace("{CurrentUrl}", Address).Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));

            switch (_Action)
            {
                case Actions.Exit:
                    App.Instance.CloseSLBr(true);
                    break;
                case Actions.Undo:
                    Back();
                    break;
                case Actions.Redo:
                    Forward();
                    break;
                case Actions.Refresh:
                    Refresh();
                    break;
                case Actions.HardRefresh:
                    Refresh(true);
                    break;
                case Actions.ClearCacheHardRefresh:
                    Refresh(true, true);
                    break;
                case Actions.Navigate:
                    Navigate(V1);
                    break;

                case Actions.CreateTab:
                    if (V2 == "Tab")
                    {
                        BrowserTabItem _Tab = Tab.ParentWindow.GetBrowserTabWithId(int.Parse(V1));
                        Tab.ParentWindow.NewTab(_Tab.Content.Address, true, Tab.ParentWindow.Tabs.IndexOf(_Tab) + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                    }
                    else if (V2 == "Private")
                        Tab.ParentWindow.NewTab(V1, true, -1, true);
                    else
                        Tab.ParentWindow.NewTab(V1, true);
                    break;
                case Actions.CloseTab:
                    Tab.ParentWindow.CloseTab(int.Parse(V1), int.Parse(V2));
                    break;
                case Actions.NewWindow:
                    App.Instance.NewWindow();
                    break;

                case Actions.DevTools:
                    DevTools();
                    break;
                case Actions.SizeEmulator:
                    SizeEmulator();
                    break;
                case Actions.SetSideBarDock:
                    SetSideBarDock(int.Parse(V1));
                    break;
                case Actions.Favourite:
                    Favourite();
                    break;
                case Actions.OpenFileExplorer:
                    App.Instance.OpenFileExplorer(V1);
                    break;
                case Actions.OpenAsPopupBrowser:
                    OpenAsPopupBrowser(V1);
                    break;
                case Actions.SwitchUserPopup:
                    App.Instance.SwitchUserPopup();
                    break;
                case Actions.ReaderMode:
                    ToggleReaderMode();
                    break;

                case Actions.CloseSideBar:
                    ToggleSideBar(true);
                    break;
                case Actions.NewsFeed:
                    NewsFeed();
                    break;

                case Actions.Print:
                    WebView?.Print();
                    break;
                case Actions.Mute:
                    ToggleMute();
                    break;
                case Actions.Find:
                    Find(string.Empty);
                    break;

                /*case Actions.ZoomIn:
                    Zoom(1);
                    break;
                case Actions.ZoomOut:
                    Zoom(-1);
                    break;
                case Actions.ZoomReset:
                    Zoom(0);
                    break;*/
                case Actions.ToggleCompactTabs:
                    App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.TabAlignment, !App.Instance.CompactTab, App.Instance.AllowHomeButton, App.Instance.AllowTranslateButton, App.Instance.AllowReaderModeButton, App.Instance.ShowExtensionButton, App.Instance.ShowFavouritesBar, App.Instance.AllowQRButton, App.Instance.AllowWebEngineButton);
                    break;
                case Actions.InstallWebApp:
                    Dispatcher.Invoke(async () =>
                    {
                        if (App.Instance.AvailableWebAppManifests.ContainsKey(CurrentWebAppManifestUrl))
                            CurrentWebAppManifest = App.Instance.AvailableWebAppManifests.GetValueOrDefault(CurrentWebAppManifestUrl);
                        else
                        {
                            CurrentWebAppManifest = await WebAppHandler.FetchManifestAsync(Address, CurrentWebAppManifestUrl);
                            if (CurrentWebAppManifest != null)
                                App.Instance.AvailableWebAppManifests.Add(CurrentWebAppManifestUrl, CurrentWebAppManifest);
                        }
                        if (CurrentWebAppManifest != null)
                        {
                            InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", $"Install {CurrentWebAppManifest.ShortName ?? CurrentWebAppManifest.Name}", "This site can be installed as an application.", "\ueb3b", "Install", "Cancel");
                            InfoWindow.Topmost = true;
                            if (InfoWindow.ShowDialog() == true)
                            {
                                await WebAppHandler.Install(CurrentWebAppManifest);
                                /*MessageBox.Show("Name: " + CurrentWebAppManifest.Name);
                                MessageBox.Show("Display: " + CurrentWebAppManifest.Display);
                                MessageBox.Show("StartUrl: " + CurrentWebAppManifest.StartUrl);
                                foreach (ManifestIcon _ManifestIcon in CurrentWebAppManifest.Icons)
                                    MessageBox.Show("Icon: " + _ManifestIcon.Source + " | " + _ManifestIcon.Type + " | " + _ManifestIcon.Sizes);*/
                            }
                        }
                    });
                    break;
                case Actions.QR:
                    if (V1 == "0")
                    {
                        if (QRBitmap == null)
                        {
                            QRBitmap = new QRSaveBitmapImage(App.MiniQREncoder.Encode(Address))
                            {
                                ModuleSize = 5,
                                QuietZone = 10
                            }.CreateQRCodeBitmap();
                        }
                        QRImage.Source = QRBitmap;
                        QRButton.OpenPopup();
                    }
                    else if (V1 == "1")
                    {
                        if (QRBitmap != null)
                            Clipboard.SetImage(QRBitmap);
                    }
                    else
                    {
                        if (QRBitmap != null)
                        {
                            try
                            {
                                string ScreenshotPath = App.Instance.GlobalSave.Get("ScreenshotPath");
                                if (!Directory.Exists(ScreenshotPath))
                                    Directory.CreateDirectory(ScreenshotPath);
                                string Url = Path.Combine(ScreenshotPath, Utils.SanitizeFileName(Address) + ".png");
                                Utils.SaveImage(QRBitmap, Url);
                                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
                            }
                            catch { }
                        }
                    }
                    break;
                case Actions.SwitchWebEngine:
                    switch (V1)
                    {
                        case "0":
                            if (WebView?.Engine == WebEngineType.Chromium)
                            {
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Already Using Chromium web engine", "This tab is already running with the Chromium web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.Chromium);
                            break;
                        case "1":
                            if (WebView?.Engine == WebEngineType.ChromiumEdge)
                            {
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Already Using Edge web engine", "This tab is already running with the Edge web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            string? AvailableVersion = null;
                            try
                            {
                                AvailableVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                            }
                            catch (WebView2RuntimeNotFoundException)
                            {
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "WebView2 Runtime Unavailable", "Microsoft Edge WebView2 Runtime is not installed on your device.", "\ue7f9", "Download", "Cancel");
                                InfoWindow.Topmost = true;
                                if (InfoWindow.ShowDialog() == true)
                                    Tab.ParentWindow.NewTab("https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/", true, Tab.ParentWindow.TabsUI.SelectedIndex + 1);
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.ChromiumEdge);
                            break;
                        case "2":
                            if (WebView?.Engine == WebEngineType.Trident)
                            {
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Already Using Trident web engine", "This tab is already running with the Trident web engine. No changes are necessary.");
                                InfoWindow.ShowDialog();
                                break;
                            }
                            DisposeBrowserCore();
                            CreateWebView(Address, WebEngineType.Trident);
                            break;
                        case "-1":
                            WebEngineType Engine = (WebEngineType)App.Instance.GlobalSave.GetInt("WebEngine");
                            if (WebView?.Engine == Engine)
                                break;
                            DisposeBrowserCore();
                            CreateWebView(Address, Engine);
                            break;
                    }
                    break;
                case Actions.Translate:
                    Translate(V1 == "1");
                    break;
            }
        }
        public WriteableBitmap? QRBitmap = null;

        void CreateWebView(string Url, WebEngineType Engine)
        {
            if (WebView != null)
                return;
            Address = Url;
            Tab.IsUnloaded = true;
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.ProgressBarVisibility = Visibility.Collapsed;
            switch (Engine)
            {
                case WebEngineType.Chromium:
                    WebEngineButtonIcon.Text = "\x2600";
                    break;
                case WebEngineType.ChromiumEdge:
                    WebEngineButtonIcon.Text = "\x2601";
                    break;
                case WebEngineType.Trident:
                    WebEngineButtonIcon.Text = "\x2602";
                    break;
            }

            WebViewBrowserSettings WebViewSettings = new WebViewBrowserSettings()
            {
                Private = Private,
                AudioListener = !App.Instance.LiteMode
            };

            WebView = WebViewManager.Create(Engine, Url, WebViewSettings);

            WebView?.Control.AllowDrop = true;
            WebView?.Control.IsManipulationEnabled = true;
            WebView?.Control.UseLayoutRounding = true;

            WebView?.IsBrowserInitializedChanged += WebView_IsBrowserInitializedChanged;
            //WebView?.Control.PreviewMouseWheel += Chromium_PreviewMouseWheel;

            WebView?.FaviconChanged += WebView_FaviconChanged;
            WebView?.AuthenticationRequested += WebView_AuthenticationRequested;
            WebView?.BeforeNavigation += WebView_BeforeNavigation;
            WebView?.ContextMenuRequested += WebView_ContextMenuRequested;
            WebView?.ExternalProtocolRequested += WebView_ExternalProtocolRequested;
            //WebView?.FindResult += WebView_FindResult;
            WebView?.FrameLoadStart += WebView_FrameLoadStart;
            WebView?.FullscreenChanged += WebView_FullscreenChanged;
            WebView?.JavaScriptMessageReceived += WebView_JavaScriptMessageReceived;
            WebView?.LoadingStateChanged += WebView_LoadingStateChanged;
            WebView?.NavigationError += WebView_NavigationError;
            WebView?.NewTabRequested += WebView_NewTabRequested;
            WebView?.PermissionRequested += WebView_PermissionRequested;
            WebView?.ResourceLoaded += WebView_ResourceLoaded;
            WebView?.ResourceRequested += WebView_ResourceRequested;
            //WebView?.ResourceResponded += WebView_ResourceResponded;
            WebView?.ScriptDialogOpened += WebView_ScriptDialogOpened;
            WebView?.StatusMessage += WebView_StatusMessage;
            WebView?.TitleChanged += WebView_TitleChanged;
            WebView?.AudioPlayingChanged += WebView_AudioPlayingChanged;

            CoreContainer.Visibility = Visibility.Collapsed;
            CoreContainer.Children.Add(WebView?.Control);
            //Chromium.Visibility = Visibility.Collapsed;//VIDEO

            /*Tab.ParentWindow.WindowState = WindowState.Normal;//VIDEO
            Tab.ParentWindow.WindowStyle = WindowStyle.None;//VIDEO
            Tab.ParentWindow.WindowState = WindowState.Maximized;//VIDEO*/
        }

        private async void WebView_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            if (WebView.IsBrowserInitialized)
            {
                CoreContainer.Visibility = Visibility.Visible;
                Tab.IsUnloaded = false;
                Tab.BrowserCommandsVisibility = Visibility.Visible;
                if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                    Tab.ProgressBarVisibility = Visibility.Visible;
                if (bool.Parse(App.Instance.GlobalSave.Get("NetworkLimit")))
                {
                    float Bandwidth = float.Parse(App.Instance.GlobalSave.Get("Bandwidth"));
                    LimitNetwork(0, Bandwidth, Bandwidth);
                }
                await ToggleEfficientAdBlock(App.Instance.AdBlock == 2);
                UserAgentBranding = !Private;
                if (UserAgentBranding)
                {
                    await WebView?.CallDevToolsAsync("Emulation.setUserAgentOverride", new
                    {
                        userAgent = App.Instance.UserAgent,
                        userAgentMetadata = App.Instance.UserAgentData
                    });
                    await WebView?.CallDevToolsAsync("Network.setUserAgentOverride", new
                    {
                        userAgent = App.Instance.UserAgent,
                        userAgentMetadata = App.Instance.UserAgentData
                    });
                }
                if (App.Instance.LiteMode)
                {
                    await WebView?.CallDevToolsAsync("Emulation.setDataSaverOverride", new
                    {
                        dataSaverEnabled = true
                    });
                }
            }
        }

        private void WebView_TitleChanged(object? sender, string e)
        {
            Tab.Header = e;
            if (Tab == Tab.ParentWindow.Tabs[Tab.ParentWindow.TabsUI.SelectedIndex])
                Title = e + " - SLBr";
        }

        private void WebView_StatusMessage(object? sender, string e)
        {
            if (string.IsNullOrEmpty(e))
                StatusBarPopup.IsOpen = false;
            else
            {
                StatusMessage.Text = e;
                StatusBarPopup.IsOpen = true;
            }
        }

        private void WebView_ScriptDialogOpened(object? sender, ScriptDialogEventArgs e)
        {
            if (e.DialogType == ScriptDialogType.Alert)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Alert", $"{Utils.Host(e.Url)}", e.Text);
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
            else if (e.DialogType == ScriptDialogType.Confirm)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Confirmation", $"{Utils.Host(e.Url)}", e.Text, string.Empty, "OK", "Cancel");
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
            else if (e.DialogType == ScriptDialogType.Prompt)
            {
                PromptDialogWindow InfoWindow = new PromptDialogWindow("Prompt", $"{Utils.Host(e.Url)}", e.Text, e.DefaultPrompt);
                InfoWindow.Topmost = true;
                e.Handled = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    e.PromptResult = InfoWindow.UserInput;
                    e.Result = true;
                }
                else
                    e.Result = false;
            }
            else if (e.DialogType == ScriptDialogType.BeforeUnload)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Warning", e.IsReload ? "Reload site?" : "Leave site?", "You may lose unsaved changes. Do you want to continue?", string.Empty, e.IsReload ? "Reload" : "Leave", "Cancel");
                InfoWindow.Topmost = true;
                e.Handled = true;
                e.Result = InfoWindow.ShowDialog() == true;
            }
        }

        /*private void WebView_ResourceResponded(object? sender, ResourceRespondedResult e)
        {
            if (App.Instance.AMP && e.ResourceRequestType == ResourceRequestType.MainFrame)
            {
                string Url = e.Url;
                WebView.GetSourceAsync().ContinueWith(TaskHtml =>
                {
                    string? AMPUrl = Utils.ParseAMPLink(TaskHtml.Result, Url);
                    if (!string.IsNullOrEmpty(AMPUrl))
                    {
                        Stop();
                        Navigate(AMPUrl);
                    }
                });
            }
        }*/

        public ConcurrentDictionary<string, bool> HostCache = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        public long Image_Budget = 2 * 1024 * 1024;
        public long Stylesheet_Budget = 400 * 1024;
        public long Script_Budget = 500 * 1024;
        public long Font_Budget = 300 * 1024;
        public long Frame_Budget = 5;

        public void ResetBudgets()
        {
            Image_Budget = 2 * 1024 * 1024;
            Stylesheet_Budget = 400 * 1024;
            Script_Budget = 500 * 1024;
            Font_Budget = 300 * 1024;
            Frame_Budget = 5;
        }

        public bool IsOverBudget(ResourceRequestType _ResourceType)
        {
            switch (_ResourceType)
            {
                case ResourceRequestType.Image:
                    return Image_Budget <= 0;
                case ResourceRequestType.Stylesheet:
                    return Stylesheet_Budget <= 0;
                case ResourceRequestType.Script:
                    return Script_Budget <= 0;
                case ResourceRequestType.Font:
                    return Font_Budget <= 0;
                case ResourceRequestType.SubFrame:
                    return Frame_Budget <= 0;
                default:
                    return false;
            }
        }

        public void DeductFromBudget(ResourceRequestType _ResourceType, long DataLength)
        {
            switch (_ResourceType)
            {
                case ResourceRequestType.Image:
                    Image_Budget -= DataLength;
                    return;
                case ResourceRequestType.Stylesheet:
                    Stylesheet_Budget -= DataLength;
                    return;
                case ResourceRequestType.Script:
                    Script_Budget -= DataLength;
                    return;
                case ResourceRequestType.Font:
                    Font_Budget -= DataLength;
                    return;
                case ResourceRequestType.SubFrame:
                    Frame_Budget -= DataLength;
                    return;
                default:
                    break;
            }
        }

        private void WebView_ResourceRequested(object? sender, ResourceRequestEventArgs e)
        {
            //MessageBox.Show(e.Url);
            if (!Utils.IsHttpScheme(e.Url))
            {
                e.Cancel = false;
                return;
            }
            if (!App.Instance.ExternalFonts && e.ResourceRequestType == ResourceRequestType.Font)
            {
                e.Cancel = true;
                return;
            }
            if (App.Instance.NeverSlowMode)
            {
                if (IsOverBudget(e.ResourceRequestType))
                {
                    e.Cancel = true;
                    return;
                }
                foreach (string Pattern in App.FailedScripts)
                {
                    if (e.Url.Contains(Pattern, StringComparison.Ordinal))
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            if (App.Instance.AdBlock == 1)
            {
                if (string.IsNullOrEmpty(e.FocusedUrl))
                {
                    string Host = Utils.FastHost(e.FocusedUrl);
                    if (App.Instance.AdBlockAllowList.Has(Host))
                    {
                        e.Cancel = false;
                        return;
                    }
                }
                if (e.ResourceRequestType == ResourceRequestType.Ping)
                {
                    Interlocked.Increment(ref App.Instance.TrackersBlocked);
                    e.Cancel = true;
                    return;
                }
                else if (Utils.IsPossiblyAd(e.ResourceRequestType))
                {
                    string Host = Utils.FastHost(e.Url);
                    bool Cached = HostCache.TryGetValue(Host, out bool Blocked);
                    if (Blocked)
                    {
                        e.Cancel = true;
                        return;
                    }
                    if (!Cached)
                    {
                        if (App.Ads.Has(Host))
                        {
                            Interlocked.Increment(ref App.Instance.AdsBlocked);
                            HostCache[Host] = true;
                            e.Cancel = true;
                            return;
                        }
                        else if (App.Analytics.Has(Host))
                        {
                            Interlocked.Increment(ref App.Instance.TrackersBlocked);
                            HostCache[Host] = true;
                            e.Cancel = true;
                            return;
                        }
                        HostCache[Host] = false;
                    }
                    if (e.ResourceRequestType == ResourceRequestType.Script)
                    {
                        if (App.HasInLinkRegex.IsMatch(e.Url))
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            if (App.Instance.LiteMode)
                e.ModifiedHeaders.Add("Save-Data", "on");
            if (UserAgentBranding)
            {
                e.ModifiedHeaders.Add("User-Agent", App.Instance.UserAgent);
                e.ModifiedHeaders.Add("sec-ch-ua", App.Instance.UserAgentBrandsString);
            }
            if (bool.Parse(App.Instance.GlobalSave.Get("WarnCodec")) && WebView.Engine == WebEngineType.Chromium && e.ResourceRequestType == ResourceRequestType.Media && Utils.IsProprietaryCodec(Utils.GetFileExtension(e.Url)))
            {
                Dispatcher.BeginInvoke(() =>
                {
                    InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Proprietary Codecs Detected", "This site is trying to play media using formats not supported by Chromium (CEF).\nDo you want to switch to the Edge (WebView2) engine?", "\xe8ab", "Yes", "No");
                    InfoWindow.Topmost = true;
                    if (InfoWindow.ShowDialog() == true)
                        Action(Actions.SwitchWebEngine, null, "1");
                });
            }
        }

        private void WebView_ResourceLoaded(object? sender, ResourceLoadedResult e)
        {
            if (App.Instance.NeverSlowMode)
            {
                if (e.Success)
                    DeductFromBudget(e.ResourceRequestType, e.ReceivedContentLength);
                else// if (status == UrlRequestStatus.Failed)
                {
                    if (e.ResourceRequestType != ResourceRequestType.Script)
                        return;
                    App.FailedScripts.Add(Utils.CleanUrl(e.Url, true, true, true, true, true));
                }
            }
        }

        private void WebView_BeforeNavigation(object? sender, BeforeNavigationEventArgs e)
        {
            if (e.IsMainFrame)
            {
                if (WebView.Engine == WebEngineType.Chromium && !WebViewManager.OverrideRequests.ContainsKey(e.Url))
                {
                    if (Utils.IsHttpScheme(e.Url))
                    {
                        if (!Private && App.Instance.WebRiskService != WebRiskHandler.SecurityService.None && Utils.GetFileExtension(e.Url) != ".pdf")
                        {
                            WebRiskHandler.ThreatType _ThreatType = App.Instance._WebRiskHandler.IsSafe(e.Url, App.Instance.WebRiskService);
                            if (_ThreatType == WebRiskHandler.ThreatType.Malware || _ThreatType == WebRiskHandler.ThreatType.Unwanted_Software)
                                WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Malware_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                            else if (_ThreatType == WebRiskHandler.ThreatType.Social_Engineering)
                                WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Deception_Error, Encoding.UTF8), "text/html", -1, _ThreatType.ToString());
                        }
                    }
                    else if (e.Url.StartsWith("chrome:", StringComparison.Ordinal))
                    {
                        bool Block = false;
                        //https://source.chromium.org/chromium/chromium/src/+/main:ios/chrome/browser/shared/model/url/chrome_url_constants.cc
                        switch (e.Url.Substring(9))
                        {
                            case string s when s.StartsWith("settings", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("history", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("downloads", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("flags", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("new-tab-page", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("bookmarks", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("apps", StringComparison.Ordinal):
                                Block = true;
                                break;

                            case string s when s.StartsWith("dino", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("management", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("new-tab-page-third-party", StringComparison.Ordinal):
                                Block = true;
                                break;

                            case string s when s.StartsWith("favicon", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("sandbox", StringComparison.Ordinal):
                                Block = true;
                                break;

                            case string s when s.StartsWith("bookmarks-side-panel.top-chrome", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("customize-chrome-side-panel.top-chrome", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("read-later.top-chrome", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("tab-search.top-chrome", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("tab-strip.top-chrome", StringComparison.Ordinal):
                                Block = true;
                                break;

                            case string s when s.StartsWith("support-tool", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("privacy-sandbox-dialog", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("chrome-signin", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("browser-switch", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("profile-picker", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("intro", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("sync-confirmation", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("app-settings", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("managed-user-profile-notice", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("reset-password", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("connection-help", StringComparison.Ordinal):
                                Block = true;
                                break;
                            case string s when s.StartsWith("connection-monitoring-detected", StringComparison.Ordinal):
                                Block = true;
                                break;
                        }
                        if (Block)
                            WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Instance.GenerateCannotConnect(e.Url, -300, "ERR_INVALID_URL"), Encoding.UTF8), "text/html", -1, string.Empty);
                    }
                }

                if (!Private && App.Instance.AMP && Utils.IsHttpScheme(e.Url) && WebView.Engine != WebEngineType.Trident && !WebViewManager.OverrideRequests.ContainsKey(e.Url))
                {
                    string? AMPUrl = Utils.GetAMPUrl(e.Url);
                    if (AMPUrl != null && AMPUrl != e.Url)
                    {
                        WebView.Stop();
                        Address = AMPUrl;
                        return;
                    }
                }
                //WARNING: Do not remove BeginInvoke, otherwise it will somehow freeze CefSharp
                Dispatcher.BeginInvoke(async () =>
                {
                    if (App.Instance.NeverSlowMode)
                        ResetBudgets();
                    HostCache.Clear();
                    QRBitmap = null;
                    SetIcon(await App.Instance.SetIcon(string.Empty, e.Url, Private));
                });
            }
        }

        BitmapImage? WebIcon;

        void SetIcon(BitmapImage Image, bool Force = false)
        {
            WebIcon = Image;
            Tab.Icon = Force ? Image : ((Muted || !AudioPlaying) ? Image : App.Instance.AudioIcon);
        }

        private void WebView_PermissionRequested(object? sender, PermissionRequestedEventArgs e)
        {
            string Permissions = string.Empty;
            string PermissionIcons = string.Empty;
            foreach (WebPermissionKind Option in Enum.GetValues(typeof(WebPermissionKind)))
            {
                if (e.Kind.HasFlag(Option) && Option != WebPermissionKind.None)
                {
                    switch (Option)
                    {
                        /*case ProperPermissionRequestType.AccessibilityEvents:
                            Permissions += "Respond to Accessibility Events";
                            break;*/
                        case WebPermissionKind.ArSession:
                            Permissions += "Use your camera to create a 3D map of your surroundings";
                            PermissionIcons += "\xE809";
                            break;
                        case WebPermissionKind.CameraPanTiltZoom:
                            Permissions += "Move your camera";
                            PermissionIcons += "\xE714";
                            break;
                        case WebPermissionKind.CameraStream:
                            Permissions += "Use your camera";
                            PermissionIcons += "\xE714";
                            break;
                        case WebPermissionKind.CapturedSurfaceControl:
                            Permissions += "Scroll and zoom the contents of your shared tab";
                            PermissionIcons += "\xec6c";
                            break;
                        case WebPermissionKind.Clipboard:
                            Permissions += "See text and images in clipboard";
                            PermissionIcons += "\xF0E3";
                            break;
                        case WebPermissionKind.TopLevelStorageAccess:
                            Permissions += "Access cookies and site Data";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.DiskQuota:
                            Permissions += "Store files on this device";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.LocalFonts:
                            Permissions += "Use your computer fonts";
                            PermissionIcons += "\xE8D2";
                            break;
                        case WebPermissionKind.Geolocation:
                            Permissions += "Know your location";
                            PermissionIcons += "\xECAF";
                            break;
                        case WebPermissionKind.IdentityProvider:
                            Permissions += "Use your accounts to login to websites";
                            PermissionIcons += "\xef58";
                            break;
                        case WebPermissionKind.IdleDetection:
                            Permissions += "Know when you're actively using this device";
                            PermissionIcons += "\xEA6C";
                            break;
                        case WebPermissionKind.MicStream:
                            Permissions += "Use your microphone";
                            PermissionIcons += "\xE720";
                            break;
                        case WebPermissionKind.MidiSysex:
                            Permissions += "Use your MIDI devices";
                            PermissionIcons += "\xEC4F";
                            break;
                        case WebPermissionKind.MultipleDownloads:
                            Permissions += "Download multiple files";
                            PermissionIcons += "\xE896";
                            break;
                        case WebPermissionKind.Notifications:
                            Permissions += "Show notifications";
                            PermissionIcons += "\xEA8F";
                            break;
                        case WebPermissionKind.KeyboardLock:
                            Permissions += "Lock and use your keyboard";
                            PermissionIcons += "\xf26b";
                            break;
                        case WebPermissionKind.PointerLock:
                            Permissions += "Lock and use your mouse";
                            PermissionIcons += "\xf271";
                            break;
                        case WebPermissionKind.ProtectedMediaIdentifier:
                            Permissions += "Know your unique device identifier";
                            PermissionIcons += "\xef3f";
                            break;
                        case WebPermissionKind.RegisterProtocolHandler:
                            Permissions += "Open web links";
                            PermissionIcons += "\xE71B";
                            break;
                        case WebPermissionKind.StorageAccess:
                            Permissions += "Access cookies and site Data";
                            PermissionIcons += "\xE8B7";
                            break;
                        case WebPermissionKind.VrSession:
                            Permissions += "Use your virtual reality devices";
                            PermissionIcons += "\xEC94";
                            break;
                        case WebPermissionKind.WindowManagement:
                            Permissions += "Manage windows on all your displays";
                            PermissionIcons += "\xE737";
                            break;
                        case WebPermissionKind.FileSystemAccess:
                            Permissions += "Access file system";
                            PermissionIcons += "\xEC50";//E8B7
                            break;
                        case WebPermissionKind.ScreenShare:
                            Permissions += "Share your screen\n";
                            PermissionIcons += "\xE7F4\n";
                            break;
                        case WebPermissionKind.RecordAudio:
                            Permissions += "Capture desktop audio\n";
                            PermissionIcons += "\xE7F3\n";
                            break;
                    }
                    Permissions += "\n";
                    PermissionIcons += "\n";
                }
            }

            Permissions = Permissions.TrimEnd('\n');
            PermissionIcons = PermissionIcons.TrimEnd('\n');
            if (string.IsNullOrEmpty(Permissions))
                Permissions = e.Kind.ToString();

            var InfoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(e.Url)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
            InfoWindow.Topmost = true;

            bool? Result = InfoWindow.ShowDialog();
            if (Result == true)
                e.State = WebPermissionState.Allow;
            else
                e.State = WebPermissionState.Deny;
        }

        private void WebView_NewTabRequested(object? sender, NewTabRequestEventArgs e)
        {
            if (e.Popup.HasValue)
            {
                //TODO: Add popup permission check
                int Width = (int)e.Popup.Value.Width;
                if (Width == 0)
                    Width = 600;
                int Height = (int)e.Popup.Value.Height;
                if (Height == 0)
                    Height = 650;
                PopupBrowser Popup = new PopupBrowser(e.Url, Width, Height);
                Popup.Show();
                if (e.Popup.Value.Left != 0)
                    Popup.Left = e.Popup.Value.Left;
                if (e.Popup.Value.Top != 0)
                    Popup.Top = e.Popup.Value.Top;
                e.WebView = Popup.WebView;
            }
            else
                e.WebView = Tab.ParentWindow.NewTab(e.Url, !e.Background, Tab.ParentWindow.TabsUI.SelectedIndex + 1, Private ? Private : bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
        }

        private void WebView_NavigationError(object? sender, NavigationErrorEventArgs e)
        {
            if (WebView.Engine == WebEngineType.ChromiumEdge && e.ErrorText == "Unknown") //For Edge's SmartScreen error page
                return;
            if (WebView.Engine == WebEngineType.Trident && e.ErrorCode == -2146697203) //Custom protocols in IE
                return;
            WebViewManager.RegisterOverrideRequest(e.Url, ResourceHandler.GetByteArray(App.Instance.GenerateCannotConnect(e.Url, e.ErrorCode, e.ErrorText), Encoding.UTF8), "text/html", 1);
            Navigate(e.Url);
        }

        private async void WebView_LoadingStateChanged(object? sender, bool e)
        {
            if (WebView == null || !WebView.IsBrowserInitialized)
                return;
            if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                WebView?.ExecuteScript(Scripts.InternalScript);
            BackButton.IsEnabled = CanGoBack;
            ForwardButton.IsEnabled = CanGoForward;
            ReloadButton.Content = IsLoading ? "\xF78A" : "\xE72C";
            await WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
            {
                enabled = App.Instance.CurrentTheme.DarkWebPage
            });

            CurrentWebAppManifest = null;
            CurrentWebAppManifestUrl = string.Empty;
            InstallWebAppButton.Visibility = Visibility.Collapsed;
            BrowserLoadChanged(Address, IsLoading);
            if (!IsLoading)
            {
                if (!Private)
                    App.Instance.AddHistory(Address, Title);
                if (!App.Instance.LiteMode && bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll")))
                    WebView.ExecuteScript(Scripts.ScrollScript);
                if (!Address.StartsWith("slbr:", StringComparison.Ordinal))
                {
                    if (WebView.CanExecuteJavascript)
                    {
                        if (Utils.IsHttpScheme(Address))
                        {
                            if (App.Instance.SkipAds)
                            {
                                /*if (Address.AsSpan().IndexOf("youtube.com", StringComparison.Ordinal) >= 0)
                                {
                                    Chromium.ExecuteScriptAsync(Scripts.YouTubeHideAdScript);
                                    if (Address.AsSpan().IndexOf("/watch?v=", StringComparison.Ordinal) >= 0)
                                        Chromium.ExecuteScriptAsync(Scripts.YouTubeSkipAdScript);
                                }*/
                                if (Address.AsSpan().IndexOf("youtube.com/watch?v=", StringComparison.Ordinal) >= 0)
                                    WebView?.ExecuteScript(Scripts.YouTubeSkipAdScript);
                            }
                            if (Address.AsSpan().IndexOf("chromewebstore.google.com/detail", StringComparison.Ordinal) >= 0)
                                WebView?.ExecuteScript(Scripts.WebStoreScript);
                            if (bool.Parse(App.Instance.GlobalSave.Get("WebNotifications")))
                                WebView?.ExecuteScript(Scripts.NotificationPolyfill);
                            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("OpenSearch")))
                            {
                                string SiteHost = Utils.FastHost(Address);
                                if (App.Instance.SearchEngines.Find(i => i.Host == SiteHost) == null)
                                    WebView?.ExecuteScript(Scripts.OpenSearchScript);
                            }

                            if (bool.Parse(App.Instance.GlobalSave.Get("WebApps")))
                            {
                                var (Installable, ManifestUrl) = await IsInstallableAsync();
                                if (Installable)
                                {
                                    InstallWebAppButton.Visibility = Visibility.Visible;
                                    CurrentWebAppManifestUrl = ManifestUrl;
                                }
                            }
                        }
                        else if (Address.StartsWith("file:///", StringComparison.Ordinal))
                            WebView?.ExecuteScript(Scripts.FileScript);
                    }
                    if (bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme")))
                    {
                        try
                        {
                            string CustomThemeColor = string.Empty;
                            string? Task = await WebView?.EvaluateScriptAsync("document.querySelector('meta[name=\"theme-color\"]')?.content");
                            if (Task != null)
                                CustomThemeColor = Task;
                            if (!string.IsNullOrEmpty(CustomThemeColor))
                            {
                                IsCustomTheme = true;
                                Color PrimaryColor = Utils.ParseThemeColor(CustomThemeColor);

                                Theme SiteTheme = App.Instance.GenerateTheme(PrimaryColor);
                                SetAppearance(SiteTheme);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(SiteTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(SiteTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(SiteTheme.BorderColor);
                            }
                            else if (IsCustomTheme)
                            {
                                IsCustomTheme = false;
                                SetAppearance(App.Instance.CurrentTheme);
                                TabItem _TabItem = Tab.ParentWindow.TabsUI.ItemContainerGenerator.ContainerFromItem(Tab) as TabItem;
                                _TabItem.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.FontColor);
                                _TabItem.Background = new SolidColorBrush(App.Instance.CurrentTheme.PrimaryColor);
                                _TabItem.BorderBrush = new SolidColorBrush(App.Instance.CurrentTheme.BorderColor);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private async void HandleInternalMessage(IDictionary<string, object> Message)
        {
            if (!Message.ContainsKey("function"))
                return;
            switch (Message["function"]?.ToString())
            {
                case "Downloads":
                    WebView?.ExecuteScript($"internal.receive(\"downloads={JsonSerializer.Serialize(App.Instance.Downloads).Replace("\\", "\\\\").Replace("\"", "\\\"")}\")");
                    break;

                case "History":
                    WebView?.ExecuteScript($"internal.receive(\"history={JsonSerializer.Serialize(App.Instance.History).Replace("\\", "\\\\").Replace("\"", "\\\"")}\")");
                    break;

                case "background":
                    string Url = string.Empty;
                    switch (App.Instance.GlobalSave.GetInt("HomepageBackground"))
                    {
                        case 0:
                            Url = App.Instance.GlobalSave.Get("CustomBackgroundImage");
                            if (!Utils.IsHttpScheme(Url) && File.Exists(Url))
                                Url = $"Data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(Url))}";
                            break;
                        case 1:
                            int BingBackground = App.Instance.GlobalSave.GetInt("BingBackground");
                            if (BingBackground == 0)
                            {
                                try
                                {
                                    XmlDocument Doc = new XmlDocument();
                                    Doc.LoadXml(new WebClient().DownloadString("http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=1&mbl=1&mkt=en-US"));
                                    Url = "http://www.bing.com/" + Doc.SelectSingleNode("/images/image/url").InnerText;
                                }
                                catch { }
                            }
                            else
                                Url = "http://bingw.jasonzeng.dev/?index=random";
                            break;
                        case 2:
                            Url = "http://picsum.photos/1920/1080?random";
                            break;
                        case 3:
                            using (var Client = new HttpClient())
                            {
                                string Json = await Client.GetStringAsync("https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY");

                                using var Document = JsonDocument.Parse(Json);
                                var Root = Document.RootElement;
                                if (!App.Instance.LiteMode && Root.TryGetProperty("hdurl", out var HDUrl))
                                    Url = HDUrl.GetString() ?? string.Empty;
                                else if (Root.TryGetProperty("url", out var _Url))
                                    Url = _Url.GetString() ?? string.Empty;
                            }
                            break;
                    }
                    if (!string.IsNullOrEmpty(Url))
                        WebView?.ExecuteScript($"document.documentElement.style.backgroundImage = \"url('{Url}')\";");
                    break;

                case "OpenDownload":
                    WebDownloadItem? Item = App.Instance.Downloads.GetValueOrDefault((string)Message["variable"]);
                    if (Item != null)
                        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{Item.FullPath}\"") { UseShellExecute = true });
                    break;

                case "CancelDownload":
                    App.Instance.Downloads.GetValueOrDefault((string)Message["variable"])?.Cancel();
                    break;

                case "ClearHistory":
                    Dispatcher.BeginInvoke(App.Instance.History.Clear);
                    break;

                case "Search":
                    Address = Utils.FilterUrlForBrowser(Message["variable"]?.ToString() ?? string.Empty, App.Instance.DefaultSearchProvider.SearchUrl);
                    break;
            }
        }

        private void WebView_JavaScriptMessageReceived(object? sender, string e)
        {
            if (string.IsNullOrWhiteSpace(e))
                return;
            //MessageBox.Show(e);
            IDictionary<string, object>? Message;
            try
            {
                Message = JsonSerializer.Deserialize<Dictionary<string, object>>(e);
            }
            catch { return; }
            if (Message == null || !Message.ContainsKey("type"))
                return;

            switch (Message["type"].ToString())
            {
                case "OpenSearch":
                    App.Instance.SaveOpenSearch(Message["name"]?.ToString()!, Message["url"]?.ToString()!);
                    break;
                case "Internal":
                    if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                        HandleInternalMessage(Message);
                    break;
                case "Notification":
                    var DataJson = Message["Data"]?.ToString();
                    if (string.IsNullOrWhiteSpace(DataJson))
                        return;
                    var Data = JsonSerializer.Deserialize<List<object>>(DataJson);
                    if (Data != null && Data.Count == 2)
                    {
                        var ToastXML = new Windows.Data.Xml.Dom.XmlDocument();
                        ToastXML.LoadXml(@$"<toast>
    <visual>
        <binding template=""ToastText04"">
            <text id=""1"">{Data[0].ToString()}</text>
            <text id=""2"">{((IDictionary<string, object>)JsonSerializer.Deserialize<ExpandoObject>(((JsonElement)Data[1]).GetRawText()))["body"].ToString()}</text>
            <text id=""3"">{Utils.Host(Address, false)}</text>
        </binding>
    </visual>
</toast>");
                        ToastNotificationManager.CreateToastNotifier("SLBr").Show(new ToastNotification(ToastXML));
                    }
                    break;
            }
        }

        private void WebView_FullscreenChanged(object? sender, bool e)
        {
            Tab.ParentWindow.Fullscreen(e, this);
        }

        private void WebView_FrameLoadStart(object? sender, string e)
        {
            if (Utils.IsHttpScheme(e))
            {
                WebView?.ExecuteScript(Scripts.AntiCloseScript);//Replacement for DoClose of LifeSpanHandler in RuntimeStyle Chrome
                WebView?.ExecuteScript(Scripts.ShiftContextMenuScript);
                if (bool.Parse(App.Instance.GlobalSave.Get("AntiTamper")))
                {
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiFullscreen")))
                        WebView?.ExecuteScript(Scripts.AntiFullscreenScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect")))
                        WebView?.ExecuteScript(Scripts.LateAntiDevtoolsScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu")))
                        WebView?.ExecuteScript(Scripts.ForceContextMenuScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("TextSelection")))
                        WebView?.ExecuteScript(Scripts.AllowInteractionScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter")))
                        WebView?.ExecuteScript(Scripts.RemoveFilterCSS);
                    if (bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay")))
                        WebView?.ExecuteScript(Scripts.RemoveOverlayCSS);
                }
                if (bool.Parse(App.Instance.GlobalSave.Get("ForceLazy")))
                    WebView?.ExecuteScript(Scripts.ForceLazyLoad);
            }
        }

        /*private void WebView_FindResult(object? sender, FindResult e)
        {
        }*/

        private void WebView_ExternalProtocolRequested(object? sender, ExternalProtocolEventArgs e)
        {
            string ProtocolName = e.Url;
            if (e.Url.StartsWith("ms-settings"))
                ProtocolName = "Settings";
            else if (e.Url.StartsWith("ms-photos"))
                ProtocolName = "Photos";
            InformationDialogWindow InfoWindow = new InformationDialogWindow("Warning", $"Open {ProtocolName}", "A website is requesting to open this application.", string.Empty, "Open", "Cancel");
            InfoWindow.Topmost = true;
            e.Launch = InfoWindow.ShowDialog() == true;
        }

        private void WebView_ContextMenuRequested(object? sender, WebContextMenuEventArgs e)
        {
            bool IsPageMenu = true;
            ContextMenu BrowserMenu = new ContextMenu();
            foreach (WebContextMenuType i in Enum.GetValues(typeof(WebContextMenuType)))
            {
                if (e.MenuType.HasFlag(i))
                {
                    //BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = i.ToString(), Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.LinkUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                    if (BrowserMenu.Items.Count != 0 && BrowserMenu.Items[BrowserMenu.Items.Count - 1].GetType() == typeof(MenuItem))
                        BrowserMenu.Items.Add(new Separator());
                    if (i == WebContextMenuType.Link)
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uE8A7", Header = "Open in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(e.LinkUrl, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy link", Command = new RelayCommand(_ => Clipboard.SetText(e.LinkUrl)) });
                    }
                    else if (i == WebContextMenuType.Selection && !e.IsEditable)
                    {
                        IsPageMenu = false;
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText)), true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+C", Icon = "\ue8c8", Header = "Copy", Command = new RelayCommand(_ => Clipboard.SetText(e.SelectionText)) });
                        BrowserMenu.Items.Add(new Separator());
                        BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select All", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                    }
                    else if (i == WebContextMenuType.Media)
                    {
                        if (e.MediaType == WebContextMenuMediaType.Image)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\xe8b9",
                                Header = "Copy image",
                                Command = new RelayCommand(_ => {
                                    try { Utils.DownloadAndCopyImage(e.SourceUrl); }
                                    catch { Clipboard.SetText(e.SourceUrl); }
                                })
                            });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy image link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save image as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem
                            {
                                Icon = "\uF6Fa",
                                Header = "Search image",
                                Command = new RelayCommand(_ =>
                                {
                                    string Url = string.Empty;
                                    switch (App.Instance.GlobalSave.GetInt("ImageSearch"))
                                    {
                                        case 0:
                                            Url = $"https://lens.google.com/uploadbyurl?url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 1:
                                            Url = $"https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 2:
                                            Url = $"https://yandex.com/images/search?rpt=imageview&url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                        case 3:
                                            Url = $"https://tineye.com/search?url={Uri.EscapeDataString(e.SourceUrl)}";
                                            break;
                                    }
                                    Tab.ParentWindow.NewTab(Url, true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                                })
                            });
                            //model.AddItem((CefMenuCommand)26502, "Open in paintbrush");
                        }
                        else if (e.MediaType == WebContextMenuMediaType.Video)
                        {
                            IsPageMenu = false;
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue71b", Header = "Copy video link", Command = new RelayCommand(_ => Clipboard.SetText(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save video as", Command = new RelayCommand(_ => WebView?.Download(e.SourceUrl)) });
                            BrowserMenu.Items.Add(new MenuItem { Icon = "\uee49", Header = "Picture in picture", Command = new RelayCommand(_ => WebView?.ExecuteScript("(async()=>{let playingVideo=Array.from(document.querySelectorAll('video')).find(v=>!v.paused&&!v.ended&&v.readyState>2);if (!playingVideo){playingVideo=document.querySelector('video');}if (playingVideo&&document.pictureInPictureEnabled){await playingVideo.requestPictureInPicture();}})();")) });
                        }
                    }
                }
            }
            if (e.IsEditable)
            {
                if (e.SpellCheck && e.DictionarySuggestions.Count != 0)
                {
                    foreach (string Suggestion in e.DictionarySuggestions)
                    {
                        BrowserMenu.Items.Add(new MenuItem { Icon = "\uf87b", Header = Suggestion, Command = new RelayCommand(_ =>
                        {
                            if (WebView is ChromiumWebView ChromiumWebView)
                                ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().ReplaceMisspelling(Suggestion);
                        })});
                    }
                    BrowserMenu.Items.Add(new MenuItem { Icon = "\ue82e", Header = "Add to dictionary", Command = new RelayCommand(_ =>
                    {
                        if (WebView is ChromiumWebView ChromiumWebView)
                            ((ChromiumWebBrowser)ChromiumWebView.Control).GetBrowserHost().AddWordToDictionary(e.MisspelledWord);
                    }) });
                    BrowserMenu.Items.Add(new Separator());
                }

                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Win+Period", Icon = "\ue76e", Header = "Emoji", Command = new RelayCommand(_ => CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji)) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+Z", Icon = "\ue7a7", Header = "Undo", Command = new RelayCommand(_ => WebView?.Undo()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+Y", Icon = "\ue7a6", Header = "Redo", Command = new RelayCommand(_ => WebView?.Redo()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+X", Icon = "\ue8c6", Header = "Cut", Command = new RelayCommand(_ => WebView?.Cut()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+C", Icon = "\ue8c8", Header = "Copy", Command = new RelayCommand(_ => WebView?.Copy()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+V", Icon = "\ue77f", Header = "Paste", Command = new RelayCommand(_ => WebView?.Paste()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue74d", Header = "Delete", Command = new RelayCommand(_ => WebView?.Delete()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select All", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                if (!string.IsNullOrEmpty(e.SelectionText))
                {
                    BrowserMenu.Items.Add(new Separator());
                    BrowserMenu.Items.Add(new MenuItem { Icon = "\uF6Fa", Header = $"Search \"{e.SelectionText.Cut(20, true)}\" in new tab", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab(Utils.FixUrl(string.Format(App.Instance.DefaultSearchProvider.SearchUrl, e.SelectionText)), true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                }
            }
            else if (IsPageMenu)// && e.MediaType == WebContextMenuMediaType.None)
            {
                BrowserMenu.Items.Add(new MenuItem { IsEnabled = WebView.CanGoBack, Icon = "\uE76B", Header = "Back", Command = new RelayCommand(_ => WebView?.Back()) });
                BrowserMenu.Items.Add(new MenuItem { IsEnabled = WebView.CanGoForward, Icon = "\uE76C", Header = "Forward", Command = new RelayCommand(_ => WebView?.Forward()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE72C", Header = "Refresh", Command = new RelayCommand(_ => WebView?.Refresh()) });
                BrowserMenu.Items.Add(new Separator());
                BrowserMenu.Items.Add(new MenuItem { Icon = "\ue792", Header = "Save as", Command = new RelayCommand(_ => WebView?.SaveAs()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE749", Header = "Print", Command = new RelayCommand(_ => WebView?.Print()) });
                BrowserMenu.Items.Add(new MenuItem { InputGestureText = "Ctrl+A", Icon = "\ue8b3", Header = "Select All", Command = new RelayCommand(_ => WebView?.SelectAll()) });
                BrowserMenu.Items.Add(new Separator());

                BrowserMenu.Items.Add(new MenuItem { IsEnabled = !IsLoading && !Address.StartsWith("slbr:", StringComparison.Ordinal), Icon = "\uE8C1", Header = $"Translate to {TranslateComboBox.SelectedValue}", Command = new RelayCommand(_ => Translate()) });
                BrowserMenu.Items.Add(new MenuItem { Icon = "\uE924", Header = "Screenshot", Command = new RelayCommand(_ => Screenshot()) });

                /*MenuItem ZoomSubMenuModel = new MenuItem { Icon = "\ue71e", Header = "Zoom" };
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue8a3", Header = "Zoom in", Command = new RelayCommand(_ => Zoom(1)) });
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue71f", Header = "Zoom out", Command = new RelayCommand(_ => Zoom(-1)) });
                ZoomSubMenuModel.Items.Add(new MenuItem { Icon = "\ue72c", Header = "Reset", Command = new RelayCommand(_ => Zoom(0)) });

                menu.Items.Add(ZoomSubMenuModel);*/

                BrowserMenu.Items.Add(new Separator());

                MenuItem AdvancedSubMenuModel = new MenuItem { Icon = "\uec7a", Header = "Advanced" };
                AdvancedSubMenuModel.Items.Add(new MenuItem { Icon = "\uec7a", Header = "Inspect", Command = new RelayCommand(_ => DevTools()) });
                AdvancedSubMenuModel.Items.Add(new MenuItem { Icon = "\ue943", Header = "View source", Command = new RelayCommand(_ => Tab.ParentWindow.NewTab($"view-source:{e.FrameUrl}", true, Tab.ParentWindow.TabsUI.SelectedIndex + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")))) });
                BrowserMenu.Items.Add(AdvancedSubMenuModel);
            }
            BrowserMenu.PlacementTarget = WebView?.Control;
            BrowserMenu.IsOpen = true;
        }

        private void WebView_AuthenticationRequested(object? sender, WebAuthenticationRequestedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CredentialsDialogWindow _CredentialsDialogWindow = new CredentialsDialogWindow($"Sign in to {Utils.FastHost(e.Url)}", "\uec19");
                _CredentialsDialogWindow.Topmost = true;
                if (_CredentialsDialogWindow.ShowDialog().ToBool())
                {
                    e.Username = _CredentialsDialogWindow.Username;
                    e.Password = _CredentialsDialogWindow.Password;
                }
                else
                    e.Cancel = true;
            });
        }

        private async void WebView_FaviconChanged(object? sender, string e)
        {
            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("Favicons")))
                SetIcon(await App.Instance.SetIcon(e, Address, Private));
        }

        public void UnFocus()
        {
            //SLBr seems to freeze when switching from a loaded tab with devtools to an unloaded tab
            DevTools(true);
            if (App.Instance.LiteMode && WebView != null && WebView.Engine == WebEngineType.ChromiumEdge && WebView.IsBrowserInitialized)
            {
                WebView?.CallDevToolsAsync("Page.setWebLifecycleState", new
                {
                    state = "frozen"
                });
            }
        }

        public void ReFocus()
        {
            if (Tab.IsUnloaded)
            {
                InitializeBrowserComponent();
                if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
                {
                    WebView?.Control?.Visibility = Visibility.Collapsed;
                    if (_Settings == null)
                    {
                        _Settings = new Settings(this);
                        CoreContainer.Children.Add(_Settings);
                    }
                    _Settings.Visibility = Visibility.Visible;
                }
                else
                {
                    WebView?.Control?.Visibility = Visibility.Visible;//VIDEO
                    if (_Settings != null)
                    {
                        CoreContainer.Children.Remove(_Settings);
                        _Settings?.Dispose();
                        _Settings = null;
                    }
                }
            }
            else
            {
                if (WebView.Engine == WebEngineType.ChromiumEdge)
                {
                    //Warning: WebView2 somehow forgets the auto dark mode after a while
                    WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
                    {
                        enabled = App.Instance.CurrentTheme.DarkWebPage
                    });
                    //if (WebView2DevToolsHWND != IntPtr.Zero)
                    //    WebView2DevTools_SizeChanged(null, null);
                    if (App.Instance.LiteMode && WebView != null && WebView.IsBrowserInitialized)
                    {
                        WebView?.CallDevToolsAsync("Page.setWebLifecycleState", new
                        {
                            state = "active"
                        });
                    }
                }
            }
        }

        public async void LimitNetwork(int LatencyMs, double DownloadLimitMbps, double UploadLimitMbps)
        {
            //DevToolsClient _DevToolsClient = Chromium.GetDevToolsClient();
            //_DevToolsClient.Network.EnableAsync();
            await WebView?.CallDevToolsAsync("Network.enable");
            //_DevToolsClient.Network.EmulateNetworkConditionsAsync(false, LatencyMs, DownloadThroughput, UploadThroughput, CefSharp.DevTools.Network.ConnectionType.Wifi);
            // Mbps to bytes per second

            //TODO: Switch to Network.emulateNetworkConditionsByRule for https://issues.chromium.org/issues/40434685
            //Network.emulateNetworkConditions is going to be deprecated
            await WebView?.CallDevToolsAsync("Network.emulateNetworkConditions", new
            {
                offline = false,
                latency = LatencyMs,
                downloadThroughput = DownloadLimitMbps * 125000,
                uploadThroughput = UploadLimitMbps * 125000,
                connectionType = "wifi",
            });
        }

        /*private void Chromium_FrameLoadStart(object? sender, FrameLoadStartEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                if (Utils.IsHttpScheme(e.Url))
                {
                    e.Browser.ExecuteScriptAsync(Scripts.AntiCloseScript);//Replacement for DoClose of LifeSpanHandler in RuntimeStyle Chrome
                    e.Browser.ExecuteScriptAsync(Scripts.ShiftContextMenuScript);
                    if (bool.Parse(App.Instance.GlobalSave.Get("AntiTamper")))
                    {
                        if (bool.Parse(App.Instance.GlobalSave.Get("AntiFullscreen")))
                            e.Browser.ExecuteScriptAsync(Scripts.AntiFullscreenScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect")))
                            e.Browser.ExecuteScriptAsync(Scripts.LateAntiDevtoolsScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu")))
                            e.Browser.ExecuteScriptAsync(Scripts.ForceContextMenuScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("TextSelection")))
                            e.Browser.ExecuteScriptAsync(Scripts.AllowInteractionScript);
                        if (bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter")))
                            e.Browser.ExecuteScriptAsync(Scripts.RemoveFilterCSS);
                        if (bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay")))
                            e.Browser.ExecuteScriptAsync(Scripts.RemoveOverlayCSS);
                    }
                    if (bool.Parse(App.Instance.GlobalSave.Get("ForceLazy")))
                        e.Browser.ExecuteScriptAsync(Scripts.ForceLazyLoad);
                }
                else if (e.Url.StartsWith("slbr:", StringComparison.Ordinal))
                    e.Browser.ExecuteScriptAsync(App.InternalJavascriptFunction);
            }
        }*/

        /*private void Chromium_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            if (e.Delta != 0)
                Zoom(e.Delta);
        }*/

        /*private void Chromium_LoadError(object? sender, LoadErrorEventArgs e)
        {
            if (e.ErrorCode == CefErrorCode.Aborted)
                return;
            Dispatcher.Invoke(() =>
            {
                _ResourceRequestHandlerFactory.RegisterHandler(e.FailedUrl, ResourceHandler.GetByteArray(App.Instance.GenerateCannotConnect(e.FailedUrl, e.ErrorCode, e.ErrorText), Encoding.UTF8), "text/html", 1, string.Empty);
                e.Frame.LoadUrl(e.FailedUrl);
            });
        }*/

        bool IsCustomTheme = false;
        bool DevToolsAdBlock = false;

        public async Task ToggleEfficientAdBlock(bool Boolean)
        {
            AdBlockToggleButton.IsEnabled = !Boolean;
            AdBlockContainer.ToolTip = Boolean ? "Whitelist is unavailable in efficient ad block mode." : string.Empty;
            if (Boolean && !DevToolsAdBlock)
            {
                await WebView?.CallDevToolsAsync("Network.enable");
                await WebView?.CallDevToolsAsync("Network.setBlockedURLs", new { urls = App.BlockedAdPatterns });
                DevToolsAdBlock = true;
            }
            else if (!Boolean && DevToolsAdBlock)
            {
                await WebView?.CallDevToolsAsync("Network.setBlockedURLs", new { urls = Array.Empty<string>() });
                await WebView?.CallDevToolsAsync("Network.disable");
                DevToolsAdBlock = false;
            }
        }

        WebAppManifest? CurrentWebAppManifest;
        string CurrentWebAppManifestUrl;

        public async Task<(bool Installable, string ManifestUrl)> IsInstallableAsync()
        {
            if (WebView != null && WebView.CanExecuteJavascript)
            {
                try
                {
                    string? Response = await WebView.EvaluateScriptAsync(Scripts.DetectPWA);
                    if (string.IsNullOrEmpty(Response))
                        return (false, string.Empty);

                    using var Document = JsonDocument.Parse(Response);
                    string Manifest = Document.RootElement.GetProperty("manifest").GetString();
                    bool ServiceWorker = Document.RootElement.GetProperty("service_worker").GetBoolean();

                    return (!string.IsNullOrEmpty(Manifest) && ServiceWorker, Manifest);

                    /*string? Response = await WebView?.EvaluateScriptAsync(Scripts.DetectPWA);
                    if (Response == null)
                        return (false, "");
                    dynamic o = Response.Result;
                    string Manifest = o.manifest;
                    bool SW = o.service_worker;

                    return (!string.IsNullOrEmpty(Manifest) && SW, Manifest);*/
                }
                catch { }
            }
            return (false, string.Empty);
        }

        public bool CanUnload()
        {
            return (Muted || !AudioPlaying) && WebView != null && WebView.IsBrowserInitialized;
        }

        public async Task<bool> IsArticle()
        {
            if (WebView != null && WebView.IsBrowserInitialized && WebView.CanExecuteJavascript)
            {
                try
                {
                    var Response = await WebView?.EvaluateScriptAsync(Scripts.ArticleScript);
                    //if (Response.Success && Response.Result is bool IsArticle)
                    //    return IsArticle;
                    if (Response != null)
                        return bool.Parse(Response);
                }
                catch { return false; }
            }
            return false;
        }

        private void AdBlockToggleButton_Click(object sender, RoutedEventArgs e)
        {
            string Host = Utils.FastHost(Address);
            if (AdBlockToggleButton.IsChecked.ToBool())
                App.Instance.AdBlockAllowList.Remove(Host);
            else
                App.Instance.AdBlockAllowList.Add(Host);
        }

        async void BrowserLoadChanged(string Address, bool? IsLoading = null)
        {
            string OutputUrl = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(Address));
            if (OmniBox.Text != OutputUrl)
            {
                if (IsOmniBoxModifiable())
                {
                    if (Address.StartsWith("slbr://newtab", StringComparison.Ordinal))
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Visible;
                        OmniBox.Text = string.Empty;
                    }
                    else
                    {
                        OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                        OmniBox.Text = OutputUrl;
                    }
                    OmniBoxIsDropdown = false;
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                }
                OmniBox.Tag = Address;
            }
            if (FavouriteExists(Address) != -1)
            {
                FavouriteButton.Content = "\xEB52";
                FavouriteButton.Foreground = App.Instance.FavouriteColor;
                FavouriteButton.ToolTip = "Remove from favourites";
                Tab.FavouriteCommandHeader = "Remove from favourites";
            }
            else
            {
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
            if (Address.StartsWith("slbr://settings", StringComparison.Ordinal))
            {
                if (WebView != null)
                    WebView?.Control?.Visibility = Visibility.Collapsed;
                if (_Settings == null)
                {
                    _Settings = new Settings(this);
                    CoreContainer.Children.Add(_Settings);
                }
                _Settings.Visibility = Visibility.Visible;
            }
            else
            {
                if (WebView != null)
                    WebView?.Control?.Visibility = Visibility.Visible;//VIDEO
                if (_Settings != null)
                {
                    CoreContainer.Children.Remove(_Settings);
                    _Settings?.Dispose();
                    _Settings = null;
                }
            }
            bool IsHTTP = Utils.IsHttpScheme(Address);
            if (IsHTTP && App.Instance.AdBlock != 0)
            {
                string Host = Utils.FastHost(Address);
                AdBlockHostText.Text = Host;
                AdBlockToggleButton.IsChecked = !App.Instance.AdBlockAllowList.Has(Host);
                AdBlockContainer.Visibility = Visibility.Visible;
            }
            else
                AdBlockContainer.Visibility = Visibility.Collapsed;
            if (Translated)
                Translated = false;
            if (IsReaderMode)
                IsReaderMode = false;
            if (IsLoading != null)
            {
                SiteInformationIcon.FontFamily = App.Instance.IconFont;
                SiteInformationPopupIcon.FontFamily = App.Instance.IconFont;
                LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                bool IsLoadingBool = IsLoading.ToBool();
                if (App.Instance.AllowTranslateButton)
                    TranslateButton.IsEnabled = !IsLoadingBool;
                if (!IsLoadingBool)
                {
                    string SetSiteInfo = string.Empty;
                    if (WebViewManager.OverrideRequests.TryGetValue(Address, out RequestOverrideItem Item))
                    {
                        if (!string.IsNullOrEmpty(Item.Error))
                        {
                            if (Item.Error.StartsWith("Malware") || Item.Error.StartsWith("Potentially_Harmful_Application") || Item.Error.StartsWith("Social_Engineering") || Item.Error.StartsWith("Unwanted_Software"))
                                SetSiteInfo = "Danger";
                        }
                    }
                    if (string.IsNullOrEmpty(SetSiteInfo))
                    {
                        if (IsHTTP)
                        {
                            SiteInformationCertificate.Visibility = Visibility.Visible;
                            if (WebView != null && WebView.IsBrowserInitialized)
                            {
                                if (WebView.IsSecure)
                                {
                                    SetSiteInfo = "Secure";
                                    if (WebView is ChromiumWebView ChromiumView)
                                    {
                                        NavigationEntry _NavigationEntry = await ((ChromiumWebBrowser)ChromiumView.Control).GetVisibleNavigationEntryAsync();
                                        if (_NavigationEntry != null)
                                        {
                                            /*if (_NavigationEntry.HttpStatusCode == 0)
                                            {
                                                WebView?.Stop();
                                                WebView?.Navigate(WebView?.Address);
                                                return;
                                            }*/
                                            if (_NavigationEntry.HttpStatusCode == 418)
                                                SetSiteInfo = "Teapot";

                                            CertificateValidation.Text = _NavigationEntry.SslStatus.IsSecureConnection ? "Certificate is valid" : "Certificate is invalid";
                                            SslStatus _SSL = _NavigationEntry.SslStatus;
                                            if (_SSL.X509Certificate != null)
                                            {
                                                CertificateInfo.Visibility = Visibility.Visible;
                                                var IssuedTo = Utils.ParseCertificateIssue(_SSL.X509Certificate.Subject);
                                                IssueToCommonName.Text = IssuedTo.Item1;
                                                IssueToCompany.Text = IssuedTo.Item2;
                                                var IssuedBy = Utils.ParseCertificateIssue(_SSL.X509Certificate.Issuer);
                                                IssueByCommonName.Text = IssuedBy.Item1;
                                                IssueByCompany.Text = IssuedBy.Item2;
                                                CertificateStart.Text = _SSL.X509Certificate.NotBefore.Date.ToShortDateString();
                                                CertificateEnd.Text = _SSL.X509Certificate.NotAfter.Date.ToShortDateString();
                                            }
                                            else
                                                CertificateInfo.Visibility = Visibility.Collapsed;
                                        }
                                        else
                                            CertificateInfo.Visibility = Visibility.Collapsed;
                                    }
                                }
                                else
                                    SetSiteInfo = "Insecure";
                            }
                            else
                            {
                                if (Address.StartsWith("https:", StringComparison.Ordinal))
                                    SetSiteInfo = "Secure";
                                else
                                    SetSiteInfo = "Insecure";
                                CertificateInfo.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            if (Address.StartsWith("file:", StringComparison.Ordinal))
                                SetSiteInfo = "File";
                            else if (Address.StartsWith("slbr:", StringComparison.Ordinal))
                                SetSiteInfo = "SLBr";
                            else if (Address.StartsWith("chrome-extension:", StringComparison.Ordinal))
                                SetSiteInfo = "Extension";
                            else
                                SetSiteInfo = "Protocol";
                        }
                    }
                    switch (SetSiteInfo)
                    {
                        case "Secure":
                            SiteInformationIcon.Text = "\xE72E";
                            SiteInformationIcon.Foreground = App.Instance.LimeGreenColor;
                            SiteInformationText.Text = $"Secure";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE72E";
                            SiteInformationPopupIcon.Foreground = App.Instance.LimeGreenColor;
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is secure";
                            break;
                        case "Insecure":
                            SiteInformationIcon.Text = "\xE785";
                            SiteInformationIcon.Foreground = App.Instance.RedColor;
                            SiteInformationText.Text = $"Insecure";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE785";
                            SiteInformationPopupIcon.Foreground = App.Instance.RedColor;
                            SiteInformationPopupText.Text = $"Connection to {Utils.Host(Address)} is insecure";
                            break;
                        case "File":
                            SiteInformationIcon.Text = "\xE8B7";
                            SiteInformationIcon.Foreground = App.Instance.NavajoWhiteColor;
                            SiteInformationText.Text = $"File";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE8B7";
                            SiteInformationPopupIcon.Foreground = App.Instance.NavajoWhiteColor;
                            SiteInformationPopupText.Text = $"Local or shared file";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "SLBr":
                            SiteInformationIcon.Text = "\u2603";
                            SiteInformationIcon.FontFamily = App.Instance.SLBrFont;
                            SiteInformationIcon.Foreground = App.Instance.SLBrColor;
                            SiteInformationText.Text = $"SLBr";
                            TranslateButton.Visibility = Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\u2603";
                            SiteInformationPopupIcon.FontFamily = App.Instance.SLBrFont;
                            SiteInformationPopupIcon.Foreground = App.Instance.SLBrColor;
                            SiteInformationPopupText.Text = $"Secure SLBr page";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Danger":
                            SiteInformationIcon.Text = "\xE730";
                            SiteInformationIcon.Foreground = App.Instance.RedColor;
                            SiteInformationText.Text = $"Danger";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE730";
                            SiteInformationPopupIcon.Foreground = App.Instance.RedColor;
                            SiteInformationPopupText.Text = $"Dangerous site";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Protocol":
                            SiteInformationIcon.Text = "\xE774";
                            SiteInformationIcon.Foreground = App.Instance.CornflowerBlueColor;
                            SiteInformationText.Text = $"Protocol";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xE774";
                            SiteInformationPopupIcon.Foreground = App.Instance.CornflowerBlueColor;
                            SiteInformationPopupText.Text = $"Network protocol";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Extension":
                            SiteInformationIcon.Text = "\xEA86";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationText.Text = $"Extension";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xEA86";
                            SiteInformationPopupIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationPopupText.Text = $"Extension";
                            SiteInformationCertificate.Visibility = Visibility.Collapsed;
                            break;
                        case "Teapot":
                            SiteInformationIcon.Text = "\xEC32";
                            SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationText.Text = $"Teapot";
                            TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton ? Visibility.Visible : Visibility.Collapsed;
                            SiteInformationPopupIcon.Text = "\xEC32";
                            SiteInformationPopupIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                            SiteInformationPopupText.Text = "I'm a teapot";
                            break;
                    }
                    LoadingStoryboard?.Seek(TimeSpan.Zero);
                    LoadingStoryboard?.Stop();
                    if (App.Instance.AllowReaderModeButton && IsHTTP)
                        ReaderModeButton.Visibility = (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed;
                    else
                        ReaderModeButton.Visibility = Visibility.Collapsed;
                    SiteInformationPopupButton.IsEnabled = true;
                }
                else if (SiteInformationText.Text != "Loading")
                {
                    SiteInformationIcon.Text = "\xF16A";
                    SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                    SiteInformationText.Text = "Loading";
                    LoadingStoryboard?.Begin();
                    SiteInformationPopupButton.IsEnabled = false;
                }
            }
        }

        private string PAddress;
        public string Address
        {
            get
            {
                if (WebView != null)
                    PAddress = WebView?.Address;
                return PAddress;
            }
            set
            {
                PAddress = value;
                WebView?.Address = value;
            }
        }
        private string PTitle;
        public string Title
        {
            get
            {
                if (WebView != null)
                    PTitle = WebView?.Title != null && WebView?.Title.Trim().Length > 0 ? WebView?.Title : Utils.CleanUrl(Address);
                return PTitle;
            }
            set
            {
                PTitle = value;
            }
        }
        public bool CanGoBack
        {
            get
            {
                return WebView?.CanGoBack ?? false;
            }
        }
        public bool CanGoForward
        {
            get
            {
                return WebView?.CanGoForward ?? false;
            }
        }
        public bool IsLoading
        {
            get
            {
                return WebView?.IsLoading ?? false;
            }
        }

        public void Unload()
        {
            SetAudioState(false);
            if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon")))
                SetIcon(App.Instance.UnloadedIcon, true);
            DisposeBrowserCore();
            Tab.IsUnloaded = true;
            Tab.BrowserCommandsVisibility = Visibility.Collapsed;
            Tab.ProgressBarVisibility = Visibility.Collapsed;
        }
        private void Browser_GotFocus(object sender, RoutedEventArgs e)
        {
            ReFocus();
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            WebView?.Back();
        }
        public void Forward()
        {
            if (!CanGoForward)
                return;
            WebView?.Forward();
        }
        public void Refresh(bool IgnoreCache = false, bool ClearCache = false)
        {
            if (!IsLoading)
                WebView?.Refresh(IgnoreCache, ClearCache);
            else
                Stop();
        }
        public void Stop()
        {
            WebView?.Stop();
        }

        public static void ActivatePopup(Popup popup)
        {
            DllUtils.SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(popup.Child)).Handle);
        }

        public async void Find(string Text, bool Forward = true, bool FindNext = false)
        {
            if (Text == string.Empty)
            {
                try
                {
                    var Response = await WebView?.EvaluateScriptAsync("window.getSelection().toString();");
                    if (Response != null)
                        Text = Response.Trim('"');
                }
                catch { }
            }
            FindPopup.IsOpen = true;
            FindTextBox.Text = Text;
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
            PreviousFindButton.IsEnabled = Text.Length > 0;
            NextFindButton.IsEnabled = Text.Length > 0;
            WebView?.Find(Text, Forward, false, FindNext);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            string Value = ((Button)sender).ToolTip.ToString()!;
            if (Value == "Close")
                StopFind();
            else if (Value == "Previous")
                Find(FindTextBox.Text, false, true);
            else if (Value == "Next")
                Find(FindTextBox.Text, true, true);
        }

        private void FindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (FindTextBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                    Find(FindTextBox.Text, true, true);
                else
                    Find(FindTextBox.Text);
                PreviousFindButton.IsEnabled = true;
                NextFindButton.IsEnabled = true;
            }
            else
            {
                PreviousFindButton.IsEnabled = false;
                NextFindButton.IsEnabled = false;
                WebView?.StopFind();
            }
        }

        private void FindTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ActivatePopup(FindPopup);
            Keyboard.Focus(FindTextBox);
            FindPopup.Focus();
        }

        public void StopFind()
        {
            FindPopup.IsOpen = false;
            WebView?.StopFind();
        }
        public void Navigate(string Url)
        {
            Address = Url;
        }

        bool ActiveSizeEmulation;
        private void SizeEmulator()
        {
            SizeEmulatorColumn1.Width = new GridLength(0);
            SizeEmulatorColumn2.Width = new GridLength(0);
            SizeEmulatorRow1.Height = new GridLength(0);
            SizeEmulatorRow2.Height = new GridLength(0);
            SizeEmulatorColumnSplitter1.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorColumnSplitter2.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorRowSplitter1.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            SizeEmulatorRowSplitter2.Visibility = ActiveSizeEmulation ? Visibility.Collapsed : Visibility.Visible;
            ActiveSizeEmulation = !ActiveSizeEmulation;
        }
        private void OpenAsPopupBrowser(string Url)
        {
            new PopupBrowser(Url, -1, -1).Show();
        }
        private void SetSideBarDock(int DockID)
        {
            SideBarColumnLeft.Width = new GridLength(0);
            SideBarColumnRight.Width = new GridLength(0);
            SideBarRowBottom.Height = new GridLength(0);
            SideBarRowTop.Height = new GridLength(0);
            if (IsLoaded)
            {
                SideBarRowSplitterBottom.Visibility = Visibility.Collapsed;
                SideBarRowSplitterTop.Visibility = Visibility.Collapsed;
                SideBarColumnSplitterLeft.Visibility = Visibility.Collapsed;
                SideBarColumnSplitterRight.Visibility = Visibility.Collapsed;
            }
            if (!IsInitialized)
                DockID = 0;
            switch (DockID)
            {
                case 0://RIGHT
                    if (IsLoaded)
                        SideBarColumnSplitterRight.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 2);
                    Grid.SetRow(SideBar, 1);
                    SideBarColumnRight.MinWidth = 200;
                    SideBarColumnRight.Width = new GridLength(500);
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarRowTop.MinHeight = 0;
                    SideBarRowBottom.MinHeight = 0;
                    SideBar.BorderThickness = new Thickness(1, 0, 0, 0);
                    break;
                case 1://LEFT
                    if (IsLoaded)
                        SideBarColumnSplitterLeft.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 0);
                    Grid.SetRow(SideBar, 1);
                    SideBarColumnLeft.MinWidth = 200;
                    SideBarColumnLeft.Width = new GridLength(500);
                    SideBarColumnRight.MinWidth = 0;
                    SideBarRowTop.MinHeight = 0;
                    SideBarRowBottom.MinHeight = 0;
                    SideBar.BorderThickness = new Thickness(0, 0, 1, 0);
                    break;
                case 2://BOTTOM
                    if (IsLoaded)
                        SideBarRowSplitterBottom.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 1);
                    Grid.SetRow(SideBar, 2);
                    SideBarRowBottom.MinHeight = 200;
                    SideBarRowBottom.Height = new GridLength(350);
                    SideBarRowTop.MinHeight = 0;
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarColumnRight.MinWidth = 0;
                    SideBar.BorderThickness = new Thickness(0, 1, 0, 0);
                    break;
                case 3://TOP
                    if (IsLoaded)
                        SideBarRowSplitterTop.Visibility = Visibility.Visible;
                    Grid.SetColumn(SideBar, 1);
                    Grid.SetRow(SideBar, 0);
                    SideBarRowTop.MinHeight = 200;
                    SideBarRowTop.Height = new GridLength(350);
                    SideBarRowBottom.MinHeight = 0;
                    SideBarColumnLeft.MinWidth = 0;
                    SideBarColumnRight.MinWidth = 0;
                    SideBar.BorderThickness = new Thickness(0, 0, 0, 1);
                    break;
            }
            if (!IsInitialized)
            {
                SideBarColumnRight.MinWidth = 0;
                SideBarColumnRight.Width = new GridLength(0);
            }
        }

        bool IsUtilityContainerOpen;
        IWindowInfo SideBarWindowInfo;
        public HwndHoster DevToolsHost;
        //public IntPtr WebView2DevToolsHWND = IntPtr.Zero;
        public void ToggleSideBar(bool ForceClose = false)
        {
            DevToolsToolBar.Visibility = Visibility.Collapsed;
            if (IsUtilityContainerOpen || ForceClose)
            {
                SideBarColumnLeft.MinWidth = 0;
                SideBarColumnRight.MinWidth = 0;
                SideBarRowBottom.MinHeight = 0;
                SideBarRowTop.MinHeight = 0;
                SideBarColumnLeft.Width = new GridLength(0);
                SideBarColumnRight.Width = new GridLength(0);
                SideBarRowBottom.Height = new GridLength(0);
                SideBarRowTop.Height = new GridLength(0);
                if (IsUtilityContainerOpen)
                {
                    SideBarCoreContainer.Children.Clear();
                    _NewsFeed = null;
                    /*if (WebView2DevToolsHWND != IntPtr.Zero)
                    {
                        App.Instance.WebView2DevTools.Remove(WebView2DevToolsHWND);
                        DllUtils.SetParent(WebView2DevToolsHWND, DllUtils.GetDesktopWindow());
                        DllUtils.PostMessage(WebView2DevToolsHWND, DllUtils.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        WebView2DevToolsHWND = IntPtr.Zero;
                    }*/
                    if (DevToolsHost != null)
                    {
                        if (WebView is ChromiumWebView ChromiumView)
                            ((ChromiumWebBrowser)ChromiumView.Control)?.BrowserCore.CloseDevTools();
                        DevToolsHost.Dispose();
                        DevToolsHost = null;
                    }
                    SideBarWindowInfo?.Dispose();
                    SideBarWindowInfo = null;
                    IsUtilityContainerOpen = false;
                    GC.Collect();
                }
            }
            else
            {
                SetSideBarDock(3 - SideBarDockDropdown.SelectedIndex);
                IsUtilityContainerOpen = true;
            }
            SideBar.Visibility = IsUtilityContainerOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        public void DevTools(bool ForceClose = false, int XCoord = 0, int YCoord = 0)
        {
            if (!ForceClose)
            {
                if (IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
                {
                    ToggleSideBar(ForceClose);
                    return;
                }
                /*if (WebView.Engine == WebEngineType.Trident)
                {
                    InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Inspector Unavailable", "Trident webview does not support an inspector tool.", "\uec7a");
                    InfoWindow.Topmost = true;
                    InfoWindow.ShowDialog();
                    return;
                }*/

                if (WebView.Engine != WebEngineType.Chromium)
                {
                    if (WebView is ChromiumEdgeWebView EdgeWebView)
                        ((WebView2)EdgeWebView.Control).CoreWebView2.OpenDevToolsWindow();
                    else
                    {
                        InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Inspector Unavailable", "Trident webview does not support an inspector tool.", "\uec7a");
                        InfoWindow.Topmost = true;
                        InfoWindow.ShowDialog();
                    }
                    return;
                }
            }
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                DevToolsToolBar.Visibility = Visibility.Visible;
                DevToolsHost = new HwndHoster();
                SideBarCoreContainer.Children.Add(DevToolsHost);
                Grid.SetColumn(DevToolsHost, 1);
                Grid.SetRow(DevToolsHost, 1);

                DevToolsHost.HorizontalAlignment = HorizontalAlignment.Stretch;
                DevToolsHost.VerticalAlignment = VerticalAlignment.Stretch;

                DevToolsHost.Loaded += (s, args) =>
                {
                    if (DevToolsHost != null)
                    {
                        if (WebView is ChromiumWebView ChromiumView)
                        {
                            SideBarWindowInfo = WindowInfo.Create();
                            SideBarWindowInfo.SetAsChild(DevToolsHost.Handle);
                            ((ChromiumWebBrowser)ChromiumView.Control)?.BrowserCore?.ShowDevTools(SideBarWindowInfo, XCoord, YCoord);
                        }
                        //Damn you webview2, docked devtools works, except for the fact that it can't handle keyboard input
                        /*else if (WebView is ChromiumEdgeWebView EdgeWebView)
                        {
                            Dispatcher.BeginInvoke(async () =>
                            {
                                ((WebView2)EdgeWebView.Control).CoreWebView2.OpenDevToolsWindow();
                                await Task.Delay(600);
                                string DevToolsName = $"DevTools - {Utils.CleanUrl(Address, false, false, false, false, true)}";
                                DllUtils.EnumWindows((hWnd, lParam) =>
                                {
                                    DllUtils.GetWindowThreadProcessId(hWnd, out uint pid);
                                    Process _Process = Process.GetProcessById((int)pid);
                                    if (_Process.ProcessName.Contains("msedgewebview2", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (DllUtils.IsWindowVisible(hWnd) && DllUtils.GetWindowTextRaw(hWnd) == DevToolsName && !App.Instance.WebView2DevTools.Contains(hWnd))
                                        {
                                            WebView2DevToolsHWND = hWnd;
                                            return false;
                                        }
                                    }
                                    return true;
                                }, IntPtr.Zero);
                                if (WebView2DevToolsHWND != IntPtr.Zero)
                                {
                                    App.Instance.WebView2DevTools.Add(WebView2DevToolsHWND);
                                    DllUtils.SetParent(WebView2DevToolsHWND, DevToolsHost.Handle);

                                    int DevToolsWindowStyle = DllUtils.GetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_STYLE);
                                    DevToolsWindowStyle &= ~(DllUtils.WS_OVERLAPPEDWINDOW | DllUtils.WS_CAPTION | DllUtils.WS_THICKFRAME | DllUtils.WS_MINIMIZEBOX | DllUtils.WS_MAXIMIZEBOX | DllUtils.WS_SYSMENU);
                                    DevToolsWindowStyle |= DllUtils.WS_CHILD | DllUtils.WS_VISIBLE;
                                    DllUtils.SetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_STYLE, DevToolsWindowStyle);

                                    int DevToolsWindowExStyle = DllUtils.GetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_EXSTYLE);
                                    DevToolsWindowExStyle &= ~(DllUtils.WS_EX_DLGMODALFRAME | DllUtils.WS_EX_CLIENTEDGE | DllUtils.WS_EX_STATICEDGE);
                                    DllUtils.SetWindowLong(WebView2DevToolsHWND, DllUtils.GWL_EXSTYLE, DevToolsWindowExStyle);

                                    WebView2DevTools_SizeChanged(null, null);

                                    DevToolsHost.SizeChanged += WebView2DevTools_SizeChanged;
                                }
                            });
                        }*/
                    }
                };
            }
            SideBar.Visibility = IsUtilityContainerOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        /*private void WebView2DevTools_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DllUtils.SetWindowPos(WebView2DevToolsHWND, IntPtr.Zero, -7, -30, (int)DevToolsHost.ActualWidth + 14, (int)DevToolsHost.ActualHeight + 37, DllUtils.SWP_NOZORDER | DllUtils.SWP_FRAMECHANGED | DllUtils.SWP_SHOWWINDOW);
        }*/

        bool PIsReaderMode = false;
        bool IsReaderMode
        {
            get { return PIsReaderMode; }
            set
            {
                PIsReaderMode = value;
                if (value)
                    Dispatcher.BeginInvoke(() => ReaderModeButton.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.IndicatorColor));
                else
                    Dispatcher.BeginInvoke(() => ReaderModeButton.ClearValue(Control.ForegroundProperty));
            }
        }
        public async void ToggleReaderMode()
        {
            IsReaderMode = !IsReaderMode;
            if (IsReaderMode)
                WebView?.ExecuteScript(Scripts.ReaderModeScript);
            else
                Refresh();
        }

        News _NewsFeed;
        public void NewsFeed(bool ForceClose = false)
        {
            if (!ForceClose && IsUtilityContainerOpen && (_NewsFeed != null || DevToolsHost != null))
            {
                ToggleSideBar(ForceClose);
                return;
            }
            ToggleSideBar(ForceClose);
            if (IsUtilityContainerOpen)
            {
                DevToolsToolBar.Visibility = Visibility.Collapsed;
                _NewsFeed = new News(this);
                SideBarCoreContainer.Children.Add(_NewsFeed);
                Grid.SetColumn(_NewsFeed, 1);
                Grid.SetRow(_NewsFeed, 1);

                _NewsFeed.HorizontalAlignment = HorizontalAlignment.Stretch;
                _NewsFeed.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        bool Muted => WebView?.IsMuted ?? false;
        public void ToggleMute()
        {
            WebView?.IsMuted = !WebView.IsMuted;
            MuteMenuItem.Icon = Muted ? "\xe767" : "\xe74f";
            MuteMenuItem.Header = Muted ? "Unmute" : "Mute";
            SetAudioState(null);
        }

        public void Favourite()
        {
            int FavouriteExistIndex = FavouriteExists(Address);
            if (FavouriteExistIndex != -1)
            {
                App.Instance.Favourites.RemoveAt(FavouriteExistIndex);
                FavouriteButton.Content = "\xEB51";
                FavouriteButton.Foreground = (SolidColorBrush)FindResource("FontBrush");
                FavouriteButton.ToolTip = "Add to favourites";
                Tab.FavouriteCommandHeader = "Add to favourites";
            }
            else if (!IsLoading)
            {
                PromptDialogWindow InfoWindow = new PromptDialogWindow("Prompt", $"Add Favourite", "Name", Title);
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    App.Instance.Favourites.Add(new ActionStorage(InfoWindow.UserInput, $"4<,>{Address}", Address));
                    FavouriteButton.Content = "\xEB52";
                    FavouriteButton.Foreground = App.Instance.FavouriteColor;
                    FavouriteButton.ToolTip = "Remove from favourites";
                    Tab.FavouriteCommandHeader = "Remove from favourites";
                }
            }
        }

        public void SetFavouritesBarVisibility()
        {
            if (App.Instance.ShowFavouritesBar == 0)
            {
                if (App.Instance.Favourites.Count == 0)
                {
                    FavouriteScrollViewer.Margin = new Thickness(0);
                    FavouriteContainer.Height = 5;
                }
                else
                {
                    FavouriteScrollViewer.Margin = new Thickness(5);
                    FavouriteContainer.Height = 41.25f;
                }
            }
            else if (App.Instance.ShowFavouritesBar == 1)
            {
                FavouriteScrollViewer.Margin = new Thickness(5);
                FavouriteContainer.Height = 41.25f;
            }
            else if (App.Instance.ShowFavouritesBar == 2)
            {
                FavouriteScrollViewer.Margin = new Thickness(0);
                FavouriteContainer.Height = 5;
            }
        }

        int FavouriteExists(string Url)
        {
            if (App.Instance.Favourites.Count == 0)
                return -1;
            return App.Instance.Favourites.ToList().FindIndex(0, i => i.Tooltip == Url);
        }
        /*public void Zoom(int Delta)
        {
            if (WebView == null)
                return;
            if (Delta == 0)
                WebView?.ZoomFactor = 1;
            //{
                //MessageBox.Show(WebView?.ZoomFactor.ToString());
            //}
            //Chromium.ZoomResetCommand.Execute(null);
            else if (Delta > 0)
                WebView?.ZoomFactor += 0.1;
            //Chromium.ZoomInCommand.Execute(null);
            else if (Delta < 0)
                WebView?.ZoomFactor -= 0.1;
            //Chromium.ZoomOutCommand.Execute(null);
        }*/
        public async void Screenshot()
        {
            if (WebView == null)
                return;
            try
            {
                int _ScreenshotFormat = App.Instance.GlobalSave.GetInt("ScreenshotFormat");
                string FileExtension = "jpg";
                WebScreenshotFormat ScreenshotFormat = WebScreenshotFormat.JPEG;
                if (_ScreenshotFormat == 1)
                {
                    FileExtension = "png";
                    ScreenshotFormat = WebScreenshotFormat.PNG;
                }
                string ScreenshotPath = App.Instance.GlobalSave.Get("ScreenshotPath");
                DateTime CurrentTime = DateTime.Now;
                string Url = Path.Combine(ScreenshotPath, Regex.Replace($"{WebView?.Title} {CurrentTime.Day}-{CurrentTime.Month}-{CurrentTime.Year} {string.Format("{0:hh:mm tt}", DateTime.Now)}.{FileExtension}", "[^a-zA-Z0-9._ -]", string.Empty));
                File.WriteAllBytes(Url, await WebView?.TakeScreenshotAsync(ScreenshotFormat));
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
            catch { }
        }

        bool PrivateTranslate = false;
        bool Translated
        {
            get {  return PrivateTranslate; }
            set
            {
                PrivateTranslate = value;
                //Dispatcher.BeginInvoke(() => TranslateButton.Foreground = new SolidColorBrush(value ? App.Instance.CurrentTheme.IndicatorColor : App.Instance.CurrentTheme.FontColor));
                if (value)
                    Dispatcher.BeginInvoke(() => TranslateButton.Foreground = new SolidColorBrush(App.Instance.CurrentTheme.IndicatorColor));
                else
                    Dispatcher.BeginInvoke(() => TranslateButton.ClearValue(Control.ForegroundProperty));
            }
        }

        public async void Translate(bool Original = false)
        {
            if (Original)
            {
                TranslateButton.ClosePopup();
                if (Translated == true)
                    Refresh();
                return;
            }
            string Texts = await WebView.EvaluateScriptAsync(Scripts.GetTranslationText);
            List<string> TranslatedTexts = null;
            if (TranslateButton.Visibility == Visibility.Visible)
                TranslateButton.OpenPopup();
            await Dispatcher.BeginInvoke(() => TranslateLoadingPanel.Visibility = Visibility.Visible);
            List<string> AllTexts = JsonSerializer.Deserialize<List<string>>(Texts);
            string TargetLanguage = App.Instance.AllLocales.Where(i => i.Value == TranslateComboBox.SelectedValue).First().Key;
            switch (App.Instance.GlobalSave.GetInt("TranslationProvider"))
            {
                case 0:
                    IEnumerable<List<string>> GBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = new List<string>();
                    List<Task<List<string>>> GBatchTasks = new();

                    foreach (List<string> Batch in GBatches)
                    {
                        GBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, SECRETS.GOOGLE_TRANSLATE_ENDPOINT))
                            {
                                TranslateRequest.Headers.Add("Origin", "https://www.google.com");
                                TranslateRequest.Headers.Add("Accept", "*/*");
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new object[] { new object[] { Batch, "auto", TargetLanguage }, "te_lib" }), Encoding.UTF8, "application/json+protobuf");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<object> Json = JsonSerializer.Deserialize<List<object>>(Data);
                                if (Json == null || Json.Count == 0)
                                    return new List<string>();
                                if (Json[0] is not JsonElement Element || Element.ValueKind != JsonValueKind.Array)
                                    return new List<string>();
                                return Element.EnumerateArray().Select(e => HttpUtility.HtmlDecode(e.GetString())).ToList()!;
                            }
                        }));
                    }

                    var GResults = await Task.WhenAll(GBatchTasks);
                    foreach (List<string> BatchResult in GResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                    /*using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, SECRETS.GOOGLE_TRANSLATE_ENDPOINT))
                    {
                        TranslateRequest.Headers.Add("Origin", "https://www.google.com");
                        TranslateRequest.Headers.Add("Accept", "");
                        TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                        TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new object[] { new object[] { AllTexts, "auto", TargetLanguage }, "te_lib" }), Encoding.UTF8, "application/json+protobuf");
                        var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                        string Data = await Response.Content.ReadAsStringAsync();
                        List<object> Json = JsonSerializer.Deserialize<List<object>>(Data);
                        if (!Response.IsSuccessStatusCode)
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                        if (Json == null || Json.Count == 0)
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                        if (Json[0] is not JsonElement Element || Element.ValueKind != JsonValueKind.Array)
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                        TranslatedTexts = Element.EnumerateArray().Select(e => HttpUtility.HtmlDecode(e.GetString())).ToList()!;
                    }
                    break;*/
                case 1:
                    IEnumerable<List<string>> MBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = new List<string>();
                    List<Task<List<string>>> MBatchTasks = new();

                    foreach (List<string> Batch in MBatches)
                    {
                        MBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, string.Format(SECRETS.MICROSOFT_TRANSLATE_ENDPOINT, TargetLanguage)))
                            {
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(Batch), Encoding.UTF8, "application/json");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<string> Result = new List<string>();
                                try
                                {
                                    using JsonDocument Document = JsonDocument.Parse(Data);
                                    foreach (JsonElement Item in Document.RootElement.EnumerateArray())
                                    {
                                        if (Item.TryGetProperty("translations", out JsonElement TranslationsElement))
                                        {
                                            foreach (JsonElement TranslationElement in TranslationsElement.EnumerateArray())
                                            {
                                                if (TranslationElement.TryGetProperty("text", out JsonElement TextElement))
                                                    Result.Add(TextElement.GetString() ?? "");
                                            }
                                        }
                                    }
                                    return Result;
                                }
                                catch
                                {
                                    return new List<string>();
                                }
                            }
                        }));
                    }

                    var MResults = await Task.WhenAll(MBatchTasks);
                    foreach (List<string> BatchResult in MResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                    /*using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, string.Format(SECRETS.MICROSOFT_TRANSLATE_ENDPOINT, App.Instance.AllLocales.Where(i => i.Value == TranslateComboBox.SelectedValue).First().Key)))
                    {
                        TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                        TranslateRequest.Content = new StringContent(Texts, Encoding.UTF8, "application/json");
                        var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                        if (!Response.IsSuccessStatusCode)
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                        string Data = await Response.Content.ReadAsStringAsync();
                        TranslatedTexts = new List<string>();
                        try
                        {
                            using JsonDocument Document = JsonDocument.Parse(Data);
                            foreach (var Item in Document.RootElement.EnumerateArray())
                            {
                                if (Item.TryGetProperty("translations", out var TranslationsElement))
                                {
                                    foreach (var TranslationElement in TranslationsElement.EnumerateArray())
                                    {
                                        if (TranslationElement.TryGetProperty("text", out var TextElement))
                                            TranslatedTexts.Add(TextElement.GetString() ?? "");
                                    }
                                }
                            }
                        }
                        catch
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                    }*/
                case 2:
                    string SourceLanguage = "";
                    try
                    {
                        using (HttpRequestMessage LanguageDetectRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(SECRETS.YANDEX_LANGUAGE_DETECTION_ENDPOINT, $"{Utils.GenerateSID()}-0-0", HttpUtility.UrlEncode(AllTexts.First()))))
                        {
                            var Response = await App.MiniHttpClient.SendAsync(LanguageDetectRequest);
                            if (!Response.IsSuccessStatusCode)
                            {
                                Dispatcher.BeginInvoke(() => {
                                    TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                    InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                    InfoWindow.Topmost = true;
                                    InfoWindow.ShowDialog();
                                });
                                return;
                            }
                            string Data = await Response.Content.ReadAsStringAsync();
                            using var Document = JsonDocument.Parse(Data);
                            if (Document.RootElement.TryGetProperty("lang", out JsonElement LanguageElement))
                                SourceLanguage = LanguageElement.GetString() ?? "en";
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(() => {
                            TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                            InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                            InfoWindow.Topmost = true;
                            InfoWindow.ShowDialog();
                        });
                        return;
                    }
                    IEnumerable<List<string>> Batches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 16).Select(g => g.Select(x => x.t).ToList());

                    TargetLanguage = TargetLanguage.Split('-').First();
                    string YandexUserAgent = UserAgentGenerator.BuildUserAgentFromProduct("YaBrowser/25.2.0.0");
                    TranslatedTexts = new List<string>();
                    List<Task<List<string>>> BatchTasks = new();

                    foreach (List<string> Batch in Batches)
                    {
                        BatchTasks.Add(Task.Run(async () =>
                        {
                            List<string> EncodedTexts = Batch.Select(t => "text=" + HttpUtility.UrlEncode(t)).ToList();
                            string TextParameters = string.Join("&", EncodedTexts);
                            using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(SECRETS.YANDEX_ENDPOINT, $"{Utils.GenerateSID()}-0-0", $"{SourceLanguage}-{TargetLanguage}", TextParameters)))
                            {
                                TranslateRequest.Headers.Add("User-Agent", YandexUserAgent);
                                try
                                {
                                    var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                    Response.EnsureSuccessStatusCode();

                                    string Data = await Response.Content.ReadAsStringAsync();
                                    if (JsonDocument.Parse(Data).RootElement.TryGetProperty("text", out JsonElement TranslatedTexts))
                                        return TranslatedTexts.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                }
                                catch
                                {
                                    return new List<string>();
                                }
                            }
                            return new List<string>();
                        }));
                    }

                    var Results = await Task.WhenAll(BatchTasks);
                    foreach (List<string> BatchResult in Results)
                        TranslatedTexts.AddRange(BatchResult);
                    break;
                case 3:
                    TargetLanguage = TargetLanguage switch
                    {
                        "zh" => "zh-Hans",
                        "zh-CN" => "zh-Hans",
                        "zh-TW" => "zh-Hant",
                        "zh-HK" => "zh-Hant",
                        _ => TargetLanguage
                    };

                    IEnumerable<List<string>> LBatches = AllTexts.Select((t, i) => new { t, i }).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.t).ToList());

                    TranslatedTexts = new List<string>();
                    List<Task<List<string>>> LBatchTasks = new();

                    foreach (List<string> Batch in LBatches)
                    {
                        LBatchTasks.Add(Task.Run(async () =>
                        {
                            using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, SECRETS.LINGVANEX_ENDPOINT))
                            {
                                TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                                TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new
                                {
                                    target = TargetLanguage,
                                    q = Batch
                                }), Encoding.UTF8, "application/json");
                                var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                                if (!Response.IsSuccessStatusCode)
                                    return new List<string>();
                                string Data = await Response.Content.ReadAsStringAsync();
                                List<string> Result = new List<string>();

                                try
                                {
                                    using var Document = JsonDocument.Parse(Data);
                                    if (Document.RootElement.TryGetProperty("translatedText", out JsonElement TranslatedText))
                                    {
                                        foreach (var item in TranslatedText.EnumerateArray())
                                            Result.Add(item.GetString() ?? "");
                                    }
                                }
                                catch { }
                                return Result;
                            }
                        }));
                    }

                    var LResults = await Task.WhenAll(LBatchTasks);
                    foreach (List<string> BatchResult in LResults)
                        TranslatedTexts.AddRange(BatchResult);
                    break;


                    /*using (HttpRequestMessage TranslateRequest = new HttpRequestMessage(HttpMethod.Post, SECRETS.LINGVANEX_ENDPOINT))
                    {
                        TranslateRequest.Headers.Add("User-Agent", App.Instance.UserAgent);
                        TranslateRequest.Content = new StringContent(JsonSerializer.Serialize(new
                        {
                            target = TargetLanguage,
                            q = AllTexts
                        }), Encoding.UTF8, "application/json");
                        var Response = await App.MiniHttpClient.SendAsync(TranslateRequest);
                        if (!Response.IsSuccessStatusCode)
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                        string Data = await Response.Content.ReadAsStringAsync();
                        TranslatedTexts = new List<string>();

                        try
                        {
                            using var Document = JsonDocument.Parse(Data);
                            if (Document.RootElement.TryGetProperty("translatedText", out JsonElement TranslatedText))
                            {
                                foreach (var item in TranslatedText.EnumerateArray())
                                    TranslatedTexts.Add(item.GetString() ?? "");
                            }
                        }
                        catch
                        {
                            Dispatcher.BeginInvoke(() => {
                                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                                InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                                InfoWindow.Topmost = true;
                                InfoWindow.ShowDialog();
                            });
                            return;
                        }
                    }
                    break;*/
            }
            if (TranslatedTexts == null || TranslatedTexts.Count == 0)
            {
                Dispatcher.BeginInvoke(() => {
                    TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                    InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "Translation Unavailable", "Unable to translate website.", "\uE8C1");
                    InfoWindow.Topmost = true;
                    InfoWindow.ShowDialog();
                });
                return;
            }
            WebView.ExecuteScript(string.Format(Scripts.SetTranslationText, JsonSerializer.Serialize(TranslatedTexts)));
            Translated = true;
            Dispatcher.BeginInvoke(() => {
                TranslateLoadingPanel.Visibility = Visibility.Collapsed;
                TranslateButton.ClosePopup();
            });
            
        }

        private void FavouriteScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            FavouriteScrollViewer.ScrollToHorizontalOffset(FavouriteScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
        }

        private void OmniBoxContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            SiteInformationText.Visibility = Visibility.Visible;
            OmniBoxHovered = true;
        }

        private void OmniBoxContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!OmniBoxFocused)
                SiteInformationText.Visibility = Visibility.Collapsed;
            OmniBoxHovered = false;
        }

        public void OmniBoxEnter()
        {
            string Url = Utils.FilterUrlForBrowser(OmniBox.Text, (OmniBoxOverrideSearch ?? App.Instance.DefaultSearchProvider).SearchUrl);
            if (Url.StartsWith("javascript:", StringComparison.Ordinal))
            {
                WebView?.ExecuteScript(Url.Substring(11));
                OmniBox.Text = OmniBox.Tag.ToString();
            }
            else if (!Utils.IsProgramUrl(Url))
                Address = Url;
            if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
            {
                OmniBoxFastTimer.Stop();
                OmniBoxSmartTimer.Stop();
            }
            OmniBox.IsDropDownOpen = false;
            Keyboard.ClearFocus();
            WebView?.Control?.Focus();
            if (OmniBoxOverrideSearch != null)
            {
                OmniBoxStatus.Visibility = Visibility.Collapsed;
                OmniBoxOverrideSearch = null;
                if (OmniBox.Text.Length != 0)
                {
                    foreach (SearchProvider Search in App.Instance.SearchEngines)
                    {
                        if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                            OmniBoxStatusText.Text = $"Search {Search.Name}";
                            OmniBoxStatus.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                SetTemporarySiteInformation();
            }
        }

        private bool IsIgnorableKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F24)
                return true;
            return key switch
            {
                Key.Escape or Key.PrintScreen or Key.Pause or Key.Scroll
                or Key.Insert or Key.Delete or Key.Home or Key.End
                or Key.PageUp or Key.PageDown or Key.Up or Key.Down
                or Key.Left or Key.Right or Key.CapsLock or Key.NumLock
                or Key.Tab or Key.LWin or Key.RWin
                or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt => true,
                _ => false
            };
        }

        SearchProvider? OmniBoxOverrideSearch;

        private void OmniBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (OmniBoxOverrideSearch != null)
                {
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = null;
                    if (OmniBox.Text.Length != 0)
                    {
                        foreach (SearchProvider Search in App.Instance.SearchEngines)
                        {
                            if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                            {
                                OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                                OmniBoxStatusText.Text = $"Search {Search.Name}";
                                OmniBoxStatus.Visibility = Visibility.Visible;
                                break;
                            }
                        }
                    }
                    SetTemporarySiteInformation();
                    e.Handled = true;
                }
                else if (OmniBoxStatus.Visibility == Visibility.Visible)
                {
                    OmniBox.Text = string.Empty;
                    OmniBoxStatus.Visibility = Visibility.Collapsed;
                    OmniBoxOverrideSearch = App.Instance.SearchEngines[(int)OmniBoxStatus.Tag];
                    SetTemporarySiteInformation();
                    e.Handled = true;
                }
            }
        }

        private void OmniBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (OmniBox.Text.Trim().Length > 0)
            {
                if (e.Key == Key.Return)
                    OmniBoxEnter();
                else
                {
                    if (IsIgnorableKey(e.Key) || Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Windows)
                        return;
                    LoadingStoryboard = SiteInformationIcon.FindResource("LoadingAnimation") as Storyboard;
                    LoadingStoryboard?.Seek(TimeSpan.Zero);
                    LoadingStoryboard?.Stop();
                    if (OmniBox.Text.Length != 0)
                    {
                        SetTemporarySiteInformation();

                        if (!Private && bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions")))
                        {
                            OmniBoxFastTimer.Stop();
                            OmniBoxSmartTimer.Stop();
                            OmniBoxFastTimer.Start();
                            if (OmniBoxOverrideSearch == null && bool.Parse(App.Instance.GlobalSave.Get("SmartSuggestions")))
                                OmniBoxSmartTimer.Start();
                        }
                    }
                    else
                    {
                        SiteInformationIcon.FontFamily = App.Instance.IconFont;
                        SiteInformationIcon.Text = "\xE721";
                        SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                        SiteInformationText.Text = $"Search";
                        SiteInformationPopupButton.ToolTip = $"Searching: {OmniBox.Text}";
                    }
                    if (OmniBox.IsDropDownOpen)
                    {
                        OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
                        OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
                    }
                }
            }
        }
        public bool IsOmniBoxModifiable()
        {
            return !OmniBoxFocused;
        }
        public void SetTemporarySiteInformation()
        {
            SiteInformationIcon.FontFamily = App.Instance.IconFont;
            if (OmniBoxOverrideSearch == null)
            {
                if (OmniBox.Text.StartsWith("search:", StringComparison.Ordinal))
                {
                    SiteInformationIcon.Text = "\xE721";
                    SiteInformationText.Text = "Search";
                    SiteInformationPopupButton.ToolTip = $"Searching: {OmniBox.Text.Substring(7).Trim()}";
                }
                else if (OmniBox.Text.StartsWith("domain:", StringComparison.Ordinal))
                {
                    SiteInformationIcon.Text = "\xE71B";
                    SiteInformationText.Text = "Address";
                    SiteInformationPopupButton.ToolTip = $"Address: {OmniBox.Text.Substring(7).Trim()}";
                }
                else if (Utils.IsProgramUrl(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE756";
                    SiteInformationText.Text = "Program";
                    SiteInformationPopupButton.ToolTip = $"Open program: {OmniBox.Text}";
                }
                else if (Utils.IsCode(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE943";
                    SiteInformationText.Text = "Code";
                    SiteInformationPopupButton.ToolTip = $"Code: {OmniBox.Text}";
                }
                else if (Utils.IsUrl(OmniBox.Text))
                {
                    SiteInformationIcon.Text = "\xE71B";
                    SiteInformationText.Text = "Address";
                    SiteInformationPopupButton.ToolTip = $"Address: {OmniBox.Text}";
                }
                else
                {
                    SiteInformationIcon.Text = "\xE721";
                    SiteInformationText.Text = "Search";
                    SiteInformationPopupButton.ToolTip = $"Searching: {OmniBox.Text}";
                }
                SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
            }
            else
            {
                switch (OmniBoxOverrideSearch.Name)
                {
                    case "YouTube":
                        SiteInformationIcon.Text = "\xE786";
                        SiteInformationIcon.Foreground = App.Instance.RedColor;
                        break;
                    case "ChatGPT":
                        SiteInformationIcon.Text = "\xe713";
                        SiteInformationIcon.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#10A37F");
                        break;
                    case "Perplexity":
                        SiteInformationIcon.Text = "\xedad";
                        SiteInformationIcon.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#21808D");
                        break;
                    case "Wikipedia":
                        SiteInformationIcon.Text = "\xe8f1";
                        SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                        break;
                    default:
                        SiteInformationIcon.Text = "\xE721";
                        SiteInformationIcon.Foreground = (SolidColorBrush)FindResource("FontBrush");
                        break;
                }
                SiteInformationText.Text = OmniBoxOverrideSearch.Name;
                SiteInformationPopupButton.ToolTip = $"Searching {OmniBoxOverrideSearch.Name}: {OmniBox.Text.Trim()}";
            }
        }

        private void OmniBox_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (OmniBox.IsKeyboardFocusWithin)
            {
                try
                {
                    if (OmniBox.Text == Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(OmniBox.Tag.ToString())))
                        OmniBox.Text = OmniBox.Tag.ToString();
                }
                catch { }
                OmniBoxBorder.BorderThickness = new Thickness(2);
                OmniBoxBorder.BorderBrush = (SolidColorBrush)FindResource("IndicatorBrush");
                OmniBoxFocused = true;
                OmniBoxPlaceholder.Visibility = Visibility.Hidden;
                if (!OmniBoxHovered)
                    SiteInformationText.Visibility = Visibility.Visible;
            }
            else
            {
                OmniBox.IsDropDownOpen = false;
                try
                {
                    if (OmniBox.Text.Trim().Length == 0)
                        OmniBoxPlaceholder.Visibility = Visibility.Visible;
                    if (Utils.CleanUrl(OmniBox.Text) == Utils.CleanUrl(OmniBox.Tag.ToString()))
                        OmniBox.Text = Utils.ConvertUrlToReadableUrl(App.Instance._IdnMapping, Utils.CleanUrl(OmniBox.Tag.ToString()));
                }
                catch { }
                OmniBoxBorder.BorderThickness = new Thickness(1);
                OmniBoxBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
                OmniBoxFocused = false;
                if (!OmniBoxHovered)
                    SiteInformationText.Visibility = Visibility.Collapsed;
            }
        }

        bool OmniBoxFocused;
        bool OmniBoxHovered;

        Size MaximizedSize = Size.Empty;
        private void CoreContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size NewSize = new Size(CoreContainerSizeEmulator.ActualWidth, CoreContainerSizeEmulator.ActualHeight);
            if (MaximizedSize == Size.Empty)
                MaximizedSize = NewSize;
            Size Percentage = new Size(NewSize.Width / MaximizedSize.Width, NewSize.Height / MaximizedSize.Height);

            SizeEmulatorColumn1.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorColumn2.MaxWidth = 900 * Percentage.Width;
            SizeEmulatorRow1.MaxHeight = 400 * Percentage.Height;
            SizeEmulatorRow2.MaxHeight = 400 * Percentage.Height;

            SizeEmulatorColumn1.Width = new GridLength(0);
            SizeEmulatorColumn2.Width = new GridLength(0);
            SizeEmulatorRow1.Height = new GridLength(0);
            SizeEmulatorRow2.Height = new GridLength(0);
        }

        public async void SetAppearance(Theme _Theme)
        {
            SetFavouritesBarVisibility();
            HomeButton.Visibility = App.Instance.AllowHomeButton ? Visibility.Visible : Visibility.Collapsed;
            QRButton.Visibility = App.Instance.AllowQRButton ? Visibility.Visible : Visibility.Collapsed;
            WebEngineButton.Visibility = App.Instance.AllowWebEngineButton ? Visibility.Visible : Visibility.Collapsed;
            if (!IsLoading)
                TranslateButton.Visibility = !Private && App.Instance.AllowTranslateButton && !Address.StartsWith("slbr:", StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;

            if (WebView != null && WebView.IsBrowserInitialized)
            {
                await WebView?.CallDevToolsAsync("Emulation.setAutoDarkModeOverride", new
                {
                    enabled = _Theme.DarkWebPage
                });
                ReaderModeButton.Visibility = App.Instance.AllowReaderModeButton ? (WebView.CanExecuteJavascript && (await IsArticle()) ? Visibility.Visible : Visibility.Collapsed) : Visibility.Collapsed;
            }
            else
                ReaderModeButton.Visibility = Visibility.Collapsed;

            if (App.Instance.ShowExtensionButton == 0)
                ExtensionsButton.Visibility = App.Instance.Extensions.Any() ? Visibility.Visible : Visibility.Collapsed;
            else if (App.Instance.ShowExtensionButton == 1)
                ExtensionsButton.Visibility = Visibility.Visible;
            else
                ExtensionsButton.Visibility = Visibility.Collapsed;

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        private bool Disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                if (Disposing)
                {
                    DisposeBrowserCore();
                    LoadingStoryboard.Remove();
                    DownloadsPopup.ItemsSource = null;
                    FavouritesPanel.ItemsSource = null;
                    FavouriteListMenu.ItemsSource = null;
                    HistoryListMenu.Collection = null;
                    ExtensionsMenu.ItemsSource = null;
                }
                Disposed = true;
            }
        }

        ~Browser()
        {
            Dispose(false);
        }

        public void DisposeBrowserCore()
        {
            SmartSuggestionCancellation?.Cancel();
            OmniBoxFastTimer?.Stop();
            OmniBoxSmartTimer?.Stop();

            ToggleSideBar(true);
            CoreContainer.Children.Clear();
            SideBarCoreContainer.Children.Clear();

            //_RequestHandler = null;
            //_ResourceRequestHandlerFactory = null;
            if (WebView != null)
            {
                if (WebView.IsBrowserInitialized)
                    Address = WebView.Address;
                WebView?.IsBrowserInitializedChanged -= WebView_IsBrowserInitializedChanged;
                //WebView?.Control.PreviewMouseWheel -= Chromium_PreviewMouseWheel;

                WebView?.FaviconChanged -= WebView_FaviconChanged;
                WebView?.AuthenticationRequested -= WebView_AuthenticationRequested;
                WebView?.BeforeNavigation -= WebView_BeforeNavigation;
                WebView?.ContextMenuRequested -= WebView_ContextMenuRequested;
                WebView?.ExternalProtocolRequested -= WebView_ExternalProtocolRequested;
                //WebView?.FindResult -= WebView_FindResult;
                WebView?.FrameLoadStart -= WebView_FrameLoadStart;
                WebView?.FullscreenChanged -= WebView_FullscreenChanged;
                WebView?.JavaScriptMessageReceived -= WebView_JavaScriptMessageReceived;
                WebView?.LoadingStateChanged -= WebView_LoadingStateChanged;
                WebView?.NavigationError -= WebView_NavigationError;
                WebView?.NewTabRequested -= WebView_NewTabRequested;
                WebView?.PermissionRequested -= WebView_PermissionRequested;
                WebView?.ResourceLoaded -= WebView_ResourceLoaded;
                WebView?.ResourceRequested -= WebView_ResourceRequested;
                //WebView?.ResourceResponded -= WebView_ResourceResponded;
                WebView?.ScriptDialogOpened -= WebView_ScriptDialogOpened;
                WebView?.StatusMessage -= WebView_StatusMessage;
                WebView?.TitleChanged -= WebView_TitleChanged;
                /*Chromium.LifeSpanHandler = null;
                Chromium.DownloadHandler = null;
                Chromium.RequestHandler = null;
                Chromium.MenuHandler = null;
                Chromium.KeyboardHandler = null;
                Chromium.JsDialogHandler = null;
                Chromium.PermissionHandler = null;
                Chromium.DialogHandler = null;
                Chromium.ResourceRequestHandlerFactory = null;
                Chromium.DisplayHandler = null;*/

                /* Chromium.IsBrowserInitializedChanged -= Chromium_IsBrowserInitializedChanged;
                 Chromium.FrameLoadStart -= Chromium_FrameLoadStart;
                 Chromium.LoadingStateChanged -= Chromium_LoadingStateChanged;
                 Chromium.TitleChanged -= Chromium_TitleChanged;
                 Chromium.StatusMessage -= Chromium_StatusMessage;
                 Chromium.LoadError -= Chromium_LoadError;
                 Chromium.PreviewMouseWheel -= Chromium_PreviewMouseWheel;
                 Chromium.JavascriptMessageReceived -= Chromium_JavascriptMessageReceived;*/

                WebView?.Dispose();
            }
            _Settings?.Dispose();
            _Settings = null;
            WebView = null;
            //GC.Collect(GC.MaxGeneration);
            //GC.SuppressFinalize(this);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void InspectorDockDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Action(Actions.SetSideBarDock, null, (3 - SideBarDockDropdown.SelectedIndex).ToString());
        }

        private ObservableCollection<OmniSuggestion> Suggestions = new ObservableCollection<OmniSuggestion>();
        private DispatcherTimer OmniBoxFastTimer;
        private DispatcherTimer OmniBoxSmartTimer;
        bool OmniBoxIsDropdown = false;

        private CancellationTokenSource? SmartSuggestionCancellation;

        private async void OmniBoxFastTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxFastTimer.Stop();
            string CurrentText = OmniBox.Text;
            SolidColorBrush IconColor = (SolidColorBrush)FindResource("FontBrush");
            if (OmniBoxOverrideSearch != null)
            {
                switch (OmniBoxOverrideSearch.Name)
                {
                    case "YouTube":
                        IconColor = App.Instance.RedColor;
                        break;
                    case "ChatGPT":
                        IconColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#10A37F");
                        break;
                    case "Perplexity":
                        IconColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#21808D");
                        break;
                }
            }
            Suggestions.Clear();
            OmniBox.Text = CurrentText;
            Suggestions.Add(App.GenerateSuggestion(CurrentText, App.GetMiniSearchType(CurrentText), IconColor));
            try
            {
                string SuggestionsUrl = string.Format((OmniBoxOverrideSearch ?? App.Instance.DefaultSearchProvider).SuggestUrl, Uri.EscapeDataString(CurrentText));
                if (string.IsNullOrEmpty(SuggestionsUrl))
                {
                    string ResponseText = await App.MiniHttpClient.GetStringAsync(SuggestionsUrl);
                    /*if (Search.Name == "YouTube")
                    {
                        ResponseText = Utils.RemovePrefix(ResponseText, "window.google.ac.h(");
                        ResponseText = Utils.RemovePrefix(ResponseText, ")", false, true);
                    }*/
                    using (JsonDocument Document = JsonDocument.Parse(ResponseText))
                    {
                        foreach (JsonElement Suggestion in Document.RootElement[1].EnumerateArray())
                        {
                            string SuggestionStr = Suggestion.GetString();
                            Suggestions.Add(App.GenerateSuggestion(SuggestionStr, App.GetMiniSearchType(SuggestionStr), IconColor));
                        }
                    }
                }
            }
            catch { }
            OmniBox.IsDropDownOpen = Suggestions.Count > 0;
            OmniBoxIsDropdown = true;

            OmniBox.Focus();
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;

            if (OmniBox.Text.Length != 0 && OmniBoxOverrideSearch == null)
            {
                foreach (SearchProvider Search in App.Instance.SearchEngines)
                {
                    if (Search.Name.StartsWith(OmniBox.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        OmniBoxStatus.Tag = App.Instance.SearchEngines.IndexOf(Search);
                        OmniBoxStatusText.Text = $"Search {Search.Name}";
                        OmniBoxStatus.Visibility = Visibility.Visible;
                        return;
                    }
                }
            }
            OmniBoxStatus.Visibility = Visibility.Collapsed;
        }

        private async void OmniBoxSmartTimer_Tick(object? sender, EventArgs e)
        {
            OmniBoxSmartTimer.Stop();
            if (!OmniBox.IsDropDownOpen)
                return;
            string Text = OmniBox.Text.Trim();
            string Type = App.GetSmartType(Text);
            if (Type == "None")
                return;
            SmartSuggestionCancellation?.Cancel();
            SmartSuggestionCancellation = new CancellationTokenSource();
            var Token = SmartSuggestionCancellation.Token;

            OmniSuggestion Suggestion = await App.Instance.GenerateSmartSuggestion(Text, Type, Token);
            if (!Token.IsCancellationRequested)
            {
                Suggestions.RemoveAt(0);
                Suggestions.Insert(0, Suggestion);
            }
        }

        private void OmniBox_DropDownOpened(object sender, EventArgs e)
        {
            WebView?.Control.Focusable = false;
            OmniBoxPopup.HorizontalOffset = -(SiteInformationPanel.ActualWidth + 8);// + 4 + 4
            OmniBoxPopupDropDown.Width = OmniBoxContainer.ActualWidth;
        }

        private void OmniBox_DropDownClosed(object sender, EventArgs e)
        {
            WebView?.Control.Focusable = true;
        }

        int CaretIndex = 0;

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Browser_Loaded;
            OmniTextBox = OmniBox.Template.FindName("PART_EditableTextBox", OmniBox) as TextBox;
            OmniTextBox.PreviewKeyDown += (sender, args) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CaretIndex = OmniTextBox.CaretIndex;
                }), DispatcherPriority.Input);
            };

            OmniTextBox.GotKeyboardFocus += (sender, args) =>
            {
                args.Handled = true;
                OmniTextBox.CaretIndex = CaretIndex;
                if (!OmniBoxIsDropdown)
                    OmniTextBox.SelectAll();
            };
            OmniBoxPopup = OmniBox.Template.FindName("Popup", OmniBox) as Popup;
            OmniBoxPopupDropDown = OmniBox.Template.FindName("DropDown", OmniBox) as Grid;
            OmniBox.ItemsSource = Suggestions;
            if (Address == "slbr://newtab" || Address == "about:blank")
            {
                Keyboard.Focus(OmniBox);
                OmniBox.Focus();
            }
        }

        Window ExtensionWindow;
        private void LoadExtensionPopup(object sender, RoutedEventArgs e)
        {
            //TODO: Use PopupBrowser instead
            Extension _Extension = App.Instance.Extensions.ToList().Find(i => i.ID == ((FrameworkElement)sender).Tag.ToString());
            if (_Extension == null)
                return;
            ExtensionWindow = new Window();
            ChromiumWebBrowser ExtensionBrowser = new ChromiumWebBrowser(_Extension.Popup);
            ExtensionBrowser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            HwndSource _HwndSource = HwndSource.FromHwnd(new WindowInteropHelper(ExtensionWindow).EnsureHandle());
            _HwndSource.AddHook(WndProc);
            ExtensionBrowser.LoadingStateChanged += (s, args) =>
            {
                if (!args.IsLoading)
                    ExtensionBrowser.ExecuteScriptAsync(Scripts.ExtensionScript);
            };
            int trueValue = 0x01;
            int falseValue = 0x00;
            DllUtils.DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref App.Instance.CurrentTheme.DarkTitleBar ? ref trueValue : ref falseValue, Marshal.SizeOf(typeof(int)));
            DllUtils.DwmSetWindowAttribute(_HwndSource.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            ExtensionBrowser.JavascriptMessageReceived += ExtensionBrowser_JavascriptMessageReceived;
            ExtensionBrowser.SnapsToDevicePixels = true;
            //ExtensionBrowser.MenuHandler = App.Instance._LimitedContextMenuHandler;
            //ExtensionBrowser.DownloadHandler = App.Instance._DownloadHandler;
            //TODO
            ExtensionBrowser.AllowDrop = true;
            ExtensionBrowser.IsManipulationEnabled = true;
            ExtensionWindow.Content = ExtensionBrowser;
            ExtensionWindow.Title = _Extension.Name + " - Extension";
            ExtensionWindow.ResizeMode = ResizeMode.NoResize;
            ExtensionWindow.SizeChanged += ExtensionWindow_SizeChanged;
            ExtensionWindow.MaxHeight = 700;
            ExtensionWindow.MaxWidth = 700;
            ExtensionWindow.ShowDialog();
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DllUtils.WM_SYSCOMMAND:
                    int Command = wParam.ToInt32() & 0xfff0;
                    if (Command == DllUtils.SC_MOVE)
                        handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        private void ExtensionWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Rect WorkArea = SystemParameters.WorkArea;
            ExtensionWindow.Left = (WorkArea.Width - ExtensionWindow.Width) / 2 + WorkArea.Left;
            ExtensionWindow.Top = (WorkArea.Height - ExtensionWindow.Height) / 2 + WorkArea.Top;
        }

        private void ExtensionBrowser_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                dynamic data = e.Message;
                ExtensionWindow.Height = data.height;
                ExtensionWindow.Width = data.width;
            });
        }

        private void DownloadCancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Instance.Downloads.GetValueOrDefault(((FrameworkElement)sender).Tag.ToString())?.Cancel();
            }
            catch { }
        }

        private void DownloadOpenButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{App.Instance.Downloads.GetValueOrDefault(((FrameworkElement)sender).Tag.ToString()).FullPath}\"") { UseShellExecute = true });
        }

        private void FavouriteButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
                Tab.ParentWindow.NewTab(Values[1], false, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
            }
        }
    }
}