using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

[Collection("Providers diagnostics")]
public class CodexScannerIntegrationTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset ScanFloor = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);
    private readonly string _dir;

    public CodexScannerIntegrationTests()
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

    private static CodexParsedRollout ParseUtc(string path, IReadOnlyList<string> lines) =>
        CodexLogParser.Parse(path, lines, Tz);

    private IncrementalLogScanner<CodexParsedRollout> NewScanner(int parserVersion) => new(
        PhysicalFileSystemSource.Instance,
        new UsageScanCache<CodexParsedRollout>(CachePath, parserVersion));

    private string CopyFixture(string fixtureName, string relativePath)
    {
        var target = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(FixturePath(fixtureName), target);
        return target;
    }

    private static string FixturePath(string fixtureName) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", fixtureName);

    private static UsageProviderSnapshot SnapshotOf(IReadOnlyList<ScannedFile<CodexParsedRollout>> files) =>
        new CodexUsageProvider().BuildSnapshot(
            files.Select(f => f.Payload).Where(p => p is not null)!);

    [Fact]
    public void WindowsOnlyFixtureProducesHandVerifiedTotals()
    {
        CopyFixture("windows-codex.jsonl", @"win-root\sessions\2026\06\17\rollout.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "win-root")], ScanFloor, ParseUtc);

        var file = Assert.Single(files);
        Assert.Equal(ScannedFileStatus.Parsed, file.Status);
        var snapshot = SnapshotOf(files);
        Assert.Equal("codex", snapshot.ProviderId);
        Assert.Equal("Codex", snapshot.DisplayName);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(153_073L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(5_979L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(0L, snapshot.Entries.Sum(e => e.CacheWrite));
        Assert.Equal(161_280L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(320_332L, snapshot.Entries.Sum(e => e.Total));
        Assert.All(snapshot.Entries, e => Assert.StartsWith(
            "codex|00000000-0000-4000-8000-0000000000034|", e.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void WslOnlyFixtureProducesHandVerifiedTotals()
    {
        CopyFixture("wsl-codex.jsonl", @"wsl-root\sessions\rollout.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "wsl-root")], ScanFloor, ParseUtc);

        var snapshot = SnapshotOf(files);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(31_891L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(504L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(95_232L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(127_627L, snapshot.Entries.Sum(e => e.Total));
        Assert.All(snapshot.Entries, e => Assert.Equal("2026-09-06", e.LocalDay));
        Assert.All(snapshot.Entries, e => Assert.Equal("gpt-5.6-terra", e.Model));
    }

    [Fact]
    public void MixedWindowsAndWslRootsProduceCombinedHandVerifiedTotals()
    {
        CopyFixture("windows-codex.jsonl", @"mixed\win\sessions\rollout.jsonl");
        CopyFixture("wsl-codex.jsonl", @"mixed\wsl\sessions\rollout.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "mixed", "win"), Path.Combine(_dir, "mixed", "wsl")], ScanFloor, ParseUtc);

        Assert.Equal(2, files.Count);
        var snapshot = SnapshotOf(files);
        Assert.Equal(8, snapshot.Entries.Count);
        Assert.Equal(184_964L, snapshot.Entries.Sum(e => e.Input));
        Assert.Equal(6_483L, snapshot.Entries.Sum(e => e.Output));
        Assert.Equal(256_512L, snapshot.Entries.Sum(e => e.CacheRead));
        Assert.Equal(447_959L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void SameRolloutExposedViaTwoRootsCountsOnce()
    {
        CopyFixture("windows-codex.jsonl", @"rootA\rollout.jsonl");
        CopyFixture("windows-codex.jsonl", @"rootB\copy.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "rootA"), Path.Combine(_dir, "rootB")], ScanFloor, ParseUtc);

        Assert.Equal(2, files.Count);
        var snapshot = SnapshotOf(files);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(320_332L, snapshot.Entries.Sum(e => e.Total));
    }

    [Fact]
    public void ForkFixturesThroughTheScannerKeepUsageOnce()
    {
        CopyFixture(@"CodexFork\parent.jsonl", @"fork-root\rollout-parent.jsonl");
        CopyFixture(@"CodexFork\child.jsonl", @"fork-root\rollout-child.jsonl");
        CopyFixture(@"CodexFork\sibling.jsonl", @"fork-root\rollout-sibling.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "fork-root")], ScanFloor, ParseUtc);

        Assert.Equal(3, files.Count);
        var snapshot = SnapshotOf(files);
        Assert.Equal(369_215L, snapshot.Entries.Sum(e => e.Total));
        Assert.Equal(snapshot.Entries.Count, snapshot.Entries.Select(e => e.Id).Distinct().Count());
    }

    [Fact]
    public void SubagentFixturesThroughTheScannerKeepOwnUsage()
    {
        CopyFixture(@"CodexSubagent\parent-v145.jsonl", @"sub-root\rollout-parent.jsonl");
        CopyFixture(@"CodexSubagent\child-v145.jsonl", @"sub-root\rollout-child.jsonl");
        var files = NewScanner(CodexLogParser.ParserVersion).Scan(
            [Path.Combine(_dir, "sub-root")], ScanFloor, ParseUtc);

        var snapshot = SnapshotOf(files);
        Assert.Equal(106_583L, snapshot.Entries.Sum(e => e.Total));
        Assert.Equal(5, snapshot.Entries.Count);
    }

    [Fact]
    public void SecondScanServesCacheWithoutDoubleCounting()
    {
        CopyFixture("windows-codex.jsonl", @"logs\rollout.jsonl");
        CopyFixture("wsl-codex.jsonl", @"logs\rollout-wsl.jsonl");
        var root = Path.Combine(_dir, "logs");

        var first = NewScanner(CodexLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.Equal(2, first.Count);
        Assert.All(first, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));
        Assert.Equal(447_959L, SnapshotOf(first).Entries.Sum(e => e.Total));

        var second = NewScanner(CodexLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.Equal(2, second.Count);
        Assert.All(second, f => Assert.Equal(ScannedFileStatus.Cached, f.Status));
        foreach (var cached in second)
        {
            var original = first.Single(f => f.Path == cached.Path).Payload!;
            var roundTripped = cached.Payload!;
            Assert.Equal(original.Path, roundTripped.Path);
            Assert.Equal(original.SessionID, roundTripped.SessionID);
            Assert.Equal(original.ParentSessionID, roundTripped.ParentSessionID);
            Assert.Equal(original.ForkedAt, roundTripped.ForkedAt);
            Assert.Equal(original.IsSubagent, roundTripped.IsSubagent);
            Assert.Equal(original.Events, roundTripped.Events);
        }
        Assert.Equal(447_959L, SnapshotOf(second).Entries.Sum(e => e.Total));
    }

    [Fact]
    public void ParserVersionBumpForcesFullReparse()
    {
        CopyFixture("windows-codex.jsonl", @"logs\rollout.jsonl");
        var root = Path.Combine(_dir, "logs");

        var first = NewScanner(CodexLogParser.ParserVersion).Scan([root], ScanFloor, ParseUtc);
        Assert.All(first, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));

        var afterBump = NewScanner(CodexLogParser.ParserVersion + 1).Scan([root], ScanFloor, ParseUtc);
        Assert.All(afterBump, f => Assert.Equal(ScannedFileStatus.Parsed, f.Status));
        Assert.Equal(320_332L, SnapshotOf(afterBump).Entries.Sum(e => e.Total));
    }

    [Fact]
    public void BareMethodGroupWiringScansFixture()
    {
        CopyFixture("windows-codex.jsonl", @"logs\rollout.jsonl");
        var root = Path.Combine(_dir, "logs");

        var files = NewScanner(CodexLogParser.ParserVersion).Scan([root], ScanFloor, CodexLogParser.Parse);

        Assert.Single(files);
        var snapshot = SnapshotOf(files);
        Assert.Equal(4, snapshot.Entries.Count);
        Assert.Equal(320_332L, snapshot.Entries.Sum(e => e.Total));
    }
}
