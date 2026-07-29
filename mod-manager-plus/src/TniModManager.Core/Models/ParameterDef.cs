using System.Text.Json;

namespace TniModManager.Core.Models;

public sealed class ParameterDef
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "string";
    public JsonElement? Default { get; set; }
    public string Description { get; set; } = "";
    public List<string>? Options { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}
