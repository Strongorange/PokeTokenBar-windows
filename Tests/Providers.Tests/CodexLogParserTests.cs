using System.Text;
using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class CodexLogParserTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    private static IReadOnlyList<string> FixtureLines(string name) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    internal static string TokenLine(
        string ts, long input = 1_000, long cached = 200, long output = 50, long reasoning = 10) =>
        "{\"type\":\"event_msg\",\"timestamp\":\"" + ts + "\",\"payload\":{\"type\":\"token_count\",\"info\":{" +
        "\"last_token_usage\":{\"input_tokens\":" + input + ",\"cached_input_tokens\":" + cached +
        ",\"output_tokens\":" + output + ",\"reasoning_output_tokens\":" + reasoning +
        ",\"total_tokens\":" + (input + output) + "}}}}";

    internal static string SessionMetaLine(string id, string ts) =>
        "{\"type\":\"session_meta\",\"timestamp\":\"" + ts + "\",\"payload\":{\"id\":\"" + id +
        "\",\"session_id\":\"" + id + "\"}}";

    internal static string StateLine(
        string ts,
        long cumulativeInput,
        long cumulativeOutput,
        long lastInput,
        long lastOutput,
        long cumulativeCached = 0,
        long cumulativeReasoning = 0,
        long lastCached = 0,
        long lastReasoning = 0,
        long? lastTotal = null,
        long? cacheWrite = null)
    {
        var cumulativeTotal = cumulativeInput + cumulativeOutput;
        var reportedLastTotal = lastTotal ?? (lastInput + lastOutput);
        var cacheWriteField = cacheWrite is { } value ? ",\"cache_write_input_tokens\":" + value : "";
        return "{\"type\":\"event_msg\",\"timestamp\":\"" + ts +
               "\",\"payload\":{\"type\":\"token_count\",\"info\":{" +
               "\"total_token_usage\":{\"input_tokens\":" + cumulativeInput +
               ",\"cached_input_tokens\":" + cumulativeCached + cacheWriteField +
               ",\"output_tokens\":" + cumulativeOutput +
               ",\"reasoning_output_tokens\":" + cumulativeReasoning +
               ",\"total_tokens\":" + cumulativeTotal + "}," +
               "\"last_token_usage\":{\"input_tokens\":" + lastInput +
               ",\"cached_input_tokens\":" + lastCached + cacheWriteField +
               ",\"output_tokens\":" + lastOutput +
               ",\"reasoning_output_tokens\":" + lastReasoning +
               ",\"total_tokens\":" + reportedLastTotal + "}}}}";
    }

    [Fact]
    public void WindowsFixtureParsesCli1400TurnDeltasWithStickyModel()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "windows-codex.jsonl");
        var rollout = CodexLogParser.Parse(path, FixtureLines("windows-codex.jsonl"), Tz);

        Assert.Equal("00000000-0000-4000-8000-0000000000034", rollout.SessionID);
        Assert.Null(rollout.ParentSessionID);
        Assert.False(rollout.IsSubagent);
        Assert.Equal(4, rollout.Events.Count);
        Assert.All(rollout.Events, e =>
        {
            Assert.Equal("gpt-5.5", e.Entry.Model);
            Assert.Equal("2026-06-17", e.Entry.LocalDay);
            Assert.Equal("00000000-0000-4000-8000-0000000000034", e.SessionID);
            Assert.NotNull(e.UsageState);
        });
        var first = Assert.Single(rollout.Events, e => e.Entry.Id == "codex|windows-codex.jsonl|0");
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 5, 12, 14, 412, TimeSpan.Zero), first.Entry.Date);
        Assert.Equal(85_846, first.Entry.Input);
        Assert.Equal(2_267, first.Entry.Output);
        Assert.Equal(0, first.Entry.CacheWrite);
        Assert.Equal(2_432, first.Entry.CacheRead);
        Assert.Equal(90_545, first.Entry.Total);
        var last = Assert.Single(rollout.Events, e => e.Entry.Id == "codex|windows-codex.jsonl|3");
        Assert.Equal(new DateTimeOffset(2026, 6, 17, 5, 13, 11, 52, TimeSpan.Zero), last.Entry.Date);
        Assert.Equal(78_796, last.Entry.Total);
        Assert.False(first.Entry.CostUnavailable);
    }

    [Fact]
    public void WslFixtureParsesCli1534ShapeAndIgnoresExtras()
    {
        var rollout = CodexLogParser.Parse(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "wsl-codex.jsonl"),
            FixtureLines("wsl-codex.jsonl"), Tz);

        Assert.Equal("00000000-0000-4000-8000-0000000000035", rollout.SessionID);
        Assert.Equal(4, rollout.Events.Count);
        Assert.All(rollout.Events, e =>
        {
            Assert.Equal("gpt-5.6-terra", e.Entry.Model);
            Assert.Equal("2026-09-06", e.Entry.LocalDay);
            Assert.Equal(0, e.Entry.CacheWrite);
        });
        var first = Assert.Single(rollout.Events, e => e.Entry.Id == "codex|wsl-codex.jsonl|0");
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 23, 18, 29, 740, TimeSpan.Zero), first.Entry.Date);
        Assert.Equal(22_213, first.Entry.Input);
        Assert.Equal(244, first.Entry.Output);
        Assert.Equal(6_912, first.Entry.CacheRead);
        Assert.Equal(29_369, first.Entry.Total);
        Assert.All(rollout.Events, e => Assert.Equal(0, e.UsageState!.Cumulative.CacheWriteInput));
    }

    [Fact]
    public void FixtureTotalsMatchHandComputedSumsAndFinalCumulative()
    {
        var windows = CodexLogParser.Parse(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "windows-codex.jsonl"),
            FixtureLines("windows-codex.jsonl"), Tz);
        Assert.Equal(153_073L, windows.Events.Sum(e => e.Entry.Input));
        Assert.Equal(5_979L, windows.Events.Sum(e => e.Entry.Output));
        Assert.Equal(0L, windows.Events.Sum(e => e.Entry.CacheWrite));
        Assert.Equal(161_280L, windows.Events.Sum(e => e.Entry.CacheRead));
        Assert.Equal(320_332L, windows.Events.Sum(e => e.Entry.Total));
        Assert.Equal(320_332L, windows.Events[^1].UsageState!.Cumulative.Total);

        var wsl = CodexLogParser.Parse(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "wsl-codex.jsonl"),
            FixtureLines("wsl-codex.jsonl"), Tz);
        Assert.Equal(31_891L, wsl.Events.Sum(e => e.Entry.Input));
        Assert.Equal(504L, wsl.Events.Sum(e => e.Entry.Output));
        Assert.Equal(0L, wsl.Events.Sum(e => e.Entry.CacheWrite));
        Assert.Equal(95_232L, wsl.Events.Sum(e => e.Entry.CacheRead));
        Assert.Equal(127_627L, wsl.Events.Sum(e => e.Entry.Total));
        Assert.Equal(127_627L, wsl.Events[^1].UsageState!.Cumulative.Total);
    }

    [Fact]
    public void WslFixtureLocalDayShiftsToKstWithInjectedTimeZone()
    {
        var tz = TimeZoneInfo.CreateCustomTimeZone(
            "ptb-test-plus9", TimeSpan.FromHours(9), "ptb-test-plus9", "ptb-test-plus9");
        var rollout = CodexLogParser.Parse(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "wsl-codex.jsonl"),
            FixtureLines("wsl-codex.jsonl"), tz);
        Assert.All(rollout.Events, e => Assert.Equal("2026-09-07", e.Entry.LocalDay));
    }

    [Fact]
    public void InfoNullTokenCountRecordIsSkippedEntirely()
    {
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            "{\"timestamp\":\"2026-04-01T01:00:01.000Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":null}}",
            TokenLine("2026-07-30T01:00:02.000Z"),
        };
        var rollout = CodexLogParser.Parse("rollout-a.jsonl", lines, Tz);
        var evt = Assert.Single(rollout.Events);
        Assert.Equal("codex|rollout-a.jsonl|0", evt.Entry.Id);
    }

    [Fact]
    public void TotalOnlyLastIsTrustedWhenThereIsNoCumulative()
    {
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            "{\"type\":\"event_msg\",\"timestamp\":\"2026-07-30T01:00:01.000Z\",\"payload\":{\"type\":\"token_count\",\"info\":{\"last_token_usage\":{\"input_tokens\":0,\"cached_input_tokens\":0,\"output_tokens\":0,\"reasoning_output_tokens\":0,\"total_tokens\":100}}}}",
        };
        var evt = Assert.Single(CodexLogParser.Parse("rollout-a.jsonl", lines, Tz).Events);
        Assert.Equal(100, evt.Entry.Input);
        Assert.Equal(0, evt.Entry.Output);
        Assert.Equal(0, evt.Entry.CacheRead);
        Assert.Equal(100, evt.Entry.Total);
        Assert.True(evt.Entry.CostUnavailable);
        Assert.Null(evt.UsageState);
    }

    [Fact]
    public void TotalOnlyLastIsTrustedWhenCumulativeComponentsAreEmpty()
    {
        var line =
            "{\"type\":\"event_msg\",\"timestamp\":\"2026-07-30T01:00:01.000Z\",\"payload\":{\"type\":\"token_count\",\"info\":{" +
            "\"total_token_usage\":{\"input_tokens\":0,\"cached_input_tokens\":0,\"cache_write_input_tokens\":0,\"output_tokens\":0,\"reasoning_output_tokens\":0,\"total_tokens\":100}," +
            "\"last_token_usage\":{\"input_tokens\":0,\"cached_input_tokens\":0,\"cache_write_input_tokens\":0,\"output_tokens\":0,\"reasoning_output_tokens\":0,\"total_tokens\":100}}}}";
        var evt = Assert.Single(CodexLogParser.Parse("rollout-a.jsonl",
            [SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"), line], Tz).Events);
        Assert.Equal(100, evt.Entry.Input);
        Assert.True(evt.Entry.CostUnavailable);
    }

    [Fact]
    public void TotalOnlyLastIsTrustedWhenLastTotalEqualsCumulativeTotal()
    {
        var line = StateLine("2026-07-30T01:00:01.000Z",
            cumulativeInput: 50, cumulativeOutput: 50, lastInput: 0, lastOutput: 0,
            lastTotal: 100);
        var evt = Assert.Single(CodexLogParser.Parse("rollout-a.jsonl",
            [SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"), line], Tz).Events);
        Assert.Equal(100, evt.Entry.Input);
        Assert.Equal(100, evt.Entry.Total);
    }

    [Fact]
    public void ForkPostReplayZeroContextShapeStaysZero()
    {
        var line = StateLine("2026-07-30T01:00:01.000Z",
            cumulativeInput: 100, cumulativeOutput: 10, lastInput: 0, lastOutput: 0,
            lastTotal: 6_742);
        var evt = Assert.Single(CodexLogParser.Parse("rollout-a.jsonl",
            [SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"), line], Tz).Events);
        Assert.Equal(0, evt.Entry.Total);
        Assert.True(evt.Entry.CostUnavailable);
    }

    [Fact]
    public void ModelDefaultsToCodexAndTurnContextMakesItSticky()
    {
        var turnContext =
            "{\"type\":\"turn_context\",\"timestamp\":\"2026-07-30T01:00:00.500Z\",\"payload\":{\"turn_id\":\"t1\",\"model\":\"gpt-5.5\"}}";
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            TokenLine("2026-07-30T01:00:00.100Z"),
            turnContext,
            TokenLine("2026-07-30T01:00:01.000Z"),
        };
        var rollout = CodexLogParser.Parse("rollout-a.jsonl", lines, Tz);
        Assert.Equal("codex", rollout.Events[0].Entry.Model);
        Assert.Equal("gpt-5.5", rollout.Events[1].Entry.Model);
    }

    [Fact]
    public void ModelCanBeNestedInsideTurnContextPayload()
    {
        var nested =
            "{\"type\":\"turn_context\",\"timestamp\":\"2026-07-30T01:00:00.500Z\",\"payload\":{\"turn_context\":{\"model\":\"gpt-5.6-terra\"}}}";
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            nested,
            TokenLine("2026-07-30T01:00:01.000Z"),
        };
        var rollout = CodexLogParser.Parse("rollout-a.jsonl", lines, Tz);
        Assert.Equal("gpt-5.6-terra", Assert.Single(rollout.Events).Entry.Model);
    }

    [Fact]
    public void ConsecutiveIdenticalStatesCollapseToOneEvent()
    {
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            StateLine("2026-07-30T01:00:02.000Z", 100, 10, 100, 10),
            StateLine("2026-07-30T01:00:03.000Z", 300, 30, 200, 20),
        };
        var rollout = CodexLogParser.Parse("rollout-a.jsonl", lines, Tz);
        Assert.Equal(2, rollout.Events.Count);
        Assert.Equal("codex|rollout-a.jsonl|0", rollout.Events[0].Entry.Id);
        Assert.Equal("codex|rollout-a.jsonl|2", rollout.Events[1].Entry.Id);
    }

    [Fact]
    public void SameStateAcrossSessionSwitchDoesNotCollapse()
    {
        var lines = new[]
        {
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            SessionMetaLine("session-b", "2026-07-30T01:00:01.500Z"),
            StateLine("2026-07-30T01:00:02.000Z", 100, 10, 100, 10),
        };
        var rollout = CodexLogParser.Parse("rollout-a.jsonl", lines, Tz);
        Assert.Equal(2, rollout.Events.Count);
        Assert.Equal("session-a", rollout.Events[0].SessionID);
        Assert.Equal("session-b", rollout.Events[1].SessionID);
    }

    [Fact]
    public void FirstSessionMetaWinsForRolloutIdentity()
    {
        var lines = new[]
        {
            "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-28T08:28:24.065Z\",\"payload\":{\"id\":\"child\",\"forked_from_id\":\"parent\",\"thread_source\":\"user\"}}",
            "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-28T08:28:24.066Z\",\"payload\":{\"id\":\"parent\",\"session_id\":\"parent\",\"thread_source\":\"automation\"}}",
            TokenLine("2026-07-28T08:28:24.066Z"),
        };
        var rollout = CodexLogParser.Parse("rollout-child.jsonl", lines, Tz);
        Assert.Equal("child", rollout.SessionID);
        Assert.Equal("parent", rollout.ParentSessionID);
        Assert.False(rollout.IsSubagent);
        Assert.Equal(new DateTimeOffset(2026, 7, 28, 8, 28, 24, 65, TimeSpan.Zero), rollout.ForkedAt);
        Assert.Equal("parent", Assert.Single(rollout.Events).SessionID);
    }

    [Fact]
    public void SubagentMetaUsesIdFirstAndDetectsSubagentSource()
    {
        var subagentMeta =
            "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-30T06:48:11.438Z\",\"payload\":{\"id\":\"child\",\"session_id\":\"parent\",\"parent_thread_id\":\"parent\",\"source\":{\"subagent\":{\"thread_spawn\":{\"parent_thread_id\":\"parent\",\"depth\":1}}}}}";
        var rollout = CodexLogParser.Parse("rollout-child.jsonl",
            [subagentMeta, TokenLine("2026-07-30T06:48:18.000Z")], Tz);
        Assert.Equal("child", rollout.SessionID);
        Assert.Equal("parent", rollout.ParentSessionID);
        Assert.True(rollout.IsSubagent);
    }

    [Fact]
    public void AbsurdTokenCountsClampInsteadOfFailing()
    {
        var absurd =
            "{\"type\":\"event_msg\",\"timestamp\":\"2026-07-30T01:00:01.000Z\",\"payload\":{\"type\":\"token_count\",\"info\":{\"total_token_usage\":{\"input_tokens\":1e30,\"cached_input_tokens\":0,\"output_tokens\":10,\"total_tokens\":1e30},\"last_token_usage\":{\"input_tokens\":100,\"cached_input_tokens\":0,\"output_tokens\":10,\"total_tokens\":110}}}}";
        var rollout = CodexLogParser.Parse("rollout-huge.jsonl",
            [SessionMetaLine("huge", "2026-07-30T01:00:00.000Z"), absurd], Tz);
        var evt = Assert.Single(rollout.Events);
        Assert.Equal(110, evt.Entry.Total);
        Assert.Equal(UsageAggregation.MaxParsedTokenValue, evt.UsageState!.Cumulative.Input);

        var absurdLast =
            "{\"type\":\"event_msg\",\"timestamp\":\"2026-07-30T01:00:01.000Z\",\"payload\":{\"type\":\"token_count\",\"info\":{\"total_token_usage\":{\"input_tokens\":10,\"cached_input_tokens\":0,\"output_tokens\":5,\"total_tokens\":15},\"last_token_usage\":{\"input_tokens\":1e30,\"cached_input_tokens\":0,\"output_tokens\":1e30,\"total_tokens\":1e30}}}}";
        var clamped = CodexLogParser.Parse("rollout-huge.jsonl",
            [SessionMetaLine("huge", "2026-07-30T01:00:00.000Z"), absurdLast], Tz);
        Assert.Equal(UsageAggregation.MaxParsedTokenValue, Assert.Single(clamped.Events).Entry.Input);
        Assert.Equal(UsageAggregation.MaxParsedTokenValue, clamped.Events[0].Entry.Output);
    }

    [Fact]
    public void NegativeAndNonNumericTokensFoldToZero()
    {
        var line =
            "{\"type\":\"event_msg\",\"timestamp\":\"2026-07-30T01:00:01.000Z\",\"payload\":{\"type\":\"token_count\",\"info\":{\"last_token_usage\":{\"input_tokens\":-5,\"cached_input_tokens\":null,\"output_tokens\":\"nope\",\"total_tokens\":0}}}}";
        var evt = Assert.Single(CodexLogParser.Parse("rollout-a.jsonl",
            [SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"), line], Tz).Events);
        Assert.Equal(0, evt.Entry.Input);
        Assert.Equal(0, evt.Entry.Output);
        Assert.Equal(0, evt.Entry.CacheRead);
    }

    [Fact]
    public void EmptyInputYieldsEmptyRollout()
    {
        var rollout = CodexLogParser.Parse("rollout-empty.jsonl", Array.Empty<string>(), Tz);
        Assert.Empty(rollout.Events);
        Assert.Null(rollout.SessionID);
        Assert.Null(rollout.ParentSessionID);
        Assert.False(rollout.IsSubagent);
    }

    [Fact]
    public void NonTokenCountLinesAreSkipped()
    {
        var lines = new[]
        {
            "{not json at all",
            "",
            "{\"type\":\"response_item\",\"timestamp\":\"2026-07-30T01:00:00.000Z\",\"payload\":{\"text\":\"token_count mentioned but wrong type\"}}",
            SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            TokenLine("2026-07-30T01:00:01.000Z"),
        };
        Assert.Single(CodexLogParser.Parse("rollout-a.jsonl", lines, Tz).Events);
    }
}
