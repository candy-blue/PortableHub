namespace PortableHub.Core.Interfaces;

public interface IStartupService
{
    bool IsAutoStartEnabled();
    bool SetAutoStart(bool enable, bool startMinimizedToTray);
}
