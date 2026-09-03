using Dapper;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;

namespace PortableHub.Infrastructure.Repositories;

public class SoftwareRepository : ISoftwareRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public SoftwareRepository(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Software>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT s.Id, s.RootId, s.Name, s.ExePath, s.RelativePath, s.Description, s.Arguments,
                   s.WorkingDirectory, s.IconPath, s.CategoryId, s.IsFavorite, s.LaunchCount,
                   s.LastLaunchedAt, s.SortOrder, s.RunAsAdmin, s.SingleInstance, s.Tags,
                   s.CreatedAt, s.UpdatedAt, COALESCE(c.Name, '') AS CategoryName
            FROM Software s
            LEFT JOIN Category c ON s.CategoryId = c.Id
            ORDER BY s.SortOrder ASC, s.Id ASC;
        ";

        var list = await conn.QueryAsync<Software>(sql);
        return list.AsList();
    }

    public async Task<Software?> GetByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT s.Id, s.RootId, s.Name, s.ExePath, s.RelativePath, s.Description, s.Arguments,
                   s.WorkingDirectory, s.IconPath, s.CategoryId, s.IsFavorite, s.LaunchCount,
                   s.LastLaunchedAt, s.SortOrder, s.RunAsAdmin, s.SingleInstance, s.Tags,
                   s.CreatedAt, s.UpdatedAt, COALESCE(c.Name, '') AS CategoryName
            FROM Software s
            LEFT JOIN Category c ON s.CategoryId = c.Id
            WHERE s.Id = @Id;
        ";

        return await conn.QuerySingleOrDefaultAsync<Software>(sql, new { Id = id });
    }

    public async Task<int> AddAsync(Software software)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        software.CreatedAt = DateTime.UtcNow;
        software.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO Software (
                RootId, Name, ExePath, RelativePath, Description, Arguments,
                WorkingDirectory, IconPath, CategoryId, IsFavorite, LaunchCount,
                LastLaunchedAt, SortOrder, RunAsAdmin, SingleInstance, Tags,
                CreatedAt, UpdatedAt
            ) VALUES (
                @RootId, @Name, @ExePath, @RelativePath, @Description, @Arguments,
                @WorkingDirectory, @IconPath, @CategoryId, @IsFavorite, @LaunchCount,
                @LastLaunchedAt, @SortOrder, @RunAsAdmin, @SingleInstance, @Tags,
                @CreatedAt, @UpdatedAt
            );
            SELECT last_insert_rowid();
        ";

        var id = await conn.ExecuteScalarAsync<int>(sql, software);
        software.Id = id;
        return id;
    }

    public async Task UpdateAsync(Software software)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        software.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            UPDATE Software SET
                RootId = @RootId,
                Name = @Name,
                ExePath = @ExePath,
                RelativePath = @RelativePath,
                Description = @Description,
                Arguments = @Arguments,
                WorkingDirectory = @WorkingDirectory,
                IconPath = @IconPath,
                CategoryId = @CategoryId,
                IsFavorite = @IsFavorite,
                RunAsAdmin = @RunAsAdmin,
                SingleInstance = @SingleInstance,
                Tags = @Tags,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
        ";

        await conn.ExecuteAsync(sql, software);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync("DELETE FROM Software WHERE Id = @Id;", new { Id = id });
    }

    public async Task UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> sortOrders)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        foreach (var item in sortOrders)
        {
            await conn.ExecuteAsync(@"
                UPDATE Software 
                SET SortOrder = @SortOrder, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id;
            ", new { Id = item.Id, SortOrder = item.SortOrder, UpdatedAt = DateTime.UtcNow }, transaction);
        }

        transaction.Commit();
    }

    public async Task UpdateCategoryAsync(int softwareId, int categoryId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            UPDATE Software 
            SET CategoryId = @CategoryId, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id;
        ", new { Id = softwareId, CategoryId = categoryId, UpdatedAt = DateTime.UtcNow });
    }

    public async Task IncrementLaunchCountAsync(int id, DateTime launchedAt)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            UPDATE Software 
            SET LaunchCount = LaunchCount + 1, LastLaunchedAt = @LastLaunchedAt, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id;
        ", new { Id = id, LastLaunchedAt = launchedAt, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdateFavoriteAsync(int id, bool isFavorite)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            UPDATE Software 
            SET IsFavorite = @IsFavorite, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id;
        ", new { Id = id, IsFavorite = isFavorite ? 1 : 0, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdatePathAsync(int id, string newExePath, int? rootId, string? relativePath)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            UPDATE Software 
            SET ExePath = @ExePath, RootId = @RootId, RelativePath = @RelativePath, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id;
        ", new { Id = id, ExePath = newExePath, RootId = rootId, RelativePath = relativePath, UpdatedAt = DateTime.UtcNow });
    }
}
