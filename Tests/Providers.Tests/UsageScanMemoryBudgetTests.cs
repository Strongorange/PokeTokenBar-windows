using System.Text;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

/// <summary>
/// Allocation budgets for scanning large logs. Allocations are counted on the scanning thread, so the numbers
/// do not depend on GC timing; a scanner that reads a whole file into one string blows past them.
/// </summary>
[Collection("Providers diagnostics")]
public class UsageScanMemoryBudgetTests : IDisposable
{
    private const long MiB = 1024 * 1024;
    private const long LogSize = 64 * MiB;
    private const long FirstScanBudget = 32 * MiB;
    private const long AppendBytes = 1 * MiB;
    private const long AppendScanBudget = 8 * MiB;
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private readonly string _dir;

    public UsageScanMemoryBudgetTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-memory-budget-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private static long AllocatedBy(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static string ClaudeToolResultLine(int n) =>
        "{\"type\":\"user\",\"timestamp\":\"2026-11-10T01:00:00Z\",\"message\":{\"role\":\"user\",\"content\":[{\"type\":\"tool_result\",\"content\":\"" +
        new string((char)('a' + n % 26), 30_000) + "\"}]}}";

    private static string ClaudeUsageLine(int n) =>
        "{\"type\":\"assistant\",\"timestamp\":\"2026-11-10T01:00:00Z\",\"requestId\":\"req_" + n +
        "\",\"message\":{\"id\":\"msg_" + n + "\",\"model\":\"claude-opus\",\"content\":[{\"type\":\"text\",\"text\":\"" +
        new string('z', 600) + "\"}],\"usage\":{\"input_tokens\":10,\"output_tokens\":20,\"cache_creation_input_tokens\":0,\"cache_read_input_tokens\":5}}}";

    private static string CodexResponseLine(int n) =>
        "{\"timestamp\":\"2026-11-10T01:00:00Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"function_call_output\",\"output\":\"" +
        new string((char)('a' + n % 26), 30_000) + "\"}}";

    private static string CodexTokenLine(int n) =>
        "{\"timestamp\":\"2026-11-10T01:00:00Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":{" +
        "\"total_token_usage\":{\"input_tokens\":" + (n + 1) * 100 + ",\"cached_input_tokens\":0,\"output_tokens\":" + (n + 1) * 10 +
        ",\"reasoning_output_tokens\":0,\"total_tokens\":" + (n + 1) * 110 + "}," +
        "\"last_token_usage\":{\"input_tokens\":100,\"cached_input_tokens\":0,\"output_tokens\":10,\"reasoning_output_tokens\":0,\"total_tokens\":110}}}}";

    private const string CodexHeader =
        "{\"timestamp\":\"2026-11-10T01:00:00Z\",\"type\":\"session_meta\",\"payload\":{\"id\":\"00000000-0000-4000-8000-000000000001\"}}\n" +
        "{\"timestamp\":\"2026-11-10T01:00:00Z\",\"type\":\"turn_context\",\"payload\":{\"model\":\"gpt-5.5\"}}\n";

    /// <summary>Writes blocks of one large irrelevant line and one usage line until the file reaches <paramref name="size"/>.</summary>
    private static int AppendBlocks(string path, long size, Func<int, string> bulky, Func<int, string> usage, int start = 0)
    {
        using var stream = new FileStream(path, FileMode.Append);
        var written = 0L;
        var n = start;
        while (written < size)
        {
            var block = Encoding.UTF8.GetBytes(bulky(n) + "\n" + usage(n) + "\n");
            stream.Write(block);
            written += block.Length;
            n++;
        }
        return n;
    }

    private sealed class LineCounter
    {
        public int Count;

        public IEnumerable<string> Through(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                Count++;
                yield return line;
            }
        }
    }

    private (string Root, string Log) NewLog()
    {
        var root = Path.Combine(_dir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return (root, Path.Combine(root, "rollout-big.jsonl"));
    }

    [Fact]
    public void LargeClaudeLogStaysWithinTheAllocationBudget()
    {
        var (root, log) = NewLog();
        var next = AppendBlocks(log, LogSize, ClaudeToolResultLine, ClaudeUsageLine);
        var handed = new LineCounter();
        var parser = new LogParser<List<UsageEntry>>(
            ClaudeLogParser.MightMatter, (_, lines, resumeFrom) => ClaudeLogParser.Parse(handed.Through(lines), Tz, resumeFrom));
        var scanner = new IncrementalLogScanner<List<UsageEntry>>(
            PhysicalFileSystemSource.Instance, new UsageScanCache<List<UsageEntry>>(Path.Combine(root, "cache.json"), 1));

        IReadOnlyList<ScannedFile<List<UsageEntry>>> first = [];
        var firstScan = AllocatedBy(() => first = scanner.Scan([root], DateTimeOffset.MinValue, parser));
        var total = AppendBlocks(log, AppendBytes, ClaudeToolResultLine, ClaudeUsageLine, next);
        handed.Count = 0;
        IReadOnlyList<ScannedFile<List<UsageEntry>>> second = [];
        var appendScan = AllocatedBy(() => second = scanner.Scan([root], DateTimeOffset.MinValue, parser));

        Assert.Equal(next, Assert.Single(first).Payload!.Count);
        Assert.Equal(total, Assert.Single(second).Payload!.Count);
        Assert.Equal(total - next, handed.Count);
        Assert.True(firstScan < FirstScanBudget, $"first scan allocated {firstScan / MiB} MiB");
        Assert.True(appendScan < AppendScanBudget, $"append scan allocated {appendScan / MiB} MiB");
    }

    [Fact]
    public void LargeCodexRolloutStaysWithinTheAllocationBudget()
    {
        var (root, log) = NewLog();
        File.WriteAllText(log, CodexHeader);
        var next = AppendBlocks(log, LogSize, CodexResponseLine, CodexTokenLine);
        var handed = new LineCounter();
        var parser = new LogParser<CodexParsedRollout>(
            CodexLogParser.MightMatter, (path, lines, resumeFrom) => CodexLogParser.Parse(path, handed.Through(lines), Tz, resumeFrom));
        var scanner = new IncrementalLogScanner<CodexParsedRollout>(
            PhysicalFileSystemSource.Instance, new UsageScanCache<CodexParsedRollout>(Path.Combine(root, "cache.json"), 1));

        IReadOnlyList<ScannedFile<CodexParsedRollout>> first = [];
        var firstScan = AllocatedBy(() => first = scanner.Scan([root], DateTimeOffset.MinValue, parser));
        var total = AppendBlocks(log, AppendBytes, CodexResponseLine, CodexTokenLine, next);
        handed.Count = 0;
        IReadOnlyList<ScannedFile<CodexParsedRollout>> second = [];
        var appendScan = AllocatedBy(() => second = scanner.Scan([root], DateTimeOffset.MinValue, parser));

        Assert.Equal(next, Assert.Single(first).Payload!.Events.Count);
        Assert.Equal(total, Assert.Single(second).Payload!.Events.Count);
        Assert.Equal(total - next, handed.Count);
        Assert.True(firstScan < FirstScanBudget, $"first scan allocated {firstScan / MiB} MiB");
        Assert.True(appendScan < AppendScanBudget, $"append scan allocated {appendScan / MiB} MiB");
    }
}
