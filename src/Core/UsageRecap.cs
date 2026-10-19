using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokeTokenBar.Core;

/// <summary>
/// Day-by-day token totals kept across months, so a recap survives the 1st of the month and
/// beyond the raw-log scan window. Port of macOS UsageLedger (#332): a local cache of numbers
/// the app already derived — nothing to transfer between devices, and it rebuilds itself from
/// the next refresh.
/// </summary>
public sealed class UsageLedger
{
    public Dictionary<string, long> TokensByDay { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The first day the ledger vouches for. From there on a missing row means "no usage";
    /// before it the app was not recording, which a recap must not present as a quiet stretch.
    /// </summary>
    public string? CoveredSince { get; set; }

    public long Tokens(string day) => TokensByDay.TryGetValue(day, out var tokens) ? tokens : 0;

    public bool Covers(string day) =>
        CoveredSince is { } since && string.CompareOrdinal(day, since) >= 0;

    /// <summary>
    /// Records a refresh. A day never goes down: a rotated or pruned log makes the provider
    /// report less than it did yesterday, and the recap would lose days that really happened.
    /// Today keeps climbing all day, so taking the larger of the two is also what the live
    /// total does.
    /// </summary>
    public void Merge(IReadOnlyList<DailyUsage> series)
    {
        var earliest = (string?)null;
        foreach (var day in series)
        {
            if (day.TotalTokens > 0)
                TokensByDay[day.Date] = Math.Max(Tokens(day.Date), day.TotalTokens);
            if (earliest is null || string.CompareOrdinal(day.Date, earliest) < 0)
                earliest = day.Date;
        }
        // The series lists every day of its window, empty ones included, so the whole stretch
        // was scanned: that is what makes a zero a real zero.
        if (earliest is not null && (CoveredSince is null || string.CompareOrdinal(earliest, CoveredSince) < 0))
            CoveredSince = earliest;
    }

    /// <summary>
    /// Drops what no recap can reach any more. Day keys are yyyy-MM-dd, which sorts
    /// lexicographically the same way it sorts chronologically.
    /// </summary>
    public void Prune(string cutoff)
    {
        TokensByDay = TokensByDay
            .Where(pair => string.CompareOrdinal(pair.Key, cutoff) >= 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        // The dropped days are unknown again, not empty.
        if (CoveredSince is { } since && string.CompareOrdinal(since, cutoff) < 0)
            CoveredSince = cutoff;
    }

    public JsonObject Write() => new()
    {
        ["coveredSince"] = CoveredSince,
        ["tokensByDay"] = new JsonObject(TokensByDay.Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .Select(key => KeyValuePair.Create<string, JsonNode?>(key, TokensByDay[key]))),
    };

    /// <summary>
    /// Hand-edited files are data, not truth: keep what parses as a day and drop the rest.
    /// A junk key that sorted below every real day would otherwise backdate the coverage.
    /// </summary>
    public static UsageLedger Read(JsonElement element)
    {
        var ledger = new UsageLedger();
        if (element.ValueKind != JsonValueKind.Object) return ledger;
        if (element.TryGetProperty("tokensByDay", out var days) && days.ValueKind == JsonValueKind.Object)
        {
            foreach (var day in days.EnumerateObject())
            {
                if (day.Value.ValueKind != JsonValueKind.Number) continue;
                if (!day.Value.TryGetInt64(out var tokens) || tokens <= 0) continue;
                if (ParseDay(day.Name) is null) continue;
                ledger.TokensByDay[day.Name] = tokens;
            }
        }
        if (element.TryGetProperty("coveredSince", out var since)
            && since.ValueKind == JsonValueKind.String
            && ParseDay(since.GetString()) is not null)
        {
            ledger.CoveredSince = since.GetString();
        }
        return ledger;
    }

    public static DateTimeOffset? ParseDay(string? raw) =>
        DateTimeOffset.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
}

public enum RecapScope
{
    Week,
    Month,
    Year
}

/// <summary>
/// One calendar week, month or year. Calendar periods rather than rolling windows, so stepping
/// back lands on "the week of the 7th", not on an arbitrary seven days. Weeks start on Sunday —
/// the same rule as the week totals and the year heatmap grid (UsageAggregation.StartOfWeek
/// with InvariantCulture); if that ever becomes locale-aware, all three must switch together.
/// </summary>
public sealed record RecapPeriod(RecapScope Scope, DateTime LocalStart, DateTime LocalEndExclusive)
{
    /// <summary>The one week-start rule shared by the week totals, the heatmap grid and recaps.</summary>
    public static CultureInfo WeekStartCulture => CultureInfo.InvariantCulture;

    public static RecapPeriod Containing(RecapScope scope, DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(instant, timeZone).Date;
        return scope switch
        {
            RecapScope.Week => new RecapPeriod(scope,
                UsageAggregation.StartOfWeek(local, WeekStartCulture),
                UsageAggregation.StartOfWeek(local, WeekStartCulture).AddDays(7)),
            RecapScope.Month => new RecapPeriod(scope,
                new DateTime(local.Year, local.Month, 1),
                new DateTime(local.Year, local.Month, 1).AddMonths(1)),
            RecapScope.Year => new RecapPeriod(scope,
                new DateTime(local.Year, 1, 1),
                new DateTime(local.Year + 1, 1, 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };
    }

    public RecapPeriod Shifted(int offset) => Scope switch
    {
        RecapScope.Week => new RecapPeriod(Scope, LocalStart.AddDays(7 * offset), LocalEndExclusive.AddDays(7 * offset)),
        RecapScope.Month => this with
        {
            LocalStart = LocalStart.AddMonths(offset),
            LocalEndExclusive = LocalStart.AddMonths(offset + 1)
        },
        RecapScope.Year => new RecapPeriod(Scope, LocalStart.AddYears(offset), LocalEndExclusive.AddYears(offset)),
        _ => this,
    };

    public DateTimeOffset StartInstant(TimeZoneInfo timeZone) =>
        MidnightInstant(LocalStart, timeZone);

    public DateTimeOffset EndInstant(TimeZoneInfo timeZone) =>
        MidnightInstant(LocalEndExclusive, timeZone);

    private static DateTimeOffset MidnightInstant(DateTime localDate, TimeZoneInfo timeZone) =>
        new(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0,
            timeZone.GetUtcOffset(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified)));

    /// <summary>Every local date of the period, start inclusive, end exclusive.</summary>
    public List<DateTime> LocalDays()
    {
        var days = new List<DateTime>();
        for (var cursor = LocalStart; cursor < LocalEndExclusive; cursor = cursor.AddDays(1))
            days.Add(cursor);
        return days;
    }

    public static string DayKey(DateTime localDate) =>
        localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed record RecapBucket(string Key, long Tokens, bool IsCurrent, bool HasData);

public sealed record RecapDay(string Key, long Tokens);

/// <summary>
/// A recap of one period — read-only, computed on demand (macOS UsageRecap.Make). Nothing here
/// is a reward or a goal: it reports what already happened.
/// </summary>
public sealed record UsageRecap(
    RecapPeriod Period,
    IReadOnlyList<RecapBucket> Buckets,
    long Total,
    bool IsInProgress,
    long? PreviousTotal,
    RecapDay? BestDay,
    int ActiveDays,
    int CountedDays,
    int BestStreak,
    IReadOnlyList<DexEntry> Graduated,
    bool CanGoBack)
{
    /// <summary>Change against the previous period, null when there is no honest comparison.</summary>
    public double? Delta
    {
        get
        {
            if (PreviousTotal is not { } previous || previous <= 0) return null;
            return (double)(Total - previous) / previous;
        }
    }

    public static UsageRecap Make(
        UsageLedger ledger, IReadOnlyList<DexEntry> dex, RecapPeriod period, DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var today = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        bool Known(string key) => ledger.Tokens(key) > 0 || ledger.Covers(key);

        var periodDays = period.LocalDays();
        var elapsed = periodDays
            .Where(day => day <= today)
            .Select(day => new RecapDay(RecapPeriod.DayKey(day), ledger.Tokens(RecapPeriod.DayKey(day))))
            .ToList();

        var bestStreak = 0;
        var run = 0;
        foreach (var day in elapsed)
        {
            run = day.Tokens > 0 ? run + 1 : 0;
            bestStreak = Math.Max(bestStreak, run);
        }

        List<RecapBucket> buckets;
        if (period.Scope == RecapScope.Year)
        {
            buckets = [];
            for (var month = 0; month < 12; month++)
            {
                var start = new DateTime(period.LocalStart.Year, 1, 1).AddMonths(month);
                var end = start.AddMonths(1);
                var keys = periodDays
                    .Where(day => day >= start && day < end && day <= today)
                    .Select(RecapPeriod.DayKey)
                    .ToList();
                buckets.Add(new RecapBucket(
                    RecapPeriod.DayKey(start),
                    keys.Sum(key => ledger.Tokens(key)),
                    start <= today && today < end,
                    keys.Any(Known)));
            }
        }
        else
        {
            buckets = periodDays
                .Select(day => new RecapBucket(
                    RecapPeriod.DayKey(day),
                    ledger.Tokens(RecapPeriod.DayKey(day)),
                    day == today,
                    day <= today && Known(RecapPeriod.DayKey(day))))
                .ToList();
        }

        var isInProgress = period.LocalStart <= today && today < period.LocalEndExclusive;
        var previous = period.Shifted(-1);
        var previousDays = previous.LocalDays().Select(RecapPeriod.DayKey).ToList();
        // A running period is compared over the same number of days, or the 18th of the month
        // would read as a 40% drop against a whole month.
        var compared = isInProgress ? previousDays.Take(elapsed.Count).ToList() : previousDays;
        long? previousTotal = compared.Count > 0 && ledger.Covers(compared[0])
            ? compared.Sum(key => ledger.Tokens(key))
            : null;

        var graduated = dex
            .Where(entry => !entry.IsReleased
                && entry.CaughtAt is { } caughtAt
                && caughtAt >= period.StartInstant(timeZone)
                && caughtAt < period.EndInstant(timeZone))
            .OrderByDescending(entry => entry.CaughtAt ?? DateTimeOffset.MinValue)
            .ToList();

        return new UsageRecap(
            period,
            buckets,
            elapsed.Sum(day => day.Tokens),
            isInProgress,
            previousTotal,
            elapsed.Where(day => day.Tokens > 0).MaxBy(day => day.Tokens),
            elapsed.Count(day => day.Tokens > 0),
            elapsed.Count(day => Known(day.Key)),
            bestStreak,
            graduated,
            previousDays.Count > 0 && ledger.Covers(previousDays[^1]));
    }
}
