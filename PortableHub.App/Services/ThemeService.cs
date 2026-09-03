using System.Windows;
using Microsoft.Win32;

namespace PortableHub.App.Services;

public class ThemeService
{
    private string _currentTheme = "System";

    public event EventHandler? ThemeChanged;

    public void ApplyTheme(string theme)
    {
        _currentTheme = theme;
        var isDark = theme switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsWindowsInDarkMode()
        };

        ApplyThemeResources(isDark);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal)
            {
                return intVal == 0;
            }
        }
        catch
        {
            // Default to dark on error
        }
        return false;
    }

    private static void ApplyThemeResources(bool isDark)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        var resources = app.Resources;

        if (isDark)
        {
            resources["AppBackground"] = System.Windows.Media.ColorConverter.ConvertFromString("#202020");
            resources["AppSurface"] = System.Windows.Media.ColorConverter.ConvertFromString("#2C2C2C");
            resources["AppCard"] = System.Windows.Media.ColorConverter.ConvertFromString("#2B2B2B");
            resources["AppCardHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#323232");
            resources["AppBorder"] = System.Windows.Media.ColorConverter.ConvertFromString("#383838");
            resources["AppTextPrimary"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppTextSecondary"] = System.Windows.Media.ColorConverter.ConvertFromString("#A0A0A0");
            resources["AppTextTertiary"] = System.Windows.Media.ColorConverter.ConvertFromString("#707070");
            resources["AppAccent"] = System.Windows.Media.ColorConverter.ConvertFromString("#0078D4");
            resources["AppAccentHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#187BD1");
            resources["AppDanger"] = System.Windows.Media.ColorConverter.ConvertFromString("#C42B1C");
            resources["AppSuccess"] = System.Windows.Media.ColorConverter.ConvertFromString("#107C41");
            resources["AppInputBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E");
            resources["AppSidebarBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#181818");
            resources["AppBadgeBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#333333");
        }
        else
        {
            resources["AppBackground"] = System.Windows.Media.ColorConverter.ConvertFromString("#F3F3F3");
            resources["AppSurface"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppCard"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppCardHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#F9F9F9");
            resources["AppBorder"] = System.Windows.Media.ColorConverter.ConvertFromString("#E5E5E5");
            resources["AppTextPrimary"] = System.Windows.Media.ColorConverter.ConvertFromString("#1B1B1B");
            resources["AppTextSecondary"] = System.Windows.Media.ColorConverter.ConvertFromString("#5E5E5E");
            resources["AppTextTertiary"] = System.Windows.Media.ColorConverter.ConvertFromString("#8A8A8A");
            resources["AppAccent"] = System.Windows.Media.ColorConverter.ConvertFromString("#0067C0");
            resources["AppAccentHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#005FB8");
            resources["AppDanger"] = System.Windows.Media.ColorConverter.ConvertFromString("#C42B1C");
            resources["AppSuccess"] = System.Windows.Media.ColorConverter.ConvertFromString("#0F7B0F");
            resources["AppInputBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppSidebarBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#EBEBEB");
            resources["AppBadgeBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#F0F0F0");
        }

        // Apply dark titlebar and corners to all open WPF windows
        foreach (Window window in app.Windows)
        {
            ApplyDwmAttributes(window, isDark);
        }
    }

    public static void ApplyDwmAttributes(Window window, bool isDark)
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. Immersive dark mode (Windows 10 19041+ & Windows 11)
            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // 2. Windows 11 Rounded corners (DWMWCP_ROUND = 2)
            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
        }
        catch
        {
            // Ignore on older Windows versions
        }
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
