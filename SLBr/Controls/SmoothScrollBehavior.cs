/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SLBr.Controls
{
    public static class SmoothScrollBehavior
    {
        public static bool IsDisabled { get; set; } = false;

        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(SmoothScrollBehavior), new PropertyMetadata(false, OnEnableChanged));

        private static double VelocityX;
        private static double VelocityY;
        private static ScrollViewer? ActiveScrollViewer;
        private static bool Hooked;

        public static bool GetEnable(DependencyObject _Object) =>
            (bool)_Object.GetValue(EnableProperty);

        public static void SetEnable(DependencyObject _Object, bool Value) =>
            _Object.SetValue(EnableProperty, Value);

        private static void OnEnableChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (_Object is not UIElement Viewer)
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
                if (Source is Visual || Source is Visual3D)
                    Source = VisualTreeHelper.GetParent(Source);
                else
                    Source = LogicalTreeHelper.GetParent(Source);
            }
            return false;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (IsDisabled) return;
            ScrollViewer Viewer = Utils.FindAncestorOfType<ScrollViewer>(sender as DependencyObject);
            if (Viewer == null || IsInnerScrollable(e.OriginalSource as DependencyObject, Viewer))
                return;

            e.Handled = true;
            ActiveScrollViewer = Viewer;

            bool ForceHorizontal = Viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled && Viewer.ScrollableWidth > 0;
            bool Horizontal = ForceHorizontal || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            double Delta = -e.Delta;

            if (App.Instance.LiteMode)
            {
                if (Horizontal && Viewer.ScrollableWidth > 0)
                    Viewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(Viewer.HorizontalOffset + Delta, Viewer.ScrollableWidth)));
                else if (Viewer.ScrollableHeight > 0)
                    Viewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(Viewer.VerticalOffset + Delta, Viewer.ScrollableHeight)));
            }
            else
            {
                if (Horizontal && Viewer.ScrollableWidth > 0)
                    VelocityX += Delta * 0.1;
                else
                    VelocityY += Delta * 0.1;

                if (!Hooked)
                {
                    CompositionTarget.Rendering += OnRendering;
                    Hooked = true;
                }
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
    }
}
