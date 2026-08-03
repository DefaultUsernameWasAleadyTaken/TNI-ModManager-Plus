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
    private bool _suppressListSync;

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
    [ObservableProperty] private bool _showAliasFullUsage;
    [ObservableProperty] private bool _aliasEditorVisible;
    [ObservableProperty] private string _aliasHelpTitle = "";
    [ObservableProperty] private string _aliasHelpKindLabel = "";
    [ObservableProperty] private string _aliasHelpSummary = "";
    [ObservableProperty] private string _aliasHelpUsageText = "";
    [ObservableProperty] private string _aliasHelpExamplesText = "";
    [ObservableProperty] private bool _showAliasHelp;
    [ObservableProperty] private bool _showAliasHelpUsage;
    [ObservableProperty] private bool _showAliasHelpExamples;
    [ObservableProperty] private bool _canInsertHelpExample;
    [ObservableProperty] private string _aliasNameErrorText = "";
    [ObservableProperty] private bool _showAliasNameError;
    [ObservableProperty] private bool _isCompletionOpen;
    [ObservableProperty] private int _selectedCompletionIndex;
    [ObservableProperty] private int _aliasCommandCaretIndex;
    [ObservableProperty] private string _aliasFilter = "";
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private string _completionDetailTitle = "";
    [ObservableProperty] private string _completionDetailBody = "";
    [ObservableProperty] private bool _showCompletionDetail;
    [ObservableProperty] private bool _showCommandSegments;
    [ObservableProperty] private int _activeCommandSegmentIndex = -1;

    private string? _helpInsertText;
    private bool _dismissingCompletion;

    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];
    public ObservableCollection<AliasListItemViewModel> VisibleAliases { get; } = [];
    public ObservableCollection<AliasPreviewSegment> LivePreviewSegments { get; } = [];
    public ObservableCollection<AliasCompletionItem> CompletionItems { get; } = [];
    public ObservableCollection<AliasSegmentChipViewModel> CommandSegments { get; } = [];

    /// <summary>View ставит CaretIndex на TextBox после программной вставки.</summary>
    public event Action<int>? RequestCaretIndex;

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
        _suppressListSync = true;
        try
        {
            AliasName = value.Name;
            AliasCommand = value.Command;
        }
        finally
        {
            _suppressListSync = false;
        }

        UpdateAliasPreview();
        UpdateAliasNameValidation();
    }

    partial void OnAliasCommandChanged(string value)
    {
        SyncDraftToSelected();
        UpdateAliasPreview();
        if (!_suppressCompletion)
            RefreshCompletion();
    }

    partial void OnAliasNameChanged(string value)
    {
        SyncDraftToSelected();
        UpdateAliasPreview();
        UpdateAliasNameValidation();
    }

    partial void OnAliasFilterChanged(string value) => ApplyFilter();

    partial void OnAliasCommandCaretIndexChanged(int value)
    {
        if (!_suppressCompletion)
            RefreshCompletion();
        UpdateTokenHelp();
    }

    partial void OnSelectedCompletionIndexChanged(int value) => UpdateCompletionDetail();

    /// <summary>Параметр: empty | arg | on | try</summary>
    [RelayCommand]
    private void AddAlias(string? template = null)
    {
        var name = "new_alias";
        var index = 1;
        while (Aliases.Any(alias => alias.Name == name))
            name = $"new_alias_{index++}";

        var command = template switch
        {
            "arg" => "$1",
            "on" => "on $1",
            "try" => "try  then  else ",
            _ => ""
        };

        var item = new AliasListItemViewModel(name, command);
        Aliases.Add(item);
        ApplyFilter();
        SelectedAlias = item;
        RefreshDirty();
    }

    [RelayCommand]
    private void DeleteAlias()
    {
        if (SelectedAlias is null)
            return;

        Aliases.Remove(SelectedAlias);
        SelectedAlias = null;
        AliasEditorVisible = false;
        ApplyFilter();
        RefreshDirty();
    }

    [RelayCommand]
    private void SaveAliases()
    {
        FlushDraftToSelected(requireValidName: true);
        if (SelectedAlias is not null && ShowAliasNameError)
        {
            _shell.SetStatus(AliasNameErrorText);
            return;
        }

        if (SelectedAlias is not null && string.IsNullOrWhiteSpace(AliasName))
        {
            _shell.SetStatus(UiStrings.AliasNameRequired);
            return;
        }

        foreach (var alias in Aliases)
        {
            if (_catalog.IsReservedAliasName(alias.Name))
            {
                _shell.SetStatus(UiStrings.AliasNameReserved(alias.Name));
                return;
            }

            if (string.IsNullOrWhiteSpace(alias.Name))
            {
                _shell.SetStatus(UiStrings.AliasNameRequired);
                return;
            }
        }

        var names = Aliases.Select(a => a.Name.Trim()).ToList();
        if (names.Count != names.Distinct(StringComparer.Ordinal).Count())
        {
            _shell.SetStatus(UiStrings.AliasNameDuplicate);
            return;
        }

        var aliases = Aliases
            .Where(alias => !string.IsNullOrWhiteSpace(alias.Name))
            .ToDictionary(alias => alias.Name.Trim(), alias => alias.Command, StringComparer.Ordinal);
        var keep = SelectedAlias?.Name.Trim();
        _settings.SaveAliases(aliases);
        _shell.SetStatus(UiStrings.AliasesSaved);
        ReloadAliases(keep);
    }

    [RelayCommand]
    private void CloseAliasEditor()
    {
        FlushDraftToSelected(requireValidName: false);
        SelectedAlias = null;
        AliasEditorVisible = false;
        ClearEditorFields();
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
        UpdateTokenHelp();
    }

    /// <summary>Вызвать из view после установки CaretIndex на TextBox.</summary>
    public void FinishAcceptCompletionCaret(int caretIndex)
    {
        AliasCommandCaretIndex = Math.Clamp(caretIndex, 0, AliasCommand.Length);
        _suppressCaretSync = false;
    }

    public bool ShouldSyncCaretFromView => !_suppressCaretSync;

    [RelayCommand]
    private void OpenCompletion()
    {
        RefreshCompletion(allowEmptyPrefix: true);
    }

    [RelayCommand]
    private void InsertHelpExample()
    {
        if (string.IsNullOrWhiteSpace(_helpInsertText))
            return;

        var snippet = _helpInsertText.Trim();
        _suppressCompletion = true;
        try
        {
            if (string.IsNullOrWhiteSpace(AliasCommand))
            {
                AliasCommand = snippet;
                AliasCommandCaretIndex = AliasCommand.Length;
            }
            else
            {
                var sep = AliasCommand.EndsWith(' ') || AliasCommand.EndsWith(';') ? "" : " ";
                AliasCommand += sep + snippet;
                AliasCommandCaretIndex = AliasCommand.Length;
            }
        }
        finally
        {
            _suppressCompletion = false;
        }

        DismissCompletion();
        UpdateAliasPreview();
        UpdateTokenHelp();
        RequestCaretIndex?.Invoke(AliasCommandCaretIndex);
    }

    [RelayCommand]
    private void SelectCommandSegment(int index)
    {
        if (index < 0 || index >= CommandSegments.Count)
            return;

        var chip = CommandSegments[index];
        ActiveCommandSegmentIndex = index;
        foreach (var c in CommandSegments)
            c.IsActive = c.Index == index;

        AliasCommandCaretIndex = chip.CaretStart;
        RequestCaretIndex?.Invoke(chip.CaretStart);
        UpdateTokenHelp();
    }

    [RelayCommand]
    private void DismissCompletion()
    {
        if (_dismissingCompletion)
            return;

        _dismissingCompletion = true;
        try
        {
            IsCompletionOpen = false;
            CompletionItems.Clear();
            SelectedCompletionIndex = 0;
            ClearCompletionDetail();
        }
        finally
        {
            _dismissingCompletion = false;
        }
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
            UpdateTokenHelp();
            return;
        }

        AliasCommandCaretIndex = caretIndex;
    }

    public void RefreshThemeBrushes()
    {
        foreach (var alias in Aliases)
            alias.RefreshBrush();
        AliasKindBrush = ThemeBrushResolver.GetAlias(AliasAnalyzer.Analyze(AliasCommand).Kind);
        UpdateAliasPreview();
    }

    public void RefreshLocalizedLabels()
    {
        foreach (var alias in Aliases)
        {
            alias.RefreshLocalizedLabels();
            alias.RefreshCommandPreview();
        }

        UpdateAliasPreview();
        ApplyFilter();
    }

    private void SyncDraftToSelected()
    {
        if (_suppressListSync || SelectedAlias is null)
            return;

        FlushDraftToSelected(requireValidName: false);
        RefreshDirty();
    }

    private void FlushDraftToSelected(bool requireValidName)
    {
        if (SelectedAlias is null)
            return;

        SelectedAlias.Command = AliasCommand;
        var name = AliasName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            if (requireValidName)
                return;
            return;
        }

        if (_catalog.IsReservedAliasName(name))
            return;

        SelectedAlias.Name = name;
    }

    private void ReloadAliases(string? selectName = null)
    {
        var keep = selectName;
        _suppressListSync = true;
        try
        {
            SelectedAlias = null;
            Aliases.Clear();
            foreach (var (name, command) in _settings.CmdAliases.OrderBy(item => item.Key, StringComparer.Ordinal))
                Aliases.Add(new AliasListItemViewModel(name, command));
            ApplyFilter();
            if (!string.IsNullOrEmpty(keep))
                SelectedAlias = Aliases.FirstOrDefault(a => a.Name == keep);
        }
        finally
        {
            _suppressListSync = false;
        }

        if (SelectedAlias is not null)
        {
            AliasEditorVisible = true;
            AliasName = SelectedAlias.Name;
            AliasCommand = SelectedAlias.Command;
            UpdateAliasPreview();
            UpdateAliasNameValidation();
        }
        else
        {
            AliasEditorVisible = false;
            ClearEditorFields();
        }

        RefreshDirty();
    }

    private void ApplyFilter()
    {
        var query = AliasFilter.Trim();
        VisibleAliases.Clear();
        IEnumerable<AliasListItemViewModel> items = Aliases;
        if (!string.IsNullOrEmpty(query))
        {
            items = Aliases.Where(a =>
                a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Command.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in items)
            VisibleAliases.Add(item);
    }

    private void RefreshDirty()
    {
        var saved = _settings.CmdAliases;
        if (Aliases.Count != saved.Count)
        {
            HasUnsavedChanges = true;
            return;
        }

        foreach (var alias in Aliases)
        {
            if (!saved.TryGetValue(alias.Name.Trim(), out var command) ||
                !string.Equals(command, alias.Command, StringComparison.Ordinal))
            {
                HasUnsavedChanges = true;
                return;
            }
        }

        HasUnsavedChanges = false;
    }

    private void ClearEditorFields()
    {
        _suppressListSync = true;
        try
        {
            AliasName = "";
            AliasCommand = "";
        }
        finally
        {
            _suppressListSync = false;
        }

        AliasKindText = "";
        AliasInvocationText = "";
        AliasArgsSummaryText = "";
        ShowAliasArgsSummary = false;
        AliasDeviceNoticeText = "";
        ShowAliasDeviceNotice = false;
        AliasFullUsageText = "";
        ShowAliasFullUsage = false;
        ClearTokenHelp();
        AliasNameErrorText = "";
        ShowAliasNameError = false;
        AliasCommandCaretIndex = 0;
        _suppressCaretSync = false;
        _suppressCompletion = false;
        DismissCompletion();
        LivePreviewSegments.Clear();
        CommandSegments.Clear();
        ShowCommandSegments = false;
        ActiveCommandSegmentIndex = -1;
    }

    private void RefreshCompletion(bool allowEmptyPrefix = false)
    {
        if (_dismissingCompletion)
            return;

        var userNames = Aliases.Select(a => a.Name);
        var items = _catalog.Suggest(
            AliasCommand,
            AliasCommandCaretIndex,
            limit: 14,
            userNames,
            allowEmptyPrefix);

        // Не дёргать ListBox Clear→Add без нужды — источник вылетов Avalonia Popup.
        if (!allowEmptyPrefix
            && items.Count == 0
            && !IsCompletionOpen
            && CompletionItems.Count == 0)
        {
            UpdateCompletionDetail();
            return;
        }

        var same = items.Count == CompletionItems.Count
                   && items.Zip(CompletionItems, (a, b) =>
                       a.Name == b.Name && a.Kind == b.Kind).All(x => x);
        if (same && IsCompletionOpen == items.Count > 0)
        {
            UpdateCompletionDetail();
            return;
        }

        CompletionItems.Clear();
        foreach (var item in items)
            CompletionItems.Add(item);
        IsCompletionOpen = CompletionItems.Count > 0;
        if (SelectedCompletionIndex < 0 || SelectedCompletionIndex >= CompletionItems.Count)
            SelectedCompletionIndex = 0;
        UpdateCompletionDetail();
    }

    private void RebuildCommandSegments(AliasInfo info)
    {
        var spans = AliasAnalyzer.GetCommandSpans(AliasCommand);
        ShowCommandSegments = spans.Count > 1;
        CommandSegments.Clear();
        if (!ShowCommandSegments)
        {
            ActiveCommandSegmentIndex = spans.Count == 1 ? 0 : -1;
            return;
        }

        var active = AliasAnalyzer.FindSpanAt(AliasCommand, AliasCommandCaretIndex)?.Index ?? 0;
        ActiveCommandSegmentIndex = active;
        foreach (var span in spans)
        {
            var first = AliasAnalyzer.FirstWord(span.Text) ?? span.Text;
            if (first.Length > 18)
                first = first[..17] + "…";
            var title = $"{span.Index + 1}. {first}";
            CommandSegments.Add(new AliasSegmentChipViewModel(
                span.Index,
                title,
                span.Start,
                SelectCommandSegmentCommand)
            {
                IsActive = span.Index == active
            });
        }
    }

    private void SyncActiveSegmentFromCaret()
    {
        if (!ShowCommandSegments || CommandSegments.Count == 0)
            return;

        var span = AliasAnalyzer.FindSpanAt(AliasCommand, AliasCommandCaretIndex);
        var index = span?.Index ?? 0;
        if (index == ActiveCommandSegmentIndex)
            return;

        ActiveCommandSegmentIndex = index;
        foreach (var chip in CommandSegments)
            chip.IsActive = chip.Index == index;
    }

    private void UpdateTokenHelp()
    {
        SyncActiveSegmentFromCaret();

        // Пока открыт completion — справка по выбранному пункту, иначе по сегменту/токену.
        AliasTokenManual? manual = null;
        if (IsCompletionOpen && SelectedCompletionItem is { } selected)
            manual = _catalog.ResolveNameManual(selected.Name)
                     ?? new AliasTokenManual(
                         selected.Name,
                         selected.Kind,
                         selected.Hint,
                         string.IsNullOrWhiteSpace(selected.UsageLine) ? [] : [selected.UsageLine],
                         []);

        manual ??= _catalog.ResolveTokenManual(AliasCommand, AliasCommandCaretIndex);
        ApplyManualToHelp(manual);
    }

    private void UpdateCompletionDetail()
    {
        if (!IsCompletionOpen || SelectedCompletionItem is null)
        {
            ClearCompletionDetail();
            UpdateTokenHelp();
            return;
        }

        var item = SelectedCompletionItem;
        var manual = _catalog.ResolveNameManual(item.Name);
        CompletionDetailTitle = $"{item.Name} · {UiStrings.FormatCompletionKind(item.Kind)}";
        if (manual is not null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(manual.Summary))
                parts.Add(manual.Summary!);
            foreach (var u in manual.Usage.Take(2))
                parts.Add(u);
            foreach (var e in manual.Examples.Take(1))
                parts.Add(e);
            CompletionDetailBody = string.Join(Environment.NewLine, parts);
        }
        else
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Hint))
                parts.Add(item.Hint!);
            if (!string.IsNullOrWhiteSpace(item.UsageLine))
                parts.Add(item.UsageLine!);
            CompletionDetailBody = string.Join(Environment.NewLine, parts);
        }

        ShowCompletionDetail = !string.IsNullOrWhiteSpace(CompletionDetailBody);
        ApplyManualToHelp(manual ?? new AliasTokenManual(
            item.Name,
            item.Kind,
            item.Hint,
            string.IsNullOrWhiteSpace(item.UsageLine) ? [] : [item.UsageLine],
            []));
    }

    private void ClearCompletionDetail()
    {
        CompletionDetailTitle = "";
        CompletionDetailBody = "";
        ShowCompletionDetail = false;
    }

    private void ApplyManualToHelp(AliasTokenManual? manual)
    {
        if (manual is null)
        {
            ClearTokenHelp();
            return;
        }

        AliasHelpTitle = manual.Name;
        AliasHelpKindLabel = UiStrings.FormatCompletionKind(manual.Kind);
        AliasHelpSummary = manual.Summary ?? "";
        AliasHelpUsageText = string.Join(Environment.NewLine, manual.Usage.Take(4));
        AliasHelpExamplesText = string.Join(Environment.NewLine, manual.Examples.Take(3));
        ShowAliasHelpUsage = !string.IsNullOrWhiteSpace(AliasHelpUsageText);
        ShowAliasHelpExamples = !string.IsNullOrWhiteSpace(AliasHelpExamplesText);
        _helpInsertText = manual.PrimaryExample;
        CanInsertHelpExample = !string.IsNullOrWhiteSpace(_helpInsertText);
        ShowAliasHelp = !string.IsNullOrWhiteSpace(AliasHelpSummary)
                        || ShowAliasHelpUsage
                        || ShowAliasHelpExamples;
    }

    private void ClearTokenHelp()
    {
        AliasHelpTitle = "";
        AliasHelpKindLabel = "";
        AliasHelpSummary = "";
        AliasHelpUsageText = "";
        AliasHelpExamplesText = "";
        ShowAliasHelpUsage = false;
        ShowAliasHelpExamples = false;
        _helpInsertText = null;
        CanInsertHelpExample = false;
        ShowAliasHelp = false;
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

        var duplicate = Aliases.Any(a =>
            !ReferenceEquals(a, SelectedAlias) &&
            string.Equals(a.Name.Trim(), name, StringComparison.Ordinal));
        if (duplicate)
        {
            ShowAliasNameError = true;
            AliasNameErrorText = UiStrings.AliasNameDuplicate;
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
            ShowAliasFullUsage = false;
            ClearTokenHelp();
            CommandSegments.Clear();
            ShowCommandSegments = false;
            ActiveCommandSegmentIndex = -1;
            LivePreviewSegments.Clear();
            foreach (var segment in AliasAnalyzer.BuildLivePreviewSegments(info, AliasCommand, UiStrings.AliasPreviewPlaceholder))
                LivePreviewSegments.Add(segment);
            SelectedAlias?.RefreshLocalizedLabels();
            SelectedAlias?.RefreshCommandPreview();
            return;
        }

        AliasInvocationText = AliasPreviewBuilder.BuildInvocation(AliasName, info);
        AliasFullUsageText = AliasPreviewBuilder.BuildFullUsage(AliasName, info);
        ShowAliasFullUsage = !string.IsNullOrWhiteSpace(AliasFullUsageText);

        ShowAliasArgsSummary = info.Variables.Count > 0;
        AliasArgsSummaryText = ShowAliasArgsSummary
            ? UiStrings.AliasArgsRequired(info.Variables.Count, AliasPreviewBuilder.FormatVariablesList(info))
            : "";

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

        RebuildCommandSegments(info);
        UpdateTokenHelp();
        SelectedAlias?.RefreshLocalizedLabels();
        SelectedAlias?.RefreshCommandPreview();
    }
}
