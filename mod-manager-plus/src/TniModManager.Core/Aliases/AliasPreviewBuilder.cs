using System.Text;

namespace TniModManager.Core.Aliases;

/// <summary>Тексты preview редактора алиасов (legacy Update-AliasPreview).</summary>
public static class AliasPreviewBuilder
{
    private static readonly string[] SampleArgs =
    [
        "192.168.1.1",
        "10.0.0.2",
        "backup.conf",
        "archive.bak"
    ];

    public static string BuildInvocation(string? aliasName, AliasInfo info)
    {
        var name = string.IsNullOrWhiteSpace(aliasName) ? "<alias_name>" : aliasName.Trim();
        var sb = new StringBuilder("> ").Append(name);

        for (var i = 1; i <= info.MaxVariable; i++)
            sb.Append(" <arg").Append(i).Append('>');

        if (!info.HasOn)
            sb.Append(" {on <device>}");
        if (!info.HasUsing)
            sb.Append(" {using <debugger>}");

        return sb.ToString();
    }

    public static string BuildFullUsage(string? aliasName, AliasInfo info)
    {
        var name = string.IsNullOrWhiteSpace(aliasName) ? "<alias>" : aliasName.Trim();
        var sb = new StringBuilder(name);

        for (var i = 1; i <= info.MaxVariable; i++)
        {
            sb.Append(' ');
            sb.Append(i <= SampleArgs.Length ? SampleArgs[i - 1] : $"arg{i}");
        }

        if (!info.HasOn)
            sb.Append(" on 192.168.1.100");
        if (!info.HasUsing)
            sb.Append(" using 192.168.1.50");

        return sb.ToString();
    }

    public static string FormatVariablesList(AliasInfo info)
    {
        if (info.Variables.Count == 0)
            return "";

        return string.Join(" ", info.Variables.Select(v => $"${v}"));
    }

    public static string SampleArgument(int index1Based) =>
        index1Based >= 1 && index1Based <= SampleArgs.Length
            ? SampleArgs[index1Based - 1]
            : $"arg{index1Based}";
}
