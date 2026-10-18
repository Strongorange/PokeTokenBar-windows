using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

/// <summary>
/// Port of the macOS DexSearchAndFilterTests contract (#329): one matcher for both screens,
/// ID/name/multilingual search, shiny + rarity filters, and every sort option.
/// </summary>
public class DexQueryTests
{
    private static readonly DateTimeOffset T0 = new(1_700_000_100, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = new(1_700_000_200, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(1_700_000_300, TimeSpan.Zero);

    private static CompanionDexRow Row(int id, string name, Rarity rarity, bool shiny = false,
        Dictionary<string, string>? names = null) =>
        new(id, name, rarity, shiny, false, Names: names);

    private static Dictionary<string, string> Names(params (string Lang, string Name)[] entries) =>
        entries.ToDictionary(entry => entry.Lang, entry => entry.Name);

    private static List<CompanionDexRow> Store() =>
    [
        Row(1, "Bulbasaur", Rarity.Common, names: Names(("ko", "이상해씨"), ("fr", "Bulbizarre"))),
        Row(2, "Ivysaur", Rarity.Common, names: Names(("ko", "이상해풀"), ("fr", "Herbizarre"))),
        Row(3, "Venusaur", Rarity.Common, names: Names(("ko", "이상해꽃"), ("fr", "Florizarre"))),
        Row(25, "Pikachu", Rarity.Rare, shiny: true, names: Names(("ko", "피카츄"))),
        Row(26, "Raichu", Rarity.Rare, shiny: true, names: Names(("ko", "라이츄"))),
        Row(150, "Mewtwo", Rarity.Legendary, names: Names(("ko", "뮤츠")))
    ];

    private static CompanionCatchRow Catch(string key, int baseID, int finalID, Rarity rarity,
        DateTimeOffset? caughtAt, bool shiny = false, bool raising = false,
        Dictionary<int, Dictionary<string, string>>? names = null)
    {
        var chain = new List<CompanionChainNode>();
        if (names is not null)
            foreach (var id in names.Keys.OrderBy(id => id))
                chain.Add(new CompanionChainNode(id, names[id].GetValueOrDefault("en", $"#{id}")));
        else
            chain.Add(new CompanionChainNode(finalID, $"#{finalID}"));
        return new CompanionCatchRow(key, baseID, rarity, shiny, raising, false, "", null,
            caughtAt, chain,
            names?.Values.SelectMany(byLang => byLang.Values).ToList());
    }

    private static List<CompanionCatchRow> CatchStore() =>
    [
        Catch("entry-1", 1, 3, Rarity.Common, T0, names: new Dictionary<int, Dictionary<string, string>>
        {
            [1] = new() { ["en"] = "Bulbasaur", ["fr"] = "Bulbizarre" },
            [2] = new() { ["en"] = "Ivysaur" },
            [3] = new() { ["en"] = "Venusaur" }
        }),
        Catch("entry-2", 25, 26, Rarity.Rare, T1, shiny: true,
            names: new Dictionary<int, Dictionary<string, string>>
            {
                [25] = new() { ["en"] = "Pikachu", ["ko"] = "피카츄" },
                [26] = new() { ["en"] = "Raichu" }
            }),
        Catch("entry-3", 150, 150, Rarity.Legendary, T2,
            names: new Dictionary<int, Dictionary<string, string>>
            {
                [150] = new() { ["en"] = "Mewtwo", ["ko"] = "뮤츠" }
            })
    ];

    [Fact]
    public void DexSearchMatchesByIDAndName()
    {
        var store = Store();

        // Exact number, with or without the # prefix.
        Assert.Contains(25, DexQuery.FilterAndSortDexRows(store, "25").Select(row => row.SpeciesID));
        Assert.DoesNotContain(1, DexQuery.FilterAndSortDexRows(store, "25").Select(row => row.SpeciesID));
        var byHash = DexQuery.FilterAndSortDexRows(store, "#150");
        Assert.Equal(150, Assert.Single(byHash).SpeciesID);

        // Partial number, with or without the # prefix.
        Assert.Equal([150], DexQuery.FilterAndSortDexRows(store, "15").Select(row => row.SpeciesID));
        Assert.Equal(["entry-3"], DexQuery.FilterAndSortCatchRows(CatchStore(), "#15").Select(row => row.Key));

        // Name, case-insensitive.
        Assert.Equal(3, Assert.Single(DexQuery.FilterAndSortDexRows(store, "Venusaur")).SpeciesID);
        Assert.Contains(25, DexQuery.FilterAndSortDexRows(store, "pika").Select(row => row.SpeciesID));

        // Multilingual: French and Korean names.
        Assert.Contains(1, DexQuery.FilterAndSortDexRows(store, "Bulbizarre").Select(row => row.SpeciesID));
        Assert.Equal(150, Assert.Single(DexQuery.FilterAndSortDexRows(store, "뮤츠")).SpeciesID);

        // No match / blank query matches all.
        Assert.Empty(DexQuery.FilterAndSortDexRows(store, "MissingNo"));
        Assert.Equal(store.Count, DexQuery.FilterAndSortDexRows(store, "").Count);
        Assert.Equal(store.Count, DexQuery.FilterAndSortDexRows(store, "   ").Count);
    }

    /// <summary>
    /// Regression: both screens must fold accents, so "flabebe" finds Flabébé although the
    /// raw strings keep their diacritics.
    /// </summary>
    [Fact]
    public void NameSearchIgnoresCaseAndDiacriticsInDexAndCatchLog()
    {
        var dex = new List<CompanionDexRow>
        {
            Row(669, "Flabébé", Rarity.Common, names: Names(("ko", "플라베베"), ("fr", "Flabébé"))),
            Row(25, "Pikachu", Rarity.Rare)
        };
        var log = new List<CompanionCatchRow>
        {
            Catch("entry-flabebe", 669, 669, Rarity.Common, T0,
                names: new Dictionary<int, Dictionary<string, string>>
                    { [669] = new() { ["en"] = "Flabébé", ["fr"] = "Flabébé" } }),
            Catch("entry-pikachu", 25, 25, Rarity.Rare, T1,
                names: new Dictionary<int, Dictionary<string, string>> { [25] = new() { ["en"] = "Pikachu" } })
        };

        foreach (var query in new[] { "flabebe", "FLABEBE", "Flabébé", "flabébe" })
        {
            Assert.Equal([669], DexQuery.FilterAndSortDexRows(dex, query).Select(row => row.SpeciesID));
            Assert.Equal(["entry-flabebe"], DexQuery.FilterAndSortCatchRows(log, query).Select(row => row.Key));
        }
    }

    [Fact]
    public void DexFiltersByShinyAndRarity()
    {
        var store = Store();

        Assert.All(DexQuery.FilterAndSortDexRows(store, shinyOnly: true), row => Assert.True(row.IsShiny));
        Assert.Equal(150, Assert.Single(DexQuery.FilterAndSortDexRows(store, rarity: Rarity.Legendary)).SpeciesID);
        Assert.Equal(3, DexQuery.FilterAndSortDexRows(store, rarity: Rarity.Common).Count);
        Assert.Equal(2, DexQuery.FilterAndSortDexRows(store, rarity: Rarity.Rare, shinyOnly: true).Count);
        Assert.Empty(DexQuery.FilterAndSortDexRows(store, rarity: Rarity.Legendary, shinyOnly: true));
    }

    [Fact]
    public void CatchLogFiltersByShinyAndRarity()
    {
        var store = CatchStore();

        Assert.Equal("entry-2", Assert.Single(DexQuery.FilterAndSortCatchRows(store, shinyOnly: true)).Key);
        Assert.Equal("entry-3",
            Assert.Single(DexQuery.FilterAndSortCatchRows(store, rarity: Rarity.Legendary)).Key);
        Assert.Equal("entry-2",
            Assert.Single(DexQuery.FilterAndSortCatchRows(store, query: "Raichu")).Key);
    }

    [Fact]
    public void DexSortingCoversEveryOption()
    {
        var store = Store();

        Assert.Equal([1, 2, 3, 25, 26, 150],
            DexQuery.FilterAndSortDexRows(store, sort: DexSortOption.NumberAsc)
                .Select(row => row.SpeciesID));
        Assert.Equal([150, 26, 25, 3, 2, 1],
            DexQuery.FilterAndSortDexRows(store, sort: DexSortOption.NumberDesc)
                .Select(row => row.SpeciesID));

        var nameAsc = DexQuery.FilterAndSortDexRows(store, sort: DexSortOption.NameAsc)
            .Select(row => row.Name).ToList();
        var sorted = nameAsc.OrderBy(name => name, StringComparer.CurrentCulture).ToList();
        Assert.Equal(sorted, nameAsc);

        var nameDesc = DexQuery.FilterAndSortDexRows(store, sort: DexSortOption.NameDesc)
            .Select(row => row.Name).ToList();
        Assert.Equal(nameAsc.OrderByDescending(name => name, StringComparer.CurrentCulture).ToList(),
            nameDesc);

        var rarityRanks = DexQuery.FilterAndSortDexRows(store, sort: DexSortOption.RarityDesc)
            .Select(row => row.Rarity.SortRank()).ToList();
        Assert.Equal(rarityRanks.OrderByDescending(rank => rank), rarityRanks);
    }

    [Fact]
    public void CatchLogSortingCoversEveryOption()
    {
        var store = CatchStore();

        Assert.Equal(["entry-3", "entry-2", "entry-1"],
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.RecentFirst)
                .Select(row => row.Key));
        Assert.Equal(["entry-1", "entry-2", "entry-3"],
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.OldestFirst)
                .Select(row => row.Key));
        Assert.Equal([3, 26, 150],
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.NumberAsc)
                .Select(FinalID));
        Assert.Equal([150, 26, 3],
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.NumberDesc)
                .Select(FinalID));
        Assert.Equal([Rarity.Legendary, Rarity.Rare, Rarity.Common],
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.RarityDesc)
                .Select(row => row.Rarity));
        var nameAsc = DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.NameAsc)
            .Select(SortName).ToList();
        Assert.Equal(nameAsc.OrderBy(name => name, StringComparer.CurrentCulture), nameAsc);
        var nameDesc = DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.NameDesc)
            .Select(SortName).ToList();
        Assert.Equal(nameAsc.OrderByDescending(name => name, StringComparer.CurrentCulture), nameDesc);
    }

    /// <summary>The raising individual stays pinned first under the default recent sort.</summary>
    [Fact]
    public void RecentFirstKeepsTheRaisingRowPinnedAndOldestFirstMovesItLast()
    {
        var store = CatchStore();
        store.Add(Catch("active", 4, 5, Rarity.Common, null, raising: true,
            names: new Dictionary<int, Dictionary<string, string>>
                { [5] = new() { ["en"] = "Raisingmon" } }));

        Assert.Equal("active",
            DexQuery.FilterAndSortCatchRows(store)[0].Key);
        Assert.Equal("active",
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.RecentFirst)[0].Key);
        Assert.Equal("active",
            DexQuery.FilterAndSortCatchRows(store, sort: CatchLogSortOption.OldestFirst)[^1].Key);
    }

    [Fact]
    public void SortOptionLabelsExistInAllLanguages()
    {
        foreach (var lang in AppLanguages.All)
        {
            foreach (var option in Enum.GetValues<DexSortOption>())
                Assert.False(string.IsNullOrEmpty(DexQuery.Label(lang, option)),
                    $"missing DexSortOption label {option} in {lang}");
            foreach (var option in Enum.GetValues<CatchLogSortOption>())
                Assert.False(string.IsNullOrEmpty(DexQuery.Label(lang, option)),
                    $"missing CatchLogSortOption label {option} in {lang}");
            Assert.False(string.IsNullOrEmpty(DashboardText.DexSearchPlaceholder(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.SortTitle(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.FilterShinyOnly(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.NoSearchResults(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.ClearFilters(lang)));
        }
    }

    private static int FinalID(CompanionCatchRow row) => row.Chain[^1].SpeciesID;

    private static string SortName(CompanionCatchRow row) => row.Chain[^1].Name;
}
