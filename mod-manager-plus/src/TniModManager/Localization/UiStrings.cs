using TniModManager.Core.Aliases;
using TniModManager.Core.Models;

namespace TniModManager.Localization;

/// <summary>Строки для статусов и enum-лейблов (AXAML берёт DynamicResource).</summary>
public static class UiStrings
{
    private static string _lang = LocalizationManager.En;

    public static void SetLanguage(string language) =>
        _lang = LocalizationManager.Normalize(language);

    private static bool Ru => _lang == LocalizationManager.Ru;

    public static string LanguageTooltip => Ru ? "Язык" : "Language";
    public static string ThemeToLight => Ru ? "Переключить на светлую тему" : "Switch to light theme";
    public static string ThemeToDark => Ru ? "Переключить на тёмную тему" : "Switch to dark theme";
    public static string Loading => Ru ? "Загрузка…" : "Loading...";
    public static string StartupError(string message) =>
        Ru ? $"Ошибка запуска: {message}" : $"Startup error: {message}";

    public static string AppUpdateButton(string version) =>
        Ru ? $"Обновление {version}" : $"Update {version}";

    public static string DownloadingAppUpdate(string version) =>
        Ru ? $"Скачивание обновления {version}…" : $"Downloading app update {version}...";

    public static string DownloadingAppUpdatePercent(double percent) =>
        Ru ? $"Скачивание обновления… {percent:0}%" : $"Downloading app update... {percent:0}%";

    public static string AutoUpdateFailed(string message) =>
        Ru
            ? $"Автообновление не удалось: {message}. Открываю страницу релиза."
            : $"Automatic update failed: {message}. Opening the release page.";

    public static string LaunchingGame => Ru ? "Запуск игры через Steam…" : "Launching game via Steam...";
    public static string LaunchFailed(string message) =>
        Ru ? $"Не удалось запустить: {message}" : $"Launch failed: {message}";

    public static string UpdateDownloadedClose =>
        Ru
            ? "Обновление скачано. Закройте приложение, чтобы применить и перезапустить."
            : "Update downloaded. Close the app to apply it and restart.";

    public static string UpdateInstalledRestart =>
        Ru
            ? "Обновление установлено. Перезапустите приложение."
            : "Update installed. Restart the app to use the new version.";

    public static string NoCompatibleAsset =>
        Ru ? "Не найден подходящий файл релиза." : "No compatible release asset was found.";

    public static string CannotLocateExecutable =>
        Ru ? "Не удалось найти исполняемый файл приложения." : "Cannot locate the running executable.";

    public static string ArchiveMissingExecutable(string name) =>
        Ru ? $"В архиве нет файла {name}." : $"The archive does not contain {name}.";

    public static string NoteLuajit =>
        Ru
            ? "Замечание: luajit-support не установлен — он нужен для Lua-модов."
            : "Note: luajit-support not installed — Lua mods need it.";

    public static string RefreshingMods => Ru ? "Обновление списка модов…" : "Refreshing mods...";
    public static string Removed(string name) => Ru ? $"Удалён {name}" : $"Removed {name}";
    public static string ConfigSaved => Ru ? "Конфигурация сохранена!" : "Configuration saved!";
    public static string ConfigSaveFailed => Ru ? "Не удалось сохранить конфигурацию." : "Failed to save configuration.";
    public static string DownloadingMod(string id) => Ru ? $"Скачивание {id}…" : $"Downloading {id}...";
    public static string DownloadingModPercent(string id, double percent) =>
        Ru ? $"Скачивание {id}… {percent:0}%" : $"Downloading {id}... {percent:0}%";
    public static string ModInstalled(string id, string version) =>
        Ru ? $"{id} v{version} установлен." : $"{id} v{version} installed.";
    public static string DownloadFailed(string message) =>
        Ru ? $"Ошибка загрузки: {message}" : $"Download failed: {message}";
    public static string LoadedReleases(int count) =>
        Ru ? $"Загружено релизов с GitHub: {count}." : $"Loaded {count} GitHub releases.";
    public static string GitHubUnavailable(string message) =>
        Ru
            ? $"GitHub недоступен: {message} (показаны локальные моды)"
            : $"GitHub unavailable: {message} (showing local mods)";

    public static string AliasesSaved => Ru ? "Алиасы сохранены." : "Aliases saved.";
    public static string EmptyPreview => Ru ? "(пусто)" : "(empty)";
    public static string UpdateAvailablePrefix => Ru ? "Доступно обновление · " : "Update available · ";

    public static string FormatModSource(ModSource source) => source switch
    {
        ModSource.Downloaded => Ru ? "Скачан" : "Downloaded",
        ModSource.Manual => Ru ? "Вручную" : "Manual",
        _ => Ru ? "Доступен" : "Available"
    };

    public static string FormatAliasKind(AliasKind kind) => kind switch
    {
        AliasKind.Variable => Ru ? "Переменная" : "Variable",
        AliasKind.Compound => Ru ? "Составной" : "Compound",
        AliasKind.Conditional => Ru ? "Условный" : "Conditional",
        AliasKind.Complex => Ru ? "Сложный" : "Complex",
        _ => Ru ? "Простой" : "Plain"
    };
}
