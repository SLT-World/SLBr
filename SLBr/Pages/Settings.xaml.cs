using CefSharp;
using SLBr.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public static Settings Instance;

        public Settings()
        {
            InitializeComponent();
            Instance = this;
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            MainWindow.Instance.NewBrowserTab(e.Uri.ToString(), 0, true, MainWindow.Instance.BrowserTabs.SelectedIndex + 1);
            e.Handled = true;
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

            RestoreTabsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("RestoreTabs"));
            DownloadPromptCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("DownloadPrompt"));

            TabUnloadingCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("TabUnloading"));
            FullAddressCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("FullAddress"));
            AdBlockCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("AdBlock"));
            TrackerBlockCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("TrackerBlock"));

            IPFSCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("IPFS"));

            WaybackCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("Wayback"));
            GeminiCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("Gemini"));
            GopherCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("Gopher"));
            ModernWikipediaCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("ModernWikipedia"));
            if (!RenderModeComboBox.Items.Contains("Hardware"))
            {
                RenderModeComboBox.Items.Add("Hardware");
                RenderModeComboBox.Items.Add("Software");
            }
            RenderModeComboBox.SelectionChanged += RenderModeComboBox_SelectionChanged;
            RenderModeComboBox.SelectedValue = MainWindow.Instance.MainSave.Get("RenderMode");

            FramerateTextBox.Text = MainWindow.Instance.Framerate.ToString();
            JavacriptCheckBox.IsChecked = MainWindow.Instance.Javascript.ToBoolean();
            LoadImagesCheckBox.IsChecked = MainWindow.Instance.LoadImages.ToBoolean();
            LocalStorageCheckBox.IsChecked = MainWindow.Instance.LocalStorage.ToBoolean();
            DatabasesCheckBox.IsChecked = MainWindow.Instance.Databases.ToBoolean();
            WebGLCheckBox.IsChecked = MainWindow.Instance.WebGL.ToBoolean();
            AboutVersion.Text = $"Version {MainWindow.Instance.ReleaseVersion} (Chromium {Cef.ChromiumVersion})";
            string ThemeName = MainWindow.Instance.MainSave.Get("Theme");
            foreach (object o in ThemeSelection.Items)
            {
                if ((string)((Border)o).Tag == ThemeName)
                {
                    ThemeSelection.SelectedItem = o;
                    //ThemeSelection_SelectionChanged(null, null);
                    break;
                }
            }
            ApplyTheme(MainWindow.Instance.GetTheme());
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
                HomepageTextBox.Text = Utils.FixUrl(HomepageTextBox.Text);
                /*if (HomepageTextBox.Text.Contains("."))
                {
                    MainWindow.Instance.MainSave.Set("Homepage", HomepageTextBox.Text);
                }
                else
                {
                    MainWindow.Instance.MainSave.Set("Homepage", new Uri(MainWindow.Instance.MainSave.Get("Search_Engine")).Host);
                }*/
                MainWindow.Instance.MainSave.Set("Homepage", HomepageTextBox.Text);
            }
        }
        private void ASEPrefixTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ASEPrefixTextBox.Text.Trim().Length > 0)
            {
                if (ASEPrefixTextBox.Text.Contains("."))
                {
                    string Url = Utils.FixUrl(ASEPrefixTextBox.Text.Trim().Replace(" ", ""));
                    if (!Url.Contains("{0}"))
                        Url += "{0}";
                    MainWindow.Instance.SearchEngines.Add(Url);
                    if (!SearchEngineComboBox.Items.Contains(Url))
                        SearchEngineComboBox.Items.Add(Url);
                    SearchEngineComboBox.SelectedValue = Url;
                    MainWindow.Instance.MainSave.Set("Search_Engine", Url);
                    ASEPrefixTextBox.Text = string.Empty;
                }
            }
        }
        private void DownloadPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;
            MainWindow.Instance.MainSave.Set("DownloadPath", _TextBox.Text);
        }
        private void ScreenshotPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;
            MainWindow.Instance.MainSave.Set("ScreenshotPath", _TextBox.Text);
        }
        private void AdBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.AdBlock((bool)_CheckBox.IsChecked);
            //NewMessage($"SLBr Ad Block has been {((bool)_CheckBox.IsChecked ? "enabled" : "disabled")}, refresh the webpages to see the change.", false);
        }
        private void TrackerBlockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.TrackerBlock((bool)_CheckBox.IsChecked);
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} block trackers.", false);
        }
        private void RestoreTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("RestoreTabs", _CheckBox.IsChecked.ToString());
        }
        private void DownloadPromptCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("DownloadPrompt", _CheckBox.IsChecked.ToString());
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always" : "not")} prompt before downloading anything.", false);
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
        }
        private void WaybackCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("Wayback", _CheckBox.IsChecked.ToString());
        }
        private void IPFSCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("IPFS", _CheckBox.IsChecked.ToString());
        }
        private void GeminiCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("Gemini", _CheckBox.IsChecked.ToString());
        }
        private void GopherCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("Gopher", _CheckBox.IsChecked.ToString());
        }
        private void ModernWikipediaCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("ModernWikipedia", _CheckBox.IsChecked.ToString());
        }
        private void ThemeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Border SelectedItem = (Border)ThemeSelection.SelectedItem;
            string Text = (string)SelectedItem.Tag;
            Theme _Theme = MainWindow.Instance.GetTheme(Text);
            if (_Theme == null)
                return;
            MainWindow.Instance.MainSave.Set("Theme", Text);
            MainWindow.Instance.ApplyTheme(_Theme);
            ApplyTheme(_Theme);
        }
        private void ApplySandboxButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                int Framerate = int.Parse(FramerateTextBox.Text);
                if (Framerate > 90)
                    Framerate = 90;
                else if (Framerate < 1)
                    Framerate = 10;
                FramerateTextBox.Text = Framerate.ToString();
                MainWindow.Instance.SetSandbox(Framerate, ((bool)JavacriptCheckBox.IsChecked).ToCefState(), ((bool)LoadImagesCheckBox.IsChecked).ToCefState(), ((bool)LocalStorageCheckBox.IsChecked).ToCefState(), ((bool)DatabasesCheckBox.IsChecked).ToCefState(), ((bool)WebGLCheckBox.IsChecked).ToCefState());
            }));
        }

        private void FramerateTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
