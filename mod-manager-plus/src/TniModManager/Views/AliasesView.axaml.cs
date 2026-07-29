using Avalonia.Controls;

namespace TniModManager.Views;

public partial class AliasesView : UserControl
{
    public AliasesView()
    {
        InitializeComponent();
        // ScrollViewer иначе отдаёт StackPanel ∞ по ширине → текст без правого отступа.
        AliasEditorScroll.SizeChanged += (_, e) =>
        {
            AliasEditorContent.MaxWidth = e.NewSize.Width > 0 ? e.NewSize.Width : double.PositiveInfinity;
        };
    }
}
