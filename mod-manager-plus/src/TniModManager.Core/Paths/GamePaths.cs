using System.Reflection;
using System.Runtime.InteropServices;

namespace TniModManager.Core.Paths;

public sealed class GamePaths
{
    public const string ConfigFileName = "entry.lua";
    public const string ManagedMarkerFileName = "mod.managed";
    public const int SteamAppId = 2939600;
    public const string AppDisplayName = "Mod Manager Plus";
    /// <summary>Релизы приложения (self-update). Каталог модов — mod-sources.json.</summary>
    public const string AppGitHubRepo = "DefaultUsernameWasAleadyTaken/TNI-ModManager-Plus";

    /// <summary>Версия приложения из Version.props (InformationalVersion сборки Core).</summary>
    public static string ModManagerVersion
    {
        get
        {
            var info = typeof(GamePaths).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                var plus = info.IndexOf('+');
                return plus >= 0 ? info[..plus] : info;
            }

            var v = typeof(GamePaths).Assembly.GetName().Version;
            return v is null ? "0.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public string GameDataPath { get; }
    public string ModsDirectory { get; }
    public string SettingsPath { get; }
    public string ModCachePath { get; }
    public string ReleaseCachePath { get; }
    public string UiSettingsPath { get; }

    public GamePaths(string gameDataPath, string modsDirectory)
    {
        GameDataPath = gameDataPath;
        ModsDirectory = modsDirectory;
        SettingsPath = Path.Combine(gameDataPath, "settings.json");
        ModCachePath = Path.Combine(gameDataPath, "mod_cache.json");
        ReleaseCachePath = Path.Combine(gameDataPath, "release_cache.json");
        UiSettingsPath = Path.Combine(gameDataPath, "mm_plus_ui.json");
    }

    public static GamePaths Create()
    {
        string gameDataPath;
        string modsName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            gameDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Godot", "app_userdata", "Tower Networking Inc");
            modsName = "Mods";
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            gameDataPath = Path.Combine(
                home, ".local", "share", "godot", "app_userdata", "Tower Networking Inc");
            modsName = "mods";
        }

        return new GamePaths(
            gameDataPath,
            Path.Combine(gameDataPath, modsName));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(GameDataPath);
        Directory.CreateDirectory(ModsDirectory);
    }
}
