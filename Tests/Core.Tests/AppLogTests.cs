using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class AppLogTests : IDisposable
{
    private readonly string _dir;

    public AppLogTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-applog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string LogPath => Path.Combine(_dir, "PokeTokenBar.log");

    [Fact]
    public void WriteAndFlushLandsLineInFile()
    {
        AppLog.Configure(LogPath);
        AppLog.WriteAndFlush("hello diagnostics");
        var content = File.ReadAllText(LogPath);
        Assert.Contains("hello diagnostics", content);
        Assert.StartsWith("[", content);
        Assert.EndsWith("\n", content);
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z\]", content);
    }

    [Fact]
    public void WriteIsAsynchronousButFlushWaitsForDrain()
    {
        AppLog.Configure(LogPath);
        for (var i = 0; i < 100; i++) AppLog.Write($"line {i}");
        AppLog.Flush();
        var lines = File.ReadAllLines(LogPath);
        Assert.Equal(100, lines.Length);
        Assert.Contains("line 99", lines[^1]);
    }

    [Fact]
    public void RotationMovesOverflowToOldGeneration()
    {
        AppLog.Configure(LogPath, maxBytes: 200);
        var payload = new string('x', 120);
        AppLog.WriteAndFlush(payload);
        AppLog.WriteAndFlush(payload);
        AppLog.WriteAndFlush(payload);
        var old = Path.Combine(_dir, "PokeTokenBar.old.log");
        Assert.True(File.Exists(old), "rotated generation should exist");
        Assert.True(File.Exists(LogPath), "active log should exist after rotation");
        var oldSize = new FileInfo(old).Length;
        Assert.True(oldSize > 200, $"old generation should hold the overflow, got {oldSize}");
    }

    [Fact]
    public void ConfigureSwitchesDestination()
    {
        var second = Path.Combine(_dir, "second.log");
        AppLog.Configure(LogPath);
        AppLog.WriteAndFlush("first destination");
        AppLog.Configure(second);
        AppLog.WriteAndFlush("second destination");
        Assert.Contains("first destination", File.ReadAllText(LogPath));
        Assert.Contains("second destination", File.ReadAllText(second));
        Assert.DoesNotContain("second destination", File.ReadAllText(LogPath));
    }
}
