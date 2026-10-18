namespace PokeTokenBar.Core;

public static class SpriteCatalog
{
    public const string PokemonBase = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon";
    public const string AnimatedPath = "versions/generation-v/black-white/animated";

    public static string AssetName(int speciesID, PokemonForm? form) =>
        PokemonForms.AssetName(speciesID, form);

    /// <summary>PokeAPI names form GIFs `201-b`/`492-sky`; static PNGs of separate varieties use the
    /// variety id (`10006.png`). Cache files keep the `<id>-<form>` name either way.</summary>
    public static string StaticAssetName(int speciesID, PokemonForm? form) =>
        PokemonForms.StaticAssetName(speciesID, form);

    public static string CacheKey(int speciesID, bool animated, bool shiny, PokemonForm? form = null) =>
        $"{AssetName(speciesID, form)}-{(shiny ? "sh" : "")}{(animated ? "a" : "s")}";

    public static string FileExtension(bool animated) => animated ? "gif" : "png";

    public static string CacheFileName(int speciesID, bool animated, bool shiny, PokemonForm? form = null) =>
        $"{CacheKey(speciesID, animated, shiny, form)}.{FileExtension(animated)}";

    public static string SpriteUrl(int speciesID, bool animated, bool shiny, PokemonForm? form = null)
    {
        var name = animated ? AssetName(speciesID, form) : StaticAssetName(speciesID, form);
        var path = animated ? $"{AnimatedPath}/" : "";
        var color = shiny ? "shiny/" : "";
        return $"{PokemonBase}/{path}{color}{name}.{FileExtension(animated)}";
    }

    public const string EggCacheKey = "egg";

    public static string EggUrl => $"{PokemonBase}/egg.png";

    public const string ItemsBase = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items";

    public static string ItemCacheKey(string name) => $"item-{name}";

    public static string ItemUrl(string name) => $"{ItemsBase}/{name}.png";
}
