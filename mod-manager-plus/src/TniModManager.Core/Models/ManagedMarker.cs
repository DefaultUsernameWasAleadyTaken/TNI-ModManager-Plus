using System.Text.Json.Serialization;

namespace TniModManager.Core.Models;

public sealed class ManagedMarker
{
    [JsonPropertyName("managedBy")]
    public string ManagedBy { get; set; } = "TNI-ModManager-Plus";

    [JsonPropertyName("modManagerVersion")]
    public string ModManagerVersion { get; set; } = Paths.GamePaths.ModManagerVersion;

    [JsonPropertyName("folderId")]
    public string FolderId { get; set; } = "";

    [JsonPropertyName("installedVersion")]
    public string InstalledVersion { get; set; } = "";

    [JsonPropertyName("installedAt")]
    public DateTimeOffset InstalledAt { get; set; } = DateTimeOffset.UtcNow;
}
