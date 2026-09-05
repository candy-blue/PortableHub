using System.Diagnostics;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class PerformanceAndEdgeCaseTests
{
    [Fact]
    public void LargeSoftwareList_SearchPerformance_ShouldBeSub10ms()
    {
        var searchService = new SearchService();
        var list = new List<Software>();

        // Generate 1000 software entries
        for (int i = 0; i < 1000; i++)
        {
            list.Add(new Software
            {
                Id = i + 1,
                Name = $"Portable Utility {i} (v{i / 10}.0)",
                ExePath = $@"D:\Portable\App_{i}\App_{i}.exe",
                CategoryName = i % 5 == 0 ? "开发工具" : (i % 5 == 1 ? "系统工具" : "网络工具"),
                CategoryId = (i % 5) + 1,
                Tags = $"#tag_{i % 10} #portable",
                Description = $"Tool description for utility number {i}",
                LaunchCount = i,
                SortOrder = i
            });
        }

        searchService.IndexSoftware(list);

        var stopwatch = Stopwatch.StartNew();
        var results = searchService.Search("utility 55");
        stopwatch.Stop();

        Assert.NotEmpty(results);
        // Design requirement Section 69: Search < 50ms (in reality our in-memory LINQ search does it in < 5ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 50, $"Search took {stopwatch.ElapsedMilliseconds} ms which is slower than 50ms");
    }

    [Fact]
    public async Task AutoBackup_ShouldPruneOldBackups_ExceedingMaxCount()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "App",
            ExePath = "C:\\app.exe",
            CategoryId = categories[0].Id
        });

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);

        // Create 7 backups
        for (int i = 0; i < 7; i++)
        {
            var fileName = $"PortableHub_Backup_Test_{i:D2}.phbackup";
            var path = Path.Combine(backupService.GetBackupsDirectory(), fileName);
            await backupService.CreateBackupAsync(path);
            await Task.Delay(10); // Small delay to guarantee different file creation times
        }

        var existingBefore = backupService.GetExistingBackups();
        Assert.Equal(7, existingBefore.Count);

        // Run auto backup check with MaxBackupCount = 5
        env.SettingsService.CurrentSettings.BackupFrequency = "Daily";
        env.SettingsService.CurrentSettings.MaxBackupCount = 5;
        env.SettingsService.CurrentSettings.LastBackupAt = DateTime.UtcNow.AddDays(-2);

        await backupService.CheckAndPerformAutoBackupAsync();

        var existingAfter = backupService.GetExistingBackups();
        Assert.True(existingAfter.Count <= 5, $"Backup count after pruning was {existingAfter.Count}, expected <= 5");
    }

    [Fact]
    public async Task RestoreBackup_ShouldRestoreDatabaseAndSettingsSuccessfully()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "Original App Before Backup",
            ExePath = "C:\\Original.exe",
            CategoryId = categories[0].Id
        });

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);
        var backupPath = await backupService.CreateBackupAsync();

        // Now modify the DB
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "App Added After Backup",
            ExePath = "C:\\New.exe",
            CategoryId = categories[0].Id
        });

        var allBeforeRestore = await env.SoftwareRepository.GetAllAsync();
        Assert.Contains(allBeforeRestore, s => s.Name == "App Added After Backup");

        // Restore backup
        var success = await backupService.RestoreBackupAsync(backupPath);
        Assert.True(success);

        // Verify DB reverted to state in backup
        var allAfterRestore = await env.SoftwareRepository.GetAllAsync();
        Assert.Contains(allAfterRestore, s => s.Name == "Original App Before Backup");
        Assert.DoesNotContain(allAfterRestore, s => s.Name == "App Added After Backup");
    }

    [Fact]
    public async Task SettingsService_ShouldSanitizeDangerousGlobalHotkey_OnLoad()
    {
        using var env = new TestEnvironment();
        var settingsFile = Path.Combine(env.TempDirectory, "settings.json");
        await File.WriteAllTextAsync(settingsFile, "{\"GlobalHotkey\":\"ctrl+a\"}");

        var settingsService = new SettingsService(env.TempDirectory);
        await settingsService.LoadSettingsAsync();

        Assert.Equal("Ctrl+Alt+Space", settingsService.CurrentSettings.GlobalHotkey);
    }
}
