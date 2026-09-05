namespace PortableHub.Core.Interfaces;

public record LanguageOption(string Code, string DisplayName, string NativeName);

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }
    void SetLanguage(string cultureCode);
    string GetString(string key, string? fallback = null);
    event EventHandler? LanguageChanged;
}
