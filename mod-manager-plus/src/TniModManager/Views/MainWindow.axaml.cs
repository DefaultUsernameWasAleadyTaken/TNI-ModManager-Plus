using Avalonia.Controls;
using TniModManager.ViewModels;

namespace TniModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.InitializeAsync();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        // Первый Closing: отменяем и спрашиваем; при подтверждении закрываем повторно.
        if (e.Cancel)
            return;

        if (!vm.Aliases.HasUnsavedChanges)
            return;

        e.Cancel = true;
        if (await vm.ConfirmCloseAsync().ConfigureAwait(true))
        {
            Closing -= OnClosing;
            Close();
        }
    }
}
