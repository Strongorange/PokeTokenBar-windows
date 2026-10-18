using System.Globalization;

namespace PokeTokenBar.Core;

public static class TokenFormatter
{
    public static string Compact(long value)
    {
        var v = Math.Abs((double)value);
        var sign = value < 0 ? "-" : "";
        if (v < 1_000) return value.ToString(CultureInfo.InvariantCulture);
        // K and M use one decimal: promote when rounding would display 1000.0.
        if (v < 999_950) return sign + Trim(v / 1_000, 1) + "K";
        if (v < 999_950_000) return sign + Trim(v / 1_000_000, 1) + "M";
        return sign + Trim(v / 1_000_000_000, 2) + "B";
    }

    public static string Grouped(long value, CultureInfo? culture = null) =>
        value.ToString("N0", culture ?? CultureInfo.CurrentCulture);

    public static string Cost(double usd) =>
        string.Create(CultureInfo.InvariantCulture, $"${usd:F2}");

    /// <summary>
    /// Menu-bar-short cost: $9.5 / $311 / $1.2K. The tier is picked from the <em>rounded</em>
    /// string, not the raw value — 99.96 shows "$100", not "$100.0".
    /// </summary>
    public static string CostCompact(double usd)
    {
        var tenths = string.Create(CultureInfo.InvariantCulture, $"{usd:F1}");
        if (double.TryParse(tenths, NumberStyles.Float, CultureInfo.InvariantCulture, out var v1)
            && v1 < 100)
            return $"${tenths}";
        var whole = string.Create(CultureInfo.InvariantCulture, $"{usd:F0}");
        if (double.TryParse(whole, NumberStyles.Float, CultureInfo.InvariantCulture, out var v0)
            && v0 < 10_000)
            return $"${whole}";
        return string.Create(CultureInfo.InvariantCulture, $"${usd / 1_000:F1}K");
    }

    /// <summary>79.96 → "80%" (not "80.0%"), 88.35 → "88.3%".</summary>
    public static string Percent(double value)
    {
        var tenths = string.Create(CultureInfo.InvariantCulture, $"{value:F1}");
        return (tenths.EndsWith(".0", StringComparison.Ordinal) ? tenths[..^2] : tenths) + "%";
    }

    private static string Trim(double value, int decimals)
    {
        var s = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        return s.TrimEnd('0').TrimEnd('.');
    }
}
