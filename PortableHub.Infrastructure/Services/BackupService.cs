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

        var dataDir = _settingsService.GetDataDirectory();
        var iconsDir = _settingsService.GetIconsDirectory();
        var dbPath = _settingsService.GetDatabasePath();
        var dbDir = Path.GetDirectoryName(dbPath);

        // Ensure all target directories exist before extraction
        if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }
        if (!string.IsNullOrEmpty(iconsDir) && !Directory.Exists(iconsDir))
        {
            Directory.CreateDirectory(iconsDir);
        }

        // Clean up any existing SQLite WAL and SHM files to prevent WAL corruption / stale replay
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";
        if (File.Exists(walPath))
        {
            try { File.Delete(walPath); } catch { }
        }
        if (File.Exists(shmPath))
        {
            try { File.Delete(shmPath); } catch { }
        }
        if (File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }

        // 3. Extract contents
        using (var zip = ZipFile.OpenRead(backupFilePath))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.Equals("portablehub.db", StringComparison.OrdinalIgnoreCase))
                {
                    var destDir = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    entry.ExtractToFile(dbPath, overwrite: true);
                }
                else if (entry.FullName.Equals("settings.json", StringComparison.OrdinalIgnoreCase))
                {
                    var dest = Path.Combine(dataDir, "settings.json");
                    var destDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    entry.ExtractToFile(dest, overwrite: true);
                }
                else if (entry.FullName.StartsWith("icons/", StringComparison.OrdinalIgnoreCase) ||
                         entry.FullName.StartsWith("icons\\", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileName(entry.FullName);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var dest = Path.Combine(iconsDir, fileName);
                        var destDir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        entry.ExtractToFile(dest, overwrite: true);
                    }
                }
            }
        }

        // Clear pools again after extraction to invalidate old file handles
        SqliteConnection.ClearAllPools();

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

        // 1. Persist Roots
        var existingRoots = await _rootRepository.GetAllAsync();
        var existingRootPaths = new HashSet<string>(
            existingRoots.Where(r => !string.IsNullOrWhiteSpace(r.Path)).Select(r => r.Path.TrimEnd('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        if (export.Roots != null)
        {
            foreach (var r in export.Roots)
            {
                if (string.IsNullOrWhiteSpace(r.Path))
                    continue;

                var normalizedPath = r.Path.TrimEnd('\\', '/');
                if (!existingRootPaths.Contains(normalizedPath))
                {
                    var rootName = string.IsNullOrWhiteSpace(r.Name)
                        ? (Path.GetFileName(normalizedPath) ?? normalizedPath)
                        : r.Name;

                    var newRoot = new RootDirectory
                    {
                        Name = rootName,
                        Path = r.Path,
                        IsAvailable = Directory.Exists(r.Path),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _rootRepository.AddAsync(newRoot);
                    existingRootPaths.Add(normalizedPath);
                }
            }
        }

        // Reload roots to obtain fresh IDs for software mapping
        var allRoots = await _rootRepository.GetAllAsync();

        // 2. Persist Categories with null-safety
        var existingCategories = await _categoryRepository.GetAllAsync();
        var categoryMap = existingCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        if (export.Categories != null)
        {
            foreach (var cat in export.Categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Name))
                    continue;

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
        }

        // FK Safety: Ensure at least one valid fallback category exists in the database
        if (categoryMap.Count == 0)
        {
            var fallbackCat = new Category
            {
                Name = "其他",
                Icon = "Apps24",
                Color = "#6B7280",
                SortOrder = 1,
                IsSystem = true
            };
            var id = await _categoryRepository.AddAsync(fallbackCat);
            categoryMap[fallbackCat.Name] = id;
        }

        var defaultCategoryId = categoryMap.Values.First();
        var count = 0;

        // 3. Persist Software with null-safety and FK guarantee
        if (export.Software != null)
        {
            foreach (var s in export.Software)
            {
                if (string.IsNullOrWhiteSpace(s.ExePath))
                    continue;

                var catId = defaultCategoryId;
                if (!string.IsNullOrWhiteSpace(s.Category))
                {
                    if (categoryMap.TryGetValue(s.Category, out var foundId))
                    {
                        catId = foundId;
                    }
                    else
                    {
                        // Dynamically create category if specified in software item
                        var newCat = new Category
                        {
                            Name = s.Category,
                            Icon = "Folder24",
                            Color = "#3B82F6",
                            SortOrder = categoryMap.Count + 1
                        };
                        catId = await _categoryRepository.AddAsync(newCat);
                        categoryMap[s.Category] = catId;
                    }
                }

                // Resolve RootId if matching root found
                int? rootId = null;
                var normalizedExe = s.ExePath.TrimEnd('\\', '/');
                foreach (var root in allRoots)
                {
                    if (!string.IsNullOrWhiteSpace(root.Path))
                    {
                        var normRoot = root.Path.TrimEnd('\\', '/');
                        if (normalizedExe.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            rootId = root.Id;
                            break;
                        }
                    }
                }

                var softwareName = string.IsNullOrWhiteSpace(s.Name)
                    ? (Path.GetFileNameWithoutExtension(s.ExePath) ?? "Unnamed")
                    : s.Name;

                var software = new Software
                {
                    RootId = rootId,
                    Name = softwareName,
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
        }

        return count;
    }
}
