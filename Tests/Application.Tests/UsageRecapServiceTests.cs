using System.Text.Json;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application.Tests;

/// <summary>
/// Ledger feeding on the refresh path and the RecapQuery view model (#332/#367/#368).
/// </summary>
public class UsageRecapServiceTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir;
    private readonly string _cacheDir;

    public UsageRecapServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-recap-tests", Guid.NewGuid().ToString("N"));
        _cacheDir = Path.Combine(_dir, "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private UsageRefreshService BuildService(IReadOnlyList<UsageEntry> entries) =>
        new(new UsageRefreshOptions
        {
            CacheDirectory = _cacheDir,
            TimeZone = Tz,
            Clock = () => Now,
            RootSource = new StaticRootSource(),
            Providers =
            [
                new ProviderRegistration("fixture", "Fixture",
                    new HashSet<UsageRootKind>(),
                    (now, timeZone, roots) => new ProviderRefreshOutcome(true,
                        new UsageProviderSnapshot("fixture", "Fixture", entries))),
            ],
        });

    private sealed class StaticRootSource : IUsageRootSource
    {
        public IReadOnlyList<UsageRoot> Discover() => [];
    }

    private static UsageEntry Entry(string id, long total, DateTimeOffset date) => new(
        id, date, UsageAggregation.LocalDay(date, Tz), "fixture-model",
        total, 0, 0, 0, null);

    [Fact]
    public async Task TheFirstRefreshAlreadyFillsTheLedger()
    {
        var service = BuildService(
        [
            Entry("a", 1_000, Now),
            Entry("b", 500, Now.AddDays(-1)),
        ]);

        await service.RefreshAsync();

        var ledger = service.LedgerCopy();
        Assert.Equal(1_000, ledger.Tokens("2026-09-23"));
        Assert.Equal(500, ledger.Tokens("2026-09-22"));
        // The year series enumerates from the grid start ~53 weeks back, so coverage reaches
        // the whole scan window at once and in-window zeros are real zeros.
        var expected = UsageAggregation.YearGridStart(new DateTime(2026, 9, 23))
            .ToString("yyyy-MM-dd");
        Assert.Equal(expected, ledger.CoveredSince);
        Assert.True(File.Exists(UsageLedgerFile.PathFor(_cacheDir)));
    }

    [Fact]
    public async Task ARefreshKeepsLastYearAndDropsTheYearBefore()
    {
        File.WriteAllText(UsageLedgerFile.PathFor(_cacheDir), """
            {"coveredSince": "2024-06-01",
             "tokensByDay": {"2024-06-15": 100, "2025-01-01": 200, "2026-09-23": 300}}
            """);
        var service = BuildService([]);

        await service.RefreshAsync();

        var ledger = service.LedgerCopy();
        Assert.DoesNotContain("2024-06-15", ledger.TokensByDay);
        Assert.Equal(200, ledger.Tokens("2025-01-01"));
        Assert.Equal(300, ledger.Tokens("2026-09-23"));
    }

    [Fact]
    public async Task AnUnchangedLedgerIsNotRewritten()
    {
        var service = BuildService([Entry("a", 1_000, Now)]);
        await service.RefreshAsync();
        var path = UsageLedgerFile.PathFor(_cacheDir);
        var firstWrite = File.GetLastWriteTimeUtc(path);

        await service.RefreshAsync();

        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task CorruptLedgerFileStartsFreshWithoutThrowing()
    {
        File.WriteAllText(UsageLedgerFile.PathFor(_cacheDir), "{ not json");

        var service = BuildService([Entry("a", 1_000, Now)]);
        await service.RefreshAsync();

        var ledger = service.LedgerCopy();
        Assert.Equal(1_000, ledger.Tokens("2026-09-23"));
        Assert.True(File.Exists(UsageLedgerFile.PathFor(_cacheDir) + ".corrupt"));
    }

    [Fact]
    public void RecapViewCarriesPeriodTitleBucketsAndToolTips()
    {
        var ledger = new UsageLedger { CoveredSince = "2026-09-21" };
        ledger.TokensByDay["2026-09-21"] = 12_345;
        var view = RecapQuery.Build(ledger, [], RecapScope.Week, 0, Now, Tz, AppLanguage.En);

        Assert.Equal("September 20 – September 26", view.Title);
        Assert.Equal(7, view.Buckets.Count);
        Assert.Equal("12,345", view.TotalText);
        Assert.Contains(view.Buckets, bucket => bucket.Label == "M");

        var used = view.Buckets.Single(bucket => bucket.Tokens == 12_345);
        Assert.EndsWith(", 12,345", used.ToolTip);
        // Today (September 23) is covered with no usage: a real zero, not "no data".
        var zero = view.Buckets.Single(bucket =>
            bucket.ToolTip.StartsWith("September 23, ", StringComparison.Ordinal));
        Assert.True(zero.HasData);
        Assert.EndsWith(", 0", zero.ToolTip);
        // Sunday, before coverage started, is unknown — "no data", not a fake zero.
        var missing = view.Buckets.Single(bucket =>
            bucket.ToolTip.StartsWith("September 20, ", StringComparison.Ordinal));
        Assert.EndsWith(", no data", missing.ToolTip);
    }

    [Fact]
    public void RecapViewComparesSameDaysAndLabelsCurrentPeriod()
    {
        var ledger = new UsageLedger { CoveredSince = "2026-09-13" };
        ledger.TokensByDay["2026-09-21"] = 600;
        ledger.TokensByDay["2026-09-22"] = 400;
        ledger.TokensByDay["2026-09-16"] = 100;

        var view = RecapQuery.Build(ledger, [], RecapScope.Week, 0, Now, Tz, AppLanguage.En);

        Assert.NotNull(view.CompareNote);
        Assert.Contains("same days last week", view.CompareNote);
        Assert.Equal("This week", view.CurrentPeriodLabel);
        Assert.True(view.IsInProgress);
        // offset never goes positive: the future has no usage.
        Assert.Equal(0, view.Offset);
    }

    [Fact]
    public void RecapViewGraduatesNameFromTheDex()
    {
        var ledger = new UsageLedger { CoveredSince = "2026-09-20" };
        var entry = new DexEntry(1, 3, [1, 2, 3], Rarity.Common,
            new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero), isShiny: true,
            names: new Dictionary<int, Dictionary<string, string>>
                { [3] = new() { ["en"] = "Venusaur", ["ko"] = "이상해꽃" } });
        var view = RecapQuery.Build(ledger, [entry], RecapScope.Week, 0, Now, Tz, AppLanguage.Ko);

        var graduate = Assert.Single(view.Graduates);
        Assert.Equal("이상해꽃", graduate.Name);
        Assert.Equal(3, graduate.SpeciesID);
        Assert.True(graduate.IsShiny);
        Assert.Null(graduate.Form);
    }

    [Fact]
    public void RecapViewYearScopeLabelsMonths()
    {
        var view = RecapQuery.Build(new UsageLedger(), [], RecapScope.Year, 0, Now, Tz, AppLanguage.En);
        Assert.Equal(12, view.Buckets.Count);
        Assert.Equal("Jan", view.Buckets[0].Label);
        Assert.Equal("2026", view.Title);
    }
}
