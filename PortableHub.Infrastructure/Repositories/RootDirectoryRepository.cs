using Dapper;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;

namespace PortableHub.Infrastructure.Repositories;

public class RootDirectoryRepository : IRootDirectoryRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public RootDirectoryRepository(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<RootDirectory>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var list = await conn.QueryAsync<RootDirectory>(@"
            SELECT Id, Name, Path, IsAvailable, CreatedAt 
            FROM RootDirectory 
            ORDER BY Id ASC;
        ");
        return list.AsList();
    }

    public async Task<RootDirectory?> GetByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<RootDirectory>(@"
            SELECT Id, Name, Path, IsAvailable, CreatedAt 
            FROM RootDirectory 
            WHERE Id = @Id;
        ", new { Id = id });
    }

    public async Task<int> AddAsync(RootDirectory root)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        root.CreatedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO RootDirectory (Name, Path, IsAvailable, CreatedAt)
            VALUES (@Name, @Path, @IsAvailable, @CreatedAt);
            SELECT last_insert_rowid();
        ";

        var id = await conn.ExecuteScalarAsync<int>(sql, root);
        root.Id = id;
        return id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync("DELETE FROM RootDirectory WHERE Id = @Id;", new { Id = id });
    }

    public async Task UpdateAsync(RootDirectory root)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            UPDATE RootDirectory 
            SET Name = @Name, Path = @Path, IsAvailable = @IsAvailable 
            WHERE Id = @Id;
        ", root);
    }
}
