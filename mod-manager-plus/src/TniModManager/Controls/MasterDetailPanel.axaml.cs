using Avalonia;
using Avalonia.Controls;

namespace TniModManager.Controls;

public partial class MasterDetailPanel : UserControl
{
    public static readonly StyledProperty<object?> SidebarContentProperty =
        AvaloniaProperty.Register<MasterDetailPanel, object?>(nameof(SidebarContent));

    public static readonly StyledProperty<object?> DetailContentProperty =
        AvaloniaProperty.Register<MasterDetailPanel, object?>(nameof(DetailContent));

    public static readonly StyledProperty<double> SidebarMinWidthProperty =
        AvaloniaProperty.Register<MasterDetailPanel, double>(nameof(SidebarMinWidth), 240);

    public MasterDetailPanel()
    {
        InitializeComponent();
    }

    public object? SidebarContent
    {
        get => GetValue(SidebarContentProperty);
        set => SetValue(SidebarContentProperty, value);
    }

    public object? DetailContent
    {
        get => GetValue(DetailContentProperty);
        set => SetValue(DetailContentProperty, value);
    }

    public double SidebarMinWidth
    {
        get => GetValue(SidebarMinWidthProperty);
        set => SetValue(SidebarMinWidthProperty, value);
    }
}
