using System.Text.RegularExpressions;

namespace TniModManager.Core.Aliases;

public enum AliasKind
{
    Plain,
    Variable,
    Compound,
    Conditional,
    Complex
}

public sealed class AliasInfo
{
    public static AliasInfo Empty { get; } = new()
    {
        Kind = AliasKind.Plain,
        Variables = [],
        Commands = [],
        MaxVariable = 0
    };

    public AliasKind Kind { get; init; }
    public IReadOnlyList<int> Variables { get; init; } = [];
    public int MaxVariable { get; init; }
    public bool HasOn { get; init; }
    public bool HasUsing { get; init; }
    public bool HasTryThen { get; init; }
    public bool HasElse { get; init; }
    public bool HasTryElse { get; init; }
    public bool IsCompound { get; init; }
    public IReadOnlyList<string> Commands { get; init; } = [];
}

public enum AliasPreviewTokenKind
{
    Normal,
    Variable,
    Keyword,
    OnUsing,
    Separator,
    Placeholder
}

public sealed record AliasPreviewSegment(string Text, AliasPreviewTokenKind Kind);

/// <summary>Разбор cmd_alias: тип, $n, on/using — как в legacy ModManagerGUI.ps1.</summary>
public static partial class AliasAnalyzer
{
    [GeneratedRegex(@"\$(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"\bon\s+\$?\d*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OnRegex();

    [GeneratedRegex(@"\busing\s+\$?\d*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UsingRegex();

    [GeneratedRegex(@"\balways\s+on\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlwaysOnRegex();

    [GeneratedRegex(@"\balways\s+using\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlwaysUsingRegex();

    [GeneratedRegex(@"\btry\b.*\bthen\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TryThenRegex();

    [GeneratedRegex(@"\btry\b.*\belse\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TryElseRegex();

    [GeneratedRegex(@"\belse\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ElseRegex();

    [GeneratedRegex(@"(\$\d+|\btry\b|\bthen\b|\belse\b|\bon\b|\busing\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HighlightRegex();

    public static AliasInfo Analyze(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return AliasInfo.Empty;

        var variables = VariableRegex().Matches(command)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        var hasOn = OnRegex().IsMatch(command) || AlwaysOnRegex().IsMatch(command);
        var hasUsing = UsingRegex().IsMatch(command) || AlwaysUsingRegex().IsMatch(command);
        var hasTryThen = TryThenRegex().IsMatch(command);
        var hasElse = ElseRegex().IsMatch(command);
        var hasTryElse = TryElseRegex().IsMatch(command);
        var hasConditional = hasTryThen || hasTryElse;
        var isCompound = command.Contains(';');

        IReadOnlyList<string> commands = isCompound
            ? command.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [command.Trim()];

        AliasKind kind;
        if (hasConditional && isCompound) kind = AliasKind.Complex;
        else if (hasConditional) kind = AliasKind.Conditional;
        else if (isCompound) kind = AliasKind.Compound;
        else if (variables.Count > 0) kind = AliasKind.Variable;
        else kind = AliasKind.Plain;

        return new AliasInfo
        {
            Kind = kind,
            Variables = variables,
            MaxVariable = variables.Count > 0 ? variables.Max() : 0,
            HasOn = hasOn,
            HasUsing = hasUsing,
            HasTryThen = hasTryThen,
            HasElse = hasElse,
            HasTryElse = hasTryElse,
            IsCompound = isCompound,
            Commands = commands
        };
    }

    public static IReadOnlyList<AliasPreviewSegment> HighlightCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
            return [];

        var parts = HighlightRegex().Split(command);
        var result = new List<AliasPreviewSegment>(parts.Length);
        foreach (var part in parts)
        {
            if (part.Length == 0)
                continue;

            var kind = ClassifyToken(part);
            result.Add(new AliasPreviewSegment(part, kind));
        }

        return result;
    }

    public static IReadOnlyList<AliasPreviewSegment> BuildLivePreviewSegments(AliasInfo info, string command, string emptyPlaceholder)
    {
        if (string.IsNullOrWhiteSpace(command))
            return [new AliasPreviewSegment(emptyPlaceholder, AliasPreviewTokenKind.Placeholder)];

        if (!info.IsCompound || info.Commands.Count <= 1)
            return HighlightCommand(command.Trim());

        var segments = new List<AliasPreviewSegment>();
        for (var i = 0; i < info.Commands.Count; i++)
        {
            if (i > 0)
                segments.Add(new AliasPreviewSegment(Environment.NewLine, AliasPreviewTokenKind.Separator));

            var prefix = i == 0 ? "  ┌─ " : "  └─ ";
            segments.Add(new AliasPreviewSegment(prefix, AliasPreviewTokenKind.Separator));
            segments.AddRange(HighlightCommand(info.Commands[i]));
        }

        return segments;
    }

    private static AliasPreviewTokenKind ClassifyToken(string token)
    {
        if (token.Length >= 2 && token[0] == '$' && token.Skip(1).All(char.IsDigit))
            return AliasPreviewTokenKind.Variable;

        if (token.Equals("try", StringComparison.OrdinalIgnoreCase)
            || token.Equals("then", StringComparison.OrdinalIgnoreCase)
            || token.Equals("else", StringComparison.OrdinalIgnoreCase))
            return AliasPreviewTokenKind.Keyword;

        if (token.Equals("on", StringComparison.OrdinalIgnoreCase)
            || token.Equals("using", StringComparison.OrdinalIgnoreCase))
            return AliasPreviewTokenKind.OnUsing;

        return AliasPreviewTokenKind.Normal;
    }

    public static string KindColor(AliasKind kind) => kind switch
    {
        AliasKind.Variable => "#C4923A",
        AliasKind.Compound => "#6A9B7A",
        AliasKind.Conditional => "#5B9FD4",
        AliasKind.Complex => "#C45C5C",
        _ => "#6E7888"
    };
}
