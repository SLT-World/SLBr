using System.Globalization;
using System.Windows.Data;

namespace SLBr.Controls
{
    public class LessThanConverter : IValueConverter
    {
        public double Threshold { get; set; } = 60;

        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value is double Length)
                return Length < Threshold;
            return false;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
            => throw new NotImplementedException();
    }
}
