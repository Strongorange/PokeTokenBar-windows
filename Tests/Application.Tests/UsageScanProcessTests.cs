using System.Text;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application.Tests;

public class UsageScanProcessTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T09:00:00Z");
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ptb-scan-process-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private sealed class NoRoots : IUsageRootSource
    {
        public IReadOnlyList<UsageRoot> Discover() => [];
    }

    private static UsageEntry Entry(string id, string model, long input, long output, DateTimeOffset date) =>
        new(id, date, UsageAggregation.LocalDay(date, Tz), model, input, output, 10, 20);

    private static readonly UsageEntry[] Entries =
    [
        Entry("a", "claude-sonnet-4-5", 1200, 300, Now),
        Entry("b", "gpt-5.5", 400_000, 900, Now.AddDays(-1)),
        Entry("c", "unknown-model", 50, 5, Now.AddDays(-40)),
    ];

    private UsageRefreshService Service(Func<CancellationToken, UsageDisplayState>? compute, Action onProviderRefresh) =>
        new(new UsageRefreshOptions
        {
            CacheDirectory = _dir,
            TimeZone = Tz,
            Clock = () => Now,
            RootSource = new NoRoots(),
            Compute = compute,
            Providers =
            [
                new ProviderRegistration("fixture", "Fixture", new HashSet<UsageRootKind>(), (_, _, _) =>
                {
                    onProviderRefresh();
                    return ProviderRefreshOutcome.From(true, new UsageProviderSnapshot("fixture", "Fixture", Entries));
                }),
            ],
        });

    private static string Serialize(UsageDisplayState state)
    {
        using var stream = new MemoryStream();
        UsageScanProcess.Write(stream, state);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void StatePrintedByTheScanProcessReadsBackUnchanged()
    {
        var state = Service(null, () => { }).RefreshAll();

        var json = Serialize(state);
        var read = UsageScanProcess.Read(json);

        Assert.Equal(json, Serialize(read));
        Assert.Equal(state.TodayTokens, read.TodayTokens);
        Assert.Equal(state.YearDaily!.Count, read.YearDaily!.Count);
        Assert.Equal(state.TodayModels!.OrderBy(m => m.Key), read.TodayModels!.OrderBy(m => m.Key));
        Assert.Equal(state.MonthCostCoverage, read.MonthCostCoverage);
    }

    [Fact]
    public async Task OutOfProcessResultIsUsedWithoutTouchingTheProviders()
    {
        var providerCalls = 0;
        var expected = Service(null, () => { }).RefreshAll();
        var service = Service(_ => expected, () => providerCalls++);

        var state = await service.RefreshAsync();

        Assert.Same(expected, state);
        Assert.Equal(0, providerCalls);
    }

    [Fact]
    public async Task FailedOutOfProcessRefreshFallsBackToScanningInProcess()
    {
        var providerCalls = 0;
        var service = Service(_ => throw new TimeoutException("scan process did not finish"), () => providerCalls++);

        var state = await service.RefreshAsync();

        Assert.Equal(1, providerCalls);
        Assert.Equal(Entries[0].Total, state.TodayTokens);
    }
}
