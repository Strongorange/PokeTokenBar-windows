using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

/// <summary>
/// Port of the macOS UsageRecapTests contract (#332): ledger monotonicity and coverage,
/// calendar periods, bucket semantics (used/zero/no-data), same-days comparison for running
/// periods, streaks, graduations and the canGoBack bound.
/// </summary>
public class UsageRecapTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private static string Key(int year, int month, int day) =>
        new DateTime(year, month, day).ToString("yyyy-MM-dd");

    private static DailyUsage Day(string date, long tokens) => new(date, 0, 0, 0, 0, tokens, 0);

    private static UsageLedger Ledger(string since, params (string Day, long Tokens)[] rows)
    {
        var ledger = new UsageLedger { CoveredSince = since };
        foreach (var (day, tokens) in rows)
            ledger.TokensByDay[day] = tokens;
        return ledger;
    }

    private static UsageRecap Make(UsageLedger ledger, RecapScope scope, int offset,
        IReadOnlyList<DexEntry> dex, DateTimeOffset now) =>
        UsageRecap.Make(ledger, dex,
            RecapPeriod.Containing(scope, now, Tz).Shifted(offset), now, Tz);

    // MARK: Ledger

    [Fact]
    public void ADayNeverGoesDown()
    {
        var ledger = Ledger(Key(2026, 9, 1), (Key(2026, 9, 15), 900));
        ledger.Merge([Day(Key(2026, 9, 15), 700)]);
        Assert.Equal(900, ledger.Tokens(Key(2026, 9, 15)));
        ledger.Merge([Day(Key(2026, 9, 15), 1_000)]);
        Assert.Equal(1_000, ledger.Tokens(Key(2026, 9, 15)));
    }

    [Fact]
    public void TheSeriesVouchesForItsEmptyDaysToo()
    {
        var ledger = new UsageLedger();
        ledger.Merge([Day(Key(2026, 9, 1), 100), Day(Key(2026, 9, 2), 0)]);
        Assert.Equal(Key(2026, 9, 1), ledger.CoveredSince);
        Assert.True(ledger.Covers(Key(2026, 9, 2)));
        Assert.False(ledger.Covers(Key(2026, 8, 31)));
    }

    [Fact]
    public void PruningForgetsTheCoverageItDrops()
    {
        var ledger = Ledger(Key(2026, 1, 1), (Key(2026, 1, 5), 10), (Key(2026, 9, 1), 20));
        ledger.Prune(Key(2026, 9, 1));
        Assert.DoesNotContain(Key(2026, 1, 5), ledger.TokensByDay);
        Assert.Equal(Key(2026, 9, 1), ledger.CoveredSince);
        Assert.False(ledger.Covers(Key(2026, 8, 31)));
    }

    [Fact]
    public void JunkIsDroppedOnLoad()
    {
        var ledger = UsageLedger.Read(JsonDocument.Parse("""
            {"tokensByDay": {"2026-09-01": 100, "junk": 5, "not-a-day": 7, "2026-09-02": 0},
             "coveredSince": "garbage"}
            """).RootElement);
        Assert.Equal([Key(2026, 9, 1)], ledger.TokensByDay.Keys);
        Assert.Null(ledger.CoveredSince);
    }

    [Fact]
    public void SaveAndLoadRoundTrip()
    {
        var ledger = Ledger(Key(2026, 8, 1), (Key(2026, 9, 17), 12_345));
        var reloaded = UsageLedger.Read(JsonDocument.Parse(ledger.Write().ToJsonString()).RootElement);
        Assert.Equal(ledger.CoveredSince, reloaded.CoveredSince);
        Assert.Equal(ledger.TokensByDay, reloaded.TokensByDay);
    }

    // MARK: Periods

    [Fact]
    public void AWeekStartsOnSunday()
    {
        // 2026-09-23 is a Wednesday; the recap week runs Sunday the 20th through Saturday the 26th.
        var period = RecapPeriod.Containing(RecapScope.Week, Now, Tz);
        Assert.Equal(new DateTime(2026, 9, 20), period.LocalStart);
        Assert.Equal(new DateTime(2026, 9, 27), period.LocalEndExclusive);
    }

    [Fact]
    public void SteppingBackLandsOnWholePeriods()
    {
        var week = RecapPeriod.Containing(RecapScope.Week, Now, Tz).Shifted(-1);
        Assert.Equal(new DateTime(2026, 9, 13), week.LocalStart);
        var month = RecapPeriod.Containing(RecapScope.Month, Now, Tz).Shifted(-1);
        Assert.Equal(new DateTime(2026, 8, 1), month.LocalStart);
        Assert.Equal(new DateTime(2026, 9, 1), month.LocalEndExclusive);
        var year = RecapPeriod.Containing(RecapScope.Year, Now, Tz).Shifted(-1);
        Assert.Equal(new DateTime(2025, 1, 1), year.LocalStart);
        // Month-end clamping: March 31 back one month lands on the whole of February.
        var march = RecapPeriod.Containing(RecapScope.Month,
            new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero), Tz).Shifted(-1);
        Assert.Equal(new DateTime(2026, 2, 1), march.LocalStart);
        Assert.Equal(new DateTime(2026, 3, 1), march.LocalEndExclusive);
    }

    // MARK: Recap

    [Fact]
    public void EachScopeDrawsItsOwnMeters()
    {
        var ledger = Ledger(Key(2026, 1, 1));
        Assert.Equal(7, Make(ledger, RecapScope.Week, 0, [], Now).Buckets.Count);
        Assert.Equal(30, Make(ledger, RecapScope.Month, 0, [], Now).Buckets.Count);
        Assert.Equal(12, Make(ledger, RecapScope.Year, 0, [], Now).Buckets.Count);
    }

    [Fact]
    public void FutureAndUnrecordedDaysAreNotZeros()
    {
        var ledger = Ledger(Key(2026, 9, 21), (Key(2026, 9, 21), 5_000));
        var recap = Make(ledger, RecapScope.Week, 0, [], Now);

        var used = recap.Buckets.Single(bucket => bucket.Tokens == 5_000);
        Assert.True(used.HasData);

        // Wednesday has no usage but the ledger vouches for it: a real zero.
        var zero = recap.Buckets.Single(bucket => bucket.Key == Key(2026, 9, 23));
        Assert.True(zero.HasData);
        Assert.Equal(0, zero.Tokens);

        // Thursday is the future and Sunday, before the ledger started, is unknown.
        Assert.False(recap.Buckets.Single(bucket => bucket.Key == Key(2026, 9, 24)).HasData);
        Assert.False(recap.Buckets.Single(bucket => bucket.Key == Key(2026, 9, 20)).HasData);
        Assert.True(recap.Buckets.Single(bucket => bucket.Key == Key(2026, 9, 23)).IsCurrent);
    }

    [Fact]
    public void ARunningPeriodIsComparedOverTheSameDays()
    {
        var ledger = Ledger(Key(2026, 8, 1),
            (Key(2026, 9, 21), 600), // this week's Wednesday-so-far: Wed 21? no — see keys below
            (Key(2026, 9, 22), 400),
            (Key(2026, 9, 16), 100), // last week
            (Key(2026, 9, 17), 100),
            (Key(2026, 9, 18), 100));
        // This week (Sep 20-26) elapsed = Sun 20..Wed 23 = days 0..3; only 21,22 have usage → 1000.
        var recap = Make(ledger, RecapScope.Week, 0, [], Now);
        Assert.Equal(1_000, recap.Total);
        // Compared with the same 4 days last week (13,14,15,16): only 16 has usage → 100.
        Assert.Equal(100, recap.PreviousTotal);
        Assert.True(recap.IsInProgress);
    }

    [Fact]
    public void AFinishedPeriodIsComparedWhole()
    {
        var ledger = Ledger(Key(2026, 8, 1),
            (Key(2026, 9, 14), 300), (Key(2026, 9, 15), 300), // last week
            (Key(2026, 9, 8), 999));                            // the week before that
        var recap = Make(ledger, RecapScope.Week, -1, [], Now);
        Assert.Equal(600, recap.Total);
        // The finished week is compared with the whole week before it (Sep 6-12), not 2 days.
        Assert.Equal(999, recap.PreviousTotal);
        Assert.False(recap.IsInProgress);
    }

    [Fact]
    public void AHalfCoveredPreviousPeriodIsNotAComparison()
    {
        // Coverage starts inside the previous week, so its early days are unknown: no delta.
        var ledger = Ledger(Key(2026, 9, 16), (Key(2026, 9, 21), 100));
        var recap = Make(ledger, RecapScope.Week, 0, [], Now);
        Assert.Null(recap.PreviousTotal);
        Assert.Null(recap.Delta);
    }

    [Fact]
    public void TheBestStreakStaysInsideThePeriod()
    {
        var ledger = Ledger(Key(2026, 8, 1),
            (Key(2026, 9, 21), 10), (Key(2026, 9, 22), 10), (Key(2026, 9, 23), 10),
            (Key(2026, 9, 19), 10), (Key(2026, 9, 18), 10), (Key(2026, 9, 17), 10), (Key(2026, 9, 16), 10));
        var recap = Make(ledger, RecapScope.Week, 0, [], Now);
        Assert.Equal(3, recap.BestStreak); // not the 4-day run that crosses the period edge
    }

    [Fact]
    public void GraduationsInThePeriodOnly()
    {
        var inPeriod = new DexEntry(1, 3, [1, 2, 3], Rarity.Common,
            new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        var before = new DexEntry(25, 26, [25, 26], Rarity.Rare,
            new DateTimeOffset(2026, 9, 19, 10, 0, 0, TimeSpan.Zero));
        var released = new DexEntry(50, 50, [50], Rarity.Rare,
            new DateTimeOffset(2026, 9, 22, 11, 0, 0, TimeSpan.Zero), releasedAt: Now);
        var recap = Make(Ledger(Key(2026, 8, 1)), RecapScope.Week, 0, [inPeriod, before, released], Now);
        Assert.Equal([inPeriod.ID], recap.Graduated.Select(entry => entry.ID));
    }

    [Fact]
    public void GoingBackStopsWhereTheLedgerStarts()
    {
        var ledger = Ledger(Key(2026, 9, 1));
        Assert.True(Make(ledger, RecapScope.Week, -1, [], Now).CanGoBack);
        Assert.False(Make(ledger, RecapScope.Week, -4, [], Now).CanGoBack);
    }

    [Fact]
    public void AnEmptyLedgerStillDrawsThePeriod()
    {
        var recap = Make(new UsageLedger(), RecapScope.Month, 0, [], Now);
        Assert.Equal(30, recap.Buckets.Count);
        Assert.Equal(0, recap.Total);
        Assert.All(recap.Buckets, bucket => Assert.False(bucket.HasData));
        Assert.Null(recap.PreviousTotal);
        Assert.Equal(0, recap.CountedDays);
    }

    [Fact]
    public void BestDayAndCountedDaysReportElapsedDaysOnly()
    {
        var ledger = Ledger(Key(2026, 9, 21), (Key(2026, 9, 21), 5_000));
        var recap = Make(ledger, RecapScope.Week, 0, [], Now);
        Assert.Equal(Key(2026, 9, 21), recap.BestDay?.Key);
        Assert.Equal(5_000, recap.BestDay?.Tokens);
        // Sunday 20 (before coverage) is not counted; Mon-Wed are.
        Assert.Equal(3, recap.CountedDays);
        Assert.Equal(1, recap.ActiveDays);
    }

    [Fact]
    public void CurrentPeriodLabelsFollowTheScopeInEveryLanguage()
    {
        foreach (var lang in AppLanguages.All)
        {
            Assert.Equal(DashboardText.ThisWeekLabel(lang),
                DashboardText.RecapCurrentPeriod(lang, RecapScope.Week));
            Assert.Equal(DashboardText.ThisMonthLabel(lang),
                DashboardText.RecapCurrentPeriod(lang, RecapScope.Month));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapCurrentPeriod(lang, RecapScope.Year)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapOpen(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapPrevious(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapNext(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapBestDay(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapActiveDays(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapBestStreak(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapGraduates(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapNoGraduates(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapNoData(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapCompareSoFar(lang, RecapScope.Year)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RecapScopeName(lang, RecapScope.Month)));
        }
    }
}
