using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

/// <summary>
/// Pure presentation decisions shared by the dashboard's dex grid and the
/// catch log, extracted from DashboardWindow code-behind so they get unit
/// coverage (user principle: every unit modular and individually testable).
/// Colors are hex WITHOUT the leading '#' — the UI's alpha-tint brush helper
/// concatenates the '#'-prefixed alpha channel onto them.
/// </summary>
public static class CompanionPresentation
{
    public static readonly Rarity[] RarityDisplayOrder =
        [Rarity.Legendary, Rarity.Rare, Rarity.Uncommon, Rarity.Common];

    public static string RarityHex(Rarity rarity) => rarity switch
    {
        Rarity.Legendary => "F7630C",
        Rarity.Rare => "0078D4",
        Rarity.Uncommon => "107C10",
        _ => "8A8A8A"
    };

    public static IReadOnlyList<CompanionDexRow> VisibleDexRows(
        IReadOnlyList<CompanionDexRow> rows, Rarity? filter) =>
        filter is { } rarity
            ? rows.Where(row => row.Rarity == rarity).ToList()
            : rows;

    public static IReadOnlyList<CompanionCatchRow> VisibleCatchRows(
        IReadOnlyList<CompanionCatchRow> rows, Rarity? filter) =>
        filter is { } rarity
            ? rows.Where(row => row.Rarity == rarity).ToList()
            : rows;

    public static int RarityTally<T>(IReadOnlyList<T> rows, Rarity rarity, Func<T, Rarity> rarityOf) =>
        rows.Count(row => rarityOf(row) == rarity);

    /// <summary>
    /// Provider chip selection: keep the current pick while it still has
    /// usage, otherwise fall back to the first provider with usage.
    /// </summary>
    public static string? ResolveSelectedProvider(
        IReadOnlyList<ProviderUsageSummary> selectable, string? current) =>
        selectable.All(provider => provider.ProviderId != current)
            ? selectable.FirstOrDefault()?.ProviderId
            : current;
}
