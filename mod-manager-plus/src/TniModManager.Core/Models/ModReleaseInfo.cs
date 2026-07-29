namespace TniModManager.Core.Models;

public sealed class ModReleaseInfo
{
    public string ModId { get; set; } = "";
    public string Version { get; set; } = "";
    public string TagName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string AssetName { get; set; } = "";
    public long Size { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string ReleaseNotes { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
}
