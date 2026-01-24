using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;

namespace WinUI
{
    class TabPanel : Panel
    {
        private double _RowHeight;
        private double _ScaleFactor;

        static TabPanel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TabPanel), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(TabPanel), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
        }

        private void Child_Unloaded(object? sender, EventArgs e)
        {
            if (sender is UIElement Element)
            {
                CurrentSize.Width = ActualWidth;
                SetChildrenMaxWidths(CurrentSize.Width);
            }
        }
        bool DesignMode;
        private DispatcherTimer ResizeTimer;
        public TabPanel()
        {
            DesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            if (!DesignMode)
            {
                ResizeTimer = new DispatcherTimer();
                ResizeTimer.Interval = TimeSpan.FromMilliseconds(500);
                ResizeTimer.Tick += ResizeTimer_Tick;
            }
        }

        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            ResizeTimer.Stop();

            SetChildrenMaxWidths(CurrentSize.Width);
        }

        Size CurrentSize;

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (!DesignMode)
            {
                base.OnRenderSizeChanged(sizeInfo);
                ResizeTimer.Stop();
                ResizeTimer.Start();
                CurrentSize = sizeInfo.NewSize;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (!DesignMode)
            {
                if (e.Property.ToString() == "TabOnceActiveElement")
                {
                    foreach (UIElement Child in Children)
                    {
                        if (Child is FrameworkElement Element)
                        {
                            Element.Unloaded -= Child_Unloaded;
                            Element.Unloaded += Child_Unloaded;
                        }
                    }
                    CurrentSize.Width = ActualWidth;
                    SetChildrenMaxWidths(ActualWidth);
                }
            }
        }

        public void SetChildrenMaxWidths(double MaxWidth)
        {
            double _Width = 0.0;
            _RowHeight = 0.0;
            Size MaximumSize = new Size(MaxWidth, 45.0);
            foreach (UIElement Element in Children)
            {
                Element.SetValue(MaxWidthProperty, 250.0);
                Element.Measure(MaximumSize);
                Size _Size = GetDesiredSizeLessMargin(Element);
                _RowHeight = Math.Max(_RowHeight, _Size.Height);
                _Width += _Size.Width;
            }

            if (_Width > MaximumSize.Width)
            {
                _ScaleFactor = MaximumSize.Width / _Width;
                _Width = 0.0;
                foreach (UIElement Element in Children)
                {
                    Element.Measure(new Size(Element.DesiredSize.Width * _ScaleFactor, MaximumSize.Height));
                    _Width += Element.DesiredSize.Width;
                }
            }
            else
                _ScaleFactor = 1.0;
            Size ArrangeSize = new Size(_Width, _RowHeight);
            Point _Point = new Point();
            foreach (UIElement Element in Children)
            {
                Size Size1 = Element.DesiredSize;
                Size Size2 = GetDesiredSizeLessMargin(Element);
                Thickness _Margin = (Thickness)Element.GetValue(MarginProperty);
                double TabWidth = Size2.Width;
                if (Element.DesiredSize.Width != Size2.Width)
                    TabWidth = ArrangeSize.Width - _Point.X;
                if (Children.IndexOf(Element) != Children.Count - 1)
                {
                    Element.SetValue(WidthProperty, double.NaN);
                    Element.SetValue(MaxWidthProperty, Math.Max(TabWidth, 40));
                }
                double LeftRightMargin = Math.Max(0.0, -(_Margin.Left + _Margin.Right));
                _Point.X += Size1.Width + (LeftRightMargin * _ScaleFactor);
            }
        }
        protected override Size MeasureOverride(Size AvailableSize)
        {
            double _Width = 0.0;
            _RowHeight = 0.0;
            foreach (UIElement Element in Children)
            {
                Element.Measure(AvailableSize);
                Size size = GetDesiredSizeLessMargin(Element);
                _RowHeight = Math.Max(_RowHeight, size.Height);
                _Width += size.Width;
            }

            if (_Width > AvailableSize.Width)
            {
                _ScaleFactor = AvailableSize.Width / _Width;
                _Width = 0.0;
                foreach (UIElement Element in Children)
                {
                    Element.Measure(new Size(Element.DesiredSize.Width * _ScaleFactor, AvailableSize.Height));
                    _Width += Element.DesiredSize.Width;
                }
            }
            else
                _ScaleFactor = 1.0;
            return new Size(_Width, _RowHeight);
        }
        protected override Size ArrangeOverride(Size ArrangeSize)
        {
            Point _Point = new Point();
            foreach (UIElement Element in Children)
            {
                Size Size1 = Element.DesiredSize;
                Size Size2 = GetDesiredSizeLessMargin(Element);
                Thickness margin = (Thickness)Element.GetValue(MarginProperty);
                double _Width = Size2.Width;
                if (Element.DesiredSize.Width != Size2.Width)
                    _Width = ArrangeSize.Width - _Point.X;
                Element.Arrange(new Rect(_Point, new Size(Math.Min(_Width, Size2.Width), _RowHeight)));
                if (Children.IndexOf(Element) != Children.Count - 1)
                    Element.SetValue(MinWidthProperty, Math.Max(_Width, 40));
                double leftRightMargin = Math.Max(0.0, -(margin.Left + margin.Right));
                _Point.X += Size1.Width + (leftRightMargin * _ScaleFactor);
            }
            return ArrangeSize;
        }

        private Size GetDesiredSizeLessMargin(UIElement Element)
        {
            Thickness _Margin = (Thickness)Element.GetValue(MarginProperty);
            Size _Size = new Size();
            _Size.Height = Math.Max(0.0, Element.DesiredSize.Height - (_Margin.Top + _Margin.Bottom));
            _Size.Width = Math.Max(0.0, Element.DesiredSize.Width - (_Margin.Left + _Margin.Right));
            return _Size;
        }
    }
}
