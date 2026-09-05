using System.Runtime.InteropServices;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class WindowsHotkeyService : IHotkeyService
{
    private const int HOTKEY_ID = 9001;
    private const int TEST_HOTKEY_ID = 9002;
    private const int WM_HOTKEY = 0x0312;

    private IntPtr _windowHandle = IntPtr.Zero;
    private bool _isRegistered;
    private string _currentHotkey = string.Empty;

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered => _isRegistered;
    public string CurrentHotkey => _currentHotkey;

    public bool Register(string hotkeyString, IntPtr windowHandle)
    {
        var targetHandle = windowHandle != IntPtr.Zero ? windowHandle : _windowHandle;
        Unregister(targetHandle);

        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return false;
        }

        if (!ParseHotkey(hotkeyString, out var modifiers, out var vk))
        {
            return false;
        }

        var success = RegisterHotKey(targetHandle, HOTKEY_ID, modifiers, vk);
        if (success)
        {
            _windowHandle = targetHandle;
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

    public bool TestHotkeyAvailable(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        if (!ParseHotkey(hotkeyString, out var modifiers, out var vk))
            return false;

        // Mouse buttons cannot be registered via Windows RegisterHotKey API
        if (IsMouseKey(hotkeyString))
            return false;

        // If it's already registered as our current hotkey, it is available
        if (_isRegistered && string.Equals(_currentHotkey, hotkeyString, StringComparison.OrdinalIgnoreCase))
            return true;

        if (_windowHandle != IntPtr.Zero)
        {
            var success = RegisterHotKey(_windowHandle, TEST_HOTKEY_ID, modifiers, vk);
            if (success)
            {
                UnregisterHotKey(_windowHandle, TEST_HOTKEY_ID);
                return true;
            }
            return false;
        }

        return true;
    }

    public static bool IsMouseKey(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString)) return false;
        var s = hotkeyString.ToLowerInvariant();
        return s.Contains("鼠标") || s.Contains("mouse") || s.Contains("mbutton") || 
               s.Contains("xbutton") || s.Contains("中键") || s.Contains("滚轮");
    }

    public static bool ParseHotkey(string hotkeyString, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

        var text = hotkeyString.Trim();
        // Support direct concatenation like "ctrl," or "alt," without a plus sign
        if (text.EndsWith(",") && !text.EndsWith("+,") && text.Length > 1)
        {
            text = text[..^1] + "+,";
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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
            "ENTER" or "RETURN" => 0x0D,
            "ESC" or "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "BACKSPACE" or "BACK" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PRIOR" => 0x21,
            "PAGEDOWN" or "NEXT" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "CAPSLOCK" or "CAPS" => 0x14,
            "PRINTSCREEN" or "SNAPSHOT" => 0x2C,
            "SCROLL" => 0x91,
            "PAUSE" => 0x13,

            // Punctuation keys
            "," or "COMMA" or "OEMCOMMA" => 0xBC, // VK_OEM_COMMA
            "." or "PERIOD" or "OEMPERIOD" or "DOT" => 0xBE, // VK_OEM_PERIOD
            "/" or "SLASH" or "OEM2" or "OEMQUESTION" => 0xBF, // VK_OEM_2
            ";" or "SEMICOLON" or "OEM1" => 0xBA, // VK_OEM_1
            "'" or "QUOTE" or "OEM7" or "\"" => 0xDE, // VK_OEM_7
            "[" or "OPENBRACKET" or "LBRACKET" or "OEM4" => 0xDB, // VK_OEM_4
            "]" or "CLOSEBRACKET" or "RBRACKET" or "OEM6" => 0xDD, // VK_OEM_6
            "\\" or "BACKSLASH" or "OEM5" => 0xDC, // VK_OEM_5
            "-" or "MINUS" or "OEMMINUS" => 0xBD, // VK_OEM_MINUS
            "=" or "PLUS" or "OEMPLUS" or "EQUALS" => 0xBB, // VK_OEM_PLUS
            "`" or "TILDE" or "OEM3" or "BACKQUOTE" => 0xC0, // VK_OEM_3

            // Function keys F1 - F24
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

            // Mouse buttons (mapped to virtual key codes)
            "鼠标中键" or "中键" or "MOUSEMIDDLE" or "MBUTTON" or "MOUSE3" or "MID" => 0x04, // VK_MBUTTON
            "鼠标左键" or "左键" or "MOUSELEFT" or "LBUTTON" or "MOUSE1" => 0x01, // VK_LBUTTON
            "鼠标右键" or "右键" or "MOUSERIGHT" or "RBUTTON" or "MOUSE2" => 0x02, // VK_RBUTTON
            "鼠标侧键1" or "鼠标侧键" or "鼠标4" or "XBUTTON1" or "MOUSE4" => 0x05, // VK_XBUTTON1
            "鼠标侧键2" or "鼠标5" or "XBUTTON2" or "MOUSE5" => 0x06, // VK_XBUTTON2
            "鼠标滚轮" or "滚轮" or "WHEEL" => 0x04,

            _ when keyPart.Length == 1 && keyPart[0] >= 'A' && keyPart[0] <= 'Z' => (uint)keyPart[0],
            _ when keyPart.Length == 1 && keyPart[0] >= '0' && keyPart[0] <= '9' => (uint)keyPart[0],
            _ => 0
        };

        if (vk == 0) return false;

        // Disallow dangerous single-key combinations like Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X that hijack system-wide clipboard/editing
        uint modWithoutNoRepeat = modifiers & ~MOD_NOREPEAT;
        if (modWithoutNoRepeat == MOD_CONTROL && vk >= 'A' && vk <= 'Z')
        {
            return false;
        }

        return true;
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
