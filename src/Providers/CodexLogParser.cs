using System.Text;
using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public static class CodexLogParser
{
    public const int ParserVersion = 1;

    private const string SessionMetaMarker = "session_meta";
    private const string ModelMarker = "\"model\"";
    private const string TokenCountMarker = "token_count";

    internal sealed record CodexSessionMeta(
        string? Id,
        string? ParentId,
        DateTimeOffset? Date,
        bool IsSubagent);

    private sealed record ParsedToken(UsageEntry Entry, CodexUsageState? UsageState);

    private static readonly byte[] SessionMetaMarkerBytes = Encoding.UTF8.GetBytes(SessionMetaMarker);
    private static readonly byte[] ModelMarkerBytes = Encoding.UTF8.GetBytes(ModelMarker);
    private static readonly byte[] TokenCountMarkerBytes = Encoding.UTF8.GetBytes(TokenCountMarker);

    /// <summary>Same test as the string markers in <see cref="Parse(string, IEnumerable{string}, TimeZoneInfo?, CodexParsedRollout?)"/>, on raw bytes.</summary>
    public static bool MightMatter(ReadOnlySpan<byte> line) =>
        line.IndexOf(SessionMetaMarkerBytes) >= 0 ||
        line.IndexOf(ModelMarkerBytes) >= 0 ||
        line.IndexOf(TokenCountMarkerBytes) >= 0;

    public static CodexParsedRollout Parse(string path, IEnumerable<string> lines) =>
        Parse(path, lines, timeZone: null);

    /// <param name="resumeFrom">Result of parsing the earlier part of the same file; parsing continues from its state.</param>
    public static CodexParsedRollout Parse(
        string path, IEnumerable<string> lines, TimeZoneInfo? timeZone, CodexParsedRollout? resumeFrom = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        var fileName = Path.GetFileName(path);
        var resume = resumeFrom?.Resume;
        List<CodexUsageEvent>? events = resumeFrom is { Events.Count: > 0 } ? [.. resumeFrom.Events] : null;
        var sessionID = resumeFrom?.SessionID;
        var parentSessionID = resumeFrom?.ParentSessionID;
        var currentSessionID = resume?.CurrentSessionID;
        var forkedAt = resumeFrom?.ForkedAt;
        var isSubagent = resumeFrom?.IsSubagent ?? false;
        (string SessionId, CodexUsageState State)? previousUsageState =
            resume is { PreviousUsageSessionID: { } previousSession, PreviousUsageState: { } previousState }
                ? (previousSession, previousState)
                : null;
        var model = resume?.Model ?? "codex";
        var turn = resume?.Turn ?? 0;
        foreach (var line in lines)
        {
            if (line.Contains(SessionMetaMarker, StringComparison.Ordinal) &&
                TryParseSessionMeta(line) is { } meta)
            {
                if (sessionID is null)
                {
                    sessionID = meta.Id;
                    parentSessionID = meta.ParentId;
                    forkedAt = meta.Date;
                    isSubagent = meta.IsSubagent;
                }
                if (meta.Id is { } id && id != currentSessionID)
                {
                    currentSessionID = id;
                    previousUsageState = null;
                }
            }
            if (line.Contains(ModelMarker, StringComparison.Ordinal) && TryParseModel(line) is { } parsedModel)
                model = parsedModel;
            if (!line.Contains(TokenCountMarker, StringComparison.Ordinal)) continue;
            if (ParseTokenLine(line, fileName, turn, model, tz) is not { } token) continue;
            turn++;
            if (token.UsageState is { } state && currentSessionID is { } currentId)
            {
                if (previousUsageState is { } previous &&
                    previous.SessionId == currentId &&
                    previous.State == state)
                    continue;
                previousUsageState = (currentId, state);
            }
            else
            {
                previousUsageState = null;
            }
            events ??= [];
            events.Add(new CodexUsageEvent(token.Entry, token.UsageState, currentSessionID));
        }
        var endState = new CodexResumeState(
            currentSessionID, previousUsageState?.SessionId, previousUsageState?.State, model, turn);
        return new CodexParsedRollout(path, sessionID, parentSessionID, forkedAt, isSubagent, events ?? [], endState);
    }

    internal static CodexSessionMeta? TryParseSessionMeta(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var envelope = document.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object) return null;
            if (StringProperty(envelope, "type") != "session_meta") return null;
            if (!envelope.TryGetProperty("payload", out var payload) ||
                payload.ValueKind != JsonValueKind.Object) return null;
            var id = NonEmpty(StringProperty(payload, "id")) ?? NonEmpty(StringProperty(payload, "session_id"));
            var parentId = NonEmpty(StringProperty(payload, "forked_from_id")) ??
                           NonEmpty(StringProperty(payload, "parent_thread_id"));
            var date = IsoDates.Date(StringProperty(envelope, "timestamp"));
            var isSubagent = StringProperty(payload, "thread_source") == "subagent" ||
                             (payload.TryGetProperty("source", out var source) &&
                              source.ValueKind == JsonValueKind.Object &&
                              source.TryGetProperty("subagent", out _));
            return new CodexSessionMeta(id, parentId, date, isSubagent);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? TryParseModel(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var envelope = document.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object) return null;
            if (!envelope.TryGetProperty("payload", out var payload) ||
                payload.ValueKind != JsonValueKind.Object) return null;
            if (StringProperty(payload, "model") is { } direct) return direct;
            if (payload.TryGetProperty("turn_context", out var turnContext) &&
                turnContext.ValueKind == JsonValueKind.Object &&
                StringProperty(turnContext, "model") is { } nested)
                return nested;
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static ParsedToken? ParseTokenLine(
        string line, string file, int turn, string model, TimeZoneInfo timeZone)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var envelope = document.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object) return null;
            if (!envelope.TryGetProperty("payload", out var payload) ||
                payload.ValueKind != JsonValueKind.Object) return null;
            if (StringProperty(payload, "type") != "token_count") return null;
            if (!payload.TryGetProperty("info", out var info) || info.ValueKind != JsonValueKind.Object) return null;
            if (!info.TryGetProperty("last_token_usage", out var last) ||
                last.ValueKind != JsonValueKind.Object) return null;
            if (IsoDates.Date(StringProperty(envelope, "timestamp")) is not { } date) return null;
            var inputTotal = IntValue(last, "input_tokens");
            var cached = IntValue(last, "cached_input_tokens");
            var output = IntValue(last, "output_tokens");
            var nonCachedInput = Math.Max(0, inputTotal - cached);
            var lastTotal = IntValue(last, "total_tokens");
            CodexUsageVector? cumulative = null;
            if (info.TryGetProperty("total_token_usage", out var totalUsage) &&
                totalUsage.ValueKind == JsonValueKind.Object)
                cumulative = ReadVector(totalUsage);
            long input;
            long outTokens;
            long cacheRead;
            var componentsAllZero = nonCachedInput + cached + output == 0;
            if (componentsAllZero && lastTotal > 0 && ShouldTrustTotalOnlyLast(lastTotal, cumulative))
            {
                input = lastTotal;
                outTokens = 0;
                cacheRead = 0;
            }
            else
            {
                input = nonCachedInput;
                outTokens = output;
                cacheRead = cached;
            }
            var entry = new UsageEntry(
                $"codex|{file}|{turn}",
                date,
                UsageAggregation.LocalDay(date, timeZone),
                model,
                input,
                outTokens,
                0,
                cacheRead,
                CostUnavailable: componentsAllZero && lastTotal > 0);
            return new ParsedToken(entry, cumulative is null ? null : new CodexUsageState(cumulative, ReadVector(last)));
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static bool ShouldTrustTotalOnlyLast(long lastTotal, CodexUsageVector? cumulative)
    {
        if (cumulative is null) return true;
        if (cumulative.BillableComponents() == 0 && cumulative.Total > 0) return true;
        return cumulative.Total == lastTotal;
    }

    internal static CodexUsageVector ReadVector(JsonElement obj) => new(
        IntValue(obj, "input_tokens"),
        IntValue(obj, "cached_input_tokens"),
        IntValue(obj, "cache_write_input_tokens"),
        IntValue(obj, "output_tokens"),
        IntValue(obj, "reasoning_output_tokens"),
        IntValue(obj, "total_tokens"));

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

    private static string? StringProperty(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? NonEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
