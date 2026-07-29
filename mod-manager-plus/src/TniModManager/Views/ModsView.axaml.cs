using Avalonia.Controls;

namespace TniModManager.Views;

public partial class ModsView : UserControl
{
    public ModsView()
    {
        InitializeComponent();
        // ScrollViewer иначе отдаёт TextBlock ∞ по ширине → описание вылезает за край.
        ModDescriptionScroll.SizeChanged += (_, e) =>
        {
            if (e.NewSize.Width > 0)
                ModDescriptionText.MaxWidth = Math.Max(40, e.NewSize.Width - 12);
        };
    }
}
