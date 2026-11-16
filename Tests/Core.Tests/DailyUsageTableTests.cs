using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class DailyUsageTableTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T12:00:00Z");

    private static UsageEntry Entry(string localDay, string model, long input, long output, long cacheRead = 0) =>
        new($"{localDay}|{model}|{input}", DateTimeOffset.Parse(localDay + "T01:00:00Z"), localDay, model, input, output, 0, cacheRead);

    private static readonly UsageEntry[] Claude =
    [
        Entry("2026-11-01", "claude-sonnet-4-5", 1200, 300, 5000),
        Entry("2026-11-15", "claude-sonnet-4-5", 300_000, 900),
        Entry("2026-11-16", "claude-opus-4-5", 800, 40),
    ];

    private static readonly UsageEntry[] Codex =
    [
        Entry("2026-11-15", "gpt-5", 4000, 700, 1000),
        Entry("2026-11-16", "gpt-5", 100, 10),
        Entry("2026-11-16", "claude-opus-4-5", 5, 0),
    ];

    [Fact]
    public void MergedProviderTablesMatchOneTableBuiltFromAllEntries()
    {
        var merged = new DailyUsageTable();
        merged.AddRange(DailyUsageTable.From(Claude));
        merged.AddRange(DailyUsageTable.From(Codex));
        var direct = DailyUsageTable.From([.. Claude, .. Codex]);

        foreach (var day in new[] { "2026-11-01", "2026-11-15", "2026-11-16" })
        {
            var a = merged.Daily(day, includeModels: true)!;
            var b = direct.Daily(day, includeModels: true)!;
            Assert.Equal(b.TotalTokens, a.TotalTokens);
            Assert.Equal(b.TotalCost, a.TotalCost, 9);
            Assert.Equal(b.CostCoverage, a.CostCoverage);
            Assert.Equal(b.Models!.OrderBy(m => m.Key), a.Models!.OrderBy(m => m.Key));
        }
        Assert.Equal(direct.YearDailySeries(Now, Utc).Select(d => d.TotalTokens), merged.YearDailySeries(Now, Utc).Select(d => d.TotalTokens));
    }

    [Fact]
    public void PeriodMatchesTheEntryBasedPeriod()
    {
        var entries = (UsageEntry[])[.. Claude, .. Codex];
        var fromTable = DailyUsageTable.From(entries).Period("2026-11", "2026-11-01", "2026-11-16");
        var fromEntries = UsageAggregation.Period(entries, "2026-11", "2026-11-01", "2026-11-16");
        Assert.Equal(fromEntries.TotalTokens, fromTable.TotalTokens);
        Assert.Equal(fromEntries.TotalCost, fromTable.TotalCost, 9);
        Assert.Equal(fromEntries.CostCoverage, fromTable.CostCoverage);
    }

    [Fact]
    public void LongContextRequestKeepsItsOwnPriceInsideADayTotal()
    {
        // 272k 넘는 프롬프트만 장문 단가라, 같은 날 토큰을 먼저 합친 뒤 계산하면 짧은 요청까지 비싸짐
        var day = DailyUsageTable.From([Entry("2026-11-15", "gpt-5.5", 300_000, 900), Entry("2026-11-15", "gpt-5.5", 1_000, 50)])
            .Daily("2026-11-15")!;
        var perRequest = ModelPricing.Cost("gpt-5.5", 300_000, 900, 0, 0) + ModelPricing.Cost("gpt-5.5", 1_000, 50, 0, 0);
        var summedFirst = ModelPricing.Cost("gpt-5.5", 301_000, 950, 0, 0);
        Assert.Equal(perRequest, day.TotalCost, 9);
        Assert.NotEqual(summedFirst, day.TotalCost, 9);
    }

    [Fact]
    public void DailyModelsAreACopyOfTheTableState()
    {
        var table = DailyUsageTable.From(Claude);
        var models = table.Daily("2026-11-16", includeModels: true)!.Models!;
        models["claude-opus-4-5"] = 0;
        Assert.Equal(840, table.Daily("2026-11-16", includeModels: true)!.Models!["claude-opus-4-5"]);
    }
}
