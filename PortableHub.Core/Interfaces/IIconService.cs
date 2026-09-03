namespace PortableHub.Core.Interfaces;

public interface IIconService
{
    Task<string?> ExtractAndCacheIconAsync(string exePath, int? softwareId = null);
    Task<string?> SaveCustomIconAsync(string sourceImagePath, int? softwareId = null);
    string GetIconsDirectory();
    string? GetCachedIconPath(string exePath);
}
