namespace PokeTokenBar.Platform.Windows;

public static class PathNormalizer
{
    private const string WslDollarPrefix = @"\\wsl$";
    private const string WslLocalhostPrefix = @"\\wsl.localhost";

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string expanded;
        try
        {
            expanded = Environment.ExpandEnvironmentVariables(raw.Trim());
        }
        catch
        {
            return null;
        }
        if (expanded.Length == 0) return null;
        try
        {
            var full = Path.GetFullPath(expanded);
            return TrimTrailingSeparators(CanonicalizeWslServer(full));
        }
        catch
        {
            return null;
        }
    }

    private static string CanonicalizeWslServer(string path)
    {
        if (!path.StartsWith(WslDollarPrefix, StringComparison.OrdinalIgnoreCase)) return path;
        var rest = path.Length > WslDollarPrefix.Length
            ? @"\" + path[WslDollarPrefix.Length..].TrimStart('\\')
            : string.Empty;
        return WslLocalhostPrefix + rest;
    }

    private static string TrimTrailingSeparators(string path)
    {
        if (path.Length <= 3) return path;
        return path.TrimEnd('\\', '/');
    }
}
