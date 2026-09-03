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
        var version = await conn.ExecuteScalarAsync<int>("SELECT Version FROM SchemaVersion;");
        Assert.Equal(1, version);

        // 2. Verify categories created
        var categories = await env.CategoryRepository.GetAllAsync();
        Assert.True(categories.Count >= 9);
        Assert.Contains(categories, c => c.Name == "开发工具");
        Assert.Contains(categories, c => c.Name == "系统工具");
        Assert.Contains(categories, c => c.Name == "其他");

        // 3. Verify WAL mode enabled
        var journalMode = await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode;");
        Assert.Equal("wal", journalMode?.ToLowerInvariant());
    }
}
