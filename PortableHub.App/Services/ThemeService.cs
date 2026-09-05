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
                UpdateDynamicThemeColors(true);
                break;
            case "Light":
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                UpdateDynamicThemeColors(false);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                UpdateDynamicThemeColors(IsWindowsInDarkMode());
                break;
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static void UpdateDynamicThemeColors(bool isDark)
    {
        var res = Application.Current?.Resources;
        if (res == null) return;

        System.Windows.Media.Color bg, surface, card, cardHover, secBg, border, textPrim, textSec, textTert, inputBg, sidebarBg;

        if (isDark)
        {
            bg = System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20);
            surface = System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B);
            card = System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B);
            cardHover = System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33);
            secBg = System.Windows.Media.Color.FromRgb(0x25, 0x25, 0x25);
            border = System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A);
            textPrim = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
            textSec = System.Windows.Media.Color.FromRgb(0xD4, 0xD4, 0xD8);
            textTert = System.Windows.Media.Color.FromRgb(0xA1, 0xA1, 0xAA);
            inputBg = System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x1E);
            sidebarBg = System.Windows.Media.Color.FromRgb(0x18, 0x18, 0x18);
        }
        else
        {
            bg = System.Windows.Media.Color.FromRgb(0xF7, 0xF7, 0xF7);
            surface = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
            card = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
            cardHover = System.Windows.Media.Color.FromRgb(0xF5, 0xF5, 0xF5);
            secBg = System.Windows.Media.Color.FromRgb(0xF2, 0xF2, 0xF2);
            border = System.Windows.Media.Color.FromRgb(0xE5, 0xE5, 0xE5);
            textPrim = System.Windows.Media.Color.FromRgb(0x18, 0x18, 0x1B);
            textSec = System.Windows.Media.Color.FromRgb(0x3F, 0x3F, 0x46);
            textTert = System.Windows.Media.Color.FromRgb(0x52, 0x52, 0x5B);
            inputBg = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
            sidebarBg = System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3);
        }

        res["AppBackground"] = bg;
        res["AppSurface"] = surface;
        res["AppCard"] = card;
        res["AppCardHover"] = cardHover;
        res["AppSecondaryBackground"] = secBg;
        res["AppBorder"] = border;
        res["AppTextPrimary"] = textPrim;
        res["AppTextSecondary"] = textSec;
        res["AppTextTertiary"] = textTert;
        res["AppInputBg"] = inputBg;
        res["AppSidebarBg"] = sidebarBg;

        res["BrushBackground"] = new System.Windows.Media.SolidColorBrush(bg);
        res["BrushSurface"] = new System.Windows.Media.SolidColorBrush(surface);
        res["BrushCard"] = new System.Windows.Media.SolidColorBrush(card);
        res["BrushCardHover"] = new System.Windows.Media.SolidColorBrush(cardHover);
        res["BrushSecondaryBackground"] = new System.Windows.Media.SolidColorBrush(secBg);
        res["BrushBorder"] = new System.Windows.Media.SolidColorBrush(border);
        res["BrushTextPrimary"] = new System.Windows.Media.SolidColorBrush(textPrim);
        res["BrushTextSecondary"] = new System.Windows.Media.SolidColorBrush(textSec);
        res["BrushTextTertiary"] = new System.Windows.Media.SolidColorBrush(textTert);
        res["BrushInputBg"] = new System.Windows.Media.SolidColorBrush(inputBg);
        res["BrushSidebarBg"] = new System.Windows.Media.SolidColorBrush(sidebarBg);
    }

    public static bool IsWindowsInDarkMode()
    {
        return ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark;
    }
}
