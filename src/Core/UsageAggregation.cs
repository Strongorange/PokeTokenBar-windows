using System.Globalization;

namespace PokeTokenBar.Core;

public readonly record struct UsageEntry(
    string Id,
    DateTimeOffset Date,
    string LocalDay,
    string Model,
    long Input,
    long Output,
    long CacheWrite,
    long CacheRead,
    double? ExplicitCost = null,
    bool? CostIsEstimate = null,
    bool? CostUnavailable = null)
{
    public long Total => Input + Output + CacheWrite + CacheRead;
}

public class UsageBucket
{
    public long Input, Output, CacheWrite, CacheRead;
    public double Cost;
    public CostCoverage CostCoverage = CostCoverage.Empty;

    public long Total => Input + Output + CacheWrite + CacheRead;

    public void Add(UsageEntry e)
    {
        Input += e.Input;
        Output += e.Output;
        CacheWrite += e.CacheWrite;
        CacheRead += e.CacheRead;
        if (e.Total <= 0) return;
        if (e.ExplicitCost is { } reported && double.IsFinite(reported) && reported >= 0)
        {
            Cost += reported;
            CostCoverage = CostCoverage.Merge(e.CostIsEstimate == true ? CostCoverage.Estimate : CostCoverage.Source);
        }
        else if (e.CostUnavailable != true)
        {
            var estimate = ModelPricing.EstimatedCost(e.Model, e.Input, e.Output, e.CacheWrite, e.CacheRead);
            if (estimate is { } value)
            {
                Cost += value;
                CostCoverage = CostCoverage.Merge(CostCoverage.Estimate);
                return;
            }
            CostCoverage = CostCoverage.Merge(CostCoverage.Unavailable);
        }
        else
        {
            CostCoverage = CostCoverage.Merge(CostCoverage.Unavailable);
        }
    }

    public void Merge(UsageBucket other)
    {
        Input += other.Input;
        Output += other.Output;
        CacheWrite += other.CacheWrite;
        CacheRead += other.CacheRead;
        Cost += other.Cost;
        CostCoverage = CostCoverage.Merge(other.CostCoverage);
    }

    public DailyUsage ToDaily(string date) =>
        new(date, Input, Output, CacheWrite, CacheRead, Total, Cost, costCoverage: CostCoverage);
}

public static class UsageAggregation
{
    public static readonly long MaxParsedTokenValue = 1_000_000_000_000_000;
    public static readonly TimeSpan BlockWindow = TimeSpan.FromHours(5);

    public static List<UsageEntry> DedupKeepMax(IEnumerable<UsageEntry> entries)
    {
        var byId = new Dictionary<string, UsageEntry>();
        foreach (var e in entries)
        {
            if (byId.TryGetValue(e.Id, out var existing))
            {
                if (e.Total > existing.Total) byId[e.Id] = e;
            }
            else
            {
                byId[e.Id] = e;
            }
        }
        return byId.Values.ToList();
    }

    public static DailyUsage? Daily(IEnumerable<UsageEntry> entries, string localDay, bool includeModels = false) =>
        DailyUsageTable.From(entries.Where(e => e.LocalDay == localDay)).Daily(localDay, includeModels);

    public static PeriodUsage Period(IEnumerable<UsageEntry> entries, string periodKey, string fromDay, string toDay)
    {
        var bucket = new UsageBucket();
        foreach (var e in entries)
        {
            if (string.CompareOrdinal(e.LocalDay, fromDay) >= 0 && string.CompareOrdinal(e.LocalDay, toDay) <= 0)
                bucket.Add(e);
        }
        return new PeriodUsage(periodKey, bucket.Total, bucket.Cost, bucket.CostCoverage);
    }

    public static List<DailyUsage> MonthDailySeries(IEnumerable<UsageEntry> entries, DateTimeOffset now,
        TimeZoneInfo? timeZone = null) =>
        DailyUsageTable.From(entries).MonthDailySeries(now, timeZone);

    // 잔디 히트맵(1년) 창. 깃허브처럼 53주이고, 오늘-364일이 속한 주의 일요일부터 그려 열을 주 단위로 맞춤
    public static readonly TimeSpan HeatmapWindow = TimeSpan.FromDays(365);

    public static DateTime YearGridStart(DateTime localToday) =>
        StartOfWeek(localToday.AddDays(-364), CultureInfo.InvariantCulture);

    public static List<DailyUsage> YearDailySeries(IEnumerable<UsageEntry> entries, DateTimeOffset now,
        TimeZoneInfo? timeZone = null) =>
        DailyUsageTable.From(entries).YearDailySeries(now, timeZone);

    public static BlockUsage? ActiveBlock(IEnumerable<UsageEntry> entries, DateTimeOffset now)
    {
        var windowStart = now - BlockWindow;
        var recent = entries
            .Where(e => e.Date >= windowStart && e.Total > 0)
            .OrderBy(e => e.Date)
            .ToList();
        if (recent.Count == 0) return null;
        var bucket = new UsageBucket();
        foreach (var e in recent) bucket.Add(e);
        var minutes = Math.Max(1, (now - recent[0].Date).TotalMinutes);
        var tpm = bucket.Total / minutes;
        var first = recent[0].Date;
        return new BlockUsage(
            $"block-{first.ToUnixTimeSeconds()}",
            IsoDates.Format(first),
            IsoDates.Format(first + BlockWindow),
            true, bucket.Total, bucket.Cost, tpm, bucket.CostCoverage);
    }

    public static string LocalDay(DateTimeOffset instant, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        return TimeZoneInfo.ConvertTime(instant, tz)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string MonthKey(DateTimeOffset instant, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        return TimeZoneInfo.ConvertTime(instant, tz)
            .ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    public static string TodayKey(TimeZoneInfo? timeZone = null) =>
        LocalDay(DateTimeOffset.UtcNow, timeZone);

    public static DateTime StartOfMonth(DateTime localDate) =>
        new(localDate.Year, localDate.Month, 1);

    public static DateTime StartOfWeek(DateTime localDate, CultureInfo? culture = null)
    {
        var dtf = (culture ?? CultureInfo.CurrentCulture).DateTimeFormat;
        var diff = ((int)localDate.DayOfWeek - (int)dtf.FirstDayOfWeek + 7) % 7;
        return localDate.Date.AddDays(-diff);
    }

    public static DateTimeOffset EnrichmentScanStart(DateTimeOffset now, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var candidates = new[]
        {
            new DateTimeOffset(StartOfMonth(localNow.Date), tz.GetUtcOffset(localNow)).ToUniversalTime(),
            new DateTimeOffset(StartOfWeek(localNow.Date), tz.GetUtcOffset(localNow)).ToUniversalTime(),
            now - BlockWindow,
        };
        return candidates.Min();
    }

    /// 프로바이더 스캔 하한 — 히트맵 1년 창을 포함해 가장 오래된 후보.
    /// 기존 EnrichmentScanStart 는 그대로 두고(테스트·의존 파이프라인 고정),
    /// 1년 잔디가 필요해진 스캔 경로만 이쪽으로 옮긴다.
    public static DateTimeOffset HeatmapScanStart(DateTimeOffset now, TimeZoneInfo? timeZone = null)
    {
        var candidates = new[]
        {
            EnrichmentScanStart(now, timeZone),
            now - HeatmapWindow,
        };
        return candidates.Min();
    }
}
