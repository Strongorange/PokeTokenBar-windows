using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class CodexUsageProviderTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    private static CodexParsedRollout ParseFixture(string name) =>
        CodexLogParser.Parse(
            Path.Combine(AppContext.BaseDirectory, "fixtures", name),
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "fixtures", name)), Tz);

    [Fact]
    public void ProviderIdentifiesAsCodex()
    {
        var provider = new CodexUsageProvider();
        Assert.Equal("codex", provider.ProviderId);
        Assert.Equal("Codex", provider.DisplayName);
    }

    [Fact]
    public void BuildSnapshotDedupsDuplicateRolloutsByCanonicalId()
    {
        var provider = new CodexUsageProvider();
        var windows = ParseFixture("windows-codex.jsonl");
        var duplicate = ParseFixture("windows-codex.jsonl");

        var snapshot = provider.BuildSnapshot([windows, duplicate]);

        Assert.Equal("codex", snapshot.ProviderId);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(320_332L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void ParseFileProducesRolloutFromLines()
    {
        var provider = new CodexUsageProvider();
        var lines = new[]
        {
            CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
        };
        var rollout = provider.ParseFile("rollout-a.jsonl", lines);
        Assert.Equal("session-a", rollout.SessionID);
        Assert.Single(rollout.Events);
    }
}
