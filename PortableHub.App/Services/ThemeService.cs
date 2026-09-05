using System.ComponentModel;
using System.Windows;
using iNKORE.UI.WPF.Modern;

namespace PortableHub.App.Services;

public class ThemeService
{
    private string _currentTheme = "System";

    public event EventHandler? ThemeChanged;

    public string CurrentTheme => _currentTheme;

    public ThemeService()
    {
        // Observe actual system theme changes when "System" theme mode is selected (Section 27)
        var descriptor = DependencyPropertyDescriptor.FromProperty(ThemeManager.ActualApplicationThemeProperty, typeof(ThemeManager));
        descriptor?.AddValueChanged(ThemeManager.Current, (s, e) =>
        {
            if (_currentTheme == "System")
            {
                UpdateDynamicThemeColors(IsWindowsInDarkMode());
                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public void ApplyTheme(string theme)
    {
        _currentTheme = theme;

        switch (theme)
        {
            case "Dark":
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                UpdateDynamicThemeColors(true);
                break;
            case "Light":
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                UpdateDynamicThemeColors(false);
                break;
            default:
                ThemeManager.Current.ApplicationTheme = null;
                UpdateDynamicThemeColors(IsWindowsInDarkMode());
                break;
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static void UpdateDynamicThemeColors(bool isDark)
    {
        var res = Application.Current?.Resources;
        if (res == null) return;

        System.Windows.Media.Color bg, surface, card, cardHover, secBg, border, controlBorder, textPrim, textSec, textTert, inputBg, sidebarBg;

        if (isDark)
        {
            bg = System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20);
            surface = System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B);
            card = System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B);
            cardHover = System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33);
            secBg = System.Windows.Media.Color.FromRgb(0x25, 0x25, 0x25);
            border = System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A);
            controlBorder = System.Windows.Media.Color.FromRgb(0x70, 0x70, 0x70);
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
            controlBorder = System.Windows.Media.Color.FromRgb(0x8A, 0x88, 0x86);
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
        res["AppControlBorder"] = controlBorder;
        res["AppTextPrimary"] = textPrim;
        res["AppTextSecondary"] = textSec;
        res["AppTextTertiary"] = textTert;
        res["AppInputBg"] = inputBg;
        res["AppSidebarBg"] = sidebarBg;

        var brushBg = new System.Windows.Media.SolidColorBrush(bg);
        var brushSurface = new System.Windows.Media.SolidColorBrush(surface);
        var brushCard = new System.Windows.Media.SolidColorBrush(card);
        var brushCardHover = new System.Windows.Media.SolidColorBrush(cardHover);
        var brushSecBg = new System.Windows.Media.SolidColorBrush(secBg);
        var brushBorder = new System.Windows.Media.SolidColorBrush(border);
        var brushControlBorder = new System.Windows.Media.SolidColorBrush(controlBorder);
        var brushTextPrim = new System.Windows.Media.SolidColorBrush(textPrim);
        var brushTextSec = new System.Windows.Media.SolidColorBrush(textSec);
        var brushTextTert = new System.Windows.Media.SolidColorBrush(textTert);
        var brushInputBg = new System.Windows.Media.SolidColorBrush(inputBg);
        var brushSidebarBg = new System.Windows.Media.SolidColorBrush(sidebarBg);

        res["BrushBackground"] = brushBg;
        res["BrushSurface"] = brushSurface;
        res["BrushCard"] = brushCard;
        res["BrushCardHover"] = brushCardHover;
        res["BrushSecondaryBackground"] = brushSecBg;
        res["BrushBorder"] = brushBorder;
        res["BrushTextPrimary"] = brushTextPrim;
        res["BrushTextSecondary"] = brushTextSec;
        res["BrushTextTertiary"] = brushTextTert;
        res["BrushInputBg"] = brushInputBg;
        res["BrushSidebarBg"] = brushSidebarBg;

        // Design Token Brush Aliases (Section 16)
        res["AppBackgroundBrush"] = brushBg;
        res["AppSurfaceBrush"] = brushSurface;
        res["AppCardBrush"] = brushCard;
        res["AppCardHoverBrush"] = brushCardHover;
        res["AppSecondaryBackgroundBrush"] = brushSecBg;
        res["AppBorderBrush"] = brushBorder;
        res["AppControlBorderBrush"] = brushControlBorder;
        res["ControlStrokeColorDefaultBrush"] = brushControlBorder;
        res["AppPrimaryTextBrush"] = brushTextPrim;
        res["AppSecondaryTextBrush"] = brushTextSec;
        res["AppTertiaryTextBrush"] = brushTextTert;

        // Ensure accent tokens are always available and synchronized
        var accentColor = System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4);
        var accentHoverColor = System.Windows.Media.Color.FromRgb(0x18, 0x7B, 0xD1);
        var brushAccent = new System.Windows.Media.SolidColorBrush(accentColor);
        var brushAccentHover = new System.Windows.Media.SolidColorBrush(accentHoverColor);
        res["AppAccent"] = accentColor;
        res["AppAccentBrush"] = brushAccent;
        res["BrushAccent"] = brushAccent;
        res["SystemAccentColorPrimaryBrush"] = brushAccent;
        res["AppAccentHover"] = accentHoverColor;
        res["AppAccentHoverBrush"] = brushAccentHover;
        res["BrushAccentHover"] = brushAccentHover;
        res["SystemAccentColorSecondaryBrush"] = brushAccentHover;

        // Text fill color brush aliases
        res["TextFillColorPrimaryBrush"] = brushTextPrim;
        res["TextFillColorSecondaryBrush"] = brushTextSec;
        res["TextFillColorTertiaryBrush"] = brushTextTert;

        var favColor = System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B);
        res["AppFavorite"] = favColor;
        var favBrush = new System.Windows.Media.SolidColorBrush(favColor);
        res["BrushFavorite"] = favBrush;
        res["AppFavoriteBrush"] = favBrush;

        // Card and stroke brushes with high-contrast surfaces for both themes
        res["CardBackgroundFillColorDefaultBrush"] = brushCard;
        res["CardBackgroundFillColorSecondaryBrush"] = brushSecBg;
        res["CardStrokeColorDefaultBrush"] = brushBorder;

        // Ensure text and icons on primary accent buttons are always pure crisp white in both light and dark modes
        res["TextOnAccentFillColorPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        res["TextOnAccentFillColorSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF));
    }

    public static bool IsWindowsInDarkMode()
    {
        return ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
    }
}
