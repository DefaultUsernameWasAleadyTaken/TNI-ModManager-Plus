using System.Text.Json;
using TniModManager.Core.Models;
using TniModManager.Core.Paths;

namespace TniModManager.Core.Cache;

/// <summary>Дисковый кэш каталога релизов GitHub — чтобы при rate limit не терять список.</summary>
public sealed class ReleaseCatalogCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly GamePaths _paths;

    public ReleaseCatalogCache(GamePaths paths) => _paths = paths;

    public DateTimeOffset? SavedAt { get; private set; }

    public Dictionary<string, ModReleaseInfo> Load()
    {
        SavedAt = null;
        if (!File.Exists(_paths.ReleaseCachePath))
            return new Dictionary<string, ModReleaseInfo>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(_paths.ReleaseCachePath);
            var file = JsonSerializer.Deserialize<ReleaseCacheFile>(json, JsonOptions);
            if (file?.Releases is null || file.Releases.Count == 0)
                return new Dictionary<string, ModReleaseInfo>(StringComparer.OrdinalIgnoreCase);

            SavedAt = file.SavedAt;
            return new Dictionary<string, ModReleaseInfo>(file.Releases, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ModReleaseInfo>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Save(IReadOnlyDictionary<string, ModReleaseInfo> releases)
    {
        if (releases.Count == 0)
            return;

        Directory.CreateDirectory(_paths.GameDataPath);
        SavedAt = DateTimeOffset.UtcNow;
        var file = new ReleaseCacheFile
        {
            SavedAt = SavedAt,
            Releases = new Dictionary<string, ModReleaseInfo>(releases, StringComparer.OrdinalIgnoreCase)
        };
        File.WriteAllText(_paths.ReleaseCachePath, JsonSerializer.Serialize(file, JsonOptions));
    }

    private sealed class ReleaseCacheFile
    {
        public DateTimeOffset? SavedAt { get; set; }
        public Dictionary<string, ModReleaseInfo>? Releases { get; set; }
    }
}
