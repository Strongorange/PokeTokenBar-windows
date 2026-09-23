using System.Text.Json;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application.Tests;

[Collection("Application diagnostics")]
public class UsageRefreshServiceTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly string ParentSession = "11111111-1111-4111-8111-111111111111";
    private static readonly string ChildSession = "22222222-2222-4222-8222-222222222222";

    private readonly string _dir;
    private readonly string _profile;
    private readonly string _cacheDir;

    public UsageRefreshServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-application-tests", Guid.NewGuid().ToString("N"));
        _profile = Path.Combine(_dir, "profile");
        _cacheDir = Path.Combine(_dir, "cache");
        Directory.CreateDirectory(_profile);
        Directory.CreateDirectory(_cacheDir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private UsageRefreshService BuildService(
        CountingRootSource? rootSource = null,
        Func<CodexRolloutFile, string?>? probe = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var fileSystem = new CachedFileSystemSource(PhysicalFileSystemSource.Instance);
        return new UsageRefreshService(new UsageRefreshOptions
        {
            CacheDirectory = _cacheDir,
            TimeZone = Tz,
            Clock = () => Now,
            RootSource = rootSource ?? CreateRootSource(),
            FileSystem = fileSystem,
            Providers = ProviderCatalog.Default(_cacheDir, fileSystem, probe),
            Delay = delay,
        });
    }

    private CountingRootSource CreateRootSource(ManualResetEventSlim? gate = null) =>
        new(new UsageRootOptions { UserProfile = _profile, IncludeWsl = false }, gate);

    [Fact]
    public async Task CombinedTotalsComeFromBothProviders()
    {
        WriteClaudeTodayAndEarlierThisMonth();
        WriteCodexSingleSessionToday();

        var state = await BuildService().RefreshAsync();

        Assert.Equal(Now, state.AsOfUtc);
        var claude = state.Provider("claude_code")!;
        Assert.True(claude.Available);
        Assert.Equal(3300, claude.TodayTokens);
        Assert.Equal(3850, claude.MonthTokens);
        var codex = state.Provider("codex")!;
        Assert.True(codex.Available);
        Assert.Equal(3300, codex.TodayTokens);
        Assert.Equal(3300, codex.MonthTokens);
        Assert.Equal(6600, state.TodayTokens);
        Assert.Equal(7150, state.MonthTokens);
    }

    [Fact]
    public async Task MissingCodexRootsMarkCodexUnavailable()
    {
        WriteClaudeTodayAndEarlierThisMonth();

        var state = await BuildService().RefreshAsync();

        Assert.True(state.Provider("claude_code")!.Available);
        var codex = state.Provider("codex")!;
        Assert.False(codex.Available);
        Assert.Equal(0, codex.TodayTokens);
        Assert.Equal(3300, state.TodayTokens);
        Assert.Equal(3850, state.MonthTokens);
    }

    [Fact]
    public async Task OutOfWindowParentIsDependencyOnly()
    {
        WriteCodexParentChild();

        var state = await BuildService().RefreshAsync();

        var codex = state.Provider("codex")!;
        Assert.True(codex.Available);
        Assert.Equal(1210, codex.TodayTokens);
        Assert.Equal(1210, codex.MonthTokens);
        Assert.Equal(1210, state.TodayTokens);
    }

    [Fact]
    public async Task TransientProbeFailureSkipsRoundWithoutPoisoningLaterRounds()
    {
        WriteCodexParentChild();
        var probe = new FlakyProbe();
        var service = BuildService(probe: probe.Probe);

        await Assert.ThrowsAsync<IOException>(() => service.RefreshAsync());
        Assert.Null(service.Current);
        Assert.Contains("refresh failed", Diagnostics.Read(_dir));

        var state = await service.RefreshAsync();
        Assert.Equal(1210, state.Provider("codex")!.TodayTokens);
        Assert.Equal(2, probe.Calls);
    }

    [Fact]
    public async Task RepeatRefreshAndNewFileDoNotDoubleCount()
    {
        WriteClaudeTodayAndEarlierThisMonth();
        WriteCodexSingleSessionToday();
        var service = BuildService();

        var first = await service.RefreshAsync();
        Assert.Equal(6600, first.TodayTokens);
        Assert.True(File.Exists(Path.Combine(_cacheDir, ProviderCatalog.ClaudeCacheFileName)));
        Assert.True(File.Exists(Path.Combine(_cacheDir, ProviderCatalog.CodexCacheFileName)));

        var second = await service.RefreshAsync();
        Assert.Equal(6600, second.TodayTokens);
        Assert.Equal(7150, second.MonthTokens);

        WriteClaudeFile(@"p2\extra.jsonl",
            ClaudeLine("msg_x1", "2026-09-23T11:00:00.000Z", 600, 60, 0));
        var third = await service.RefreshAsync();
        Assert.Equal(7260, third.TodayTokens);
        Assert.Equal(7810, third.MonthTokens);
    }

    [Fact]
    public async Task ConcurrentManualRefreshesExecuteSingleFlight()
    {
        WriteClaudeTodayAndEarlierThisMonth();
        var gate = new ManualResetEventSlim(false);
        var roots = CreateRootSource(gate);
        var service = BuildService(roots);

        var first = service.RefreshAsync();
        try
        {
            WaitUntil(() => Volatile.Read(ref roots.Calls) == 1);
            var second = service.RefreshAsync();
            await Task.Delay(100);
            Assert.Equal(1, Volatile.Read(ref roots.Calls));

            gate.Set();
            var fromFirst = await first;
            var fromSecond = await second;
            Assert.Same(fromFirst, fromSecond);
            Assert.Equal(1, Volatile.Read(ref roots.Calls));
        }
        finally
        {
            gate.Set();
            try { await first; } catch { }
        }
    }

    [Fact]
    public async Task TimerTickTriggersRefreshAndCancellationStopsLoop()
    {
        WriteClaudeTodayAndEarlierThisMonth();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var roots = CreateRootSource();
        var service = BuildService(roots, delay: (_, ct) => gate.Task.WaitAsync(ct));
        using var cts = new CancellationTokenSource();

        var loop = service.RunAsync(cts.Token);
        await Task.Delay(50);
        Assert.Equal(0, Volatile.Read(ref roots.Calls));

        try
        {
            gate.SetResult();
            WaitUntil(() => service.Current is not null);
            Assert.True(Volatile.Read(ref roots.Calls) >= 1);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop);
        }
        finally
        {
            cts.Cancel();
            try { await loop; } catch (OperationCanceledException) { }
        }
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition())
        {
            if (Environment.TickCount64 > deadline)
                throw new TimeoutException("condition not met within timeout");
            Thread.Sleep(10);
        }
    }

    private void WriteClaudeTodayAndEarlierThisMonth()
    {
        WriteClaudeFile(@"p1\session.jsonl",
            ClaudeLine("msg_c1", "2026-09-23T10:00:00.000Z", 1000, 100, 0),
            ClaudeLine("msg_c2", "2026-09-23T10:05:00.000Z", 2000, 200, 0),
            ClaudeLine("msg_c3", "2026-09-05T10:00:00.000Z", 500, 50, 0));
    }

    private void WriteCodexSingleSessionToday()
    {
        WriteCodexFile(@"2026\09\23\rollout-2026-09-23T11-00-00-single.jsonl",
            null,
            CodexMetaLine("2026-09-23T11:00:00.000Z", "33333333-3333-4333-8333-333333333333"),
            CodexTokenLine("2026-09-23T11:00:00.000Z", (1000, 0, 100, 1100), (1000, 0, 100, 1100)),
            CodexTokenLine("2026-09-23T11:00:02.000Z", (3000, 0, 300, 3300), (2000, 0, 200, 2200)));
    }

    private void WriteCodexParentChild()
    {
        WriteCodexFile(@"2026\08\10\rollout-2026-08-10T10-00-00-oldpar.jsonl",
            new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
            CodexMetaLine("2026-09-23T11:00:00.000Z", ParentSession),
            CodexTokenLine("2026-09-23T11:00:00.100Z", (1000, 0, 100, 1100), (1000, 0, 100, 1100)),
            CodexTokenLine("2026-09-23T11:00:00.200Z", (2000, 0, 200, 2200), (1000, 0, 100, 1100)));
        WriteCodexFile(@"2026\09\23\rollout-2026-09-23T12-00-00-newchl.jsonl",
            null,
            CodexMetaLine("2026-09-23T12:00:00.000Z", ChildSession, ParentSession),
            CodexMetaLine("2026-09-23T11:00:00.000Z", ParentSession),
            CodexTokenLine("2026-09-23T12:00:00.000Z", (1000, 0, 100, 1100), (1000, 0, 100, 1100)),
            CodexTokenLine("2026-09-23T12:00:02.000Z", (2000, 0, 200, 2200), (1000, 0, 100, 1100)),
            CodexTokenLine("2026-09-23T12:05:00.000Z", (2500, 0, 250, 2750), (500, 0, 50, 550)),
            CodexTokenLine("2026-09-23T12:06:00.000Z", (3100, 0, 310, 3410), (600, 0, 60, 660)));
    }

    private void WriteClaudeFile(string relativePath, params string[] lines)
    {
        var path = Path.Combine(_profile, ".claude", "projects", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
    }

    private void WriteCodexFile(string relativePath, DateTimeOffset? mtimeUtc, params string[] lines)
    {
        var path = Path.Combine(_profile, ".codex", "sessions", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        if (mtimeUtc is { } mtime)
            File.SetLastWriteTimeUtc(path, mtime.UtcDateTime);
    }

    private static string ClaudeLine(string messageId, string timestamp, long input, long output, long cacheRead) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["type"] = "assistant",
            ["timestamp"] = timestamp,
            ["message"] = new Dictionary<string, object?>
            {
                ["id"] = messageId,
                ["type"] = "message",
                ["role"] = "assistant",
                ["model"] = "test-model",
                ["usage"] = new Dictionary<string, object?>
                {
                    ["input_tokens"] = input,
                    ["output_tokens"] = output,
                    ["cache_creation_input_tokens"] = 0,
                    ["cache_read_input_tokens"] = cacheRead,
                },
            },
        });

    private static string CodexMetaLine(string timestamp, string sessionId, string? forkedFrom = null)
    {
        var payload = new Dictionary<string, object?> { ["id"] = sessionId };
        if (forkedFrom is not null) payload["forked_from_id"] = forkedFrom;
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["type"] = "session_meta",
            ["timestamp"] = timestamp,
            ["payload"] = payload,
        });
    }

    private static string CodexTokenLine(
        string timestamp,
        (long Input, long Cached, long Output, long Total) cumulative,
        (long Input, long Cached, long Output, long Total) last) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["timestamp"] = timestamp,
            ["type"] = "event_msg",
            ["payload"] = new Dictionary<string, object?>
            {
                ["type"] = "token_count",
                ["info"] = new Dictionary<string, object?>
                {
                    ["total_token_usage"] = Vector(cumulative),
                    ["last_token_usage"] = Vector(last),
                    ["model_context_window"] = 258400,
                },
            },
        });

    private static Dictionary<string, object?> Vector((long Input, long Cached, long Output, long Total) vector) =>
        new()
        {
            ["input_tokens"] = vector.Input,
            ["cached_input_tokens"] = vector.Cached,
            ["output_tokens"] = vector.Output,
            ["reasoning_output_tokens"] = 0,
            ["total_tokens"] = vector.Total,
        };

    private sealed class CountingRootSource(UsageRootOptions options, ManualResetEventSlim? gate) : IUsageRootSource
    {
        public int Calls;

        public IReadOnlyList<UsageRoot> Discover()
        {
            Interlocked.Increment(ref Calls);
            gate?.Wait();
            return UsageRootDiscovery.Discover(options);
        }
    }

    private sealed class FlakyProbe
    {
        private int _calls;

        public int Calls => _calls;

        public string? Probe(CodexRolloutFile file)
        {
            Interlocked.Increment(ref _calls);
            if (_calls == 1) throw new IOException("transient probe failure");
            return CodexRolloutProbe.ProbeFile(file.Path);
        }
    }
}
