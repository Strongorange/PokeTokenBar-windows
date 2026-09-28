using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Platform.Windows.Tests;

[Collection("Platform diagnostics")]
public class AppSettingsFileTests : IDisposable
{
    private readonly string _dir;

    public AppSettingsFileTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-settings-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    [Fact]
    public void MissingFileYieldsDefaultDifficulty()
    {
        var settings = AppSettingsFile.Load(SettingsPath);

        Assert.Equal(PokemonBalance.DefaultDifficulty, settings.GrowthDifficulty);
        Assert.Equal(PokemonBalance.DefaultDifficulty, settings.ShopDifficulty);
    }

    [Fact]
    public void RoundTripsDifficultiesAcrossRestart()
    {
        var saved = new AppSettings { GrowthDifficulty = 1.5, ShopDifficulty = 0.2 };
        AppSettingsFile.Save(SettingsPath, saved);

        var loaded = AppSettingsFile.Load(SettingsPath);

        Assert.Equal(1.5, loaded.GrowthDifficulty);
        Assert.Equal(0.2, loaded.ShopDifficulty);
    }

    [Fact]
    public void OutOfRangeAndCorruptValuesFallBackToClampedDefaults()
    {
        File.WriteAllText(SettingsPath,
            """{"growthDifficulty": 9.0, "shopDifficulty": -4}""");
        var clamped = AppSettingsFile.Load(SettingsPath);
        Assert.Equal(PokemonBalance.DifficultyUpperBound, clamped.GrowthDifficulty);
        Assert.Equal(PokemonBalance.DifficultyLowerBound, clamped.ShopDifficulty);

        File.WriteAllText(SettingsPath, "{ not json");
        var corrupt = AppSettingsFile.Load(SettingsPath);
        Assert.Equal(PokemonBalance.DefaultDifficulty, corrupt.GrowthDifficulty);
        Assert.Equal(PokemonBalance.DefaultDifficulty, corrupt.ShopDifficulty);
        Assert.Contains("app settings decode failed", Diagnostics.Read(_dir));
    }

    [Fact]
    public void DefaultPathHonorsStateDirOverride()
    {
        var previous = Environment.GetEnvironmentVariable("PTB_STATE_DIR");
        try
        {
            Environment.SetEnvironmentVariable("PTB_STATE_DIR", _dir);
            Assert.Equal(SettingsPath, AppSettingsFile.DefaultPath());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PTB_STATE_DIR", previous);
        }
    }

    [Fact]
    public void MissingFileYieldsPetDefaults()
    {
        var settings = AppSettingsFile.Load(SettingsPath);

        Assert.False(settings.PetEnabled);
        Assert.Null(settings.PetX);
        Assert.Null(settings.PetY);
        Assert.Equal(AppSettingsFile.DefaultPetSize, settings.PetSize);
    }

    [Fact]
    public void RoundTripsPetPreferencesAcrossRestart()
    {
        var saved = new AppSettings
        {
            PetEnabled = true,
            PetX = 1234.5,
            PetY = -42,
            PetSize = 128,
        };
        AppSettingsFile.Save(SettingsPath, saved);

        var loaded = AppSettingsFile.Load(SettingsPath);

        Assert.True(loaded.PetEnabled);
        Assert.Equal(1234.5, loaded.PetX);
        Assert.Equal(-42, loaded.PetY);
        Assert.Equal(128, loaded.PetSize);
    }

    [Fact]
    public void RoundTripsDisabledPetWithNoPosition()
    {
        var saved = new AppSettings { PetEnabled = false };
        AppSettingsFile.Save(SettingsPath, saved);

        var loaded = AppSettingsFile.Load(SettingsPath);

        Assert.False(loaded.PetEnabled);
        Assert.Null(loaded.PetX);
        Assert.Null(loaded.PetY);
    }

    [Fact]
    public void OutOfRangePetSizeClampsAndGarbageFallsBack()
    {
        File.WriteAllText(SettingsPath,
            """{"petEnabled": true, "petSize": 9999, "petX": 10}""");
        var clamped = AppSettingsFile.Load(SettingsPath);
        Assert.Equal(AppSettingsFile.PetSizeMaximum, clamped.PetSize);
        Assert.True(clamped.PetEnabled);
        Assert.Equal(10, clamped.PetX);
        Assert.Null(clamped.PetY);

        File.WriteAllText(SettingsPath, """{"petSize": 1}""");
        var floored = AppSettingsFile.Load(SettingsPath);
        Assert.Equal(AppSettingsFile.PetSizeMinimum, floored.PetSize);
    }

    [Fact]
    public void MissingFileYieldsNoScanRoots()
    {
        var settings = AppSettingsFile.Load(SettingsPath);

        Assert.Empty(settings.ScanRoots);
    }

    [Fact]
    public void RoundTripsScanRootsAcrossRestart()
    {
        var saved = new AppSettings
        {
            ScanRoots =
            [
                new ScanRootEntry("claude", @"D:\logs\claude"),
                new ScanRootEntry("codex", @"D:\logs\codex"),
            ],
        };
        AppSettingsFile.Save(SettingsPath, saved);

        var loaded = AppSettingsFile.Load(SettingsPath);

        Assert.Equal(
            new ScanRootEntry("claude", @"D:\logs\claude"), loaded.ScanRoots[0]);
        Assert.Equal(
            new ScanRootEntry("codex", @"D:\logs\codex"), loaded.ScanRoots[1]);
    }

    [Fact]
    public void ScanRootGarbageEntriesAreDropped()
    {
        File.WriteAllText(SettingsPath, """
            {"scanRoots": [
                {"provider": "claude", "path": "D:\\logs\\ok"},
                {"provider": "unknown-provider", "path": "D:\\logs\\x"},
                {"provider": " CLAUDE ", "path": "D:\\logs\\normalized"},
                {"provider": "codex"},
                {"path": "D:\\logs\\no-provider"},
                "not-an-object"
            ]}
            """);
        var loaded = AppSettingsFile.Load(SettingsPath);

        Assert.Equal(2, loaded.ScanRoots.Count);
        Assert.Equal(new ScanRootEntry("claude", @"D:\logs\ok"), loaded.ScanRoots[0]);
        Assert.Equal(new ScanRootEntry("claude", @"D:\logs\normalized"), loaded.ScanRoots[1]);
    }

    [Fact]
    public void SavingDropsInvalidScanRoots()
    {
        var saved = new AppSettings
        {
            ScanRoots =
            [
                new ScanRootEntry("claude", @"D:\logs\keep"),
                new ScanRootEntry("bogus", @"D:\logs\drop"),
                new ScanRootEntry("opencode", "   "),
            ],
        };
        AppSettingsFile.Save(SettingsPath, saved);

        var loaded = AppSettingsFile.Load(SettingsPath);

        var entry = Assert.Single(loaded.ScanRoots);
        Assert.Equal(new ScanRootEntry("claude", @"D:\logs\keep"), entry);
    }
}
