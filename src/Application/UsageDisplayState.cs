namespace PokeTokenBar.Application;

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
    double MonthCost)
{
    public ProviderUsageSummary? Provider(string providerId) =>
        Providers.FirstOrDefault(provider => provider.ProviderId == providerId);
}
