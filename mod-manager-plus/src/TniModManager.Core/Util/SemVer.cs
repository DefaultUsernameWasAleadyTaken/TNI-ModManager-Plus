namespace TniModManager.Core.Util;

public static class SemVer
{
    public static int Compare(string? version1, string? version2)
    {
        try
        {
            var v1 = Parse(version1);
            var v2 = Parse(version2);
            for (var i = 0; i < 3; i++)
            {
                if (v1[i] > v2[i]) return 1;
                if (v1[i] < v2[i]) return -1;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool IsNewer(string? candidate, string? current) =>
        Compare(candidate, current) > 0;

    private static int[] Parse(string? version)
    {
        var parts = (version ?? "0").Split('.', StringSplitOptions.RemoveEmptyEntries);
        var result = new int[3];
        for (var i = 0; i < 3; i++)
            result[i] = i < parts.Length && int.TryParse(parts[i], out var n) ? n : 0;
        return result;
    }
}
