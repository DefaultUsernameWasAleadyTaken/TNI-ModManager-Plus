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
    private readonly GameCommandCatalog _catalog;
    private bool _suppressCompletion;
    private bool _suppressCaretSync;

    public AliasesViewModel(GameSettingsStore settings, IAppShell shell, GameCommandCatalog? catalog = null)
    {
        _settings = settings;
        _shell = shell;
        _catalog = catalog ?? GameCommandCatalog.Default;
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
    [ObservableProperty] private string _aliasContextHelpText = "";
    [ObservableProperty] private bool _showAliasContextHelp;
    [ObservableProperty] private string _aliasNameErrorText = "";
    [ObservableProperty] private bool _showAliasNameError;
    [ObservableProperty] private bool _isCompletionOpen;
    [ObservableProperty] private int _selectedCompletionIndex;
    [ObservableProperty] private int _aliasCommandCaretIndex;

    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];
    public ObservableCollection<AliasPreviewSegment> LivePreviewSegments { get; } = [];
    public ObservableCollection<AliasCompletionItem> CompletionItems { get; } = [];

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

    partial void OnAliasCommandChanged(string value)
    {
        UpdateAliasPreview();
        if (!_suppressCompletion)
            RefreshCompletion();
    }

    partial void OnAliasNameChanged(string value)
    {
        UpdateAliasPreview();
        UpdateAliasNameValidation();
    }

    partial void OnAliasCommandCaretIndexChanged(int value)
    {
        if (!_suppressCompletion)
            RefreshCompletion();
        UpdateContextHelp();
    }

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
            var edited = AliasName.Trim();
            if (_catalog.IsReservedAliasName(edited))
            {
                UpdateAliasNameValidation();
                _shell.SetStatus(UiStrings.AliasNameReserved(edited));
                return;
            }

            SelectedAlias.Name = edited;
            SelectedAlias.Command = AliasCommand;
        }

        foreach (var alias in Aliases)
        {
            if (_catalog.IsReservedAliasName(alias.Name))
            {
                _shell.SetStatus(UiStrings.AliasNameReserved(alias.Name));
                return;
            }
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

        if (_catalog.IsReservedAliasName(name))
        {
            UpdateAliasNameValidation();
            _shell.SetStatus(UiStrings.AliasNameReserved(name));
            return;
        }

        SelectedAlias.Name = name;
        SelectedAlias.Command = AliasCommand;
        UpdateAliasPreview();
        _shell.SetStatus(UiStrings.AliasApplied(name));
    }

    [RelayCommand]
    private void AcceptCompletion(AliasCompletionItem? item = null)
    {
        item ??= SelectedCompletionItem;
        if (item is null || !IsCompletionOpen)
            return;

        // Держим suppress до FinishAcceptCompletionCaret — иначе TextChanged затрёт caret.
        _suppressCaretSync = true;
        _suppressCompletion = true;
        try
        {
            var (text, caret) = GameCommandCatalog.ApplyCompletion(AliasCommand, AliasCommandCaretIndex, item.Name);
            AliasCommand = text;
            AliasCommandCaretIndex = caret;
        }
        finally
        {
            _suppressCompletion = false;
        }

        DismissCompletion();
        UpdateAliasPreview();
        UpdateContextHelp();
    }

    /// <summary>Вызвать из view после установки CaretIndex на TextBox.</summary>
    public void FinishAcceptCompletionCaret(int caretIndex)
    {
        AliasCommandCaretIndex = Math.Clamp(caretIndex, 0, AliasCommand.Length);
        _suppressCaretSync = false;
    }

    public bool ShouldSyncCaretFromView => !_suppressCaretSync;

    [RelayCommand]
    private void DismissCompletion()
    {
        IsCompletionOpen = false;
        CompletionItems.Clear();
        SelectedCompletionIndex = 0;
    }

    [RelayCommand]
    private void MoveCompletionSelection(int delta)
    {
        if (!IsCompletionOpen || CompletionItems.Count == 0)
            return;
        var next = SelectedCompletionIndex + delta;
        if (next < 0) next = CompletionItems.Count - 1;
        if (next >= CompletionItems.Count) next = 0;
        SelectedCompletionIndex = next;
    }

    public AliasCompletionItem? SelectedCompletionItem =>
        SelectedCompletionIndex >= 0 && SelectedCompletionIndex < CompletionItems.Count
            ? CompletionItems[SelectedCompletionIndex]
            : null;

    public void NotifyCommandCaret(int caretIndex)
    {
        if (_suppressCaretSync)
            return;

        if (AliasCommandCaretIndex == caretIndex)
        {
            RefreshCompletion();
            UpdateContextHelp();
            return;
        }

        AliasCommandCaretIndex = caretIndex;
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
        // Runs в Live Preview держат старые SolidColorBrush — пересобрать под текущую тему.
        UpdateAliasPreview();
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
        AliasContextHelpText = "";
        ShowAliasContextHelp = false;
        AliasNameErrorText = "";
        ShowAliasNameError = false;
        AliasCommandCaretIndex = 0;
        _suppressCaretSync = false;
        _suppressCompletion = false;
        DismissCompletion();
        LivePreviewSegments.Clear();
    }

    private void RefreshCompletion()
    {
        var userNames = Aliases.Select(a => a.Name);
        var items = _catalog.Suggest(AliasCommand, AliasCommandCaretIndex, limit: 12, userNames);
        CompletionItems.Clear();
        foreach (var item in items)
            CompletionItems.Add(item);
        IsCompletionOpen = CompletionItems.Count > 0;
        SelectedCompletionIndex = 0;
    }

    private void UpdateContextHelp()
    {
        var help = _catalog.BuildContextHelp(AliasCommand, AliasCommandCaretIndex);
        AliasContextHelpText = help ?? "";
        ShowAliasContextHelp = !string.IsNullOrWhiteSpace(help);
    }

    private void UpdateAliasNameValidation()
    {
        var name = AliasName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowAliasNameError = false;
            AliasNameErrorText = "";
            return;
        }

        if (_catalog.IsReservedAliasName(name))
        {
            ShowAliasNameError = true;
            AliasNameErrorText = UiStrings.AliasNameReserved(name);
            return;
        }

        ShowAliasNameError = false;
        AliasNameErrorText = "";
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
            ShowAliasContextHelp = false;
            AliasContextHelpText = "";
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

        // Notice по каталогу: только если тело содержит команды с requires_on/using.
        var req = _catalog.GetRequirementNotice(AliasCommand);
        if (req is not null)
        {
            ShowAliasDeviceNotice = true;
            AliasDeviceNoticeText = UiStrings.AliasDeviceNotice(req.NeedOn, req.NeedUsing);
        }
        else
        {
            ShowAliasDeviceNotice = false;
            AliasDeviceNoticeText = "";
        }

        LivePreviewSegments.Clear();
        foreach (var segment in AliasAnalyzer.BuildLivePreviewSegments(info, AliasCommand, UiStrings.AliasPreviewPlaceholder))
            LivePreviewSegments.Add(segment);

        UpdateContextHelp();
        SelectedAlias?.RefreshLocalizedLabels();
    }
}
