using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public static class OpenCodeLogParser
{
    public const int ParserVersion = 2;

    public static List<UsageEntry> Parse(string path, IReadOnlyList<string> lines, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        var sourceKey = Path.GetFileName(path);
        List<UsageEntry>? entries = null;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (ParseRow(line, sourceKey, tz) is not { } entry) continue;
            entries ??= [];
            entries.Add(entry);
        }
        return entries is null ? [] : UsageAggregation.DedupKeepMax(entries);
    }

    public static UsageEntry? ParseRow(string rowJson, string sourceKey, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        try
        {
            using var document = JsonDocument.Parse(rowJson);
            var row = document.RootElement;
            if (row.ValueKind != JsonValueKind.Object) return null;
            var id = StringProperty(row, "id");
            if (string.IsNullOrEmpty(id)) return null;
            if (!row.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) return null;
            var fallbackMillis = MillisValue(row, "time_created");
            return ParseMessageData(id, data, sourceKey, fallbackMillis, tz);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static UsageEntry? ParseMessage(
        string id, string dataJson, string sourceKey, long? fallbackCreatedMillis, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        try
        {
            using var document = JsonDocument.Parse(dataJson);
            return ParseMessageData(id, document.RootElement, sourceKey, fallbackCreatedMillis, tz);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// OpenCode v2 session_message row (upstream #363): assistant usage moves to the
    /// event-sourced projection. `role` is not in the payload (the query filters
    /// type='assistant'), the model is a nested {id, providerID} object instead of the v1
    /// top-level modelID string, and tokens carry no `total`.
    /// </summary>
    public static UsageEntry? ParseSessionMessage(
        string id, string dataJson, string sourceKey, long? fallbackCreatedMillis, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        try
        {
            using var document = JsonDocument.Parse(dataJson);
            var data = document.RootElement;
            if (data.ValueKind != JsonValueKind.Object) return null;
            if (!data.TryGetProperty("model", out var model)
                || model.ValueKind != JsonValueKind.Object) return null;
            var modelID = StringProperty(model, "id");
            if (string.IsNullOrEmpty(modelID)) return null;
            if (string.IsNullOrEmpty(StringProperty(model, "providerID"))) return null;
            return BuildEntry(id, data, modelID, sourceKey, fallbackCreatedMillis, tz);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static UsageEntry? ParseMessageData(
        string id, JsonElement data, string sourceKey, long? fallbackCreatedMillis, TimeZoneInfo timeZone)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        // macOS parity (parseOpenCodeMessage): `role` is not required — early v1 history rows
        // carry no role; only an explicit non-assistant role disqualifies the row.
        var role = StringProperty(data, "role");
        if (role is not null && role != "assistant") return null;
        return BuildEntry(id, data, StringProperty(data, "modelID") ?? "unknown",
            sourceKey, fallbackCreatedMillis, timeZone);
    }

    private static UsageEntry? BuildEntry(
        string id, JsonElement data, string model, string sourceKey,
        long? fallbackCreatedMillis, TimeZoneInfo timeZone)
    {
        if (!data.TryGetProperty("tokens", out var tokens) || tokens.ValueKind != JsonValueKind.Object) return null;

        var input = 0L;
        var output = 0L;
        var cacheWrite = 0L;
        var cacheRead = 0L;
        {
            input = IntValue(tokens, "input");
            output = IntValue(tokens, "output");
            var reasoning = IntValue(tokens, "reasoning");
            output = Math.Min(UsageAggregation.MaxParsedTokenValue, output + reasoning);
            if (tokens.TryGetProperty("cache", out var cache) && cache.ValueKind == JsonValueKind.Object)
            {
                cacheWrite = IntValue(cache, "write");
                cacheRead = IntValue(cache, "read");
            }
        }

        var stampMillis = fallbackCreatedMillis;
        if (data.TryGetProperty("time", out var time) && time.ValueKind == JsonValueKind.Object)
            stampMillis = MillisValue(time, "completed") ?? MillisValue(time, "created") ?? stampMillis;
        if (stampMillis is not { } millis) return null;
        DateTimeOffset date;
        try
        {
            date = DateTimeOffset.FromUnixTimeMilliseconds(millis);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        double? explicitCost = null;
        if (data.TryGetProperty("cost", out var costElement) && costElement.ValueKind == JsonValueKind.Number)
        {
            if (costElement.TryGetDouble(out var cost) && double.IsFinite(cost) && cost > 0)
                explicitCost = cost;
        }

        return new UsageEntry(
            $"opencode|{sourceKey}|{id}",
            date,
            UsageAggregation.LocalDay(date, timeZone),
            model,
            input,
            output,
            cacheWrite,
            cacheRead,
            explicitCost,
            CostIsEstimate: false);
    }

    private static long IntValue(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Number) return 0;
        if (element.TryGetInt64(out var integer))
            return integer <= 0 ? 0 : Math.Min(integer, UsageAggregation.MaxParsedTokenValue);
        if (!element.TryGetDouble(out var dbl) || !double.IsFinite(dbl)) return 0;
        if (dbl <= 0) return 0;
        return dbl >= UsageAggregation.MaxParsedTokenValue
            ? UsageAggregation.MaxParsedTokenValue
            : (long)dbl;
    }

    private static long? MillisValue(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Number) return null;
        if (element.TryGetInt64(out var integer)) return integer > 0 ? integer : null;
        if (element.TryGetDouble(out var dbl) && double.IsFinite(dbl) && dbl > 0) return (long)dbl;
        return null;
    }

    private static string? StringProperty(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
