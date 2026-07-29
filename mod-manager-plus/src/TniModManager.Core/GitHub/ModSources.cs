using System.Reflection;
using System.Text.Json;

namespace TniModManager.Core.GitHub;

/// <summary>
/// Список GitHub-репозиториев с релизами модов.
/// Источник: embedded mod-sources.json (опционально override файлом рядом с exe).
/// </summary>
public static class ModSources
{
    public const string FileName = "mod-sources.json";
    private const string EmbeddedLogicalName = "TniModManager.Core.mod-sources.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly string[] FallbackRepos =
    [
        "CJFWeatherhead/TNI-Mods"
    ];

    public static IReadOnlyList<string> GetRepositories(string? baseDirectory = null)
    {
        var fromFile = TryLoadFromFile(baseDirectory);
        if (fromFile is { Count: > 0 })
            return fromFile;

        var fromEmbedded = TryLoadFromEmbedded();
        if (fromEmbedded is { Count: > 0 })
            return fromEmbedded;

        return FallbackRepos;
    }

    public static IReadOnlyList<string> ParseJson(string json)
    {
        var data = JsonSerializer.Deserialize<ModSourcesData>(json, JsonOptions);
        return Normalize(data?.ModRepositories);
    }

    private static IReadOnlyList<string>? TryLoadFromFile(string? baseDirectory)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        var path = Path.Combine(dir, FileName);
        if (!File.Exists(path))
            return null;
        try
        {
            return ParseJson(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string>? TryLoadFromEmbedded()
    {
        var asm = typeof(ModSources).Assembly;
        var stream = asm.GetManifestResourceStream(EmbeddedLogicalName);
        if (stream is null)
        {
            var match = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(FileName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                stream = asm.GetManifestResourceStream(match);
        }

        if (stream is null)
            return null;

        try
        {
            using (stream)
            using (var reader = new StreamReader(stream))
                return ParseJson(reader.ReadToEnd());
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? repos)
    {
        if (repos is null)
            return [];

        return repos
            .Select(NormalizeRepo)
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Принимает owner/repo или полный URL GitHub.</summary>
    public static string NormalizeRepo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var s = value.Trim().TrimEnd('/');
        if (s.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            s = s["https://github.com/".Length..];
        else if (s.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
            s = s["http://github.com/".Length..];

        var slash = s.IndexOf('/');
        if (slash <= 0 || slash == s.Length - 1)
            return "";

        var rest = s[(slash + 1)..];
        var next = rest.IndexOf('/');
        if (next >= 0)
            rest = rest[..next];

        return $"{s[..slash]}/{rest}";
    }

    private sealed class ModSourcesData
    {
        public List<string>? ModRepositories { get; set; }
    }
}
