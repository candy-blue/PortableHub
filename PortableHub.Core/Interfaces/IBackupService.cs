using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string? customDestinationFile = null);
    Task<bool> RestoreBackupAsync(string backupFilePath);
    Task CheckAndPerformAutoBackupAsync();
    IReadOnlyList<string> GetExistingBackups();
    string GetBackupsDirectory();
    Task<string> ExportJsonAsync(string targetFilePath);
    Task<int> ImportJsonAsync(string sourceFilePath);
}
