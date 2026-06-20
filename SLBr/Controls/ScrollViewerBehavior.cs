/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;

namespace SLBr.Controls
{
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior), new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static double GetVerticalOffset(DependencyObject _Object) =>
            (double)_Object.GetValue(VerticalOffsetProperty);

        public static void SetVerticalOffset(DependencyObject _Object, double Value) =>
            _Object.SetValue(VerticalOffsetProperty, Value);

        private static void OnVerticalOffsetChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (_Object is ScrollViewer Viewer)
                Viewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
