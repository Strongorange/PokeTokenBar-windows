namespace PokeTokenBar.Core;

public enum RelativeTimeBucket
{
    None,
    Minutes,
    Hours,
    Days
}

/// <summary>
/// Coarse relative-time buckets for the catch log's caught-at line (macOS uses
/// SwiftUI's .relative date style; Windows resolves the bucket here and formats
/// via DashboardText). Pure and clock-free for testability.
/// </summary>
public static class RelativeTimes
{
    public static RelativeTimeBucket Bucket(DateTimeOffset? caughtAt, DateTimeOffset now)
    {
        if (caughtAt is not { } at) return RelativeTimeBucket.None;
        var span = now - at;
        if (span <= TimeSpan.Zero) return RelativeTimeBucket.Minutes;
        if (span < TimeSpan.FromHours(1)) return RelativeTimeBucket.Minutes;
        if (span < TimeSpan.FromHours(24)) return RelativeTimeBucket.Hours;
        return RelativeTimeBucket.Days;
    }

    public static int BucketValue(DateTimeOffset caughtAt, DateTimeOffset now)
    {
        var span = now - caughtAt;
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        if (span < TimeSpan.FromHours(1)) return Math.Max(0, (int)Math.Floor(span.TotalMinutes));
        if (span < TimeSpan.FromHours(24)) return (int)Math.Floor(span.TotalHours);
        return (int)Math.Floor(span.TotalDays);
    }
}
