using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private bool _dismissingCompletion;

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
    [ObservableProperty] private bool _showActiveStepBar;
    [ObservableProperty] private string _activeStepLabel = "";
    [ObservableProperty] private int _activeStepIndex;
    [ObservableProperty] private int _activeStepCount;
    [ObservableProperty] private bool _canGoPrevStep;
    [ObservableProperty] private bool _canGoNextStep;
    [ObservableProperty] private string _aliasHelpHeading = "";

    public ObservableCollection<AliasListItemViewModel> Aliases { get; } = [];
    public ObservableCollection<AliasListItemViewModel> VisibleAliases { get; } = [];
    public ObservableCollection<AliasPreviewSegment> LivePreviewSegments { get; } = [];
    public ObservableCollection<AliasCompletionItem> CompletionItems { get; } = [];
    public ObservableCollection<AliasPreviewLineViewModel> PreviewLines { get; } = [];

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
            AliasCommand = AliasAnalyzer.FormatCompoundForEditor(value.Command);
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
            .ToDictionary(
                alias => alias.Name.Trim(),
                alias => AliasAnalyzer.NormalizeCompoundForStorage(alias.Command),
                StringComparer.Ordinal);
        var keep = SelectedAlias?.Name.Trim();
        _settings.SaveAliases(aliases);
        _shell.SetStatus(UiStrings.AliasesSaved);
        ReloadAliases(keep);
    }

    [RelayCommand]
    private void OpenAliasesFolder()
    {
        try
        {
            // Алиасы игры — в settings.json (cmd_alias) в userdata Godot.
            var settingsPath = _settings.SettingsPath;
            var dir = Path.GetDirectoryName(settingsPath) ?? _settings.GameDataPath;
            Directory.CreateDirectory(dir);
            OpenGameDataInFileManager(dir, settingsPath);
        }
        catch (Exception ex)
        {
            _shell.SetStatus(UiStrings.OpenUrlFailed(ex.Message));
        }
    }

    /// <summary>Открыть userdata игры в проводнике / xdg-open (на Linux FileName=dir не работает).</summary>
    private static void OpenGameDataInFileManager(string directory, string settingsPath)
    {
        if (OperatingSystem.IsWindows())
        {
            if (File.Exists(settingsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/select,\"" + settingsPath + "\"",
                    UseShellExecute = true
                });
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + directory + "\"",
                UseShellExecute = true
            });
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "xdg-open",
            ArgumentList = { directory },
            UseShellExecute = false
        });
    }

    [RelayCommand]
    private void AcceptCompletion(AliasCompletionItem? item = null)
    {
        item ??= SelectedCompletionItem;
        if (item is null || !IsCompletionOpen)
            return;

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

    public void FinishAcceptCompletionCaret(int caretIndex)
    {
        AliasCommandCaretIndex = Math.Clamp(caretIndex, 0, AliasCommand.Length);
        _suppressCaretSync = false;
    }

    public bool ShouldSyncCaretFromView => !_suppressCaretSync;

    [RelayCommand]
    private void OpenCompletion() => RefreshCompletion(allowEmptyPrefix: true);

    [RelayCommand]
    private void SelectCommandSegment(int index)
    {
        var spans = AliasAnalyzer.GetCommandSpans(AliasCommand);
        if (index < 0 || index >= spans.Count)
            return;

        var span = spans[index];
        _suppressCaretSync = true;
        try
        {
            AliasCommandCaretIndex = span.Start;
            RequestCaretIndex?.Invoke(span.Start);
            // Явно фиксируем шаг — не полагаемся на caret-события после Focus.
            UpdateActiveStepBarFromIndex(index, spans);
            SyncPreviewLineActive();
            UpdateTokenHelpKeepingStep(index);
        }
        finally
        {
            _suppressCaretSync = false;
        }
    }

    private void UpdateActiveStepBarFromIndex(int index, IReadOnlyList<AliasCommandSpan> spans)
    {
        ActiveStepCount = spans.Count;
        ShowActiveStepBar = spans.Count > 1;
        if (!ShowActiveStepBar)
        {
            ClearActiveStepBar();
            if (spans.Count == 1)
                ActiveStepIndex = 0;
            return;
        }

        ActiveStepIndex = index;
        ActiveStepLabel = UiStrings.AliasStepLabel(
            index + 1,
            ActiveStepCount,
            AliasAnalyzer.FormatSegmentLabel(spans[index].Text));
        CanGoPrevStep = index > 0;
        CanGoNextStep = index + 1 < ActiveStepCount;
    }

    private void UpdateTokenHelpKeepingStep(int stepIndex)
    {
        AliasTokenManual? manual = null;
        if (IsCompletionOpen && SelectedCompletionItem is { } selected)
            manual = _catalog.ResolveNameManual(selected.Name)
                     ?? new AliasTokenManual(
                         selected.Name,
                         selected.Kind,
                         selected.Hint,
                         string.IsNullOrWhiteSpace(selected.UsageLine) ? [] : [selected.UsageLine],
                         []);

        var spans = AliasAnalyzer.GetCommandSpans(AliasCommand);
        if (manual is null && stepIndex >= 0 && stepIndex < spans.Count)
        {
            var first = AliasAnalyzer.FirstWord(spans[stepIndex].Text);
            manual = _catalog.ResolveNameManual(first);
        }

        manual ??= _catalog.ResolveTokenManual(AliasCommand, AliasCommandCaretIndex);
        ApplyManualToHelp(manual);
    }

    [RelayCommand]
    private void SelectPrevSegment()
    {
        if (ActiveStepIndex > 0)
            SelectCommandSegment(ActiveStepIndex - 1);
    }

    [RelayCommand]
    private void SelectNextSegment()
    {
        if (ActiveStepIndex + 1 < ActiveStepCount)
            SelectCommandSegment(ActiveStepIndex + 1);
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
            return;

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
            {
                var editorCmd = AliasAnalyzer.FormatCompoundForEditor(command);
                Aliases.Add(new AliasListItemViewModel(name, editorCmd));
            }

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
            AliasCommand = AliasAnalyzer.FormatCompoundForEditor(SelectedAlias.Command);
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
            var normalized = AliasAnalyzer.NormalizeCompoundForStorage(alias.Command);
            if (!saved.TryGetValue(alias.Name.Trim(), out var command) ||
                !string.Equals(command, normalized, StringComparison.Ordinal))
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
        PreviewLines.Clear();
        ClearActiveStepBar();
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

    private void RebuildPreviewLines()
    {
        var spans = AliasAnalyzer.GetCommandSpans(AliasCommand);
        PreviewLines.Clear();
        if (spans.Count == 0)
            return;

        for (var i = 0; i < spans.Count; i++)
        {
            var prefix = spans.Count == 1
                ? ""
                : i == 0
                    ? "┌─ "
                    : i == spans.Count - 1
                        ? "└─ "
                        : "├─ ";
            PreviewLines.Add(new AliasPreviewLineViewModel(
                spans[i].Index,
                prefix,
                spans[i].Text,
                spans[i].Start,
                SelectCommandSegmentCommand)
            {
                IsActive = spans[i].Index == ActiveStepIndex
            });
        }
    }

    private void UpdateActiveStepBar()
    {
        var spans = AliasAnalyzer.GetCommandSpans(AliasCommand);
        ActiveStepCount = spans.Count;
        ShowActiveStepBar = spans.Count > 1;
        if (!ShowActiveStepBar)
        {
            ClearActiveStepBar();
            if (spans.Count == 1)
                ActiveStepIndex = 0;
            return;
        }

        var span = AliasAnalyzer.FindSpanAt(AliasCommand, AliasCommandCaretIndex);
        ActiveStepIndex = span?.Index ?? 0;
        var text = spans[ActiveStepIndex].Text;
        ActiveStepLabel = UiStrings.AliasStepLabel(ActiveStepIndex + 1, ActiveStepCount, AliasAnalyzer.FormatSegmentLabel(text));
        CanGoPrevStep = ActiveStepIndex > 0;
        CanGoNextStep = ActiveStepIndex + 1 < ActiveStepCount;
    }

    private void ClearActiveStepBar()
    {
        ShowActiveStepBar = false;
        ActiveStepLabel = "";
        ActiveStepIndex = -1;
        ActiveStepCount = 0;
        CanGoPrevStep = false;
        CanGoNextStep = false;
    }

    private void SyncPreviewLineActive()
    {
        foreach (var line in PreviewLines)
            line.IsActive = line.Index == ActiveStepIndex;
    }

    private void UpdateTokenHelp()
    {
        UpdateActiveStepBar();
        SyncPreviewLineActive();

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
        ShowAliasHelp = !string.IsNullOrWhiteSpace(AliasHelpSummary)
                        || ShowAliasHelpUsage
                        || ShowAliasHelpExamples;
        AliasHelpHeading = ShowActiveStepBar
            ? UiStrings.AliasHelpStepHeading(ActiveStepIndex + 1, ActiveStepCount)
            : UiStrings.AliasManualHeading;
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
        ShowAliasHelp = false;
        AliasHelpHeading = UiStrings.AliasManualHeading;
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
            ClearActiveStepBar();
            LivePreviewSegments.Clear();
            PreviewLines.Clear();
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

        UpdateActiveStepBar();
        RebuildPreviewLines();
        UpdateTokenHelp();
        SelectedAlias?.RefreshLocalizedLabels();
        SelectedAlias?.RefreshCommandPreview();
    }
}
