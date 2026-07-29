namespace TniModManager.Core.Models;

public sealed class ModCacheEntry
{
    public ModSource Source { get; set; } = ModSource.Downloaded;
    public string Version { get; set; } = "";
    public string? FolderId { get; set; }
    public DateTimeOffset? InstalledAt { get; set; }
}
