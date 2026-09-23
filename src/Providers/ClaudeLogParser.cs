using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public static class ClaudeLogParser
{
    public const int ParserVersion = 1;

    public static List<UsageEntry> Parse(string path, IReadOnlyList<string> lines) => Parse(lines);

    public static List<UsageEntry> Parse(IReadOnlyList<string> lines, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        List<UsageEntry>? entries = null;
        foreach (var line in lines)
        {
            if (!line.Contains("\"usage\"", StringComparison.Ordinal)) continue;
            if (!line.Contains("\"assistant\"", StringComparison.Ordinal)) continue;
            if (ParseLine(line, tz) is not { } entry) continue;
            entries ??= [];
            entries.Add(entry);
        }
        return entries is null ? [] : UsageAggregation.DedupKeepMax(entries);
    }

    internal static UsageEntry? ParseLine(string line, TimeZoneInfo timeZone)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var envelope = document.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object) return null;
            if (StringProperty(envelope, "type") != "assistant") return null;
            if (!envelope.TryGetProperty("message", out var message) ||
                message.ValueKind != JsonValueKind.Object) return null;
            if (!message.TryGetProperty("usage", out var usage) ||
                usage.ValueKind != JsonValueKind.Object) return null;
            if (IsoDates.Date(StringProperty(envelope, "timestamp")) is not { } date) return null;
            var id = (StringProperty(message, "id") ?? "") + "|" + (StringProperty(envelope, "requestId") ?? "");
            return new UsageEntry(
                id,
                date,
                UsageAggregation.LocalDay(date, timeZone),
                StringProperty(message, "model") ?? "unknown",
                TokenValue(usage, "input_tokens"),
                TokenValue(usage, "output_tokens"),
                TokenValue(usage, "cache_creation_input_tokens"),
                TokenValue(usage, "cache_read_input_tokens"));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? StringProperty(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static long TokenValue(JsonElement usage, string name) =>
        usage.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.Number &&
        element.TryGetInt64(out var value)
            ? value
            : 0;
}
