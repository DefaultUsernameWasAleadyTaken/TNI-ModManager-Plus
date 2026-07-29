using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace TniModManager.Localization;

/// <summary>Подмена словаря строк и код языка для ViewModels.</summary>
public static class LocalizationManager
{
    public const string En = "en";
    public const string Ru = "ru";

    private static readonly Uri EnUri = new("avares://TNI-ModManager-Plus/Localization/Strings.en.axaml");
    private static readonly Uri RuUri = new("avares://TNI-ModManager-Plus/Localization/Strings.ru.axaml");

    private static ResourceInclude? _currentInclude;

    public static string Current { get; private set; } = En;

    public static event Action? LanguageChanged;

    public static void Apply(string language)
    {
        var code = Normalize(language);
        Current = code;
        UiStrings.SetLanguage(code);

        if (Application.Current?.Resources.MergedDictionaries is { } merged)
        {
            if (_currentInclude is not null)
                merged.Remove(_currentInclude);

            _currentInclude = new ResourceInclude(new Uri("avares://TNI-ModManager-Plus/"))
            {
                Source = code == Ru ? RuUri : EnUri
            };
            merged.Add(_currentInclude);
        }

        LanguageChanged?.Invoke();
    }

    public static string Normalize(string? language) =>
        string.Equals(language, Ru, StringComparison.OrdinalIgnoreCase) ? Ru : En;

    public static string DisplayName(string? language) =>
        Normalize(language) == Ru ? "Русский" : "English";
}
