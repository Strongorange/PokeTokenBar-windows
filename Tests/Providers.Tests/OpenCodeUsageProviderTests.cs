using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class OpenCodeUsageProviderTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "opencode", "opencode-messages.jsonl");

    [Fact]
    public void ProviderIdentifiesAsOpenCode()
    {
        var provider = new OpenCodeUsageProvider();

        Assert.Equal("opencode", provider.ProviderId);
        Assert.Equal("OpenCode", provider.DisplayName);
    }

    [Fact]
    public void ParseFileProducesEntriesFromFixtureLines()
    {
        var provider = new OpenCodeUsageProvider();

        var payload = provider.ParseFile(FixturePath, File.ReadAllLines(FixturePath));

        Assert.Equal(9, payload.Count);
        Assert.All(payload, e => Assert.StartsWith("opencode|opencode-messages.jsonl|", e.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void BuildSnapshotDedupsAcrossRootPayloadsKeepingMax()
    {
        var provider = new OpenCodeUsageProvider();
        var first = OpenCodeLogParser.Parse(FixturePath, File.ReadAllLines(FixturePath), Tz);
        var second = OpenCodeLogParser.Parse(FixturePath, File.ReadAllLines(FixturePath), Tz);

        var snapshot = provider.BuildSnapshot([first, second]);

        Assert.Equal("opencode", snapshot.ProviderId);
        Assert.Equal("OpenCode", snapshot.DisplayName);
        Assert.Equal(first.Count, snapshot.Entries.Count);
        Assert.Equal(first.Sum(e => e.Total), snapshot.Entries.Sum(e => e.Total));
    }
}
