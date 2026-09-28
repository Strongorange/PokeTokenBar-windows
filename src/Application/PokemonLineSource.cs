using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public readonly record struct BaseSpecies(int Id, int CaptureRate);

public static class PokemonResourceKinds
{
    public const string Type = "type";
    public const string Ability = "ability";
    public const string Move = "move";
}

public sealed class PokemonLineSource
{
    public const string SnapshotResourceName = "PokeTokenBar.Application.Assets.pokemon-snapshot.json";

    private readonly Dictionary<int, EvoLine> _lines;
    private readonly IReadOnlyList<BaseSpecies> _bases;
    private readonly Dictionary<int, PokemonDetails> _details;
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _resourceNames;

    public IReadOnlyList<BaseSpecies> Bases => _bases;

    public PokemonLineSource(IReadOnlyList<BaseSpecies> bases, Dictionary<int, EvoLine> lines,
        Dictionary<int, PokemonDetails>? details = null,
        Dictionary<string, Dictionary<string, Dictionary<string, string>>>? resourceNames = null)
    {
        _bases = bases;
        _lines = lines;
        _details = details ?? [];
        _resourceNames = resourceNames ?? [];
    }

    public static PokemonLineSource Default()
    {
        var assembly = typeof(PokemonLineSource).Assembly;
        using var stream = assembly.GetManifestResourceStream(SnapshotResourceName)
            ?? throw new InvalidOperationException($"embedded snapshot not found: {SnapshotResourceName}");
        return Load(stream);
    }

    public static PokemonLineSource Load(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("snapshot is not an object");
        if (!root.TryGetProperty("format", out var format)
            || format.GetString() != "poketokenbar.pokemon-snapshot")
            throw new JsonException("snapshot format mismatch");
        var schema = root.TryGetProperty("schema", out var schemaElement)
            && schemaElement.ValueKind == JsonValueKind.Number ? schemaElement.GetInt32() : 0;
        if (schema is not (1 or 2))
            throw new JsonException($"unsupported snapshot schema {schema}");

        var names = new Dictionary<int, Dictionary<string, string>>();
        if (root.TryGetProperty("names", out var namesElement)
            && namesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var species in namesElement.EnumerateObject())
            {
                if (!int.TryParse(species.Name, out var id)
                    || species.Value.ValueKind != JsonValueKind.Object)
                    continue;
                var byLang = new Dictionary<string, string>();
                foreach (var lang in species.Value.EnumerateObject())
                    if (lang.Value.ValueKind == JsonValueKind.String)
                        byLang[lang.Name] = lang.Value.GetString()!;
                if (byLang.Count > 0) names[id] = byLang;
            }
        }

        var lines = new Dictionary<int, EvoLine>();
        if (root.TryGetProperty("lines", out var linesElement)
            && linesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var line in linesElement.EnumerateArray())
            {
                if (line.ValueKind != JsonValueKind.Object) continue;
                if (!line.TryGetProperty("base", out var baseElement)
                    || baseElement.ValueKind != JsonValueKind.Number) continue;
                var baseID = baseElement.GetInt32();
                var captureRate = line.TryGetProperty("captureRate", out var capture)
                    && capture.ValueKind == JsonValueKind.Number ? capture.GetInt32() : 255;
                var legendary = line.TryGetProperty("legendary", out var lg)
                    && lg.ValueKind == JsonValueKind.True;
                var mythical = line.TryGetProperty("mythical", out var my)
                    && my.ValueKind == JsonValueKind.True;
                if (!line.TryGetProperty("tree", out var tree)
                    || tree.ValueKind != JsonValueKind.Array) continue;
                var node = ReadNode(tree);
                if (node is null) continue;
                var rarity = Rarities.From(captureRate, legendary, mythical);
                lines[baseID] = new EvoLine(baseID, node, rarity, names);
            }
        }

        var bases = new List<BaseSpecies>();
        if (root.TryGetProperty("bases", out var basesElement)
            && basesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in basesElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object) continue;
                if (!entry.TryGetProperty("id", out var idElement)
                    || idElement.ValueKind != JsonValueKind.Number) continue;
                var captureRate = entry.TryGetProperty("captureRate", out var capture)
                    && capture.ValueKind == JsonValueKind.Number ? capture.GetInt32() : 255;
                bases.Add(new BaseSpecies(idElement.GetInt32(), captureRate));
            }
        }
        var details = ReadDetails(root);
        var resourceNames = ReadResourceNames(root);
        return new PokemonLineSource(bases, lines, details, resourceNames);
    }

    private static Dictionary<int, PokemonDetails> ReadDetails(JsonElement root)
    {
        var details = new Dictionary<int, PokemonDetails>();
        if (!root.TryGetProperty("details", out var detailsElement)
            || detailsElement.ValueKind != JsonValueKind.Object)
            return details;
        foreach (var species in detailsElement.EnumerateObject())
        {
            if (!int.TryParse(species.Name, out var id)
                || species.Value.ValueKind != JsonValueKind.Object)
                continue;
            var parsed = ReadDetail(species.Value);
            if (parsed is not null) details[id] = parsed;
        }
        return details;
    }

    private static PokemonDetails? ReadDetail(JsonElement e)
    {
        if (!e.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
            return null;
        var details = new PokemonDetails
        {
            Name = nameElement.GetString()!,
            Height = ReadInt(e, "height"),
            Weight = ReadInt(e, "weight"),
            BaseExperience = e.TryGetProperty("baseExperience", out var baseExp)
                && baseExp.ValueKind == JsonValueKind.Number ? baseExp.GetInt32() : null,
            GenderRate = e.TryGetProperty("genderRate", out var genderRate)
                && genderRate.ValueKind == JsonValueKind.Number ? genderRate.GetInt32() : -1
        };
        if (e.TryGetProperty("types", out var types)
            && types.ValueKind == JsonValueKind.Array)
            foreach (var type in types.EnumerateArray())
                if (type.ValueKind == JsonValueKind.String)
                    details.Types.Add(type.GetString()!);
        if (e.TryGetProperty("baseStats", out var stats)
            && stats.ValueKind == JsonValueKind.Object)
            foreach (var stat in stats.EnumerateObject())
                if (stat.Value.ValueKind == JsonValueKind.Number)
                    details.BaseStats[stat.Name] = stat.Value.GetInt32();
        if (e.TryGetProperty("abilities", out var abilities)
            && abilities.ValueKind == JsonValueKind.Array)
            foreach (var ability in abilities.EnumerateArray())
            {
                if (ability.ValueKind != JsonValueKind.Object) continue;
                if (!ability.TryGetProperty("name", out var abilityName)
                    || abilityName.ValueKind != JsonValueKind.String) continue;
                details.Abilities.Add(new PokemonAbilityOption
                {
                    Name = abilityName.GetString()!,
                    Slot = ReadInt(ability, "slot"),
                    IsHidden = ability.TryGetProperty("hidden", out var hidden)
                        && hidden.ValueKind == JsonValueKind.True
                });
            }
        if (e.TryGetProperty("moves", out var moves)
            && moves.ValueKind == JsonValueKind.Array)
            foreach (var move in moves.EnumerateArray())
            {
                if (move.ValueKind != JsonValueKind.Array || move.GetArrayLength() == 0) continue;
                if (move[0].ValueKind != JsonValueKind.String) continue;
                var option = new PokemonMoveOption { Name = move[0].GetString()! };
                for (var i = 1; i + 1 < move.GetArrayLength(); i += 2)
                {
                    if (move[i].ValueKind != JsonValueKind.String
                        || move[i + 1].ValueKind != JsonValueKind.Number)
                        continue;
                    option.LearnMethods.Add(new PokemonMoveLearnMethod
                    {
                        Method = move[i].GetString()!,
                        Level = move[i + 1].GetInt32()
                    });
                }
                if (option.LearnMethods.Count > 0) details.Moves.Add(option);
            }
        return details;
    }

    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> ReadResourceNames(
        JsonElement root)
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        if (!root.TryGetProperty("resourceNames", out var resourceElement)
            || resourceElement.ValueKind != JsonValueKind.Object)
            return result;
        foreach (var kind in resourceElement.EnumerateObject())
        {
            if (kind.Value.ValueKind != JsonValueKind.Object) continue;
            var bySlug = new Dictionary<string, Dictionary<string, string>>();
            foreach (var slug in kind.Value.EnumerateObject())
            {
                if (slug.Value.ValueKind != JsonValueKind.Object) continue;
                var byLang = new Dictionary<string, string>();
                foreach (var lang in slug.Value.EnumerateObject())
                    if (lang.Value.ValueKind == JsonValueKind.String)
                        byLang[lang.Name] = lang.Value.GetString()!;
                if (byLang.Count > 0) bySlug[slug.Name] = byLang;
            }
            if (bySlug.Count > 0) result[kind.Name] = bySlug;
        }
        return result;
    }

    private static int ReadInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static EvoNode? ReadNode(JsonElement element)
    {
        if (element.GetArrayLength() < 2) return null;
        if (element[0].ValueKind != JsonValueKind.Number) return null;
        var children = new List<EvoNode>();
        if (element[1].ValueKind == JsonValueKind.Array)
            foreach (var child in element[1].EnumerateArray())
            {
                var node = ReadNode(child);
                if (node is not null) children.Add(node);
            }
        return new EvoNode(element[0].GetInt32(), children);
    }

    public EvoLine? Line(int baseSpeciesID) =>
        _lines.TryGetValue(baseSpeciesID, out var line) ? line : null;

    public PokemonDetails? Details(int speciesID) =>
        _details.TryGetValue(speciesID, out var details) ? details : null;

    public string ResourceName(string kind, string slug, AppLanguage language)
    {
        if (_resourceNames.TryGetValue(kind, out var bySlug)
            && bySlug.TryGetValue(slug, out var byLang)
            && language.ResolveName(byLang) is { } name)
            return name;
        return PokemonNameLocalization.Identifier(slug);
    }
}
