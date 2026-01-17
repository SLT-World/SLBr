using CefSharp;
using CefSharp.DevTools.Autofill;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class Favourites : UserControl, IPageOverlay
    {
        public Favourites(Browser _BrowserView)
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
            BrowserView.Tab.ParentWindow.NewTab(Values[1], true, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1);
        }

        private void FavouriteButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                string[] Values = ((FrameworkElement)sender).Tag.ToString().Split("<,>");
                BrowserView.Tab.ParentWindow.NewTab(Values[1], false, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1);
            }
        }

        private void NewFavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Add Favourite",
                new List<InputField>
                {
                        new InputField { Name = "Name", IsRequired = true, Type = DialogInputType.Text },
                        new InputField { Name = "URL", IsRequired = true, Type = DialogInputType.Text },
                },
                "\ue946"
            );
            _DynamicDialogWindow.Topmost = true;
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                string URL = _DynamicDialogWindow.InputFields[1].Value.Trim();
                App.Instance.Favourites.Add(new ActionStorage(_DynamicDialogWindow.InputFields[0].Value, $"4<,>{URL}", URL));
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FavouritesList.ItemsSource = App.Instance.Favourites;
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
            List<ActionStorage> Selected = FavouritesList.SelectedItems.Cast<ActionStorage>().ToList();
            foreach (ActionStorage Favourite in Selected)
                App.Instance.Favourites.Remove(Favourite);
        }

        private void DeleteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is ActionStorage Favourite)
                App.Instance.Favourites.Remove(Favourite);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is ActionStorage Favourite)
            {
                DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Edit Favourite",
                    new List<InputField>
                    {
                        new InputField { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Name },
                        new InputField { Name = "URL", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Tooltip },
                    },
                    "\ue70f"
                );
                _DynamicDialogWindow.Topmost = true;
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    Favourite.Name = _DynamicDialogWindow.InputFields[0].Value;
                    string URL = _DynamicDialogWindow.InputFields[1].Value.Trim();
                    Favourite.Tooltip = URL;
                    Favourite.Arguments = $"4<,>{URL}";
                }
            }
        }

        private void FavouritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSelectedButton.Visibility = FavouritesList.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FavouritesList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && FavouritesList.SelectedItems.Count > 0)
            {
                DeleteSelectedButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string SearchText = SearchBox.Text.ToLowerInvariant();
            if (SearchText.Length == 0)
                FavouritesList.ItemsSource = App.Instance.Favourites;
            else
                FavouritesList.ItemsSource = App.Instance.Favourites.Where(i => (i.Name?.ToLowerInvariant().Contains(SearchText) ?? false) || (i.Tooltip?.ToLowerInvariant().Contains(SearchText) ?? false));
        }
    }
}
