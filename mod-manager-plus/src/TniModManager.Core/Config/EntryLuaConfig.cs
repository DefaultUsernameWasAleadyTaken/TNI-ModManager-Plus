using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TniModManager.Core.Config;

public static class EntryLuaConfig
{
    private static readonly Regex SectionRegex = new(
        @"(?s)(-- ===== MOD CONFIGURATION START =====.*?local config = \{)(.*?)(\}.*?-- ===== MOD CONFIGURATION END =====)",
        RegexOptions.Compiled);

    private static readonly Regex LineRegex = new(
        @"^\s*(\w+)\s*=\s*(.+?),?\s*(--.*)?$",
        RegexOptions.Compiled);

    public static Dictionary<string, object?> Read(string entryLuaPath)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!File.Exists(entryLuaPath)) return result;

        var content = File.ReadAllText(entryLuaPath);
        var match = SectionRegex.Match(content);
        if (!match.Success) return result;

        foreach (var rawLine in match.Groups[2].Value.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--", StringComparison.Ordinal))
                continue;

            var m = LineRegex.Match(line);
            if (!m.Success) continue;

            var key = m.Groups[1].Value;
            var value = m.Groups[2].Value.Trim().TrimEnd(',');
            var commentIdx = value.IndexOf("--", StringComparison.Ordinal);
            if (commentIdx >= 0) value = value[..commentIdx].Trim().TrimEnd(',');

            result[key] = ParseValue(value);
        }

        return result;
    }

    public static bool Write(string entryLuaPath, IDictionary<string, object?> config)
    {
        if (!File.Exists(entryLuaPath)) return false;
        var content = File.ReadAllText(entryLuaPath);
        var match = SectionRegex.Match(content);
        if (!match.Success) return false;

        var sb = new StringBuilder();
        sb.AppendLine();
        foreach (var key in config.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.Append("    ");
            sb.Append(key);
            sb.Append(" = ");
            sb.Append(FormatValue(config[key]));
            sb.Append(',');
            sb.AppendLine();
        }

        var newContent = match.Groups[1].Value + sb + match.Groups[3].Value;
        File.WriteAllText(entryLuaPath, newContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return true;
    }

    private static object? ParseValue(string value)
    {
        if (value is "true") return true;
        if (value is "false") return false;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            return value[1..^1];
        return value.Trim('"');
    }

    private static string FormatValue(object? value) => value switch
    {
        bool b => b ? "true" : "false",
        int or long => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        float or double or decimal => Convert.ToDouble(value, CultureInfo.InvariantCulture)
            .ToString("0.0#####", CultureInfo.InvariantCulture),
        null => "\"\"",
        _ => $"\"{value}\""
    };
}
