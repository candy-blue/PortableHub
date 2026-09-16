using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;
using PortableHub.Infrastructure.Services;
using PortableHub.Infrastructure.Windows;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PortableHub.Tests;

public class AdversarialMilestone1Tests
{
    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            throw new InvalidOperationException("WScript.Shell COM class is not registered on this system.");

        object? shell = Activator.CreateInstance(shellType);
        if (shell == null)
            throw new InvalidOperationException("Failed to instantiate WScript.Shell.");

        object? shortcut = null;
        try
        {
            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(shortcutPath);
            dynamic dynamicShortcut = shortcut!;
            dynamicShortcut.TargetPath = targetPath;
            dynamicShortcut.Save();
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
            {
                try { Marshal.FinalReleaseComObject(shortcut); } catch { }
            }
            if (shell != null && Marshal.IsComObject(shell))
            {
                try { Marshal.FinalReleaseComObject(shell); } catch { }
            }
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    #region 1. Shortcut Helper Adversarial & Cyclic Tests

    [Fact]
    public void Shortcut_SelfReferencingSymlink_TerminatesWithoutStackOverflow_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_Self_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var shortcutA = Path.Combine(tempDir, "SelfRef.lnk");
            // Create self-referencing symbolic link
            CreateSymbolicLink(shortcutA, shortcutA, 0x2);

            // Must terminate cleanly without StackOverflowException and return null
            var resolved = ShortcutHelper.NormalizeDroppedPath(shortcutA);
            Assert.Null(resolved);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Shortcut_MutualCycleSymlink_TerminatesWithoutStackOverflow_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_Mutual_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var shortcutA = Path.Combine(tempDir, "LinkA.lnk");
            var shortcutB = Path.Combine(tempDir, "LinkB.lnk");

            CreateSymbolicLink(shortcutA, shortcutB, 0x2);
            CreateSymbolicLink(shortcutB, shortcutA, 0x2);

            var resolvedA = ShortcutHelper.NormalizeDroppedPath(shortcutA);
            Assert.Null(resolvedA);

            var resolvedB = ShortcutHelper.NormalizeDroppedPath(shortcutB);
            Assert.Null(resolvedB);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Shortcut_JunctionLoop_TerminatesWithoutStackOverflow_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_Junc_" + Guid.NewGuid().ToString("N"));
        var dirA = Path.Combine(tempDir, "DirA");
        var dirB = Path.Combine(tempDir, "DirB");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);

        try
        {
            var loopAtoB = Path.Combine(dirA, "LoopB");
            var loopBtoA = Path.Combine(dirB, "LoopA");

            // Create directory junction loop
            CreateSymbolicLink(loopAtoB, dirB, 0x3);
            CreateSymbolicLink(loopBtoA, dirA, 0x3);

            var resolved = ShortcutHelper.NormalizeDroppedPath(dirA);
            // Must terminate safely and not throw StackOverflowException
            Assert.Null(resolved);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Shortcut_ValidShortcutToExe_ResolvesSuccessfully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_Valid_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var targetExe = Path.Combine(tempDir, "TargetApp.exe");
            File.WriteAllBytes(targetExe, new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

            var shortcut = Path.Combine(tempDir, "AppShortcut.lnk");
            CreateShortcut(shortcut, targetExe);

            var resolved = ShortcutHelper.NormalizeDroppedPath(shortcut);
            Assert.NotNull(resolved);
            Assert.Equal(targetExe, resolved);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Shortcut_ValidShortcutToDirectory_ResolvesPrimaryExe()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_DirLnk_" + Guid.NewGuid().ToString("N"));
        var appDir = Path.Combine(tempDir, "MyCoolApp");
        Directory.CreateDirectory(appDir);
        try
        {
            var primaryExe = Path.Combine(appDir, "MyCoolApp.exe");
            File.WriteAllBytes(primaryExe, new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

            var shortcut = Path.Combine(tempDir, "DirShortcut.lnk");
            CreateShortcut(shortcut, appDir);

            var resolved = ShortcutHelper.NormalizeDroppedPath(shortcut);
            Assert.NotNull(resolved);
            Assert.Equal(primaryExe, resolved);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Shortcut_NonExistentTarget_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvShortcut_Broken_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var shortcut = Path.Combine(tempDir, "Broken.lnk");
            var nonExistentExe = Path.Combine(tempDir, "Does_Not_Exist_" + Guid.NewGuid().ToString("N") + ".exe");
            CreateShortcut(shortcut, nonExistentExe);

            var result = ShortcutHelper.NormalizeDroppedPath(shortcut);
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region 2. LaunchService Process Handle Leak Tests

    [Fact]
    public void LaunchService_IsRunning_RepeatedCalls_DoesNotLeakHandles()
    {
        var launchService = new LaunchService(null!);
        var currentProc = Process.GetCurrentProcess();
        var currentExe = currentProc.MainModule?.FileName ?? "dotnet.exe";
        var software = new Software { ExePath = currentExe };

        // Warm up and JIT
        for (int i = 0; i < 5; i++)
        {
            _ = launchService.IsRunning(software);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        currentProc.Refresh();
        var initialHandles = currentProc.HandleCount;

        // Perform 100 repeated calls
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            var running = launchService.IsRunning(software);
            Assert.True(running);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        currentProc.Refresh();
        var finalHandles = currentProc.HandleCount;

        var delta = finalHandles - initialHandles;
        Assert.True(delta <= 5, $"Process handles leaked! Initial: {initialHandles}, Final: {finalHandles}, Delta: {delta}");
    }

    [Fact]
    public void LaunchService_ActivateExistingWindow_RepeatedCalls_DoesNotLeakHandles()
    {
        var launchService = new LaunchService(null!);
        var currentProc = Process.GetCurrentProcess();
        var currentExe = currentProc.MainModule?.FileName ?? "dotnet.exe";
        var software = new Software { ExePath = currentExe };

        for (int i = 0; i < 5; i++)
        {
            _ = launchService.ActivateExistingWindow(software);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        currentProc.Refresh();
        var initialHandles = currentProc.HandleCount;

        const int iterations = 50;
        for (int i = 0; i < iterations; i++)
        {
            _ = launchService.ActivateExistingWindow(software);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        currentProc.Refresh();
        var finalHandles = currentProc.HandleCount;

        var delta = finalHandles - initialHandles;
        Assert.True(delta <= 5, $"ActivateExistingWindow leaked handles! Initial: {initialHandles}, Final: {finalHandles}, Delta: {delta}");
    }

    #endregion

    #region 3. Drive Root Recursion & Bound Enforcement Tests

    [Theory]
    [InlineData(@"C:\", true)]
    [InlineData(@"C:", true)]
    [InlineData(@"D:\", true)]
    [InlineData(@"D:", true)]
    [InlineData(@"d:\", true)]
    [InlineData(@"c:/", true)]
    [InlineData(@"\\server\share\", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData(@"C:\Windows", false)]
    [InlineData(@"C:\Program Files", false)]
    [InlineData(@"D:\MyFolder\App", false)]
    [InlineData(@"", false)]
    [InlineData(null, false)]
    public void ShortcutHelper_IsDriveRoot_ExhaustiveVerification(string? path, bool expected)
    {
        var result = ShortcutHelper.IsDriveRoot(path);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShortcutHelper_FindPrimaryExeInDirectory_DriveRoots_CompletesImmediatelyWithoutRecursion()
    {
        var sw = Stopwatch.StartNew();
        _ = ShortcutHelper.FindPrimaryExeInDirectory(@"C:\");
        sw.Stop();

        // A full recursion on C:\ would take > 10 seconds.
        // Guarded enumeration takes < 500ms.
        Assert.True(sw.ElapsedMilliseconds < 1000, $"FindPrimaryExeInDirectory on C:\\ took {sw.ElapsedMilliseconds} ms (must not recurse full volume).");

        if (Directory.Exists(@"D:\"))
        {
            sw.Restart();
            _ = ShortcutHelper.FindPrimaryExeInDirectory(@"D:\");
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 1000, $"FindPrimaryExeInDirectory on D:\\ took {sw.ElapsedMilliseconds} ms.");
        }
    }

    [Fact]
    public async Task PathRepairService_TryAutoRepairPath_DriveRoots_CompletesImmediatelyWithoutFullDiskScan()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var repairService = new PathRepairService(env.SoftwareRepository);

        var ghostSoftware = new Software
        {
            Id = 999,
            Name = "GhostApp",
            ExePath = @"D:\GhostDir\GhostApp_12345.exe",
            RelativePath = null
        };

        var driveRoots = new List<RootDirectory>
        {
            new() { Id = 1, Name = "C Drive", Path = @"C:\" },
            new() { Id = 2, Name = "D Drive", Path = @"D:\" }
        };

        var sw = Stopwatch.StartNew();
        var result = await repairService.TryAutoRepairPathAsync(ghostSoftware, driveRoots);
        sw.Stop();

        Assert.Null(result);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"TryAutoRepairPathAsync took {sw.ElapsedMilliseconds} ms on drive roots (must not scan full disk volumes).");
    }

    [Fact]
    public async Task PathRepairService_TryAutoRepairPath_Subdirectory_RecursesUpToDepth3AndFindsRelocatedExe()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var tempDir = Path.Combine(Path.GetTempPath(), "PH_RepairSubdir_" + Guid.NewGuid().ToString("N"));
        var deepSubdir = Path.Combine(tempDir, "Folder1", "Folder2", "Bin");
        Directory.CreateDirectory(deepSubdir);

        try
        {
            var targetExe = Path.Combine(deepSubdir, "TargetTool.exe");
            File.WriteAllBytes(targetExe, new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

            var root = new RootDirectory { Id = 1, Name = "TempRoot", Path = tempDir };
            await env.RootRepository.AddAsync(root);

            var categories = await env.CategoryRepository.GetAllAsync();
            var swRecord = new Software
            {
                Name = "TargetTool",
                ExePath = @"E:\OldLocation\TargetTool.exe", // Broken path
                CategoryId = categories[0].Id,
                RelativePath = null
            };
            var swId = await env.SoftwareRepository.AddAsync(swRecord);
            swRecord.Id = swId;

            var repairService = new PathRepairService(env.SoftwareRepository);
            var repaired = await repairService.TryAutoRepairPathAsync(swRecord, new[] { root });

            Assert.NotNull(repaired);
            Assert.Equal(targetExe, repaired);

            var updated = await env.SoftwareRepository.GetByIdAsync(swId);
            Assert.NotNull(updated);
            Assert.Equal(targetExe, updated.ExePath);
            Assert.Equal(Path.Combine("Folder1", "Folder2", "Bin", "TargetTool.exe"), updated.RelativePath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region 4. Settings Concurrency & Atomic Persistence Tests

    [Fact]
    public async Task SettingsService_ConcurrentSavesAndLoads_RemainConsistentWithoutCorruption()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_AdvSettings_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new SettingsService(tempDir);
            service.InitializePortableMode(true);

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                var iteration = i;
                if (iteration % 2 == 0)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        service.CurrentSettings.CardSize = $"Size_{iteration}";
                        await service.SaveSettingsAsync();
                    }));
                }
                else
                {
                    tasks.Add(Task.Run(() =>
                    {
                        service.CurrentSettings.CardSize = $"SyncSize_{iteration}";
                        service.SaveSettings();
                    }));
                }
            }

            await Task.WhenAll(tasks);

            var settingsFile = Path.Combine(service.GetDataDirectory(), "settings.json");
            Assert.True(File.Exists(settingsFile));

            var json = await File.ReadAllTextAsync(settingsFile);
            var parsed = JsonSerializer.Deserialize<AppSettings>(json);
            Assert.NotNull(parsed);
            Assert.NotNull(parsed.CardSize);

            var tmpFiles = Directory.GetFiles(service.GetDataDirectory(), "*.tmp");
            Assert.Empty(tmpFiles);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region 5. Backup & Restore WAL Cleanup and Foreign Key Integrity Tests

    [Fact]
    public async Task BackupService_RestoreBackup_RemovesStaleWalAndShmFiles()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "InitialBackupApp",
            ExePath = @"C:\Init.exe",
            CategoryId = categories[0].Id
        });

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);
        var backupFile = await backupService.CreateBackupAsync();

        var dbPath = env.SettingsService.GetDatabasePath();
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";
        await File.WriteAllTextAsync(walPath, "Stale WAL Content That Must Be Removed");
        await File.WriteAllTextAsync(shmPath, "Stale SHM Content That Must Be Removed");

        Assert.True(File.Exists(walPath));
        Assert.True(File.Exists(shmPath));

        var success = await backupService.RestoreBackupAsync(backupFile);
        Assert.True(success);

        Assert.False(File.Exists(walPath), "Stale .db-wal was not removed during restore!");
        Assert.False(File.Exists(shmPath), "Stale .db-shm was not removed during restore!");

        var softwareList = await env.SoftwareRepository.GetAllAsync();
        Assert.Contains(softwareList, s => s.Name == "InitialBackupApp");
    }

    [Fact]
    public async Task BackupService_ImportJson_HandlesMissingCategoryAndEmptyRoots_WithoutFKViolation()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);

        var tempDir = Path.Combine(Path.GetTempPath(), "PH_JsonImportTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var jsonPath = Path.Combine(tempDir, "export.json");
            var jsonContent = """
{
    "ExportTime": "2026-09-06T00:00:00Z",
    "Version": "1.0.0",
    "Roots": [
        { "Name": "ImportedRoot", "Path": "C:\\Tools" }
    ],
    "Categories": [],
    "Software": [
        {
            "Name": "NoCategoryApp",
            "ExePath": "C:\\Tools\\App1\\tool.exe",
            "Category": null
        },
        {
            "Name": "UnmappedCategoryApp",
            "ExePath": "C:\\Tools\\App2\\tool.exe",
            "Category": "NonExistentCategoryName"
        }
    ]
}
""";
            await File.WriteAllTextAsync(jsonPath, jsonContent);

            var importedCount = await backupService.ImportJsonAsync(jsonPath);
            Assert.Equal(2, importedCount);

            var allSoftware = await env.SoftwareRepository.GetAllAsync();
            Assert.Contains(allSoftware, s => s.Name == "NoCategoryApp");
            Assert.Contains(allSoftware, s => s.Name == "UnmappedCategoryApp");

            var allRoots = await env.RootRepository.GetAllAsync();
            Assert.Contains(allRoots, r => r.Path == @"C:\Tools");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
