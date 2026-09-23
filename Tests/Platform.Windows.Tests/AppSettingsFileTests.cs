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
}
