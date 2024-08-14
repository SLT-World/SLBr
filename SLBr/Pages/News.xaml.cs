using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
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
using System.Xml;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for News.xaml
    /// </summary>
    public partial class News : UserControl
    {
        Browser BrowserView;
        XmlDataProvider NewsXML;

        public News(Browser _BrowserView)
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
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }

        private void Navigate(object sender, RoutedEventArgs e)
        {
            Button _Button = sender as Button;
            XmlNode _XmlNode = _Button.Tag as XmlNode;
            BrowserView.Navigate(_XmlNode.InnerText);
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchTextBox.Text = SearchTextBox.Text.Trim();
                string Url = "";
                if (SearchTextBox.Text.Length > 0)
                {
                    Url = $"http://news.google.com/rss/search?q={Uri.EscapeDataString(SearchTextBox.Text)}&hl=en-US";
                    BackButton.Visibility = Visibility.Visible;
                }
                else
                {
                    Url = $"http://news.google.com/rss?hl=en-US";
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
            NewsXML.Source = new Uri($"http://news.google.com/rss?hl=en-US");
            SearchTextBox.Text = "";
            Keyboard.ClearFocus();
            BackButton.Visibility = Visibility.Collapsed;
        }

        private void TopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;
            Actions _Action;
            var Target = (FrameworkElement)sender;
            string _Tooltip = Target.ToolTip.ToString();
            NewsXML.Source = new Uri($"http://news.google.com/rss/search?q={_Tooltip}&hl=en-US");
            BackButton.Visibility = Visibility.Visible;
        }
    }
}
