using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SLBr.Controls
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture) =>
            (bool)Value ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
            => throw new NotImplementedException();
    }
}
