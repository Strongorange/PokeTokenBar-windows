using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows;

public sealed class AppSettings
{
    public double GrowthDifficulty { get; set; } = PokemonBalance.DefaultDifficulty;
    public double ShopDifficulty { get; set; } = PokemonBalance.DefaultDifficulty;
    public bool PetEnabled { get; set; }
    public double? PetX { get; set; }
    public double? PetY { get; set; }
    public double PetSize { get; set; } = AppSettingsFile.DefaultPetSize;
}

public static class AppSettingsFile
{
    public const string FileName = "settings.json";
    public const double PetSizeMinimum = 48;
    public const double PetSizeMaximum = 384;
    public const double DefaultPetSize = 96;

    public static double ClampPetSize(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, PetSizeMinimum, PetSizeMaximum) : DefaultPetSize;

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
                if (root.TryGetProperty("petEnabled", out var petEnabled)
                    && petEnabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    settings.PetEnabled = petEnabled.GetBoolean();
                if (root.TryGetProperty("petX", out var petX) && petX.ValueKind == JsonValueKind.Number)
                    settings.PetX = petX.GetDouble();
                if (root.TryGetProperty("petY", out var petY) && petY.ValueKind == JsonValueKind.Number)
                    settings.PetY = petY.GetDouble();
                if (root.TryGetProperty("petSize", out var petSize) && petSize.ValueKind == JsonValueKind.Number)
                    settings.PetSize = petSize.GetDouble();
            }
            settings.GrowthDifficulty = PokemonBalance.ClampDifficulty(settings.GrowthDifficulty);
            settings.ShopDifficulty = PokemonBalance.ClampDifficulty(settings.ShopDifficulty);
            settings.PetSize = ClampPetSize(settings.PetSize);
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
            ["petEnabled"] = settings.PetEnabled,
            ["petX"] = settings.PetX,
            ["petY"] = settings.PetY,
            ["petSize"] = ClampPetSize(settings.PetSize),
        };
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json.ToJsonString(IndentedJson));
        File.Move(temp, path, overwrite: true);
    }
}
