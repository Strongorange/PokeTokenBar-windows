namespace PokeTokenBar.Core;

/// <summary>
/// An individual's appearance within a species: Unown's letter, Shaymin's Sky Forme, Shellos's sea.
/// The value belongs to the individual and is resolved per species of its line, so a Sandy Burmy
/// becomes a Sandy Wormadam while the Mothim branch keeps Mothim's single appearance. Species IDs
/// and evolution paths are unchanged; the form only selects sprites, names and collection slots.
/// Ported from macOS PokemonForm.swift (forms feature, upstream #376).
/// </summary>
public readonly record struct PokemonForm(string Raw)
{
    /// <summary>Unown shows its letter instead of a localized form name.</summary>
    public string Symbol => Raw switch
    {
        "exclamation" => "!",
        "question" => "?",
        _ => Raw.ToUpperInvariant()
    };
}

/// <summary>
/// `PokemonID` is the PokeAPI variety whose static PNG holds this form (`10006.png` for Sky
/// Shaymin); null means the static sprite is named like the animated one (`412-sandy.png`).
/// `FormID` is the PokeAPI `pokemon-form` resource that carries the localized form names.
/// </summary>
public sealed record PokemonFormEntry(PokemonForm Form, int FormID, int? PokemonID = null);

public static class PokemonForms
{
    public const int UnownSpeciesID = 201;

    /// <summary>
    /// Every catalogued species lists its default appearance first; that one keeps the plain
    /// `<id>.png`/`<id>.gif` sprites. Only forms with a Gen V animated sprite are listed, the same
    /// source that bounds hatchable species. Gender differences are not forms.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, IReadOnlyList<PokemonFormEntry>> Catalog =
        new Dictionary<int, IReadOnlyList<PokemonFormEntry>>
        {
            [UnownSpeciesID] = UnownEntries(),
            [351] =
            [
                new(new PokemonForm("normal"), 351),
                new(new PokemonForm("sunny"), 10028, 10013),
                new(new PokemonForm("rainy"), 10029, 10014),
                new(new PokemonForm("snowy"), 10030, 10015)
            ],
            [386] =
            [
                new(new PokemonForm("normal"), 386),
                new(new PokemonForm("attack"), 10031, 10001),
                new(new PokemonForm("defense"), 10032, 10002),
                new(new PokemonForm("speed"), 10033, 10003)
            ],
            [412] =
            [
                new(new PokemonForm("plant"), 412),
                new(new PokemonForm("sandy"), 10034),
                new(new PokemonForm("trash"), 10035)
            ],
            [413] =
            [
                new(new PokemonForm("plant"), 413),
                new(new PokemonForm("sandy"), 10036, 10004),
                new(new PokemonForm("trash"), 10037, 10005)
            ],
            [421] =
            [
                new(new PokemonForm("overcast"), 421),
                new(new PokemonForm("sunshine"), 10038)
            ],
            [422] =
            [
                new(new PokemonForm("west"), 422),
                new(new PokemonForm("east"), 10039)
            ],
            [423] =
            [
                new(new PokemonForm("west"), 423),
                new(new PokemonForm("east"), 10040)
            ],
            [479] =
            [
                new(new PokemonForm("normal"), 479),
                new(new PokemonForm("heat"), 10058, 10008),
                new(new PokemonForm("wash"), 10059, 10009),
                new(new PokemonForm("frost"), 10060, 10010),
                new(new PokemonForm("fan"), 10061, 10011),
                new(new PokemonForm("mow"), 10062, 10012)
            ],
            [487] =
            [
                new(new PokemonForm("altered"), 487),
                new(new PokemonForm("origin"), 10063, 10007)
            ],
            [492] =
            [
                new(new PokemonForm("land"), 492),
                new(new PokemonForm("sky"), 10064, 10006)
            ],
            [493] =
            [
                new(new PokemonForm("normal"), 493),
                new(new PokemonForm("fighting"), 10045),
                new(new PokemonForm("flying"), 10047),
                new(new PokemonForm("poison"), 10052),
                new(new PokemonForm("ground"), 10050),
                new(new PokemonForm("rock"), 10054),
                new(new PokemonForm("bug"), 10041),
                new(new PokemonForm("ghost"), 10048),
                new(new PokemonForm("steel"), 10055),
                new(new PokemonForm("fire"), 10046),
                new(new PokemonForm("water"), 10056),
                new(new PokemonForm("grass"), 10049),
                new(new PokemonForm("electric"), 10044),
                new(new PokemonForm("psychic"), 10053),
                new(new PokemonForm("ice"), 10051),
                new(new PokemonForm("dragon"), 10043),
                new(new PokemonForm("dark"), 10042),
                new(new PokemonForm("fairy"), 10085)
            ],
            [550] =
            [
                new(new PokemonForm("red-striped"), 550),
                new(new PokemonForm("blue-striped"), 10066, 10016)
            ],
            [555] =
            [
                new(new PokemonForm("standard"), 555),
                new(new PokemonForm("zen"), 10067, 10017)
            ],
            [585] =
            [
                new(new PokemonForm("spring"), 585),
                new(new PokemonForm("summer"), 10068),
                new(new PokemonForm("autumn"), 10069),
                new(new PokemonForm("winter"), 10070)
            ],
            [586] =
            [
                new(new PokemonForm("spring"), 586),
                new(new PokemonForm("summer"), 10071),
                new(new PokemonForm("autumn"), 10072),
                new(new PokemonForm("winter"), 10073)
            ],
            [641] =
            [
                new(new PokemonForm("incarnate"), 641),
                new(new PokemonForm("therian"), 10079, 10019)
            ],
            [642] =
            [
                new(new PokemonForm("incarnate"), 642),
                new(new PokemonForm("therian"), 10080, 10020)
            ],
            [645] =
            [
                new(new PokemonForm("incarnate"), 645),
                new(new PokemonForm("therian"), 10081, 10021)
            ],
            [646] =
            [
                new(new PokemonForm("normal"), 646),
                new(new PokemonForm("black"), 10082, 10022),
                new(new PokemonForm("white"), 10083, 10023)
            ],
            [647] =
            [
                new(new PokemonForm("ordinary"), 647),
                new(new PokemonForm("resolute"), 10084, 10024)
            ],
            [648] =
            [
                new(new PokemonForm("aria"), 648),
                new(new PokemonForm("pirouette"), 10074, 10018)
            ],
            [649] =
            [
                new(new PokemonForm("normal"), 649),
                new(new PokemonForm("douse"), 10075),
                new(new PokemonForm("shock"), 10076),
                new(new PokemonForm("burn"), 10077),
                new(new PokemonForm("chill"), 10078)
            ]
        };

    private static IReadOnlyList<PokemonFormEntry> UnownEntries()
    {
        var entries = new List<PokemonFormEntry>();
        var letters = "abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString())
            .Concat(["exclamation", "question"]).ToList();
        for (var index = 0; index < letters.Count; index++)
            entries.Add(new PokemonFormEntry(new PokemonForm(letters[index]),
                index == 0 ? UnownSpeciesID : 10000 + index));
        return entries;
    }

    /// <summary>
    /// Catalogued species whose line starts elsewhere. The individual's form is chosen at hatch,
    /// so Cherubi and Darumaka eggs already know which Cherrim or Darmanitan they will become.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, int> LineBaseIDs =
        new Dictionary<int, int> { [413] = 412, [421] = 420, [423] = 422, [555] = 554, [586] = 585 };

    public static IReadOnlyList<PokemonFormEntry> Entries(int speciesID) =>
        Catalog.TryGetValue(speciesID, out var entries) ? entries : [];

    /// <summary>The species' forms, default first. Empty when the species has a single appearance.</summary>
    public static IReadOnlyList<PokemonForm> Forms(int speciesID) =>
        Entries(speciesID).Select(e => e.Form).ToList();

    public static bool HasForms(int speciesID) => Catalog.ContainsKey(speciesID);

    /// <summary>Species of the line that change appearance with the individual's form, in line order.</summary>
    public static IReadOnlyList<int> FormSpecies(int baseID) =>
        Catalog.Keys.Where(id => LineBaseIDs.GetValueOrDefault(id, id) == baseID).OrderBy(id => id).ToList();

    /// <summary>Everything an individual of this line can carry, default first.</summary>
    public static IReadOnlyList<PokemonForm> LineForms(int baseID)
    {
        var seen = new HashSet<PokemonForm>();
        return FormSpecies(baseID)
            .SelectMany(Forms)
            .Where(form => seen.Add(form))
            .ToList();
    }

    /// <summary>
    /// The appearance of an individual at one species: its own form when that species has it,
    /// otherwise the species default. Null for species without forms.
    /// </summary>
    public static PokemonForm? Resolved(int speciesID, PokemonForm? form)
    {
        var forms = Forms(speciesID);
        if (forms.Count == 0) return null;
        return form is { } value && forms.Contains(value) ? value : forms[0];
    }

    /// <summary>
    /// Normalizes the form an individual stores: null for lines without forms, and the line default
    /// for records written before this line had forms (older Unown saves keep their A).
    /// </summary>
    public static PokemonForm? ResolvedLine(int baseID, PokemonForm? form)
    {
        var forms = LineForms(baseID);
        if (forms.Count == 0) return null;
        return form is { } value && forms.Contains(value) ? value : forms[0];
    }

    public static bool IsDefault(PokemonForm? form, int speciesID) =>
        Forms(speciesID).FirstOrDefault() == Resolved(speciesID, form);

    public static int SortOrder(PokemonForm? form, int speciesID)
    {
        if (form is not { } value) return 0;
        var forms = Forms(speciesID);
        for (var i = 0; i < forms.Count; i++)
            if (forms[i] == value) return i;
        return 0;
    }

    /// <summary>Chosen only after the species roll: uncollected forms weigh 2, collected forms weigh 1.</summary>
    public static PokemonForm Roll(ulong roll, IReadOnlyList<PokemonForm> forms,
        IReadOnlySet<PokemonForm> collected)
    {
        var weights = forms.Select(f => CollectionWeight.Adjusted(2, collected.Contains(f))).ToArray();
        var total = weights.Sum();
        var remaining = (int)(roll % (ulong)total);
        for (var i = 0; i < forms.Count - 1; i++)
        {
            remaining -= weights[i];
            if (remaining < 0) return forms[i];
        }
        return forms[^1]; // Any remaining roll belongs to the last form.
    }

    /// <summary>
    /// Parses a stored/raw form identifier. Unknown values read as null so the caller's
    /// resolution falls back to the default form (macOS: malformed saves degrade, never trap).
    /// </summary>
    public static PokemonForm? Parse(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        return Catalog.Values.SelectMany(entries => entries)
            .FirstOrDefault(e => e.Form.Raw == raw) is { } entry ? entry.Form : null;
    }

    /// <summary>
    /// Animated GIFs and cache files use `<id>-<form>`; the default keeps the plain `<id>` names
    /// (and so the cache keys of saves written before forms existed).
    /// </summary>
    public static string AssetName(int speciesID, PokemonForm? form)
    {
        var resolved = Resolved(speciesID, form);
        if (resolved is null || IsDefault(resolved, speciesID)) return speciesID.ToString();
        return $"{speciesID}-{resolved.Value.Raw}";
    }

    /// <summary>Static PNGs of forms that are separate PokeAPI varieties live under the variety id.</summary>
    public static string StaticAssetName(int speciesID, PokemonForm? form)
    {
        var resolved = Resolved(speciesID, form);
        if (resolved is { } value
            && Entries(speciesID).FirstOrDefault(e => e.Form == value)?.PokemonID is { } pokemonID)
            return pokemonID.ToString();
        return AssetName(speciesID, form);
    }

    /// <summary>PokeAPI `pokemon-form` id holding the localized form names. Null for species without forms.</summary>
    public static int? FormID(int speciesID, PokemonForm? form)
    {
        var resolved = Resolved(speciesID, form);
        return resolved is { } value
            ? Entries(speciesID).FirstOrDefault(e => e.Form == value)?.FormID
            : null;
    }

    /// <summary>
    /// Castform, Rotom, Kyurem and Genesect have no official name for their usual look:
    /// PokeAPI only says "Forme de Motisma" or nothing at all.
    /// </summary>
    private static readonly HashSet<int> UnnamedDefaultSpecies = [351, 479, 646, 649];

    private static bool HasUnnamedDefault(int speciesID, PokemonForm? form) =>
        UnnamedDefaultSpecies.Contains(speciesID) && IsDefault(form, speciesID);

    /// <summary>
    /// Localized form name ("Forme Céleste"), Unown's letter, the localized "Normal Form" of an
    /// unnamed usual look, or the identifier when the snapshot carries no translation for it.
    /// Null for species without forms. macOS loads these live from PokeAPI with a 30-day cache;
    /// the Windows port embeds them in the pokemon snapshot instead (`pokemon-form` resources).
    /// </summary>
    public static string? Label(int speciesID, PokemonForm? form, AppLanguage language,
        Func<int, IReadOnlyDictionary<string, string>?> namesByID)
    {
        var resolved = Resolved(speciesID, form);
        if (resolved is null) return null;
        if (speciesID == UnownSpeciesID) return resolved.Value.Symbol;
        if (HasUnnamedDefault(speciesID, resolved)) return DashboardText.NormalFormLabel(language);
        if (FormID(speciesID, resolved) is { } id
            && namesByID(id) is { } byLang
            && language.ResolveName(byLang) is { } name)
            return name;
        return PokemonNameLocalization.Identifier(resolved.Value.Raw);
    }

    /// <summary>Unown always names its letter; other species name only a non-default form.</summary>
    public static string DisplayName(string name, int speciesID, PokemonForm? form, string? label)
    {
        var resolved = Resolved(speciesID, form);
        if (resolved is null) return name;
        if (speciesID == UnownSpeciesID) return $"{name} [{resolved.Value.Symbol}]";
        if (IsDefault(resolved, speciesID) || string.IsNullOrEmpty(label)) return name;
        return $"{name} [{label}]";
    }
}

public static class CollectionWeight
{
    public static int Adjusted(int weight, bool isCollected) =>
        isCollected ? Math.Max(1, weight / 2) : Math.Max(1, weight);
}
