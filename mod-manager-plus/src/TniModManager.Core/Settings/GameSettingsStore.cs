using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TniModManager.Core.Paths;

namespace TniModManager.Core.Settings;

public sealed class GameSettingsStore
{
    private readonly GamePaths _paths;
    private JsonObject _root = new();

    public GameSettingsStore(GamePaths paths) => _paths = paths;

    public Dictionary<string, string> CmdAliases { get; private set; } = new(StringComparer.Ordinal);

    public void Load()
    {
        CmdAliases = new Dictionary<string, string>(StringComparer.Ordinal);
        _root = new JsonObject();
        if (!File.Exists(_paths.SettingsPath)) return;
        try
        {
            var text = File.ReadAllText(_paths.SettingsPath);
            _root = JsonNode.Parse(text) as JsonObject ?? new JsonObject();
            if (_root["cmd_alias"] is JsonObject aliases)
            {
                foreach (var (k, v) in aliases)
                {
                    if (v != null)
                        CmdAliases[k] = v.ToString();
                }
            }
        }
        catch
        {
            _root = new JsonObject();
        }
    }

    public void SaveAliases(IDictionary<string, string> aliases)
    {
        CmdAliases = new Dictionary<string, string>(aliases, StringComparer.Ordinal);
        var node = new JsonObject();
        foreach (var (k, v) in CmdAliases.OrderBy(x => x.Key, StringComparer.Ordinal))
            node[k] = v;
        _root["cmd_alias"] = node;
        Directory.CreateDirectory(_paths.GameDataPath);
        var json = _root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_paths.SettingsPath, json, new UTF8Encoding(false));
    }
}

public static class GameLauncher
{
    public static void LaunchSteamGame(int appId = GamePaths.SteamAppId)
    {
        var uri = $"steam://rungameid/{appId}";
        Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true
        });
    }
}
