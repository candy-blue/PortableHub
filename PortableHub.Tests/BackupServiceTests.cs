using System.IO.Compression;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task CreateBackup_ShouldProduceValidPhbackupZipArchive()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "SumatraPDF",
            ExePath = @"D:\Tools\SumatraPDF.exe",
            CategoryId = categories[0].Id
        });

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);

        var backupPath = await backupService.CreateBackupAsync();
        Assert.True(File.Exists(backupPath));
        Assert.EndsWith(".phbackup", backupPath);

        using var zip = ZipFile.OpenRead(backupPath);
        Assert.NotNull(zip.GetEntry("manifest.json"));
        Assert.NotNull(zip.GetEntry("portablehub.db"));
        Assert.NotNull(zip.GetEntry("settings.json"));
    }

    [Fact]
    public async Task JsonExportAndImport_ShouldRoundTripData()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var originalId = await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "Roundtrip Test App",
            ExePath = @"D:\Tools\App.exe",
            Arguments = "-arg1",
            CategoryId = categories[0].Id,
            IsFavorite = true
        });

        var backupService = new BackupService(env.SettingsService, env.SoftwareRepository, env.CategoryRepository, env.RootRepository);

        var exportFile = Path.Combine(env.TempDirectory, "export_test.json");
        await backupService.ExportJsonAsync(exportFile);
        Assert.True(File.Exists(exportFile));

        // Delete from repository
        await env.SoftwareRepository.DeleteAsync(originalId);
        var afterDelete = await env.SoftwareRepository.GetByIdAsync(originalId);
        Assert.Null(afterDelete);

        // Import back from JSON
        var importedCount = await backupService.ImportJsonAsync(exportFile);
        Assert.True(importedCount >= 1);

        var all = await env.SoftwareRepository.GetAllAsync();
        var reimported = all.FirstOrDefault(s => s.Name == "Roundtrip Test App");
        Assert.NotNull(reimported);
        Assert.Equal(@"D:\Tools\App.exe", reimported.ExePath);
        Assert.Equal("-arg1", reimported.Arguments);
        Assert.True(reimported.IsFavorite);
    }
}
