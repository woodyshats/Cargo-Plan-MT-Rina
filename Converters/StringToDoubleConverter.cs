using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CargoPlanApp.Converters
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                string format = parameter as string ?? "F2";
                return d.ToString(format, CultureInfo.InvariantCulture);
            }
            if (value is null) return "";
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
                return r;
            return DependencyProperty.UnsetValue;
        }
    }
}