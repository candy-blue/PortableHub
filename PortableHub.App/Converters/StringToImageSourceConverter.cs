using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PortableHub.App.Converters;

/// <summary>
/// Converts an icon file path to a frozen BitmapImage with high-performance memory caching (Section 44).
/// </summary>
public class StringToImageSourceConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, (DateTime LastModified, BitmapImage Image)> ImageCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Windows.Media.ImageSource imageSource)
        {
            return imageSource;
        }

        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }

            if (File.Exists(path))
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    var lastWrite = File.GetLastWriteTimeUtc(fullPath);

                    if (ImageCache.TryGetValue(fullPath, out var cached) && cached.LastModified == lastWrite)
                    {
                        return cached.Image;
                    }

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    ImageCache[fullPath] = (lastWrite, bitmap);
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
