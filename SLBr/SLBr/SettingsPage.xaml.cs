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
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            //}
            //if (MainWindow.Instance.MainSave.Has("Homepage"))
            HomepageTextBox.Text = MainWindow.Instance.MainSave.Get("Homepage");
            DownloadPathTextBox.Text = MainWindow.Instance.MainSave.Get("DownloadPath");
            //if (MainWindow.Instance.MainSave.Has("DarkTheme"))
            DarkThemeCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme"));

            BlockedKeywordsTextBox.Text = MainWindow.Instance.MainSave.Get("BlockedKeywords");
            BlockRedirectTextBox.Text = MainWindow.Instance.MainSave.Get("BlockRedirect");
            BlockKeywordsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("BlockKeywords"));

            RestoreTabsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("RestoreTabs"));
            AskForDownloadPathCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("AFDP"));
            ATSADSECheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("ATSADSE"));
            DarkTheme(bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme")));
            SearchEngineComboBox.SelectionChanged += SearchEngineComboBox_SelectionChanged;
            HideTabsCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("HideTabs"));
            WeblightCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("Weblight"));
            TabUnloadingCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("TabUnloading"));
            FullAddressCheckBox.IsChecked = bool.Parse(MainWindow.Instance.MainSave.Get("FullAddress"));
        }

        public void DarkTheme(bool Toggle)
        {
            if (Toggle)
            {
                Resources["PrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202225"));
                Resources["FontBrush"] = new SolidColorBrush(Colors.White);
                Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#36393F"));
                Resources["UnselectedTabBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3136"));
                Resources["ControlFontBrush"] = new SolidColorBrush(Colors.Gainsboro);
            }
            else
            {
                Resources["PrimaryBrush"] = new SolidColorBrush(Colors.White);
                Resources["FontBrush"] = new SolidColorBrush(Colors.Black);
                Resources["BorderBrush"] = new SolidColorBrush(Colors.Gainsboro);
                Resources["UnselectedTabBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
                Resources["ControlFontBrush"] = new SolidColorBrush(Colors.Gray);
            }
        }

        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox _ComboBox = (ComboBox)sender;
            MainWindow.Instance.MainSave.Set("Search_Engine", _ComboBox.SelectedValue.ToString());
            NewMessage("The default search provider has been successfully changed and saved.", false);
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

        private void DarkThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("DarkTheme", _CheckBox.IsChecked.ToString());
            DarkTheme(bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme")));
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.DarkTheme(bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme")));
                if (NewsPage.Instance != null)
                {
                    NewsPage.Instance.DarkTheme(bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme")));
                }
            }));
            //NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not")} enable dark theme.", false);
            NewMessage($"SLBr Dark Theme has been {((bool)_CheckBox.IsChecked ? "enabled" : "disabled")}.", false);
        }
        private void BlockKeywordsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("BlockKeywords", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}block the entered keywords.", false);
            //NewMessage($"Restore tabs has been {((bool)_CheckBox.IsChecked ? "enabled" : "disabled")}.", false);
        }
        private void RestoreTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("RestoreTabs", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}restore tabs.", false);
        }
        private void ATSADSECheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("ATSADSE", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}ask if to set a search provider as the default search provider.", false);
        }
        private void HideTabsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.HideTabs(_CheckBox.IsChecked == true ? true : false);
            }));
        }
        private void AFDPCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("AFDP", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}ask for download path on each download.", false);
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
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "" : "not ")}load websites with Weblight.", false);
        }
        private void TabUnloadingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("TabUnloading", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "unload tabs free up resources and memory" : "not unload tabs")}.", false);
        }
        private void FullAddressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var _CheckBox = sender as CheckBox;
            MainWindow.Instance.MainSave.Set("FullAddress", _CheckBox.IsChecked.ToString());
            NewMessage($"SLBr will {((bool)_CheckBox.IsChecked ? "always " : "not ")}show full URLs.", false);
        }
        public void NewMessage(string Content, bool IncludeButton = true, string ButtonContent = "", string ButtonArguments = "", string ToolTip = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Prompt(Content, IncludeButton, ButtonContent, ButtonArguments, ToolTip);
            }));
        }
    }
}
