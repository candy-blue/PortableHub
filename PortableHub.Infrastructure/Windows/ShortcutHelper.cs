using System.IO;

namespace PortableHub.Infrastructure.Windows;

public static class ShortcutHelper
{
    /// <summary>
    /// Resolves the target executable path from a Windows .lnk shortcut file.
    /// Uses native Windows WScript.Shell COM object via dynamic invocation without third-party dependencies.
    /// </summary>
    public static string? ResolveShortcut(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
            return null;

        if (!Path.GetExtension(shortcutPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            string? targetPath = shortcut.TargetPath;

            if (!string.IsNullOrWhiteSpace(targetPath) && (File.Exists(targetPath) || Directory.Exists(targetPath)))
            {
                return targetPath;
            }
        }
        catch
        {
            // Ignore shortcut resolution errors and return null
        }

        return null;
    }

    /// <summary>
    /// Analyzes a dropped directory and attempts to find its primary executable file.
    /// </summary>
    public static string? FindPrimaryExeInDirectory(string dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
            return null;

        try
        {
            var dirName = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            
            // 1. Check top-level exe files
            var exes = Directory.GetFiles(dirPath, "*.exe", SearchOption.TopDirectoryOnly);
            if (exes.Length == 0)
            {
                // Also search 1-level subdirectories if not found in root (e.g. app/bin)
                exes = Directory.GetFiles(dirPath, "*.exe", SearchOption.AllDirectories);
            }

            if (exes.Length == 0)
                return null;

            if (exes.Length == 1)
                return exes[0];

            // 2. Filter out uninstallers and utilities
            var candidates = exes.Where(e =>
            {
                var fn = Path.GetFileNameWithoutExtension(e).ToLowerInvariant();
                return !fn.StartsWith("unins") &&
                       !fn.StartsWith("setup") &&
                       !fn.StartsWith("install") &&
                       !fn.StartsWith("update") &&
                       !fn.Contains("crash") &&
                       !fn.Contains("helper") &&
                       !fn.Contains("elevate") &&
                       !fn.Contains("service");
            }).ToList();

            if (candidates.Count == 0)
            {
                candidates = exes.ToList();
            }

            // 3. Match directory name
            var matchDir = candidates.FirstOrDefault(e =>
                Path.GetFileNameWithoutExtension(e).Equals(dirName, StringComparison.OrdinalIgnoreCase));
            if (matchDir != null)
                return matchDir;

            // 4. Return the shortest file name candidate (usually the main app, e.g. "Code.exe" vs "Code-Helper.exe")
            return candidates.OrderBy(c => Path.GetFileName(c).Length).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Normalizes a dropped file or directory path into an executable path if valid.
    /// </summary>
    public static string? NormalizeDroppedPath(string droppedPath)
    {
        if (string.IsNullOrWhiteSpace(droppedPath))
            return null;

        // If it's a directory
        if (Directory.Exists(droppedPath))
        {
            return FindPrimaryExeInDirectory(droppedPath);
        }

        if (!File.Exists(droppedPath))
            return null;

        var ext = Path.GetExtension(droppedPath).ToLowerInvariant();

        // If it's a .lnk shortcut
        if (ext == ".lnk")
        {
            var resolved = ResolveShortcut(droppedPath);
            if (!string.IsNullOrEmpty(resolved))
            {
                return NormalizeDroppedPath(resolved);
            }
            return null;
        }

        // Supported executable file extensions
        if (ext is ".exe" or ".bat" or ".cmd")
        {
            return droppedPath;
        }

        return null;
    }
}
