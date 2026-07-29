using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TniModManager.Core.Cache;
using TniModManager.Core.Config;
using TniModManager.Core.GitHub;
using TniModManager.Core.Models;
using TniModManager.Core.Mods;
using TniModManager.Core.Paths;
using TniModManager.Localization;

namespace TniModManager.ViewModels;

public partial class ModsViewModel : ViewModelBase
{
    private readonly GamePaths _paths;
    private readonly ModCacheStore _cache;
    private readonly ReleaseCatalogCache _releaseCache;
    private readonly GitHubReleaseClient _github;
    private readonly ModDiscovery _discovery;
    private readonly ModInstallService _install;
    private readonly IAppShell _shell;
    private List<ModInfo> _allMods = [];
    private Dictionary<string, ModReleaseInfo> _releases = new(StringComparer.OrdinalIgnoreCase);

    public ModsViewModel(
        GamePaths paths,
        ModCacheStore cache,
        GitHubReleaseClient github,
        ModDiscovery discovery,
        ModInstallService install,
        IAppShell shell)
    {
        _paths = paths;
        _cache = cache;
        _releaseCache = new ReleaseCatalogCache(paths);
        _github = github;
        _discovery = discovery;
        _install = install;
        _shell = shell;
    }

    public bool IsFilterAll => FilterMode == "All";
    public bool IsFilterInstalled => FilterMode == "Installed";
    public bool IsFilterAvailable => FilterMode == "Available";

    [ObservableProperty] private string _filterMode = "All";
    [ObservableProperty] private ModListItemViewModel? _selectedModItem;
    [ObservableProperty] private bool _hasSelectedMod;
    [ObservableProperty] private string _modNameText = "";
    [ObservableProperty] private string _modSourceText = "";
    [ObservableProperty] private IBrush _modSourceBrush = Brushes.Gray;
    [ObservableProperty] private string _modVersionBadgeText = "";
    [ObservableProperty] private string _modAuthorText = "";
    [ObservableProperty] private string _modGameVersionText = "";
    [ObservableProperty] private string _modLastUpdatedText = "";
    [ObservableProperty] private string _modDescriptionText = "";
    [ObservableProperty] private bool _showDownload;
    [ObservableProperty] private bool _showUpdate;
    [ObservableProperty] private bool _showRemove;
    [ObservableProperty] private bool _showGitHub;
    [ObservableProperty] private bool _showUpdateNotice;
    [ObservableProperty] private string _updateVersionText = "";
    [ObservableProperty] private string _updateNoticeHeader = "";
    [ObservableProperty] private string _updateNotesText = "";
    [ObservableProperty] private bool _showParameters;
    [ObservableProperty] private bool _hasUiConfigWarning;

    public ObservableCollection<ModListItemViewModel> VisibleMods { get; } = [];
    public ObservableCollection<ParamRowViewModel> ParameterRows { get; } = [];

    public Task LoadAsync()
    {
        LoadModsLocalOnly();
        if (!File.Exists(Path.Combine(_paths.ModsDirectory, "luajit-support", "entry.elf")))
            _shell.SetStatus(UiStrings.NoteLuajit);
        return Task.CompletedTask;
    }

    partial void OnSelectedModItemChanged(ModListItemViewModel? value) => ApplySelectedMod(value?.Mod);

    partial void OnFilterModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterInstalled));
        OnPropertyChanged(nameof(IsFilterAvailable));
        RebuildVisibleMods();
    }

    [RelayCommand]
    private void SetFilter(string? mode)
    {
        if (mode is "All" or "Installed" or "Available")
            FilterMode = mode;
    }

    /// <summary>Явное обновление каталога с GitHub (кнопка «Обновить»).</summary>
    [RelayCommand]
    public async Task RefreshModsAsync()
    {
        if (!_shell.TryEnterBusy())
            return;

        _shell.SetStatus(UiStrings.RefreshingMods);
        try
        {
            await FetchRemoteCatalogAsync().ConfigureAwait(true);
        }
        finally
        {
            _shell.ExitBusy();
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null || string.IsNullOrEmpty(mod.ZipUrl))
            return;

        if (!_releases.TryGetValue(mod.Id, out var release) &&
            (mod.FolderId is null || !_releases.TryGetValue(mod.FolderId, out release)))
        {
            release = new ModReleaseInfo
            {
                ModId = mod.Id,
                Version = mod.RemoteVersion ?? mod.Version,
                DownloadUrl = mod.ZipUrl
            };
        }

        await RunInstallAsync(release).ConfigureAwait(true);
    }

    [RelayCommand]
    private Task UpdateSelectedAsync() => DownloadSelectedAsync();

    [RelayCommand]
    private void RemoveSelected()
    {
        var mod = SelectedModItem?.Mod;
        if (mod?.FolderPath is null || mod.FolderId is null)
            return;

        _install.RemoveDownloaded(mod.FolderPath, mod.FolderId);
        _shell.SetStatus(UiStrings.Removed(mod.Name));
        LoadModsLocalOnly();
    }

    [RelayCommand]
    private void OpenModGitHub()
    {
        var url = SelectedModItem?.Mod.HtmlUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _shell.SetStatus(UiStrings.OpenUrlFailed(ex.Message));
        }
    }

    [RelayCommand]
    private void SaveParameters()
    {
        var mod = SelectedModItem?.Mod;
        if (mod?.FolderPath is null)
            return;

        var entry = Path.Combine(mod.FolderPath, GamePaths.ConfigFileName);
        var values = ParameterRows.ToDictionary(row => row.Name, row => row.GetValue(), StringComparer.Ordinal);
        _shell.SetStatus(EntryLuaConfig.Write(entry, values)
            ? UiStrings.ConfigSaved
            : UiStrings.ConfigSaveFailed);
    }

    [RelayCommand]
    private void ResetParameters()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null)
            return;

        ParameterRows.Clear();
        foreach (var parameter in mod.Parameters)
            ParameterRows.Add(ParamRowViewModel.FromDef(parameter, null));
    }

    public void RefreshThemeBrushes()
    {
        foreach (var mod in VisibleMods)
            mod.RefreshBrush();
        if (SelectedModItem is not null)
            ApplySelectedMod(SelectedModItem.Mod);
    }

    public void RefreshLocalizedLabels()
    {
        foreach (var mod in VisibleMods)
            mod.RefreshLocalizedLabels();
        if (SelectedModItem is not null)
            ApplySelectedMod(SelectedModItem.Mod);
    }

    private async Task RunInstallAsync(ModReleaseInfo release)
    {
        if (!_shell.TryEnterBusy())
            return;

        _shell.BeginProgress(UiStrings.DownloadingMod(release.ModId));
        var progress = new Progress<double>(percent =>
            _shell.ReportProgress(percent, UiStrings.DownloadingModPercent(release.ModId, percent)));
        try
        {
            await _install.InstallFromReleaseAsync(release, progress).ConfigureAwait(true);
            _shell.SetStatus(UiStrings.ModInstalled(release.ModId, release.Version));
            LoadModsLocalOnly(preserveStatus: true);
        }
        catch (Exception ex)
        {
            _shell.SetStatus(UiStrings.DownloadFailed(ex.Message));
        }
        finally
        {
            _shell.EndProgress();
            _shell.ExitBusy();
        }
    }

    /// <summary>Локальные моды + кэш каталога, без запроса к GitHub.</summary>
    private void LoadModsLocalOnly(bool preserveStatus = false)
    {
        _cache.Load();
        var installed = _discovery.GetInstalledMods();

        if (_releases.Count == 0)
            _releases = _releaseCache.Load();

        _allMods = _discovery.MergeWithReleases(installed, _releases);
        RebuildVisibleMods();

        if (preserveStatus)
            return;

        if (_releases.Count > 0)
            _shell.SetStatus(UiStrings.LoadedCachedReleases(_releases.Count, _releaseCache.SavedAt));
        else
            _shell.SetStatus(UiStrings.CatalogCacheEmpty);
    }

    private async Task FetchRemoteCatalogAsync()
    {
        _cache.Load();
        var installed = _discovery.GetInstalledMods();

        if (_releases.Count == 0)
            _releases = _releaseCache.Load();

        try
        {
            _releases = await _github.GetLatestModReleasesAsync().ConfigureAwait(true);
            _releaseCache.Save(_releases);
            _shell.SetStatus(UiStrings.LoadedReleases(_releases.Count));
        }
        catch (Exception ex)
        {
            if (_releases.Count == 0)
                _releases = _releaseCache.Load();

            _shell.SetStatus(_releases.Count > 0
                ? UiStrings.GitHubUnavailableCached(ex.Message, _releases.Count)
                : UiStrings.GitHubUnavailable(ex.Message));
        }

        _allMods = _discovery.MergeWithReleases(installed, _releases);
        RebuildVisibleMods();
    }

    private void RebuildVisibleMods()
    {
        IEnumerable<ModInfo> mods = FilterMode switch
        {
            "Installed" => _allMods.Where(mod => mod.Source != ModSource.Available),
            "Available" => _allMods.Where(mod => mod.Source == ModSource.Available),
            _ => _allMods
        };

        var selectedId = SelectedModItem?.Mod.Id;
        VisibleMods.Clear();
        foreach (var mod in mods)
            VisibleMods.Add(new ModListItemViewModel(mod));
        SelectedModItem = VisibleMods.FirstOrDefault(item => item.Mod.Id == selectedId);
    }

    private void ApplySelectedMod(ModInfo? mod)
    {
        HasSelectedMod = mod is not null;
        if (mod is null)
        {
            ModNameText = "";
            ModSourceText = "";
            ModSourceBrush = ThemeBrushResolver.Get("SourceAvailableBrush");
            ModVersionBadgeText = "";
            ModAuthorText = "";
            ModGameVersionText = "";
            ModLastUpdatedText = "";
            ModDescriptionText = "";
            ShowDownload = false;
            ShowUpdate = false;
            ShowRemove = false;
            ShowGitHub = false;
            ShowUpdateNotice = false;
            UpdateVersionText = "";
            UpdateNoticeHeader = "";
            UpdateNotesText = "";
            HasUiConfigWarning = false;
            ShowParameters = false;
            ParameterRows.Clear();
            return;
        }

        ModNameText = mod.Name;
        ModSourceText = UiStrings.FormatModSource(mod.Source);
        ModSourceBrush = ThemeBrushResolver.Get(mod.Source switch
        {
            ModSource.Downloaded => "SourceDownloadedBrush",
            ModSource.Manual => "SourceManualBrush",
            _ => "SourceAvailableBrush"
        });
        ModVersionBadgeText = string.IsNullOrEmpty(mod.Version) ? "" : $"v{mod.Version}";
        ModAuthorText = mod.Author;
        ModGameVersionText = mod.GameVersion;
        ModLastUpdatedText = mod.LastUpdated;
        ModDescriptionText = mod.Description;
        ShowDownload = mod.Source == ModSource.Available;
        ShowUpdate = mod.HasUpdate;
        ShowRemove = mod.Source == ModSource.Downloaded;
        ShowGitHub = !string.IsNullOrWhiteSpace(mod.HtmlUrl);
        ShowUpdateNotice = mod.HasUpdate;
        UpdateVersionText = mod.RemoteVersion ?? "";
        UpdateNoticeHeader = UiStrings.UpdateAvailablePrefix + UpdateVersionText;
        UpdateNotesText = mod.RemoteNotes ?? "";
        HasUiConfigWarning = mod.HasUiConfigPs1;

        ParameterRows.Clear();
        ShowParameters = mod.Source != ModSource.Available &&
                         mod.FolderPath is not null &&
                         mod.Parameters.Count > 0;
        if (!ShowParameters || mod.FolderPath is null)
            return;

        var current = EntryLuaConfig.Read(Path.Combine(mod.FolderPath, GamePaths.ConfigFileName));
        foreach (var parameter in mod.Parameters)
        {
            current.TryGetValue(parameter.Name, out var value);
            ParameterRows.Add(ParamRowViewModel.FromDef(parameter, value));
        }
    }
}
