using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<int> AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id, int fallbackCategoryId);
    Task UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> sortOrders);
}
