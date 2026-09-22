using System.Text.Json;
using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class SaveTransferTests
{
    private static CompanionState SampleState() => new()
    {
        InstallBaselineSet = true,
        UsedSinceInstall = 3_000_000_000,
        SpentTokens = 500_000_000,
        EggUsage = 1_000_000,
        EggTier = Rarity.Uncommon,
        PendingHatchID = 25,
        ClaimedTodayTokensByProvider = new Dictionary<string, long> { ["claude_code"] = 123_000 },
        LastDate = "2026-09-22",
        Active = new MonState(25, [25, 26], [25, 26], 1, 100_000, Rarity.Common, 2,
            isShiny: true, nature: PokemonNature.Jolly,
            profile: PokemonProfile.Generate(1234, 50_000_000, "instance-1")),
        RepresentativeSpeciesID = 26,
        Dex =
        [
            new DexEntry(1, 3, [1, 2, 3], Rarity.Common,
                DateTimeOffset.Parse("2026-08-01T10:00:00Z").ToUniversalTime(),
                names: new Dictionary<int, Dictionary<string, string>>
                {
                    [1] = new() { ["en"] = "Bulbasaur" },
                    [2] = new() { ["en"] = "Ivysaur" },
                    [3] = new() { ["en"] = "Venusaur" }
                })
        ],
        CollectedFinals = ["1:3"],
        Language = AppLanguage.Ko,
        Inventory = new Dictionary<string, long> { ["rareCandy"] = 2 },
        CandyGrantTier = new Dictionary<string, int> { ["claude:fiveHour"] = 2 },
        CandyFeatureSeeded = true
    };

    [Fact]
    public void EncodeDecodeRoundTripsState()
    {
        var state = SampleState();
        var data = SaveTransfer.Encode(state, "2.5.4", "test-device",
            DateTimeOffset.Parse("2026-09-22T12:00:00Z").ToUniversalTime());
        var envelope = SaveTransfer.Decode(data);
        Assert.Equal(SaveEnvelope.FormatID, envelope.Format);
        Assert.Equal(2, envelope.Schema);
        Assert.Equal("2.5.4", envelope.AppVersion);
        Assert.Equal("test-device", envelope.SourceDevice);
        var decoded = envelope.State;
        Assert.Equal(state.UsedSinceInstall, decoded.UsedSinceInstall);
        Assert.Equal(state.SpentTokens, decoded.SpentTokens);
        Assert.Equal(state.EggUsage, decoded.EggUsage);
        Assert.Null(decoded.EggTier);
        Assert.Null(decoded.PendingHatchID);
        Assert.Equal(123_000, decoded.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal("2026-09-22", decoded.LastDate);
        Assert.NotNull(decoded.Active);
        Assert.Equal(26, decoded.Active!.CurrentID);
        Assert.True(decoded.Active.IsShiny);
        Assert.Equal(PokemonNature.Jolly, decoded.Active.Nature);
        Assert.Equal("instance-1", decoded.Active.Profile!.InstanceID);
        Assert.Equal(50_000_000, decoded.Active.Profile.GrowthTokens);
        Assert.Single(decoded.Dex);
        Assert.Equal(3, decoded.Dex[0].FinalID);
        Assert.Equal(1, decoded.Dex[0].CurrentNamesVersionCheck());
        Assert.Equal(DateTimeOffset.Parse("2026-08-01T10:00:00Z").ToUniversalTime(), decoded.Dex[0].CaughtAt);
        Assert.Contains("1:3", decoded.CollectedFinals);
        Assert.Equal(AppLanguage.Ko, decoded.Language);
        Assert.Equal(2, decoded.Inventory["rareCandy"]);
        Assert.Equal(2, decoded.CandyGrantTier["claude:fiveHour"]);
        Assert.True(decoded.CandyFeatureSeeded);
    }

    [Fact]
    public void EncodeIsPrettySortedAndUsesIsoDates()
    {
        var data = SaveTransfer.Encode(new CompanionState(), "1.0", "dev",
            DateTimeOffset.Parse("2026-09-22T12:00:34Z").ToUniversalTime());
        var text = System.Text.Encoding.UTF8.GetString(data);
        Assert.Contains("\"exportedAt\": \"2026-09-22T12:00:34Z\"", text);
        var indexOfApp = text.IndexOf("\"appVersion\"");
        var indexOfFormat = text.IndexOf("\"format\"");
        var indexOfState = text.IndexOf("\"state\"");
        Assert.True(indexOfApp < indexOfFormat && indexOfFormat < indexOfState);
        Assert.Contains("\n  ", text);
    }

    [Fact]
    public void ForeignJsonIsNotASaveFile()
    {
        Assert.Throws<SaveTransferException>(() =>
            SaveTransfer.Decode(System.Text.Encoding.UTF8.GetBytes("{\"hello\": 1}")));
        Assert.Throws<SaveTransferException>(() =>
            SaveTransfer.Decode(System.Text.Encoding.UTF8.GetBytes("[1,2,3]")));
    }

    [Fact]
    public void NewerSchemaIsRejected()
    {
        var json = "{\"format\": \"poketokenbar.save\", \"schema\": 3}";
        var ex = Assert.Throws<SaveTransferException>(() =>
            SaveTransfer.Decode(System.Text.Encoding.UTF8.GetBytes(json)));
        Assert.Equal(SaveTransferError.NewerSchema, ex.Error);
    }

    [Fact]
    public void OversizedFileIsRejected()
    {
        var header = System.Text.Encoding.UTF8.GetBytes(
            "{\"format\": \"poketokenbar.save\", \"schema\": 2, \"state\": {}, \"pad\": \"");
        var data = header.Concat(new byte[SaveTransfer.MaxFileBytes]).ToArray();
        var ex = Assert.Throws<SaveTransferException>(() => SaveTransfer.Decode(data));
        Assert.Equal(SaveTransferError.FileTooLarge, ex.Error);
    }

    [Fact]
    public void SanitizeClampsTokensAndStructuralValues()
    {
        var state = new CompanionState
        {
            UsedSinceInstall = 9_000_000_000_000_000,
            SpentTokens = -5,
            EggUsage = 9_000_000_000_000_000,
            Active = new MonState(25, [25, 26], null, 9, 9_000_000_000_000_000, Rarity.Common, 99)
        };
        var sanitized = SaveTransfer.Sanitized(state);
        Assert.Equal(SaveTransfer.MaxTokenValue, sanitized.UsedSinceInstall);
        Assert.Equal(0, sanitized.SpentTokens);
        Assert.Equal(SaveTransfer.MaxTokenValue, sanitized.EggUsage);
        Assert.Equal(12, sanitized.Active!.TotalForms);
        Assert.Equal(1, sanitized.Active.StageIndex);
        Assert.Equal(SaveTransfer.MaxTokenValue, sanitized.Active.UsedAtStage);
    }

    [Fact]
    public void SanitizeDropsEggTierAndPreRollWhenActiveExists()
    {
        var state = new CompanionState
        {
            EggTier = Rarity.Rare,
            PendingHatchID = 132,
            Active = new MonState(25, [25], null, 0, 0, Rarity.Common, 1)
        };
        var sanitized = SaveTransfer.Sanitized(state);
        Assert.Null(sanitized.EggTier);
        Assert.Null(sanitized.PendingHatchID);
    }

    [Fact]
    public void SanitizeDropsUnsatisfiableLegendaryEggTier()
    {
        var state = new CompanionState { EggTier = Rarity.Legendary };
        Assert.Null(SaveTransfer.Sanitized(state).EggTier);
        Assert.Equal(Rarity.Rare, SaveTransfer.Sanitized(new CompanionState { EggTier = Rarity.Rare }).EggTier);
    }

    [Fact]
    public void SanitizeDropsGhostRepresentativeSpecies()
    {
        var state = new CompanionState { RepresentativeSpeciesID = 999 };
        Assert.Null(SaveTransfer.Sanitized(state).RepresentativeSpeciesID);

        var owned = new CompanionState
        {
            RepresentativeSpeciesID = 26,
            Active = new MonState(25, [25, 26], null, 1, 0, Rarity.Common, 2)
        };
        Assert.Equal(26, SaveTransfer.Sanitized(owned).RepresentativeSpeciesID);
    }

    [Fact]
    public void RebaseKeepsLanguageAndMergesGrantTiers()
    {
        var imported = SampleState();
        imported.CandyGrantTier["window-a"] = 1;
        var current = new CompanionState
        {
            Language = AppLanguage.En,
            CandyGrantTier = new Dictionary<string, int> { ["window-a"] = 2, ["window-b"] = 1 }
        };
        var rebased = SaveTransfer.RebasedForThisDevice(imported, current,
            new Dictionary<string, long>(), "2026-09-22", hasUsageData: false);
        Assert.Equal(AppLanguage.En, rebased.Language);
        Assert.Equal(2, rebased.CandyGrantTier["window-a"]);
        Assert.Equal(1, rebased.CandyGrantTier["window-b"]);
        Assert.Equal(2, rebased.CandyGrantTier["claude:fiveHour"]);
        Assert.False(rebased.InstallBaselineSet);
        Assert.Null(rebased.ClaimedTodayTokensByProvider);
        Assert.Equal("", rebased.LastDate);
    }

    [Fact]
    public void RebaseSeedsLedgerWhenCurrentProviderDataExists()
    {
        var imported = SampleState();
        var current = new CompanionState { Language = AppLanguage.Ja };
        var today = new Dictionary<string, long> { ["codex"] = 555 };
        var rebased = SaveTransfer.RebasedForThisDevice(imported, current, today, "2026-09-22", hasUsageData: true);
        Assert.True(rebased.InstallBaselineSet);
        Assert.Equal(555, rebased.ClaimedTodayTokensByProvider!["codex"]);
        Assert.Equal("2026-09-22", rebased.LastDate);
    }

    [Fact]
    public void FileNamesCarryDates()
    {
        var date = DateTimeOffset.Parse("2026-09-22T05:06:07Z");
        Assert.Equal("PokeTokenBar-Save-2026-09-22.json", SaveTransfer.SuggestedFileName(date));
        Assert.Equal("companion-state.pre-import-2026-09-22-050607.json", SaveTransfer.BackupFileName(date));
    }

    [Fact]
    public void LenientDecodeAbsorbsCorruptFields()
    {
        var json = """
        {
          "usedSinceInstall": "not a number",
          "eggTier": "bogus",
          "dex": [{ "baseID": 1, "pathIDs": [] }, { "baseID": 4, "finalID": 7, "chainOrder": [4,7], "rarity": "common" }],
          "inventory": { "rareCandy": 3 },
          "active": { "baseID": 25, "pathIDs": [] },
          "candyFeatureSeeded": true
        }
        """;
        var state = CompanionStateCodec.Read(JsonDocument.Parse(json).RootElement);
        Assert.Equal(0, state.UsedSinceInstall);
        Assert.Null(state.EggTier);
        Assert.Null(state.Active);
        Assert.Single(state.Dex);
        Assert.Equal(7, state.Dex[0].FinalID);
        Assert.Equal(3, state.Inventory["rareCandy"]);
        Assert.True(state.CandyFeatureSeeded);
    }

    [Fact]
    public void MissingClaimedMapStaysNullForBaselineSeeding()
    {
        var state = CompanionStateCodec.Read(JsonDocument.Parse("""{"usedSinceInstall": 5}""").RootElement);
        Assert.Null(state.ClaimedTodayTokensByProvider);

        var seeded = CompanionStateCodec.Read(
            JsonDocument.Parse("""{"claimedTodayTokensByProvider": {"codex": 7}}""").RootElement);
        Assert.NotNull(seeded.ClaimedTodayTokensByProvider);
        Assert.Equal(7, seeded.ClaimedTodayTokensByProvider!["codex"]);
    }

    [Fact]
    public void RuntimeSaveDatesUseReferenceEpochDoubles()
    {
        var caughtAt = DateTimeOffset.Parse("2026-08-01T10:00:00.5Z").ToUniversalTime();
        var state = new CompanionState
        {
            Dex = [new DexEntry(1, 3, [1, 3], Rarity.Common, caughtAt)]
        };
        var json = CompanionStateCodec.Write(state, SaveDateMode.ReferenceEpochDouble);
        var text = json.ToJsonString();
        Assert.DoesNotContain("2026-08-01", text);
        var node = System.Text.Json.Nodes.JsonNode.Parse(text)!;
        var seconds = node["dex"]![0]!["caughtAt"]!.GetValue<double>();
        var expected = (caughtAt - new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
        Assert.Equal(expected, seconds, 3);
        var decoded = CompanionStateCodec.Read(System.Text.Json.Nodes.JsonNode.Parse(text)!.AsObject()
            .Deserialize<JsonElement>());
        Assert.Equal(caughtAt, decoded.Dex[0].CaughtAt);
    }
}

public static class DexEntryTestExtensions
{
    public static int CurrentNamesVersionCheck(this DexEntry entry) =>
        entry.NamesVersion ?? 0;
}
