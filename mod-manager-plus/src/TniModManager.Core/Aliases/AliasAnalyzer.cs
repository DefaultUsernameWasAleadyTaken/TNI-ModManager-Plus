namespace TniModManager.Core.Aliases;

public enum AliasKind
{
    Plain,
    Variable,
    Compound,
    Conditional,
    Complex
}

public static class AliasAnalyzer
{
    public static AliasKind Analyze(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return AliasKind.Plain;
        var hasVar = command.Contains('$');
        var hasSemi = command.Contains(';');
        var hasTry = command.Contains("try", StringComparison.OrdinalIgnoreCase)
                     || command.Contains("then", StringComparison.OrdinalIgnoreCase)
                     || command.Contains("else", StringComparison.OrdinalIgnoreCase);

        var flags = (hasVar ? 1 : 0) + (hasSemi ? 1 : 0) + (hasTry ? 1 : 0);
        if (flags >= 2) return AliasKind.Complex;
        if (hasTry) return AliasKind.Conditional;
        if (hasSemi) return AliasKind.Compound;
        if (hasVar) return AliasKind.Variable;
        return AliasKind.Plain;
    }

    public static string KindColor(AliasKind kind) => kind switch
    {
        AliasKind.Variable => "#FF9800",
        AliasKind.Compound => "#9C27B0",
        AliasKind.Conditional => "#3F51B5",
        AliasKind.Complex => "#E91E63",
        _ => "#607D8B"
    };
}
