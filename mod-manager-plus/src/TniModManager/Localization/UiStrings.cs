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

    public static string UpdateRestarting =>
        Ru
            ? "Обновление установлено. Перезапуск…"
            : "Update installed. Restarting...";

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

    public static string LoadedCachedReleases(int count, DateTimeOffset? savedAt)
    {
        if (savedAt is null)
        {
            return Ru
                ? $"Каталог из кэша: {count} мод. (Обновить — с GitHub)"
                : $"Cached catalog: {count} mods. (Refresh fetches GitHub)";
        }

        var local = savedAt.Value.ToLocalTime().ToString("g");
        return Ru
            ? $"Каталог из кэша: {count} мод. (сохранён {local}; Обновить — с GitHub)"
            : $"Cached catalog: {count} mods (saved {local}; Refresh fetches GitHub)";
    }

    public static string CatalogCacheEmpty =>
        Ru
            ? "Кэш каталога пуст. Нажмите «Обновить», чтобы загрузить моды с GitHub."
            : "Catalog cache is empty. Press Refresh to load mods from GitHub.";

    public static string GitHubUnavailable(string message) =>
        Ru
            ? $"GitHub недоступен: {message} (показаны локальные моды)"
            : $"GitHub unavailable: {message} (showing local mods)";

    public static string GitHubUnavailableCached(string message, int count) =>
        Ru
            ? $"GitHub недоступен: {message} (показан сохранённый каталог, {count} мод.)"
            : $"GitHub unavailable: {message} (showing cached catalog, {count} mods)";

    public static string AliasesSaved => Ru ? "Алиасы сохранены." : "Aliases saved.";
    public static string EmptyPreview => Ru ? "(пусто)" : "(empty)";
    public static string AliasPreviewPlaceholder =>
        Ru ? "Введите команду выше…" : "Enter a command above...";
    public static string AliasNameRequired =>
        Ru ? "Укажите имя алиаса." : "Please enter an alias name.";
    public static string AliasNameReserved(string name) =>
        Ru
            ? $"«{name}» зарезервировано и нельзя использовать как имя алиаса."
            : $"'{name}' is reserved and cannot be used as an alias name.";
    public static string AliasNameDuplicate =>
        Ru ? "Имя алиаса уже используется." : "That alias name is already in use.";

    public static string AliasStepLabel(int current, int total, string segment) =>
        Ru ? $"Шаг {current}/{total} · {segment}" : $"Step {current}/{total} · {segment}";

    public static string AliasHelpStepHeading(int current, int total) =>
        Ru ? $"Справка · шаг {current}/{total}" : $"Manual · step {current}/{total}";

    public static string AliasManualHeading =>
        Ru ? "Справка" : "Manual";

    public static string AliasContextHelp =>
        Ru ? "Справка по токену" : "Token help";
    public static string AliasCompletion =>
        Ru ? "Автодополнение" : "Autocomplete";

    public static string FormatCompletionKind(AliasCompletionKind kind) => kind switch
    {
        AliasCompletionKind.Command => Ru ? "команда" : "cmd",
        AliasCompletionKind.Program => Ru ? "программа" : "prog",
        AliasCompletionKind.Keyword => Ru ? "ключ" : "kw",
        AliasCompletionKind.DeviceType => Ru ? "устройство" : "device",
        AliasCompletionKind.Traffic => Ru ? "трафик" : "traffic",
        AliasCompletionKind.UserAlias => Ru ? "алиас" : "alias",
        _ => kind.ToString()
    };

    public static string AliasArgsRequired(int count, string variables) =>
        Ru
            ? $"Этому алиасу нужно аргументов: {count} — {variables}"
            : $"This alias requires {count} argument(s): {variables}";

    public static string AliasDeviceNotice(bool needOn, bool needUsing)
    {
        var parts = new List<string>();
        if (needOn)
            parts.Add(Ru ? "'on <адрес устройства>'" : "'on <device address>'");
        if (needUsing)
            parts.Add(Ru ? "'using <адрес отладчика>'" : "'using <debugger address>'");

        var joined = string.Join(Ru ? " и/или " : " and/or ", parts);
        return Ru
            ? $"Командам нужен суффикс {joined}, если игрок не задал 'always on' / 'always using'."
            : $"Commands require {joined} suffix unless 'always on' or 'always using' is set by the player.";
    }

    public static string OpenUrlFailed(string message) =>
        Ru ? $"Не удалось открыть ссылку: {message}" : $"Could not open link: {message}";

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
