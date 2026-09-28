using System.Globalization;

namespace PokeTokenBar.Core;

public static class DailyTrendMetrics
{
    public const double Track = 26;
    public const double Spacing = 1.5;
    public const double Baseline = 1.5;
    public const double TickHeight = 1.5;

    public static double BarHeight(long tokens, long peak)
    {
        if (peak <= 0 || tokens <= 0) return Baseline;
        var ratio = Math.Min(1.0, (double)tokens / peak);
        return Math.Max(Baseline, ratio * Track);
    }

    public static string? AxisLabel(string date, string today, int labelInterval = 7,
        int minimumSeparation = 3)
    {
        if (!TryDayOfMonth(date, out var dayOfMonth)) return null;
        if (date == today) return dayOfMonth.ToString(CultureInfo.InvariantCulture);
        var isRegular = dayOfMonth == 1 || (labelInterval > 0 && dayOfMonth % labelInterval == 0);
        if (!isRegular) return null;
        if (TryDayOfMonth(today, out var todayOfMonth) &&
            Math.Abs(dayOfMonth - todayOfMonth) < minimumSeparation) return null;
        return dayOfMonth.ToString(CultureInfo.InvariantCulture);
    }

    public static bool IsWeekend(string date)
    {
        if (!TryParseDay(date, out var day)) return false;
        return day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    public static string DayStamp(string date, AppLanguage language)
    {
        if (!TryParseDay(date, out var day)) return "";
        return day.ToString(StampPattern(language), CultureInfo.GetCultureInfo(CultureName(language)));
    }

    private static string StampPattern(AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => "M. d. (ddd)",
        AppLanguage.Ja => "M/d(ddd)",
        AppLanguage.Es => "ddd dd/MM",
        AppLanguage.Fr => "ddd dd/MM",
        AppLanguage.Pt => "ddd, dd/MM",
        AppLanguage.De => "ddd., d.M.",
        _ => "ddd, M/d"
    };

    private static string CultureName(AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => "ko-KR",
        AppLanguage.Ja => "ja-JP",
        AppLanguage.Es => "es-ES",
        AppLanguage.Fr => "fr-FR",
        AppLanguage.Pt => "pt-BR",
        AppLanguage.De => "de-DE",
        _ => "en-US"
    };

    private static bool TryParseDay(string date, out DateTime day) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out day);

    private static bool TryDayOfMonth(string date, out int dayOfMonth)
    {
        dayOfMonth = 0;
        return date.Length == 10 &&
               int.TryParse(date[^2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out dayOfMonth);
    }
}
