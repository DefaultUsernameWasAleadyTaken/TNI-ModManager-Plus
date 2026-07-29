using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TniModManager.Core.Aliases;
using TniModManager.Core.Cache;
using TniModManager.Core.Config;
using TniModManager.Core.GitHub;
using TniModManager.Core.Models;
using TniModManager.Core.Mods;
using TniModManager.Core.Paths;
using TniModManager.Core.Settings;

namespace TniModManager.ViewModels;

public partial class ModListItemViewModel : ObservableObject
{
    public ModInfo Mod { get; }

    public ModListItemViewModel(ModInfo mod) => Mod = mod;

    public string DisplayName => Mod.Name;
    public string StatusIcon => Mod.Source switch
    {
        ModSource.Available => "DL",
        _ when Mod.IsEnabled => "✓",
        _ => "−"
    };
    public string SourceLabel => Mod.Source.ToString();
    public IBrush BorderBrush => new SolidColorBrush(Color.Parse(Mod.Source switch
    {
        ModSource.Downloaded => "#0078D4",
        ModSource.Manual => "#9C27B0",
        _ => "#607D8B"
    }));
    public bool ShowUpdateBadge => Mod.HasUpdate;
}

public partial class AliasListItemViewModel : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _command;

    public AliasListItemViewModel(string name, string command)
    {
        _name = name;
        _command = command;
    }

    public AliasKind Kind => AliasAnalyzer.Analyze(Command);
    public string KindLabel => Kind.ToString();
    public IBrush KindBrush => new SolidColorBrush(Color.Parse(AliasAnalyzer.KindColor(Kind)));
}

public partial class MainViewModel : ViewModelBase
{
    private readonly GamePaths _paths = GamePaths.Create();
    private readonly ModCacheStore _cache;
    private readonly GitHubReleaseClient _github = new();
    private readonly ModDiscovery _discovery;
    private readonly ModInstallService _install;
    private readonly GameSettingsStore _settings;
    private List<ModInfo> _allMods = [];
    private Dictionary<string, ModReleaseInfo> _releases = new(StringComparer.OrdinalIgnoreCase);

    public MainViewModel()
    {
        _cache = new ModCacheStore(_paths);
        _discovery = new ModDiscovery(_paths, _cache);
        _install = new ModInstallService(_paths, _cache, _github);
        _settings = new GameSettingsStore(_paths);
        WindowTitle = $"Tower Networking Inc - Mod Manager Plus v{GamePaths.ModManagerVersion}";
        StatusText = "Loading...";
    }

    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private string _downloadStatusText = "";
    [ObservableProperty] private string _filterMode = "All";
    [ObservableProperty] private ModListItemViewModel? _selectedModItem;
    [ObservableProperty] private bool _hasSelectedMod;
    [ObservableProperty] private string _modNameText = "";
    [ObservableProperty] private string _modSourceText = "";
    [ObservableProperty] private IBrush _modSourceBrush = Brushes.Gray;
    [ObservableProperty] private string _modVersionBadgeText = "";
    [ObservableProperty] private string _modAuthorText = "";
    [ObservableProperty] private string _modStatusText = "";
    [ObservableProperty] private string _modVersionText = "";
    [ObservableProperty] private string _modGameVersionText = "";
    [ObservableProperty] private string _modLastUpdatedText = "";
    [ObservableProperty] private string _modDescriptionText = "";
    [ObservableProperty] private bool _showDownload;
    [ObservableProperty] private bool _showUpdate;
    [ObservableProperty] private bool _showRemove;
    [ObservableProperty] private bool _showDisable;
    [ObservableProperty] private bool _showEnable;
    [ObservableProperty] private bool _showUpdateNotice;
    [ObservableProperty] private string _updateVersionText = "";
    [ObservableProperty] private string _updateNotesText = "";
    [ObservableProperty] private bool _showParameters;
    [ObservableProperty] private bool _hasUiConfigWarning;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private AliasListItemViewModel? _selectedAlias;
    [ObservableProperty] private string _aliasName = "";
    [ObservableProperty] private string _aliasCommand = "";
    [ObservableProperty] private string _aliasKindText = "Plain";
    [ObservableProperty] private IBrush _aliasKindBrush = Brushes.Gray;
    [ObservableProperty] private string _aliasPreview = "";
    [ObservableProperty] private bool _aliasEditorVisible;

    public ObservableCollection<ModListItemViewModel> VisibleMods { get; } = [];
    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];
    public ObservableCollection<ParamRowViewModel> ParameterRows { get; } = [];

    public async Task InitializeAsync()
    {
        try
        {
            _paths.EnsureDirectories();
            _cache.Load();
            _settings.Load();
            ReloadAliases();
            await RefreshModsAsync().ConfigureAwait(true);

            if (!File.Exists(Path.Combine(_paths.ModsDirectory, "luajit-support", "entry.elf")))
                StatusText = "Note: luajit-support not installed — Lua mods need it.";
        }
        catch (Exception ex)
        {
            StatusText = $"Startup error: {ex.Message}";
        }
    }

    partial void OnSelectedModItemChanged(ModListItemViewModel? value) => ApplySelectedMod(value?.Mod);

    partial void OnFilterModeChanged(string value) => RebuildVisibleMods();

    partial void OnSelectedAliasChanged(AliasListItemViewModel? value)
    {
        if (value is null)
        {
            AliasEditorVisible = false;
            return;
        }
        AliasEditorVisible = true;
        AliasName = value.Name;
        AliasCommand = value.Command;
        UpdateAliasPreview();
    }

    partial void OnAliasCommandChanged(string value) => UpdateAliasPreview();

    [RelayCommand]
    private async Task RefreshModsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Refreshing mods...";
        try
        {
            _cache.Load();
            var installed = _discovery.GetInstalledMods();
            try
            {
                _releases = await _github.GetLatestModReleasesAsync().ConfigureAwait(true);
                StatusText = $"Loaded {_releases.Count} GitHub releases.";
            }
            catch (Exception ex)
            {
                StatusText = $"GitHub unavailable: {ex.Message} (showing local mods)";
                _releases = new Dictionary<string, ModReleaseInfo>(StringComparer.OrdinalIgnoreCase);
            }
            _allMods = _discovery.MergeWithReleases(installed, _releases);
            RebuildVisibleMods();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null || string.IsNullOrEmpty(mod.ZipUrl)) return;
        if (!_releases.TryGetValue(mod.Id, out var release) &&
            (mod.FolderId is null || !_releases.TryGetValue(mod.FolderId, out release)))
        {
            release = new ModReleaseInfo
            {
                ModId = mod.Id,
                Version = mod.RemoteVersion ?? mod.Version,
                DownloadUrl = mod.ZipUrl!
            };
        }

        await RunInstallAsync(release!).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task UpdateSelectedAsync() => await DownloadSelectedAsync().ConfigureAwait(true);

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var mod = SelectedModItem?.Mod;
        if (mod?.FolderPath is null || mod.FolderId is null) return;
        _install.RemoveDownloaded(mod.FolderPath, mod.FolderId);
        StatusText = $"Removed {mod.Name}";
        await RefreshModsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DisableSelectedAsync()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null) return;
        _install.SetEnabled(mod, enabled: false);
        StatusText = mod.Source == ModSource.Downloaded ? $"Removed {mod.Name}" : $"Disabled {mod.Name}";
        await RefreshModsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task EnableSelectedAsync()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null) return;
        _install.SetEnabled(mod, enabled: true);
        StatusText = $"Enabled {mod.Name}";
        await RefreshModsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private void SaveParameters()
    {
        var mod = SelectedModItem?.Mod;
        if (mod?.FolderPath is null) return;
        var entry = Path.Combine(mod.FolderPath, GamePaths.ConfigFileName);
        var dict = ParameterRows.ToDictionary(r => r.Name, r => r.GetValue(), StringComparer.Ordinal);
        if (EntryLuaConfig.Write(entry, dict))
            StatusText = "Configuration saved!";
        else
            StatusText = "Failed to save configuration.";
    }

    [RelayCommand]
    private void ResetParameters()
    {
        var mod = SelectedModItem?.Mod;
        if (mod is null) return;
        ParameterRows.Clear();
        foreach (var p in mod.Parameters)
            ParameterRows.Add(ParamRowViewModel.FromDef(p, null));
    }

    [RelayCommand]
    private void LaunchGame()
    {
        try
        {
            GameLauncher.LaunchSteamGame();
            StatusText = "Launching game via Steam...";
        }
        catch (Exception ex)
        {
            StatusText = $"Launch failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddAlias()
    {
        var name = "new_alias";
        var i = 1;
        while (Aliases.Any(a => a.Name == name))
            name = $"new_alias_{i++}";
        var item = new AliasListItemViewModel(name, "");
        Aliases.Add(item);
        SelectedAlias = item;
    }

    [RelayCommand]
    private void DeleteAlias()
    {
        if (SelectedAlias is null) return;
        Aliases.Remove(SelectedAlias);
        SelectedAlias = null;
        AliasEditorVisible = false;
    }

    [RelayCommand]
    private void SaveAliases()
    {
        if (SelectedAlias is not null)
        {
            SelectedAlias.Name = AliasName.Trim();
            SelectedAlias.Command = AliasCommand;
        }
        var map = Aliases
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .ToDictionary(a => a.Name.Trim(), a => a.Command, StringComparer.Ordinal);
        _settings.SaveAliases(map);
        StatusText = "Aliases saved.";
        ReloadAliases();
    }

    [RelayCommand]
    private void ApplyAliasEdits()
    {
        if (SelectedAlias is null) return;
        SelectedAlias.Name = AliasName.Trim();
        SelectedAlias.Command = AliasCommand;
        UpdateAliasPreview();
    }

    [RelayCommand]
    private void InsertAliasSnippet(string? snippet)
    {
        if (string.IsNullOrEmpty(snippet)) return;
        AliasCommand += snippet;
    }

    [RelayCommand]
    private void ExitApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private async Task RunInstallAsync(ModReleaseInfo release)
    {
        IsBusy = true;
        IsProgressVisible = true;
        DownloadProgress = 0;
        DownloadStatusText = $"Downloading {release.ModId}...";
        var progress = new Progress<double>(p =>
        {
            DownloadProgress = p;
            DownloadStatusText = $"Downloading {release.ModId}... {p:0}%";
        });
        try
        {
            await _install.InstallFromReleaseAsync(release, progress).ConfigureAwait(true);
            StatusText = $"{release.ModId} v{release.Version} installed.";
            await RefreshModsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
            IsBusy = false;
        }
    }

    private void RebuildVisibleMods()
    {
        IEnumerable<ModInfo> q = _allMods;
        q = FilterMode switch
        {
            "Installed" => q.Where(m => m.Source != ModSource.Available),
            "Available" => q.Where(m => m.Source == ModSource.Available),
            _ => q
        };
        var selectedId = SelectedModItem?.Mod.Id;
        VisibleMods.Clear();
        foreach (var m in q)
            VisibleMods.Add(new ModListItemViewModel(m));
        SelectedModItem = VisibleMods.FirstOrDefault(m => m.Mod.Id == selectedId);
    }

    private void ApplySelectedMod(ModInfo? mod)
    {
        HasSelectedMod = mod is not null;
        if (mod is null) return;

        ModNameText = mod.Name;
        ModSourceText = mod.Source.ToString();
        ModSourceBrush = new SolidColorBrush(Color.Parse(mod.Source switch
        {
            ModSource.Downloaded => "#0078D4",
            ModSource.Manual => "#9C27B0",
            _ => "#607D8B"
        }));
        ModVersionBadgeText = string.IsNullOrEmpty(mod.Version) ? "" : $"v{mod.Version}";
        ModAuthorText = mod.Author;
        ModStatusText = mod.IsEnabled ? "Enabled" : (mod.Source == ModSource.Available ? "Not installed" : "Disabled");
        ModVersionText = mod.Version;
        ModGameVersionText = mod.GameVersion;
        ModLastUpdatedText = mod.LastUpdated;
        ModDescriptionText = mod.Description;

        ShowDownload = mod.Source == ModSource.Available;
        ShowUpdate = mod.HasUpdate;
        ShowRemove = mod.Source == ModSource.Downloaded && mod.IsEnabled;
        ShowDisable = mod.Source == ModSource.Manual && mod.IsEnabled;
        ShowEnable = mod.Source == ModSource.Manual && !mod.IsEnabled;
        ShowUpdateNotice = mod.HasUpdate;
        UpdateVersionText = mod.RemoteVersion ?? "";
        UpdateNotesText = mod.RemoteNotes ?? "";

        HasUiConfigWarning = mod.HasUiConfigPs1;
        ParameterRows.Clear();
        ShowParameters = mod.Source != ModSource.Available && mod.FolderPath is not null && mod.Parameters.Count > 0;
        if (ShowParameters && mod.FolderPath is not null)
        {
            var current = EntryLuaConfig.Read(Path.Combine(mod.FolderPath, GamePaths.ConfigFileName));
            foreach (var p in mod.Parameters)
            {
                current.TryGetValue(p.Name, out var cur);
                ParameterRows.Add(ParamRowViewModel.FromDef(p, cur));
            }
        }
    }

    private void ReloadAliases()
    {
        Aliases.Clear();
        foreach (var (name, cmd) in _settings.CmdAliases.OrderBy(x => x.Key, StringComparer.Ordinal))
            Aliases.Add(new AliasListItemViewModel(name, cmd));
    }

    private void UpdateAliasPreview()
    {
        var kind = AliasAnalyzer.Analyze(AliasCommand);
        AliasKindText = kind.ToString();
        AliasKindBrush = new SolidColorBrush(Color.Parse(AliasAnalyzer.KindColor(kind)));
        AliasPreview = string.IsNullOrWhiteSpace(AliasCommand) ? "(empty)" : AliasCommand;
    }
}

public partial class ParamRowViewModel : ObservableObject
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Type { get; init; } = "string";
    public string Description { get; init; } = "";
    public List<string> Options { get; init; } = [];

    [ObservableProperty] private bool _boolValue;
    [ObservableProperty] private string _textValue = "";
    [ObservableProperty] private string _selectedOption = "";

    public bool IsBoolean => Type == "boolean";
    public bool IsSelect => Type == "select";
    public bool IsText => Type is not ("boolean" or "select");

    public static ParamRowViewModel FromDef(ParameterDef def, object? current)
    {
        var row = new ParamRowViewModel
        {
            Name = def.Name,
            Label = string.IsNullOrEmpty(def.Label) ? def.Name : def.Label,
            Type = def.Type.ToLowerInvariant(),
            Description = def.Description,
            Options = def.Options ?? []
        };

        var value = current ?? (def.Default.HasValue ? JsonElementToObject(def.Default.Value) : null);
        switch (row.Type)
        {
            case "boolean":
                row.BoolValue = value is true or "true";
                break;
            case "select":
                row.SelectedOption = value?.ToString() ?? row.Options.FirstOrDefault() ?? "";
                break;
            default:
                row.TextValue = value?.ToString() ?? "";
                break;
        }
        return row;
    }

    public object? GetValue() => Type switch
    {
        "boolean" => BoolValue,
        "select" => SelectedOption,
        "integer" when int.TryParse(TextValue, out var i) => i,
        "number" when double.TryParse(TextValue, out var d) => d,
        _ => TextValue
    };

    private static object? JsonElementToObject(System.Text.Json.JsonElement el) => el.ValueKind switch
    {
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        System.Text.Json.JsonValueKind.String => el.GetString(),
        _ => el.ToString()
    };
}
