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
    private readonly SemaphoreSlim _fileLock = new(1, 1);

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

        // Clean up any dangling temporary settings files from previous abnormal terminations
        try
        {
            if (Directory.Exists(_dataDirectory))
            {
                var danglingFiles = Directory.GetFiles(_dataDirectory, "settings_*.tmp");
                foreach (var f in danglingFiles)
                {
                    try { File.Delete(f); } catch { }
                }
            }
        }
        catch { }
    }

    public string GetDataDirectory() => _dataDirectory;

    public string GetDatabasePath() => Path.Combine(_dataDirectory, "portablehub.db");

    public string GetIconsDirectory() => Path.Combine(_dataDirectory, "icons");

    public string GetLogsDirectory() => Path.Combine(_dataDirectory, "logs");

    public string GetBackupsDirectory() => Path.Combine(_dataDirectory, "backups");

    public async Task LoadSettingsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var settingsFile = Path.Combine(_dataDirectory, "settings.json");
            var backupFile = Path.Combine(_dataDirectory, "settings.json.bak");

            AppSettings? loaded = null;

            if (File.Exists(settingsFile))
            {
                loaded = TryReadSettings(settingsFile);
            }

            // Fallback to backup file if primary file was corrupted or 0 bytes
            if (loaded == null && File.Exists(backupFile))
            {
                loaded = TryReadSettings(backupFile);
            }

            if (loaded != null)
            {
                _settings = loaded;
                if (string.IsNullOrWhiteSpace(_settings.GlobalHotkey) ||
                    !WindowsHotkeyService.ParseHotkey(_settings.GlobalHotkey, out _, out _))
                {
                    _settings.GlobalHotkey = "Ctrl+Alt+Space";
                }
            }
            else
            {
                _settings = new AppSettings();
                SaveSettingsInternal();
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private static AppSettings? TryReadSettings(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length == 0)
                return null;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<AppSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveSettingsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            SaveSettingsInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public void SaveSettings()
    {
        _fileLock.Wait();
        try
        {
            SaveSettingsInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void SaveSettingsInternal()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        var settingsFile = Path.Combine(_dataDirectory, "settings.json");
        var tempFile = Path.Combine(_dataDirectory, $"settings_{Guid.NewGuid():N}.tmp");
        var backupFile = Path.Combine(_dataDirectory, "settings.json.bak");

        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });

            // 1. Write completely to temp file and flush
            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
            {
                writer.Write(json);
                writer.Flush();
            }

            // 2. Maintain backup copy of last good configuration
            if (File.Exists(settingsFile))
            {
                try
                {
                    File.Copy(settingsFile, backupFile, overwrite: true);
                }
                catch { }
            }

            // 3. Atomically replace destination file (NTFS MoveFileEx atomic directory entry swap)
            File.Move(tempFile, settingsFile, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
            throw;
        }
    }
}
