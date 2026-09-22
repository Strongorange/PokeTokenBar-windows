using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class GameBalanceTests
{
    [Fact]
    public void GraduationTotalsByRarity()
    {
        Assert.Equal(750_000_000, PokemonBalance.GraduationTotal(Rarity.Common));
        Assert.Equal(1_875_000_000, PokemonBalance.GraduationTotal(Rarity.Uncommon));
        Assert.Equal(3_000_000_000, PokemonBalance.GraduationTotal(Rarity.Rare));
        Assert.Equal(6_000_000_000, PokemonBalance.GraduationTotal(Rarity.Legendary));
    }

    [Fact]
    public void PhaseThresholdSingleFormIsGraduationTotal()
    {
        Assert.Equal(750_000_000, PokemonBalance.PhaseThreshold(Rarity.Common, 1, 0));
    }

    [Fact]
    public void PhaseThresholdScalesByStageOverTriangularDenominator()
    {
        Assert.Equal(250_000_000, PokemonBalance.PhaseThreshold(Rarity.Common, 2, 0));
        Assert.Equal(500_000_000, PokemonBalance.PhaseThreshold(Rarity.Common, 2, 1));
    }

    [Fact]
    public void PhaseThresholdRoundsHalfAwayFromZero()
    {
        Assert.Equal(187_500_000, PokemonBalance.PhaseThreshold(Rarity.Uncommon, 4, 0));
        Assert.Equal(375_000_000, PokemonBalance.PhaseThreshold(Rarity.Uncommon, 4, 1));
        Assert.Equal(562_500_000, PokemonBalance.PhaseThreshold(Rarity.Uncommon, 4, 2));
        Assert.Equal(750_000_000, PokemonBalance.PhaseThreshold(Rarity.Uncommon, 4, 3));
    }

    [Fact]
    public void PhaseThresholdGrowthDivisionRoundsHalfAwayFromZero()
    {
        var standard = PokemonBalance.PhaseThreshold(Rarity.Common, 6, 1);
        Assert.Equal(71_428_571, standard);
        Assert.Equal(35_714_286, PokemonBalance.PhaseThreshold(Rarity.Common, 6, 1,
            PokemonBalance.RepeatGrowthMultiplier));
    }

    [Fact]
    public void PhaseThresholdAppliesGrowthMultiplier()
    {
        Assert.Equal(375_000_000, PokemonBalance.PhaseThreshold(Rarity.Common, 1, 0,
            PokemonBalance.RepeatGrowthMultiplier));
    }

    [Fact]
    public void RarityClassificationFromCaptureRate()
    {
        Assert.Equal(Rarity.Legendary, Rarities.From(3, true, false));
        Assert.Equal(Rarity.Legendary, Rarities.From(3, false, true));
        Assert.Equal(Rarity.Rare, Rarities.From(45, false, false));
        Assert.Equal(Rarity.Uncommon, Rarities.From(120, false, false));
        Assert.Equal(Rarity.Common, Rarities.From(255, false, false));
        Assert.Equal(0, Rarity.Common.SortRank());
        Assert.Equal(3, Rarity.Legendary.SortRank());
    }

    [Fact]
    public void DifficultyClampsOutOfRangeValues()
    {
        Assert.Equal(1.0, PokemonBalance.ClampDifficulty(double.NaN));
        Assert.Equal(0.1, PokemonBalance.ClampDifficulty(-5));
        Assert.Equal(2.0, PokemonBalance.ClampDifficulty(100));
    }

    [Fact]
    public void DifficultyPositionRoundTrips()
    {
        Assert.Equal(0.0, PokemonBalance.DifficultyPosition(0.1), 12);
        Assert.Equal(1.0, PokemonBalance.DifficultyPosition(2.0), 12);
        var mid = PokemonBalance.DifficultyAtPosition(0.5);
        Assert.Equal(0.45, mid, 12);
        Assert.Equal(0.5, PokemonBalance.DifficultyPosition(mid), 2);
    }

    [Fact]
    public void DifficultySnapsNearDefault()
    {
        var defaultPosition = PokemonBalance.DifficultyPosition(1.0);
        Assert.Equal(1.0, PokemonBalance.DifficultyAtPosition(defaultPosition + 0.005));
    }

    [Fact]
    public void SnapDifficultyKeepsTwoSignificantDigits()
    {
        Assert.Equal(0.45, PokemonBalance.SnapDifficulty(0.4472136), 12);
        Assert.Equal(1.2, PokemonBalance.SnapDifficulty(1.234), 12);
        Assert.Equal(0.1, PokemonBalance.SnapDifficulty(0));
    }

    [Fact]
    public void ScaledAppliesClampedDifficulty()
    {
        Assert.Equal(1_500_000_000, PokemonBalance.Scaled(750_000_000, 2.0));
        Assert.Equal(375_000_000, PokemonBalance.Scaled(750_000_000, 0.5));
        Assert.Equal(750_000_000, PokemonBalance.Scaled(750_000_000, double.PositiveInfinity));
    }

    [Fact]
    public void FreshEggPricesFollowGraduationRatios()
    {
        Assert.Equal(1_000_000_000, FreshEggs.PriceGuaranteeing(null));
        Assert.Equal(2_500_000_000, FreshEggs.PriceGuaranteeing(Rarity.Uncommon));
        Assert.Equal(4_000_000_000, FreshEggs.PriceGuaranteeing(Rarity.Rare));
    }

    [Fact]
    public void ShopEntryPrices()
    {
        Assert.Equal(500_000_000, ShopEntry.FromItem(ItemKind.RareCandy).Price);
        Assert.Equal(100_000_000, ShopEntry.FromItem(ItemKind.Mint).Price);
        Assert.Equal(3_000_000_000, ShopEntry.FromItem(ItemKind.ShinyCharm).Price);
        Assert.Equal(2_500_000_000, ShopEntry.FromEgg(Rarity.Uncommon).Price);
    }

    [Fact]
    public void CandyGrantCountsByWindowClass()
    {
        Assert.Equal(1, CandyGrant.CountFor(WindowClass.Session));
        Assert.Equal(5, CandyGrant.CountFor(WindowClass.Weekly));
    }
}

public class UnownFormTests
{
    [Fact]
    public void RollFavorsUncollectedForms()
    {
        var collected = new HashSet<UnownForm> { UnownForm.A };
        Assert.Equal(UnownForm.A, UnownForms.Roll(0, collected));
        Assert.Equal(UnownForm.B, UnownForms.Roll(1, collected));
    }

    [Fact]
    public void RollWithoutCollectionUsesUniformDoubleWeight()
    {
        Assert.Equal(UnownForm.A, UnownForms.Roll(0, new HashSet<UnownForm>()));
        Assert.Equal(UnownForm.B, UnownForms.Roll(2, new HashSet<UnownForm>()));
    }

    [Fact]
    public void LastFormAbsorbsRemainingRoll()
    {
        Assert.Equal(UnownForm.Question, UnownForms.Roll(54, new HashSet<UnownForm>()));
        Assert.Equal(UnownForm.Question, UnownForms.Roll(55, new HashSet<UnownForm>()));
        Assert.NotEqual(UnownForm.Question, UnownForms.Roll(53, new HashSet<UnownForm>()));
    }

    [Fact]
    public void ResolvedOnlyAppliesToUnownSpecies()
    {
        Assert.Equal(UnownForm.A, UnownForms.Resolved(201, null));
        Assert.Equal(UnownForm.B, UnownForms.Resolved(201, UnownForm.B));
        Assert.Null(UnownForms.Resolved(25, UnownForm.B));
    }

    [Fact]
    public void DisplayNameAppendsSymbolForUnown()
    {
        Assert.Equal("Unown [!]", UnownForms.DisplayName("Unown", 201, UnownForm.Exclamation));
        Assert.Equal("Pikachu", UnownForms.DisplayName("Pikachu", 25, UnownForm.B));
    }

    [Fact]
    public void CollectionWeightHalvesButKeepsMinimumOne()
    {
        Assert.Equal(1, CollectionWeight.Adjusted(1, true));
        Assert.Equal(1, CollectionWeight.Adjusted(1, false));
        Assert.Equal(1, CollectionWeight.Adjusted(2, true));
        Assert.Equal(2, CollectionWeight.Adjusted(2, false));
    }
}
