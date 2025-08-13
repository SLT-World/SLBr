using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace WinUI
{
    public class PopupButton : ToggleButton
    {
        private Popup _Popup;

        static PopupButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButton), new FrameworkPropertyMetadata(typeof(PopupButton)));
        }

        public PopupButton()
        {
            DataContextChanged += (s, e) =>
            {
                if (PopupContent != null)
                    PopupContent.DataContext = DataContext;
            };
        }

        public FrameworkElement PopupContent
        {
            get { return (FrameworkElement)GetValue(PopupContentProperty); }
            set { SetValue(PopupContentProperty, value); }
        }

        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register("PopupContent", typeof(FrameworkElement), typeof(PopupButton),
                new PropertyMetadata(null, OnPopupContentChanged));

        private static void OnPopupContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PopupButton _Button = (PopupButton)d;
            if (_Button._Popup != null)
                _Button._Popup.Child = e.NewValue as UIElement;
            if (e.NewValue is FrameworkElement element && _Button.DataContext != null)
                element.DataContext = _Button.DataContext;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _Popup = new Popup
            {
                PlacementTarget = this,
                Placement = PlacementMode.Bottom,
                AllowsTransparency = true,
                StaysOpen = false,
                PopupAnimation = PopupAnimation.Fade
            };
            if (PopupContent != null)
                _Popup.Child = PopupContent;
            _Popup.Closed += (s, e) => IsChecked = false;
        }

        protected override void OnClick()
        {
            if (_Popup != null)
            {
                _Popup.IsOpen = !_Popup.IsOpen;
                IsChecked = _Popup.IsOpen;
            }
        }

        public void OpenPopup()
        {
            if (_Popup != null)
            {
                _Popup.IsOpen = true;
                IsChecked = true;
            }
        }

        public void ClosePopup()
        {
            if (_Popup != null)
            {
                _Popup.IsOpen = false;
                IsChecked = false;
            }
        }
    }
}
