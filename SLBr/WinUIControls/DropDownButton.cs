/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

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
            Binding _Binding = new("Menu.IsOpen")
            {
                Source = this
            };
            SetBinding(IsCheckedProperty, _Binding);
            DataContextChanged += (sender, args) =>
            {
                Menu?.DataContext = DataContext;
            };
        }

        public ContextMenu Menu
        {
            get => (ContextMenu)GetValue(MenuProperty);
            set => SetValue(MenuProperty, value);
        }
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null, OnMenuChanged));

        public double MaxDropDownHeight
        {
            get => (double)GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register("MaxDropDownHeight", typeof(double), typeof(DropDownButton), new PropertyMetadata(double.PositiveInfinity, OnMaxDropDownHeightChanged));

        private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton _DropDownButton = (DropDownButton)d;
            ContextMenu _ContextMenu = (ContextMenu)e.NewValue;
            if (_DropDownButton.DataContext != null)
                _ContextMenu.DataContext = _DropDownButton.DataContext;
            if (_ContextMenu != null && _ContextMenu.Style == null)
            {
                _ContextMenu.Style = (Style)Application.Current.Resources["ScrollableContextMenu"];
                _ContextMenu.MaxHeight = _DropDownButton.MaxDropDownHeight;
            }
        }

        private static void OnMaxDropDownHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton _DropDownButton = (DropDownButton)d;
            _DropDownButton.Menu?.MaxHeight = (double)e.NewValue;
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

        public void OpenMenu()
        {
            if (Menu != null)
            {
                Menu.PlacementTarget = this;
                Menu.Placement = PlacementMode.Bottom;
                Menu.IsOpen = true;
            }
        }

        public void CloseMenu()
        {
            Menu?.IsOpen = false;
        }
    }
}
