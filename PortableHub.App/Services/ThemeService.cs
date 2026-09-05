using System.Windows;
using Wpf.Ui.Appearance;

namespace PortableHub.App.Services;

public class ThemeService
{
    private string _currentTheme = "System";

    public event EventHandler? ThemeChanged;

    public string CurrentTheme => _currentTheme;

    public void ApplyTheme(string theme)
    {
        _currentTheme = theme;

        switch (theme)
        {
            case "Dark":
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            case "Light":
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                break;
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsWindowsInDarkMode()
    {
        return ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark;
    }
}
