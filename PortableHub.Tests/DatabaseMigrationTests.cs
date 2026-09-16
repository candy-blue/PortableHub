using Dapper;
using Xunit;

namespace PortableHub.Tests;

public class DatabaseMigrationTests
{
    [Fact]
    public async Task Migration_ShouldCreateTables_And_SeedDefaultCategories()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        using var conn = env.ConnectionFactory.CreateConnection();
        await conn.OpenAsync();

        // 1. Verify schema version
        var version = await conn.ExecuteScalarAsync<int>("SELECT MAX(Version) FROM SchemaVersion;");
        Assert.Equal(3, version);

        // 2. Verify categories created - only one default "其他" category
        var categories = await env.CategoryRepository.GetAllAsync();
        Assert.Single(categories);
        Assert.Equal("其他", categories[0].Name);
        Assert.Equal("Apps24", categories[0].Icon);

        // 3. Verify LinkedSoftwareIds column exists on Software table
        var columnInfo = await conn.QueryAsync("PRAGMA table_info(Software);");
        var columnNames = columnInfo.Select(c => (string)c.name).ToList();
        Assert.Contains("LinkedSoftwareIds", columnNames);

        // 4. Verify WAL mode enabled
        var journalMode = await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode;");
        Assert.Equal("wal", journalMode?.ToLowerInvariant());
    }
}
