using CefSharp;
using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Handlers;
using SLBr.Pages;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml.Linq;

namespace SLBr
{
    public class HotKey
    {
        public HotKey(Action _Callback, int _KeyCode, bool HasControl, bool HasShift, bool HasAlt)
        {
            Callback = _Callback;
            KeyCode = _KeyCode;
            Control = HasControl;
            Shift = HasShift;
            Alt = HasAlt;
        }

        public int KeyCode { get; }
        public bool Control { get; }
        public bool Shift { get; }
        public bool Alt { get; }

        public Action Callback { get; }
    }

    public static class HotKeyManager
    {
        public static HashSet<HotKey> HotKeys = new HashSet<HotKey>();

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

    public class Extension : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        private string PID;
        public string ID
        {
            get { return PID; }
            set
            {
                PID = value;
                RaisePropertyChanged(nameof(ID));
            }
        }

        private string PName;
        public string Name
        {
            get { return PName; }
            set
            {
                PName = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        private string PPopup;
        public string Popup
        {
            get { return PPopup; }
            set
            {
                PPopup = value;
                RaisePropertyChanged(nameof(Popup));
            }
        }

        private string PVersion;
        public string Version
        {
            get { return PVersion; }
            set
            {
                PVersion = value;
                RaisePropertyChanged(nameof(Version));
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
            get { return PDescription; }
            set
            {
                PDescription = value;
                RaisePropertyChanged(nameof(Description));
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

    public class SearchProvider
    {
        public string Name = "";
        public string Host = "";
        public string SearchUrl = "";
        public string SuggestUrl = "";
    }

    public class OmniSuggestion
    {
        public string Text { get; set; }
        public string Result { get; set; }
        public string Icon { get; set; }
        public SolidColorBrush Color { get; set; }
    }

    public class DownloadEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        private string PID;
        public string ID
        {
            get { return PID; }
            set
            {
                PID = value;
                RaisePropertyChanged(nameof(ID));
            }
        }

        private string PFileName;
        public string FileName
        {
            get { return PFileName; }
            set
            {
                PFileName = value;
                RaisePropertyChanged(nameof(FileName));
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
        }

        private int PPercentComplete;
        public int PercentComplete
        {
            get { return PPercentComplete; }
            set
            {
                PPercentComplete = value;
                RaisePropertyChanged(nameof(PercentComplete));
            }
        }

        private string PFormattedProgress;
        public string FormattedProgress
        {
            get { return PFormattedProgress; }
            set
            {
                PFormattedProgress = value;
                RaisePropertyChanged(nameof(FormattedProgress));
            }
        }

        private Visibility POpen;
        public Visibility Open
        {
            get { return POpen; }
            set
            {
                POpen = value;
                RaisePropertyChanged(nameof(Open));
            }
        }

        private Visibility PStop;
        public Visibility Stop
        {
            get { return PStop; }
            set
            {
                PStop = value;
                RaisePropertyChanged(nameof(Stop));
            }
        }

        private Visibility PProgress;
        public Visibility Progress
        {
            get { return PProgress; }
            set
            {
                PProgress = value;
                RaisePropertyChanged(nameof(Progress));
            }
        }

        private bool PIsIndeterminate;
        public bool IsIndeterminate
        {
            get { return PIsIndeterminate; }
            set
            {
                PIsIndeterminate = value;
                RaisePropertyChanged(nameof(IsIndeterminate));
            }
        }
    }

    public partial class App : Application
    {
        public static App Instance;

        public const string AMPEndpoint = "https://acceleratedmobilepageurl.googleapis.com/v1/ampUrls:batchGet";

        public List<MainWindow> AllWindows = new List<MainWindow>();
        public List<SearchProvider> SearchEngines = new List<SearchProvider>();
        public SearchProvider DefaultSearchProvider;
        public List<Theme> Themes = new List<Theme>()
        {
            new Theme("Light", Colors.White, Colors.WhiteSmoke, Colors.Gainsboro, Colors.Gray, Colors.Black, (Color)ColorConverter.ConvertFromString("#3399FF"), false, false),
            new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), (Color)ColorConverter.ConvertFromString("#2F3136"), (Color)ColorConverter.ConvertFromString("#36393F"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3399FF"), true, true),
            new Theme("Purple", (Color)ColorConverter.ConvertFromString("#191025"), (Color)ColorConverter.ConvertFromString("#251C31"), (Color)ColorConverter.ConvertFromString("#2B2237"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#934CFE"), true, true),
        };
        public DomainList AdBlockAllowList = new DomainList();

        public IdnMapping _IdnMapping = new IdnMapping();

        public Saving GlobalSave;
        public Saving FavouritesSave;
        public Saving SearchSave;
        public Saving StatisticsSave;
        public Saving LanguagesSave;
        public Saving AllowListSave;

        public List<Saving> WindowsSaves = new List<Saving>();

        public SolidColorBrush FavouriteColor;
        public SolidColorBrush SLBrColor;
        public SolidColorBrush RedColor;
        public SolidColorBrush CornflowerBlueColor;
        public SolidColorBrush NavajoWhiteColor;
        public SolidColorBrush LimeGreenColor;
        public SolidColorBrush OrangeColor;
        public SolidColorBrush GreenColor;
        public FontFamily IconFont;
        public FontFamily SLBrFont;

        public string Username = "Default";
        public string UserApplicationWindowsPath;
        public string UserApplicationDataPath;
        public string ExecutablePath;
        public string ExtensionsPath;
        public string ResourcesPath;
        //public string CdnPath;

        bool AppInitialized;

        public static readonly string[] URLConfusables =
        {
            "rn",//m, rnicrosoft
            "vv",//w
            "cl",//d
            "0",//o
            "1",//l
            "5",//S
        };

        public ObservableCollection<ActionStorage> Favourites = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> History = new ObservableCollection<ActionStorage>();
        private List<Extension> PrivateExtensions = new List<Extension>();
        public List<Extension> Extensions
        {
            get { return PrivateExtensions; }
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
                                BrowserView.ExtensionsButton.Visibility = value.Any() ? Visibility.Visible : Visibility.Collapsed;
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

        public Dictionary<string, WebAppManifest> AvailableWebAppManifests = new Dictionary<string, WebAppManifest>();

        public void AddHistory(string Url, string Title)
        {
            for (int i = 0; i < History.Count; i++)
            {
                ActionStorage Entry = History[i];
                if (Entry.Tooltip == Url)
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
        public ObservableCollection<DownloadEntry> VisibleDownloads = new ObservableCollection<DownloadEntry>();
        public Dictionary<string, WebDownloadItem> Downloads = new Dictionary<string, WebDownloadItem>();
        public void UpdateDownloadItem(WebDownloadItem Item)
        {
            Downloads[Item.ID] = Item;
            Dispatcher.BeginInvoke(() =>
            {
                foreach (MainWindow _Window in AllWindows)
                    _Window.TaskbarProgress.ProgressValue = Item.State == WebDownloadState.Completed ? 0 : Item.Progress;
                DownloadEntry _Entry = VisibleDownloads.FirstOrDefault(d => d.ID == Item.ID);
                if (_Entry != null)
                {
                    if (string.IsNullOrEmpty(_Entry.FileName))
                    {
                        _Entry.FileName = Path.GetFileName(Item.FullPath);
                        if (!string.IsNullOrEmpty(_Entry.FileName))
                        {
                            switch (_Entry.FileName.Split(".").Last())
                            {
                                case "zip":
                                case "rar":
                                case "7z":
                                case "tgz":
                                case "gz"://.tar.gz
                                    _Entry.Icon = "\uF012";
                                    break;
                                case "txt":
                                    _Entry.Icon = "\uF000";
                                    break;
                                case "png":
                                case "jpg":
                                case "jpeg":
                                case "avif":
                                case "svg":
                                case "webp":
                                case "jfif":
                                case "bmp":
                                    _Entry.Icon = "\uE91B";
                                    break;
                                case "gif":
                                    _Entry.Icon = "\uF4A9";
                                    break;
                                case "mp3":
                                case "mp2":
                                    _Entry.Icon = "\uEA69";
                                    break;
                                case "pdf":
                                    _Entry.Icon = "\uEA90";
                                    break;

                                case "blend":
                                case "obj":
                                case "fbx":
                                case "max":
                                case "stl":
                                case "x3d":
                                case "3ds":
                                case "dae":
                                case "glb":
                                case "gltf":
                                case "ply":
                                    _Entry.Icon = "\uF158";
                                    break;
                                case "mp4":
                                case "avi":
                                case "ogg":
                                case "webm":
                                case "mov":
                                case "mpej":
                                case "wmv":
                                case "h264":
                                case "mkv":
                                    _Entry.Icon = "\uE786";
                                    break;
                                default:
                                    _Entry.Icon = "\uE8A5";
                                    break;
                            }
                            MainWindow Current = CurrentFocusedWindow();
                            if (Current != null)
                            {
                                Browser BrowserView = Current.Tabs[Current.TabsUI.SelectedIndex].Content;
                                if (BrowserView != null)
                                    BrowserView.OpenDownloadsButton.OpenPopup();
                            }
                        }
                    }
                    _Entry.PercentComplete = (int)(Item.Progress * 100);
                    if (Item.State == WebDownloadState.Completed)
                    {
                        _Entry.FormattedProgress = $"{FormatBytes(Item.TotalBytes)} - Complete";
                        _Entry.Open = Visibility.Visible;
                        _Entry.Stop = Visibility.Collapsed;
                        _Entry.Progress = Visibility.Collapsed;
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
                        if (Item.TotalBytes > 0)
                            _Entry.FormattedProgress = FormatBytes(Item.ReceivedBytes, false) + "/" + FormatBytes(Item.TotalBytes) + " - Downloading";
                        else
                        {
                            _Entry.FormattedProgress = FormatBytes(Item.ReceivedBytes) + " - Downloading";
                            _Entry.IsIndeterminate = true;
                        }
                        _Entry.Open = Visibility.Collapsed;
                        _Entry.Stop = Visibility.Visible;
                        _Entry.Progress = Visibility.Visible;
                    }
                    /*_Entry.Icon = "\ue7ba"
                     _Entry.FormattedProgress = "Suspicious download blocked"
                    _Entry.Icon = "\uea39"
                     _Entry.FormattedProgress = "Dangerous download blocked"*/
                }
                else
                    VisibleDownloads.Insert(0, new DownloadEntry { ID = Item.ID });
            });
        }

        static string[] FileSizes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string FormatBytes(long Bytes, bool ContainSizes = true)
        {
            if (Bytes == 0)
                return "0 Byte";
            int i = (int)Math.Floor(Math.Log(Bytes) / Math.Log(1000));
            string Output = (Bytes / Math.Pow(1000, i)).ToString("F2");
            if (ContainSizes)
                Output += $" {FileSizes[i]}";
            return Output;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Instance = this;
            InitializeApp();
            JumpList jumpList = new JumpList();
            jumpList.ShowRecentCategory = true;
            jumpList.ShowFrequentCategory = true;
            JumpTask NewWindowTask = new JumpTask
            {
                Title = "New window",
                Description = "Open a new browser window",
                ApplicationPath = ExecutablePath,
                Arguments = "--window",
                IconResourcePath = ExecutablePath,
                IconResourceIndex = 0
            };
            jumpList.JumpItems.Add(NewWindowTask);
            JumpList.SetJumpList(Current, jumpList);
            jumpList.Apply();
        }

        static Mutex _Mutex;

        public string UserAgent;
        public string UserAgentBrandsString;
        public WebUserAgentMetaData UserAgentData;

        public void LoadExtensions()
        {
            Extensions.Clear();
            if (Directory.Exists(ExtensionsPath))
            {
                var ExtensionsDirectory = Directory.GetDirectories(ExtensionsPath);
                if (ExtensionsDirectory.Length != 0)
                {
                    List<Extension> _Extensions = new List<Extension>();
                    foreach (var ExtensionParentDirectory in ExtensionsDirectory)
                    {
                        try
                        {
                            string ExtensionDirectory = Directory.EnumerateDirectories(ExtensionParentDirectory).FirstOrDefault();
                            if (Directory.Exists(ExtensionDirectory))
                            {
                                string[] Manifests = Directory.GetFiles(ExtensionDirectory, "manifest.json", SearchOption.TopDirectoryOnly);
                                foreach (string ManifestFile in Manifests)
                                {
                                    JsonElement Manifest = JsonDocument.Parse(File.ReadAllText(ManifestFile)).RootElement;

                                    Extension _Extension = new Extension() { ID = Path.GetFileName(ExtensionParentDirectory), Version = Manifest.GetProperty("version").ToString()/*, ManifestVersion = Manifest.GetProperty("manifest_version").ToString()*/ };

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
                                    List<string> VarsInMessages = new List<string>();
                                    if (Manifest.TryGetProperty("name", out JsonElement ExtensionName))
                                    {
                                        string Name = ExtensionName.GetString();
                                        if (Name.StartsWith("__MSG_", StringComparison.Ordinal))
                                            VarsInMessages.Add($"Name<|>{Name}");
                                        else
                                            _Extension.Name = Name;
                                    }
                                    if (Manifest.TryGetProperty("description", out JsonElement ExtensionDescription))
                                    {
                                        string Description = ExtensionDescription.GetString();
                                        if (Description.StartsWith("__MSG_", StringComparison.Ordinal))
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
                                            JsonElement Messages = JsonDocument.Parse(File.ReadAllText(MessagesFile)).RootElement;
                                            string[] Vars = Var.Split("<|>");
                                            if (Vars[0] == "Description")
                                            {
                                                _Extension.Description = Messages.GetProperty(Vars[1].Remove(0, 5).Trim('_')).GetProperty("message").ToString();
                                                break;
                                            }
                                            else if (Vars[0] == "Name")
                                            {
                                                _Extension.Name = Messages.GetProperty(Vars[1].Remove(0, 5).Trim('_')).GetProperty("message").ToString();
                                                break;
                                            }
                                        }
                                    }

                                    //_Extension.IsEnabled = true;
                                    _Extensions.Add(_Extension);
                                }
                            }
                        }
                        catch { }
                    }
                    Extensions = _Extensions;
                }
            }
        }

        public static OmniSuggestion GenerateSuggestion(string Text, string Type, SolidColorBrush IconColor)
        {
            OmniSuggestion Suggestion = new OmniSuggestion { Text = Text, Color = IconColor };
            switch (Type)
            {
                case "S":
                    Suggestion.Icon = "\xE721";
                    break;
                case "W":
                    Suggestion.Icon = "\xE71B";
                    break;
                case "P":
                    Suggestion.Icon = "\xE756";
                    break;
                case "C":
                    Suggestion.Icon = "\xE943";
                    break;
                case "F":
                    Suggestion.Icon = "\xe838";//e8b7
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
            else if (Text.StartsWith("file:///", StringComparison.Ordinal))
                return "File";
            else if (Utils.IsUrl(Text))
                return "Url";
            return "Search";
        }*/

        public async Task<List<(string Word, List<string> Suggestions)>> SpellCheck(string Text)
        {
            var Results = new List<(string, List<string>)>();
            string Json = await MiniHttpClient.GetStringAsync(string.Format(SECRETS.SPELLCHECK_ENDPOINT, Locale.Tooltip, WebUtility.UrlEncode(Text)));

            using JsonDocument Document = JsonDocument.Parse(Json);
            if (Document.RootElement.TryGetProperty("matches", out JsonElement Matches))
            {
                foreach (JsonElement Match in Matches.EnumerateArray())
                {
                    if (!Match.TryGetProperty("context", out JsonElement context))
                        continue;

                    List<string> Suggestions = new();
                    if (Match.TryGetProperty("replacements", out JsonElement replacements))
                    {
                        foreach (JsonElement repl in replacements.EnumerateArray())
                            Suggestions.Add(repl.GetProperty("value").GetString());
                    }

                    string ContextText = context.GetProperty("text").GetString();
                    int Offset = Match.GetProperty("offset").GetInt32();
                    int Length = Match.GetProperty("length").GetInt32();

                    Results.Add((ContextText.Substring(Offset, Length), Suggestions));
                }
            }
            return Results;
        }

        public static string GetMiniSearchType(string Text)
        {
            if (Text.StartsWith("search:", StringComparison.Ordinal))
                return "S";
            else if (Text.StartsWith("domain:", StringComparison.Ordinal))
                return "W";
            else if (Utils.IsProgramUrl(Text))
                return "P";
            else if (Utils.IsCode(Text))
                return "C";
            else if (Text.StartsWith("file:///", StringComparison.Ordinal))
                return "F";
            else if (Utils.IsUrl(Text))
                return "W";
            return "S";
        }

        public static string GetSmartType(string Text)
        {
            if (Regex.IsMatch(Text, @"^[\d\s\.\+\-\*/%\(\)]+$"))
                return "Math";
            else if (Text.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                return "Define";
            else if (Text.StartsWith("weather ", StringComparison.OrdinalIgnoreCase))
                return "Weather";
            else if (Text.StartsWith("translate ", StringComparison.OrdinalIgnoreCase))
                return "Translate";
            else if (Regex.IsMatch(Text, @"\b\d+(?:\.\d+)?\s+[a-zA-Z]{3}\s+(?:to|in)\s+[a-zA-Z]{3}\b"))
                return "Currency";
            return "None";
        }

        public async Task<OmniSuggestion> GenerateSmartSuggestion(string Text, string Type, CancellationToken Token)
        {
            OmniSuggestion Suggestion = new OmniSuggestion { Text = Text };

            Suggestion.Icon = "\xE721";
            switch (Type)
            {
                case "Math":
                    try
                    {
                        Suggestion.Result = $"= {new DataTable().Compute(Text, null)?.ToString()}";
                        Suggestion.Icon = "\uE8EF";
                    }
                    catch { }
                    break;
                case "Define":
                    try
                    {
                        string Json = await MiniHttpClient.GetStringAsync($"https://api.dictionaryapi.dev/api/v2/entries/en/{Text.Substring(7).Trim()}");
                        JsonDocument _JsonDocument = JsonDocument.Parse(Json);
                        string Result = _JsonDocument.RootElement[0].GetProperty("meanings")[0].GetProperty("definitions")[0].GetProperty("definition").GetString();
                        Suggestion.Result = $"- {Result}";
                        Suggestion.Icon = "\uE82D";
                    }
                    catch { }
                    break;
                case "Translate":
                    Match TranslateMatch = Regex.Match(Text, @"^translate\s+(?<Phrase>.+?)\s+to\s+(?<Lang>.+)", RegexOptions.IgnoreCase);
                    if (TranslateMatch.Success)
                    {
                        Suggestion.Result = "- Unavailable";
                        Suggestion.Icon = "\uE8C1";
                        string LanguageInput = TranslateMatch.Groups["Lang"].Value.Trim().ToLowerInvariant();
                        string LanguageCode = (LanguageInput.Length == 2) ? LanguageInput : AllLocales.FirstOrDefault(x => x.Value.Contains(LanguageInput, StringComparison.OrdinalIgnoreCase)).Key;
                        if (!string.IsNullOrEmpty(LanguageCode))
                        {
                            try
                            {
                                string Response = await MiniHttpClient.GetStringAsync($"https://translate.googleapis.com/translate_a/single?client=gtx&dt=t&sl=auto&tl={LanguageCode}&q={Uri.EscapeDataString(TranslateMatch.Groups["Phrase"].Value.Trim())}");
                                Suggestion.Result = $"- {JsonDocument.Parse(Response).RootElement[0][0][0].GetString()}";
                            }
                            catch { }
                        }
                    }
                    break;
                case "Currency":
                    Match CurrencyMatch = Regex.Match(Text, @"(?<Amount>\d+(\.\d+)?)\s+(?<From>[A-Za-z]{3})\s+(?:to|in)\s+(?<To>[A-Za-z]{3})");
                    if (CurrencyMatch.Success)
                    {
                        Suggestion.Result = "- Unavailable";
                        Suggestion.Icon = "\ue8ee";
                        string Amount = CurrencyMatch.Groups["Amount"].Value;
                        string From = CurrencyMatch.Groups["From"].Value.ToUpper();
                        string To = CurrencyMatch.Groups["To"].Value.ToUpper();
                        try
                        {
                            string Response = await MiniHttpClient.GetStringAsync($"https://api.frankfurter.app/latest?amount={Amount}&from={From}&to={To}");

                            using JsonDocument _JsonDocument = JsonDocument.Parse(Response);
                            JsonElement Root = _JsonDocument.RootElement;

                            if (Root.TryGetProperty("rates", out JsonElement Rates) && Rates.TryGetProperty(To, out JsonElement Output))
                                Suggestion.Result = $"- {Amount} {From} ≈ {Output.GetDouble():0.00} {To}";
                        }
                        catch { }
                    }
                    break;

                case "Weather":
                    Suggestion.Icon = "\uE9CA";
                    string Location = Regex.Replace(Text, @"^weather(\s+in)?\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
                    try
                    {
                        string WeatherEndpoint = $"https://api.openweathermap.org/data/2.5/weather?lang=en&q={Location}&appid={SECRETS.WEATHER_API_KEY}&units=metric";
                        using (HttpClient Client = new HttpClient())
                        {
                            HttpResponseMessage Response = Client.GetAsync(WeatherEndpoint).Result;
                            if (Response.IsSuccessStatusCode)
                            {
                                JsonElement Data = JsonDocument.Parse(Response.Content.ReadAsStringAsync().Result).RootElement;
                                double Temperature = Data.GetProperty("main").GetProperty("temp").GetDouble();
                                string Description = Utils.CapitalizeAllFirstCharacters(Data.GetProperty("weather")[0].GetProperty("description").GetString());

                                Suggestion.Result = $"{Temperature} °C | {Description}";
                            }
                            else
                                Suggestion.Result = $"- No data";
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { Suggestion.Result = "- Unavailable"; }
                    break;
            }

            return Suggestion;
        }

        public bool Background = false;

        public BitmapFrame Icon;
        public static HttpClient MiniHttpClient = new HttpClient();
        public static Random MiniRandom = new Random();
        public static QREncoder MiniQREncoder = new QREncoder();

        //public List<IntPtr> WebView2DevTools = new List<IntPtr>();

        private void InitializeApp()
        {
            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";
            string CommandLineUrl = string.Empty;
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--user=", StringComparison.Ordinal))
                {
                    Username = Flag.Replace("--user=", string.Empty).Replace(" ", "-");
                    if (Utils.IsEmptyOrWhiteSpace(Username))
                        Username = "Default";
                    else if (Username != "Default")
                        AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30-" + Username + "}";
                }
                else if (Flag == "--background")
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
                    if (Flag.StartsWith("--", StringComparison.Ordinal))
                        continue;
                    CommandLineUrl = Flag;
                }
            }
            DllUtils.SetCurrentProcessExplicitAppUserModelID(AppUserModelID);

            _Mutex = new Mutex(true, AppUserModelID);
            if (string.IsNullOrEmpty(CommandLineUrl))
            {
                if (!_Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                    if (OtherInstance != null)
                        MessageHelper.SendDataMessage(OtherInstance, "Start<|>" + Username);
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
            MiniHttpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xml,*/*");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            Dispatcher.UnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Set Google API keys. See http://www.chromium.org/developers/how-tos/api-keys
            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/google_api_keys.h
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);

            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            ExtensionsPath = Path.Combine(UserApplicationDataPath, "User Data", "Default", "Extensions");
            ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            //CdnPath = Path.Combine(ResourcesPath, "cdn");

            LocaleNames = AllLocales.Select(i => i.Value).ToList();

            InitializeSaves();

            if (Username != "Default")
            {
                string IconPath = Path.Combine(UserApplicationDataPath, "Icon.png");
                if (!File.Exists(IconPath))
                    Utils.SaveImage(GenerateProfileIcon("pack://application:,,,/Resources/SLBr.ico", Username.Substring(0, 1).ToUpper()), IconPath);
                Icon = BitmapFrame.Create(new Uri(IconPath, UriKind.Absolute));
            }

            FavouriteColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA2A55");
            SLBrColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#0092FF");
            RedColor = new SolidColorBrush(Colors.Red);
            CornflowerBlueColor = new SolidColorBrush(Colors.CornflowerBlue);
            NavajoWhiteColor = new SolidColorBrush(Colors.NavajoWhite);
            LimeGreenColor = new SolidColorBrush(Colors.LimeGreen);
            GreenColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#3AE872");
            OrangeColor = new SolidColorBrush(Colors.Orange);
            IconFont = (FontFamily)Application.Current.Resources["IconFontFamily"];
            SLBrFont = new FontFamily(new Uri("pack://application:,,,/SLBr;component/"), "./Fonts/#SLBr Icons");

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
                                ApplicationRegistry.Close();

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
            AppInitialized = true;
            if (!Background)
                ContinueBackgroundInitialization();
            /*ChromiumBookmarkManager.Bookmarks Bookmarks = ChromiumBookmarkManager.Import(@"User Data\Profile 1\Bookmarks");
            foreach (var bookmark in Bookmarks.roots.bookmark_bar.children)
            {
                if (bookmark.children != null)
                    continue;
                if (bookmark.name == "")
                    continue;
                MessageBox.Show(bookmark.name + "|" + bookmark.url);
            }*/
        }

        public Theme GenerateTheme(Color BaseColor, string Name = "Temp")
        {
            double a = 1 - (0.299 * BaseColor.R + 0.587 * BaseColor.G + 0.114 * BaseColor.B) / 255;
            Theme SiteTheme = null;
            if (a < 0.4)
            {
                SiteTheme = new Theme(Name, Themes[0]);
                SiteTheme.FontColor = Colors.Black;
                SiteTheme.DarkTitleBar = false;
                SiteTheme.DarkWebPage = false;
            }
            else if (a < 0.7)
            {
                SiteTheme = new Theme(Name, Themes[0]);
                SiteTheme.FontColor = Colors.White;
                SiteTheme.DarkTitleBar = false;
                SiteTheme.DarkWebPage = false;
                SiteTheme.SecondaryColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Min(255, BaseColor.R * 0.95f),
                    (byte)Math.Min(255, BaseColor.G * 0.95f),
                    (byte)Math.Min(255, BaseColor.B * 0.95f));
                SiteTheme.BorderColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Min(255, BaseColor.R * 0.90f),
                    (byte)Math.Min(255, BaseColor.G * 0.90f),
                    (byte)Math.Min(255, BaseColor.B * 0.90f));
                SiteTheme.GrayColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Min(255, BaseColor.R * 0.75f),
                    (byte)Math.Min(255, BaseColor.G * 0.75f),
                    (byte)Math.Min(255, BaseColor.B * 0.75f));
            }
            else
            {
                SiteTheme = new Theme(Name, Themes[1]);
                SiteTheme.FontColor = Colors.White;
                SiteTheme.DarkTitleBar = true;
                SiteTheme.DarkWebPage = true;
                SiteTheme.SecondaryColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Max(0, BaseColor.R * 1.25f),
                    (byte)Math.Max(0, BaseColor.G * 1.25f),
                    (byte)Math.Max(0, BaseColor.B * 1.25f));
                SiteTheme.BorderColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Max(0, BaseColor.R * 1.35f),
                    (byte)Math.Max(0, BaseColor.G * 1.35f),
                    (byte)Math.Max(0, BaseColor.B * 1.35f));
                SiteTheme.GrayColor = Color.FromArgb(BaseColor.A,
                    (byte)Math.Max(0, BaseColor.R * 1.95f),
                    (byte)Math.Max(0, BaseColor.G * 1.95f),
                    (byte)Math.Max(0, BaseColor.B * 1.95f));
            }
            SiteTheme.PrimaryColor = BaseColor;
            return SiteTheme;
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
            if (Utils.IsInternetAvailable() && bool.Parse(GlobalSave.Get("CheckUpdate")))
                CheckUpdate();
            if (Environment.IsPrivilegedProcess)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Warning", "Elevated Privileges Detected", "SLBr is running with administrator privileges, which may pose security risks. It is recommended to run SLBr without elevated rights.", "\ue7ba");
                InfoWindow.Topmost = true;
                InfoWindow.ShowDialog();
            }
        }

        public void CheckUpdate()
        {
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", UserAgentGenerator.BuildChromeBrand());
                    _WebClient.Headers.Add("Accept", "*/*");
                    string NewVersion = JsonDocument.Parse(_WebClient.DownloadString("https://api.github.com/repos/slt-world/slbr/releases/latest")).RootElement.GetProperty("tag_name").ToString();
                    if (!NewVersion.StartsWith(ReleaseVersion, StringComparison.Ordinal))
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
                        InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", "Update Available", "A newer version of SLBr is ready for download.", "\ue895", "Download", "Dismiss");
                        InfoWindow.Topmost = true;
                        if (InfoWindow.ShowDialog() == true)
                            Update();
                    }
                    else
                        UpdateAvailable = ReleaseVersion;
                }
                catch { }
            }
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

        public static async Task DiscordWebhookSendInfo(string Content)
        {
            var Payload = new
            {
                content = Content,
                username = "SLBr Diagnostics"
            };

            await MiniHttpClient.PostAsync(SECRETS.DISCORD_WEBHOOK, new StringContent(JsonSerializer.Serialize(Payload), Encoding.UTF8, "application/json"));
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportError(e.Exception);
#if !DEBUG
            Save();
            e.SetObserved();
#endif
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception _Exception = e.ExceptionObject as Exception;
            ReportError(_Exception);
#if !DEBUG
            Save();
#endif
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportError(e.Exception);
#if !DEBUG
            Save();
            e.Handled = true;
#endif
        }

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
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(Report);
#if DEBUG
            MessageBox.Show(Report);
#endif
        }

        private static string FormatInnerException(Exception Error)
        {
            var Builder = new StringBuilder();
            int Depth = 0;

            while (Error.InnerException != null)
            {
                Error = Error.InnerException;
                Builder.AppendLine($"{new string(' ', Depth * 2)}--> {Error.GetType().FullName}: {Error.Message}");
                Depth++;
            }

            return Builder.Length == 0 ? "None" : Builder.ToString();
        }

        const string ReportExceptionText = @"**Automatic Report**
> - Version: `{0}`
> - CEF: `{1}`
> - WebView2: `{2}`
> - Message: ```{3}```
> - Source: `{4} `
> - Target Site: `{5} `

Stack Trace: ```{6} ```

Inner Exception: ```{7} ```";
        public int TrackersBlocked;
        public int AdsBlocked;

        public int AdBlock;
        public bool AMP;

        //https://chromium-review.googlesource.com/c/chromium/src/+/1265506
        public bool NeverSlowMode;

        public bool SkipAds;
        public bool ExternalFonts;
        public bool SmartDarkMode;

        public void SetSmartDarkMode(bool _SmartDarkMode)
        {
            GlobalSave.Set("SmartDarkMode", _SmartDarkMode.ToString());
            SmartDarkMode = _SmartDarkMode;
        }

        public void SetExternalFonts(bool _ExternalFonts)
        {
            GlobalSave.Set("ExternalFonts", _ExternalFonts.ToString());
            ExternalFonts = _ExternalFonts;
        }

        public WebRiskHandler.SecurityService WebRiskService;

        public void SetWebRiskService(int Service)
        {
            GlobalSave.Set("WebRiskService", Service);
            WebRiskService = (WebRiskHandler.SecurityService)Service;
        }
        public void SetYouTube(bool _SkipAds)
        {
            GlobalSave.Set("SkipAds", _SkipAds.ToString());
            SkipAds = _SkipAds;
        }
        public void SetNeverSlowMode(bool Boolean)
        {
            GlobalSave.Set("NeverSlowMode", Boolean.ToString());
            NeverSlowMode = Boolean;
        }
        public void SetAdBlock(int Type)
        {
            GlobalSave.Set("AdBlock", Type);
            AdBlock = Type;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                    BrowserView.ToggleEfficientAdBlock(AdBlock == 2);
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
                GlobalSave.Set("TabUnloadingTime", Time);
            foreach (MainWindow _Window in AllWindows)
                _Window.UpdateUnloadTimer();
        }
        public void OpenFileExplorer(string Url)
        {
            Process.Start(new ProcessStartInfo { Arguments = $"/select, \"{Url}\"", FileName = "explorer.exe" });
        }

        public void SwitchUserPopup()
        {
            DynamicDialogWindow _DynamicDialogWindow = new DynamicDialogWindow("Prompt", "Switch Profile", new List<InputField> { new InputField { Name = "Enter profile username to switch to:", IsRequired = true, Type = DialogInputType.Text, Value = "" } }, "\xE77B");
            _DynamicDialogWindow.Topmost = true;
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                string Input = _DynamicDialogWindow.InputFields[0].Value.Trim();
                if (Input != Username)
                    Process.Start(new ProcessStartInfo() { FileName = ExecutablePath, Arguments = $"--user={Input}" });
            }
        }

        public void SaveOpenSearch(string Name, string Url)
        {
            try
            {
                if (SearchEngines.Find(x => x.Name == Name) != null)
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

        public ObservableCollection<ActionStorage> Languages = new ObservableCollection<ActionStorage>();
        public ActionStorage Locale;

        public Dictionary<string, string> AllLocales = new Dictionary<string, string>
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

        public string GetLocaleIcon(string ISO)
        {
            if (ISO.StartsWith("zh-TW", StringComparison.Ordinal))
                return "\xe981";
            else if (ISO.StartsWith("zh", StringComparison.Ordinal))
                return "\xE982";
            else if (ISO.StartsWith("ja", StringComparison.Ordinal))
                return "\xe985";
            else if (ISO.StartsWith("ko", StringComparison.Ordinal))
                return "\xe97d";
            else if (ISO.StartsWith("en", StringComparison.Ordinal))
                return "\xe97e";
            return "\xf2b7";
        }

        public bool LiteMode;
        public bool HighPerformanceMode;

        private void InitializeSaves()
        {
            GlobalSave = new Saving("Save.bin", UserApplicationDataPath);
            FavouritesSave = new Saving("Favourites.bin", UserApplicationDataPath);
            SearchSave = new Saving("Search.bin", UserApplicationDataPath);
            StatisticsSave = new Saving("Statistics.bin", UserApplicationDataPath);
            LanguagesSave = new Saving("Languages.bin", UserApplicationDataPath);
            AllowListSave = new Saving("AllowList.bin", UserApplicationDataPath);

            if (!GlobalSave.Has("StartupBoost"))
            {
                StartupManager.EnableStartup();
                GlobalSave.Set("StartupBoost", true.ToString());
            }

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

            int SearchCount = SearchSave.GetInt("Count", 0);
            if (SearchCount != 0)
            {
                for (int i = 0; i < SearchCount; i++)
                {
                    SearchProvider _SearchProvider = new SearchProvider();
                    var Values = SearchSave.Get($"{i}").Split("<#>");
                    if (Values.Length != 3)
                    {
                        DefaultSearchProvider = new SearchProvider { Name = "Google", Host = "google.com", SearchUrl = "https://google.com/search?q={0}", SuggestUrl = "https://suggestqueries.google.com/complete/search?client=chrome&output=toolbar&q={0}" };
                        SearchEngines = new List<SearchProvider>
                        {
                            DefaultSearchProvider,
                            new SearchProvider { Name = "Bing", Host = "bing.com", SearchUrl = "https://bing.com/search?q={0}", SuggestUrl = "https://api.bing.com/osjson.aspx?query={0}" },
                            new SearchProvider { Name = "Ecosia", Host = "ecosia.org", SearchUrl = "https://www.ecosia.org/search?q={0}", SuggestUrl = "https://ac.ecosia.org/autocomplete?q={0}&type=list" },
                            new SearchProvider { Name = "Brave Search", Host = "search.brave.com", SearchUrl = "https://search.brave.com/search?q={0}", SuggestUrl = "https://search.brave.com/api/suggest?q={0}" },
                            new SearchProvider { Name = "DuckDuckGo", Host = "duckduckgo.com", SearchUrl = "https://duckduckgo.com/?q={0}", SuggestUrl = "http://duckduckgo.com/ac/?type=list&q={0}" },
                            new SearchProvider { Name = "Yandex", Host = "yandex.com", SearchUrl = "https://yandex.com/search/?text={0}", SuggestUrl = "https://suggest.yandex.com/suggest-ff.cgi?part={0}" },
                            new SearchProvider { Name = "Yahoo Search", Host = "search.yahoo.com", SearchUrl = "https://search.yahoo.com/search?p={0}", SuggestUrl = "https://ff.search.yahoo.com/gossip?output=fxjson&command={0}" },
                        };
                        break;
                    }
                    else
                    {
                        _SearchProvider.Name = Values[0];
                        _SearchProvider.Host = Utils.FastHost(Values[1]);
                        _SearchProvider.SearchUrl = Values[1];
                        _SearchProvider.SuggestUrl = Values[2];
                    }
                    SearchEngines.Add(_SearchProvider);
                }
            }
            else
            {
                DefaultSearchProvider = new SearchProvider { Name = "Google", Host = "google.com", SearchUrl = "https://google.com/search?q={0}", SuggestUrl = "https://suggestqueries.google.com/complete/search?client=chrome&output=toolbar&q={0}" };
                SearchEngines = new List<SearchProvider>
                {
                    DefaultSearchProvider,
                    new SearchProvider { Name = "Bing", Host = "bing.com", SearchUrl = "https://bing.com/search?q={0}", SuggestUrl = "https://api.bing.com/osjson.aspx?query={0}" },
                    new SearchProvider { Name = "Ecosia", Host = "ecosia.org", SearchUrl = "https://www.ecosia.org/search?q={0}", SuggestUrl = "https://ac.ecosia.org/autocomplete?type=list&q={0}" },
                    new SearchProvider { Name = "Brave Search", Host = "search.brave.com", SearchUrl = "https://search.brave.com/search?q={0}", SuggestUrl = "https://search.brave.com/api/suggest?q={0}" },
                    new SearchProvider { Name = "DuckDuckGo", Host = "duckduckgo.com", SearchUrl = "https://duckduckgo.com/?q={0}", SuggestUrl = "http://duckduckgo.com/ac/?type=list&q={0}" },
                    new SearchProvider { Name = "Yandex", Host = "yandex.com", SearchUrl = "https://yandex.com/search/?text={0}", SuggestUrl = "https://suggest.yandex.com/suggest-ff.cgi?part={0}" },
                    new SearchProvider { Name = "Yahoo Search", Host = "search.yahoo.com", SearchUrl = "https://search.yahoo.com/search?p={0}", SuggestUrl = "https://ff.search.yahoo.com/gossip?output=fxjson&command={0}" },
                };
            }
            string SearchEngineName = GlobalSave.Get("SearchEngine", "Google");
            DefaultSearchProvider = SearchEngines.Find(i => i.Name == SearchEngineName);
            if (DefaultSearchProvider == null)
                DefaultSearchProvider = SearchEngines.Find(i => i.SearchUrl.Contains("google.com", StringComparison.Ordinal));

            int AllowListCount = AllowListSave.GetInt("Count", -1);
            if (AllowListCount != -1)
            {
                for (int i = 0; i < AllowListCount; i++)
                    AdBlockAllowList.Add(AllowListSave.Get($"{i}"));
            }
            else
            {
                AdBlockAllowList.Add("ecosia.org");
                AdBlockAllowList.Add("youtube.com");
            }

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

            SetMobileView(bool.Parse(GlobalSave.Get("MobileView", false.ToString())));

            if (!GlobalSave.Has("WarnCodec"))
                GlobalSave.Set("WarnCodec", true);
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
            TrackersBlocked = StatisticsSave.GetInt("BlockedTrackers", 0);
            AdsBlocked = StatisticsSave.GetInt("BlockedAds", 0);

            if (!GlobalSave.Has("TabUnloading"))
                GlobalSave.Set("TabUnloading", true.ToString());
            if (!GlobalSave.Has("ShowUnloadProgress"))
                GlobalSave.Set("ShowUnloadProgress", false.ToString());
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

            if (!GlobalSave.Has("SendDiagnostics"))
                GlobalSave.Set("SendDiagnostics", true);
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
            if (!GlobalSave.Has("SmoothScroll"))
                GlobalSave.Set("SmoothScroll", true);

            if (!GlobalSave.Has("BrowserHardwareAcceleration"))
                GlobalSave.Set("BrowserHardwareAcceleration", (RenderCapability.Tier >> 16) != 0);
            if (!GlobalSave.Has("ExperimentalFeatures"))
                GlobalSave.Set("ExperimentalFeatures", false);
            if (!GlobalSave.Has("Performance"))
                GlobalSave.Set("Performance", 1);
            LiteMode = GlobalSave.GetInt("Performance") == 0;
            HighPerformanceMode = GlobalSave.GetInt("Performance") == 2;

            if (!GlobalSave.Has("JIT"))
                GlobalSave.Set("JIT", true);
            if (!GlobalSave.Has("PDF"))
                GlobalSave.Set("PDF", true);

            if (!GlobalSave.Has("HomepageBackground"))
                GlobalSave.Set("HomepageBackground", 0);
            if (!GlobalSave.Has("BingBackground"))
                GlobalSave.Set("BingBackground", 0);
            if (!GlobalSave.Has("CustomBackgroundQuery"))
                GlobalSave.Set("CustomBackgroundQuery", string.Empty);
            if (!GlobalSave.Has("CustomBackgroundImage"))
                GlobalSave.Set("CustomBackgroundImage", string.Empty);

            if (!GlobalSave.Has("ImageSearch"))
                GlobalSave.Set("ImageSearch", 0);
            if (!GlobalSave.Has("TranslationProvider"))
                GlobalSave.Set("TranslationProvider", 0);

            if (!GlobalSave.Has("WebEngine"))
                GlobalSave.Set("WebEngine", 0);

            if (!GlobalSave.Has("AntiTamper"))
                GlobalSave.Set("AntiTamper", false);
            if (!GlobalSave.Has("AntiInspectDetect"))
                GlobalSave.Set("AntiInspectDetect", true);
            if (!GlobalSave.Has("AntiFullscreen"))
                GlobalSave.Set("AntiFullscreen", false);
            if (!GlobalSave.Has("BypassSiteMenu"))
                GlobalSave.Set("BypassSiteMenu", false);
            if (!GlobalSave.Has("TextSelection"))
                GlobalSave.Set("TextSelection", true);
            if (!GlobalSave.Has("RemoveFilter"))
                GlobalSave.Set("RemoveFilter", true);
            if (!GlobalSave.Has("RemoveOverlay"))
                GlobalSave.Set("RemoveOverlay", false);
            if (!GlobalSave.Has("ForceLazy"))
                GlobalSave.Set("ForceLazy", false);
            if (!GlobalSave.Has("FullscreenPopup"))
                GlobalSave.Set("FullscreenPopup", true);
            GlobalSave.GetInt("FaviconService", 0);

            SetWebRiskService(GlobalSave.GetInt("WebRiskService", 1));

            try
            {
                using (var Key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                    Themes.Add(new Theme("Auto", (Key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
            }
            catch
            {
                Themes.Add(new Theme("Auto", Themes[1]));
            }
            Theme CustomTheme = GenerateTheme(Utils.HexToColor(GlobalSave.Get("CustomTheme", Utils.ColorToHex(Colors.Red))), "Custom");
            Themes.Add(CustomTheme);
        }
        private void InitializeUISaves(string CommandLineUrl = "")
        {
            SetSmartDarkMode(bool.Parse(GlobalSave.Get("SmartDarkMode", false.ToString())));
            SetExternalFonts(bool.Parse(GlobalSave.Get("ExternalFonts", true.ToString())));
            SetYouTube(bool.Parse(GlobalSave.Get("SkipAds", false.ToString())));
            SetNeverSlowMode(bool.Parse(GlobalSave.Get("NeverSlowMode", false.ToString())));
            SetAdBlock(GlobalSave.GetInt("AdBlock", 1));
            SetAMP(bool.Parse(GlobalSave.Get("AMP", false.ToString())));
            SetRenderMode(GlobalSave.GetInt("RenderMode", (RenderCapability.Tier >> 16) == 0 ? 1 : 0));
            
            for (int i = 0; i < FavouritesSave.GetInt("Count", 0); i++)
            {
                string[] Value = FavouritesSave.Get(i.ToString(), true);
                Favourites.Add(new ActionStorage(Value[1], $"4<,>{Value[0]}", Value[0]));
            }
            Favourites.CollectionChanged += Favourites_CollectionChanged;
            SetAppearance(GetTheme(GlobalSave.Get("Theme", "Auto")), GlobalSave.GetInt("TabAlignment", 0), bool.Parse(GlobalSave.Get("CompactTab", true.ToString())), bool.Parse(GlobalSave.Get("HomeButton", true.ToString())), bool.Parse(GlobalSave.Get("TranslateButton", true.ToString())), bool.Parse(GlobalSave.Get("ReaderButton", true.ToString())), GlobalSave.GetInt("ExtensionButton", 0), GlobalSave.GetInt("FavouritesBar", 0), bool.Parse(GlobalSave.Get("QRButton", true.ToString())), bool.Parse(GlobalSave.Get("WebEngineButton", true.ToString())));
            if (bool.Parse(GlobalSave.Get("RestoreTabs", false.ToString())))
            {
                foreach (Saving TabsSave in WindowsSaves)
                {
                    MainWindow _Window = new MainWindow();
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
                        for (int i = 0; i < TabCount; i++)
                        {
                            string Url = TabsSave.Get(i.ToString(), "slbr://newtab");
                            if (Utils.IsEmptyOrWhiteSpace(Url))
                                Url = "slbr://newtab";
                            _Window.NewTab(Url, false, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                        }
                        if (GlobalSave.GetInt("TabAlignment", 0) == 1)
                            _Window.TabsUI.SelectedIndex = TabsSave.GetInt("Selected", 0) + 1;
                        else
                            _Window.TabsUI.SelectedIndex = TabsSave.GetInt("Selected", 0);
                    }
                    else
                        _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
                    _Window.TabsUI.Visibility = Visibility.Visible;
                }
            }
            if (!string.IsNullOrEmpty(CommandLineUrl))
                CurrentFocusedWindow().NewTab(CommandLineUrl, true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
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
            CurrentWindow.Fullscreen(ForceClose ? false : !CurrentWindow.IsFullscreen);
        }
        public void DevTools(string Id = "") =>
            CurrentFocusedWindow().DevTools(Id);
        public void Find(string Text = "") =>
            CurrentFocusedWindow().Find(Text);
        public void Screenshot() =>
            CurrentFocusedWindow().Screenshot();
        public void NewWindow()
        {
            MainWindow _Window = new MainWindow();
            _Window.Show();
            _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
            _Window.TabsUI.Visibility = Visibility.Visible;
        }

        public static FastHashSet<string> FailedScripts = new();
        public static readonly string[] BlockedAdPatterns = ["ads.google.com", "*.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "*.googleadservices.com", "adservice.google.com",
                        "googleadservices.com", "doubleclick.net", "google-analytics.com",
                        "syndicatedsearch.goog", "*.doubleclick.net", "*.g.doubleclick.net",
            "gads.pubmatic.com", "ads.pubmatic.com", "ogads-pa.clients6.google.com",
            "ads.facebook.com", "an.facebook.com",
            "cdn.snigelweb.com", "cdn.connectad.io",
            "pool.admedo.com", "c.pub.network",
            "media.ethicalads.io",
            "app-measurement.com",
            "ad.youtube.com", "ads.youtube.com", "youtube.cleverads.vn",
            "prod.di.api.cnn.io", "get.s-onetag.com", "assets.bounceexchange.com", "gn-web-assets.api.bbc.com", "pub.doubleverify.com",
            "events.reddit.com",
            "ads.tiktok.com", "ads-sg.tiktok.com", "ads.adthrive.com", "ads-api.tiktok.com", "business-api.tiktok.com",
            "ads.reddit.com", "d.reddit.com", "rereddit.com", "events.redditmedia.com",
            "ads-twitter.com", "static.ads-twitter.com", "ads-api.twitter.com", "advertising.twitter.com",
            "ads.pinterest.com", "ads-dev.pinterest.com",
            "adtago.s3.amazonaws.com", "advice-ads.s3.amazonaws.com", "advertising-api-eu.amazon.com", "amazonclix.com",
            "ads.linkedin.com",
            "*.dianomi.com",
            "*.media.net",
            "media.fastclick.net", "cdn.fastclick.net",
            "global.adserver.yahoo.com", "advertising.yahoo.com", "ads.yahoo.com", "ads.yap.yahoo.com", "adserver.yahoo.com", "partnerads.ysm.yahoo.com", "adtech.yahooinc.com", "advertising.yahooinc.co",
            "api-adservices.apple.com", "advertising.apple.com", "tr.iadsdk.apple.com",
            "yandexadexchange.net", "adsdk.yandex.ru", "advertising.yandex.ru", "an.yandex.ru",

            "secure-ds.serving-sys.com", "*.innovid.com",
            "*.outbrain.com.",
            "*.adcolony.com",
            "adm.hotjar.com",
            "files.adform.net",
            "static.adsafeprotected.com", "pixel.adsafeprotected.com",
            "t.adx.opera.com",
            "asadcdn.com",
            "ads.yieldmo.com", "ads.servenobid.com", "e3.adpushup.com", "c1.adform.net",
            "ib.adnxs.com",
            "*.smartadserver.com", "ad.a-ads.com",
            "cdn.carbonads.com", "px.ads.linkedin.com",
            "*.adsrvr.org",
            "scdn.cxense.com",
            "acdn.adnxs.com",
            "js.adscale.de",
            "js.hsadspixel.net",
            "ad.mopub.com",
            "*.juicyads.com",
            "a.realsrv.com", "mc.yandex.ru", "a.vdo.ai", "adfox.yandex.ru", "adfstat.yandex.ru", "offerwall.yandex.net",
            "ads.msn.com", "adnxs.com", "adnexus.net", "bingads.microsoft.com",
            "dt.adsafeprotected.com",
            "amazonaax.com", "*.amazon-adsystem.com",
            "ads.betweendigital.com", "rtb.adpone.com", "ads.themoneytizer.com", "*.criteo.com",

            "*.rubiconproject.com",

            "*.ad.gt", "powerad.ai", "hb.brainlyads.com", "pixel.quantserve.com", "ads.anura.io", "static.getclicky.com",
            "ad.turn.com", "rtb.mfadsrvr.com", "ad.mrtnsvr.com", "s.ad.smaato.net",
            "adpush.technoratimedia.com", "pixel.tapad.com", "secure.adnxs.com", "px.adhigh.net",
            "epnt.ebay.com", "*.moatads.com", "s.pubmine.com", "px.ads.linkedin.com", "p.adsymptotic.com",
            "btloader.com", "ad-delivery.net",
            "services.vlitag.com", "tag.vlitag.com", "assets.vlitag.com",
            "*.adserver.snapads.com",
            "cdn.adsafeprotected.com",
            "rp.liadm.com",

            "adx.adform.net",
            "prebid.a-mo.net",
            "a.pub.network",
            "widgets.outbrain.com",
            "hb.adscale.de", "bitcasino.io",

            "h.seznam.cz", "d.seznam.cz", "ssp.seznam.cz",
            "cdn.performax.cz", "dale.performax.cz", "chip.performax.cz","ssl-google-analytics.l.google.com", "www-google-analytics.l.google.com", "www-googletagmanager.l.google.com", "analytic-google.com", "*.google-analytics.com",
            "analytics.google.com", "*.googleanalytics.com", "*.admobclick.com", "firebaselogging-pa.googleapis.com",
            "sp.ecosia.org",
            "analytics.facebook.com", "pixel.facebook.com",
            "analytics.tiktok.com", "analytics-sg.tiktok.com", "log.byteoversea.com",
            "analytics.pinterest.com", "widgets.pinterest.com", "log.pinterest.com", "trk.pinterest.com",
            "analytics.pointdrive.linkedin.com",
            "analyticsengine.s3.amazonaws.com", "affiliationjs.s3.amazonaws.com", "analytics.s3.amazonaws.com",
            "analytics.mobile.yandex.net", "appmetrica.yandex.com", "extmaps-api.yandex.net", "appmetrica.yandex.ru", "metrika.yandex.ru",
            "analytics.yahoo.com", "ups.analytics.yahoo.com", "analytics.query.yahoo.com", "log.fc.yahoo.com", "geo.yahoo.com", "udc.yahoo.com", "udcm.yahoo.com", "gemini.yahoo.com",
            "metrics.apple.com",
            "*.bugsnag.com",
            "*.sentry-cdn.com", "app.getsentry.com",
            "stats.gc.apple.com", "iadsdk.apple.com",
            "collector.github.com",
            "cloudflareinsights.com",
            "*.hotjar.com",
            "hotjar-analytics.com",
            "mouseflow.com", "*.mouseflow.com",
            "stats.wp.com",
            "*.datatrics.com",
            "*.ero-advertising.com",
            "analytics.archive.org",
            "*.freshmarketer.com", "freshmarketer.com",
            "openbid.pubmatic.com", "prebid.media.net", "hbopenbid.pubmatic.com",
            "collector.cdp.cnn.com", "smetrics.cnn.com", "mybbc-analytics.files.bbci.co.uk", "a1.api.bbc.co.uk", "xproxy.api.bbc.com",
            "*.dotmetrics.net", "scripts.webcontentassessor.com",
            "collector.brandmetrics.com", "Builder.scorecardresearch.com",
            "queue.simpleanalyticscdn.com",
            "cdn.permutive.com", "api.permutive.com",

            "luckyorange.com", "*.luckyorange.com", "*.luckyorange.net",

            "securemetrics.apple.com", "supportmetrics.apple.com", "metrics.icloud.com",

            "tr.snapchat.com", "sc-analytics.appspot.com", "app-analytics.snapchat.com",
            "crashlogs.whatsapp.net",
            "metrics.mzstatic.com",

            "click.a-ads.com",
            "static.criteo.net",
            "www.clarity.ms",
            "u.clarity.ms",

            "s.cdn.turner.com",
            "logx.optimizely.com",
            "signal-metrics-collector-beta.s-onetag.com",
            "connect-metrics-collector.s-onetag.com",
            "ping.chartbeat.net",
            "logs.browser-intake-datadoghq.com",
            "onsiterecs.api.boomtrain.com",

            "b.6sc.co",
            "api.bounceexchange.com", "events.bouncex.net",
            "assets.adobedtm.com",
            "static.chartbeat.com",
            "dsum-sec.casalemedia.com",

            "aa.agkn.com",
            "material.anonymised.io",
            "static.anonymised.io",
            "*.tinypass.com",
            "dw-usr.userreport.com",
            "capture-api.reachlocalservices.com",
            "discovery.evvnt.com",
            "mab.chartbeat.com",
            "sync.sharethis.com",
            "bcp.crwdcntrl.net",

            "*.doubleverify.com", "onetag-sys.com",
            "id5-sync.com", "bttrack.com", "idsync.rlcdn.com", "u.openx.net", "sync-t1.taboola.com", "x.bidswitch.net", "rtd-tm.everesttech.net", "usermatch.krxd.net", "visitor.omnitagjs.com", "ping.chartbeat.net",
            "sync.outbrain.com",
            "collect.mopinion.com", "pb-server.ezoic.com",
            "demand.trafficroots.com", "sync.srv.stackadapt.com", "sync.ipredictive.com", "analytics.vdo.ai", "tag-api-2-1.ccgateway.net", "sync.search.spotxchange.com",
            "reporting.powerad.ai", "monitor.ebay.com", "beacon.walmart.com", "capture.condenastdigital.com"];

        public static readonly string[] HasInLink = {
            //https://github.com/the-advoid/ad-void/blob/main/AdVoid.Full.txt
            //https://github.com/hoshsadiq/adblock-nocoin-list/blob/master/nocoin.txt
            "/ads.js", "/ads.min.js", "/ad.js", "/ad.min.js", "/pagead.js",
            "/async-ads.js", "/admanager.js", "/ad-manager.js",
            "/analytics.js", "/analytics.min.js", "/tracker.js", "/tracker.min.js",
            "/ad-provider.js", "/adframe.js", "/adsbygoogle.js", "/advertising.js", "/advertisers.js", "/advertisement.min.js",
            "/gtag.js", "/insight.js", "/insight.min.js", "/tag.js", "/tag.min.js",
            "/trace.js", "/track.js", "/track.min.js", "/tracking.js", "/tracking.min.js",
            "/prebid.js", "/moneybid.js",
            "/webcoin.js", "/webcoin.min.js",
            "miner.js", "miner.min.js", //Don't add / for these miners so they cover deepminer and etc
            "/outbrain.js"
            //"cryptonight.wasm"
            ///disable-devtool.min.js
            //jsdelivr.net/npm/disable-devtool$script
            //redditstatic.com/ads/$script
            //redditstatic.com/ads/pixel.js^$script

            /*"survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"*//*, "youtube.com/ptracking", "youtube.com/pagead/adview", "youtube.com/api/stats/ads", "youtube.com/pagead/interaction",*/
        };
        public static readonly Regex HasInLinkRegex = new Regex(
            string.Join("|", HasInLink.Select(Regex.Escape)),
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
        );
        /*public static readonly Trie MinersFiles = new Trie {
         //https://github.com/xd4rker/MinerBlock/blob/master/assets/filters.txt
            "cryptonight.wasm", "deepminer.js", "deepminer.min.js", "coinhive.min.js", "monero-miner.js", "wasmminer.wasm", "wasmminer.js", "cn-asmjs.min.js", "gridcash.js",
            "worker-asmjs.min.js", "miner.js", "webmr4.js", "webmr.js", "webxmr.js",
            "lib/crypta.js", "static/js/tpb.js", "bitrix/js/main/core/core_tasker.js", "bitrix/js/main/core/core_loader.js", "vbb/me0w.js", "lib/crlt.js", "pool/direct.js",
            "plugins/wp-monero-miner-pro", "plugins/ajcryptominer", "plugins/aj-cryptominer",
            "?perfekt=wss://", "?proxy=wss://", "?proxy=ws://"
        };*/
        /*public static readonly DomainList Miners = new DomainList {
            //https://v.firebog.net/hosts/static/w3kbl.txt
            //https://github.com/hoshsadiq/adblock-nocoin-list/blob/master/hosts.txt
            "jsecoin.com", "crypto-loot.com", "minerad.com"
        };*/
        public static readonly DomainList Ads = new DomainList {
            "ads.google.com", "*.googlesyndication.com", "googletagservices.com", "googletagmanager.com", "*.googleadservices.com", "adservice.google.com",

            "syndicatedsearch.goog", "*.doubleclick.net", "*.g.doubleclick.net",
            "gads.pubmatic.com", "ads.pubmatic.com", "ogads-pa.clients6.google.com",
            "ads.facebook.com", "an.facebook.com",
            "cdn.snigelweb.com", "cdn.connectad.io",
            "pool.admedo.com", "c.pub.network",
            "media.ethicalads.io",
            "app-measurement.com",
            "ad.youtube.com", "ads.youtube.com", "youtube.cleverads.vn",
            "prod.di.api.cnn.io", "get.s-onetag.com", "assets.bounceexchange.com", "gn-web-assets.api.bbc.com", "pub.doubleverify.com",
            "events.reddit.com",
            "ads.tiktok.com", "ads-sg.tiktok.com", "ads.adthrive.com", "ads-api.tiktok.com", "business-api.tiktok.com",
            "ads.reddit.com", "d.reddit.com", "rereddit.com", "events.redditmedia.com",
            "ads-twitter.com", "static.ads-twitter.com", "ads-api.twitter.com", "advertising.twitter.com",
            "ads.pinterest.com", "ads-dev.pinterest.com",
            "adtago.s3.amazonaws.com", "advice-ads.s3.amazonaws.com", "advertising-api-eu.amazon.com", "amazonclix.com",
            "ads.linkedin.com",
            "*.dianomi.com",
            "*.media.net",
            "media.fastclick.net", "cdn.fastclick.net",
            "global.adserver.yahoo.com", "advertising.yahoo.com", "ads.yahoo.com", "ads.yap.yahoo.com", "adserver.yahoo.com", "partnerads.ysm.yahoo.com", "adtech.yahooinc.com", "advertising.yahooinc.co",
            "api-adservices.apple.com", "advertising.apple.com", "tr.iadsdk.apple.com",
            "yandexadexchange.net", "adsdk.yandex.ru", "advertising.yandex.ru", "an.yandex.ru",

            "secure-ds.serving-sys.com", "*.innovid.com", "*.html-load.com", "html-load.com",
            "*.outbrain.com.",
            "*.adcolony.com",
            "adm.hotjar.com",
            "files.adform.net",
            "static.adsafeprotected.com", "pixel.adsafeprotected.com",
            //"*.ad.xiaomi.com", "*.ad.intl.xiaomi.com",
            //"adsfs.oppomobile.com", "*.ads.oppomobile.com",
            "t.adx.opera.com",
            //"bdapi-ads.realmemobile.com", "bdapi-in-ads.realmemobile.com",
            //"business.samsungusa.com", "*.samsungads.com", "*.samsungadhub.com", "samsung-com.112.2o7.net", "ads.samsung.com",
            //"click.oneplus.com", "click.oneplus.cn", "open.oneplus.net",
            "asadcdn.com",
            "ads.yieldmo.com", "ads.servenobid.com", "e3.adpushup.com", "c1.adform.net",
            "ib.adnxs.com",
            "*.smartadserver.com", "ad.a-ads.com",
            "cdn.carbonads.com", "px.ads.linkedin.com",
            "*.adsrvr.org",
            "scdn.cxense.com",
            "acdn.adnxs.com",
            "js.adscale.de",
            "js.hsadspixel.net",
            "ad.mopub.com",
            "*.juicyads.com",
            "a.realsrv.com", "mc.yandex.ru", "a.vdo.ai", "adfox.yandex.ru", "adfstat.yandex.ru", "offerwall.yandex.net",
            "ads.msn.com", "adnxs.com", "adnexus.net", "bingads.microsoft.com",
            "dt.adsafeprotected.com",
            "amazonaax.com", "*.amazon-adsystem.com",
            "ads.betweendigital.com", "rtb.adpone.com", "ads.themoneytizer.com", "*.criteo.com",

            "*.rubiconproject.com",

            "*.ad.gt", "powerad.ai", "hb.brainlyads.com", "pixel.quantserve.com", "ads.anura.io", "static.getclicky.com",
            "ad.turn.com", "rtb.mfadsrvr.com", "ad.mrtnsvr.com", "s.ad.smaato.net",
            "adpush.technoratimedia.com", "pixel.tapad.com", "secure.adnxs.com", "px.adhigh.net",
            "epnt.ebay.com", "*.moatads.com", "s.pubmine.com", "px.ads.linkedin.com", "p.adsymptotic.com",
            "btloader.com", "ad-delivery.net",
            "services.vlitag.com", "tag.vlitag.com", "assets.vlitag.com",
            "*.adserver.snapads.com",
            "cdn.adsafeprotected.com",
            "rp.liadm.com",
            "ads.playground.xyz",
            "prebid.ad.smaato.net",
            "a.teads.tv",
            "targeting.unrulymedia.com",

            "adx.adform.net",
            "prebid.a-mo.net",
            "a.pub.network",
            "widgets.outbrain.com",
            "hb.adscale.de", "bitcasino.io",

            "h.seznam.cz", "d.seznam.cz", "ssp.seznam.cz",
            "cdn.performax.cz", "dale.performax.cz", "chip.performax.cz"
        };
        public static readonly DomainList Analytics = new DomainList { "ssl-google-analytics.l.google.com", "www-google-analytics.l.google.com", "www-googletagmanager.l.google.com", "analytic-google.com", "*.google-analytics.com",
            "analytics.google.com", "*.googleanalytics.com", "*.admobclick.com", "firebaselogging-pa.googleapis.com",
            "sp.ecosia.org",
            "analytics.facebook.com", "pixel.facebook.com",
            "analytics.tiktok.com", "analytics-sg.tiktok.com", "log.byteoversea.com",
            "analytics.pinterest.com", "widgets.pinterest.com", "log.pinterest.com", "trk.pinterest.com",
            "analytics.pointdrive.linkedin.com",
            "analyticsengine.s3.amazonaws.com", "affiliationjs.s3.amazonaws.com", "analytics.s3.amazonaws.com",
            "analytics.mobile.yandex.net", "appmetrica.yandex.com", "extmaps-api.yandex.net", "appmetrica.yandex.ru", "metrika.yandex.ru",
            "analytics.yahoo.com", "ups.analytics.yahoo.com", "analytics.query.yahoo.com", "log.fc.yahoo.com", "geo.yahoo.com", "udc.yahoo.com", "udcm.yahoo.com", "gemini.yahoo.com",
            "metrics.apple.com",
            "*.bugsnag.com",
            "*.sentry-cdn.com", "app.getsentry.com",
            "stats.gc.apple.com", "iadsdk.apple.com",
            "collector.github.com",
            "cloudflareinsights.com",
            "*.hotjar.com",
            "hotjar-analytics.com",
            "mouseflow.com", "*.mouseflow.com",
            "stats.wp.com",
            "*.datatrics.com",
            "fundingchoicesmessages.google.com",
            "mp.4dex.io",
            "*.inmobi.com",
            "script.4dex.io",
            "*.ero-advertising.com",
            "analytics.archive.org",
            "*.freshmarketer.com",
            "*.presage.io",
            "openbid.pubmatic.com", "prebid.media.net", "hbopenbid.pubmatic.com",
            "collector.cdp.cnn.com", "smetrics.cnn.com", "mybbc-analytics.files.bbci.co.uk", "a1.api.bbc.co.uk", "xproxy.api.bbc.com",
            "*.dotmetrics.net", "scripts.webcontentassessor.com",
            "collector.brandmetrics.com", "Builder.scorecardresearch.com",
            "queue.simpleanalyticscdn.com",
            "cdn.permutive.com", "api.permutive.com",

            "luckyorange.com", "*.luckyorange.com", "*.luckyorange.net",

            //"smetrics.samsung.com", "nmetrics.samsung.com", "analytics-api.samsunghealthcn.com", "analytics.samsungknox.com",
            //"iot-eu-logser.realme.com", "iot-logser.realme.com",
            "securemetrics.apple.com", "supportmetrics.apple.com", "metrics.icloud.com",
            //"books-analytics-events.apple.com", "weather-analytics-events.apple.com", "notes-analytics-events.apple.com",

            "tr.snapchat.com", "sc-analytics.appspot.com", "app-analytics.snapchat.com",
            "crashlogs.whatsapp.net",
            "metrics.mzstatic.com",

            "click.a-ads.com",
            "static.criteo.net",
            "www.clarity.ms",
            "u.clarity.ms",

            /*"data.mistat.xiaomi.com",
            "data.mistat.intl.xiaomi.com",
            "data.mistat.india.xiaomi.com",
            "data.mistat.rus.xiaomi.com",
            "tracking.miui.com",
            "sa.api.intl.miui.com",
            "tracking.intl.miui.com",
            "tracking.india.miui.com",
            "tracking.rus.miui.com",

            "*.hicloud.com",*/

            "s.cdn.turner.com",
            "logx.optimizely.com",
            "signal-metrics-collector-beta.s-onetag.com",
            "connect-metrics-collector.s-onetag.com",
            "ping.chartbeat.net",
            "logs.browser-intake-datadoghq.com",
            "onsiterecs.api.boomtrain.com",

            "b.6sc.co",
            "api.bounceexchange.com", "events.bouncex.net",
            "assets.adobedtm.com",
            "static.chartbeat.com",
            "dsum-sec.casalemedia.com",

            "aa.agkn.com",
            "material.anonymised.io",
            "static.anonymised.io",
            "*.tinypass.com",
            "dw-usr.userreport.com",
            "capture-api.reachlocalservices.com",
            "discovery.evvnt.com",
            "mab.chartbeat.com",
            "sync.sharethis.com",
            "bcp.crwdcntrl.net",

            "*.doubleverify.com", "onetag-sys.com",
            "id5-sync.com", "bttrack.com", "idsync.rlcdn.com", "u.openx.net", "sync-t1.taboola.com", "x.bidswitch.net", "rtd-tm.everesttech.net", "usermatch.krxd.net", "visitor.omnitagjs.com", "ping.chartbeat.net",
            "sync.outbrain.com",
            "collect.mopinion.com", "pb-server.ezoic.com",
            "demand.trafficroots.com", "sync.srv.stackadapt.com", "sync.ipredictive.com", "analytics.vdo.ai", "tag-api-2-1.ccgateway.net", "sync.search.spotxchange.com",
            "reporting.powerad.ai", "monitor.ebay.com", "beacon.walmart.com", "capture.condenastdigital.com"
        };

        public WebRiskHandler _WebRiskHandler;

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
                    CurrentWindow.GetTab().Content.Favourite();
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
            }
        }

        private void InitializeBrowser()
        {
            //Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1";
            //Settings.UserAgentProduct = $"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}";
            //Settings.UserAgent = UserAgent;

            string UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));

            WebViewSettings Settings = new WebViewSettings();
            Settings.RegisterProtocol("gemini", WebViewManager.GeminiHandler);
            Settings.RegisterProtocol("gopher", WebViewManager.GopherHandler);
            Settings.RegisterProtocol("slbr", WebViewManager.SLBrHandler);

            Settings.CefRuntimeStyle = GlobalSave.GetInt("ChromiumRuntimeStyle", 1) == 1 ? CefRuntimeStyle.Alloy : CefRuntimeStyle.Chrome;
            switch (GlobalSave.GetInt("TridentVersion", 4))
            {
                case 0: Settings.TridentVersion = TridentEmulationVersion.IE7; break;
                case 1: Settings.TridentVersion = TridentEmulationVersion.IE8; break;
                case 2: Settings.TridentVersion = TridentEmulationVersion.IE9; break;
                case 3: Settings.TridentVersion = TridentEmulationVersion.IE10; break;
                case 4: Settings.TridentVersion = TridentEmulationVersion.IE11; break;
            }

            Settings.UserDataPath = UserDataPath;
            Settings.Language = Locale.Tooltip;
            Settings.Languages = Languages.Select(i => i.Tooltip).ToArray();
            Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));

            Settings.Performance = (PerformancePreset)GlobalSave.GetInt("Performance");
            Settings.DownloadFolderPath = GlobalSave.Get("DownloadPath");
            Settings.DownloadPrompt = bool.Parse(GlobalSave.Get("DownloadPrompt"));
            Settings.GPUAcceleration = bool.Parse(GlobalSave.Get("BrowserHardwareAcceleration"));
            Settings.SpellCheck = bool.Parse(GlobalSave.Get("SpellCheck"));

            SetBrowserFlags(Settings);

            WebViewManager.Settings = Settings;
            WebViewManager.RuntimeSettings.PDFViewer = bool.Parse(GlobalSave.Get("PDF"));

            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(), (int)Key.R, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(), (int)Key.F5, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Refresh(true), (int)Key.F5, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Fullscreen(), (int)Key.F11, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(2), (int)Key.Escape, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => DevTools(), (int)Key.F12, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => Find(), (int)Key.F, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(0), (int)Key.F6, false, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(1), (int)Key.T, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(3), (int)Key.D, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(4), (int)Key.S, true, false, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(5), (int)Key.F9, false, false, false));
            //For some reason Chrome runtime style opens a new chrome window
            //HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(6), (int)Key.N, true, true, false));
            HotKeyManager.HotKeys.Add(new HotKey(() => KeyAction(6), (int)Key.P, true, false, false));

            WebViewManager.DownloadManager.DownloadStarted += UpdateDownloadItem;
            WebViewManager.DownloadManager.DownloadUpdated += UpdateDownloadItem;
            WebViewManager.DownloadManager.DownloadCompleted += UpdateDownloadItem;

            _WebRiskHandler = new WebRiskHandler();

            WebEngineType DefaultEngine = (WebEngineType)GlobalSave.GetInt("WebEngine");
            switch (DefaultEngine)
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

        /*#if DEBUG
                public string GetPreferencesString(string _String, string Parents, KeyValuePair<string, object> ObjectPair)
                {
                    if (ObjectPair.Value is System.Dynamic.ExpandoObject expando)
                    {
                        foreach (KeyValuePair<string, object> property in (IDictionary<string, object>)expando)
                            _String = $"{GetPreferencesString(_String, Parents + $"[{ObjectPair.Key}]", property)}";
                        if (string.IsNullOrEmpty(Parents))
                            _String += "\n";
                    }
                    else if (ObjectPair.Value is List<object> _List)
                        _String += string.Join(", ", _List);
                    else
                    {
                        if (!string.IsNullOrEmpty(Parents))
                            _String += $"{Parents}: ";
                        _String += $"{ObjectPair.Key}: {ObjectPair.Value}\n";
                    }
                    return _String;
                }
        #endif*/

        public string GenerateCannotConnect(string Url, int ErrorCode, string ErrorText)
        {
            string Host = Utils.Host(Url);
            string HTML = Cannot_Connect_Error.Replace("{Site}", Host).Replace("{Error}", ErrorText);
            switch (ErrorCode)
            {
                /*case CefErrorCode.ConnectionTimedOut:
                    HTML = HTML.Replace("{Description}", $"{Host} took too long to respond.");
                    break;
                case CefErrorCode.ConnectionReset:
                    HTML = HTML.Replace("{Description}", $"The connection was reset.");
                    break;
                case CefErrorCode.ConnectionFailed:
                    HTML = HTML.Replace("{Description}", $"The connection failed.");
                    break;
                case CefErrorCode.ConnectionRefused:
                    HTML = HTML.Replace("{Description}", $"{Host} refused to connect.");
                    break;
                case CefErrorCode.ConnectionClosed:
                    HTML = HTML.Replace("{Description}", $"{Host} unexpectedly closed the connection.");
                    break;
                case CefErrorCode.InternetDisconnected:
                    HTML = HTML.Replace("{Description}", $"Internet was disconnected.");
                    break;
                case CefErrorCode.NameNotResolved:
                    HTML = HTML.Replace("{Description}", $"The URL entered could not be resolved.");
                    break;
                case CefErrorCode.NetworkChanged:
                    HTML = HTML.Replace("{Description}", $"{Host} took too long to respond.");
                    break;
                case CefErrorCode.CertInvalid:
                case CefErrorCode.CertDateInvalid:
                case CefErrorCode.CertAuthorityInvalid:
                case CefErrorCode.CertCommonNameInvalid:
                    HTML = HTML.Replace("{Description}", $"The connection to {Host} is not private.");
                    break;*/
                //TODO
                default:
                    HTML = HTML.Replace("{Description}", $"Error Code: {ErrorCode}");
                    break;
            }
            return HTML;
        }

        public const string Cannot_Connect_Error = @"<html><head><title>Unable to connect to {Site}</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Unable to connect to {Site}</h2><h5 id=""description"">{Description}</h5><h5 id=""error"" style=""margin:0px; color:#646464;"">{Error}</h5></div></body></html>";
        public const string Process_Crashed_Error = @"<html><head><title>Process crashed</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Process crashed</h2><h5>Process crashed while attempting to load content. Refresh the page to resolve the problem.</h5></div></body></html>";
        public const string Deception_Error = @"<html><head><title>Deceptive site ahead</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Deceptive site ahead</h2><h5>The site may contain deceptive content that may trick you into installing software or revealing personal information.</h5></div></body></html>";
        public const string Malware_Error = @"<html><head><title>Dangerous site ahead</title><style>html{background:darkred;}body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons','Segoe MDL2 Assets';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Dangerous site ahead</h2><h5>The site may install harmful and malicious software that may manipulate or steal personal information.</h5></div></body></html>";

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
        public const string DummyUrl = "dummy.invalid";
        public static void SetUrlFlags(WebViewSettings Settings)
        {
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
            Settings.AddFlag("model-quality-service-url", DummyUrl);
            Settings.AddFlag("oauth-account-manager-url", DummyUrl);
            Settings.AddFlag("secure-connect-api-url", DummyUrl);

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

            if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
            {
                //Settings.AddFlag("webtransport-developer-mode");
                Settings.AddFlag("enable-experimental-cookie-features");

                Settings.AddFlag("enable-experimental-webassembly-features");
                Settings.AddFlag("enable-experimental-webassembly-jspi");

                Settings.AddFlag("enable-experimental-web-platform-features");

                Settings.AddFlag("enable-javascript-harmony");
                Settings.AddFlag("enable-javascript-experimental-shared-memory");

                Settings.AddFlag("enable-future-v8-vm-features");
                //Settings.AddFlag("enable-hardware-secure-decryption-experiment");
                //Settings.AddFlag("text-box-trim");

                //Settings.AddFlag("enable-devtools-experiments");

                Settings.AddFlag("enable-webgl-developer-extensions");
                //Settings.AddFlag("enable-webgl-draft-extensions");
                //Settings.AddFlag("enable-webgpu-developer-features");
                Settings.AddFlag("enable-experimental-extension-apis");
            }
        }

        private void SetBackgroundFlags(WebViewSettings Settings)
        {
            //https://github.com/cefsharp/CefSharp/commit/2f96ee9bb16254d40cce8eaa6144107b689c8ff4
            //Settings.Flags.Remove("disable-back-forward-cache");
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
                Settings.AddFlag("gpu-disk-cache-size-kb", $"{128 * 1024}");

                Settings.AddFlag("force-effective-connection-type", "Slow-2G-On-Cellular");
                //Settings.AddFlag("num-raster-threads", "4"); //RETIRED FLAG
                Settings.AddFlag("renderer-process-limit", "4");
                //Settings.AddFlag("use-mobile-user-agent");
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
            //Settings.AddFlag("v8-cache-options");

            //Settings.AddFlag("disable-v8-idle-tasks");


            Settings.AddFlag("enable-parallel-downloading");
        }

        /*NEVER SLOW MODE FLAGS
        // The adapter selecting strategy related to GPUPowerPreference.
        https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/service_utils.cc
        none = WebGPUPowerPreference::kNone
        default-low-power = WebGPUPowerPreference::kDefaultLowPower;
        default-high-performance = WebGPUPowerPreference::kDefaultHighPerformance;
        force-low-power = WebGPUPowerPreference::kForceLowPower;
        force-high-performance = WebGPUPowerPreference::kForceHighPerformance;

        use-webgpu-power-preference = force-low-power

        // Allows explicitly specifying the shader disk cache size for embedded devices. Default value is 6MB. On Android, 2MB is default and 128KB for low-end devices.
        //https://source.chromium.org/chromium/chromium/src/+/main:gpu/config/gpu_preferences.h
        gpu-disk-cache-size-kb = 2 * 1024 * 1024 / Low End Device Mode 128 * 1024

        // Override the maximum framerate as can be specified in calls to getUserMedia. This flag expects a value. Example: --max-gum-fps=17.5
        max-gum-fps

        // Forces the maximum disk space to be used by the disk cache, in bytes.
        disk-cache-size

        // Specifies the max number of bytes that should be used by the skia font cache. If the cache needs to allocate more, skia will purge previous entries.
        skia-font-cache-limit-mb = 100KiB

        // Specifies the max number of bytes that should be used by the skia resource cache. The previous entries are purged from the cache when the memory useage exceeds this limit.
        skia-resource-cache-limit-mb = 100KiB

        // Allows user to override maximum number of active WebGL contexts per renderer process.
        max-active-webgl-contexts

        // Sets the maximium decoded image size limitation.
        max-decoded-image-size-mb = 1MiB

        // Sets the maximum number of WebMediaPlayers allowed per frame.
        max-web-media-player-count

        // Sets the maximum size, in megabytes. The log file can grow to before older data is overwritten. Do not use this flag if you want an unlimited file size.
        net-log-max-size-mb

        // This is only used when we did not set buffer size in trace config and will be used for all trace sessions. If not provided, we will use the default value provided in perfetto_config.cc
        default-trace-buffer-size-limit-in-kb

        // Configures the size of the shared memory buffer used for tracing. Value is provided in kB. Defaults to 4096. Should be a multiple of the SMB page size (currently 32kB on Desktop or 4kB on Android).
        trace-smb-size

        // Sets the maximum GPU memory to use for discardable caches.
        force-gpu-mem-discardable-limit-mb

        // Sets the total amount of memory that may be allocated for GPU resources
        force-gpu-mem-available-mb

        // Sets the maximum texture size in pixels.
        force-max-texture-size

        // Sets the maximum size of the in-memory gpu program cache, in kb //https://source.chromium.org/chromium/chromium/src/+/main:gpu/config/gpu_preferences.h
        gpu-program-cache-size-kb = 2 * 1024 * 1024 / Low End Device Mode 128 * 1024

        // Specifies the heap limit for Vulkan memory. TODO(crbug.com/40161102): Remove this switch.
        vulkan-heap-memory-limit-mb
        // Specifies the sync CPU limit for total Vulkan memory. TODO(crbug.com/40161102): Remove this switch.
        vulkan-sync-cpu-memory-limit-mb

        // Allows explicitly specifying MSE audio/video buffer sizes as megabytes. Default values are 150M for video and 12M for audio.
        mse-audio-buffer-size-limit-mb
        mse-video-buffer-size-limit-mb

        disallow-doc-written-script-loads
        BlinkSettings = "disallowFetchForDocWrittenScriptsInMainFrame=true"
        6 connections per proxy server
        */

        private void SetGraphicsFlags(WebViewSettings Settings)
        {
            Settings.AddFlag("in-process-gpu");
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
            Settings.AddFlag("enable-tls13-early-data");

            Settings.AddFlag("reduce-accept-language");
            //Settings.AddFlag("reduce-transfer-size-updated-ipc");

            //Settings.AddFlag("enable-network-information-downlink-max");
            //Settings.AddFlag("enable-precise-memory-info");

            Settings.AddFlag("enable-quic");
            Settings.AddFlag("enable-spdy4");
            Settings.AddFlag("enable-ipv6");

            Settings.AddFlag("no-proxy-server");
            //Settings.AddFlag("winhttp-proxy-resolver");
            Settings.AddFlag("no-pings");

            Settings.AddFlag("disable-background-networking");
            Settings.AddFlag("disable-component-extensions-with-background-pages");
        }

        private void SetSecurityFlags(WebViewSettings Settings)
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
            Settings.AddFlag("auto-select-desktop-capture-source", "Entire screen");

            //Settings.AddFlag("turn-off-streaming-media-caching-always");
            //Settings.AddFlag("turn-off-streaming-media-caching-on-battery");
        }

        private void SetFeatureFlags(WebViewSettings Settings)
        {
            /*[Blink Settings]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/public/common/web_preferences/web_preferences.h
             * https://chromium.googlesource.com/chromium/blink/+/refs/heads/main/Source/core/frame/Settings.in
             * https://source.chromium.org/chromium/chromium/src/+/main:out/lacros-Debug/gen/third_party/blink/public/mojom/webpreferences/web_preferences.mojom.js
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/core/frame/settings.json5
             * 
             * bypassCSP=true //disable Content Security Policy
             */

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
             * 
             * https://stackoverflow.com/questions/73055564/which-nodejs-v8-flags-for-benchmarking
             * https://stackoverflow.com/questions/48387040/how-do-i-determine-the-correct-max-old-space-size-for-node-js
             * 
             * --always-opt //This does not improve performance, on the contrary; it causes V8 to waste CPU cycles on useless work.
             * --predictable-gc-schedule
             */

            /*[enable/disable-blink-features]
             * https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/runtime_enabled_features.json5
             * 
             * NetInfoConstantType, NetInfoDownlinkMax, V8IdleTasks, PrettyPrintJSONDocument
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
            Settings.AddFlag("force-fieldtrials", "SimpleCacheTrial/ExperimentYes/");
            
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
            string EnableFeatures = "EnableLazyLoadImageForInvisiblePage:enabled_page_type/all_invisible_page,HeapProfilerReporting,ReducedReferrerGranularity,ThirdPartyStoragePartitioning,PrecompileInlineScripts,OptimizeHTMLElementUrls,UseEcoQoSForBackgroundProcess,EnableLazyLoadImageForInvisiblePage,ParallelDownloading,TrackingProtection3pcd,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
            //https://github.com/chromiumembedded/cef/issues/3991
            //https://github.com/chromiumembedded/cef/issues/3966
            string DisableFeatures = "StorageNotificationService,LensOverlay,KAnonymityService,NetworkTimeServiceQuerying,LiveCaption,DefaultWebAppInstallation,PersistentHistograms,Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,SegmentationPlatform,InstalledApp,BrowsingTopics,Fledge,FledgeBiddingAndAuctionServer,InterestFeedContentSuggestions,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
            //WebBluetooth,MediaRouter,
            string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility";
            string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI";//Adding ,DocumentPictureInPictureAPI would stop WebView2's NewWindowRequested from being called on PiP popups

            try
            {
                Settings.AddFlag("disable-features", DisableFeatures);
                Settings.AddFlag("enable-features", EnableFeatures);
                Settings.AddFlag("enable-blink-features", EnableBlinkFeatures);
                Settings.AddFlag("disable-blink-features", DisableBlinkFeatures);
            }
            catch
            {
                Settings.Flags["disable-features"] += "," + DisableFeatures;
                Settings.Flags["enable-features"] += "," + EnableFeatures;
                Settings.Flags["enable-blink-features"] += "," + EnableBlinkFeatures;
                Settings.Flags["disable-blink-features"] += "," + DisableBlinkFeatures;
            }

            //https://source.chromium.org/chromiumos/chromiumos/codesearch/+/main:src/platform2/login_manager/feature_flags_tables.h;l=840?q=enable-lazy-image-loading


            //https://github.com/Alex313031/thorium/blob/main/src/chrome/browser/chrome_content_browser_client.cc
            //https://chromium.googlesource.com/chromium/src/third_party/+/master/blink/renderer/core/frame/settings.json5
            //https://chromium.googlesource.com/chromium/src/+/HEAD/third_party/blink/public/platform/web_effective_connection_type.h
            Settings.AddFlag("blink-settings", "hyperlinkAuditingEnabled=false,smoothScrollForFindEnabled=true,disallowFetchForDocWrittenScriptsInMainFrame=true");

            if (HighPerformanceMode)
            {
                JsFlags += " --fast-math";
                Settings.Flags["blink-settings"] += ",lazyLoadEnabled=false";
                //https://www.aboutchromebooks.com/chrome-flagsscheduler-configuration/
                Settings.Flags["enable-features"] += ",SchedulerConfiguration:scheduler_configuration/enabled";
            }
            else
            {
                Settings.Flags["blink-settings"] += ",lowPriorityIframesThreshold=5,dnsPrefetchingEnabled=false,doHtmlPreloadScanning=false";
                Settings.Flags["enable-features"] += ",LazyImageLoading:automatic-lazy-load-images-enabled/true/restrict-lazy-load-images-to-data-saver-only/false,LazyFrameLoading:automatic-lazy-load-frames-enabled/true/restrict-lazy-load-frames-to-data-saver-only/false,LowLatencyCanvas2dImageChromium,LowLatencyWebGLImageChromium,NoStatePrefetchHoldback,ReduceCpuUtilization2,MemorySaverModeRenderTuning,OomIntervention,QuickIntensiveWakeUpThrottlingAfterLoading,LowerHighResolutionTimerThreshold,BatterySaverModeAlignWakeUps,RestrictThreadPoolInBackground,IntensiveWakeUpThrottling:grace_period_seconds/5,MemoryCacheStrongReference,OptOutZeroTimeoutTimersFromThrottling,CheckHTMLParserBudgetLessOften,Canvas2DHibernation,Canvas2DHibernationReleaseTransferMemory";
                Settings.Flags["disable-features"] += ",LoadingPredictorPrefetch,SpeculationRulesPrefetchFuture,NavigationPredictor,Prerender2MainFrameNavigation,Prerender2NoVarySearch,Prerender2";

                Settings.Flags["enable-blink-features"] += ",SkipPreloadScanning,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
                Settings.Flags["disable-blink-features"] += ",Prerender2";

                if (LiteMode)
                {
                    //https://github.com/cypress-io/cypress/issues/22622
                    //https://issues.chromium.org/issues/40220332
                    Settings.Flags["disable-features"] += ",LoadingTasksUnfreezable,LogJsConsoleMessages,BoostImagePriority,BoostImageSetLoadingTaskPriority,BoostFontLoadingTaskPriority,BoostVideoLoadingTaskPriority,BoostRenderBlockingStyleLoadingTaskPriority,BoostNonRenderBlockingStyleLoadingTaskPriority";
                    Settings.Flags["enable-features"] += ",LiteVideo,AllowAggressiveThrottlingWithWebSocket,stop-in-background,ClientHintsSaveData,SaveDataImgSrcset,LowPriorityScriptLoading,LowPriorityAsyncScriptExecution";
                    Settings.Flags["enable-blink-features"] += ",PrefersReducedData,ForceReduceMotion";//
                    Settings.Flags["blink-settings"] += ",imageAnimationPolicy=1,prefersReducedTransparency=true,prefersReducedMotion=true,lazyLoadingFrameMarginPxUnknown=0,lazyLoadingFrameMarginPxOffline=0,lazyLoadingFrameMarginPxSlow2G=0,lazyLoadingFrameMarginPx2G=0,lazyLoadingFrameMarginPx3G=0,lazyLoadingFrameMarginPx4G=0,lazyLoadingImageMarginPxUnknown=0,lazyLoadingImageMarginPxOffline=0,lazyLoadingImageMarginPxSlow2G=0,lazyLoadingImageMarginPx2G=0,lazyLoadingImageMarginPx3G=0,lazyLoadingImageMarginPx4G=0";
                    JsFlags += " --max-lazy --lite-mode --noexpose-wasm --optimize-for-size";
                }
                else
                {
                    Settings.Flags["blink-settings"] += ",lazyLoadingFrameMarginPxUnknown=250,lazyLoadingFrameMarginPxOffline=500,lazyLoadingFrameMarginPxSlow2G=500,lazyLoadingFrameMarginPx2G=400,lazyLoadingFrameMarginPx3G=300,lazyLoadingFrameMarginPx4G=200,lazyLoadingImageMarginPxUnknown=250,lazyLoadingImageMarginPxOffline=500,lazyLoadingImageMarginPxSlow2G=500,lazyLoadingImageMarginPx2G=400,lazyLoadingImageMarginPx3G=300,lazyLoadingImageMarginPx4G=200";
                }
                //https://chromium.googlesource.com/v8/v8/+/master/src/flags/flag-definitions.h
                JsFlags += " --efficiency-mode --battery-saver-mode --memory-saver-mode";
            }
            if (!LiteMode)
            {
                JsFlags += " --enable-experimental-regexp-engine-on-excessive-backtracks --expose-wasm --wasm-lazy-compilation --asm-wasm-lazy-compilation --wasm-lazy-validation --experimental-wasm-gc --wasm-async-compilation --wasm-opt --experimental-wasm-branch-hinting --experimental-wasm-instruction-tracing";
                if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
                    JsFlags += " --experimental-wasm-jspi --experimental-wasm-memory64 --experimental-wasm-type-reflection";
            }
            if (!bool.Parse(GlobalSave.Get("JIT")))
                JsFlags += " --jitless";
            Settings.JavaScriptFlags = JsFlags;
        }

        private void SetEdgeFlags(WebViewSettings Settings)
        {
            // Does this actually work? Disabling msSmartScreenProtection in --disable-features does seem to work
            //msLocalSpellcheck,msFreezeAdFramesImmediately,msEdgeAdaptiveCPUThrottling
            //msEdgeWebViewApplyWebResourceRequestedFilterForOOPIFs
            string EnableFeatures = "msWebView2CancelInitialNavigation,msWebView2CodeCache,msWebView2TreatAppSuspendAsDeviceSuspend";
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

        public void ClearAllData()
        {
            AdsBlocked = 0;
            TrackersBlocked = 0;
            History.Clear();
            Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
            Cef.GetGlobalRequestContext().ClearHttpAuthCredentialsAsync();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                {
                    if (BrowserView != null && BrowserView.WebView != null && BrowserView.WebView.IsBrowserInitialized)
                    {
                        if (BrowserView.WebView.CanExecuteJavascript)
                            BrowserView.WebView.ExecuteScript("localStorage.clear();sessionStorage.clear();");
                        //https://github.com/cefsharp/CefSharp/issues/1234
                        BrowserView.WebView.CallDevToolsAsync("Storage.clearDataForOrigin", new
                        {
                            origin = "*",
                            storageTypes = "all"
                        });
                        BrowserView.WebView.CallDevToolsAsync("Page.clearCompilationCache");
                        BrowserView.WebView.CallDevToolsAsync("Page.resetNavigationHistory");
                        BrowserView.WebView.CallDevToolsAsync("Network.clearBrowserCookies");
                        BrowserView.WebView.CallDevToolsAsync("Network.clearBrowserCache");
                    }
                }
            }
            InformationDialogWindow InfoWindow = new InformationDialogWindow("Information", $"Settings", "All browsing data has been cleared.", "\ue713");
            InfoWindow.Topmost = true;
            InfoWindow.ShowDialog();
        }

        public void Save()
        {
            StatisticsSave.Set("BlockedTrackers", TrackersBlocked.ToString());
            StatisticsSave.Set("BlockedAds", AdsBlocked.ToString());

            FavouritesSave.Clear();
            FavouritesSave.Set("Count", Favourites.Count.ToString(), false);
            for (int i = 0; i < Favourites.Count; i++)
                FavouritesSave.Set(i.ToString(), Favourites[i].Tooltip, Favourites[i].Name, false);
            FavouritesSave.Save();

            SearchSave.Clear();
            SearchSave.Set("Count", SearchEngines.Count.ToString(), false);
            for (int i = 0; i < SearchEngines.Count; i++)
            {
                SearchProvider _SearchProvider = SearchEngines[i];
                SearchSave.Set(i.ToString(), $"{_SearchProvider.Name}<#>{_SearchProvider.SearchUrl}<#>{_SearchProvider.SuggestUrl}", false);
            }
            SearchSave.Save();

            AllowListSave.Clear();
            AllowListSave.Set("Count", AdBlockAllowList.AllDomains.Count().ToString(), false);
            int DomainIndex = 0;
            foreach (string Domain in AdBlockAllowList.AllDomains)
            {
                AllowListSave.Set(DomainIndex.ToString(), Domain, false);
                DomainIndex++;
            }
            AllowListSave.Save();

            LanguagesSave.Clear();
            LanguagesSave.Set("Count", Languages.Count.ToString(), false);
            LanguagesSave.Set("Selected", Languages.IndexOf(Locale), false);
            for (int i = 0; i < Languages.Count; i++)
                LanguagesSave.Set(i.ToString(), Languages[i].Tooltip, false);
            LanguagesSave.Save();

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
                        if (Tab.ParentWindow != null && !Tab.Content.Private)
                        {
                            TabsSave.Set(Count.ToString(), Tab.Content.Address, false);
                            if (i == OriginalSelectedIndex)
                                SelectedIndex = Count;
                            Count++;
                        }
                    }
                    TabsSave.Set("Selected", SelectedIndex.ToString());
                    TabsSave.Set("Count", Count.ToString());
                }
            }
        }

        public void CloseSLBr(bool ExecuteCloseEvents = true)
        {
            new Thread(() => {
                Thread.Sleep(1000);
                try { Process.GetCurrentProcess().Kill(); }
                catch {}
            }) { IsBackground = true }.Start();
            if (AppInitialized)
                Save();
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

        public BitmapImage TabIcon;
        public BitmapImage PrivateIcon;
        public BitmapImage AudioIcon;
        public BitmapImage PDFTabIcon;
        public BitmapImage SettingsTabIcon;
        public BitmapImage HistoryTabIcon;
        public BitmapImage DownloadsTabIcon;
        public BitmapImage UnloadedIcon;

        public BitmapImage GetIcon(string Url, bool IsPrivate = false)
        {
            if (Utils.GetFileExtension(Url) != ".pdf")
            {
                if (!IsPrivate && Utils.IsHttpScheme(Url))
                {
                    switch (GlobalSave.GetInt("FaviconService", 0))
                    {
                        case 0:
                            string GIconUrl = "https://t0.gstatic.com/faviconV2?client=chrome_desktop&nfrp=2&check_seen=true&size=24&min_size=16&max_size=256&fallback_opts=TYPE,SIZE,URL&url=" + Utils.CleanUrl(Url, true, true, true, false, false);
                            /*if (FaviconCache.TryGetValue(GIconUrl, out BitmapImage GCachedImage))
                                return GCachedImage;*/
                            BitmapImage _GImage = new BitmapImage(new Uri(GIconUrl));
                            if (_GImage.CanFreeze)
                                _GImage.Freeze();
                            //FaviconCache[GIconUrl] = _GImage;
                            return _GImage;
                        case 1:
                            string YIconUrl = "https://favicon.yandex.net/favicon/" + Utils.FastHost(Url);
                            /*if (FaviconCache.TryGetValue(YIconUrl, out BitmapImage YCachedImage))
                                return YCachedImage;*/
                            BitmapImage _YImage = new BitmapImage(new Uri(YIconUrl));
                            if (_YImage.CanFreeze)
                                _YImage.Freeze();
                            //FaviconCache[YIconUrl] = _YImage;
                            return _YImage;
                        case 2:
                            string DIconUrl = "https://icons.duckduckgo.com/ip3/" + Utils.FastHost(Url) + ".ico";
                            /*if (FaviconCache.TryGetValue(DIconUrl, out BitmapImage DCachedImage))
                                return DCachedImage;*/
                            BitmapImage _DImage = new BitmapImage(new Uri(DIconUrl));
                            if (_DImage.CanFreeze)
                                _DImage.Freeze();
                            //FaviconCache[DIconUrl] = _DImage;
                            return _DImage;
                        case 3:
                            string AIconUrl = "https://f1.allesedv.com/32/" + Utils.FastHost(Url);
                            /*if (FaviconCache.TryGetValue(AIconUrl, out BitmapImage ACachedImage))
                                return ACachedImage;*/
                            BitmapImage _AImage = new BitmapImage(new Uri(AIconUrl));
                            if (_AImage.CanFreeze)
                                _AImage.Freeze();
                            //FaviconCache[AIconUrl] = _AImage;
                            return _AImage;
                    }
                }
                else if (Url.StartsWith("slbr://settings", StringComparison.Ordinal))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://history", StringComparison.Ordinal))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://downloads", StringComparison.Ordinal))
                    return DownloadsTabIcon;
                return IsPrivate ? PrivateIcon : TabIcon;
            }
            else
                return PDFTabIcon;
        }

        public static Brush GetContrastBrush(Color bgColor)
        {
            return (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255 > 0.6 ? Brushes.Black : Brushes.White;
        }
        /*public enum ProfileIconStyle
        {
            Default, //Chrome-like
            CircleOverlay,
            Flat,
            Square,
        }*/
        public static RenderTargetBitmap GenerateProfileIcon(string BaseIconPath, string Initial, int Size = 64)//,ProfileIconStyle IconStyle = ProfileIconStyle.Default
        {
            BitmapImage BaseBitmap = new BitmapImage(new Uri(BaseIconPath));
            var BaseImage = new Image
            {
                Source = BaseBitmap,
                Width = Size,
                Height = Size
            };

            DrawingVisual Visual = new DrawingVisual();
            using (var Context = Visual.RenderOpen())
            {
                Context.DrawImage(BaseBitmap, new Rect(0, 0, Size, Size));

                int BadgeSize = Size / 2;
                SolidColorBrush BadgeColor = new SolidColorBrush(Color.FromRgb((byte)MiniRandom.Next(100, 255), (byte)MiniRandom.Next(100, 255), (byte)MiniRandom.Next(100, 255)));
                Rect BadgeRect = new Rect(Size - BadgeSize, Size - BadgeSize, BadgeSize, BadgeSize);
                Point BadgeCenter = new Point(BadgeRect.X + BadgeSize / 2, BadgeRect.Y + BadgeSize / 2);
                Context.DrawEllipse(BadgeColor, null, BadgeCenter, BadgeSize / 2, BadgeSize / 2);
                /*switch (IconStyle)
                {
                    case ProfileIconStyle.CircleOverlay:
                        Context.DrawEllipse(BadgeColor, null, BadgeCenter, BadgeSize, BadgeSize);
                        break;
                    case ProfileIconStyle.Flat:
                        Context.DrawRectangle(BadgeColor, null, new Rect(0, 0, Size, Size));
                        break;
                    case ProfileIconStyle.Square:
                        Context.DrawRectangle(BadgeColor, null, BadgeRect);
                        break;
                    default:
                        Context.DrawEllipse(BadgeColor, null, BadgeCenter, BadgeSize / 2, BadgeSize / 2);
                        break;
                }*/

                FormattedText FormattedText = new FormattedText(
                    Initial,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Bold"),
                    BadgeSize / 1.25,
                    GetContrastBrush(BadgeColor.Color),
                    1.25
                );

                /*Point Location = IconStyle switch
                {
                    ProfileIconStyle.Flat => new Point((Size - FormattedText.Width) / 2, (Size - FormattedText.Height) / 2),
                    ProfileIconStyle.CircleOverlay => new Point((Size - FormattedText.Width) / 2, (Size - FormattedText.Height) / 2),
                    _ => new Point(BadgeRect.X + (BadgeSize - FormattedText.Width) / 2, BadgeRect.Y + (BadgeSize - FormattedText.Height) / 2)
                };
                Context.DrawText(FormattedText, Location);*/

                Point Location = new Point(BadgeRect.X + (BadgeSize - FormattedText.Width) / 2, BadgeRect.Y + (BadgeSize - FormattedText.Height) / 2);
                Context.DrawText(FormattedText, Location);
            }

            RenderTargetBitmap Bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
            Bitmap.Render(Visual);
            if (Bitmap.CanFreeze)
                Bitmap.Freeze();
            return Bitmap;
        }

        private static readonly Dictionary<string, BitmapImage?> FaviconCache = new();
        private static readonly Dictionary<string, Task<BitmapImage?>> DownloadingFavicons = new();
        private static readonly LinkedList<string> CacheOrder = new();
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

        public async Task<BitmapImage> SetIcon(string IconUrl, string Url = "", bool IsPrivate = false)
        {
            if (Utils.GetFileExtension(Url) != ".pdf")
            {
                if (!IsPrivate && Utils.IsHttpScheme(IconUrl))
                {
                    if (FaviconCache.TryGetValue(IconUrl, out BitmapImage? CachedImage))
                        return CachedImage ?? TabIcon;
                    try
                    {
                        if (DownloadingFavicons.TryGetValue(IconUrl, out Task<BitmapImage?> PendingTask))
                            return await PendingTask ?? TabIcon;
                        Task<BitmapImage?> IconTask = Task.Run(async () =>
                        {
                            byte[]? ImageData = await DownloadFaviconAsync(IconUrl);
                            if (ImageData != null)
                            {
                                BitmapImage Bitmap = new BitmapImage();
                                using (MemoryStream Stream = new MemoryStream(ImageData))
                                {
                                    Bitmap.BeginInit();
                                    Bitmap.StreamSource = Stream;
                                    Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    Bitmap.EndInit();
                                    if (Bitmap.CanFreeze)
                                        Bitmap.Freeze();
                                }
                                CacheFavicon(IconUrl, Bitmap);
                                return Bitmap;
                            }
                            else
                                return null;
                        });
                        DownloadingFavicons[IconUrl] = IconTask;
                        BitmapImage? Result = await IconTask;
                        DownloadingFavicons.Remove(IconUrl);
                        return Result ?? TabIcon;
                    }
                    catch { }
                }
                else if (IconUrl.StartsWith("data:image/", StringComparison.Ordinal))
                {
                    try
                    {
                        //CacheFavicon(IconUrl, Bitmap);
                        return Utils.ConvertBase64ToBitmapImage(IconUrl);
                    }
                    catch
                    {
                        //CacheFavicon(IconUrl, null);
                    }
                }
                else if (Url.StartsWith("slbr://settings", StringComparison.Ordinal))
                    return SettingsTabIcon;
                else if (Url.StartsWith("slbr://history", StringComparison.Ordinal))
                    return HistoryTabIcon;
                else if (Url.StartsWith("slbr://downloads", StringComparison.Ordinal))
                    return DownloadsTabIcon;
                return IsPrivate ? PrivateIcon : TabIcon;
            }
            else
                return PDFTabIcon;
        }
        private async Task<byte[]?> DownloadFaviconAsync(string Url)
        {
            Debug.Write("Downloaded\n");
            if (string.IsNullOrEmpty(Url))
                return null;
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", UserAgentGenerator.BuildChromeBrand());
                    _WebClient.Headers.Add("Accept", "image/*;");
                    return await _WebClient.DownloadDataTaskAsync(Url);
                }
                catch { return null; }
            }
        }

        public bool MobileView;

        public void SetMobileView(bool Toggle)
        {
            UserAgent = Toggle ? UserAgentGenerator.BuildMobileUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}") : UserAgentGenerator.BuildUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}");
            MobileView = Toggle;
            UserAgentData = new WebUserAgentMetaData
            {
                Brands = new List<WebUserAgentBrand>
                {
                    new WebUserAgentBrand
                    {
                        Brand = "SLBr",
                        Version = ReleaseVersion.Split('.')[0]
                    },
                    new WebUserAgentBrand
                    {
                        Brand = "Chromium",
                        Version = Cef.ChromiumVersion.Split('.')[0]
                    }
                },
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
        public int ShowFavouritesBar;
        public int TabAlignment;
        public bool CompactTab;
        public void SetAppearance(Theme _Theme, int _TabAlignment, bool _CompactTab, bool _AllowHomeButton, bool _AllowTranslateButton, bool _AllowReaderModeButton, int _ShowExtensionButton, int _ShowFavouritesBar, bool _AllowQRButton, bool _AllowWebEngineButton)
        {
            AllowHomeButton = _AllowHomeButton;
            AllowTranslateButton = _AllowTranslateButton;
            AllowReaderModeButton = _AllowReaderModeButton;
            AllowQRButton = _AllowQRButton;
            AllowWebEngineButton = _AllowWebEngineButton;
            ShowExtensionButton = _ShowExtensionButton;
            ShowFavouritesBar = _ShowFavouritesBar;
            TabAlignment = _TabAlignment;
            CompactTab = _CompactTab;

            GlobalSave.Set("TabAlignment", TabAlignment);
            GlobalSave.Set("CompactTab", CompactTab);
            GlobalSave.Set("TranslateButton", AllowTranslateButton);
            GlobalSave.Set("HomeButton", AllowHomeButton);
            GlobalSave.Set("ReaderButton", AllowReaderModeButton);
            GlobalSave.Set("QRButton", AllowQRButton);
            GlobalSave.Set("WebEngineButton", AllowWebEngineButton);
            GlobalSave.Set("ExtensionButton", ShowExtensionButton);
            GlobalSave.Set("FavouritesBar", ShowFavouritesBar);

            CurrentTheme = _Theme;
            GlobalSave.Set("Theme", CurrentTheme.Name);

            //FontColor = new SolidColorBrush(_Theme.FontColor);

            int IconSize = 40;
            int DPI = 95;
            TextBlock _TextBlock = new TextBlock
            {
                FontFamily = IconFont,
                Text = "\uEC6C",
                Width = IconSize,
                Height = IconSize,
                FontSize = IconSize,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = CurrentTheme.DarkWebPage ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                Margin = new Thickness(1, 2, 0, 0)
            };
            RenderTargetBitmap RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            PngBitmapEncoder Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                TabIcon = _BitmapImage;
            }

            _TextBlock.Text = "\uEA90";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                PDFTabIcon = _BitmapImage;
            }

            _TextBlock.Text = "\uE727";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                PrivateIcon = _BitmapImage;
            }

            _TextBlock.Text = "\ue767";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                AudioIcon = _BitmapImage;
            }

            _TextBlock.Text = "\uE713";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                SettingsTabIcon = _BitmapImage;
            }

            _TextBlock.Text = "\ue81c";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                HistoryTabIcon = _BitmapImage;
            }

            _TextBlock.Text = "\ue896";
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                DownloadsTabIcon = _BitmapImage;
            }

            _TextBlock.Text = "\uEC0A";
            _TextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3AE872"));
            RenderBitmap = new RenderTargetBitmap(IconSize, IconSize, DPI, DPI, PixelFormats.Pbgra32);
            _TextBlock.Measure(new Size(IconSize, IconSize));
            _TextBlock.Arrange(new Rect(new Size(IconSize, IconSize)));
            RenderBitmap.Render(_TextBlock);
            Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(RenderBitmap));
            using (MemoryStream Stream = new MemoryStream())
            {
                Encoder.Save(Stream);
                Stream.Seek(0, SeekOrigin.Begin);

                BitmapImage _BitmapImage = new BitmapImage();
                _BitmapImage.BeginInit();
                _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                _BitmapImage.StreamSource = Stream;
                _BitmapImage.EndInit();
                if (_BitmapImage.CanFreeze)
                    _BitmapImage.Freeze();

                UnloadedIcon = _BitmapImage;
            }

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

        /*ZoomIn = 40,
        ZoomOut = 41,
        ZoomReset = 42,*/

        HardRefresh = 50,
        ClearCacheHardRefresh = 51,

        ToggleCompactTabs = 55,
        InstallWebApp = 56,
        QR = 57,
        SwitchWebEngine = 58,
        Translate = 59
    }

    public class ActionStorage : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public ActionStorage(string _Name, string _Arguments, string _Tooltip, bool _Toggle = false)
        {
            Name = _Name;
            Arguments = _Arguments;
            Tooltip = _Tooltip;
            Toggle = _Toggle;
        }

        public string Name
        {
            get { return DName; }
            set
            {
                DName = value;
                RaisePropertyChanged("Name");
            }
        }
        public string Arguments
        {
            get { return DArguments; }
            set
            {
                DArguments = value;
                RaisePropertyChanged("Arguments");
            }
        }
        public string Tooltip
        {
            get { return DTooltip; }
            set
            {
                DTooltip = value;
                RaisePropertyChanged("Tooltip");
            }
        }
        public bool Toggle
        {
            get { return DToggle; }
            set
            {
                DToggle = value;
                RaisePropertyChanged("Toggle");
            }
        }

        private string DName { get; set; }
        private string DArguments { get; set; }
        private string DTooltip { get; set; }
        private bool DToggle { get; set; }
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
            if (!Url.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            if (!Url.EndsWith(Suffix, StringComparison.Ordinal)) return false;
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
        public const string GetFaviconScript = @"(function() { var links = document.getElementsByTagName('link'); for (var i = 0; i < links.length; i++) { var rel = links[i].getAttribute('rel'); if (rel && rel.toLowerCase().indexOf('icon') !== -1) { return links[i].href; } } return ''; })();";

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
        if (socialWords.some(word => text.includes(word))) {
            el.remove();
        }
    });

  function isBlacklisted(node) {
    if (node.nodeType !== Node.ELEMENT_NODE) return false;
    //if (!root.contains(node)) return false;
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
        for (const child of node.childNodes) {
          fragment.appendChild(cleanNode(child));
        }
        return fragment;
      }

      const el = document.createElement(tag);
      if (allowedAttrs[tag]) {
        for (const attr of allowedAttrs[tag]) {
          if (node.hasAttribute(attr)) {
            el.setAttribute(attr, node.getAttribute(attr));
          }
        }
      }
      for (const child of node.childNodes) {
        el.appendChild(cleanNode(child));
      }
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
        
        public const string CefAudioScript = @"(function () {
  if (window.__cef_audio__) return;
  window.__cef_audio__ = true;

  let lastState = null;
  let checkScheduled = false;
  let observer = null;
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
      if (window.__cef_audio_ctxs.length > 0) {
        foundMedia = true;
      }
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
      if (checksWithoutMedia > 20 && observer) {
        observer.disconnect();
        observer = null;
        console.log(""Cef audio monitor: MutationObserver auto-disabled (no media found)."");
      }
    } else {
      checksWithoutMedia = 0;
    }
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
    observer = new MutationObserver(scheduleCheck);
    observer.observe(document.body, { childList: true, subtree: true });
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
          case ""history"":
            UpdateList(value);
            break;
          case ""downloads"":
            UpdateList(value);
            break;
        }
    },
    downloads: function() {
        engine.postMessage({type:""Internal"",function:'Downloads'});
    },
    history: function() {
        engine.postMessage({type:""Internal"",function:'History'});
    },
    openDownload: function(num) {
        engine.postMessage({type:""Internal"",function:'OpenDownload',variable:num});
    },
    cancelDownload: function(num) {
        engine.postMessage({type:""Internal"",function:'CancelDownload',variable:num});
    },
    clearHistory: function(num) {
        engine.postMessage({type:""Internal"",function:'ClearHistory'});
    },
    search: function(val) {
        engine.postMessage({type:""Internal"",function:'Search',variable:val});
    }
};";
        public const string NotificationPolyfill = @"(function () {
  if (window.__slbr_notification__) return;
  window.__slbr_notification__ = true;
class Notification {
constructor(title, options = {}) {
    if(Notification.permission!=='granted'){throw new Error(""Notification permission not granted."");}
        this.onclick = null;
        this.onshow = null;
        this.onclose = null;
        this.onerror = null;
        if(typeof engine !== 'undefined' && typeof engine.postMessage === 'function') {
            let packageSet=new Set();packageSet.add(title).add(options);
            engine.postMessage({type:""Notification"",data:JSON.stringify([...packageSet])});
        }
        setTimeout(() => {if(typeof this.onshow==='function')this.onshow();},0);
        if(Notification.autoClose){setTimeout(()=>this.close(),Notification.autoClose);}
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
    if (text==='Add to Chrome'||text==='Remove from Chrome')
      button.textContent=text.replace('Chrome','SLBr')
  }
}
scanButton();
new MutationObserver(scanButton).observe(document.body,{attributes:true,childList:true,subtree:true});
})();";
        public const string YouTubeSkipAdScript = @"(function() {
  if (window.__slbr_youtube_ad__) return;
  window.__slbr_youtube_ad__ = true;

    let lastSkipTime = 0;
    const SLBradObserver = new MutationObserver(() => {
        const now = Date.now();
        const skipButtons = document.querySelectorAll('.ytp-ad-skip-button, .ytp-ad-skip-button-modern');
        for (const btn of skipButtons) {
            btn.click();
        }
        if (now - lastSkipTime < 2000) return;
        const adVideo = document.querySelector('div.ad-showing video');
        if (adVideo && adVideo.duration && adVideo.currentTime < adVideo.duration - 0.5) {
            adVideo.currentTime = adVideo.duration;
            lastSkipTime = now;
        }
    });

    const SLBrABinterval = setInterval(() => {
        if (document.readyState === ""complete"" && document.body) {
            SLBradObserver.observe(document.body, { childList: true, subtree: true });
            clearInterval(SLBrABinterval);
        }
    }, 500);
})();";
        //public const string YouTubeHideAdScript = "var SLBrYTStyle=document.createElement('style');SLBrYTStyle.textContent=`ytd-action-companion-ad-renderer,ytd-display-ad-renderer,ytd-video-masthead-ad-advertiser-info-renderer,ytd-video-masthead-ad-primary-video-renderer,ytd-in-feed-ad-layout-renderer,ytd-ad-slot-renderer,yt-about-this-ad-renderer,yt-mealbar-promo-renderer,ytd-statement-banner-renderer,ytd-ad-slot-renderer,ytd-in-feed-ad-layout-renderer,ytd-banner-promo-renderer-backgroundstatement-banner-style-type-compact,.ytd-video-masthead-ad-v3-renderer,div#root.style-scope.ytd-display-ad-renderer.yt-simple-endpoint,div#sparkles-container.style-scope.ytd-promoted-sparkles-web-renderer,div#main-container.style-scope.ytd-promoted-video-renderer,div#player-ads.style-scope.ytd-watch-flexy,ad-slot-renderer,ytm-promoted-sparkles-web-renderer,masthead-ad,tp-yt-iron-overlay-backdrop,#masthead-ad{display:none !important;}`;document.head.appendChild(SLBrYTStyle);";
        //public const string ScrollCSS = "var scstyle=document.createElement('style');scstyle.textContent=`::-webkit-scrollbar {width:15px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color:transparent;}::-webkit-scrollbar-thumb {height:56px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color: gainsboro;transition:background-color 0.5s;}::-webkit-scrollbar-thumb:hover{background-color:gray;transition:background-color 0.5s;}::-webkit-scrollbar-corner{background-color:transparent;}`;document.head.append(scstyle);";
        public const string ScrollScript = @"!function(){var s,i,c,a,o={frameRate:150,animationTime:400,stepSize:100,pulseAlgorithm:!0,pulseScale:4,pulseNormalize:1,accelerationDelta:50,accelerationMax:3,keyboardSupport:!0,arrowScroll:50,fixedBackground:!0,excluded:""""},p=o,u=!1,d=!1,l={x:0,y:0},f=!1,m=document.documentElement,h=[],v={left:37,up:38,right:39,down:40,spacebar:32,pageup:33,pagedown:34,end:35,home:36},y={37:1,38:1,39:1,40:1};function b(){if(!f&&document.body){f=!0;var e=document.body,t=document.documentElement,o=window.innerHeight,n=e.scrollHeight;if(m=0<=document.compatMode.indexOf(""CSS"")?t:e,s=e,p.keyboardSupport&&Y(""keydown"",D),top!=self)d=!0;else if(o<n&&(e.offsetHeight<=o||t.offsetHeight<=o)){var r,a=document.createElement(""div"");a.style.cssText=""position:absolute; z-index:-10000; top:0; left:0; right:0; height:""+m.scrollHeight+""px"",document.body.appendChild(a),c=function(){r||(r=setTimeout(function(){u||(a.style.height=""0"",a.style.height=m.scrollHeight+""px"",r=null)},500))},setTimeout(c,10),Y(""resize"",c);if((i=new R(c)).observe(e,{attributes:!0,childList:!0,characterData:!1}),m.offsetHeight<=o){var l=document.createElement(""div"");l.style.clear=""both"",e.appendChild(l)}}p.fixedBackground||u||(e.style.backgroundAttachment=""scroll"",t.style.backgroundAttachment=""scroll"")}}var g=[],S=!1,x=Date.now();function k(d,f,m){var e,t;if(e=0<(e=f)?1:-1,t=0<(t=m)?1:-1,(l.x!==e||l.y!==t)&&(l.x=e,l.y=t,g=[],x=0),1!=p.accelerationMax){var o=Date.now()-x;if(o<p.accelerationDelta){var n=(1+50/o)/2;1<n&&(n=Math.min(n,p.accelerationMax),f*=n,m*=n)}x=Date.now()}if(g.push({x:f,y:m,lastX:f<0?.99:-.99,lastY:m<0?.99:-.99,start:Date.now()}),!S){var r=q(),h=d===r||d===document.body;null==d.$scrollBehavior&&function(e){var t=M(e);if(null==B[t]){var o=getComputedStyle(e,"""")[""scroll-behavior""];B[t]=""smooth""==o}return B[t]}(d)&&(d.$scrollBehavior=d.style.scrollBehavior,d.style.scrollBehavior=""auto"");var w=function(e){for(var t=Date.now(),o=0,n=0,r=0;r<g.length;r++){var a=g[r],l=t-a.start,i=l>=p.animationTime,c=i?1:l/p.animationTime;p.pulseAlgorithm&&(c=F(c));var s=a.x*c-a.lastX>>0,u=a.y*c-a.lastY>>0;o+=s,n+=u,a.lastX+=s,a.lastY+=u,i&&(g.splice(r,1),r--)}h?window.scrollBy(o,n):(o&&(d.scrollLeft+=o),n&&(d.scrollTop+=n)),f||m||(g=[]),g.length?j(w,d,1e3/p.frameRate+1):(S=!1,null!=d.$scrollBehavior&&(d.style.scrollBehavior=d.$scrollBehavior,d.$scrollBehavior=null))};j(w,d,0),S=!0}}function e(e){f||b();var t=e.target;if(e.defaultPrevented||e.ctrlKey)return!0;if(N(s,""embed"")||N(t,""embed"")&&/\.pdf/i.test(t.src)||N(s,""object"")||t.shadowRoot)return!0;var o=-e.wheelDeltaX||e.deltaX||0,n=-e.wheelDeltaY||e.deltaY||0;o||n||(n=-e.wheelDelta||0),1===e.deltaMode&&(o*=40,n*=40);var r=z(t);return r?!!function(e){if(!e)return;h.length||(h=[e,e,e]);e=Math.abs(e),h.push(e),h.shift(),clearTimeout(a),a=setTimeout(function(){try{localStorage.SS_deltaBuffer=h.join("","")}catch(e){}},1e3);var t=120<e&&P(e);return!P(120)&&!P(100)&&!t}(n)||(1.2<Math.abs(o)&&(o*=p.stepSize/120),1.2<Math.abs(n)&&(n*=p.stepSize/120),k(r,o,n),e.preventDefault(),void C()):!d||!W||(Object.defineProperty(e,""target"",{value:window.frameElement}),parent.wheel(e))}function D(e){var t=e.target,o=e.ctrlKey||e.altKey||e.metaKey||e.shiftKey&&e.keyCode!==v.spacebar;document.body.contains(s)||(s=document.activeElement);var n=/^(button|submit|radio|checkbox|file|color|image)$/i;if(e.defaultPrevented||/^(textarea|select|embed|object)$/i.test(t.nodeName)||N(t,""input"")&&!n.test(t.type)||N(s,""video"")||function(e){var t=e.target,o=!1;if(-1!=document.URL.indexOf(""www.youtube.com/watch""))do{if(o=t.classList&&t.classList.contains(""html5-video-controls""))break}while(t=t.parentNode);return o}(e)||t.isContentEditable||o)return!0;if((N(t,""button"")||N(t,""input"")&&n.test(t.type))&&e.keyCode===v.spacebar)return!0;if(N(t,""input"")&&""radio""==t.type&&y[e.keyCode])return!0;var r=0,a=0,l=z(s);if(!l)return!d||!W||parent.keydown(e);var i=l.clientHeight;switch(l==document.body&&(i=window.innerHeight),e.keyCode){case v.up:a=-p.arrowScroll;break;case v.down:a=p.arrowScroll;break;case v.spacebar:a=-(e.shiftKey?1:-1)*i*.9;break;case v.pageup:a=.9*-i;break;case v.pagedown:a=.9*i;break;case v.home:l==document.body&&document.scrollingElement&&(l=document.scrollingElement),a=-l.scrollTop;break;case v.end:var c=l.scrollHeight-l.scrollTop-i;a=0<c?c+10:0;break;case v.left:r=-p.arrowScroll;break;case v.right:r=p.arrowScroll;break;default:return!0}k(l,r,a),e.preventDefault(),C()}function t(e){s=e.target}var n,r,M=(n=0,function(e){return e.uniqueID||(e.uniqueID=n++)}),E={},T={},B={};function C(){clearTimeout(r),r=setInterval(function(){E=T=B={}},1e3)}function H(e,t,o){for(var n=o?E:T,r=e.length;r--;)n[M(e[r])]=t;return t}function z(e){var t=[],o=document.body,n=m.scrollHeight;do{var r=(!1?E:T)[M(e)];if(r)return H(t,r);if(t.push(e),n===e.scrollHeight){var a=O(m)&&O(o)||X(m);if(d&&L(m)||!d&&a)return H(t,q())}else if(L(e)&&X(e))return H(t,e)}while(e=e.parentElement)}function L(e){return e.clientHeight+10<e.scrollHeight}function O(e){return""hidden""!==getComputedStyle(e,"""").getPropertyValue(""overflow-y"")}function X(e){var t=getComputedStyle(e,"""").getPropertyValue(""overflow-y"");return""scroll""===t||""auto""===t}function Y(e,t,o){window.addEventListener(e,t,o||!1)}function A(e,t,o){window.removeEventListener(e,t,o||!1)}function N(e,t){return e&&(e.nodeName||"""").toLowerCase()===t.toLowerCase()}if(window.localStorage&&localStorage.SS_deltaBuffer)try{h=localStorage.SS_deltaBuffer.split("","")}catch(e){}function K(e,t){return Math.floor(e/t)==e/t}function P(e){return K(h[0],e)&&K(h[1],e)&&K(h[2],e)}var $,j=window.requestAnimationFrame||window.webkitRequestAnimationFrame||window.mozRequestAnimationFrame||function(e,t,o){window.setTimeout(e,o||1e3/60)},R=window.MutationObserver||window.WebKitMutationObserver||window.MozMutationObserver,q=($=document.scrollingElement,function(){if(!$){var e=document.createElement(""div"");e.style.cssText=""height:10000px;width:1px;"",document.body.appendChild(e);var t=document.body.scrollTop;document.documentElement.scrollTop,window.scrollBy(0,3),$=document.body.scrollTop!=t?document.body:document.documentElement,window.scrollBy(0,-3),document.body.removeChild(e)}return $});function V(e){var t;return((e*=p.pulseScale)<1?e-(1-Math.exp(-e)):(e-=1,(t=Math.exp(-1))+(1-Math.exp(-e))*(1-t)))*p.pulseNormalize}function F(e){return 1<=e?1:e<=0?0:(1==p.pulseNormalize&&(p.pulseNormalize/=V(1)),V(e))}
try{window.addEventListener(""test"",null,Object.defineProperty({},""passive"",{get:function(){ee=!0}}))}catch(e){}var te=!!ee&&{passive:!1},oe=""onwheel""in document.createElement(""div"")?""wheel"":""mousewheel"";function ne(e){for(var t in e)o.hasOwnProperty(t)&&(p[t]=e[t])}oe&&(Y(oe,e,te),Y(""mousedown"",t),Y(""load"",b)),ne.destroy=function(){i&&i.disconnect(),A(oe,e),A(""mousedown"",t),A(""keydown"",D),A(""resize"",c),A(""load"",b)},window.SmoothScrollOptions&&ne(window.SmoothScrollOptions),""function""==typeof define&&define.amd?define(function(){return ne}):""object""==typeof exports?module.exports=ne:window.SmoothScroll=ne}();
SmoothScroll({animationTime:400,stepSize:100,accelerationDelta:50,accelerationMax:3,keyboardSupport:true,arrowScroll:50,pulseAlgorithm:true,pulseScale:4,pulseNormalize:1,touchpadSupport:false,fixedBackground:true,excluded:''});";
        public const string ExtensionScript = "var rect=document.body.getBoundingClientRect();engine.postMessage({width:rect.width+16,height:rect.height+40});";
        
        public const string OpenSearchScript = @"(function(){let link=document.querySelector('link[rel=""search""][type=""application/opensearchdescription+xml""]');if (link){engine.postMessage({type:'OpenSearch',url:link.href,name:link.title||''});}})();";

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
        /*warn:function(){},
        error:function(){},*/
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
        public const string ForceLazyLoad = @"
    function applyLazyLoading(el) {
        if (el.tagName !== 'IMG' && el.tagName !== 'IFRAME') return;

        const originalSrc = el.getAttribute('src');
        if (!originalSrc || el.dataset.lazyfixed) return;
        el.dataset.lazyfixed = '1';
        el.setAttribute('loading', 'lazy');

        el.src = '';
        requestAnimationFrame(() => {
            el.setAttribute('src', originalSrc);
        });
    }

    function initObserver() {
        document.querySelectorAll('img:not([loading]), iframe:not([loading])').forEach(applyLazyLoading);

        const SLBrlazyobserver = new MutationObserver((mutations) => {
            for (const mutation of mutations) {
                if (mutation.type === 'childList') {
                    for (const node of mutation.addedNodes) {
                        if (node.nodeType !== 1) continue;
                        if (node.tagName === 'IMG' || node.tagName === 'IFRAME') {
                            applyLazyLoading(node);
                        } else {
                            node.querySelectorAll?.('img:not([loading]), iframe:not([loading])').forEach(applyLazyLoading);
                        }
                    }
                }
            }
        });

        const target = document.documentElement || document.body;
        if (target) {
            SLBrlazyobserver.observe(target, {
                childList: true,
                subtree: true
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initObserver);
    } else {
        initObserver();
    }";

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
            if (!trimmed || trimmed.length <= 1 || trimmed.startsWith('<') || trimmed.includes('{{') || trimmed.includes('}}') || /^[\\s<>{{}}\\/]+$/.test(trimmed))
                return NodeFilter.FILTER_REJECT;
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

        public const string CheckNativeDarkModeScript = @"(function() {{
function detectDarkAppearance() {
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
return avg < 110;
}

if (detectDarkAppearance()) return 0;
return 1;
}})();";
    }

    public class WebAppManifest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("short_name")] public string ShortName { get; set; }
        [JsonPropertyName("start_url")] public string StartUrl { get; set; } = "/";
        [JsonPropertyName("display")] public string Display { get; set; } = "standalone";
        [JsonPropertyName("background_color")] public string BackgroundColor { get; set; }
        [JsonPropertyName("theme_color")] public string ThemeColor { get; set; }
        [JsonPropertyName("icons")] public List<ManifestIcon> Icons { get; set; } = new();
    }

    public class ManifestIcon
    {
        [JsonPropertyName("src")] public string Source { get; set; }
        [JsonPropertyName("sizes")] public string Sizes { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("purpose")] public string Purpose { get; set; }
    }
}
