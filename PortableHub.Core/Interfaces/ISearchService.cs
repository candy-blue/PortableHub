using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface ISearchService
{
    void IndexSoftware(IEnumerable<Software> softwareList);
    IReadOnlyList<Software> Search(string query, int? categoryId = null, bool favoriteOnly = false, bool recentOnly = false);
    IReadOnlyList<Software> SearchQuickLauncher(string query, int maxResults = 10);
}
