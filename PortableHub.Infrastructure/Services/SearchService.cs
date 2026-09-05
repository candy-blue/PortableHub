using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly List<Software> _indexedList = [];
    private readonly object _lock = new();

    public void IndexSoftware(IEnumerable<Software> softwareList)
    {
        lock (_lock)
        {
            _indexedList.Clear();
            _indexedList.AddRange(softwareList);
        }
    }

    public IReadOnlyList<Software> Search(string query, int? categoryId = null, bool favoriteOnly = false, bool recentOnly = false)
    {
        lock (_lock)
        {
            IEnumerable<Software> items = _indexedList;

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                items = items.Where(s => s.CategoryId == categoryId.Value);
            }

            if (favoriteOnly)
            {
                items = items.Where(s => s.IsFavorite);
            }

            if (recentOnly)
            {
                items = items.Where(s => s.LastLaunchedAt.HasValue)
                             .OrderByDescending(s => s.LastLaunchedAt);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return items.ToList();
            }

            var cleanQuery = query.Trim().ToLowerInvariant();
            var tokens = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return items.Where(s => MatchesAllTokens(s, tokens)).ToList();
        }
    }

    public IReadOnlyList<Software> SearchQuickLauncher(string query, int maxResults = 10)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Return favorites first, then recently used / frequently used
                return _indexedList
                    .OrderByDescending(s => s.IsFavorite)
                    .ThenByDescending(s => s.LastLaunchedAt ?? DateTime.MinValue)
                    .ThenByDescending(s => s.LaunchCount)
                    .Take(maxResults)
                    .ToList();
            }

            var cleanQuery = query.Trim().ToLowerInvariant();
            var tokens = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return _indexedList
                .Where(s => MatchesAllTokens(s, tokens))
                .Select(s => new { Software = s, Score = CalculateScore(s, cleanQuery) })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Software.LaunchCount)
                .ThenByDescending(x => x.Software.LastLaunchedAt ?? DateTime.MinValue)
                .Take(maxResults)
                .Select(x => x.Software)
                .ToList();
        }
    }

    private static bool MatchesAllTokens(Software s, string[] tokens)
    {
        var name = s.Name.ToLowerInvariant();
        var category = s.CategoryName.ToLowerInvariant();
        var tags = (s.Tags ?? string.Empty).ToLowerInvariant();
        var desc = (s.Description ?? string.Empty).ToLowerInvariant();
        var path = s.ExePath.ToLowerInvariant();

        foreach (var token in tokens)
        {
            if (!name.Contains(token) &&
                !category.Contains(token) &&
                !tags.Contains(token) &&
                !desc.Contains(token) &&
                !path.Contains(token))
            {
                return false;
            }
        }

        return true;
    }

    private static int CalculateScore(Software s, string cleanQuery)
    {
        int score = 0;
        var name = s.Name.ToLowerInvariant();

        if (name == cleanQuery)
            score += 1000;
        else if (name.StartsWith(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 800;
        else if (name.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 500;
        else
            score += 100;

        var tags = (s.Tags ?? string.Empty).ToLowerInvariant();
        if (tags.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 300;

        var cat = s.CategoryName.ToLowerInvariant();
        if (cat.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 200;

        var desc = (s.Description ?? string.Empty).ToLowerInvariant();
        if (desc.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 200;

        var path = s.ExePath.ToLowerInvariant();
        if (path.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            score += 100;

        if (s.IsFavorite)
            score += 100;

        if (s.LastLaunchedAt.HasValue)
        {
            var span = DateTime.UtcNow - s.LastLaunchedAt.Value;
            if (span.TotalDays < 7)
                score += 150;
            else if (span.TotalDays < 30)
                score += 80;
        }

        score += Math.Min(s.LaunchCount * 5, 100);

        return score;
    }
}
