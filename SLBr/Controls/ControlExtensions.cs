using System.Windows;

namespace SLBr.Controls
{
    public static class ControlExtensions
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(ControlExtensions), new PropertyMetadata(string.Empty));
        public static string GetIcon(DependencyObject obj) => (string)obj.GetValue(IconProperty);
        public static void SetIcon(DependencyObject obj, string value) => obj.SetValue(IconProperty, value);
    }
}
