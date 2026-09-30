using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

public class GameTabPresentationTests
{
    [Fact]
    public void HeaderNameUsesActiveNameAndMarksShiny()
    {
        Assert.Equal("Speciemon", GameTabPresentation.HeaderName(View(hasActive: true, "Speciemon")));
        Assert.Equal("Speciemon ✨", GameTabPresentation.HeaderName(
            View(hasActive: true, "Speciemon", isShiny: true)));
        Assert.Equal(DashboardText.TokenEgg(AppLanguage.En),
            GameTabPresentation.HeaderName(View(hasActive: false)));
    }

    [Fact]
    public void ShinyTooltipOnlyForActiveShiny()
    {
        Assert.Null(GameTabPresentation.ShinyTooltip(View(hasActive: false)));
        Assert.Null(GameTabPresentation.ShinyTooltip(View(hasActive: true, isShiny: false)));
        Assert.Equal("✨ " + DashboardText.ShinyLabel(AppLanguage.En),
            GameTabPresentation.ShinyTooltip(View(hasActive: true, isShiny: true)));
    }

    [Fact]
    public void RarityCapsuleLabelIsUppercase()
    {
        Assert.Equal("RARE",
            GameTabPresentation.RarityCapsuleLabel(Rarity.Rare, AppLanguage.En));
        Assert.Equal("LEGENDARY",
            GameTabPresentation.RarityCapsuleLabel(Rarity.Legendary, AppLanguage.En));
    }

    [Fact]
    public void DetailLineJoinsStageCaptionAndNature()
    {
        Assert.Equal(
            DashboardText.StageLabel(AppLanguage.En, 1, 3) + " · Jolly",
            GameTabPresentation.DetailLine(View(hasActive: true), "Jolly"));
        Assert.Equal(
            DashboardText.StageLabel(AppLanguage.En, 1, 3),
            GameTabPresentation.DetailLine(View(hasActive: true), ""));
    }

    [Fact]
    public void DetailLineUsesFinalFormOnLastStage()
    {
        Assert.Equal(DashboardText.FinalForm(AppLanguage.En),
            GameTabPresentation.DetailLine(
                View(hasActive: true, stageIndex: 2, totalForms: 3), ""));
    }

    [Fact]
    public void DetailLineForEggFollowsImminence()
    {
        Assert.Equal(DashboardText.EggIncubating(AppLanguage.En),
            GameTabPresentation.DetailLine(View(hasActive: false, eggProgress: 0.5), ""));
        Assert.Equal(DashboardText.EggImminent(AppLanguage.En),
            GameTabPresentation.DetailLine(View(hasActive: false, eggProgress: 0.9), ""));
        Assert.Equal(DashboardText.EggImminent(AppLanguage.En),
            GameTabPresentation.DetailLine(View(hasActive: false, eggProgress: 0.95), ""));
    }

    [Fact]
    public void IsFinalStageRequiresFormsAndLastIndex()
    {
        Assert.True(GameTabPresentation.IsFinalStage(
            View(hasActive: true, stageIndex: 2, totalForms: 3)));
        Assert.False(GameTabPresentation.IsFinalStage(
            View(hasActive: true, stageIndex: 1, totalForms: 3)));
        Assert.False(GameTabPresentation.IsFinalStage(
            View(hasActive: true, stageIndex: 0, totalForms: 0)));
    }

    [Fact]
    public void ProgressCaptionCountsRemainingTokens()
    {
        Assert.Equal(
            DashboardText.ToNextEvolution(AppLanguage.En, TokenFormatter.Grouped(30)),
            GameTabPresentation.ProgressCaption(
                View(hasActive: true, stageUsed: 20, stageThreshold: 50)));
        Assert.Equal(
            DashboardText.ToGraduation(AppLanguage.En, TokenFormatter.Grouped(50)),
            GameTabPresentation.ProgressCaption(
                View(hasActive: true, stageIndex: 2, totalForms: 3,
                    stageUsed: 0, stageThreshold: 50)));
        Assert.Equal(
            DashboardText.EggToHatch(AppLanguage.En, TokenFormatter.Grouped(40)),
            GameTabPresentation.ProgressCaption(
                View(hasActive: false, eggUsed: 60, eggThreshold: 100)));
    }

    [Fact]
    public void ProgressCaptionClampsOveruseToZero()
    {
        Assert.Equal(
            DashboardText.ToNextEvolution(AppLanguage.En, TokenFormatter.Grouped(0)),
            GameTabPresentation.ProgressCaption(
                View(hasActive: true, stageUsed: 80, stageThreshold: 50)));
        Assert.Equal(
            DashboardText.EggToHatch(AppLanguage.En, TokenFormatter.Grouped(0)),
            GameTabPresentation.ProgressCaption(
                View(hasActive: false, eggUsed: 120, eggThreshold: 100)));
    }

    [Fact]
    public void ProgressValuePicksStageOrEgg()
    {
        Assert.Equal(0.25, GameTabPresentation.ProgressValue(
            View(hasActive: true, stageProgress: 0.25, eggProgress: 0.9)));
        Assert.Equal(0.9, GameTabPresentation.ProgressValue(
            View(hasActive: false, stageProgress: 0.25, eggProgress: 0.9)));
    }

    [Fact]
    public void StatusBadgeForGrowthBoost()
    {
        var badge = GameTabPresentation.StatusBadge(
            View(hasActive: true, hasGrowthBoost: true));
        Assert.NotNull(badge);
        Assert.Equal(GameBadgeKind.GrowthBoost, badge.Kind);
        Assert.Null(badge.Rarity);
        Assert.Equal(
            DashboardText.GrowthBoost(AppLanguage.En, 2),
            badge.Text);
    }

    [Fact]
    public void StatusBadgeForEggGuarantee()
    {
        var badge = GameTabPresentation.StatusBadge(
            View(hasActive: false, eggGuarantee: Rarity.Legendary));
        Assert.NotNull(badge);
        Assert.Equal(GameBadgeKind.EggGuarantee, badge.Kind);
        Assert.Equal(Rarity.Legendary, badge.Rarity);
        Assert.Equal(DashboardText.EggGuaranteeHint(AppLanguage.En, Rarity.Legendary),
            badge.Text);
    }

    [Fact]
    public void StatusBadgeAbsentWithoutBoostOrGuarantee()
    {
        Assert.Null(GameTabPresentation.StatusBadge(View(hasActive: true)));
        Assert.Null(GameTabPresentation.StatusBadge(View(hasActive: false)));
    }

    [Fact]
    public void RaisingNatureReadsRaisingIndividual()
    {
        Assert.Equal("", GameTabPresentation.RaisingNature(null));
        Assert.Equal("", GameTabPresentation.RaisingNature(Detail()));
        Assert.Equal("Jolly", GameTabPresentation.RaisingNature(Detail(
            Individual(nature: "Jolly"))));
        Assert.Equal("", GameTabPresentation.RaisingNature(Detail(
            Individual(nature: "", isRaising: true))));
    }

    [Fact]
    public void CombatLineEmptyWithoutActiveOrDetail()
    {
        Assert.Equal("", GameTabPresentation.CombatLine(View(hasActive: false), Detail(
            Individual())));
        Assert.Equal("", GameTabPresentation.CombatLine(View(hasActive: true), null));
        Assert.Equal("", GameTabPresentation.CombatLine(View(hasActive: false), null));
    }

    [Fact]
    public void CombatLineJoinsLevelGenderNatureAbilityTypesAndTotal()
    {
        var detail = Detail(
            Individual(level: 12, gender: "♂", nature: "Jolly", ability: "Overgrow"),
            Individual(isRaising: false));
        Assert.Equal(
            "Lv. 12 · ♂ · Jolly · Overgrow · Grass/Poison · " +
            DashboardText.BaseTotalLabel(AppLanguage.En) + " 318",
            GameTabPresentation.CombatLine(View(hasActive: true), detail));
    }

    [Fact]
    public void CombatLineMarksHiddenAbilityAndOmitsEmptyParts()
    {
        var detail = Detail(Individual(
            level: 5, gender: "", nature: "", ability: "Chlorophyll", abilityHidden: true));
        Assert.Equal(
            "Lv. 5 · Chlorophyll (" + DashboardText.HiddenMark(AppLanguage.En) +
            ") · Grass/Poison · " + DashboardText.BaseTotalLabel(AppLanguage.En) + " 318",
            GameTabPresentation.CombatLine(View(hasActive: true), detail));
    }

    [Fact]
    public void CombatLineWithoutRaisingIndividualSkipsLevelParts()
    {
        var detail = Detail(Individual(isRaising: false));
        Assert.Equal(
            "Grass/Poison · " + DashboardText.BaseTotalLabel(AppLanguage.En) + " 318",
            GameTabPresentation.CombatLine(View(hasActive: true), detail));
    }

    [Fact]
    public void DexHeaderCountsSpeciesAndWallet()
    {
        var header = GameTabPresentation.DexHeader(
            View(hasActive: true, dexCount: 4, availableTokens: 12_345));
        Assert.StartsWith(DashboardText.DexTitle(AppLanguage.En) + ": ", header);
        Assert.Contains(DashboardText.DexSpeciesCount(AppLanguage.En, 4), header);
        Assert.Contains(
            DashboardText.WalletLabel(AppLanguage.En) + " " + TokenFormatter.Grouped(12_345),
            header);
        Assert.EndsWith(DashboardText.DetailHint(AppLanguage.En), header);
    }

    [Fact]
    public void CatchHeaderCountsRows()
    {
        var rows = new List<CompanionCatchRow>
        {
            new("a", 1, Rarity.Common, false, true, false, "", null, null, []),
            new("b", 2, Rarity.Rare, false, false, true, "", null, null, []),
        };
        Assert.Equal(
            DashboardText.CatchLogTitle(AppLanguage.En) + " · " +
            DashboardText.DexTotalCount(AppLanguage.En, 2),
            GameTabPresentation.CatchHeader(View(hasActive: true, catchRows: rows)));
    }

    [Fact]
    public void TileTooltipListsAllBadges()
    {
        var plain = GameTabPresentation.TileTooltip(
            new CompanionDexRow(1, "Speciemon", Rarity.Rare, false, false),
            0, false, AppLanguage.En);
        Assert.Equal(
            "#1 Speciemon · " + DashboardText.RarityLabel(AppLanguage.En, Rarity.Rare),
            plain);

        var full = GameTabPresentation.TileTooltip(
            new CompanionDexRow(201, "Unown", Rarity.Common, true, true),
            5, true, AppLanguage.En);
        Assert.StartsWith("#201 Unown ✨ · ", full);
        Assert.Contains("  ← " + DashboardText.RaisingLabel(AppLanguage.En), full);
        Assert.Contains(" · " + DashboardText.UnownFormsCollected(AppLanguage.En, 5), full);
        Assert.EndsWith(" · " + DashboardText.RepresentativeBadge(AppLanguage.En), full);
    }

    [Fact]
    public void TileNamePrefixesRaisingAndSuffixesUnownCount()
    {
        Assert.Equal("Speciemon", GameTabPresentation.TileName(
            new CompanionDexRow(1, "Speciemon", Rarity.Rare, false, false), 0));
        Assert.Equal("← Speciemon", GameTabPresentation.TileName(
            new CompanionDexRow(1, "Speciemon", Rarity.Rare, false, true), 0));
        Assert.EndsWith($" 3/{UnownForms.All.Length}", GameTabPresentation.TileName(
            new CompanionDexRow(201, "Unown", Rarity.Common, false, false), 3));
    }

    [Fact]
    public void CaughtAgoFormatsBucketOrEmpty()
    {
        var now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("", GameTabPresentation.CaughtAgo(null, now, AppLanguage.En));
        var caught = now - TimeSpan.FromDays(3);
        Assert.Equal(
            DashboardText.CaughtAgo(AppLanguage.En, RelativeTimeBucket.Days, 3),
            GameTabPresentation.CaughtAgo(caught, now, AppLanguage.En));
    }

    private static CompanionDetailIndividual Individual(
        bool isRaising = true, int level = 5, string gender = "", string nature = "",
        string ability = "", bool abilityHidden = false) =>
        new("#1", null, false, isRaising, level, gender, nature, ability, abilityHidden, [], []);

    private static CompanionDetailSnapshot Detail(params CompanionDetailIndividual[] individuals) =>
        new(1, "Speciemon", Rarity.Rare,
            ["Grass", "Poison"], 7, 69, 318, [], [], [],
            individuals.ToList(), [], AppLanguage.En);

    private static CompanionGameView View(
        bool hasActive, string activeName = "Speciemon", bool isShiny = false,
        bool hasGrowthBoost = false, int stageIndex = 0, int totalForms = 3,
        long stageUsed = 0, long stageThreshold = 0, double stageProgress = 0,
        long eggUsed = 0, long eggThreshold = 0, double eggProgress = 0,
        int dexCount = 0, long availableTokens = 0,
        Rarity? eggGuarantee = null,
        IReadOnlyList<CompanionCatchRow>? catchRows = null) =>
        new(hasActive, activeName, hasActive ? Rarity.Rare : null, isShiny, hasGrowthBoost,
            stageIndex, totalForms, stageUsed, stageThreshold, stageProgress,
            !hasActive, eggUsed, eggThreshold, eggProgress,
            dexCount, 0, availableTokens, 1, 1, [], [], [], [], [], [],
            1, null, eggGuarantee, null, false, null,
            CompanionStatusKind.Idle, null, catchRows ?? [], [], AppLanguage.En);
}
