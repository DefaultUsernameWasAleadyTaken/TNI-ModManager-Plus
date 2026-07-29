using System.Text.Json;
using System.Text.RegularExpressions;
using TniModManager.Core.Models;
using YamlDotNet.RepresentationModel;

namespace TniModManager.Core.Config;

public static class MetadataReader
{
    public static (Dictionary<string, object?> Meta, List<ParameterDef> Parameters)? TryReadFolder(string folderPath)
    {
        var jsoncPath = Path.Combine(folderPath, "mod.jsonc");
        var yamlPath = Path.Combine(folderPath, "metadata.yaml");

        Dictionary<string, object?>? meta = null;
        List<ParameterDef> parameters = [];

        if (File.Exists(jsoncPath))
            meta = TryParseModJsonc(File.ReadAllText(jsoncPath));

        Dictionary<string, object?>? yaml = null;
        if (File.Exists(yamlPath))
            yaml = TryParseMetadataYaml(File.ReadAllText(yamlPath), out parameters);

        if (meta is null && yaml is null) return null;

        meta ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (yaml != null)
        {
            foreach (var key in new[] { "Parameters", "Notes", "Development Status", "Last Updated", "Creation Date", "Game Version Supported", "Image", "Name", "Author", "Description", "Version", "ID" })
            {
                if (yaml.TryGetValue(key, out var val) && val is not null)
                {
                    if (!meta.ContainsKey(key) || meta[key] is null || (meta[key] is string s && string.IsNullOrEmpty(s)))
                        meta[key] = val;
                }
            }
            if (parameters.Count == 0 && yaml.TryGetValue("_parameters", out var p) && p is List<ParameterDef> list)
                parameters = list;
        }

        return (meta, parameters);
    }

    public static Dictionary<string, object?>? TryParseModJsonc(string content)
    {
        try
        {
            var lines = content.Split('\n').Select(line =>
            {
                var trimmed = Regex.Replace(line, @"^\s*//.*$", "");
                return Regex.Replace(trimmed, @"(?<!"")//(?!.*"").*$", "");
            });
            var json = string.Join('\n', lines);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var authors = root.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString()).Where(x => x != null))
                : "Unknown";
            var description = root.TryGetProperty("description", out var d)
                ? d.ValueKind == JsonValueKind.Array
                    ? string.Join("\n", d.EnumerateArray().Select(x => x.GetString()))
                    : d.GetString() ?? ""
                : "";

            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = root.TryGetProperty("id", out var id) ? id.GetString() : "",
                ["Name"] = root.TryGetProperty("name", out var name) ? name.GetString() : "",
                ["Author"] = authors,
                ["Version"] = root.TryGetProperty("version", out var ver) ? ver.GetString() : "",
                ["Description"] = description,
                ["Development Status"] = "Active Development",
                ["Game Version Supported"] = "stable",
                ["Last Updated"] = "",
            };
        }
        catch
        {
            return null;
        }
    }

    public static Dictionary<string, object?>? TryParseMetadataYaml(string content, out List<ParameterDef> parameters)
    {
        parameters = [];
        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(content));
            if (yaml.Documents.Count == 0) return null;
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (keyNode, valueNode) in root.Children)
            {
                var key = ((YamlScalarNode)keyNode).Value ?? "";
                if (key.Equals("Parameters", StringComparison.OrdinalIgnoreCase) && valueNode is YamlSequenceNode seq)
                {
                    parameters = ParseParameters(seq);
                    dict["_parameters"] = parameters;
                    continue;
                }
                dict[key] = ScalarOrString(valueNode);
            }
            return dict;
        }
        catch
        {
            return null;
        }
    }

    private static List<ParameterDef> ParseParameters(YamlSequenceNode seq)
    {
        var list = new List<ParameterDef>();
        foreach (var item in seq.Children.OfType<YamlMappingNode>())
        {
            var p = new ParameterDef();
            foreach (var (k, v) in item.Children)
            {
                var key = ((YamlScalarNode)k).Value ?? "";
                switch (key)
                {
                    case "Name": p.Name = ScalarOrString(v)?.ToString() ?? ""; break;
                    case "Label": p.Label = ScalarOrString(v)?.ToString() ?? ""; break;
                    case "Type": p.Type = ScalarOrString(v)?.ToString() ?? "string"; break;
                    case "Description": p.Description = ScalarOrString(v)?.ToString() ?? ""; break;
                    case "Default":
                        p.Default = ToJsonElement(v);
                        break;
                    case "Options" when v is YamlSequenceNode opts:
                        p.Options = opts.Children.Select(c => ScalarOrString(c)?.ToString() ?? "").ToList();
                        break;
                    case "Min" when double.TryParse(ScalarOrString(v)?.ToString(), out var min):
                        p.Min = min; break;
                    case "Max" when double.TryParse(ScalarOrString(v)?.ToString(), out var max):
                        p.Max = max; break;
                }
            }
            if (string.IsNullOrEmpty(p.Label)) p.Label = p.Name;
            list.Add(p);
        }
        return list;
    }

    private static object? ScalarOrString(YamlNode node) => node switch
    {
        YamlScalarNode s => s.Value,
        YamlSequenceNode seq => string.Join("\n", seq.Children.Select(c => ScalarOrString(c)?.ToString())),
        _ => node.ToString()
    };

    private static JsonElement? ToJsonElement(YamlNode node)
    {
        try
        {
            var text = ScalarOrString(node)?.ToString() ?? "";
            if (text is "true" or "false")
                return JsonSerializer.SerializeToElement(text == "true");
            if (long.TryParse(text, out var l))
                return JsonSerializer.SerializeToElement(l);
            if (double.TryParse(text, out var d))
                return JsonSerializer.SerializeToElement(d);
            return JsonSerializer.SerializeToElement(text);
        }
        catch
        {
            return null;
        }
    }
}
