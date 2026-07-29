using Avalonia.Controls;
using Avalonia.Interactivity;
using TniModManager.ViewModels;

namespace TniModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.InitializeAsync();
    }

    private void OnFilterAll(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.FilterMode = "All";
    }

    private void OnFilterInstalled(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.FilterMode = "Installed";
    }

    private void OnFilterAvailable(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.FilterMode = "Available";
    }
}
