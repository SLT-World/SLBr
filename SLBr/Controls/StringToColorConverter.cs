using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace SLBr.Controls
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value == null)
                return Brushes.Gray;

            float Brightness = Parameter == null ? 1 : float.Parse((string)Parameter);

            using var _MD5 = MD5.Create();
            byte[] Hash = _MD5.ComputeHash(Encoding.UTF8.GetBytes(Value.ToString()));

            byte R = (byte)((Hash[0] % 128 + 64) * Brightness);
            byte G = (byte)((Hash[1] % 128 + 64) * Brightness);
            byte B = (byte)((Hash[2] % 128 + 64) * Brightness);

            return new SolidColorBrush(Color.FromRgb(R, G, B));
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
            => throw new NotImplementedException();
    }
}
