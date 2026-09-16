using System.IO;
using System.Runtime.InteropServices;

namespace PortableHub.Infrastructure.Windows;

public static class ShortcutHelper
{
    private const int MaxShortcutDepth = 10;

    /// <summary>
    /// Checks whether the specified path represents a drive root (e.g., C:\, D:\, D:, \\server\share\).
    /// </summary>
    public static bool IsDriveRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var trimmed = path.Trim();
        if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
        {
            trimmed += Path.DirectorySeparatorChar;
        }

        try
        {
            var fullPath = Path.GetFullPath(trimmed);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
                return false;

            return string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

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

        object? shell = null;
        object? shortcut = null;
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;

            shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(shortcutPath);
            if (shortcut == null) return null;

            dynamic dynamicShortcut = shortcut;
            string? targetPath = dynamicShortcut.TargetPath;

            if (!string.IsNullOrWhiteSpace(targetPath) && (File.Exists(targetPath) || Directory.Exists(targetPath)))
            {
                return targetPath;
            }
        }
        catch
        {
            // Ignore shortcut resolution errors and return null
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
            {
                try { Marshal.FinalReleaseComObject(shortcut); } catch { }
            }
            if (shell != null && Marshal.IsComObject(shell))
            {
                try { Marshal.FinalReleaseComObject(shell); } catch { }
            }
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
            var isDrive = IsDriveRoot(dirPath);
            var dirName = isDrive ? string.Empty : Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            // 1. Check top-level exe files with IgnoreInaccessible
            var topOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };
            var exes = Directory.GetFiles(dirPath, "*.exe", topOptions);

            // If not found in root and not a drive root, search shallow subdirectories (max depth 2, e.g. app/bin)
            if (exes.Length == 0 && !isDrive)
            {
                var subOptions = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 2
                };
                exes = Directory.GetFiles(dirPath, "*.exe", subOptions);
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

            // 3. Match directory name (if not a drive root)
            if (!string.IsNullOrEmpty(dirName))
            {
                var matchDir = candidates.FirstOrDefault(e =>
                    Path.GetFileNameWithoutExtension(e).Equals(dirName, StringComparison.OrdinalIgnoreCase));
                if (matchDir != null)
                    return matchDir;
            }

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
        return NormalizeDroppedPathCore(droppedPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
    }

    private static string? NormalizeDroppedPathCore(string droppedPath, HashSet<string> visited, int depth)
    {
        if (string.IsNullOrWhiteSpace(droppedPath) || depth > MaxShortcutDepth)
            return null;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(droppedPath);
        }
        catch
        {
            return null;
        }

        if (!visited.Add(fullPath))
        {
            // Cycle detected
            return null;
        }

        // If it's a directory
        if (Directory.Exists(fullPath))
        {
            return FindPrimaryExeInDirectory(fullPath);
        }

        if (!File.Exists(fullPath))
            return null;

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();

        // If it's a .lnk shortcut
        if (ext == ".lnk")
        {
            var resolved = ResolveShortcut(fullPath);
            if (!string.IsNullOrEmpty(resolved))
            {
                return NormalizeDroppedPathCore(resolved, visited, depth + 1);
            }
            return null;
        }

        // Supported executable file extensions
        if (ext is ".exe" or ".bat" or ".cmd")
        {
            return fullPath;
        }

        return null;
    }

    /// <summary>
    /// Creates a Windows .lnk shortcut on the current user's desktop.
    /// </summary>
    public static bool CreateDesktopShortcut(string targetPath, string shortcutName, string? arguments = null, string? workingDir = null, string? iconPath = null)
    {
        if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
            return false;

        object? shell = null;
        object? shortcut = null;
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!Directory.Exists(desktop)) return false;

            // Clean invalid file name chars
            var safeName = string.Join("_", shortcutName.Split(Path.GetInvalidFileNameChars()));
            var shortcutFile = Path.Combine(desktop, $"{safeName}.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return false;

            shell = Activator.CreateInstance(shellType);
            if (shell == null) return false;

            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(shortcutFile);
            if (shortcut == null) return false;

            dynamic dynamicShortcut = shortcut;
            dynamicShortcut.TargetPath = targetPath;
            if (!string.IsNullOrWhiteSpace(arguments)) dynamicShortcut.Arguments = arguments;
            if (!string.IsNullOrWhiteSpace(workingDir)) dynamicShortcut.WorkingDirectory = workingDir;
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath)) dynamicShortcut.IconLocation = iconPath;
            dynamicShortcut.Save();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
            {
                try { Marshal.FinalReleaseComObject(shortcut); } catch { }
            }
            if (shell != null && Marshal.IsComObject(shell))
            {
                try { Marshal.FinalReleaseComObject(shell); } catch { }
            }
        }
    }
}

