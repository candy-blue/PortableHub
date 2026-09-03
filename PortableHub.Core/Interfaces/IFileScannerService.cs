using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface IFileScannerService
{
    Task<IReadOnlyList<SoftwareScanCandidate>> ScanDirectoryAsync(
        string directoryPath, 
        IReadOnlyList<Category> existingCategories,
        int? rootId = null,
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default);

    SoftwareScanCandidate AnalyzeExe(string exePath, IReadOnlyList<Category> existingCategories, int? rootId = null);
    bool IsExcludedExe(string filePath);
}
