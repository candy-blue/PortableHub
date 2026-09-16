using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;
using PortableHub.Infrastructure.Repositories;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class Milestone1StressTests
{
    #region 1. SQLite WAL Residue Cleanup & Missing Directory Restoration

    [Fact]
    public async Task RestoreBackup_CleansUpWalAndShmResidue_AndPreventsStaleReplay()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var catId = (await env.CategoryRepository.GetAllAsync())[0].Id;
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "InitialApp_V1",
            ExePath = @"D:\Tools\InitialApp.exe",
            CategoryId = catId
        });

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        var backupPath = await backupService.CreateBackupAsync();
        Assert.True(File.Exists(backupPath));

        var dbPath = env.SettingsService.GetDatabasePath();
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";

        // Inject WAL mode and dirty un-backed-up transactions into the current database
        using (var conn = env.ConnectionFactory.CreateConnection())
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode=WAL;
                INSERT INTO Software (Name, ExePath, CategoryId, CreatedAt, UpdatedAt)
                VALUES ('DirtyApp_Uncommitted', 'D:\Tools\Dirty.exe', " + catId + @", datetime('now'), datetime('now'));
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Ensure WAL residue actually exists on disk
        if (!File.Exists(walPath))
        {
            await File.WriteAllBytesAsync(walPath, new byte[1024]);
        }
        if (!File.Exists(shmPath))
        {
            await File.WriteAllBytesAsync(shmPath, new byte[512]);
        }

        Assert.True(File.Exists(walPath), "WAL file should exist prior to restore");
        Assert.True(File.Exists(shmPath), "SHM file should exist prior to restore");

        // Perform restore
        var restored = await backupService.RestoreBackupAsync(backupPath);
        Assert.True(restored, "RestoreBackupAsync should succeed");

        // Open restored database and verify integrity
        using (var conn = env.ConnectionFactory.CreateConnection())
        {
            await conn.OpenAsync();
            using var integrityCmd = conn.CreateCommand();
            integrityCmd.CommandText = "PRAGMA integrity_check;";
            var checkResult = (string?)await integrityCmd.ExecuteScalarAsync();
            Assert.Equal("ok", checkResult);

            using var queryCmd = conn.CreateCommand();
            queryCmd.CommandText = "SELECT Name FROM Software;";
            using var reader = await queryCmd.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            Assert.Contains("InitialApp_V1", names);
            Assert.DoesNotContain("DirtyApp_Uncommitted", names);
        }
    }

    [Fact]
    public async Task RestoreBackup_WhenDestinationDirectoriesMissing_RecreatesStructureAndRestores()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var catId = (await env.CategoryRepository.GetAllAsync())[0].Id;
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "MissingDirApp",
            ExePath = @"D:\Tools\MissingDirApp.exe",
            CategoryId = catId
        });

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        var backupPath = await backupService.CreateBackupAsync();

        // Create completely new isolated target directory
        var newTargetBaseDir = Path.Combine(Path.GetTempPath(), "PortableHub_RestoreTarget_" + Guid.NewGuid().ToString("N"));
        try
        {
            var targetSettings = new SettingsService(newTargetBaseDir);
            targetSettings.InitializePortableMode(true);

            // Completely wipe target data and icons directories
            var dataDir = targetSettings.GetDataDirectory();
            var iconsDir = targetSettings.GetIconsDirectory();
            if (Directory.Exists(dataDir)) Directory.Delete(dataDir, true);
            if (Directory.Exists(iconsDir)) Directory.Delete(iconsDir, true);

            Assert.False(Directory.Exists(dataDir));
            Assert.False(Directory.Exists(iconsDir));

            var targetFactory = new DatabaseConnectionFactory(targetSettings);
            var targetSoftwareRepo = new SoftwareRepository(targetFactory);
            var targetCategoryRepo = new CategoryRepository(targetFactory);
            var targetRootRepo = new RootDirectoryRepository(targetFactory);

            var targetBackupService = new BackupService(
                targetSettings,
                targetSoftwareRepo,
                targetCategoryRepo,
                targetRootRepo);

            var result = await targetBackupService.RestoreBackupAsync(backupPath);
            Assert.True(result, "Restore into missing directories should succeed");

            Assert.True(Directory.Exists(dataDir), "Data directory should be recreated");
            Assert.True(File.Exists(targetSettings.GetDatabasePath()), "Database file should be restored");
            Assert.True(File.Exists(Path.Combine(dataDir, "settings.json")), "settings.json should be restored");

            var restoredSoftware = await targetSoftwareRepo.GetAllAsync();
            Assert.Contains(restoredSoftware, s => s.Name == "MissingDirApp");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            try
            {
                if (Directory.Exists(newTargetBaseDir))
                {
                    Directory.Delete(newTargetBaseDir, true);
                }
            }
            catch { }
        }
    }

    [Fact]
    public async Task RestoreBackup_InvalidOrEmptyArchive_ReturnsFalseGracefully()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        // 1. Non-existent file
        var resNonExistent = await backupService.RestoreBackupAsync(@"D:\non_existent_file.phbackup");
        Assert.False(resNonExistent);

        // 2. Empty zip file without db or manifest
        var emptyZipPath = Path.Combine(env.TempDirectory, "empty.phbackup");
        using (var zip = ZipFile.Open(emptyZipPath, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("dummy.txt");
            using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("empty");
        }

        var resEmptyZip = await backupService.RestoreBackupAsync(emptyZipPath);
        Assert.False(resEmptyZip);
    }

    #endregion

    #region 2. JSON Import Edge Cases & Foreign Key Integrity

    [Fact]
    public async Task ImportJson_EmptyCategoriesAndNullSoftware_HandlesGracefullyWithoutError()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        // Case A: Categories is null, Software has items with null Category
        var jsonFileA = Path.Combine(env.TempDirectory, "import_null_cat.json");
        var exportA = new
        {
            Version = "1.0",
            Categories = (object[]?)null,
            Software = new[]
            {
                new { Name = "AppWithNullCat", ExePath = @"C:\Tools\AppA.exe", Category = (string?)null }
            }
        };
        await File.WriteAllTextAsync(jsonFileA, JsonSerializer.Serialize(exportA));

        var countA = await backupService.ImportJsonAsync(jsonFileA);
        Assert.Equal(1, countA);

        var softwareA = (await env.SoftwareRepository.GetAllAsync()).FirstOrDefault(s => s.Name == "AppWithNullCat");
        Assert.NotNull(softwareA);
        Assert.True(softwareA.CategoryId > 0);

        // Verify foreign key points to a valid category in DB
        var categoryA = await env.CategoryRepository.GetByIdAsync(softwareA.CategoryId);
        Assert.NotNull(categoryA);

        // Case B: Software list is null
        var jsonFileB = Path.Combine(env.TempDirectory, "import_null_software.json");
        var exportB = new
        {
            Version = "1.0",
            Categories = new[]
            {
                new { Name = "ExtraCategory", Icon = "Folder24", Color = "#123456", SortOrder = 10 }
            },
            Software = (object[]?)null
        };
        await File.WriteAllTextAsync(jsonFileB, JsonSerializer.Serialize(exportB));

        var countB = await backupService.ImportJsonAsync(jsonFileB);
        Assert.Equal(0, countB);

        var catsB = await env.CategoryRepository.GetAllAsync();
        Assert.Contains(catsB, c => c.Name == "ExtraCategory");
    }

    [Fact]
    public async Task ImportJson_UnmappedCategory_DynamicallyCreatesCategoryAndPreservesForeignKey()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        var jsonFile = Path.Combine(env.TempDirectory, "import_dynamic_cat.json");
        var export = new
        {
            Version = "1.0",
            Categories = Array.Empty<object>(),
            Software = new[]
            {
                new { Name = "AppWithNewCategory", ExePath = @"C:\Tools\NewCatApp.exe", Category = "BrandNewCategory_XYZ" }
            }
        };
        await File.WriteAllTextAsync(jsonFile, JsonSerializer.Serialize(export));

        var count = await backupService.ImportJsonAsync(jsonFile);
        Assert.Equal(1, count);

        var allCats = await env.CategoryRepository.GetAllAsync();
        var dynamicCat = allCats.FirstOrDefault(c => c.Name == "BrandNewCategory_XYZ");
        Assert.NotNull(dynamicCat);

        var importedApp = (await env.SoftwareRepository.GetAllAsync()).FirstOrDefault(s => s.Name == "AppWithNewCategory");
        Assert.NotNull(importedApp);
        Assert.Equal(dynamicCat.Id, importedApp.CategoryId);
    }

    [Fact]
    public async Task ImportJson_RootsPersistence_PersistsRootsAndResolvesRootId()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        var jsonFile = Path.Combine(env.TempDirectory, "import_roots.json");
        var export = new
        {
            Version = "1.0",
            Roots = new[]
            {
                new { Name = "DevToolsRoot", Path = @"D:\DevTools" },
                new { Name = "DevToolsDuplicate", Path = @"D:\DevTools\" }, // Duplicate path
                new { Name = "EmptyPathRoot", Path = "" } // Empty path
            },
            Software = new[]
            {
                new { Name = "VsCode", ExePath = @"D:\DevTools\VSCode\Code.exe", Category = "开发工具" },
                new { Name = "ExternalTool", ExePath = @"E:\Other\Tool.exe", Category = "系统工具" }
            }
        };
        await File.WriteAllTextAsync(jsonFile, JsonSerializer.Serialize(export));

        var count = await backupService.ImportJsonAsync(jsonFile);
        Assert.Equal(2, count);

        var allRoots = await env.RootRepository.GetAllAsync();
        Assert.Contains(allRoots, r => r.Name == "DevToolsRoot" && r.Path == @"D:\DevTools");
        // Ensure no duplicate path was added
        Assert.Single(allRoots.Where(r => r.Path.TrimEnd('\\', '/').Equals(@"D:\DevTools", StringComparison.OrdinalIgnoreCase)));
        // Ensure empty path was skipped
        Assert.DoesNotContain(allRoots, r => string.IsNullOrWhiteSpace(r.Path));

        var vsCode = (await env.SoftwareRepository.GetAllAsync()).FirstOrDefault(s => s.Name == "VsCode");
        Assert.NotNull(vsCode);
        Assert.NotNull(vsCode.RootId);

        var devRoot = allRoots.First(r => r.Name == "DevToolsRoot");
        Assert.Equal(devRoot.Id, vsCode.RootId);

        var externalTool = (await env.SoftwareRepository.GetAllAsync()).FirstOrDefault(s => s.Name == "ExternalTool");
        Assert.NotNull(externalTool);
        Assert.Null(externalTool.RootId);
    }

    [Fact]
    public async Task SqliteForeignKeys_AreStrictlyEnforced_AndDirectViolationsFail()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        // 1. Confirm that raw invalid foreign key throws SqliteException with foreign keys enabled
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            using var conn = env.ConnectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Software (Name, ExePath, CategoryId, CreatedAt, UpdatedAt)
                VALUES ('IllegalSoftware', 'C:\Illegal.exe', 999999, datetime('now'), datetime('now'));
            ";
            await cmd.ExecuteNonQueryAsync();
        });

        // 2. Confirm that ImportJsonAsync always respects FK and never violates constraints
        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        var jsonFile = Path.Combine(env.TempDirectory, "import_fk_stress.json");
        var export = new
        {
            Version = "1.0",
            Categories = new object[]
            {
                new { Name = "ValidCat", Icon = "Apps24", Color = "#000", SortOrder = 1 }
            },
            Software = new[]
            {
                new { Name = "Tool1", ExePath = @"C:\t1.exe", Category = (string?)"ValidCat" },
                new { Name = "Tool2", ExePath = @"C:\t2.exe", Category = (string?)"NonExistentCat1" },
                new { Name = "Tool3", ExePath = @"C:\t3.exe", Category = (string?)"" },
                new { Name = "Tool4", ExePath = @"C:\t4.exe", Category = (string?)null }
            }
        };
        await File.WriteAllTextAsync(jsonFile, JsonSerializer.Serialize(export));

        var count = await backupService.ImportJsonAsync(jsonFile);
        Assert.Equal(4, count);

        // Verify all 4 have valid CategoryId in Category table
        var allSoftware = await env.SoftwareRepository.GetAllAsync();
        var allCats = await env.CategoryRepository.GetAllAsync();
        var validCatIds = allCats.Select(c => c.Id).ToHashSet();

        foreach (var sw in allSoftware)
        {
            Assert.Contains(sw.CategoryId, validCatIds);
        }
    }

    #endregion

    #region 3. SettingsService Atomic File Swap & Concurrent Save Stress

    [Fact]
    public async Task SettingsService_ConcurrentSaves_SurvivesHighContentionWithoutCorruption()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var service = env.SettingsService;
        const int concurrentWorkers = 50;
        var tasks = new Task[concurrentWorkers];

        for (int i = 0; i < concurrentWorkers; i++)
        {
            int index = i;
            tasks[i] = Task.Run(async () =>
            {
                // Alternating async and sync saves
                if (index % 2 == 0)
                {
                    service.CurrentSettings.WindowWidth = 800 + index;
                    service.CurrentSettings.Theme = index % 4 == 0 ? "Dark" : "Light";
                    await service.SaveSettingsAsync();
                }
                else
                {
                    service.CurrentSettings.WindowHeight = 600 + index;
                    service.CurrentSettings.Language = index % 3 == 0 ? "zh-TW" : "zh-CN";
                    service.SaveSettings();
                }
            });
        }

        // Must complete without throwing any IOException or sharing violation
        await Task.WhenAll(tasks);

        // Verify settings file exists, is non-empty, and parses properly
        var settingsPath = Path.Combine(env.SettingsService.GetDataDirectory(), "settings.json");
        Assert.True(File.Exists(settingsPath));
        var content = await File.ReadAllTextAsync(settingsPath);
        Assert.False(string.IsNullOrWhiteSpace(content));

        var parsed = JsonSerializer.Deserialize<AppSettings>(content);
        Assert.NotNull(parsed);

        // Reload fresh service from disk
        var reloadService = new SettingsService(env.TempDirectory);
        reloadService.InitializePortableMode(true);
        await reloadService.LoadSettingsAsync();
        Assert.NotNull(reloadService.CurrentSettings);
    }

    [Fact]
    public async Task SettingsService_CorruptedPrimaryFile_RecoversFromBak()
    {
        using var env = new TestEnvironment();
        var settingsService = env.SettingsService;

        settingsService.CurrentSettings.Theme = "Dark";
        settingsService.CurrentSettings.GlobalHotkey = "Ctrl+Shift+P";
        await settingsService.SaveSettingsAsync();

        // Second save ensures .bak exists
        settingsService.CurrentSettings.WindowWidth = 1200;
        await settingsService.SaveSettingsAsync();

        var dataDir = settingsService.GetDataDirectory();
        var primaryFile = Path.Combine(dataDir, "settings.json");
        var backupFile = Path.Combine(dataDir, "settings.json.bak");

        Assert.True(File.Exists(primaryFile));
        Assert.True(File.Exists(backupFile));

        // Corrupt primary file with garbage truncated bytes
        await File.WriteAllTextAsync(primaryFile, "{\"Theme\":\"Corrupted_Incomplete_Jso");

        // Reload using a new instance
        var recoveryService = new SettingsService(env.TempDirectory);
        recoveryService.InitializePortableMode(true);
        await recoveryService.LoadSettingsAsync();

        // Must recover valid state from .bak
        Assert.NotNull(recoveryService.CurrentSettings);
        Assert.True(recoveryService.CurrentSettings.WindowWidth >= 800);
        Assert.False(string.IsNullOrWhiteSpace(recoveryService.CurrentSettings.Theme));
    }

    [Fact]
    public async Task SettingsService_ZeroBytePrimaryFile_RecoversFromBak()
    {
        using var env = new TestEnvironment();
        var settingsService = env.SettingsService;

        settingsService.CurrentSettings.Theme = "Dark";
        await settingsService.SaveSettingsAsync();
        // Second save ensures .bak exists
        await settingsService.SaveSettingsAsync();

        var dataDir = settingsService.GetDataDirectory();
        var primaryFile = Path.Combine(dataDir, "settings.json");
        var backupFile = Path.Combine(dataDir, "settings.json.bak");

        Assert.True(File.Exists(primaryFile));
        Assert.True(File.Exists(backupFile));

        // Overwrite primary file with 0 bytes
        await File.WriteAllBytesAsync(primaryFile, Array.Empty<byte>());

        var recoveryService = new SettingsService(env.TempDirectory);
        recoveryService.InitializePortableMode(true);
        await recoveryService.LoadSettingsAsync();

        Assert.NotNull(recoveryService.CurrentSettings);
        Assert.Equal("Dark", recoveryService.CurrentSettings.Theme);
    }

    [Fact]
    public void SettingsService_EnsureDirectories_CleansUpDanglingTmpFiles()
    {
        var tempBase = Path.Combine(Path.GetTempPath(), "PortableHub_DanglingTmp_" + Guid.NewGuid().ToString("N"));
        try
        {
            var dataDir = Path.Combine(tempBase, "data");
            Directory.CreateDirectory(dataDir);

            // Create fake dangling tmp files
            var tmp1 = Path.Combine(dataDir, "settings_12345.tmp");
            var tmp2 = Path.Combine(dataDir, "settings_abcdef.tmp");
            File.WriteAllText(tmp1, "dangling");
            File.WriteAllText(tmp2, "dangling");

            Assert.True(File.Exists(tmp1));
            Assert.True(File.Exists(tmp2));

            // Instantiating SettingsService should execute EnsureDirectories and prune dangling tmp files
            var svc = new SettingsService(tempBase);
            svc.InitializePortableMode(true);

            Assert.False(File.Exists(tmp1));
            Assert.False(File.Exists(tmp2));
        }
        finally
        {
            try { Directory.Delete(tempBase, true); } catch { }
        }
    }

    #endregion

    #region 4. FileScannerService Relative Paths & Bin/Lib Tool Detection

    [Theory]
    [InlineData(@"D:\PortableApps\Git\bin\git.exe", false)]
    [InlineData(@"D:\PortableApps\Git\cmd\git.exe", false)]
    [InlineData(@"D:\PortableApps\MyTool\lib\engine.exe", false)]
    [InlineData(@"D:\PortableApps\DevApp\bin\dev.exe", false)]
    [InlineData(@"D:\PortableApps\Tool\runtimes\win-x64\native\app.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\node_modules\bin\app.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\.git\hooks\hook.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\resources\helper.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\uninstall.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\python.exe", true)]
    [InlineData(@"D:\PortableApps\Tool\conhost.exe", true)]
    public void FileScannerService_IsExcludedExe_AllowsBinAndLibWhileExcludingAuxiliary(string path, bool expectedExcluded)
    {
        var scanner = new FileScannerService();
        var isExcluded = scanner.IsExcludedExe(path);
        Assert.Equal(expectedExcluded, isExcluded);
    }

    [Fact]
    public async Task FileScannerService_ScanDirectory_FindsToolsInBinAndLib_AndPopulatesRelativePath()
    {
        var scanRoot = Path.Combine(Path.GetTempPath(), "PortableHub_ScanStress_" + Guid.NewGuid().ToString("N"));
        try
        {
            // Create valid tools in bin and lib folders
            var gitBin = Path.Combine(scanRoot, "Git", "bin", "git.exe");
            var gitCmd = Path.Combine(scanRoot, "Git", "cmd", "git.exe");
            var toolLib = Path.Combine(scanRoot, "CustomTool", "lib", "tool.exe");
            var devBin = Path.Combine(scanRoot, "DevApp", "bin", "dev.exe");

            // Create excluded files
            var runtimesExe = Path.Combine(scanRoot, "App", "runtimes", "win-x64", "native.exe");
            var nodeModulesExe = Path.Combine(scanRoot, "App", "node_modules", "cli.exe");
            var gitHooksExe = Path.Combine(scanRoot, "App", ".git", "hooks", "hook.exe");
            var resourcesExe = Path.Combine(scanRoot, "App", "resources", "res.exe");
            var uninstallerExe = Path.Combine(scanRoot, "App", "uninstall.exe");
            var pythonExe = Path.Combine(scanRoot, "App", "python.exe");

            var allFiles = new[]
            {
                gitBin, gitCmd, toolLib, devBin,
                runtimesExe, nodeModulesExe, gitHooksExe, resourcesExe, uninstallerExe, pythonExe
            };

            foreach (var f in allFiles)
            {
                var dir = Path.GetDirectoryName(f)!;
                Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(f, new byte[16]);
            }

            var categories = new List<Category>
            {
                new() { Id = 1, Name = "开发工具" },
                new() { Id = 2, Name = "系统工具" },
                new() { Id = 3, Name = "其他" }
            };

            var scanner = new FileScannerService();
            var candidates = await scanner.ScanDirectoryAsync(scanRoot, categories, rootId: 42);

            // Assert candidates count: 4 valid executables
            Assert.Equal(4, candidates.Count);

            var gitBinCandidate = candidates.FirstOrDefault(c => c.ExePath == gitBin);
            Assert.NotNull(gitBinCandidate);
            Assert.Equal(Path.Combine("Git", "bin", "git.exe"), gitBinCandidate.RelativePath);
            Assert.Equal(42, gitBinCandidate.RootId);

            var gitCmdCandidate = candidates.FirstOrDefault(c => c.ExePath == gitCmd);
            Assert.NotNull(gitCmdCandidate);
            Assert.Equal(Path.Combine("Git", "cmd", "git.exe"), gitCmdCandidate.RelativePath);

            var toolLibCandidate = candidates.FirstOrDefault(c => c.ExePath == toolLib);
            Assert.NotNull(toolLibCandidate);
            Assert.Equal(Path.Combine("CustomTool", "lib", "tool.exe"), toolLibCandidate.RelativePath);

            var devBinCandidate = candidates.FirstOrDefault(c => c.ExePath == devBin);
            Assert.NotNull(devBinCandidate);
            Assert.Equal(Path.Combine("DevApp", "bin", "dev.exe"), devBinCandidate.RelativePath);

            // Assert excluded are absent
            Assert.DoesNotContain(candidates, c => c.ExePath == runtimesExe);
            Assert.DoesNotContain(candidates, c => c.ExePath == nodeModulesExe);
            Assert.DoesNotContain(candidates, c => c.ExePath == gitHooksExe);
            Assert.DoesNotContain(candidates, c => c.ExePath == resourcesExe);
            Assert.DoesNotContain(candidates, c => c.ExePath == uninstallerExe);
            Assert.DoesNotContain(candidates, c => c.ExePath == pythonExe);
        }
        finally
        {
            try { Directory.Delete(scanRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task FileScannerService_ScanDirectory_CancellationStopsExecution()
    {
        var scanRoot = Path.Combine(Path.GetTempPath(), "PortableHub_ScanCancel_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scanRoot);
        try
        {
            for (int i = 0; i < 20; i++)
            {
                var f = Path.Combine(scanRoot, $"tool_{i}.exe");
                await File.WriteAllBytesAsync(f, new byte[10]);
            }

            var scanner = new FileScannerService();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancelled

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await scanner.ScanDirectoryAsync(scanRoot, [], null, null, cts.Token);
            });
        }
        finally
        {
            try { Directory.Delete(scanRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task ImportJson_MalformedJson_ThrowsJsonException_AndEmptyExePathIsSkipped()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(
            env.SettingsService,
            env.SoftwareRepository,
            env.CategoryRepository,
            env.RootRepository);

        // Sub-case A: Malformed truncated JSON
        var badJsonPath = Path.Combine(env.TempDirectory, "bad.json");
        await File.WriteAllTextAsync(badJsonPath, "{ \"Software\": [ { \"Name\": ");

        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            await backupService.ImportJsonAsync(badJsonPath);
        });

        // Sub-case B: Software item with empty Name should fallback to ExePath filename, and empty ExePath should be skipped
        var testJsonPath = Path.Combine(env.TempDirectory, "empty_fields.json");
        var export = new
        {
            Version = "1.0",
            Software = new[]
            {
                new { Name = "", ExePath = @"D:\Tools\AutoNamedApp.exe", Category = (string?)null },
                new { Name = "NoExeApp", ExePath = "", Category = (string?)null },
                new { Name = "WhitespaceExeApp", ExePath = "   ", Category = (string?)null }
            }
        };
        await File.WriteAllTextAsync(testJsonPath, JsonSerializer.Serialize(export));

        var count = await backupService.ImportJsonAsync(testJsonPath);
        Assert.Equal(1, count); // Only AutoNamedApp is imported

        var all = await env.SoftwareRepository.GetAllAsync();
        var autoNamed = all.FirstOrDefault(s => s.ExePath == @"D:\Tools\AutoNamedApp.exe");
        Assert.NotNull(autoNamed);
        Assert.Equal("AutoNamedApp", autoNamed.Name);
    }

    [Fact]
    public async Task SettingsService_LoadSettings_WhenNoFilesExist_InitializesDefaultsAndSavesToDisk()
    {
        var tempBase = Path.Combine(Path.GetTempPath(), "PortableHub_FreshSettings_" + Guid.NewGuid().ToString("N"));
        try
        {
            var svc = new SettingsService(tempBase);
            svc.InitializePortableMode(true);

            var settingsPath = Path.Combine(svc.GetDataDirectory(), "settings.json");
            Assert.False(File.Exists(settingsPath));

            await svc.LoadSettingsAsync();

            Assert.NotNull(svc.CurrentSettings);
            Assert.True(File.Exists(settingsPath), "settings.json should be generated and saved on fresh start");
            Assert.Equal("Ctrl+Alt+Space", svc.CurrentSettings.GlobalHotkey);
        }
        finally
        {
            try { Directory.Delete(tempBase, true); } catch { }
        }
    }

    #endregion
}
