using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Platform.Windows.Tests;

public class ScanRootSettingsTests
{
    [Fact]
    public void ApplyRoutesEntriesToProviderBuckets()
    {
        var options = new UsageRootOptions();

        ScanRootSettings.Apply(options,
        [
            new ScanRootEntry("claude", @"D:\logs\a"),
            new ScanRootEntry("codex", @"D:\logs\b"),
            new ScanRootEntry("opencode", @"D:\logs\c"),
        ]);

        Assert.Equal([@"D:\logs\a"], options.ExtraClaudeRoots);
        Assert.Equal([@"D:\logs\b"], options.ExtraCodexRoots);
        Assert.Equal([@"D:\logs\c"], options.ExtraOpenCodeRoots);
    }

    [Fact]
    public void ApplyIgnoresUnknownProvidersBlankPathsAndDuplicates()
    {
        var options = new UsageRootOptions();

        ScanRootSettings.Apply(options,
        [
            new ScanRootEntry("claude", @"D:\logs\a"),
            new ScanRootEntry("CLAUDE", @"d:\LOGS\A"),
            new ScanRootEntry("nope", @"D:\logs\x"),
            new ScanRootEntry("codex", "  "),
        ]);

        var claude = Assert.Single(options.ExtraClaudeRoots);
        Assert.Equal(@"D:\logs\a", claude);
        Assert.Empty(options.ExtraCodexRoots);
        Assert.Empty(options.ExtraOpenCodeRoots);
    }

    [Fact]
    public void NormalizeProviderLowercasesAndValidates()
    {
        Assert.Equal("claude", ScanRootSettings.NormalizeProvider(" Claude "));
        Assert.Equal("codex", ScanRootSettings.NormalizeProvider("CODEX"));
        Assert.Equal("opencode", ScanRootSettings.NormalizeProvider("opencode"));
        Assert.Null(ScanRootSettings.NormalizeProvider("gemini"));
        Assert.Null(ScanRootSettings.NormalizeProvider(null));
    }
}
