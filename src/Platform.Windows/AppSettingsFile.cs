using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows;

public sealed class AppSettings
{
    public double GrowthDifficulty { get; set; } = PokemonBalance.DefaultDifficulty;
    public double ShopDifficulty { get; set; } = PokemonBalance.DefaultDifficulty;
}

public static class AppSettingsFile
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static string DefaultPath()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("PTB_STATE_DIR");
        var directory = string.IsNullOrWhiteSpace(overrideDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PokeTokenBar")
            : overrideDirectory;
        return Path.Combine(directory, FileName);
    }

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var settings = new AppSettings();
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("growthDifficulty", out var growth)
                    && growth.ValueKind == JsonValueKind.Number)
                    settings.GrowthDifficulty = growth.GetDouble();
                if (root.TryGetProperty("shopDifficulty", out var shop)
                    && shop.ValueKind == JsonValueKind.Number)
                    settings.ShopDifficulty = shop.GetDouble();
            }
            settings.GrowthDifficulty = PokemonBalance.ClampDifficulty(settings.GrowthDifficulty);
            settings.ShopDifficulty = PokemonBalance.ClampDifficulty(settings.ShopDifficulty);
            return settings;
        }
        catch (Exception ex)
        {
            AppLog.Write($"app settings decode failed — using defaults: {path}: {ex.Message}");
            return new AppSettings();
        }
    }

    public static void Save(string path, AppSettings settings)
    {
        var json = new System.Text.Json.Nodes.JsonObject
        {
            ["growthDifficulty"] = PokemonBalance.ClampDifficulty(settings.GrowthDifficulty),
            ["shopDifficulty"] = PokemonBalance.ClampDifficulty(settings.ShopDifficulty),
        };
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json.ToJsonString(IndentedJson));
        File.Move(temp, path, overwrite: true);
    }
}
