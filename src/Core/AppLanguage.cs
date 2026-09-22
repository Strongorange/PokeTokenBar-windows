using System.Globalization;

namespace PokeTokenBar.Core;

public enum AppLanguage
{
    Ko, En, Ja, Es, Fr, Pt, De
}

public static class AppLanguages
{
    public static readonly AppLanguage[] All =
        [AppLanguage.Ko, AppLanguage.En, AppLanguage.Ja, AppLanguage.Es, AppLanguage.Fr, AppLanguage.Pt, AppLanguage.De];

    public static string Raw(AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => "ko",
        AppLanguage.En => "en",
        AppLanguage.Ja => "ja",
        AppLanguage.Es => "es",
        AppLanguage.Fr => "fr",
        AppLanguage.Pt => "pt",
        AppLanguage.De => "de",
        _ => "en"
    };

    public static AppLanguage? Parse(string? raw)
    {
        if (raw is null) return null;
        foreach (var lang in All)
            if (Raw(lang) == raw) return lang;
        return null;
    }

    public static string[] ApiCodes(this AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => ["ko"],
        AppLanguage.En => ["en"],
        AppLanguage.Ja => ["ja-hrkt", "ja"],
        AppLanguage.Es => ["es"],
        AppLanguage.Fr => ["fr"],
        AppLanguage.Pt => ["pt-br", "pt"],
        AppLanguage.De => ["de"],
        _ => ["en"]
    };

    public static string Label(this AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => "한국어",
        AppLanguage.En => "English",
        AppLanguage.Ja => "日本語",
        AppLanguage.Es => "Español",
        AppLanguage.Fr => "Français",
        AppLanguage.Pt => "Português",
        AppLanguage.De => "Deutsch",
        _ => "English"
    };

    public static string? ResolveName(this AppLanguage lang, IReadOnlyDictionary<string, string> byLang) =>
        PokemonNameLocalization.Resolve(byLang, lang.ApiCodes());

    public static AppLanguage SystemDefault() =>
        SystemDefault(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public static AppLanguage SystemDefault(string? preferredLanguage)
    {
        var prefix = preferredLanguage?[..Math.Min(2, preferredLanguage.Length)].ToLowerInvariant();
        return prefix switch
        {
            "ko" => AppLanguage.Ko,
            "ja" => AppLanguage.Ja,
            "es" => AppLanguage.Es,
            "fr" => AppLanguage.Fr,
            "pt" => AppLanguage.Pt,
            "de" => AppLanguage.De,
            _ => AppLanguage.En
        };
    }
}

public static class PokemonNameLocalization
{
    public static string LanguageCode(string raw) =>
        raw.Trim().ToLowerInvariant().Replace("_", "-");

    public static Dictionary<string, string> ResolveCodes(IReadOnlyDictionary<string, string> names)
    {
        var normalized = new Dictionary<string, string>();
        foreach (var key in names.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var value = names[key].Trim();
            if (value.Length > 0) normalized[LanguageCode(key)] = value;
        }
        return normalized;
    }

    public static string? Resolve(IReadOnlyDictionary<string, string> names, IEnumerable<string> preferredCodes)
    {
        var normalized = ResolveCodes(names);
        foreach (var code in preferredCodes.Append("en"))
        {
            if (normalized.TryGetValue(LanguageCode(code), out var name)) return name;
        }
        return null;
    }

    public static string Identifier(string raw) =>
        string.Join(" ", raw.Split('-').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
}
