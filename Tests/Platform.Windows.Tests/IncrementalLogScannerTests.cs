using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Platform.Windows.Tests;

[Collection("Platform diagnostics")]
public class IncrementalLogScannerTests : IDisposable
{
    private readonly string _dir;
    private static readonly DateTimeOffset ScanFloor = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);

    public IncrementalLogScannerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-scanner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string CachePath => Path.Combine(_dir, "usage-cache.json");

    private IncrementalLogScanner<int> NewScanner(int parserVersion = 1) => new(
        PhysicalFileSystemSource.Instance, new UsageScanCache<int>(CachePath, parserVersion));

    private static int LineCount(string path, IReadOnlyList<string> lines) => lines.Count;

    private string WriteLog(string relativePath, string content)
    {
        var path = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void SecondScanParsesNothingAndKeepsAggregateIdentical()
    {
        var claudeRoot = Path.Combine(_dir, "scan", "claude");
        var codexRoot = Path.Combine(_dir, "scan", "codex");
        Directory.CreateDirectory(claudeRoot);
        Directory.CreateDirectory(codexRoot);
        var expectedTotal = 0;
        foreach (var fixture in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "fixtures")))
        {
            var target = Path.Combine(fixture.Contains("codex") ? codexRoot : claudeRoot, Path.GetFileName(fixture));
            File.Copy(fixture, target);
            expectedTotal += File.ReadAllText(target).Split('\n').Length - 1;
        }

        var first = NewScanner();
        var firstResults = first.Scan([claudeRoot, codexRoot], ScanFloor, LineCount);
        Assert.Equal(4, firstResults.Count);
        Assert.All(firstResults, r => Assert.Equal(ScannedFileStatus.Parsed, r.Status));
        Assert.Equal(expectedTotal, firstResults.Sum(r => r.Payload));

        var second = NewScanner();
        var secondResults = second.Scan([claudeRoot, codexRoot], ScanFloor, LineCount);
        Assert.Equal(4, secondResults.Count);
        Assert.DoesNotContain(secondResults, r => r.Status == ScannedFileStatus.Parsed);
        Assert.All(secondResults, r => Assert.Equal(ScannedFileStatus.Cached, r.Status));
        Assert.Equal(firstResults.Sum(r => r.Payload), secondResults.Sum(r => r.Payload));
    }

    [Fact]
    public void ModifiedFileIsReparsedWhileOthersStayCached()
    {
        var a = WriteLog(@"logs\a.jsonl", "one\ntwo\n");
        WriteLog(@"logs\b.jsonl", "one\n");
        var root = Path.Combine(_dir, "logs");
        var first = NewScanner();
        Assert.Equal(2, first.Scan([root], ScanFloor, LineCount).Count(r => r.Status == ScannedFileStatus.Parsed));

        File.AppendAllText(a, "three\n");
        var second = NewScanner();
        var results = second.Scan([root], ScanFloor, LineCount);
        var reparsed = results.Single(r => r.Status == ScannedFileStatus.Parsed);
        Assert.Equal(PathNormalizer.Normalize(a), reparsed.Path);
        Assert.Equal(3, reparsed.Payload);
        Assert.Single(results, r => r.Status == ScannedFileStatus.Cached);
    }

    [Fact]
    public void PartialFinalLineIsSkippedAndLogged()
    {
        WriteLog(@"logs\a.jsonl", "{\"a\":1}\n{\"a\":2}\n{\"partial");
        var results = NewScanner().Scan([Path.Combine(_dir, "logs")], ScanFloor, LineCount);
        var file = Assert.Single(results);
        Assert.Equal(2, file.Payload);
        Assert.Contains("partial final line skipped", Diagnostics.Read(_dir));
    }

    [Fact]
    public void LockedFileYieldsReadFailedWithStalePayloadUntilReleased()
    {
        var path = WriteLog(@"logs\a.jsonl", "l1\nl2\nl3\n");
        var root = Path.Combine(_dir, "logs");
        var scanner = NewScanner();
        var firstPass = Assert.Single(scanner.Scan([root], ScanFloor, LineCount));
        Assert.Equal(ScannedFileStatus.Parsed, firstPass.Status);
        Assert.Equal(3, firstPass.Payload);

        File.AppendAllText(path, "l4\n");
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var lockedPass = Assert.Single(scanner.Scan([root], ScanFloor, LineCount));
            Assert.Equal(ScannedFileStatus.ReadFailed, lockedPass.Status);
            Assert.Equal(3, lockedPass.Payload);
        }
        var releasedPass = Assert.Single(scanner.Scan([root], ScanFloor, LineCount));
        Assert.Equal(ScannedFileStatus.Parsed, releasedPass.Status);
        Assert.Equal(4, releasedPass.Payload);
        Assert.Contains("log read failed", Diagnostics.Read(_dir));
    }

    [Fact]
    public void FilesOlderThanScanFloorAreSkipped()
    {
        var path = WriteLog(@"logs\a.jsonl", "one\n");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-10));
        var results = NewScanner().Scan([Path.Combine(_dir, "logs")], DateTimeOffset.UtcNow.AddDays(-5), LineCount);
        Assert.Empty(results);
    }

    [Fact]
    public void OverlappingRootsScanEachFileOnce()
    {
        WriteLog(@"logs\a.jsonl", "one\n");
        WriteLog(@"logs\claude\b.jsonl", "one\ntwo\n");
        WriteLog(@"logs\claude\sub\c.jsonl", "one\n");
        WriteLog(@"logs\codex\d.jsonl", "one\n");
        var parsedCalls = 0;
        int CountingParse(string path, IReadOnlyList<string> lines)
        {
            parsedCalls++;
            return lines.Count;
        }
        var logs = Path.Combine(_dir, "logs");
        var results = NewScanner().Scan([logs, Path.Combine(logs, "claude")], ScanFloor, CountingParse);
        Assert.Equal(4, results.Count);
        Assert.Equal(4, parsedCalls);
    }

    [Fact]
    public void MissingRootIsLoggedNotThrown()
    {
        var results = NewScanner().Scan([Path.Combine(_dir, "nope")], ScanFloor, LineCount);
        Assert.Empty(results);
        Assert.Contains("scan root missing", Diagnostics.Read(_dir));
    }

    [Fact]
    public void HiddenAndNonLogFilesAreIgnored()
    {
        WriteLog(@"logs\a.jsonl", "one\n");
        WriteLog(@"logs\.hidden.jsonl", "one\n");
        WriteLog(@"logs\readme.txt", "one\n");
        WriteLog(@"logs\.meta\inner.jsonl", "one\n");
        var results = NewScanner().Scan([Path.Combine(_dir, "logs")], ScanFloor, LineCount);
        var file = Assert.Single(results);
        Assert.EndsWith("a.jsonl", file.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParserVersionChangeForcesFullReparse()
    {
        WriteLog(@"logs\a.jsonl", "one\n");
        WriteLog(@"logs\b.jsonl", "one\n");
        var root = Path.Combine(_dir, "logs");
        Assert.Equal(2, NewScanner(parserVersion: 1).Scan([root], ScanFloor, LineCount).Count);
        var afterUpgrade = NewScanner(parserVersion: 2).Scan([root], ScanFloor, LineCount);
        Assert.Equal(2, afterUpgrade.Count(r => r.Status == ScannedFileStatus.Parsed));
    }
}

public class SplitLinesTests
{
    [Fact]
    public void EmptyTextYieldsNoLines()
    {
        var lines = IncrementalLogScanner<int>.SplitLines("", out var dropped);
        Assert.Empty(lines);
        Assert.False(dropped);
    }

    [Fact]
    public void TrailingNewlineMeansNoPartialLine()
    {
        var lines = IncrementalLogScanner<int>.SplitLines("a\nb\n", out var dropped);
        Assert.Equal(["a", "b"], lines);
        Assert.False(dropped);
    }

    [Fact]
    public void MissingTrailingNewlineDropsPartialFinalLine()
    {
        var lines = IncrementalLogScanner<int>.SplitLines("a\nb", out var dropped);
        Assert.Equal(["a"], lines);
        Assert.True(dropped);
    }

    [Fact]
    public void CarriageReturnsAreTrimmed()
    {
        var lines = IncrementalLogScanner<int>.SplitLines("a\r\nb\r\n", out var dropped);
        Assert.Equal(["a", "b"], lines);
        Assert.False(dropped);
    }

    [Fact]
    public void BlankLinesArePreserved()
    {
        var lines = IncrementalLogScanner<int>.SplitLines("a\n\nb\n", out var dropped);
        Assert.Equal(["a", "", "b"], lines);
        Assert.False(dropped);
    }
}
