using System.Runtime.InteropServices;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class WindowsHotkeyService : IHotkeyService
{
    private const int HOTKEY_ID = 9001;
    private const int WM_HOTKEY = 0x0312;

    private IntPtr _windowHandle = IntPtr.Zero;
    private bool _isRegistered;
    private string _currentHotkey = string.Empty;

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered => _isRegistered;
    public string CurrentHotkey => _currentHotkey;

    public bool Register(string hotkeyString, IntPtr windowHandle)
    {
        Unregister(windowHandle);

        if (string.IsNullOrWhiteSpace(hotkeyString) || windowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (!ParseHotkey(hotkeyString, out var modifiers, out var vk))
        {
            return false;
        }

        var success = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, vk);
        if (success)
        {
            _windowHandle = windowHandle;
            _isRegistered = true;
            _currentHotkey = hotkeyString;
        }

        return success;
    }

    public void Unregister(IntPtr windowHandle)
    {
        var handle = windowHandle != IntPtr.Zero ? windowHandle : _windowHandle;
        if (handle != IntPtr.Zero && _isRegistered)
        {
            UnregisterHotKey(handle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    public void ProcessWindowMessage(int msg, IntPtr wParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public static bool ParseHotkey(string hotkeyString, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_WIN = 0x0008;
        const uint MOD_NOREPEAT = 0x4000;

        modifiers |= MOD_NOREPEAT;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i].ToLowerInvariant();
            if (p == "ctrl" || p == "control") modifiers |= MOD_CONTROL;
            else if (p == "alt") modifiers |= MOD_ALT;
            else if (p == "shift") modifiers |= MOD_SHIFT;
            else if (p == "win" || p == "windows") modifiers |= MOD_WIN;
        }

        var keyPart = parts[^1].ToUpperInvariant();
        vk = keyPart switch
        {
            "SPACE" => 0x20,
            "ENTER" => 0x0D,
            "ESC" or "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            _ when keyPart.Length == 1 && keyPart[0] >= 'A' && keyPart[0] <= 'Z' => (uint)keyPart[0],
            _ when keyPart.Length == 1 && keyPart[0] >= '0' && keyPart[0] <= '9' => (uint)keyPart[0],
            _ => 0
        };

        return vk != 0;
    }

    public void Dispose()
    {
        Unregister(_windowHandle);
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
