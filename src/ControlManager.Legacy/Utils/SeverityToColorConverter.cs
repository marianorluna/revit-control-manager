using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ControlManager.Models;

namespace ControlManager.Utils
{
    /// <summary>
    /// Convierte <see cref="Severity"/> en color de fondo de fila.
    /// </summary>
    public sealed class SeverityToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush HighBrush = CreateBrush("FFDDDD");
        private static readonly SolidColorBrush MediumBrush = CreateBrush("FFFBCC");
        private static readonly SolidColorBrush LowBrush = CreateBrush("DDFFEE");

        private static SolidColorBrush CreateBrush(string rgbHex)
        {
            byte r = System.Convert.ToByte(rgbHex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(rgbHex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(rgbHex.Substring(4, 2), 16);
            var color = Color.FromRgb(r, g, b);
            return new SolidColorBrush(color);
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Severity s)
            {
                switch (s)
                {
                    case Severity.High:
                        return HighBrush;
                    case Severity.Medium:
                        return MediumBrush;
                    default:
                        return LowBrush;
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
