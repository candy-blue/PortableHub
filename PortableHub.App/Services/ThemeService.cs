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
            resources["AppBackground"] = System.Windows.Media.ColorConverter.ConvertFromString("#18181B");
            resources["AppSurface"] = System.Windows.Media.ColorConverter.ConvertFromString("#27272A");
            resources["AppCard"] = System.Windows.Media.ColorConverter.ConvertFromString("#202023");
            resources["AppCardHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#2E2E33");
            resources["AppBorder"] = System.Windows.Media.ColorConverter.ConvertFromString("#3F3F46");
            resources["AppTextPrimary"] = System.Windows.Media.ColorConverter.ConvertFromString("#F4F4F5");
            resources["AppTextSecondary"] = System.Windows.Media.ColorConverter.ConvertFromString("#A1A1AA");
            resources["AppTextTertiary"] = System.Windows.Media.ColorConverter.ConvertFromString("#71717A");
            resources["AppAccent"] = System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6");
            resources["AppAccentHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#2563EB");
            resources["AppDanger"] = System.Windows.Media.ColorConverter.ConvertFromString("#EF4444");
            resources["AppSuccess"] = System.Windows.Media.ColorConverter.ConvertFromString("#10B981");
            resources["AppInputBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#222226");
            resources["AppSidebarBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#141416");
        }
        else
        {
            resources["AppBackground"] = System.Windows.Media.ColorConverter.ConvertFromString("#F4F5F7");
            resources["AppSurface"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppCard"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppCardHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#F0F1F3");
            resources["AppBorder"] = System.Windows.Media.ColorConverter.ConvertFromString("#E4E4E7");
            resources["AppTextPrimary"] = System.Windows.Media.ColorConverter.ConvertFromString("#18181B");
            resources["AppTextSecondary"] = System.Windows.Media.ColorConverter.ConvertFromString("#71717A");
            resources["AppTextTertiary"] = System.Windows.Media.ColorConverter.ConvertFromString("#A1A1AA");
            resources["AppAccent"] = System.Windows.Media.ColorConverter.ConvertFromString("#2563EB");
            resources["AppAccentHover"] = System.Windows.Media.ColorConverter.ConvertFromString("#1D4ED8");
            resources["AppDanger"] = System.Windows.Media.ColorConverter.ConvertFromString("#DC2626");
            resources["AppSuccess"] = System.Windows.Media.ColorConverter.ConvertFromString("#059669");
            resources["AppInputBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
            resources["AppSidebarBg"] = System.Windows.Media.ColorConverter.ConvertFromString("#ECEEF2");
        }
    }
}
