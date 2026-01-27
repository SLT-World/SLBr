using SLBr.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class FavouritesPage : UserControl, IPageOverlay
    {
        public FavouritesPage(Browser _BrowserView)
        {
            InitializeComponent();
            BrowserView = _BrowserView;
        }

        public void Dispose()
        {
        }

        Browser BrowserView;

        private void FavouriteButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
            {
                if (e.ChangedButton == MouseButton.Left)
                    BrowserView.Navigate(Favourite.Url);
                else if (e.ChangedButton == MouseButton.Middle)
                    BrowserView.Tab.ParentWindow.NewTab(Favourite.Url, false, BrowserView.Tab.ParentWindow.TabsUI.SelectedIndex + 1, BrowserView.Private, BrowserView.Tab.TabGroup);
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
                App.Instance.Favourites.Add(new Favourite() { Type = "url", Url = URL, Name = _DynamicDialogWindow.InputFields[0].Value });
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
            List<Favourite> Selected = FavouritesList.SelectedItems.Cast<Favourite>().ToList();
            foreach (Favourite Favourite in Selected)
                App.Instance.Favourites.Remove(Favourite);
        }

        private void DeleteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
                App.Instance.Favourites.Remove(Favourite);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
            {
                DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Edit Favourite",
                    new List<InputField>
                    {
                        new InputField { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Name },
                        new InputField { Name = "URL", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Url },
                    },
                    "\ue70f"
                );
                _DynamicDialogWindow.Topmost = true;
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    Favourite.Name = _DynamicDialogWindow.InputFields[0].Value;
                    Favourite.Url = _DynamicDialogWindow.InputFields[1].Value.Trim();
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
                FavouritesList.ItemsSource = App.Instance.Favourites.Where(i => i.Type == "url" && (i.Name?.ToLowerInvariant().Contains(SearchText) ?? false) || (i.Url?.ToLowerInvariant().Contains(SearchText) ?? false));
        }
    }
}
