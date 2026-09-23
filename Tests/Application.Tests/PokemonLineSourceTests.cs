using System.Text;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

public class PokemonLineSourceTests
{
    internal const string MinimalSnapshot = """
        {
          "format": "poketokenbar.pokemon-snapshot",
          "schema": 1,
          "bases": [ {"id": 1, "captureRate": 255}, {"id": 10, "captureRate": 255}, {"id": 50, "captureRate": 45} ],
          "lines": [
            {"base": 1, "captureRate": 255, "legendary": false, "mythical": false, "tree": [1, [[2, [[3, []]]]]]},
            {"base": 10, "captureRate": 255, "legendary": false, "mythical": false, "tree": [10, []]},
            {"base": 50, "captureRate": 45, "legendary": false, "mythical": false, "tree": [50, []]},
            {"base": 132, "captureRate": 35, "legendary": false, "mythical": false, "tree": [132, []]}
          ],
          "names": {
            "1": {"en": "Speciemon", "ko": "테스트몬"},
            "2": {"en": "Specimature"},
            "3": {"en": "Specifinal"},
            "10": {"en": "Solo"},
            "50": {"en": "Raremon"},
            "132": {"en": "Ditto"}
          }
        }
        """;

    private static PokemonLineSource Minimal() =>
        PokemonLineSource.Load(new MemoryStream(Encoding.UTF8.GetBytes(MinimalSnapshot)));

    [Fact]
    public void ParsesBasesLinesAndNames()
    {
        var source = Minimal();

        Assert.Equal([1, 10, 50], source.Bases.Select(b => b.Id));
        Assert.Equal(255, source.Bases[0].CaptureRate);

        var line = source.Line(1)!;
        Assert.Equal(1, line.BaseID);
        Assert.Equal(Rarity.Common, line.Rarity);
        Assert.Equal(3, line.TotalForms);
        Assert.Equal([3], line.Tree.FinalIDs);
        Assert.Equal("Speciemon", line.LocalizedName(1, AppLanguage.En));
        Assert.Equal("테스트몬", line.LocalizedName(1, AppLanguage.Ko));

        Assert.Equal(Rarity.Rare, source.Line(50)!.Rarity);
        Assert.Equal(Rarity.Rare, source.Line(132)!.Rarity);
        Assert.Null(source.Line(999));
    }

    [Fact]
    public void RejectsWrongFormat()
    {
        var bad = MinimalSnapshot.Replace("poketokenbar.pokemon-snapshot", "something.else", StringComparison.Ordinal);
        Assert.Throws<System.Text.Json.JsonException>(() =>
            PokemonLineSource.Load(new MemoryStream(Encoding.UTF8.GetBytes(bad))));
    }

    [Fact]
    public void EmbeddedSnapshotLoads()
    {
        var source = PokemonLineSource.Default();

        Assert.True(source.Bases.Count > 200);
        Assert.DoesNotContain(source.Bases, b => b.Id == PokemonOdds.DittoSpeciesID);

        var ditto = source.Line(PokemonOdds.DittoSpeciesID)!;
        Assert.NotNull(ditto);
        Assert.Empty(ditto.Tree.Children);
        Assert.Equal(Rarity.Rare, ditto.Rarity);

        var pichu = source.Line(172)!;
        Assert.NotNull(pichu);
        Assert.Equal(3, pichu.TotalForms);
        Assert.Equal("피카츄", pichu.LocalizedName(25, AppLanguage.Ko));
        Assert.Equal("Pikachu", pichu.LocalizedName(25, AppLanguage.En));

        var eevee = source.Line(133)!;
        Assert.NotNull(eevee);
        Assert.Equal(7, eevee.Tree.Children.Count);
    }
}
