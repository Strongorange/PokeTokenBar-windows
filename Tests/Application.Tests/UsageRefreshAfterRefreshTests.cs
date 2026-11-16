using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application.Tests;

public class UsageRefreshAfterRefreshTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ptb-after-refresh-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private sealed class NoRoots : IUsageRootSource
    {
        public IReadOnlyList<UsageRoot> Discover() => [];
    }

    private UsageRefreshService Service(ProviderRefreshDelegate refresh, Action afterRefresh) =>
        new(new UsageRefreshOptions
        {
            CacheDirectory = _dir,
            TimeZone = TimeZoneInfo.Utc,
            RootSource = new NoRoots(),
            Providers = [new ProviderRegistration("fixture", "Fixture", new HashSet<UsageRootKind>(), refresh)],
            AfterRefresh = afterRefresh,
        });

    [Fact]
    public async Task AfterRefreshRunsOnceForEachSuccessfulRefresh()
    {
        var calls = 0;
        var service = Service(
            (_, _, _) => ProviderRefreshOutcome.From(true, new UsageProviderSnapshot("fixture", "Fixture", [])),
            () => calls++);

        await service.RefreshAsync();
        await service.RefreshAsync();

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task AfterRefreshDoesNotRunWhenTheRefreshFails()
    {
        var calls = 0;
        var service = Service((_, _, _) => throw new InvalidOperationException("boom"), () => calls++);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshAsync());

        Assert.Equal(0, calls);
    }
}
