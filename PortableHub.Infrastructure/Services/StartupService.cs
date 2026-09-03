using Microsoft.Win32;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "PortableHub";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var val = key?.GetValue(AppName);
            return val != null;
        }
        catch
        {
            return false;
        }
    }

    public bool SetAutoStart(bool enable, bool startMinimizedToTray)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
                return false;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
                var command = $"\"{exePath}\"";
                if (startMinimizedToTray)
                {
                    command += " --minimized";
                }
                key.SetValue(AppName, command);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
