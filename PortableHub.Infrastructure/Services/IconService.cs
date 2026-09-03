using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class IconService : IIconService
{
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, string> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    public IconService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string GetIconsDirectory() => _settingsService.GetIconsDirectory();

    public string? GetCachedIconPath(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            return null;

        lock (_iconCache)
        {
            if (_iconCache.TryGetValue(exePath, out var cached) && File.Exists(cached))
            {
                return cached;
            }
        }

        var hash = ComputeHash(exePath);
        var expectedFile = Path.Combine(GetIconsDirectory(), $"{hash}.png");
        if (File.Exists(expectedFile))
        {
            lock (_iconCache)
            {
                _iconCache[exePath] = expectedFile;
            }
            return expectedFile;
        }

        return null;
    }

    public async Task<string?> ExtractAndCacheIconAsync(string exePath, int? softwareId = null)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        var cached = GetCachedIconPath(exePath);
        if (cached != null)
        {
            return cached;
        }

        return await Task.Run(() =>
        {
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon == null)
                    return null;

                using var bitmap = icon.ToBitmap();
                var hash = ComputeHash(exePath);
                var targetPath = Path.Combine(GetIconsDirectory(), $"{hash}.png");

                // Ensure directory exists
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                bitmap.Save(targetPath, ImageFormat.Png);

                lock (_iconCache)
                {
                    _iconCache[exePath] = targetPath;
                }

                return targetPath;
            }
            catch
            {
                return null;
            }
        });
    }

    public async Task<string?> SaveCustomIconAsync(string sourceImagePath, int? softwareId = null)
    {
        if (string.IsNullOrWhiteSpace(sourceImagePath) || !File.Exists(sourceImagePath))
        {
            return null;
        }

        return await Task.Run(() =>
        {
            try
            {
                using var img = Image.FromFile(sourceImagePath);
                var fileName = $"custom_{Guid.NewGuid():N}.png";
                var targetPath = Path.Combine(GetIconsDirectory(), fileName);

                using var bmp = new Bitmap(img);
                bmp.Save(targetPath, ImageFormat.Png);

                return targetPath;
            }
            catch
            {
                return null;
            }
        });
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
