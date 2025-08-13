using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Emulation;
using CefSharp.Enums;
using CefSharp.SchemeHandler;
using CefSharp.Wpf.HwndHost;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SLBr
{
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
        public string Name;
        public string Host;
        public string SearchUrl;
        public string SuggestUrl;
    }

    public class OmniSuggestion
    {
        public string Text { get; set; }
        public string Result { get; set; }
        public string Icon { get; set; }
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

        private int PID;
        public int ID
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

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App Instance;

        //public const string AMPEndpoint = "https://acceleratedmobilepageurl.googleapis.com/v1/ampUrls:batchGet";

        public List<MainWindow> AllWindows = new List<MainWindow>();
        public List<SearchProvider> SearchEngines = new List<SearchProvider>();
        public SearchProvider DefaultSearchProvider;
        public List<Theme> Themes = new List<Theme>()
        {
            new Theme("Light", Colors.White, Colors.WhiteSmoke, Colors.Gainsboro, Colors.Gray, Colors.Black, (Color)ColorConverter.ConvertFromString("#3399FF"), false, false),
            new Theme("Dark", (Color)ColorConverter.ConvertFromString("#202225"), (Color)ColorConverter.ConvertFromString("#2F3136"), (Color)ColorConverter.ConvertFromString("#36393F"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3399FF"), true, true),
            new Theme("Purple", (Color)ColorConverter.ConvertFromString("#191025"), (Color)ColorConverter.ConvertFromString("#251C31"), (Color)ColorConverter.ConvertFromString("#2B2237"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#934CFE"), true, true),
            new Theme("Green", (Color)ColorConverter.ConvertFromString("#163C2C"), (Color)ColorConverter.ConvertFromString("#18352B"), (Color)ColorConverter.ConvertFromString("#1A3029"), Colors.Gainsboro, Colors.White, (Color)ColorConverter.ConvertFromString("#3AE872"), true, true)
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

        public string Username = "Default";
        public string UserApplicationWindowsPath;
        public string UserApplicationDataPath;
        public string ExecutablePath;
        public string ExtensionsPath;
        public string ResourcesPath;
        public string CdnPath;

        bool AppInitialized;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        private ObservableCollection<ActionStorage> PrivateFavourites = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> Favourites
        {
            get { return PrivateFavourites; }
            set
            {
                PrivateFavourites = value;
                RaisePropertyChanged("Favourites");
            }
        }
        private ObservableCollection<ActionStorage> PrivateHistory = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> History
        {
            get { return PrivateHistory; }
            set
            {
                PrivateHistory = value;
                RaisePropertyChanged("History");
            }
        }
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
                            foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null)) {
                                BrowserView.ExtensionsButton.Visibility = Visibility.Visible;
                                BrowserView.ExtensionsMenu.ItemsSource = Extensions;
                            }
                        }
                        break;
                    case 2:
                        foreach (MainWindow _Window in AllWindows)
                        {
                            foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null)) {
                                BrowserView.ExtensionsButton.Visibility = Visibility.Collapsed;
                                BrowserView.ExtensionsMenu.ItemsSource = Extensions;
                            }
                        }
                        break;
                }
            }
        }


        public void AddHistory(string Url, string Title)
        {
            ActionStorage HistoryEntry = new ActionStorage(Title, $"4<,>{Url}", Url);
            if (History.Contains(HistoryEntry))
                History.Remove(HistoryEntry);
            History.Insert(0, HistoryEntry);
        }
        public ObservableCollection<DownloadEntry> VisibleDownloads = new ObservableCollection<DownloadEntry>();
        public Dictionary<int, DownloadItem> Downloads = new Dictionary<int, DownloadItem>();
        public void UpdateDownloadItem(DownloadItem Item)
        {
            Downloads[Item.Id] = Item;
            Dispatcher.Invoke(() =>
            {
                foreach (MainWindow _Window in AllWindows)
                    _Window.TaskbarProgress.ProgressValue = Item.IsComplete ? 0 : Item.PercentComplete / 100.0;
                DownloadEntry _Entry = VisibleDownloads.FirstOrDefault(d => d.ID == Item.Id);
                if (_Entry != null)
                {
                    if (string.IsNullOrEmpty(_Entry.FileName))
                    {
                        _Entry.FileName = Path.GetFileName(Item.FullPath);
                        if (!string.IsNullOrEmpty(_Entry.FileName))
                        {
                            if (_Entry.FileName.EndsWith(".zip") || _Entry.FileName.EndsWith(".rar") || _Entry.FileName.EndsWith(".7z") ||
                                _Entry.FileName.EndsWith(".tar.gz") || _Entry.FileName.EndsWith(".tgz"))
                                _Entry.Icon = "\uF012";
                            else if (_Entry.FileName.EndsWith(".gif"))
                                _Entry.Icon = "\uF4A9";
                            else if (_Entry.FileName.EndsWith(".txt"))
                                _Entry.Icon = "\uF000";
                            else if (_Entry.FileName.EndsWith(".blend") || _Entry.FileName.EndsWith(".obj") || _Entry.FileName.EndsWith(".fbx") || _Entry.FileName.EndsWith(".max") ||
                                _Entry.FileName.EndsWith(".stl") || _Entry.FileName.EndsWith(".x3d") || _Entry.FileName.EndsWith(".3ds") || _Entry.FileName.EndsWith(".dae") ||
                                _Entry.FileName.EndsWith(".glb") || _Entry.FileName.EndsWith(".gltf") || _Entry.FileName.EndsWith(".ply"))
                                _Entry.Icon = "\uF158";
                            else if (_Entry.FileName.EndsWith(".mp3") || _Entry.FileName.EndsWith(".mp2"))
                                _Entry.Icon = "\uEA69";
                            else if (_Entry.FileName.EndsWith(".mp4") || _Entry.FileName.EndsWith(".avi") || _Entry.FileName.EndsWith(".ogg") || _Entry.FileName.EndsWith(".webm") ||
                                _Entry.FileName.EndsWith(".mov") || _Entry.FileName.EndsWith(".mpej") || _Entry.FileName.EndsWith(".wmv") || _Entry.FileName.EndsWith(".h264") || _Entry.FileName.EndsWith(".mkv"))
                                _Entry.Icon = "\uE786";
                            else if (_Entry.FileName.EndsWith(".pdf"))
                                _Entry.Icon = "\uEA90";
                            else if (_Entry.FileName.EndsWith(".png") || _Entry.FileName.EndsWith(".jpg") || _Entry.FileName.EndsWith(".jpeg") || _Entry.FileName.EndsWith(".avif") ||
                                _Entry.FileName.EndsWith(".svg") || _Entry.FileName.EndsWith(".webp") || _Entry.FileName.EndsWith(".jfif") || _Entry.FileName.EndsWith(".bmp"))
                                _Entry.Icon = "\uE91B";
                            else
                                _Entry.Icon = "\uE8A5";
                            MainWindow Current = CurrentFocusedWindow();
                            if (Current != null)
                            {
                                Browser BrowserView = Current.Tabs[Current.TabsUI.SelectedIndex].Content;
                                if (BrowserView != null)
                                    BrowserView.OpenDownloadsButton.OpenPopup();
                            }
                        }
                    }
                    _Entry.PercentComplete = Item.PercentComplete;
                    if (Item.IsComplete)
                    {
                        _Entry.FormattedProgress = $"{FormatBytes(Item.TotalBytes)} - Complete";
                        _Entry.Open = Visibility.Visible;
                        _Entry.Stop = Visibility.Collapsed;
                        _Entry.Progress = Visibility.Collapsed;
                    }
                    else if (Item.IsCancelled)
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
                    VisibleDownloads.Add(new DownloadEntry { ID = Item.Id });
            });
        }
        static string[] FileSizes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private static string FormatBytes(long Bytes, bool ContainSizes = true)
        {
            if (Bytes == 0) return "0 Byte";
            int k = 1000;
            int i = (int)Math.Floor(Math.Log(Bytes) / Math.Log(k));
            string Output = (Bytes / Math.Pow(k, i)).ToString("F2");
            if (ContainSizes)
                Output += $" {FileSizes[i]}";
            return Output;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Instance = this;
            InitializeApp();
        }

        static Mutex _Mutex;
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public string UserAgent;
        public string UserAgentBrandsString;
        public UserAgentMetadata UserAgentData;

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

        public static OmniSuggestion GenerateSuggestion(string Text, string Type)
        {
            OmniSuggestion Suggestion = new OmniSuggestion { Text = Text };
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
                        var result = new DataTable().Compute(Text, null)?.ToString();
                        Suggestion.Result = $"= {result}";
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

                        string LanguageCode = "";
                        if (LanguageInput.Length == 2)
                            LanguageCode = LanguageInput;
                        else
                            LanguageCode = AllLocales.FirstOrDefault(x => x.Value.Contains(LanguageInput, StringComparison.OrdinalIgnoreCase)).Key;

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
                    string Location = Regex.Replace(Text, @"^weather(\s+in)?\s+", "", RegexOptions.IgnoreCase).Trim();
                    try
                    {
                        using HttpResponseMessage Response = await MiniHttpClient.GetAsync($"https://wttr.in/{Uri.EscapeDataString(Location)}?format=1", Token);
                        string Result = (await Response.Content.ReadAsStringAsync()).Trim();
                        Token.ThrowIfCancellationRequested();
                        if (Result.Length > 0)
                        {
                            Match TemperatureMatch = Regex.Match(Result, @"[-+]?\d+\s*°C");
                            string Temperature = TemperatureMatch.Success ? TemperatureMatch.Value : "";
                            string Description = Result switch
                            {
                                string s when s.StartsWith("🌤", StringComparison.Ordinal) => "Mostly sunny",
                                string s when s.StartsWith("☀️", StringComparison.Ordinal) => "Sunny",
                                string s when s.StartsWith("⛅", StringComparison.Ordinal) => "Partly cloudy",
                                string s when s.StartsWith("☁️", StringComparison.Ordinal) => "Cloudy",
                                string s when s.StartsWith("🌧", StringComparison.Ordinal) => "Rainy",
                                string s when s.StartsWith("🌨", StringComparison.Ordinal) => "Snowy",
                                string s when s.StartsWith("🌪", StringComparison.Ordinal) => "Storm",
                                _ => "Weather"
                            };

                            Suggestion.Result = $"{Temperature} | {Description}";
                        }
                        else
                        {
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

        private void InitializeApp()
        {
            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            string AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30}";
            string CommandLineUrl = "";
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--user=", StringComparison.Ordinal))
                {
                    Username = Flag.Replace("--user=", "").Replace(" ", "-");
                    if (Username != "Default")
                        AppUserModelID = "{ab11da56-fbdf-4678-916e-67e165b21f30-" + Username + "}";
                }
                else if (Flag == "--background")
                    Background = true;
                else
                {
                    if (Flag.StartsWith("--", StringComparison.Ordinal))
                        continue;
                    CommandLineUrl = Flag;
                }
            }
            SetCurrentProcessExplicitAppUserModelID(AppUserModelID);

            _Mutex = new Mutex(true, AppUserModelID);
            if (string.IsNullOrEmpty(CommandLineUrl))
            {
                if (!_Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Process OtherInstance = Utils.GetAlreadyRunningInstance(Process.GetCurrentProcess());
                    if (OtherInstance != null)
                        MessageHelper.SendDataMessage(OtherInstance, "Start<|>"+ Username);
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
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ReleaseVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Set Google API keys. See http://www.chromium.org/developers/how-tos/api-keys
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", SECRETS.GOOGLE_API_KEY);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", SECRETS.GOOGLE_DEFAULT_CLIENT_ID);
            Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", SECRETS.GOOGLE_DEFAULT_CLIENT_SECRET);

            UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username);
            UserApplicationWindowsPath = Path.Combine(UserApplicationDataPath, "Windows");
            ExecutablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            ExtensionsPath = Path.Combine(UserApplicationDataPath, "User Data", "Default", "Extensions");
            ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            CdnPath = Path.Combine(ResourcesPath, "cdn");

            InitializeSaves();

            if (Username != "Default")
            {
                string IconPath = Path.Combine(UserApplicationDataPath, $"Icon.png");
                if (!File.Exists(IconPath))
                {
                    ImageSource IconImage = GenerateProfileIcon("pack://application:,,,/Resources/SLBr.ico", Username.Substring(0, 1).ToUpper());
                    SaveImageSourceToFile(IconImage, IconPath);
                }
                Icon = BitmapFrame.Create(new Uri(IconPath, UriKind.Absolute));
            }

            InitializeUISaves(CommandLineUrl);

            if (Utils.IsAdministrator())
            {
                using (var CheckKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
                {
                    if (CheckKey.GetValue("SLBr") == null)
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
                        CheckKey.SetValue("SLBr", "Software\\Clients\\StartMenuInternet\\SLBr\\Capabilities");
                    }
                }
            }
            InitializeCEF();
            AppInitialized = true;
            if (!Background)
                ContinueBackgroundInitialization();
        }

        public void ContinueBackgroundInitialization()
        {
            foreach (MainWindow _Window in AllWindows)
            {
                _Window.WindowState = WindowState.Maximized;
                _Window.ShowInTaskbar = true;
                _Window.Show();
                _Window.Activate();
            }
            if (Utils.IsInternetAvailable() && bool.Parse(GlobalSave.Get("CheckUpdate")))
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
                            var ToastXML = new XmlDocument();
                            ToastXML.LoadXml(@$"<toast><visual><binding template=""ToastText02""><text id=""1"">New update available</text><text id=""2"">{NewVersion}</text></binding></visual></toast>");
                            ToastNotificationManager.CreateToastNotifier("SLBr").Show(new ToastNotification(ToastXML));
                        }
                    }
                    catch { }
                }
            }
            Background = false;
        }

        public static async Task DiscordWebhookSendInfo(string Content)
        {
            var Payload = new
            {
                content = Content,
                username = "SLBr Diagnostics"
            };

            //var Response = 
            await MiniHttpClient.PostAsync(SECRETS.DISCORD_WEBHOOK, new StringContent(JsonSerializer.Serialize(Payload), Encoding.UTF8, "application/json"));
            /*if (!Response.IsSuccessStatusCode)
            {
                string responseBody = await Response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Webhook Error] {Response.StatusCode}: {responseBody}");
            }*/
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));

            if (bool.Parse(GlobalSave.Get("SuppressError")))
            {
                Save();
                e.SetObserved();
            }
            else
                MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //MessageBox.Show(e.ExceptionObject.ToString());
            Exception _Exception = e.ExceptionObject as Exception;
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), _Exception.Message, _Exception.Source, _Exception.TargetSite, _Exception.StackTrace, _Exception.InnerException));

            if (bool.Parse(GlobalSave.Get("SuppressError")))
                Save();
            else
                MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), _Exception.Message, _Exception.Source, _Exception.TargetSite, _Exception.StackTrace, _Exception.InnerException));
        }

        public const string InternalJavascriptFunction = @"window.internal = {
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
          case ""background"":
            document.documentElement.style.backgroundImage = value;
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
    },
    background: function(val) {
        engine.postMessage({type:""Internal"",function:'Background'});
    }
};";

        const string ExceptionText = @"[SLBr] {0}
[CEF] {1}
[CPU Architecture] {2}

[Message] {3}
[Source] {4}

[Target Site] {5}

[Stack Trace] {6}

[Inner Exception] {7}";
        const string ReportExceptionText = @"**Automatic Report**
> - Version: `{0}`
> - CEF Version: `{1}`
> - CPU Architecture: `{2}`
> - Message: ```{3}```
> - Source: `{4} `
> - Target Site: `{5} `

Stack Trace: ```{6} ```

Inner Exception: ```{7} ```";
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (bool.Parse(GlobalSave.Get("SendDiagnostics")))
                DiscordWebhookSendInfo(string.Format(ReportExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));

            if (bool.Parse(GlobalSave.Get("SuppressError")))
            {
                Save();
                e.Handled = true;
            }
            else
                MessageBox.Show(string.Format(ExceptionText, ReleaseVersion, Cef.CefVersion, RuntimeInformation.ProcessArchitecture.ToString(), e.Exception.Message, e.Exception.Source, e.Exception.TargetSite, e.Exception.StackTrace, e.Exception.InnerException));
        }
        public int TrackersBlocked;
        public int AdsBlocked;

        public int AdBlock;
        public bool AMP;

        //https://chromium-review.googlesource.com/c/chromium/src/+/1265506
        public bool NeverSlowMode;

        public bool SkipAds;
        public bool ExternalFonts;

        public void SetExternalFonts(bool _ExternalFonts)
        {
            GlobalSave.Set("ExternalFonts", _ExternalFonts.ToString());
            ExternalFonts = _ExternalFonts;
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
            /*Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.Ads, Boolean ? ContentSettingValues.Block : ContentSettingValues.Default);
            });*/
            GlobalSave.Set("AdBlock", Type);
            AdBlock = Type;
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null))
                {
                    if (BrowserView.Chromium != null && BrowserView.Chromium.IsBrowserInitialized && BrowserView.Chromium.CanExecuteJavascriptInMainFrame)
                    {
                        using (var DevToolsClient = BrowserView.Chromium.GetDevToolsClient())
                        {
                            //DevToolsClient.Page.SetAdBlockingEnabledAsync(AdBlock != 0);
                            /*if (AdBlock == 2)
                                BrowserView.ToggleEfficientAdBlock(DevToolsClient, AdBlockAllowList.Has(Utils.FastHost(BrowserView.Address)));
                            else
                                BrowserView.ToggleEfficientAdBlock(DevToolsClient, false);*/
                            BrowserView.ToggleEfficientAdBlock(DevToolsClient, AdBlock == 2);
                        }
                    }
                }
            }
        }
        public void SetAMP(bool Toggle)
        {
            /*Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.Ads, Boolean ? ContentSettingValues.Block : ContentSettingValues.Default);
            });*/
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
                    string Template = Urls.Attribute("template")?.Value.Replace("{searchTerms}", "{0}");

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

        //public Dictionary<string, bool> PopupPermissionHosts = new Dictionary<string, bool>();

        private ObservableCollection<ActionStorage> PrivateLanguages = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> Languages
        {
            get { return PrivateLanguages; }
            set
            {
                PrivateLanguages = value;
                RaisePropertyChanged("Languages");
            }
        }
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
            //return "\xE8C1";
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
                            new SearchProvider { Name = "Ecosia", Host = "ecosia.org", SearchUrl = "https://www.ecosia.org/search?q={0}", SuggestUrl = "https://ac.ecosia.org/autocomplete?type=list&q={0}" },
                            new SearchProvider { Name = "Brave Search", Host = "search.brave.com", SearchUrl = "https://search.brave.com/search?q={0}", SuggestUrl = "https://search.brave.com/api/suggest?q={0}" },
                            new SearchProvider { Name = "DuckDuckGo", Host = "duckduckgo.com", SearchUrl = "https://duckduckgo.com/?q={0}", SuggestUrl = "http://duckduckgo.com/ac/?type=list&q={0}" },
                            new SearchProvider { Name = "Yahoo Search", Host = "search.yahoo.com", SearchUrl = "https://search.yahoo.com/search?p=e{0}", SuggestUrl = "https://ff.search.yahoo.com/gossip?output=fxjson&command={0}" },
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
                    new SearchProvider { Name = "Yahoo Search", Host = "search.yahoo.com", SearchUrl = "https://search.yahoo.com/search?p=e{0}", SuggestUrl = "https://ff.search.yahoo.com/gossip?output=fxjson&command={0}" },
                };

                /*SearchEngines = new List<string>() {
                    "https://google.com/search?q={0}",
                    "https://bing.com/search?q={0}",
                    "https://www.ecosia.org/search?q={0}",
                    "https://duckduckgo.com/?q={0}",
                    //"https://search.brave.com/search?q={0}",
                    //"https://search.yahoo.com/search?p={0}",
                    //"https://yandex.com/search/?text={0}","
                };*/
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

            SetGoogleSafeBrowsing(bool.Parse(GlobalSave.Get("GoogleSafeBrowsing", true.ToString())));
            SetMobileView(bool.Parse(GlobalSave.Get("MobileView", false.ToString())));

            if (!GlobalSave.Has("CheckUpdate"))
                GlobalSave.Set("CheckUpdate", true);
            if (!GlobalSave.Has("PrivateTabs"))
                GlobalSave.Set("PrivateTabs", false);
            if (!GlobalSave.Has("QuickImage"))
                GlobalSave.Set("QuickImage", true);
            if (!GlobalSave.Has("SuppressError"))
                GlobalSave.Set("SuppressError", true);
            if (!GlobalSave.Has("EnhanceImage"))
                GlobalSave.Set("EnhanceImage", false);
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
            /*if (!GlobalSave.Has("IPFS"))
                GlobalSave.Set("IPFS", true);
            if (!GlobalSave.Has("Wayback"))
                GlobalSave.Set("Wayback", true);
            if (!GlobalSave.Has("Gemini"))
                GlobalSave.Set("Gemini", true);
            if (!GlobalSave.Has("Gopher"))
                GlobalSave.Set("Gopher", true);*/
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
            if (!GlobalSave.Has("AdaptiveTheme"))
                GlobalSave.Set("AdaptiveTheme", false);

            if (!GlobalSave.Has("ScreenshotFormat"))
                GlobalSave.Set("ScreenshotFormat", 0);

            if (!GlobalSave.Has("Favicons"))
                GlobalSave.Set("Favicons", true);
            if (!GlobalSave.Has("SmoothScroll"))
                GlobalSave.Set("SmoothScroll", true);

            if (!GlobalSave.Has("ChromiumHardwareAcceleration"))
                GlobalSave.Set("ChromiumHardwareAcceleration", (RenderCapability.Tier >> 16) != 0);
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
                GlobalSave.Set("CustomBackgroundQuery", "");
            if (!GlobalSave.Has("CustomBackgroundImage"))
                GlobalSave.Set("CustomBackgroundImage", "");

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

            if (!GlobalSave.Has("BlockFingerprint"))
                GlobalSave.Set("BlockFingerprint", false);
            if (!GlobalSave.Has("FingerprintLevel"))
                GlobalSave.Set("FingerprintLevel", "Minimal");

            /*if (!GlobalSave.Has("DefaultBrowserEngine"))
                GlobalSave.Set("DefaultBrowserEngine", 0);*/
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                    Themes.Add(new Theme("Auto", (key.GetValue("SystemUsesLightTheme") as int? == 1) ? Themes[0] : Themes[1]));
            }
            catch
            {
                Themes.Add(new Theme("Auto", Themes[1]));
            }
        }
        private void InitializeUISaves(string CommandLineUrl = "")
        {
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
            SetAppearance(GetTheme(GlobalSave.Get("Theme", "Auto")), GlobalSave.GetInt("TabAlignment", 0), bool.Parse(GlobalSave.Get("CompactTab", true.ToString())), bool.Parse(GlobalSave.Get("HomeButton", true.ToString())), bool.Parse(GlobalSave.Get("TranslateButton", true.ToString())), bool.Parse(GlobalSave.Get("ReaderButton", true.ToString())), GlobalSave.GetInt("ExtensionButton", 0), GlobalSave.GetInt("FavouritesBar", 0));
            if (bool.Parse(GlobalSave.Get("RestoreTabs", true.ToString())))
            {
                for (int t = 0; t < WindowsSaves.Count; t++)
                {
                    MainWindow _Window = new MainWindow();
                    if (Background)
                    {
                        _Window.WindowState = WindowState.Minimized;
                        _Window.ShowInTaskbar = false;
                    }
                    else
                        _Window.Show();
                    Saving TabsSave = WindowsSaves[t];
                    int TabCount = TabsSave.GetInt("Count", 0);
                    if (TabCount != 0)
                    {
                        for (int i = 0; i < TabCount; i++)
                            _Window.NewTab(TabsSave.Get(i.ToString(), "slbr://newtab"));
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

            Color _PrimaryColor = (Color)FindResource("PrimaryBrushColor");
            string PrimaryHex = $"#{_PrimaryColor.R:X2}{_PrimaryColor.G:X2}{_PrimaryColor.B:X2}{_PrimaryColor.A:X2}";
            Color _SecondaryColor = (Color)FindResource("SecondaryBrushColor");
            string SecondaryHex = $"#{_SecondaryColor.R:X2}{_SecondaryColor.G:X2}{_SecondaryColor.B:X2}{_SecondaryColor.A:X2}";
            Color _BorderColor = (Color)FindResource("BorderBrushColor");
            string BorderHex = $"#{_BorderColor.R:X2}{_BorderColor.G:X2}{_BorderColor.B:X2}{_BorderColor.A:X2}";
            Color _GrayColor = (Color)FindResource("GrayBrushColor");
            string GrayHex = $"#{_GrayColor.R:X2}{_GrayColor.G:X2}{_GrayColor.B:X2}{_GrayColor.A:X2}";
            Color _FontColor = (Color)FindResource("FontBrushColor");
            string FontHex = $"#{_FontColor.R:X2}{_FontColor.G:X2}{_FontColor.B:X2}{_FontColor.A:X2}";

            Color _IndicatorColor = (Color)FindResource("IndicatorBrushColor");
            string IndicatorHex = $"#{_IndicatorColor.R:X2}{_IndicatorColor.G:X2}{_IndicatorColor.B:X2}{_IndicatorColor.A:X2}";
            DevToolCSS = $@":root {{
  /* Colors begin */

  /* Chrome Desktop Design System */

  /* Use on all surfaces */

  --sys-color-on-surface: {FontHex};
  --sys-color-on-surface-subtle: {FontHex};
  --sys-color-on-surface-secondary: {FontHex};
  --sys-color-on-surface-primary: {FontHex};

  /* Universal surfaces */

  --sys-color-surface: {PrimaryHex};
  --sys-color-surface-variant: {PrimaryHex};

  /* Containers */

  --sys-color-tonal-container: color-mix(in srgb, {SecondaryHex} 14%, {BorderHex});
  --sys-color-on-tonal-container: {IndicatorHex};
  --sys-color-tertiary-container: var(--ref-palette-tertiary90);
  --sys-color-on-tertiary-container: var(--ref-palette-tertiary10);
  --sys-color-error-container: var(--ref-palette-error90);
  --sys-color-on-error-container: {FontHex};
  --sys-color-neutral-container: {SecondaryHex};
  --sys-color-omnibox-container: {PrimaryHex};

  /* Prominent accent colors */

  --sys-color-primary: {IndicatorHex};
  --sys-color-on-primary: var(--ref-palette-primary100);
  --sys-color-secondary: var(--ref-palette-secondary40);
  --sys-color-on-secondary: var(--ref-palette-secondary100);
  --sys-color-tertiary: var(--ref-palette-tertiary40);
  --sys-color-on-tertiary: var(--ref-palette-tertiary100);
  --sys-color-error: var(--ref-palette-error40);
  --sys-color-on-error: var(--ref-palette-error100);

  /* Chrome base surface */

  --sys-color-base: {SecondaryHex};
--sys-color-base-container: {PrimaryHex};
  --sys-color-base-container-elevated: {PrimaryHex};

  /* Corresponding base on colors */

  --sys-color-on-base: var(--ref-palette-neutral10);
  --sys-color-on-base-divider: var(--ref-palette-primary90);
  /* Inverse */

  --sys-color-inverse-surface: var(--ref-palette-neutral20);
  --sys-color-inverse-primary: var(--ref-palette-primary80);
  --sys-color-inverse-on-surface: var(--ref-palette-neutral95);

  /* Outlines */

  --sys-color-outline: {BorderHex};
  --sys-color-tonal-outline: {BorderHex};
  --sys-color-neutral-outline: {BorderHex};
  --sys-color-yellow-outline: var(--ref-palette-yellow70);
  --sys-color-error-outline: var(--ref-palette-error80);
  --sys-color-divider: {BorderHex};
  --sys-color-divider-on-tonal-container: {BorderHex};
  --sys-color-divider-prominent: {BorderHex};

  /* States */

  --sys-color-state-hover-on-prominent: color-mix(in srgb, var(--ref-palette-neutral99) 10%, transparent);
  --sys-color-state-hover-on-subtle: color-mix(in srgb, var(--ref-palette-neutral10) 6%, transparent);
  --sys-color-state-hover-dim-blend-protection: rgb(6 46 111 / 18%);
  --sys-color-state-hover-bright-blend-protection: rgb(31 31 31 / 6%);
  --sys-color-state-ripple-neutral-on-prominent: color-mix(in srgb, var(--ref-palette-neutral99) 16%, transparent);
  --sys-color-state-ripple-neutral-on-subtle: color-mix(in srgb, var(--ref-palette-neutral10) 8%, transparent);
  --sys-color-state-ripple-primary: color-mix(in srgb, var(--ref-palette-primary70) 32%, transparent);
  --sys-color-state-focus-ring: var(--ref-palette-primary40);
  --sys-color-state-focus-select: var(--ref-palette-primary80);
  --sys-color-state-focus-highlight: rgb(31 31 31 / 6%);
  --sys-color-state-disabled: rgb(31 31 31 / 38%);
  --sys-color-state-disabled-container: rgb(31 31 31 / 12%);
  --sys-color-state-header-hover: var(--ref-palette-primary80);
  --sys-color-state-on-header-hover: var(--ref-palette-primary20);
  --sys-color-state-text-highlight: var(--ref-palette-primary40);
  --sys-color-state-on-text-highlight: var(--ref-palette-neutral-variant100);
  --sys-color-state-scrim: rgb(0 0 0 / 60%);

  /* Surfaces */

  --sys-color-surface5: {SecondaryHex};
  --sys-color-surface4: {SecondaryHex};
  --sys-color-surface3: {SecondaryHex};
  --sys-color-surface2: {SecondaryHex};
  --sys-color-surface1: {SecondaryHex};

  /* Header surfaces */
  --sys-color-header-container: green;

  /* Chrome DevTools Design System */

  /* Prominent accent colors for icons */

  --sys-color-primary-bright: var(--ref-palette-primary50);
  --sys-color-blue-bright: var(--ref-palette-blue50);
  --sys-color-green-bright: var(--ref-palette-green60);
  --sys-color-error-bright: var(--ref-palette-error50);
  --sys-color-orange-bright: var(--ref-palette-orange60);
  --sys-color-yellow-bright: var(--ref-palette-yellow60);
  --sys-color-cyan-bright: var(--ref-palette-cyan50);
  --sys-color-purple-bright: var(--ref-palette-purple50);
  --sys-color-neutral-bright: var(--ref-palette-neutral70);
  --sys-color-pink-bright: var(--ref-palette-pink60);

  /* Prominent accent colors for text */

  --sys-color-blue: var(--ref-palette-blue40);
  --sys-color-on-blue: var(--ref-palette-blue100);
  --sys-color-green: var(--ref-palette-green40);
  --sys-color-on-green: var(--ref-palette-green100);
  --sys-color-orange: var(--ref-palette-orange40);
  --sys-color-on-orange: var(--ref-palette-orange100);
  --sys-color-yellow: var(--ref-palette-yellow40);
  --sys-color-on-yellow: var(--ref-palette-yellow100);
  --sys-color-cyan: var(--ref-palette-cyan40);
  --sys-color-on-cyan: var(--ref-palette-cyan100);
  --sys-color-purple: var(--ref-palette-purple40);
  --sys-color-on-purple: var(--ref-palette-purple100);
  --sys-color-pink: var(--ref-palette-pink40);
  --sys-color-on-pink: var(--ref-palette-pink100);

  /* Containers */

  --sys-color-yellow-container: var(--ref-palette-yellow90);
  --sys-color-on-yellow-container: var(--ref-palette-yellow10);

  /* Universal surfaces */

  --sys-color-cdt-base: var(--sys-color-base-container);
  --sys-color-cdt-base-container: {PrimaryHex};

  /* Tinted surfaces */

  --sys-color-surface-yellow: rgb(254 246 213 / 100%);
  --sys-color-surface-yellow-high: rgb(253 240 185 / 100%);
  --sys-color-surface-error: rgb(252 235 235 / 100%);
  --sys-color-surface-green: rgb(219 243 226 / 100%);

  /* Corresponding on colors */

  --sys-color-on-surface-yellow: var(--ref-palette-yellow20);
  --sys-color-on-surface-error: var(--ref-palette-error30);
  --sys-color-on-surface-green: var(--ref-palette-green20);

  /* Syntax highlighting */

  --sys-color-token-variable: var(--sys-color-on-surface);
  --sys-color-token-property: var(--sys-color-on-surface);
  --sys-color-token-property-special: var(--ref-palette-error50);
  --sys-color-token-type: var(--ref-palette-green50);
  --sys-color-token-definition: var(--ref-palette-blue30);
  --sys-color-token-variable-special: var(--ref-palette-blue30);
  --sys-color-token-builtin: var(--ref-palette-blue20);
  --sys-color-token-keyword: var(--ref-palette-pink40);
  --sys-color-token-number: var(--ref-palette-blue40);
  --sys-color-token-string: var(--ref-palette-error40);
  --sys-color-token-string-special: var(--ref-palette-error50);
  --sys-color-token-atom: var(--ref-palette-blue20);
  --sys-color-token-tag: var(--ref-palette-pink30);
  --sys-color-token-attribute: var(--ref-palette-orange40);
  --sys-color-token-attribute-value: var(--ref-palette-blue30);
  --sys-color-token-comment: var(--ref-palette-green40);
  --sys-color-token-meta: var(--ref-palette-neutral60);
  --sys-color-token-deleted: var(--ref-palette-error50);
  --sys-color-token-inserted: var(--ref-palette-green60);
  --sys-color-token-pseudo-element: var(--ref-palette-blue40);
  --sys-color-token-subtle: var(--ref-palette-neutral60);

  /**
  * Gradients
  */
  --sys-color-gradient-primary: var(--ref-palette-primary90);
  --sys-color-gradient-tertiary: var(--ref-palette-tertiary95);

&.theme-with-dark-background {{
    /* States */
    --sys-color-state-hover-on-prominent: color-mix(in srgb, var(--ref-palette-neutral10) 6%, transparent);
    --sys-color-state-hover-on-subtle: color-mix(in srgb, var(--ref-palette-neutral99) 10%, transparent);
    --sys-color-state-hover-dim-blend-protection: rgb(31 31 31 / 10%);
    --sys-color-state-hover-bright-blend-protection: rgb(31 31 31 / 16%);
    --sys-color-state-ripple-neutral-on-prominent: color-mix(in srgb, var(--ref-palette-neutral10) 12%, transparent);
    --sys-color-state-ripple-neutral-on-subtle: color-mix(in srgb, var(--ref-palette-neutral99) 16%, transparent);
    --sys-color-state-ripple-primary: color-mix(in srgb, var(--ref-palette-primary60) 32%, transparent);
    --sys-color-state-focus-ring: var(--ref-palette-primary80);
    --sys-color-state-focus-select: var(--ref-palette-secondary50);
    --sys-color-state-focus-highlight: rgb(253 252 251 / 10%);
    --sys-color-state-disabled: rgb(227 227 227 / 38%);
    --sys-color-state-disabled-container: rgb(227 227 227 / 12%);
    --sys-color-state-header-hover: var(--ref-palette-secondary30);
    --sys-color-state-on-header-hover: var(--ref-palette-secondary90);
    --sys-color-state-text-highlight: var(--ref-palette-primary80);
    --sys-color-state-on-text-highlight: var(--ref-palette-neutral-variant0);

    /* Header surface */
    --sys-color-header-container: var(--ref-palette-neutral25);

    /* Chrome DevTools Design System */

    /* Prominent accent colors for icons */

    --sys-color-primary-bright: var(--ref-palette-primary70);
    --sys-color-blue-bright: var(--ref-palette-blue70);
    --sys-color-green-bright: var(--ref-palette-green70);
    --sys-color-error-bright: var(--ref-palette-error60);
    --sys-color-orange-bright: var(--ref-palette-orange70);
    --sys-color-yellow-bright: var(--ref-palette-yellow70);
    --sys-color-cyan-bright: var(--ref-palette-cyan70);
    --sys-color-purple-bright: var(--ref-palette-purple70);
    --sys-color-neutral-bright: var(--ref-palette-neutral50);
    --sys-color-pink-bright: var(--ref-palette-pink70);

    /* Prominent accent colors for text */

    --sys-color-blue: var(--ref-palette-blue80);
    --sys-color-on-blue: var(--ref-palette-blue20);
    --sys-color-green: var(--ref-palette-green80);
    --sys-color-on-green: var(--ref-palette-green20);
    --sys-color-orange: var(--ref-palette-orange80);
    --sys-color-on-orange: var(--ref-palette-orange20);
    --sys-color-yellow: var(--ref-palette-yellow80);
    --sys-color-on-yellow: var(--ref-palette-yellow20);
    --sys-color-cyan: var(--ref-palette-cyan80);
    --sys-color-on-cyan: var(--ref-palette-cyan20);
    --sys-color-purple: var(--ref-palette-purple80);
    --sys-color-on-purple: var(--ref-palette-purple20);
    --sys-color-pink: var(--ref-palette-pink80);
    --sys-color-on-pink: var(--ref-palette-pink20);

    /* Containers */

    --sys-color-yellow-container: var(--ref-palette-yellow30);
    --sys-color-on-yellow-container: var(--ref-palette-yellow90);

    /* Universal surfaces */

    --sys-color-cdt-base: var(--sys-color-base);
    --sys-color-cdt-base-container: var(--sys-color-base-container);

    /* Tinted surfaces */

    --sys-color-surface-yellow: rgb(65 60 38 / 100%);
    --sys-color-surface-yellow-high: rgb(76 68 37 / 100%);
    --sys-color-surface-error: rgb(78 53 52 / 100%);
    --sys-color-surface-green: rgb(43 70 51 / 100%);

    /* Corresponding on colors */

    --sys-color-on-surface-yellow: var(--ref-palette-yellow90);
    --sys-color-on-surface-error: var(--ref-palette-error90);
    --sys-color-on-surface-green: var(--ref-palette-green90);


    /* Syntax highlighting */

    --sys-color-token-variable: var(--ref-palette-neutral80);
    --sys-color-token-property: var(--ref-palette-yellow70);
    --sys-color-token-property-special: var(--ref-palette-cyan80);
    --sys-color-token-type: var(--ref-palette-blue70);
    --sys-color-token-definition: var(--ref-palette-blue70);
    --sys-color-token-variable-special: var(--ref-palette-blue40);
    --sys-color-token-builtin: var(--ref-palette-blue80);
    --sys-color-token-keyword: var(--ref-palette-purple60);
    --sys-color-token-number: var(--ref-palette-green90);
    --sys-color-token-string: var(--ref-palette-orange70);
    --sys-color-token-string-special: var(--ref-palette-orange70);
    --sys-color-token-atom: var(--ref-palette-green90);
    --sys-color-token-tag: var(--ref-palette-blue70);
    --sys-color-token-attribute: var(--ref-palette-blue80);
    --sys-color-token-attribute-value: var(--ref-palette-orange70);
    --sys-color-token-comment: var(--ref-palette-neutral70);
    --sys-color-token-meta: var(--ref-palette-neutral60);
    --sys-color-token-deleted: var(--ref-palette-error50);
    --sys-color-token-inserted: var(--ref-palette-green60);
    --sys-color-token-pseudo-element: var(--ref-palette-pink70);
    --sys-color-token-subtle: var(--ref-palette-neutral60);

    /**
    * Gradients
    */
    --sys-color-gradient-primary: var(--ref-palette-primary30);
    --sys-color-gradient-tertiary: var(--ref-palette-tertiary30);
  }}

  /* Colors end */

  /* Sizes begin */

  --sys-size-1: 1px;
  --sys-size-2: 2px;
  --sys-size-3: 4px;
  --sys-size-4: 6px;
  --sys-size-5: 8px;
  --sys-size-6: 12px;
  --sys-size-7: 14px;
  --sys-size-8: 16px;
  --sys-size-9: 20px;
  --sys-size-10: 22px;
  --sys-size-11: 24px;
  --sys-size-12: 28px;
  --sys-size-13: 32px;
  --sys-size-14: 40px;
  --sys-size-15: 44px;
  --sys-size-16: 48px;
  --sys-size-17: 56px;
  --sys-size-18: 64px;
  --sys-size-19: 80px;
  --sys-size-20: 112px;
  --sys-size-21: 128px;
  --sys-size-22: 144px;
  --sys-size-23: 160px;
  --sys-size-24: 176px;
  --sys-size-25: 192px;
  --sys-size-26: 208px;
  --sys-size-27: 224px;
  --sys-size-28: 240px;
  --sys-size-29: 256px;
  --sys-size-30: 288px;
  --sys-size-31: 320px;
  --sys-size-32: 384px;
  --sys-size-33: 448px;
  --sys-size-34: 512px;
  --sys-size-35: 576px;
  --sys-size-36: 672px;
  --sys-size-37: 768px;
  --sys-size-38: 896px;
  --sys-size-39: 1024px;
  --sys-size-40: 1152px;
  --sys-size-41: 1280px;

  /* Sizes end */

  /* Typography begin */

  /* This will be overridden by the platform-X classes that get
   * added to the html tag on load, but is here as a safe
   * fallback
   */
  --default-font-family: "".SFNSDisplay-Regular"", ""Helvetica Neue"", ""Lucida Grande"", sans-serif;
  --monospace-font-size: 10px;
  --monospace-font-family: monospace;
  --source-code-font-size: 11px;
  --source-code-font-family: monospace;

  /* Default fonts */
  &.platform-linux {{
    --default-font-family: ""Google Sans Text"", ""Google Sans"", system-ui, sans-serif;
    --monospace-font-size: 11px;
    --monospace-font-family: ""Noto Sans Mono"", ""DejaVu Sans Mono"", monospace;
    --source-code-font-size: 11px;
    --source-code-font-family: ""Noto Sans Mono"", ""DejaVu Sans Mono"", monospace;
  }}

  &.platform-mac {{
    --default-font-family: system-ui, sans-serif;
    --monospace-font-size: 11px;
    --monospace-font-family: monospace;
    --source-code-font-size: 11px;
    --source-code-font-family: monospace;
  }}

  &.platform-windows {{
    --default-font-family: system-ui, sans-serif;
    --monospace-font-size: var(--sys-size-6);
    --monospace-font-family: monospace;
    --source-code-font-size: var(--sys-size-6);
    --source-code-font-family: monospace;
  }}

  &.platform-screenshot-test {{
    --default-font-family: roboto;
    --monospace-font-family: roboto;
    --source-code-font-family: roboto;
  }}

  --ref-typeface-weight-regular: 400;
  --ref-typeface-weight-medium: 500;
  --ref-typeface-weight-bold: 700;
  --sys-typescale-headline1: var(--ref-typeface-weight-medium) var(--sys-typescale-headline1-size) / var(--sys-typescale-headline1-line-height) var(--default-font-family);
  --sys-typescale-headline2: var(--ref-typeface-weight-medium) var(--sys-typescale-headline2-size) / var(--sys-typescale-headline2-line-height) var(--default-font-family);
  --sys-typescale-headline3: var(--ref-typeface-weight-medium) var(--sys-typescale-headline3-size) / var(--sys-typescale-headline3-line-height) var(--default-font-family);
  --sys-typescale-headline4: var(--ref-typeface-weight-medium) var(--sys-typescale-headline4-size) / var(--sys-typescale-headline4-line-height) var(--default-font-family);
  --sys-typescale-headline5: var(--ref-typeface-weight-medium) var(--sys-typescale-headline5-size) / var(--sys-typescale-headline5-line-height) var(--default-font-family);
  --sys-typescale-body1-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-body1-size) / var(--sys-typescale-body1-line-height) var(--default-font-family);
  --sys-typescale-body2-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-body2-size) / var(--sys-typescale-body2-line-height) var(--default-font-family);
  --sys-typescale-body3-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-body3-size) / var(--sys-typescale-body3-line-height) var(--default-font-family);
  --sys-typescale-body4-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-body4-size) / var(--sys-typescale-body4-line-height) var(--default-font-family);
  --sys-typescale-body5-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-body5-size) / var(--sys-typescale-body5-line-height) var(--default-font-family);
  --sys-typescale-body1-medium: var(--ref-typeface-weight-medium) var(--sys-typescale-body1-size) / var(--sys-typescale-body1-line-height) var(--default-font-family);
  --sys-typescale-body2-medium: var(--ref-typeface-weight-medium) var(--sys-typescale-body2-size) / var(--sys-typescale-body2-line-height) var(--default-font-family);
  --sys-typescale-body3-medium: var(--ref-typeface-weight-medium) var(--sys-typescale-body3-size) / var(--sys-typescale-body3-line-height) var(--default-font-family);
  --sys-typescale-body4-medium: var(--ref-typeface-weight-medium) var(--sys-typescale-body4-size) / var(--sys-typescale-body4-line-height) var(--default-font-family);
  --sys-typescale-body5-medium: var(--ref-typeface-weight-medium) var(--sys-typescale-body5-size) / var(--sys-typescale-body5-line-height) var(--default-font-family);
  --sys-typescale-body1-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-body1-size) / var(--sys-typescale-body1-line-height) var(--default-font-family);
  --sys-typescale-body2-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-body2-size) / var(--sys-typescale-body2-line-height) var(--default-font-family);
  --sys-typescale-body3-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-body3-size) / var(--sys-typescale-body3-line-height) var(--default-font-family);
  --sys-typescale-body4-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-body4-size) / var(--sys-typescale-body4-line-height) var(--default-font-family);
  --sys-typescale-body5-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-body5-size) / var(--sys-typescale-body5-line-height) var(--default-font-family);
  --sys-typescale-monospace-regular: var(--ref-typeface-weight-regular) var(--sys-typescale-monospace-size) / var(--sys-typescale-monospace-line-height) var(--monospace-font-family);
  --sys-typescale-monospace-bold: var(--ref-typeface-weight-bold) var(--sys-typescale-monospace-size) / var(--sys-typescale-monospace-line-height) var(--monospace-font-family);
  --sys-typescale-headline1-size: 24px;
  --sys-typescale-headline2-size: 20px;
  --sys-typescale-headline3-size: 18px;
  --sys-typescale-headline4-size: 16px;
  --sys-typescale-headline5-size: 14px;
  --sys-typescale-body1-size: 16px;
  --sys-typescale-body2-size: 14px;
  --sys-typescale-body3-size: 13px;
  --sys-typescale-body4-size: 12px;
  --sys-typescale-body5-size: 11px;
  --sys-typescale-monospace-size: 11px;
  --sys-typescale-headline1-line-height: 32px;
  --sys-typescale-headline2-line-height: 24px;
  --sys-typescale-headline3-line-height: 24px;
  --sys-typescale-headline4-line-height: 24px;
  --sys-typescale-headline5-line-height: 20px;
  --sys-typescale-body1-line-height: 24px;
  --sys-typescale-body2-line-height: 20px;
  --sys-typescale-body3-line-height: 20px;
  --sys-typescale-body4-line-height: 16px;
  --sys-typescale-body5-line-height: 16px;
  --sys-typescale-monospace-line-height: 1.2;

  /* Typography end */

  /* Elevation begin */

  --sys-elevation-level1: 0 1px 2px 0 rgb(0 0 0 / 30%), 0 1px 3px 1px rgb(0 0 0 / 15%);
  --sys-elevation-level2: 0 1px 2px 0 rgb(0 0 0 / 30%), 0 2px 6px 2px rgb(0 0 0 / 15%);
  --sys-elevation-level3: 0 4px 8px 3px rgb(0 0 0 / 15%), 0 1px 3px 0 rgb(0 0 0 / 30%);
  --sys-elevation-level4: 0 6px 10px 4px rgb(0 0 0 / 15%), 0 2px 3px 0 rgb(0 0 0 / 30%);
  --sys-elevation-level5: 0 8px 12px 6px rgb(0 0 0 / 15%), 0 4px 4px 0 rgb(0 0 0 / 30%);

  /* Elevation end */

  /* Shape begin */

  --sys-shape-corner-extra-small: 4px;
  --sys-shape-corner-small: 8px;
  --sys-shape-corner-medium-small: 12px;
  --sys-shape-corner-medium: 16px;
  --sys-shape-corner-large: 24px;
  --sys-shape-corner-full: 9999px;

  /* Shape end */

  /* Motion begin */

  --sys-motion-curve-spatial: cubic-bezier(0.27, 1.06, 0.18, 1.00);
  --sys-motion-duration-short4: 200ms;
  --sys-motion-duration-medium2: 300ms;
  --sys-motion-duration-long2: 500ms;
  --sys-motion-easing-emphasized: cubic-bezier(0.2, 0, 0, 1);

  /* Motion end */

  /**
    * Reference colors. Reference colors are a set of base colors that get updated on Chrome
    * color theme changes and should not be directly used (except when defining system or
    * application colors).
    *
    * DON'T CHANGE AND DON'T USE THEM DIRECTLY IN CODE.
    * More info: https://chromium.googlesource.com/devtools/devtools-frontend/+/main/docs/styleguide/ux/styles.md#colors
   **/
   --ref-palette-primary0: var(--color-ref-primary0, rgb(0 0 0 / 100%));
   --ref-palette-primary10: var(--color-ref-primary10, rgb(4 30 73 / 100%));
   --ref-palette-primary20: var(--color-ref-primary20, rgb(6 46 111 / 100%));
   --ref-palette-primary30: var(--color-ref-primary30, rgb(8 66 160 / 100%));
   --ref-palette-primary40: var(--color-ref-primary40, rgb(11 87 208 / 100%));
   --ref-palette-primary50: var(--color-ref-primary50, rgb(27 110 243 / 100%));
   --ref-palette-primary60: var(--color-ref-primary60, rgb(76 141 246 / 100%));
   --ref-palette-primary70: {IndicatorHex};
   --ref-palette-primary80: var(--color-ref-primary80, rgb(168 199 250 / 100%));
   --ref-palette-primary90: var(--color-ref-primary90, rgb(211 227 253 / 100%));
   --ref-palette-primary95: var(--color-ref-primary95, rgb(236 243 254 / 100%));
   --ref-palette-primary99: var(--color-ref-primary99, rgb(250 251 255 / 100%));
   --ref-palette-primary100: var(--color-ref-primary100, rgb(255 255 255 / 100%));
   --ref-palette-secondary0: var(--color-ref-secondary0, rgb(0 0 0 / 100%));
   --ref-palette-secondary10: var(--color-ref-secondary10, rgb(0 29 53 / 100%));
   --ref-palette-secondary15: var(--color-ref-secondary15, rgb(0 40 69 / 100%));
   --ref-palette-secondary20: var(--color-ref-secondary20, rgb(0 51 85 / 100%));
   --ref-palette-secondary25: var(--color-ref-secondary25, rgb(0 63 102 / 100%));
   --ref-palette-secondary30: var(--color-ref-secondary30, rgb(0 74 119 / 100%));
   --ref-palette-secondary35: var(--color-ref-secondary35, rgb(0 87 137 / 100%));
   --ref-palette-secondary40: var(--color-ref-secondary40, rgb(0 99 155 / 100%));
   --ref-palette-secondary50: var(--color-ref-secondary50, rgb(4 125 183 / 100%));
   --ref-palette-secondary60: var(--color-ref-secondary60, rgb(57 152 211 / 100%));
   --ref-palette-secondary70: var(--color-ref-secondary70, rgb(90 179 240 / 100%));
   --ref-palette-secondary80: var(--color-ref-secondary80, rgb(127 207 255 / 100%));
   --ref-palette-secondary90: var(--color-ref-secondary90, rgb(223 243 255 / 100%));
   --ref-palette-secondary95: var(--color-ref-secondary95, rgb(223 243 255 / 100%));
   --ref-palette-secondary99: var(--color-ref-secondary99, rgb(247 252 255 / 100%));
   --ref-palette-secondary100: var(--color-ref-secondary100, rgb(255 255 255 / 100%));
   --ref-palette-tertiary0: var(--color-ref-tertiary0, rgb(0 0 0 / 100%));
   --ref-palette-tertiary10: var(--color-ref-tertiary10, rgb(7 39 17 / 100%));
   --ref-palette-tertiary20: var(--color-ref-tertiary20, rgb(10 56 24 / 100%));
   --ref-palette-tertiary30: var(--color-ref-tertiary30, rgb(15 82 35 / 100%));
   --ref-palette-tertiary40: var(--color-ref-tertiary40, rgb(20 108 46 / 100%));
   --ref-palette-tertiary50: var(--color-ref-tertiary50, rgb(25 134 57 / 100%));
   --ref-palette-tertiary60: var(--color-ref-tertiary60, rgb(30 164 70 / 100%));
   --ref-palette-tertiary70: var(--color-ref-tertiary70, rgb(55 190 95 / 100%));
   --ref-palette-tertiary80: var(--color-ref-tertiary80, rgb(109 213 140 / 100%));
   --ref-palette-tertiary90: var(--color-ref-tertiary90, rgb(196 238 208 / 100%));
   --ref-palette-tertiary95: var(--color-ref-tertiary95, rgb(231 248 237 / 100%));
   --ref-palette-tertiary99: var(--color-ref-tertiary99, rgb(242 255 238 / 100%));
   --ref-palette-tertiary100: var(--color-ref-tertiary100, rgb(255 255 255 / 100%));
   --ref-palette-error0: var(--color-ref-error0, rgb(0 0 0 / 100%));
   --ref-palette-error10: var(--color-ref-error10, rgb(65 14 11 / 100%));
   --ref-palette-error20: var(--color-ref-error20, rgb(96 20 16 / 100%));
   --ref-palette-error30: var(--color-ref-error30, rgb(140 29 24 / 100%));
   --ref-palette-error40: var(--color-ref-error40, rgb(179 38 30 / 100%));
   --ref-palette-error50: var(--color-ref-error50, rgb(220 54 46 / 100%));
   --ref-palette-error60: var(--color-ref-error60, rgb(228 105 98 / 100%));
   --ref-palette-error70: var(--color-ref-error70, rgb(236 146 142 / 100%));
   --ref-palette-error80: var(--color-ref-error80, rgb(242 184 181 / 100%));
   --ref-palette-error90: var(--color-ref-error90, rgb(249 222 220 / 100%));
   --ref-palette-error95: var(--color-ref-error95, rgb(231 248 237 / 100%));
   --ref-palette-error99: var(--color-ref-error99, rgb(242 255 238 / 100%));
   --ref-palette-error100: var(--color-ref-error100, rgb(255 255 255 / 100%));
   --ref-palette-neutral0: var(--color-ref-neutral0, rgb(0 0 0 / 100%));
   --ref-palette-neutral10: var(--color-ref-neutral10, rgb(31 31 31 / 100%));
   --ref-palette-neutral15: var(--color-ref-neutral15, rgb(40 40 40 / 100%));
   --ref-palette-neutral20: var(--color-ref-neutral20, rgb(48 48 48 / 100%));
   --ref-palette-neutral25: var(--color-ref-neutral25, rgb(60 60 60 / 100%));
   --ref-palette-neutral30: var(--color-ref-neutral30, rgb(71 71 71 / 100%));
   --ref-palette-neutral40: var(--color-ref-neutral40, rgb(94 94 94 / 100%));
   --ref-palette-neutral50: var(--color-ref-neutral50, rgb(117 117 117 / 100%));
   --ref-palette-neutral60: var(--color-ref-neutral60, rgb(143 143 143 / 100%));
   --ref-palette-neutral70: var(--color-ref-neutral70, rgb(171 171 171 / 100%));
   --ref-palette-neutral80: var(--color-ref-neutral80, rgb(199 199 199 / 100%));
   --ref-palette-neutral90: var(--color-ref-neutral90, rgb(227 227 227 / 100%));
   --ref-palette-neutral94: var(--color-ref-neutral94, rgb(239 237 237 / 100%));
   --ref-palette-neutral95: var(--color-ref-neutral95, rgb(242 242 242 / 100%));
   --ref-palette-neutral98: var(--color-ref-neutral98, rgb(250 249 248 / 100%));
   --ref-palette-neutral99: var(--color-ref-neutral99, rgb(253 252 251 / 100%));
   --ref-palette-neutral100: var(--color-ref-neutral100, rgb(255 255 255 / 100%));
   --ref-palette-neutral-variant0: var(--color-ref-neutral-variant0, rgb(0 0 0 / 100%));
   --ref-palette-neutral-variant10: var(--color-ref-neutral-variant10, rgb(25 29 28 / 100%));
   --ref-palette-neutral-variant20: var(--color-ref-neutral-variant20, rgb(45 49 47 / 100%));
   --ref-palette-neutral-variant30: var(--color-ref-neutral-variant30, rgb(68 71 70 / 100%));
   --ref-palette-neutral-variant40: var(--color-ref-neutral-variant40, rgb(92 95 94 / 100%));
   --ref-palette-neutral-variant50: var(--color-ref-neutral-variant50, rgb(116 119 117 / 100%));
   --ref-palette-neutral-variant60: var(--color-ref-neutral-variant60, rgb(142 145 143 / 100%));
   --ref-palette-neutral-variant70: var(--color-ref-neutral-variant70, rgb(169 172 170 / 100%));
   --ref-palette-neutral-variant80: var(--color-ref-neutral-variant80, rgb(196 199 197 / 100%));
   --ref-palette-neutral-variant90: var(--color-ref-neutral-variant90, rgb(225 227 225 / 100%));
   --ref-palette-neutral-variant95: var(--color-ref-neutral-variant95, rgb(239 242 239 / 100%));
   --ref-palette-neutral-variant99: var(--color-ref-neutral-variant99, rgb(250 253 251 / 100%));
   --ref-palette-neutral-variant100: var(--color-ref-neutral-variant100, rgb(255 255 255 / 100%));

   /* Additional fixed colors */
   --ref-palette-blue0: rgb(0 0 0 / 100%);
   --ref-palette-blue10: rgb(4 30 73 / 100%);
   --ref-palette-blue20: rgb(6 46 111 / 100%);
   --ref-palette-blue30: rgb(8 66 160 / 100%);
   --ref-palette-blue40: rgb(11 87 208 / 100%);
   --ref-palette-blue50: rgb(27 110 243 / 100%);
   --ref-palette-blue60: rgb(76 141 246 / 100%);
   --ref-palette-blue70: rgb(124 172 248 / 100%);
   --ref-palette-blue80: rgb(168 199 250 / 100%);
   --ref-palette-blue90: rgb(211 227 253 / 100%);
   --ref-palette-blue95: rgb(236 243 254 / 100%);
   --ref-palette-blue99: rgb(250 251 255 / 100%);
   --ref-palette-blue100: rgb(255 255 255 / 100%);
   --ref-palette-green0: rgb(0 0 0 / 100%);
   --ref-palette-green10: rgb(7 39 17 / 100%);
   --ref-palette-green20: rgb(10 56 24 / 100%);
   --ref-palette-green30: rgb(15 82 35 / 100%);
   --ref-palette-green40: rgb(20 108 46 / 100%);
   --ref-palette-green50: rgb(25 134 57 / 100%);
   --ref-palette-green60: rgb(30 164 70 / 100%);
   --ref-palette-green70: rgb(55 190 95 / 100%);
   --ref-palette-green80: rgb(109 213 140 / 100%);
   --ref-palette-green90: rgb(196 238 208 / 100%);
   --ref-palette-green95: rgb(231 248 237 / 100%);
   --ref-palette-green99: rgb(242 255 238 / 100%);
   --ref-palette-green100: rgb(255 255 255 / 100%);
   --ref-palette-orange0: rgb(0 0 0 / 100%);
   --ref-palette-orange10: rgb(53 16 2 / 100%);
   --ref-palette-orange20: rgb(85 32 5 / 100%);
   --ref-palette-orange30: rgb(121 50 11 / 100%);
   --ref-palette-orange40: rgb(159 67 18 / 100%);
   --ref-palette-orange50: rgb(198 85 26 / 100%);
   --ref-palette-orange60: rgb(233 103 37 / 100%);
   --ref-palette-orange70: rgb(254 141 89 / 100%);
   --ref-palette-orange80: rgb(254 183 150 / 100%);
   --ref-palette-orange90: rgb(255 220 204 / 100%);
   --ref-palette-orange95: rgb(255 236 230 / 100%);
   --ref-palette-orange99: rgb(255 251 255 / 100%);
   --ref-palette-orange100: rgb(255 255 255 / 100%);
   --ref-palette-yellow0: rgb(0 0 0 / 100%);
   --ref-palette-yellow10: rgb(36 26 0 / 100%);
   --ref-palette-yellow20: rgb(62 47 0 / 100%);
   --ref-palette-yellow30: rgb(92 67 0 / 100%);
   --ref-palette-yellow40: rgb(151 103 0 / 100%);
   --ref-palette-yellow50: rgb(202 138 4 / 100%);
   --ref-palette-yellow60: rgb(234 179 8 / 100%);
   --ref-palette-yellow70: rgb(250 204 21 / 100%);
   --ref-palette-yellow80: rgb(253 224 71 / 100%);
   --ref-palette-yellow90: rgb(253 243 170 / 100%);
   --ref-palette-yellow95: rgb(252 248 210 / 100%);
   --ref-palette-yellow99: rgb(255 251 235 / 100%);
   --ref-palette-yellow100: rgb(255 255 255 / 100%);
   --ref-palette-cyan0: rgb(0 0 0 / 100%);
   --ref-palette-cyan10: rgb(0 31 40 / 100%);
   --ref-palette-cyan20: rgb(0 53 67 / 100%);
   --ref-palette-cyan30: rgb(0 78 96 / 100%);
   --ref-palette-cyan40: rgb(0 103 127 / 100%);
   --ref-palette-cyan50: rgb(0 130 159 / 100%);
   --ref-palette-cyan60: rgb(0 157 193 / 100%);
   --ref-palette-cyan70: rgb(56 185 222 / 100%);
   --ref-palette-cyan80: rgb(92 213 251 / 100%);
   --ref-palette-cyan90: rgb(183 234 255 / 100%);
   --ref-palette-cyan95: rgb(221 245 255 / 100%);
   --ref-palette-cyan99: rgb(249 253 255 / 100%);
   --ref-palette-cyan100: rgb(255 255 255 / 100%);
   --ref-palette-purple0: rgb(0 0 0 / 100%);
   --ref-palette-purple10: rgb(47 0 77 / 100%);
   --ref-palette-purple20: rgb(77 0 122 / 100%);
   --ref-palette-purple30: rgb(110 0 171 / 100%);
   --ref-palette-purple40: rgb(140 30 211 / 100%);
   --ref-palette-purple50: rgb(167 67 238 / 100%);
   --ref-palette-purple60: rgb(191 103 255 / 100%);
   --ref-palette-purple70: rgb(209 144 255 / 100%);
   --ref-palette-purple80: rgb(226 182 255 / 100%);
   --ref-palette-purple90: rgb(243 218 255 / 100%);
   --ref-palette-purple95: rgb(251 236 255 / 100%);
   --ref-palette-purple99: rgb(255 251 255 / 100%);
   --ref-palette-purple100: rgb(255 255 255 / 100%);
   --ref-palette-pink0: rgb(0 0 0 / 100%);
   --ref-palette-pink10: rgb(62 0 29 / 100%);
   --ref-palette-pink20: rgb(101 0 51 / 100%);
   --ref-palette-pink30: rgb(142 0 75 / 100%);
   --ref-palette-pink40: rgb(185 0 99 / 100%);
   --ref-palette-pink50: rgb(223 35 125 / 100%);
   --ref-palette-pink55: rgb(255 72 150 / 100%);
   --ref-palette-pink60: rgb(255 72 150 / 100%);
   --ref-palette-pink70: rgb(255 132 175 / 100%);
   --ref-palette-pink80: rgb(255 177 200 / 100%);
   --ref-palette-pink90: rgb(255 217 226 / 100%);
   --ref-palette-pink95: rgb(255 236 240 / 100%);
   --ref-palette-pink99: rgb(255 251 255 / 100%);
   --ref-palette-pink100: rgb(255 255 255 / 100%);
   --ref-palette-indigo0: rgb(0 0 0 / 100%);
   --ref-palette-indigo10: rgb(23 0 101 / 100%);
   --ref-palette-indigo20: rgb(41 0 159 / 100%);
   --ref-palette-indigo30: rgb(63 28 203 / 100%);
   --ref-palette-indigo40: rgb(88 64 227 / 100%);
   --ref-palette-indigo50: rgb(113 93 253 / 100%);
   --ref-palette-indigo60: rgb(141 127 255 / 100%);
   --ref-palette-indigo70: rgb(170 160 255 / 100%);
   --ref-palette-indigo80: rgb(199 191 255 / 100%);
   --ref-palette-indigo90: rgb(228 223 255 / 100%);
   --ref-palette-indigo95: rgb(243 238 255 / 100%);
   --ref-palette-indigo99: rgb(255 251 255 / 100%);
   --ref-palette-indigo100: rgb(255 255 255 / 100%);
}}";

        }

        private void Favourites_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                    BrowserView?.Favourites_CollectionChanged();
            }
        }

        public MainWindow CurrentFocusedWindow()
        {
            var focusedWindow = AllWindows.FirstOrDefault(w => w.IsFocused);
            if (focusedWindow != null) return focusedWindow;
            var activeWindow = AllWindows.FirstOrDefault(w => w.IsActive);
            if (activeWindow != null) return activeWindow;
            var maximizedWindow = AllWindows.FirstOrDefault(w => w.WindowState == WindowState.Maximized);
            if (maximizedWindow != null) return maximizedWindow;
            var normalWindow = AllWindows.FirstOrDefault(w => w.WindowState == WindowState.Normal);
            if (normalWindow != null) return normalWindow;
            return null;
        }

        public void Refresh(bool IgnoreCache = false)
        {
            CurrentFocusedWindow().Refresh("", IgnoreCache);
        }
        public void Fullscreen(bool ForceClose = false)
        {
            MainWindow CurrentWindow = CurrentFocusedWindow();
            if (ForceClose && !CurrentWindow.IsFullscreen)
                return;
            CurrentWindow.Fullscreen(ForceClose ? false : !CurrentWindow.IsFullscreen);
        }
        public void DevTools(string Id = "")
        {
            CurrentFocusedWindow().DevTools(Id);
        }
        public void Find(string Text = "")
        {
            CurrentFocusedWindow().Find(Text);
        }
        public void Screenshot()
        {
            CurrentFocusedWindow().Screenshot();
        }
        public void NewWindow()
        {
            MainWindow _Window = new MainWindow();
            _Window.Show();
            _Window.NewTab(GlobalSave.Get("Homepage"), true, -1, bool.Parse(GlobalSave.Get("PrivateTabs")));
        }

        public static readonly Regex AMPRegex = new(@"<link\s[^>]*rel=['""]amphtml['""][^>]*href=['""]([^'"">]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            "collector.brandmetrics.com", "sb.scorecardresearch.com",
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

            "secure-ds.serving-sys.com", "*.innovid.com",
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
            "*.ero-advertising.com",
            "analytics.archive.org",
            "*.freshmarketer.com",
            "openbid.pubmatic.com", "prebid.media.net", "hbopenbid.pubmatic.com",
            "collector.cdp.cnn.com", "smetrics.cnn.com", "mybbc-analytics.files.bbci.co.uk", "a1.api.bbc.co.uk", "xproxy.api.bbc.com",
            "*.dotmetrics.net", "scripts.webcontentassessor.com",
            "collector.brandmetrics.com", "sb.scorecardresearch.com",
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

        public LifeSpanHandler _LifeSpanHandler;
        public DownloadHandler _DownloadHandler;
        public LimitedContextMenuHandler _LimitedContextMenuHandler;
        public RequestHandler _RequestHandler;
        public ContextMenuHandler _ContextMenuHandler;
        public KeyboardHandler _KeyboardHandler;
        public JsDialogHandler _JsDialogHandler;
        public PermissionHandler _PermissionHandler;
        public SafeBrowsingHandler _SafeBrowsing;
        public DialogHandler _DialogHandler;

        public string ReleaseVersion;

        public string DevToolCSS;

        private void InitializeCEF()
        {
            _LifeSpanHandler = new LifeSpanHandler(false);
            _DownloadHandler = new DownloadHandler();
            _RequestHandler = new RequestHandler();
            _LimitedContextMenuHandler = new LimitedContextMenuHandler();
            _ContextMenuHandler = new ContextMenuHandler();
            _KeyboardHandler = new KeyboardHandler();
            _JsDialogHandler = new JsDialogHandler();
            _PermissionHandler = new PermissionHandler();
            _DialogHandler = new DialogHandler();
            _SafeBrowsing = new SafeBrowsingHandler(SECRETS.GOOGLE_API_KEY, SECRETS.GOOGLE_DEFAULT_CLIENT_ID);

            //_KeyboardHandler.AddKey(Screenshot, (int)Key.S, true);
            _KeyboardHandler.AddKey(delegate () { Refresh(); }, (int)Key.F5);
            _KeyboardHandler.AddKey(delegate () { Refresh(true); }, (int)Key.F5, true);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(); }, (int)Key.F11);
            _KeyboardHandler.AddKey(delegate () { Fullscreen(true); }, (int)Key.Escape);
            _KeyboardHandler.AddKey(delegate () { DevTools(); }, (int)Key.F12);
            _KeyboardHandler.AddKey(delegate () { Find(); }, (int)Key.F, true);

            CefSettings Settings = new CefSettings();
            Settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;

            Settings.Locale = Locale.Tooltip;
            Settings.AcceptLanguageList = string.Join(",", Languages.Select(i => i.Tooltip));
            //Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1";
            //Settings.UserAgentProduct = $"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}";
            //Settings.UserAgent = UserAgent;

            string UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
            Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));
            Settings.LogSeverity = LogSeverity.Error;
            Settings.CachePath = Path.GetFullPath(Path.Combine(UserDataPath, "Cache"));
            Settings.RootCachePath = UserDataPath;

            SetCEFFlags(Settings);

            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gemini",
                SchemeHandlerFactory = new GeminiSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "gopher",
                SchemeHandlerFactory = new GopherSchemeHandlerFactory()
            });
            /*Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipfs",
                SchemeHandlerFactory = new IPFSSchemeHandlerFactory()
            });
            Settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ipns",
                SchemeHandlerFactory = new IPNSSchemeHandlerFactory()
            });*/

            string[] SLBrURLs = ["Credits", "License", "NewTab", "Downloads", "History", "Settings", "Tetris", "WhatsNew"];
            foreach (string _Scheme in SLBrURLs)
            {
                string Lower = _Scheme.ToLower();
                Settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "slbr",
                    DomainName = Lower,
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(ResourcesPath, hostName: Lower, defaultPage: $"{_Scheme}.html"),
                    IsSecure = true,
                    IsLocal = true,
                    IsStandard = true,
                    IsCorsEnabled = true
                });
            }

            CefSharpSettings.RuntimeStyle = GlobalSave.GetInt("ChromiumRuntimeStyle", 1) == 1 ? CefRuntimeStyle.Alloy : CefRuntimeStyle.Chrome;
            //Alloy: No bottom chrome status bar, No chrome permission popups, Pointer lock broken, 
            //Chrome: Find broken, Has "Esc" popup prompts for Fullscreen & Pointer Locks

            Cef.Initialize(Settings);
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var GlobalRequestContext = Cef.GetGlobalRequestContext();
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.Geolocation, ContentSettingValues.Block);
                //GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.JavascriptOptimizer, ContentSettingValues.Allow);
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.AdsData, ContentSettingValues.Block);
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.Notifications, ContentSettingValues.Ask);
                GlobalRequestContext.SetContentSetting(null, null, ContentSettingTypes.Popups, ContentSettingValues.Ask);
                bool PDFViewerExtension = bool.Parse(GlobalSave.Get("PDF"));

                /*string _Preferences = "";
                foreach (KeyValuePair<string, object> e in GlobalRequestContext.GetAllPreferences(true))
                    _Preferences = GetPreferencesString(_Preferences, "", e);
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt")))
                    outputFile.Write(_Preferences);*/

                //https://github.com/cefsharp/CefSharp/issues/4986
                string Error;
                GlobalRequestContext.SetPreference("privacy_sandbox.apis_enabled", false, out Error);

                GlobalRequestContext.SetPreference("compact_mode", true, out Error);
                GlobalRequestContext.SetPreference("history.saving_disabled", true, out Error);
                GlobalRequestContext.SetPreference("profile.cookies_control_mode", 1, out Error);
                GlobalRequestContext.SetPreference("profile.content_settings.enable_cpss.geolocation", false, out Error);
                GlobalRequestContext.SetPreference("accessibility.captions.live_caption_enabled", false, out Error);

                GlobalRequestContext.SetPreference("autofill.credit_card_enabled", false, out Error);
                GlobalRequestContext.SetPreference("autofill.profile_enabled", false, out Error);
                GlobalRequestContext.SetPreference("autofill.save_data", false, out Error);
                GlobalRequestContext.SetPreference("autofill.enabled", false, out Error);
                GlobalRequestContext.SetPreference("payments.can_make_payment_enabled", false, out Error);
                GlobalRequestContext.SetPreference("credentials_enable_service", false, out Error);
                GlobalRequestContext.SetPreference("profile.password_manager_enabled", false, out Error);

                //GlobalRequestContext.SetPreference("scroll_to_text_fragment_enabled", false, out Error);
                GlobalRequestContext.SetPreference("url_keyed_anonymized_data_collection.enabled", false, out Error);

                GlobalRequestContext.SetPreference("download_bubble_enabled", false, out Error);
                GlobalRequestContext.SetPreference("download_bubble.partial_view_enabled", false, out Error);
                GlobalRequestContext.SetPreference("download_duplicate_file_prompt_enabled", false, out Error);
                //GlobalRequestContext.SetPreference("profile.default_content_setting_values.automatic_downloads", 1, out Error);
                //GlobalRequestContext.SetPreference("profile.default_content_setting_values.notifications", 2, out Error);

                GlobalRequestContext.SetPreference("shopping_list_enabled", false, out Error);
                GlobalRequestContext.SetPreference("browser_labs_enabled", false, out Error);
                GlobalRequestContext.SetPreference("allow_dinosaur_easter_egg", false, out Error);
                GlobalRequestContext.SetPreference("feedback_allowed", false, out Error);
                GlobalRequestContext.SetPreference("policy.feedback_surveys_enabled", false, out Error);
                GlobalRequestContext.SetPreference("ntp.promo_visible", false, out Error);
                GlobalRequestContext.SetPreference("ntp.shortcust_visible", false, out Error);
                GlobalRequestContext.SetPreference("ntp_snippets.enable", false, out Error);
                GlobalRequestContext.SetPreference("ntp_snippets_by_dse.enable", false, out Error);
                GlobalRequestContext.SetPreference("search.suggest_enabled", false, out Error);
                GlobalRequestContext.SetPreference("side_search.enabled", false, out Error);
                GlobalRequestContext.SetPreference("translate.enabled", false, out Error);
                GlobalRequestContext.SetPreference("history.saving_disabled", false, out Error);
                GlobalRequestContext.SetPreference("media_router.enable_media_router", false, out Error);
                GlobalRequestContext.SetPreference("documentsuggest.enabled", false, out Error);
                GlobalRequestContext.SetPreference("alternate_error_pages.enabled", false, out Error);
                GlobalRequestContext.SetPreference("https_first_balanced_mode_enabled", true, out Error);
                //GlobalRequestContext.SetPreference("enable_do_not_track", bool.Parse(GlobalSave.Get("DoNotTrack")), out errorMessage);
                //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/preloading/preloading_prefs.h
                GlobalRequestContext.SetPreference("net.network_prediction_options", HighPerformanceMode ? 0 : 2, out Error);
                GlobalRequestContext.SetPreference("safebrowsing.enabled", false, out Error);
                //GlobalRequestContext.SetPreference("browser.theme.follows_system_colors", false, out Error);

                GlobalRequestContext.SetPreference("browser.enable_spellchecking", bool.Parse(GlobalSave.Get("SpellCheck")), out Error);
                //GlobalRequestContext.SetPreference("spellcheck.use_spelling_service", false, out Error);
                GlobalRequestContext.SetPreference("spellcheck.dictionaries", Languages.Select(i => i.Tooltip), out Error);
                GlobalRequestContext.SetPreference("intl.accept_languages", Languages.Select(i => i.Tooltip), out Error);

                GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !PDFViewerExtension, out Error);
                GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !PDFViewerExtension, out Error);

                if (bool.Parse(GlobalSave.Get("BlockFingerprint")))
                    GlobalRequestContext.SetPreference("webrtc.ip_handling_policy", "disable_non_proxied_udp", out Error);
                //GlobalRequestContext.SetPreference("profile.content_settings.enable_quiet_permission_ui.geolocation", false, out Error);
            });
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

        public string GenerateCannotConnect(string Url, CefErrorCode ErrorCode, string ErrorText)
        {
            string Host = Utils.Host(Url);
            string HTML = Cannot_Connect_Error.Replace("{Site}", Host).Replace("{Error}", ErrorText);
            switch (ErrorCode)
            {
                case CefErrorCode.ConnectionTimedOut:
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
                    break;
                default:
                    HTML = HTML.Replace("{Description}", $"Error Code: {ErrorCode}");
                    break;
            }
            return HTML;
        }

        public const string Cannot_Connect_Error = @"<html><head><title>Unable to connect to {Site}</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Unable to connect to {Site}</h2><h5 id=""description"">{Description}</h5><h5 id=""error"" style=""margin:0px; color:#646464;"">{Error}</h5></div></body></html>";
        public const string Process_Crashed_Error = @"<html><head><title>Process crashed</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Process crashed</h2><h5>Process crashed while attempting to load content. Refresh the page to resolve the problem.</h5></div></body></html>";
        public const string Deception_Error = @"<html><head><title>Deceptive site ahead</title><style>body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Deceptive site ahead</h2><h5>The site may contain deceptive content that may trick you into installing software or revealing personal information.</h5></div></body></html>";
        public const string Malware_Error = @"<html><head><title>Dangerous site ahead</title><style>html{background:darkred;}body{text-align:center;width:100%;margin:0px;font-family:'Segoe UI',Tahoma,sans-serif;}h5{font-weight:500;}#content{width:100%;margin-top:140px;}.icon{font-family:'Segoe Fluent Icons';font-size:150px;user-select:none;}a{color:skyblue;text-decoration:none;}</style></head><body><div id=""content""><h1 class=""icon""></h1><h2>Dangerous site ahead</h2><h5>The site may install harmful and malicious software that may manipulate or steal personal information.</h5></div></body></html>";

        private void SetCEFFlags(CefSettings Settings)
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
            //force-gpu-mem-available-mb https://source.chromium.org/chromium/chromium/src/+/main:gpu/command_buffer/service/gpu_switches.cc
            //disable-file-system Disable FileSystem API.
        }
        public const string DummyUrl = "dummy.invalid";
        private void SetUrlFlags(CefSettings Settings)
        {
            //https://github.com/melo936/ChromiumHardening/blob/main/flags/chrome-command-line.md

            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/gaia/gaia_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chromecast/net/net_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:google_apis/gcm/engine/gservices_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/google/core/common/google_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/policy/core/common/policy_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/common/chrome_switches.cc
            Settings.CefCommandLineArgs.Add("connectivity-check-url", "https://cp.cloudflare.com/generate_204");
            Settings.CefCommandLineArgs.Add("sync-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("gaia-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("gcm-checkin-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("gcm-mcs-endpoint", DummyUrl);
            Settings.CefCommandLineArgs.Add("gcm-registration-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("google-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("google-apis-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("google-base-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("lso-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("model-quality-service-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("oauth-account-manager-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("secure-connect-api-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/variations/variations_switches.cc
            Settings.CefCommandLineArgs.Add("variations-server-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("variations-insecure-server-url", DummyUrl);

            Settings.CefCommandLineArgs.Add("device-management-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("realtime-reporting-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("encrypted-reporting-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("file-storage-server-upload-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/search_provider_logos/switches.cc
            Settings.CefCommandLineArgs.Add("google-doodle-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("third-party-doodle-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("search-provider-logo-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/translate/core/common/translate_switches.cc
            Settings.CefCommandLineArgs.Add("translate-script-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:components/autofill/core/common/autofill_features.cc
            Settings.CefCommandLineArgs.Add("autofill-server-url", DummyUrl);

            //https://source.chromium.org/chromium/chromium/src/+/main:chromecast/base/chromecast_switches.cc
            Settings.CefCommandLineArgs.Add("override-metrics-upload-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("crash-server-url", DummyUrl);
            Settings.CefCommandLineArgs.Add("ignore-google-port-numbers");
        }

        private void SetChromeFlags(CefSettings Settings)
        {
            //https://source.chromium.org/chromium/chromium/src/+/main:tools/perf/testdata/crossbench_output/speedometer_3.0/speedometer_3.0.json?q=disable-component-update&ss=chromium%2Fchromium%2Fsrc
            //Settings.CefCommandLineArgs.Add("disable-crashpad-for-testing");
            //Settings.CefCommandLineArgs.Add("disable-sync");
            Settings.CefCommandLineArgs.Add("disable-translate");
            Settings.CefCommandLineArgs.Add("disable-variations-seed-fetch");

            //Settings.CefCommandLineArgs.Add("disable-fre");
            Settings.CefCommandLineArgs.Add("no-default-browser-check");
            Settings.CefCommandLineArgs.Add("no-first-run");
            //Settings.CefCommandLineArgs.Add("disable-first-run-ui");
            //Settings.CefCommandLineArgs.Add("disable-ntp-most-likely-favicons-from-server");
            //Settings.CefCommandLineArgs.Add("disable-client-side-phishing-detection");
            Settings.CefCommandLineArgs.Add("disable-domain-reliability");


            Settings.CefCommandLineArgs.Add("disable-chrome-tracing-computation");
            //Settings.CefCommandLineArgs.Add("disable-scroll-to-text-fragment");

            //Settings.CefCommandLineArgs.Add("disable-ntp-other-sessions-menu");
            Settings.CefCommandLineArgs.Add("disable-default-apps");

            Settings.CefCommandLineArgs.Add("disable-modal-animations");
            //Settings.CefCommandLineArgs.Add("material-design-ink-drop-animation-speed", "fast");

            Settings.CefCommandLineArgs.Add("no-network-profile-warning");

            Settings.CefCommandLineArgs.Add("disable-login-animations");
            Settings.CefCommandLineArgs.Add("disable-stack-profiler");
            Settings.CefCommandLineArgs.Add("disable-system-font-check");
            //Settings.CefCommandLineArgs.Add("disable-infobars");
            Settings.CefCommandLineArgs.Add("disable-breakpad");
            Settings.CefCommandLineArgs.Add("disable-crash-reporter");
            Settings.CefCommandLineArgs.Add("disable-crashpad-forwarding");

            Settings.CefCommandLineArgs.Add("disable-top-sites");
            //Settings.CefCommandLineArgs.Add("disable-minimum-show-duration");
            //Settings.CefCommandLineArgs.Add("disable-startup-promos-for-testing");
            //Settings.CefCommandLineArgs.Add("disable-contextual-search");
            Settings.CefCommandLineArgs.Add("no-service-autorun");
            Settings.CefCommandLineArgs.Add("disable-auto-reload");
            //Settings.CefCommandLineArgs.Add("bypass-account-already-used-by-another-profile-check");

            //Settings.CefCommandLineArgs.Add("metrics-recording-only");

            //Settings.CefCommandLineArgs.Add("disable-cloud-policy-on-signin");

            //Settings.CefCommandLineArgs.Add("disable-dev-shm-usage");
            Settings.CefCommandLineArgs.Add("disable-dinosaur-easter-egg"); //enable-dinosaur-easter-egg-alt-images
            //Settings.CefCommandLineArgs.Add("oobe-skip-new-user-check-for-testing");

            //Settings.CefCommandLineArgs.Add("disable-gaia-services"); //https://source.chromium.org/chromium/chromium/src/+/main:ash/constants/ash_switches.cc
            
            Settings.CefCommandLineArgs.Add("wm-window-animations-disabled");
            Settings.CefCommandLineArgs.Add("animation-duration-scale", "0");
            Settings.CefCommandLineArgs.Add("disable-histogram-customizer");

            //REMOVE MOST CHROMIUM POPUPS
            Settings.CefCommandLineArgs.Add("suppress-message-center-popups");
            Settings.CefCommandLineArgs.Add("disable-prompt-on-repost");
            Settings.CefCommandLineArgs.Add("propagate-iph-for-testing");
            Settings.CefCommandLineArgs.Add("disable-search-engine-choice-screen");
            Settings.CefCommandLineArgs.Add("ash-no-nudges");
            Settings.CefCommandLineArgs.Add("noerrdialogs");
            Settings.CefCommandLineArgs.Add("disable-notifications"); //CLOUDFLARE ISSUE

            //Settings.CefCommandLineArgs.Add("hide-crash-restore-bubble");
            //Settings.CefCommandLineArgs.Add("disable-chrome-login-prompt");
        }

        private void SetFrameworkFlags(CefSettings Settings)
        {
            if (!LiteMode)
            {
                Settings.CefCommandLineArgs.Add("enable-webassembly-baseline");
                Settings.CefCommandLineArgs.Add("enable-webassembly-tiering");
                Settings.CefCommandLineArgs.Add("enable-webassembly-lazy-compilation");
                Settings.CefCommandLineArgs.Add("enable-webassembly-memory64");
            }

            if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
            {
                //Settings.CefCommandLineArgs.Add("webtransport-developer-mode");
                Settings.CefCommandLineArgs.Add("enable-experimental-cookie-features");

                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-webassembly-jspi");

                Settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");

                Settings.CefCommandLineArgs.Add("enable-javascript-harmony");
                Settings.CefCommandLineArgs.Add("enable-javascript-experimental-shared-memory");

                Settings.CefCommandLineArgs.Add("enable-future-v8-vm-features");
                //Settings.CefCommandLineArgs.Add("enable-hardware-secure-decryption-experiment");
                //Settings.CefCommandLineArgs.Add("text-box-trim");

                //Settings.CefCommandLineArgs.Add("enable-devtools-experiments");

                Settings.CefCommandLineArgs.Add("enable-webgl-developer-extensions");
                //Settings.CefCommandLineArgs.Add("enable-webgl-draft-extensions");
                //Settings.CefCommandLineArgs.Add("enable-webgpu-developer-features");
                Settings.CefCommandLineArgs.Add("enable-experimental-extension-apis");
            }
        }

        private void SetBackgroundFlags(CefSettings Settings)
        {
            //DISABLES ECOSIA SEARCHBOX
            //Settings.CefCommandLineArgs.Add("headless"); //Run in headless mode without a UI or display server dependencies.

            //https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/graphics/dark_mode_settings_builder.cc
            //https://chromium.googlesource.com/chromium/src/+/refs/heads/main/third_party/blink/renderer/platform/graphics/dark_mode_settings.h
            Settings.CefCommandLineArgs.Add("dark-mode-settings", "ImagePolicy=1");//,ImageClassifierPolicy=0,InversionAlgorithm=2

            //Disabling site isolation somehow increases memory usage by 10 MB
            /*Settings.CefCommandLineArgs.Add("no-sandbox");
            Settings.CefCommandLineArgs.Add("disable-setuid-sandbox");
            Settings.CefCommandLineArgs.Add("site-isolation-trial-opt-out");
            Settings.CefCommandLineArgs.Add("disable-site-isolation-trials");
            Settings.CefCommandLineArgs.Add("isolate-origins", "https://challenges.cloudflare.com");*/

            //Settings.CefCommandLineArgs.Add("enable-raster-side-dark-mode-for-images");

            Settings.CefCommandLineArgs.Add("process-per-site");
            Settings.CefCommandLineArgs.Add("password-store", "basic");
            Settings.CefCommandLineArgs.Add("disable-mipmap-generation"); // Disables mipmap generation in Skia. Used a workaround for select low memory devices

            //This change makes it so when EnableHighResolutionTimer(true) which is on AC power the timer is 1ms and EnableHighResolutionTimer(false) is 4ms.
            //https://bugs.chromium.org/p/chromium/issues/detail?id=153139
            Settings.CefCommandLineArgs.Add("disable-highres-timer");


            //Settings.CefCommandLineArgs.Add("enable-throttle-display-none-and-visibility-hidden-cross-origin-iframes"); //Causes memory to be 100 MB more than if disabled when minimized
            //Settings.CefCommandLineArgs.Add("quick-intensive-throttling-after-loading"); //Causes memory to be 100 MB more than if disabled when minimized
            Settings.CefCommandLineArgs.Add("intensive-wake-up-throttling-policy", "1");
            if (LiteMode)
            {
                //Turns device memory into 0.5
                Settings.CefCommandLineArgs.Add("enable-low-end-device-mode"); //Causes memory to be 20 MB more when minimized, but reduces 80 MB when not minimized

                Settings.CefCommandLineArgs.Add("disable-best-effort-tasks"); //NO LONGER PREVENTS GOOGLE LOGIN IN 27/6/2025

                Settings.CefCommandLineArgs.Add("disable-smooth-scrolling");

                Settings.CefCommandLineArgs.Add("disable-low-res-tiling"); //https://codereview.chromium.org/196473007/

                Settings.CefCommandLineArgs.Add("force-prefers-reduced-motion");
                Settings.CefCommandLineArgs.Add("disable-logging");

                Settings.CefCommandLineArgs.Add("max-web-media-player-count", "1");
                // 75 for desktop browsers and 40 for mobile browsers
                //https://chromium-review.googlesource.com/c/chromium/src/+/2816118

                Settings.CefCommandLineArgs.Add("gpu-program-cache-size-kb", $"{128 * 1024}");
                Settings.CefCommandLineArgs.Add("gpu-disk-cache-size-kb", $"{128 * 1024}");

                Settings.CefCommandLineArgs.Add("force-effective-connection-type", "Slow-2G");
                //Settings.CefCommandLineArgs.Add("num-raster-threads", "4"); //RETIRED FLAG
                Settings.CefCommandLineArgs.Add("renderer-process-limit", "4");
                //Settings.CefCommandLineArgs.Add("use-mobile-user-agent");
            }
            else
            {
                if (!bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
                    Settings.CefCommandLineArgs.Add("enable-low-res-tiling");
                if (HighPerformanceMode)
                {
                    Settings.CefCommandLineArgs.Add("no-pre-read-main-dll");
                    Settings.CefCommandLineArgs.Remove("disable-mipmap-generation");
                    Settings.CefCommandLineArgs.Remove("disable-highres-timer");
                    Settings.CefCommandLineArgs.Remove("intensive-wake-up-throttling-policy");
                    Settings.CefCommandLineArgs.Add("disable-ipc-flooding-protection");
                    Settings.CefCommandLineArgs.Add("disable-renderer-backgrounding");
                    Settings.CefCommandLineArgs.Add("disable-background-timer-throttling");
                    Settings.CefCommandLineArgs.Add("disable-extensions-http-throttling");
                    Settings.CefCommandLineArgs.Add("allow-http-background-page");

                    Settings.CefCommandLineArgs.Add("enable-benchmarking");
                    //Settings.CefCommandLineArgs.Add("enable-benchmarking-api");
                    Settings.CefCommandLineArgs.Add("enable-net-benchmarking");

                    Settings.CefCommandLineArgs.Add("scheduler-configuration");
                    Settings.CefCommandLineArgs.Add("font-cache-shared-handle"); //Increases memory by 5 MB
                    //audio-process-high-priority
                }
                else
                {
                    Settings.CefCommandLineArgs.Add("gpu-program-cache-size-kb", $"{2 * 1024 * 1024}");
                    Settings.CefCommandLineArgs.Add("gpu-disk-cache-size-kb", $"{2 * 1024 * 1024}");
                    //Settings.CefCommandLineArgs.Add("component-updater", "fast-update");
                }
            }

            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_switches.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/optimization_guide/hints_fetcher_browsertest.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:components/optimization_guide/core/optimization_guide_features.cc
            Settings.CefCommandLineArgs.Add("disable-fetching-hints-at-navigation-start");
            Settings.CefCommandLineArgs.Add("disable-model-download-verification");
            Settings.CefCommandLineArgs.Add("disable-component-update");
            Settings.CefCommandLineArgs.Add("component-updater", "disable-background-downloads,disable-delta-updates"); //https://source.chromium.org/chromium/chromium/src/+/main:components/component_updater/component_updater_command_line_config_policy.cc


            //https://github.com/portapps/brave-portable/issues/26
            //https://github.com/chromium/chromium/blob/2ca8c5037021c9d2ecc00b787d58a31ed8fc8bcb/third_party/blink/renderer/bindings/core/v8/v8_cache_options.h
            //Settings.CefCommandLineArgs.Add("v8-cache-options");

            //Settings.CefCommandLineArgs.Add("disable-v8-idle-tasks");


            Settings.CefCommandLineArgs.Add("enable-parallel-downloading");
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

        private void SetGraphicsFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("in-process-gpu");
            if (bool.Parse(GlobalSave.Get("ChromiumHardwareAcceleration")))
            {
                Settings.CefCommandLineArgs.Add("enable-gpu");
                Settings.CefCommandLineArgs.Add("enable-zero-copy");
                Settings.CefCommandLineArgs.Add("disable-software-rasterizer");
                Settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
                //Settings.CefCommandLineArgs.Add("gpu-rasterization-msaa-sample-count", MainSave.Get("MSAASampleCount"));
                //if (MainSave.Get("AngleGraphicsBackend").ToLower() != "default")
                //    Settings.CefCommandLineArgs.Add("use-angle", MainSave.Get("AngleGraphicsBackend"));
                Settings.CefCommandLineArgs.Add("enable-accelerated-2d-canvas");
                /*if (LiteMode)
                    Settings.CefCommandLineArgs.Add("use-webgpu-power-preference", "force-low-power");
                else
                    Settings.CefCommandLineArgs.Add("use-webgpu-power-preference", "default-low-power");*/
            }
            else
            {
                Settings.CefCommandLineArgs.Add("disable-gpu");
                Settings.CefCommandLineArgs.Add("disable-gpu-compositing");
                Settings.CefCommandLineArgs.Add("disable-gpu-vsync");
                Settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");
                Settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-encode");
                Settings.CefCommandLineArgs.Add("disable-accelerated-video-decode");
                Settings.CefCommandLineArgs.Add("disable-accelerated-mjpeg-decode");
                Settings.CefCommandLineArgs.Add("disable-video-capture-use-gpu-memory-buffer");
            }
        }

        private void SetNetworkFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("enable-tls13-early-data");

            Settings.CefCommandLineArgs.Add("reduce-accept-language");
            //Settings.CefCommandLineArgs.Add("reduce-transfer-size-updated-ipc");

            //Settings.CefCommandLineArgs.Add("enable-network-information-downlink-max");
            //Settings.CefCommandLineArgs.Add("enable-precise-memory-info");

            Settings.CefCommandLineArgs.Add("enable-quic");
            Settings.CefCommandLineArgs.Add("enable-spdy4");
            Settings.CefCommandLineArgs.Add("enable-ipv6");

            Settings.CefCommandLineArgs.Add("no-proxy-server");
            //Settings.CefCommandLineArgs.Add("winhttp-proxy-resolver");
            Settings.CefCommandLineArgs.Add("no-pings");

            Settings.CefCommandLineArgs.Add("disable-background-networking");
            Settings.CefCommandLineArgs.Add("disable-component-extensions-with-background-pages");
        }

        private void SetSecurityFlags(CefSettings Settings)
        {
            Settings.CefCommandLineArgs.Add("unsafely-disable-devtools-self-xss-warnings");
            Settings.CefCommandLineArgs.Add("disallow-doc-written-script-loads");

            if (bool.Parse(GlobalSave.Get("BlockFingerprint")))
            {
                //Settings.UserAgentProduct = "";
                if (GlobalSave.Get("FingerprintLevel") == "Strict")
                {
                    Settings.CefCommandLineArgs.Add("disable-reading-from-canvas");
                    Settings.CefCommandLineArgs.Add("disable-webgl");
                    Settings.CefCommandLineArgs.Add("disable-extensions");
                }
                //Settings.CefCommandLineArgs.Remove("reduce-accept-language");
                //Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
            }
        }

        private void SetMediaFlags(CefSettings Settings)
        {
            //Settings.CefCommandLineArgs.Add("enable-canvas-2d-dynamic-rendering-mode-switching");

            //Settings.CefCommandLineArgs.Add("autoplay-policy", HighPerformanceMode ? "no-user-gesture-required" : "user-gesture-required");
            Settings.CefCommandLineArgs.Add("autoplay-policy", LiteMode ? "user-gesture-required" : "no-user-gesture-required");
            Settings.CefCommandLineArgs.Add("animated-image-resume");
            Settings.CefCommandLineArgs.Add("disable-image-animation-resync");
            Settings.CefCommandLineArgs.Add("disable-checker-imaging");

            if (LiteMode)
                Settings.CefCommandLineArgs.Add("enable-lite-video");
            //Settings.CefCommandLineArgs.Add("lite-video-force-override-decision");

            //FLAG SEEMS TO NOT EXIST BUT IT DOES WORK
            Settings.CefCommandLineArgs.Add("enable-speech-input");

            //BREAKS PERMISSIONS, DO NOT ADD
            //Settings.CefCommandLineArgs.Add("enable-media-stream");

            //Settings.CefCommandLineArgs.Add("enable-media-session-service");

            Settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            //Settings.CefCommandLineArgs.Add("disable-rtc-smoothness-algorithm");
            Settings.CefCommandLineArgs.Add("auto-select-desktop-capture-source", "Entire screen");

            //Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-always");
            //Settings.CefCommandLineArgs.Add("turn-off-streaming-media-caching-on-battery");
        }

        private void SetFeatureFlags(CefSettings Settings)
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
            Settings.CefCommandLineArgs.Add("force-fieldtrials", "SimpleCacheTrial/ExperimentYes/");
            
            string JsFlags = "";
            if (HighPerformanceMode)
                JsFlags = "";
            else
                JsFlags = "--max-old-space-size=512,--optimize-gc-for-battery,--memory-reducer-favors-memory,--efficiency-mode,--battery-saver-mode";// "--always-opt,--gc-global,--gc-experiment-reduce-concurrent-marking-tasks";

            //DEFAULT ENABLED: MemoryPurgeInBackground, stop-in-background
            //ANDROID: InputStreamOptimizations
            //MemorySaverModeRenderTuning
            //MemoryPurgeOnFreezeLimit
            //DevToolsImprovedNetworkError
            //MHTML_Improvements, OptimizeHTMLElementUrls,WebFontsCacheAwareTimeoutAdaption, EstablishGpuChannelAsync
            //https://source.chromium.org/chromium/chromium/src/+/main:components/download/public/common/download_features.cc
            //https://source.chromium.org/chromium/chromium/src/+/main:services/network/public/cpp/features.cc
            string EnableFeatures = "HeapProfilerReporting,ReducedReferrerGranularity,ThirdPartyStoragePartitioning,PrecompileInlineScripts,OptimizeHTMLElementUrls,UseEcoQoSForBackgroundProcess,EnableLazyLoadImageForInvisiblePage,ParallelDownloading,TrackingProtection3pcd,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
            string DisableFeatures = "KAnonymityService,NetworkTimeServiceQuerying,LiveCaption,DefaultWebAppInstallation,PersistentHistograms,Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,SegmentationPlatform,InstalledApp,BrowsingTopics,Fledge,FledgeBiddingAndAuctionServer,InterestFeedContentSuggestions,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
            //WebBluetooth,MediaRouter,
            string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility";
            string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI,DocumentPictureInPictureAPI";

            try
            {
                Settings.CefCommandLineArgs.Add("disable-features", DisableFeatures);
                Settings.CefCommandLineArgs.Add("enable-features", EnableFeatures);
                Settings.CefCommandLineArgs.Add("enable-blink-features", EnableBlinkFeatures);
                Settings.CefCommandLineArgs.Add("disable-blink-features", DisableBlinkFeatures);
            }
            catch
            {
                Settings.CefCommandLineArgs["disable-features"] += "," + DisableFeatures;
                Settings.CefCommandLineArgs["enable-features"] += "," + EnableFeatures;
                Settings.CefCommandLineArgs["enable-blink-features"] += "," + EnableBlinkFeatures;
                Settings.CefCommandLineArgs["disable-blink-features"] += "," + DisableBlinkFeatures;
            }

            //https://github.com/Alex313031/thorium/blob/main/src/chrome/browser/chrome_content_browser_client.cc
            //https://chromium.googlesource.com/chromium/src/third_party/+/master/blink/renderer/core/frame/settings.json5
            //https://chromium.googlesource.com/chromium/src/+/HEAD/third_party/blink/public/platform/web_effective_connection_type.h
            Settings.CefCommandLineArgs.Add("blink-settings", "hyperlinkAuditingEnabled=false,smoothScrollForFindEnabled=true,disallowFetchForDocWrittenScriptsInMainFrame=true");

            if (HighPerformanceMode)
            {
                JsFlags += ",--fast-math";
                Settings.CefCommandLineArgs["blink-settings"] += ",lazyLoadEnabled=false";
                //https://www.aboutchromebooks.com/chrome-flagsscheduler-configuration/
                Settings.CefCommandLineArgs["enable-features"] += ",SchedulerConfiguration:scheduler_configuration/enabled";
            }
            else
            {
                Settings.CefCommandLineArgs["blink-settings"] += ",lowPriorityIframesThreshold=5,dnsPrefetchingEnabled=false,doHtmlPreloadScanning=false";
                Settings.CefCommandLineArgs["enable-features"] += ",LowLatencyCanvas2dImageChromium,LowLatencyWebGLImageChromium,NoStatePrefetchHoldback,ReduceCpuUtilization2,MemorySaverModeRenderTuning,OomIntervention,QuickIntensiveWakeUpThrottlingAfterLoading,LowerHighResolutionTimerThreshold,BatterySaverModeAlignWakeUps,RestrictThreadPoolInBackground,IntensiveWakeUpThrottling:grace_period_seconds/5,MemoryCacheStrongReference,OptOutZeroTimeoutTimersFromThrottling,CheckHTMLParserBudgetLessOften,Canvas2DHibernation,Canvas2DHibernationReleaseTransferMemory";
                Settings.CefCommandLineArgs["disable-features"] += ",LoadingPredictorPrefetch,SpeculationRulesPrefetchFuture,NavigationPredictor,Prerender2MainFrameNavigation,Prerender2NoVarySearch,Prerender2";

                Settings.CefCommandLineArgs["enable-blink-features"] += ",SkipPreloadScanning,LazyInitializeMediaControls,LazyFrameLoading,LazyImageLoading";
                Settings.CefCommandLineArgs["disable-blink-features"] += ",Prerender2";

                if (LiteMode)
                {
                    //https://github.com/cypress-io/cypress/issues/22622
                    //https://issues.chromium.org/issues/40220332
                    Settings.CefCommandLineArgs["disable-features"] += ",LoadingTasksUnfreezable,LogJsConsoleMessages,BoostImagePriority,BoostImageSetLoadingTaskPriority,BoostFontLoadingTaskPriority,BoostVideoLoadingTaskPriority,BoostRenderBlockingStyleLoadingTaskPriority,BoostNonRenderBlockingStyleLoadingTaskPriority";
                    Settings.CefCommandLineArgs["enable-features"] += ",AllowAggressiveThrottlingWithWebSocket,stop-in-background,ClientHintsSaveData,SaveDataImgSrcset,LowPriorityScriptLoading,LowPriorityAsyncScriptExecution";
                    Settings.CefCommandLineArgs["enable-blink-features"] += ",PrefersReducedData,ForceReduceMotion";//
                    Settings.CefCommandLineArgs["blink-settings"] += ",imageAnimationPolicy=1,prefersReducedTransparency=true,prefersReducedMotion=true,lazyLoadingFrameMarginPxUnknown=0,lazyLoadingFrameMarginPxOffline=0,lazyLoadingFrameMarginPxSlow2G=0,lazyLoadingFrameMarginPx2G=0,lazyLoadingFrameMarginPx3G=0,lazyLoadingFrameMarginPx4G=0,lazyLoadingImageMarginPxUnknown=0,lazyLoadingImageMarginPxOffline=0,lazyLoadingImageMarginPxSlow2G=0,lazyLoadingImageMarginPx2G=0,lazyLoadingImageMarginPx3G=0,lazyLoadingImageMarginPx4G=0";
                    JsFlags += ",--max-lazy,--lite-mode,--noexpose-wasm,--optimize-for-size";
                }
                else
                {
                    Settings.CefCommandLineArgs["blink-settings"] += ",lazyLoadingFrameMarginPxUnknown=250,lazyLoadingFrameMarginPxOffline=500,lazyLoadingFrameMarginPxSlow2G=500,lazyLoadingFrameMarginPx2G=400,lazyLoadingFrameMarginPx3G=300,lazyLoadingFrameMarginPx4G=200,lazyLoadingImageMarginPxUnknown=250,lazyLoadingImageMarginPxOffline=500,lazyLoadingImageMarginPxSlow2G=500,lazyLoadingImageMarginPx2G=400,lazyLoadingImageMarginPx3G=300,lazyLoadingImageMarginPx4G=200";
                }
                //https://chromium.googlesource.com/v8/v8/+/master/src/flags/flag-definitions.h
                JsFlags += "--efficiency-mode,--battery-saver-mode,--memory-saver-mode";
            }
            if (!LiteMode)
            {
                JsFlags += "--enable-experimental-regexp-engine-on-excessive-backtracks,--expose-wasm,--wasm-lazy-compilation,--asm-wasm-lazy-compilation,--wasm-lazy-validation,--experimental-wasm-gc,--wasm-async-compilation,--wasm-opt,--experimental-wasm-branch-hinting,--experimental-wasm-instruction-tracing";
                if (bool.Parse(GlobalSave.Get("ExperimentalFeatures")))
                    JsFlags += ",--experimental-wasm-jspi,--experimental-wasm-memory64,--experimental-wasm-type-reflection";
            }
            if (!bool.Parse(GlobalSave.Get("JIT")))
                JsFlags += ",--jitless";
            Settings.JavascriptFlags = JsFlags;
        }


        public Theme CurrentTheme;
        public Theme GetTheme(string Name = "")
        {
            if (string.IsNullOrEmpty(Name) && CurrentTheme != null)
                return CurrentTheme;
            Theme _Theme = Themes.Find(i => i.Name == Name);
            return _Theme == null ? Themes[0] : _Theme;
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
            Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
            Cef.GetGlobalRequestContext().ClearHttpAuthCredentialsAsync();
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content))
                {
                    if (BrowserView != null && BrowserView.Chromium != null && BrowserView.Chromium.IsBrowserInitialized && BrowserView.Chromium.CanExecuteJavascriptInMainFrame)
                    {
                        BrowserView.Chromium.ExecuteScriptAsync("localStorage.clear();sessionStorage.clear();");
                        using (var DevToolsClient = BrowserView.Chromium.GetDevToolsClient())
                        {
                            //https://github.com/cefsharp/CefSharp/issues/1234
                            DevToolsClient.Storage.ClearDataForOriginAsync("*", "all");
                            DevToolsClient.Page.ClearCompilationCacheAsync();
                            DevToolsClient.Page.ResetNavigationHistoryAsync();
                            DevToolsClient.Network.ClearBrowserCookiesAsync();
                            DevToolsClient.Network.ClearBrowserCacheAsync();
                        }
                    }
                }
            }
            InformationDialogWindow InfoWindow = new InformationDialogWindow("Alert", $"Settings", "All browsing data has been cleared", "\ue713");
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
                string SearchString = $"{_SearchProvider.Name}<#>{_SearchProvider.SearchUrl}<#>{_SearchProvider.SuggestUrl}";
                SearchSave.Set(i.ToString(), SearchString, false);
            }
            SearchSave.Save();

            AllowListSave.Clear();
            AllowListSave.Set("Count", AdBlockAllowList.AllDomains.Count().ToString(), false);
            int d = 0;
            foreach (string Domain in AdBlockAllowList.AllDomains)
            {
                AllowListSave.Set(d.ToString(), Domain, false);
                d++;
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
            {
                Save();
            }
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
                    BitmapImage _Image = new BitmapImage(new Uri("https://t0.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&size=24&url=" + Utils.CleanUrl(Url, true, true, true, false, false)));
                    if (_Image.CanFreeze)
                        _Image.Freeze();
                    return _Image;
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

        public static void SaveImageSourceToFile(ImageSource Source, string FilePath)
        {
            if (Source is RenderTargetBitmap Bitmap)
            {
                PngBitmapEncoder Encoder = new PngBitmapEncoder();
                Encoder.Frames.Add(BitmapFrame.Create(Bitmap));
                using (var Stream = File.Open(FilePath, FileMode.Create))
                    Encoder.Save(Stream);
            }
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
        public static ImageSource GenerateProfileIcon(string BaseIconPath, string Initial, int Size = 64)//,ProfileIconStyle IconStyle = ProfileIconStyle.Default
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

        public async Task<BitmapImage> SetIcon(string IconUrl, string Url = "", bool IsPrivate = false)
        {
            if (Utils.GetFileExtension(Url) != ".pdf")
            {
                if (!IsPrivate && Utils.IsHttpScheme(IconUrl))
                {
                    try
                    {
                        byte[] ImageData = await DownloadImageDataAsync(IconUrl);
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
                            return Bitmap;
                        }
                        else
                            return TabIcon;
                    }
                    catch { return TabIcon; }
                }
                else if (IconUrl.StartsWith("data:image/", StringComparison.Ordinal))
                {
                    try
                    {
                        return Utils.ConvertBase64ToBitmapImage(IconUrl);
                    }
                    catch { }
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
        private async Task<byte[]> DownloadImageDataAsync(string uri)
        {
            using (WebClient _WebClient = new WebClient())
            {
                try
                {
                    _WebClient.Headers.Add("User-Agent", UserAgentGenerator.BuildChromeBrand());
                    _WebClient.Headers.Add("Accept", "image/*;");
                    return await _WebClient.DownloadDataTaskAsync(new Uri(uri));
                }
                catch { return null; }
            }
        }

        public bool GoogleSafeBrowsing;
        public bool MobileView;

        public void SetGoogleSafeBrowsing(bool Toggle)
        {
            GoogleSafeBrowsing = Toggle;
            GlobalSave.Set("GoogleSafeBrowsing", Toggle);
        }

        public void SetMobileView(bool Toggle)
        {
            UserAgent = Toggle ? UserAgentGenerator.BuildMobileUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}") : UserAgentGenerator.BuildUserAgentFromProduct($"SLBr/{ReleaseVersion} {UserAgentGenerator.BuildChromeBrand()}");
            MobileView = Toggle;
            UserAgentData = new UserAgentMetadata
            {
                Brands = new List<UserAgentBrandVersion>
                {
                    new UserAgentBrandVersion
                    {
                        Brand = "SLBr",
                        Version = ReleaseVersion.Split('.')[0]
                    },
                    new UserAgentBrandVersion
                    {
                        Brand = "Chromium",
                        Version = Cef.ChromiumVersion.Split('.')[0]
                    }
                },
                Architecture = Toggle ? "arm" : UserAgentGenerator.GetCPUArchitecture(),
                Model = "",
                Platform = Toggle ? "Android" : "Windows",
                PlatformVersion = Toggle ? "10" : UserAgentGenerator.GetPlatformVersion(),//https://textslashplain.com/2021/09/21/determining-os-platform-version/
                FullVersion = Cef.ChromiumVersion,
                Mobile = Toggle
            };
            UserAgentBrandsString = string.Join(", ", UserAgentData.Brands.Select(b => $"\"{b.Brand}\";v=\"{b.Version}\""));

            GlobalSave.Set("MobileView", Toggle);
            bool BlockFingerprint = bool.Parse(GlobalSave.Get("BlockFingerprint", false.ToString()));
            foreach (MainWindow _Window in AllWindows)
            {
                foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.Chromium != null && i.Chromium.IsBrowserInitialized))
                {
                    BrowserView.UserAgentBranding = !BrowserView.Private && !BlockFingerprint;
                    if (BrowserView.UserAgentBranding)
                        BrowserView.Chromium.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync(UserAgent, null, null, UserAgentData);
                }
            }
        }

        public void SetDimUnloadedIcon(bool Toggle)
        {
            GlobalSave.Set("DimUnloadedIcon", Toggle);
            foreach (MainWindow _Window in AllWindows)
                _Window.SetDimUnloadedIcon(Toggle);
        }
        public void SetAppearance(Theme _Theme, int TabAlignment, bool CompactTab, bool AllowHomeButton, bool AllowTranslateButton, bool AllowReaderModeButton, int ShowExtensionButton, int ShowFavouritesBar)
        {
            GlobalSave.Set("TabAlignment", TabAlignment);

            GlobalSave.Set("TranslateButton", AllowTranslateButton);
            GlobalSave.Set("HomeButton", AllowHomeButton);
            GlobalSave.Set("ReaderButton", AllowReaderModeButton);
            GlobalSave.Set("ExtensionButton", ShowExtensionButton);
            GlobalSave.Set("FavouritesBar", ShowFavouritesBar);

            CurrentTheme = _Theme;
            GlobalSave.Set("Theme", CurrentTheme.Name);

            int IconSize = 40;
            int DPI = 95;
            TextBlock _TextBlock = new TextBlock
            {
                FontFamily = new FontFamily("Segoe Fluent Icons"),
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
                _Window.SetAppearance(_Theme, TabAlignment, CompactTab, AllowHomeButton, AllowTranslateButton, AllowReaderModeButton, ShowExtensionButton, ShowFavouritesBar);
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

        //SwitchBrowser = 11,
        
        CloseSideBar = 22,
        NewsFeed = 23,

        Print = 30,
        Mute = 31,
        Find = 32,

        ZoomIn = 40,
        ZoomOut = 41,
        ZoomReset = 42,

        HardRefresh = 50,
        ClearCacheHardRefresh = 51,
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
        public const string AntiCloseScript = "window.close=function(){};";
        public const string AntiFullscreenScript = "Element.prototype.requestFullscreen=function(){};Element.prototype.webkitRequestFullscreen=function(){};document.exitFullscreen=function(){};document.webkitExitFullscreen=function(){};Object.defineProperties(document,{fullscreenElement:{get:()=>null},webkitFullscreenElement:{get:()=>null}});";
    
        public const string ReaderScript = @"const tagsToRemove=['header','footer','nav','aside','ads','script'];
tagsToRemove.forEach(tag=>{
    const elements=document.getElementsByTagName(tag);
    while(elements[0]){elements[0].parentNode.removeChild(elements[0]);}
});
const selectorsToRemove=['.ad','.sidebar','#ad-container','.footer','.nav','.site-top-menu','.site-header','.site-footer','.sub-headerbar','.article-left-sidebar','.article-right-sidebar','.article_bottom_text','.read-next-panel','.article-meta-author-details','.onopen-discussion-panel','.author-wrapper','.follow','.share-list','.article-social-share-top','.recommended-intersection-ref','.engagement-widgets','#further-reading','.trending','.detailDiscovery','.globalFooter','.relatedlinks','#social_zone','#user-feedback','#user-feedback-button','.feedback-section','#opinionsListing'];
selectorsToRemove.forEach(selector=>{
    document.querySelectorAll(selector).forEach(element=>{element.parentNode.removeChild(element);});
});
const article=document.querySelector('article');
if (article){
    document.body.innerHTML='';
    document.body.appendChild(article);
} else {
    const mainContent=document.getElementById('main-content');
    if (mainContent){
        document.body.innerHTML='';
        document.body.appendChild(mainContent);
    }
}";

        public const string ReaderCSS = @"* {
    box-shadow: none !important;
}
body {
    max-width: 800px !important;
    margin: 0 auto !important;
    padding: 20px !important;
    background-color: #f4f4f4 !important;
    color: #333 !important;
    font-family: 'Arial', sans-serif !important;
    line-height: 1.6 !important;
    font-size: 18px !important;
    border: none !important;
}
div {
    background: none !important;
    font-family: 'Arial', sans-serif !important;
    border: none !important;
}
article {
    background: none !important;
    width: 100% !important;
    margin: 0 !important;
    padding: 0;
    font-family: 'Arial', sans-serif !important;
}
section {
    background: none !important;
    width: 100% !important;
    margin: 0 !important;
    padding: 0;
    font-family: 'Arial', sans-serif !important;
}
h1, h2, h3, h4 {
    font-family: 'Arial', sans-serif !important;
    font-weight: bold !important;
    color: #333 !important;
    margin-top: 50px !important;
    margin-bottom: 6.25px !important;
    padding: 0 0 6.25px !important;
    border-radius: 0 !important;
    border-top: none !important;
    border-left: none !important;
    border-right: none !important;
    border-bottom: 2.5px solid gainsboro !important;
}
span {
    color: #333 !important;
    padding: 0 !important;
    background: none !important;
}
p {
    color: #333 !important;
    padding: 0 !important;
    background: none !important;
}
a {
    color: cornflowerblue !important;
    text-decoration: none !important;
    background: none !important;
}
a:hover {
    filter: brightness(75%) !important;
}
pre {
    border-radius: 10px !important;
    background: white !important;
    border: 2.5px solid gainsboro !important;
    padding: 10px !important;
}
blockquote {
    padding: 25px !important;
    border-radius: 10px !important;
    background: gainsboro !important;
    margin: 0 !important;
    border: none !important;
}
blockquote {
    border-radius: 10px !important;
    background: white !important;
    border: 2.5px solid gainsboro !important;
}
figure {
    width: 100% !important;
}
video {
    max-width: 100% !important;
    width: 100% !important;
    height: auto !important;
    border-radius: 10px !important;
}
img {
    max-width: 100% !important;
    width: 100% !important;
    height: auto !important;
    border-radius: 10px !important;
}";
        public const string ArticleScript = "(function(){var metaTags=document.getElementsByTagName('meta');for(var i=0;i<metaTags.length;i++){if (metaTags[i].getAttribute('property')==='og:type'&&metaTags[i].getAttribute('content')==='article'){return true;}if (metaTags[i].getAttribute('name')==='article:author'){return true;}}return false;})();";
        public const string TabUnloadScript = @"function SLBrSetupMediaListeners(mediaElement) {
    if (mediaElement.tagName==='AUDIO'&&(mediaElement.muted||mediaElement.volume===0)){return;}
    mediaElement.removeEventListener('play',function(){engine.postMessage({type:""Media"",event:1});});
    mediaElement.removeEventListener('pause',function(){engine.postMessage({type:""Media"",event:0});});
    mediaElement.removeEventListener('ended',function(){engine.postMessage({type:""Media"",event:0});});
    mediaElement.addEventListener('play',function(){engine.postMessage({type:""Media"",event:1});});
    mediaElement.addEventListener('pause',function(){engine.postMessage({type:""Media"",event:0});});
    mediaElement.addEventListener('ended',function(){engine.postMessage({type:""Media"",event:0});});
}
new MutationObserver(function(mutationsList){
    for (let mutation of mutationsList){
        if (mutation.type==='childList'){
            mutation.addedNodes.forEach(function(node) {
                if (node.tagName==='VIDEO'||node.tagName==='AUDIO')
                    SLBrSetupMediaListeners(node);
                else if (node.querySelectorAll)
                    node.querySelectorAll('video,audio').forEach(function(mediaElement) {SLBrSetupMediaListeners(mediaElement);});
            });
        }
    }
}).observe(document.body,{childList:true,subtree:true});
document.querySelectorAll('video,audio').forEach(function(mediaElement){SLBrSetupMediaListeners(mediaElement);});";
        public const string FileScript = @"document.documentElement.setAttribute('style',""display:table;margin:auto;"")
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
    element.setAttribute('style',""font-family:'Segoe Fluent Icons';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;color:navajowhite;"")
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
            element.setAttribute('style',""font-family:'Segoe Fluent Icons';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;color:navajowhite;"")
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
            element.setAttribute('style',""font-family:'Segoe Fluent Icons';margin:0;padding:0;display:inline;vertical-align:middle;user-select:none;"")
        }
        row.querySelector('td').prepend(element);
        row.children.item(0).setAttribute('style',""text-align:left;padding:7.5px;"");
        row.children.item(1).setAttribute('style',""text-align:center;padding:7.5px;"");
        row.children.item(2).setAttribute('style',""text-align:center;padding:7.5px;"");
    }
});";
        public const string NotificationPolyfill = @"class Notification {
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
window.Notification = Notification;";
        public const string WebStoreScript = @"function scanButton(){
const buttonQueries = ['button span[jsname]:not(:empty)']
for (const button of document.querySelectorAll(buttonQueries.join(','))){
    const text=button.textContent||''
    if (text==='Add to Chrome'||text==='Remove from Chrome')
      button.textContent=text.replace('Chrome','SLBr')
  }
}
scanButton();
new MutationObserver(scanButton).observe(document.body,{attributes:true,childList:true,subtree:true});";
        public const string YouTubeSkipAdScript = @"(function() {
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
        //public const string YouTubeHideAdScript = "var style=document.createElement('style');style.textContent=`ytd-action-companion-ad-renderer,ytd-display-ad-renderer,ytd-video-masthead-ad-advertiser-info-renderer,ytd-video-masthead-ad-primary-video-renderer,ytd-in-feed-ad-layout-renderer,ytd-ad-slot-renderer,yt-about-this-ad-renderer,yt-mealbar-promo-renderer,ytd-statement-banner-renderer,ytd-ad-slot-renderer,ytd-in-feed-ad-layout-renderer,ytd-banner-promo-renderer-backgroundstatement-banner-style-type-compact,.ytd-video-masthead-ad-v3-renderer,div#root.style-scope.ytd-display-ad-renderer.yt-simple-endpoint,div#sparkles-container.style-scope.ytd-promoted-sparkles-web-renderer,div#main-container.style-scope.ytd-promoted-video-renderer,div#player-ads.style-scope.ytd-watch-flexy,ad-slot-renderer,ytm-promoted-sparkles-web-renderer,masthead-ad,tp-yt-iron-overlay-backdrop,#masthead-ad{display:none !important;}`;document.head.appendChild(style);";
        public const string ScrollCSS = "var scstyle=document.createElement('style');scstyle.textContent=`::-webkit-scrollbar {width:15px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color:transparent;}::-webkit-scrollbar-thumb {height:56px;border-radius:10px;border:5px solid transparent;background-clip:content-box;background-color: gainsboro;transition:background-color 0.5s;}::-webkit-scrollbar-thumb:hover{background-color:gray;transition:background-color 0.5s;}::-webkit-scrollbar-corner{background-color:transparent;}`;document.head.append(scstyle);";
        public const string ScrollScript = @"!function(){var s,i,c,a,o={frameRate:150,animationTime:400,stepSize:100,pulseAlgorithm:!0,pulseScale:4,pulseNormalize:1,accelerationDelta:50,accelerationMax:3,keyboardSupport:!0,arrowScroll:50,fixedBackground:!0,excluded:""""},p=o,u=!1,d=!1,l={x:0,y:0},f=!1,m=document.documentElement,h=[],v={left:37,up:38,right:39,down:40,spacebar:32,pageup:33,pagedown:34,end:35,home:36},y={37:1,38:1,39:1,40:1};function b(){if(!f&&document.body){f=!0;var e=document.body,t=document.documentElement,o=window.innerHeight,n=e.scrollHeight;if(m=0<=document.compatMode.indexOf(""CSS"")?t:e,s=e,p.keyboardSupport&&Y(""keydown"",D),top!=self)d=!0;else if(o<n&&(e.offsetHeight<=o||t.offsetHeight<=o)){var r,a=document.createElement(""div"");a.style.cssText=""position:absolute; z-index:-10000; top:0; left:0; right:0; height:""+m.scrollHeight+""px"",document.body.appendChild(a),c=function(){r||(r=setTimeout(function(){u||(a.style.height=""0"",a.style.height=m.scrollHeight+""px"",r=null)},500))},setTimeout(c,10),Y(""resize"",c);if((i=new R(c)).observe(e,{attributes:!0,childList:!0,characterData:!1}),m.offsetHeight<=o){var l=document.createElement(""div"");l.style.clear=""both"",e.appendChild(l)}}p.fixedBackground||u||(e.style.backgroundAttachment=""scroll"",t.style.backgroundAttachment=""scroll"")}}var g=[],S=!1,x=Date.now();function k(d,f,m){var e,t;if(e=0<(e=f)?1:-1,t=0<(t=m)?1:-1,(l.x!==e||l.y!==t)&&(l.x=e,l.y=t,g=[],x=0),1!=p.accelerationMax){var o=Date.now()-x;if(o<p.accelerationDelta){var n=(1+50/o)/2;1<n&&(n=Math.min(n,p.accelerationMax),f*=n,m*=n)}x=Date.now()}if(g.push({x:f,y:m,lastX:f<0?.99:-.99,lastY:m<0?.99:-.99,start:Date.now()}),!S){var r=q(),h=d===r||d===document.body;null==d.$scrollBehavior&&function(e){var t=M(e);if(null==B[t]){var o=getComputedStyle(e,"""")[""scroll-behavior""];B[t]=""smooth""==o}return B[t]}(d)&&(d.$scrollBehavior=d.style.scrollBehavior,d.style.scrollBehavior=""auto"");var w=function(e){for(var t=Date.now(),o=0,n=0,r=0;r<g.length;r++){var a=g[r],l=t-a.start,i=l>=p.animationTime,c=i?1:l/p.animationTime;p.pulseAlgorithm&&(c=F(c));var s=a.x*c-a.lastX>>0,u=a.y*c-a.lastY>>0;o+=s,n+=u,a.lastX+=s,a.lastY+=u,i&&(g.splice(r,1),r--)}h?window.scrollBy(o,n):(o&&(d.scrollLeft+=o),n&&(d.scrollTop+=n)),f||m||(g=[]),g.length?j(w,d,1e3/p.frameRate+1):(S=!1,null!=d.$scrollBehavior&&(d.style.scrollBehavior=d.$scrollBehavior,d.$scrollBehavior=null))};j(w,d,0),S=!0}}function e(e){f||b();var t=e.target;if(e.defaultPrevented||e.ctrlKey)return!0;if(N(s,""embed"")||N(t,""embed"")&&/\.pdf/i.test(t.src)||N(s,""object"")||t.shadowRoot)return!0;var o=-e.wheelDeltaX||e.deltaX||0,n=-e.wheelDeltaY||e.deltaY||0;o||n||(n=-e.wheelDelta||0),1===e.deltaMode&&(o*=40,n*=40);var r=z(t);return r?!!function(e){if(!e)return;h.length||(h=[e,e,e]);e=Math.abs(e),h.push(e),h.shift(),clearTimeout(a),a=setTimeout(function(){try{localStorage.SS_deltaBuffer=h.join("","")}catch(e){}},1e3);var t=120<e&&P(e);return!P(120)&&!P(100)&&!t}(n)||(1.2<Math.abs(o)&&(o*=p.stepSize/120),1.2<Math.abs(n)&&(n*=p.stepSize/120),k(r,o,n),e.preventDefault(),void C()):!d||!W||(Object.defineProperty(e,""target"",{value:window.frameElement}),parent.wheel(e))}function D(e){var t=e.target,o=e.ctrlKey||e.altKey||e.metaKey||e.shiftKey&&e.keyCode!==v.spacebar;document.body.contains(s)||(s=document.activeElement);var n=/^(button|submit|radio|checkbox|file|color|image)$/i;if(e.defaultPrevented||/^(textarea|select|embed|object)$/i.test(t.nodeName)||N(t,""input"")&&!n.test(t.type)||N(s,""video"")||function(e){var t=e.target,o=!1;if(-1!=document.URL.indexOf(""www.youtube.com/watch""))do{if(o=t.classList&&t.classList.contains(""html5-video-controls""))break}while(t=t.parentNode);return o}(e)||t.isContentEditable||o)return!0;if((N(t,""button"")||N(t,""input"")&&n.test(t.type))&&e.keyCode===v.spacebar)return!0;if(N(t,""input"")&&""radio""==t.type&&y[e.keyCode])return!0;var r=0,a=0,l=z(s);if(!l)return!d||!W||parent.keydown(e);var i=l.clientHeight;switch(l==document.body&&(i=window.innerHeight),e.keyCode){case v.up:a=-p.arrowScroll;break;case v.down:a=p.arrowScroll;break;case v.spacebar:a=-(e.shiftKey?1:-1)*i*.9;break;case v.pageup:a=.9*-i;break;case v.pagedown:a=.9*i;break;case v.home:l==document.body&&document.scrollingElement&&(l=document.scrollingElement),a=-l.scrollTop;break;case v.end:var c=l.scrollHeight-l.scrollTop-i;a=0<c?c+10:0;break;case v.left:r=-p.arrowScroll;break;case v.right:r=p.arrowScroll;break;default:return!0}k(l,r,a),e.preventDefault(),C()}function t(e){s=e.target}var n,r,M=(n=0,function(e){return e.uniqueID||(e.uniqueID=n++)}),E={},T={},B={};function C(){clearTimeout(r),r=setInterval(function(){E=T=B={}},1e3)}function H(e,t,o){for(var n=o?E:T,r=e.length;r--;)n[M(e[r])]=t;return t}function z(e){var t=[],o=document.body,n=m.scrollHeight;do{var r=(!1?E:T)[M(e)];if(r)return H(t,r);if(t.push(e),n===e.scrollHeight){var a=O(m)&&O(o)||X(m);if(d&&L(m)||!d&&a)return H(t,q())}else if(L(e)&&X(e))return H(t,e)}while(e=e.parentElement)}function L(e){return e.clientHeight+10<e.scrollHeight}function O(e){return""hidden""!==getComputedStyle(e,"""").getPropertyValue(""overflow-y"")}function X(e){var t=getComputedStyle(e,"""").getPropertyValue(""overflow-y"");return""scroll""===t||""auto""===t}function Y(e,t,o){window.addEventListener(e,t,o||!1)}function A(e,t,o){window.removeEventListener(e,t,o||!1)}function N(e,t){return e&&(e.nodeName||"""").toLowerCase()===t.toLowerCase()}if(window.localStorage&&localStorage.SS_deltaBuffer)try{h=localStorage.SS_deltaBuffer.split("","")}catch(e){}function K(e,t){return Math.floor(e/t)==e/t}function P(e){return K(h[0],e)&&K(h[1],e)&&K(h[2],e)}var $,j=window.requestAnimationFrame||window.webkitRequestAnimationFrame||window.mozRequestAnimationFrame||function(e,t,o){window.setTimeout(e,o||1e3/60)},R=window.MutationObserver||window.WebKitMutationObserver||window.MozMutationObserver,q=($=document.scrollingElement,function(){if(!$){var e=document.createElement(""div"");e.style.cssText=""height:10000px;width:1px;"",document.body.appendChild(e);var t=document.body.scrollTop;document.documentElement.scrollTop,window.scrollBy(0,3),$=document.body.scrollTop!=t?document.body:document.documentElement,window.scrollBy(0,-3),document.body.removeChild(e)}return $});function V(e){var t;return((e*=p.pulseScale)<1?e-(1-Math.exp(-e)):(e-=1,(t=Math.exp(-1))+(1-Math.exp(-e))*(1-t)))*p.pulseNormalize}function F(e){return 1<=e?1:e<=0?0:(1==p.pulseNormalize&&(p.pulseNormalize/=V(1)),V(e))}
try{window.addEventListener(""test"",null,Object.defineProperty({},""passive"",{get:function(){ee=!0}}))}catch(e){}var te=!!ee&&{passive:!1},oe=""onwheel""in document.createElement(""div"")?""wheel"":""mousewheel"";function ne(e){for(var t in e)o.hasOwnProperty(t)&&(p[t]=e[t])}oe&&(Y(oe,e,te),Y(""mousedown"",t),Y(""load"",b)),ne.destroy=function(){i&&i.disconnect(),A(oe,e),A(""mousedown"",t),A(""keydown"",D),A(""resize"",c),A(""load"",b)},window.SmoothScrollOptions&&ne(window.SmoothScrollOptions),""function""==typeof define&&define.amd?define(function(){return ne}):""object""==typeof exports?module.exports=ne:window.SmoothScroll=ne}();
SmoothScroll({animationTime:400,stepSize:100,accelerationDelta:50,accelerationMax:3,keyboardSupport:true,arrowScroll:50,pulseAlgorithm:true,pulseScale:4,pulseNormalize:1,touchpadSupport:false,fixedBackground:true,excluded:''});";
        public const string ExtensionScript = "var rect=document.body.getBoundingClientRect();engine.postMessage({width:rect.width+16,height:rect.height+40});";
        public const string SharpenImageScript = "var spstyle=document.createElement('style');spstyle.textContent=`img,video{transition:filter 0.3s ease-in-out;filter: contrast(1.2) brightness(1.1) saturate(1.2) !important;}`;document.head.appendChild(spstyle);";

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

        //public const string YouTubeAdCSS = "var SLBrYTStyle=document.createElement('style');SLBrYTStyle.textContent=`ytd-engagement-panel-section-list-renderer{display:none;}`;document.head.append(SLBrYTStyle);";
    }
}
