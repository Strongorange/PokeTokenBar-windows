using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

/// <summary>
/// Port of the macOS PokemonFormTests contract (upstream #376): catalog shape, per-species
/// resolution, sprite names, legacy save keys, collection semantics and labels.
/// </summary>
public class PokemonFormsTests
{
    private static PokemonForm Form(string raw) => new(raw);

    private static readonly PokemonForm Plant = Form("plant");
    private static readonly PokemonForm Sandy = Form("sandy");

    [Fact]
    public void CatalogListsDefaultFirstWithinTheHatchableRange()
    {
        var catalogued = new[] { 201, 351, 386, 412, 413, 421, 422, 423, 479, 487, 492, 493,
            550, 555, 585, 586, 641, 642, 645, 646, 647, 648, 649 };
        Assert.Equal(23, catalogued.Length);
        foreach (var speciesID in catalogued)
        {
            Assert.True(PokemonForms.HasForms(speciesID), $"{speciesID} should be catalogued");
            var forms = PokemonForms.Forms(speciesID);
            Assert.True(forms.Count >= 2, $"{speciesID} needs a default plus one alternate");
            Assert.DoesNotContain(forms, form => string.IsNullOrEmpty(form.Raw));
            // The default appearance keeps the plain `<id>` sprite names.
            Assert.Equal(speciesID.ToString(), SpriteCatalog.AssetName(speciesID, forms[0]));
        }
        Assert.False(PokemonForms.HasForms(25));
        Assert.False(PokemonForms.HasForms(414));
        Assert.Equal(28, PokemonForms.Forms(201).Count);
        Assert.Equal(18, PokemonForms.Forms(493).Count); // Arceus types
        Assert.Equal("a", PokemonForms.Forms(201)[0].Raw);
        Assert.Equal("plant", PokemonForms.Forms(412)[0].Raw);
    }

    [Fact]
    public void LinesCarryFormsFromTheirBaseEvenWhenTheBaseLooksTheSame()
    {
        Assert.Equal([412, 413], PokemonForms.FormSpecies(412));
        Assert.Equal([585, 586], PokemonForms.FormSpecies(585));
        // Cherubi and Darumaka have no forms of their own; their lines carry Cherrim's
        // and Darmanitan's, so eggs of the base already know the form they will become.
        Assert.Equal([421], PokemonForms.FormSpecies(420));
        Assert.Equal([555], PokemonForms.FormSpecies(554));
        Assert.Single(PokemonForms.FormSpecies(479));
        Assert.Empty(PokemonForms.FormSpecies(25));
        Assert.Empty(PokemonForms.FormSpecies(414)); // Mothim keeps its single look
    }

    [Fact]
    public void FormResolvesPerSpeciesOfTheLine()
    {
        var sky = Form("sky");
        Assert.Equal(sky, PokemonForms.Resolved(492, sky));
        Assert.Null(PokemonForms.Resolved(470, sky));
        // Sandy Burmy is a Sandy Wormadam; the Mothim branch shows Mothim's default.
        Assert.Equal(Sandy, PokemonForms.Resolved(412, Sandy));
        Assert.Equal(Sandy, PokemonForms.Resolved(413, Sandy));
        Assert.Null(PokemonForms.Resolved(414, Sandy));
        // Line-level normalization keeps Sandy on record for the whole Burmy line.
        Assert.Equal(Sandy, PokemonForms.ResolvedLine(412, Sandy));
        Assert.Equal(Plant, PokemonForms.ResolvedLine(412, Form("sky")));
        Assert.Null(PokemonForms.ResolvedLine(25, Sandy));
        Assert.Null(PokemonForms.ResolvedLine(414, Sandy));
    }

    [Fact]
    public void CacheKeysSeparateFormsAndKeepDefaultKeys()
    {
        Assert.Equal("492-s", SpriteCatalog.CacheKey(492, false, false, null));
        Assert.Equal("492-sky-a", SpriteCatalog.CacheKey(492, true, false, Form("sky")));
        Assert.Equal("201-b-sha", SpriteCatalog.CacheKey(201, true, true, Form("b")));
        Assert.Equal("201-a", SpriteCatalog.CacheKey(201, true, false, Form("a")));
        Assert.Equal("25-a", SpriteCatalog.CacheKey(25, true, false, Form("sky")));
    }

    [Fact]
    public void SavesWrittenBeforeGenericFormsKeepTheirUnownLetters()
    {
        const string json = """
        {"pendingHatchID":201,"pendingUnownForm":"k","representativeSpeciesID":201,"representativeUnownForm":"q",
         "active":{"baseID":201,"pathIDs":[201],"stageIndex":0,"usedAtStage":0,"rarity":"common","totalForms":1,
                   "unownForm":"z"},
         "dex":[{"baseID":201,"finalID":201,"chainOrder":[201],"rarity":"common","unownForm":"q"}]}
        """;
        var state = CompanionStateCodec.Read(JsonDocument.Parse(json).RootElement);
        Assert.Equal(Form("k"), state.PendingForm);
        Assert.Equal(Form("z"), state.Active!.Form);
        Assert.Equal(Form("q"), state.Dex[0].Form);
        Assert.Equal(Form("q"), state.RepresentativeForm);

        var reencoded = CompanionStateCodec.Write(state, SaveDateMode.Iso8601);
        Assert.Equal("z", reencoded["active"]!["form"]!.GetValue<string>());
        Assert.Equal("q", reencoded["representativeForm"]!.GetValue<string>());
        Assert.Null(reencoded["active"]!["unownForm"]);
    }

    [Fact]
    public void StoredFormIsNormalizedAgainstTheLine()
    {
        const string json = """
        {"active":{"baseID":412,"pathIDs":[412,413],"stageIndex":1,"usedAtStage":0,"rarity":"common",
                   "totalForms":2,"form":"sky"},
         "dex":[{"baseID":1,"finalID":1,"chainOrder":[1],"rarity":"common","form":"sandy"}]}
        """;
        var state = CompanionStateCodec.Read(JsonDocument.Parse(json).RootElement);
        Assert.Equal(Plant, state.Active!.Form); // a form from another line falls back to the line default
        Assert.Null(state.Dex[0].Form); // lines without forms store none
    }

    [Fact]
    public void NewKeysWinOverLegacyUnownKeys()
    {
        const string json = """
        {"active":{"baseID":201,"pathIDs":[201],"stageIndex":0,"usedAtStage":0,"rarity":"common",
                   "totalForms":1,"form":"b","unownForm":"z"}}
        """;
        var state = CompanionStateCodec.Read(JsonDocument.Parse(json).RootElement);
        Assert.Equal(Form("b"), state.Active!.Form);
    }

    private static DexEntry Burmy(int[] chain, PokemonForm form, bool shiny = false) =>
        new(412, chain[^1], chain.ToList(), Rarity.Common, null, isShiny: shiny, form: form);

    [Fact]
    public void EvolutionKeepsTheCloakAndMothimKeepsItsSingleLook()
    {
        var state = new CompanionState
        {
            Active = new MonState(412, [412, 413], null, 1, 0, Rarity.Common, 2, form: Sandy),
            Dex = [Burmy([412, 414], Sandy)]
        };
        Assert.True(state.OwnsSpecies(413, Sandy));
        Assert.False(state.OwnsSpecies(413, Plant));
        Assert.True(state.OwnsSpecies(414));
        Assert.Equal([Sandy], state.CollectedForms(412));
        Assert.Equal([Sandy], state.CollectedForms(413));
        Assert.Empty(state.CollectedForms(414));
    }

    [Fact]
    public void LineFormCountsOnlyOnceEverySpeciesShowingItIsOwned()
    {
        var state = new CompanionState { Dex = [Burmy([412, 414], Sandy)] };
        Assert.DoesNotContain(Sandy, state.CollectedLineForms(412)); // Sandy Wormadam is still missing
        state.Dex.Add(Burmy([412, 413], Sandy));
        Assert.Equal([Sandy], state.CollectedLineForms(412));
        Assert.Empty(state.CollectedLineForms(201));
    }

    [Fact]
    public void ShinyOwnershipIsTrackedPerForm()
    {
        var state = new CompanionState
        {
            Dex =
            [
                Burmy([412, 413], Sandy, shiny: true),
                Burmy([412, 413], Plant)
            ]
        };
        Assert.True(state.OwnsShinySpecies(413, Sandy));
        Assert.False(state.OwnsShinySpecies(413, Plant));
        Assert.False(state.OwnsShinySpecies(412, Plant));
    }

    [Fact]
    public void LabelUsesUnownLettersUnnamedDefaultsAndLocalizedNames()
    {
        Assert.Equal("K", PokemonForms.Label(201, Form("k"), AppLanguage.En, _ => null));
        Assert.Equal("!", PokemonForms.Label(201, Form("exclamation"), AppLanguage.Ko, _ => null));
        Assert.Null(PokemonForms.Label(25, Form("sky"), AppLanguage.En, _ => null));
        // Castform's usual look has no official name: localized "Normal Form".
        Assert.Equal(DashboardText.NormalFormLabel(AppLanguage.Ko),
            PokemonForms.Label(351, Form("normal"), AppLanguage.Ko, _ => null));
        Assert.Equal(DashboardText.NormalFormLabel(AppLanguage.En),
            PokemonForms.Label(479, Form("normal"), AppLanguage.En, _ => null));
        // Non-default forms read the pokemon-form resource names with language fallback.
        Assert.Equal("Forme Céleste", PokemonForms.Label(492, Form("sky"), AppLanguage.Fr,
            id => id == 10064 ? new Dictionary<string, string> { ["fr"] = "Forme Céleste", ["en"] = "Sky Forme" }
                : null));
        // Missing translations fall back to the identifier ("Red Striped").
        Assert.Equal("Red Striped", PokemonForms.Label(550, Form("red-striped"), AppLanguage.En, _ => null));
    }

    [Fact]
    public void DisplayNameShowsOnlyNonDefaultFormsExceptUnown()
    {
        Assert.Equal("Unown [K]", PokemonForms.DisplayName("Unown", 201, Form("k"), "K"));
        Assert.Equal("Shaymin", PokemonForms.DisplayName("Shaymin", 492, Form("land"), "Land Forme"));
        Assert.Equal("Shaymin [Sky Forme]",
            PokemonForms.DisplayName("Shaymin", 492, Form("sky"), "Sky Forme"));
        Assert.Equal("Shaymin", PokemonForms.DisplayName("Shaymin", 492, Form("sky"), null));
        Assert.Equal("Pikachu", PokemonForms.DisplayName("Pikachu", 25, Form("sky"), "Sky Forme"));
    }

    [Fact]
    public void SortOrderFollowsTheCatalogAndDefaultsToZero()
    {
        Assert.Equal(0, PokemonForms.SortOrder(null, 201));
        Assert.Equal(1, PokemonForms.SortOrder(Form("b"), 201));
        Assert.Equal(27, PokemonForms.SortOrder(Form("question"), 201));
        Assert.Equal(0, PokemonForms.SortOrder(Form("b"), 25));
    }

    [Fact]
    public void ParseAcceptsOnlyCataloguedIdentifiers()
    {
        Assert.Equal(Form("sandy"), PokemonForms.Parse("sandy"));
        Assert.Equal(Form("exclamation"), PokemonForms.Parse("exclamation"));
        Assert.Null(PokemonForms.Parse("spiky-eared"));
        Assert.Null(PokemonForms.Parse(null));
        Assert.Null(PokemonForms.Parse(""));
    }
}
