using Avalonia.Controls;
using Avalonia.Controls.Documents;
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
