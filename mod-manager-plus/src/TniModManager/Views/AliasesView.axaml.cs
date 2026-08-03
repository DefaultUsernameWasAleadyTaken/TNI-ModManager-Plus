using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using TniModManager.Core.Aliases;
using TniModManager.ViewModels;

namespace TniModManager.Views;

public partial class AliasesView : UserControl
{
    private AliasesViewModel? _vm;
    private bool _popupClosing;

    public AliasesView()
    {
        InitializeComponent();
        AliasEditorScroll.SizeChanged += (_, e) =>
        {
            if (AliasEditorContent is null) return;
            AliasEditorContent.MaxWidth = e.NewSize.Width > 0 ? e.NewSize.Width : double.PositiveInfinity;
        };
        DataContextChanged += OnDataContextChanged;
        AliasCommandBox.GotFocus += OnAliasCommandCaretChanged;
        AliasCommandBox.PointerReleased += OnAliasCommandPointerReleased;
        AliasCommandBox.LostFocus += OnAliasCommandLostFocus;
        AliasCommandBox.TextChanged += OnAliasCommandTextChanged;
        AliasCommandBox.KeyDown += OnAliasCommandKeyDown;
        AliasCommandBox.KeyUp += OnAliasCommandKeyUp;
        AliasCommandBox.PropertyChanged += OnAliasCommandBoxPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.LivePreviewSegments.CollectionChanged -= OnPreviewSegmentsChanged;
            _vm.RequestCaretIndex -= OnRequestCaretIndex;
        }

        _vm = DataContext as AliasesViewModel;
        if (_vm is null)
            return;

        _vm.LivePreviewSegments.CollectionChanged += OnPreviewSegmentsChanged;
        _vm.RequestCaretIndex += OnRequestCaretIndex;
        RebuildLivePreviewInlines();
    }

    private void OnAliasCommandBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.CaretIndexProperty)
            SyncCaretFromBox();
    }

    private void OnRequestCaretIndex(int caretIndex)
    {
        try
        {
            AliasCommandBox.Focus();
            AliasCommandBox.CaretIndex = Math.Clamp(caretIndex, 0, AliasCommandBox.Text?.Length ?? 0);
            _vm?.FinishAcceptCompletionCaret(AliasCommandBox.CaretIndex);
        }
        catch
        {
            // TextBox может быть ещё не в дереве при смене DataContext.
        }
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

    private void OnAliasCommandKeyUp(object? sender, KeyEventArgs e) =>
        SyncCaretFromBox();

    private void OnAliasCommandLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (_vm is { IsCompletionOpen: true } && CompletionList is { IsFocused: true })
                return;
            SafeDismissCompletion();
        }, DispatcherPriority.Background);
    }

    private void OnCompletionPopupClosed(object? sender, EventArgs e)
    {
        if (_popupClosing || _vm is not { IsCompletionOpen: true })
            return;

        _popupClosing = true;
        try
        {
            // Отложить: иначе Closed → Clear Items → reentrancy в Popup Avalonia.
            Dispatcher.UIThread.Post(SafeDismissCompletion, DispatcherPriority.Background);
        }
        finally
        {
            _popupClosing = false;
        }
    }

    private void SafeDismissCompletion()
    {
        try
        {
            _vm?.DismissCompletionCommand.Execute(null);
        }
        catch
        {
            // Игнор: popup уже разобран.
        }
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
                    SafeDismissCompletion();
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _vm.OpenCompletionCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnCompletionDoubleTapped(object? sender, TappedEventArgs e)
    {
        AcceptCompletionFromUi();
        AliasCommandBox.Focus();
    }

    private void AcceptCompletionFromUi()
    {
        if (_vm?.SelectedCompletionItem is null) return;
        try
        {
            _vm.AcceptCompletionCommand.Execute(_vm.SelectedCompletionItem);
            AliasCommandBox.CaretIndex = _vm.AliasCommandCaretIndex;
            _vm.FinishAcceptCompletionCaret(AliasCommandBox.CaretIndex);
        }
        catch
        {
            // Не ронять UI при сбое применения completion.
        }
    }

    private void SyncCaretFromBox()
    {
        if (_vm is null || !_vm.ShouldSyncCaretFromView) return;
        try
        {
            _vm.NotifyCommandCaret(AliasCommandBox.CaretIndex);
        }
        catch
        {
            // Защита от гонок при закрытии редактора.
        }
    }

    private void RebuildLivePreviewInlines()
    {
        if (LivePreviewText is null)
            return;

        try
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
        catch
        {
            // Ignore: контрол может быть в процессе разборки.
        }
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

        var brush = ThemeBrushResolver.Get(key);
        return brush is ISolidColorBrush solid
            ? new SolidColorBrush(solid.Color)
            : brush;
    }
}
