using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TniModManager.Core.Aliases;
using TniModManager.Core.Settings;

namespace TniModManager.ViewModels;

public partial class AliasesViewModel : ViewModelBase
{
    private readonly GameSettingsStore _settings;
    private readonly IAppShell _shell;

    public AliasesViewModel(GameSettingsStore settings, IAppShell shell)
    {
        _settings = settings;
        _shell = shell;
    }

    [ObservableProperty] private AliasListItemViewModel? _selectedAlias;
    [ObservableProperty] private string _aliasName = "";
    [ObservableProperty] private string _aliasCommand = "";
    [ObservableProperty] private string _aliasKindText = "Plain";
    [ObservableProperty] private IBrush _aliasKindBrush = Brushes.Gray;
    [ObservableProperty] private string _aliasPreview = "";
    [ObservableProperty] private bool _aliasEditorVisible;

    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];

    public void Load() => ReloadAliases();

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
    private void AddAlias()
    {
        var name = "new_alias";
        var index = 1;
        while (Aliases.Any(alias => alias.Name == name))
            name = $"new_alias_{index++}";

        var item = new AliasListItemViewModel(name, "");
        Aliases.Add(item);
        SelectedAlias = item;
    }

    [RelayCommand]
    private void DeleteAlias()
    {
        if (SelectedAlias is null)
            return;

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

        var aliases = Aliases
            .Where(alias => !string.IsNullOrWhiteSpace(alias.Name))
            .ToDictionary(alias => alias.Name.Trim(), alias => alias.Command, StringComparer.Ordinal);
        _settings.SaveAliases(aliases);
        _shell.SetStatus("Aliases saved.");
        ReloadAliases();
    }

    [RelayCommand]
    private void ApplyAliasEdits()
    {
        if (SelectedAlias is null)
            return;

        SelectedAlias.Name = AliasName.Trim();
        SelectedAlias.Command = AliasCommand;
        UpdateAliasPreview();
    }

    [RelayCommand]
    private void InsertAliasSnippet(string? snippet)
    {
        if (!string.IsNullOrEmpty(snippet))
            AliasCommand += snippet;
    }

    public void RefreshThemeBrushes()
    {
        foreach (var alias in Aliases)
            alias.RefreshBrush();
        AliasKindBrush = ThemeBrushResolver.GetAlias(AliasAnalyzer.Analyze(AliasCommand));
    }

    private void ReloadAliases()
    {
        Aliases.Clear();
        foreach (var (name, command) in _settings.CmdAliases.OrderBy(item => item.Key, StringComparer.Ordinal))
            Aliases.Add(new AliasListItemViewModel(name, command));
    }

    private void UpdateAliasPreview()
    {
        var kind = AliasAnalyzer.Analyze(AliasCommand);
        AliasKindText = kind.ToString();
        AliasKindBrush = ThemeBrushResolver.GetAlias(kind);
        AliasPreview = string.IsNullOrWhiteSpace(AliasCommand) ? "(empty)" : AliasCommand;
    }
}
