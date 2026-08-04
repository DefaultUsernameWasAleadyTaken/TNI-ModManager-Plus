using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using TniModManager.Localization;

namespace TniModManager.Views;

public partial class ModsView : UserControl
{
    public ModsView()
    {
        InitializeComponent();
        FilterBar.LayoutUpdated += (_, _) => SyncSidebarWidth();
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        SyncSidebarWidth();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) =>
        LocalizationManager.LanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged() =>
        Dispatcher.UIThread.Post(SyncSidebarWidth, DispatcherPriority.Loaded);

    /// <summary>Ширина сайдбара = intrinsic-ширина ряда фильтров (текст не обрезается).</summary>
    private void SyncSidebarWidth()
    {
        FilterBar.Measure(Size.Infinity);
        var width = FilterBar.DesiredSize.Width;
        if (width < 1)
            width = FilterBar.Bounds.Width;
        if (width < 1)
            return;

        if (double.IsNaN(SidebarRoot.Width) || Math.Abs(SidebarRoot.Width - width) > 0.5)
            SidebarRoot.Width = width;
    }
}
