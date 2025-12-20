using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SLBr.Controls
{
    public static class SmoothScrollBehavior
    {
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(SmoothScrollBehavior), new PropertyMetadata(false, OnEnableChanged));

        private static double VelocityX;
        private static double VelocityY;
        private static ScrollViewer? ActiveScrollViewer;
        private static bool Hooked;

        public static void SetEnable(DependencyObject element, bool value)
            => element.SetValue(EnableProperty, value);

        public static bool GetEnable(DependencyObject element)
            => (bool)element.GetValue(EnableProperty);

        private static void OnEnableChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (element is not UIElement Viewer)
                return;

            if ((bool)e.NewValue)
                Viewer.PreviewMouseWheel += OnPreviewMouseWheel;
            else
                Viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private static bool IsInnerScrollable(DependencyObject Source, ScrollViewer OuterViewer)
        {
            while (Source != null && Source != OuterViewer)
            {
                if (Source is ScrollViewer Viewer)
                {
                    if (Viewer != OuterViewer && (Viewer.ScrollableHeight > 0 || Viewer.ScrollableWidth > 0))
                        return true;
                }
                Source = VisualTreeHelper.GetParent(Source);
            }
            return false;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer Viewer = FindScrollViewer(sender as DependencyObject);
            if (Viewer == null || IsInnerScrollable(e.OriginalSource as DependencyObject, Viewer))
                return;

            e.Handled = true;
            ActiveScrollViewer = Viewer;

            bool ForceHorizontal = Viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled && Viewer.ScrollableWidth > 0;

            bool Horizontal = (ForceHorizontal || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

            if (Horizontal && Viewer.ScrollableWidth > 0)
                VelocityX += -e.Delta * 0.1;
            else
                VelocityY += -e.Delta * 0.1;

            if (!Hooked)
            {
                CompositionTarget.Rendering += OnRendering;
                Hooked = true;
            }
        }

        private static void OnRendering(object sender, EventArgs e)
        {
            if (ActiveScrollViewer == null)
                return;
            
            if (Math.Abs(VelocityY) > 0.01)
            {
                ActiveScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(ActiveScrollViewer.VerticalOffset + VelocityY, ActiveScrollViewer.ScrollableHeight)));
                VelocityY *= 0.85;
            }
            if (Math.Abs(VelocityX) > 0.01)
            {
                ActiveScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(ActiveScrollViewer.HorizontalOffset + VelocityX, ActiveScrollViewer.ScrollableWidth)));
                VelocityX *= 0.85;
            }

            if (Math.Abs(VelocityY) < 0.1 && Math.Abs(VelocityX) < 0.1)
            {
                VelocityX = 0;
                VelocityY = 0;
                CompositionTarget.Rendering -= OnRendering;
                Hooked = false;
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject element)
        {
            while (element != null)
            {
                if (element is ScrollViewer Viewer)
                    return Viewer;
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }
}
