using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    string GetDataDirectory();
    string GetDatabasePath();
    string GetIconsDirectory();
    string GetLogsDirectory();
    string GetBackupsDirectory();
    bool IsPortableMode { get; }
    void InitializePortableMode(bool isExplicitPortable);
}
