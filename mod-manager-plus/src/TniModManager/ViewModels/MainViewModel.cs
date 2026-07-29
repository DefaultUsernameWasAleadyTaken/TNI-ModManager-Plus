using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TniModManager.Core.Cache;
using TniModManager.Core.GitHub;
using TniModManager.Core.Mods;
using TniModManager.Core.Paths;
using TniModManager.Core.Settings;
using TniModManager.Core.Util;
using TniModManager.Localization;

namespace TniModManager.ViewModels;

public partial class MainViewModel : ViewModelBase, IAppShell
{
    private readonly GamePaths _paths;
    private readonly ModCacheStore _cache;
    private readonly GitHubReleaseClient _github;
    private readonly GameSettingsStore _settings;
    private readonly AppUiSettings _uiSettings;
    private AppReleaseInfo? _appRelease;

    public MainViewModel(AppUiSettings? uiSettings = null)
    {
        _paths = GamePaths.Create();
        _cache = new ModCacheStore(_paths);
        _github = new GitHubReleaseClient();
        var discovery = new ModDiscovery(_paths, _cache);
        var install = new ModInstallService(_paths, _cache, _github);
        _settings = new GameSettingsStore(_paths);
        _uiSettings = uiSettings ?? new AppUiSettings(_paths);
        if (uiSettings is null)
            _uiSettings.Load();

        Mods = new ModsViewModel(_paths, _cache, _github, discovery, install, this);
        Aliases = new AliasesViewModel(_settings, this);
        WindowTitle = $"{GamePaths.AppDisplayName} v{GamePaths.ModManagerVersion}";
        StatusText = UiStrings.Loading;
        IsDarkTheme = !_uiSettings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        LanguageLabel = LocalizationManager.DisplayName(_uiSettings.Language);
        LanguageCode = LocalizationManager.Normalize(_uiSettings.Language);
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    public ModsViewModel Mods { get; }
    public AliasesViewModel Aliases { get; }
    public string AppVersionBadgeText => $"v{GamePaths.ModManagerVersion}";
    public string ThemeToggleTooltip => IsDarkTheme ? UiStrings.ThemeToLight : UiStrings.ThemeToDark;
    public string AppUpdateButtonLabel =>
        string.IsNullOrWhiteSpace(AppUpdateVersion) ? "" : UiStrings.AppUpdateButton(AppUpdateVersion);

    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private string _downloadStatusText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _languageLabel = "English";
    [ObservableProperty] private string _languageCode = "en";
    [ObservableProperty] private bool _showAppUpdate;
    [ObservableProperty] private string _appUpdateVersion = "";

    public async Task InitializeAsync()
    {
        try
        {
            _paths.EnsureDirectories();
            _cache.Load();
            _settings.Load();
            Aliases.Load();
            _ = CheckAppUpdateAsync();
            await Mods.LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(UiStrings.StartupError(ex.Message));
        }
    }

    partial void OnIsDarkThemeChanged(bool value) => OnPropertyChanged(nameof(ThemeToggleTooltip));

    partial void OnAppUpdateVersionChanged(string value) => OnPropertyChanged(nameof(AppUpdateButtonLabel));

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _uiSettings.Theme = IsDarkTheme ? "Dark" : "Light";
        if (Application.Current is { } app)
            app.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        _uiSettings.Save();
        RefreshThemeBrushes();
    }

    [RelayCommand]
    private void SelectLanguage(string? language)
    {
        var code = LocalizationManager.Normalize(language);
        _uiSettings.Language = code;
        LanguageLabel = LocalizationManager.DisplayName(code);
        LanguageCode = code;
        _uiSettings.Save();
        LocalizationManager.Apply(code);
    }

    [RelayCommand]
    private async Task CheckAppUpdateAsync()
    {
        try
        {
            var release = await _github.GetLatestAppReleaseAsync().ConfigureAwait(true);
            if (!SemVer.IsNewer(release.Version, GamePaths.ModManagerVersion))
                return;
            _appRelease = release;
            AppUpdateVersion = release.Version;
            ShowAppUpdate = true;
        }
        catch
        {
            // Проверка обновления не должна мешать загрузке библиотеки модов.
        }
    }

    [RelayCommand]
    private async Task UpdateAppAsync()
    {
        if (!TryEnterBusy())
            return;
        BeginProgress(UiStrings.DownloadingAppUpdate(AppUpdateVersion));
        try
        {
            _appRelease ??= await _github.GetLatestAppReleaseAsync().ConfigureAwait(true);
            var asset = SelectPlatformAsset(_appRelease)
                ?? throw new InvalidOperationException(UiStrings.NoCompatibleAsset);
            var downloadPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{asset.Name}");
            var progress = new Progress<double>(percent =>
                ReportProgress(percent, UiStrings.DownloadingAppUpdatePercent(percent)));
            await _github.DownloadFileAsync(asset.DownloadUrl, downloadPath, progress).ConfigureAwait(true);
            InstallAppUpdate(ExtractUpdatePayload(downloadPath, asset.Name));
            ShowAppUpdate = false;
        }
        catch (Exception ex)
        {
            SetStatus(UiStrings.AutoUpdateFailed(ex.Message));
            OpenLatestReleasePage();
        }
        finally
        {
            EndProgress();
            ExitBusy();
        }
    }

    [RelayCommand]
    private void LaunchGame()
    {
        try
        {
            GameLauncher.LaunchSteamGame();
            SetStatus(UiStrings.LaunchingGame);
        }
        catch (Exception ex)
        {
            SetStatus(UiStrings.LaunchFailed(ex.Message));
        }
    }

    [RelayCommand]
    private static void ExitApp() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    public void SetStatus(string text) => RunOnUiThread(() => StatusText = text);

    public bool TryEnterBusy()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(TryEnterBusy).GetAwaiter().GetResult();
        if (IsBusy)
            return false;
        IsBusy = true;
        return true;
    }

    public void ExitBusy() => RunOnUiThread(() => IsBusy = false);

    public void BeginProgress(string statusText) => RunOnUiThread(() =>
    {
        IsProgressVisible = true;
        DownloadProgress = 0;
        DownloadStatusText = statusText;
    });

    public void ReportProgress(double percent, string statusText) => RunOnUiThread(() =>
    {
        DownloadProgress = percent;
        DownloadStatusText = statusText;
    });

    public void EndProgress() => RunOnUiThread(() => IsProgressVisible = false);

    private static void RunOnUiThread(Action action) =>
        (Dispatcher.UIThread.CheckAccess() ? action : () => Dispatcher.UIThread.Post(action))();

    private void RefreshThemeBrushes()
    {
        Mods.RefreshThemeBrushes();
        Aliases.RefreshThemeBrushes();
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e) => RefreshThemeBrushes();

    private void OnLanguageChanged()
    {
        RunOnUiThread(() =>
        {
            LanguageLabel = LocalizationManager.DisplayName(LocalizationManager.Current);
            LanguageCode = LocalizationManager.Current;
            OnPropertyChanged(nameof(ThemeToggleTooltip));
            OnPropertyChanged(nameof(AppUpdateButtonLabel));
            Mods.RefreshLocalizedLabels();
            Aliases.RefreshLocalizedLabels();
        });
    }

    private static AppReleaseAsset? SelectPlatformAsset(AppReleaseInfo release)
    {
        var runtime = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" : "linux-x64";
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "TNI-ModManager-Plus.exe" : "TNI-ModManager-Plus";
        return release.Assets
            .Where(asset => asset.Name.Contains("TNI-ModManager-Plus", StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.Name.Contains(runtime, StringComparison.OrdinalIgnoreCase) || asset.Name.Equals(executableName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(asset => asset.Name.Contains(runtime, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    private static string ExtractUpdatePayload(string downloadPath, string assetName)
    {
        if (!assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return downloadPath;
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "TNI-ModManager-Plus.exe" : "TNI-ModManager-Plus";
        using var archive = ZipFile.OpenRead(downloadPath);
        var entry = archive.Entries.FirstOrDefault(item => Path.GetFileName(item.FullName).Equals(executableName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException(UiStrings.ArchiveMissingExecutable(executableName));
        var payloadPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{executableName}");
        entry.ExtractToFile(payloadPath, true);
        return payloadPath;
    }

    private void InstallAppUpdate(string payloadPath)
    {
        var targetPath = Environment.ProcessPath
            ?? throw new InvalidOperationException(UiStrings.CannotLocateExecutable);
        var workDir = Path.GetDirectoryName(targetPath) ?? Environment.CurrentDirectory;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var stagedPath = targetPath + ".update";
            File.Copy(payloadPath, stagedPath, true);
            var scriptPath = Path.Combine(Path.GetTempPath(), $"tni-mm-update-{Guid.NewGuid():N}.cmd");
            var pid = Environment.ProcessId;
            // Wait-Process + start /B: updater не умирает вместе с приложением.
            File.WriteAllText(scriptPath, $"""
                @echo off
                powershell -NoProfile -ExecutionPolicy Bypass -Command "Wait-Process -Id {pid} -ErrorAction SilentlyContinue"
                set "TRIES=0"
                :retry
                set /a TRIES+=1
                move /Y "{stagedPath}" "{targetPath}" >nul 2>&1
                if errorlevel 1 if %TRIES% LSS 15 (
                  timeout /t 1 /nobreak >nul
                  goto retry
                )
                cd /d "{workDir}"
                start "" "{targetPath}"
                del "%~f0"
                """);
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // start /B — отдельный процесс вне дерева MM, иначе Shutdown убивает updater
                Arguments = $"/C start \"TNI-MM-Update\" /B cmd.exe /C \"\"{scriptPath}\"\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            });
            SetStatus(UiStrings.UpdateRestarting);
            ScheduleAppShutdown();
            return;
        }

        var mode = File.GetUnixFileMode(targetPath);
        var stagedLinuxPath = targetPath + ".update";
        File.Copy(payloadPath, stagedLinuxPath, true);
        File.SetUnixFileMode(stagedLinuxPath, mode);
        File.Move(stagedLinuxPath, targetPath, true);
        Process.Start(new ProcessStartInfo
        {
            FileName = targetPath,
            WorkingDirectory = workDir,
            UseShellExecute = true
        });
        SetStatus(UiStrings.UpdateRestarting);
        ScheduleAppShutdown();
    }

    /// <summary>Закрыть UI после старта updater/нового процесса, чтобы подмена exe прошла.</summary>
    private static void ScheduleAppShutdown()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
            else
                Environment.Exit(0);
        }, DispatcherPriority.Background);
    }

    private static void OpenLatestReleasePage() => Process.Start(new ProcessStartInfo(
        $"https://github.com/{GamePaths.AppGitHubRepo}/releases/latest") { UseShellExecute = true });
}
