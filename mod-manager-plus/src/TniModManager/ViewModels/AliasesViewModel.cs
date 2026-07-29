using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TniModManager.Core.Aliases;
using TniModManager.Core.Settings;
using TniModManager.Localization;

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
    [ObservableProperty] private string _aliasKindText = "";
    [ObservableProperty] private IBrush _aliasKindBrush = Brushes.Gray;
    [ObservableProperty] private string _aliasInvocationText = "";
    [ObservableProperty] private string _aliasArgsSummaryText = "";
    [ObservableProperty] private bool _showAliasArgsSummary;
    [ObservableProperty] private string _aliasDeviceNoticeText = "";
    [ObservableProperty] private bool _showAliasDeviceNotice;
    [ObservableProperty] private string _aliasFullUsageText = "";
    [ObservableProperty] private bool _aliasEditorVisible;

    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];
    public ObservableCollection<AliasPreviewSegment> LivePreviewSegments { get; } = [];

    public void Load() => ReloadAliases();

    partial void OnSelectedAliasChanged(AliasListItemViewModel? value)
    {
        if (value is null)
        {
            AliasEditorVisible = false;
            ClearEditorFields();
            return;
        }

        AliasEditorVisible = true;
        AliasName = value.Name;
        AliasCommand = value.Command;
        UpdateAliasPreview();
    }

    partial void OnAliasCommandChanged(string value) => UpdateAliasPreview();
    partial void OnAliasNameChanged(string value) => UpdateAliasPreview();

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
        _shell.SetStatus(UiStrings.AliasesSaved);
        ReloadAliases();
    }

    [RelayCommand]
    private void ApplyAliasEdits()
    {
        if (SelectedAlias is null)
            return;

        var name = AliasName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _shell.SetStatus(UiStrings.AliasNameRequired);
            return;
        }

        SelectedAlias.Name = name;
        SelectedAlias.Command = AliasCommand;
        UpdateAliasPreview();
        _shell.SetStatus(UiStrings.AliasApplied(name));
    }

    [RelayCommand]
    private void CancelAliasEdits()
    {
        SelectedAlias = null;
        AliasEditorVisible = false;
        ClearEditorFields();
    }

    [RelayCommand]
    private void InsertAliasSnippet(string? snippet)
    {
        if (string.IsNullOrEmpty(snippet))
            return;

        var next = AliasAnalyzer.Analyze(AliasCommand).MaxVariable + 1;
        AliasCommand += snippet switch
        {
            "$n" => $" ${next}",
            "on $n" => $" on ${next}",
            "using $n" => $" using ${next}",
            _ => snippet
        };
    }

    public void RefreshThemeBrushes()
    {
        foreach (var alias in Aliases)
            alias.RefreshBrush();
        AliasKindBrush = ThemeBrushResolver.GetAlias(AliasAnalyzer.Analyze(AliasCommand).Kind);
    }

    public void RefreshLocalizedLabels()
    {
        foreach (var alias in Aliases)
            alias.RefreshLocalizedLabels();
        UpdateAliasPreview();
    }

    private void ReloadAliases()
    {
        Aliases.Clear();
        foreach (var (name, command) in _settings.CmdAliases.OrderBy(item => item.Key, StringComparer.Ordinal))
            Aliases.Add(new AliasListItemViewModel(name, command));
    }

    private void ClearEditorFields()
    {
        AliasName = "";
        AliasCommand = "";
        AliasKindText = "";
        AliasInvocationText = "";
        AliasArgsSummaryText = "";
        ShowAliasArgsSummary = false;
        AliasDeviceNoticeText = "";
        ShowAliasDeviceNotice = false;
        AliasFullUsageText = "";
        LivePreviewSegments.Clear();
    }

    private void UpdateAliasPreview()
    {
        var info = AliasAnalyzer.Analyze(AliasCommand);
        AliasKindText = UiStrings.FormatAliasKind(info.Kind);
        AliasKindBrush = ThemeBrushResolver.GetAlias(info.Kind);

        if (string.IsNullOrWhiteSpace(AliasCommand))
        {
            AliasInvocationText = "";
            ShowAliasArgsSummary = false;
            AliasArgsSummaryText = "";
            ShowAliasDeviceNotice = false;
            AliasDeviceNoticeText = "";
            AliasFullUsageText = "";
            LivePreviewSegments.Clear();
            foreach (var segment in AliasAnalyzer.BuildLivePreviewSegments(info, AliasCommand, UiStrings.AliasPreviewPlaceholder))
                LivePreviewSegments.Add(segment);
            SelectedAlias?.RefreshLocalizedLabels();
            return;
        }

        AliasInvocationText = AliasPreviewBuilder.BuildInvocation(AliasName, info);
        AliasFullUsageText = AliasPreviewBuilder.BuildFullUsage(AliasName, info);

        ShowAliasArgsSummary = info.Variables.Count > 0;
        AliasArgsSummaryText = ShowAliasArgsSummary
            ? UiStrings.AliasArgsRequired(info.Variables.Count, AliasPreviewBuilder.FormatVariablesList(info))
            : "";

        ShowAliasDeviceNotice = !info.HasOn || !info.HasUsing;
        AliasDeviceNoticeText = ShowAliasDeviceNotice
            ? UiStrings.AliasDeviceNotice(!info.HasOn, !info.HasUsing)
            : "";

        LivePreviewSegments.Clear();
        foreach (var segment in AliasAnalyzer.BuildLivePreviewSegments(info, AliasCommand, UiStrings.AliasPreviewPlaceholder))
            LivePreviewSegments.Add(segment);

        SelectedAlias?.RefreshLocalizedLabels();
    }
}
