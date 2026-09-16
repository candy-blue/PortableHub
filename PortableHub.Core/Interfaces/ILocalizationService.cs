namespace PortableHub.Core.Interfaces;

public record LanguageOption(string Code, string DisplayName, string NativeName);

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }
    void SetLanguage(string cultureCode);
    string GetString(string key, string? fallback = null);
    string GetFormattedString(string key, string fallbackFormat, params object[] args);
    event EventHandler? LanguageChanged;
}
