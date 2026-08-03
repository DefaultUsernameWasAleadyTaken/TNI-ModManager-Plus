using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TniModManager.Core.Aliases;

public enum AliasCompletionKind
{
    Command,
    Keyword,
    Program,
    DeviceType,
    Traffic,
    UserAlias
}

public sealed record AliasCompletionItem(
    string Name,
    AliasCompletionKind Kind,
    string? Hint = null,
    string? UsageLine = null);

/// <summary>Карточка справки по токену / выбранному completion.</summary>
public sealed record AliasTokenManual(
    string Name,
    AliasCompletionKind Kind,
    string? Summary,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Examples)
{
    public string? PrimaryExample
    {
        get
        {
            foreach (var raw in Examples)
            {
                var normalized = NormalizeExample(raw);
                if (!string.IsNullOrWhiteSpace(normalized))
                    return normalized;
            }

            return Usage.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
        }
    }

    public static string? NormalizeExample(string? example)
    {
        if (string.IsNullOrWhiteSpace(example))
            return null;
        var s = example.Trim();
        if (s.StartsWith("e.g.:", StringComparison.OrdinalIgnoreCase))
            s = s[5..].Trim();
        else if (s.StartsWith("e.g.", StringComparison.OrdinalIgnoreCase))
            s = s[4..].Trim().TrimStart(':').Trim();
        s = s.Trim().Trim('\'', '"');
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}

/// <summary>
/// In-game terminal / program vocabulary for alias authoring helpers.
/// Loaded from embedded alias_helper_catalog.json.
/// </summary>
public sealed partial class GameCommandCatalog
{
    private const string EmbeddedLogicalName = "TniModManager.Core.Aliases.alias_helper_catalog.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Lazy<GameCommandCatalog> DefaultLazy = new(LoadEmbedded);

    private static readonly HashSet<string> ProgramVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "install", "start", "stop", "uninstall", "describe"
    };

    public static GameCommandCatalog Default => DefaultLazy.Value;

    public IReadOnlyList<CatalogCommand> Commands { get; }
    public IReadOnlyList<CatalogProgram> Programs { get; }
    public IReadOnlyList<string> Keywords { get; }
    public IReadOnlySet<string> ReservedNames { get; }
    public IReadOnlyList<string> ScanDeviceTypes { get; }
    public IReadOnlyList<string> TrafficExamples { get; }

    private readonly Dictionary<string, CatalogCommand> _commandsByName;
    private readonly Dictionary<string, CatalogProgram> _programsByName;

    public GameCommandCatalog(
        IReadOnlyList<CatalogCommand> commands,
        IReadOnlyList<CatalogProgram> programs,
        IReadOnlyList<string> keywords,
        IReadOnlyList<string> reserved,
        IReadOnlyList<string> scanDeviceTypes,
        IReadOnlyList<string> trafficExamples)
    {
        Commands = commands;
        Programs = programs;
        Keywords = keywords;
        ReservedNames = new HashSet<string>(reserved, StringComparer.OrdinalIgnoreCase);
        ScanDeviceTypes = scanDeviceTypes;
        TrafficExamples = trafficExamples;
        _commandsByName = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        _programsByName = programs.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static GameCommandCatalog LoadEmbedded()
    {
        var asm = typeof(GameCommandCatalog).Assembly;
        var stream = asm.GetManifestResourceStream(EmbeddedLogicalName)
            ?? asm.GetManifestResourceNames()
                .Where(n => n.EndsWith("alias_helper_catalog.json", StringComparison.OrdinalIgnoreCase))
                .Select(asm.GetManifestResourceStream)
                .FirstOrDefault();

        if (stream is null)
            return new GameCommandCatalog([], [], ["try", "then", "else", "on", "using", "always"], [], [], []);

        using (stream)
        using (var reader = new StreamReader(stream))
            return Parse(reader.ReadToEnd());
    }

    public static GameCommandCatalog Parse(string json)
    {
        var data = JsonSerializer.Deserialize<CatalogDto>(json, JsonOptions) ?? new CatalogDto();
        var commands = (data.Commands ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new CatalogCommand(
                c.Name!.Trim(),
                c.Summary,
                c.Usage ?? [],
                c.Examples ?? [],
                c.RequiresOn,
                c.RequiresUsing))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var programs = (data.Programs ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new CatalogProgram(p.Name!.Trim(), Truncate(p.Summary, 120), p.InstallTotal))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var keywords = (data.Keywords ?? [])
            .Select(k => k.Name?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var reserved = (data.Reserved ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GameCommandCatalog(
            commands,
            programs,
            keywords,
            reserved,
            data.ScanDeviceTypes ?? [],
            data.TrafficExamples ?? []);
    }

    public bool IsReservedAliasName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && ReservedNames.Contains(name.Trim());

    public CatalogCommand? FindCommand(string? name) =>
        name is not null && _commandsByName.TryGetValue(name, out var c) ? c : null;

    public CatalogProgram? FindProgram(string? name) =>
        name is not null && _programsByName.TryGetValue(name, out var p) ? p : null;

    /// <summary>
    /// Suggest completions for the token at <paramref name="caretIndex"/> in <paramref name="text"/>.
    /// </summary>
    /// <param name="allowEmptyPrefix">Если true — список даже без введённого префикса (Ctrl+Space).</param>
    public IReadOnlyList<AliasCompletionItem> Suggest(
        string? text,
        int caretIndex,
        int limit = 12,
        IEnumerable<string>? userAliasNames = null,
        bool allowEmptyPrefix = false)
    {
        text ??= "";
        if (caretIndex < 0) caretIndex = 0;
        if (caretIndex > text.Length) caretIndex = text.Length;

        if (!TryGetToken(text, caretIndex, out var tokenStart, out var prefix))
            return [];

        // Не предлагать внутри $123
        if (tokenStart > 0 && text[tokenStart - 1] == '$')
            return [];

        if (prefix.Length < 1 && !allowEmptyPrefix)
            return [];

        var before = text[..tokenStart];
        var prev = GetPreviousTokens(before, 3);
        var mode = ResolveMode(prev);

        var results = new List<AliasCompletionItem>();
        switch (mode)
        {
            case SuggestMode.Programs:
                AddMatchingPrograms(results, prefix, limit);
                break;
            case SuggestMode.DeviceTypes:
                AddMatchingStrings(results, ScanDeviceTypes, prefix, AliasCompletionKind.DeviceType, limit);
                break;
            case SuggestMode.Traffic:
                AddMatchingStrings(results, TrafficExamples, prefix, AliasCompletionKind.Traffic, limit);
                break;
            default:
                AddMatchingCommands(results, prefix, limit);
                AddMatchingKeywords(results, prefix, limit - results.Count);
                if (results.Count < limit && userAliasNames is not null)
                {
                    foreach (var name in userAliasNames
                                 .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                    {
                        if (results.Count >= limit) break;
                        if (results.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        results.Add(new AliasCompletionItem(name, AliasCompletionKind.UserAlias,
                            Hint: "user alias"));
                    }
                }
                break;
        }

        return results;
    }

    /// <summary>Справка по токену под курсором или по первой команде сегмента (;).</summary>
    public AliasTokenManual? ResolveTokenManual(string? text, int caretIndex)
    {
        text ??= "";
        if (caretIndex < 0) caretIndex = 0;
        if (caretIndex > text.Length) caretIndex = text.Length;

        if (TryGetToken(text, caretIndex, out var tokenStart, out var token))
        {
            if (tokenStart >= 0 && tokenStart <= text.Length)
            {
                var end = tokenStart + token.Length;
                while (end < text.Length && IsTokenChar(text[end])) end++;
                token = text[tokenStart..end];
            }

            var byToken = ResolveNameManual(token);
            if (byToken is not null)
                return byToken;
        }

        var span = AliasAnalyzer.FindSpanAt(text, caretIndex);
        if (span is null)
            return null;

        var first = AliasAnalyzer.FirstWord(span.Value.Text);
        return ResolveNameManual(first);
    }

    public AliasTokenManual? ResolveNameManual(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_commandsByName.TryGetValue(name, out var cmd))
        {
            return new AliasTokenManual(
                cmd.Name,
                AliasCompletionKind.Command,
                cmd.Summary,
                cmd.Usage,
                cmd.Examples);
        }

        if (_programsByName.TryGetValue(name, out var prog))
        {
            var usage = prog.InstallTotal is int total
                ? (IReadOnlyList<string>)[$"install size: {total}"]
                : Array.Empty<string>();
            return new AliasTokenManual(
                prog.Name,
                AliasCompletionKind.Program,
                prog.Summary,
                usage,
                []);
        }

        if (Keywords.Any(k => k.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return new AliasTokenManual(
                name,
                AliasCompletionKind.Keyword,
                KeywordHint(name),
                [],
                []);
        }

        return null;
    }

    /// <summary>
    /// Replace the token at caret with <paramref name="completion"/>; returns new text and caret.
    /// </summary>
    public static (string Text, int Caret) ApplyCompletion(string? text, int caretIndex, string completion)
    {
        text ??= "";
        if (caretIndex < 0) caretIndex = 0;
        if (caretIndex > text.Length) caretIndex = text.Length;
        if (!TryGetToken(text, caretIndex, out var tokenStart, out var prefix))
            return (text, caretIndex);

        var tokenEnd = tokenStart + prefix.Length;
        // extend through rest of token to the right of caret
        while (tokenEnd < text.Length && IsTokenChar(text[tokenEnd]))
            tokenEnd++;

        var newText = text[..tokenStart] + completion + text[tokenEnd..];
        var newCaret = tokenStart + completion.Length;
        return (newText, newCaret);
    }

    public CommandRequirementNotice? GetRequirementNotice(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return null;

        var info = AliasAnalyzer.Analyze(commandText);
        // always on/using — глобальные суффиксы игрока; достаточно в любом месте тела.
        var globalAlwaysOn = AlwaysOnRegex().IsMatch(commandText);
        var globalAlwaysUsing = AlwaysUsingRegex().IsMatch(commandText);
        var needOn = false;
        var needUsing = false;

        foreach (var part in info.Commands)
        {
            var first = FirstWord(part);
            if (first is null) continue;
            if (!_commandsByName.TryGetValue(first, out var cmd)) continue;

            // Проверяем суффиксы по сегменту (;), а не по всему алиасу.
            var seg = AliasAnalyzer.Analyze(part);
            if (cmd.RequiresOn && !seg.HasOn && !globalAlwaysOn) needOn = true;
            if (cmd.RequiresUsing && !seg.HasUsing && !globalAlwaysUsing) needUsing = true;
        }

        if (!needOn && !needUsing)
            return null;
        return new CommandRequirementNotice(needOn, needUsing);
    }

    [GeneratedRegex(@"\balways\s+on\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlwaysOnRegex();

    [GeneratedRegex(@"\balways\s+using\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlwaysUsingRegex();

    public string? BuildContextHelp(string? text, int caretIndex)
    {
        var manual = ResolveTokenManual(text, caretIndex);
        if (manual is null)
            return null;

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(manual.Summary))
            lines.Add(manual.Summary!);
        foreach (var u in manual.Usage.Take(3))
            lines.Add(u);
        foreach (var e in manual.Examples.Take(2))
            lines.Add(e);
        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
    }

    private void AddMatchingCommands(List<AliasCompletionItem> results, string prefix, int limit)
    {
        foreach (var cmd in Commands.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (results.Count >= limit) break;
            if (!cmd.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var hint = !string.IsNullOrWhiteSpace(cmd.Summary)
                ? Truncate(cmd.Summary, 90)
                : cmd.Usage.FirstOrDefault();
            var usage = cmd.Usage.FirstOrDefault();
            results.Add(new AliasCompletionItem(cmd.Name, AliasCompletionKind.Command, hint, usage));
        }
    }

    private void AddMatchingKeywords(List<AliasCompletionItem> results, string prefix, int limit)
    {
        if (limit <= 0) return;
        var added = 0;
        foreach (var kw in Keywords)
        {
            if (added >= limit) break;
            if (!kw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (results.Any(r => r.Name.Equals(kw, StringComparison.OrdinalIgnoreCase))) continue;
            results.Add(new AliasCompletionItem(kw, AliasCompletionKind.Keyword, KeywordHint(kw)));
            added++;
        }
    }

    private void AddMatchingPrograms(List<AliasCompletionItem> results, string prefix, int limit)
    {
        foreach (var prog in Programs.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (results.Count >= limit) break;
            if (!prog.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var hint = prog.Summary;
            if (prog.InstallTotal is int total)
                hint = string.IsNullOrWhiteSpace(hint) ? $"size {total}" : $"{Truncate(hint, 70)} · size {total}";
            results.Add(new AliasCompletionItem(prog.Name, AliasCompletionKind.Program, Truncate(hint, 90)));
        }
    }

    private static string? KeywordHint(string kw) => kw.ToLowerInvariant() switch
    {
        "try" => "try <cmd> [then <ok>] [else <fail>]",
        "then" => "runs if try succeeded",
        "else" => "runs if try failed",
        "on" => "target device address",
        "using" => "debugger address",
        "always" => "always on / always using defaults",
        _ => "keyword"
    };

    private static void AddMatchingStrings(
        List<AliasCompletionItem> results,
        IReadOnlyList<string> values,
        string prefix,
        AliasCompletionKind kind,
        int limit)
    {
        foreach (var value in values)
        {
            if (results.Count >= limit) break;
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(new AliasCompletionItem(value, kind));
        }
    }

    private static SuggestMode ResolveMode(IReadOnlyList<string> prev)
    {
        if (prev.Count >= 1)
        {
            var last = prev[^1];
            if (last.Equals("with", StringComparison.OrdinalIgnoreCase))
                return SuggestMode.Traffic;
            if (last.Equals("scan", StringComparison.OrdinalIgnoreCase))
                return SuggestMode.DeviceTypes;
            if (ProgramVerbs.Contains(last)
                && prev.Count >= 2
                && prev[^2].Equals("program", StringComparison.OrdinalIgnoreCase))
                return SuggestMode.Programs;
        }

        return SuggestMode.Default;
    }

    public static bool TryGetToken(string text, int caretIndex, out int tokenStart, out string prefix)
    {
        tokenStart = caretIndex;
        prefix = "";
        if (caretIndex > text.Length) return false;

        var i = caretIndex;
        while (i > 0 && IsTokenChar(text[i - 1]))
            i--;
        tokenStart = i;
        prefix = text[i..caretIndex];
        return true;
    }

    private static List<string> GetPreviousTokens(string before, int max)
    {
        var matches = TokenRegex().Matches(before);
        var list = new List<string>();
        for (var i = matches.Count - 1; i >= 0 && list.Count < max; i--)
            list.Insert(0, matches[i].Value);
        return list;
    }

    private static string? FirstWord(string part)
    {
        var m = TokenRegex().Match(part);
        return m.Success ? m.Value : null;
    }

    private static bool IsTokenChar(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '-' or '/' or '*';

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_/\*-]*", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    private enum SuggestMode { Default, Programs, DeviceTypes, Traffic }

    private sealed class CatalogDto
    {
        public List<CommandDto>? Commands { get; set; }
        public List<ProgramDto>? Programs { get; set; }
        public List<KeywordDto>? Keywords { get; set; }
        public List<string>? Reserved { get; set; }
        [JsonPropertyName("scan_device_types")]
        public List<string>? ScanDeviceTypes { get; set; }
        [JsonPropertyName("traffic_examples")]
        public List<string>? TrafficExamples { get; set; }
    }

    private sealed class CommandDto
    {
        public string? Name { get; set; }
        public string? Summary { get; set; }
        public List<string>? Usage { get; set; }
        public List<string>? Examples { get; set; }
        [JsonPropertyName("requires_on")]
        public bool RequiresOn { get; set; }
        [JsonPropertyName("requires_using")]
        public bool RequiresUsing { get; set; }
    }

    private sealed class ProgramDto
    {
        public string? Name { get; set; }
        public string? Summary { get; set; }
        [JsonPropertyName("install_total")]
        public int? InstallTotal { get; set; }
    }

    private sealed class KeywordDto
    {
        public string? Name { get; set; }
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
    }
}

public sealed record CatalogCommand(
    string Name,
    string? Summary,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Examples,
    bool RequiresOn,
    bool RequiresUsing);

public sealed record CatalogProgram(string Name, string? Summary, int? InstallTotal);

public sealed record CommandRequirementNotice(bool NeedOn, bool NeedUsing);
