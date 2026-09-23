using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

[Collection("Providers diagnostics")]
public class ClaudeScannerIntegrationTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset ScanFloor = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);
    private readonly string _dir;

    public ClaudeScannerIntegrationTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-providers-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string CachePath => Path.Combine(_dir, "usage-cache.json");

    private static List<UsageEntry> ParseUtc(string path, IReadOnlyList<string> lines) =>
        ClaudeLogParser.Parse(lines, Tz);

    private IncrementalLogScanner<List<UsageEntry>> NewScanner(int parserVersion) => new(
        PhysicalFileSystemSource.Instance,
        new UsageScanCache<List<UsageEntry>>(CachePath, parserVersion));

    private string CopyFixture(string fixtureName, string relativePath)
    {
        var target = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "fixtures", fixtureName), target);
        return target;
    }

    private static UsageProviderSnapshot SnapshotOf(IReadOnlyList<ScannedFile<List<UsageEntry>>> files) =>
        new ClaudeUsageProvider().BuildSnapshot(files.Select(f => f.Payload ?? []));

    [Fact]
    public void WindowsOnlyFixtureProducesHandVerifiedTotals()
    {
        var root = Path.Combine(_dir, "win-root");
        CopyFixture("windows-claude.jsonl", @"win-root\project\session.jsonl");
        var files = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ClaudeLogParser.Parse);

        var file = Assert.Single(files);
        Assert.Equal(ScannedFileStatus.Parsed, file.Status);
        var snapshot = SnapshotOf(files);
        Assert.Equal(2, snapshot.Entries.Count);
        Assert.Equal(39058L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(320L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(0L, snapshot.Entries.Sum(e => e.CacheWrite));
        Assert.Equal(7808L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(47186L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void WslOnlyFixtureProducesHandVerifiedTotals()
    {
        var root = Path.Combine(_dir, "wsl-root");
        CopyFixture("wsl-claude.jsonl", @"wsl-root\project\session.jsonl");
        var files = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);

        var snapshot = SnapshotOf(files);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(8L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(1064L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(174878L, snapshot.Entries.Sum(e => e.CacheWrite));
        Assert.Equal(57786L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(233736L, snapshot.Entries.Sum(e => e.Total));
        Assert.All(snapshot.Entries, e => Assert.Equal("2026-09-01", e.LocalDay));
    }

    [Fact]
    public void MixedWindowsAndWslRootsProduceCombinedHandVerifiedTotals()
    {
        CopyFixture("windows-claude.jsonl", @"mixed\win\project\session.jsonl");
        CopyFixture("wsl-claude.jsonl", @"mixed\wsl\project\session.jsonl");
        var files = NewScanner(ClaudeLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "mixed", "win"), Path.Combine(_dir, "mixed", "wsl")], ScanFloor, ParseUtc);

        Assert.Equal(2, files.Count);
        var snapshot = SnapshotOf(files);
        Assert.Equal(6, snapshot.Entries.Count);
        Assert.Equal(39066L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(1384L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(174878L, snapshot.Entries.Sum(e => e.CacheWrite));
        Assert.Equal(65594L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(280922L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void SameFixtureExposedViaTwoRootsCountsOnce()
    {
        CopyFixture("windows-claude.jsonl", @"rootA\session.jsonl");
        CopyFixture("windows-claude.jsonl", @"rootB\copy.jsonl");
        var files = NewScanner(ClaudeLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "rootA"), Path.Combine(_dir, "rootB")], ScanFloor, ParseUtc);

        Assert.Equal(2, files.Count);
        var snapshot = SnapshotOf(files);
        Assert.Equal(2, snapshot.Entries.Count);
        Assert.Equal(47186L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void SecondScanServesCacheWithoutDoubleCounting()
    {
        CopyFixture("windows-claude.jsonl", @"logs\session.jsonl");
        CopyFixture("wsl-claude.jsonl", @"logs\wsl-session.jsonl");
        var root = Path.Combine(_dir, "logs");

        var first = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.Equal(2, first.Count);
        Assert.All(first, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));
        Assert.Equal(280922L, first.SelectMany(f => f.Payload ?? []).Sum(e => e.Total));

        var second = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.Equal(2, second.Count);
        Assert.All(second, f => Assert.Equal(ScannedFileStatus.Cached, f.Status));
        foreach (var cached in second)
        {
            var original = first.Single(f => f.Path == cached.Path);
            Assert.Equal(original.Payload!, cached.Payload!);
        }
        Assert.Equal(280922L, SnapshotOf(second).Entries.Sum(e => e.Total));
    }

    [Fact]
    public void ParserVersionBumpForcesFullReparse()
    {
        CopyFixture("windows-claude.jsonl", @"logs\session.jsonl");
        var root = Path.Combine(_dir, "logs");

        var first = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.All(first, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));

        var afterBump = NewScanner(ClaudeLogParser.ParserVersion + 1).Scan([root], ScanFloor, ParseUtc);
        Assert.All(afterBump, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));
        Assert.Equal(47186L, SnapshotOf(afterBump).Entries.Sum(e => e.Total));
    }

    [Fact]
    public void AppendedStreamRestartDuplicateIsCountedOnceAtMax()
    {
        var path = CopyFixture("windows-claude.jsonl", @"logs\session.jsonl");
        var root = Path.Combine(_dir, "logs");
        Assert.Equal(47186L,
            SnapshotOf(NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc))
                .Entries.Sum(e => e.Total));

        File.AppendAllText(path,
            "{\"type\":\"assistant\",\"timestamp\":\"2026-06-17T07:46:40.000Z\"," +
            "\"message\":{\"id\":\"msg_0000000000000000000000000011\",\"type\":\"message\"," +
            "\"role\":\"assistant\",\"model\":\"glm-5.2\",\"content\":\"x\"," +
            "\"usage\":{\"input_tokens\":19529,\"output_tokens\":999," +
            "\"cache_creation_input_tokens\":0,\"cache_read_input_tokens\":3904}}}\n");

        var rescan = NewScanner(ClaudeLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.Equal(ScannedFileStatus.Parsed, Assert.Single(rescan).Status);
        var snapshot = SnapshotOf(rescan);
        Assert.Equal(2, snapshot.Entries.Count);
        Assert.Equal(48025L, snapshot.Entries.Sum(e => e.Total));
        var restarted = Assert.Single(snapshot.Entries,
            e => e.Id == "msg_0000000000000000000000000011|");
        Assert.Equal(999, restarted.Output);
    }
}
