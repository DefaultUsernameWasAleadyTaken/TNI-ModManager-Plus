using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using TniModManager.Core.Models;
using TniModManager.Core.Paths;
using TniModManager.Core.Util;

namespace TniModManager.Core.GitHub;

public sealed record AppReleaseAsset(string Name, string DownloadUrl, long Size);

public sealed record AppReleaseInfo(
    string TagName,
    string Version,
    string HtmlUrl,
    IReadOnlyList<AppReleaseAsset> Assets);

public sealed class GitHubReleaseClient
{
    private static readonly Regex TagRegex = new(@"^(.+)-v(\d+\.\d+\.\d+)$", RegexOptions.Compiled);
    private readonly HttpClient _http;

    public GitHubReleaseClient(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TNI-ModManager-Plus", GamePaths.ModManagerVersion));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<Dictionary<string, ModReleaseInfo>> GetLatestModReleasesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ModReleaseInfo>(StringComparer.OrdinalIgnoreCase);
        var repos = ModSources.GetRepositories();
        Exception? lastError = null;
        var anySuccess = false;

        foreach (var repo in repos)
        {
            try
            {
                await FetchRepoReleasesAsync(repo, result, cancellationToken).ConfigureAwait(false);
                anySuccess = true;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (!anySuccess && lastError is not null)
            throw lastError;

        return result;
    }

    private async Task FetchRepoReleasesAsync(
        string repo,
        Dictionary<string, ModReleaseInfo> result,
        CancellationToken cancellationToken)
    {
        const int perPage = 100;
        const int maxPages = 5;

        for (var page = 1; page <= maxPages; page++)
        {
            var uri = $"https://api.github.com/repos/{repo}/releases?per_page={perPage}&page={page}";
            using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                break;

            foreach (var release in doc.RootElement.EnumerateArray())
                TryAddRelease(release, result);

            if (doc.RootElement.GetArrayLength() < perPage)
                break;
        }
    }

    private static void TryAddRelease(JsonElement release, Dictionary<string, ModReleaseInfo> result)
    {
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        var match = TagRegex.Match(tag);
        if (!match.Success) return;

        var modId = match.Groups[1].Value;
        var version = match.Groups[2].Value;
        if (!release.TryGetProperty("assets", out var assets)) return;

        JsonElement? zip = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                zip = asset;
                break;
            }
        }
        if (zip is null) return;

        var info = new ModReleaseInfo
        {
            ModId = modId,
            Version = version,
            TagName = tag,
            DownloadUrl = zip.Value.GetProperty("browser_download_url").GetString() ?? "",
            AssetName = zip.Value.GetProperty("name").GetString() ?? "",
            Size = zip.Value.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
            PublishedAt = release.TryGetProperty("published_at", out var pub) && DateTimeOffset.TryParse(pub.GetString(), out var dt) ? dt : null,
            ReleaseNotes = release.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            HtmlUrl = release.TryGetProperty("html_url", out var html) ? html.GetString() ?? "" : "",
        };

        if (!result.TryGetValue(modId, out var existing) || SemVer.IsNewer(version, existing.Version))
            result[modId] = info;
    }

    public async Task<AppReleaseInfo> GetLatestAppReleaseAsync(CancellationToken cancellationToken = default)
    {
        var uri = $"https://api.github.com/repos/{GamePaths.AppGitHubRepo}/releases/latest";
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var versionMatch = Regex.Match(tag, @"\d+\.\d+\.\d+");
        var version = versionMatch.Success ? versionMatch.Value : tag.TrimStart('v', 'V');
        var assets = new List<AppReleaseAsset>();
        if (root.TryGetProperty("assets", out var assetsElement))
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                assets.Add(new AppReleaseAsset(
                    asset.GetProperty("name").GetString() ?? "",
                    asset.GetProperty("browser_download_url").GetString() ?? "",
                    asset.TryGetProperty("size", out var size) ? size.GetInt64() : 0));
            }
        }

        return new AppReleaseInfo(
            tag,
            version,
            root.TryGetProperty("html_url", out var html) ? html.GetString() ?? "" : "",
            assets);
    }

    public async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(destinationPath);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            readTotal += read;
            if (total > 0)
                progress?.Report(100.0 * readTotal / total);
        }
        progress?.Report(100);
    }
}
