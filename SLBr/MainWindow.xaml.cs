using SLBr.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
                    string _strMsg = Marshal.PtrToStringUni(_dataStruct.lpData, _dataStruct.cbData / 2);
                    NewTab(_strMsg, true);
                    if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                    SetForegroundWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        public void UpdateMica()
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (App.Instance.CurrentTheme.DarkTitleBar)
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        private void InitializeWindow()
        {
            Title = App.Instance.Username == "Default" ? "SLBr" : $"{App.Instance.Username} - SLBr";
            ID = Utils.GenerateRandomId();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            source.AddHook(new HwndSourceHook(WndProc));
            App.Instance.AllWindows.Add(this);
            if (App.Instance.WindowsSaves.Count < App.Instance.AllWindows.Count)
                App.Instance.WindowsSaves.Add(new Saving($"Window_{App.Instance.WindowsSaves.Count}.bin", App.Instance.UserApplicationWindowsPath));
            InitializeComponent();
            Tabs.Add(new BrowserTabItem(null)
            {
                TabStyle = (Style)FindResource("VerticalIconTabButton")
            });
            TabsUI.ItemsSource = Tabs;
            UpdateUnloadTimer();
            App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ExecuteCloseEvent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
        }

        public DispatcherTimer GCTimer;

        private DateTime GCTimerStartTime;
        private int GCTimerDuration;

        public void UpdateUnloadTimer()
        {
            if (bool.Parse(App.Instance.GlobalSave.Get("TabUnloading")))
            {
                GCTimerDuration = int.Parse(App.Instance.GlobalSave.Get("TabUnloadingTime"));
                if (GCTimer != null)
                    GCTimer.Stop();
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
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    _Tab.ProgressBarVisibility = Visibility.Collapsed;
                    if (!_Tab.IsUnloaded && _Tab.Content != null && _Tab.Content._Settings != null)
                            _Tab.Content._Settings.UnloadProgressBar.Value = 0;
                }
                if (GCTimer != null)
                    GCTimer.Stop();
            }
        }

        private void GCCollect_Tick(object sender, EventArgs e)
        {
            TimeSpan Elapsed = DateTime.Now - GCTimerStartTime;
            double Progress = (Elapsed.TotalSeconds / (GCTimerDuration * 60)) * 1;
            if (Progress >= 1)
            {
                GCTimerStartTime = DateTime.Now;
                UnloadTabs();
            }
            double VisualProgress = Math.Min(Progress, 1) * 100;
            foreach (BrowserTabItem _Tab in Tabs)
            {
                if (!_Tab.IsUnloaded)
                {
                    _Tab.Progress = VisualProgress;
                    if (_Tab.Content != null && _Tab.Content._Settings != null)
                        _Tab.Content._Settings.UnloadProgressBar.Value = VisualProgress;
                }
                else
                    _Tab.ProgressBarVisibility = Visibility.Collapsed;
            }
        }
        private void GCCollect_EfficientTick(object sender, EventArgs e)
        {
            UnloadTabs();
        }

        public void SetAppearance(Theme _Theme, string TabAlignment, bool AllowHomeButton, bool AllowTranslateButton, bool AllowAIButton, bool AllowReaderModeButton)
        {
            if (TabAlignment == "Vertical")
            {
                TabsUI.Style = Resources["VerticalTabControl"] as Style;
                Tabs[Tabs.Count - 1].TabStyle = (Style)FindResource("VerticalIconTabButton");
            }
            else if (TabAlignment == "Horizontal")
            {
                TabsUI.Style = FindResource(typeof(WinUITabControl)) as Style;
                Tabs[Tabs.Count - 1].TabStyle = (Style)FindResource("IconTabButton");
            }

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;

            foreach (BrowserTabItem Tab in Tabs)
            {
                if (Tab.Content != null)
                    Tab.Content.SetAppearance(_Theme, AllowHomeButton, AllowTranslateButton, AllowAIButton, AllowReaderModeButton);
            }
            //Why did I have this?
            /*WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowStyle = WindowStyle.SingleBorderWindow;*/
            UpdateMica();
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;
            var Values = ((FrameworkElement)sender).Tag.ToString().Split(new string[] { "<,>" }, StringSplitOptions.None);
            Action((Actions)int.Parse(Values[0]), sender, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
        }

        private void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            try
            {
                Browser _BrowserView = GetTab().Content;
                if (_BrowserView != null)
                {
                    V1 = V1.Replace("{CurrentUrl}", _BrowserView.Address);
                    V1 = V1.Replace("{CurrentInspectorUrl}", _BrowserView.Address);
                }
            }
            catch { }
            V1 = V1.Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));

            switch (_Action)
            {
                case Actions.Exit:
                    App.Instance.CloseSLBr(false);
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
                    if (V2 == "CurrentIndex")
                        NewTab(V1, true, TabsUI.SelectedIndex + 1);
                    else
                        NewTab(V1, true);
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
            {
                BrowserTabItem SelectedTab = Tabs[TabsUI.SelectedIndex];
                if (SelectedTab.Content != null)
                    SelectedTab.Content.ReFocus();
            }
        }

        public void UnloadTabs()
        {
            BrowserTabItem SelectedTab = Tabs[TabsUI.SelectedIndex];
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (WindowState == WindowState.Minimized || Tab != SelectedTab)
                {
                    if (Tab.Content != null)
                        UnloadTab(Tab.Content);
                }
            }
        }
        public void ForceUnloadTab(int Id)
        {
            BrowserTabItem _Tab = GetBrowserTabWithId(Id);
            if (_Tab.Content != null)
                UnloadTab(_Tab.Content, true);
        }
        /*private async void UnloadTab(Browser BrowserView, bool Bypass = false)
        {
            if (BrowserView.Chromium != null && BrowserView.Chromium.IsBrowserInitialized)
            {
                if (!Bypass && !await BrowserView.CanUnload())
                    return;
                BrowserView.Unload();
            }
        }*/
        private void UnloadTab(Browser BrowserView, bool Bypass = false)
        {
            if (BrowserView.Chromium != null && BrowserView.Chromium.IsBrowserInitialized)
            {
                if (!Bypass && !BrowserView.CanUnload())
                    return;
                BrowserView.Unload();
            }
        }
        public void Favourite(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            _Tab.Content.Favourite();
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
            Browser _Browser = GetTab().Content;
            if (_Browser == null)
                return;
            _Browser.Navigate(Url);
        }
        public bool IsFullscreen;
        public void Fullscreen(bool Fullscreen)
        {
            IsFullscreen = Fullscreen;
            if (Fullscreen)
            {
                Browser BrowserView = GetTab().Content;
                if (BrowserView != null)
                {
                    BrowserView.CoreContainer.Children.Remove(BrowserView.Chromium);
                    FullscreenContainer.Children.Add(BrowserView.Chromium);
                    Keyboard.Focus(BrowserView.Chromium);

                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Browser BrowserView = GetTab().Content;
                if (BrowserView != null)
                {
                    try
                    {
                        FullscreenContainer.Children.Remove(BrowserView.Chromium);
                        BrowserView.CoreContainer.Children.Add(BrowserView.Chromium);
                        Keyboard.Focus(BrowserView.Chromium);
                    }
                    catch { }
                }
            }
        }
        public void DevTools(string Id = "", int XCoord = 0, int YCoord = 0)
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            _Tab.Content.DevTools();
        }
        public void NewTab(string Url, bool IsSelected = false, int Index = -1)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            Url = Url.Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));
            BrowserTabItem _Tab = new BrowserTabItem(this) { Header = Utils.CleanUrl(Url, true, true, true, true), BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = new Browser(Url, _Tab);
            Tabs.Insert(Index != -1 ? Index : Tabs.Count - 1, _Tab);
            if (IsSelected)
                TabsUI.SelectedIndex = Tabs.IndexOf(_Tab);
        }

        public void SwitchToTab(BrowserTabItem _Tab)
        {
            TabsUI.SelectedIndex = Tabs.IndexOf(_Tab);
            if (_Tab.Content != null)
                Keyboard.Focus(_Tab.Content.Chromium);
        }
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
                    if (TabsUI.SelectedIndex > 0)
                        TabsUI.SelectedIndex = TabsUI.SelectedIndex - 1;
                    else
                        TabsUI.SelectedIndex = TabsUI.SelectedIndex + 1;
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
            Browser BrowserView = GetTab().Content;
            if (BrowserView != null)
                BrowserView.Find(Text);
        }
        public void Screenshot()
        {
            Browser BrowserView = GetTab().Content;
            if (BrowserView != null)
                BrowserView.Screenshot();
        }

        public void Zoom(int Delta)
        {
            Browser BrowserView = GetTab().Content;
            if (BrowserView != null)
                BrowserView.Zoom(Delta);
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
                if (TabsUI.SelectedIndex == TabsUI.Items.Count - 1)
                    NewTab(App.Instance.GlobalSave.Get("Homepage"), true);
                else
                {
                    BrowserTabItem _CurrentTab = Tabs[TabsUI.SelectedIndex];
                    if (_CurrentTab.Content != null)
                    {
                        Keyboard.Focus(_CurrentTab.Content.Chromium);
                        _CurrentTab.Content.ReFocus();
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
            if (GCTimer != null)
                GCTimer.Stop();
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