using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

public class DailyTrendMetricsTests
{
    private const string Today = "2026-09-23";

    [Fact]
    public void BarHeightCoversZeroAndPeakBoundaries()
    {
        Assert.Equal(DailyTrendMetrics.Baseline, DailyTrendMetrics.BarHeight(0, 100));
        Assert.Equal(DailyTrendMetrics.Baseline, DailyTrendMetrics.BarHeight(50, 0));
        Assert.Equal(DailyTrendMetrics.Baseline, DailyTrendMetrics.BarHeight(0, 0));
        Assert.Equal(13, DailyTrendMetrics.BarHeight(50, 100));
        Assert.Equal(DailyTrendMetrics.Track, DailyTrendMetrics.BarHeight(100, 100));
        Assert.Equal(DailyTrendMetrics.Track, DailyTrendMetrics.BarHeight(200, 100));
        Assert.Equal(DailyTrendMetrics.Baseline, DailyTrendMetrics.BarHeight(1, 1_000_000));
    }

    [Fact]
    public void AxisLabelMarksFirstDayIntervalsAndToday()
    {
        Assert.Equal("1", DailyTrendMetrics.AxisLabel("2026-09-01", Today));
        Assert.Equal("7", DailyTrendMetrics.AxisLabel("2026-09-07", Today));
        Assert.Equal("14", DailyTrendMetrics.AxisLabel("2026-09-14", Today));
        Assert.Null(DailyTrendMetrics.AxisLabel("2026-09-19", Today));
        Assert.Equal("23", DailyTrendMetrics.AxisLabel("2026-09-23", Today));
    }

    [Fact]
    public void AxisLabelSuppressesRegularLabelsNearTodayButKeepsFarOnes()
    {
        Assert.Null(DailyTrendMetrics.AxisLabel("2026-09-21", Today));
        Assert.Equal("21", DailyTrendMetrics.AxisLabel("2026-09-21", "2026-09-25"));
        Assert.Null(DailyTrendMetrics.AxisLabel("2026-10-01", "2026-10-02"));
        Assert.Equal("1", DailyTrendMetrics.AxisLabel("2026-10-01", "2026-10-05"));
    }

    [Fact]
    public void AxisLabelReturnsNothingForUnparseableDates()
    {
        Assert.Null(DailyTrendMetrics.AxisLabel("", Today));
        Assert.Null(DailyTrendMetrics.AxisLabel("2026-9-7", Today));
    }

    [Fact]
    public void WeekendFollowsSaturdaySunday()
    {
        Assert.True(DailyTrendMetrics.IsWeekend("2026-09-26"));
        Assert.True(DailyTrendMetrics.IsWeekend("2026-09-27"));
        Assert.False(DailyTrendMetrics.IsWeekend("2026-09-28"));
        Assert.False(DailyTrendMetrics.IsWeekend("not-a-date"));
    }

    [Fact]
    public void DayStampUsesLanguageSpecificOrderAndWeekdayNames()
    {
        Assert.Equal("9. 28. (월)", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.Ko));
        Assert.Equal("Mon, 9/28", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.En));
        Assert.Equal("9/28(月)", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.Ja));
        Assert.Equal("lun 28/09", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.Es));
        Assert.Equal("lun. 28/09", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.Fr));
        Assert.Equal("seg., 28/09", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.Pt));
        Assert.Equal("Mo., 28.9.", DailyTrendMetrics.DayStamp("2026-09-28", AppLanguage.De));
    }

    [Fact]
    public void DayStampReturnsEmptyForUnparseableDate()
    {
        Assert.Equal("", DailyTrendMetrics.DayStamp("garbage", AppLanguage.En));
    }

    [Fact]
    public void PeakFindsHighestTotalAndHandlesNullSeries()
    {
        var series = new List<DailyUsage>
        {
            new("2026-09-21", 0, 0, 0, 0, 120, 0),
            new("2026-09-22", 0, 0, 0, 0, 480, 0),
            new("2026-09-23", 0, 0, 0, 0, 90, 0),
        };

        Assert.Equal(480, DailyTrendMetrics.Peak(series));
        Assert.Equal(0, DailyTrendMetrics.Peak(null));
        Assert.Equal(0, DailyTrendMetrics.Peak([]));
        Assert.Equal(0, DailyTrendMetrics.Peak(
            new List<DailyUsage> { new("2026-09-21", 0, 0, 0, 0, 0, 0) }));
    }

    [Fact]
    public void ShowsCostRequiresAtLeastOneKnownCoverage()
    {
        var unknown = new List<DailyUsage>
        {
            new("2026-09-21", 0, 0, 0, 0, 120, 0, costCoverage: CostCoverage.Unavailable),
        };
        var known = new List<DailyUsage>
        {
            new("2026-09-21", 0, 0, 0, 0, 120, 0, costCoverage: CostCoverage.Unavailable),
            new("2026-09-22", 0, 0, 0, 0, 120, 1.5, costCoverage: CostCoverage.Source),
        };

        Assert.False(DailyTrendMetrics.ShowsCost(null));
        Assert.False(DailyTrendMetrics.ShowsCost(unknown));
        Assert.True(DailyTrendMetrics.ShowsCost(known));
    }

    [Fact]
    public void TodayUsageMatchesDateKeyAndFallsBackToNull()
    {
        var series = new List<DailyUsage>
        {
            new("2026-09-22", 0, 0, 0, 0, 480, 0),
            new("2026-09-23", 0, 0, 0, 0, 90, 0),
        };

        var today = DailyTrendMetrics.TodayUsage(series, "2026-09-23");
        Assert.NotNull(today);
        Assert.Equal(90, today!.TotalTokens);
        Assert.Null(DailyTrendMetrics.TodayUsage(series, "2026-09-24"));
        Assert.Null(DailyTrendMetrics.TodayUsage(null, "2026-09-23"));
    }
}
