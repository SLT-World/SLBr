using System;
using System.Windows.Data;
using System.Windows.Media;

namespace SLBr.Converters
{
    public class ColorToSolidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is Color)
                return new SolidColorBrush((Color)value);

            throw new InvalidOperationException("Unsupported type [" + value.GetType().Name + "], ColorToSolidConverter.Convert()");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is SolidColorBrush)
                return ((SolidColorBrush)value).Color;

            throw new InvalidOperationException("Unsupported type [" + value.GetType().Name + "], ColorToSolidConverter.ConvertBack()");
        }

    }
}
