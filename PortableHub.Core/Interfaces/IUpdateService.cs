namespace PortableHub.Core.Interfaces;

public record UpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    bool HasUpdate,
    string? ReleaseTitle,
    string? ReleaseNotes,
    string? ReleaseUrl,
    DateTime? PublishedAt,
    string? ErrorMessage = null
);

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
