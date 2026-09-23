using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows.Tests;

[Collection("Platform diagnostics")]
public class UsageScanCacheTests : IDisposable
{
    private readonly string _dir;

    public UsageScanCacheTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-cache-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string CachePath => Path.Combine(_dir, "usage-cache.json");

    private static FileFingerprint Fingerprint(DateTimeOffset mtime, long size) => new(mtime, size);

    [Fact]
    public void SignatureMatchHitsAndMismatchMisses()
    {
        var cache = new UsageScanCache<string>(CachePath, parserVersion: 1);
        var stamp = DateTimeOffset.UtcNow;
        cache.Put(@"C:\logs\a.jsonl", Fingerprint(stamp, 100), "payload");
        Assert.True(cache.TryGet(@"C:\logs\a.jsonl", Fingerprint(stamp, 100), out var hit));
        Assert.Equal("payload", hit);
        Assert.False(cache.TryGet(@"C:\logs\a.jsonl", Fingerprint(stamp.AddSeconds(1), 100), out _));
        Assert.False(cache.TryGet(@"C:\logs\a.jsonl", Fingerprint(stamp, 101), out _));
    }

    [Fact]
    public void SurvivesRestartViaDiskPersistence()
    {
        var stamp = DateTimeOffset.UtcNow;
        var first = new UsageScanCache<int>(CachePath, parserVersion: 1);
        first.Put(@"C:\logs\a.jsonl", Fingerprint(stamp, 10), 42);
        first.Save();
        var second = new UsageScanCache<int>(CachePath, parserVersion: 1);
        Assert.True(second.TryGet(@"C:\logs\a.jsonl", Fingerprint(stamp, 10), out var payload));
        Assert.Equal(42, payload);
    }

    [Fact]
    public void ParserVersionChangeInvalidatesEverything()
    {
        var stamp = DateTimeOffset.UtcNow;
        var first = new UsageScanCache<int>(CachePath, parserVersion: 1);
        first.Put(@"C:\logs\a.jsonl", Fingerprint(stamp, 10), 42);
        first.Save();
        Assert.Equal(1, first.EntryCount);
        var second = new UsageScanCache<int>(CachePath, parserVersion: 2);
        Assert.Equal(0, second.EntryCount);
    }

    [Fact]
    public void CorruptCacheFileStartsEmptyWithoutThrowing()
    {
        File.WriteAllText(CachePath, "{ not json !!!");
        var cache = new UsageScanCache<int>(CachePath, parserVersion: 1);
        Assert.Equal(0, cache.EntryCount);
        Assert.Contains("usage cache load failed", Diagnostics.Read(_dir));
    }

    [Fact]
    public void PrunesBlobsOlderThanRetentionOnSave()
    {
        var now = DateTimeOffset.UtcNow;
        var cache = new UsageScanCache<int>(CachePath, parserVersion: 1);
        cache.Put(@"C:\logs\old.jsonl", Fingerprint(now - UsageScanCache<int>.BlobRetention - TimeSpan.FromDays(1), 10), 1);
        cache.Put(@"C:\logs\fresh.jsonl", Fingerprint(now, 10), 2);
        cache.Save(now);
        Assert.Equal(1, cache.EntryCount);
        Assert.True(cache.TryGet(@"C:\logs\fresh.jsonl", Fingerprint(now, 10), out _));
    }

    [Fact]
    public void SaveWritesNoFileWhenNothingChanged()
    {
        var cache = new UsageScanCache<int>(CachePath, parserVersion: 1);
        cache.Save();
        Assert.False(File.Exists(CachePath));
    }

    [Fact]
    public void TryGetAnyReturnsEntryWithAnySignature()
    {
        var stamp = DateTimeOffset.UtcNow;
        var cache = new UsageScanCache<string>(CachePath, parserVersion: 1);
        cache.Put(@"C:\logs\a.jsonl", Fingerprint(stamp, 100), "old");
        Assert.True(cache.TryGetAny(@"C:\logs\a.jsonl", out var stale));
        Assert.Equal("old", stale);
        Assert.False(cache.TryGetAny(@"C:\logs\missing.jsonl", out _));
    }
}
