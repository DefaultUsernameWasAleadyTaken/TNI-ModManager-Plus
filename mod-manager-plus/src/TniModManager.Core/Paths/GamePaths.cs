using System.Runtime.InteropServices;

namespace TniModManager.Core.Paths;

public sealed class GamePaths
{
    public const string ConfigFileName = "entry.lua";
    public const string ManagedMarkerFileName = "mod.managed";
    public const int SteamAppId = 2939600;
    public const string ModManagerVersion = "3.8.0";
    public const string GitHubRepo = "CJFWeatherhead/TNI-Mods";

    public string GameDataPath { get; }
    public string ModsDirectory { get; }
    public string DisabledModsDirectory { get; }
    public string SettingsPath { get; }
    public string ModCachePath { get; }

    public GamePaths(string gameDataPath, string modsDirectory, string disabledModsDirectory)
    {
        GameDataPath = gameDataPath;
        ModsDirectory = modsDirectory;
        DisabledModsDirectory = disabledModsDirectory;
        SettingsPath = Path.Combine(gameDataPath, "settings.json");
        ModCachePath = Path.Combine(gameDataPath, "mod_cache.json");
    }

    public static GamePaths Create()
    {
        string gameDataPath;
        string modsName;
        string disabledName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            gameDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Godot", "app_userdata", "Tower Networking Inc");
            modsName = "Mods";
            disabledName = "Mods_Disabled";
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            gameDataPath = Path.Combine(
                home, ".local", "share", "godot", "app_userdata", "Tower Networking Inc");
            modsName = "mods";
            disabledName = "mods_disabled";
        }

        return new GamePaths(
            gameDataPath,
            Path.Combine(gameDataPath, modsName),
            Path.Combine(gameDataPath, disabledName));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(GameDataPath);
        Directory.CreateDirectory(ModsDirectory);
        Directory.CreateDirectory(DisabledModsDirectory);
    }
}
