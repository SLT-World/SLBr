/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Handlers;
using SLBr.Pages;
using SLBr.WebView;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml.Linq;

namespace SLBr
{
    public class HotKey(Action _Callback, int _KeyCode, bool HasControl, bool HasShift, bool HasAlt)
    {
        public int KeyCode { get; } = _KeyCode;
        public bool Control { get; } = HasControl;
        public bool Shift { get; } = HasShift;
        public bool Alt { get; } = HasAlt;

        public Action Callback { get; } = _Callback;
    }

    public class InfoBar : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public InfoBar() { }
        public bool IsClosable
        {
            get => _IsClosable;
            set
            {
                _IsClosable = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsClosable = true;
        public string Title
        {
            get => _Title;
            set
            {
                _Title = value;
                RaisePropertyChanged();
            }
        }
        private string _Title;
        public List<UIElementLayer> Description { get; set; } = [];
        //TODO: Replace Icon with UIElementLayer list for layering based on order.
        public string? Icon
        {
            get => _Icon;
            set
            {
                _Icon = value;
                RaisePropertyChanged();
            }
        }
        private string? _Icon = null;

        public SolidColorBrush IconForeground
        {
            get => _IconForeground;
            set
            {
                _IconForeground = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush _IconForeground;

        public List<UIElementLayer> Actions { get; set; } = [];
    }

    public class UIElementLayer : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public bool IsEnabled
        {
            get => _IsEnabled;
            set
            {
                _IsEnabled = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsEnabled = true;

        public string? ToolTip
        {
            get => _ToolTip;
            set
            {
                _ToolTip = value;
                RaisePropertyChanged();
            }
        }
        private string? _ToolTip;

        public string? Text
        {
            get => _Text;
            set
            {
                _Text = value;
                RaisePropertyChanged();
            }
        }
        private string? _Text;

        public ICommand? Command
        {
            get => _Command;
            set
            {
                _Command = value;
                RaisePropertyChanged();
            }
        }
        private ICommand? _Command;

        public SolidColorBrush? Background
        {
            get => _Background;
            set
            {
                _Background = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush? _Background;

        public SolidColorBrush? Foreground
        {
            get => _Foreground;
            set
            {
                _Foreground = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush? _Foreground;

        public SolidColorBrush? BorderBrush
        {
            get => _BorderBrush;
            set
            {
                _BorderBrush = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush? _BorderBrush;

        public double? BorderThickness
        {
            get => _BorderThickness;
            set
            {
                _BorderThickness = value;
                RaisePropertyChanged();
            }
        }
        private double? _BorderThickness;
    }

    public class AdBlockList : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public bool IsEnabled
        {
            get => _IsEnabled;
            set
            {
                _IsEnabled = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsEnabled = false;

        public string Url
        {
            get => _Url;
            set
            {
                _Url = value.ToLowerInvariant();
                RaisePropertyChanged();
            }
        }
        private string _Url = "";

        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                RaisePropertyChanged();
            }
        }
        private string _Name = "";
    }

    public static class HotKeyManager
    {
        public static HashSet<HotKey> HotKeys = [];

        public static void HandleKeyDown(KeyEventArgs e)
        {
            bool Ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool Shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool Alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

            foreach (HotKey Key in HotKeys)
            {
                if (Key.KeyCode == (int)e.Key && Key.Control == Ctrl && Key.Shift == Shift && Key.Alt == Alt)
                {
                    Application.Current?.Dispatcher.Invoke(() => Key.Callback());
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    public class TabGroup : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public TabGroup(MainWindow _ParentWindow)
        {
            ParentWindow = _ParentWindow;
        }
        public MainWindow ParentWindow { get; set; }
        public bool IsCollapsed
        {
            get => _IsCollapsed;
            set
            {
                _IsCollapsed = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsCollapsed;
        public string Header
        {
            get => _Header;
            set
            {
                if (ParentWindow.TabGroups.Any(i => i.Header == value))
                    return;
                _Header = value;
                RaisePropertyChanged();
            }
        }
        private string _Header;
        public SolidColorBrush Background
        {
            get => _Background;
            set
            {
                _Background = value;
                Foreground = (SolidColorBrush)Utils.GetContrastBrush(value.Color);
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush _Background;

        public SolidColorBrush Foreground
        {
            get => _Foreground;
            set
            {
                _Foreground = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush _Foreground;
        public Guid ID { get; } = Guid.NewGuid();
    }

    public class BrowserTabItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public BrowserTabItem(MainWindow _ParentWindow)
        {
            if (_ParentWindow != null)
            {
                ID = Utils.GenerateRandomId();
                ParentWindow = _ParentWindow;
            }
        }
        public BrowserTabType Type { get; set; } = BrowserTabType.Navigation;
        public ImageSource Preview { get; set; }
        public bool IsUnloaded
        {
            get => _IsUnloaded;
            set
            {
                _IsUnloaded = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsUnloaded;
        public string Header
        {
            get => _Header;
            set
            {
                _Header = value;
                RaisePropertyChanged();
            }
        }
        private string _Header;
        public BitmapSource Icon
        {
            get => _Icon;
            set
            {
                _Icon = value;
                RaisePropertyChanged();
            }
        }
        private BitmapSource _Icon;
        public TabGroup? TabGroup
        {
            get => _TabGroup;
            set
            {
                _TabGroup = value;
                RaisePropertyChanged();
            }
        }
        private TabGroup? _TabGroup = null;
        public Browser Content { get; set; }
        public MainWindow ParentWindow { get; set; }
        public int ID
        {
            get => _ID;
            set
            {
                FavouriteCommandHeader = "Add to favourites";
                _ID = value;
                RaisePropertyChanged();
            }
        }
        private int _ID;

        public double Progress
        {
            get => _Progress;
            set
            {
                _Progress = value;
                RaisePropertyChanged();
            }
        }
        private double _Progress = 0;

        public Visibility ProgressBarVisibility
        {
            get => _ProgressBarVisibility;
            set
            {
                _ProgressBarVisibility = value;
                RaisePropertyChanged();
            }
        }
        private Visibility _ProgressBarVisibility;
        public string FavouriteCommandHeader
        {
            get => _FavouriteCommandHeader;
            set
            {
                _FavouriteCommandHeader = value;
                RaisePropertyChanged();
            }
        }
        private string _FavouriteCommandHeader;
    }
    public enum BrowserTabType
    {
        Navigation = 0,
        Group = 1,
        Add = 2,
    }

    public class Profile : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        private string PName;
        public string Name
        {
            get => PName;
            set
            {
                PName = value;
                Initial = value[0].ToString().ToUpper();
                ReadOnlySpan<byte> Hash = MD5.HashData(Encoding.UTF8.GetBytes(value));

                byte R = (byte)(Hash[0] % 128 + 64);
                byte G = (byte)(Hash[1] % 128 + 64);
                byte B = (byte)(Hash[2] % 128 + 64);

                Brush = new SolidColorBrush(Color.FromRgb(R, G, B));
                Foreground = (SolidColorBrush)Utils.GetContrastBrush(Brush.Color);
                RaisePropertyChanged();
            }
        }

        private string PInitial;
        public string Initial
        {
            get => PInitial;
            set
            {
                PInitial = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush PBrush;
        public SolidColorBrush Brush
        {
            get => PBrush;
            set
            {
                PBrush = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush PForeground;
        public SolidColorBrush Foreground
        {
            get => PForeground;
            set
            {
                PForeground = value;
                RaisePropertyChanged();
            }
        }

        /*private string PIcon;
        public string Icon
        {
            get { return PIcon; }
            set
            {
                PIcon = value;
                RaisePropertyChanged(nameof(Icon));
            }
        }*/
        public ProfileType Type { get; set; } = ProfileType.User;
        private bool PDefault = false;
        public bool Default
        {
            get => PDefault;
            set
            {
                PDefault = value;
                RaisePropertyChanged();
            }
        }
    }
    public enum ProfileType
    {
        User,
        System
    }

    public class Extension : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        private string PID;
        public string ID
        {
            get => PID;
            set
            {
                PID = value;
                RaisePropertyChanged();
            }
        }

        private string PName;
        public string Name
        {
            get => PName;
            set
            {
                PName = value;
                RaisePropertyChanged();
            }
        }

        private string PPopup;
        public string Popup
        {
            get => PPopup;
            set
            {
                PPopup = value;
                RaisePropertyChanged();
            }
        }

        private string PVersion;
        public string Version
        {
            get => PVersion;
            set
            {
                PVersion = value;
                RaisePropertyChanged();
            }
        }

        /*private string PManifestVersion;
        public string ManifestVersion
        {
            get { return PManifestVersion; }
            set
            {
                PManifestVersion = value;
                RaisePropertyChanged(nameof(ManifestVersion));
            }
        }

        private string PIcon;
        public string Icon
        {
            get { return PIcon; }
            set
            {
                PIcon = value;
                RaisePropertyChanged(nameof(Icon));
            }
        }*/

        private string PDescription;
        public string Description
        {
            get => PDescription;
            set
            {
                PDescription = value;
                RaisePropertyChanged();
            }
        }

        /*private bool PIsEnabled = true;
        public bool IsEnabled
        {
            get { return PIsEnabled; }
            set
            {
                PIsEnabled = value;
                RaisePropertyChanged(nameof(IsEnabled));
            }
        }*/
    }

    public class SearchProvider : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                RaisePropertyChanged();
            }
        }
        private string _Name = "";
        public string Host = "";
        public string SearchUrl = "";
        public string SuggestUrl = "";
    }

    public class OmniSuggestion
    {
        public string Text { get; set; }
        public string Display { get; set; }
        public string SubText { get; set; }
        public string Icon { get; set; }
        public string Hidden { get; set; }
        public SolidColorBrush Color { get; set; }
        public SearchProvider? ProviderOverride { get; set; }
    }

    public class DownloadEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        private string PID;
        public string ID
        {
            get => PID;
            set
            {
                PID = value;
                RaisePropertyChanged();
            }
        }

        private string PFileName;
        public string FileName
        {
            get => PFileName;
            set
            {
                PFileName = value;
                RaisePropertyChanged();
            }
        }

        private string PIcon;
        public string Icon
        {
            get => PIcon;
            set
            {
                PIcon = value;
                RaisePropertyChanged();
            }
        }

        private int PPercentComplete;
        public int PercentComplete
        {
            get => PPercentComplete;
            set
            {
                PPercentComplete = value;
                RaisePropertyChanged();
            }
        }

        private string PFormattedProgress;
        public string FormattedProgress
        {
            get => PFormattedProgress;
            set
            {
                PFormattedProgress = value;
                RaisePropertyChanged();
            }
        }
        private SolidColorBrush PColor;
        public SolidColorBrush Color
        {
            get => PColor;
            set
            {
                PColor = value;
                RaisePropertyChanged();
            }
        }

        private Visibility POpen;
        public Visibility Open
        {
            get => POpen;
            set
            {
                POpen = value;
                RaisePropertyChanged();
            }
        }

        private Visibility PStop;
        public Visibility Stop
        {
            get => PStop;
            set
            {
                PStop = value;
                RaisePropertyChanged();
            }
        }

        private Visibility PProgress;
        public Visibility Progress
        {
            get => PProgress;
            set
            {
                PProgress = value;
                RaisePropertyChanged();
            }
        }

        private bool PIsIndeterminate;
        public bool IsIndeterminate
        {
            get => PIsIndeterminate;
            set
            {
                PIsIndeterminate = value;
                RaisePropertyChanged();
            }
        }
    }

    public partial class App : Application
    {
        public static App Instance;

        public const string AMPEndpoint = "https://acceleratedmobilepageurl.googleapis.com/v1/ampUrls:batchGet";

        public List<SearchProvider> AllSystemSearchEngines = [
            new() { Host = "__Program__", Name = "Tabs" },
            new() { Host = "__Program__", Name = "History" },
            new() { Host = "__Program__", Name = "Favourites" }
        ];

        public List<MainWindow> AllWindows = [];
        public ObservableCollection<InfoBar> InfoBars = [];
        public ObservableCollection<SearchProvider> SearchEngines = [];
        public SearchProvider DefaultSearchProvider;
        public List<Theme> Themes =
        [
            new Theme("Light", Colors.White, Colors.WhiteSmoke, Colors.Gainsboro, Colors.Gray, Colors.Black, (Color)ColorConverter.ConvertFromString("#3399FF"), false, false),
            new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), (Color)ColorConverter.ConvertFromString("#2F3136"), (Color)ColorConverter.ConvertFromString("#36393F"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3399FF"), true, true),
            new Theme("Purple", (Color)ColorConverter.ConvertFromString("#191025"), (Color)ColorConverter.ConvertFromString("#251C31"), (Color)ColorConverter.ConvertFromString("#2B2237"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#934CFE"), true, true),
        ];

        public IdnMapping _IdnMapping = new();

        public Saving AppSave;
        public Saving GlobalSave;
        public Saving SearchSave;
        public Saving StatisticsSave;
        public Saving LanguagesSave;
        public Saving AllowListSave;
        public Saving AdBlockSave;

        public List<Saving> WindowsSaves = [];

        public SolidColorBrush FavouriteColor;
        public SolidColorBrush SLBrColor;
        public SolidColorBrush RedColor;
        public SolidColorBrush CornflowerBlueColor;
        public SolidColorBrush NavajoWhiteColor;
        public SolidColorBrush LimeGreenColor;
        public SolidColorBrush WhiteColor;
        public SolidColorBrush OrangeColor;
        public SolidColorBrush GreenColor;
        public FontFamily IconFont;
        public FontFamily SLBrFont;

        public Profile CurrentProfile;
        public string ApplicationLocalDataPath;
        public string UserApplicationWindowsPath;
        public string UserApplicationDataPath;
        public string ExecutablePath;
        public string ExtensionsPath;
        public string ResourcesPath;
        public string AdBlockDataPath;
        public string NotificationTempPath;
        //public string CdnPath;

        public bool AppInitialized;

        public static readonly Lazy<string[]> URLConfusables = new(() => [
            "rn",//m, rnicrosoft
            "vv",//w
            "cl",//d
            "0",//o
            "1",//l
            "5",//S
        ]);

        public ObservableCollection<Favourite> Favourites = [];
        public ObservableCollection<ActionStorage> History = [];
        private List<Extension> PrivateExtensions = [];
        public List<Extension> Extensions
        {
            get => PrivateExtensions;
            set
            {
                PrivateExtensions = value;
                switch (GlobalSave.GetInt("ExtensionButton"))
                {
                    case 0:
                        foreach (MainWindow _Window in AllWindows)
                        {
                            foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                            {
                                BrowserView.ExtensionsButton.Visibility = value.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
                                BrowserView.ExtensionsMenu.ItemsSource = Extensions;
                            }
                        }
                        break;
                    case 1:
                        foreach (MainWindow _Window in AllWindows)
                        {
                            foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                            {
                                BrowserView.ExtensionsButton.Visibility = Visibility.Visible;
                                BrowserView.ExtensionsMenu.ItemsSource = Extensions;
                            }
                        }
                        break;
                    case 2:
                        foreach (MainWindow _Window in AllWindows)
                        {
                            foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                            {
                                BrowserView.ExtensionsButton.Visibility = Visibility.Collapsed;
                                BrowserView.ExtensionsMenu.ItemsSource = Extensions;
                            }
                        }
                        break;
                }
            }
        }

        public Dictionary<string, WebAppManifest> AvailableWebAppManifests = [];

        public void AddHistory(string Url, string Title)
        {
            for (int i = 0; i < History.Count; i++)
            {
                if (History[i].Tooltip == Url)
                    History.RemoveAt(i);
            }
            History.Insert(0, new ActionStorage(Title, $"4<,>{Url}", Url));
            /*JumpList.AddToRecentCategory(new JumpTask()
            {
                Title = Title,
                ApplicationPath = ExecutablePath,
                Arguments = Url,
                IconResourcePath = ExecutablePath,
                IconResourceIndex = 0
            });*/

            /*JumpList RecentJumpList = new JumpList();
            JumpTask NewWindowTask = new JumpTask
            {
                Title = "New window",
                Description = "Open a new browser window",
                ApplicationPath = ExecutablePath,
                Arguments = "--window",
                IconResourcePath = ExecutablePath,
                IconResourceIndex = 0
            };
            RecentJumpList.JumpItems.Add(NewWindowTask);
            JumpList.SetJumpList(Application.Current, RecentJumpList);*/
        }
        public ObservableCollection<DownloadEntry> VisibleDownloads = [];
        public Dictionary<string, WebDownloadItem> Downloads = [];
        public void UpdateDownloadItem(WebDownloadItem Item)
        {
            Downloads[Item.ID] = Item;
            Dispatcher.BeginInvoke(async () =>
            {
                foreach (MainWindow _Window in AllWindows)
                    _Window.TaskbarItem.ProgressValue = Item.State == WebDownloadState.Completed ? 0 : Item.Progress;
                DownloadEntry _Entry = VisibleDownloads.FirstOrDefault(d => d.ID == Item.ID);
                if (_Entry != null)
                {
                    if (string.IsNullOrEmpty(_Entry.FileName))
                    {
                        _Entry.FileName = Path.GetFileName(Item.FullPath);
                        if (!string.IsNullOrEmpty(_Entry.FileName))
                        {
                            _Entry.Icon = _Entry.FileName.Split(".").Last() switch
                            {
                                "zip" or "rar" or "7z" or "tgz" or "gz" =>
                                    "\uF012",

                                "txt" =>
                                    "\uF000",

                                "png" or "jpg" or "jpeg" or "avif" or "svg" or "webp" or "jfif" or "bmp" =>
                                    "\uE91B",

                                "gif" =>
                                    "\uF4A9",

                                "mp3" or "mp2" =>
                                    "\uEA69",

                                "pdf" =>
                                    "\uEA90",

                                "blend" or "obj" or "fbx" or "max" or "stl" or "x3d" or "3ds" or "dae" or "glb" or "gltf" or "ply" =>
                                    "\uF158",

                                "mp4" or "avi" or "ogg" or "webm" or "mov" or "mpej" or "wmv" or "h264" or "mkv" =>
                                    "\uE786",

                                _ => "\uE8A5",
                            };
                            MainWindow Current = CurrentFocusedWindow();
                            if (Current != null)
                                Current?.Tabs[Current.TabsUI.SelectedIndex].Content?.OpenDownloadsButton.OpenPopup();
                        }
                    }
                    _Entry.PercentComplete = (int)(Item.Progress * 100);
                    if (Item.State == WebDownloadState.Completed)
                    {
                        _Entry.Stop = Visibility.Collapsed;
                        if (DownloadSecurityService != DownloadSecurityService.None)
                        {
                            try
                            {
                                _Entry.FormattedProgress = "Safety Check - Scanning";
                                _Entry.IsIndeterminate = true;
                                DownloadVerdict Verdict = await _DownloadRiskHandler.IsSafe(Item.TempPath, Item.FullPath, Item.Url, DownloadSecurityService);
                                _Entry.Progress = Visibility.Collapsed;
                                switch (Verdict)
                                {
                                    case DownloadVerdict.Dangerous:
                                        File.Delete(Item.TempPath);
                                        _Entry.Icon = "\uea39";
                                        _Entry.Color = RedColor;
                                        _Entry.FormattedProgress = "Dangerous - Blocked";
                                        //_Entry.Open = Visibility.Collapsed;
                                        break;
                                    case DownloadVerdict.Uncommon:
                                        _Entry.Icon = "\ue7ba";
                                        _Entry.Color = OrangeColor;
                                        _Entry.FormattedProgress = "Suspicious - Complete";
                                        _Entry.Open = Visibility.Visible;
                                        WebViewManager.DownloadManager.RemoveFileStaging(Item);
                                        break;
                                    case DownloadVerdict.DangerousHost:
                                        _Entry.Icon = "\ue7ba";
                                        _Entry.Color = OrangeColor;
                                        _Entry.FormattedProgress = "Dangerous host - Complete";
                                        _Entry.Open = Visibility.Visible;
                                        WebViewManager.DownloadManager.RemoveFileStaging(Item);
                                        break;
                                    default:
                                        _Entry.FormattedProgress = $"{FormatBytes(Item.TotalBytes)} - Complete";
                                        _Entry.Open = Visibility.Visible;
                                        WebViewManager.DownloadManager.RemoveFileStaging(Item);
                                        break;
                                }
                            }
                            catch
                            {
                                _Entry.Progress = Visibility.Collapsed;
                                _Entry.FormattedProgress = $"{FormatBytes(Item.TotalBytes)} - Complete";
                                _Entry.Open = Visibility.Visible;
                                WebViewManager.DownloadManager.RemoveFileStaging(Item);
                            }
                        }
                        else
                        {
                            _Entry.Progress = Visibility.Collapsed;
                            _Entry.FormattedProgress = $"{FormatBytes(Item.TotalBytes)} - Complete";
                            _Entry.Open = Visibility.Visible;
                            WebViewManager.DownloadManager.RemoveFileStaging(Item);
                        }
                    }
                    else if (Item.State == WebDownloadState.Canceled)
                    {
                        if (string.IsNullOrEmpty(_Entry.FileName))
                        {
                            VisibleDownloads.Remove(_Entry);
                            return;
                        }
                        _Entry.FormattedProgress = "Canceled";
                        _Entry.Open = Visibility.Collapsed;
                        _Entry.Stop = Visibility.Collapsed;
                        _Entry.Progress = Visibility.Collapsed;
                    }
                    else if (Item.State == WebDownloadState.Paused)
                    {
                        _Entry.FormattedProgress = "Paused";
                        _Entry.Open = Visibility.Collapsed;
                        _Entry.Stop = Visibility.Visible;
                        _Entry.Progress = Visibility.Visible;
                    }
                    else
                    {
                        string FormattedDescription;
                        DateTime? EndTime = Item.EndTime ?? Item.CalculatedEndTime;
                        if (EndTime.HasValue)
                        {
                            TimeSpan TimeLeft = EndTime.Value - DateTime.Now;
                            if (TimeLeft.Ticks < 0)
                                TimeLeft = TimeSpan.Zero;
                            if (TimeLeft.TotalDays >= 1)
                                FormattedDescription = $"{(int)TimeLeft.TotalDays} days left";
                            else if (TimeLeft.TotalHours >= 1)
                                FormattedDescription = $"{(int)TimeLeft.TotalHours} hours left";
                            else if (TimeLeft.TotalMinutes >= 1)
                                FormattedDescription = $"{(int)TimeLeft.TotalMinutes} minutes left";
                            else
                                FormattedDescription = $"{(int)TimeLeft.TotalSeconds} seconds left";
                        }
                        else
                            FormattedDescription = "Downloading";
                        if (Item.TotalBytes > 0)
                        {
                            int TargetIndex = (int)Math.Floor(Math.Log(Item.TotalBytes) / Math.Log(1000));
                            if (TargetIndex >= FileSizes.Value.Length)
                                TargetIndex = FileSizes.Value.Length - 1;
                            _Entry.FormattedProgress = $"{FormatBytes(Item.ReceivedBytes, false, TargetIndex)}/{FormatBytes(Item.TotalBytes, true, TargetIndex)} - {FormattedDescription}";
                        }
                        else
                        {
                            _Entry.FormattedProgress = $"{FormatBytes(Item.ReceivedBytes)} - {FormattedDescription}";
                            _Entry.IsIndeterminate = true;
                        }
                        _Entry.Open = Visibility.Collapsed;
                        _Entry.Stop = Visibility.Visible;
                        _Entry.Progress = Visibility.Visible;
                    }
                }
                else
                {
                    VisibleDownloads.Insert(0, new DownloadEntry { ID = Item.ID });
                    CurrentFocusedWindow().GetTab()?.Content?.SetDownloadsButtonVisibility();
                }
            });
        }

        static readonly Lazy<string[]> FileSizes = new(() => ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"]);
        public static string FormatBytes(long Bytes, bool ContainSizes = true, int? ForcedIndex = null)
        {
            if (Bytes == 0)
                return ContainSizes ? "0 Bytes" : "0.00";
            int i = ForcedIndex ?? (int)Math.Floor(Math.Log(Bytes) / Math.Log(1000));
            if (i >= FileSizes.Value.Length)
                i = FileSizes.Value.Length - 1;
            string Output = (Bytes / Math.Pow(1000, i)).ToString("F2");
            if (ContainSizes)
                Output += $" {FileSizes.Value[i]}";
            return Output;
        }
        public ObservableCollection<Profile> Profiles { get; set; } = [
            new() { Name = "Guest", Type = ProfileType.System},
            new() { Name = "Add", Type = ProfileType.System},
        ];

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e); 
            ApplicationLocalDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr");
            if (!Directory.Exists(ApplicationLocalDataPath))
                Directory.CreateDirectory(ApplicationLocalDataPath);
            /*string[] Tests = ["Tổng cộng", "арр", "𐌁𝕠𝖇", "https://github.com∕kubernetes∕kubernetes∕archive∕refs∕tags∕@v1271.zip", "https://www.google.com∕search∕.o7.fi/", "https://poliisi.fi⁄.nettivinkki.fi/", "аррӏе"];
            foreach (string ToTest in Tests)
            {
                string ReadTest = Utils.BuildTextSkeleton(ToTest);
                List<char> Different = [];
                foreach (char _Char in ReadTest)
                {
                    if (_Char > 127)
                    {
                        Different.Add(_Char);
                        continue;
                    }
                }
                MessageBox.Show($"{ReadTest} {Different.Count} LEFT: {string.Join("|", Different)}");
            }*/

            Instance = this;
            try
            {
                using var Key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true);
                CurrentTheme = Key.GetValue("SystemUsesLightTheme") as int? == 1 ? Themes[0] : Themes[1];
            }
            catch { CurrentTheme = Themes[0]; }
            AppSave = new Saving("App.bin", ApplicationLocalDataPath);
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            string[] ProfileDirectories = Directory.GetDirectories(ApplicationLocalDataPath, "*", SearchOption.TopDirectoryOnly);

            foreach (string DirectoryPath in ProfileDirectories)
            {
                DirectoryInfo _DirectoryInfo = new DirectoryInfo(DirectoryPath);
                Profiles.Insert(0, new Profile { Name = _DirectoryInfo.Name, Type = ProfileType.User });
            }
            Profile? DefaultProfile = null;
            if (!AppSave.Has("StartupProfiles"))
                AppSave.Set("StartupProfiles", true);
            if (AppSave.Has("Default"))
            {
                string ProfileName = AppSave.Get("Default");
                foreach (Profile _Profile in Profiles)
                {
                    if (_Profile.Name == ProfileName)
                    {
                        DefaultProfile = _Profile;
                        break;
                    }
                }
            }
            if (DefaultProfile == null)
            {
                DefaultProfile = Profiles.FirstOrDefault(i => i.Type == ProfileType.User);
                if (DefaultProfile == null)
                {
                    AppSave.Remove("Default");
                    AppSave.Save();
                }
            }
            //WARNING: Keep the ifs separate.
            if (DefaultProfile != null)
            {
                DefaultProfile.Default = true;
                AppSave.Set("Default", DefaultProfile.Name);
                AppSave.Save();
            }

            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            Profile? SelectedProfile = null;
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--user="))
                {
                    string Username = Flag.Replace("--user=", string.Empty).Replace(" ", "-");
                    SelectedProfile = Profiles.FirstOrDefault(i => i.Name == Username);
                    if (SelectedProfile == null)
                    {
                        SelectedProfile = new Profile { Name = Username };
                        Profiles.Insert(0, CurrentProfile);
                    }
                    break;
                }
                else if (Flag == "--guest")
                {
                    SelectedProfile = Profiles.First(i => i.Type == ProfileType.System && i.Name == "Guest");
                    break;
                }
            }
            if (SelectedProfile == null && DefaultProfile != null && !bool.Parse(AppSave.Get("StartupProfiles")))
                SelectedProfile = DefaultProfile;
            //SelectedProfile = Profiles.First(i => i.Type == ProfileType.System && i.Name == "Guest");
            //Warning: Keep separate.

            //TODO: Investigate, this specific feature appears to be highly unstable, consistently breaking without any direct modification.
            if (SelectedProfile == null)
            {
                ProfileManagerWindow ProfileManager = new();
                ProfileManager.Show();
            }
            else
                InitializeApp(Args, SelectedProfile);
        }

        static Mutex _Mutex;

        public string UserAgent;
        public string UserAgentBrandsString;
        public WebUserAgentMetaData UserAgentData;

        public void LoadExtensions()
        {
            //TODO: Handle WebView2 extensions & CefSharp unpacked extensions.
            Extensions.Clear();
            if (Directory.Exists(ExtensionsPath))
            {
                var ExtensionsDirectory = Directory.GetDirectories(ExtensionsPath);
                if (ExtensionsDirectory.Length != 0)
                {
                    List<Extension> _Extensions = [];
                    foreach (var ExtensionParentDirectory in ExtensionsDirectory)
                    {
                        try
                        {
                            string ExtensionDirectory = Directory.EnumerateDirectories(ExtensionParentDirectory).FirstOrDefault();
                            if (!Directory.Exists(ExtensionDirectory))
                                ExtensionDirectory = ExtensionParentDirectory;
                            string[] Manifests = Directory.GetFiles(ExtensionDirectory, "manifest.json", SearchOption.TopDirectoryOnly);
                            foreach (string ManifestFile in Manifests)
                            {
                                using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestFile));
                                JsonElement Manifest = Document.RootElement;

                                Extension _Extension = new() { ID = Path.GetFileName(ExtensionParentDirectory), Version = Manifest.GetProperty("version").ToString()/*, ManifestVersion = Manifest.GetProperty("manifest_version").ToString()*/ };

                                if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                                {
                                    if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup))
                                        _Extension.Popup = $"chrome-extension://{_Extension.ID}/{ExtensionPopup.GetString()}";
                                    /*else if (ExtensionAction.TryGetProperty("default_icon", out JsonElement defaultIconValue))
                                    {
                                        var firstIcon = defaultIconValue.EnumerateObject().OrderBy(kvp => int.Parse(kvp.Name)).FirstOrDefault();
                                        _Extension.Icon = $"chrome-extension://{ExtensionID}/{firstIcon.Value.GetString()}";
                                    }*/
                                }
                                List<string> VarsInMessages = [];
                                if (Manifest.TryGetProperty("name", out JsonElement ExtensionName))
                                {
                                    string Name = ExtensionName.GetString();
                                    if (Name.StartsWith("__MSG_"))
                                        VarsInMessages.Add($"Name<|>{Name}");
                                    else
                                        _Extension.Name = Name;
                                }
                                if (Manifest.TryGetProperty("description", out JsonElement ExtensionDescription))
                                {
                                    string Description = ExtensionDescription.GetString();
                                    if (Description.StartsWith("__MSG_"))
                                        VarsInMessages.Add($"Description<|>{Description}");
                                    else
                                        _Extension.Description = Description;
                                }

                                foreach (string Var in VarsInMessages)
                                {
                                    string _Locale = "en";
                                    string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(ExtensionDirectory, "_locales"));
                                    foreach (string LocaleDirectory in LocalesDirectory)
                                    {
                                        string CompareLocale = Locale.Name.Replace("-", "_");
                                        if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                                        {
                                            _Locale = CompareLocale;
                                            break;
                                        }
                                    }
                                    string[] MessagesFiles = Directory.GetFiles(Path.Combine(ExtensionDirectory, "_locales", _Locale), "messages.json", SearchOption.TopDirectoryOnly);
                                    foreach (string MessagesFile in MessagesFiles)
                                    {
                                        using JsonDocument MDocument = JsonDocument.Parse(File.ReadAllText(MessagesFile));
                                        JsonElement Messages = MDocument.RootElement;
                                        string[] Vars = Var.Split("<|>");
                                        if (Vars[0] == "Description")
                                        {
                                            _Extension.Description = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                            break;
                                        }
                                        else if (Vars[0] == "Name")
                                        {
                                            _Extension.Name = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                            break;
                                        }
                                    }
                                }

                                //_Extension.IsEnabled = true;
                                _Extensions.Add(_Extension);
                            }
                        }
                        catch { }
                    }
                    Extensions = _Extensions;
                }
            }
        }

        public static OmniSuggestion GenerateSuggestion(string Display, string Type, SolidColorBrush Color, string SubText = "", string? Actual = null, SearchProvider? ProviderOverride = null, string? Hidden = null)
        {
            OmniSuggestion Suggestion = new(){ Text = Actual ?? Display, Display = Display, Color = Color, SubText = SubText, ProviderOverride = ProviderOverride, Hidden = Hidden };
            switch (Type)
            {
                case "S":
                    Suggestion.Icon = "\xE721";
                    break;
                case "W":
                    Suggestion.Icon = "\xE774";
                    break;
                case "P":
                    Suggestion.Icon = "\xE756";
                    break;
                case "C":
                    Suggestion.Icon = "\xE943";
                    break;
                case "F":
                    Suggestion.Icon = "\xe838";//e8b7
                    //Suggestion.Color = Instance.NavajoWhiteColor;
                    break;
                case "T":
                    Suggestion.Icon = "\xec6c";
                    break;
            }
            return Suggestion;
        }

        /*public static string GetSearchType(string Text)
        {
            if (Text.StartsWith("search:"))
                return "Search";
            else if (Text.StartsWith("domain:"))
                return "Url";
            else if (Utils.IsProgramUrl(Text))
                return "Program";
            else if (Utils.IsCode(Text))
                return "Code";
            else if (Text.StartsWith("file:///"))
                return "File";
            else if (Utils.IsUrl(Text))
                return "Url";
            return "Search";
        }*/

        private static Lazy<TextBox> SpellCheckTextBox = new(() => new() { SpellCheck = { IsEnabled = true } });

        public async Task<List<(string Word, List<string> Suggestions)>> SpellCheck(string Text, CancellationToken Token)
        {
            List<(string, List<string>)> Results = [];
            try
            {
                Token.ThrowIfCancellationRequested();
                switch (GlobalSave.GetInt("SpellCheckProvider"))
                {
                    case 0:
                        XmlLanguage CurrentLanguage = XmlLanguage.GetLanguage(Locale.Tooltip);
                        if (SpellCheckTextBox.Value.Language != CurrentLanguage)
                            SpellCheckTextBox.Value.Language = CurrentLanguage;
                        string[] Words = Text.Split([' ', ',', ';', '.']);
                        foreach (string Word in Words)
                        {
                            Token.ThrowIfCancellationRequested();

                            SpellCheckTextBox.Value.Text = Word;
                            SpellingError SpellError = SpellCheckTextBox.Value.GetSpellingError(0);
                            if (SpellError != null && SpellError.Suggestions.Any())
                                Results.Add((Word, new List<string>(SpellError.Suggestions)));
                        }
                        break;
                    case 1:
                        {
                            HttpResponseMessage Response = await MiniHttpClient.PostAsync(SECRETS.GOOGLE_SPELLCHECK_ENDPOINT, new StringContent($"{{\"text\":\"{Text}\",\"language\":\"{Locale.Tooltip}\",\"originCountry\":\"USA\"}}", Encoding.UTF8, "application/json"), Token);
                            Response.EnsureSuccessStatusCode();
                            string Json = await Response.Content.ReadAsStringAsync(Token);

                            using JsonDocument Document = JsonDocument.Parse(Json);
                            if (Document.RootElement.TryGetProperty("spellingCheckResponse", out JsonElement SpellcheckResponse) && SpellcheckResponse.TryGetProperty("misspellings", out JsonElement Misspellings))
                            {
                                foreach (JsonElement Misspelling in Misspellings.EnumerateArray())
                                {
                                    int Offset = Misspelling.GetProperty("charStart").GetInt32();
                                    int Length = Misspelling.GetProperty("charLength").GetInt32();
                                    List<string> Suggestions = [];
                                    if (Misspelling.TryGetProperty("suggestions", out JsonElement Replacements))
                                    {
                                        foreach (JsonElement Replacement in Replacements.EnumerateArray())
                                            Suggestions.Add(Replacement.GetProperty("suggestion").GetString()!);
                                    }
                                    if (Suggestions.Count != 0)
                                        Results.Add((Text.Substring(Offset, Length), Suggestions));
                                }
                            }
                            break;
                        }
                    case 2:
                        {
                            HttpResponseMessage Response = await MiniHttpClient.PostAsync(SECRETS.MICROSOFT_SPELLCHECK_ENDPOINT, new StringContent($"{{\"SessionId\": \"\",\"AppId\": \"Edge_Win32\",\"LanguageUxId\": \"{Locale.Tooltip}\",\"Content\": [{{\"TileId\": \"{Locale.Tooltip}\",\"RevisionId\": \"0\",\"TileElements\": [{{\"LanguageId\": \"{Locale.Tooltip}\",\"Text\": \"{Text}\",\"TextUnit\": 8}}]}}],\"Descriptors\":[{{\"Name\": \"FlightIds\",\"Value\": \"wac-wordeditorservicemultiplegrammarcritiquespersentence-treatment\"}},{{\"Name\": \"LicenseType\",\"Value\": \"NoLicense\"}}]}}", Encoding.UTF8, "application/json"), Token);
                            Response.EnsureSuccessStatusCode();
                            string Json = await Response.Content.ReadAsStringAsync(Token);

                            using JsonDocument Document = JsonDocument.Parse(Json);
                            if (Document.RootElement.TryGetProperty("Critiques", out JsonElement Critiques))
                            {
                                foreach (JsonElement Critique in Critiques.EnumerateArray())
                                {
                                    if (!Critique.TryGetProperty("Context", out JsonElement Context))
                                        continue;

                                    List<string> Suggestions = [];
                                    if (Critique.TryGetProperty("Suggestions", out JsonElement Replacements))
                                    {
                                        foreach (JsonElement Replacement in Replacements.EnumerateArray())
                                            Suggestions.Add(Replacement.GetProperty("Text").GetString()!);
                                    }

                                    string ContextText = Context.GetString()!;
                                    int Offset = Critique.GetProperty("Start").GetInt32();
                                    int Length = Critique.GetProperty("Length").GetInt32();

                                    if (Suggestions.Count != 0)
                                        Results.Add((ContextText.Substring(Offset, Length), Suggestions));
                                }
                            }
                            break;
                        }
                    case 3:
                        {
                            string Json = await MiniHttpClient.GetStringAsync(string.Format(SECRETS.LANGUAGETOOL_SPELLCHECK_ENDPOINT, Locale.Tooltip, WebUtility.UrlEncode(Text)), Token);

                            using JsonDocument Document = JsonDocument.Parse(Json);
                            if (Document.RootElement.TryGetProperty("matches", out JsonElement Matches))
                            {
                                foreach (JsonElement Match in Matches.EnumerateArray())
                                {
                                    if (!Match.TryGetProperty("context", out JsonElement Context))
                                        continue;

                                    List<string> Suggestions = [];
                                    if (Match.TryGetProperty("replacements", out JsonElement Replacements))
                                    {
                                        foreach (JsonElement Replacement in Replacements.EnumerateArray())
                                            Suggestions.Add(Replacement.GetProperty("value").GetString()!);
                                    }

                                    string ContextText = Context.GetProperty("text").GetString()!;
                                    int Offset = Match.GetProperty("offset").GetInt32();
                                    int Length = Match.GetProperty("length").GetInt32();

                                    if (Suggestions.Count != 0)
                                        Results.Add((ContextText.Substring(Offset, Length), Suggestions));
                                }
                            }
                            break;
                        }
                    case 4:
                        {
                            string Json = await MiniHttpClient.GetStringAsync(string.Format(SECRETS.YANDEX_SPELLCHECK_ENDPOINT, Locale.Tooltip, WebUtility.UrlEncode(Text)), Token);

                            using JsonDocument Document = JsonDocument.Parse(Json);
                            if (Document.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement Match in Document.RootElement.EnumerateArray())
                                {
                                    string Word = Match.GetProperty("word").GetString()!;
                                    List<string> Suggestions = [];
                                    if (Match.TryGetProperty("s", out JsonElement Replacements))
                                    {
                                        foreach (JsonElement Replacement in Replacements.EnumerateArray())
                                            Suggestions.Add(Replacement.GetString()!);
                                    }
                                    if (Suggestions.Count != 0)
                                        Results.Add((Word, Suggestions));
                                }
                            }
                            break;
                        }
                }
            }
            catch { }
            return Results;
        }

        public static string GetMiniSearchType(string Text)
        {
            if (Text.StartsWith("search:"))
                return "S";
            else if (Text.StartsWith("domain:"))
                return "W";
            else if (Utils.IsProgramUrl(Text))
                return "P";
            else if (Utils.IsCode(Text))
                return "C";
            else if (Text.StartsWith("file:///"))
                return "F";
            else if (Utils.IsUrl(Text))
                return "W";
            return "S";
        }

        public static int GetSmartType(string Text)
        {
            if (Utils.MathRegex().IsMatch(Text))
                return 1;
            else if (Text.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                return 2;
            else if (Text.StartsWith("weather ", StringComparison.OrdinalIgnoreCase))
                return 3;
            else if (Text.StartsWith("translate ", StringComparison.OrdinalIgnoreCase))
                return 4;
            else if (Utils.SimpleCurrencyRegex().IsMatch(Text))
                return 5;
            return -1;
        }

        public async Task<OmniSuggestion> GenerateSmartSuggestion(string Text, int Type)
        {
            OmniSuggestion Suggestion = new() { Text = Text, Display = Text, Icon = "\xE721" };
            switch (Type)
            {
                case 1:
                    try
                    {
                        Suggestion.SubText = $"= {new DataTable().Compute(Text, null)?.ToString()}";
                        Suggestion.Icon = "\xE8EF";
                    }
                    catch { }
                    break;
                case 2:
                    try
                    {
                        string Response = await MiniHttpClient.GetStringAsync($"https://api.dictionaryapi.dev/api/v2/entries/en/{Text.Substring(7).Trim()}");
                        using JsonDocument Document = JsonDocument.Parse(Response);
                        string Result = Document.RootElement[0].GetProperty("meanings")[0].GetProperty("definitions")[0].GetProperty("definition").GetString();
                        Suggestion.SubText = $"- {Result}";
                        Suggestion.Icon = "\xE82D";
                    }
                    catch { }
                    break;
                case 3:
                    Suggestion.Icon = "\xE9CA";
                    string Location = Regex.Replace(Text, @"^weather(\s+in)?\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
                    try
                    {
                        HttpResponseMessage Response = MiniHttpClient.GetAsync($"https://api.openweathermap.org/data/2.5/weather?lang=en&q={Location}&appid={SECRETS.WEATHER_API_KEY}&units=metric").Result;
                        if (Response.IsSuccessStatusCode)
                        {
                            using JsonDocument Document = JsonDocument.Parse(Response.Content.ReadAsStringAsync().Result);
                            double Temperature = Document.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
                            string Description = Document.RootElement.GetProperty("weather")[0].GetProperty("description").GetString().ToTitleCase();

                            Suggestion.SubText = $"{Temperature} °C | {Description}";
                        }
                        else
                            Suggestion.SubText = $"- No data";
                    }
                    catch (OperationCanceledException) { }
                    catch { Suggestion.SubText = "- Unavailable"; }
                    break;
                case 4:
                    Match TranslateMatch = Utils.TranslateRegex().Match(Text);
                    if (TranslateMatch.Success)
                    {
                        Suggestion.SubText = "- Unavailable";
                        Suggestion.Icon = "\xE8C1";
                        string LanguageInput = TranslateMatch.Groups["Lang"].Value.Trim().ToLowerInvariant();
                        string LanguageCode = (LanguageInput.Length == 2) ? LanguageInput : AllLocales.FirstOrDefault(x => x.Value.Contains(LanguageInput, StringComparison.OrdinalIgnoreCase)).Key;
                        if (!string.IsNullOrEmpty(LanguageCode))
                        {
                            try
                            {
                                string Response = await MiniHttpClient.GetStringAsync($"https://translate.googleapis.com/translate_a/single?client=gtx&dt=t&sl=auto&tl={LanguageCode}&q={Uri.EscapeDataString(TranslateMatch.Groups["Phrase"].Value.AsSpan().Trim())}");
                                using JsonDocument Document = JsonDocument.Parse(Response);
                                Suggestion.SubText = $"- {Document.RootElement[0][0][0].GetString()}";
                            }
                            catch { }
                        }
                    }
                    break;
                case 5:
                    Match CurrencyMatch = Utils.CurrencyRegex().Match(Text);
                    if (CurrencyMatch.Success)
                    {
                        Suggestion.SubText = "- Unavailable";
                        Suggestion.Icon = "\xe8ee";
                        string Amount = CurrencyMatch.Groups["Amount"].Value;
                        string From = CurrencyMatch.Groups["From"].Value.ToUpper();
                        string To = CurrencyMatch.Groups["To"].Value.ToUpper();
                        try
                        {
                            string Response = await MiniHttpClient.GetStringAsync($"https://api.frankfurter.app/latest?amount={Amount}&from={From}&to={To}");
                            using JsonDocument Document = JsonDocument.Parse(Response);
                            if (Document.RootElement.TryGetProperty("rates", out JsonElement Rates) && Rates.TryGetProperty(To, out JsonElement Output))
                                Suggestion.SubText = $"- {Amount} {From} ≈ {Output.GetDouble():0.00} {To}";
                        }
                        catch { }
                    }
                    break;
            }
            return Suggestion;
        }

        public bool Background = false;

        public static HttpClient MiniHttpClient = HttpClientFactory.Create(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        });
        /*public static HttpClient MimicHttpClient = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });*/
        public static Random MiniRandom = new();
        public static QREncoder MiniQREncoder = new();

        public List<IntPtr> WebView2DevTools = [];

        public async void InitializeApp(IEnumerable<string> Args, Profile? SelectedProfile = null)
        {
            if (SelectedProfile != null && SelectedProfile.Type == ProfileType.System && SelectedProfile.Name == "Guest")
                ReadOnlyInstance = true;
            JumpList _JumpList = new()
            {
                ShowRecentCategory = true,
                ShowFrequentCategory = true
            };
            JumpTask NewWindowTask = new()
            {
                Title = "New window",
                Description = "Open a new browser window",
                ApplicationPath = ExecutablePath,
                Arguments = "--window",
                IconResourcePath = ExecutablePath,
                IconResourceIndex = 0
            };
            _JumpList.JumpItems.Add(NewWindowTask);
            JumpList.SetJumpList(Current, _JumpList);
            _JumpList.Apply();

            string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";
            string CommandLineUrl = string.Empty;
            foreach (string Flag in Args)
            {
                if (Flag == "--background")
                    Background = true;
                else if (Flag == "--window")
                {
                    Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                    if (OtherInstance != null)
                    {
                        MessageHelper.SendDataMessage(OtherInstance, "NewWindow");
                        Shutdown(1);
                        Environment.Exit(0);
                        return;
                    }
                }
                else
                {
                    if (Flag.StartsWith("--"))
                        continue;
                    CommandLineUrl = Flag;
                }
            }
            CurrentProfile = SelectedProfile;
            if (CurrentProfile == null)
            {
                CurrentProfile = Profiles.FirstOrDefault(i => i.Default);
                if (CurrentProfile == null)
                {
                    CurrentProfile = new Profile { Name = "Default", Default = true };
                    Profiles.Insert(0, CurrentProfile);
                }
            }
            AppUserModelID = $"SLT.SLBr.{CurrentProfile.Name}";
            DllUtils.SetCurrentProcessExplicitAppUserModelID(AppUserModelID);

            _Mutex = new Mutex(true, AppUserModelID);
            if (string.IsNullOrEmpty(CommandLineUrl))
            {
                if (!_Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                    if (OtherInstance != null)
                        MessageHelper.SendDataMessage(OtherInstance, "Start<|>" + CurrentProfile.Name);
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                if (OtherInstance != null)
                {
                    MessageHelper.SendDataMessage(OtherInstance, "Url<|>"+CommandLineUrl);
                    Shutdown(1);
                    Environment.Exit(0);
                    return;
                }
            }

            MiniHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentGenerator.BuildChromeBrand());
            MiniHttpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            Dispatcher.UnhandledException += App_DispatcherUnhandledException;

            /*TODO: Investigate error "A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (Hwnd of zero is not valid.)" "System.ArgumentException: Hwnd of zero is not valid."
             * Reproduction: Delete AppData\Local\SLBr, run in Debug, create & open new profile, observe MessageBox.
             * Issue only reproducable on first-time run of profile.
             */

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Set Google API keys. See http://www.chromium.org/developers/how-tos/api-keys
            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/google_api_keys.h
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);

            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", CurrentProfile.Name);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            ExtensionsPath = Path.Combine(UserApplicationDataPath, "User Data", "Default", "Extensions");
            ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            AdBlockDataPath = Path.Combine(UserApplicationDataPath, "Filters");
            NotificationTempPath = Path.Combine(Path.GetTempPath(), "SLBr_NotificationCache");
            //CdnPath = Path.Combine(ResourcesPath, "cdn");

            LocaleNames = AllLocales.Select(i => i.Value).ToList();

            InfoBars.CollectionChanged += InfoBars_CollectionChanged;

            FavouriteColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
            FavouriteColor.SafeFreeze();
            SLBrColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#0092FF");
            SLBrColor.SafeFreeze();
            RedColor = new SolidColorBrush(Colors.Red);
            RedColor.SafeFreeze();
            CornflowerBlueColor = new SolidColorBrush(Colors.CornflowerBlue);
            CornflowerBlueColor.SafeFreeze();
            NavajoWhiteColor = new SolidColorBrush(Colors.NavajoWhite);
            NavajoWhiteColor.SafeFreeze();
            LimeGreenColor = new SolidColorBrush(Colors.LimeGreen);
            LimeGreenColor.SafeFreeze();
            GreenColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#3AE872");
            GreenColor.SafeFreeze();
            OrangeColor = new SolidColorBrush(Colors.Orange);
            OrangeColor.SafeFreeze();
            WhiteColor = new SolidColorBrush(Colors.White);
            WhiteColor.SafeFreeze();
            IconFont = (FontFamily)Resources["IconFontFamily"];
            SLBrFont = new FontFamily(new Uri("pack://application:,,,/SLBr;component/"), "./Fonts/#SLBr Icons");
            await InitializeSaves();

            //MimicHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            //MimicHttpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            /*MimicHttpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            MimicHttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", UserAgentBrandsString.Replace("\r", "").Replace("\n", "").Trim());
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Arch", UserAgentGenerator.GetCPUArchitecture());
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Model", "\"\"");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform-Version", "\"19.0.0\"");


            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            MimicHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            MimicHttpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "?1");*/

            InitializeBrowser();
            InitializeUISaves(CommandLineUrl);
            if (Environment.IsPrivilegedProcess)
            {
                try
                {
                    using (var CheckKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
                    {
                        if (CheckKey?.GetValue("SLBr") == null)
                        {
                            using (var Key = Registry.ClassesRoot.CreateSubKey("SLBr", true))
                            {
                                Key.SetValue(null, "SLBr Document");
                                Key.SetValue("AppUserModelId", "SLBr");

                                RegistryKey ApplicationRegistry = Key.CreateSubKey("Application", true);
                                ApplicationRegistry.SetValue("AppUserModelId", "SLBr");
                                ApplicationRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                                ApplicationRegistry.SetValue("ApplicationName", "SLBr");
                                ApplicationRegistry.SetValue("ApplicationCompany", "SLT Softwares");
                                ApplicationRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                                ApplicationRegistry.Close();

                                RegistryKey IconRegistry = Key.CreateSubKey("DefaultIcon", true);
                                IconRegistry.SetValue(null, $"{ExecutablePath},0");
                                IconRegistry.Close();

                                RegistryKey CommandRegistry = Key.CreateSubKey("shell\\open\\command", true);
                                CommandRegistry.SetValue(null, $"\"{ExecutablePath}\" \"%1\"");
                                CommandRegistry.Close();
                            }
                            using (var Key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Clients\\StartMenuInternet", true).CreateSubKey("SLBr", true))
                            {
                                if (Key.GetValue(null) as string != "SLBr")
                                    Key.SetValue(null, "SLBr");

                                RegistryKey CapabilitiesRegistry = Key.CreateSubKey("Capabilities", true);
                                CapabilitiesRegistry.SetValue("ApplicationDescription", "Browse the web with a fast, lightweight web browser.");
                                CapabilitiesRegistry.SetValue("ApplicationIcon", $"{ExecutablePath},0");
                                CapabilitiesRegistry.SetValue("ApplicationName", $"SLBr");

                                RegistryKey StartMenuRegistry = CapabilitiesRegistry.CreateSubKey("Startmenu", true);
                                StartMenuRegistry.SetValue("StartMenuInternet", "SLBr");
                                StartMenuRegistry.Close();

                                RegistryKey FileAssociationsRegistry = CapabilitiesRegistry.CreateSubKey("FileAssociations", true);
                                FileAssociationsRegistry.SetValue(".xhtml", "SLBr");
                                FileAssociationsRegistry.SetValue(".xht", "SLBr");
                                FileAssociationsRegistry.SetValue(".shtml", "SLBr");
                                FileAssociationsRegistry.SetValue(".html", "SLBr");
                                FileAssociationsRegistry.SetValue(".htm", "SLBr");
                                FileAssociationsRegistry.SetValue(".pdf", "SLBr");
                                FileAssociationsRegistry.SetValue(".svg", "SLBr");
                                FileAssociationsRegistry.SetValue(".webp", "SLBr");
                                FileAssociationsRegistry.Close();

                                RegistryKey URLAssociationsRegistry = CapabilitiesRegistry.CreateSubKey("URLAssociations", true);
                                URLAssociationsRegistry.SetValue("http", "SLBr");
                                URLAssociationsRegistry.SetValue("https", "SLBr");
                                URLAssociationsRegistry.Close();

                                CapabilitiesRegistry.Close();

                                RegistryKey DefaultIconRegistry = Key.CreateSubKey("DefaultIcon", true);
                                DefaultIconRegistry.SetValue(null, $"{ExecutablePath},0");
                                DefaultIconRegistry.Close();

                                RegistryKey CommandRegistry = Key.CreateSubKey("shell\\open\\command", true);
                                CommandRegistry.SetValue(null, $"\"{ExecutablePath}\"");
                                CommandRegistry.Close();
                            }
                            CheckKey?.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                        }
                    }
                }
                catch { }
            }
            try
            {
                using var CheckKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\AppUserModelId\\SLBr.Toast", false);
                if (CheckKey == null)
                {
                    using RegistryKey Key = Registry.CurrentUser.CreateSubKey("Software\\Classes\\AppUserModelId\\SLBr.Toast");
                    if (Key != null)
                    {
                        Key.SetValue("DisplayName", "SLBr", RegistryValueKind.String);
                        Key.SetValue("IconUri", Path.Combine(ResourcesPath, "SLBr.ico"), RegistryValueKind.String);
                    }
                }
            }
            catch { }
            AppInitialized = true;
            _ = CleanTempCache();
            if (!Background)
                ContinueBackgroundInitialization();
        }

        //TODO: Implement full https://developer.mozilla.org/en-US/docs/Web/API/Notification API support.
        //, bool RTL = false
        public static void ShowPortableNotification(string Title, string Body, string SubText, string? Icon, string? Image, bool Silent = false)
        {
            StringBuilder XML = new(); XML.Append("<toast>");
            if (Silent)
                XML.Append("<audio silent='true'/>");
            XML.Append("<visual><binding template='ToastGeneric'>");
            if (!string.IsNullOrEmpty(Icon) && (Utils.IsHttpScheme(Icon) || File.Exists(Icon)))
                XML.Append("<image placement='appLogoOverride' src='").Append(SecurityElement.Escape(Icon)).Append("'/>");

            if (!string.IsNullOrEmpty(Image) && (Utils.IsHttpScheme(Image) || File.Exists(Image)))
                XML.Append("<image placement='hero' src='").Append(SecurityElement.Escape(Image)).Append("'/>");
            XML.Append("<text>").Append(SecurityElement.Escape(Title)).Append("</text>");
            XML.Append("<text>").Append(SecurityElement.Escape(Body)).Append("</text>");
            XML.Append("<text>").Append(SecurityElement.Escape(SubText)).Append("</text>");
            XML.Append("</binding></visual></toast>");
            string Script = $@"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime] | Out-Null
$XML = [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType=WindowsRuntime]::new()

$XML.LoadXml('{XML.ToString().Replace("'", "''")}')

$Toast = [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType=WindowsRuntime]::new($XML)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('SLBr.Toast').Show($Toast)";

            Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Sta -EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(Script))}",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }

        public async Task<string?> DownloadNotificationAsset(string Url)
        {
            if (string.IsNullOrEmpty(Url) || !Utils.IsHttpScheme(Url))
                return null;
            //if (!Utils.IsHttpScheme(Url))
            //    return File.Exists(Url) ? "file:///" + Path.GetFullPath(Url).Replace("\\", "/") : null;
            try
            {
                string Extension = Utils.GetFileExtension(Url).ToLower();
                if (string.IsNullOrEmpty(Extension) || Extension is not ".png" and not ".jpg" and not ".jpeg" and not ".gif" and not ".webp" and not ".bmp" and not ".ico")
                    return null;
                if (!Directory.Exists(NotificationTempPath))
                    Directory.CreateDirectory(NotificationTempPath);
                string TargetPath = Path.Combine(NotificationTempPath, $"{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Url)))}{Extension}");
                if (File.Exists(TargetPath))
                    return TargetPath;
                /*byte[] Data = await MiniHttpClient.GetByteArrayAsync(Url);
                if (Data == null || Data.Length < 4)
                    return null;
                if (!Utils.IsValidImageFromBytes(Data))
                    return null;
                await File.WriteAllBytesAsync(TargetPath, Data);
                return TargetPath;*/

                using (var HeadRequest = new HttpRequestMessage(HttpMethod.Head, Url))
                using (var HeaderResponse = await MiniHttpClient.SendAsync(HeadRequest, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (HeaderResponse.IsSuccessStatusCode && HeaderResponse.Content.Headers.ContentType != null)
                    {
                        string MimeType = HeaderResponse.Content.Headers.ContentType.MediaType.ToLower();
                        if (!MimeType.StartsWith("image/") && MimeType != "application/octet-stream")
                            return null;
                    }
                }

                using var Response = await MiniHttpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
                if (!Response.IsSuccessStatusCode) return null;

                using Stream NetworkStream = await Response.Content.ReadAsStreamAsync();
                using MemoryStream _MemoryStream = new();
                byte[] Buffer = new byte[4096];
                int BytesRead;
                bool SignatureVerified = false;

                while ((BytesRead = await NetworkStream.ReadAsync(Buffer)) > 0)
                {
                    _MemoryStream.Write(Buffer, 0, BytesRead);
                    if (!SignatureVerified && _MemoryStream.Length >= 12)
                    {
                        if (!Utils.IsValidImageBytes(_MemoryStream.ToArray()))
                            return null;
                        SignatureVerified = true;
                    }
                }
                if (!SignatureVerified && !Utils.IsValidImageBytes(_MemoryStream.ToArray()))
                    return null;
                await File.WriteAllBytesAsync(TargetPath, _MemoryStream.ToArray());
                return TargetPath;
            }
            catch
            {
                return null;
            }
        }

        public async Task CleanTempCache(bool Force = false)
        {
            try
            {
                if (!Directory.Exists(NotificationTempPath))
                    return;
                if (Force)
                    Directory.Delete(NotificationTempPath, true);
                else
                {
                    DirectoryInfo DirectoryInfo = new(NotificationTempPath);
                    DateTime Threshold = DateTime.Now.AddDays(-7);
                    foreach (FileInfo File in DirectoryInfo.GetFiles())
                    {
                        if (File.LastAccessTime < Threshold)
                            File.Delete();
                    }
                }
            }
            catch { }
        }

        private void InfoBars_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser _Browser in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                    _Browser.SyncInfobars();
            }
        }

        public Theme GenerateTheme(Color BaseColor, string Name = "Temp")
        {
            double a = 1 - (0.299 * BaseColor.R + 0.587 * BaseColor.G + 0.114 * BaseColor.B) / 255;
            Theme SiteTheme;
            if (a < 0.4)
            {
                SiteTheme = new Theme(Name, Themes[0])
                {
                    FontColor = Colors.Black,
                    DarkTitleBar = false,
                    DarkWebPage = false
                };
            }
            else if (a < 0.7)
            {
                SiteTheme = new Theme(Name, Themes[0])
                {
                    FontColor = Colors.White,
                    DarkTitleBar = false,
                    DarkWebPage = false,
                    SecondaryColor = Color.FromArgb(BaseColor.A, (byte)Math.Min(255, BaseColor.R * 0.95f), (byte)Math.Min(255, BaseColor.G * 0.95f), (byte)Math.Min(255, BaseColor.B * 0.95f)),
                    BorderColor = Color.FromArgb(BaseColor.A, (byte)Math.Min(255, BaseColor.R * 0.90f), (byte)Math.Min(255, BaseColor.G * 0.90f), (byte)Math.Min(255, BaseColor.B * 0.90f)),
                    GrayColor = Color.FromArgb(BaseColor.A, (byte)Math.Min(255, BaseColor.R * 0.75f), (byte)Math.Min(255, BaseColor.G * 0.75f), (byte)Math.Min(255, BaseColor.B * 0.75f))
                };
            }
            else
            {
                SiteTheme = new Theme(Name, Themes[1])
                {
                    FontColor = Colors.White,
                    DarkTitleBar = true,
                    DarkWebPage = true,
                    SecondaryColor = Color.FromArgb(BaseColor.A, (byte)Math.Max(0, BaseColor.R * 1.25f), (byte)Math.Max(0, BaseColor.G * 1.25f), (byte)Math.Max(0, BaseColor.B * 1.25f)),
                    BorderColor = Color.FromArgb(BaseColor.A, (byte)Math.Max(0, BaseColor.R * 1.35f), (byte)Math.Max(0, BaseColor.G * 1.35f), (byte)Math.Max(0, BaseColor.B * 1.35f)),
                    GrayColor = Color.FromArgb(BaseColor.A, (byte)Math.Max(0, BaseColor.R * 1.95f), (byte)Math.Max(0, BaseColor.G * 1.95f), (byte)Math.Max(0, BaseColor.B * 1.95f))
                };
            }
            SiteTheme.PrimaryColor = BaseColor;
            return SiteTheme;
        }

        public void SwitchTab(int ID)
        {
            foreach (MainWindow _Window in AllWindows)
            {
                BrowserTabItem? Tab = _Window.GetBrowserTabWithId(ID);
                if (Tab != null)
                {
                    _Window.TabsUI.SelectedItem = Tab;
                    if (_Window.WindowState == WindowState.Minimized)
                        _Window.WindowState = WindowState.Maximized;
                    _Window.ShowInTaskbar = true;
                    _Window.Show();
                    _Window.Activate();
                    break;
                }
            }
        }

        public string UpdateAvailable = string.Empty;

        public void ContinueBackgroundInitialization()
        {
            foreach (MainWindow _Window in AllWindows)
            {
                _Window.WindowState = WindowState.Maximized;
                _Window.ShowInTaskbar = true;
                _Window.Show();
                _Window.Activate();
            }
            Background = false;
            if (Environment.IsPrivilegedProcess)
                InfoBars.Add(new() { Icon = "\ue7ba", IconForeground = OrangeColor, Title = "Elevated Privileges Detected", Description = [new() { Text = "SLBr is running with administrator privileges, which may pose security risks. It is recommended to run SLBr without elevated rights." }] });
            if (Utils.IsInternetAvailable())
            {
                if (bool.Parse(GlobalSave.Get("CheckUpdate")))
                    CheckUpdate();
            }
        }

        public async Task CheckUpdate()
        {
            if (!string.IsNullOrEmpty(UpdateAvailable))
                return;
            try
            {
                using HttpRequestMessage Request = new(HttpMethod.Get, "https://api.github.com/repos/slt-world/slbr/releases/latest");
                Request.Headers.UserAgent.ParseAdd(UserAgentGenerator.BuildChromeBrand());
                Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                using var Response = await MiniHttpClient.SendAsync(Request);
                Response.EnsureSuccessStatusCode();
                string Data = await Response.Content.ReadAsStringAsync();
                using JsonDocument Document = JsonDocument.Parse(Data);
                string NewVersion = Document.RootElement.GetProperty("tag_name").ToString();
                if (!NewVersion.StartsWith(ReleaseVersion))
                {
                    UpdateAvailable = NewVersion;
                    foreach (MainWindow _Window in AllWindows)
                    {
                        foreach (Browser _Browser in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                        {
                            _Browser.NewUpdateMenu.Visibility = Visibility.Visible;
                            _Browser.NewUpdateMenuSeparator.Visibility = Visibility.Visible;
                        }
                    }
                    ShowUpdateInfoBar();
                }
                else
                    UpdateAvailable = ReleaseVersion;
            }
            catch { }
        }

        public void ShowUpdateInfoBar()
        {
            if (NewUpdateInfoBar != null)
                return;
            NewUpdateInfoBar = new()
            {
                Icon = "\xe895",
                Title = "Update Available",
                Description = [new() { Text = "A newer version of SLBr is ready for download." }],
                Actions = [new() { Text = "Update", Command = new RelayCommand(() => { CloseInfoBar(NewUpdateInfoBar); Update(); }) }]
            };
            InfoBars.Add(NewUpdateInfoBar);
        }

        InfoBar? NewUpdateInfoBar = null;

        public void CloseInfoBar(InfoBar Bar)
        {
            InfoBars.Remove(Bar);
            if (Bar == NewUpdateInfoBar)
                NewUpdateInfoBar = null;
        }

        public void Update()
        {
            string AppDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string TemporaryUpdater = Path.Combine(Path.GetTempPath(), "SLBr_Updater.exe");

            if (File.Exists(TemporaryUpdater))
                File.Delete(TemporaryUpdater);
            File.Copy(Path.Combine(AppDirectory, "Updater.exe"), TemporaryUpdater, true);

            Process.Start(new ProcessStartInfo
            {
                FileName = TemporaryUpdater,
                Arguments = AppDirectory.Contains(' ') ? $"\"{AppDirectory}\"" : AppDirectory,
                UseShellExecute = false
            });

            CloseSLBr();
        }

        /*public static async Task ReportInfo(string Content)
        {
            //TODO
        }*/

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
#if DEBUG
            ReportError(e.Exception.Flatten());
#endif
#if !DEBUG
            Save();
            e.SetObserved();
#endif
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            ReportError(e.ExceptionObject as Exception);
#endif
#if !DEBUG
            Save();
#endif
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            ReportError(e.Exception);
#endif
#if !DEBUG
            Save();
            e.Handled = true;
#endif
        }

#if DEBUG
        private void ReportError(Exception Error)
        {
            string Report = string.Format(ReportExceptionText,
                ReleaseVersion,
                Cef.CefVersion,
                WebViewManager.WebView2Version,
                Error.Message,
                Error.Source,
                Error.TargetSite,
                Error.StackTrace,
                FormatInnerException(Error));
            //if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
            //    ReportInfo(Report);
            MessageBox.Show(Report);
            Dispatcher.BeginInvoke(() => Clipboard.SetText(Report));
        }

        private static string FormatInnerException(Exception Error)
        {
            StringBuilder Builder = new();
            int Depth = 0;

            while (Error.InnerException != null)
            {
                Error = Error.InnerException;
                Builder.AppendLine($"{new string(' ', Depth * 2)}--> {Error.GetType().FullName}: {Error.Message}");
                Depth++;
            }

            return Builder.Length == 0 ? "None" : Builder.ToString();
        }
#endif

        public static readonly Dictionary<string, Type> CustomPageOverlays = new()
        {
            { "settings", typeof(SettingsPage) },
            { "favourites", typeof(FavouritesPage) },
            { "history", typeof(HistoryPage) },
            { "downloads", typeof(DownloadsPage) },
        };

#if DEBUG
        const string ReportExceptionText = @"Automatic Report
- Version: {0}
- CEF: {1}
- WebView2: {2}
- Message: {3}
- Source: {4}
- Target Site: {5}

Stack Trace: {6}

Inner Exception: {7}";
#endif
        public int AdsBlocked;

        public int AdBlock;
        public bool AMP;

        //https://chromium-review.googlesource.com/c/chromium/src/+/1265506
        public bool NeverSlowMode;

        public bool ModernURL;
        public bool PunycodeURL;
        public bool TrimURL;
        public bool HomographProtection;
        public bool ExternalFonts;
        public bool SmartDarkMode;
        public bool TabPreview;
        public bool TabMemory;

        public int BlockScreenCapture;

        public void SetBlockScreenCapture(int _BlockScreenCapture)
        {
            GlobalSave.Set("BlockScreenCapture", _BlockScreenCapture);
            BlockScreenCapture = _BlockScreenCapture;
            foreach (MainWindow _Window in AllWindows)
                _Window.SetWindowDisplayAffinity();
        }

        public void SetTabMemory(bool Toggle)
        {
            GlobalSave.Set("TabMemory", Toggle.ToString());
            TabMemory = Toggle;
        }

        public void SetTabPreview(bool Toggle)
        {
            GlobalSave.Set("TabPreview", Toggle.ToString());
            TabPreview = Toggle;
            if (!Toggle)
            {
                foreach (MainWindow _Window in AllWindows)
                {
                    foreach (BrowserTabItem Tab in _Window.Tabs)
                        Tab.Preview = null;
                }
            }
        }

        public void SetSmartDarkMode(bool Toggle)
        {
            GlobalSave.Set("SmartDarkMode", Toggle.ToString());
            SmartDarkMode = Toggle;
        }

        public void SetExternalFonts(bool Toggle)
        {
            GlobalSave.Set("ExternalFonts", Toggle.ToString());
            ExternalFonts = Toggle;
        }

        public WebSecurityService WebRiskService;
        public DownloadSecurityService DownloadSecurityService;

        public void SetDownloadSecurityService(int Service)
        {
            GlobalSave.Set("DownloadSecurityService", Service);
            DownloadSecurityService = (DownloadSecurityService)Service;
            _DownloadRiskHandler?.SafeHashes.Clear();
        }

        public void SetWebRiskService(int Service)
        {
            GlobalSave.Set("WebRiskService", Service);
            WebRiskService = (WebSecurityService)Service;
            _WebRiskHandler?.SafeHashes.Clear();
            var ToRemove = WebViewManager.OverrideRequests.Where(i => i.Value.Error != -1).Select(i => i.Key);
            foreach (var Key in ToRemove)
                WebViewManager.UnregisterOverrideRequest(Key);
        }
        public void SetTrimURL(bool Toggle)
        {
            GlobalSave.Set("TrimURL", Toggle.ToString());
            TrimURL = Toggle;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                {
                    if (BrowserView.OmniBoxOverlayText.Visibility == Visibility.Visible)
                        BrowserView.SetOverlayDisplay(TrimURL, HomographProtection);
                }
            }
        }
        public void SetModernURL(bool Toggle)
        {
            GlobalSave.Set("ModernURL", Toggle.ToString());
            ModernURL = Toggle;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                {
                    if (BrowserView.OmniBoxOverlayText.Visibility == Visibility.Visible)
                        BrowserView.SetOverlayDisplay(TrimURL, HomographProtection);
                }
            }
        }
        public void SetPunycodeURL(bool Toggle)
        {
            GlobalSave.Set("PunycodeURL", Toggle.ToString());
            PunycodeURL = Toggle;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                {
                    if (BrowserView.OmniBoxOverlayText.Visibility == Visibility.Visible)
                        BrowserView.SetOverlayDisplay(TrimURL, HomographProtection);
                }
            }
        }
        public void SetHomographProtection(bool Toggle)
        {
            GlobalSave.Set("HomographProtection", Toggle.ToString());
            HomographProtection = Toggle;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                {
                    if (BrowserView.OmniBoxOverlayText.Visibility == Visibility.Visible)
                        BrowserView.SetOverlayDisplay(TrimURL, HomographProtection);
                }
            }
        }
        public void SetNeverSlowMode(bool Toggle)
        {
            GlobalSave.Set("NeverSlowMode", Toggle.ToString());
            NeverSlowMode = Toggle;
        }
        public void SetAdBlock(int Type)
        {
            if (Type > 1)
                Type = 1;
            GlobalSave.Set("AdBlock", Type);
            AdBlock = Type;
            if (AdBlock != 0 && _AdBlockHandler == null)
            {
                _AdBlockHandler = new AdBlockHandler(AllowListSave);
                Dispatcher.BeginInvoke(async () =>
                {
                    await SetAdBlockLists();
                });
            }
        }
        public async Task SetAdBlockLists()
        {
            if (AdBlock == 0 || _AdBlockHandler == null)
                return;
            _AdBlockHandler.Clear();
            if (!Directory.Exists(AdBlockDataPath))
                Directory.CreateDirectory(AdBlockDataPath);
            foreach (AdBlockList List in AdBlockLists)
            {
                if (List.IsEnabled)
                {
                    string FileName = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(List.Url))) + ".txt";
                    string FilePath = Path.Combine(AdBlockDataPath, FileName);
                    if (File.Exists(FilePath))
                        _AdBlockHandler.ParseAdd(await File.ReadAllTextAsync(FilePath));
                    else
                    {
                        try
                        {
                            using CancellationTokenSource _CancellationTokenSource = new(TimeSpan.FromSeconds(120));
                            using HttpResponseMessage Response = await MiniHttpClient.GetAsync(List.Url, HttpCompletionOption.ResponseHeadersRead, _CancellationTokenSource.Token);
                            Response.EnsureSuccessStatusCode();
                            using Stream _Stream = await Response.Content.ReadAsStreamAsync(_CancellationTokenSource.Token);
                            using FileStream _FileStream = new(FilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

                            await _Stream.CopyToAsync(_FileStream, _CancellationTokenSource.Token);
                            _FileStream.Close();
                            _AdBlockHandler.ParseAdd(await File.ReadAllTextAsync(FilePath));
                        }
                        catch
                        {
                            if (File.Exists(FilePath))
                            {
                                try { File.Delete(FilePath); } catch { }
                            }
                        }
                    }
                }
            }
        }
        public void SetAMP(bool Toggle)
        {
            GlobalSave.Set("AMP", Toggle.ToString());
            AMP = Toggle;
        }
        public void SetRenderMode(int Mode)
        {
            RenderOptions.ProcessRenderMode = Mode == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
            GlobalSave.Set("RenderMode", Mode);
        }
        public void UpdateTabUnloadingTimer(int Time = -1)
        {
            if (Time != -1)
            {
                GlobalSave.Set("TabUnloadingTime", Time);
                GCTimerDuration = Time;
            }
            GCTimer?.Stop();
            if (!bool.Parse(GlobalSave.Get("TabUnloading")))
            {
                ResetGCProgress();
                return;
            }
            GCTimer ??= new DispatcherTimer();
            if (bool.Parse(GlobalSave.Get("ShowUnloadProgress")))
            {
                GCTimer.Interval = TimeSpan.FromMilliseconds(250);
                GCTimer.Tick -= GCCollect_EfficientTick;
                GCTimer.Tick += GCCollect_Tick;
            }
            else
            {
                ResetGCProgress();
                ScheduleNextEfficientTick();
                GCTimer.Tick -= GCCollect_Tick;
                GCTimer.Tick += GCCollect_EfficientTick;
            }
            GCTimer.Start();
        }

        public void ResetGCProgress()
        {
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (BrowserTabItem _Tab in _Window.Tabs)
                    _Tab.ProgressBarVisibility = Visibility.Collapsed;
            }
        }

        public void ScheduleNextEfficientTick()
        {
            DateTime? Next = AllWindows.SelectMany(i => i.Tabs).Where(i => !i.IsUnloaded && i.Content != null).Select(i => i.Content.NextUnloadTime).OrderBy(i => i).FirstOrDefault();
            if (Next == null)
            {
                GCTimer.Interval = TimeSpan.FromMinutes(1);
                return;
            }
            TimeSpan Delay = Next.Value - DateTime.Now;
            GCTimer.Interval = Delay > TimeSpan.Zero ? Delay : TimeSpan.Zero;
        }

        private void GCCollect_Tick(object? sender, EventArgs e)
        {
            DateTime Now = DateTime.Now;
            foreach (MainWindow _Window in AllWindows)
            {
                if (_Window.WindowState == WindowState.Minimized)
                    continue;
                foreach (BrowserTabItem Tab in _Window.Tabs)
                {
                    if (Tab.IsUnloaded || Tab.Content == null)
                    {
                        Tab.ProgressBarVisibility = Visibility.Collapsed;
                        continue;
                    }

                    double Progress = Math.Min((Now - Tab.Content.LastActive).TotalSeconds / (GCTimerDuration * 60.0), 1.0);

                    Tab.Progress = Progress;
                    Tab.ProgressBarVisibility = Visibility.Visible;

                    if (Progress >= 1)
                        _Window.UnloadTab(Tab.Content);
                }
            }
        }
        private void GCCollect_EfficientTick(object? sender, EventArgs e)
        {
            foreach (MainWindow _Window in AllWindows)
                _Window.UnloadTabs();
            ScheduleNextEfficientTick();
        }

        public DispatcherTimer GCTimer;

        public int GCTimerDuration;

        public void SwitchUserPopup() =>
            new ProfileManagerWindow().Show();

        public void CopyToClipboard(object Object, int Type)
        {
            if (Object is string Text)
                Clipboard.SetText(Text);
            else if (Object is BitmapSource Source)
                Clipboard.SetImage(Source);
            switch (Type)
            {
                case 0:
                    CurrentFocusedWindow().OpenToast("Link copied", "\xe71b");
                    break;
                case 1:
                    CurrentFocusedWindow().OpenToast("Image copied", "\xe8c8");
                    break;
                default:
                    CurrentFocusedWindow().OpenToast("Text copied", "\xe8c8");
                    break;
            }
        }

        public void SaveOpenSearch(string Name, string Url)
        {
            try
            {
                if (SearchEngines.Any(x => x.Name == Name))
                    return;
                string Response = MiniHttpClient.GetStringAsync(Url).Result;
                XDocument XML = XDocument.Parse(Response);
                XNamespace Namespace = "http://a9.com/-/spec/opensearch/1.1/";
                var SearchProviderInfo = new SearchProvider
                {
                    Name = string.IsNullOrEmpty(Name) ? XML.Root.Element(Namespace + "ShortName")?.Value : Name,
                    //FaviconUrl = doc.Root.Element(ns + "Image")?.Value
                };

                foreach (XElement? Urls in XML.Root.Elements(Namespace + "Url"))
                {
                    string Type = Urls.Attribute("type")?.Value;
                    string Template = Urls.Attribute("template")?.Value.Replace("{searchTerms}", "{0}").Replace("{startPage?}", "1");
                    if (Type == "application/x-suggestions+json")
                        SearchProviderInfo.SuggestUrl = Template;
                    else if (Type == "text/html")
                        SearchProviderInfo.SearchUrl = Template;
                }
                if (SearchProviderInfo.SearchUrl.Length > 0)
                {
                    SearchProviderInfo.Host = Utils.FastHost(SearchProviderInfo.SearchUrl);
                    SearchEngines.Add(SearchProviderInfo);
                }
            }
            catch { }
        }

        public ObservableCollection<ActionStorage> Languages = [];
        public ActionStorage Locale;

        public Dictionary<string, string> AllLocales = new()
        {
            { "af", "Afrikaans" },
            { "af-ZA", "Afrikaans (South Africa)" },

            { "ar", "Arabic" },
            { "ar-SA", "Arabic (Saudi Arabia)" },
            { "ar-EG", "Arabic (Egypt)" },
            { "ar-MA", "Arabic (Morocco)" },
            { "ar-LB", "Arabic (Lebanon)" },

            { "az", "Azerbaijani" },
            { "az-AZ", "Azerbaijani (Azerbaijan)" },

            { "bg", "Bulgarian" },
            { "bg-BG", "Bulgarian (Bulgaria)" },

            { "bn", "Bengali" },
            { "bn-BD", "Bengali (Bangladesh)" },
            { "bn-IN", "Bengali (India)" },

            { "cs", "Czech" },
            { "cs-CZ", "Czech (Czech Republic)" },

            { "da", "Danish" },
            { "da-DK", "Danish (Denmark)" },

            { "de", "German" },
            { "de-DE", "German (Germany)" },
            { "de-CH", "German (Switzerland)" },

            { "el", "Greek" },
            { "el-GR", "Greek (Greece)" },

            { "en", "English" },
            { "en-US", "English (United States)" },
            { "en-GB", "English (United Kingdom)" },
            { "en-CA", "English (Canada)" },
            { "en-AU", "English (Australia)" },

            { "es", "Spanish" },
            { "es-ES", "Spanish (Spain)" },
            { "es-MX", "Spanish (Mexico)" },
            { "es-AR", "Spanish (Argentina)" },

            { "fa", "Persian" },
            { "fa-IR", "Persian (Iran)" },

            { "fi", "Finnish" },
            { "fi-FI", "Finnish (Finland)" },

            { "fr", "French" },
            { "fr-FR", "French (France)" },
            { "fr-CA", "French (Canada)" },

            { "he", "Hebrew" },
            { "he-IL", "Hebrew (Israel)" },

            { "hi", "Hindi" },
            { "hi-IN", "Hindi (India)" },

            { "hu", "Hungarian" },
            { "hu-HU", "Hungarian (Hungary)" },

            { "id", "Indonesian" },
            { "id-ID", "Indonesian (Indonesia)" },

            { "it", "Italian" },
            { "it-IT", "Italian (Italy)" },
            { "it-CH", "Italian (Switzerland)" },

            { "ja", "Japanese" },
            { "ja-JP", "Japanese (Japan)" },

            { "ko", "Korean" },
            { "ko-KR", "Korean (South Korea)" },

            { "ms", "Malay" },
            { "ms-MY", "Malay (Malaysia)" },

            { "nl", "Dutch" },
            { "nl-NL", "Dutch (Netherlands)" },

            { "pl", "Polish" },
            { "pl-PL", "Polish (Poland)" },

            { "pt", "Portuguese" },
            { "pt-PT", "Portuguese (Portugal)" },
            { "pt-BR", "Portuguese (Brazil)" },

            { "ro", "Romanian" },
            { "ro-RO", "Romanian (Romania)" },

            { "ru", "Russian" },
            { "ru-RU", "Russian (Russia)" },

            { "sv", "Swedish" },
            { "sv-SE", "Swedish (Sweden)" },

            { "th", "Thai" },
            { "th-TH", "Thai (Thailand)" },

            { "tr", "Turkish" },
            { "tr-TR", "Turkish (Turkey)" },

            { "uk", "Ukrainian" },
            { "uk-UA", "Ukrainian (Ukraine)" },

            { "vi", "Vietnamese" },
            { "vi-VN", "Vietnamese (Vietnam)" },

            { "zh", "Chinese" },
            { "zh-CN", "Chinese (Simplified, China)" },
            { "zh-TW", "Chinese (Traditional, Taiwan)" },
            { "zh-HK", "Chinese (Hong Kong)" },

            { "zu", "Zulu" },
            { "zu-ZA", "Zulu (South Africa)" },
        };
        public List<string> LocaleNames = null;

        public ObservableCollection<AdBlockList> AdBlockLists = [];

        public static string GetLocaleIcon(string ISO)
        {
            if (ISO.StartsWith("zh-TW"))
                return "\xe981";
            else if (ISO.StartsWith("zh"))
                return "\xE982";
            else if (ISO.StartsWith("ja"))
                return "\xe985";
            else if (ISO.StartsWith("ko"))
                return "\xe97d";
            else if (ISO.StartsWith("en"))
                return "\xe97e";
            return "\xf2b7";
        }

        public bool LiteMode;
        public bool HighPerformanceMode;

        public bool Synchronized = false;
        public bool FavouritesSetUp = false;

        public bool ReadOnlyInstance = false;

        private async Task InitializeSaves()
        {
            GlobalSave = new Saving("Save.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            LanguagesSave = new Saving("Languages.bin", UserApplicationDataPath);
            AllowListSave = new Saving("AllowList.bin", UserApplicationDataPath);
            AdBlockSave = new Saving("AdBlock.bin", UserApplicationDataPath);

            HttpClientFactory.IsHappyEyeballsEnabled = bool.Parse(GlobalSave.Get("HappyEyeballs", true.ToString()));
            if (!GlobalSave.Has("SyncGitHub"))
                GlobalSave.Set("SyncGitHub", "");
            if (!GlobalSave.Has("SyncGist"))
                GlobalSave.Set("SyncGist", "");
            if (!GlobalSave.Has("SyncData"))
                GlobalSave.Set("SyncData", "Favourites,Settings");//,Tabs
            if (!GlobalSave.Has("Sync"))
                GlobalSave.Set("Sync", false);
            //TODO: Implement "Sync Provider" variety [GitHub Gist, Google Drive, OneDrive, etc]
            //TODO: Implement data compression.
            else if (bool.Parse(GlobalSave.Get("Sync")))
            {
                if (Utils.IsInternetAvailable())
                {
                    try
                    {
                        string SyncGitHubToken = GlobalSave.Get("SyncGitHub");
                        if (!string.IsNullOrEmpty(SyncGitHubToken))
                        {
                            string SyncGistID = GlobalSave.Get("SyncGist");
                            HttpClient Client = new();
                            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SyncGitHubToken);
                            Client.DefaultRequestHeaders.UserAgent.ParseAdd($"SLBr/{ReleaseVersion}");
                            if (string.IsNullOrEmpty(SyncGistID))
                            {
                                var GistResponse = await Client.GetAsync("https://api.github.com/gists");
                                if (GistResponse.IsSuccessStatusCode)
                                {
                                    string JSON = await GistResponse.Content.ReadAsStringAsync();
                                    using JsonDocument Document = JsonDocument.Parse(JSON);
                                    foreach (var Gist in Document.RootElement.EnumerateArray())
                                    {
                                        if (Gist.GetProperty("description").GetString() == "SLBr Sync")
                                        {
                                            SyncGistID = Gist.GetProperty("id").GetString()!;
                                            GlobalSave.Set("SyncGist", SyncGistID);
                                            break;
                                        }
                                    }
                                }
                            }
                            //WARNING: Keep separate.
                            if (!string.IsNullOrEmpty(SyncGistID))
                            {
                                var GistResponse = await Client.GetAsync($"https://api.github.com/gists/{SyncGistID}");
                                if (GistResponse.IsSuccessStatusCode)
                                {
                                    string[] SyncedData = GlobalSave.Get("SyncData").Split(',');

                                    string JSON = await GistResponse.Content.ReadAsStringAsync();
                                    using JsonDocument Document = JsonDocument.Parse(JSON);
                                    string SyncFileContent = Document.RootElement.GetProperty("files").GetProperty("slbr-sync.json").GetProperty("content").GetString()!;
                                    Dictionary<string, string> SyncedFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(SyncFileContent)!;
                                    if (SyncedData.Contains("Settings"))
                                    {
                                        if (SyncedFiles.TryGetValue("Save.bin", out var SaveRaw))
                                        {
                                            GlobalSave.Process(SaveRaw);
                                            //WARNING: Important, do not remove.
                                            GlobalSave.Set("Sync", true);
                                            GlobalSave.Set("SyncGitHub", SyncGitHubToken);
                                            GlobalSave.Set("SyncGist", SyncGistID);
                                        }
                                        if (SyncedFiles.TryGetValue("Search.bin", out var SearchRaw))
                                            SearchSave.Process(SearchRaw);
                                        if (SyncedFiles.TryGetValue("Languages.bin", out var LanguagesRaw))
                                            LanguagesSave.Process(LanguagesRaw);
                                        if (SyncedFiles.TryGetValue("AllowList.bin", out var AllowListRaw))
                                            AllowListSave.Process(AllowListRaw);
                                        if (SyncedFiles.TryGetValue("AdBlock.bin", out var AdBlockRaw))
                                            AdBlockSave.Process(AdBlockRaw);
                                    }
                                    if (SyncedData.Contains("Favourites") && SyncedFiles.TryGetValue("Favourites.bin", out var FavouritesRaw))
                                    {
                                        try
                                        {
                                            BookmarksManager.Bookmarks BookmarksContainer = JsonSerializer.Deserialize<BookmarksManager.Bookmarks>(FavouritesRaw, new JsonSerializerOptions
                                            {
                                                PropertyNameCaseInsensitive = true
                                            })!;
                                            foreach (Favourite Bookmark in BookmarksContainer.Roots.Bookmarks.Children)
                                            {
                                                if (string.IsNullOrEmpty(Bookmark.Name) && !string.IsNullOrEmpty(Bookmark.Url))
                                                    Bookmark.Name = Utils.FastHost(Bookmark.Url);
                                                Favourites.Add(Bookmark);
                                            }
                                            FavouritesSetUp = true;
                                        }
                                        catch { }
                                    }
                                    Synchronized = true;
                                    //if (SyncedData.Contains("Tabs"))
                                }
                                else
                                    GlobalSave.Set("SyncGist", "");
                            }
                        }
                    }
                    catch { }
                    //TODO: Create folders of windows for the tab sync?
                }
                if (!Synchronized)
                {
                    InfoBars.Add(new()
                    {
                        Icon = "\xec9c",
                        IconForeground = OrangeColor,
                        Title = "Sync Failed",
                        Description = [new() { Text = "SLBr failed to synchronize data." }]
                    });
                }
            }

            if (!FavouritesSetUp)
            {
                string FavouritesPath = Path.Combine(UserApplicationDataPath, "Favourites.bin");
                if (File.Exists(FavouritesPath))
                {
                    try
                    {
                        string FavouritesJSON = File.ReadAllText(FavouritesPath);
                        BookmarksManager.Bookmarks BookmarksContainer = JsonSerializer.Deserialize<BookmarksManager.Bookmarks>(FavouritesJSON, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!;
                        foreach (Favourite Bookmark in BookmarksContainer.Roots.Bookmarks.Children)
                        {
                            if (string.IsNullOrEmpty(Bookmark.Name) && !string.IsNullOrEmpty(Bookmark.Url))
                                Bookmark.Name = Utils.FastHost(Bookmark.Url);
                            Favourites.Add(Bookmark);
                        }
                    }
                    catch { }
                }
            }

            if (!GlobalSave.Has("StartupBoost"))
                GlobalSave.Set("StartupBoost", false);
            /*{
                if (CurrentProfile.Default)
                    StartupManager.EnableStartup(CurrentProfile.Name);
                GlobalSave.Set("StartupBoost", CurrentProfile.Default.ToString());
            }*/

            if (!ReadOnlyInstance)
            {
                if (!Directory.Exists(UserApplicationWindowsPath))
                {
                    Directory.CreateDirectory(UserApplicationWindowsPath);
                    WindowsSaves.Add(new Saving($"Window_0.bin", UserApplicationWindowsPath));
                }
                else
                {
                    int WindowsSavesCount = Directory.EnumerateFiles(UserApplicationWindowsPath).Count();
                    if (WindowsSavesCount != 0)
                    {
                        for (int i = 0; i < WindowsSavesCount; i++)
                            WindowsSaves.Add(new Saving($"Window_{i}.bin", UserApplicationWindowsPath));
                    }
                    else
                        WindowsSaves.Add(new Saving($"Window_0.bin", UserApplicationWindowsPath));
                }
            }

            bool UseDefaultSearchEngines = false;
            int SearchCount = SearchSave.GetInt("Count", 0);
            if (SearchCount != 0)
            {
                for (int i = 0; i < SearchCount; i++)
                {
                    string[] Values = SearchSave.Get($"{i}").Split("<#>");
                    if (Values.Length != 3)
                    {
                        UseDefaultSearchEngines = true;
                        break;
                    }
                    else
                    {
                        SearchEngines.Add(new SearchProvider()
                        {
                            Name = Values[0],
                            Host = Utils.FastHost(Values[1]),
                            SearchUrl = Values[1],
                            SuggestUrl = Values[2]
                        });
                    }
                }
            }
            else
                UseDefaultSearchEngines = true;
            if (UseDefaultSearchEngines)
            {
                DefaultSearchProvider = new() { Name = "Google", Host = "google.com", SearchUrl = "https://google.com/search?q={0}", SuggestUrl = "https://suggestqueries.google.com/complete/search?client=chrome&output=toolbar&q={0}" };
                SearchEngines =
                [
                    DefaultSearchProvider,
                    new() { Name = "Bing", Host = "bing.com", SearchUrl = "https://bing.com/search?q={0}", SuggestUrl = "https://api.bing.com/osjson.aspx?query={0}" },
                    new() { Name = "Ecosia", Host = "ecosia.org", SearchUrl = "https://www.ecosia.org/search?q={0}", SuggestUrl = "https://ac.ecosia.org/autocomplete?q={0}&type=list" },
                    new() { Name = "Brave Search", Host = "search.brave.com", SearchUrl = "https://search.brave.com/search?q={0}", SuggestUrl = "https://search.brave.com/api/suggest?q={0}" },
                    new() { Name = "DuckDuckGo", Host = "duckduckgo.com", SearchUrl = "https://duckduckgo.com/?q={0}", SuggestUrl = "http://duckduckgo.com/ac/?type=list&q={0}" },
                    new() { Name = "Yandex", Host = "yandex.com", SearchUrl = "https://yandex.com/search/?text={0}", SuggestUrl = "https://suggest.yandex.com/suggest-ff.cgi?part={0}" },
                    new() { Name = "Yahoo Search", Host = "search.yahoo.com", SearchUrl = "https://search.yahoo.com/search?p={0}", SuggestUrl = "https://ff.search.yahoo.com/gossip?output=fxjson&command={0}" },
                ];
            }
            string SearchEngineName = GlobalSave.Get("SearchEngine", "Google");
            if (string.IsNullOrEmpty(SearchEngineName))
            {
                GlobalSave.Set("SearchEngine", "Google");
                SearchEngineName = "Google";
            }
            DefaultSearchProvider = SearchEngines.FirstOrDefault(i => i.Name == SearchEngineName) ?? SearchEngines.FirstOrDefault(i => i.SearchUrl.Contains("google.com"));

            int LanguageCount = LanguagesSave.GetInt("Count", 0);
            if (LanguageCount != 0)
            {
                for (int i = 0; i < LanguageCount; i++)
                {
                    string ISO = LanguagesSave.Get($"{i}");
                    if (AllLocales.TryGetValue(ISO, out string Name))
                        Languages.Add(new ActionStorage(Name, GetLocaleIcon(ISO), ISO));
                }
                Locale = Languages[LanguagesSave.GetInt("Selected", 0)];
            }
            else
            {
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en-US"), GetLocaleIcon("en-US"), "en-US"));
                Languages.Add(new ActionStorage(AllLocales.GetValueOrDefault("en"), GetLocaleIcon("en"), "en"));
                Locale = Languages[0];
            }

            int AdBlockUrlCount = AdBlockSave.GetInt("Count", 0);
            bool UseDefaultAdBlockLists = false;
            if (AdBlockUrlCount != 0)
            {
                for (int i = 0; i < AdBlockUrlCount; i++)
                {
                    string[] Values = AdBlockSave.Get($"{i}").Split("<#>");
                    if (Values.Length != 3)
                    {
                        UseDefaultAdBlockLists = true;
                        break;
                    }
                    else
                    {
                        AdBlockLists.Add(new AdBlockList()
                        {
                            Name = Values[0],
                            Url = Values[1],
                            IsEnabled = Values[2] == "1"
                        });
                    }
                }
            }
            else
                UseDefaultAdBlockLists = true;
            if (UseDefaultAdBlockLists)
            {
                //TODO: Fetch title from file content.
                AdBlockLists =
                [
                    //https://github.com/yokoffing/filterlists?tab=readme-ov-file#optimized-lists
                    //https://easylist-downloads.adblockplus.org/easylist_noelemhide.txt

                    new AdBlockList { Name = "EasyList", Url = "https://easylist.to/easylist/easylist.txt", IsEnabled = true },
                    new AdBlockList { Name = "EasyPrivacy", Url = "https://easylist.to/easylist/easyprivacy.txt", IsEnabled = true },
                    //new AdBlockList { Name = "AdGuard Base filter", Url = "https://filters.adtidy.org/extension/chromium/filters/2.txt" },

                    //new AdBlockList { Name = "EasyList", Url = "https://filters.adtidy.org/extension/ublock/filters/101_optimized.txt", IsEnabled = true },
                    //new AdBlockList { Name = "EasyPrivacy", Url = "https://filters.adtidy.org/extension/ublock/filters/118_optimized.txt", IsEnabled = true },
                    new AdBlockList { Name = "AdGuard Base filter + EasyList", Url = "https://filters.adtidy.org/extension/ublock/filters/2_optimized.txt" },
                    new AdBlockList { Name = "AdGuard Tracking Protection filter", Url = "https://filters.adtidy.org/extension/ublock/filters/3_optimized.txt" },
                    new AdBlockList { Name = "Peter Lowe Adservers", Url = "https://pgl.yoyo.org/adservers/serverlist.php?hostformat=adblockplus&mimetype=plaintext" },
                    new AdBlockList { Name = "NoCoin Filter List", Url = "https://raw.githubusercontent.com/hoshsadiq/adblock-nocoin-list/refs/heads/master/nocoin.txt" },
                ];
            }

            SetMobileView(bool.Parse(GlobalSave.Get("MobileView", false.ToString())));

            if (!GlobalSave.Has("Toast"))
                GlobalSave.Set("Toast", true);
            if (!GlobalSave.Has("WarnCodec"))
                GlobalSave.Set("WarnCodec", true);
            if (!GlobalSave.Has("WaybackInfoBar"))
                GlobalSave.Set("WaybackInfoBar", true);
            if (!GlobalSave.Has("HomographInfoBar"))
                GlobalSave.Set("HomographInfoBar", true);
            if (!GlobalSave.Has("CheckUpdate"))
                GlobalSave.Set("CheckUpdate", true);
            if (!GlobalSave.Has("PrivateTabs"))
                GlobalSave.Set("PrivateTabs", false);
            if (!GlobalSave.Has("QuickImage"))
                GlobalSave.Set("QuickImage", true);
            if (!GlobalSave.Has("OpenSearch"))
                GlobalSave.Set("OpenSearch", false);
            if (!GlobalSave.Has("SearchSuggestions"))
                GlobalSave.Set("SearchSuggestions", true);
            if (!GlobalSave.Has("SmartSuggestions"))
                GlobalSave.Set("SmartSuggestions", true);
            if (!GlobalSave.Has("SpellCheck"))
                GlobalSave.Set("SpellCheck", true);
            if (!GlobalSave.Has("NetworkLimit"))
                GlobalSave.Set("NetworkLimit", false);
            if (!GlobalSave.Has("Bandwidth"))
                GlobalSave.Set("Bandwidth", "200");

            if (!GlobalSave.Has("Homepage"))
                GlobalSave.Set("Homepage", "slbr://newtab");
            AdsBlocked = StatisticsSave.GetInt("BlockedAds", 0);

            if (!GlobalSave.Has("TabUnloading"))
                GlobalSave.Set("TabUnloading", true);
            if (!GlobalSave.Has("ShowUnloadProgress"))
                GlobalSave.Set("ShowUnloadProgress", false);
            if (!GlobalSave.Has("DimUnloadedIcon"))
                GlobalSave.Set("DimUnloadedIcon", true);
            if (!GlobalSave.Has("ShowUnloadedIcon"))
                GlobalSave.Set("ShowUnloadedIcon", true);
            UpdateTabUnloadingTimer(GlobalSave.GetInt("TabUnloadingTime", 10));
            if (!GlobalSave.Has("DownloadPrompt"))
                GlobalSave.Set("DownloadPrompt", true);
            if (!GlobalSave.Has("DownloadPath"))
                GlobalSave.Set("DownloadPath", Utils.GetFolderPath(Utils.FolderGuids.Downloads));
            if (!GlobalSave.Has("ScreenshotPath"))
                GlobalSave.Set("ScreenshotPath", Path.Combine(Utils.GetFolderPath(Utils.FolderGuids.Pictures), "Screenshots", "SLBr"));

            //if (!GlobalSave.Has("SendDiagnostics"))
            //    GlobalSave.Set("SendDiagnostics", false);
            if (!GlobalSave.Has("WebNotifications"))
                GlobalSave.Set("WebNotifications", true);
            if (!GlobalSave.Has("WebApps"))
                GlobalSave.Set("WebApps", true);
            if (!GlobalSave.Has("AdaptiveTheme"))
                GlobalSave.Set("AdaptiveTheme", false);

            if (!GlobalSave.Has("ScreenshotFormat"))
                GlobalSave.Set("ScreenshotFormat", 0);

            if (!GlobalSave.Has("Favicons"))
                GlobalSave.Set("Favicons", true);
            SmoothScrollBehavior.IsDisabled = !bool.Parse(GlobalSave.Get("SmoothScroll", true.ToString()));

            if (!GlobalSave.Has("BrowserHardwareAcceleration"))
                GlobalSave.Set("BrowserHardwareAcceleration", (RenderCapability.Tier >> 16) != 0);
            if (!GlobalSave.Has("ReduceDisk"))
                GlobalSave.Set("ReduceDisk", false);
            if (!GlobalSave.Has("Performance"))
                GlobalSave.Set("Performance", 1);
            int PerformanceMode = GlobalSave.GetInt("Performance");
            LiteMode = PerformanceMode == 0;
            HighPerformanceMode = PerformanceMode == 2;
            //HappyEyeballs.ConnectionAttemptDelay = HighPerformanceMode ? 200 : 250;

            if (!GlobalSave.Has("JIT"))
                GlobalSave.Set("JIT", true);
            if (!GlobalSave.Has("PDF"))
                GlobalSave.Set("PDF", true);

            if (!GlobalSave.Has("ImageSearch"))
                GlobalSave.Set("ImageSearch", 0);
            if (!GlobalSave.Has("TranslationProvider"))
                GlobalSave.Set("TranslationProvider", 0);
            if (!GlobalSave.Has("SpellCheckProvider"))
                GlobalSave.Set("SpellCheckProvider", 0);

            if (!GlobalSave.Has("WebEngine"))
            {
                int DefaultEngine = 1;
                string? AvailableVersion = null;
                try { AvailableVersion = CoreWebView2Environment.GetAvailableBrowserVersionString(); }
                catch (WebView2RuntimeNotFoundException) { DefaultEngine = 0; }
                GlobalSave.Set("WebEngine", DefaultEngine);
            }

            if (!GlobalSave.Has("AntiTamper"))
                GlobalSave.Set("AntiTamper", false);
            if (!GlobalSave.Has("AntiInspectDetect"))
                GlobalSave.Set("AntiInspectDetect", false);
            if (!GlobalSave.Has("AntiFullscreen"))
                GlobalSave.Set("AntiFullscreen", false);
            if (!GlobalSave.Has("BypassSiteMenu"))
                GlobalSave.Set("BypassSiteMenu", false);
            if (!GlobalSave.Has("TextSelection"))
                GlobalSave.Set("TextSelection", false);
            if (!GlobalSave.Has("RemoveFilter"))
                GlobalSave.Set("RemoveFilter", false);
            if (!GlobalSave.Has("RemoveOverlay"))
                GlobalSave.Set("RemoveOverlay", false);
            if (!GlobalSave.Has("FullscreenPopup"))
                GlobalSave.Set("FullscreenPopup", true);
            if (!GlobalSave.Has("FaviconService"))
                GlobalSave.Set("FaviconService", 0);

            SetWebRiskService(GlobalSave.GetInt("WebRiskService", 1));
            SetDownloadSecurityService(GlobalSave.GetInt("DownloadSecurityService", 1));

            try
            {
                using var Key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true);
                Themes.Add(new Theme("System", (Key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
            }
            catch
            {
                Themes.Add(new Theme("System", Themes[1]));
            }
            Theme CustomTheme = GenerateTheme(Utils.HexToColor(GlobalSave.Get("CustomTheme", Utils.ColorToHex(Colors.Red))), "Custom");
            Themes.Add(CustomTheme);
        }
        private void InitializeUISaves(string CommandLineUrl = "")
        {
            SetSmartDarkMode(bool.Parse(GlobalSave.Get("SmartDarkMode", false.ToString())));
            SetBlockScreenCapture(int.Parse(GlobalSave.Get("BlockScreenCapture", "1")));
            SetTabMemory(bool.Parse(GlobalSave.Get("TabMemory", true.ToString())));
            SetTabPreview(bool.Parse(GlobalSave.Get("TabPreview", false.ToString())));
            SetExternalFonts(bool.Parse(GlobalSave.Get("ExternalFonts", true.ToString())));
            SetModernURL(bool.Parse(GlobalSave.Get("ModernURL", false.ToString())));
            SetPunycodeURL(bool.Parse(GlobalSave.Get("PunycodeURL", false.ToString())));
            SetTrimURL(bool.Parse(GlobalSave.Get("TrimURL", true.ToString())));
            SetHomographProtection(bool.Parse(GlobalSave.Get("HomographProtection", true.ToString())));
            SetNeverSlowMode(bool.Parse(GlobalSave.Get("NeverSlowMode", false.ToString())));
            SetAdBlock(GlobalSave.GetInt("AdBlock", 0));
            SetAMP(bool.Parse(GlobalSave.Get("AMP", false.ToString())));
            SetRenderMode(GlobalSave.GetInt("RenderMode", (RenderCapability.Tier >> 16) == 0 ? 1 : 0));

            Favourites.CollectionChanged += Favourites_CollectionChanged;

            SetAppearance(GetTheme(GlobalSave.Get("Theme", "System")), GlobalSave.GetInt("TabAlignment", 0), double.Parse(GlobalSave.Get("VerticalTabWidth", "250")), bool.Parse(GlobalSave.Get("HomeButton", true.ToString())), bool.Parse(GlobalSave.Get("TranslateButton", true.ToString())), bool.Parse(GlobalSave.Get("ReaderButton", true.ToString())), GlobalSave.GetInt("ExtensionButton", 0),  GlobalSave.GetInt("DownloadsButton", 0), GlobalSave.GetInt("FavouritesBar", 0), bool.Parse(GlobalSave.Get("QRButton", true.ToString())), bool.Parse(GlobalSave.Get("WebEngineButton", true.ToString())));
            bool PrivateTabs = bool.Parse(GlobalSave.Get("PrivateTabs"));
            //WARNING: Do not remove RestoreTabs boolean.
            bool RestoreTabs = bool.Parse(GlobalSave.Get("RestoreTabs", (!ReadOnlyInstance).ToString()));
            if (WindowsSaves.Count != 0 && RestoreTabs)
                foreach (Saving TabsSave in WindowsSaves)
                {
                    MainWindow _Window = new();
                    if (Background)
                    {
                        _Window.WindowState = WindowState.Minimized;
                        _Window.ShowInTaskbar = false;
                    }
                    else
                        _Window.Show();
                    int TabCount = TabsSave.GetInt("Count", 0);
                    if (TabCount != 0)
                    {
                        int SelectedIndex = TabsSave.GetInt("Selected", 0);
                        for (int i = 0; i < TabCount; i++)
                        {
                            string[] Data = TabsSave.Get(i.ToString(), true);
                            string Url;
                            string TabGroupName = string.Empty;
                            BrowserTabType TabType;
                            if (Data.Length == 1)
                            {
                                Url = Data[0];
                                TabType = BrowserTabType.Navigation;
                            }
                            else
                            {
                                TabType = (BrowserTabType)int.Parse(Data[0]);
                                Url = Data[1];
                                TabGroupName = Data[2];
                            }
                            if (TabType == BrowserTabType.Navigation)
                            {
                                if (Utils.IsEmptyOrWhiteSpace(Url))
                                    Url = "slbr://newtab";
                                _Window.NewTab(Url, i == SelectedIndex, -1, PrivateTabs, _Window.TabGroups.FirstOrDefault(i => i.Header == TabGroupName));
                            }
                            else if (TabType == BrowserTabType.Group)
                                _Window.NewTabGroup(TabGroupName, Utils.HexToColor(Url), -1, Data.Length > 3 ? Data[3] == "0" : false);
                        }
                    }
                    else
                        _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, PrivateTabs);
                    _Window.TabsUI.Visibility = Visibility.Visible;
                }
            else
            {
                MainWindow _Window = new();
                if (Background)
                {
                    _Window.WindowState = WindowState.Minimized;
                    _Window.ShowInTaskbar = false;
                }
                else
                {
                    _Window.Show();
                    _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, PrivateTabs);
                    _Window.TabsUI.Visibility = Visibility.Visible;
                }
            }
            if (!string.IsNullOrEmpty(CommandLineUrl))
                CurrentFocusedWindow().NewTab(CommandLineUrl, true, -1, PrivateTabs);
        }

        private void Favourites_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                    BrowserView?.Favourites_CollectionChanged();
                _Window.SetTabAlignment();
            }
        }

        public MainWindow CurrentFocusedWindow() =>
            AllWindows.FirstOrDefault(w => w.IsFocused || w.IsActive || w.WindowState == WindowState.Maximized || w.WindowState == WindowState.Normal) ?? AllWindows.First();

        public void Refresh(bool IgnoreCache = false) =>
            CurrentFocusedWindow().Refresh(string.Empty, IgnoreCache);
        public void Fullscreen(bool ForceClose = false)
        {
            MainWindow CurrentWindow = CurrentFocusedWindow();
            if (ForceClose && !CurrentWindow.IsFullscreen)
                return;
            CurrentWindow.Fullscreen(!ForceClose && !CurrentWindow.IsFullscreen);
        }
        public void DevTools(string Id = "") =>
            CurrentFocusedWindow().DevTools(Id);
        public void Find(string Text = "") =>
            CurrentFocusedWindow().Find(Text);
        public void Screenshot() =>
            CurrentFocusedWindow().Screenshot();
        public void NewWindow(bool ForcePrivate = false)
        {
            MainWindow _Window = new();
            _Window.Show();
            _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, ForcePrivate || bool.Parse(GlobalSave.Get("PrivateTabs")));
            _Window.TabsUI.Visibility = Visibility.Visible;
        }

        public static FastHashSet<string> FailedScripts = [];

        public WebRiskHandler _WebRiskHandler;
        public DownloadRiskHandler _DownloadRiskHandler;
        public AdBlockHandler _AdBlockHandler;

        public string ReleaseVersion;

        public void KeyAction(int _Action)
        {
            MainWindow CurrentWindow = CurrentFocusedWindow();
            switch (_Action)
            {
                case 0:
                    Browser BrowserView = CurrentWindow.GetTab().Content;
                    BrowserView.OmniBox.Focus();
                    Keyboard.Focus(BrowserView.OmniBox);
                    break;
                case 1:
                    CurrentWindow.NewTab(GlobalSave.Get("Homepage"), true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                    break;
                case 2:
                    Fullscreen(true);
                    CurrentWindow.StopFind();
                    CurrentWindow.GetTab().Content.Stop();
                    break;
                case 3:
                    CurrentWindow.GetTab().Content.FavouriteAction();
                    break;
                case 4:
                    CurrentWindow.GetTab().Content.WebView?.SaveAs();
                    break;
                case 5:
                    CurrentWindow.GetTab().Content.ToggleReaderMode();
                    break;
                case 6:
                    CurrentWindow.GetTab().Content.WebView?.Print();
                    break;
                case 7:
                    int RightCurrentIndex = CurrentWindow.TabsUI.SelectedIndex;
                    for (int i = 1; i <= CurrentWindow.Tabs.Count; i++)
                    {
                        int Index = (RightCurrentIndex + i) % CurrentWindow.Tabs.Count;
                        if (Index != RightCurrentIndex && CurrentWindow.Tabs[Index].Type == BrowserTabType.Navigation)
                        {
                            CurrentWindow.TabsUI.SelectedIndex = Index;
                            break;
                        }
                    }
                    break;
                case 8:
                    int LeftCurrentIndex = CurrentWindow.TabsUI.SelectedIndex;
                    for (int i = 1; i <= CurrentWindow.Tabs.Count; i++)
                    {
                        int Index = (LeftCurrentIndex - i + CurrentWindow.Tabs.Count) % CurrentWindow.Tabs.Count;
                        if (Index != LeftCurrentIndex && CurrentWindow.Tabs[Index].Type == BrowserTabType.Navigation)
                        {
                            CurrentWindow.TabsUI.SelectedIndex = Index;
                            break;
                        }
                    }
                    break;
                case 9:
                    BrowserTabItem? TargetTab = CurrentWindow.Tabs.Where(i => i.Type == BrowserTabType.Navigation).OrderByDescending(i => CurrentWindow.Tabs.IndexOf(i)).FirstOrDefault();
                    if (TargetTab != null)
                        CurrentWindow.TabsUI.SelectedItem = TargetTab;
                    break;
                case 10:
                    CurrentWindow.GetTab().Content.Navigate(GlobalSave.Get("Homepage"));
                    break;
                case 11:
                    CurrentWindow.CloseTab(CurrentWindow.GetTab().ID, CurrentWindow.ID);
                    break;
                case 12:
                    CurrentWindow.Close();
                    break;
                case 13:
                    CurrentWindow.NewTab("slbr://history", true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                    break;
                case 14:
                    CurrentWindow.NewTab("slbr://favourites", true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                    break;
                case 15:
                    CurrentWindow.NewTab("slbr://downloads", true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                    break;
                case 16:
                    CurrentWindow.GetTab().Content?.OptionsButton.OpenMenu();
                    break;
                case 17:
                    BrowserTabItem CurrentTab = CurrentWindow.GetTab();
                    CurrentWindow.NewTab($"view-source:{CurrentTab.Content?.Address}", true, CurrentWindow.TabsUI.SelectedIndex + 1, CurrentTab.Content?.Private ?? bool.Parse(GlobalSave.Get("PrivateTabs")), CurrentTab.TabGroup);
                    break;
                case 18:
                    CurrentWindow.GetTab().Content?.WebView.ExecuteScript("window.scrollTo({top:0,behavior:'smooth'});");
                    break;
                case 19:
                    CurrentWindow.GetTab().Content?.WebView.ExecuteScript("window.scrollTo({top:document.body.scrollHeight,behavior:'smooth'});");
                    break;
            }
        }

        private void InitializeBrowser()
        {
            //Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1";
            //Settings.UserAgentProduct = $"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}";
            //Settings.UserAgent = UserAgent;

            WebViewSettings Settings = new();
            Settings.RegisterProtocol("gemini", WebViewManager.GeminiHandler);
            Settings.RegisterProtocol("gopher", WebViewManager.GopherHandler);
            Settings.RegisterProtocol("slbr", WebViewManager.SLBrHandler);

            Settings.CefRuntimeStyle = GlobalSave.GetInt("ChromiumRuntimeStyle", 0) == 1 ? CefRuntimeStyle.Alloy : CefRuntimeStyle.Chrome;
            switch (GlobalSave.GetInt("TridentVersion", 4))
            {
                case 0: Settings.TridentVersion = TridentEmulationVersion.IE7; break;
                case 1: Settings.TridentVersion = TridentEmulationVersion.IE8; break;
                case 2: Settings.TridentVersion = TridentEmulationVersion.IE9; break;
                case 3: Settings.TridentVersion = TridentEmulationVersion.IE10; break;
                case 4: Settings.TridentVersion = TridentEmulationVersion.IE11; break;
                case 5: Settings.TridentVersion = TridentEmulationVersion.Edge; break;
            }
            if (!ReadOnlyInstance)
                Settings.UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
            Settings.Language = Locale.Tooltip;
            Settings.Languages = Languages.Select(i => i.Tooltip).ToArray();
            Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));

            Settings.Performance = (PerformancePreset)GlobalSave.GetInt("Performance");
            Settings.GPUAcceleration = bool.Parse(GlobalSave.Get("BrowserHardwareAcceleration"));

            SetBrowserFlags(Settings);

            WebViewManager.Settings = Settings;
            WebViewManager.RuntimeSettings.SpellCheck = bool.Parse(GlobalSave.Get("SpellCheck"));
            WebViewManager.RuntimeSettings.DownloadFolderPath = GlobalSave.Get("DownloadPath");
            WebViewManager.RuntimeSettings.DownloadPrompt = bool.Parse(GlobalSave.Get("DownloadPrompt"));
            WebViewManager.RuntimeSettings.PDFViewer = bool.Parse(GlobalSave.Get("PDF"));

            //https://support.google.com/chrome/answer/157179
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(), (int)Key.R, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(), (int)Key.F5, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(true), (int)Key.R, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(true), (int)Key.F5, false, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Fullscreen(), (int)Key.F11, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(2), (int)Key.Escape, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => DevTools(), (int)Key.F12, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => DevTools(), (int)Key.I, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => DevTools(), (int)Key.J, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Find(), (int)Key.F, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Find(), (int)Key.F3, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(0), (int)Key.F6, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(1), (int)Key.T, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(3), (int)Key.D, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(4), (int)Key.S, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(5), (int)Key.F9, false, false, false));
            //For some reason Chrome runtime style opens a new chrome window
            //HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(6), (int)Key.N, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(6), (int)Key.P, true, false, false));

            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(7), (int)Key.Tab, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(8), (int)Key.Tab, true, true, false));

            //HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(7), (int)Key.PageUp, true, false, false));
            //HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(8), (int)Key.PageDown, true, false, false));

            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(9), (int)Key.NumPad9, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(9), (int)Key.D9, true, false, false));

            HotKeyManager.HotKeys.Add(new HotKey(() => NewWindow(), (int)Key.N, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => NewWindow(true), (int)Key.N, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(10), (int)Key.Home, false, false, true));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(11), (int)Key.W, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(11), (int)Key.F4, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(12), (int)Key.W, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(12), (int)Key.F4, false, false, true));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(13), (int)Key.H, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(14), (int)Key.O, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(15), (int)Key.J, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(16), (int)Key.F, false, false, true));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(16), (int)Key.E, false, false, true));
            HotKeyManager.HotKeys.Add(new HotKey(SwitchUserPopup, (int)Key.M, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(17), (int)Key.U, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(18), (int)Key.Home, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(19), (int)Key.End, false, false, false));

            WebViewManager.DownloadManager.DownloadStarted += UpdateDownloadItem;
            WebViewManager.DownloadManager.DownloadUpdated += UpdateDownloadItem;
            WebViewManager.DownloadManager.DownloadCompleted += UpdateDownloadItem;

            _WebRiskHandler = new WebRiskHandler();
            _DownloadRiskHandler = new DownloadRiskHandler();

            switch ((WebEngineType)GlobalSave.GetInt("WebEngine"))
            {
                case WebEngineType.Chromium:
                    WebViewManager.InitializeCEF();
                    break;
                case WebEngineType.ChromiumEdge:
                    WebViewManager.InitializeWebView2();
                    break;
                case WebEngineType.Trident:
                    WebViewManager.InitializeTrident();
                    break;
            }
            LoadExtensions();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                    BrowserView?.InitializeBrowserComponent();
            }
        }

        public static string GenerateCannotConnect(string Url, WebErrorCode ErrorCode, string ErrorText) =>
            string.Format(CannotConnectError, Utils.FastHost(Url), ErrorCode, ErrorText);

        public const string CannotConnectError = @"<html><head><title>Unable to connect to {0}</title><style>body{{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}}h5{{font-weight:500;}}button{{border:0;padding:10px;border-radius:5px;cursor:pointer;position:absolute;}}#content{{width:90%;max-width:700px;margin: 140px auto 0 auto;}}.icon{{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}}a{{color:skyblue;text-decoration:none;}}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Unable to connect to {0}</h2><h5 id=""description"">{1}</h5><h5 id=""error"" style=""margin:0px; color:#646464;"">{2}</h5></div></body></html>";
        public const string ProcessCrashedError = @"<html><head><title>Process crashed</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}button{border:0;padding:10px;border-radius:5px;cursor:pointer;position:absolute;}#content{width:90%;max-width:700px;margin: 140px auto 0 auto;}.icon{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Process crashed</h2><h5>Process crashed while attempting to load content. Refresh the page to resolve the problem.</h5></div></body></html>";
        public const string WebRiskInterstitialPage = @"<html><head><title>Dangerous site ahead</title><style>html{{background:#A4000F;color:white;}}body{{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}}h5{{font-weight:500;}}button{{border:0;padding:10px;border-radius:5px;cursor:pointer;position:absolute;}}#content{{width:90%;max-width:700px;margin: 140px auto 0 auto;}}.icon{{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}}a{{color:skyblue;text-decoration:none;}}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Dangerous site ahead</h2><h5>{0}</h5><div style=""position:relative;""><button style=""left:0;border:1px solid white;background:transparent;color:white;"" onclick=""engine.postMessage({{type:'__web_risk_ignore__'}})"">Proceed anyway</button><button style=""right:0;background:white;"" onclick=""history.back()"">Go back</button></div></div></body></html>";
        public const string WebRiskBillingInterstitialPage = @"<html><head><title>Deceptive billing ahead</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}button{border:0;padding:10px;border-radius:5px;cursor:pointer;position:absolute;}#content{width:90%;max-width:700px;margin: 140px auto 0 auto;}.icon{color:red;font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Deceptive billing ahead</h2><h5>The site may attempt to trick you into agreeing to hidden fees or recurring subscription charges.</h5><div style=""position:relative;""><button style=""left:0;border:1px solid gainsboro;background:transparent;"" onclick=""engine.postMessage({type:'__web_risk_ignore__'})"">Proceed anyway</button><button style=""right:0;"" onclick=""history.back()"">Go back</button></div></div></body></html>";
        public const string HistoryPlaceholder = @"<html><head><script>window.addEventListener(""pageshow"",function(e){e.persisted&&location.reload()});</script></head></html>";
        public const string OverlayPagePlaceholder = "<html><head><title>{0}</title></head></html>";

        private void SetBrowserFlags(WebViewSettings Settings)
        {
            SetChromeFlags(Settings);
            SetBackgroundFlags(Settings);
            SetNetworkFlags(Settings);
            SetFrameworkFlags(Settings);
            SetGraphicsFlags(Settings);
            SetMediaFlags(Settings);
            SetSecurityFlags(Settings);
            SetFeatureFlags(Settings);
            SetUrlFlags(Settings);
            SetEdgeFlags(Settings);
            //force-gpu-mem-available-mb https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/gpu_switches.cc
            //disable-file-system Disable FileSystem API.
        }
        public static void SetUrlFlags(WebViewSettings Settings)
        {
            const string DummyUrl = "dummy.invalid";
            //https://github.com/melo936/ChromiumHardening/blob/main/flags/chrome-command-line.md

            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/gaia/gaia_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chromecast/net/net_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/gcm/engine/gservices_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/google/core/common/google_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/policy/core/common/policy_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/chrome_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/gaia/gaia_urls_unittest.cc;l=144?q=%22const%20char%20k%22%20%22%5B%5D%22%20%22-url%5C%22%22&start=31
            Settings.AddFlag("connectivity-check-url", "https://cp.cloudflare.com/generate_204");
            Settings.AddFlag("sync-url", DummyUrl);
            Settings.AddFlag("gaia-url", DummyUrl);
            Settings.AddFlag("gcm-checkin-url", DummyUrl);
            Settings.AddFlag("gcm-mcs-endpoint", DummyUrl);
            Settings.AddFlag("gcm-registration-url", DummyUrl);
            Settings.AddFlag("google-url", DummyUrl);
            Settings.AddFlag("google-apis-url", DummyUrl);
            Settings.AddFlag("google-base-url", DummyUrl);
            Settings.AddFlag("lso-url", DummyUrl);
            Settings.AddFlag("oauth-account-manager-url", DummyUrl);
            Settings.AddFlag("secure-connect-api-url", DummyUrl);

            Settings.AddFlag("model-quality-service-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/variations/variations_switches.cc
            Settings.AddFlag("variations-server-url", DummyUrl);
            Settings.AddFlag("variations-insecure-server-url", DummyUrl);

            Settings.AddFlag("device-management-url", DummyUrl);
            Settings.AddFlag("realtime-reporting-url", DummyUrl);
            Settings.AddFlag("encrypted-reporting-url", DummyUrl);
            Settings.AddFlag("file-storage-server-upload-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/search_provider_logos/switches.cc
            Settings.AddFlag("google-doodle-url", DummyUrl);
            Settings.AddFlag("third-party-doodle-url", DummyUrl);
            Settings.AddFlag("search-provider-logo-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/translate/core/common/translate_switches.cc
            Settings.AddFlag("translate-script-url", DummyUrl);
            Settings.AddFlag("translate-security-origin", "");
            Settings.AddFlag("translate-ranker-model-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/autofill/core/common/autofill_features.cc
            Settings.AddFlag("autofill-server-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:chromecast/base/chromecast_switches.cc
            Settings.AddFlag("override-metrics-upload-url", DummyUrl);
            Settings.AddFlag("crash-server-url", DummyUrl);
            Settings.AddFlag("ignore-google-port-numbers");

            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/chrome_features.cc
            Settings.AddFlag("glic-guest-url", DummyUrl);
            Settings.AddFlag("glic-user-status-url", DummyUrl);
            Settings.AddFlag("glic-user-status-oauth2-scope", DummyUrl);
            Settings.AddFlag("glic-fre-url", DummyUrl);
            Settings.AddFlag("glic-caa-link-url", DummyUrl);
            Settings.AddFlag("glic-allowed-origins-override", "");

            //https://source.chromium.org/chromium/chromium/src/+/main:components/safe_browsing/core/common/safebrowsing_switches.cc
            //Settings.AddFlag("binary-upload-service-url", "");
            //cloud-print-url, url-filtering-endpoint
            //trace-upload-url
        }

        public static void SetChromeFlags(WebViewSettings Settings)
        {
            //https://source.chromium.org/chromium/chromium/src/+/main:tools/perf/testdata/crossbench_output/speedometer_3.0/speedometer_3.0.json?q=disable-component-update&ss=chromium%2Fchromium%2Fsrc
            //Settings.AddFlag("disable-crashpad-for-testing");
            //Settings.AddFlag("disable-sync");
            Settings.AddFlag("disable-translate");
            Settings.AddFlag("disable-variations-seed-fetch");

            //Settings.AddFlag("disable-fre");
            Settings.AddFlag("no-default-browser-check");
            Settings.AddFlag("no-first-run");
            //Settings.AddFlag("disable-first-run-ui");
            //Settings.AddFlag("disable-ntp-most-likely-favicons-from-server");
            //Settings.AddFlag("disable-client-side-phishing-detection");
            Settings.AddFlag("disable-domain-reliability");


            Settings.AddFlag("disable-chrome-tracing-computation");
            //Settings.AddFlag("disable-scroll-to-text-fragment");

            //Settings.AddFlag("disable-ntp-other-sessions-menu");
            Settings.AddFlag("disable-default-apps");

            Settings.AddFlag("disable-modal-animations");
            //Settings.AddFlag("material-design-ink-drop-animation-speed", "fast");

            Settings.AddFlag("no-network-profile-warning");

            Settings.AddFlag("disable-login-animations");
            Settings.AddFlag("disable-stack-profiler");
            Settings.AddFlag("disable-system-font-check");
            //Settings.AddFlag("disable-infobars");
            Settings.AddFlag("disable-breakpad");
            Settings.AddFlag("disable-crash-reporter");
            Settings.AddFlag("disable-crashpad-forwarding");

            Settings.AddFlag("disable-top-sites");
            //Settings.AddFlag("disable-minimum-show-duration");
            //Settings.AddFlag("disable-startup-promos-for-testing");
            //Settings.AddFlag("disable-contextual-search");
            Settings.AddFlag("no-service-autorun");
            Settings.AddFlag("disable-auto-reload");
            //Settings.AddFlag("bypass-account-already-used-by-another-profile-check");

            //Settings.AddFlag("metrics-recording-only");

            //Settings.AddFlag("disable-cloud-policy-on-signin");

            //Settings.AddFlag("disable-dev-shm-usage");
            Settings.AddFlag("disable-dinosaur-easter-egg"); //enable-dinosaur-easter-egg-alt-images
            //Settings.AddFlag("oobe-skip-new-user-check-for-testing");

            //Settings.AddFlag("disable-gaia-services"); //https://source.chromium.org/chromium/chromium/src/+/main:ash/constants/ash_switches.cc
            
            Settings.AddFlag("wm-window-animations-disabled");
            Settings.AddFlag("animation-duration-scale", "0");
            Settings.AddFlag("disable-histogram-customizer");

            //REMOVE MOST CHROMIUM POPUPS
            Settings.AddFlag("suppress-message-center-popups");
            Settings.AddFlag("disable-prompt-on-repost");
            Settings.AddFlag("propagate-iph-for-testing");
            Settings.AddFlag("disable-search-engine-choice-screen");
            Settings.AddFlag("ash-no-nudges");
            Settings.AddFlag("noerrdialogs");
            Settings.AddFlag("disable-notifications");

            //Settings.AddFlag("hide-crash-restore-bubble");
            //Settings.AddFlag("disable-chrome-login-prompt");
        }

        private void SetFrameworkFlags(WebViewSettings Settings)
        {
            if (!LiteMode)
            {
                Settings.AddFlag("enable-webassembly-baseline");
                Settings.AddFlag("enable-webassembly-tiering");
                Settings.AddFlag("enable-webassembly-lazy-compilation");
                Settings.AddFlag("enable-webassembly-memory64");
            }
        }

        private void SetBackgroundFlags(WebViewSettings Settings)
        {
            //DISABLES ECOSIA SEARCHBOX
            //Settings.AddFlag("headless"); //Run in headless mode without a UI or display server dependencies.

            //https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/graphics/dark_mode_settings_builder.cc
            //https://chromium.googlesource.com/chromium/src/+/refs/heads/main/third_party/blink/renderer/platform/graphics/dark_mode_settings.h
            //Settings.AddFlag("dark-mode-settings", "");//ImagePolicy=1,ImageClassifierPolicy=1,InversionAlgorithm=3

            //Disabling site isolation somehow increases memory usage by 10 MB
            /*Settings.AddFlag("no-sandbox");
            Settings.AddFlag("disable-setuid-sandbox");
            Settings.AddFlag("site-isolation-trial-opt-out");
            Settings.AddFlag("disable-site-isolation-trials");
            Settings.AddFlag("isolate-origins", "https://challenges.cloudflare.com");*/

            //Settings.AddFlag("enable-raster-side-dark-mode-for-images");

            Settings.AddFlag("do-not-de-elevate");

            Settings.AddFlag("process-per-site");
            Settings.AddFlag("password-store", "basic");
            Settings.AddFlag("disable-mipmap-generation"); // Disables mipmap generation in Skia. Used a workaround for select low memory devices

            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139
            Settings.AddFlag("disable-highres-timer");

            //https://github.com/chromiumembedded/cef/blob/master/libcef/common/cef_switches.cc
            //Settings.AddFlag("enable-spelling-service");
            //Settings.AddFlag("disable-spell-checking");

            //Settings.AddFlag("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes"); //Causes memory to be 100 MB more than if disabled when minimized
            //Settings.AddFlag("quick-intensive-throttling-after-loading"); //Causes memory to be 100 MB more than if disabled when minimized
            Settings.AddFlag("intensive-wake-up-throttling-policy", "1");
            if (LiteMode)
            {
                //Turns device memory into 0.5
                Settings.AddFlag("enable-low-end-device-mode"); //Causes memory to be 20 MB more when minimized, but reduces 80 MB when not minimized

                //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/ui/startup/bad_flags_prompt.cc
                // This flag delays execution of base::TaskPriority::BEST_EFFORT tasks until
                // shutdown. The queue of base::TaskPriority::BEST_EFFORT tasks can increase
                // memory usage. Also, while it should be possible to use Chrome almost
                // normally with this flag, it is expected that some non-visible operations
                // such as writing user data to disk, cleaning caches, reporting metrics or
                // updating components won't be performed until shutdown.
                //switches::kDisableBestEffortTasks,
                Settings.AddFlag("disable-best-effort-tasks"); //NO LONGER PREVENTS GOOGLE LOGIN IN 27/6/2025

                Settings.AddFlag("disable-smooth-scrolling");

                Settings.AddFlag("disable-low-res-tiling"); //https://codereview.chromium.org/196473007/

                Settings.AddFlag("force-prefers-reduced-motion");
                Settings.AddFlag("disable-logging");

                Settings.AddFlag("max-web-media-player-count", "1");
                // 75 for desktop browsers and 40 for mobile browsers
                //https://chromium-review.googlesource.com/c/chromium/src/+/2816118

                Settings.AddFlag("gpu-program-cache-size-kb", $"{128 * 1024}");

                Settings.AddFlag("force-effective-connection-type", "Slow-2G-On-Cellular");
                //Settings.AddFlag("num-raster-threads", "4"); //RETIRED FLAG
                Settings.AddFlag("renderer-process-limit", "4");

                if (bool.Parse(GlobalSave.Get("ReduceDisk")))
                {
                    Settings.AddFlag("v8-cache-options", "none");
                    Settings.AddFlag("disk-cache-size", "1");
                    Settings.AddFlag("gpu-disk-cache-size-kb", "1");
                    Settings.AddFlag("skia-font-cache-limit-mb", "1");
                    Settings.AddFlag("skia-resource-cache-limit-mb", "1");
                    //Settings.AddFlag("disable-gpu-program-cache");
                    Settings.AddFlag("disable-gpu-shader-disk-cache");
                }
                else
                {
                    Settings.AddFlag("gpu-disk-cache-size-kb", $"{128 * 1024}");
                    Settings.AddFlag("disk-cache-size", $"{80 * 1024 * 1024}");//https://chromium.googlesource.com/chromium/src/+/master/net/disk_cache/cache_util.cc
                }
            }
            else
            {
                if (!bool.Parse(GlobalSave.Get("BrowserHardwareAcceleration")))
                    Settings.AddFlag("enable-low-res-tiling");
                if (HighPerformanceMode)
                {
                    Settings.AddFlag("no-pre-read-main-dll");
                    Settings.Flags.Remove("disable-mipmap-generation");
                    Settings.Flags.Remove("disable-highres-timer");
                    Settings.Flags.Remove("intensive-wake-up-throttling-policy");
                    Settings.AddFlag("disable-ipc-flooding-protection");
                    Settings.AddFlag("disable-renderer-backgrounding");
                    Settings.AddFlag("disable-background-timer-throttling");
                    Settings.AddFlag("disable-extensions-http-throttling");
                    Settings.AddFlag("allow-http-background-page");

                    Settings.AddFlag("enable-benchmarking");
                    //Settings.AddFlag("enable-benchmarking-api");
                    Settings.AddFlag("enable-net-benchmarking");

                    Settings.AddFlag("scheduler-configuration");
                    Settings.AddFlag("font-cache-shared-handle"); //Increases memory by 5 MB
                    //audio-process-high-priority
                }
                else
                {
                    Settings.AddFlag("renderer-process-limit", "10");
                    Settings.AddFlag("gpu-program-cache-size-kb", $"{2 * 1024 * 1024}");
                    Settings.AddFlag("gpu-disk-cache-size-kb", $"{2 * 1024 * 1024}");
                    //Settings.AddFlag("component-updater", "fast-update");
                }
            }

            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/optimization_guide/hints_fetcher_browsertest.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
            Settings.AddFlag("disable-fetching-hints-at-navigation-start");
            Settings.AddFlag("disable-model-download-verification");
            Settings.AddFlag("disable-component-update");
            Settings.AddFlag("component-updater", "disable-background-downloads,disable-delta-updates"); //https://source.chromium.org/chromium/chromium/src/+/main:components/component_updater/component_updater_command_line_config_policy.cc

            //https://github.com/portapps/brave-portable/issues/26
            //https://github.com/chromium/chromium/blob/2ca8c5037021c9d2ecc00b787d58a31ed8fc8bcb/third_party/blink/renderer/bindings/core/v8/v8_cache_options.h

            //Settings.AddFlag("disable-v8-idle-tasks");

            //Settings.AddFlag("enable-parallel-downloading");
        }

        private void SetGraphicsFlags(WebViewSettings Settings)
        {
            //Settings.AddFlag("in-process-gpu");//WARNING: Causes blank HTML dropdowns.
            if (bool.Parse(GlobalSave.Get("BrowserHardwareAcceleration")))
            {
                Settings.AddFlag("enable-gpu");
                Settings.AddFlag("enable-zero-copy");
                Settings.AddFlag("disable-software-rasterizer");
                Settings.AddFlag("enable-gpu-rasterization");
                //Settings.AddFlag("gpu-rasterization-msaa-sample-count", MainSave.Get("MSAASampleCount"));
                //if (MainSave.Get("AngleGraphicsBackend").ToLower() != "default")
                //    Settings.AddFlag("use-angle", MainSave.Get("AngleGraphicsBackend"));
                Settings.AddFlag("enable-accelerated-2d-canvas");
                /*if (LiteMode)
                    Settings.AddFlag("use-webgpu-power-preference", "force-low-power");
                else
                    Settings.AddFlag("use-webgpu-power-preference", "default-low-power");*/
                if (HighPerformanceMode)
                    Settings.AddFlag("force-high-performance-gpu");
            }
            else
            {
                Settings.AddFlag("disable-gpu");
                Settings.AddFlag("disable-gpu-compositing");
                Settings.AddFlag("disable-gpu-vsync");
                Settings.AddFlag("disable-gpu-shader-disk-cache");
                Settings.AddFlag("disable-accelerated-2d-canvas");
                Settings.AddFlag("disable-accelerated-video-encode");
                Settings.AddFlag("disable-accelerated-video-decode");
                Settings.AddFlag("disable-accelerated-mjpeg-decode");
                Settings.AddFlag("disable-video-capture-use-gpu-memory-buffer");
            }
        }

        public static void SetNetworkFlags(WebViewSettings Settings)
        {
            //Settings.AddFlag("reduce-transfer-size-updated-ipc");

            //Settings.AddFlag("enable-network-information-downlink-max");
            //Settings.AddFlag("enable-precise-memory-info");

            Settings.AddFlag("enable-quic");

            //Settings.AddFlag("no-proxy-server");
            //Settings.AddFlag("winhttp-proxy-resolver");
            Settings.AddFlag("no-pings");

            Settings.AddFlag("disable-background-networking");
            Settings.AddFlag("disable-component-extensions-with-background-pages");
        }

        private static void SetSecurityFlags(WebViewSettings Settings)
        {
            Settings.AddFlag("unsafely-disable-devtools-self-xss-warnings");
            Settings.AddFlag("disallow-doc-written-script-loads");
        }

        private void SetMediaFlags(WebViewSettings Settings)
        {
            //Settings.AddFlag("enable-canvas-2d-dynamic-rendering-mode-switching");

            //Settings.AddFlag("autoplay-policy", HighPerformanceMode ? "no-user-gesture-required" : "user-gesture-required");
            Settings.AddFlag("autoplay-policy", LiteMode ? "user-gesture-required" : "no-user-gesture-required");
            Settings.AddFlag("animated-image-resume");
            Settings.AddFlag("disable-image-animation-resync");
            Settings.AddFlag("disable-checker-imaging");

            if (LiteMode)
            {
                Settings.AddFlag("enable-lite-video");
                Settings.AddFlag("lite-video-force-override-decision");
            }

            //FLAG SEEMS TO NOT EXIST BUT IT DOES WORK
            Settings.AddFlag("enable-speech-input");

            //BREAKS PERMISSIONS, DO NOT ADD
            //Settings.AddFlag("enable-media-stream");

            //Settings.AddFlag("enable-media-session-service");

            Settings.AddFlag("enable-usermedia-screen-capturing");

            //Settings.AddFlag("disable-rtc-smoothness-algorithm");
            if (Settings.CefRuntimeStyle == CefRuntimeStyle.Alloy)
                Settings.AddFlag("auto-select-desktop-capture-source", "Entire screen");

            //Settings.AddFlag("turn-off-streaming-media-caching-always");
            //Settings.AddFlag("turn-off-streaming-media-caching-on-battery");
        }

        private void SetFeatureFlags(WebViewSettings Settings)
        {
            /*[Enums]
             * https://chromium.googlesource.com/chromium/blink/+/master/Source/bindings/core/v8/V8CacheOptions.h
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/public/mojom/webpreferences/web_preferences.mojom
             * https://source.chromium.org/chromium/chromium/src/+/main:net/nqe/effective_connection_type.cc
             */

            /*[enable/disable-features]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/common/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/autofill/core/common/autofill_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/segmentation_platform/public/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/feed/feed_feature_list.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:sandbox/policy/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:ui/base/ui_base_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:android_webview/common/aw_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:content/public/common/content_features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/page_image_service/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/scheduler/main_thread/memory_purge_manager.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:content/browser/loader/navigation_url_loader_impl.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/embedder_support/android/util/features.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:components/js_injection/renderer/js_communication.cc
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:android_webview/java/src/org/chromium/android_webview/common/ProductionSupportedFlagList.java
             * https://chromium.googlesource.com/chromium/src/+/efa55ec49b91438d5a9c0930ef19038d517914d1
             * 
             * BackForwardCacheWithKeepaliveRequest ReduceGpuPriorityOnBackground ProcessHtmlDataImmediately SetLowPriorityForBeacon
             * ImageService ImageServiceSuggestPoweredImages ImageServiceOptimizationGuideSalientImages
             * BackgroundResourceFetch
             */

            /*[js-flags]
             * https://chromium.googlesource.com/v8/v8/+/master/src/flags/flag-definitions.h
             * --always-opt //This does not improve performance, on the contrary; it causes V8 to waste CPU cycles on useless work.
             */

            /*[Others]
             * https://github.com/brave/brave-core/pull/19457
             * https://go.dev/solutions/google/chrome
             * https://news.ycombinator.com/item?id=32741359
             * https://github.com/brave/brave-browser/issues/13
             * https://github.com/brave/brave-core/pull/114/files
             * https://github.com/brave/brave-core/blob/master/app/feature_defaults_unittest.cc
             * https://github.com/brave/brave-browser/issues/3855
             * 
             * https://github.com/brave/brave-browser/wiki/Deviations-from-Chromium-(features-we-disable-or-remove)
             * https://github.com/Alex313031/thorium/blob/main/infra/PATCHES.md
             * https://github.com/search?q=repo%3Airidium-browser%2Firidium-browser+patch&type=commits
             * https://github.com/bromite/bromite/tree/master/build/patches
             * https://github.com/ungoogled-software/ungoogled-chromium/tree/master/patches
             * https://github.com/saiarcot895/chromium-ubuntu-build/tree/master/debian/patches
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:components/network_session_configurator/browser/network_session_configurator_unittest.cc
             * https://source.chromium.org/chromium/chromium/src/+/main:net/quic/quic_context.h
             * 
             * https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/pref_names.h
             * https://source.chromium.org/chromium/chromium/src/+/main:components/metrics/metrics_pref_names.cc
             * 
             * https://www.chromium.org/developers/design-documents/network-stack/disk-cache/very-simple-backend/
             */

            //https://source.chromium.org/chromium/chromium/src/+/main:components/network_session_configurator/browser/network_session_configurator.cc
            
            string JsFlags = string.Empty;
            //TODO: Add high performance mode JS flags
            if (!HighPerformanceMode)
                JsFlags = "--max-old-space-size=512 --optimize-gc-for-battery --memory-reducer-favors-memory --efficiency-mode --battery-saver-mode";// "--always-opt,--gc-global,--gc-experiment-reduce-concurrent-marking-tasks";

            //DEFAULT ENABLED: MemoryPurgeInBackground, stop-in-background
            //ANDROID: InputStreamOptimizations
            //MemorySaverModeRenderTuning
            //MemoryPurgeOnFreezeLimit
            //DevToolsImprovedNetworkError
            //MHTML_Improvements, OptimizeHTMLElementUrls,WebFontsCacheAwareTimeoutAdaption, EstablishGpuChannelAsync
            //https://source.chromium.org/chromium/chromium/src/+/main:components/download/public/common/download_features.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:services/network/public/cpp/features.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:base/features.cc
            //OptimizeWebRequestProxy
            //NOTE: Removed ParallelDownloading due to crashes.
            //DiskCacheBackendExperiment:backend/simple,
            //PartialLowEndModeOnMidRangeDevices,PartialLowEndModeOn3GbDevices, Android / ChromeOS exclusive.
            string EnableFeatures = "EnableTLS13EarlyData,HappyEyeballsV3,JXLImageFormat,EnableLazyLoadImageForInvisiblePage:enabled_page_type/all_invisible_page,HeapProfilerReporting,ReducedReferrerGranularity,ThirdPartyStoragePartitioning,PrecompileInlineScripts,OptimizeHTMLElementUrls,UseEcoQoSForBackgroundProcess,EnableLazyLoadImageForInvisiblePage,TrackingProtection3pcd,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
            //https://github.com/chromiumembedded/cef/issues/3991
            //https://github.com/chromiumembedded/cef/issues/3966
            string DisableFeatures = "KeepDefaultSearchEngineRendererAlive,StorageNotificationService,KAnonymityService,NetworkTimeServiceQuerying,LiveCaption,DefaultWebAppInstallation,PersistentHistograms,Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,OptimizationGuideOnDeviceModel,OptimizationGuideModelExecution,OnDeviceModelService,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,SegmentationPlatform,InstalledApp,BrowsingTopics,Fledge,FledgeBiddingAndAuctionServer,InterestFeedContentSuggestions,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
            //WebBluetooth,MediaRouter,
            string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility";
            string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI";//Adding ,DocumentPictureInPictureAPI would stop WebView2's NewWindowRequested from being called on PiP popups

            try { Settings.AddFlag("disable-features", DisableFeatures); }
            catch { Settings.Flags["disable-features"] += "," + DisableFeatures; }
            try { Settings.AddFlag("enable-features", EnableFeatures); }
            catch { Settings.Flags["enable-features"] += "," + EnableFeatures; }
            //enable/disable-blink-features: https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/runtime_enabled_features.json5
            try { Settings.AddFlag("enable-blink-features", EnableBlinkFeatures); }
            catch { Settings.Flags["enable-blink-features"] += "," + EnableBlinkFeatures; }
            try { Settings.AddFlag("disable-blink-features", DisableBlinkFeatures); }
            catch { Settings.Flags["disable-blink-features"] += "," + DisableBlinkFeatures; }

            /*[Blink Settings]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/public/common/web_preferences/web_preferences.h
             * https://chromium.googlesource.com/chromium/blink/+/refs/heads/main/Source/core/frame/Settings.in
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/core/frame/settings.json5
             * bypassCSP=true //disable Content Security Policy
             */
            //https://chromium.googlesource.com/chromium/src/+/HEAD/third_party/blink/public/platform/web_effective_connection_type.h
            Settings.AddFlag("blink-settings", "hyperlinkAuditingEnabled=false,smoothScrollForFindEnabled=true,spellCheckEnabledByDefault=false,hideDownloadUI=true");
            //,disallowFetchForDocWrittenScriptsInMainFrame=true
            
            if (bool.Parse(GlobalSave.Get("BrowserHardwareAcceleration")))
                Settings.Flags["enable-features"] += ",kD3D12VideoDecoder,kD3D12VideoEncodeAccelerator,D3D12SharedImageEncode";

            if (HighPerformanceMode)
            {
                JsFlags += " --fast-math";
                Settings.Flags["blink-settings"] += ",lazyLoadEnabled=false";
                //https://www.aboutchromebooks.com/chrome-flagsscheduler-configuration/
                Settings.Flags["enable-features"] += ",SchedulerConfiguration:scheduler_configuration/enabled";
            }
            else
            {
                Settings.Flags["blink-settings"] += ",batterySaverEnabled=true,preloadingDisabled=true,lowPriorityIframesThreshold=5,dnsPrefetchingEnabled=false,doHtmlPreloadScanning=false";
                Settings.Flags["enable-features"] += ",ThrottleMainFrameTo60Hz,ThrottleRepeatedNoDamageFrames,PauseMutedBackgroundAudio,InfiniteTabsFreezing,UnimportantFramesPriority,ThrottleUnimportantFrameRate,LazyImageLoading:automatic-lazy-load-images-enabled/true/restrict-lazy-load-images-to-data-saver-only/false,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LowLatencyCanvas2dImageChromium,LowLatencyWebGLImageChromium,NoStatePrefetchHoldback,ReduceCpuUtilization2,MemorySaverModeRenderTuning,OomIntervention,QuickIntensiveWakeUpThrottlingAfterLoading,LowerHighResolutionTimerThreshold,BatterySaverModeAlignWakeUps,RestrictThreadPoolInBackground,IntensiveWakeUpThrottling:grace_period_seconds/5,MemoryCacheStrongReference,OptOutZeroTimeoutTimersFromThrottling,CheckHTMLParserBudgetLessOften,Canvas2DHibernation,Canvas2DHibernationReleaseTransferMemory";
                Settings.Flags["disable-features"] += ",RenderMutedAudio,LoadingPredictorPrefetch,SpeculationRulesPrefetchFuture,NavigationPredictor,Prerender2MainFrameNavigation,Prerender2NoVarySearch,Prerender2";

                Settings.Flags["enable-blink-features"] += ",SkipPreloadScanning,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
                Settings.Flags["disable-blink-features"] += ",Prerender2";

                if (LiteMode)
                {
                    //https://github.com/cypress-io/cypress/issues/22622
                    //https://issues.chromium.org/issues/40220332
                    Settings.Flags["disable-features"] += ",LoadingTasksUnfreezable,LogJsConsoleMessages,BoostImagePriority,BoostImageSetLoadingTaskPriority,BoostFontLoadingTaskPriority,BoostVideoLoadingTaskPriority,BoostRenderBlockingStyleLoadingTaskPriority,BoostNonRenderBlockingStyleLoadingTaskPriority";
                    Settings.Flags["enable-features"] += ",LiteVideo,AllowAggressiveThrottlingWithWebSocket,stop-in-background,ClientHintsSaveData,SaveDataImgSrcset,LowPriorityScriptLoading,LowPriorityAsyncScriptExecution";
                    Settings.Flags["enable-blink-features"] += ",PrefersReducedData,ForceReduceMotion";
                    Settings.Flags["blink-settings"] += ",webGL1Enabled=false,webGL2Enabled=false,imageAnimationPolicy=1,prefersReducedTransparency=true,prefersReducedMotion=true,lazyLoadingFrameMarginPxUnknown=0,lazyLoadingFrameMarginPxOffline=0,lazyLoadingFrameMarginPxSlow2G=0,lazyLoadingFrameMarginPx2G=0,lazyLoadingFrameMarginPx3G=0,lazyLoadingFrameMarginPx4G=0,lazyLoadingImageMarginPxUnknown=0,lazyLoadingImageMarginPxOffline=0,lazyLoadingImageMarginPxSlow2G=0,lazyLoadingImageMarginPx2G=0,lazyLoadingImageMarginPx3G=0,lazyLoadingImageMarginPx4G=0";
                    JsFlags += " --max-lazy --lite-mode --noexpose-wasm --optimize-for-size";
                }
                else
                    Settings.Flags["blink-settings"] += ",lazyLoadingFrameMarginPxUnknown=250,lazyLoadingFrameMarginPxOffline=500,lazyLoadingFrameMarginPxSlow2G=500,lazyLoadingFrameMarginPx2G=400,lazyLoadingFrameMarginPx3G=300,lazyLoadingFrameMarginPx4G=200,lazyLoadingImageMarginPxUnknown=250,lazyLoadingImageMarginPxOffline=500,lazyLoadingImageMarginPxSlow2G=500,lazyLoadingImageMarginPx2G=400,lazyLoadingImageMarginPx3G=300,lazyLoadingImageMarginPx4G=200";
                //https://chromium.googlesource.com/v8/v8/+/master/src/flags/flag-definitions.h
                JsFlags += " --efficiency-mode --battery-saver-mode --memory-saver-mode";
            }
            if (!LiteMode)
                JsFlags += " --enable-experimental-regexp-engine-on-excessive-backtracks --expose-wasm --wasm-lazy-compilation --asm-wasm-lazy-compilation --wasm-lazy-validation --experimental-wasm-gc --wasm-async-compilation --wasm-opt --experimental-wasm-branch-hinting --experimental-wasm-instruction-tracing";
            if (!bool.Parse(GlobalSave.Get("JIT")))
                JsFlags += " --jitless";
            Settings.JavaScriptFlags = JsFlags;
        }

        private static void SetEdgeFlags(WebViewSettings Settings)
        {
            //edge-webview-foreground-boost-opt-in
            // Does this actually work? Disabling msSmartScreenProtection in --disable-features does seem to work
            //msLocalSpellcheck,msFreezeAdFramesImmediately,msEdgeAdaptiveCPUThrottling
            //msEdgeWebViewApplyWebResourceRequestedFilterForOOPIFs

            //WARNING: Do not include msWebView2CancelInitialNavigation. application/octet-stream URLs cause crashes on boot.
            string EnableFeatures = "msWebView2CodeCache,msWebView2TreatAppSuspendAsDeviceSuspend";
            try
            {
                Settings.AddFlag("enable-features", EnableFeatures);
            }
            catch
            {
                Settings.Flags["enable-features"] += "," + EnableFeatures;
            }
        }

        public Theme CurrentTheme;
        public Theme GetTheme(string Name = "")
        {
            if (string.IsNullOrEmpty(Name) && CurrentTheme != null)
                return CurrentTheme;
            return Themes.Find(i => i.Name == Name) ?? Themes[0];
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (AppInitialized)
                CloseSLBr(true);
            base.OnExit(e);
        }

        public async Task ClearAllData()
        {
            AdsBlocked = 0;
            History.Clear();
            Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
            await Cef.GetGlobalRequestContext().ClearHttpAuthCredentialsAsync();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                {
                    if (BrowserView != null && BrowserView.WebView != null && BrowserView.WebView.IsBrowserInitialized)
                    {
                        if (BrowserView.WebView.CanExecuteJavascript)
                            BrowserView.WebView.ExecuteScript("localStorage.clear();sessionStorage.clear();");
                        //https://github.com/cefsharp/CefSharp/issues/1234
                        await BrowserView.WebView.CallDevToolsAsync("Storage.clearDataForOrigin", new
                        {
                            origin = "*",
                            storageTypes = "all"
                        });
                        await BrowserView.WebView.CallDevToolsAsync("Page.clearCompilationCache");
                        await BrowserView.WebView.CallDevToolsAsync("Page.resetNavigationHistory");
                        await BrowserView.WebView.CallDevToolsAsync("Network.clearBrowserCookies");
                        await BrowserView.WebView.CallDevToolsAsync("Network.clearBrowserCache");
                        if (BrowserView.WebView is ChromiumEdgeWebView EdgeWebView)
                            EdgeWebView.BrowserCore?.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
                    }
                }
            }
            await CleanTempCache(true);
            InformationDialogWindow InfoWindow = new("Information", $"Settings", "All browsing data has been cleared.", "\ue713")
            {
                Topmost = true
            };
            InfoWindow.ShowDialog();
        }

        public async Task Save()
        {
            if (ReadOnlyInstance)
                return;
            string GlobalRaw = GlobalSave.Save();

            StatisticsSave.Set("BlockedAds", AdsBlocked.ToString());
            StatisticsSave.Save();

            string FavouritesRaw = JsonSerializer.Serialize(new BookmarksManager.Bookmarks() { Roots = new() { Bookmarks = new() { Name = "Bookmarks bar", Children = Favourites, Type = "folder" } } }, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(Path.Combine(UserApplicationDataPath, "Favourites.bin"), FavouritesRaw);

            SearchSave.Clear();
            SearchSave.Set("Count", SearchEngines.Count.ToString());
            for (int i = 0; i < SearchEngines.Count; i++)
            {
                SearchProvider _SearchProvider = SearchEngines[i];
                SearchSave.Set(i.ToString(), $"{_SearchProvider.Name}<#>{_SearchProvider.SearchUrl}<#>{_SearchProvider.SuggestUrl}");
            }
            string SearchRaw = SearchSave.Save();

            if (_AdBlockHandler != null)
            {
                AllowListSave.Clear();
                AllowListSave.Set("Count", _AdBlockHandler.Whitelist.Count.ToString());
                int DomainIndex = 0;
                foreach (string Domain in _AdBlockHandler.Whitelist)
                {
                    AllowListSave.Set(DomainIndex.ToString(), Domain);
                    DomainIndex++;
                }
            }
            string AllowListRaw = AllowListSave.Save();

            AdBlockSave.Clear();
            AdBlockSave.Set("Count", AdBlockLists.Count.ToString());
            for (int i = 0; i < AdBlockLists.Count; i++)
            {
                AdBlockList _AdBlockList = AdBlockLists[i];
                int Enabled = _AdBlockList.IsEnabled ? 1 : 0;
                AdBlockSave.Set(i.ToString(), $"{_AdBlockList.Name}<#>{_AdBlockList.Url}<#>{Enabled}");
            }
            string AdBlockRaw = AdBlockSave.Save();


            LanguagesSave.Clear();
            LanguagesSave.Set("Count", Languages.Count.ToString());
            LanguagesSave.Set("Selected", Languages.IndexOf(Locale));
            for (int i = 0; i < Languages.Count; i++)
                LanguagesSave.Set(i.ToString(), Languages[i].Tooltip);
            string LanguagesRaw = LanguagesSave.Save();

            foreach (FileInfo _File in new DirectoryInfo(UserApplicationWindowsPath).GetFiles())
                _File.Delete();
            if (bool.Parse(GlobalSave.Get("RestoreTabs")))
            {
                foreach (MainWindow _Window in AllWindows)
                {
                    Saving TabsSave = WindowsSaves[AllWindows.IndexOf(_Window)];
                    TabsSave.Clear();
                    int Count = 0;
                    int SelectedIndex = 0;
                    int OriginalSelectedIndex = _Window.TabsUI.SelectedIndex;
                    for (int i = 0; i < _Window.Tabs.Count; i++)
                    {
                        BrowserTabItem Tab = _Window.Tabs[i];
                        if ((Tab.Type == BrowserTabType.Navigation && !Tab.Content.Private) || Tab.Type == BrowserTabType.Group)
                        {
                            if (Tab.Type == BrowserTabType.Navigation)
                            {
                                TabsSave.Set(Count.ToString(), ((int)Tab.Type).ToString(), Tab.Content.Address, Tab.TabGroup?.Header ?? "");
                                if (i == OriginalSelectedIndex)
                                    SelectedIndex = Count;
                            }
                            else if (Tab.Type == BrowserTabType.Group)
                                TabsSave.Set(Count.ToString(), ((int)Tab.Type).ToString(), Utils.ColorToHex(Tab.TabGroup.Background.Color), Tab.TabGroup.Header, Tab.TabGroup.IsCollapsed ? "0" : "1");
                            Count++;
                        }
                    }
                    if (Count != 0)
                    {
                        TabsSave.Set("Selected", SelectedIndex.ToString());
                        TabsSave.Set("Count", Count.ToString());
                        TabsSave.Save();
                    }
                }
            }

            if (!PreventSync && bool.Parse(GlobalSave.Get("Sync")) && Utils.IsInternetAvailable())
            {
                try
                {
                    PreventSync = true;
                    string SyncGitHubToken = GlobalSave.Get("SyncGitHub");
                    if (!string.IsNullOrEmpty(SyncGitHubToken))
                    {
                        string SyncGistID = GlobalSave.Get("SyncGist");
                        string[] SyncedData = GlobalSave.Get("SyncData").Split(',');
                        Dictionary<string, string> SyncData = [];
                        //if (SyncedData.Contains("Tabs"))
                        if (SyncedData.Contains("Favourites"))
                            SyncData["Favourites.bin"] = FavouritesRaw;
                        if (SyncedData.Contains("Settings"))
                        {
                            SyncData["Save.bin"] = GlobalRaw;
                            SyncData["Search.bin"] = SearchRaw;
                            SyncData["Languages.bin"] = LanguagesRaw;
                            SyncData["AllowList.bin"] = AllowListRaw;
                            SyncData["AdBlock.bin"] = AdBlockRaw;
                        }
                        string SyncJson = JsonSerializer.Serialize(SyncData, new JsonSerializerOptions { WriteIndented = false });
                        HttpClient Client = new();
                        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SyncGitHubToken);
                        Client.DefaultRequestHeaders.UserAgent.ParseAdd($"SLBr/{ReleaseVersion}");
                        if (string.IsNullOrEmpty(SyncGistID))
                        {
                            var Payload = new
                            {
                                description = "SLBr Sync",
                                @public = false,
                                files = new Dictionary<string, object>
                                {
                                    ["slbr-sync.json"] = new { content = SyncJson }
                                }
                            };
                            var GistResponse = await Client.PostAsync("https://api.github.com/gists", new StringContent(JsonSerializer.Serialize(Payload), Encoding.UTF8, "application/json"));
                            string JSON = await GistResponse.Content.ReadAsStringAsync();

                            using JsonDocument Document = JsonDocument.Parse(JSON);
                            SyncGistID = Document.RootElement.GetProperty("id").GetString()!;
                            GlobalSave.Set("SyncGist", SyncGistID);
                            GlobalSave.Save();
                        }
                        else
                        {
                            var Payload = new
                            {
                                files = new Dictionary<string, object>
                                {
                                    ["slbr-sync.json"] = new { content = SyncJson }
                                }
                            };
                            await Client.PatchAsync($"https://api.github.com/gists/{SyncGistID}", new StringContent(JsonSerializer.Serialize(Payload), Encoding.UTF8, "application/json"));
                        }
                    }
                }
                catch { }
            }
        }

        public bool PreventSync = false;

        public async void CloseSLBr(bool ExecuteCloseEvents = true)
        {
            GCTimer?.Stop();
            new Thread(() => {
                Thread.Sleep(1000);
                try { Process.GetCurrentProcess().Kill(); }
                catch {}
            }) { IsBackground = true }.Start();
            if (AppInitialized)
                await Save();
            if (ExecuteCloseEvents)
            {
                for (int i = 0; i < AllWindows.Count; i++)
                    AllWindows[i].Close();
            }
            MiniHttpClient.Dispose();
            AppInitialized = false;
            Cef.Shutdown();
            Shutdown();
        }

        public BitmapSource TabIcon;
        public BitmapSource NewTabIcon;
        public BitmapSource PrivateIcon;
        public BitmapSource AudioIcon;
        public BitmapSource PDFTabIcon;
        public BitmapSource SettingsTabIcon;
        public BitmapSource HistoryTabIcon;
        public BitmapSource FavouritesTabIcon;
        public BitmapSource DownloadsTabIcon;
        public BitmapSource UnloadedIcon;

        public BitmapSource GetIcon(string Url, bool IsPrivate = false)
        {
            if (IsPrivate)
                return PrivateIcon;
            else if (Utils.GetFileExtension(Url) != ".pdf")
            {
                if (!IsPrivate && Utils.IsHttpScheme(Url) && bool.Parse(GlobalSave.Get("Favicons")))
                {
                    switch (GlobalSave.GetInt("FaviconService", 0))
                    {
                        case 0:
                            string GIconUrl = "https://t0.gstatic.com/faviconV2?client=chrome_desktop&nfrp=2&check_seen=true&size=24&min_size=16&max_size=256&fallback_opts=TYPE,SIZE,URL&url=" + Utils.CleanUrl(Url, true, true, true, false, false);
                            /*if (FaviconCache.TryGetValue(GIconUrl, out BitmapImage GCachedImage))
                                return GCachedImage;*/
                            BitmapImage _GImage = new(new Uri(GIconUrl));
                            _GImage.SafeFreeze();
                            //FaviconCache[GIconUrl] = _GImage;
                            return _GImage;
                        case 1:
                            string YIconUrl = "https://favicon.yandex.net/favicon/" + Utils.FastHost(Url);
                            /*if (FaviconCache.TryGetValue(YIconUrl, out BitmapImage YCachedImage))
                                return YCachedImage;*/
                            BitmapImage _YImage = new(new Uri(YIconUrl));
                            _YImage.SafeFreeze();
                            //FaviconCache[YIconUrl] = _YImage;
                            return _YImage;
                        case 2:
                            string DIconUrl = "https://icons.duckduckgo.com/ip3/" + Utils.FastHost(Url) + ".ico";
                            /*if (FaviconCache.TryGetValue(DIconUrl, out BitmapImage DCachedImage))
                                return DCachedImage;*/
                            BitmapImage _DImage = new(new Uri(DIconUrl));
                            _DImage.SafeFreeze();
                            //FaviconCache[DIconUrl] = _DImage;
                            return _DImage;
                        case 3:
                            string AIconUrl = "https://f1.allesedv.com/32/" + Utils.FastHost(Url);
                            /*if (FaviconCache.TryGetValue(AIconUrl, out BitmapImage ACachedImage))
                                return ACachedImage;*/
                            BitmapImage _AImage = new(new Uri(AIconUrl));
                            _AImage.SafeFreeze();
                            //FaviconCache[AIconUrl] = _AImage;
                            return _AImage;
                    }
                }
                else if (Url.StartsWith("slbr://newtab"))
                    return NewTabIcon;
                else if (Url.StartsWith("slbr://settings"))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://downloads"))
                    return DownloadsTabIcon;
                else if (Url.StartsWith("slbr://history"))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://favourites"))
                    return FavouritesTabIcon;
                return TabIcon;
            }
            else
                return PDFTabIcon;
        }

        private static readonly Dictionary<string, BitmapImage?> FaviconCache = [];
        private static readonly Dictionary<string, Task<BitmapImage?>> DownloadingFavicons = [];
        private static readonly LinkedList<string> CacheOrder = [];
        private const int MaxCacheSize = 500;
        private static void CacheFavicon(string Key, BitmapImage? Bitmap)
        {
            if (FaviconCache.ContainsKey(Key))
                return;
            FaviconCache[Key] = Bitmap;
            CacheOrder.AddLast(Key);
            if (CacheOrder.Count > MaxCacheSize)
            {
                var Oldest = CacheOrder.First!;
                CacheOrder.RemoveFirst();
                FaviconCache.Remove(Oldest.Value);
            }
        }

        public async Task<BitmapSource> SetIcon(string IconUrl, string Url = "", bool IsPrivate = false)
        {
            //TODO: Remove PDF icon.
            if (IsPrivate)
                return PrivateIcon;
            if (Utils.GetFileExtension(Url) != ".pdf")
            {
                if (Utils.IsHttpScheme(Url) && bool.Parse(GlobalSave.Get("Favicons")))
                {
                    //NOTE: Data scheme is not utilized within internal URLs.
                    if (IconUrl.StartsWith("data:"))
                    {
                        if (IconUrl.StartsWith("data:image/"))
                        {
                            try { return Utils.ConvertBase64ToBitmapImage(IconUrl); }
                            catch { }
                        }
                    }
                    else
            {
                        if (string.IsNullOrEmpty(IconUrl))
                            IconUrl = Utils.FastHost(Url, false, true) + "/favicon.ico";
                        if (Utils.IsHttpScheme(IconUrl))
                {
                    if (FaviconCache.TryGetValue(IconUrl, out BitmapImage? CachedImage))
                        return CachedImage ?? TabIcon;
                    try
                    {
                        if (DownloadingFavicons.TryGetValue(IconUrl, out Task<BitmapImage?>? PendingTask))
                            return await PendingTask ?? TabIcon;
                        async Task<BitmapImage?> DownloadIconTask()
                        {
                            try
                            {
                                using HttpRequestMessage Request = new(HttpMethod.Get, IconUrl);
                                Request.Headers.Referrer = new Uri(Utils.FastHost(IconUrl, false, true));
                                using var Response = await MiniHttpClient.SendAsync(Request);
                                        if (!Response.IsSuccessStatusCode)
                                        {
                                            CacheFavicon(IconUrl, null);
                                            return null;
                                        }
                                using Stream _Stream = await Response.Content.ReadAsStreamAsync();
                                BitmapImage Bitmap = new();
                                Bitmap.BeginInit();
                                Bitmap.StreamSource = _Stream;
                                Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                Bitmap.EndInit();
                                Bitmap.SafeFreeze();
                                CacheFavicon(IconUrl, Bitmap);
                                return Bitmap;
                            }
                                    catch { CacheFavicon(IconUrl, null); return null; }
                            finally { DownloadingFavicons.Remove(IconUrl); }
                        }
                        Task<BitmapImage?> IconTask = DownloadIconTask();
                        DownloadingFavicons[IconUrl] = IconTask;
                        return await IconTask ?? TabIcon;
                    }
                    catch { }
                }
                else if (IconUrl.StartsWith("data:image/"))
                {
                    try
                    {
                        return Utils.ConvertBase64ToBitmapImage(IconUrl);
                    }
                    catch { }
                }
                else if (Url.StartsWith("slbr://newtab"))
                    return NewTabIcon;
                else if (Url.StartsWith("slbr://settings"))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://history"))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://favourites"))
                    return FavouritesTabIcon;
                else if (Url.StartsWith("slbr://downloads"))
                    return DownloadsTabIcon;
                return TabIcon;
            }
            else
                return PDFTabIcon;
        }

        public bool MobileView;

        public void SetMobileView(bool Toggle)
        {
            UserAgent = Toggle ? UserAgentGenerator.BuildMobileUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}") : UserAgentGenerator.BuildUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}");
            MobileView = Toggle;
            UserAgentData = new()
            {
                Brands =
                [
                    new()
                    {
                        Brand = "SLBr",
                        Version = ReleaseVersion.Split('.')[0]
                    },
                    new()
                    {
                        Brand = "Chromium",
                        Version = Cef.ChromiumVersion.Split('.')[0]
                    }
                ],
                Architecture = Toggle ? "arm" : UserAgentGenerator.GetCPUArchitecture(),
                Model = string.Empty,
                Platform = Toggle ? "Android" : "Windows",
                PlatformVersion = Toggle ? "10" : UserAgentGenerator.GetPlatformVersion(),//https://textslashplain.com/2021/09/21/determining-os-platform-version/
                FullVersion = Cef.ChromiumVersion,
                Mobile = Toggle
            };
            //WARNING: \r\n SHOULD NOT BE REMOVED, CLOUDFLARE TURNSTILE WILL NOT WORK
            UserAgentBrandsString = "\r\n" + string.Join(", ", UserAgentData.Brands.Select(b => $"\"{b.Brand}\";v=\"{b.Version}\""));

            GlobalSave.Set("MobileView", Toggle);
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.WebView != null && i.WebView.IsBrowserInitialized))
                {
                    BrowserView.UserAgentBranding = !BrowserView.Private;
                    if (BrowserView.UserAgentBranding)
                    {
                        BrowserView.WebView?.CallDevToolsAsync("Emulation.setUserAgentOverride", new
                        {
                            userAgent = UserAgent,
                            userAgentMetadata = UserAgentData
                        });
                        BrowserView.WebView?.CallDevToolsAsync("Network.setUserAgentOverride", new
                        {
                            userAgent = UserAgent,
                            userAgentMetadata = UserAgentData
                        });
                    }
                }
            }
        }

        public void SetDimUnloadedIcon(bool Toggle)
        {
            GlobalSave.Set("DimUnloadedIcon", Toggle);
            foreach (MainWindow _Window in AllWindows)
                _Window.DimUnloadedIcon = Toggle;
        }
        public bool AllowHomeButton;
        public bool AllowTranslateButton;
        public bool AllowReaderModeButton;
        public bool AllowQRButton;
        public bool AllowWebEngineButton;
        public int ShowExtensionButton;
        public int ShowDownloadsButton;
        public int ShowFavouritesBar;
        public int TabAlignment;
        public double VerticalTabWidth;
        public void SetAppearance(Theme _Theme, int _TabAlignment, double _VerticalTabWidth, bool _AllowHomeButton, bool _AllowTranslateButton, bool _AllowReaderModeButton, int _ShowExtensionButton, int _ShowDownloadsButton, int _ShowFavouritesBar, bool _AllowQRButton, bool _AllowWebEngineButton)
        {
            AllowHomeButton = _AllowHomeButton;
            AllowTranslateButton = _AllowTranslateButton;
            AllowReaderModeButton = _AllowReaderModeButton;
            AllowQRButton = _AllowQRButton;
            AllowWebEngineButton = _AllowWebEngineButton;
            ShowExtensionButton = _ShowExtensionButton;
            ShowDownloadsButton = _ShowDownloadsButton;
            ShowFavouritesBar = _ShowFavouritesBar;
            TabAlignment = _TabAlignment;
            VerticalTabWidth = _VerticalTabWidth;

            GlobalSave.Set("TabAlignment", TabAlignment);
            GlobalSave.Set("VerticalTabWidth", _VerticalTabWidth);
            GlobalSave.Set("TranslateButton", AllowTranslateButton);
            GlobalSave.Set("HomeButton", AllowHomeButton);
            GlobalSave.Set("ReaderButton", AllowReaderModeButton);
            GlobalSave.Set("QRButton", AllowQRButton);
            GlobalSave.Set("WebEngineButton", AllowWebEngineButton);
            GlobalSave.Set("ExtensionButton", ShowExtensionButton);
            GlobalSave.Set("DownloadsButton", ShowDownloadsButton);
            GlobalSave.Set("FavouritesBar", ShowFavouritesBar);

            CurrentTheme = _Theme;
            GlobalSave.Set("Theme", CurrentTheme.Name);

            const int IconSize = 40;
            const int DPI = 94;
            Typeface IconTypeface = new(IconFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            Point Origin = new(1, 1);

            RenderTargetBitmap RenderFontIcon(string Text, Brush Foreground, Point Origin)
            {
                DrawingVisual Visual = new();
                using (DrawingContext Context = Visual.RenderOpen())
                {
                    FormattedText FormattedText = new(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, IconTypeface, IconSize, Foreground, VisualTreeHelper.GetDpi(Visual).PixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Center,
                        MaxTextWidth = IconSize
                    };
                    Context.DrawText(FormattedText, Origin);
                }
                RenderTargetBitmap Bitmap = new(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
                Bitmap.Render(Visual);
                Bitmap.SafeFreeze();
                return Bitmap;
            }

            Brush IconBrush = CurrentTheme.DarkWebPage ? Brushes.White : Brushes.Black;

            TabIcon = RenderFontIcon("\ue774", IconBrush, Origin);
            NewTabIcon = RenderFontIcon("\uEC6C", IconBrush, Origin);
            PDFTabIcon = RenderFontIcon("\uEA90", IconBrush, Origin);
            PrivateIcon = RenderFontIcon("\uE727", IconBrush, Origin);
            AudioIcon = RenderFontIcon("\ue767", IconBrush, Origin);
            SettingsTabIcon = RenderFontIcon("\uE713", IconBrush, Origin);
            HistoryTabIcon = RenderFontIcon("\ue81c", IconBrush, Origin);
            FavouritesTabIcon = RenderFontIcon("\ueb51", IconBrush, Origin);
            DownloadsTabIcon = RenderFontIcon("\ue896", IconBrush, new Point(2, 0));
            UnloadedIcon = RenderFontIcon("\uEC0A", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3AE872")), Origin);

            foreach (MainWindow _Window in AllWindows)
                _Window.SetAppearance(_Theme);
        }
    }

    public class Theme
    {
        public Theme(string _Name, Theme BaseTheme)
        {
            Name = _Name;
            PrimaryColor = BaseTheme.PrimaryColor;
            SecondaryColor = BaseTheme.SecondaryColor;
            BorderColor = BaseTheme.BorderColor;
            GrayColor = BaseTheme.GrayColor;
            FontColor = BaseTheme.FontColor;
            IndicatorColor = BaseTheme.IndicatorColor;

            DarkTitleBar = BaseTheme.DarkTitleBar;
            DarkWebPage = BaseTheme.DarkWebPage;
        }
        public Theme(string _Name, Color _PrimaryColor, Color _SecondaryColor, Color _BorderColor, Color _GrayColor, Color _FontColor, Color _IndicatorColor, bool _DarkTitleBar = false, bool _DarkWebPage = false)
        {
            Name = _Name;
            PrimaryColor = _PrimaryColor;
            SecondaryColor = _SecondaryColor;
            BorderColor = _BorderColor;
            GrayColor = _GrayColor;
            FontColor = _FontColor;
            IndicatorColor = _IndicatorColor;

            DarkTitleBar = _DarkTitleBar;
            DarkWebPage = _DarkWebPage;
        }
        public string Name;
        public Color BorderColor;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color GrayColor;
        public Color FontColor;
        public Color IndicatorColor;
        public bool DarkTitleBar;
        public bool DarkWebPage;
    }

    public enum Actions
    {
        Exit = 0,
        Undo = 1,
        Redo = 2,
        Refresh = 3,
        Navigate = 4,

        CreateTab = 5,
        CloseTab = 6,
        NewWindow = 7,
        UnloadTab = 8,

        DevTools = 9,
        SizeEmulator = 10,
        SetSideBarDock = 11,

        Favourite = 12,
        OpenFileExplorer = 13,
        OpenAsPopupBrowser = 14,
        SwitchUserPopup = 15,

        ReaderMode = 17,
        
        CloseSideBar = 22,
        NewsFeed = 23,

        Print = 30,
        Mute = 31,
        Find = 32,
        Share = 33,

        /*ZoomIn = 40,
        ZoomOut = 41,
        ZoomReset = 42,*/

        HardRefresh = 50,
        ClearCacheHardRefresh = 51,

        InstallWebApp = 56,
        QR = 57,
        SwitchWebEngine = 58,
        Translate = 59,

        CreateGroup = 80,
        Ungroup = 81,
        ModifyGroup = 82,
    }

    public class ActionStorage : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public ActionStorage(string _Name, string _Arguments, string _Tooltip)
        {
            Name = _Name;
            Arguments = _Arguments;
            Tooltip = _Tooltip;
        }

        public string Name
        {
            get => DName;
            set
            {
                DName = value;
                RaisePropertyChanged();
            }
        }
        public string Arguments
        {
            get => DArguments;
            set
            {
                DArguments = value;
                RaisePropertyChanged();
            }
        }
        public string Tooltip
        {
            get => DTooltip;
            set
            {
                DTooltip = value;
                RaisePropertyChanged();
            }
        }

        private string DName { get; set; }
        private string DArguments { get; set; }
        private string DTooltip { get; set; }
    }

    /*public struct CdnEntry
    {
        public readonly string Prefix;
        public readonly string Suffix;
        public readonly string LocalPath;

        public CdnEntry(string _Prefix, string _Suffix, string _LocalPath)
        {
            Prefix = _Prefix;
            Suffix = _Suffix;
            LocalPath = _LocalPath;
        }

        public bool TryMatch(string Url, out string? ResolvedPath)
        {
            ResolvedPath = null;
            if (!Url.StartsWith(Prefix)) return false;
            if (!Url.EndsWith(Suffix)) return false;
            int VersionStart = Prefix.Length;
            int VersionEnd = Url.Length - Suffix.Length;
            if (VersionEnd <= VersionStart) return false;

            string Version = Url.Substring(VersionStart, VersionEnd - VersionStart);
            ResolvedPath = string.Format(LocalPath, Version);
            return true;
        }
    }*/

    public static class Scripts
    {
        public const string WebView2ReplaceMisspelling = @"(function() {
    const active = document.activeElement;
    if (active && (active.tagName === ""INPUT"" || active.tagName === ""TEXTAREA"")) {
        const start = active.selectionStart;
        const end = active.selectionEnd;
        if (start === null || end === null || start === end) return;
        const before = active.value.substring(0, start);
        const after = active.value.substring(end);
        active.focus();
        active.select();
        const text = before + ""{0}"" + after;
        if (!document.execCommand('insertText', false, text)) active.setRangeText(text);
        active.selectionStart = active.selectionEnd = start + ""{0}"".length;
    }
})();";
        
        public const string GetFaviconScript = @"(function() { var links = document.getElementsByTagName('link');
    for (var i = 0; i < links.length; i++) {
        var rel = links[i].getAttribute('rel');
        if (rel && rel.toLowerCase().indexOf('icon') !== -1)
        {
            var href = links[i].getAttribute('href') || '';
            var isSvg = (href.toLowerCase().indexOf('.svg') !== -1) || ((links[i].getAttribute('type') || '').toLowerCase().indexOf('svg') !== -1);
            if (!isSvg) return href;
        }
    }
    return '';
})();";
        public const string ReaderModeScript = @"(function() {
  const allowedTags = new Set([
    ""a"", ""p"", ""blockquote"", ""code"", ""span"",
    ""h1"", ""h2"", ""h3"", ""h4"", ""h5"", ""h6"",
    ""img"", ""video"",
    ""ul"", ""ol"", ""li"",
    ""em"", ""strong"", ""b"", ""i"", ""u"", ""br""
  ]);
  const allowedAttrs = {
    ""a"": [""href""],
    ""img"": [""src"", ""alt""],
    ""video"": [""controls""],
    ""source"": [""src"", ""type""],
    ""ul"": [], ""ol"": [], ""li"": [],
    ""code"": [],
    ""blockquote"": [],
    ""span"": [],
    ""p"": [],
    ""em"": [], ""strong"": [], ""b"": [], ""i"": [], ""u"": [], ""br"": [],
    ""h1"": [], ""h2"": [], ""h3"": [], ""h4"": [], ""h5"": [], ""h6"": []
  };
  const blacklistSelectors = [
    ""nav"", ""footer"", ""header"", ""aside"",
    ""script"", ""style"",
    ""[class='ad' i]"", ""[class^='ad-' i]"", ""[class$='-ad' i]"",
    ""[id='ad' i]"", ""[id^='ad-' i]"", ""[id$='-ad' i]"",
    ""[class*='social' i]"", ""[id*='social' i]"",
    ""[class*='promo' i]"", ""[id*='promo' i]"",
    ""[class*='related' i]"", ""[id*='related' i]"",
    ""[class*='comments' i]"", ""[id*='comments' i]"",
    ""[class*='share' i]"", ""[id*='share' i]""
  ];
    blacklistSelectors.forEach(selector => {
        document.body.querySelectorAll(selector).forEach(el => el.remove());
    });
    const socialWords = [""share"", ""save"", ""facebook"", ""twitter"", ""linkedin"", ""whatsapp""];
    document.body.querySelectorAll(""a, button"").forEach(el => {
        const text = el.textContent.trim().toLowerCase();
        if (socialWords.some(word => text.includes(word))) el.remove();
    });
  function isBlacklisted(node) {
    if (node.nodeType !== Node.ELEMENT_NODE) return false;
    return blacklistSelectors.some(sel => node.matches(sel));
  }
  function cleanNode(node) {
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent.replace(/\s+/g, "" "").trim();
      if (!text) return document.createDocumentFragment();
      if (/window\.[A-Za-z0-9_]+\s*=/.test(text)) return document.createDocumentFragment();
      return document.createTextNode(text);
    }
    if (node.nodeType === Node.ELEMENT_NODE) {
      if (isBlacklisted(node)) return document.createDocumentFragment();
      const tag = node.tagName.toLowerCase();
      if (!allowedTags.has(tag)) {
        const fragment = document.createDocumentFragment();
        for (const child of node.childNodes) fragment.appendChild(cleanNode(child));
        return fragment;
      }
      const el = document.createElement(tag);
      if (allowedAttrs[tag]) {
        for (const attr of allowedAttrs[tag]) {
          if (node.hasAttribute(attr)) el.setAttribute(attr, node.getAttribute(attr));
        }
      }
      for (const child of node.childNodes) el.appendChild(cleanNode(child));
      return el;
    }

    return document.createDocumentFragment();
  }

  function getTextLength(el) {
    return el.innerText ? el.innerText.replace(/\s+/g, "" "").length : 0;
  }
  function findMainContent() {
    const candidates = Array.from(document.querySelectorAll(""article, main, [role='main']""));
    if (candidates.length === 0) {
      Array.from(document.querySelectorAll(""div"")).forEach(div => {
        if (getTextLength(div) > 200) candidates.push(div);
      });
    }
    if (candidates.length === 0) return document.body;
    return candidates.reduce((a, b) => getTextLength(a) > getTextLength(b) ? a : b);
  }
  const root = findMainContent();
  const cleaned = cleanNode(root.cloneNode(true));
  const container = document.createElement(""div"");
  container.appendChild(cleaned);
  let contentHtml = container.innerHTML.replace(/\s*\n\s*/g, ""\n"").replace(/\n{2,}/g, ""\n\n"");
  document.head.innerHTML = `
    <meta charset=""utf-8"">
    <title>${document.title.trim()}</title>
    <style>
      body {
        max-width: 720px;
        margin: 2rem auto;
        font-family: system-ui, sans-serif;
        font-size: 1.05rem;
        line-height: 1.6;
        background: #fafafa;
        color: #222;
        padding: 0 1rem;
      }
      h1, h2, h3, h4, h5, h6 {
        margin: 1.2em 0 0.5em;
      }
      p { margin: 1em 0; }
    p a::before {
        content: "" "";
    }

    p a::after {
        content: "" "";
    }
      blockquote {
        border-left: 5px solid #ccc;
        margin: 1em 0;
        padding: 0.5em 1em;
        color: #555;
        background: #f9f9f9;
        border-radius: 5px;
      }
      code {
        background: #eee;
        padding: 0.2em 0.4em;
        border-radius: 4px;
        font-family: monospace;
      }
      pre code {
        display: block;
        padding: 1em;
        overflow-x: auto;
      }
      img, video {
        max-width: 100%;
        display: block;
        margin: 1em auto;
        border-radius: 5px;
      }
      ul, ol { margin: 1em 0 1em 2em; }
      a { color: #0645ad; text-decoration: none; }
      a:hover { text-decoration: underline; }
    </style>`;
  document.body.innerHTML=contentHtml.trim();
})();";


        public const string AntiCloseScript = "window.close=function(){};";
        public const string AntiFullscreenScript = "Element.prototype.requestFullscreen=function(){};Element.prototype.webkitRequestFullscreen=function(){};document.exitFullscreen=function(){};document.webkitExitFullscreen=function(){};Object.defineProperties(document,{fullscreenElement:{get:()=>null},webkitFullscreenElement:{get:()=>null}});";
    
        public const string ArticleScript = "(function(){var metaTags=document.getElementsByTagName('meta');for(var i=0;i<metaTags.length;i++){if (metaTags[i].getAttribute('property')==='og:type'&&metaTags[i].getAttribute('content')==='article'){return true;}if (metaTags[i].getAttribute('name')==='article:author'){return true;}}return false;})();";
        
        public const string CefAudioScript = @"(function() {
  if (window.__cef_audio__) return;
  window.__cef_audio__ = true;
  let lastState = null;
  let checkScheduled = false;
  let __slbr_audio_observer__ = null;
  let checksWithoutMedia = 0;
  function checkElements() {
    let playing = false;
    let foundMedia = false;
    const mediaEls = document.querySelectorAll('audio,video');
    if (mediaEls.length > 0) {
      foundMedia = true;
      for (let i = 0; i < mediaEls.length; i++) {
        const el = mediaEls[i];
        if (!el.paused && !el.muted && el.volume > 0 && el.readyState > 2) {
          playing = true;
          break;
        }
      }
    }
    if (!playing && window.__cef_audio_ctxs) {
      if (window.__cef_audio_ctxs.length > 0) foundMedia = true;
      for (let i = 0; i < window.__cef_audio_ctxs.length; i++) {
        if (window.__cef_audio_ctxs[i].state === 'running') {
          playing = true;
          break;
        }
      }
    }
    if (playing !== lastState) {
      lastState = playing;
      engine.postMessage({ type: '__cef_audio__', playing: playing ? 1 : 0 });
    }
    if (!foundMedia) {
      checksWithoutMedia++;
      if (checksWithoutMedia > 20 && __slbr_audio_observer__) {
        __slbr_audio_observer__.disconnect();
        __slbr_audio_observer__ = null;
        console.log(""Cef audio monitor: MutationObserver auto-disabled (no media found)."");
      }
    }
    else checksWithoutMedia = 0;
  }
  function scheduleCheck() {
    if (!checkScheduled) {
      checkScheduled = true;
      requestAnimationFrame(() => {
        checkScheduled = false;
        checkElements();
      });
    }
  }
  (function () {
    const Orig = window.AudioContext || window.webkitAudioContext;
    if (!Orig) return;
    window.__cef_audio_ctxs = [];
    function WrappedAudioContext() {
      const ctx = new Orig();
      window.__cef_audio_ctxs.push(ctx);
      const oldClose = ctx.close.bind(ctx);
      ctx.close = function () {
        const i = window.__cef_audio_ctxs.indexOf(ctx);
        if (i > -1) window.__cef_audio_ctxs.splice(i, 1);
        return oldClose();
      };
      return ctx;
    }
    WrappedAudioContext.prototype = Orig.prototype;
    window.AudioContext = WrappedAudioContext;
  })();
  ['play', 'playing', 'pause', 'volumechange', 'ended'].forEach(function (ev) {
    document.addEventListener(ev, scheduleCheck, true);
  });
  if (document.body) {
    __slbr_audio_observer__ = new MutationObserver(scheduleCheck);
    __slbr_audio_observer__.observe(document.body, { childList: true, subtree: true });
  }
  checkElements();
})();";
        /*public const string TridentAudioScript = @"(function(){
  if (window.__trident_audio__) return;
  window.__trident_audio__ = true;
  var lastState = null;
  function check() {
    var playing = false;
    var els = document.querySelectorAll('audio,video');
    for (var i = 0; i < els.length; i++) {
      var el = els[i];
      if (!el.paused && !el.muted && el.volume > 0 && el.readyState > 2) {
        playing = true;
        break;
      }
    }
    if (playing !== lastState) {
      lastState = playing;
      if (window.external && typeof window.external.audioChanged === ""function"") {
        window.external.audioChanged(playing ? 1 : 0);
      }
    }
  }
  var events = ['play','playing','pause','volumechange','ended'];
  for (var i = 0; i < events.length; i++) {
    document.addEventListener(events[i], check, true);
  }
  setInterval(check, 1000);
  check();
})();";*/

        public const string FileScript = @"(function () {
if (window.__slbr_file__) return;
window.__slbr_file__ = true;
document.documentElement.setAttribute('style',""display:table;margin:auto;"")
document.body.setAttribute('style',""margin:35px auto;font-family:system-ui;"")
var HeaderElement=document.getElementById('header');
HeaderElement.setAttribute('style',""border:2px solid grey;border-radius:5px;padding:0 10px;margin:0 0 10px 0;"")
HeaderElement.textContent=HeaderElement.textContent.replace('Index of ','');
document.getElementById('nameColumnHeader').setAttribute('style',""text-align:left;padding:7.5px;"");
document.getElementById('sizeColumnHeader').setAttribute('style',""text-align:center;padding:7.5px;"");
document.getElementById('dateColumnHeader').setAttribute('style',""text-align:center;padding:7.5px;"");
var fstyle=document.createElement('style');
fstyle.type='text/css';
fstyle.innerHTML=`@media (prefers-color-scheme:light){a{color:black;}tr:nth-child(even){background-color: gainsboro;}#theader{background-color:gainsboro;}}
@media (prefers-color-scheme:dark){a{color:white;}tr:nth-child(even){background-color:#202225;}#theader{background-color:#202225;}}
td:first-child,th:first-child{border-radius:5px 0 0 5px;}
td:last-child,th:last-child{border-radius:0 5px 5px 0;}
table{width:100%;}`;
document.body.appendChild(fstyle);
const ParentDir=document.getElementById('parentDirLinkBox');
if (ParentDir)
{
    if (window.getComputedStyle(ParentDir).display === 'block'){ParentDir.setAttribute('style','display:block;padding:7.5px;margin:0 0 10px 0;');}
    else{ParentDir.setAttribute('style','display:none;');}
    ParentDir.querySelector('a.icon.up').setAttribute('style','background:none;padding-inline-start:.25em;');
    var element=document.createElement('p');
    element.setAttribute('style',""font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;color:navajowhite;"")
    element.innerHTML='';
    ParentDir.prepend(element);
    ParentDir.querySelector('#parentDirText').innerHTML=""Parent Directory"";
}
document.querySelectorAll('tbody > tr').forEach(row => {
    const link=row.querySelector('a.icon');
    if (link){
        link.setAttribute('style', 'background: none; padding-inline-start: .5em;');
        var element=document.createElement('p');
        if (row.querySelector('a.icon.dir')){
            link.textContent=link.textContent.replace(/\/$/,'');
            element.innerHTML='';
            element.setAttribute('style',""font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;color:navajowhite;"")
        }
        else if (row.querySelector('a.icon.file')){
            if (link.innerHTML.endsWith("".pdf""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".png"")||link.innerHTML.endsWith("".jpg"")||link.innerHTML.endsWith("".jpeg"")||link.innerHTML.endsWith("".avif"")||link.innerHTML.endsWith("".svg"")||link.innerHTML.endsWith("".webp"")||link.innerHTML.endsWith("".jfif"")||link.innerHTML.endsWith("".bmp""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".mp4"")||link.innerHTML.endsWith("".avi"")||link.innerHTML.endsWith("".ogg"")||link.innerHTML.endsWith("".webm"")||link.innerHTML.endsWith("".mov"")||link.innerHTML.endsWith("".mpej"")||link.innerHTML.endsWith("".wmv"")||link.innerHTML.endsWith("".h264"")||link.innerHTML.endsWith("".mkv""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".zip"")||link.innerHTML.endsWith("".rar"")||link.innerHTML.endsWith("".7z"")||link.innerHTML.endsWith("".tar.gz"")||link.innerHTML.endsWith("".tgz""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".txt""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".mp3"")||link.innerHTML.endsWith("".mp2""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".gif""))
                element.innerHTML='';
            else if (link.innerHTML.endsWith("".blend"")||link.innerHTML.endsWith("".obj"")||link.innerHTML.endsWith("".fbx"")||link.innerHTML.endsWith("".max"")||link.innerHTML.endsWith("".stl"")||link.innerHTML.endsWith("".x3d"")||link.innerHTML.endsWith("".3ds"")||link.innerHTML.endsWith("".dae"")||link.innerHTML.endsWith("".glb"")||link.innerHTML.endsWith("".gltf"")||link.innerHTML.endsWith("".ply""))
                element.innerHTML='';
            else
                element.innerHTML='';
            element.setAttribute('style',""font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;"")
        }
        row.querySelector('td').prepend(element);
        row.children.item(0).setAttribute('style',""text-align:left;padding:7.5px;"");
        row.children.item(1).setAttribute('style',""text-align:center;padding:7.5px;"");
        row.children.item(2).setAttribute('style',""text-align:center;padding:7.5px;"");
    }
});
})();";
        public const string InternalScript = @"window.internal = {
    receive: function(data) {
        const [key, ...rest] = data.split('=');
        const value = rest.join('=');
        switch (key) {
          case ""cors"":
            onCorsResult(decodeURIComponent(value));
            break;
          case ""file_picker"":
            onFilePickerResult(decodeURIComponent(value));
            break;
        }
    },
    search: function(val) {
        engine.postMessage({type:""__internal__"",function:'search',variable:val});
    }
};";
        public const string NotificationPolyfill = @"(function () {
if (window.__slbr_notification__) return;
window.__slbr_notification__ = true;
class Notification {
constructor(title, options = {}) {
    if(Notification.permission!=='granted') throw new Error(""Notification permission not granted."");
    this.title = title;
    this.body = options.body || """";
    this.icon = options.icon || """";
    this.image = options.image || """";
    this.silent = options.silent === true; 
    this.onclick = null;
    this.onshow = null;
    this.onclose = null;
    this.onerror = null;
    if(typeof engine !== 'undefined' && typeof engine.postMessage === 'function') {
        let packageSet=new Set();packageSet.add(title).add(options);
        engine.postMessage({type:""__notification__"",
                data: JSON.stringify({
                    title: this.title,
                    body: this.body,
                    icon: this.icon,
                    image: this.image,
                    silent: this.silent
                })});
    }
    setTimeout(() => {if(typeof this.onshow==='function')this.onshow();},0);
    if(Notification.autoClose) setTimeout(()=>this.close(),Notification.autoClose);
}
close(){if(typeof this.onclose==='function')this.onclose();}
static requestPermission(callback){if(callback)callback('granted');return Promise.resolve('granted');}
static get permission(){return 'granted';}
}
Notification.autoClose = 7000;
window.Notification = Notification;
})();";
        public const string WebStoreScript = @"(function () {
if (window.__slbr_web_store__) return;
window.__slbr_web_store__ = true;
function scanButton(){
const buttonQueries = ['button span[jsname]:not(:empty)']
for (const button of document.querySelectorAll(buttonQueries.join(','))){
    const text=button.textContent||''
    if (text==='Add to Chrome'||text==='Remove from Chrome') button.textContent=text.replace('Chrome','SLBr')
  }
}
scanButton();
new MutationObserver(scanButton).observe(document.body,{attributes:true,childList:true,subtree:true});
})();";
        public const string ScrollScript = @"!function(){var e,t,o,n={pulseNormalize:1},r=n,a=!1,i={x:0,y:0},l=!1,c=document.documentElement,u=[],s=37,d=38,f=39,m=40,h=32,v=33,w=34,y=35,p=36,b={37:1,38:1,39:1,40:1};function g(){if(!l&&document.body){l=!0;var o=document.body,n=document.documentElement,r=window.innerHeight,i=o.scrollHeight;if(c=0<=document.compatMode.indexOf(""CSS"")?n:o,e=o,X(""keydown"",B),top!=self)a=!0;else if(r<i&&(o.offsetHeight<=r||n.offsetHeight<=r)){var u,s=document.createElement(""div"");if(s.style.cssText=""position:absolute; z-index:-10000; top:0; left:0; right:0; height:""+c.scrollHeight+""px"",document.body.appendChild(s),t=function(){u||(u=setTimeout(function(){s.style.height=""0"",s.style.height=c.scrollHeight+""px"",u=null},500))},setTimeout(t,10),X(""resize"",t),new _(t).observe(o,{attributes:!0,childList:!0,characterData:!1}),c.offsetHeight<=r){var d=document.createElement(""div"");d.style.clear=""both"",o.appendChild(d)}}}}var x=[],k=!1,E=Date.now();function C(e,t,o){var n,r;n=0<(n=t)?1:-1,r=0<(r=o)?1:-1,(i.x!==n||i.y!==r)&&(i.x=n,i.y=r,x=[],E=0);var a=Date.now()-E;if(a<50){var l=(1+50/a)/2;1<l&&(l=Math.min(l,3),t*=l,o*=l)}if(E=Date.now(),x.push({x:t,y:o,lastX:t<0?.99:-.99,lastY:o<0?.99:-.99,start:Date.now()}),!k){var c=V(),u=e===c||e===document.body;null==e.$scrollBehavior&&function(e){var t=D(e);if(null==K[t]){var o=getComputedStyle(e,"""")[""scroll-behavior""];K[t]=""smooth""==o}return K[t]}(e)&&(e.$scrollBehavior=e.style.scrollBehavior,e.style.scrollBehavior=""auto"");var s=function(n){for(var r=Date.now(),a=0,i=0,l=0;l<x.length;l++){var c=x[l],d=r-c.start,f=d>=400,m=f?1:d/400;m=G(m);var h=c.x*m-c.lastX|0,v=c.y*m-c.lastY|0;a+=h,i+=v,c.lastX+=h,c.lastY+=v,f&&(x.splice(l,1),l--)}u?window.scrollBy(a,i):(a&&(e.scrollLeft+=a),i&&(e.scrollTop+=i)),t||o||(x=[]),x.length?I(s,e,1e3/150+1):(k=!1,null!=e.$scrollBehavior&&(e.style.scrollBehavior=e.$scrollBehavior,e.$scrollBehavior=null))};I(s,e,0),k=!0}}function S(t){l||g();var n=t.target;if(t.defaultPrevented||t.ctrlKey)return!0;if(Y(e,""embed"")||Y(n,""embed"")&&/\.pdf/i.test(n.src)||Y(e,""object"")||n.shadowRoot)return!0;var r=-t.wheelDeltaX||t.deltaX||0,i=-t.wheelDeltaY||t.deltaY||0;r||i||(i=-t.wheelDelta||0),1===t.deltaMode&&(r*=40,i*=40);var c=$(n);return c?!!function(e){if(e){u.length||(u=[e,e,e]),e=Math.abs(e),u.push(e),u.shift(),clearTimeout(o),o=setTimeout(function(){try{localStorage.SS_deltaBuffer=u.join("","")}catch(e){}},1e3);var t=120<e&&A(e);return!A(120)&&!A(100)&&!t}}(i)||(1.2<Math.abs(r)&&(r*=100/120),1.2<Math.abs(i)&&(i*=100/120),C(c,r,i),t.preventDefault(),void N()):!a||!W||(Object.defineProperty(t,""target"",{value:window.frameElement}),parent.wheel(t))}function B(t){var o=t.target,n=t.ctrlKey||t.altKey||t.metaKey||t.shiftKey&&t.keyCode!==h;document.body.contains(e)||(e=document.activeElement);var r=/^(button|submit|radio|checkbox|file|color|image)$/i;if(t.defaultPrevented||/^(textarea|select|embed|object)$/i.test(o.nodeName)||Y(o,""input"")&&!r.test(o.type)||Y(e,""video"")||function(e){var t=e.target,o=!1;if(-1!=document.URL.indexOf(""www.youtube.com/watch""))do{if(o=t.classList&&t.classList.contains(""html5-video-controls""))break}while(t=t.parentNode);return o}(t)||o.isContentEditable||n)return!0;if((Y(o,""button"")||Y(o,""input"")&&r.test(o.type))&&t.keyCode===h)return!0;if(Y(o,""input"")&&""radio""==o.type&&b[t.keyCode])return!0;var i=0,l=0,c=$(e);if(!c)return!a||!W||parent.keydown(t);var u=c.clientHeight;switch(c==document.body&&(u=window.innerHeight),t.keyCode){case d:l=-50;break;case m:l=50;break;case h:l=-(t.shiftKey?1:-1)*u*.9;break;case v:l=.9*-u;break;case w:l=.9*u;break;case p:c==document.body&&document.scrollingElement&&(c=document.scrollingElement),l=-c.scrollTop;break;case y:var g=c.scrollHeight-c.scrollTop-u;l=0<g?g+10:0;break;case s:i=-50;break;case f:i=50;break;default:return!0}C(c,i,l),t.preventDefault(),N()}function H(t){e=t.target}var M,T,D=(M=0,function(e){return e.uniqueID||(e.uniqueID=M++)}),L={},z={},K={};function N(){clearTimeout(T),T=setInterval(function(){L=z=K={}},1e3)}function O(e,t,o){for(var n=o?L:z,r=e.length;r--;)n[D(e[r])]=t;return t}function $(e){var t=[],o=document.body,n=c.scrollHeight;do{var r=z[D(e)];if(r)return O(t,r);if(t.push(e),n===e.scrollHeight){var i=j(c)&&j(o)||q(c);if(a&&P(c)||!a&&i)return O(t,V())}else if(P(e)&&q(e))return O(t,e)}while(e=e.parentElement)}function P(e){return e.clientHeight+10<e.scrollHeight}function j(e){return""hidden""!==getComputedStyle(e,"""").getPropertyValue(""overflow-y"")}function q(e){var t=getComputedStyle(e,"""").getPropertyValue(""overflow-y"");return""scroll""===t||""auto""===t}function X(e,t,o){window.addEventListener(e,t,o||!1)}function Y(e,t){return e&&(e.nodeName||"""").toLowerCase()===t.toLowerCase()}if(window.localStorage&&localStorage.SS_deltaBuffer)try{u=localStorage.SS_deltaBuffer.split("","")}catch(S){}function R(e,t){return Math.floor(e/t)==e/t}function A(e){return R(u[0],e)&&R(u[1],e)&&R(u[2],e)}var F,I=window.requestAnimationFrame||window.webkitRequestAnimationFrame||window.mozRequestAnimationFrame||function(e,t,o){window.setTimeout(e,o||1e3/60)},_=window.MutationObserver||window.WebKitMutationObserver||window.MozMutationObserver,V=(F=document.scrollingElement,function(){if(!F){var e=document.createElement(""div"");e.style.cssText=""height:10000px;width:1px;"",document.body.appendChild(e);var t=document.body.scrollTop;document.documentElement.scrollTop,window.scrollBy(0,3),F=document.body.scrollTop!=t?document.body:document.documentElement,window.scrollBy(0,-3),document.body.removeChild(e)}return F});function U(e){var t;return((e*=4)<1?e-(1-Math.exp(-e)):(e-=1,(t=Math.exp(-1))+(1-Math.exp(-e))*(1-t)))*r.pulseNormalize}function G(e){return 1<=e?1:e<=0?0:(1==r.pulseNormalize&&(r.pulseNormalize/=U(1)),U(e))}try{window.addEventListener(""test"",null,Object.defineProperty({},""passive"",{get:function(){ee=!0}}))}catch(S){}var J=!!ee&&{passive:!1},Q=""onwheel""in document.createElement(""div"")?""wheel"":""mousewheel"";Q&&(X(Q,S,J),X(""mousedown"",H),X(""load"",g))}();";
        public const string ExtensionScript = "var rect=document.body.getBoundingClientRect();engine.postMessage({width:rect.width+16,height:rect.height+40});";
        
        public const string OpenSearchScript = @"(function(){let link=document.querySelector('link[rel=""search""][type=""application/opensearchdescription+xml""]');if (link){engine.postMessage({type:'__opensearch__',url:link.href,name:link.title||''});}})();";

        public const string ShiftContextMenuScript = @"document.addEventListener('contextmenu',function(e){if (e.shiftKey){e.stopPropagation();}},true);";
        public const string AllowInteractionScript = @"document.addEventListener(""DOMContentLoaded"",function(){[""contextmenu"",""selectstart"",""copy"",""cut"",""paste"",""mousedown""].forEach(t=>{document.body.addEventListener(t,function(t){t.stopPropagation()},!0)});let t=document.createElement(""style"");t.textContent=""*{user-select:text !important;-webkit-user-select:text !important;pointer-events:auto !important;}"",document.head.appendChild(t)});";
        public const string ForceContextMenuScript = @"document.querySelectorAll('[oncontextmenu]').forEach(el=>{el.removeAttribute('oncontextmenu');});
let atcall=document.getElementsByTagName(""*"");
for (let i=0;i<atcall.length;i++){if (typeof atcall[i].oncontextmenu==='function'){atcall[i].oncontextmenu = null;}}
const atcAddEvent = EventTarget.prototype.addEventListener;
EventTarget.prototype.addEventListener = function(type,listener,options) {
    if (type==='contextmenu'){
        const wrapped=function(e){const result=listener(e);e.stopImmediatePropagation();return result;};
        return atcAddEvent.call(this,type,wrapped,options);
    }
    return atcAddEvent.call(this,type,listener,options);
};
window.addEventListener(""contextmenu"",e=>{e.stopImmediatePropagation();},true);";
        public const string RemoveFilterCSS = "var atrbstyle=document.createElement(\"style\");atrbstyle.textContent=\"*{filter:none !important;backdrop-filter:none !important;}\",document.head.append(atrbstyle);";
        public const string RemoveOverlayCSS = "!function(){function e(){function e(e){let o=getComputedStyle(e);return(parseInt(o.zIndex)||0)>=1e3&&(\"fixed\"===o.position||\"absolute\"===o.position)&&e.offsetHeight>200&&e.offsetWidth>200}document.querySelectorAll(\"*\").forEach(o=>{e(o)&&o.remove()});let o=new MutationObserver(o=>{for(let t of o)t.addedNodes.forEach(o=>{1===o.nodeType&&e(o)&&o.remove()})});o.observe(document.body,{childList:!0,subtree:!0})}document.body?e():window.addEventListener(\"DOMContentLoaded\",e)}();";

        public const string LateAntiDevtoolsScript = @"(function(){'use strict';
    function suspiciousCallback(callback){try{const code=callback.toString();return code.includes('debugger')||code.includes('about:blank')||code.includes('performance.now')||code.includes('console.clear')||code.includes('window.outerHeight')||code.includes('openDevTools');}catch{return false;}}
    Object.defineProperty(window,'outerWidth',{get:function(){return window.innerWidth;}});
    Object.defineProperty(window,'outerHeight',{get:function(){return window.innerHeight;}});

    const originalSetInterval=window.setInterval;
    window.setInterval=function(callback,delay) {
        if (typeof callback==='function'&&typeof delay==='number'&&suspiciousCallback(callback)) {
            return originalSetInterval(()=>{},delay);
        }
        return originalSetInterval.apply(this,arguments);
    };
    const originalSetTimeout=window.setTimeout;
    window.setTimeout=function(callback,delay) {
        if (typeof callback==='function'&&typeof delay==='number'&&suspiciousCallback(callback)) {
            return originalSetTimeout(()=>{},delay);
        }
        return originalSetTimeout.apply(this,arguments);
    };

    const originalAddEventListener=window.addEventListener;
    window.addEventListener=function(type,listener,options){if (type==='resize'){return;}return originalAddEventListener.apply(this,arguments);};
    window.onresize=()=>{};
    const originalConsole=window.console;
    window.console={
        ...originalConsole,
        log:function(){},
        table:function(){},
        clear:function(){}
    };
    const originalRegExpToString=RegExp.prototype.toString;
    RegExp.prototype.toString=function(){try{return originalRegExpToString.call(this);}catch{return '';}};
    const originalDefineProperty=Object.defineProperty;
    Object.defineProperty=function(obj,prop,descriptor) {
        try{if (prop==='id'&&obj instanceof HTMLElement&&descriptor.get){return originalDefineProperty(obj,prop,{value:'bypassed-id'});}}catch{}
        return originalDefineProperty.apply(this,arguments);
    };
})();
";
        public const string DetectPWA = "(async()=>{const link=document.querySelector('link[rel=\"manifest\"]');const manifest=link?link.href:null;let service_worker=false;try{service_worker=!!(navigator.serviceWorker&&(await navigator.serviceWorker.ready));}catch{}return{manifest,service_worker};})();";

        public const string GetTranslationText = @"(function() {
function shouldAcceptNode(node) {
    let parent = node.parentNode
    while (parent) {
        const tag = parent.tagName ? parent.tagName.toLowerCase() : ''
        if (['script', 'style', 'meta', 'link', 'noscript'].includes(tag)) return NodeFilter.FILTER_REJECT
        parent = parent.parentNode
    }
    const trim = node.textContent.trim()
    if (!trim || trim.length <= 1 || trim.startsWith('<') || trim.includes('{') || trim.includes('}') || /^[\s<>{}\\/]+$/.test(trim)) return NodeFilter.FILTER_REJECT
    return NodeFilter.FILTER_ACCEPT
}
const texts = []
const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, { acceptNode: shouldAcceptNode }, false)
let node
while (node = walker.nextNode()) {
    texts.push(node.textContent.trim())
}
return JSON.stringify(texts);
})();";

        public const string SetTranslationText = @"(function() {{
const translations = {0};
const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {{
        acceptNode: function(node) {{
            let parent = node.parentNode;
            while (parent) {{
                const tag = parent.tagName ? parent.tagName.toLowerCase() : '';
                if (['script','style','meta','link','noscript'].includes(tag)) return NodeFilter.FILTER_REJECT;
                parent = parent.parentNode;
            }}
            const trimmed = node.textContent.trim();
            if (!trimmed || trimmed.length <= 1 || trimmed.startsWith('<') || trimmed.includes('{{') || trimmed.includes('}}') || /^[\\s<>{{}}\\/]+$/.test(trimmed)) return NodeFilter.FILTER_REJECT;
            return NodeFilter.FILTER_ACCEPT;
        }}
    }}, false
);
let node, i = 0;
while (node = walker.nextNode()) {{
    if (i < translations.length) {{
        const beforeMatch = node.textContent.match(/^\s*/);
        const afterMatch = node.textContent.match(/\s*$/);
        const before = beforeMatch ? beforeMatch[0] : """";
        const after = afterMatch ? afterMatch[0] : """";
        node.textContent = before + translations[i] + after;
        i++;
    }}
}}
}})();";

        public const string CheckNativeDarkModeScript = @"(function() {
const brightness = (rgbStr) => {
    const m = rgbStr.match(/\d+/g);
    if (!m) return 255;
    const [r,g,b] = m.map(Number);
    return 0.299*r + 0.587*g + 0.114*b;
};
const colors = new Set();
const elements = [document.documentElement, document.body, ...document.querySelectorAll('*')].slice(0, 100);
for (const el of elements) {
    const bg = getComputedStyle(el).backgroundColor;
    if (bg && bg !== 'transparent' && !bg.includes('rgba(0, 0, 0, 0)')) {
    colors.add(bg);
    }
}
const brights = [...colors].map(brightness);
const avg = brights.length ? brights.reduce((a,b)=>a+b,0)/brights.length : 255;
return avg < 110 ? 0 : 1;
})();";

        public const string EstimatedMemoryUsageScript = @"(function() {
const domMemory = document.getElementsByTagName('*').length * 2048;
const imageMemory = [...document.images].reduce((sum, img) => sum + (img.naturalWidth * img.naturalHeight * 4), 0);
const canvasMemory = [...document.querySelectorAll('canvas')].reduce((sum, c) => sum + (c.width * c.height * 4), 0);
const total = performance.memory.totalJSHeapSize + domMemory + imageMemory + canvasMemory;
return Math.round(total / (1024 * 1024) * 10) / 10;
})();";

        public const string TextFragmentRangeScript = @"(async() => {
    const { generateFragment } = await import('https://unpkg.com/text-fragments-polyfill/dist/fragment-generation-utils.js');
    const result = generateFragment(window.getSelection());
    if (result.status === 0) {
        const fragment = result.fragment;
        const prefix = fragment.prefix ? `${encodeURIComponent(fragment.prefix).replaceAll('-','%2D')}-,` : '';
        const suffix = fragment.suffix ? `,-${encodeURIComponent(fragment.suffix).replaceAll('-','%2D')}` : '';
        const start = encodeURIComponent(fragment.textStart).replaceAll('-','%2D');
        const end = fragment.textEnd ? `,${encodeURIComponent(fragment.textEnd).replaceAll('-','%2D')}` : '';
        return `${prefix}${start}${end}${suffix}`;
    }
    return null;
})();";

        public const string WebView2DocumentCreatedScript = @"window.engine = window.chrome.webview;
window.addEventListener('auxclick', function(e) {
    if (e.button === 1) window.chrome.webview.postMessage({ type: '__edge_tab__', background: 1 });
}, true);
window.addEventListener('click', function(e) {
    if (e.ctrlKey) window.chrome.webview.postMessage({ type: '__edge_tab__', background: 1 });
    else if (e.shiftKey) window.chrome.webview.postMessage({ type: '__edge_tab__', background: 0 });
    else if (e.button === 0) window.chrome.webview.postMessage({ type: '__edge_tab__', background: 0 });
}, true);";

        public const string DefaultBackgroundColorScript = @"(function() {
    function isTransparent(color){return !color||color==='transparent'||color==='rgba(0, 0, 0, 0)';}
    if (isTransparent(window.getComputedStyle(document.documentElement).backgroundColor)&&isTransparent(window.getComputedStyle(document.body).backgroundColor))document.documentElement.style.setProperty('background-color', 'white');
})();";

        /*public const string FetchOpenGraphProtocolScript = @"(async () => {
  const getMeta = (selectors) => {
    for (const selector of selectors) {
      const el = document.querySelector(selector);
      if (el && el.getAttribute('content')) return el.getAttribute('content');
    }
    return null;
  };
  const data = {
    title: getMeta(['meta[property=""og:title""]', 'meta[name=""twitter:title""]']) || document.title,
    description: getMeta(['meta[property=""og:description""]', 'meta[name=""twitter:description""]', 'meta[name=""description""]']),
    image: getMeta(['meta[property=""og:image""]', 'meta[name=""twitter:image""]']),
    theme: getMeta(['meta[name=""theme-color""]', 'meta[name=""msapplication-TileColor""]']),
    type: getMeta(['meta[property=""og:type""]']) || 'website'
  };
  return JSON.stringify(data);
})();";*/
    }

    public class WebAppManifest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("short_name")] public string ShortName { get; set; }
        [JsonPropertyName("start_url")] public string StartUrl { get; set; } = "/";
        [JsonPropertyName("display")] public string Display { get; set; } = "standalone";
        [JsonPropertyName("background_color")] public string BackgroundColor { get; set; }
        [JsonPropertyName("theme_color")] public string ThemeColor { get; set; }
        [JsonPropertyName("icons")] public List<ManifestIcon> Icons { get; set; } = [];
    }

    public class ManifestIcon
    {
        [JsonPropertyName("src")] public string Source { get; set; }
        [JsonPropertyName("sizes")] public string Sizes { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("purpose")] public string Purpose { get; set; }
    }
}
