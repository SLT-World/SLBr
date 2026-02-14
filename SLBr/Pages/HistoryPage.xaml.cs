/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class HistoryPage : UserControl, IPageOverlay
    {
        public HistoryPage(Browser _BrowserView)
        {
            InitializeComponent();
            BrowserView = _BrowserView;
        }

        public void Dispose()
        {
        }

        Browser BrowserView;

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
            BrowserView.Tab.ParentWindow.NewTab(Values[1], true, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1, BrowserView.Private, BrowserView.Tab.TabGroup);
        }

        private void HistoryButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
                BrowserView.Tab.ParentWindow.NewTab(Values[1], false, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1, BrowserView.Private, BrowserView.Tab.TabGroup);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            HistoryList.ItemsSource = App.Instance.History;
            ApplyTheme(App.Instance.CurrentTheme);
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

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            List<ActionStorage> Selected = HistoryList.SelectedItems.Cast<ActionStorage>().ToList();
            foreach (ActionStorage HistoryEntry in Selected)
                App.Instance.History.Remove(HistoryEntry);
        }

        private void DeleteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is ActionStorage HistoryEntry)
                App.Instance.History.Remove(HistoryEntry);
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSelectedButton.Visibility = HistoryList.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HistoryList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && HistoryList.SelectedItems.Count > 0)
            {
                DeleteSelectedButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string SearchText = SearchBox.Text.ToLowerInvariant();
            if (SearchText.Length == 0)
                HistoryList.ItemsSource = App.Instance.History;
            else
                HistoryList.ItemsSource = App.Instance.History.Where(i => (i.Name?.ToLowerInvariant().Contains(SearchText) ?? false) || (i.Tooltip?.ToLowerInvariant().Contains(SearchText) ?? false));
        }
    }
}
