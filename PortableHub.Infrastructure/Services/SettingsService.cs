using System.Text.Json;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _baseDir;
    private bool _isPortableMode;
    private string _dataDirectory = string.Empty;
    private AppSettings _settings = new();

    public AppSettings CurrentSettings => _settings;
    public bool IsPortableMode => _isPortableMode;

    public SettingsService(string? customBaseDirectory = null)
    {
        _baseDir = customBaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
        DetectPortableMode();
        EnsureDirectories();
    }

    public void InitializePortableMode(bool isExplicitPortable)
    {
        if (isExplicitPortable)
        {
            _isPortableMode = true;
            _dataDirectory = Path.Combine(_baseDir, "data");
        }
        else
        {
            DetectPortableMode();
        }
        EnsureDirectories();
    }

    private void DetectPortableMode()
    {
        var portableMarker = Path.Combine(_baseDir, "portable");
        var portableMarkerTxt = Path.Combine(_baseDir, "portable.txt");
        var dataFolderInApp = Path.Combine(_baseDir, "data");

        if (File.Exists(portableMarker) || File.Exists(portableMarkerTxt) || Directory.Exists(dataFolderInApp))
        {
            _isPortableMode = true;
            _dataDirectory = Path.Combine(_baseDir, "data");
        }
        else
        {
            _isPortableMode = false;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dataDirectory = Path.Combine(localAppData, "PortableHub");
        }
    }

    private void EnsureDirectories()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        if (!Directory.Exists(GetIconsDirectory()))
        {
            Directory.CreateDirectory(GetIconsDirectory());
        }
        if (!Directory.Exists(GetBackupsDirectory()))
        {
            Directory.CreateDirectory(GetBackupsDirectory());
        }
        if (!Directory.Exists(GetLogsDirectory()))
        {
            Directory.CreateDirectory(GetLogsDirectory());
        }
    }

    public string GetDataDirectory() => _dataDirectory;

    public string GetDatabasePath() => Path.Combine(_dataDirectory, "portablehub.db");

    public string GetIconsDirectory() => Path.Combine(_dataDirectory, "icons");

    public string GetLogsDirectory() => Path.Combine(_dataDirectory, "logs");

    public string GetBackupsDirectory() => Path.Combine(_dataDirectory, "backups");

    public async Task LoadSettingsAsync()
    {
        var settingsFile = Path.Combine(_dataDirectory, "settings.json");
        if (File.Exists(settingsFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(settingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                    if (string.IsNullOrWhiteSpace(_settings.GlobalHotkey) ||
                        !WindowsHotkeyService.ParseHotkey(_settings.GlobalHotkey, out _, out _))
                    {
                        _settings.GlobalHotkey = "Ctrl+Alt+Space";
                    }
                }
            }
            catch
            {
                // Fallback to defaults on corrupt settings
                _settings = new AppSettings();
            }
        }
        else
        {
            _settings = new AppSettings();
            await SaveSettingsAsync();
        }
    }

    public async Task SaveSettingsAsync()
    {
        var settingsFile = Path.Combine(_dataDirectory, "settings.json");
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile, json);
    }
}
