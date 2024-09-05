using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace WinUI
{
    public class DropDownButton : ToggleButton
    {
        public DropDownButton()
        {
            Binding binding = new Binding("Menu.IsOpen");
            binding.Source = this;
            SetBinding(IsCheckedProperty, binding);
            DataContextChanged += (sender, args) =>
            {
                if (Menu != null)
                    Menu.DataContext = DataContext;
            };
        }

        public ContextMenu Menu
        {
            get { return (ContextMenu)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu",
            typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null, OnMenuChanged));

        public double MaxDropDownHeight
        {
            get { return (double)GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        public static readonly DependencyProperty MaxDropDownHeightProperty =
            DependencyProperty.Register("MaxDropDownHeight", typeof(double), typeof(DropDownButton), new PropertyMetadata(double.PositiveInfinity, OnMaxDropDownHeightChanged));

        private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dropDownButton = (DropDownButton)d;
            var contextMenu = (ContextMenu)e.NewValue;
            if (dropDownButton.DataContext != null)
                contextMenu.DataContext = dropDownButton.DataContext;
            if (contextMenu != null && contextMenu.Style == null)
            {
                contextMenu.Style = (Style)Application.Current.Resources["ScrollableContextMenu"];
                contextMenu.MaxHeight = dropDownButton.MaxDropDownHeight;
            }
        }

        private static void OnMaxDropDownHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dropDownButton = (DropDownButton)d;
            if (dropDownButton.Menu != null)
                dropDownButton.Menu.MaxHeight = (double)e.NewValue;
        }

        protected override void OnClick()
        {
            if (Menu != null)
            {
                Menu.PlacementTarget = this;
                Menu.Placement = PlacementMode.Bottom;
                Menu.IsOpen = true;
            }
        }
    }
}
