using Avalonia;
using Avalonia.Media;
using TniModManager.Core.Aliases;

namespace TniModManager.ViewModels;

public static class ThemeBrushResolver
{
    public static IBrush Get(string key)
    {
        var app = Application.Current;
        return app is not null &&
               app.Resources.TryGetResource(key, app.ActualThemeVariant, out var value) &&
               value is IBrush brush
            ? brush
            : Brushes.Gray;
    }

    public static IBrush GetAlias(AliasKind kind) => Get(kind switch
    {
        AliasKind.Variable => "AliasVariableBrush",
        AliasKind.Compound => "AliasCompoundBrush",
        AliasKind.Conditional => "AliasConditionalBrush",
        AliasKind.Complex => "AliasComplexBrush",
        _ => "AliasPlainBrush"
    });
}
