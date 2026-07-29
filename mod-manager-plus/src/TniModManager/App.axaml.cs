using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using TniModManager.Core.Paths;
using TniModManager.Core.Settings;
using TniModManager.Localization;
using TniModManager.ViewModels;
using TniModManager.Views;

namespace TniModManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = GamePaths.Create();
            var uiSettings = new AppUiSettings(paths);
            uiSettings.Load();
            RequestedThemeVariant = uiSettings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
            LocalizationManager.Apply(uiSettings.Language);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(uiSettings),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
