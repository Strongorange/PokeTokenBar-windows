namespace PokeTokenBar.Platform.Windows;

public sealed record ScanRootEntry(string Provider, string Path);

public static class ScanRootSettings
{
    public const string ClaudeProvider = "claude";
    public const string CodexProvider = "codex";
    public const string OpenCodeProvider = "opencode";

    public static readonly string[] Providers =
        [ClaudeProvider, CodexProvider, OpenCodeProvider];

    public static string? NormalizeProvider(string? raw) =>
        raw is null ? null : raw.Trim().ToLowerInvariant() switch
        {
            ClaudeProvider => ClaudeProvider,
            CodexProvider => CodexProvider,
            OpenCodeProvider => OpenCodeProvider,
            _ => null,
        };

    public static void Apply(UsageRootOptions options, IEnumerable<ScanRootEntry> entries)
    {
        var claude = new List<string>();
        var codex = new List<string>();
        var opencode = new List<string>();
        foreach (var entry in entries)
        {
            var provider = NormalizeProvider(entry.Provider);
            var path = entry.Path?.Trim();
            if (provider is null || string.IsNullOrEmpty(path)) continue;
            var bucket = provider switch
            {
                ClaudeProvider => claude,
                CodexProvider => codex,
                _ => opencode,
            };
            if (!bucket.Contains(path, StringComparer.OrdinalIgnoreCase)) bucket.Add(path);
        }
        options.ExtraClaudeRoots = claude;
        options.ExtraCodexRoots = codex;
        options.ExtraOpenCodeRoots = opencode;
    }
}
