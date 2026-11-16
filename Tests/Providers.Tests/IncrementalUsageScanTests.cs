using System.Text;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

[Collection("Providers diagnostics")]
public class IncrementalUsageScanTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Floor = DateTimeOffset.MinValue;
    private readonly string _dir;

    private static readonly LogParser<List<UsageEntry>> Claude =
        new(ClaudeLogParser.MightMatter, (_, lines, resumeFrom) => ClaudeLogParser.Parse(lines, Tz, resumeFrom));

    private static readonly LogParser<CodexParsedRollout> Codex =
        new(CodexLogParser.MightMatter, (path, lines, resumeFrom) => CodexLogParser.Parse(path, lines, Tz, resumeFrom));

    public IncrementalUsageScanTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-incremental-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    public static TheoryData<string> ClaudeFixtures => ["windows-claude.jsonl", "wsl-claude.jsonl"];

    public static TheoryData<string> CodexFixtures =>
    [
        "windows-codex.jsonl", "wsl-codex.jsonl",
        @"CodexFork\parent.jsonl", @"CodexFork\child.jsonl", @"CodexFork\sibling.jsonl",
    ];

    private static byte[] FixtureBytes(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    /// <summary>Every byte position that ends a line, and every position in the middle of a line.</summary>
    private static IEnumerable<int> SplitPoints(byte[] bytes)
    {
        var lineStart = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != (byte)'\n') continue;
            yield return lineStart + (i - lineStart) / 2;
            yield return i + 1;
            lineStart = i + 1;
        }
    }

    private (string Root, string Log) NewLog()
    {
        var root = Path.Combine(_dir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return (root, Path.Combine(root, "rollout-session.jsonl"));
    }

    private IncrementalLogScanner<T> NewScanner<T>(string root) =>
        new(PhysicalFileSystemSource.Instance, new UsageScanCache<T>(Path.Combine(root, "cache.json"), 1));

    private static T ScanSingle<T>(IncrementalLogScanner<T> scanner, string root, LogParser<T> parser) =>
        Assert.Single(scanner.Scan([root], Floor, parser)).Payload!;

    private static List<string> AllLines(byte[] bytes) =>
        Encoding.UTF8.GetString(bytes).Split('\n').SkipLast(1).Select(line => line.TrimEnd('\r')).ToList();

    private static List<string> Describe(IEnumerable<UsageEntry> entries) =>
        entries.Select(e => $"{e.Id}|{e.Date:O}|{e.Model}|{e.Total}").Order(StringComparer.Ordinal).ToList();

    private static List<string> Describe(CodexParsedRollout rollout) =>
    [
        $"{rollout.SessionID}|{rollout.ParentSessionID}|{rollout.ForkedAt:O}|{rollout.IsSubagent}|{rollout.Resume}",
        .. rollout.Events.Select(e => $"{e.Entry.Id}|{e.Entry.Model}|{e.Entry.Total}|{e.UsageState?.Fingerprint()}|{e.SessionID}"),
    ];

    [Theory]
    [MemberData(nameof(ClaudeFixtures))]
    public void ClaudeFileReadInTwoPartsMatchesOneFullParse(string fixture)
    {
        var bytes = FixtureBytes(fixture);
        var expected = Describe(ClaudeLogParser.Parse(AllLines(bytes), Tz));
        foreach (var split in SplitPoints(bytes))
        {
            var (root, log) = NewLog();
            var scanner = NewScanner<List<UsageEntry>>(root);
            File.WriteAllBytes(log, bytes[..split]);
            ScanSingle(scanner, root, Claude);
            using (var stream = new FileStream(log, FileMode.Append)) stream.Write(bytes, split, bytes.Length - split);

            Assert.Equal(expected, Describe(ScanSingle(scanner, root, Claude)));
        }
    }

    [Theory]
    [MemberData(nameof(CodexFixtures))]
    public void CodexFileReadInTwoPartsMatchesOneFullParse(string fixture)
    {
        var bytes = FixtureBytes(fixture);
        foreach (var split in SplitPoints(bytes))
        {
            var (root, log) = NewLog();
            var expected = Describe(CodexLogParser.Parse(log, AllLines(bytes), Tz));
            var scanner = NewScanner<CodexParsedRollout>(root);
            File.WriteAllBytes(log, bytes[..split]);
            ScanSingle(scanner, root, Codex);
            using (var stream = new FileStream(log, FileMode.Append)) stream.Write(bytes, split, bytes.Length - split);

            Assert.Equal(expected, Describe(ScanSingle(scanner, root, Codex)));
        }
    }

    [Fact]
    public void FileRewrittenInPlaceIsParsedAgainFromTheStart()
    {
        var (root, log) = NewLog();
        var scanner = NewScanner<List<UsageEntry>>(root);
        var first = FixtureBytes("windows-claude.jsonl");
        var second = FixtureBytes("wsl-claude.jsonl");
        File.WriteAllBytes(log, first);
        ScanSingle(scanner, root, Claude);

        // 길이가 더 길어 이어 쓴 것처럼 보이지만 앞부분이 달라진 파일
        File.WriteAllBytes(log, [.. second, .. first]);

        var expected = Describe(ClaudeLogParser.Parse(AllLines([.. second, .. first]), Tz));
        Assert.Equal(expected, Describe(ScanSingle(scanner, root, Claude)));
    }

    [Fact]
    public void FileThatShrankIsParsedAgainFromTheStart()
    {
        var (root, log) = NewLog();
        var scanner = NewScanner<List<UsageEntry>>(root);
        var both = (byte[])[.. FixtureBytes("windows-claude.jsonl"), .. FixtureBytes("wsl-claude.jsonl")];
        File.WriteAllBytes(log, both);
        ScanSingle(scanner, root, Claude);

        var shorter = FixtureBytes("wsl-claude.jsonl");
        File.WriteAllBytes(log, shorter);

        Assert.Equal(Describe(ClaudeLogParser.Parse(AllLines(shorter), Tz)), Describe(ScanSingle(scanner, root, Claude)));
    }
}
