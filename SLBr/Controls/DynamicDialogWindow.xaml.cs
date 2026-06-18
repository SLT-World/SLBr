/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    public enum DialogInputType
    {
        Text,
        Label,
        Boolean,
        Color,
        Dropdown,
        Tree
    }

    public class InputField : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        private string _Value = "";
        private bool _BoolValue = false;
        private UIElementLayer _TreeValue;

        public string Name { get; set; } = "";
        public bool _IsRequired { get; set; }
        public bool IsRequired
        {
            get => _IsRequired;
            set
            {
                if (_IsRequired != value)
                {
                    _IsRequired = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DialogInputType Type { get; set; } = DialogInputType.Text;

        public string Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool BoolValue
        {
            get => _BoolValue;
            set
            {
                if (_BoolValue != value)
                {
                    _BoolValue = value;
                    RaisePropertyChanged();
                }
            }
        }

        public UIElementLayer TreeValue
        {
            get => _TreeValue;
            set
            {
                if (_TreeValue != value)
                {
                    _TreeValue = value;
                    RaisePropertyChanged();
                }
            }
        }

        public List<string> Options { get; set; }
        public ObservableCollection<UIElementLayer> Children { get; set; }
    }

    /// <summary>
    /// Interaction logic for DynamicDialogWindow.xaml
    /// </summary>
    public partial class DynamicDialogWindow : Window
    {
        public ObservableCollection<InputField> InputFields { get; set; } = [];

        public DynamicDialogWindow(string _Title, string Question, List<InputField> DefaultFields, string Icon = "", string PositiveText = "OK", string NegativeText = "Cancel")
        {
            Title = _Title;

            InitializeComponent();
            InputsList.ItemsSource = InputFields;

            TitleText.Text = Question;
            foreach (InputField Field in DefaultFields)
                InputFields.Add(Field);
            ValidateInputs(null, null);

            if (!string.IsNullOrEmpty(Icon))
                TitleIcon.Text = Icon;

            PositiveButton.Visibility = string.IsNullOrEmpty(PositiveText) ? Visibility.Collapsed : Visibility.Visible;
            PositiveButton.Content = PositiveText;
            NegativeButton.Visibility = string.IsNullOrEmpty(NegativeText) ? Visibility.Collapsed : Visibility.Visible;
            NegativeButton.Content = NegativeText;

            ApplyTheme(App.Instance.CurrentTheme);
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.125)
            });
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
        private async void PositiveButton_Click(object sender, RoutedEventArgs e)
        {
            PositiveButton.IsEnabled = false;
            NegativeButton.IsEnabled = false;
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.125)
            });
            await Task.Delay(125);
            DialogResult = true;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject Root) where T : DependencyObject
        {
            if (Root == null)
                yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Root); i++)
            {
                DependencyObject Child = VisualTreeHelper.GetChild(Root, i);
                if (Child is T t)
                    yield return t;
                foreach (T Descendant in FindVisualChildren<T>(Child))
                    yield return Descendant;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            TextBox? FirstTextBox = FindVisualChildren<TextBox>(InputsList).FirstOrDefault();
            FirstTextBox?.SelectAll();
            FirstTextBox?.Focus();
            foreach (ListBox List in FindVisualChildren<ListBox>(InputsList))
            {
                if (List.DataContext is not InputField Field)
                    continue;
                if (Field.Type != DialogInputType.Color)
                    continue;
                if (string.IsNullOrWhiteSpace(Field.Value))
                {
                    List.SelectedIndex = 0;
                    Field.Value = Utils.ColorToHex(Colors.White);
                    continue;
                }
                foreach (ListBoxItem Item in List.Items)
                {
                    if (Item.Background is SolidColorBrush Background)
                    {
                        if (string.Equals(Utils.ColorToHex(Background.Color), Field.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            List.SelectedItem = Item;
                            break;
                        }
                        else if (Item.ToolTip?.ToString() == "Custom")
                        {
                            Color Value = Utils.HexToColor(Field.Value);
                            Item.Background = new SolidColorBrush(Value);
                            Item.Foreground = Utils.GetContrastBrush(Value);
                            List.SelectedItem = Item;
                            break;
                        }
                    }
                }
            }
            foreach (ComboBox _ComboBox in FindVisualChildren<ComboBox>(InputsList))
            {
                if (_ComboBox.DataContext is not InputField Field)
                    continue;
                if (Field.Type != DialogInputType.Dropdown)
                    continue;
                if (Field.Value == null && Field.Options.Count > 0)
                {
                    _ComboBox.SelectedIndex = 0;
                    Field.Value = (string)_ComboBox.SelectedItem;
                }
            }

            foreach (TreeView Tree in FindVisualChildren<TreeView>(InputsList))
            {
                if (Tree.DataContext is not InputField Field)
                    continue;
                if (Field.Children == null || Field.Children.Count == 0)
                    continue;
                TreeViewItem? Target;
                if (Field.TreeValue != null)
                {
                    Target = Utils.GetTreeViewItemContainer(Tree, Field.TreeValue);
                    Target ??= Utils.GetFirstTreeViewItem(Tree);
                }
                else
                    Target = Utils.GetFirstTreeViewItem(Tree);
                if (Target != null)
                {
                    Target.IsSelected = true;
                    if (Target.HasItems)
                        Target.IsExpanded = true;
                    Target.Focus();
                }
            }
            HasInitialized = true;
        }

        bool HasInitialized = false;

        private void ValidateInputs(object sender, KeyEventArgs e)
        {
            PositiveButton.IsEnabled = InputFields.All(i => (i.Type != DialogInputType.Text || !i.IsRequired || !string.IsNullOrWhiteSpace(i.Value)) && (i.Type != DialogInputType.Dropdown || !i.IsRequired || i.Value != null));
        }

        private void ColorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!HasInitialized)
                return;
            if (sender is not ListBox List)
                return;
            if (List.DataContext is not InputField Field)
                return;
            if (List.SelectedItem is not ListBoxItem Item)
                return;
            if (Item.ToolTip?.ToString() == "Custom")
            {
                ColorPickerWindow Picker = new(Utils.HexToColor(Field.Value))
                {
                    Topmost = true
                };
                Color Value = Picker.ShowDialog() == true ? Picker.UserInput.Color : Utils.HexToColor(Field.Value);
                Field.Value = Utils.ColorToHex(Value);
                Item.Background = new SolidColorBrush(Value);
                Item.Foreground = Utils.GetContrastBrush(Value);
                return;
            }
            if (Item.Background is SolidColorBrush Background)
                Field.Value = Utils.ColorToHex(Background.Color);
            ValidateInputs(null, null);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!HasInitialized)
                return;
            /*if (sender is not ComboBox _ComboBox)
                return;
            if (_ComboBox.DataContext is not InputField Field)
                return;*/
            ValidateInputs(null, null);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is not TreeView Tree || Tree.DataContext is not InputField Field)
                return;
            if (e.NewValue is UIElementLayer SelectedLayer)
            {
                Field.TreeValue = SelectedLayer;
                ValidateInputs(null, null);
            }
        }
    }
}
