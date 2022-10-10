using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SLBr.Converters
{
    public class SolidToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is SolidColorBrush)
                return ((SolidColorBrush)value).Color;

            throw new InvalidOperationException("Unsupported type [" + value.GetType().Name + "], SolidToColorConverter.ConvertBack()");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is Color)
                return new SolidColorBrush((Color)value);

            throw new InvalidOperationException("Unsupported type [" + value.GetType().Name + "], SolidToColorConverter.Convert()");
        }
    }
}
