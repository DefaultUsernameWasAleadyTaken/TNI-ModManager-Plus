using TniModManager.Core.Aliases;
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
        Assert.Contains("DefaultUsernameWasAleadyTaken/TNI-MM-Mods", repos);
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

public class AliasAnalyzerTests
{
    [Fact]
    public void Analyze_VariableAndSuffixes()
    {
        var info = AliasAnalyzer.Analyze("scan $1 $2 on $1 using $2");
        Assert.Equal(AliasKind.Variable, info.Kind);
        Assert.Equal(new[] { 1, 2 }, info.Variables);
        Assert.Equal(2, info.MaxVariable);
        Assert.True(info.HasOn);
        Assert.True(info.HasUsing);
    }

    [Fact]
    public void Analyze_Complex()
    {
        var info = AliasAnalyzer.Analyze("try a on $1 then b; c else d");
        Assert.Equal(AliasKind.Complex, info.Kind);
        Assert.True(info.IsCompound);
        Assert.True(info.HasTryThen);
    }

    [Fact]
    public void Preview_FullUsage_AddsMissingSuffixes()
    {
        var info = AliasAnalyzer.Analyze("ping $1");
        var usage = AliasPreviewBuilder.BuildFullUsage("probe", info);
        Assert.Equal("probe 192.168.1.1 on 192.168.1.100 using 192.168.1.50", usage);
    }

    [Fact]
    public void Preview_Invocation_Shape()
    {
        var info = AliasAnalyzer.Analyze("scan $1 $2");
        Assert.Equal("> probe <arg1> <arg2> {on <device>} {using <debugger>}",
            AliasPreviewBuilder.BuildInvocation("probe", info));
    }
}
