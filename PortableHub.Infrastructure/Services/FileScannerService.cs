using System.Diagnostics;
using System.Text.RegularExpressions;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class FileScannerService : IFileScannerService
{
    private static readonly HashSet<string> ExcludedExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "uninstall", "unins000", "unins001", "setup", "installer", "install",
        "update", "updater", "crashpad_handler", "helper", "service", "driver",
        "elevate", "vc_redist", "dxwebsetup", "vcredist_x64", "vcredist_x86",
        "python", "pythonw", "node", "npm", "npx", "git", "cmd", "powershell",
        "conhost", "bash", "ssh", "7za", "7zr", "regsvr32", "rundll32",
        "unitycrashhandler64", "unitycrashhandler32", "ffmpeg", "ffprobe"
    };

    private static readonly string[] ExcludedFolders =
    [
        "bin", "lib", "runtimes", "plugins", "resources", "node_modules", ".git",
        "obj", "packages", "locales", "swiftshader", "x86", "x64", "arm64"
    ];

    public bool IsExcludedExe(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return true;

        if (ExcludedExeNames.Contains(fileName))
            return true;

        var lowerName = fileName.ToLowerInvariant();
        if (lowerName.StartsWith("unins") || lowerName.StartsWith("uninstall") ||
            lowerName.StartsWith("setup") || lowerName.StartsWith("install") ||
            lowerName.EndsWith("_helper") || lowerName.Contains("crashpad") ||
            lowerName.Contains("crash_handler"))
        {
            return true;
        }

        // Check excluded folder parts
        var dirParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (int i = 0; i < dirParts.Length - 1; i++)
        {
            var part = dirParts[i].ToLowerInvariant();
            if (ExcludedFolders.Contains(part))
            {
                // If it's inside a deep bin/lib/runtime folder, exclude it
                return true;
            }
        }

        return false;
    }

    public async Task<IReadOnlyList<SoftwareScanCandidate>> ScanDirectoryAsync(
        string directoryPath, 
        IReadOnlyList<Category> existingCategories,
        int? rootId = null,
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        return await Task.Run(() =>
        {
            var candidates = new List<SoftwareScanCandidate>();
            var allExeFiles = new List<string>();

            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false
                };

                foreach (var file in Directory.EnumerateFiles(directoryPath, "*.exe", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allExeFiles.Add(file);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Ignore enumeration errors
            }

            int count = 0;
            foreach (var exe in allExeFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                count++;
                progress?.Report((int)((count / (double)Math.Max(1, allExeFiles.Count)) * 100));

                if (IsExcludedExe(exe))
                {
                    continue;
                }

                var candidate = AnalyzeExe(exe, existingCategories, rootId);
                candidates.Add(candidate);
            }

            return (IReadOnlyList<SoftwareScanCandidate>)candidates;
        }, cancellationToken);
    }

    public SoftwareScanCandidate AnalyzeExe(string exePath, IReadOnlyList<Category> existingCategories, int? rootId = null)
    {
        var fileInfo = new FileInfo(exePath);
        var fileName = fileInfo.Name;
        var deducedName = CleanFileName(Path.GetFileNameWithoutExtension(exePath));
        string? description = null;
        string? productName = null;
        string? company = null;
        string? version = null;

        try
        {
            var vi = FileVersionInfo.GetVersionInfo(exePath);
            description = vi.FileDescription;
            productName = vi.ProductName;
            company = vi.CompanyName;
            version = vi.FileVersion;

            if (!string.IsNullOrWhiteSpace(productName) && !IsGenericTitle(productName))
            {
                deducedName = productName.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(description) && !IsGenericTitle(description))
            {
                deducedName = description.Trim();
            }
        }
        catch
        {
            // Ignore FileVersionInfo exceptions
        }

        // Deduce category
        var (catName, catId) = DeduceCategory(deducedName, fileName, existingCategories);

        return new SoftwareScanCandidate
        {
            ExePath = exePath,
            FileName = fileName,
            DeducedName = deducedName,
            DeducedCategory = catName,
            CategoryId = catId,
            FileDescription = description,
            ProductName = productName,
            CompanyName = company,
            FileVersion = version,
            ConfidenceScore = 100,
            IsSelected = true,
            RootId = rootId
        };
    }

    private static string CleanFileName(string rawName)
    {
        var name = rawName;
        name = Regex.Replace(name, @"[-_]?(x64|x86|win64|win32|64bit|32bit|portable|setup|v\d+.*)$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"Portable$", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(name) ? rawName : name.Trim();
    }

    private static bool IsGenericTitle(string s)
    {
        var l = s.Trim().ToLowerInvariant();
        return l is "setup" or "installer" or "application" or "main application" or "launcher";
    }

    private static (string CategoryName, int CategoryId) DeduceCategory(string deducedName, string fileName, IReadOnlyList<Category> categories)
    {
        var text = $"{deducedName} {fileName}".ToLowerInvariant();

        var devKeywords = new[] { "code", "git", "vs", "studio", "dev", "ide", "editor", "python", "rust", "database", "dbeaver", "navicat", "postman" };
        var sysKeywords = new[] { "everything", "wiztree", "cpu-z", "gpu-z", "hwinfo", "task", "reg", "clean", "autorun", "rufus", "dism", "system", "process" };
        var netKeywords = new[] { "web", "chrome", "firefox", "edge", "clash", "v2ray", "ftp", "xftp", "cpolar", "wireshark", "download", "idm", "torrent", "browser", "net" };
        var officeKeywords = new[] { "office", "word", "excel", "pdf", "sumatra", "notepad", "note", "markdown", "obsidian", "typora", "doc" };
        var mediaKeywords = new[] { "media", "player", "vlc", "mpv", "potplayer", "ffmpeg", "obs", "audio", "video", "music", "sound" };
        var imageKeywords = new[] { "photo", "image", "draw", "paint", "photoshop", "gimp", "snip", "screen", "capture", "viewer" };
        var fileKeywords = new[] { "7z", "7-zip", "zip", "rar", "tar", "bandizip", "peazip", "totalcmd", "copy", "sync" };
        var secKeywords = new[] { "sec", "guard", "safe", "virus", "hash", "cert", "keepass", "bitwarden", "pwd", "password" };

        string targetName = "其他";

        if (devKeywords.Any(k => text.Contains(k))) targetName = "开发工具";
        else if (sysKeywords.Any(k => text.Contains(k))) targetName = "系统工具";
        else if (netKeywords.Any(k => text.Contains(k))) targetName = "网络工具";
        else if (officeKeywords.Any(k => text.Contains(k))) targetName = "办公工具";
        else if (imageKeywords.Any(k => text.Contains(k))) targetName = "图形图像";
        else if (mediaKeywords.Any(k => text.Contains(k))) targetName = "多媒体";
        else if (fileKeywords.Any(k => text.Contains(k))) targetName = "文件管理";
        else if (secKeywords.Any(k => text.Contains(k))) targetName = "安全工具";

        var found = categories.FirstOrDefault(c => c.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            return (found.Name, found.Id);
        }

        var other = categories.FirstOrDefault(c => c.Name == "其他") ?? categories.FirstOrDefault();
        return other != null ? (other.Name, other.Id) : (targetName, 1);
    }
}
