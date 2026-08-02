using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using TniModManager.Core.Aliases;
using TniModManager.ViewModels;

namespace TniModManager.Views;

public partial class AliasesView : UserControl
{
    private AliasesViewModel? _vm;

    public AliasesView()
    {
        InitializeComponent();
        AliasEditorScroll.SizeChanged += (_, e) =>
        {
            AliasEditorContent.MaxWidth = e.NewSize.Width > 0 ? e.NewSize.Width : double.PositiveInfinity;
        };
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.LivePreviewSegments.CollectionChanged -= OnPreviewSegmentsChanged;

        _vm = DataContext as AliasesViewModel;
        if (_vm is null)
            return;

        _vm.LivePreviewSegments.CollectionChanged += OnPreviewSegmentsChanged;
        RebuildLivePreviewInlines();
    }

    private void OnPreviewSegmentsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
        RebuildLivePreviewInlines();

    private void OnAliasCommandTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_vm is { ShouldSyncCaretFromView: false })
            return;
        SyncCaretFromBox();
    }

    private void OnAliasCommandCaretChanged(object? sender, GotFocusEventArgs e) =>
        SyncCaretFromBox();

    private void OnAliasCommandPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        SyncCaretFromBox();

    private void OnAliasCommandLostFocus(object? sender, RoutedEventArgs e)
    {
        // Дать DoubleTapped по списку сработать до закрытия.
        if (_vm is null) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_vm is { IsCompletionOpen: true } && CompletionList.IsFocused)
                return;
            _vm?.DismissCompletionCommand.Execute(null);
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    private void OnCompletionPopupClosed(object? sender, EventArgs e)
    {
        // Light-dismiss закрывает Popup, не обновляя VM при OneWay — синхронизируем.
        if (_vm is { IsCompletionOpen: true })
            _vm.DismissCompletionCommand.Execute(null);
    }

    private void OnAliasCommandKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm is null) return;

        if (_vm.IsCompletionOpen && _vm.CompletionItems.Count > 0)
        {
            switch (e.Key)
            {
                case Key.Down:
                    _vm.MoveCompletionSelectionCommand.Execute(1);
                    e.Handled = true;
                    return;
                case Key.Up:
                    _vm.MoveCompletionSelectionCommand.Execute(-1);
                    e.Handled = true;
                    return;
                case Key.Tab:
                case Key.Enter when !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    AcceptCompletionFromUi();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    _vm.DismissCompletionCommand.Execute(null);
                    e.Handled = true;
                    return;
            }
        }

        // После обычных клавиш обновить caret на следующем тике.
        Avalonia.Threading.Dispatcher.UIThread.Post(SyncCaretFromBox, Avalonia.Threading.DispatcherPriority.Input);
    }

    private void OnCompletionDoubleTapped(object? sender, TappedEventArgs e)
    {
        AcceptCompletionFromUi();
        AliasCommandBox.Focus();
    }

    private void AcceptCompletionFromUi()
    {
        if (_vm?.SelectedCompletionItem is null) return;
        _vm.AcceptCompletionCommand.Execute(_vm.SelectedCompletionItem);
        AliasCommandBox.CaretIndex = _vm.AliasCommandCaretIndex;
        _vm.FinishAcceptCompletionCaret(AliasCommandBox.CaretIndex);
    }

    private void SyncCaretFromBox()
    {
        if (_vm is null || !_vm.ShouldSyncCaretFromView) return;
        _vm.NotifyCommandCaret(AliasCommandBox.CaretIndex);
    }

    private void RebuildLivePreviewInlines()
    {
        var inlines = new InlineCollection();
        if (_vm is not null)
        {
            foreach (var segment in _vm.LivePreviewSegments)
            {
                inlines.Add(new Run(segment.Text)
                {
                    Foreground = BrushFor(segment.Kind),
                    FontWeight = segment.Kind is AliasPreviewTokenKind.Variable
                        or AliasPreviewTokenKind.Keyword
                        ? FontWeight.Bold
                        : segment.Kind == AliasPreviewTokenKind.OnUsing
                            ? FontWeight.SemiBold
                            : FontWeight.Normal
                });
            }
        }

        LivePreviewText.Inlines = inlines;
    }

    private static IBrush BrushFor(AliasPreviewTokenKind kind)
    {
        var key = kind switch
        {
            AliasPreviewTokenKind.Variable => "AliasVariableBrush",
            AliasPreviewTokenKind.Keyword => "AliasComplexBrush",
            AliasPreviewTokenKind.OnUsing => "AliasCompoundBrush",
            AliasPreviewTokenKind.Separator => "AliasConditionalBrush",
            AliasPreviewTokenKind.Placeholder => "MutedBrush",
            _ => "PreviewCommandBrush"
        };

        // Копия цвета: DynamicResource-кисть не обновляет уже созданные Run при смене темы.
        var brush = ThemeBrushResolver.Get(key);
        return brush is ISolidColorBrush solid
            ? new SolidColorBrush(solid.Color)
            : brush;
    }
}
