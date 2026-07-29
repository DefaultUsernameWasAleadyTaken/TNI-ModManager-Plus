using System.IO.Compression;
using System.Text.Json;
using TniModManager.Core.Cache;
using TniModManager.Core.GitHub;
using TniModManager.Core.Models;
using TniModManager.Core.Paths;

namespace TniModManager.Core.Mods;

public sealed class ModInstallService
{
    private readonly GamePaths _paths;
    private readonly ModCacheStore _cache;
    private readonly GitHubReleaseClient _github;

    public ModInstallService(GamePaths paths, ModCacheStore cache, GitHubReleaseClient github)
    {
        _paths = paths;
        _cache = cache;
        _github = github;
    }

    public async Task InstallFromReleaseAsync(
        ModReleaseInfo release,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _paths.EnsureDirectories();
        var tempZip = Path.Combine(Path.GetTempPath(), $"{release.ModId}-{release.Version}.zip");
        var target = Path.Combine(_paths.ModsDirectory, release.ModId);

        try
        {
            await _github.DownloadFileAsync(release.DownloadUrl, tempZip, progress, cancellationToken)
                .ConfigureAwait(false);
            ExtractModZip(tempZip, release.ModId, target);
            WriteManagedMarker(target, release.ModId, release.Version);
            _cache.SetDownloaded(release.ModId, release.Version);
        }
        finally
        {
            TryDelete(tempZip);
        }
    }

    public void RemoveDownloaded(string modFolderPath, string cacheKey)
    {
        if (Directory.Exists(modFolderPath))
            Directory.Delete(modFolderPath, recursive: true);
        _cache.Remove(cacheKey);
    }

    public static void ExtractModZip(string zipPath, string modId, string targetPath)
    {
        var tempExtract = Path.Combine(Path.GetTempPath(), $"tni-mod-extract-{modId}-{Guid.NewGuid():N}");
        try
        {
            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, true);
            ZipFile.ExtractToDirectory(zipPath, tempExtract);

            var innerMods = Path.Combine(tempExtract, "mods", modId);
            var innerDirect = Path.Combine(tempExtract, modId);
            string source;
            if (Directory.Exists(innerMods)) source = innerMods;
            else if (Directory.Exists(innerDirect)) source = innerDirect;
            else source = tempExtract;

            if (Directory.Exists(targetPath))
                Directory.Delete(targetPath, true);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            CopyDirectory(source, targetPath);
        }
        finally
        {
            TryDeleteDir(tempExtract);
        }
    }

    private void WriteManagedMarker(string modFolder, string folderId, string version)
    {
        var marker = new ManagedMarker
        {
            ManagedBy = "TNI-ModManager-Plus",
            ModManagerVersion = GamePaths.ModManagerVersion,
            FolderId = folderId,
            InstalledVersion = version,
            InstalledAt = DateTimeOffset.UtcNow
        };
        var path = Path.Combine(modFolder, GamePaths.ManagedMarkerFileName);
        var json = JsonSerializer.Serialize(marker);
        File.WriteAllText(path, json);
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    private static void TryDeleteDir(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { /* ignore */ }
    }
}
