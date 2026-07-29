using System.Text.Json;
using TniModManager.Core.Cache;
using TniModManager.Core.Config;
using TniModManager.Core.Models;
using TniModManager.Core.Paths;
using TniModManager.Core.Util;

namespace TniModManager.Core.Mods;

public sealed class ModDiscovery
{
    private readonly GamePaths _paths;
    private readonly ModCacheStore _cache;

    public ModDiscovery(GamePaths paths, ModCacheStore cache)
    {
        _paths = paths;
        _cache = cache;
    }

    public List<ModInfo> GetInstalledMods()
    {
        var mods = new List<ModInfo>();
        ScanDirectory(_paths.ModsDirectory, mods);
        return mods;
    }

    public List<ModInfo> MergeWithReleases(
        IReadOnlyList<ModInfo> installed,
        IReadOnlyDictionary<string, ModReleaseInfo> releases)
    {
        var list = new List<ModInfo>();
        var installedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in installed)
        {
            installedIds.Add(mod.Id);
            if (!string.IsNullOrEmpty(mod.FolderId))
                installedIds.Add(mod.FolderId);

            var releaseKey = releases.ContainsKey(mod.FolderId ?? "") ? mod.FolderId!
                : releases.ContainsKey(mod.Id) ? mod.Id : null;

            if (releaseKey != null && releases.TryGetValue(releaseKey, out var rel))
            {
                mod.RemoteVersion = rel.Version;
                mod.RemoteNotes = rel.ReleaseNotes;
                mod.ReleaseTag = rel.TagName;
                mod.ZipUrl = rel.DownloadUrl;
                mod.HtmlUrl = FirstUrl(rel.HtmlUrl, ReleasePageFromDownloadUrl(rel.DownloadUrl));
                if (mod.Source == ModSource.Downloaded && SemVer.IsNewer(rel.Version, mod.Version))
                    mod.HasUpdate = true;
            }
            list.Add(mod);
        }

        foreach (var (id, rel) in releases)
        {
            if (installedIds.Contains(id)) continue;
            list.Add(new ModInfo
            {
                Id = id,
                Name = id,
                Version = rel.Version,
                Description = rel.ReleaseNotes,
                Source = ModSource.Available,
                RemoteVersion = rel.Version,
                RemoteNotes = rel.ReleaseNotes,
                ReleaseTag = rel.TagName,
                ZipUrl = rel.DownloadUrl,
                HtmlUrl = FirstUrl(rel.HtmlUrl, ReleasePageFromDownloadUrl(rel.DownloadUrl)),
            });
        }

        return list.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? FirstUrl(params string?[] candidates) =>
        candidates.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

    /// <summary>
    /// https://github.com/owner/repo/releases/download/tag/file.zip
    /// → https://github.com/owner/repo/releases/tag/tag
    /// </summary>
    private static string? ReleasePageFromDownloadUrl(string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return null;

        const string marker = "/releases/download/";
        var idx = downloadUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var after = downloadUrl[(idx + marker.Length)..];
        var slash = after.IndexOf('/');
        if (slash <= 0)
            return null;

        var tag = after[..slash];
        var repoRoot = downloadUrl[..idx];
        return $"{repoRoot}/releases/tag/{tag}";
    }

    private void ScanDirectory(string root, List<ModInfo> mods)
    {
        if (!Directory.Exists(root)) return;
        foreach (var folder in Directory.GetDirectories(root))
        {
            var folderId = Path.GetFileName(folder);
            var parsed = MetadataReader.TryReadFolder(folder);
            if (parsed is null) continue;
            var (meta, parameters) = parsed.Value;

            var id = Str(meta, "ID") is { Length: > 0 } mid ? mid : folderId;
            var version = Str(meta, "Version") ?? "";
            var source = ModSource.Manual;
            var markerPath = Path.Combine(folder, GamePaths.ManagedMarkerFileName);
            if (File.Exists(markerPath))
            {
                source = ModSource.Downloaded;
                try
                {
                    var marker = JsonSerializer.Deserialize<ManagedMarker>(File.ReadAllText(markerPath));
                    if (!string.IsNullOrEmpty(marker?.InstalledVersion))
                        version = marker!.InstalledVersion;
                }
                catch { /* ignore */ }
            }
            else
            {
                var cache = _cache.TryGet(id) ?? _cache.TryGet(folderId);
                if (cache != null)
                {
                    source = cache.Source;
                    if (!string.IsNullOrEmpty(cache.Version))
                        version = cache.Version;
                }
            }

            mods.Add(new ModInfo
            {
                Id = id,
                FolderId = folderId,
                FolderPath = folder,
                Name = Str(meta, "Name") ?? folderId,
                Author = Str(meta, "Author") ?? "Unknown",
                Description = Str(meta, "Description") ?? "",
                Version = version,
                GameVersion = Str(meta, "Game Version Supported") ?? "stable",
                LastUpdated = Str(meta, "Last Updated") ?? "",
                Status = Str(meta, "Development Status") ?? "Active Development",
                Source = source,
                Parameters = parameters,
                HasUiConfigPs1 = File.Exists(Path.Combine(folder, "ui-config.ps1")),
            });
        }
    }

    private static string? Str(Dictionary<string, object?> meta, string key) =>
        meta.TryGetValue(key, out var v) ? v?.ToString() : null;
}
