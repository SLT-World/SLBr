/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SLBr.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class FavouritesPage : UserControl, IPageOverlay, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public Favourite CurrentFolder
        {
            get => _CurrentFolder;
            set
            {
                _CurrentFolder = value;
                RaisePropertyChanged();
            }
        }
        private Favourite _CurrentFolder;

        private ObservableCollection<Favourite> FavouritesBar;
        
        public FavouritesPage()
        {
            InitializeComponent();
            DataContext = this;
            FavouritesBar = [new Favourite()
            {
                Name = "Favourites bar",
                Type = "folder",
                Children = App.Instance.FavouriteManager.Favourites
            }];
            CurrentFolder = FavouritesBar[0];
        }

        public void Initialize(Browser _BrowserView)
        {
            BrowserView = _BrowserView;
        }

        public void OnNavigated() { }

        public void Dispose()
        {
            FavouritesList.ItemsSource = null;
            FavouritesTreeView.ItemsSource = null;
            GC.SuppressFinalize(this);
        }

        Browser BrowserView;

        private void FavouriteButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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
            BrowserView.FavouriteAction(true, CurrentFolder);
        }

        private void NewFavouriteFolderButton_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.NewFavouriteFolder(CurrentFolder);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FavouritesTreeView.ItemsSource = FavouritesBar;
            ApplyTheme(App.Instance.CurrentTheme);
            FavouritesTreeView.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (FavouritesTreeView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                FavouritesTreeView.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                TreeViewItem? Target = Utils.GetTreeViewItemContainer(FavouritesTreeView, CurrentFolder);
                Target?.IsSelected = true;
            }
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
                App.Instance.FavouriteManager.Remove(Favourite);
        }

        private void DeleteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
                App.Instance.FavouriteManager.Remove(Favourite);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Favourite Favourite)
            {
                List<InputField> Inputs = [
                    new() { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Name },
                ];
                if (Favourite.Type == "url")
                    Inputs.Add(new() { Name = "URL", IsRequired = true, Type = DialogInputType.Text, Value = Favourite.Url });
                DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Edit Favourite", Inputs, "\ue70f")
                {
                    Topmost = true
                };
                if (_DynamicDialogWindow.ShowDialog() == true)
                {
                    Favourite.Name = _DynamicDialogWindow.InputFields[0].Value;
                    if (Favourite.Type == "url")
                        Favourite.Url = _DynamicDialogWindow.InputFields[1].Value.Trim();
                }
            }
        }

        private void FavouritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != sender)
                return;
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
            //TODO: Enhance search functionality.
            if (SearchText.Length == 0)
                FavouritesList.ItemsSource = CurrentFolder.Children;
            else
                FavouritesList.ItemsSource = CurrentFolder.Children.Where(i => i.Type == "url" && (i.Name?.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase) ?? false) || (i.Url?.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject ClickedElement = e.OriginalSource as DependencyObject;
            while (ClickedElement != null && ClickedElement != sender)
            {
                if (ClickedElement is Button)
                    return;
                ClickedElement = VisualTreeHelper.GetParent(ClickedElement);
            }
            if (sender is ListBoxItem Item && Item.IsSelected)
            {
                // && _Favourite.Children != null && _Favourite.Children.Any()
                if (Item.DataContext is Favourite _Favourite && _Favourite.Type == "folder")
                {
                    CurrentFolder = _Favourite;
                    TreeViewItem? Target = Utils.GetTreeViewItemContainer(FavouritesTreeView, CurrentFolder);
                    Target?.IsSelected = true;
                }
                else
                    Item.IsSelected = false;
                e.Handled = true;
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.OriginalSource != sender)
                return;
            if (e.NewValue is Favourite _Favourite)
            {
                CurrentFolder = _Favourite;
                TreeViewItem? Target = Utils.GetTreeViewItemContainer(FavouritesTreeView, CurrentFolder);
                if (Target != null)
                {
                    if (Target.HasItems)
                        Target.IsExpanded = true;
                    Target.Focus();
                }
            }
        }
    }
}
