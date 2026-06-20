/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;

namespace SLBr.Controls
{
    public static class ControlExtensions
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(ControlExtensions), new PropertyMetadata(string.Empty));
        
        public static string GetIcon(DependencyObject _Object) =>
            (string)_Object.GetValue(IconProperty);

        public static void SetIcon(DependencyObject _Object, string Value) =>
            _Object.SetValue(IconProperty, Value);
    }
}
