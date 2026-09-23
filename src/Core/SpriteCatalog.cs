namespace PokeTokenBar.Core;

public static class SpriteCatalog
{
    public const string PokemonBase = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon";
    public const string AnimatedPath = "versions/generation-v/black-white/animated";

    public static string AssetName(int speciesID, UnownForm? unownForm)
    {
        var resolved = UnownForms.Resolved(speciesID, unownForm);
        if (resolved is null || resolved == UnownForm.A) return speciesID.ToString();
        return $"{speciesID}-{UnownForms.Raw(resolved.Value)}";
    }

    public static string CacheKey(int speciesID, bool animated, bool shiny, UnownForm? unownForm = null) =>
        $"{AssetName(speciesID, unownForm)}-{(shiny ? "sh" : "")}{(animated ? "a" : "s")}";

    public static string FileExtension(bool animated) => animated ? "gif" : "png";

    public static string CacheFileName(int speciesID, bool animated, bool shiny, UnownForm? unownForm = null) =>
        $"{CacheKey(speciesID, animated, shiny, unownForm)}.{FileExtension(animated)}";

    public static string SpriteUrl(int speciesID, bool animated, bool shiny, UnownForm? unownForm = null)
    {
        var name = AssetName(speciesID, unownForm);
        var path = animated ? $"{AnimatedPath}/" : "";
        var color = shiny ? "shiny/" : "";
        return $"{PokemonBase}/{path}{color}{name}.{FileExtension(animated)}";
    }

    public const string EggCacheKey = "egg";

    public static string EggUrl => $"{PokemonBase}/egg.png";
}
