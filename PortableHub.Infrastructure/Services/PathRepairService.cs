using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Windows;

namespace PortableHub.Infrastructure.Services;

public class PathRepairService : IPathRepairService
{
    private readonly ISoftwareRepository _softwareRepository;

    public PathRepairService(ISoftwareRepository softwareRepository)
    {
        _softwareRepository = softwareRepository;
    }

    public string ComputeRelativePath(string fullPath, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(rootPath))
            return fullPath;

        try
        {
            var normalizedFull = Path.GetFullPath(fullPath);
            var normalizedRoot = Path.GetFullPath(rootPath);

            if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedRoot += Path.DirectorySeparatorChar;
            }

            if (normalizedFull.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFull[normalizedRoot.Length..];
            }

            return fullPath;
        }
        catch
        {
            return fullPath;
        }
    }

    public string ResolveFullPath(string? relativePath, string? rootPath, string fallbackExePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath) && !string.IsNullOrWhiteSpace(rootPath))
        {
            try
            {
                var combined = Path.Combine(rootPath, relativePath);
                if (File.Exists(combined))
                {
                    return Path.GetFullPath(combined);
                }
            }
            catch
            {
                // Fall back to fallbackExePath
            }
        }

        return fallbackExePath;
    }

    public async Task<string?> TryAutoRepairPathAsync(Software software, IEnumerable<RootDirectory> roots)
    {
        if (File.Exists(software.ExePath))
        {
            return software.ExePath;
        }

        if (string.IsNullOrWhiteSpace(software.ExePath))
        {
            return null;
        }

        string fileName;
        try
        {
            fileName = Path.GetFileName(software.ExePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
        }
        catch
        {
            return null;
        }

        var relative = software.RelativePath;

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root.Path) || !Directory.Exists(root.Path))
                continue;

            // 1. Try matching relative path in root
            if (!string.IsNullOrWhiteSpace(relative))
            {
                try
                {
                    var candidate = Path.Combine(root.Path, relative);
                    if (File.Exists(candidate))
                    {
                        var resolved = Path.GetFullPath(candidate);
                        await _softwareRepository.UpdatePathAsync(software.Id, resolved, root.Id, relative);
                        return resolved;
                    }
                }
                catch
                {
                    // Fall back to searching
                }
            }

            // 2. Try searching for identical filename in root directory (shallow/medium depth)
            // Guard against scanning entire drive root when path is a volume root (e.g. D:\)
            try
            {
                var isDrive = ShortcutHelper.IsDriveRoot(root.Path);
                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = !isDrive,
                    MaxRecursionDepth = isDrive ? 0 : 3
                };

                var matches = Directory.GetFiles(root.Path, fileName, options);
                if (matches.Length > 0)
                {
                    var matched = Path.GetFullPath(matches[0]);
                    var newRelative = ComputeRelativePath(matched, root.Path);
                    await _softwareRepository.UpdatePathAsync(software.Id, matched, root.Id, newRelative);
                    return matched;
                }
            }
            catch
            {
                // Ignore search exceptions for individual inaccessible folders or invalid roots
            }
        }

        return null;
    }

    public async Task<int> BatchRelocateRootAsync(string oldRootPath, string newRootPath, IEnumerable<Software> softwareList)
    {
        var count = 0;
        var normalizedOld = Path.GetFullPath(oldRootPath);
        if (!normalizedOld.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            normalizedOld += Path.DirectorySeparatorChar;
        }

        foreach (var s in softwareList)
        {
            if (s.ExePath.StartsWith(normalizedOld, StringComparison.OrdinalIgnoreCase))
            {
                var rel = s.ExePath[normalizedOld.Length..];
                var newPath = Path.Combine(newRootPath, rel);
                await _softwareRepository.UpdatePathAsync(s.Id, newPath, s.RootId, rel);
                count++;
            }
        }

        return count;
    }
}
