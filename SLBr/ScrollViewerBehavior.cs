using System.Windows;
using System.Windows.Controls;

namespace SLBr
{
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior), new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static double GetVerticalOffset(DependencyObject element) =>
            (double)element.GetValue(VerticalOffsetProperty);

        public static void SetVerticalOffset(DependencyObject element, double value) =>
            element.SetValue(VerticalOffsetProperty, value);

        private static void OnVerticalOffsetChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (element is ScrollViewer Viewer)
                Viewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
