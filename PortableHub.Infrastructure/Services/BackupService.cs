using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly ISettingsService _settingsService;
    private readonly ISoftwareRepository _softwareRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IRootDirectoryRepository _rootRepository;

    public BackupService(
        ISettingsService settingsService,
        ISoftwareRepository softwareRepository,
        ICategoryRepository categoryRepository,
        IRootDirectoryRepository rootRepository)
    {
        _settingsService = settingsService;
        _softwareRepository = softwareRepository;
        _categoryRepository = categoryRepository;
        _rootRepository = rootRepository;
    }

    public string GetBackupsDirectory() => _settingsService.GetBackupsDirectory();

    public IReadOnlyList<string> GetExistingBackups()
    {
        var dir = GetBackupsDirectory();
        if (!Directory.Exists(dir))
            return [];

        return Directory.GetFiles(dir, "*.phbackup")
            .OrderByDescending(File.GetCreationTimeUtc)
            .ToList();
    }

    public async Task<string> CreateBackupAsync(string? customDestinationFile = null)
    {
        var targetFile = customDestinationFile;
        if (string.IsNullOrWhiteSpace(targetFile))
        {
            var fileName = $"PortableHub_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.phbackup";
            targetFile = Path.Combine(GetBackupsDirectory(), fileName);
        }

        var dir = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(targetFile))
        {
            File.Delete(targetFile);
        }

        var allSoftware = await _softwareRepository.GetAllAsync();
        var allCategories = await _categoryRepository.GetAllAsync();
        var allRoots = await _rootRepository.GetAllAsync();

        var manifest = new BackupManifest
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            AppVersion = "1.0.0",
            SoftwareCount = allSoftware.Count,
            CategoryCount = allCategories.Count,
            RootCount = allRoots.Count,
            Description = "PortableHub Automated/Manual Backup"
        };

        var dbPath = _settingsService.GetDatabasePath();
        var settingsPath = Path.Combine(_settingsService.GetDataDirectory(), "settings.json");
        var iconsDir = _settingsService.GetIconsDirectory();

        // Checkpoint SQLite DB to ensure DB file is flushed
        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath};");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore checkpoint error if database is locked or empty
        }

        SqliteConnection.ClearAllPools();

        using (var zip = ZipFile.Open(targetFile, ZipArchiveMode.Create))
        {
            // 1. Manifest
            var manifestEntry = zip.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                await writer.WriteAsync(manifestJson);
            }

            // 2. Database
            if (File.Exists(dbPath))
            {
                var dbEntry = zip.CreateEntry("portablehub.db", CompressionLevel.Optimal);
                using var fileStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var entryStream = dbEntry.Open();
                await fileStream.CopyToAsync(entryStream);
            }

            // 3. Settings
            if (File.Exists(settingsPath))
            {
                var settingsEntry = zip.CreateEntry("settings.json", CompressionLevel.Optimal);
                using var fileStream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var entryStream = settingsEntry.Open();
                await fileStream.CopyToAsync(entryStream);
            }

            // 4. Icons
            if (Directory.Exists(iconsDir))
            {
                var iconFiles = Directory.GetFiles(iconsDir, "*.*");
                foreach (var iconFile in iconFiles)
                {
                    var entryName = Path.Combine("icons", Path.GetFileName(iconFile));
                    zip.CreateEntryFromFile(iconFile, entryName, CompressionLevel.Optimal);
                }
            }
        }

        _settingsService.CurrentSettings.LastBackupAt = DateTime.UtcNow;
        await _settingsService.SaveSettingsAsync();

        return targetFile;
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            return false;

        // Verify valid backup
        using (var zip = ZipFile.OpenRead(backupFilePath))
        {
            var hasDb = zip.GetEntry("portablehub.db") != null;
            var hasManifest = zip.GetEntry("manifest.json") != null;
            if (!hasDb && !hasManifest)
            {
                return false;
            }
        }

        // 1. Pre-restore automatic safety backup
        try
        {
            var safetyBackupName = $"PreRestore_Safety_{DateTime.UtcNow:yyyyMMdd_HHmmss}.phbackup";
            var safetyPath = Path.Combine(GetBackupsDirectory(), safetyBackupName);
            await CreateBackupAsync(safetyPath);
        }
        catch
        {
            // Ignore pre-backup errors
        }

        // 2. Close all connections by clearing pool
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // 3. Extract contents
        using (var zip = ZipFile.OpenRead(backupFilePath))
        {
            var dataDir = _settingsService.GetDataDirectory();
            var iconsDir = _settingsService.GetIconsDirectory();

            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.Equals("portablehub.db", StringComparison.OrdinalIgnoreCase))
                {
                    var dest = _settingsService.GetDatabasePath();
                    entry.ExtractToFile(dest, overwrite: true);
                }
                else if (entry.FullName.Equals("settings.json", StringComparison.OrdinalIgnoreCase))
                {
                    var dest = Path.Combine(dataDir, "settings.json");
                    entry.ExtractToFile(dest, overwrite: true);
                }
                else if (entry.FullName.StartsWith("icons/", StringComparison.OrdinalIgnoreCase) ||
                         entry.FullName.StartsWith("icons\\", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileName(entry.FullName);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var dest = Path.Combine(iconsDir, fileName);
                        entry.ExtractToFile(dest, overwrite: true);
                    }
                }
            }
        }

        // Reload settings
        await _settingsService.LoadSettingsAsync();
        return true;
    }

    public async Task CheckAndPerformAutoBackupAsync()
    {
        var settings = _settingsService.CurrentSettings;
        if (settings.BackupFrequency.Equals("Off", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var shouldBackup = false;
        var now = DateTime.UtcNow;

        if (!settings.LastBackupAt.HasValue)
        {
            shouldBackup = true;
        }
        else
        {
            var diff = now - settings.LastBackupAt.Value;
            shouldBackup = settings.BackupFrequency.ToLowerInvariant() switch
            {
                "daily" => diff.TotalDays >= 1,
                "weekly" => diff.TotalDays >= 7,
                "monthly" => diff.TotalDays >= 30,
                _ => false
            };
        }

        if (shouldBackup)
        {
            await CreateBackupAsync();
            PruneOldBackups(settings.MaxBackupCount);
        }
    }

    private void PruneOldBackups(int maxCount)
    {
        try
        {
            var backups = GetExistingBackups();
            if (backups.Count > maxCount)
            {
                var toDelete = backups.Skip(maxCount);
                foreach (var file in toDelete)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch
        {
            // Ignore pruning errors
        }
    }

    public async Task<string> ExportJsonAsync(string targetFilePath)
    {
        var allSoftware = await _softwareRepository.GetAllAsync();
        var allCategories = await _categoryRepository.GetAllAsync();
        var allRoots = await _rootRepository.GetAllAsync();

        var export = new ExportData
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Categories = allCategories.Select(c => new ExportCategoryItem
            {
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                SortOrder = c.SortOrder
            }).ToList(),
            Software = allSoftware.Select(s => new ExportSoftwareItem
            {
                Name = s.Name,
                Category = s.CategoryName,
                ExePath = s.ExePath,
                RelativePath = s.RelativePath,
                Arguments = s.Arguments,
                WorkingDirectory = s.WorkingDirectory,
                Description = s.Description,
                Tags = s.Tags,
                IsFavorite = s.IsFavorite,
                RunAsAdmin = s.RunAsAdmin,
                SingleInstance = s.SingleInstance
            }).ToList(),
            Roots = allRoots.Select(r => new ExportRootItem
            {
                Name = r.Name,
                Path = r.Path
            }).ToList()
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(targetFilePath, json);
        return targetFilePath;
    }

    public async Task<int> ImportJsonAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            return 0;

        var json = await File.ReadAllTextAsync(sourceFilePath);
        var export = JsonSerializer.Deserialize<ExportData>(json);
        if (export == null)
            return 0;

        var existingCategories = await _categoryRepository.GetAllAsync();
        var categoryMap = existingCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        // Add missing categories
        foreach (var cat in export.Categories)
        {
            if (!categoryMap.ContainsKey(cat.Name))
            {
                var newCat = new Category
                {
                    Name = cat.Name,
                    Icon = cat.Icon,
                    Color = cat.Color,
                    SortOrder = cat.SortOrder
                };
                var id = await _categoryRepository.AddAsync(newCat);
                categoryMap[cat.Name] = id;
            }
        }

        var defaultCategoryId = categoryMap.Values.FirstOrDefault();
        var count = 0;

        foreach (var s in export.Software)
        {
            var catId = defaultCategoryId;
            if (!string.IsNullOrEmpty(s.Category) && categoryMap.TryGetValue(s.Category, out var foundId))
            {
                catId = foundId;
            }

            var software = new Software
            {
                Name = s.Name,
                ExePath = s.ExePath,
                RelativePath = s.RelativePath,
                Arguments = s.Arguments,
                WorkingDirectory = s.WorkingDirectory,
                Description = s.Description,
                Tags = s.Tags,
                IsFavorite = s.IsFavorite,
                RunAsAdmin = s.RunAsAdmin,
                SingleInstance = s.SingleInstance,
                CategoryId = catId
            };

            await _softwareRepository.AddAsync(software);
            count++;
        }

        return count;
    }
}
