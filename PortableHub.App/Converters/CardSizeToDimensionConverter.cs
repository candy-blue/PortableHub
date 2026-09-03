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
                "IconSize" => 40.0,
                "FontSize" => 12.0,
                _ => 140.0
            },
            "Large" => param switch
            {
                "Width" => 220.0,
                "Height" => 220.0,
                "IconSize" => 64.0,
                "FontSize" => 15.0,
                _ => 220.0
            },
            _ => param switch // Medium
            {
                "Width" => 170.0,
                "Height" => 180.0,
                "IconSize" => 50.0,
                "FontSize" => 13.0,
                _ => 170.0
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
