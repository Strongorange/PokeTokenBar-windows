namespace PokeTokenBar.Core;

/// <summary>
/// The 18 main-series types, keyed by PokeAPI's canonical lowercase English
/// name. macOS Badge.swift (#391) 포팅.
/// </summary>
public enum PokemonType
{
    Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison, Ground,
    Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy
}

public static class PokemonTypes
{
    /// <summary>
    /// Parses a PokeAPI type name (case-insensitive). null for the 18 outside
    /// the chart (stellar, shadow, unknown spellings) — callers fall back to a
    /// neutral capsule instead of guessing a color.
    /// </summary>
    public static PokemonType? FromApiName(string? apiName) =>
        apiName is not null && Map.TryGetValue(apiName, out var type)
            ? type
            : null;

    /// <summary>Classic type-chart colors (0xRRGGBB).</summary>
    public static uint Hex(PokemonType type) => type switch
    {
        PokemonType.Normal => 0xAAAA99,
        PokemonType.Fire => 0xFF4422,
        PokemonType.Water => 0x3399FF,
        PokemonType.Electric => 0xFFCC33,
        PokemonType.Grass => 0x77CC55,
        PokemonType.Ice => 0x66CCFF,
        PokemonType.Fighting => 0xBB5544,
        PokemonType.Poison => 0xAA5599,
        PokemonType.Ground => 0xDDBB55,
        PokemonType.Flying => 0x8899FF,
        PokemonType.Psychic => 0xFF5599,
        PokemonType.Bug => 0xAABB22,
        PokemonType.Rock => 0xBBAA66,
        PokemonType.Ghost => 0x6666BB,
        PokemonType.Dragon => 0x7766EE,
        PokemonType.Dark => 0x775544,
        PokemonType.Steel => 0xAAAABB,
        PokemonType.Fairy => 0xEE99EE,
        _ => 0x888888
    };

    private static readonly Dictionary<string, PokemonType> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["normal"] = PokemonType.Normal,
        ["fire"] = PokemonType.Fire,
        ["water"] = PokemonType.Water,
        ["electric"] = PokemonType.Electric,
        ["grass"] = PokemonType.Grass,
        ["ice"] = PokemonType.Ice,
        ["fighting"] = PokemonType.Fighting,
        ["poison"] = PokemonType.Poison,
        ["ground"] = PokemonType.Ground,
        ["flying"] = PokemonType.Flying,
        ["psychic"] = PokemonType.Psychic,
        ["bug"] = PokemonType.Bug,
        ["rock"] = PokemonType.Rock,
        ["ghost"] = PokemonType.Ghost,
        ["dragon"] = PokemonType.Dragon,
        ["dark"] = PokemonType.Dark,
        ["steel"] = PokemonType.Steel,
        ["fairy"] = PokemonType.Fairy
    };
}
