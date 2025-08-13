using CefSharp;
using CefSharp.DevTools.CSS;
using Microsoft.Win32;
using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.UI.Text;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }

        private ObservableCollection<ActionStorage> PrivateAddableLanguages = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> AddableLanguages
        {
            get { return PrivateAddableLanguages; }
            set
            {
                PrivateAddableLanguages = value;
                RaisePropertyChanged("AddableLanguages");
            }
        }

        public Settings(Browser _BrowserView)
        {
            InitializeComponent();
            BrowserView = _BrowserView;
        }

        public void Dispose()
        {
            PrivateAddableLanguages.Clear();
            AddableLanguages.Clear();
        }

        Browser BrowserView;

        public void RemoveLocale(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    if (App.Instance.Languages.Count == 1)
                    {
                        var infoWindow = new InformationDialogWindow("Alert", $"Settings", "Unable to remove languages", "\uece4");
                        infoWindow.Topmost = true;
                        infoWindow.ShowDialog();
                        return;
                    }
                    ActionStorage _Language = App.Instance.Languages.Where(i => i.Tooltip == ((FrameworkElement)sender).Tag.ToString()).First();
                    App.Instance.Languages.Remove(_Language);

                    /*If replace all items in the collection and have more than 10 items.
                     * There is a significant performance improvement to gain used the Clear() -> Add() method.
                     * The Add() method is very heavy to use - every add operation refreshes the whole layout of program.
                     * Instead, use INotifyPropertyChanged pattern and simply replace the collection like this:
                     * MyObservableCollection = new ObservableCollection<T>(ListOfItems);
                     * MyObservableCollection needs to invoke the PropertyChanged event (or you can do it manually afterwords)
                     * Doing this can speed up collection renders significantly.*/

                    PrivateAddableLanguages.Add(_Language);
                    var sortedList = PrivateAddableLanguages.OrderBy(x => x.Tooltip).ToList();
                    PrivateAddableLanguages.Clear();
                    foreach (var item in sortedList)
                        PrivateAddableLanguages.Add(item);
                    RaisePropertyChanged("AddableLanguages");
                }
            }
            catch { }
        }

        public void AddLocale(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    ActionStorage _Language = AddableLanguages.Where(i => i.Tooltip == ((FrameworkElement)sender).Tag.ToString()).First();
                    App.Instance.Languages.Add(_Language);
                    AddableLanguages.Remove(_Language);
                }
            }
            catch { }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            BrowserView.Tab.ParentWindow.NewTab(e.Uri.ToString(), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
            e.Handled = true;
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.CurrentFocusedWindow().NewTab(((FrameworkElement)sender).Tag.ToString(), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                if (LanguageSelection.SelectedIndex == -1)
                    LanguageSelection.SelectedIndex = 0;
                App.Instance.Locale = App.Instance.Languages[LanguageSelection.SelectedIndex];

                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();

                    string Error;
                    IEnumerable<string> LocaleStrings = App.Instance.Languages.Select(i => i.Tooltip);
                    GlobalRequestContext.SetPreference("spellcheck.dictionaries", LocaleStrings, out Error);
                    GlobalRequestContext.SetPreference("intl.accept_languages", LocaleStrings, out Error);
                });
            }
        }

        bool SettingsInitialized = false;

        List<string> _Fonts;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsInitialized = false;
            List<string> ISOs = App.Instance.Languages.Select(i => i.Tooltip).ToList();
            if (AddableLanguages.Count == 0)
            {
                foreach (KeyValuePair<string, string> Locale in App.Instance.AllLocales)
                {
                    if (!ISOs.Contains(Locale.Key))
                        AddableLanguages.Add(new ActionStorage(Locale.Value, App.Instance.GetLocaleIcon(Locale.Key), Locale.Key));
                }
                AddableLanguages = new ObservableCollection<ActionStorage>(AddableLanguages.OrderBy(x => x.Tooltip));
            }

            LanguageSelection.ItemsSource = App.Instance.Languages;
            LanguageSelection.SelectedValue = App.Instance.Locale;
            LanguageSelection.SelectionChanged += LanguageSelection_SelectionChanged;
            AddLanguageListMenu.ItemsSource = AddableLanguages;

            foreach (string Url in App.Instance.SearchEngines.Select(i => i.Name))
            {
                if (!SearchEngineComboBox.Items.Contains(Url))
                    SearchEngineComboBox.Items.Add(Url);
            }
            SearchEngineComboBox.SelectionChanged += SearchEngineComboBox_SelectionChanged;
            string SearchEngine = App.Instance.DefaultSearchProvider.Name;
            if (SearchEngineComboBox.Items.Contains(SearchEngine))
                SearchEngineComboBox.SelectedValue = SearchEngine;

            HomepageTextBox.Text = App.Instance.GlobalSave.Get("Homepage");
            bool IsNewTab = HomepageTextBox.Text.StartsWith("slbr://newtab", StringComparison.Ordinal);
            HomepageBackgroundComboBox.IsEnabled = IsNewTab;
            BingBackgroundComboBox.IsEnabled = IsNewTab;
            BackgroundImageTextBox.IsEnabled = IsNewTab;
            BackgroundQueryTextBox.IsEnabled = IsNewTab;

            DownloadPathText.Text = App.Instance.GlobalSave.Get("DownloadPath");
            ScreenshotPathText.Text = App.Instance.GlobalSave.Get("ScreenshotPath");

            PrivateTabsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs"));
            RestoreTabsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RestoreTabs"));
            DownloadFaviconsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("Favicons"));
            CheckUpdateCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("CheckUpdate"));
            SmoothScrollCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll"));
            QuickImageCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("QuickImage"));
            SuppressErrorCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SuppressError"));
            EnhanceImageCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("EnhanceImage"));
            SpellCheckCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SpellCheck"));
            DownloadPromptCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DownloadPrompt"));
            ExternalFontsCheckBox.IsChecked = App.Instance.ExternalFonts;


            bool TabUnloadingCheck = bool.Parse(App.Instance.GlobalSave.Get("TabUnloading"));
            TabUnloadingCheckBox.IsChecked = TabUnloadingCheck;
            TabUnloadingTimeComboBox.IsEnabled = TabUnloadingCheck;
            DimIconsWhenUnloadedCheckBox.IsEnabled = TabUnloadingCheck;
            ShowUnloadedIconCheckBox.IsEnabled = TabUnloadingCheck;
            ShowUnloadTimeLeftCheckBox.IsEnabled = TabUnloadingCheck;

            AdaptiveThemeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme"));


            NeverSlowModeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("NeverSlowMode"));
            AMPCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AMP"));

            AdBlockComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("AdBlock");


            SkipAdsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SkipAds"));

            GoogleSafeBrowsingCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("GoogleSafeBrowsing"));
            MobileViewCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("MobileView"));
            ForceLazyCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ForceLazy"));

            bool BlockFingerprintChecked = bool.Parse(App.Instance.GlobalSave.Get("BlockFingerprint"));
            BlockFingerprintCheckBox.IsChecked = BlockFingerprintChecked;
            FingerprintProtectionLevelComboBox.IsEnabled = BlockFingerprintChecked;
            if (FingerprintProtectionLevelComboBox.Items.Count == 0)
            {
                FingerprintProtectionLevelComboBox.Items.Add("Minimal");
                FingerprintProtectionLevelComboBox.Items.Add("Balanced");
                FingerprintProtectionLevelComboBox.Items.Add("Random");
                FingerprintProtectionLevelComboBox.Items.Add("Strict");
            }
            FingerprintProtectionLevelComboBox.SelectionChanged += FingerprintProtectionLevelComboBox_SelectionChanged;
            FingerprintProtectionLevelComboBox.SelectedValue = App.Instance.GlobalSave.Get("FingerprintLevel");



            bool AntiTamperChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiTamper"));
            AntiTamperCheckBox.IsChecked = AntiTamperChecked;
            AntiFullscreenCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiFullscreen"));
            AntiInspectDetectCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect"));
            BypassSiteMenuCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu"));
            TextSelectionCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TextSelection"));
            RemoveFilterCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter"));
            RemoveOverlayCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay"));
            AntiFullscreenCheckBox.IsEnabled = AntiTamperChecked;
            AntiInspectDetectCheckBox.IsEnabled = AntiTamperChecked;
            BypassSiteMenuCheckBox.IsEnabled = AntiTamperChecked;
            TextSelectionCheckBox.IsEnabled = AntiTamperChecked;
            RemoveFilterCheckBox.IsEnabled = AntiTamperChecked;
            RemoveOverlayCheckBox.IsEnabled = AntiTamperChecked;



            SendDiagnosticsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SendDiagnostics"));
            WebNotificationsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WebNotifications"));

            DimIconsWhenUnloadedCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DimUnloadedIcon"));
            ShowUnloadedIconCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon"));
            ShowUnloadTimeLeftCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress"));

            int TabAlignment = App.Instance.GlobalSave.GetInt("TabAlignment");
            TabAlignmentComboBox.SelectedIndex = TabAlignment;
            CompactTabCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("CompactTab"));
            CompactTabCheckBox.Visibility = TabAlignment == 1 ? Visibility.Visible : Visibility.Collapsed;

            bool NetworkLimit = bool.Parse(App.Instance.GlobalSave.Get("NetworkLimit"));
            NetworkLimitCheckBox.IsChecked = NetworkLimit;
            BandwidthTextBox.IsEnabled = NetworkLimit;
            BandwidthUnitComboBox.IsEnabled = NetworkLimit;

            bool SearchSuggestions = bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions"));
            SearchSuggestionsCheckBox.IsChecked = SearchSuggestions;
            SmartSuggestionsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmartSuggestions"));
            SmartSuggestionsCheckBox.IsEnabled = SearchSuggestions;
            OpenSearchCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("OpenSearch"));

            FullscreenPopupCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("FullscreenPopup"));



            ChromiumHardwareAccelerationCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ChromiumHardwareAcceleration"));
            JITCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("JIT"));
            ExperimentalFeaturesCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ExperimentalFeatures"));
            StartupBoostCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("StartupBoost"));
            PDFViewerToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PDF"));

            PerformanceComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("Performance");

            RenderModeComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("RenderMode");
            BingBackgroundComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("BingBackground");

            if (TabUnloadingTimeComboBox.Items.Count == 0)
            {
                TabUnloadingTimeComboBox.Items.Add("1");
                TabUnloadingTimeComboBox.Items.Add("3");
                TabUnloadingTimeComboBox.Items.Add("5");
                TabUnloadingTimeComboBox.Items.Add("10");
                TabUnloadingTimeComboBox.Items.Add("15");
                TabUnloadingTimeComboBox.Items.Add("30");
                TabUnloadingTimeComboBox.Items.Add("45");
                TabUnloadingTimeComboBox.Items.Add("60");
                TabUnloadingTimeComboBox.Items.Add("120");
            }
            TabUnloadingTimeComboBox.SelectionChanged += TabUnloadingTimeComboBox_SelectionChanged;
            TabUnloadingTimeComboBox.SelectedValue = App.Instance.GlobalSave.Get("TabUnloadingTime");

            HomepageBackgroundComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("HomepageBackground");

            int RuntimeStyle = App.Instance.GlobalSave.GetInt("ChromiumRuntimeStyle");
            RuntimeStyleComboBox.SelectedIndex = RuntimeStyle;
            if (RuntimeStyle != 0)
                ExtensionWarning.Visibility = Visibility.Visible;

            float Bandwidth = float.Parse(App.Instance.GlobalSave.Get("Bandwidth"));
            if (Bandwidth >= 1)
            {
                BandwidthTextBox.Text = Bandwidth.ToString("0.##");
                BandwidthUnitComboBox.SelectedIndex = 0;
            }
            else
            {
                BandwidthTextBox.Text = (Bandwidth * 1000).ToString("0.##");
                BandwidthUnitComboBox.SelectedIndex = 1;
            }


            BackgroundImageTextBox.Text = App.Instance.GlobalSave.Get("CustomBackgroundImage");
            BackgroundQueryTextBox.Text = App.Instance.GlobalSave.Get("CustomBackgroundQuery");
            BackgroundImageTextBox.Visibility = HomepageBackgroundComboBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            BackgroundQueryTextBox.Visibility = Visibility.Collapsed;
            BingBackgroundComboBox.Visibility = HomepageBackgroundComboBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

            ScreenshotFormatComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("ScreenshotFormat");

            string ThemeName = App.Instance.GlobalSave.Get("Theme");
            foreach (Border ThemeItems in ThemeSelection.Items)
            {
                if ((string)ThemeItems.Tag == ThemeName)
                {
                    ThemeSelection.SelectedItem = ThemeItems;
                    break;
                }
            }

            AboutVersion.Text = $"Version {App.Instance.ReleaseVersion}";
            CEFVersion.Text = $"CEF: {(Cef.CefVersion.StartsWith("r", StringComparison.Ordinal) ? Cef.CefVersion.Substring(1, Cef.CefVersion.IndexOf("-") - 10) : Cef.CefVersion.Substring(0, Cef.CefVersion.IndexOf("-") - 10))}";
            ChromiumVersion.Text = $"Version: {Cef.ChromiumVersion}";

            AdsBlocked.Text = App.Instance.AdsBlocked.ToString();
            TrackersBlocked.Text = App.Instance.TrackersBlocked.ToString();

            HomeButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("HomeButton"));
            TranslateButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TranslateButton"));
            ReaderButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ReaderButton"));

            ExtensionButtonComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("ExtensionButton");

            FavouritesBarComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("FavouritesBar");

            if (StandardFontComboBox.Items.Count == 0)
            {
                _Fonts = Fonts.SystemFontFamilies.Select(i => i.Source).ToList();
                StandardFontComboBox.ItemsSource = _Fonts;
                SerifFontComboBox.ItemsSource = new List<string>(_Fonts);
                SansSerifFontComboBox.ItemsSource = new List<string>(_Fonts);
                FixedFontComboBox.ItemsSource = new List<string>(_Fonts);
                MathFontComboBox.ItemsSource = new List<string>(_Fonts);
            }
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var Preferences = Cef.GetGlobalRequestContext().GetAllPreferences(true);
                Dispatcher.Invoke(() =>
                {
                    if (Preferences.TryGetValue("webkit", out var WebKitObject) && WebKitObject is IDictionary<string, object> WebKit)
                    {
                        if (WebKit.TryGetValue("webprefs", out var WebPrefsObject) && WebPrefsObject is IDictionary<string, object> WebPrefs)
                        {
                            if (WebPrefs.TryGetValue("fonts", out var FontsObject) && FontsObject is IDictionary<string, object> Fonts)
                            {
                                if (Fonts.TryGetValue("standard", out var StandardFontMap) && StandardFontMap is IDictionary<string, object> StandardScriptMap)
                                {
                                    if (StandardScriptMap.TryGetValue("Zyyy", out var StandardFont))
                                        StandardFontComboBox.SelectedItem = StandardFont;
                                }
                                if (Fonts.TryGetValue("serif", out var SerifFontMap) && SerifFontMap is IDictionary<string, object> SerifScriptMap)
                                {
                                    if (SerifScriptMap.TryGetValue("Zyyy", out var SerifFont))
                                        SerifFontComboBox.SelectedItem = SerifFont;
                                }
                                if (Fonts.TryGetValue("sansserif", out var SansSerifFontMap) && SansSerifFontMap is IDictionary<string, object> SansSerifScriptMap)
                                {
                                    if (SansSerifScriptMap.TryGetValue("Zyyy", out var SansSerifFont))
                                        SansSerifFontComboBox.SelectedItem = SansSerifFont;
                                }
                                if (Fonts.TryGetValue("fixed", out var FixedFontMap) && FixedFontMap is IDictionary<string, object> FixedScriptMap)
                                {
                                    if (FixedScriptMap.TryGetValue("Zyyy", out var FixedFont))
                                        FixedFontComboBox.SelectedItem = FixedFont;
                                }
                                if (Fonts.TryGetValue("math", out var MathFontMap) && MathFontMap is IDictionary<string, object> MathScriptMap)
                                {
                                    if (MathScriptMap.TryGetValue("Zyyy", out var MathFont))
                                        MathFontComboBox.SelectedItem = MathFont;
                                }
                            }
                            if (WebPrefs.TryGetValue("default_font_size", out var FontSize))
                                FontSizeSlider.Value = (int)FontSize;
                            if (WebPrefs.TryGetValue("minimum_font_size", out var MinimumFontSize))
                                MinimumFontSizeSlider.Value = (int)MinimumFontSize;
                        }
                    }
                });
            });

            App.Instance.LoadExtensions();
            ExtensionsList.ItemsSource = App.Instance.Extensions;

            UsernameText.Text = App.Instance.Username;

            ApplyTheme(App.Instance.CurrentTheme);
            SettingsInitialized = true;
        }

        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }


        private void HomepageBackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                int Value = HomepageBackgroundComboBox.SelectedIndex;
                App.Instance.GlobalSave.Set("HomepageBackground", Value);
                BackgroundImageTextBox.Visibility = Value == 0 ? Visibility.Visible : Visibility.Collapsed;
                BackgroundQueryTextBox.Visibility = Visibility.Collapsed;
                BingBackgroundComboBox.Visibility = Value == 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void RuntimeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ChromiumRuntimeStyle", RuntimeStyleComboBox.SelectedIndex);
        }
        private void ScreenshotFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ScreenshotFormat", ScreenshotFormatComboBox.SelectedIndex);
        }
        private void TabUnloadingTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.UpdateTabUnloadingTimer(int.Parse(TabUnloadingTimeComboBox.SelectedValue.ToString()));
        }
        private void RenderModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetRenderMode(RenderModeComboBox.SelectedIndex);
        }
        private void BingBackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("BingBackground", BingBackgroundComboBox.SelectedIndex);
        }
        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Name = SearchEngineComboBox.SelectedValue.ToString();
                App.Instance.DefaultSearchProvider = App.Instance.SearchEngines.Find(i => i.Name == Name);
                App.Instance.GlobalSave.Set("SearchEngine", Name);
            }
        }
        private void FingerprintProtectionLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("FingerprintLevel", FingerprintProtectionLevelComboBox.SelectedValue.ToString());
        }
        private void AdBlockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAdBlock(AdBlockComboBox.SelectedIndex);
        }
        private void TabAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                int TabAlignment = TabAlignmentComboBox.SelectedIndex;
                App.Instance.GlobalSave.Set("TabAlignment", TabAlignment);
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignment, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex);
                CompactTabCheckBox.Visibility = TabAlignment == 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void ExtensionButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                int ExtensionButton = ExtensionButtonComboBox.SelectedIndex;
                App.Instance.GlobalSave.Set("ExtensionButton", ExtensionButton);
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButton, FavouritesBarComboBox.SelectedIndex);
            }
        }
        private void FavouritesBarComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                int FavouritesBar = FavouritesBarComboBox.SelectedIndex;
                App.Instance.GlobalSave.Set("FavouritesBar", FavouritesBar);
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBar);
            }
        }

        private void BackgroundImageTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Utils.IsUrl(BackgroundImageTextBox.Text))
                    BackgroundImageTextBox.Text = Utils.FixUrl(BackgroundImageTextBox.Text);
                App.Instance.GlobalSave.Set("CustomBackgroundImage", BackgroundImageTextBox.Text);
            }
        }
        private void BackgroundQueryTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Utils.IsUrl(BackgroundQueryTextBox.Text))
                    BackgroundQueryTextBox.Text = Utils.FixUrl(BackgroundQueryTextBox.Text);
                App.Instance.GlobalSave.Set("CustomBackgroundQuery", BackgroundQueryTextBox.Text);
            }
        }
        private void HomepageTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && HomepageTextBox.Text.Trim().Length > 0)
            {
                HomepageTextBox.Text = Utils.FixUrl(HomepageTextBox.Text);
                App.Instance.GlobalSave.Set("Homepage", HomepageTextBox.Text);
                bool IsNewTab = HomepageTextBox.Text.StartsWith("slbr://newtab", StringComparison.Ordinal);
                HomepageBackgroundComboBox.IsEnabled = IsNewTab;
                BingBackgroundComboBox.IsEnabled = IsNewTab;
                BackgroundImageTextBox.IsEnabled = IsNewTab;
                BackgroundQueryTextBox.IsEnabled = IsNewTab;
            }
        }
        private void NeverSlowModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetNeverSlowMode(NeverSlowModeCheckBox.IsChecked.ToBool());
        }
        private void AMPCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAMP(AMPCheckBox.IsChecked.ToBool());
        }


        private void SkipAdsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetYouTube(SkipAdsCheckBox.IsChecked.ToBool());
        }


        private void AdaptiveThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("AdaptiveTheme", AdaptiveThemeCheckBox.IsChecked.ToBool().ToString());
        }
        private void PrivateTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("PrivateTabs", PrivateTabsCheckBox.IsChecked.ToBool().ToString());
        }
        private void RestoreTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("RestoreTabs", RestoreTabsCheckBox.IsChecked.ToBool().ToString());
        }
        private void DownloadFaviconsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("Favicons", DownloadFaviconsCheckBox.IsChecked.ToBool().ToString());
        }
        private void CheckUpdateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("CheckUpdate", CheckUpdateCheckBox.IsChecked.ToBool().ToString());
        }
        private void SmoothScrollCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SmoothScroll", SmoothScrollCheckBox.IsChecked.ToBool().ToString());
        }
        private void QuickImageCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("QuickImage", QuickImageCheckBox.IsChecked.ToBool().ToString());
        }
        private void SuppressErrorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SuppressError", SuppressErrorCheckBox.IsChecked.ToBool().ToString());
        }
        private void EnhanceImageCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("EnhanceImage", EnhanceImageCheckBox.IsChecked.ToBool().ToString());
        }
        private void SpellCheckCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Enabled = SpellCheckCheckBox.IsChecked.ToBool();

                App.Instance.GlobalSave.Set("SpellCheck", Enabled.ToString());
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();

                    string Error;
                    GlobalRequestContext.SetPreference("browser.enable_spellchecking", Enabled, out Error);
                    GlobalRequestContext.SetPreference("spellcheck.dictionaries", App.Instance.Languages.Select(i => i.Tooltip), out Error);
                    GlobalRequestContext.SetPreference("intl.accept_languages", App.Instance.Languages.Select(i => i.Tooltip), out Error);
                });
            }
        }
        private void SearchSuggestionsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool SearchSuggestions = SearchSuggestionsCheckBox.IsChecked.ToBool();
                SmartSuggestionsCheckBox.IsEnabled = SearchSuggestions;
                App.Instance.GlobalSave.Set("SearchSuggestions", SearchSuggestions.ToString());
            }
        }
        private void SmartSuggestionsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SmartSuggestions", SmartSuggestionsCheckBox.IsChecked.ToBool().ToString());
        }
        private void OpenSearchCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("OpenSearch", OpenSearchCheckBox.IsChecked.ToBool().ToString());
        }
        private void FullscreenPopupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("FullscreenPopup", FullscreenPopupCheckBox.IsChecked.ToBool().ToString());
        }

        private void GoogleSafeBrowsingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetGoogleSafeBrowsing(GoogleSafeBrowsingCheckBox.IsChecked.ToBool());
        }

        private void ForceLazyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ForceLazy", ForceLazyCheckBox.IsChecked.ToBool().ToString());
        }
        private void MobileViewCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetMobileView(MobileViewCheckBox.IsChecked.ToBool());
        }
        private void BlockFingerprintCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = BlockFingerprintCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("BlockFingerprint", Checked.ToString());
                FingerprintProtectionLevelComboBox.IsEnabled = Checked;

                if (!Checked)
                {
                    foreach (MainWindow _Window in App.Instance.AllWindows)
                    {
                        foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.Chromium != null && i.Chromium.IsBrowserInitialized))
                        {
                            BrowserView.UserAgentBranding = !BrowserView.Private && !Checked;
                            if (BrowserView.UserAgentBranding)
                                BrowserView.Chromium.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync(App.Instance.UserAgent, null, null, App.Instance.UserAgentData);
                        }
                    }
                }
            }
        }
        private void AntiTamperCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = AntiTamperCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("AntiTamper", Checked.ToString());
                AntiFullscreenCheckBox.IsEnabled = Checked;
                AntiInspectDetectCheckBox.IsEnabled = Checked;
                BypassSiteMenuCheckBox.IsEnabled = Checked;
                TextSelectionCheckBox.IsEnabled = Checked;
                RemoveFilterCheckBox.IsEnabled = Checked;
                RemoveOverlayCheckBox.IsEnabled = Checked;
            }
        }
        private void AntiFullscreenCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("AntiFullscreen", AntiFullscreenCheckBox.IsChecked.ToBool().ToString());
        }
        private void AntiInspectDetectCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("AntiInspectDetect", AntiInspectDetectCheckBox.IsChecked.ToBool().ToString());
        }
        private void BypassSiteMenuCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("BypassSiteMenu", BypassSiteMenuCheckBox.IsChecked.ToBool().ToString());
        }
        private void TextSelectionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("TextSelection", TextSelectionCheckBox.IsChecked.ToBool().ToString());
        }
        private void RemoveFilterCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("RemoveFilter", RemoveFilterCheckBox.IsChecked.ToBool().ToString());
        }
        private void RemoveOverlayCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("RemoveOverlay", RemoveOverlayCheckBox.IsChecked.ToBool().ToString());
        }
        private void DownloadPromptCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("DownloadPrompt", DownloadPromptCheckBox.IsChecked.ToBool().ToString());
        }
        private void TabUnloadingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = TabUnloadingCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("TabUnloading", Checked.ToString());

                TabUnloadingTimeComboBox.IsEnabled = Checked;
                DimIconsWhenUnloadedCheckBox.IsEnabled = Checked;
                ShowUnloadedIconCheckBox.IsEnabled = Checked;
                ShowUnloadTimeLeftCheckBox.IsEnabled = Checked;

                App.Instance.UpdateTabUnloadingTimer();
            }
        }

        private void SendDiagnosticsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SendDiagnostics", SendDiagnosticsCheckBox.IsChecked.ToBool().ToString());
        }
        private void WebNotificationsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("WebNotifications", WebNotificationsCheckBox.IsChecked.ToBool().ToString());
        }

        private void DimUnloadedIconCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetDimUnloadedIcon(DimIconsWhenUnloadedCheckBox.IsChecked.ToBool());
        }
        private void ShowUnloadedIconCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ShowUnloadedIcon", ShowUnloadedIconCheckBox.IsChecked.ToBool().ToString());
        }
        private void ShowUnloadTimeLeftCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("ShowUnloadProgress", ShowUnloadTimeLeftCheckBox.IsChecked.ToBool().ToString());
                App.Instance.UpdateTabUnloadingTimer();
            }
        }
        private void ChromiumHardwareAccelerationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ChromiumHardwareAcceleration", ChromiumHardwareAccelerationCheckBox.IsChecked.ToBool().ToString());
        }
        private void JITCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("JIT", JITCheckBox.IsChecked.ToBool().ToString());
        }
        private void ExperimentalFeaturesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ExperimentalFeatures", ExperimentalFeaturesCheckBox.IsChecked.ToBool().ToString());
        }
        private void StartupBoostCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Value = StartupBoostCheckBox.IsChecked.ToBool();
                if (Value) StartupManager.EnableStartup();
                else StartupManager.DisableStartup();
                App.Instance.GlobalSave.Set("StartupBoost", Value.ToString());
            }
        }

        private void CompactTabCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("CompactTab", CompactTabCheckBox.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex);
            }
        }

        private void TranslateButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("TranslateButton", TranslateButtonToggleButton.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex);
            }
        }
        private void HomeButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.GlobalSave.Set("HomeButton", HomeButtonToggleButton.IsChecked.ToBool().ToString());
            App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex);
        }
        private void ReaderButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("ReaderButton", ReaderButtonToggleButton.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, CompactTabCheckBox.IsChecked.ToBool(), HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex);
            }
        }


        private void ThemeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                Border SelectedItem = (Border)ThemeSelection.SelectedItem;
                string Text = (string)SelectedItem.Tag;
                Theme _Theme = App.Instance.GetTheme(Text);
                if (_Theme == null)
                    return;
                App.Instance.SetAppearance(_Theme, App.Instance.GlobalSave.GetInt("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("CompactTab")), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")), App.Instance.GlobalSave.GetInt("ExtensionButton"), App.Instance.GlobalSave.GetInt("FavouritesBar"));
                //App.Instance.ApplyTheme(_Theme);
                ApplyTheme(_Theme);
            }
        }
        
        private void ChangeDownloadPathButton_Click(object sender, RoutedEventArgs e)
        {
            var FolderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = DownloadPathText.Text
            };
            if (FolderDialog.ShowDialog() == true)
            {
                DownloadPathText.Text = FolderDialog.FolderName;
                App.Instance.GlobalSave.Set("DownloadPath", FolderDialog.FolderName);
            }
        }
        
        private void ChangeScreenshotPathButton_Click(object sender, RoutedEventArgs e)
        {
            var FolderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = ScreenshotPathText.Text
            };
            if (FolderDialog.ShowDialog() == true)
            {
                ScreenshotPathText.Text = FolderDialog.FolderName;
                App.Instance.GlobalSave.Set("ScreenshotPath", FolderDialog.FolderName);
            }
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            BrowserView.Action(Actions.SwitchUserPopup);
        }

        private void ClearAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new InformationDialogWindow("Confirmation", "Settings", "Clear all browsing data permanently?", "\uea99", "Yes", "No");
            infoWindow.Topmost = true;

            if (infoWindow.ShowDialog() == true)
                App.Instance.ClearAllData();
        }

        private void ExtensionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                var Target = (ToggleButton)sender;
                var Values = Target.Tag.ToString().Split("<,>", StringSplitOptions.None);
                if (Values[0] == "PDF")
                {
                    bool PDFViewerExtension = Target.IsChecked.ToBool();
                    App.Instance.GlobalSave.Set("PDF", PDFViewerExtension);
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        string Error;
                        GlobalRequestContext.SetPreference("plugins.always_open_pdf_externally", !PDFViewerExtension, out Error);
                        GlobalRequestContext.SetPreference("download.open_pdf_in_system_reader", !PDFViewerExtension, out Error);
                    });
                }
            }
        }

        private void PerformanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("Performance", PerformanceComboBox.SelectedIndex);
                App.Instance.LiteMode = PerformanceComboBox.SelectedIndex == 0;
                App.Instance.HighPerformanceMode = PerformanceComboBox.SelectedIndex == 2;
            }
        }

        private void StandardFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = StandardFontComboBox.SelectedItem.ToString();
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.fonts.standard.Zyyy", Value, out string _);
                });
            }
        }

        private void SerifFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = SerifFontComboBox.SelectedItem.ToString();
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.fonts.serif.Zyyy", Value, out string _);
                });
            }
        }

        private void SansSerifFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = SansSerifFontComboBox.SelectedItem.ToString();
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.fonts.sansserif.Zyyy", Value, out string _);
                });
            }
        }

        private void FixedFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = FixedFontComboBox.SelectedItem.ToString();
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.fonts.fixed.Zyyy", Value, out string _);
                });
            }
        }

        private void MathFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = MathFontComboBox.SelectedItem.ToString();
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.fonts.math.Zyyy", Value, out string _);
                });
            }
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SettingsInitialized)
            {
                int Value = (int)FontSizeSlider.Value;
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.default_font_size", Value, out string _);
                });
            }
        }

        private void MinimumFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SettingsInitialized)
            {
                int Value = (int)MinimumFontSizeSlider.Value;
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    var GlobalRequestContext = Cef.GetGlobalRequestContext();
                    GlobalRequestContext.SetPreference("webkit.webprefs.minimum_font_size", Value, out string _);
                });
            }
        }
        private void ExternalFontsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetExternalFonts(ExternalFontsCheckBox.IsChecked.ToBool());
        }

        public float ParseBandwidthInput()
        {
            if (!float.TryParse(BandwidthTextBox.Text, out float Value))
                return 0;
            return BandwidthUnitComboBox.SelectedIndex switch
            {
                0 => Value,
                1 => Value / 1000,
                _ => 0
            };
        }
        private void NetworkLimitCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = NetworkLimitCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("NetworkLimit", Checked.ToString());

                BandwidthTextBox.IsEnabled = Checked;
                BandwidthUnitComboBox.IsEnabled = Checked;

                float Bandwidth = Checked ? float.Parse(App.Instance.GlobalSave.Get("Bandwidth")) : 0;
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.Chromium != null && i.Chromium.IsBrowserInitialized))
                        BrowserView.LimitNetwork(0, Bandwidth, Bandwidth);
                }
            }
        }

        private void BandwidthTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Any(char.IsLetter))
                e.Handled = true;
        }

        private void BandwidthTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (SettingsInitialized)
            {
                float Bandwidth = ParseBandwidthInput();
                App.Instance.GlobalSave.Set("Bandwidth", Bandwidth);

                if (!NetworkLimitCheckBox.IsChecked.ToBool())
                    Bandwidth = 0;
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.Chromium != null && i.Chromium.IsBrowserInitialized))
                        BrowserView.LimitNetwork(0, Bandwidth, Bandwidth);
                }
            }
        }

        private void BandwidthUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                float Bandwidth = ParseBandwidthInput();
                App.Instance.GlobalSave.Set("Bandwidth", Bandwidth);

                if (!NetworkLimitCheckBox.IsChecked.ToBool())
                    Bandwidth = 0;
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.Chromium != null && i.Chromium.IsBrowserInitialized))
                        BrowserView.LimitNetwork(0, Bandwidth, Bandwidth);
                }
            }
        }
    }
}
