using System.Globalization;

namespace PokeTokenBar.Core;

public static class TokenFormatter
{
    public static string Compact(long value)
    {
        var v = Math.Abs((double)value);
        var sign = value < 0 ? "-" : "";
        if (v < 1_000) return value.ToString(CultureInfo.InvariantCulture);
        if (v < 1_000_000) return sign + Trim(v / 1_000, 1) + "K";
        if (v < 1_000_000_000) return sign + Trim(v / 1_000_000, 1) + "M";
        return sign + Trim(v / 1_000_000_000, 2) + "B";
    }

    public static string Grouped(long value, CultureInfo? culture = null) =>
        value.ToString("N0", culture ?? CultureInfo.CurrentCulture);

    public static string Cost(double usd) =>
        string.Create(CultureInfo.InvariantCulture, $"${usd:F2}");

    public static string CostCompact(double usd)
    {
        if (usd < 100) return string.Create(CultureInfo.InvariantCulture, $"${usd:F1}");
        if (usd < 10_000) return string.Create(CultureInfo.InvariantCulture, $"${usd:F0}");
        return string.Create(CultureInfo.InvariantCulture, $"${usd / 1_000:F1}K");
    }

    public static string Percent(double value) =>
        value == Math.Round(value)
            ? string.Create(CultureInfo.InvariantCulture, $"{value:F0}%")
            : string.Create(CultureInfo.InvariantCulture, $"{value:F1}%");

    private static string Trim(double value, int decimals)
    {
        var s = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        return s.TrimEnd('0').TrimEnd('.');
    }
}
