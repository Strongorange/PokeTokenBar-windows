using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class UsageAggregationTests
{
    private static readonly TimeZoneInfo Seoul = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    private static UsageEntry Entry(string id, string isoUtc, string localDay, string model,
        long input, long output, long cacheWrite = 0, long cacheRead = 0) =>
        new(id, DateTimeOffset.Parse(isoUtc), localDay, model, input, output, cacheWrite, cacheRead);

    [Fact]
    public void DailyReturnsNullWhenDayTotalIsZero()
    {
        var entries = new[] { Entry("a", "2026-09-22T01:00:00Z", "2026-09-22", "claude-sonnet-5", 0, 0) };
        Assert.Null(UsageAggregation.Daily(entries, "2026-09-22"));
    }

    [Fact]
    public void DailyFiltersByLocalDay()
    {
        var entries = new[]
        {
            Entry("a", "2026-09-22T01:00:00Z", "2026-09-22", "claude-sonnet-5", 100, 50),
            Entry("b", "2026-09-23T01:00:00Z", "2026-09-23", "claude-sonnet-5", 10, 5)
        };
        var daily = UsageAggregation.Daily(entries, "2026-09-22");
        Assert.NotNull(daily);
        Assert.Equal(150, daily!.TotalTokens);
    }

    [Fact]
    public void DailyIncludeModelsSumsPerModel()
    {
        var entries = new[]
        {
            Entry("a", "2026-09-22T01:00:00Z", "2026-09-22", "claude-sonnet-5", 100, 50),
            Entry("b", "2026-09-22T02:00:00Z", "2026-09-22", "claude-opus-5", 10, 5),
            Entry("c", "2026-09-22T03:00:00Z", "2026-09-22", "claude-sonnet-5", 1, 1)
        };
        var daily = UsageAggregation.Daily(entries, "2026-09-22", includeModels: true);
        Assert.NotNull(daily);
        Assert.Equal(152, daily!.Models!["claude-sonnet-5"]);
        Assert.Equal(15, daily.Models["claude-opus-5"]);
    }

    [Fact]
    public void DedupKeepsLargestTotalPerId()
    {
        var entries = new[]
        {
            Entry("same", "2026-09-22T01:00:00Z", "2026-09-22", "claude-sonnet-5", 10, 0),
            Entry("same", "2026-09-22T01:00:01Z", "2026-09-22", "claude-sonnet-5", 10, 15),
            Entry("other", "2026-09-22T01:00:02Z", "2026-09-22", "claude-sonnet-5", 5, 5)
        };
        var deduped = UsageAggregation.DedupKeepMax(entries);
        Assert.Equal(2, deduped.Count);
        Assert.Equal(25, deduped.Single(e => e.Id == "same").Total);
    }

    [Fact]
    public void PeriodIsInclusiveOnBothEnds()
    {
        var entries = new[]
        {
            Entry("a", "2026-09-01T00:00:00Z", "2026-09-01", "claude-sonnet-5", 1, 0),
            Entry("b", "2026-09-05T00:00:00Z", "2026-09-05", "claude-sonnet-5", 2, 0),
            Entry("c", "2026-09-10T00:00:00Z", "2026-09-10", "claude-sonnet-5", 4, 0),
            Entry("d", "2026-08-31T00:00:00Z", "2026-08-31", "claude-sonnet-5", 8, 0)
        };
        var period = UsageAggregation.Period(entries, "2026-09", "2026-09-01", "2026-09-10");
        Assert.Equal(7, period.TotalTokens);
    }

    [Fact]
    public void MonthDailySeriesBuildsAxisFromMonthRangeWithExplicitZeroDays()
    {
        var entries = new[]
        {
            Entry("a", "2026-09-01T01:00:00Z", "2026-09-01", "claude-sonnet-5", 100, 0),
            Entry("b", "2026-09-05T01:00:00Z", "2026-09-05", "claude-sonnet-5", 20, 0),
            Entry("c", "2026-08-31T01:00:00Z", "2026-08-31", "claude-sonnet-5", 400, 0)
        };
        var now = DateTimeOffset.Parse("2026-09-10T03:00:00Z");
        var series = UsageAggregation.MonthDailySeries(entries, now, Seoul);
        Assert.Equal(10, series.Count);
        Assert.Equal("2026-09-01", series[0].Date);
        Assert.Equal("2026-09-10", series[^1].Date);
        Assert.Equal(100, series[0].TotalTokens);
        Assert.Equal(0, series[1].TotalTokens);
        Assert.Equal(20, series[4].TotalTokens);
        Assert.Equal(0, series[9].TotalTokens);
        Assert.DoesNotContain(series, day => day.Date == "2026-08-31");
    }

    [Fact]
    public void MonthDailySeriesSumsEqualToMonthTotal()
    {
        var entries = new[]
        {
            Entry("a", "2026-09-01T01:00:00Z", "2026-09-01", "claude-sonnet-5", 100, 0),
            Entry("b", "2026-09-05T01:00:00Z", "2026-09-05", "claude-sonnet-5", 20, 0),
            Entry("c", "2026-09-09T01:00:00Z", "2026-09-09", "claude-sonnet-5", 3, 0)
        };
        var now = DateTimeOffset.Parse("2026-09-10T03:00:00Z");
        var series = UsageAggregation.MonthDailySeries(entries, now, Seoul);
        var monthTotal = UsageAggregation.Period(entries, "2026-09", "2026-09-01", "2026-09-10");
        Assert.Equal(monthTotal.TotalTokens, series.Sum(d => d.TotalTokens));
    }

    [Fact]
    public void MonthDailySeriesSurvivesDstTransition()
    {
        var newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var entries = new[]
        {
            Entry("a", "2026-03-08T10:00:00Z", "2026-03-08", "claude-sonnet-5", 10, 0)
        };
        var now = DateTimeOffset.Parse("2026-03-09T12:00:00Z");
        var series = UsageAggregation.MonthDailySeries(entries, now, newYork);
        Assert.Equal(9, series.Count);
        Assert.Equal("2026-03-01", series[0].Date);
        Assert.Equal("2026-03-09", series[^1].Date);
    }

    [Fact]
    public void ActiveBlockExcludesOldEntriesAndZeroTotalSynthetics()
    {
        var now = DateTimeOffset.Parse("2026-09-22T12:00:00Z");
        var entries = new[]
        {
            Entry("old", "2026-09-22T06:00:00Z", "2026-09-22", "claude-sonnet-5", 1000, 0),
            Entry("synthetic", "2026-09-22T11:50:00Z", "2026-09-22", "<synthetic>", 0, 0),
            Entry("first", "2026-09-22T11:00:00Z", "2026-09-22", "claude-sonnet-5", 600, 0),
            Entry("second", "2026-09-22T11:30:00Z", "2026-09-22", "claude-sonnet-5", 300, 0)
        };
        var block = UsageAggregation.ActiveBlock(entries, now);
        Assert.NotNull(block);
        Assert.Equal(900, block!.TotalTokens);
        Assert.Equal("2026-09-22T11:00:00Z", block.StartTime);
        Assert.Equal("2026-09-22T16:00:00Z", block.EndTime);
        Assert.True(block.IsActive);
        Assert.Equal(900 / 60.0, block.TokensPerMinute!.Value, 9);
    }

    [Fact]
    public void ActiveBlockReturnsNullWhenWindowEmpty()
    {
        var now = DateTimeOffset.Parse("2026-09-22T12:00:00Z");
        var entries = new[] { Entry("old", "2026-09-22T01:00:00Z", "2026-09-22", "claude-sonnet-5", 100, 0) };
        Assert.Null(UsageAggregation.ActiveBlock(entries, now));
    }

    [Fact]
    public void LocalDayUsesConfiguredTimezone()
    {
        var instant = DateTimeOffset.Parse("2026-09-22T18:00:00Z");
        Assert.Equal("2026-09-23", UsageAggregation.LocalDay(instant, Seoul));
        Assert.Equal("2026-09-22", UsageAggregation.LocalDay(instant, TimeZoneInfo.Utc));
    }

    [Fact]
    public void StartOfWeekUsesCultureFirstDay()
    {
        var wednesday = new DateTime(2026, 9, 23);
        var sundayStart = UsageAggregation.StartOfWeek(wednesday, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        var mondayStart = UsageAggregation.StartOfWeek(wednesday, System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal(new DateTime(2026, 9, 20), sundayStart);
        Assert.Equal(new DateTime(2026, 9, 21), mondayStart);
    }

    [Fact]
    public void EnrichmentScanStartCoversAllThreeWindows()
    {
        var now = DateTimeOffset.Parse("2026-09-02T01:00:00Z");
        var start = UsageAggregation.EnrichmentScanStart(now, Seoul);
        Assert.Equal(DateTimeOffset.Parse("2026-08-30T00:00:00+09:00").ToUniversalTime(), start);
    }
}
