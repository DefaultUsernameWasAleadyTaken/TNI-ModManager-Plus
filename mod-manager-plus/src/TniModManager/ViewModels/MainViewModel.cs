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
        StatusText = "Loading...";
        IsDarkTheme = !_uiSettings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        LanguageLabel = _uiSettings.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "English" : _uiSettings.Language;
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }
    public ModsViewModel Mods { get; }
    public AliasesViewModel Aliases { get; }
    public string AppVersionBadgeText => $"v{GamePaths.ModManagerVersion}";
    public string ThemeToggleTooltip => IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";
    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private string _downloadStatusText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _languageLabel = "English";
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
            SetStatus($"Startup error: {ex.Message}");
        }
    }
    partial void OnIsDarkThemeChanged(bool value) => OnPropertyChanged(nameof(ThemeToggleTooltip));
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
        if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            return;
        _uiSettings.Language = "en";
        LanguageLabel = "English";
        _uiSettings.Save();
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
        BeginProgress($"Downloading app update {AppUpdateVersion}...");
        try
        {
            _appRelease ??= await _github.GetLatestAppReleaseAsync().ConfigureAwait(true);
            var asset = SelectPlatformAsset(_appRelease)
                ?? throw new InvalidOperationException("No compatible release asset was found.");
            var downloadPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{asset.Name}");
            var progress = new Progress<double>(percent =>
                ReportProgress(percent, $"Downloading app update... {percent:0}%"));
            await _github.DownloadFileAsync(asset.DownloadUrl, downloadPath, progress).ConfigureAwait(true);
            InstallAppUpdate(ExtractUpdatePayload(downloadPath, asset.Name));
            ShowAppUpdate = false;
        }
        catch (Exception ex)
        {
            SetStatus($"Automatic update failed: {ex.Message}. Opening the release page.");
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
            SetStatus("Launching game via Steam...");
        }
        catch (Exception ex)
        {
            SetStatus($"Launch failed: {ex.Message}");
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
    private void RefreshThemeBrushes() { Mods.RefreshThemeBrushes(); Aliases.RefreshThemeBrushes(); }
    private void OnActualThemeVariantChanged(object? sender, EventArgs e) => RefreshThemeBrushes();
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
            ?? throw new InvalidDataException($"The archive does not contain {executableName}.");
        var payloadPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{executableName}");
        entry.ExtractToFile(payloadPath, true);
        return payloadPath;
    }
    private void InstallAppUpdate(string payloadPath)
    {
        var targetPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot locate the running executable.");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var stagedPath = targetPath + ".update";
            File.Copy(payloadPath, stagedPath, true);
            var scriptPath = Path.Combine(Path.GetTempPath(), $"tni-mm-update-{Guid.NewGuid():N}.cmd");
            File.WriteAllText(scriptPath, $"""
                @echo off
                :wait
                tasklist /FI "PID eq {Environment.ProcessId}" | find "{Environment.ProcessId}" >nul
                if not errorlevel 1 (
                  timeout /t 1 /nobreak >nul
                  goto wait
                )
                move /Y "{stagedPath}" "{targetPath}" >nul
                start "" "{targetPath}"
                del "%~f0"
                """);
            Process.Start(new ProcessStartInfo("cmd.exe", $"/C \"{scriptPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            SetStatus("Update downloaded. Close the app to apply it and restart.");
            return;
        }
        var mode = File.GetUnixFileMode(targetPath);
        var stagedLinuxPath = targetPath + ".update";
        File.Copy(payloadPath, stagedLinuxPath, true);
        File.SetUnixFileMode(stagedLinuxPath, mode);
        File.Move(stagedLinuxPath, targetPath, true);
        SetStatus("Update installed. Restart the app to use the new version.");
    }
    private static void OpenLatestReleasePage() => Process.Start(new ProcessStartInfo(
        $"https://github.com/{GamePaths.AppGitHubRepo}/releases/latest") { UseShellExecute = true });
}
