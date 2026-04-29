using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EcoUtils.Converters;

[ValueConversion(typeof(bool), typeof(GridLength))]
public class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true)
        {
            if (parameter is string s)
            {
                if (s.EndsWith("*") && double.TryParse(
                        s.TrimEnd('*'),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double star))
                    return new GridLength(star, GridUnitType.Star);

                if (double.TryParse(s,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double width))
                    return new GridLength(width);
            }
            return GridLength.Auto;
        }
        return new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
