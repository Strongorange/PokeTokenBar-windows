using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

/// <summary>
/// Contract for locating the floating pet's subject in the dex grid: the
/// representative wins over the active companion, an egg only floats (and has
/// no dex row) when nothing is pinned, and filters hiding the subject must
/// read as "nothing to reveal" rather than a wrong index.
/// </summary>
public class DexFocusTests
{
    private static readonly PokemonForm UnownA = new("a");

    private static CompanionGameView View(
        int? representative = null, bool representativeShiny = false,
        PokemonForm? representativeForm = null, int activeSpeciesID = 25,
        PokemonForm? activeForm = null, bool isEgg = false, bool isShiny = false) => new(
        HasActive: !isEgg,
        ActiveName: "test",
        Rarity: null,
        IsShiny: isShiny,
        HasGrowthBoost: false,
        StageIndex: 0,
        TotalForms: 1,
        StageUsed: 0,
        StageThreshold: 100,
        StageProgress: 0,
        IsEgg: isEgg,
        EggUsed: 0,
        EggThreshold: 1,
        EggProgress: 0,
        DexCount: 3,
        LifetimeTokens: 0,
        AvailableTokens: 0,
        GrowthDifficulty: 1,
        ShopDifficulty: 1,
        StageItems: [],
        EvoLine: [],
        DexRows: [],
        RecentEvents: [],
        ShopRows: [],
        Bag: [],
        ActiveSpeciesID: activeSpeciesID,
        ActiveForm: activeForm,
        EggGuarantee: null,
        RepresentativeSpeciesID: representative,
        RepresentativeIsShiny: representativeShiny,
        RepresentativeForm: representativeForm,
        Status: CompanionStatusKind.Idle,
        StatusEvolvedName: null,
        CatchRows: [],
        Language: AppLanguage.Ko);

    [Fact]
    public void RepresentativeWinsOverTheActiveCompanion()
    {
        var view = View(representative: 26, representativeShiny: true,
            representativeForm: UnownA, activeSpeciesID: 25, isShiny: false);

        var subject = DexFocus.Resolve(view);

        Assert.Equal(new FloatingSubject(false, 26, true, UnownA), subject);
        Assert.Equal(26, DexFocus.DexSpeciesID(view));
    }

    [Fact]
    public void ActiveCompanionFloatsWhenNothingIsPinned()
    {
        var view = View(representative: null, activeSpeciesID: 25, activeForm: UnownA,
            isShiny: true);

        var subject = DexFocus.Resolve(view);

        Assert.Equal(new FloatingSubject(false, 25, true, UnownA), subject);
        Assert.Equal(25, DexFocus.DexSpeciesID(view));
    }

    [Fact]
    public void EggHasNoDexRowToHighlight()
    {
        var view = View(isEgg: true, activeSpeciesID: 0);

        var subject = DexFocus.Resolve(view);

        Assert.True(subject.Egg);
        Assert.Null(DexFocus.DexSpeciesID(view));
    }

    [Fact]
    public void PinnedRepresentativeFloatsEvenWhileEggIncubates()
    {
        var view = View(representative: 26, isEgg: true, activeSpeciesID: 0);

        var subject = DexFocus.Resolve(view);

        Assert.False(subject.Egg);
        Assert.Equal(26, DexFocus.DexSpeciesID(view));
    }

    [Fact]
    public void VisibleRowIndexFindsTheSubjectAmongFilteredRows()
    {
        var rows = new List<CompanionDexRow>
        {
            new(1, "Bulbasaur", Rarity.Common, false, false),
            new(25, "Pikachu", Rarity.Rare, false, false),
            new(26, "Raichu", Rarity.Rare, false, false),
        };

        Assert.Equal(1, DexFocus.VisibleRowIndex(rows, 25));
    }

    [Fact]
    public void VisibleRowIndexIsNullWhenFiltersHideTheSubject()
    {
        var rows = new List<CompanionDexRow>
        {
            new(1, "Bulbasaur", Rarity.Common, false, false),
        };

        Assert.Null(DexFocus.VisibleRowIndex(rows, 150));
        Assert.Null(DexFocus.VisibleRowIndex(rows, null));
    }
}
