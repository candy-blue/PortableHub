using Microsoft.Data.Sqlite;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Data;

public class DatabaseConnectionFactory
{
    private readonly ISettingsService _settingsService;

    public DatabaseConnectionFactory(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public SqliteConnection CreateConnection()
    {
        var dbPath = _settingsService.GetDatabasePath();
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();

        var conn = new SqliteConnection(connectionString);
        return conn;
    }
}
