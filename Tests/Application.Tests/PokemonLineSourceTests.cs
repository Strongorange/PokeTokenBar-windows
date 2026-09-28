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

    internal const string CombatSnapshot = """
        {
          "format": "poketokenbar.pokemon-snapshot",
          "schema": 2,
          "bases": [ {"id": 1, "captureRate": 255} ],
          "lines": [
            {"base": 1, "captureRate": 255, "legendary": false, "mythical": false, "tree": [1, [[2, [[3, []]]]]]}
          ],
          "names": {
            "1": {"en": "Speciemon", "ko": "테스트몬"},
            "2": {"en": "Specimature"},
            "3": {"en": "Specifinal"}
          },
          "details": {
            "1": {
              "name": "speciemon", "height": 5, "weight": 40, "baseExperience": 62, "genderRate": 4,
              "types": ["grass"],
              "baseStats": {"hp": 45, "attack": 49, "defense": 49, "special-attack": 65, "special-defense": 65, "speed": 45},
              "abilities": [
                {"name": "overgrow", "slot": 1, "hidden": false},
                {"name": "chlorophyll", "slot": 3, "hidden": true}
              ],
              "moves": [
                ["razor-leaf", "level-up", 13],
                ["tackle", "level-up", 1],
                ["vine-whip", "level-up", 7, "machine", 0]
              ]
            },
            "2": {
              "name": "specimature", "height": 10, "weight": 130, "genderRate": 4,
              "types": ["grass"],
              "baseStats": {"hp": 60, "attack": 62, "defense": 63, "special-attack": 80, "special-defense": 80, "speed": 60},
              "abilities": [
                {"name": "overgrow", "slot": 1, "hidden": false}
              ],
              "moves": [
                ["tackle", "level-up", 1],
                ["vine-whip", "level-up", 7]
              ]
            },
            "3": {
              "name": "specifinal", "height": 20, "weight": 1000, "genderRate": 4,
              "types": ["grass"],
              "baseStats": {"hp": 80, "attack": 82, "defense": 83, "special-attack": 100, "special-defense": 100, "speed": 80},
              "abilities": [
                {"name": "overgrow", "slot": 1, "hidden": false},
                {"name": "chlorophyll", "slot": 3, "hidden": true}
              ],
              "moves": [
                ["razor-leaf", "level-up", 13],
                ["solar-beam", "machine", 0],
                ["tackle", "level-up", 1]
              ]
            }
          },
          "resourceNames": {
            "type": {"grass": {"en": "Grass", "ko": "풀"}},
            "ability": {
              "overgrow": {"en": "Overgrow", "ko": "심록"},
              "chlorophyll": {"en": "Chlorophyll"}
            },
            "move": {
              "razor-leaf": {"en": "Razor Leaf", "ko": "잎날강타"},
              "solar-beam": {"en": "Solar Beam", "ko": "솔라빔"},
              "tackle": {"en": "Tackle", "ko": "몸통박치기"},
              "vine-whip": {"en": "Vine Whip", "ko": "덩굴채찍"}
            }
          }
        }
        """;

    private static PokemonLineSource Minimal() =>
        PokemonLineSource.Load(new MemoryStream(Encoding.UTF8.GetBytes(MinimalSnapshot)));

    private static PokemonLineSource Combat() =>
        PokemonLineSource.Load(new MemoryStream(Encoding.UTF8.GetBytes(CombatSnapshot)));

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
    public void Schema1LoadsWithoutCombatDetails()
    {
        var source = Minimal();

        Assert.Null(source.Details(1));
        Assert.Equal("Overgrow", source.ResourceName("ability", "overgrow", AppLanguage.En));
    }

    [Fact]
    public void RejectsUnknownSchema()
    {
        var bad = MinimalSnapshot.Replace("\"schema\": 1", "\"schema\": 3", StringComparison.Ordinal);
        Assert.Throws<System.Text.Json.JsonException>(() =>
            PokemonLineSource.Load(new MemoryStream(Encoding.UTF8.GetBytes(bad))));
    }

    [Fact]
    public void ParsesSchema2CombatDetails()
    {
        var source = Combat();

        var details = source.Details(1)!;
        Assert.NotNull(details);
        Assert.Equal("speciemon", details.Name);
        Assert.Equal(5, details.Height);
        Assert.Equal(40, details.Weight);
        Assert.Equal(62, details.BaseExperience);
        Assert.Equal(4, details.GenderRate);
        Assert.Equal(["grass"], details.Types);
        Assert.Equal(45, details.BaseStats["hp"]);
        Assert.Equal(318, details.BaseStatTotal);
        Assert.Equal(2, details.Abilities.Count);
        Assert.False(details.Abilities[0].IsHidden);
        Assert.True(details.Abilities[1].IsHidden);
        Assert.Equal(3, details.Moves.Count);

        var vineWhip = details.Moves.First(move => move.Name == "vine-whip");
        Assert.Equal(2, vineWhip.LearnMethods.Count);
        Assert.Equal("level-up", vineWhip.LearnMethods[0].Method);
        Assert.Equal(7, vineWhip.LearnMethods[0].Level);
        Assert.Equal("machine", vineWhip.LearnMethods[1].Method);
        Assert.Equal([("tackle", 1)], details.LevelUpMovesThrough(5).Select(move => (move.Name, move.LearnedAtLevel)));
        Assert.Equal(3, details.LevelUpMovesThrough(50).Count);
        Assert.Null(source.Details(999));
    }

    [Fact]
    public void ResolvesLocalizedResourceNamesWithFallback()
    {
        var source = Combat();

        Assert.Equal("풀", source.ResourceName(PokemonResourceKinds.Type, "grass", AppLanguage.Ko));
        Assert.Equal("Grass", source.ResourceName(PokemonResourceKinds.Type, "grass", AppLanguage.En));
        Assert.Equal("심록", source.ResourceName(PokemonResourceKinds.Ability, "overgrow", AppLanguage.Ko));
        Assert.Equal("몸통박치기", source.ResourceName(PokemonResourceKinds.Move, "tackle", AppLanguage.Ko));
        Assert.Equal("Chlorophyll", source.ResourceName(PokemonResourceKinds.Ability, "chlorophyll", AppLanguage.Ko));
        Assert.Equal("Some Move", source.ResourceName(PokemonResourceKinds.Move, "some-move", AppLanguage.Ko));
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

        var pikachu = source.Details(25)!;
        Assert.NotNull(pikachu);
        Assert.Equal("pikachu", pikachu.Name);
        Assert.Equal(6, pikachu.BaseStats.Count);
        Assert.Contains(pikachu.Moves, move => move.Name == "thunderbolt");
        Assert.Contains(pikachu.Moves, move => move.Name == "thunder-shock"
            && move.LearnMethods.Any(method => method.Method == "level-up" && method.Level == 1));
        Assert.Null(source.Details(10_001));
        Assert.Equal("전기", source.ResourceName(PokemonResourceKinds.Type, "electric", AppLanguage.Ko));
        Assert.Equal("Elektro", source.ResourceName(PokemonResourceKinds.Type, "electric", AppLanguage.De));
    }
}
