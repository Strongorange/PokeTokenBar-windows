using System.Text;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

[Collection("Application diagnostics")]
public class CompanionEngineTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir;

    public CompanionEngineTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-companion-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private sealed class SequenceRng
    {
        private readonly Queue<ulong> _values = new();

        public SequenceRng(params ulong[] values)
        {
            foreach (var value in values) _values.Enqueue(value);
        }

        public ulong Next() => _values.Count > 0 ? _values.Dequeue() : 1;
    }

    private sealed class TickingClock
    {
        private DateTimeOffset _now = Now;

        public DateTimeOffset Next() => _now += TimeSpan.FromSeconds(1);
    }

    private CompanionEngine BuildEngine(SequenceRng? rng = null, string? fileName = null,
        TickingClock? clock = null)
    {
        var lines = PokemonLineSource.Load(
            new MemoryStream(Encoding.UTF8.GetBytes(PokemonLineSourceTests.MinimalSnapshot)));
        return new CompanionEngine(new CompanionEngineOptions
        {
            StateFilePath = Path.Combine(_dir, fileName ?? "companion-state.json"),
            Clock = clock is not null ? clock.Next : () => Now,
            NextRoll = (rng ?? new SequenceRng()).Next,
            Lines = lines,
        });
    }

    private static Dictionary<string, long> Map(params (string Key, long Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value);

    [Fact]
    public void FirstRefreshSetsBaselineWithoutGrowth()
    {
        var engine = BuildEngine();

        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 2000)), "2026-09-23", true);

        var state = engine.State;
        Assert.True(state.InstallBaselineSet);
        Assert.NotNull(state.ClaimedTodayTokensByProvider);
        Assert.Equal(1000, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(2000, state.ClaimedTodayTokensByProvider["codex"]);
        Assert.Equal("2026-09-23", state.LastDate);
        Assert.Equal(0, state.UsedSinceInstall);
        Assert.Equal(0, state.EggUsage);
        var view = engine.View();
        Assert.True(view.IsEgg);
        Assert.Equal(0, view.EggUsed);
    }

    [Fact]
    public void SecondRefreshAppliesPerProviderDeltaExactlyOnce()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 2000)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 1500), ("codex", 2600)), "2026-09-23", true);

        var state = engine.State;
        Assert.Equal(1100, state.UsedSinceInstall);
        Assert.Equal(1100, state.EggUsage);
        Assert.Equal(1500, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(2600, state.ClaimedTodayTokensByProvider["codex"]);
    }

    [Fact]
    public void RepeatRefreshWithSameTotalsIsIdempotent()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000)), "2026-09-23", true);
        engine.ApplyUsage(Map(("claude_code", 3000)), "2026-09-23", true);
        var statePath = Path.Combine(_dir, "companion-state.json");
        var before = File.ReadAllText(statePath);

        engine.ApplyUsage(Map(("claude_code", 3000)), "2026-09-23", true);

        Assert.Equal(2000, engine.State.UsedSinceInstall);
        Assert.Equal(2000, engine.State.EggUsage);
        Assert.Equal(before, File.ReadAllText(statePath));
    }

    [Fact]
    public void DateRolloverResetsLedgerAndAppliesFullNewDayTotals()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 500)), "2026-09-23", true);
        engine.ApplyUsage(Map(("claude_code", 1200), ("codex", 700)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 300), ("codex", 100)), "2026-09-24", true);

        var state = engine.State;
        Assert.Equal("2026-09-24", state.LastDate);
        Assert.Equal(300, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(100, state.ClaimedTodayTokensByProvider["codex"]);
        Assert.Equal(800, state.UsedSinceInstall);
        Assert.Equal(800, state.EggUsage);

        engine.ApplyUsage(Map(("claude_code", 300), ("codex", 100)), "2026-09-24", true);
        Assert.Equal(800, state.EggUsage);
    }

    [Fact]
    public void UsageRegressionRebasesProviderLedgerWithoutNegativeDelta()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 300)), "2026-09-23", true);

        var state = engine.State;
        Assert.Equal(300, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(0, state.UsedSinceInstall);

        engine.ApplyUsage(Map(("claude_code", 800)), "2026-09-23", true);
        Assert.Equal(500, state.UsedSinceInstall);
        Assert.Equal(500, state.EggUsage);
    }

    [Fact]
    public void NewlyObservedProviderSeedsWithoutRetroactiveUsage()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 10_000)), "2026-09-23", true);

        var state = engine.State;
        Assert.Equal(0, state.UsedSinceInstall);
        Assert.Equal(10_000, state.ClaimedTodayTokensByProvider!["codex"]);

        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 12_000)), "2026-09-23", true);
        Assert.Equal(2000, state.UsedSinceInstall);
    }

    [Fact]
    public void ProviderMissingFromRefreshKeepsItsLedgerEntry()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000), ("codex", 0)), "2026-09-23", true);

        engine.ApplyUsage(Map(("codex", 50)), "2026-09-23", true);

        var state = engine.State;
        Assert.Equal(1000, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(50, state.UsedSinceInstall);
        Assert.Equal(50, state.EggUsage);
    }

    [Fact]
    public void RefreshWithoutAnyProviderDataMovesNothing()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 1000)), "2026-09-23", true);

        engine.ApplyUsage(Map(), "2026-09-23", false);

        Assert.Equal(0, engine.State.UsedSinceInstall);
        Assert.Equal("2026-09-23", engine.State.LastDate);
    }

    [Fact]
    public void EggHatchesEvolutionGraduationAndDexEndToEnd()
    {
        var engine = BuildEngine();
        engine.State.Language = AppLanguage.En;
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 5_000_000)), "2026-09-23", true);
        var state = engine.State;
        Assert.NotNull(state.Active);
        Assert.Equal(1, state.Active!.BaseID);
        Assert.Equal([1], state.Active.PathIDs);
        Assert.Equal(0, state.Active.StageIndex);
        Assert.Equal(3, state.Active.TotalForms);
        Assert.Equal(0, state.Active.UsedAtStage);
        Assert.Equal(0, state.EggUsage);
        var view = engine.View();
        Assert.True(view.HasActive);
        Assert.Equal("Speciemon", view.ActiveName);
        Assert.Equal(3, view.TotalForms);
        Assert.Equal(125_000_000, view.StageThreshold);

        engine.ApplyUsage(Map(("claude_code", 130_000_000)), "2026-09-23", true);
        Assert.Equal(2, engine.State.Active!.CurrentID);
        Assert.Equal(1, engine.State.Active.StageIndex);
        Assert.Equal(0, engine.State.Active.UsedAtStage);

        engine.ApplyUsage(Map(("claude_code", 380_000_000)), "2026-09-23", true);
        Assert.Equal(3, engine.State.Active!.CurrentID);
        Assert.Equal(2, engine.State.Active.StageIndex);

        engine.ApplyUsage(Map(("claude_code", 755_000_000)), "2026-09-23", true);
        state = engine.State;
        Assert.Null(state.Active);
        Assert.Single(state.Dex);
        Assert.Equal(3, state.Dex[0].FinalID);
        Assert.Equal([1, 2, 3], state.Dex[0].ChainOrder);
        Assert.Equal(Rarity.Common, state.Dex[0].Rarity);
        Assert.Equal(Now, state.Dex[0].CaughtAt);
        Assert.Contains("1:3", state.CollectedFinals);
        Assert.Equal(0, state.EggUsage);
        view = engine.View();
        Assert.True(view.IsEgg);
        Assert.Equal(1, view.DexCount);
        Assert.Equal(3, view.DexRows.Count);
        Assert.Contains(view.DexRows, row => row.SpeciesID == 3 && !row.IsRaising);
    }

    [Fact]
    public void OverflowTokensCarryIntoHatchedMonGrowth()
    {
        var engine = BuildEngine();
        engine.State.Language = AppLanguage.En;
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 6_000_000)), "2026-09-23", true);

        var active = engine.State.Active!;
        Assert.NotNull(active);
        Assert.Equal(1_000_000, active.UsedAtStage);
    }

    [Fact]
    public void ShinyRollProducesShinyActiveAndViewMark()
    {
        var engine = BuildEngine(new SequenceRng(64, 64));
        engine.State.Language = AppLanguage.En;
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);

        engine.ApplyUsage(Map(("claude_code", 5_000_000)), "2026-09-23", true);

        Assert.True(engine.State.Active!.IsShiny);
        Assert.True(engine.View().IsShiny);
    }

    [Fact]
    public void EggTierGuaranteeFiltersHatchCandidates()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 100)), "2026-09-23", true);
        engine.State.EggTier = Rarity.Rare;
        engine.State.EggUsage = PokemonBalance.EggHatchThreshold;

        engine.ApplyUsage(Map(("claude_code", 100)), "2026-09-23", true);

        var active = engine.State.Active!;
        Assert.NotNull(active);
        Assert.Equal(50, active.BaseID);
        Assert.Equal(Rarity.Rare, active.Rarity);
        Assert.Null(engine.State.EggTier);
    }

    [Fact]
    public void DittoDisguiseRevealsAtFirstEvolutionThreshold()
    {
        var engine = BuildEngine(new SequenceRng(1, 1, 1, 128));
        engine.State.Language = AppLanguage.En;
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);
        engine.ApplyUsage(Map(("claude_code", 5_000_000)), "2026-09-23", true);
        var disguised = engine.State.Active!;
        Assert.Equal(1, disguised.DittoDisguise);

        engine.ApplyUsage(Map(("claude_code", 130_000_000)), "2026-09-23", true);

        var revealed = engine.State.Active!;
        Assert.NotNull(revealed);
        Assert.Equal(PokemonOdds.DittoSpeciesID, revealed.BaseID);
        Assert.True(revealed.DittoRevealed);
        Assert.Equal(Rarity.Rare, revealed.Rarity);
        Assert.Equal(0, revealed.UsedAtStage);
        Assert.Equal([132], revealed.PathIDs);
    }

    [Fact]
    public void StateFileRoundTripsAcrossEngineRestart()
    {
        var statePath = Path.Combine(_dir, "companion-state.json");
        var first = BuildEngine(fileName: "companion-state.json");
        first.State.Language = AppLanguage.En;
        first.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);
        first.ApplyUsage(Map(("claude_code", 130_000_000)), "2026-09-23", true);
        Assert.Equal(2, first.State.Active!.CurrentID);

        var second = BuildEngine(fileName: "companion-state.json");

        var restored = second.State;
        Assert.True(restored.InstallBaselineSet);
        Assert.Equal(2, restored.Active!.CurrentID);
        Assert.Equal(1, restored.Active.StageIndex);
        Assert.Equal(0, restored.Active.UsedAtStage);
        Assert.Equal(130_000_000, restored.UsedSinceInstall);
        Assert.Equal(130_000_000, restored.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal(AppLanguage.En, restored.Language);
        var view = second.View();
        Assert.True(view.HasActive);
        Assert.Equal("Specimature", view.ActiveName);
    }

    [Fact]
    public void ImportSaveCreatesBackupAndReplacesState()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 3000)), "2026-09-23", true);
        Assert.Equal(0, engine.State.UsedSinceInstall);

        var imported = new CompanionState
        {
            InstallBaselineSet = true,
            UsedSinceInstall = 777_777,
            LastDate = "2026-09-20",
            Dex =
            [
                new DexEntry(50, 50, [50], Rarity.Rare, Now),
            ],
        };
        imported.CollectedFinals.Add("50:50");
        var data = SaveTransfer.Encode(imported, "test", "other-device", Now);

        engine.ImportSave(data);

        var state = engine.State;
        Assert.Equal(777_777, state.UsedSinceInstall);
        Assert.Single(state.Dex);
        Assert.Equal(50, state.Dex[0].BaseID);
        Assert.Contains("50:50", state.CollectedFinals);
        Assert.True(state.InstallBaselineSet);
        Assert.Equal(3000, state.ClaimedTodayTokensByProvider!["claude_code"]);
        Assert.Equal("2026-09-23", state.LastDate);
        var backups = Directory.GetFiles(_dir, "companion-state.pre-import-*.json");
        Assert.Single(backups);
    }

    [Fact]
    public void ImportWrongSchemaIsRejectedAndKeepsCurrentState()
    {
        var engine = BuildEngine();
        engine.ApplyUsage(Map(("claude_code", 3000)), "2026-09-23", true);
        var dexBefore = engine.State.Dex.Count;

        var future = """{"format": "poketokenbar.save", "schema": 3, "state": {}}""";
        var ex = Assert.Throws<SaveTransferException>(
            () => engine.ImportSave(Encoding.UTF8.GetBytes(future)));
        Assert.Equal(SaveTransferError.NewerSchema, ex.Error);

        Assert.Equal(dexBefore, engine.State.Dex.Count);
        Assert.Equal(0, engine.State.UsedSinceInstall);
        Assert.Empty(Directory.GetFiles(_dir, "companion-state.pre-import-*.json"));

        Assert.ThrowsAny<System.Text.Json.JsonException>(
            () => engine.ImportSave("not json at all"u8.ToArray()));
        Assert.Equal(0, engine.State.UsedSinceInstall);
    }

    [Fact]
    public void ImportBackupsArePrunedToLimit()
    {
        var clock = new TickingClock();
        var engine = BuildEngine(clock: clock);
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);
        var imported = SaveTransfer.Encode(new CompanionState(), "test", "device", Now);

        for (var i = 0; i <= SaveTransfer.BackupsToKeep; i++)
            engine.ImportSave(imported);

        Assert.Equal(SaveTransfer.BackupsToKeep, Directory.GetFiles(_dir, "companion-state.pre-import-*.json").Length);
    }

    [Fact]
    public void CorruptStateFileIsBackedUpAndEngineStartsFresh()
    {
        var statePath = Path.Combine(_dir, "companion-state.json");
        File.WriteAllText(statePath, "{ not valid json");

        var engine = BuildEngine(fileName: "companion-state.json");

        Assert.False(engine.State.InstallBaselineSet);
        Assert.True(File.Exists(statePath + ".corrupt"));
    }

    [Fact]
    public void ViewExposesStageItemsAndRecentEvents()
    {
        var engine = BuildEngine();
        engine.State.Language = AppLanguage.En;
        engine.ApplyUsage(Map(("claude_code", 0)), "2026-09-23", true);
        engine.ApplyUsage(Map(("claude_code", 130_000_000)), "2026-09-23", true);

        var view = engine.View();
        Assert.Equal(3, view.StageItems.Count);
        Assert.Equal("Speciemon", view.StageItems[0].Label);
        Assert.True(view.StageItems[0].Done);
        Assert.Equal("Specimature", view.StageItems[1].Label);
        Assert.True(view.StageItems[1].Current);
        Assert.Equal("Specifinal", view.StageItems[2].Label);
        Assert.False(view.StageItems[2].Done);
        Assert.Contains(view.RecentEvents, item => item.Text.Contains("Speciemon hatched"));
        Assert.Contains(view.RecentEvents, item => item.Text.Contains("Specimature"));
    }
}
