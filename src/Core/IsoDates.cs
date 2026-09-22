using System.Globalization;

namespace PokeTokenBar.Core;

public static class IsoDates
{
    private const string FractionalFormat = "yyyy-MM-dd'T'HH:mm:ss.fffK";
    private const string PlainFormat = "yyyy-MM-dd'T'HH:mm:ssK";

    public static DateTimeOffset? Date(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTimeOffset.TryParseExact(s, FractionalFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var withFraction))
            return withFraction;

        var dot = s.IndexOf('.');
        if (dot >= 0)
        {
            var afterDot = dot + 1;
            var tzIndex = -1;
            for (var i = afterDot; i < s.Length; i++)
            {
                if (s[i] is '+' or '-' or 'Z' or 'z') { tzIndex = i; break; }
            }
            if (tzIndex > afterDot)
            {
                var fracLength = Math.Min(3, tzIndex - afterDot);
                var frac = s.Substring(afterDot, fracLength).PadRight(3, '0');
                var rebuilt = s[..dot] + "." + frac + s[tzIndex..];
                if (DateTimeOffset.TryParseExact(rebuilt, FractionalFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var rebuiltDate))
                    return rebuiltDate;
            }
        }

        if (DateTimeOffset.TryParseExact(s, PlainFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var plain))
            return plain;
        return null;
    }

    public static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
