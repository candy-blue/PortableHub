namespace PortableHub.Core.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(string hotkeyString, IntPtr windowHandle);
    void Unregister(IntPtr windowHandle);
    bool IsRegistered { get; }
    string CurrentHotkey { get; }
    bool TestHotkeyAvailable(string hotkeyString);
}
