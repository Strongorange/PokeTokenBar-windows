using System.Text.Json;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

/// <summary>
/// Port of the macOS SaveSnapshotTests contract (#330): creation/listing, retention pruning,
/// the 12-hour auto-snapshot guard, corruption recovery, and the restore flow including the
/// read-before-prune regression at the retention limit.
/// </summary>
public class SaveSnapshotTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "ptb-snapshots-" + Guid.NewGuid().ToString("N"));

    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private string StatePath => Path.Combine(_dir, "companion-state.json");

    private static CompanionState State(int dexCount, long lifetime, MonState? active = null)
    {
        var state = new CompanionState
        {
            UsedSinceInstall = lifetime,
            Active = active
        };
        for (var i = 0; i < dexCount; i++)
            state.Dex.Add(new DexEntry(1, 3, [1, 2, 3], Rarity.Common, T0));
        return state;
    }

    private sealed class MutableClock { public DateTimeOffset Now; }

    private CompanionEngine BuildEngine(MutableClock clock) =>
        new(new CompanionEngineOptions
        {
            StateFilePath = StatePath,
            Clock = () => clock.Now,
            NextRoll = static () => 1,
            Lines = PokemonLineSource.Load(new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(PokemonLineSourceTests.MinimalSnapshot))),
        });

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void SnapshotCreationAndListingIsNewestFirstAndEnvelopeDecodable()
    {
        Directory.CreateDirectory(_dir);
        var older = SaveSnapshotManager.CreateSnapshot(State(1, 100), StatePath, T0, "0.30.0", "pc");
        var newer = SaveSnapshotManager.CreateSnapshot(State(2, 200), StatePath,
            T0 + TimeSpan.FromHours(1), "0.30.0", "pc");

        var listed = SaveSnapshotManager.ListSnapshots(StatePath);
        Assert.Equal([newer.FileName, older.FileName], listed.Select(snapshot => snapshot.FileName));
        Assert.Equal(2, listed[0].DexCount);
        Assert.Equal(200, listed[0].LifetimeTokens);
        Assert.Equal(T0 + TimeSpan.FromHours(1), listed[0].Date);

        var envelope = SaveSnapshotManager.LoadEnvelope(newer.FilePath);
        Assert.Equal(2, envelope.State.Dex.Count);
        Assert.Equal("pc", envelope.SourceDevice);
    }

    [Fact]
    public void SameSecondSnapshotGetsACounterInsteadOfOverwriting()
    {
        Directory.CreateDirectory(_dir);
        var first = SaveSnapshotManager.CreateSnapshot(State(1, 100), StatePath, T0, "0.30.0", "pc");
        var second = SaveSnapshotManager.CreateSnapshot(State(2, 200), StatePath, T0, "0.30.0", "pc");
        Assert.NotEqual(first.FileName, second.FileName);
        Assert.Equal(2, SaveSnapshotManager.ListSnapshots(StatePath).Count);
        // The counter suffix must not break stamp parsing.
        Assert.Equal(T0, SaveSnapshotManager.ParseStamp(second.FileName));
    }

    [Fact]
    public void SnapshotKeepsNonDefaultFormInSummaryAndRestoredState()
    {
        Directory.CreateDirectory(_dir);
        var active = new MonState(412, [412, 413], null, 1, 0, Rarity.Common, 2,
            form: new PokemonForm("sandy"));
        var snapshot = SaveSnapshotManager.CreateSnapshot(State(1, 100, active), StatePath,
            T0, "0.30.0", "pc");

        Assert.Equal(new PokemonForm("sandy"), snapshot.CurrentForm);
        var restored = SaveSnapshotManager.LoadState(snapshot.FilePath);
        Assert.NotNull(restored);
        Assert.Equal(new PokemonForm("sandy"), restored!.Active!.Form);
    }

    [Fact]
    public void SummariesResolveCurrentAppearanceAcrossLegacyAndEnvelopeFiles()
    {
        Directory.CreateDirectory(_dir);
        // Legacy raw state file (no envelope), dated only by its filename stamp.
        var dir = SaveSnapshotManager.SnapshotsDirectory(StatePath);
        var active = new MonState(492, [492], null, 0, 0, Rarity.Rare, 1,
            form: new PokemonForm("sky"));
        File.WriteAllText(
            Path.Combine(dir, SaveSnapshotManager.SnapshotFileName(T0 - TimeSpan.FromHours(2))),
            CompanionStateCodec.Write(State(1, 50, active), SaveDateMode.Iso8601).ToJsonString());
        SaveSnapshotManager.CreateSnapshot(State(2, 100), StatePath, T0, "0.30.0", "pc");

        var listed = SaveSnapshotManager.ListSnapshots(StatePath);
        Assert.Equal(2, listed.Count);
        var legacy = listed[^1];
        Assert.Equal(T0 - TimeSpan.FromHours(2), legacy.Date);
        Assert.Equal(new PokemonForm("sky"), legacy.CurrentForm);
    }

    [Fact]
    public void SnapshotDoesNotRevealShinyOfDisguisedDitto()
    {
        Directory.CreateDirectory(_dir);
        var disguised = new MonState(1, [1, 2], null, 0, 0, Rarity.Common, 2,
            isShiny: true, dittoDisguise: 1);
        var hidden = SaveSnapshotManager.CreateSnapshot(State(1, 100, disguised), StatePath,
            T0, "0.30.0", "pc");
        Assert.False(hidden.CurrentIsShiny);

        disguised.DittoRevealed = true;
        var revealed = SaveSnapshotManager.CreateSnapshot(State(1, 100, disguised), StatePath,
            T0 + TimeSpan.FromHours(1), "0.30.0", "pc");
        Assert.True(revealed.CurrentIsShiny);
    }

    [Fact]
    public void SnapshotPruningKeepsAtMostTen()
    {
        Directory.CreateDirectory(_dir);
        for (var i = 0; i < 12; i++)
            SaveSnapshotManager.CreateSnapshot(State(i, i), StatePath,
                T0 + TimeSpan.FromHours(i), "0.30.0", "pc");

        var listed = SaveSnapshotManager.ListSnapshots(StatePath);
        Assert.Equal(10, listed.Count);
        Assert.Equal(T0 + TimeSpan.FromHours(11), listed[0].Date);
        Assert.DoesNotContain(listed, snapshot => snapshot.Date < T0 + TimeSpan.FromHours(2));
    }

    [Fact]
    public void AutoSnapshotRunsAtMostOncePerTwelveHoursAndOnlyWithProgress()
    {
        var clock = new MutableClock { Now = T0 };
        Directory.CreateDirectory(_dir);
        var engine = BuildEngine(clock);
        // Baseline seed only — no progress yet, no snapshot.
        engine.ApplyUsage(new Dictionary<string, long> { ["claude_code"] = 0 }, "2026-10-19", true);
        Assert.Empty(SaveSnapshotManager.ListSnapshots(StatePath));

        clock.Now += TimeSpan.FromHours(1);
        engine.ApplyUsage(new Dictionary<string, long> { ["claude_code"] = 5_000_000 },
            "2026-10-19", true);
        Assert.Single(SaveSnapshotManager.ListSnapshots(StatePath));

        clock.Now += TimeSpan.FromHours(2);
        engine.ApplyUsage(new Dictionary<string, long> { ["claude_code"] = 100_000_000 },
            "2026-10-19", true);
        Assert.Single(SaveSnapshotManager.ListSnapshots(StatePath)); // within the 12h interval

        clock.Now += TimeSpan.FromHours(13);
        engine.ApplyUsage(new Dictionary<string, long> { ["claude_code"] = 150_000_000 },
            "2026-10-19", true);
        Assert.Equal(2, SaveSnapshotManager.ListSnapshots(StatePath).Count);
    }

    [Fact]
    public void CorruptStateFileRecoversFromTheLatestSnapshot()
    {
        var clock = new MutableClock { Now = T0 };
        Directory.CreateDirectory(_dir);
        var engine = BuildEngine(clock);
        engine.State.Dex.Add(new DexEntry(150, 150, [150], Rarity.Legendary, clock.Now));
        engine.State.UsedSinceInstall = 42_000_000;
        var snapshot = engine.CreateManualSnapshot();
        Assert.Equal(1, snapshot.DexCount);

        File.WriteAllText(StatePath, "{ this is not a state file");
        var reopened = BuildEngine(clock);

        Assert.Equal(1, reopened.State.Dex.Count);
        Assert.Equal(42_000_000, reopened.State.UsedSinceInstall);
        Assert.True(File.Exists(StatePath + ".corrupt"), "the corrupt original is preserved");
        using var document = JsonDocument.Parse(File.ReadAllText(StatePath));
        Assert.Equal(1, CompanionStateCodec.Read(document.RootElement).Dex.Count);
    }

    [Fact]
    public void RestoreRollsBackProgressAndPreservesCurrentStateAsSnapshot()
    {
        var clock = new MutableClock { Now = T0 };
        Directory.CreateDirectory(_dir);
        var engine = BuildEngine(clock);
        engine.State.Dex.Add(new DexEntry(1, 3, [1, 2, 3], Rarity.Common, clock.Now));
        engine.State.UsedSinceInstall = 10_000_000;
        var earlier = engine.CreateManualSnapshot();

        engine.State.Dex.Add(new DexEntry(25, 26, [25, 26], Rarity.Rare, clock.Now));
        engine.State.UsedSinceInstall = 99_000_000;
        Assert.Equal(2, engine.State.Dex.Count);

        clock.Now += TimeSpan.FromSeconds(1);
        engine.RestoreSnapshot(earlier);

        Assert.Single(engine.State.Dex);
        Assert.Equal(10_000_000, engine.State.UsedSinceInstall);
        var snapshots = engine.AvailableSnapshots();
        Assert.Equal(2, snapshots.Count);
        // The pre-restore state is preserved as the newest snapshot.
        Assert.Equal(2, snapshots[0].DexCount);
    }

    /// <summary>
    /// Regression: at the 10-snapshot retention limit, the pre-restore safety snapshot prunes
    /// the oldest file — restoring the oldest entry must still succeed because the envelope is
    /// read and validated before anything is written or pruned.
    /// </summary>
    [Fact]
    public void RestoreOldestSnapshotAtRetentionLimitSucceeds()
    {
        var clock = new MutableClock { Now = T0 };
        Directory.CreateDirectory(_dir);
        for (var i = 0; i < 10; i++)
            SaveSnapshotManager.CreateSnapshot(State(i + 1, (i + 1) * 1_000_000), StatePath,
                T0 + TimeSpan.FromHours(i), "0.30.0", "pc");
        var engine = BuildEngine(clock);
        engine.State.Dex.Add(new DexEntry(50, 50, [50], Rarity.Rare, clock.Now));

        var oldest = SaveSnapshotManager.ListSnapshots(StatePath)[^1];
        engine.RestoreSnapshot(oldest);

        Assert.Single(engine.State.Dex);
        Assert.Equal(1_000_000, engine.State.UsedSinceInstall);
    }

    [Fact]
    public void RestoreUnreadableSnapshotLeavesStateAndSnapshotsUntouched()
    {
        var clock = new MutableClock { Now = T0 };
        Directory.CreateDirectory(_dir);
        var engine = BuildEngine(clock);
        engine.State.Dex.Add(new DexEntry(1, 3, [1, 2, 3], Rarity.Common, clock.Now));
        var snapshot = engine.CreateManualSnapshot();
        var countBefore = engine.AvailableSnapshots().Count;
        File.WriteAllText(snapshot.FilePath, "{\"format\":\"poketokenbar.save\",\"schema\":2," +
                                            "\"state\":{ this is broken");

        Assert.Throws<SaveTransferException>(() => engine.RestoreSnapshot(snapshot));
        Assert.Single(engine.State.Dex);
        // Nothing is deleted on failure; the now-broken file simply no longer decodes.
        Assert.True(File.Exists(snapshot.FilePath));
        Assert.Equal(countBefore - 1, engine.AvailableSnapshots().Count);
    }

    [Fact]
    public void CorruptSnapshotFileIsSkippedGracefully()
    {
        Directory.CreateDirectory(_dir);
        var dir = SaveSnapshotManager.SnapshotsDirectory(StatePath);
        File.WriteAllText(Path.Combine(dir, SaveSnapshotManager.SnapshotFileName(T0)), "garbage");
        var good = SaveSnapshotManager.CreateSnapshot(State(1, 100), StatePath,
            T0 + TimeSpan.FromHours(1), "0.30.0", "pc");

        var listed = SaveSnapshotManager.ListSnapshots(StatePath);
        Assert.Equal([good.FileName], listed.Select(snapshot => snapshot.FileName));
        Assert.Null(SaveSnapshotManager.LoadLatestValidSnapshot(Path.Combine(_dir, "other-state.json")));
    }

    [Fact]
    public void SnapshotStringsExistInAllLanguages()
    {
        foreach (var lang in AppLanguages.All)
        {
            Assert.False(string.IsNullOrEmpty(DashboardText.SnapshotsSectionTitle(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.CreateSnapshotButton(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.CreateSnapshotHint(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.SnapshotCreatedToast(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RestoreSnapshotButton(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RestoreConfirmTitle(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.RestoreConfirmBody(
                lang, "2026-10-19", 3, "1.2M", 5, "9.9M")));
            Assert.False(string.IsNullOrEmpty(DashboardText.RestoreDoneMessage(lang, 3, "1.2M")));
            Assert.False(string.IsNullOrEmpty(DashboardText.NoSnapshotsYet(lang)));
            Assert.False(string.IsNullOrEmpty(DashboardText.SnapshotDexAndTokens(lang, 3, "1.2M")));
        }
    }
}
