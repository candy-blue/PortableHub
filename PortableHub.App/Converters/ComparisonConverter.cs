using System.Globalization;
using System.Windows.Data;

namespace PortableHub.App.Converters;

public class ComparisonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            return parameter.ToString() ?? System.Windows.Data.Binding.DoNothing;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
