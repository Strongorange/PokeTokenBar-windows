namespace PokeTokenBar.Core;

/// <summary>
/// Dot-separated numeric version comparison for release tags, ported from the
/// macOS UpdateChecker (`isNewer`). Segments are compared numerically with
/// missing segments counting as zero; non-numeric segments count as zero.
/// A single leading "v"/"V" tag prefix is stripped before comparing.
/// </summary>
public static class VersionText
{
    public static bool IsNewer(string candidate, string baseline)
    {
        var a = Segments(candidate);
        var b = Segments(baseline);
        var count = Math.Max(a.Length, b.Length);
        for (var i = 0; i < count; i++)
        {
            var x = i < a.Length ? a[i] : 0;
            var y = i < b.Length ? b[i] : 0;
            if (x != y) return x > y;
        }
        return false;
    }

    public static string Normalize(string version)
    {
        var trimmed = version.Trim();
        return trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V')
            ? trimmed[1..]
            : trimmed;
    }

    private static int[] Segments(string version) =>
        Normalize(version)
            .Split('.')
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToArray();
}
