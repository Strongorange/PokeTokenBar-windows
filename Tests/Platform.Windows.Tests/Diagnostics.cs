using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows.Tests;

[CollectionDefinition("Platform diagnostics")]
public sealed class PlatformDiagnosticsCollectionDefinition
{
}

internal static class Diagnostics
{
    public static void Configure(string directory) =>
        AppLog.Configure(Path.Combine(directory, "diag.log"));

    public static string Read(string directory)
    {
        AppLog.Flush();
        var path = Path.Combine(directory, "diag.log");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
