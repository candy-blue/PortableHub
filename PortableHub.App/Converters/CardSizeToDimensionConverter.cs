using System.Globalization;
using System.Windows.Data;

namespace PortableHub.App.Converters;

public class CardSizeToDimensionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var size = value as string ?? "Medium";
        var param = parameter as string ?? "Width";

        return size switch
        {
            "Small" => param switch
            {
                "Width" => 140.0,
                "Height" => 150.0,
                "IconSize" => 44.0,
                "FontSize" => 14.0,
                _ => 140.0
            },
            "Large" => param switch
            {
                "Width" => 220.0,
                "Height" => 225.0,
                "IconSize" => 68.0,
                "FontSize" => 16.0,
                _ => 220.0
            },
            _ => param switch // Medium
            {
                "Width" => 170.0,
                "Height" => 175.0,
                "IconSize" => 54.0,
                "FontSize" => 14.0,
                _ => 170.0
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
