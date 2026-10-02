using System.Text.Json;

namespace PokeTokenBar.Core;

public static class ModelsDevCatalog
{
    public const double MaxRatePerMillion = 1_000;

    private static readonly string[] PreferredProviders =
        ["openai", "opencode", "zai", "google", "anthropic", "azure", "openrouter"];

    public static IReadOnlyDictionary<string, ModelRate> Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseRoot(document.RootElement);
        }
        catch (JsonException)
        {
            return new Dictionary<string, ModelRate>();
        }
        catch (InvalidOperationException)
        {
            return new Dictionary<string, ModelRate>();
        }
    }

    private static IReadOnlyDictionary<string, ModelRate> ParseRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return new Dictionary<string, ModelRate>();
        Dictionary<string, (ModelRate Rate, int Rank)>? picked = null;
        foreach (var provider in root.EnumerateObject())
        {
            var rank = ProviderRank(provider.Name);
            if (!TryGetModels(provider.Value, out var models)) continue;
            foreach (var model in models.EnumerateObject())
            {
                var key = BareKey(model.Name);
                if (key.Length == 0) continue;
                if (!TryReadRate(model.Value, out var rate)) continue;
                picked ??= [];
                if (picked.TryGetValue(key, out var existing) && existing.Rank <= rank) continue;
                picked[key] = (rate, rank);
            }
        }
        if (picked is null) return new Dictionary<string, ModelRate>();
        var rates = new Dictionary<string, ModelRate>(picked.Count, StringComparer.Ordinal);
        foreach (var (key, value) in picked)
            rates[key] = value.Rate;
        return rates;
    }

    internal static int ProviderRank(string providerId)
    {
        for (var index = 0; index < PreferredProviders.Length; index++)
            if (string.Equals(PreferredProviders[index], providerId, StringComparison.OrdinalIgnoreCase))
                return index;
        return PreferredProviders.Length;
    }

    internal static string BareKey(string modelId)
    {
        var name = modelId.Trim().ToLowerInvariant();
        var slash = name.LastIndexOf('/');
        return slash >= 0 ? name[(slash + 1)..] : name;
    }

    internal static bool TryReadRate(JsonElement model, out ModelRate rate)
    {
        rate = default;
        if (model.ValueKind != JsonValueKind.Object) return false;
        if (!model.TryGetProperty("cost", out var cost) || cost.ValueKind != JsonValueKind.Object)
            return false;
        var input = RateValue(cost, "input");
        var output = RateValue(cost, "output");
        var cacheRead = RateValue(cost, "cache_read") ?? 0;
        var cacheWrite = RateValue(cost, "cache_write") ?? 0;
        if (input is null || output is null) return false;
        if (input <= 0 && output <= 0) return false;
        if (input > MaxRatePerMillion || output > MaxRatePerMillion ||
            cacheRead > MaxRatePerMillion || cacheWrite > MaxRatePerMillion) return false;
        rate = ModelRate.PerMillion(input.Value, output.Value, cacheWrite, cacheRead);
        return true;
    }

    private static double? RateValue(JsonElement cost, string name)
    {
        if (!cost.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Number)
            return null;
        if (!element.TryGetDouble(out var value) || !double.IsFinite(value)) return null;
        return value < 0 ? null : value;
    }

    private static bool TryGetModels(JsonElement provider, out JsonElement models)
    {
        models = default;
        if (provider.ValueKind != JsonValueKind.Object) return false;
        if (!provider.TryGetProperty("models", out var value) || value.ValueKind != JsonValueKind.Object)
            return false;
        models = value;
        return true;
    }
}
