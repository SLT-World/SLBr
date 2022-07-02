// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace SLBr
{
    /// <summary>
    /// Interaction logic for NewsPage.xaml
    /// </summary>
    public partial class NewsPage : Window
    {
        public static NewsPage Instance;

        public NewsPage()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(MainWindow.Instance.GetCurrentTheme());
        }

        //SUPPORT FOR MULTI NEWS LIKE YANDEX, GOOGLE ETC

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var _TextBox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                _TextBox.Text = _TextBox.Text.Trim();
                string Url = $"https://news.google.com/rss";
                if (_TextBox.Text.Length > 0)
                    Url = $"https://news.google.com/rss/search?q={_TextBox.Text.Replace(" ", "+")}";
                XmlDataProvider XML = Resources["NewsRSSFeed"] as XmlDataProvider;
                XML.Source = new Uri(Url);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            XmlDataProvider XML = Resources["NewsRSSFeed"] as XmlDataProvider;
            XML.Refresh();
            ApplyTheme(MainWindow.Instance.GetCurrentTheme());
        }

        private void Navigate(object sender, RoutedEventArgs e)
        {
            Button _Button = sender as Button;
            XmlNode _XmlNode = _Button.Tag as XmlNode;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Navigate(false, _XmlNode.InnerText);
            }));
        }
        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);
        }
    }
}
