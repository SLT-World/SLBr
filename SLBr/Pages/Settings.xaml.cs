using CefSharp;
using CefSharp.DevTools.Database;
using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Navigation;

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
                        var infoWindow = new InformationDialogWindow("Alert", $"Settings", "You can no longer remove languages because only one language remains.", "\uece4");
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
            App.Instance.CurrentFocusedWindow().NewTab(e.Uri.ToString(), true, App.Instance.CurrentFocusedWindow().TabsUI.SelectedIndex + 1);
            e.Handled = true;
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsInitialized = false;
            List<string> ISOs = App.Instance.Languages.Select(i => i.Tooltip).ToList();
            foreach (KeyValuePair<string, string> Locale in App.Instance.AllLocales)
            {
                if (!ISOs.Contains(Locale.Key))
                    AddableLanguages.Add(new ActionStorage(Locale.Value, App.Instance.GetLocaleIcon(Locale.Key), Locale.Key));
            }
            AddableLanguages = new ObservableCollection<ActionStorage>(AddableLanguages.OrderBy(x => x.Tooltip));

            LanguageSelection.ItemsSource = App.Instance.Languages;
            LanguageSelection.SelectedValue = App.Instance.Locale;
            LanguageSelection.SelectionChanged += LanguageSelection_SelectionChanged;
            AddLanguageListMenu.ItemsSource = AddableLanguages;

            foreach (string Url in App.Instance.SearchEngines)
            {
                if (!SearchEngineComboBox.Items.Contains(Url))
                    SearchEngineComboBox.Items.Add(Url);
            }
            SearchEngineComboBox.SelectionChanged += SearchEngineComboBox_SelectionChanged;
            string SearchEngine = App.Instance.GlobalSave.Get("SearchEngine");
            if (SearchEngineComboBox.Items.Contains(SearchEngine))
                SearchEngineComboBox.SelectedValue = SearchEngine;

            HomepageTextBox.Text = App.Instance.GlobalSave.Get("Homepage");
            bool IsNewTab = HomepageTextBox.Text.StartsWith("slbr://newtab");
            HomepageBackgroundComboBox.IsEnabled = IsNewTab;
            BingBackgroundComboBox.IsEnabled = IsNewTab;
            BackgroundImageTextBox.IsEnabled = IsNewTab;
            BackgroundQueryTextBox.IsEnabled = IsNewTab;

            DownloadPathTextBox.Text = App.Instance.GlobalSave.Get("DownloadPath");
            ScreenshotPathTextBox.Text = App.Instance.GlobalSave.Get("ScreenshotPath");

            RestoreTabsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RestoreTabs"));
            SmoothScrollCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll"));
            FlagEmojiCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("FlagEmoji"));
            SpellCheckCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SpellCheck"));
            DownloadPromptCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DownloadPrompt"));


            bool TabUnloadingCheck = bool.Parse(App.Instance.GlobalSave.Get("TabUnloading"));
            TabUnloadingCheckBox.IsChecked = TabUnloadingCheck;
            TabUnloadingTimeComboBox.IsEnabled = TabUnloadingCheck;
            DimIconsWhenUnloadedCheckBox.IsEnabled = TabUnloadingCheck;
            ShowUnloadedIconCheckBox.IsEnabled = TabUnloadingCheck;
            ShowUnloadTimeLeftCheckBox.IsEnabled = TabUnloadingCheck;

            AdaptiveThemeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme"));


            NeverSlowModeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("NeverSlowMode"));
            AdBlockCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AdBlock"));
            TrackerBlockCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TrackerBlock"));




            SkipAdsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SkipAds"));
            if (VideoQualityComboBox.Items.Count == 0)
            {
                VideoQualityComboBox.Items.Add("Auto");
                VideoQualityComboBox.Items.Add("144p");
                VideoQualityComboBox.Items.Add("240p");
                VideoQualityComboBox.Items.Add("360p");
                VideoQualityComboBox.Items.Add("480p");
                VideoQualityComboBox.Items.Add("720p");
                VideoQualityComboBox.Items.Add("1080p");
                VideoQualityComboBox.Items.Add("1440p");
                VideoQualityComboBox.Items.Add("2160p");
                VideoQualityComboBox.Items.Add("4320p");
            }
            VideoQualityComboBox.SelectionChanged += VideoQualityComboBox_SelectionChanged;
            VideoQualityComboBox.SelectedValue = App.Instance.GlobalSave.Get("VideoQuality");



            GoogleSafeBrowsingCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("GoogleSafeBrowsing"));

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

            SendDiagnosticsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SendDiagnostics"));
            WebNotificationsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WebNotifications"));

            DimIconsWhenUnloadedCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DimUnloadedIcon"));
            ShowUnloadedIconCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon"));
            ShowUnloadTimeLeftCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress"));

            if (TabAlignmentComboBox.Items.Count == 0)
            {
                TabAlignmentComboBox.Items.Add("Horizontal");
                TabAlignmentComboBox.Items.Add("Vertical");
            }
            TabAlignmentComboBox.SelectionChanged += TabAlignmentComboBox_SelectionChanged;
            TabAlignmentComboBox.SelectedValue = App.Instance.GlobalSave.Get("TabAlignment");

            bool SuggestionsChecked = bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions"));
            SearchSuggestionsCheckBox.IsChecked = SuggestionsChecked;
            SuggestionsSourceComboBox.IsEnabled = SuggestionsChecked;
            if (SuggestionsSourceComboBox.Items.Count == 0)
            {
                SuggestionsSourceComboBox.Items.Add("Google");
                SuggestionsSourceComboBox.Items.Add("Bing");
                SuggestionsSourceComboBox.Items.Add("Brave Search");
                //SuggestionsSourceComboBox.Items.Add("Ecosia");
                SuggestionsSourceComboBox.Items.Add("DuckDuckGo");
                SuggestionsSourceComboBox.Items.Add("Yahoo");
                SuggestionsSourceComboBox.Items.Add("Wikipedia");
                SuggestionsSourceComboBox.Items.Add("YouTube");
            }
            SuggestionsSourceComboBox.SelectionChanged += SuggestionsSourceComboBox_SelectionChanged;
            SuggestionsSourceComboBox.SelectedValue = App.Instance.GlobalSave.Get("SuggestionsSource");



            ChromiumHardwareAccelerationCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ChromiumHardwareAcceleration"));
            ExperimentalFeaturesCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ExperimentalFeatures"));
            LiteModeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("LiteMode"));
            PDFViewerToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PDFViewerExtension"));

            if (RenderModeComboBox.Items.Count == 0)
            {
                RenderModeComboBox.Items.Add("Hardware");
                RenderModeComboBox.Items.Add("Software");
            }
            RenderModeComboBox.SelectionChanged += RenderModeComboBox_SelectionChanged;
            RenderModeComboBox.SelectedValue = App.Instance.GlobalSave.Get("RenderMode");

            if (BingBackgroundComboBox.Items.Count == 0)
            {
                BingBackgroundComboBox.Items.Add("Image of the day");
                BingBackgroundComboBox.Items.Add("Random");
            }
            BingBackgroundComboBox.SelectionChanged += BingBackgroundComboBox_SelectionChanged;
            BingBackgroundComboBox.SelectedValue = App.Instance.GlobalSave.Get("BingBackground");

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

            if (HomepageBackgroundComboBox.Items.Count == 0)
            {
                HomepageBackgroundComboBox.Items.Add("Custom");
                HomepageBackgroundComboBox.Items.Add("Bing");
                //HomepageBackgroundComboBox.Items.Add("Unsplash");
                HomepageBackgroundComboBox.Items.Add("Picsum");
            }
            HomepageBackgroundComboBox.SelectionChanged += HomepageBackgroundComboBox_SelectionChanged;
            HomepageBackgroundComboBox.SelectedValue = App.Instance.GlobalSave.Get("HomepageBackground");

            BackgroundImageTextBox.Text = App.Instance.GlobalSave.Get("CustomBackgroundImage");
            BackgroundQueryTextBox.Text = App.Instance.GlobalSave.Get("CustomBackgroundQuery");
            BackgroundImageTextBox.Visibility = App.Instance.GlobalSave.Get("HomepageBackground") == "Custom" ? Visibility.Visible : Visibility.Collapsed;
            BackgroundQueryTextBox.Visibility = Visibility.Collapsed;
            BingBackgroundComboBox.Visibility = App.Instance.GlobalSave.Get("HomepageBackground") == "Bing" ? Visibility.Visible : Visibility.Collapsed;

            if (ScreenshotFormatComboBox.Items.Count == 0)
            {
                ScreenshotFormatComboBox.Items.Add("JPG");
                ScreenshotFormatComboBox.Items.Add("PNG");
                ScreenshotFormatComboBox.Items.Add("WebP");
            }
            ScreenshotFormatComboBox.SelectionChanged += ScreenshotFormatComboBox_SelectionChanged;
            ScreenshotFormatComboBox.SelectedValue = App.Instance.GlobalSave.Get("ScreenshotFormat");

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
            CEFVersion.Text = $"CEF: {(Cef.CefVersion.StartsWith("r") ? Cef.CefVersion.Substring(1, Cef.CefVersion.IndexOf("-") - 10) : Cef.CefVersion.Substring(0, Cef.CefVersion.IndexOf("-") - 10))}";
            ChromiumVersion.Text = $"Version: {Cef.ChromiumVersion}";

            //ChromiumJSVersion.Text = $"Javascript: V8 ({App.Instance.ChromiumJSVersion})";
            //ChromiumWebkit.Text = $"Webkit: 537.36 ({App.Instance.ChromiumRevision})";

            AdsBlocked.Text = App.Instance.AdsBlocked.ToString();
            TrackersBlocked.Text = App.Instance.TrackersBlocked.ToString();

            HomeButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("HomeButton"));
            AIButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AIButton"));
            TranslateButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TranslateButton"));
            ReaderButtonToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ReaderButton"));

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
                string Value = HomepageBackgroundComboBox.SelectedValue.ToString();
                App.Instance.GlobalSave.Set("HomepageBackground", Value);
                BackgroundImageTextBox.Visibility = Value == "Custom" ? Visibility.Visible : Visibility.Collapsed;
                BackgroundQueryTextBox.Visibility = Visibility.Collapsed;
                BingBackgroundComboBox.Visibility = Value == "Bing" ? Visibility.Visible : Visibility.Collapsed;
                //BackgroundQueryTextBox.Visibility = Value == "Unsplash" ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void ScreenshotFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ScreenshotFormat", ScreenshotFormatComboBox.SelectedValue.ToString());
        }
        private void TabUnloadingTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.UpdateTabUnloadingTimer(int.Parse(TabUnloadingTimeComboBox.SelectedValue.ToString()));
        }
        private void RenderModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Instance.SetRenderMode(RenderModeComboBox.SelectedValue.ToString(), true);
        }
        private void BingBackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("BingBackground", BingBackgroundComboBox.SelectedValue.ToString());
        }
        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SearchEngine", SearchEngineComboBox.SelectedValue.ToString());
        }
        private void SuggestionsSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SuggestionsSource", SuggestionsSourceComboBox.SelectedValue.ToString());
        }
        private void FingerprintProtectionLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("FingerprintLevel", FingerprintProtectionLevelComboBox.SelectedValue.ToString());
        }
        private void TabAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string TabAlignment = TabAlignmentComboBox.SelectedValue.ToString();
                App.Instance.GlobalSave.Set("TabAlignment", TabAlignment);
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignment, bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
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
                bool IsNewTab = HomepageTextBox.Text.StartsWith("slbr://newtab");
                HomepageBackgroundComboBox.IsEnabled = IsNewTab;
                BingBackgroundComboBox.IsEnabled = IsNewTab;
                BackgroundImageTextBox.IsEnabled = IsNewTab;
                BackgroundQueryTextBox.IsEnabled = IsNewTab;
            }
        }
        private void DownloadPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("DownloadPath", DownloadPathTextBox.Text);
        }
        private void ScreenshotPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ScreenshotPath", ScreenshotPathTextBox.Text);
        }
        private void NeverSlowModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetNeverSlowMode(NeverSlowModeCheckBox.IsChecked.ToBool());
        }
        private void AdBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAdBlock(AdBlockCheckBox.IsChecked.ToBool());
        }
        private void TrackerBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetTrackerBlock(TrackerBlockCheckBox.IsChecked.ToBool());
        }


        private void SkipAdsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetYouTube(SkipAdsCheckBox.IsChecked.ToBool(), VideoQualityComboBox.SelectedValue.ToString());
        }
        private void VideoQualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetYouTube(SkipAdsCheckBox.IsChecked.ToBool(), VideoQualityComboBox.SelectedValue.ToString());
        }


        private void AdaptiveThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("AdaptiveTheme", AdaptiveThemeCheckBox.IsChecked.ToBool().ToString());
        }
        private void RestoreTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("RestoreTabs", RestoreTabsCheckBox.IsChecked.ToBool().ToString());
        }
        private void SmoothScrollCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SmoothScroll", SmoothScrollCheckBox.IsChecked.ToBool().ToString());
        }
        private void FlagEmojiCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("FlagEmoji", FlagEmojiCheckBox.IsChecked.ToBool().ToString());
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
                bool Checked = SearchSuggestionsCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("SearchSuggestions", Checked.ToString());
                SuggestionsSourceComboBox.IsEnabled = Checked;
            }
        }

        private void GoogleSafeBrowsingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetGoogleSafeBrowsing(GoogleSafeBrowsingCheckBox.IsChecked.ToBool());
        }
        private void BlockFingerprintCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = BlockFingerprintCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("BlockFingerprint", Checked.ToString());
                FingerprintProtectionLevelComboBox.IsEnabled = Checked;
            }
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
        private void ExperimentalFeaturesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ExperimentalFeatures", ExperimentalFeaturesCheckBox.IsChecked.ToBool().ToString());
        }
        /*private void PDFViewerExtensionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("PDFViewerExtension", PDFViewerExtensionCheckBox.IsChecked.ToBool().ToString());
        }*/
        private void LiteModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("LiteMode", LiteModeCheckBox.IsChecked.ToBool().ToString());
        }

        private void AIButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("AIButton", AIButtonToggleButton.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
            }
        }
        private void TranslateButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("TranslateButton", TranslateButtonToggleButton.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
            }
        }
        private void HomeButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.GlobalSave.Set("HomeButton", HomeButtonToggleButton.IsChecked.ToBool().ToString());
            App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
        }
        private void ReaderButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("ReaderButton", ReaderButtonToggleButton.IsChecked.ToBool().ToString());
                App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
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
                App.Instance.SetAppearance(_Theme, App.Instance.GlobalSave.Get("TabAlignment"), bool.Parse(App.Instance.GlobalSave.Get("HomeButton")), bool.Parse(App.Instance.GlobalSave.Get("TranslateButton")), bool.Parse(App.Instance.GlobalSave.Get("AIButton")), bool.Parse(App.Instance.GlobalSave.Get("ReaderButton")));
                //App.Instance.ApplyTheme(_Theme);
                ApplyTheme(_Theme);
            }
        }

        private void AddSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddSearchTextBox.Text.Trim().Length > 0)
            {
                if (AddSearchTextBox.Text.Contains("."))
                {
                    string Url = Utils.FixUrl(AddSearchTextBox.Text.Trim().Replace(" ", ""));
                    if (!Url.Contains("{0}"))
                        Url += "{0}";
                    App.Instance.SearchEngines.Add(Url);
                    if (!SearchEngineComboBox.Items.Contains(Url))
                        SearchEngineComboBox.Items.Add(Url);
                    SearchEngineComboBox.SelectedValue = Url;
                    App.Instance.GlobalSave.Set("SearchEngine", Url);
                    AddSearchTextBox.Text = string.Empty;
                    Keyboard.ClearFocus();
                }
            }
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            BrowserView.Action(Actions.SwitchUserPopup);
        }

        private void ExtensionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                if (sender == null)
                    return;
                var Target = (ToggleButton)sender;
                var Values = Target.Tag.ToString().Split(new string[] { "<,>" }, StringSplitOptions.None);
                if (Values[0] == "PDF")
                {
                    bool PDFViewerExtension = Target.IsChecked.ToBool();
                    App.Instance.GlobalSave.Set("PDFViewerExtension", PDFViewerExtension.ToString());
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
    }
}
