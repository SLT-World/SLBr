using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Xml.Linq;
using System.Windows.Media;
using System.Globalization;
using System.Reflection;
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;

namespace WinUI
{
    class TabPanel : Panel
    {
        /*public TabPanel()
        {
            IsItemsHost = true;
            Rows = 1;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var totalMaxWidth = Children.OfType<TabItem>().Sum(tab => tab.MaxWidth);
            if (!double.IsInfinity(totalMaxWidth))
                HorizontalAlignment = constraint.Width > totalMaxWidth
                   ? HorizontalAlignment.Left
                   : HorizontalAlignment.Stretch;

            return base.MeasureOverride(constraint);
        }*/

        private double _rowHeight;
        private double _scaleFactor;

        static TabPanel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TabPanel), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(TabPanel), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
        }

        private void Child_Unloaded(object? sender, EventArgs e)
        {
            if (sender is UIElement updatedElement)
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
                //MessageBox.Show("Size Changed");
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (!DesignMode)
            {
                //if (e.Property != IsMouseOverProperty)
                //    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(e.Property.Name.ToString())));
                
                if (e.Property.ToString() == "TabOnceActiveElement")
                {
                    foreach (UIElement child in Children)
                    {
                        if (child is FrameworkElement frameworkElement)
                        {
                            frameworkElement.Unloaded -= Child_Unloaded;
                            frameworkElement.Unloaded += Child_Unloaded;
                        }
                    }
                    CurrentSize.Width = ActualWidth;
                    SetChildrenMaxWidths(ActualWidth);
                    //SetChildrenMaxWidths(SystemParameters.PrimaryScreenWidth - (margin.Left + margin.Right));
                }
            }
        }

        private void SetChildrenMaxWidths(double MaxWidth)
        {
            double width = 0.0;
            _rowHeight = 0.0;
            Size MaximumSize = new Size(MaxWidth, 45.0);
            foreach (UIElement element in Children)
            {
                element.SetValue(MaxWidthProperty, 250.0);
                element.Measure(MaximumSize);
                Size size = GetDesiredSizeLessMargin(element);
                _rowHeight = Math.Max(_rowHeight, size.Height);
                width += size.Width;
            }

            if (width > MaximumSize.Width)
            {
                _scaleFactor = MaximumSize.Width / width;
                width = 0.0;
                foreach (UIElement element in Children)
                {
                    element.Measure(new Size(element.DesiredSize.Width * _scaleFactor, MaximumSize.Height));
                    width += element.DesiredSize.Width;
                }
            }
            else
                _scaleFactor = 1.0;
            Size arrangeSize = new Size(width, _rowHeight);
            Point point = new Point();
            foreach (UIElement element in Children)
            {
                Size size1 = element.DesiredSize;
                Size size2 = GetDesiredSizeLessMargin(element);
                Thickness margin = (Thickness)element.GetValue(MarginProperty);
                double TabWidth = size2.Width;
                if (element.DesiredSize.Width != size2.Width)
                    TabWidth = arrangeSize.Width - point.X; // Last-tab-selected "fix"
                if (Children.IndexOf(element) != Children.Count - 1)
                {
                    element.SetValue(WidthProperty, double.NaN);
                    element.SetValue(MaxWidthProperty, Math.Max(TabWidth, 40));
                    //element.SetValue(WidthProperty, width);
                }
                double leftRightMargin = Math.Max(0.0, -(margin.Left + margin.Right));
                point.X += size1.Width + (leftRightMargin * _scaleFactor);
            }
        }

        // This Panel lays its children out horizontally.
        // If all children cannot fit in the allocated space,
        // the available space is divided proportionally between them.
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0.0;
            _rowHeight = 0.0;
            foreach (UIElement element in Children)
            {
                element.Measure(availableSize);
                Size size = GetDesiredSizeLessMargin(element);
                _rowHeight = Math.Max(_rowHeight, size.Height);
                width += size.Width;
            }

            if (width > availableSize.Width)
            {
                _scaleFactor = availableSize.Width / width;
                width = 0.0;
                foreach (UIElement element in Children)
                {
                    element.Measure(new Size(element.DesiredSize.Width * _scaleFactor, availableSize.Height));
                    width += element.DesiredSize.Width;
                }
            }
            else
                _scaleFactor = 1.0;
            return new Size(width, _rowHeight);
        }

        // Perform arranging of children based on the final size
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Point point = new Point();
            foreach (UIElement element in Children)
            {
                Size size1 = element.DesiredSize;
                Size size2 = GetDesiredSizeLessMargin(element);
                Thickness margin = (Thickness)element.GetValue(MarginProperty);
                double width = size2.Width;
                if (element.DesiredSize.Width != size2.Width)
                    width = arrangeSize.Width - point.X; // Last-tab-selected "fix"
                element.Arrange(new Rect(point, new Size(Math.Min(width, size2.Width), _rowHeight)));
                if (Children.IndexOf(element) != Children.Count - 1)
                {
                    //element.SetValue(WidthProperty, double.NaN);
                    element.SetValue(MinWidthProperty, Math.Max(width, 40));
                    //element.SetValue(WidthProperty, width);
                }
                double leftRightMargin = Math.Max(0.0, -(margin.Left + margin.Right));
                point.X += size1.Width + (leftRightMargin * _scaleFactor);
            }
            return arrangeSize;
        }

        // Return element's size
        // after subtracting margin
        private Size GetDesiredSizeLessMargin(UIElement element)
        {
            Thickness margin = (Thickness)element.GetValue(MarginProperty);
            Size size = new Size();
            size.Height = Math.Max(0.0, element.DesiredSize.Height - (margin.Top + margin.Bottom));
            size.Width = Math.Max(0.0, element.DesiredSize.Width - (margin.Left + margin.Right));
            return size;
        }
    }
}
