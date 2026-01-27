using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for News.xaml
    /// </summary>
    public partial class NewsPage : UserControl
    {
        Browser BrowserView;
        XmlDataProvider NewsXML;

        public NewsPage(Browser _BrowserView)
        {
            InitializeComponent();
            BrowserView = _BrowserView;
            ApplyTheme(App.Instance.CurrentTheme);
            NewsXML = Resources["NewsRSSFeed"] as XmlDataProvider;
            BackButton.Visibility = Visibility.Collapsed;
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

        private void Navigate(object sender, RoutedEventArgs e)
        {
            Button _Button = sender as Button;
            BrowserView.Navigate((_Button.Tag as XmlNode).InnerText);
        }

        private void Copy(object sender, RoutedEventArgs e)
        {
            Button _Button = sender as Button;
            Clipboard.SetText((_Button.Tag as XmlNode).InnerText);
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchTextBox.Text = SearchTextBox.Text.Trim();
                string Url = string.Empty;
                if (SearchTextBox.Text.Length > 0)
                {
                    Url = $"http://news.google.com/rss/search?q={Uri.EscapeDataString(SearchTextBox.Text)}";//&hl=en-US
                    BackButton.Visibility = Visibility.Visible;
                }
                else
                {
                    Url = $"http://news.google.com/rss";
                    BackButton.Visibility = Visibility.Collapsed;
                }
                NewsXML.Source = new Uri(Url);
                Keyboard.ClearFocus();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            NewsXML.Refresh();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NewsXML.Source = new Uri($"http://news.google.com/rss");
            SearchTextBox.Text = string.Empty;
            Keyboard.ClearFocus();
            BackButton.Visibility = Visibility.Collapsed;
        }

        //TODO: Topic buttons turn blue when selected, similar to Reader Mode & Translate buttons
        private void TopicButton_Click(object sender, RoutedEventArgs e)
        {
            NewsXML.Source = new Uri($"http://news.google.com/rss/search?q={((FrameworkElement)sender).ToolTip.ToString()}");
            BackButton.Visibility = Visibility.Visible;
        }
    }
}
