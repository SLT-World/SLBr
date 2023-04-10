using SLBr.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using CefSharp.Wpf.HwndHost;
using CefSharp;
using System.Diagnostics;
using SLBr.Handlers;
using System.Windows.Interop;
using SLBr.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Reflection;
using CefSharp.SchemeHandler;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using Microsoft.Windows.Themes;
using static SLBr.Controls.UrlScheme;
using System.Drawing.Drawing2D;
using System.Configuration;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public enum Actions
    {
        Undo = 0,
        Redo = 1,
        Refresh = 2,
        Navigate = 3,
        CreateTab = 4,
        CloseTab = 5,
        Inspect = 6,
        Favourite = 7,
        SetAudio = 8,
        Settings = 9,
        UnloadTabs = 10,
        SwitchBrowser = 11,
        OpenFileExplorer = 12,
        QRCode = 13,
        SetInspectorDock = 14,
        OpenAsPopupBrowser = 15,
        SizeEmulator = 16,
        ForceUnloadTab = 17,
        OpenNewBrowserPopup = 18,
        ClosePrompt = 19,
        Prompt = 20,
        PromptNavigate = 21,
        SwitchTabAlignment = 22,
        NewWindow = 23,
        Exit = 24,
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
            TabAlignment = App.Instance.MainSave.Get("TabAlignment");
            DimIconWhenUnloaded = bool.Parse(App.Instance.MainSave.Get("DimIconsWhenUnloaded"));

            Id = Utils.GenerateRandomId();
            ParentWindow = _ParentWindow;
            ParentWindowId = _ParentWindow.Id;
            //Action = $"5<,>{Id}";
        }

        public string TabAlignment
        {
            get { return _TabAlignment; }
            set
            {
                _TabAlignment = value;
                RaisePropertyChanged("TabAlignment");
            }
        }
        private string _TabAlignment;
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
        public bool DimIconWhenUnloaded
        {
            get { return _DimIconWhenUnloaded; }
            set
            {
                _DimIconWhenUnloaded = value;
                RaisePropertyChanged("DimIconWhenUnloaded");
            }
        }
        private bool _DimIconWhenUnloaded;
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
        /*public string Action
        {
            get { return _Action; }
            set
            {
                _Action = value;
                RaisePropertyChanged("Action");
            }
        }
        private string _Action;*/
        public UserControl Content { get; set; }
        public MainWindow ParentWindow { get; set; }
        public int Id
        {
            get { return _Id; }
            set
            {
                _Id = value;
                RefreshCommand = $"2<,>{value}";
                ForceUnloadCommand = $"17<,>{value}";
                AddToFavouritesCommand = $"7<,>{value}";
                MuteCommand = $"8<,>{value}";
                //CloseCommand = $"5<,>{value}<,>{ParentWindowId}";
                MuteCommandHeader = "Mute";
                FavouriteCommandHeader = "Add to favourites";
                RaisePropertyChanged("Id");
            }
        }
        private int _Id;
        /*public int ParentWindowId
        {
            get { return ParentWindow.Id; }
        }*/
        public int ParentWindowId
        {
            get { return _ParentWindowId; }
            set
            {
                CloseCommand = $"5<,>{Id}<,>{value}";
                RaisePropertyChanged("ParentWindowId");
            }
        }
        private int _ParentWindowId;

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
        public string ForceUnloadCommand
        {
            get { return _ForceUnloadCommand; }
            set
            {
                _ForceUnloadCommand = value;
                RaisePropertyChanged("ForceUnloadCommand");
            }
        }
        private string _ForceUnloadCommand;
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
        public string MuteCommand
        {
            get { return _MuteCommand; }
            set
            {
                _MuteCommand = value;
                RaisePropertyChanged("MuteCommand");
            }
        }
        private string _MuteCommand;
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
        public string MuteCommandHeader
        {
            get { return _MuteCommandHeader; }
            set
            {
                _MuteCommandHeader = value;
                RaisePropertyChanged("MuteCommandHeader");
            }
        }
        private string _MuteCommandHeader;
    }

    public partial class MainWindow : Window
    {
        public int Id;

        public new string Title
        {
            get
            {
                return base.Title;
            }
            set
            {
                base.Title = value;
                WindowChromeTitle.Text = value;
            }
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "\xe923" : "\xe922";
            MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
            //Timeline.SetDesiredFrameRate(, 1);
            /*Dispatcher.Invoke(new Action(() =>
            {
                switch (WindowState)
                {
                    case WindowState.Maximized:
                        ChangeWPFFrameRate(WPFFrameRate);
                        break;
                    case WindowState.Minimized:
                        ChangeWPFFrameRate(1);
                        break;
                    case WindowState.Normal:
                        ChangeWPFFrameRate(WPFFrameRate);
                        break;
                }
            }));*/
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        #region Variables
        public ObservableCollection<BrowserTabItem> Tabs = new ObservableCollection<BrowserTabItem>();
        public bool IsFullscreen;
        bool WindowInitialized;
        #endregion

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case MessageHelper.WM_COPYDATA:
                    COPYDATASTRUCT _dataStruct = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    string _strMsg = Marshal.PtrToStringUni(_dataStruct.lpData, _dataStruct.cbData / 2);
                    NewBrowserTab(_strMsg, int.Parse(App.Instance.MainSave.Get("DefaultBrowserEngine")), true);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        public MainWindow()
        {
            InitializeWindow();
        }

        private async void InitializeWindow()
        {
            Id = Utils.GenerateRandomId();
            App.Instance.AllWindows.Add(this);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            source.AddHook(new HwndSourceHook(WndProc));
            if (App.Instance.TabsSaves.Count < App.Instance.AllWindows.Count)
                App.Instance.TabsSaves.Add(new Saving($"Window_{App.Instance.TabsSaves.Count}_Tabs.bin", App.Instance.UserApplicationWindowsPath));
            if (App.Instance.Username != "Default-User")
            {
                App.Instance.AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30_" + App.Instance.Username + "}";
                SetCurrentProcessExplicitAppUserModelID(App.Instance.AppUserModelID);
            }
            InitializeComponent();
            BrowserTabs.ItemsSource = Tabs;
            GCTimer.Tick += GCCollect_Tick;
            GCTimer.Start();
            SwitchTabAlignment(App.Instance.MainSave.Get("TabAlignment"));
        }

        private DispatcherTimer GCTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 30) };
        public int UnloadTabsTimeIncrement;
        private void GCCollect_Tick(object sender, EventArgs e)
        {
            if (bool.Parse(App.Instance.MainSave.Get("TabUnloading")))
            {
                if (UnloadTabsTimeIncrement >= App.Instance.TabUnloadingTime)
                    UnloadTabs(bool.Parse(App.Instance.MainSave.Get("ShowUnloadedIcon")));
                else
                    UnloadTabsTimeIncrement += 30;
            }
            //GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        public void UnloadTabs(bool ChangeIcon)
        {
            BrowserTabItem SelectedTab = Tabs[BrowserTabs.SelectedIndex];
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (WindowState == WindowState.Minimized || Tab != SelectedTab)
                {
                    Browser BrowserView = GetBrowserView(Tab);
                    if (BrowserView != null)
                        UnloadTab(ChangeIcon, BrowserView);
                }
            }
            UnloadTabsTimeIncrement = 0;
        }
        private void UnloadTab(bool ChangeIcon, Browser BrowserView)
        {
            string CleanedAddress = BrowserView.Address;
            if (BrowserView.BrowserType == 0)
            {
                bool IsBlacklistedSite = CleanedAddress.Contains("youtube.com/watch") || CleanedAddress.Contains("meet.google.com/") || CleanedAddress.Contains("spotify.com/track/")
                        || CleanedAddress.Contains("soundcloud.com") || CleanedAddress.Contains("dailymotion.com/video/") || CleanedAddress.Contains("vimeo.com")
                        || CleanedAddress.Contains("twitch.tv/") || CleanedAddress.Contains("bitchute.com/video/") || CleanedAddress.Contains("ted.com/talks/");
                if (BrowserView.IsAudioMuted || !Utils.IsAudioPlayingInDevice() || !IsBlacklistedSite)
                    BrowserView.Unload(ChangeIcon, App.Instance.Framerate, App.Instance.Javascript, App.Instance.LoadImages, App.Instance.LocalStorage, App.Instance.Databases, App.Instance.WebGL);
            }
            else if (BrowserView.BrowserType == 1)
            {
                if (BrowserView.IsAudioMuted || (BrowserView.Edge.CoreWebView2 != null && (BrowserView.Edge.CoreWebView2.IsMuted || !BrowserView.Edge.CoreWebView2.IsDocumentPlayingAudio)))
                    BrowserView.Unload(ChangeIcon);
            }
            else if (BrowserView.BrowserType == 2)
            {
                BrowserView.Unload(ChangeIcon);
            }
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == null)
                    return;
                Actions _Action;
                var Target = (FrameworkElement)sender;
                string _Tag = Target.Tag.ToString();
                var Values = _Tag.Split(new string[] { "<,>" }, StringSplitOptions.None);//_Tag.Split(new[] { '<,>' }, 3);//2 = 3//, "&lt;,&gt;"
                _Action = (Actions)int.Parse(Values[0]);
                string LastValue = Values.Last();
                Action(_Action, sender, (Values.Length > 1) ? Values[1] : "", (Values.Length > 2) ? Values[2] : "", (Values.Length > 3) ? Values[3] : "");
            }
            catch { }
        }

        private void Action(Actions _Action, object sender = null, string V1 = "", string V2 = "", string V3 = "")
        {
            try
            {
                Browser _BrowserView = GetBrowserView();
                if (_BrowserView != null)
                {
                    V1 = V1.Replace("{CurrentUrl}", _BrowserView.Address);
                    V1 = V1.Replace("{CurrentInspectorUrl}", _BrowserView.Address);
                }
            }
            catch { }
            V1 = V1.Replace("{Homepage}", App.Instance.MainSave.Get("Homepage"));
            switch (_Action)
            {
                case Actions.Undo:
                    Undo(V1);
                    break;
                case Actions.Redo:
                    Redo(V1);
                    break;
                case Actions.Refresh:
                    Refresh(V1);
                    break;
                case Actions.CreateTab:
                    NewBrowserTab(V1, int.Parse(App.Instance.MainSave.Get("DefaultBrowserEngine")), true);
                    break;
                case Actions.CloseTab:
                    CloseBrowserTab(int.Parse(V1), int.Parse(V2));
                    break;
                case Actions.Inspect:
                    Inspect(V1);
                    break;
                case Actions.Settings:
                    OpenSettings(true, BrowserTabs.SelectedIndex + 1);
                    break;
                case Actions.Favourite:
                    Favourite(V1);
                    break;
                case Actions.ForceUnloadTab:
                    ForceUnloadTab(V1);
                    break;
                case Actions.SetAudio:
                    SetAudio(V1);
                    break;
                case Actions.SwitchTabAlignment:
                    App.Instance.SwitchTabAlignment(V1);
                    break;
                case Actions.NewWindow:
                    App.Instance.NewWindow();
                    break;
                case Actions.Exit:
                    App.Instance.CloseSLBr(false);
                    break;
            }
        }
        public void SwitchTabAlignment(string NewAlignment)
        {
            if (NewAlignment == "Vertical")
            {
                BrowserTabs.Style = Resources["VerticalTabControlStyle"] as Style;
                SwitchTabAlignmentButton.ToolTip = "Switch to horizontal tabs";
                SwitchTabAlignmentButton.Tag = "22<,>Horizontal";
                SwitchTabAlignmentButton.Content = "\xE90E";
            }
            else if (NewAlignment == "Horizontal")
            {
                BrowserTabs.Style = Resources["HorizontalTabControlStyle"] as Style;
                SwitchTabAlignmentButton.ToolTip = "Switch to vertical tabs";
                SwitchTabAlignmentButton.Tag = "22<,>Vertical";
                SwitchTabAlignmentButton.Content = "\xE90D";
            }
            foreach (BrowserTabItem _Tab in Tabs)
                _Tab.TabAlignment = NewAlignment;
        }
        public void SetAudio(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.SetAudio(!_Browser.IsAudioMuted);
        }
        public void ForceUnloadTab(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.Unload(bool.Parse(App.Instance.MainSave.Get("ShowUnloadedIcon")), App.Instance.Framerate, App.Instance.Javascript, App.Instance.LoadImages, App.Instance.LocalStorage, App.Instance.Databases, App.Instance.WebGL);
        }
        public void Favourite(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.Favourite();
        }
        public void Undo(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            if (_Browser.CanGoBack)
                _Browser.Back();
        }
        public void Redo(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            if (_Browser.CanGoForward)
                _Browser.Forward();
        }
        public void Refresh(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            if (!_Browser.IsLoading)
                _Browser.Reload();
            else
                _Browser.Stop();
        }
        public void Navigate(string Url)
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            _Browser.Navigate(Url);
        }
        public void Fullscreen(bool Fullscreen)
        {
            IsFullscreen = Fullscreen;
            if (Fullscreen)
            {
                //WindowState = WindowState.Normal;
                //WindowStyle = WindowStyle.None;
                //WindowState = WindowState.Maximized;
                Browser BrowserView = GetBrowserView();
                if (BrowserView != null)
                {
                    BrowserView.CoreContainer.Children.Remove(BrowserView.Chromium);
                    FullscreenContainer.Children.Add(BrowserView.Chromium);
                    Keyboard.Focus(BrowserView.Chromium);

                    if (bool.Parse(App.Instance.MainSave.Get("CoverTaskbarOnFullscreen")))
                    {
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.None;
                    }
                    WindowState = WindowState.Maximized;
                }
                /*foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Collapsed;
                        //BrowserView.Margin = new Thickness(0, -25, 0, 0);
                        //BrowserView.Margin = new Thickness(0, -67, 0, 0);
                        BrowserView.Margin = new Thickness(-239, -32, 0, 0);
                    }
                }*/
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Browser BrowserView = GetBrowserView();
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
                /*foreach (BrowserTabItem _Tab in Tabs)
                {
                    Browser BrowserView = GetBrowserView(_Tab);
                    if (BrowserView != null)
                    {
                        BrowserView.ToolBar.Visibility = Visibility.Visible;
                        BrowserView.Margin = new Thickness(0, 0, 0, 0);
                    }
                }*/
            }
        }
        public void Inspect(string Id = "")
        {
            BrowserTabItem _Tab = null;
            if (Id == "")
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(int.Parse(Id));
            Browser _Browser = GetBrowserView(_Tab);
            if (_Browser == null)
                return;
            _Browser.Inspect();
        }
        public void FindUI()
        {
            Browser _Browser = GetBrowserView();
            if (_Browser == null)
                return;
            Keyboard.Focus(_Browser.FindTextBox);
            _Browser.FindTextBox.SelectAll();
        }
        /*public void NewTab(UserControl Content, string Header, bool IsSelected = false, int Index = -1)
        {
            BrowserTabItem _Tab = new BrowserTabItem { Header = Header, BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = Content;
            _Tab.Id = Utils.GenerateRandomId();
            _Tab.Action = $"5<,>{_Tab.Id}";
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
        }*/
        public void NewBrowserTab(string Url, int BrowserType = 0, bool IsSelected = false, int Index = -1)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            Url = Url.Replace("{Homepage}", App.Instance.MainSave.Get("Homepage"));
            BrowserTabItem _Tab = new BrowserTabItem(this) { Header = Utils.CleanUrl(Url, true, true, true, true), BrowserCommandsVisibility = Visibility.Collapsed };
            _Tab.Content = new Browser(Url, BrowserType, _Tab);
            if (Index != -1)
                Tabs.Insert(Index, _Tab);
            else
                Tabs.Add(_Tab);
            if (IsSelected)
                BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
            //TextOptions.SetTextFormattingMode(_Tab.Content, TextFormattingMode.Display);
        }
        public void OpenSettings(bool IsSelected = false, int Index = -1)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            //if (Settings.Instance != null && Settings.Instance.Tab != null)
            //    SwitchToTab(Settings.Instance.Tab);
            //else
            {
                BrowserTabItem _Tab = new BrowserTabItem(this) { Header = "Settings", BrowserCommandsVisibility = Visibility.Collapsed };
                if (Settings.Instance == null)
                {
                    _Tab.Content = new Settings(_Tab);
                    if (Index != -1)
                        Tabs.Insert(Index, _Tab);
                    else
                        Tabs.Add(_Tab);
                    if (IsSelected)
                        SwitchToTab(_Tab);
                }
                else if (Settings.Instance.Tab != null)
                {
                    if (Settings.Instance.Tab.ParentWindow != this)
                    {
                        Settings.Instance.Tab.ParentWindow.Activate();
                        Settings.Instance.Tab.ParentWindow.Focus();
                        Settings.Instance.Tab.ParentWindow.OpenSettings(IsSelected, Index);
                        //Settings.Instance.Tab = _Tab;
                        //_Tab.Content = Settings.Instance;
                    }
                    else if (Settings.Instance.Tab.ParentWindow == this)
                        SwitchToTab(Settings.Instance.Tab);
                }
                else if (Settings.Instance.Tab == null)
                {
                    Settings.Instance.Tab = _Tab;
                    _Tab.Content = Settings.Instance;
                    if (Index != -1)
                        Tabs.Insert(Index, _Tab);
                    else
                        Tabs.Add(_Tab);
                    if (IsSelected)
                        SwitchToTab(_Tab);
                }

                //TextOptions.SetTextFormattingMode(_Tab.Content, TextFormattingMode.Display);
                //TextOptions.TextHintingModeProperty.OverrideMetadata(typeof(TextHintingMode), new FrameworkPropertyMetadata { DefaultValue = TextHintingMode.Fixed });
                //TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(TextFormattingMode), new FrameworkPropertyMetadata { DefaultValue = TextFormattingMode.Display });
            }
        }
        public void SwitchToTab(BrowserTabItem _Tab)
        {
            BrowserTabs.SelectedIndex = Tabs.IndexOf(_Tab);
            Browser BrowserView = GetBrowserView(_Tab);
            if (BrowserView != null)
                Keyboard.Focus(BrowserView.Chromium);
        }
        public BrowserTabItem GetBrowserTabWithId(int Id)
        {
            foreach (BrowserTabItem _Tab in Tabs)
            {
                if (_Tab.Id == Id)
                {
                    //MessageBox.Show(_Tab);
                    return _Tab;
                }
            }
            return null;
        }
        public void CloseBrowserTab(int Id, int WindowId)
        {
            if (WindowId != this.Id)
            {
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    if (_Window.Id == WindowId)
                    {
                        _Window.CloseBrowserTab(Id, WindowId);
                        return;
                    }
                }    
            }
            //if (Id == -1)
            //    Id = Tabs[BrowserTabs.SelectedIndex].Id;
            BrowserTabItem _Tab = null;

            //MessageBox.Show(Id.ToString());
            if (Id == -1)
                _Tab = Tabs[BrowserTabs.SelectedIndex];
            else
                _Tab = GetBrowserTabWithId(Id);
            if (Tabs.Count > 1)
            {
                bool IsSelected = Id != -1 ? _Tab == Tabs[BrowserTabs.SelectedIndex] : true;
                //MessageBox.Show(Id.ToString());
                Browser BrowserView = GetBrowserView(_Tab);
                if (BrowserView != null)
                {
                    //MessageBox.Show(BrowserView.Address);
                    BrowserView.DisposeCore();
                }
                else
                {
                    if (Settings.Instance.Tab == _Tab)
                        Settings.Instance.DisposeCore();
                }
                if (IsSelected)
                {
                    if (BrowserTabs.SelectedIndex > 0)
                        BrowserTabs.SelectedIndex = BrowserTabs.SelectedIndex - 1;
                    else
                        BrowserTabs.SelectedIndex = BrowserTabs.SelectedIndex + 1;
                }
                Tabs.Remove(_Tab);
                if (IsSelected)
                {
                    if (BrowserTabs.SelectedIndex > Tabs.Count - 1)
                        BrowserTabs.SelectedIndex = Tabs.Count - 1;
                }

                //Tabs.RemoveAt(Tabs.IndexOf(_Tab));
            }
            else
            {
                Close();
                //ExecuteCloseEvent();
            }
        }
        public void Screenshot()
        {
            Browser BrowserView = GetBrowserView(Tabs[BrowserTabs.SelectedIndex]);
            if (BrowserView != null)
                BrowserView.Screenshot();
        }

        public void Zoom(int Delta)
        {
            Browser BrowserView = GetBrowserView(Tabs[BrowserTabs.SelectedIndex]);
            if (BrowserView != null)
                BrowserView.Zoom(Delta);
        }

        public BrowserTabItem GetTab(Browser _Control = null)
        {
            if (_Control != null)
            {
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    if (GetBrowserView(_Tab) == _Control)
                        return _Tab;
                }
                return null;
            }
            else
                return Tabs[BrowserTabs.SelectedIndex];
        }
        public UserControl GetUserControlView(BrowserTabItem Tab = null)
        {
            if (Tab != null)
                return Tab.Content;
            else
                return GetTab().Content;
        }
        public Browser GetBrowserView(BrowserTabItem Tab = null)
        {
            return GetUserControlView(Tab) as Browser;
        }
        public void ApplyTheme(Theme _Theme)
        {
            int SetDarkTitleBar = _Theme.DarkTitleBar ? 1 : 0;
            //DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref SetDarkTitleBar, Marshal.SizeOf(true));

            foreach (BrowserTabItem Tab in Tabs)
            {
                Browser _Browser = GetBrowserView(Tab);
                if (_Browser != null && _Browser.Chromium != null && _Browser.Chromium.IsBrowserInitialized && _Browser.Chromium.GetDevToolsClient() != null)
                    _Browser.Chromium.GetDevToolsClient().Emulation.SetAutoDarkModeOverrideAsync(_Theme.DarkWebPage ? bool.Parse(App.Instance.MainSave.Get("DarkWebPage")) : false);
            }

            //int trueValue = 2;
            //DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref trueValue, Marshal.SizeOf(typeof(int)));
            //DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));

            //Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            //Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            //Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            //Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            //Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["UnselectedTabBrushColor"] = _Theme.UnselectedTabColor;
            Resources["ControlFontBrushColor"] = _Theme.ControlFontColor;

            //WindowState = WindowState.Normal;
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (Tab.Content is Browser _Browser)
                    _Browser.ApplyTheme(_Theme);
            }
            //WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }

        public void SetDimIconsWhenUnloaded(bool Toggle)
        {
            foreach (BrowserTabItem _Tab in Tabs)
                _Tab.DimIconWhenUnloaded = Toggle;
        }
        public void SetSandbox(int _Framerate, CefState JSState, CefState LIState, CefState LSState, CefState DBState, CefState WebGLState)
        {
            foreach (BrowserTabItem Tab in Tabs)
            {
                Browser BrowserView = GetBrowserView(Tab);
                if (BrowserView != null)
                    BrowserView.Unload(false, _Framerate, JSState, LIState, LSState, DBState, WebGLState);
            }
            UnloadTabsTimeIncrement = 0;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ExecuteCloseEvent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(App.Instance.CurrentTheme);

            if (!App.Instance.DeveloperMode)
            {
                if (Utils.CheckForInternetConnection())
                {
                    try
                    {
                        string VersionInfo = App.Instance.TinyDownloader.DownloadString("https://raw.githubusercontent.com/SLT-World/SLBr/main/Version.txt").Replace("\n", "");
                        if (!VersionInfo.StartsWith(App.Instance.ReleaseVersion))
                            ToastBox.Show(VersionInfo, $"SLBr {VersionInfo} is now available, please update SLBr to keep up with the progress.", 10);
                        //Browser CurrentBrowser = GetBrowserView();
                        //if (CurrentBrowser != null)
                        //    GetBrowserView().Prompt(false, $"SLBr {VersionInfo} is now available, please update SLBr to keep up with the progress.", true, "Download", $"24<,>https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", $"https://github.com/SLT-World/SLBr/releases/tag/{VersionInfo}", true, "\xE896");//SLBr is up to date
                    }
                    catch { }
                }
            }

            WindowInitialized = true;
        }

        private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                BrowserTabItem _CurrentTab = Tabs[BrowserTabs.SelectedIndex];
                Browser BrowserView = GetBrowserView(_CurrentTab);
                if (BrowserView != null)
                    Keyboard.Focus(BrowserView.Chromium);

                Title = _CurrentTab.Header + (App.Instance.Username == "Default-User" ? " - SLBr" : $"- {App.Instance.Username} - SLBr");
            }
            catch
            {
                Title = App.Instance.Username == "Default-User" ? " - SLBr" : $"- {App.Instance.Username} - SLBr";
            }
        }

        public void ExecuteCloseEvent()
        {
            if (GCTimer != null)
                GCTimer.Stop();
            //MessageBox.Show("Window Close");
            //try
            //{
            if (App.Instance.AllWindows.Count == 1)
                App.Instance.CloseSLBr(false);
            else if (App.Instance.TabsSaves.Count == App.Instance.AllWindows.Count)
                App.Instance.TabsSaves.RemoveAt(App.Instance.TabsSaves.Count - 1);
            //}
            //catch { }
            App.Instance.AllWindows.Remove(this);

            //Close();
        }

        /*static void ChangeWPFFrameRate(int FrameRate)
        {
            //Timeline.DesiredFrameRateProperty.DefaultMetadata.DefaultValue = FrameRate;
            FrameRateProperty.DefaultValue = FrameRate;
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), FrameRateProperty);
        }*/

        //static FrameworkPropertyMetadata FrameRateProperty;
    }
}
