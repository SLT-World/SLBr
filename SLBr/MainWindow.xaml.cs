using SLBr.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinUI;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public int ID;

        private ObservableCollection<BrowserTabItem> _Tabs = new ObservableCollection<BrowserTabItem>();
        public ObservableCollection<BrowserTabItem> Tabs
        {
            get { return _Tabs; }
            set
            {
                _Tabs = value;
                RaisePropertyChanged("Tabs");
            }
        }

        public MainWindow()
        {
            InitializeWindow();
        }

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case MessageHelper.WM_COPYDATA:
                    COPYDATASTRUCT _dataStruct = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    string Data = Marshal.PtrToStringUni(_dataStruct.lpData, _dataStruct.cbData / 2);
                    List<string> Datas = Data.Split("<|>").ToList();
                    if (Datas[0] == "Url")
                        NewTab(Datas[1], true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                    else if (Datas[0] == "Start")
                    {
                        if (App.Instance.Background && App.Instance.Username == Datas[1])
                            App.Instance.ContinueBackgroundInitialization();
                    }
                    SetForegroundWindow(new WindowInteropHelper(this).Handle);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        BrowserTabItem NewTabTab = null;

        private void InitializeWindow()
        {
            Title = App.Instance.Username == "Default" ? "SLBr" : $"{App.Instance.Username} - SLBr";
            if (App.Instance.Icon != null)
                Icon = App.Instance.Icon;
            ID = Utils.GenerateRandomId();

            HwndSource HwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            HwndSource.AddHook(new HwndSourceHook(WndProc));
            int trueValue = 0x01;
            DwmSetWindowAttribute(HwndSource.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));

            App.Instance.AllWindows.Add(this);
            if (App.Instance.WindowsSaves.Count < App.Instance.AllWindows.Count)
                App.Instance.WindowsSaves.Add(new Saving($"Window_{App.Instance.WindowsSaves.Count}.bin", App.Instance.UserApplicationWindowsPath));
            InitializeComponent();
            UpdateUnloadTimer();
            NewTabTab = new BrowserTabItem(null)
            {
                TabStyle = (Style)FindResource("IconTabButton")
            };
            Tabs.Add(NewTabTab);
            TabsUI.ItemsSource = Tabs;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ExecuteCloseEvent();
        }

        /*static readonly string[] TestUrls = {
            "https://ads.google.com/pagead.js",
            "https://example.com/js/ads.js",
            "https://googleads.g.doubleclick.net/pagead/js/ads.js",
            "https://safe.site.com/script.js",
            "https://cdn.doubleclick.net/script.js"
        };

        static readonly string[] AdIndicators = {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };

        static readonly FastHashSet<string> HasInLink = new FastHashSet<string> {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };

        static readonly HashSet<string> HasInLinkSlow = new HashSet<string> {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };


        static bool IndexOfOrdinalMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        static bool ContainsOrdinalMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        static bool IndexOfMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.IndexOf(pattern) >= 0)
                    return true;
            return false;
        }

        static bool ContainsMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        static bool LowercaseContains(string url)//Fastest in Release mode
        {
            string lower = url.ToLowerInvariant();
            foreach (var pattern in AdIndicators)
                if (lower.Contains(pattern))
                    return true;
            return false;
        }

        static bool HashSetContains(string url)
        {
            foreach (var pattern in HasInLinkSlow)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        static bool FastHashSetContains(string url)
        {
            foreach (var pattern in HasInLink)
                if (url.Contains(pattern))
                    return true;
            return false;
        }*/
        //const int Iterations = 5_000_000;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.GetInt("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("CompactTab")), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")), App.Instance.GlobalSave.GetInt("ExtensionButton"), App.Instance.GlobalSave.GetInt("FavouritesBar"));

            //Benchmark.Clear();
            /*Benchmark.Run("FastHost", Iterations, () =>
            {
                _ = Utils.FastHost("https://googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("Host", Iterations, () =>
            {
                _ = Utils.Host("https://googleads.g.doubleclick.net/pagead/js/ads.js");
            });*/
            /*Benchmark.Run("IndexOf OrdinalIgnoreCase", Iterations, () =>
            {
                _ = IndexOfOrdinalMethod(TestUrls[3]);
            });
            Benchmark.Run("Contains OrdinalIgnoreCase", Iterations, () =>
            {
                _ = ContainsOrdinalMethod(TestUrls[3]);
            });
            Benchmark.Run("IndexOf", Iterations, () =>
            {
                _ = IndexOfMethod(TestUrls[3]);
            });
            Benchmark.Run("Contains", Iterations, () =>
            {
                _ = ContainsMethod(TestUrls[3]);
            });
            Benchmark.Run("Lowercase + contains", Iterations, () =>
            {
                _ = LowercaseContains(TestUrls[3]);
            });
            Benchmark.Run("HashSet", Iterations, () =>
            {
                _ = HashSetContains(TestUrls[3]);
            });
            Benchmark.Run("FastHashSet", Iterations, () =>
            {
                _ = FastHashSetContains(TestUrls[3]);
            });*/
            //MessageBox.Show(Benchmark.Report());
        }
        public DispatcherTimer GCTimer;

        private DateTime GCTimerStartTime;
        private int GCTimerDuration;

        public void UpdateUnloadTimer()
        {
            if (bool.Parse(App.Instance.GlobalSave.Get("TabUnloading")))
            {
                GCTimer?.Stop();
                GCTimerDuration = App.Instance.GlobalSave.GetInt("TabUnloadingTime");
                GCTimer = new DispatcherTimer();

                if (bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress")))
                {
                    foreach (BrowserTabItem _Tab in Tabs)
                        _Tab.ProgressBarVisibility = _Tab.IsUnloaded ? Visibility.Collapsed : Visibility.Visible;
                    GCTimer.Tick += GCCollect_Tick;
                    GCTimer.Interval = TimeSpan.FromMilliseconds(100);
                    GCTimerStartTime = DateTime.Now;
                }
                else
                {
                    foreach (BrowserTabItem _Tab in Tabs)
                    {
                        _Tab.ProgressBarVisibility = Visibility.Collapsed;
                        if (_Tab.Content != null && _Tab.Content._Settings != null)
                            _Tab.Content._Settings.UnloadProgressBar.Value = 0;
                    }
                    GCTimer.Tick += GCCollect_EfficientTick;
                    GCTimer.Interval = new TimeSpan(0, GCTimerDuration, 0);
                }
                GCTimer.Start();
            }
            else
            {
                GCTimer?.Stop();
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    _Tab.ProgressBarVisibility = Visibility.Collapsed;
                    if (!_Tab.IsUnloaded && _Tab.Content != null && _Tab.Content._Settings != null)
                            _Tab.Content._Settings.UnloadProgressBar.Value = 0;
                }
            }
        }

        private void GCCollect_Tick(object sender, EventArgs e)
        {
            double Progress = (DateTime.Now - GCTimerStartTime).TotalSeconds / (GCTimerDuration * 60);
            if (Progress >= 1)
            {
                GCTimerStartTime = DateTime.Now;
                UnloadTabs();
            }
            if (WindowState != WindowState.Minimized)
            {
                double VisualProgress = Math.Min(Progress, 1) * 100;
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    if (_Tab.IsUnloaded)
                        _Tab.ProgressBarVisibility = Visibility.Collapsed;
                    else
                    {
                        _Tab.Progress = VisualProgress;
                        if (_Tab.Content != null && _Tab.Content._Settings != null)
                            _Tab.Content._Settings.UnloadProgressBar.Value = VisualProgress;
                    }
                }
            }
        }
        private void GCCollect_EfficientTick(object sender, EventArgs e)
        {
            UnloadTabs();
        }

        bool VerticalTabs = false;

        public void SetAppearance(Theme _Theme, int TabAlignment, bool CompactTab, bool AllowHomeButton, bool AllowTranslateButton, bool AllowReaderModeButton, int ShowExtensionButton, int ShowFavouritesBar)
        {
            if (TabAlignment == 0)
            {
                VerticalTabs = false;
                TabsUI.Style = FindResource(typeof(WinUITabControl)) as Style;
                TabsUI.Resources[typeof(TabItem)] = FindResource(typeof(TabItem)) as Style;
                NewTabTab.TabStyle = (Style)FindResource("IconTabButton");
                Tabs.Remove(NewTabTab);
                Tabs.Add(NewTabTab);
            }
            else
            {
                VerticalTabs = true;
                TabsUI.Style = Resources["VerticalTabControl"] as Style;
                TabsUI.Resources[typeof(TabItem)] = CompactTab ? (Style)FindResource("MinimizedVerticalTab") : (Style)FindResource("VerticalTab");
                NewTabTab.TabStyle = CompactTab ? (Style)FindResource("MinimizedVerticalIconTabButton") : (Style)FindResource("VerticalIconTabButton");
                Tabs.Remove(NewTabTab);
                Tabs.Insert(0, NewTabTab);
            }

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;

            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.SetAppearance(_Theme, AllowHomeButton, AllowTranslateButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);

            HwndSource HwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (App.Instance.CurrentTheme.DarkTitleBar)
                DwmSetWindowAttribute(HwndSource.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DwmSetWindowAttribute(HwndSource.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            var Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>", StringSplitOptions.None);
            Action((Actions)int.Parse(Values[0]), (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }

        private void Action(Actions _Action, string V1 = "", string V2 = "", string V3 = "")
        {
            Browser _BrowserView = GetTab().Content;
            if (_BrowserView != null)
                V1 = V1.Replace("{CurrentUrl}", _BrowserView.Address);
            V1 = V1.Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));

            switch (_Action)
            {
                case Actions.Exit:
                    App.Instance.CloseSLBr(true);
                    break;

                case Actions.Undo:
                    Undo(V1);
                    break;
                case Actions.Redo:
                    Redo(V1);
                    break;
                case Actions.Refresh:
                    Refresh(V1);
                    break;
                case Actions.Navigate:
                    Navigate(V1);
                    break;

                case Actions.CreateTab:
                    if (V2 == "Tab")
                    {
                        BrowserTabItem _Tab = GetBrowserTabWithId(int.Parse(V1));
                        NewTab(_Tab.Content.Address, true, Tabs.IndexOf(_Tab) + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                    }
                    else if (V2 == "Private")
                        NewTab(V1, true, -1, true);
                    else
                        NewTab(V1, true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                    break;
                case Actions.CloseTab:
                    CloseTab(int.Parse(V1), int.Parse(V2));
                    break;
                case Actions.NewWindow:
                    App.Instance.NewWindow();
                    break;
                case Actions.UnloadTab:
                    ForceUnloadTab(int.Parse(V1));
                    break;

                case Actions.DevTools:
                    DevTools(V1);
                    break;
                case Actions.Favourite:
                    Favourite(V1);
                    break;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
                Tabs[TabsUI.SelectedIndex].Content?.ReFocus();
        }

        public void UnloadTabs()
        {
            if (WindowState == WindowState.Minimized)
            {
                foreach (BrowserTabItem Tab in Tabs)
                {
                    if (Tab.Content != null)
                        UnloadTab(Tab.Content);
                }
            }
            else
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    if (i != TabsUI.SelectedIndex)
                    {
                        BrowserTabItem Tab = Tabs[i];
                        if (Tab.Content != null)
                            UnloadTab(Tab.Content);
                    }
                }
            }
            /*BrowserTabItem SelectedTab = Tabs[TabsUI.SelectedIndex];
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (WindowState == WindowState.Minimized || Tab != SelectedTab)
                {
                    if (Tab.Content != null)
                        UnloadTab(Tab.Content);
                }
            }*/
        }
        public void ForceUnloadTab(int Id)
        {
            BrowserTabItem _Tab = GetBrowserTabWithId(Id);
            if (_Tab.Content != null)
                UnloadTab(_Tab.Content, true);
        }
        private void UnloadTab(Browser BrowserView, bool Bypass = false)
        {
            if (Bypass || BrowserView.CanUnload())
                BrowserView.Unload();
        }
        public void Favourite(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            _Tab.Content?.Favourite();
        }
        public void Undo(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (_Tab.Content.CanGoBack)
                _Tab.Content.Back();
        }
        public void Redo(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (_Tab.Content.CanGoForward)
                _Tab.Content.Forward();
        }
        public void Refresh(string Id = "", bool IgnoreCache = false)
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (!_Tab.Content.IsLoading)
                _Tab.Content.Reload(IgnoreCache);
            else
                _Tab.Content.Stop();
        }
        public void Navigate(string Url)
        {
            GetTab().Content?.Navigate(Url);
        }
        public bool IsFullscreen;
        private DispatcherTimer FullscreenPopupTimer;
        public void Fullscreen(bool Fullscreen, Browser BrowserView = null)
        {
            IsFullscreen = Fullscreen;
            if (BrowserView == null)
                BrowserView = GetTab().Content;
            if (BrowserView != null)
            {
                if (Fullscreen)
                {
                    if (BrowserView.Chromium != null)
                    {
                        BrowserView.CoreContainer.Children.Remove(BrowserView.Chromium);
                        FullscreenContainer.Children.Add(BrowserView.Chromium);
                        FullscreenContainer.Visibility = Visibility.Visible;
                        Keyboard.Focus(BrowserView.Chromium);
                    }
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    if (bool.Parse(App.Instance.GlobalSave.Get("FullscreenPopup")))
                    {
                        FullscreenPopup.IsOpen = true;
                        FullscreenPopupTimer = new DispatcherTimer();
                        FullscreenPopupTimer.Interval = TimeSpan.FromSeconds(3);
                        FullscreenPopupTimer.Tick += FullscreenPopupTimer_Tick;
                        FullscreenPopupTimer.Start();
                    }
                }
                else
                {
                    if (BrowserView.Chromium != null)
                    {
                        FullscreenContainer.Visibility = Visibility.Collapsed;
                        FullscreenContainer.Children.Remove(BrowserView.Chromium);
                        BrowserView.CoreContainer.Children.Add(BrowserView.Chromium);
                        Keyboard.Focus(BrowserView.Chromium);
                    }
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    FullscreenPopupTimer_Tick(null, null);
                }
            }
        }
        private void FullscreenPopupTimer_Tick(object? sender, EventArgs e)
        {
            FullscreenPopup.IsOpen = false;
            FullscreenPopupTimer?.Stop();
            FullscreenPopupTimer = null;
        }

        public void DevTools(string Id = "")//, int XCoord = 0, int YCoord = 0)
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            _Tab.Content?.DevTools();//(false, XCoord, YCoord);
        }
        public void NewTab(string Url, bool IsSelected = false, int Index = -1, bool IsPrivate = false)
        {
            if (!App.Instance.Background && WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            BrowserTabItem _Tab = new BrowserTabItem(this) { Header = Utils.CleanUrl(Url, true, true, true, true), BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = new Browser(Url, _Tab, IsPrivate);
            if (VerticalTabs)
                Tabs.Insert(Index != -1 ? Index : Tabs.Count, _Tab);
            else
                Tabs.Insert(Index != -1 ? Index : Tabs.Count - 1, _Tab);
            if (IsSelected)
                TabsUI.SelectedIndex = Tabs.IndexOf(_Tab);
        }

        /*public void SwitchToTab(BrowserTabItem _Tab)
        {
            TabsUI.SelectedIndex = Tabs.IndexOf(_Tab);
            if (_Tab.Content != null)
                Keyboard.Focus(_Tab.Content.Chromium);
        }*/
        public BrowserTabItem GetBrowserTabWithId(int Id)
        {
            foreach (BrowserTabItem _Tab in Tabs)
            {
                if (_Tab.ID == Id)
                    return _Tab;
            }
            return null;
        }
        public void CloseTab(int Id, int WindowId)
        {
            if (WindowId != ID)
            {
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    if (_Window.ID == WindowId)
                    {
                        _Window.CloseTab(Id, WindowId);
                        return;
                    }
                }
            }
            BrowserTabItem _Tab = Id == -1 ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(Id);
            if (Tabs.Count > 2)
            {
                bool IsSelected = Id != -1 ? _Tab == Tabs[TabsUI.SelectedIndex] : true;
                _Tab.Content.DisposeCore();
                if (IsSelected)
                {
                    if (VerticalTabs)
                    {
                        if (TabsUI.SelectedIndex > 1)
                            TabsUI.SelectedIndex = TabsUI.SelectedIndex - 1;
                        else
                            TabsUI.SelectedIndex = TabsUI.SelectedIndex + 1;
                    }
                    else
                    {
                        if (TabsUI.SelectedIndex > 0)
                            TabsUI.SelectedIndex = TabsUI.SelectedIndex - 1;
                        else
                            TabsUI.SelectedIndex = TabsUI.SelectedIndex + 1;
                    }
                }
                Tabs.Remove(_Tab);
                if (IsSelected && TabsUI.SelectedIndex > Tabs.Count - 1)
                    TabsUI.SelectedIndex = Tabs.Count - 1;
            }
            else
                Close();
        }
        public void Find(string Text = "")
        {
            GetTab().Content?.Find(Text);
        }
        public void Screenshot()
        {
            GetTab().Content?.Screenshot();
        }

        public void Zoom(int Delta)
        {
            GetTab().Content?.Zoom(Delta);
        }

        public BrowserTabItem GetTab(Browser _Control = null)
        {
            if (_Control != null)
            {
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    if (_Tab.Content == _Control)
                        return _Tab;
                }
                return null;
            }
            else
                return Tabs[TabsUI.SelectedIndex];
        }
        /*public UserControl GetUserControlView(BrowserTabItem Tab = null)
        {
            if (Tab != null)
                return Tab.Content;
            else
                return GetTab().Content;
        }*/
        /*public Browser GetBrowserView(BrowserTabItem Tab = null)
        {
            return Tab.Content;
        }*/

        public void SetDimUnloadedIcon(bool Toggle)
        {
            foreach (BrowserTabItem _Tab in Tabs)
                _Tab.DimUnloadedIcon = Toggle;
        }

        private void TabsUI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Tabs[TabsUI.SelectedIndex].ParentWindow == null)
                {
                    if (TabsUI.Visibility == Visibility.Visible)
                        NewTab(App.Instance.GlobalSave.Get("Homepage"), true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                }
                else
                {
                    BrowserTabItem _CurrentTab = Tabs[TabsUI.SelectedIndex];
                    foreach (BrowserTabItem _Tab in Tabs)
                    {
                        if (_Tab != _CurrentTab)
                            _Tab.Content?.UnFocus();
                    }
                    if (_CurrentTab.Content != null)
                    {
                        _CurrentTab.Content.ReFocus();
                        Keyboard.Focus(_CurrentTab.Content.Chromium);
                    }
                    Title = _CurrentTab.Header + (App.Instance.Username == "Default" ? " - SLBr" : $" - {App.Instance.Username} - SLBr");
                }
            }
            catch
            {
                Title = App.Instance.Username == "Default" ? " - SLBr" : $" - {App.Instance.Username} - SLBr";
            }
        }

        public void ExecuteCloseEvent()
        {
            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.ToggleSideBar(true);
            GCTimer?.Stop();
            if (App.Instance.AllWindows.Count == 1)
                App.Instance.CloseSLBr(false);
            else if (App.Instance.WindowsSaves.Count == App.Instance.AllWindows.Count)
                App.Instance.WindowsSaves.RemoveAt(App.Instance.WindowsSaves.Count - 1);
            App.Instance.AllWindows.Remove(this);
            GC.SuppressFinalize(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            ExecuteCloseEvent();
        }
    }

    public class BrowserTabItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public BrowserTabItem(MainWindow _ParentWindow)
        {
            DimUnloadedIcon = bool.Parse(App.Instance.GlobalSave.Get("DimUnloadedIcon"));
            if (_ParentWindow != null)
            {
                ID = Utils.GenerateRandomId();
                ParentWindow = _ParentWindow;
                ParentWindowID = _ParentWindow.ID;
            }
        }
        public Style TabStyle
        {
            get { return _TabStyle; }
            set
            {
                _TabStyle = value;
                RaisePropertyChanged("TabStyle");
            }
        }
        private Style _TabStyle;

        public bool IsUnloaded
        {
            get { return _IsUnloaded; }
            set
            {
                _IsUnloaded = value;
                RaisePropertyChanged("IsUnloaded");
            }
        }
        private bool _IsUnloaded;
        public bool DimUnloadedIcon
        {
            get { return _DimUnloadedIcon; }
            set
            {
                _DimUnloadedIcon = value;
                RaisePropertyChanged("DimUnloadedIcon");
            }
        }
        private bool _DimUnloadedIcon;
        public string Header
        {
            get { return _Header; }
            set
            {
                _Header = value;
                RaisePropertyChanged("Header");
            }
        }
        private string _Header;
        public BitmapImage Icon
        {
            get { return _Icon; }
            set
            {
                _Icon = value;
                RaisePropertyChanged("Icon");
            }
        }
        private BitmapImage _Icon;
        public Browser Content { get; set; }
        public MainWindow ParentWindow { get; set; }
        public int ID
        {
            get { return _ID; }
            set
            {
                FavouriteCommandHeader = "Add to favourites";
                _ID = value;
                RaisePropertyChanged("ID");
            }
        }
        private int _ID;
        public int ParentWindowID
        {
            get { return _ParentWindowID; }
            set
            {
                DuplicateCommand = $"5<,>{ID}<,>Tab";
                RefreshCommand = $"3<,>{ID}";
                AddToFavouritesCommand = $"12<,>{ID}";
                CloseCommand = $"6<,>{ID}<,>{value}";
                UnloadCommand = $"8<,>{ID}";
                _ParentWindowID = value;
                RaisePropertyChanged("ParentWindowID");
            }
        }
        private int _ParentWindowID;

        public double Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                RaisePropertyChanged("Progress");
            }
        }
        private double _Progress = 0;

        public Visibility ProgressBarVisibility
        {
            get { return _ProgressBarVisibility; }
            set
            {
                _ProgressBarVisibility = value;
                RaisePropertyChanged("ProgressBarVisibility");
            }
        }
        private Visibility _ProgressBarVisibility;
        public Visibility BrowserCommandsVisibility
        {
            get { return _BrowserCommandsVisibility; }
            set
            {
                _BrowserCommandsVisibility = value;
                RaisePropertyChanged("BrowserCommandsVisibility");
            }
        }
        private Visibility _BrowserCommandsVisibility;
        public string DuplicateCommand
        {
            get { return _DuplicateCommand; }
            set
            {
                _DuplicateCommand = value;
                RaisePropertyChanged("DuplicateCommand");
            }
        }
        private string _DuplicateCommand;
        public string RefreshCommand
        {
            get { return _RefreshCommand; }
            set
            {
                _RefreshCommand = value;
                RaisePropertyChanged("RefreshCommand");
            }
        }
        private string _RefreshCommand;
        public string UnloadCommand
        {
            get { return _UnloadCommand; }
            set
            {
                _UnloadCommand = value;
                RaisePropertyChanged("UnloadCommand");
            }
        }
        private string _UnloadCommand;
        public string AddToFavouritesCommand
        {
            get { return _AddToFavouritesCommand; }
            set
            {
                _AddToFavouritesCommand = value;
                RaisePropertyChanged("AddToFavouritesCommand");
            }
        }
        private string _AddToFavouritesCommand;
        public string CloseCommand
        {
            get { return _CloseCommand; }
            set
            {
                _CloseCommand = value;
                RaisePropertyChanged("CloseCommand");
            }
        }
        private string _CloseCommand;
        public string FavouriteCommandHeader
        {
            get { return _FavouriteCommandHeader; }
            set
            {
                _FavouriteCommandHeader = value;
                RaisePropertyChanged("FavouriteCommandHeader");
            }
        }
        private string _FavouriteCommandHeader;
    }

    public class TabItemStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var _TabItem = item as BrowserTabItem;
            if (_TabItem != null)
            {
                if (Application.Current.MainWindow != null)
                    return _TabItem.TabStyle;
            }
            return base.SelectStyle(item, container);
        }
    }
}