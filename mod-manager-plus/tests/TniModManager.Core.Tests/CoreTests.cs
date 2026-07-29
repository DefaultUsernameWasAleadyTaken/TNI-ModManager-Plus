using TniModManager.Core.Config;
using TniModManager.Core.GitHub;
using TniModManager.Core.Mods;
using TniModManager.Core.Paths;
using TniModManager.Core.Util;

namespace TniModManager.Core.Tests;

public class SemVerTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("2.0", "1.9.9", 1)]
    public void Compare_Works(string a, string b, int expected) =>
        Assert.Equal(expected, SemVer.Compare(a, b));
}

public class ModSourcesTests
{
    [Theory]
    [InlineData("CJFWeatherhead/TNI-Mods", "CJFWeatherhead/TNI-Mods")]
    [InlineData("https://github.com/DefaultUsernameWasAleadyTaken/TNI-data-extractor", "DefaultUsernameWasAleadyTaken/TNI-data-extractor")]
    [InlineData("https://github.com/owner/repo/", "owner/repo")]
    public void NormalizeRepo_Works(string input, string expected) =>
        Assert.Equal(expected, ModSources.NormalizeRepo(input));

    [Fact]
    public void ParseJson_ReadsRepositories()
    {
        var repos = ModSources.ParseJson("""
            {
              "modRepositories": [
                "CJFWeatherhead/TNI-Mods",
                "https://github.com/DefaultUsernameWasAleadyTaken/TNI-data-extractor"
              ]
            }
            """);
        Assert.Equal(2, repos.Count);
        Assert.Contains("CJFWeatherhead/TNI-Mods", repos);
        Assert.Contains("DefaultUsernameWasAleadyTaken/TNI-data-extractor", repos);
    }

    [Fact]
    public void GetRepositories_ReturnsEmbeddedDefaults()
    {
        var repos = ModSources.GetRepositories(baseDirectory: Path.GetTempPath());
        Assert.Contains("CJFWeatherhead/TNI-Mods", repos);
        Assert.Contains("DefaultUsernameWasAleadyTaken/TNI-data-extractor", repos);
    }
}

public class EntryLuaConfigTests
{
    [Fact]
    public void RoundTrip_ConfigBlock()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tni-mm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "entry.lua");
        File.WriteAllText(path, """
            -- ===== MOD CONFIGURATION START =====
            local config = {
                money_amount = 100,
                debug_logging = false,
            }
            -- ===== MOD CONFIGURATION END =====
            print("ok")
            """);

        var read = EntryLuaConfig.Read(path);
        Assert.Equal(100, Convert.ToInt32(read["money_amount"]));
        Assert.Equal(false, read["debug_logging"]);

        read["money_amount"] = 250;
        read["debug_logging"] = true;
        Assert.True(EntryLuaConfig.Write(path, read));

        var again = EntryLuaConfig.Read(path);
        Assert.Equal(250, Convert.ToInt32(again["money_amount"]));
        Assert.Equal(true, again["debug_logging"]);
    }
}

public class GamePathsTests
{
    [Fact]
    public void Create_HasExpectedLeafNames()
    {
        var paths = GamePaths.Create();
        Assert.False(string.IsNullOrWhiteSpace(paths.GameDataPath));
        Assert.True(paths.ModsDirectory.EndsWith("mods", StringComparison.OrdinalIgnoreCase)
                    || paths.ModsDirectory.EndsWith("Mods"));
        Assert.Equal(Path.Combine(paths.GameDataPath, "mm_plus_ui.json"), paths.UiSettingsPath);
    }
}

public class ZipExtractTests
{
    [Fact]
    public void Extract_DirectFolderLayout()
    {
        var root = Path.Combine(Path.GetTempPath(), "tni-zip-" + Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(root, "staging", "demo-mod");
        Directory.CreateDirectory(staging);
        File.WriteAllText(Path.Combine(staging, "entry.lua"), "-- hi");
        var zip = Path.Combine(root, "demo.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(Path.Combine(root, "staging"), zip);

        var target = Path.Combine(root, "out", "demo-mod");
        ModInstallService.ExtractModZip(zip, "demo-mod", target);
        Assert.True(File.Exists(Path.Combine(target, "entry.lua")));
    }
}
