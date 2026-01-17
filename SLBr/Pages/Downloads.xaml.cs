using CefSharp;
using CefSharp.DevTools.Autofill;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Downloads : UserControl, IPageOverlay
    {
        public Downloads(Browser _BrowserView)
        {
            InitializeComponent();
            BrowserView = _BrowserView;
        }

        public void Dispose()
        {
        }

        Browser BrowserView;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DownloadsList.ItemsSource = App.Instance.VisibleDownloads;
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

        private void DownloadsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{App.Instance.GlobalSave.Get("DownloadPath")}\"") { UseShellExecute = true });
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadEntry> Selected = DownloadsList.SelectedItems.Cast<DownloadEntry>().Where(i => i.Stop == Visibility.Collapsed).ToList();
            foreach (DownloadEntry DownloadsEntry in Selected)
                App.Instance.VisibleDownloads.Remove(DownloadsEntry);
        }

        private void CancelSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is DownloadEntry DownloadsEntry)
            {
                try
                {
                    App.Instance.Downloads.GetValueOrDefault(DownloadsEntry.ID)?.Cancel();
                }
                catch { }
            }
        }

        private void OpenSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is DownloadEntry DownloadsEntry)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{App.Instance.Downloads.GetValueOrDefault(DownloadsEntry.ID).FullPath}\"") { UseShellExecute = true });
            }
        }

        private void DeleteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is DownloadEntry DownloadsEntry && DownloadsEntry.Stop == Visibility.Collapsed)
                App.Instance.VisibleDownloads.Remove(DownloadsEntry);
        }

        private void DownloadsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSelectedButton.Visibility = DownloadsList.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DownloadsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DownloadsList.SelectedItems.Count > 0)
            {
                DeleteSelectedButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string SearchText = SearchBox.Text.ToLowerInvariant();
            if (SearchText.Length == 0)
                DownloadsList.ItemsSource = App.Instance.VisibleDownloads;
            else
                DownloadsList.ItemsSource = App.Instance.VisibleDownloads.Where(i => i.FileName?.ToLowerInvariant().Contains(SearchText) ?? false);
        }
    }
}
