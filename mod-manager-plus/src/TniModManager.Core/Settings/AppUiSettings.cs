using System.Text.Json;
using TniModManager.Core.Paths;

namespace TniModManager.Core.Settings;

public sealed class AppUiSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly GamePaths _paths;

    public AppUiSettings(GamePaths paths) => _paths = paths;

    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en";

    public void Load()
    {
        Theme = "Dark";
        Language = "en";
        if (!File.Exists(_paths.UiSettingsPath)) return;
        try
        {
            var json = File.ReadAllText(_paths.UiSettingsPath);
            var data = JsonSerializer.Deserialize<AppUiSettingsData>(json, JsonOptions);
            if (data is null) return;
            if (!string.IsNullOrWhiteSpace(data.Theme))
                Theme = data.Theme;
            if (!string.IsNullOrWhiteSpace(data.Language))
                Language = data.Language;
        }
        catch
        {
            // keep defaults
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(_paths.GameDataPath);
        var data = new AppUiSettingsData { Theme = Theme, Language = Language };
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(_paths.UiSettingsPath, json);
    }

    private sealed class AppUiSettingsData
    {
        public string Theme { get; set; } = "Dark";
        public string Language { get; set; } = "en";
    }
}
