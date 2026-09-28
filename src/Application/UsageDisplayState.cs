namespace PokeTokenBar.Application;

using PokeTokenBar.Core;

public sealed record ProviderUsageSummary(
    string ProviderId,
    string DisplayName,
    bool Available,
    long TodayTokens,
    double TodayCost,
    long MonthTokens,
    double MonthCost);

public sealed record UsageDisplayState(
    DateTimeOffset AsOfUtc,
    IReadOnlyList<ProviderUsageSummary> Providers,
    long TodayTokens,
    double TodayCost,
    long MonthTokens,
    double MonthCost,
    IReadOnlyList<DailyUsage>? MonthDaily = null,
    IReadOnlyDictionary<string, long>? TodayModels = null)
{
    public ProviderUsageSummary? Provider(string providerId) =>
        Providers.FirstOrDefault(provider => provider.ProviderId == providerId);
}
