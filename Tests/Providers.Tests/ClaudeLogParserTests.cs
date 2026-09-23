using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class ClaudeLogParserTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    private static IReadOnlyList<string> FixtureLines(string name) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    private static string AssistantLine(
        string messageId,
        string? requestId,
        string timestamp,
        long input,
        long output,
        long cacheWrite = 0,
        long cacheRead = 0,
        string model = "claude-opus-5") =>
        "{\"type\":\"assistant\",\"timestamp\":\"" + timestamp + "\"" +
        (requestId is null ? "" : ",\"requestId\":\"" + requestId + "\"") +
        ",\"message\":{\"id\":\"" + messageId + "\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"" +
        model + "\",\"content\":\"x\",\"usage\":{\"input_tokens\":" + input +
        ",\"output_tokens\":" + output +
        ",\"cache_creation_input_tokens\":" + cacheWrite +
        ",\"cache_read_input_tokens\":" + cacheRead + "}}}";

    [Fact]
    public void WindowsFixtureParsesWithoutRequestId()
    {
        var entries = ClaudeLogParser.Parse(FixtureLines("windows-claude.jsonl"), Tz);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e =>
        {
            Assert.EndsWith("|", e.Id, StringComparison.Ordinal);
            Assert.Equal("glm-5.2", e.Model);
            Assert.Equal("2026-06-17", e.LocalDay);
            Assert.Equal(19529, e.Input);
            Assert.Equal(160, e.Output);
            Assert.Equal(0, e.CacheWrite);
            Assert.Equal(3904, e.CacheRead);
            Assert.Equal(23593, e.Total);
        });
        var first = Assert.Single(entries, e => e.Id == "msg_0000000000000000000000000011|");
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 7, 46, 31, 693, TimeSpan.Zero), first.Date);
        var second = Assert.Single(entries, e => e.Id == "msg_0000000000000000000000000014|");
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 7, 46, 32, 987, TimeSpan.Zero), second.Date);
    }

    [Fact]
    public void WslFixtureParsesWithRequestIdAndCacheFields()
    {
        var entries = ClaudeLogParser.Parse(FixtureLines("wsl-claude.jsonl"), Tz);
        Assert.Equal(4, entries.Count);
        Assert.All(entries, e =>
        {
            Assert.Equal("claude-opus-5", e.Model);
            Assert.Equal("2026-09-01", e.LocalDay);
            Assert.Contains("|req_", e.Id);
        });
        var restart = Assert.Single(entries,
            e => e.Id == "msg_0000000000000000000000000029|req_0000000000000000000000000028");
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 6, 23, 17, 140, TimeSpan.Zero), restart.Date);
        Assert.Equal(2, restart.Input);
        Assert.Equal(261, restart.Output);
        Assert.Equal(57786, restart.CacheWrite);
        Assert.Equal(0, restart.CacheRead);
        Assert.Equal(58049, restart.Total);
        var followUp = Assert.Single(entries,
            e => e.Id == "msg_0000000000000000000000000033|req_0000000000000000000000000032");
        Assert.Equal(2, followUp.Input);
        Assert.Equal(281, followUp.Output);
        Assert.Equal(1520, followUp.CacheWrite);
        Assert.Equal(57786, followUp.CacheRead);
        Assert.Equal(59589, followUp.Total);
    }

    [Fact]
    public void FixtureTokenTotalsMatchHandComputedSums()
    {
        var windows = ClaudeLogParser.Parse(FixtureLines("windows-claude.jsonl"), Tz);
        Assert.Equal(39058L, windows.Sum(e => e.Input));
        Assert.Equal(320L, windows.Sum(e => e.Output));
        Assert.Equal(0L, windows.Sum(e => e.CacheWrite));
        Assert.Equal(7808L, windows.Sum(e => e.CacheRead));
        Assert.Equal(47186L, windows.Sum(e => e.Total));

        var wsl = ClaudeLogParser.Parse(FixtureLines("wsl-claude.jsonl"), Tz);
        Assert.Equal(8L, wsl.Sum(e => e.Input));
        Assert.Equal(1064L, wsl.Sum(e => e.Output));
        Assert.Equal(174878L, wsl.Sum(e => e.CacheWrite));
        Assert.Equal(57786L, wsl.Sum(e => e.CacheRead));
        Assert.Equal(233736L, wsl.Sum(e => e.Total));
    }

    [Fact]
    public void NonUsageAndNonAssistantLinesAreSkipped()
    {
        var lines = new[]
        {
            "{\"type\":\"user\",\"timestamp\":\"2026-09-01T06:00:00.000Z\",\"message\":{\"role\":\"user\",\"content\":\"assistant usage\"}}",
            "{\"type\":\"assistant\",\"timestamp\":\"2026-09-01T06:00:00.000Z\",\"message\":{\"id\":\"m1\",\"role\":\"assistant\"}}",
            "{not json at all",
            "",
            AssistantLine("m2", null, "2026-09-01T06:00:00.000Z", 10, 5),
        };
        var entry = Assert.Single(ClaudeLogParser.Parse(lines, Tz));
        Assert.Equal("m2|", entry.Id);
    }

    [Fact]
    public void UnknownUsageAndEnvelopeFieldsAreIgnored()
    {
        var line =
            "{\"type\":\"assistant\",\"timestamp\":\"2026-09-01T06:00:00.000Z\",\"requestId\":\"req_1\"," +
            "\"effort\":\"high\",\"origin\":{\"kind\":\"human\"}," +
            "\"message\":{\"id\":\"m1\",\"model\":\"claude-opus-5\",\"role\":\"assistant\"," +
            "\"usage\":{\"input_tokens\":3,\"output_tokens\":4,\"cache_creation_input_tokens\":5," +
            "\"cache_read_input_tokens\":6,\"server_tool_use\":{\"web_search_requests\":1}," +
            "\"cache_creation\":{\"ephemeral_1h_input_tokens\":1}," +
            "\"iterations\":[{\"input_tokens\":3}],\"service_tier\":\"standard\"," +
            "\"output_tokens_details\":{\"thinking_tokens\":9}}}}";
        var entry = Assert.Single(ClaudeLogParser.Parse([line], Tz));
        Assert.Equal("m1|req_1", entry.Id);
        Assert.Equal(3, entry.Input);
        Assert.Equal(4, entry.Output);
        Assert.Equal(5, entry.CacheWrite);
        Assert.Equal(6, entry.CacheRead);
        Assert.Equal(18, entry.Total);
    }

    [Fact]
    public void StreamRestartDuplicateKeepsMaxTotal()
    {
        var lines = new[]
        {
            AssistantLine("m1", "req_1", "2026-09-01T06:00:00.000Z", 100, 100),
            AssistantLine("m1", "req_1", "2026-09-01T06:00:01.000Z", 100, 260),
        };
        var entry = Assert.Single(ClaudeLogParser.Parse(lines, Tz));
        Assert.Equal(360, entry.Total);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 6, 0, 1, TimeSpan.Zero), entry.Date);
    }

    [Fact]
    public void SameMessageIdWithDifferentRequestIdsStaysDistinct()
    {
        var lines = new[]
        {
            AssistantLine("m1", "req_1", "2026-09-01T06:00:00.000Z", 10, 1),
            AssistantLine("m1", "req_2", "2026-09-01T06:00:01.000Z", 10, 2),
        };
        Assert.Equal(2, ClaudeLogParser.Parse(lines, Tz).Count);
    }

    [Fact]
    public void MissingModelDefaultsToUnknown()
    {
        var line =
            "{\"type\":\"assistant\",\"timestamp\":\"2026-09-01T06:00:00.000Z\"," +
            "\"message\":{\"id\":\"m1\",\"usage\":{\"input_tokens\":1,\"output_tokens\":1," +
            "\"cache_creation_input_tokens\":0,\"cache_read_input_tokens\":0}}}";
        var entry = Assert.Single(ClaudeLogParser.Parse([line], Tz));
        Assert.Equal("unknown", entry.Model);
    }

    [Fact]
    public void InvalidTimestampLineIsSkipped()
    {
        var lines = new[]
        {
            AssistantLine("m1", null, "not-a-timestamp", 10, 5),
            AssistantLine("m2", null, "2026-09-01T06:00:00.000Z", 10, 5),
        };
        var entry = Assert.Single(ClaudeLogParser.Parse(lines, Tz));
        Assert.Equal("m2|", entry.Id);
    }

    [Fact]
    public void LocalDayUsesInjectedTimeZone()
    {
        var tz = TimeZoneInfo.CreateCustomTimeZone(
            "ptb-test-plus9", TimeSpan.FromHours(9), "ptb-test-plus9", "ptb-test-plus9");
        var entry = Assert.Single(ClaudeLogParser.Parse(
            [AssistantLine("m1", null, "2026-09-01T20:30:00.000Z", 1, 1)], tz));
        Assert.Equal("2026-09-02", entry.LocalDay);
    }

    [Fact]
    public void EmptyInputYieldsNoEntries() =>
        Assert.Empty(ClaudeLogParser.Parse(Array.Empty<string>(), Tz));
}
