namespace PokeTokenBar.Core;

public enum PokemonNature
{
    Hardy, Lonely, Brave, Adamant, Naughty,
    Bold, Docile, Relaxed, Impish, Lax,
    Timid, Hasty, Serious, Jolly, Naive,
    Modest, Mild, Quiet, Bashful, Rash,
    Calm, Gentle, Sassy, Careful, Quirky
}

public static class PokemonNatures
{
    public static readonly PokemonNature[] All =
    [
        PokemonNature.Hardy, PokemonNature.Lonely, PokemonNature.Brave, PokemonNature.Adamant, PokemonNature.Naughty,
        PokemonNature.Bold, PokemonNature.Docile, PokemonNature.Relaxed, PokemonNature.Impish, PokemonNature.Lax,
        PokemonNature.Timid, PokemonNature.Hasty, PokemonNature.Serious, PokemonNature.Jolly, PokemonNature.Naive,
        PokemonNature.Modest, PokemonNature.Mild, PokemonNature.Quiet, PokemonNature.Bashful, PokemonNature.Rash,
        PokemonNature.Calm, PokemonNature.Gentle, PokemonNature.Sassy, PokemonNature.Careful, PokemonNature.Quirky
    ];

    public static string Raw(PokemonNature nature) =>
        nature.ToString().ToLowerInvariant();

    public static PokemonNature? Parse(string? raw)
    {
        if (raw is null) return null;
        foreach (var nature in All)
            if (Raw(nature) == raw) return nature;
        return null;
    }

    public static string Name(PokemonNature nature, AppLanguage lang)
    {
        var names = nature switch
        {
            PokemonNature.Hardy => ("노력", "Hardy", "がんばりや", "Fuerte", "Hardi", "Esforçada", "Robust"),
            PokemonNature.Lonely => ("외로움", "Lonely", "さみしがり", "Huraña", "Solo", "Carente", "Solo"),
            PokemonNature.Brave => ("용감", "Brave", "ゆうかん", "Audaz", "Brave", "Corajosa", "Mutig"),
            PokemonNature.Adamant => ("고집", "Adamant", "いじっぱり", "Firme", "Rigide", "Teimosa", "Hart"),
            PokemonNature.Naughty => ("개구쟁이", "Naughty", "やんちゃ", "Pícara", "Mauvais", "Levada", "Frech"),
            PokemonNature.Bold => ("대담", "Bold", "ずぶとい", "Osada", "Assuré", "Ousada", "Kühn"),
            PokemonNature.Docile => ("온순", "Docile", "すなお", "Dócil", "Docile", "Dócil", "Sanft"),
            PokemonNature.Relaxed => ("무사태평", "Relaxed", "のんき", "Plácida", "Relax", "Descontraída", "Locker"),
            PokemonNature.Impish => ("장난꾸러기", "Impish", "わんぱく", "Agitada", "Malin", "Travessa", "Pfiffig"),
            PokemonNature.Lax => ("촐랑", "Lax", "のうてんき", "Floja", "Lâche", "Despreocupada", "Lasch"),
            PokemonNature.Timid => ("겁쟁이", "Timid", "おくびょう", "Miedosa", "Timide", "Medrosa", "Scheu"),
            PokemonNature.Hasty => ("성급", "Hasty", "せっかち", "Activa", "Pressé", "Apressada", "Hastig"),
            PokemonNature.Serious => ("성실", "Serious", "まじめ", "Seria", "Sérieux", "Séria", "Ernst"),
            PokemonNature.Jolly => ("명랑", "Jolly", "ようき", "Alegre", "Jovial", "Alegre", "Froh"),
            PokemonNature.Naive => ("천진난만", "Naive", "むじゃき", "Ingenua", "Naïf", "Ingênua", "Naiv"),
            PokemonNature.Modest => ("조심", "Modest", "ひかえめ", "Modesta", "Modeste", "Modesta", "Mäßig"),
            PokemonNature.Mild => ("의젓", "Mild", "おっとり", "Afable", "Doux", "Meiga", "Mild"),
            PokemonNature.Quiet => ("냉정", "Quiet", "れいせい", "Mansa", "Discret", "Discreta", "Ruhig"),
            PokemonNature.Bashful => ("수줍음", "Bashful", "てれや", "Tímida", "Pudique", "Tímida", "Zaghaft"),
            PokemonNature.Rash => ("덜렁", "Rash", "うっかりや", "Alocada", "Foufou", "Impulsiva", "Hitzig"),
            PokemonNature.Calm => ("차분", "Calm", "おだやか", "Serena", "Calme", "Calma", "Still"),
            PokemonNature.Gentle => ("얌전", "Gentle", "おとなしい", "Amable", "Gentil", "Gentil", "Zart"),
            PokemonNature.Sassy => ("건방", "Sassy", "なまいき", "Grosera", "Malpoli", "Atrevida", "Forsch"),
            PokemonNature.Careful => ("신중", "Careful", "しんちょう", "Cauta", "Prudent", "Cautelosa", "Sacht"),
            PokemonNature.Quirky => ("변덕", "Quirky", "きまぐれ", "Rara", "Bizarre", "Excêntrica", "Kauzig"),
            _ => throw new ArgumentOutOfRangeException(nameof(nature))
        };
        var index = lang switch
        {
            AppLanguage.Ko => 0,
            AppLanguage.En => 1,
            AppLanguage.Ja => 2,
            AppLanguage.Es => 3,
            AppLanguage.Fr => 4,
            AppLanguage.Pt => 5,
            AppLanguage.De => 6,
            _ => 1
        };
        return index switch
        {
            0 => names.Item1,
            1 => names.Item2,
            2 => names.Item3,
            3 => names.Item4,
            4 => names.Item5,
            5 => names.Item6,
            6 => names.Item7,
            _ => names.Item2
        };
    }

    public static double Modifier(this PokemonNature nature, string stat)
    {
        var (up, down) = nature switch
        {
            PokemonNature.Lonely => ("attack", "defense"),
            PokemonNature.Brave => ("attack", "speed"),
            PokemonNature.Adamant => ("attack", "special-attack"),
            PokemonNature.Naughty => ("attack", "special-defense"),
            PokemonNature.Bold => ("defense", "attack"),
            PokemonNature.Relaxed => ("defense", "speed"),
            PokemonNature.Impish => ("defense", "special-attack"),
            PokemonNature.Lax => ("defense", "special-defense"),
            PokemonNature.Timid => ("speed", "attack"),
            PokemonNature.Hasty => ("speed", "defense"),
            PokemonNature.Jolly => ("speed", "special-attack"),
            PokemonNature.Naive => ("speed", "special-defense"),
            PokemonNature.Modest => ("special-attack", "attack"),
            PokemonNature.Mild => ("special-attack", "defense"),
            PokemonNature.Quiet => ("special-attack", "speed"),
            PokemonNature.Rash => ("special-attack", "special-defense"),
            PokemonNature.Calm => ("special-defense", "attack"),
            PokemonNature.Gentle => ("special-defense", "defense"),
            PokemonNature.Sassy => ("special-defense", "speed"),
            PokemonNature.Careful => ("special-defense", "special-attack"),
            _ => (null, null)
        };
        if (up is null) return 1.0;
        if (stat == up) return 1.1;
        if (stat == down) return 0.9;
        return 1.0;
    }
}
