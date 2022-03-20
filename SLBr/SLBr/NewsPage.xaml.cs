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
            DarkTheme(bool.Parse(MainWindow.Instance.MainSave.Get("DarkTheme")));
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
        }

        private void Navigate(object sender, RoutedEventArgs e)
        {
            Button _Button = sender as Button;
            XmlNode _XmlNode = _Button.Tag as XmlNode;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Navigate(_XmlNode.InnerText);
            }));
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
    }
}
