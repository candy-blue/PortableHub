using Dapper;
using Microsoft.Data.Sqlite;

namespace PortableHub.Infrastructure.Data;

public class DatabaseMigrator
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public DatabaseMigrator(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task MigrateAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        // Enable WAL mode for performance and concurrent readers
        using (var walCmd = conn.CreateCommand())
        {
            walCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            await walCmd.ExecuteNonQueryAsync();
        }

        // Schema Version Table
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS SchemaVersion (
                Version INTEGER PRIMARY KEY,
                AppliedAt TEXT NOT NULL
            );
        ");

        var currentVersion = await conn.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;");

        if (currentVersion < 1)
        {
            await ApplyMigrationV1Async(conn);
        }

        if (currentVersion < 2)
        {
            await ApplyMigrationV2Async(conn);
        }

        if (currentVersion < 3)
        {
            await ApplyMigrationV3Async(conn);
        }
    }

    private async Task ApplyMigrationV1Async(SqliteConnection conn)
    {
        using var transaction = conn.BeginTransaction();

        // 1. RootDirectory Table
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS RootDirectory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL,
                IsAvailable INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );
        ", transaction: transaction);

        // 2. Category Table
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Category (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Icon TEXT,
                Color TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                IsSystem INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
        ", transaction: transaction);

        // 3. Software Table
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Software (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RootId INTEGER,
                Name TEXT NOT NULL,
                ExePath TEXT NOT NULL,
                RelativePath TEXT,
                Description TEXT,
                Arguments TEXT,
                WorkingDirectory TEXT,
                IconPath TEXT,
                CategoryId INTEGER NOT NULL,
                IsFavorite INTEGER NOT NULL DEFAULT 0,
                LaunchCount INTEGER NOT NULL DEFAULT 0,
                LastLaunchedAt TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                RunAsAdmin INTEGER NOT NULL DEFAULT 0,
                SingleInstance INTEGER NOT NULL DEFAULT 1,
                Tags TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Category(Id) ON DELETE RESTRICT,
                FOREIGN KEY (RootId) REFERENCES RootDirectory(Id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS idx_software_name ON Software(Name);
            CREATE INDEX IF NOT EXISTS idx_software_categoryid ON Software(CategoryId);
            CREATE INDEX IF NOT EXISTS idx_software_sortorder ON Software(SortOrder);
            CREATE INDEX IF NOT EXISTS idx_software_lastlaunchedat ON Software(LastLaunchedAt);
            CREATE INDEX IF NOT EXISTS idx_software_isfavorite ON Software(IsFavorite);
        ", transaction: transaction);

        // 4. Default Categories - Only "其他" by default
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Category;", transaction: transaction);
        if (count == 0)
        {
            var now = DateTime.UtcNow.ToString("o");
            var defaultCategories = new[]
            {
                new { Name = "其他", Icon = "Apps24", Color = "#6B7280", SortOrder = 1, IsSystem = 1, CreatedAt = now, UpdatedAt = now }
            };

            await conn.ExecuteAsync(@"
                INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                VALUES (@Name, @Icon, @Color, @SortOrder, @IsSystem, @CreatedAt, @UpdatedAt);
            ", defaultCategories, transaction: transaction);
        }

        // Record schema version
        await conn.ExecuteAsync(@"
            INSERT INTO SchemaVersion (Version, AppliedAt)
            VALUES (1, @AppliedAt);
        ", new { AppliedAt = DateTime.UtcNow.ToString("o") }, transaction: transaction);

        transaction.Commit();
    }

    private async Task ApplyMigrationV2Async(SqliteConnection conn)
    {
        using var transaction = conn.BeginTransaction();

        // 1. Ensure "其他" category exists
        var hasOther = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Category WHERE Name = '其他';", transaction: transaction) > 0;
        if (!hasOther)
        {
            var now = DateTime.UtcNow.ToString("o");
            await conn.ExecuteAsync(@"
                INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                VALUES ('其他', 'Apps24', '#6B7280', 1, 1, @now, @now);
            ", new { now }, transaction: transaction);
        }

        // 2. Clean up empty old default system categories
        await conn.ExecuteAsync(@"
            DELETE FROM Category 
            WHERE IsSystem = 1 
              AND Name IN ('开发工具', '系统工具', '网络工具', '办公工具', '图形图像', '多媒体', '文件管理', '安全工具')
              AND Id NOT IN (SELECT DISTINCT CategoryId FROM Software WHERE CategoryId IS NOT NULL);
        ", transaction: transaction);

        // 3. Record schema version 2
        await conn.ExecuteAsync(@"
            INSERT INTO SchemaVersion (Version, AppliedAt)
            VALUES (2, @AppliedAt);
        ", new { AppliedAt = DateTime.UtcNow.ToString("o") }, transaction: transaction);

        transaction.Commit();
    }

    private async Task ApplyMigrationV3Async(SqliteConnection conn)
    {
        using var transaction = conn.BeginTransaction();

        // 1. Add LinkedSoftwareIds column to Software table if it doesn't exist
        var hasColumn = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pragma_table_info('Software') WHERE name = 'LinkedSoftwareIds';",
            transaction: transaction) > 0;

        if (!hasColumn)
        {
            await conn.ExecuteAsync("ALTER TABLE Software ADD COLUMN LinkedSoftwareIds TEXT;", transaction: transaction);
        }

        // 2. Record schema version 3
        await conn.ExecuteAsync(@"
            INSERT INTO SchemaVersion (Version, AppliedAt)
            VALUES (3, @AppliedAt);
        ", new { AppliedAt = DateTime.UtcNow.ToString("o") }, transaction: transaction);

        transaction.Commit();
    }
}
