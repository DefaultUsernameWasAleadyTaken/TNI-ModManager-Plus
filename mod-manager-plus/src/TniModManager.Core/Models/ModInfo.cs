namespace TniModManager.Core.Models;

public sealed class ModInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public string GameVersion { get; set; } = "stable";
    public string LastUpdated { get; set; } = "";
    public string Status { get; set; } = "Active Development";
    public ModSource Source { get; set; }
    public string? FolderPath { get; set; }
    public string? FolderId { get; set; }
    public bool HasUpdate { get; set; }
    public string? RemoteVersion { get; set; }
    public string? RemoteNotes { get; set; }
    public List<ParameterDef> Parameters { get; set; } = [];
    public bool HasUiConfigPs1 { get; set; }
    public string? ReleaseTag { get; set; }
    public string? ZipUrl { get; set; }
}
