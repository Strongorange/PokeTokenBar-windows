namespace PokeTokenBar.Application;

using PokeTokenBar.Core;

public sealed record ProviderUsageSummary(
    string ProviderId,
    string DisplayName,
    bool Available,
    long TodayTokens,
    double TodayCost,
    long MonthTokens,
    double MonthCost,
    long TodayInputTokens = 0,
    long TodayOutputTokens = 0,
    long TodayCacheWriteTokens = 0,
    long TodayCacheReadTokens = 0,
    long WeekTokens = 0,
    double WeekCost = 0,
    CostCoverage TodayCostCoverage = default,
    CostCoverage WeekCostCoverage = default,
    IReadOnlyDictionary<string, long>? TodayModels = null);

public sealed record UsageDisplayState(
    DateTimeOffset AsOfUtc,
    IReadOnlyList<ProviderUsageSummary> Providers,
    long TodayTokens,
    double TodayCost,
    long MonthTokens,
    double MonthCost,
    IReadOnlyList<DailyUsage>? MonthDaily = null,
    IReadOnlyDictionary<string, long>? TodayModels = null,
    long WeekTokens = 0,
    double WeekCost = 0,
    CostCoverage TodayCostCoverage = default,
    CostCoverage WeekCostCoverage = default,
    CostCoverage MonthCostCoverage = default)
{
    public ProviderUsageSummary? Provider(string providerId) =>
        Providers.FirstOrDefault(provider => provider.ProviderId == providerId);
}
