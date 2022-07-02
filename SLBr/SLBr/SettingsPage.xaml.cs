using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SLBr
{
    public partial class SettingsPage : Page
    {
        public static SettingsPage Instance;

        public SettingsPage()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*if (bool.Parse(MainWindow.Instance.MainSave.Get("ShowTabs")))
            {
                MainWindow.Instance.AddressBox.Text = "SLBr Settings Tab";
                MainWindow.Instance.ReloadButton.Content = "\xE72C";
                MainWindow.Instance.WebsiteLoadingProgressBar.IsEnabled = false;
                MainWindow.Instance.WebsiteLoadingProgressBar.IsIndeterminate = false;
            }*/
            Random rand = new Random();
            if (rand.Next(0, 100) == 99)
                GeneralSettingsText.Text = "Hello there. General Settings.";
            foreach (string Url in MainWindow.Instance.SearchEngines)
            {
                if (!SearchEngineComboBox.Items.Contains(Url))
                    SearchEngineComboBox.Items.Add(Url);
            }
            //if (MainWindow.Instance.MainSave.Has("Search_Engine"))
            //{
            string Search_Engine = MainWindow.Instance.MainSave.Get("Search_Engine");
            if (SearchEngineComboBox.Items.Contains(Search_Engine))
                SearchEngineComboBox.SelectedValue = Search_Engine;
            SearchEngineComboBox.SelectionChanged += SearchEngineComboBox_SelectionChanged;
            //}
            //if (MainWindow.Instance.MainSave.Has("Homepage"))
            HomepageTextBox.Text = MainWindow.Instance.MainSave.Get("Homepage");
            DownloadPathTextBox.Text = MainWindow.Instance.MainSave.Get("DownloadPath");
            ScreenshotPathTextBox.Text = MainWindow.Instance.MainSave.Get("ScreenshotPath");

            BlockedKeywordsTextBox.Text = MainWindow.Instance.MainSave.Get("BlockedKeywords");
            BlockRedirectTextBox.Text = MainWindow.Instance.MainSave.Get("BlockRedirect");
            BlockKeywordsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("BlockKeywords"));
            ShowSuggestionsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("AutoSuggestions"));

            RestoreTabsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("RestoreTabs"));
            LiteModeCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("LiteMode"));
            DownloadPromptCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("DownloadPrompt"));
            FindSearchProviderCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("FindSearchProvider"));
            ShowTabsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("ShowTabs"));
            WeblightCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("Weblight"));
            TabUnloadingCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("TabUnloading"));
            FullAddressCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("FullAddress"));
            AdBlockCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("AdBlock"));
            TrackerBlockCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("TrackerBlock"));
            ShowFavouritesBarCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("FavouritesBar"));
            DarkWebpageCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("DarkWebpage"));
            ShowPerformanceMetricsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("ShowPerformanceMetrics"));
            DoNotTrackCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("DoNotTrack"));
            string ThemeName = MainWindow.Instance.MainSave.Get("Theme");
            foreach (object o in ThemeSelection.Items)
            {
                if ((string)((Border)o).Tag == ThemeName)
                {
                    ThemeSelection.SelectedItem = o;
                    ThemeSelection_SelectionChanged(null, null);
                    break;
                }
            }
            if (!RenderModeComboBox.Items.Contains("Hardware"))
            {
                RenderModeComboBox.Items.Add("Hardware");
                RenderModeComboBox.Items.Add("Software");
            }
            RenderModeComboBox.SelectionChanged += RenderModeComboBox_SelectionChanged;
            RenderModeComboBox.SelectedValue = MainWindow.Instance.MainSave.Get("RenderMode");
        }

        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);
        }

        private void RenderModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox _ComboBox = (ComboBox)sender;
            MainWindow.Instance.SetRenderMode(_ComboBox.SelectedValue.ToString(), true);
            //NewMessage("Render mode has been sucessfully changed and saved.", false);
        }
        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox _ComboBox = (ComboBox)sender;
            MainWindow.Instance.MainSave.Set("Search_Engine", _ComboBox.SelectedValue.ToString());
            //NewMessage("The default search provider has been successfully changed and saved.", false);
        }
        private void HomepageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && HomepageTextBox.Text.Trim().Length > 0)
            {
                HomepageTextBox.Text = Utils.FixUrl(HomepageTextBox.Text, false);
                /*if (HomepageTextBox.Text.Contains("."))
                {
                    MainWindow.Instance.MainSave.Set("Homepage", HomepageTextBox.Text);
                }
                else
                {
                    MainWindow.Instance.MainSave.Set("Homepage", new Uri(MainWindow.Instance.MainSave.Get("Search_Engine")).Host);
                }*/
                MainWindow.Instance.MainSave.Set("Homepage", HomepageTextBox.Text);
                NewMessage("The homepage has been successfully changed and saved.", false);
            }
        }
        private void ASEPrefixTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ASEPrefixTextBox.Text.Trim().Length > 0)
            {
                if (ASEPrefixTextBox.Text.Contains("."))
                {
                    string Url = Utils.FixUrl(ASEPrefixTextBox.Text.Trim().Replace(" ", ""), false);
                    if (!Url.Contains("{0}"))
                        Url += "{0}";
                    MainWindow.Instance.SearchEngines.Add(Url);
                    if (!SearchEngineComboBox.Items.Contains(Url))
                        SearchEngineComboBox.Items.Add(Url);
                    SearchEngineComboBox.SelectedValue = Url;
                    MainWindow.Instance.MainSave.Set("Search_Engine", Url);
                    ASEPrefixTextBox.Text = string.Empty;
                    NewMessage("The entered search provider url has been successfully added to the list.", false);
                }
            }
        }
        private void DownloadPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;
            MainWindow.Instance.MainSave.Set("DownloadPath", _TextBox.Text);
            NewMessage("Download path has been successfully changed and saved.", false);
        }
        private void ScreenshotPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;
            MainWindow.Instance.MainSave.Set("ScreenshotPath", _TextBox.Text);
            NewMessage("Screenshot path has been successfully changed and saved.", false);
        }
        private void BlockedKeywordsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;

            if (e.Key == Key.Enter/* && _TextBox.Text.Trim().Length > 0*/)
            {
                _TextBox.Text = _TextBox.Text.Trim().ToLower();
                //if (_TextBox.Text.Contains("slt,") || _TextBox.Text.Contains("sltworld,") || _TextBox.Text.Contains(",slt") || _TextBox.Text.Contains(",sltworld"))
                //{
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, ",sltworld", false, true);
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, "sltworld,", false, false);
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, ",slt", false, true);
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, "slt,", false, false);
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, ",slbr", false, true);
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, "slbr,", false, false);
                _TextBox.Text = _TextBox.Text.Replace(",sltworld,", ",");
                _TextBox.Text = _TextBox.Text.Replace(",slt,", ",");
                _TextBox.Text = _TextBox.Text.Replace(",slbr,", ",");
                //}
                //_TextBox.Text = Utils.RemovePrefix(_TextBox.Text, ",");
                _TextBox.Text = Utils.RemovePrefix(_TextBox.Text, ",", false, true);
                MainWindow.Instance.MainSave.Set("BlockedKeywords", _TextBox.Text);
                NewMessage("Blocked Keywords has been successfully changed and saved.", false);
            }
        }
        private void BlockRedirectTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                if (_TextBox.Text.Trim().Length == 0)
                    _TextBox.Text = MainWindow.Instance.MainSave.Get("Homepage");
                else
                    _TextBox.Text = Utils.FixUrl(_TextBox.Text, false);
            }
            MainWindow.Instance.MainSave.Set("BlockRedirect", _TextBox.Text);
            NewMessage($"SLBr will redirect to the entered url when a blocked keyword is detected.", false);
        }
        private void AdBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.AdBlock((bool)_CheckBox.IsChecked);
            //NewMessage($"SLBr Ad Block has been {((bool)_CheckBox.IsChecked ? "enabled" : "disabled")}, refresh the webpages to see the change.", false);
        }
        private void ShowSuggestionsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.SetAutoSuggestions((bool)_CheckBox.IsChecked);
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}show suggestions.", false);
        }
        private void TrackerBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.TrackerBlock((bool)_CheckBox.IsChecked);
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} block trackers.", false);
        }
        private void ShowPerformanceMetricsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("ShowPerformanceMetrics", _CheckBox.IsChecked.ToString());
        }
        private void BlockKeywordsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("BlockKeywords", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} block the entered keywords.", false);
        }
        private void DoNotTrackCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("DoNotTrack", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} apply Do Not Track header to requests.", false);
        }
        private void RestoreTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("RestoreTabs", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}restore tabs on the next session.", false);
        }
        private void LiteModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("LiteMode", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr Lite mode is {((bool)_CheckBox.IsChecked ? "enabled" : "disabled")}.", false);
        }
        private void FindSearchProviderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("FindSearchProvider", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")} notify when an available search provider is found.", false);
        }
        private void ShowTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.ShowTabs(_CheckBox.IsChecked == true ? true : false);
            }));
        }
        private void DownloadPromptCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("DownloadPrompt", _CheckBox.IsChecked.ToString());
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} prompt before downloading anything.", false);
        }
        private void RelaunchButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Relaunch();
            }));
        }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Reset();
            }));
        }
        private void CloseNoSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Relaunch(false);
            }));
        }
        private void WeblightCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("Weblight", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} load websites with Weblight.", false);
        }
        private void TabUnloadingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("TabUnloading", _CheckBox.IsChecked.ToString());
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "unload tabs free up resources and memory" : "not unload tabs")}.", false);
        }
        private void FullAddressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("FullAddress", _CheckBox.IsChecked.ToString());
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} show full URLs.", false);
        }
        private void ShowFavouritesBarCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("FavouritesBar", _CheckBox.IsChecked.ToString());
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} show favourites bar.", false);
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!(bool)_CheckBox.IsChecked)
                    MainWindow.Instance.FavouriteContainer.Visibility = Visibility.Collapsed;
                else if (MainWindow.Instance.Favourites.Count > 0)
                    MainWindow.Instance.FavouriteContainer.Visibility = Visibility.Visible;
            }));
        }
        private void DarkWebpageCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("DarkWebpage", _CheckBox.IsChecked.ToString());
            //NewMessage($"Refresh to see change", false);
        }
        public void NewMessage(string Content, bool IncludeButton = true, string ButtonContent = "", string ButtonArguments = "", string ToolTip = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Prompt(false, Content, IncludeButton, ButtonContent, ButtonArguments, ToolTip);
            }));
        }
        private void ThemeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Border SelectedItem = (Border)ThemeSelection.SelectedItem;
            string Text = (string)SelectedItem.Tag;
            /*Border Container = (Border)SelectedItem.Child;
            TextBlock Name = (TextBlock)Container.Child;*/
            Theme _Theme = MainWindow.Instance.GetTheme(Text);
            if (_Theme == null)
                return;
            if (Text == "Dark")
                DarkWebpageCheckBox.Visibility = Visibility.Visible;
            else
                DarkWebpageCheckBox.Visibility = Visibility.Collapsed;
            MainWindow.Instance.MainSave.Set("Theme", Text);
            MainWindow.Instance.ApplyTheme(_Theme);
            ApplyTheme(_Theme);
        }
    }
}
