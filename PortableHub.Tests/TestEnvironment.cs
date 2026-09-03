using PortableHub.Core.Interfaces;
using PortableHub.Infrastructure.Data;
using PortableHub.Infrastructure.Repositories;
using PortableHub.Infrastructure.Services;

namespace PortableHub.Tests;

public class TestEnvironment : IDisposable
{
    public string TempDirectory { get; }
    public ISettingsService SettingsService { get; }
    public DatabaseConnectionFactory ConnectionFactory { get; }
    public DatabaseMigrator Migrator { get; }
    public ISoftwareRepository SoftwareRepository { get; }
    public ICategoryRepository CategoryRepository { get; }
    public IRootDirectoryRepository RootRepository { get; }

    public TestEnvironment()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), "PortableHub_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDirectory);

        SettingsService = new SettingsService(TempDirectory);
        SettingsService.InitializePortableMode(true);

        ConnectionFactory = new DatabaseConnectionFactory(SettingsService);
        Migrator = new DatabaseMigrator(ConnectionFactory);
        SoftwareRepository = new SoftwareRepository(ConnectionFactory);
        CategoryRepository = new CategoryRepository(ConnectionFactory);
        RootRepository = new RootDirectoryRepository(ConnectionFactory);
    }

    public async Task InitializeAsync()
    {
        await SettingsService.LoadSettingsAsync();
        await Migrator.MigrateAsync();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore temporary cleanup errors
        }
    }
}
