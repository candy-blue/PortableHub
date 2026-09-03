using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PortableHub.App.Converters;

public class ColorHexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Fallback
            }
        }
        return new SolidColorBrush(Color.FromRgb(59, 130, 246));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
