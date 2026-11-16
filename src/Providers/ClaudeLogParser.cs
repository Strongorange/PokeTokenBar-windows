using System.Text;
using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public static class ClaudeLogParser
{
    public const int ParserVersion = 1;

    private const string UsageMarker = "\"usage\"";
    private const string AssistantMarker = "\"assistant\"";
    private static readonly byte[] UsageMarkerBytes = Encoding.UTF8.GetBytes(UsageMarker);
    private static readonly byte[] AssistantMarkerBytes = Encoding.UTF8.GetBytes(AssistantMarker);

    /// <summary>Same test as the string markers in <see cref="Parse(IEnumerable{string}, TimeZoneInfo?, IReadOnlyList{UsageEntry}?)"/>, on raw bytes.</summary>
    public static bool MightMatter(ReadOnlySpan<byte> line) =>
        line.IndexOf(UsageMarkerBytes) >= 0 && line.IndexOf(AssistantMarkerBytes) >= 0;

    public static List<UsageEntry> Parse(string path, IEnumerable<string> lines) => Parse(lines);

    /// <param name="resumeFrom">Entries parsed from the earlier part of the same file; new lines are merged into them.</param>
    public static List<UsageEntry> Parse(
        IEnumerable<string> lines, TimeZoneInfo? timeZone = null, IReadOnlyList<UsageEntry>? resumeFrom = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        List<UsageEntry>? entries = resumeFrom is { Count: > 0 } ? [.. resumeFrom] : null;
        foreach (var line in lines)
        {
            if (!line.Contains(UsageMarker, StringComparison.Ordinal)) continue;
            if (!line.Contains(AssistantMarker, StringComparison.Ordinal)) continue;
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
