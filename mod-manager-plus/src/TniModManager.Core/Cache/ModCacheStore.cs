using System.Text.Json;
using System.Text.Json.Serialization;
using TniModManager.Core.Models;
using TniModManager.Core.Paths;

namespace TniModManager.Core.Cache;

public sealed class ModCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly GamePaths _paths;
    private Dictionary<string, ModCacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public ModCacheStore(GamePaths paths) => _paths = paths;

    public IReadOnlyDictionary<string, ModCacheEntry> Entries => _entries;

    public void Load()
    {
        _entries = new Dictionary<string, ModCacheEntry>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_paths.ModCachePath)) return;
        try
        {
            var json = File.ReadAllText(_paths.ModCachePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, ModCacheEntry>>(json, JsonOptions);
            if (raw != null)
                _entries = new Dictionary<string, ModCacheEntry>(raw, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // keep empty
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(_paths.GameDataPath);
        var json = JsonSerializer.Serialize(_entries, JsonOptions);
        File.WriteAllText(_paths.ModCachePath, json);
    }

    public void SetDownloaded(string modId, string version)
    {
        _entries[modId] = new ModCacheEntry
        {
            Source = ModSource.Downloaded,
            Version = version,
            FolderId = modId,
            InstalledAt = DateTimeOffset.UtcNow
        };
        Save();
    }

    public void Remove(string modId)
    {
        if (_entries.Remove(modId)) Save();
    }

    public ModCacheEntry? TryGet(string idOrFolder)
    {
        if (_entries.TryGetValue(idOrFolder, out var e)) return e;
        return null;
    }
}
