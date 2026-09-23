using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class LaunchService : ILaunchService
{
    private readonly ISoftwareRepository _softwareRepository;
    private readonly Func<ProcessStartInfo, Process?> _processLauncher;

    public LaunchService(ISoftwareRepository softwareRepository, Func<ProcessStartInfo, Process?>? processLauncher = null)
    {
        _softwareRepository = softwareRepository;
        _processLauncher = processLauncher ?? (psi => Process.Start(psi));
    }

    public bool IsRunning(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.ExePath))
            return false;

        try
        {
            var processName = Path.GetFileNameWithoutExtension(software.ExePath);
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            var processes = Process.GetProcessesByName(processName);
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (var proc in processes)
                {
                    try { proc.Dispose(); } catch { }
                }
            }
        }
        catch
        {
            return false;
        }
    }

    public bool ActivateExistingWindow(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.ExePath))
            return false;

        try
        {
            var processName = Path.GetFileNameWithoutExtension(software.ExePath);
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            var processes = Process.GetProcessesByName(processName);
            try
            {
                foreach (var proc in processes)
                {
                    try
                    {
                        var handle = proc.MainWindowHandle;
                        if (handle != IntPtr.Zero)
                        {
                            ShowWindow(handle, SW_RESTORE);
                            SetForegroundWindow(handle);
                            return true;
                        }
                    }
                    catch
                    {
                        // Continue to next process instance
                    }
                }

                return false;
            }
            finally
            {
                foreach (var proc in processes)
                {
                    try { proc.Dispose(); } catch { }
                }
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<LaunchResult> LaunchAsync(Software software)
    {
        var context = new CascadeContext();
        if (software.Id > 0)
        {
            context.Visited.Add(software.Id);
        }
        return await LaunchWithLinkedCascadeAsync(software, context);
    }

    private class CascadeContext
    {
        public HashSet<int> Visited { get; } = new();
        public int LaunchedLinkedCount { get; set; }
    }

    private async Task<LaunchResult> LaunchWithLinkedCascadeAsync(Software software, CascadeContext context)
    {
        var primaryResult = await LaunchSingleAsync(software);
        if (!primaryResult.Success)
        {
            return primaryResult;
        }

        // Linked launch processing (Maximum 5 linked apps guard, cycle prevention)
        if (_softwareRepository != null)
        {
            var linkedIds = software.GetLinkedSoftwareIdList();
            foreach (var linkedId in linkedIds)
            {
                if (context.LaunchedLinkedCount >= 5) break; // Hard limit: max 5 linked apps
                if (!context.Visited.Add(linkedId)) continue; // Loop / cycle guard

                try
                {
                    var linkedSoftware = await _softwareRepository.GetByIdAsync(linkedId);
                    if (linkedSoftware != null)
                    {
                        context.LaunchedLinkedCount++;
                        // Cascade launch linked software with the same shared context
                        _ = await LaunchWithLinkedCascadeAsync(linkedSoftware, context);
                    }
                }
                catch
                {
                    // Defensive: linked app launch failure does not break primary software launch
                }
            }
        }

        return primaryResult;
    }

    private async Task<LaunchResult> LaunchSingleAsync(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.ExePath))
        {
            return LaunchResult.Fail("软件路径为空，无法启动。");
        }

        if (!File.Exists(software.ExePath))
        {
            return LaunchResult.Fail($"文件不存在: {software.ExePath}，请检查路径或重新定位。");
        }

        // Single instance check
        if (software.SingleInstance && IsRunning(software))
        {
            var activated = ActivateExistingWindow(software);
            if (activated)
            {
                // Record launch
                if (_softwareRepository != null)
                {
                    await _softwareRepository.IncrementLaunchCountAsync(software.Id, DateTime.UtcNow);
                }
                return LaunchResult.Ok(null, activated: true);
            }
        }

        try
        {
            var workingDir = !string.IsNullOrWhiteSpace(software.WorkingDirectory)
                ? software.WorkingDirectory
                : Path.GetDirectoryName(software.ExePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = software.ExePath,
                Arguments = software.Arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrEmpty(workingDir) ? string.Empty : workingDir,
                UseShellExecute = true
            };

            if (software.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            using var process = _processLauncher(startInfo);
            int? processId = null;
            if (process != null)
            {
                try
                {
                    processId = process.Id;
                }
                catch
                {
                    // ShellExecute might not expose Id in all execution scenarios
                }
            }

            if (_softwareRepository != null)
            {
                await _softwareRepository.IncrementLaunchCountAsync(software.Id, DateTime.UtcNow);
            }
            return LaunchResult.Ok(processId);
        }
        catch (Win32Exception winEx) when (winEx.NativeErrorCode == 1223)
        {
            // The operation was canceled by the user (UAC prompt declined)
            return LaunchResult.Fail("用户取消了管理员授权启动。");
        }
        catch (Exception ex)
        {
            return LaunchResult.Fail($"无法启动软件: {ex.Message}");
        }
    }

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
