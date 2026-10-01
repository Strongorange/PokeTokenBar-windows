using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public enum GameBadgeKind
{
    GrowthBoost,
    EggGuarantee
}

public sealed record GameBadge(string Text, GameBadgeKind Kind, Rarity? Rarity = null);

/// <summary>
/// Pure presentation decisions for the game tab companion header and the
/// dex grid / catch log, kept free of UI dependencies so they get unit
/// coverage (user principle: every unit modular and individually testable).
/// </summary>
public static class GameTabPresentation
{
    public static string HeaderName(CompanionGameView view) =>
        (view.HasActive ? view.ActiveName : DashboardText.TokenEgg(view.Language))
        + (view.HasActive && view.IsShiny ? " ✨" : "");

    public static string? ShinyTooltip(CompanionGameView view) =>
        view.HasActive && view.IsShiny
            ? "✨ " + DashboardText.ShinyLabel(view.Language)
            : null;

    public static string RarityCapsuleLabel(Rarity rarity, AppLanguage lang) =>
        DashboardText.RarityLabel(lang, rarity).ToUpperInvariant();

    public static bool EggImminent(CompanionGameView view) => view.EggProgress >= 0.9;

    public static bool IsFinalStage(CompanionGameView view) =>
        view.TotalForms > 0 && view.StageIndex + 1 >= view.TotalForms;

    public static string DetailLine(CompanionGameView view, string raisingNature)
    {
        var lang = view.Language;
        if (!view.HasActive)
            return EggImminent(view)
                ? DashboardText.EggImminent(lang)
                : DashboardText.EggIncubating(lang);
        var stageCaption = IsFinalStage(view)
            ? DashboardText.FinalForm(lang)
            : DashboardText.StageLabel(lang, view.StageIndex + 1, view.TotalForms);
        return raisingNature.Length > 0 ? $"{stageCaption} · {raisingNature}" : stageCaption;
    }

    public static string ProgressCaption(CompanionGameView view)
    {
        var lang = view.Language;
        if (!view.HasActive)
            return DashboardText.EggToHatch(lang,
                TokenFormatter.Grouped(Math.Max(0, view.EggThreshold - view.EggUsed)));
        var remaining = Math.Max(0, view.StageThreshold - view.StageUsed);
        return IsFinalStage(view)
            ? DashboardText.ToGraduation(lang, TokenFormatter.Grouped(remaining))
            : DashboardText.ToNextEvolution(lang, TokenFormatter.Grouped(remaining));
    }

    public static double ProgressValue(CompanionGameView view) =>
        view.HasActive ? view.StageProgress : view.EggProgress;

    public static GameBadge? StatusBadge(CompanionGameView view)
    {
        var lang = view.Language;
        if (view.HasActive)
            return view.HasGrowthBoost
                ? new GameBadge(
                    DashboardText.GrowthBoost(lang,
                        (int) Math.Round((double) PokemonBalance.RepeatGrowthMultiplier)),
                    GameBadgeKind.GrowthBoost)
                : null;
        if (view.EggGuarantee is { } guarantee)
            return new GameBadge(DashboardText.EggGuaranteeHint(lang, guarantee),
                GameBadgeKind.EggGuarantee, guarantee);
        return null;
    }

    public static string RaisingNature(CompanionDetailSnapshot? detail)
    {
        if (detail is null) return "";
        var raising = detail.Individuals.FirstOrDefault(individual => individual.IsRaising);
        return raising is not null && raising.Nature.Length > 0 ? raising.Nature : "";
    }

    public static string CombatLine(CompanionGameView view, CompanionDetailSnapshot? detail)
    {
        if (!view.HasActive || detail is null) return "";
        var lang = view.Language;
        var raising = detail.Individuals.FirstOrDefault(individual => individual.IsRaising);
        var parts = new List<string>();
        if (raising is not null)
        {
            parts.Add($"Lv. {raising.Level}");
            if (raising.Gender.Length > 0) parts.Add(raising.Gender);
            if (raising.Nature.Length > 0) parts.Add(raising.Nature);
            if (raising.Ability.Length > 0)
                parts.Add(raising.Ability + (raising.AbilityIsHidden
                    ? $" ({DashboardText.HiddenMark(lang)})" : ""));
        }
        if (detail.Types.Count > 0) parts.Add(string.Join("/", detail.Types));
        parts.Add($"{DashboardText.BaseTotalLabel(lang)} {detail.BaseStatTotal}");
        return string.Join(" · ", parts);
    }

    public static string DexHeader(CompanionGameView view) =>
        $"{DashboardText.DexTitle(view.Language)}: " +
        $"{DashboardText.DexSpeciesCount(view.Language, view.DexCount)} · " +
        $"{DashboardText.WalletLabel(view.Language)} {TokenFormatter.Grouped(view.AvailableTokens)} · " +
        DashboardText.DetailHint(view.Language);

    public static string CatchHeader(CompanionGameView view) =>
        $"{DashboardText.CatchLogTitle(view.Language)} · " +
        DashboardText.DexTotalCount(view.Language, view.CatchRows.Count);

    public static string TileTooltip(CompanionDexRow row, int unownCollected,
        bool isRepresentative, AppLanguage lang)
    {
        var star = row.IsShiny ? " ✨" : "";
        var raising = row.IsRaising ? $"  ← {DashboardText.RaisingLabel(lang)}" : "";
        var unownForms = unownCollected > 0
            ? $" · {DashboardText.UnownFormsCollected(lang, unownCollected)}" : "";
        var representative = isRepresentative ? $" · {DashboardText.RepresentativeBadge(lang)}" : "";
        return $"#{row.SpeciesID} {row.Name}{star} · " +
               $"{DashboardText.RarityLabel(lang, row.Rarity)}{raising}{unownForms}{representative}";
    }

    public static string TileName(CompanionDexRow row, int unownCollected) =>
        (row.IsRaising ? "← " : "") + row.Name +
        (unownCollected > 0 ? $" {unownCollected}/{UnownForms.All.Length}" : "");

    /// Port of the macOS dex footer line: the tile only shows number/sprite/name,
    /// so rarity is the info gained by selecting (CompanionView.footer).
    public static string DexFooterInfo(CompanionDexRow row, AppLanguage lang) =>
        $"#{row.SpeciesID} {row.Name} · {DashboardText.RarityLabel(lang, row.Rarity)}";

    /// Unown needs a form choice before it can represent, so the footer star
    /// stays hidden and the form picker (detail window) handles it instead.
    public static bool DexFooterCanSetRepresentative(CompanionDexRow row) =>
        row.SpeciesID != UnownForms.SpeciesID;

    public static string CaughtAgo(DateTimeOffset? caughtAt, DateTimeOffset now, AppLanguage lang)
    {
        if (caughtAt is not { } at) return "";
        return DashboardText.CaughtAgo(lang, RelativeTimes.Bucket(at, now),
            RelativeTimes.BucketValue(at, now));
    }
}
