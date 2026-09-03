using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface IRootDirectoryRepository
{
    Task<IReadOnlyList<RootDirectory>> GetAllAsync();
    Task<RootDirectory?> GetByIdAsync(int id);
    Task<int> AddAsync(RootDirectory root);
    Task DeleteAsync(int id);
    Task UpdateAsync(RootDirectory root);
}
