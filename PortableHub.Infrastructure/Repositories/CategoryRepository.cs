using Dapper;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;

namespace PortableHub.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public CategoryRepository(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        var result = await conn.QueryAsync<Category>(@"
            SELECT Id, Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt 
            FROM Category 
            ORDER BY SortOrder ASC, Id ASC;
        ");
        return result.AsList();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Category>(@"
            SELECT Id, Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt 
            FROM Category 
            WHERE Id = @Id;
        ", new { Id = id });
    }

    public async Task<int> AddAsync(Category category)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
            VALUES (@Name, @Icon, @Color, @SortOrder, @IsSystem, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        ";

        var id = await conn.ExecuteScalarAsync<int>(sql, category);
        category.Id = id;
        return id;
    }

    public async Task UpdateAsync(Category category)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        category.UpdatedAt = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            UPDATE Category 
            SET Name = @Name, Icon = @Icon, Color = @Color, SortOrder = @SortOrder, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
        ", category);
    }

    public async Task DeleteAsync(int id, int fallbackCategoryId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        // Move all software in this category to fallbackCategoryId
        await conn.ExecuteAsync(@"
            UPDATE Software 
            SET CategoryId = @FallbackCategoryId, UpdatedAt = @UpdatedAt 
            WHERE CategoryId = @Id;
        ", new { Id = id, FallbackCategoryId = fallbackCategoryId, UpdatedAt = DateTime.UtcNow }, transaction);

        await conn.ExecuteAsync(@"
            DELETE FROM Category 
            WHERE Id = @Id;
        ", new { Id = id }, transaction);

        transaction.Commit();
    }

    public async Task UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> sortOrders)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        foreach (var item in sortOrders)
        {
            await conn.ExecuteAsync(@"
                UPDATE Category 
                SET SortOrder = @SortOrder, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id;
            ", new { Id = item.Id, SortOrder = item.SortOrder, UpdatedAt = DateTime.UtcNow }, transaction);
        }

        transaction.Commit();
    }
}
