using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.Infrastructure.Services;

public class LaunchService : ILaunchService
{
    private readonly ISoftwareRepository _softwareRepository;

    public LaunchService(ISoftwareRepository softwareRepository)
    {
        _softwareRepository = softwareRepository;
    }

    public bool IsRunning(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.ExePath))
            return false;

        var processName = Path.GetFileNameWithoutExtension(software.ExePath);
        var processes = Process.GetProcessesByName(processName);
        return processes.Length > 0;
    }

    public bool ActivateExistingWindow(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.ExePath))
            return false;

        var processName = Path.GetFileNameWithoutExtension(software.ExePath);
        var processes = Process.GetProcessesByName(processName);

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

    public async Task<LaunchResult> LaunchAsync(Software software)
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
                await _softwareRepository.IncrementLaunchCountAsync(software.Id, DateTime.UtcNow);
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

            var process = Process.Start(startInfo);
            if (process != null)
            {
                await _softwareRepository.IncrementLaunchCountAsync(software.Id, DateTime.UtcNow);
                return LaunchResult.Ok(process.Id);
            }

            return LaunchResult.Fail("启动失败：进程未能成功创建。");
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
