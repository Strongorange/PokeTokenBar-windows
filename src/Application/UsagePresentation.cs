using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

/// <summary>
/// Pure presentation decisions for the usage home (provider chips, model
/// rows), extracted from DashboardWindow code-behind so they get unit
/// coverage (user principle: every unit modular and individually testable).
/// </summary>
public static class UsagePresentation
{
    public static IReadOnlyList<ProviderUsageSummary> SelectableProviders(
        IReadOnlyList<ProviderUsageSummary> providers) =>
        providers.Where(provider => provider.Available
            && (provider.TodayTokens > 0 || provider.WeekTokens > 0
                || provider.MonthTokens > 0)).ToList();

    public static string UnavailableNote(
        IReadOnlyList<ProviderUsageSummary> providers, AppLanguage lang)
    {
        var unavailable = providers.Where(provider => !provider.Available).ToList();
        return unavailable.Count == 0
            ? ""
            : string.Join(" · ", unavailable.Select(provider =>
                $"{provider.DisplayName} {DashboardText.ProviderNotFound(lang)}"));
    }

    public static IReadOnlyList<KeyValuePair<string, long>> OrderedModels(
        IReadOnlyDictionary<string, long>? models) =>
        models is null
            ? []
            : models.OrderByDescending(model => model.Value).ToList();

    public static string ModelShortName(string model) =>
        model.Contains('/') ? model[(model.LastIndexOf('/') + 1)..] : model;
}
