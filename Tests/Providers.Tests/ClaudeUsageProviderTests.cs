using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class ClaudeUsageProviderTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    [Fact]
    public void SnapshotIdentifiesProviderAndDedupsGlobally()
    {
        var provider = new ClaudeUsageProvider();
        Assert.Equal("claude_code", provider.ProviderId);
        Assert.Equal("Claude Code", provider.DisplayName);

        var fileA = new[]
        {
            new UsageEntry("a|", DateTimeOffset.UtcNow, "2026-09-01", "claude-opus-5", 10, 100, 0, 0),
            new UsageEntry("b|", DateTimeOffset.UtcNow, "2026-09-01", "claude-opus-5", 5, 50, 0, 0),
        };
        var fileB = new[]
        {
            new UsageEntry("a|", DateTimeOffset.UtcNow, "2026-09-01", "claude-opus-5", 10, 200, 0, 0),
        };
        var snapshot = provider.BuildSnapshot([[.. fileA], [.. fileB]]);

        Assert.Equal("claude_code", snapshot.ProviderId);
        Assert.Equal("Claude Code", snapshot.DisplayName);
        Assert.Equal(2, snapshot.Entries.Count);
        Assert.Equal(265L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void ParseFileParsesFixtureLines()
    {
        var provider = new ClaudeUsageProvider();
        var lines = File.ReadAllLines(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "windows-claude.jsonl"));
        var entries = provider.ParseFile("ignored.jsonl", lines);
        Assert.Equal(2, entries.Count);
        Assert.Equal(47186L, entries.Sum(e => e.Total));
    }
}
