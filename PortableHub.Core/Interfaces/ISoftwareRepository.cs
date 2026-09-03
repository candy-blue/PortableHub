using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface ISoftwareRepository
{
    Task<IReadOnlyList<Software>> GetAllAsync();
    Task<Software?> GetByIdAsync(int id);
    Task<int> AddAsync(Software software);
    Task UpdateAsync(Software software);
    Task DeleteAsync(int id);
    Task UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> sortOrders);
    Task UpdateCategoryAsync(int softwareId, int categoryId);
    Task IncrementLaunchCountAsync(int id, DateTime launchedAt);
    Task UpdateFavoriteAsync(int id, bool isFavorite);
    Task UpdatePathAsync(int id, string newExePath, int? rootId, string? relativePath);
}
