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

    public static DailyUsage? Daily(IEnumerable<UsageEntry> entries, string localDay, bool includeModels = false)
    {
        var bucket = new UsageBucket();
        Dictionary<string, long>? models = includeModels ? [] : null;
        foreach (var e in entries)
        {
            if (e.LocalDay != localDay) continue;
            bucket.Add(e);
            if (includeModels)
            {
                models![e.Model] = models.TryGetValue(e.Model, out var sum) ? sum + e.Total : e.Total;
            }
        }
        if (bucket.Total <= 0) return null;
        return new DailyUsage(localDay, bucket.Input, bucket.Output, bucket.CacheWrite, bucket.CacheRead,
            bucket.Total, bucket.Cost, models, bucket.CostCoverage);
    }

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
        TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var monthStart = new DateTime(localNow.Year, localNow.Month, 1);
        var lastDay = localNow.Date;

        var days = new List<string>();
        var cursor = monthStart;
        while (cursor <= lastDay)
        {
            days.Add(cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cursor = cursor.AddDays(1);
        }

        var inMonth = days.ToHashSet();
        var buckets = new Dictionary<string, UsageBucket>();
        foreach (var e in entries)
        {
            if (!inMonth.Contains(e.LocalDay)) continue;
            if (!buckets.TryGetValue(e.LocalDay, out var bucket))
            {
                bucket = new UsageBucket();
                buckets[e.LocalDay] = bucket;
            }
            bucket.Add(e);
        }

        return days.Select(day => buckets.TryGetValue(day, out var b)
            ? b.ToDaily(day)
            : new UsageBucket().ToDaily(day)).ToList();
    }

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
}
