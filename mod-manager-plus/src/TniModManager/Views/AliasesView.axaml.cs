using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TniModManager.ViewModels;

namespace TniModManager.Views;

public partial class AliasesView : UserControl
{
    private AliasesViewModel? _vm;
    private bool _popupClosing;

    public AliasesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AliasCommandBox.GotFocus += (_, _) => SyncCaretFromBox();
        AliasCommandBox.PointerReleased += (_, _) => SyncCaretFromBox();
        AliasCommandBox.LostFocus += OnAliasCommandLostFocus;
        AliasCommandBox.TextChanged += OnAliasCommandTextChanged;
        AliasCommandBox.KeyDown += OnAliasCommandKeyDown;
        AliasCommandBox.KeyUp += (_, _) => SyncCaretFromBox();
        AliasCommandBox.PropertyChanged += OnAliasCommandBoxPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.RequestCaretIndex -= OnRequestCaretIndex;

        _vm = DataContext as AliasesViewModel;
        if (_vm is null)
            return;

        _vm.RequestCaretIndex += OnRequestCaretIndex;
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
            var clamped = Math.Clamp(caretIndex, 0, AliasCommandBox.Text?.Length ?? 0);
            // Сначала caret, потом Focus: иначе GotFocus синкает старую позицию и сбивает шаг.
            AliasCommandBox.CaretIndex = clamped;
            if (!AliasCommandBox.IsFocused)
                AliasCommandBox.Focus();
            AliasCommandBox.CaretIndex = clamped;
            _vm?.FinishAcceptCompletionCaret(AliasCommandBox.CaretIndex);
        }
        catch
        {
            // TextBox может быть ещё не готов.
        }
    }

    private void OnAliasCommandTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_vm is { ShouldSyncCaretFromView: false })
            return;
        SyncCaretFromBox();
    }

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
            // Popup уже разобран.
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
            // Не ронять UI.
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
            // Гонка при закрытии редактора.
        }
    }
}
