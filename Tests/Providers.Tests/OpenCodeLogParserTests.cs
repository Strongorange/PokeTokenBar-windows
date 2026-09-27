using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class OpenCodeLogParserTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "opencode", "opencode-messages.jsonl");

    private static List<UsageEntry> ParseFixture() =>
        OpenCodeLogParser.Parse(FixturePath, File.ReadAllLines(FixturePath), Tz);

    private static UsageEntry? ParseRow(string json) =>
        OpenCodeLogParser.ParseRow(json, "fixture", Tz);

    [Fact]
    public void FixtureParsesToExpectedEntries()
    {
        var entries = ParseFixture();

        Assert.Equal(9, entries.Count);
        Assert.Equal(entries.Count, entries.Select(e => e.Id).Distinct().Count());
        Assert.All(entries, e => Assert.StartsWith("opencode|opencode-messages.jsonl|", e.Id, StringComparison.Ordinal));
        Assert.All(entries, e => Assert.Equal("2026-09-23", e.LocalDay));
        Assert.All(entries, e => Assert.True(e.Total >= 0));
    }

    [Fact]
    public void AssistantRowMapsTokensWithReasoningMergedIntoOutput()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_02", StringComparison.Ordinal));

        Assert.Equal("muse-spark-1.3-contributor-free", entry.Model);
        Assert.Equal(1747, entry.Input);
        Assert.Equal(110 + 1221, entry.Output);
        Assert.Equal(0, entry.CacheWrite);
        Assert.Equal(32753, entry.CacheRead);
        Assert.Equal(1747 + 110 + 1221 + 32753, entry.Total);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1790126430000), entry.Date);
        Assert.Null(entry.ExplicitCost);
    }

    [Fact]
    public void ReportedTotalFieldIsIgnoredInFavorOfComponents()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_05", StringComparison.Ordinal));

        Assert.NotEqual(999999999, entry.Total);
        Assert.Equal(1700, entry.Total);
    }

    [Fact]
    public void PositiveCostBecomesExplicitSourceCost()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_05", StringComparison.Ordinal));

        Assert.Equal(0.0123, entry.ExplicitCost);
        Assert.False(entry.CostIsEstimate);
    }

    [Fact]
    public void DuplicateIdKeepsMaxTotals()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_01", StringComparison.Ordinal));

        Assert.Equal(30000, entry.Input);
        Assert.Equal(100 + 400, entry.Output);
        Assert.Equal(2500, entry.CacheRead);
        Assert.Equal(33000, entry.Total);
    }

    [Fact]
    public void MissingCompletedFallsBackToCreatedThenRowStamp()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_07", StringComparison.Ordinal));

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1790130400000), entry.Date);
    }

    [Fact]
    public void MissingModelFallsBackToUnknown()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_11", StringComparison.Ordinal));

        Assert.Equal("unknown", entry.Model);
    }

    [Fact]
    public void NonAssistantTokenlessMalformedAndIdlessRowsAreSkipped()
    {
        var entries = ParseFixture();

        Assert.DoesNotContain(entries, e => e.Id.EndsWith("|msg_04", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, e => e.Id.EndsWith("|msg_08", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, e => e.Id.EndsWith("|broken", StringComparison.Ordinal));
    }

    [Fact]
    public void InFlightZeroTokenRowsAreKeptForKeepMaxDedup()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_03", StringComparison.Ordinal));

        Assert.Equal(0, entry.Total);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1790127405000), entry.Date);
    }

    [Fact]
    public void HugeTokenCountsClampToTheParsedCeiling()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_06", StringComparison.Ordinal));

        Assert.Equal(UsageAggregation.MaxParsedTokenValue, entry.Input);
        Assert.Equal(UsageAggregation.MaxParsedTokenValue, entry.Output);
        Assert.Equal(2 * UsageAggregation.MaxParsedTokenValue, entry.Total);
    }

    [Fact]
    public void NegativeTokenCountsClampToZero()
    {
        var entry = ParseFixture().Single(e => e.Id.EndsWith("|msg_09", StringComparison.Ordinal));

        Assert.Equal(0, entry.Input);
        Assert.Equal(3, entry.Output);
        Assert.Equal(0, entry.CacheWrite);
        Assert.Equal(0, entry.CacheRead);
    }

    [Fact]
    public void NullRowReturnsNothing()
    {
        Assert.Null(ParseRow("not json at all"));
        Assert.Null(ParseRow("[1,2,3]"));
        Assert.Null(ParseRow("""{"id": "msg_x", "data": "not-an-object"}"""));
        Assert.Null(ParseRow("""{"data": {"role": "assistant"}}"""));
    }

    [Fact]
    public void LocalDayUsesTheInjectedTimeZone()
    {
        var seoul = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        var row = """
            {"id": "msg_tz", "data": {"role": "assistant", "modelID": "gpt-5.4", "tokens": {"input": 1, "output": 1, "reasoning": 0, "cache": {"write": 0, "read": 0}}, "time": {"created": 1790207400000, "completed": 1790207400000}}}
            """;

        var utcEntry = OpenCodeLogParser.ParseRow(row, "fixture", Tz);
        var seoulEntry = OpenCodeLogParser.ParseRow(row, "fixture", seoul);

        Assert.NotNull(utcEntry);
        Assert.NotNull(seoulEntry);
        Assert.Equal("2026-09-23", utcEntry.Value.LocalDay);
        Assert.Equal("2026-09-24", seoulEntry.Value.LocalDay);
    }
}
