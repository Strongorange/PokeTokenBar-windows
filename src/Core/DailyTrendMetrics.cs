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

    // 잔디 히트맵(깃허브 기여 그래프 결) — 막대가 '추세'를 보여주면 히트맵은 '요일 패턴'을 보여준다.
    // 레벨 컷은 막대와 같은 선형 스케일(peak 기준 비율 4분할) — 두 렌더가 다른 이야기를 하면 안 된다.
    public const int HeatMaxLevel = 4;

    public static int HeatLevel(long tokens, long peak)
    {
        if (peak <= 0 || tokens <= 0) return 0;
        var ratio = (double)tokens / peak;
        if (ratio <= 0.25) return 1;
        if (ratio <= 0.5) return 2;
        if (ratio <= 0.75) return 3;
        return HeatMaxLevel;
    }

    /// 히트맵 행 = 요일(일요일이 0행 — 깃허브와 같은 일 시작 주).
    public static int HeatRow(string date) =>
        TryParseDay(date, out var day) ? (int)day.DayOfWeek : -1;

    /// 히트맵 열 = 주차. **첫날부터의 날짜 차이**로 계산한다 — day-of-month 기준식은
    /// 같은 달 안에서만 성립하고, 연간 그리드처럼 달을 넘으면 매달 1~31일이 같은
    /// 열로 접히는 참사가 난다(월간뷰 시절의 잠복 버그).
    public static int HeatColumn(string date, string firstDate)
    {
        if (!TryParseDay(date, out var day) || !TryParseDay(firstDate, out var first))
            return -1;
        var diff = (day.Date - first.Date).Days;
        return diff < 0 ? -1 : ((int)first.DayOfWeek + diff) / 7;
    }

    public static long Peak(IReadOnlyList<DailyUsage>? series)
    {
        long peak = 0;
        if (series is not null)
            foreach (var day in series)
                if (day.TotalTokens > peak) peak = day.TotalTokens;
        return peak;
    }

    public static bool ShowsCost(IReadOnlyList<DailyUsage>? series)
    {
        if (series is null) return false;
        foreach (var day in series)
            if (day.UsageCost.Coverage.HasKnown) return true;
        return false;
    }

    public static DailyUsage? TodayUsage(IReadOnlyList<DailyUsage>? series, string todayKey)
    {
        if (series is null) return null;
        foreach (var day in series)
            if (day.Date == todayKey) return day;
        return null;
    }

    public static string DayStamp(string date, AppLanguage language)
    {
        if (!TryParseDay(date, out var day)) return "";
        return day.ToString(StampPattern(language), CultureInfo.GetCultureInfo(CultureName(language)));
    }

    /// 히트맵 월 축 라벨 — 로케일 축약 월명(10월/Oct), de/es 처럼 마침표로 끝나면 뗀다.
    public static string MonthLabel(string date, AppLanguage language)
    {
        if (!TryParseDay(date, out var day)) return "";
        var name = CultureInfo.GetCultureInfo(CultureName(language))
            .DateTimeFormat.AbbreviatedMonthNames[day.Month - 1];
        return name.TrimEnd('.');
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
