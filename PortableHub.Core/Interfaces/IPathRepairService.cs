using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface IPathRepairService
{
    Task<string?> TryAutoRepairPathAsync(Software software, IEnumerable<RootDirectory> roots);
    Task<int> BatchRelocateRootAsync(string oldRootPath, string newRootPath, IEnumerable<Software> softwareList);
    string ComputeRelativePath(string fullPath, string rootPath);
    string ResolveFullPath(string? relativePath, string? rootPath, string fallbackExePath);
}
