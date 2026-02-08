using CefSharp;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SLBr.Pages
{
    public interface IPageOverlay : IDisposable
    {
    }

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsPage : UserControl, IPageOverlay
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));

        private ObservableCollection<ActionStorage> PrivateAddableLanguages = new ObservableCollection<ActionStorage>();
        public ObservableCollection<ActionStorage> AddableLanguages
        {
            get { return PrivateAddableLanguages; }
            set
            {
                PrivateAddableLanguages = value;
                RaisePropertyChanged();
            }
        }

        public SettingsPage(Browser _BrowserView)
        {
            InitializeComponent();
            if (App.Instance.CurrentProfile.Type == ProfileType.System && App.Instance.CurrentProfile.Name == "Guest")
            {
                UserTab.Visibility = Visibility.Collapsed;
                BrowserTab.Visibility = Visibility.Collapsed;
                AppearanceTab.Visibility = Visibility.Collapsed;
                PrivacyTab.Visibility = Visibility.Collapsed;
                ServicesTab.Visibility = Visibility.Collapsed;
                PerformanceTab.Visibility = Visibility.Collapsed;
                TabSeparator1.Visibility = Visibility.Collapsed;
                LanguagesTab.Visibility = Visibility.Collapsed;
                DownloadsTab.Visibility = Visibility.Collapsed;
                ExtensionsTab.Visibility = Visibility.Collapsed;
                SystemTab.Visibility = Visibility.Collapsed;
                TabSeparator2.Visibility = Visibility.Collapsed;
                SettingsTabControl.SelectedItem = AboutTab;
            }
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
                        var infoWindow = new InformationDialogWindow("Error", $"Settings", "The selected languages could not be removed.", "\uece4");
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
                    RaisePropertyChanged();
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
            BrowserView.Tab.ParentWindow.NewTab(e.Uri.ToString(), true, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1, BrowserView.Private, BrowserView.Tab.TabGroup);
            e.Handled = true;
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            BrowserView.Tab.ParentWindow.NewTab(((FrameworkElement)sender).Tag.ToString(), true, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1, BrowserView.Private, BrowserView.Tab.TabGroup);
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                if (LanguageSelection.SelectedIndex == -1)
                    LanguageSelection.SelectedIndex = 0;
                App.Instance.Locale = App.Instance.Languages[LanguageSelection.SelectedIndex];
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        IRequestContext GlobalRequestContext = Cef.GetGlobalRequestContext();

                        string Error;
                        IEnumerable<string> LocaleStrings = App.Instance.Languages.Select(i => i.Tooltip);
                        GlobalRequestContext.SetPreference("spellcheck.dictionaries", LocaleStrings, out Error);
                        GlobalRequestContext.SetPreference("intl.accept_languages", LocaleStrings, out Error);
                    });
                }
            }
        }

        bool SettingsInitialized = false;

        List<string> _Fonts;

        public void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowUpdateInfoBar();
        }

        public void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.CheckUpdate();
            if (!string.IsNullOrEmpty(App.Instance.UpdateAvailable))
            {
                CheckUpdateButton.Visibility = Visibility.Collapsed;
                UpdateStatusDescription.Text = $"Version {App.Instance.UpdateAvailable}";
                if (App.Instance.UpdateAvailable == App.Instance.ReleaseVersion)
                {
                    UpdateStatusColor.Foreground = App.Instance.GreenColor;
                    UpdateStatusText.Text = "SLBr is up to date";
                }
                else
                {
                    UpdateStatusColor.Foreground = App.Instance.OrangeColor;
                    UpdateStatusText.Text = "An update for SLBr is available";
                    UpdateStatusButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                UpdateStatusColor.Foreground = (SolidColorBrush)FindResource("IndicatorBrush");
                UpdateStatusText.Text = "SLBr update check was skipped";
                CheckUpdateButton.Visibility = Visibility.Visible;
                UpdateStatusDescription.Text = $"Version {App.Instance.ReleaseVersion}";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsInitialized = false;

            if (!string.IsNullOrEmpty(App.Instance.UpdateAvailable))
            {
                CheckUpdateButton.Visibility = Visibility.Collapsed;
                UpdateStatusDescription.Text = $"Version {App.Instance.UpdateAvailable}";
                if (App.Instance.UpdateAvailable == App.Instance.ReleaseVersion)
                {
                    UpdateStatusColor.Foreground = App.Instance.GreenColor;
                    UpdateStatusText.Text = "SLBr is up to date";
                }
                else
                {
                    UpdateStatusColor.Foreground = App.Instance.OrangeColor;
                    UpdateStatusText.Text = "An update for SLBr is available";
                    UpdateStatusButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                UpdateStatusColor.Foreground = (SolidColorBrush)FindResource("IndicatorBrush");
                UpdateStatusText.Text = "SLBr update check was skipped";
                CheckUpdateButton.Visibility = Visibility.Visible;
                UpdateStatusDescription.Text = $"Version {App.Instance.ReleaseVersion}";
            }

            SyncWarning.Visibility = Visibility.Collapsed;
            SyncCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("Sync"));
            if (SyncCheckBox.IsChecked.ToBool() && !App.Instance.Synchronized)
            {
                if (string.IsNullOrEmpty(App.Instance.GlobalSave.Get("SyncGitHub")))
                    SyncWarningText.Text = "No access token found.";
                else
                    SyncWarningText.Text = "Data is not synchorized.";
                SyncWarning.Visibility = Visibility.Visible;
            }
            string[] SyncedData = App.Instance.GlobalSave.Get("SyncData").Split(',');
            SyncFavouritesToggleButton.IsChecked = SyncedData.Contains("Favourites");
            //SyncTabsToggleButton.IsChecked = SyncedData.Contains("Tabs");
            SyncSettingsToggleButton.IsChecked = SyncedData.Contains("Settings");

            List<string> ISOs = App.Instance.Languages.Select(i => i.Tooltip).ToList();
            if (AddableLanguages.Count == 0)
            {
                foreach (KeyValuePair<string, string> Locale in App.Instance.AllLocales)
                {
                    if (!ISOs.Contains(Locale.Key))
                        AddableLanguages.Add(new ActionStorage(Locale.Value, App.GetLocaleIcon(Locale.Key), Locale.Key));
                }
                AddableLanguages = new ObservableCollection<ActionStorage>(AddableLanguages.OrderBy(x => x.Tooltip));
            }

            LanguageSelection.SelectionChanged -= LanguageSelection_SelectionChanged;
            LanguageSelection.ItemsSource = App.Instance.Languages;
            LanguageSelection.SelectedValue = App.Instance.Locale;
            LanguageSelection.SelectionChanged += LanguageSelection_SelectionChanged;
            AddLanguageListMenu.ItemsSource = AddableLanguages;

            SearchEngineComboBox.ItemsSource = App.Instance.SearchEngines;
            SearchEngineComboBox.SelectedValue = App.Instance.DefaultSearchProvider;

            HomepageTextBox.Text = App.Instance.GlobalSave.Get("Homepage");
            bool IsNewTab = HomepageTextBox.Text.StartsWith("slbr://newtab");
            HomepageBackgroundComboBox.IsEnabled = IsNewTab;
            BingBackgroundComboBox.IsEnabled = IsNewTab;
            BackgroundImageTextBox.IsEnabled = IsNewTab;
            BackgroundQueryTextBox.IsEnabled = IsNewTab;

            DownloadPathText.Text = App.Instance.GlobalSave.Get("DownloadPath");
            ScreenshotPathText.Text = App.Instance.GlobalSave.Get("ScreenshotPath");

            PrivateTabsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs"));
            RestoreTabsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RestoreTabs"));

            TabPreviewCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TabPreview"));
            TabMemoryCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TabMemory"));

            bool DownloadFavicons = bool.Parse(App.Instance.GlobalSave.Get("Favicons"));
            DownloadFaviconsCheckBox.IsChecked = DownloadFavicons;
            FaviconServiceComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("FaviconService");

            CheckUpdateCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("CheckUpdate"));
            SmoothScrollCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmoothScroll"));
            QuickImageCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("QuickImage"));
            //SuppressErrorCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SuppressError"));
            SpellCheckCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SpellCheck"));
            DownloadPromptCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DownloadPrompt"));
            ExternalFontsCheckBox.IsChecked = App.Instance.ExternalFonts;


            bool TabUnloadingCheck = bool.Parse(App.Instance.GlobalSave.Get("TabUnloading"));
            TabUnloadingCheckBox.IsChecked = TabUnloadingCheck;

            AdaptiveThemeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AdaptiveTheme"));
            WarnCodecCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WarnCodec"));
            WaybackInfoBarCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WaybackInfoBar"));
            HomographInfoBarCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("HomographInfoBar"));

            NeverSlowModeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("NeverSlowMode"));
            AMPCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AMP"));

            AdBlockComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("AdBlock");
            WebRiskServiceComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("WebRiskService");

            TrimURLCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TrimURL"));
            PunycodeURLCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PunycodeURL"));
            ModernURLCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ModernURL"));
            HomographProtectionCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("HomographProtection"));

            SkipAdsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SkipAds"));
            SmartDarkModeCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmartDarkMode"));

            MobileViewCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("MobileView"));
            ForceLazyCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ForceLazy"));

            bool AntiTamperChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiTamper"));
            AntiTamperCheckBox.IsChecked = AntiTamperChecked;
            AntiFullscreenCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiFullscreen"));
            AntiInspectDetectCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("AntiInspectDetect"));
            BypassSiteMenuCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("BypassSiteMenu"));
            TextSelectionCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("TextSelection"));
            RemoveFilterCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RemoveFilter"));
            RemoveOverlayCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("RemoveOverlay"));

            SendDiagnosticsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SendDiagnostics"));
            WebNotificationsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WebNotifications"));
            WebAppsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("WebApps"));

            DimIconsWhenUnloadedCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("DimUnloadedIcon"));
            ShowUnloadedIconCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadedIcon"));
            ShowUnloadTimeLeftCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ShowUnloadProgress"));

            int TabAlignment = App.Instance.GlobalSave.GetInt("TabAlignment");
            TabAlignmentComboBox.SelectedIndex = TabAlignment;

            bool NetworkLimit = bool.Parse(App.Instance.GlobalSave.Get("NetworkLimit"));
            NetworkLimitCheckBox.IsChecked = NetworkLimit;

            bool SearchSuggestions = bool.Parse(App.Instance.GlobalSave.Get("SearchSuggestions"));
            SearchSuggestionsCheckBox.IsChecked = SearchSuggestions;
            SmartSuggestionsCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("SmartSuggestions"));
            OpenSearchCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("OpenSearch"));

            FullscreenPopupCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("FullscreenPopup"));

            BrowserHardwareAccelerationCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("BrowserHardwareAcceleration"));
            JITCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("JIT"));
            ExperimentalFeaturesCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ExperimentalFeatures"));
            StartupBoostCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("StartupBoost"));
            PDFViewerToggleButton.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("PDF"));

            PerformanceComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("Performance");
            ReduceDiskCheckBox.IsChecked = bool.Parse(App.Instance.GlobalSave.Get("ReduceDisk"));
            ReduceDiskCheckBox.IsEnabled = PerformanceComboBox.SelectedIndex == 0;

            RenderModeComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("RenderMode");
            BingBackgroundComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("BingBackground");
            ImageSearchComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("ImageSearch");
            TranslationProviderComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("TranslationProvider");
            SpellcheckProviderComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("SpellcheckProvider");
            WebEngineComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("WebEngine");
            TridentVersionComboBox.SelectedIndex = App.Instance.GlobalSave.GetInt("TridentVersion");

            TabUnloadingTimeComboBox.SelectionChanged -= TabUnloadingTimeComboBox_SelectionChanged;
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
            TabUnloadingTimeComboBox.SelectedValue = App.Instance.GlobalSave.Get("TabUnloadingTime");
            TabUnloadingTimeComboBox.SelectionChanged += TabUnloadingTimeComboBox_SelectionChanged;

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

            Theme _Theme = App.Instance.GetTheme("Custom");
            CustomThemeIcon.Foreground = new SolidColorBrush(_Theme.PrimaryColor);
            CustomThemeSelection.Background = new SolidColorBrush(_Theme.SecondaryColor);
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
            ChromiumVersion.Text = $"Chromium: {Cef.ChromiumVersion}";
            CEFVersion.Text = $"CEF: {(Cef.CefVersion.StartsWith('r') ? Cef.CefVersion.Substring(1, Cef.CefVersion.IndexOf('-') - 10) : Cef.CefVersion.Substring(0, Cef.CefVersion.IndexOf('-') - 10))}";
            try { WebView2Version.Text = $"WebView2: {CoreWebView2Environment.GetAvailableBrowserVersionString()}"; }
            catch (WebView2RuntimeNotFoundException) { WebView2Version.Text = $"WebView2: Unavailable"; }
            switch (WebViewManager.Settings.TridentVersion)
            {
                case TridentEmulationVersion.IE7:
                    TridentVersion.Text = $"Trident (Internet Explorer): 7";
                    break;
                case TridentEmulationVersion.IE8:
                    TridentVersion.Text = $"Trident (Internet Explorer): 8";
                    break;
                case TridentEmulationVersion.IE9:
                    TridentVersion.Text = $"Trident (Internet Explorer): 9";
                    break;
                case TridentEmulationVersion.IE10:
                    TridentVersion.Text = $"Trident (Internet Explorer): 10";
                    break;
                case TridentEmulationVersion.IE11:
                    TridentVersion.Text = $"Trident (Internet Explorer): 11";
                    break;
            }

            AdsBlocked.Text = App.Instance.AdsBlocked.ToString();
            TrackersBlocked.Text = App.Instance.TrackersBlocked.ToString();

            HomeButtonToggleButton.IsChecked = App.Instance.AllowHomeButton;
            TranslateButtonToggleButton.IsChecked = App.Instance.AllowTranslateButton;
            ReaderButtonToggleButton.IsChecked = App.Instance.AllowReaderModeButton;
            QRButtonToggleButton.IsChecked = App.Instance.AllowQRButton;
            WebEngineButtonToggleButton.IsChecked = App.Instance.AllowWebEngineButton;

            ExtensionButtonComboBox.SelectedIndex = App.Instance.ShowExtensionButton;

            FavouritesBarComboBox.SelectedIndex = App.Instance.ShowFavouritesBar;

            if (StandardFontComboBox.Items.Count == 0)
            {
                _Fonts = Fonts.SystemFontFamilies.Select(i => i.Source).ToList();
                StandardFontComboBox.ItemsSource = _Fonts;
                SerifFontComboBox.ItemsSource = new List<string>(_Fonts);
                SansSerifFontComboBox.ItemsSource = new List<string>(_Fonts);
                FixedFontComboBox.ItemsSource = new List<string>(_Fonts);
                MathFontComboBox.ItemsSource = new List<string>(_Fonts);
            }
            if (WebViewManager.IsCefInitialized)
            {
                //TODO: APPLY FOR WEBVIEW2 & TRIDENT
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
                                        {
                                            StandardFontComboBox.SelectedItem = StandardFont;
                                            StandardFontComboBox.IsEnabled = true;
                                        }
                                    }
                                    if (Fonts.TryGetValue("serif", out var SerifFontMap) && SerifFontMap is IDictionary<string, object> SerifScriptMap)
                                    {
                                        if (SerifScriptMap.TryGetValue("Zyyy", out var SerifFont))
                                        {
                                            SerifFontComboBox.SelectedItem = SerifFont;
                                            SerifFontComboBox.IsEnabled = true;
                                        }
                                    }
                                    if (Fonts.TryGetValue("sansserif", out var SansSerifFontMap) && SansSerifFontMap is IDictionary<string, object> SansSerifScriptMap)
                                    {
                                        if (SansSerifScriptMap.TryGetValue("Zyyy", out var SansSerifFont))
                                        {
                                            SansSerifFontComboBox.SelectedItem = SansSerifFont;
                                            SansSerifFontComboBox.IsEnabled = true;
                                        }
                                    }
                                    if (Fonts.TryGetValue("fixed", out var FixedFontMap) && FixedFontMap is IDictionary<string, object> FixedScriptMap)
                                    {
                                        if (FixedScriptMap.TryGetValue("Zyyy", out var FixedFont))
                                        {
                                            FixedFontComboBox.SelectedItem = FixedFont;
                                            FixedFontComboBox.IsEnabled = true;
                                        }
                                    }
                                    if (Fonts.TryGetValue("math", out var MathFontMap) && MathFontMap is IDictionary<string, object> MathScriptMap)
                                    {
                                        if (MathScriptMap.TryGetValue("Zyyy", out var MathFont))
                                        {
                                            MathFontComboBox.SelectedItem = MathFont;
                                            MathFontComboBox.IsEnabled = true;
                                        }
                                    }
                                }
                                if (WebPrefs.TryGetValue("default_font_size", out var FontSize))
                                {
                                    FontSizeSlider.Value = (int)FontSize;
                                    FontSizeSlider.IsEnabled = true;
                                }
                                if (WebPrefs.TryGetValue("minimum_font_size", out var MinimumFontSize))
                                {
                                    MinimumFontSizeSlider.Value = (int)MinimumFontSize;
                                    MinimumFontSizeSlider.IsEnabled = true;
                                }
                            }
                        }
                    });
                });
            }

            App.Instance.LoadExtensions();
            ExtensionsList.ItemsSource = App.Instance.Extensions;

            UsernameInitial.Text = App.Instance.CurrentProfile.Initial;
            UsernameInitial.Foreground = App.Instance.CurrentProfile.Foreground;
            UsernameBackground.Background = App.Instance.CurrentProfile.Brush;
            UsernameText.Text = App.Instance.CurrentProfile.Name;

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
        private void ImageSearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ImageSearch", ImageSearchComboBox.SelectedIndex);
        }
        private void TranslationProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("TranslationProvider", TranslationProviderComboBox.SelectedIndex);
        }
        private void SpellcheckProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SpellcheckProvider", SpellcheckProviderComboBox.SelectedIndex);
        }
        private void WebEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                if (WebEngineComboBox.SelectedIndex == 1)
                {
                    string? AvailableVersion = null;
                    try
                    {
                        AvailableVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                    }
                    catch (WebView2RuntimeNotFoundException)
                    {
                        WebEngineComboBox.SelectedIndex = 0;
                        InformationDialogWindow InfoWindow = new InformationDialogWindow("Error", "WebView2 Runtime Unavailable", "Microsoft Edge WebView2 Runtime is not installed on your device.", "\ue7f9", "Download", "Cancel");
                        InfoWindow.Topmost = true;
                        if (InfoWindow.ShowDialog() == true)
                            BrowserView.Tab.ParentWindow.NewTab("https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/", true, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1);
                    }
                }
                App.Instance.GlobalSave.Set("WebEngine", WebEngineComboBox.SelectedIndex);
            }
        }
        private void TridentVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("TridentVersion", TridentVersionComboBox.SelectedIndex);
        }
        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.DefaultSearchProvider = SearchEngineComboBox.SelectedValue as SearchProvider;
                App.Instance.GlobalSave.Set("SearchEngine", App.Instance.DefaultSearchProvider.Name);
            }
        }
        private void FaviconServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("FaviconService", FaviconServiceComboBox.SelectedIndex);
        }
        private void AdBlockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAdBlock(AdBlockComboBox.SelectedIndex);
        }
        private void WebRiskServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetWebRiskService(WebRiskServiceComboBox.SelectedIndex);
        }
        private void TabAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                int TabAlignment = TabAlignmentComboBox.SelectedIndex;
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignment, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
            }
        }
        private void ExtensionButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
        }
        private void FavouritesBarComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
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
        private void TrimURLCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetTrimURL(TrimURLCheckBox.IsChecked.ToBool());
        }
        private void PunycodeURLCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetPunycodeURL(PunycodeURLCheckBox.IsChecked.ToBool());
        }
        private void ModernURLCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetModernURL(ModernURLCheckBox.IsChecked.ToBool());
        }
        private void HomographProtectionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetHomographProtection(HomographProtectionCheckBox.IsChecked.ToBool());
        }
        private void SkipAdsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetYouTube(SkipAdsCheckBox.IsChecked.ToBool());
        }
        private void SmartDarkModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetSmartDarkMode(SmartDarkModeCheckBox.IsChecked.ToBool());
        }
        private void TabPreviewCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetTabPreview(TabPreviewCheckBox.IsChecked.ToBool());
        }
        private void TabMemoryCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetTabMemory(TabMemoryCheckBox.IsChecked.ToBool());
        }
        private void AdaptiveThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("AdaptiveTheme", AdaptiveThemeCheckBox.IsChecked.ToBool().ToString());
        }
        private void WarnCodecCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("WarnCodec", WarnCodecCheckBox.IsChecked.ToBool().ToString());
        }
        private void WaybackInfoBarCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("WaybackInfoBar", WaybackInfoBarCheckBox.IsChecked.ToBool().ToString());
        }
        private void HomographInfoBarCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("HomographInfoBar", HomographInfoBarCheckBox.IsChecked.ToBool().ToString());
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
            {
                bool DownloadFavicons = DownloadFaviconsCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("Favicons", DownloadFavicons);
            }
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
        /*private void SuppressErrorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("SuppressError", SuppressErrorCheckBox.IsChecked.ToBool().ToString());
        }*/
        private void SpellCheckCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Enabled = SpellCheckCheckBox.IsChecked.ToBool();

                App.Instance.GlobalSave.Set("SpellCheck", Enabled.ToString());
                if (WebViewManager.IsCefInitialized)
                {
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
        }
        private void SearchSuggestionsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool SearchSuggestions = SearchSuggestionsCheckBox.IsChecked.ToBool();
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
        private void AntiTamperCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                bool Checked = AntiTamperCheckBox.IsChecked.ToBool();
                App.Instance.GlobalSave.Set("AntiTamper", Checked.ToString());
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
        private void WebAppsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("WebApps", WebAppsCheckBox.IsChecked.ToBool().ToString());
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
        private void BrowserHardwareAccelerationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("BrowserHardwareAcceleration", BrowserHardwareAccelerationCheckBox.IsChecked.ToBool().ToString());
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
                if (Value) StartupManager.EnableStartup(App.Instance.CurrentProfile.Name);
                else StartupManager.DisableStartup(App.Instance.CurrentProfile.Name);
                App.Instance.GlobalSave.Set("StartupBoost", Value.ToString());
            }
        }
        private void ReduceDiskCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.GlobalSave.Set("ReduceDisk", ReduceDiskCheckBox.IsChecked.ToBool().ToString());
        }

        private void TranslateButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
        }
        private void HomeButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
        }
        private void ReaderButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
        }
        private void QRButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
        }
        private void WebEngineButtonToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
                App.Instance.SetAppearance(App.Instance.CurrentTheme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
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
                if (Text == "Custom")
                {
                    ColorPickerWindow _ColorPickerWindow = new ColorPickerWindow(Utils.HexToColor(App.Instance.GlobalSave.Get("CustomTheme")));
                    _ColorPickerWindow.Topmost = true;
                    if (_ColorPickerWindow.ShowDialog() == true)
                    {
                        App.Instance.GlobalSave.Set("CustomTheme", Utils.ColorToHex(_ColorPickerWindow.UserInput.Color));
                        _Theme = App.Instance.GenerateTheme(_ColorPickerWindow.UserInput.Color, "Custom");
                        CustomThemeIcon.Foreground = new SolidColorBrush(_Theme.PrimaryColor);
                        CustomThemeSelection.Background = new SolidColorBrush(_Theme.SecondaryColor);
                    }
                }
                App.Instance.SetAppearance(_Theme, TabAlignmentComboBox.SelectedIndex, App.Instance.VerticalTabWidth, HomeButtonToggleButton.IsChecked.ToBool(), TranslateButtonToggleButton.IsChecked.ToBool(), ReaderButtonToggleButton.IsChecked.ToBool(), ExtensionButtonComboBox.SelectedIndex, FavouritesBarComboBox.SelectedIndex, QRButtonToggleButton.IsChecked.ToBool(), WebEngineButtonToggleButton.IsChecked.ToBool());
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
            InformationDialogWindow InfoWindow = new("Warning", "Clear Browsing Data", "This will permanently delete all browsing data. Do you want to continue?", "\uea99", "Yes", "No");
            InfoWindow.Topmost = true;

            if (InfoWindow.ShowDialog() == true)
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
                    WebViewManager.RuntimeSettings.PDFViewer = Target.IsChecked.ToBool();
                    App.Instance.GlobalSave.Set("PDF", WebViewManager.RuntimeSettings.PDFViewer);
                }
            }
        }

        private void PerformanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("Performance", PerformanceComboBox.SelectedIndex);
                ReduceDiskCheckBox.IsEnabled = PerformanceComboBox.SelectedIndex == 0;
                App.Instance.LiteMode = PerformanceComboBox.SelectedIndex == 0;
                App.Instance.HighPerformanceMode = PerformanceComboBox.SelectedIndex == 2;
            }
        }

        private void StandardFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = StandardFontComboBox.SelectedItem.ToString();

                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.fonts.standard.Zyyy", Value, out string _);
                    });
                }
            }
        }

        private void SerifFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = SerifFontComboBox.SelectedItem.ToString();
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.fonts.serif.Zyyy", Value, out string _);
                    });
                }
            }
        }

        private void SansSerifFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = SansSerifFontComboBox.SelectedItem.ToString();
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.fonts.sansserif.Zyyy", Value, out string _);
                    });
                }
            }
        }

        private void FixedFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = FixedFontComboBox.SelectedItem.ToString();
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.fonts.fixed.Zyyy", Value, out string _);
                    });
                }
            }
        }

        private void MathFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsInitialized)
            {
                string Value = MathFontComboBox.SelectedItem.ToString();
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.fonts.math.Zyyy", Value, out string _);
                    });
                }
            }
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SettingsInitialized)
            {
                int Value = (int)FontSizeSlider.Value;
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.default_font_size", Value, out string _);
                    });
                }
            }
        }

        private void MinimumFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SettingsInitialized)
            {
                int Value = (int)MinimumFontSizeSlider.Value;
                if (WebViewManager.IsCefInitialized)
                {
                    Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        var GlobalRequestContext = Cef.GetGlobalRequestContext();
                        GlobalRequestContext.SetPreference("webkit.webprefs.minimum_font_size", Value, out string _);
                    });
                }
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

                float Bandwidth = Checked ? float.Parse(App.Instance.GlobalSave.Get("Bandwidth")) : 0;
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.WebView != null && i.WebView.IsBrowserInitialized))
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
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.WebView != null && i.WebView.IsBrowserInitialized))
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
                    foreach (Browser BrowserView in _Window.Tabs.Select(i => i.Content).Where(i => i != null && i.WebView != null && i.WebView.IsBrowserInitialized))
                        BrowserView.LimitNetwork(0, Bandwidth, Bandwidth);
                }
            }
        }

        private void AddSearchEngineButton_Click(object sender, RoutedEventArgs e)
        {
            DynamicDialogWindow _DynamicDialogWindow = new DynamicDialogWindow("Settings", "Add search engine", new List<InputField> { new InputField { Name = "Name", Type = DialogInputType.Text, IsRequired = true }, new InputField { Name = "Search URL with {0} as query", Type = DialogInputType.Text, IsRequired = true }, new InputField { Name = "Suggestion URL with {0} as query", Type = DialogInputType.Text, IsRequired = false } }, "\xf6fa");
            _DynamicDialogWindow.Topmost = true;
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                SearchProvider _SearchProvider = new SearchProvider
                {
                    Name = _DynamicDialogWindow.InputFields[0].Value.Trim(),
                    Host = Utils.FastHost(_DynamicDialogWindow.InputFields[1].Value),
                    SearchUrl = _DynamicDialogWindow.InputFields[1].Value + (_DynamicDialogWindow.InputFields[1].Value.Contains("{0}") ? string.Empty : "{0}"),
                    SuggestUrl = _DynamicDialogWindow.InputFields[2].Value + (string.IsNullOrEmpty(_DynamicDialogWindow.InputFields[2].Value) ? string.Empty : (_DynamicDialogWindow.InputFields[2].Value.Contains("{0}") ? string.Empty : "{0}"))
                };
                App.Instance.SearchEngines.Add(_SearchProvider);
                SearchEngineComboBox.SelectedValue = _SearchProvider;
            }
        }

        private void RemoveSearchEngineButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.SearchEngines.Remove(SearchEngineComboBox.SelectedValue as SearchProvider);
            SearchEngineComboBox.SelectedItem = App.Instance.SearchEngines.FirstOrDefault();
        }

        private void EditSearchEngineButton_Click(object sender, RoutedEventArgs e)
        {
            SearchProvider _SearchProvider = SearchEngineComboBox.SelectedValue as SearchProvider;
            DynamicDialogWindow _DynamicDialogWindow = new DynamicDialogWindow("Settings", "Edit search engine", new List<InputField> { new InputField { Name = "Name", Type = DialogInputType.Text, IsRequired = true, Value = _SearchProvider.Name }, new InputField { Name = "Search URL with {0} as query", Type = DialogInputType.Text, IsRequired = true, Value = _SearchProvider.SearchUrl }, new InputField { Name = "Suggestion URL with {0} as query", Type = DialogInputType.Text, IsRequired = false, Value = _SearchProvider.SuggestUrl } }, "\xe70f");
            _DynamicDialogWindow.Topmost = true;
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                _SearchProvider.Name = _DynamicDialogWindow.InputFields[0].Value.Trim();
                _SearchProvider.Host = Utils.FastHost(_DynamicDialogWindow.InputFields[1].Value);
                _SearchProvider.SearchUrl = _DynamicDialogWindow.InputFields[1].Value + (_DynamicDialogWindow.InputFields[1].Value.Contains("{0}") ? string.Empty : "{0}");
                _SearchProvider.SuggestUrl = _DynamicDialogWindow.InputFields[2].Value + (string.IsNullOrEmpty(_DynamicDialogWindow.InputFields[2].Value) ? string.Empty : (_DynamicDialogWindow.InputFields[2].Value.Contains("{0}") ? string.Empty : "{0}"));
            }
        }
        private void SyncCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsInitialized)
            {
                App.Instance.GlobalSave.Set("Sync", SyncCheckBox.IsChecked.ToBool().ToString());
                SyncWarning.Visibility = Visibility.Collapsed;
                if (SyncCheckBox.IsChecked.ToBool())
                {
                    if (string.IsNullOrEmpty(App.Instance.GlobalSave.Get("SyncGitHub")))
                    {
                        SyncWarningText.Text = "No access token found.";
                        SyncWarning.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void SyncToggleButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> SyncedData = [];
            if (SyncFavouritesToggleButton.IsChecked.ToBool()) SyncedData.Add("Favourites");
            //if (SyncTabsToggleButton.IsChecked.ToBool()) SyncedData.Add("Tabs");
            if (SyncSettingsToggleButton.IsChecked.ToBool()) SyncedData.Add("Settings");
            App.Instance.GlobalSave.Set("SyncData", string.Join(',', SyncedData));
        }

        private async void ChangeSyncGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DynamicDialogWindow _DynamicDialogWindow = new DynamicDialogWindow("Settings", "Change GitHub Access Token", new List<InputField> { new InputField { Name = "Token", Type = DialogInputType.Text, IsRequired = false, Value = App.Instance.GlobalSave.Get("SyncGitHub") } }, "\xee7e");
                _DynamicDialogWindow.Topmost = true;
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    string Token = _DynamicDialogWindow.InputFields[0].Value.Trim();
                    if (Token == App.Instance.GlobalSave.Get("SyncGitHub"))
                        return;
                    App.Instance.PreventSync = false;
                    App.Instance.GlobalSave.Set("SyncGitHub", Token);
                    App.Instance.GlobalSave.Set("SyncGist", "");
                    SyncWarning.Visibility = Visibility.Collapsed;
                    if (string.IsNullOrEmpty(Token))
                    {
                        App.Instance.GlobalSave.Set("Sync", false);
                        SyncCheckBox.IsChecked = false;
                    }
                    else
                    {
                        HttpClient Client = new();
                        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                        Client.DefaultRequestHeaders.UserAgent.ParseAdd($"SLBr/{App.Instance.ReleaseVersion}");
                        var GistResponse = await Client.GetAsync("https://api.github.com/gists");
                        if (GistResponse.IsSuccessStatusCode)
                        {
                            string JSON = await GistResponse.Content.ReadAsStringAsync();
                            using var _JsonDocument = JsonDocument.Parse(JSON);
                            foreach (var Gist in _JsonDocument.RootElement.EnumerateArray())
                            {
                                if (Gist.GetProperty("description").GetString() == "SLBr Sync")
                                {
                                    App.Instance.GlobalSave.Set("SyncGist", Gist.GetProperty("id").GetString());

                                    InformationDialogWindow InfoWindow = new InformationDialogWindow("Settings", "Choose Sync Direction", "An existing sync was found for this GitHub account.\n\nDo you want to override existing data with the cloud data on application reboot?", "\ue753", "Yes", "No");
                                    InfoWindow.Topmost = true;
                                    if (InfoWindow.ShowDialog() == true)
                                        App.Instance.PreventSync = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
