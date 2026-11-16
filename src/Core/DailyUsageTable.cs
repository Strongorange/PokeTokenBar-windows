using System.Globalization;

namespace PokeTokenBar.Core;

/// <summary>
/// Per-day usage totals with per-model token sums: everything the usage views read, without keeping the
/// individual entries. Cost is added entry by entry, so long-context pricing is applied per request.
/// </summary>
public sealed class DailyUsageTable
{
    private sealed class Day
    {
        public readonly UsageBucket Bucket = new();
        public readonly Dictionary<string, long> ModelTokens = new(StringComparer.Ordinal);
    }

    private readonly Dictionary<string, Day> _days = new(StringComparer.Ordinal);

    public static DailyUsageTable From(IEnumerable<UsageEntry> entries)
    {
        var table = new DailyUsageTable();
        foreach (var entry in entries) table.Add(entry);
        return table;
    }

    public int DayCount => _days.Count;

    public void Add(in UsageEntry entry)
    {
        if (!_days.TryGetValue(entry.LocalDay, out var day))
        {
            day = new Day();
            _days[entry.LocalDay] = day;
        }
        day.Bucket.Add(entry);
        day.ModelTokens[entry.Model] = day.ModelTokens.TryGetValue(entry.Model, out var sum) ? sum + entry.Total : entry.Total;
    }

    public void AddRange(DailyUsageTable other)
    {
        foreach (var (date, source) in other._days)
        {
            if (!_days.TryGetValue(date, out var day))
            {
                day = new Day();
                _days[date] = day;
            }
            day.Bucket.Merge(source.Bucket);
            foreach (var (model, tokens) in source.ModelTokens)
                day.ModelTokens[model] = day.ModelTokens.TryGetValue(model, out var sum) ? sum + tokens : tokens;
        }
    }

    public DailyUsage? Daily(string localDay, bool includeModels = false)
    {
        if (!_days.TryGetValue(localDay, out var day) || day.Bucket.Total <= 0) return null;
        var bucket = day.Bucket;
        return new DailyUsage(localDay, bucket.Input, bucket.Output, bucket.CacheWrite, bucket.CacheRead,
            bucket.Total, bucket.Cost, includeModels ? new Dictionary<string, long>(day.ModelTokens) : null,
            bucket.CostCoverage);
    }

    public PeriodUsage Period(string periodKey, string fromDay, string toDay)
    {
        var bucket = new UsageBucket();
        foreach (var (date, day) in _days)
        {
            if (string.CompareOrdinal(date, fromDay) >= 0 && string.CompareOrdinal(date, toDay) <= 0)
                bucket.Merge(day.Bucket);
        }
        return new PeriodUsage(periodKey, bucket.Total, bucket.Cost, bucket.CostCoverage);
    }

    public List<DailyUsage> MonthDailySeries(DateTimeOffset now, TimeZoneInfo? timeZone = null)
    {
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone ?? TimeZoneInfo.Local);
        return Series(new DateTime(localNow.Year, localNow.Month, 1), localNow.Date);
    }

    public List<DailyUsage> YearDailySeries(DateTimeOffset now, TimeZoneInfo? timeZone = null)
    {
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone ?? TimeZoneInfo.Local);
        return Series(UsageAggregation.YearGridStart(localNow.Date), localNow.Date);
    }

    private List<DailyUsage> Series(DateTime firstDay, DateTime lastDay)
    {
        var series = new List<DailyUsage>();
        for (var cursor = firstDay; cursor <= lastDay; cursor = cursor.AddDays(1))
        {
            var date = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            series.Add(_days.TryGetValue(date, out var day) ? day.Bucket.ToDaily(date) : new UsageBucket().ToDaily(date));
        }
        return series;
    }
}
