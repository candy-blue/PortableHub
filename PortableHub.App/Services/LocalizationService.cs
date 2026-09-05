using System.Globalization;
using System.Windows;
using PortableHub.Core.Interfaces;

namespace PortableHub.App.Services;

public class LocalizationService : ILocalizationService
{
    private const string DefaultCulture = "zh-CN";
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public string CurrentLanguage { get; private set; } = DefaultCulture;

    public IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("zh-CN", "简体中文 (Simplified Chinese)", "简体中文"),
        new("zh-TW", "繁體中文 (Traditional Chinese)", "繁體中文"),
        new("en-US", "English (US)", "English")
    ];

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        _instance = this;
    }

    public void SetLanguage(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            cultureCode = DefaultCulture;
        }

        // Normalize
        cultureCode = cultureCode.Trim();
        if (!SupportedLanguages.Any(l => l.Code.Equals(cultureCode, StringComparison.OrdinalIgnoreCase)))
        {
            cultureCode = DefaultCulture;
        }

        CurrentLanguage = cultureCode;

        try
        {
            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // Fallback gracefully
        }

        ApplyDictionary(cultureCode);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key, string? fallback = null)
    {
        if (Application.Current != null)
        {
            if (Application.Current.TryFindResource(key) is string val)
            {
                return val;
            }
        }
        return fallback ?? key;
    }

    private void ApplyDictionary(string cultureCode)
    {
        if (Application.Current == null) return;

        var dictUri = new Uri($"pack://application:,,,/PortableHub.App;component/Themes/Localization/Strings.{cultureCode}.xaml", UriKind.Absolute);

        try
        {
            var newDict = new ResourceDictionary { Source = dictUri };

            // Find existing localization dictionary
            ResourceDictionary? existing = null;
            foreach (var md in Application.Current.Resources.MergedDictionaries)
            {
                if (md.Source != null && md.Source.OriginalString.Contains("/Strings."))
                {
                    existing = md;
                    break;
                }
            }

            if (existing != null)
            {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(existing);
                Application.Current.Resources.MergedDictionaries[index] = newDict;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }
        }
        catch
        {
            // In unit test or design mode where pack URIs may not be fully registered
        }
    }
}
