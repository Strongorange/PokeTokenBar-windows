using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public readonly record struct BaseSpecies(int Id, int CaptureRate);

public sealed class PokemonLineSource
{
    public const string SnapshotResourceName = "PokeTokenBar.Application.Assets.pokemon-snapshot.json";

    private readonly Dictionary<int, EvoLine> _lines;
    private readonly IReadOnlyList<BaseSpecies> _bases;

    public IReadOnlyList<BaseSpecies> Bases => _bases;

    public PokemonLineSource(IReadOnlyList<BaseSpecies> bases, Dictionary<int, EvoLine> lines)
    {
        _bases = bases;
        _lines = lines;
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
        if (schema != 1)
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
        return new PokemonLineSource(bases, lines);
    }

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
}
