using System.Security.Cryptography;
using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public sealed class CompanionEngineOptions
{
    public string StateFilePath { get; init; } = CompanionStateFile.DefaultPath();

    public Func<DateTimeOffset> Clock { get; init; } = static () => DateTimeOffset.UtcNow;

    public Func<ulong> NextRoll { get; init; } = static () =>
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return BitConverter.ToUInt64(bytes);
    };

    public PokemonLineSource Lines { get; init; } = PokemonLineSource.Default();

    public string AppVersion { get; init; } =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";

    public string DeviceName { get; init; } = Environment.MachineName;
}

public sealed record CompanionEvent(DateTimeOffset At, string Text);

public sealed record CompanionStageItem(string Label, bool Done, bool Current, bool Mystery);

public sealed record CompanionDexRow(int SpeciesID, string Name, Rarity Rarity, bool IsShiny, bool IsRaising);

public sealed record CompanionGameView(
    bool HasActive,
    string ActiveName,
    Rarity? Rarity,
    bool IsShiny,
    bool HasGrowthBoost,
    int StageIndex,
    int TotalForms,
    long StageUsed,
    long StageThreshold,
    double StageProgress,
    bool IsEgg,
    long EggUsed,
    long EggThreshold,
    double EggProgress,
    int DexCount,
    long LifetimeTokens,
    long AvailableTokens,
    IReadOnlyList<CompanionStageItem> StageItems,
    IReadOnlyList<CompanionDexRow> DexRows,
    IReadOnlyList<CompanionEvent> RecentEvents);

public sealed class CompanionEngine
{
    private const int MaxEvents = 8;
    private const int HatchRollAttempts = 8;

    private readonly CompanionEngineOptions _options;
    private readonly object _gate = new();
    private readonly List<CompanionEvent> _events = [];
    private CompanionState _state = new();
    private IReadOnlyDictionary<string, long>? _lastMap;
    private string _lastDate = "";
    private bool _lastHasUsage;

    public CompanionEngine(CompanionEngineOptions? options = null)
    {
        _options = options ?? new CompanionEngineOptions();
        _state = CompanionStateFile.Load(_options.StateFilePath);
        var changed = NormalizeActive();
        changed |= MigrateProfilesIfNeeded();
        if (changed) SaveCore();
    }

    public event Action? Changed;

    public CompanionState State
    {
        get
        {
            lock (_gate) return _state;
        }
    }

    public void ApplyUsage(UsageDisplayState displayState, TimeZoneInfo? timeZone = null)
    {
        var todayDate = UsageAggregation.LocalDay(displayState.AsOfUtc, timeZone ?? TimeZoneInfo.Local);
        var map = displayState.Providers
            .Where(provider => provider.Available)
            .ToDictionary(provider => provider.ProviderId, provider => provider.TodayTokens);
        ApplyUsage(map, todayDate, displayState.Providers.Any(provider => provider.Available));
    }

    public void ApplyUsage(IReadOnlyDictionary<string, long> todayTokensByProvider, string todayDate,
        bool hasUsageData)
    {
        bool changed;
        lock (_gate)
        {
            changed = ApplyUsageCore(todayTokensByProvider, todayDate, hasUsageData);
            if (changed) SaveCore();
        }
        if (changed) Changed?.Invoke();
    }

    private bool ApplyUsageCore(IReadOnlyDictionary<string, long> map, string todayDate, bool hasUsageData)
    {
        _lastMap = new Dictionary<string, long>(map);
        _lastDate = todayDate;
        _lastHasUsage = hasUsageData;
        var hasCurrentProviderData = hasUsageData && map.Count > 0;
        var changed = false;
        if (!_state.InstallBaselineSet)
        {
            if (!hasCurrentProviderData)
            {
                changed |= HatchIfReady();
                return changed;
            }
            _state.InstallBaselineSet = true;
            _state.ClaimedTodayTokensByProvider = new Dictionary<string, long>(map);
            _state.LastDate = todayDate;
            changed = true;
        }
        else if (hasCurrentProviderData)
        {
            long delta;
            if (_state.ClaimedTodayTokensByProvider is not { } ledger)
            {
                _state.ClaimedTodayTokensByProvider = new Dictionary<string, long>(map);
                _state.LastDate = todayDate;
                AppLog.Write("companion provider ledger seeded " +
                             $"date={todayDate} providers={string.Join(",", map.Keys.Order())}");
                changed = true;
            }
            else if (todayDate != _state.LastDate)
            {
                _state.LastDate = todayDate;
                var newLedger = ledger.Keys.ToDictionary(key => key, _ => 0L);
                foreach (var (provider, current) in map) newLedger[provider] = current;
                _state.ClaimedTodayTokensByProvider = newLedger;
                delta = map.Values.Sum();
                changed = true;
                if (delta > 0) ApplyDelta(delta);
            }
            else
            {
                var updated = new Dictionary<string, long>(ledger);
                delta = 0;
                foreach (var (provider, current) in map)
                {
                    if (!updated.TryGetValue(provider, out var previous))
                    {
                        updated[provider] = current;
                        continue;
                    }
                    if (current < previous)
                    {
                        updated[provider] = current;
                        AppLog.Write($"companion usage regression provider={provider} date={todayDate} " +
                                     $"previous={previous} current={current} drop={previous - current} — rebased provider ledger");
                        continue;
                    }
                    delta += current - previous;
                    updated[provider] = current;
                }
                if (!SameLedger(updated, ledger))
                {
                    _state.ClaimedTodayTokensByProvider = updated;
                    changed = true;
                }
                if (delta > 0) ApplyDelta(delta);
            }
        }
        changed |= HatchIfReady();
        return changed;
    }

    private void ApplyDelta(long delta)
    {
        _state.UsedSinceInstall += delta;
        if (_state.Active is null) _state.EggUsage += delta;
        else ApplyGrowth(delta);
    }

    private static bool SameLedger(Dictionary<string, long> a, Dictionary<string, long> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var (key, value) in a)
            if (!b.TryGetValue(key, out var other) || other != value)
                return false;
        return true;
    }

    private bool HatchIfReady()
    {
        if (_state.Active is not null || !_state.InstallBaselineSet) return false;
        if (_state.EggUsage < PokemonBalance.EggHatchThreshold) return false;

        for (var attempt = 0; attempt < HatchRollAttempts; attempt++)
        {
            int baseID;
            int? pendingID = null;
            if (_state.PendingHatchID is { } pending)
            {
                pendingID = pending;
                baseID = pending;
            }
            else
            {
                var rolled = ChooseBase(_state.EggTier);
                if (rolled is null) return false;
                baseID = rolled.Value;
            }
            if (_options.Lines.Line(baseID) is not { } line)
            {
                _state.PendingHatchID = null;
                _state.PendingUnownForm = null;
                continue;
            }
            if (_state.EggTier is { } tier && line.Rarity.SortRank() < tier.SortRank())
            {
                AppLog.Write($"hatch: rolled {Rarities.Raw(line.Rarity)} below guaranteed " +
                             $"{Rarities.Raw(tier)} — discarded, re-roll");
                _state.PendingHatchID = null;
                _state.PendingUnownForm = null;
                continue;
            }
            var pendingForm = pendingID == baseID ? _state.PendingUnownForm : null;
            _state.PendingHatchID = null;
            _state.PendingUnownForm = null;
            var overflow = Math.Max(0, _state.EggUsage - PokemonBalance.EggHatchThreshold);
            _state.EggUsage = 0;
            _state.EggTier = null;
            var isShiny = PokemonOdds.RollsShiny(_options.NextRoll(), OwnsShinyCharm);
            var nature = PokemonNatures.All[(int)(_options.NextRoll() % (ulong)PokemonNatures.All.Length)];
            int? dittoDisguise = DittoDisguiseHit(line.Rarity, line.TotalForms, _options.NextRoll())
                ? line.BaseID
                : null;
            var plan = MakeEvolutionPlan(line.Tree, line.BaseID);
            var profile = PokemonProfile.Generate(_options.NextRoll());
            var hasGrowthBoost = _state.HasCollectedFinal(line.BaseID);
            UnownForm? unownForm = line.BaseID == UnownForms.SpeciesID
                ? pendingForm ?? UnownForms.Roll(_options.NextRoll(), _state.CollectedUnownForms())
                : null;
            _state.Active = new MonState(
                line.BaseID, [line.BaseID], plan, 0, 0, line.Rarity, plan.Count,
                isShiny, nature, profile, hasGrowthBoost, dittoDisguise, unownForm: unownForm);
            AppLog.Write($"hatch: base={line.BaseID} rarity={Rarities.Raw(line.Rarity)} shiny={isShiny} " +
                         $"forms={plan.Count} boost={hasGrowthBoost} ditto={dittoDisguise is not null}");
            AddEvent(EventText.Hatch, DisplayName(line, line.BaseID, unownForm), isShiny);
            if (overflow > 0) ApplyGrowth(overflow);
            return true;
        }
        return false;
    }

    internal static bool DittoDisguiseHit(Rarity rarity, int totalForms, ulong roll) =>
        rarity == Rarity.Common && totalForms >= 2
        && roll % PokemonOdds.DittoDisguiseDenominator == 0;

    private int? ChooseBase(Rarity? tier)
    {
        IEnumerable<BaseSpecies> candidates = _options.Lines.Bases;
        if (tier is { } guaranteed) candidates = candidates.Where(b => guaranteed.Includes(b.CaptureRate));
        var pool = candidates.ToList();
        if (pool.Count == 0)
        {
            var tierName = tier is { } tierValue ? Rarities.Raw(tierValue) : "none";
            AppLog.Write($"hatch: no candidate for guaranteed {tierName} — egg kept, retry next refresh");
            return null;
        }
        var weights = pool.Select(b =>
            (long)CollectionWeight.Adjusted(b.CaptureRate, _state.HasCollectedFinal(b.Id))).ToArray();
        var total = weights.Sum();
        var remaining = (long)(_options.NextRoll() % (ulong)total);
        for (var i = 0; i < pool.Count - 1; i++)
        {
            remaining -= weights[i];
            if (remaining < 0) return pool[i].Id;
        }
        return pool[^1].Id;
    }

    private bool ApplyGrowth(long delta)
    {
        if (_state.Active is null) return false;
        var mutated = delta > 0;
        _state.Active.UsedAtStage += delta;
        ReconcileActiveProfileGrowth();
        var guard = 0;
        while (_state.Active is { } active && guard++ < 50)
        {
            var threshold = StageThreshold(active);
            if (active.UsedAtStage < threshold) break;
            if (_options.Lines.Line(active.BaseID) is not { } line) break;
            if (line.Tree.NodeWithID(active.CurrentID) is not { } node) break;
            if (active.DittoDisguise is not null && !active.DittoRevealed)
            {
                RevealDitto();
                break;
            }
            if (node.Children.Count == 0)
            {
                Graduate();
                mutated = true;
                break;
            }
            var nextIndex = active.StageIndex + 1;
            EvoNode next;
            if (active.PlannedPathIDs.Count > nextIndex
                && node.Children.FirstOrDefault(child => child.SpeciesID == active.PlannedPathIDs[nextIndex])
                    is { } planned)
            {
                next = planned;
            }
            else
            {
                next = PickPlannedChild(node, active.BaseID);
                var fallbackRoute = new List<int> { node.SpeciesID };
                fallbackRoute.AddRange(MakeEvolutionPlan(next, active.BaseID));
                var repaired = RepairedPlan(active.PathIDs, active.StageIndex, fallbackRoute);
                _state.Active.PlannedPathIDs = repaired;
                _state.Active.TotalForms = repaired.Count;
                AppLog.Write($"evolve: repaired invalid planned path for base {active.BaseID}");
            }
            _state.Active.PathIDs =
                active.PathIDs.Take(active.StageIndex + 1).Append(next.SpeciesID).ToList();
            _state.Active.StageIndex = active.StageIndex + 1;
            _state.Active.UsedAtStage = active.UsedAtStage - threshold;
            var newName = DisplayName(line, next.SpeciesID, _state.Active.UnownForm);
            AddEvent(EventText.Evolve, newName);
            mutated = true;
        }
        ReconcileActiveProfileGrowth();
        return mutated;
    }

    private void RevealDitto()
    {
        if (_state.Active is not { } active || active.DittoDisguise is null || active.DittoRevealed) return;
        if (_options.Lines.Line(PokemonOdds.DittoSpeciesID) is not { } dittoLine) return;
        var threshold = StageThreshold(active);
        if (active.UsedAtStage < threshold) return;
        var carryOver = Math.Max(0, active.UsedAtStage - threshold);
        var previousRarity = active.Rarity;
        var previousBaseID = active.BaseID;
        var disguiseName = _options.Lines.Line(active.BaseID) is { } disguise
            ? DisplayName(disguise, active.CurrentID, active.UnownForm)
            : $"#{active.CurrentID}";
        active.BaseID = dittoLine.BaseID;
        var plan = MakeEvolutionPlan(dittoLine.Tree, dittoLine.BaseID);
        active.PathIDs = [dittoLine.BaseID];
        active.PlannedPathIDs = plan;
        active.StageIndex = 0;
        active.Rarity = dittoLine.Rarity;
        active.TotalForms = plan.Count;
        active.UsedAtStage = carryOver;
        active.DittoRevealed = true;
        active.Profile?.RebaseForSpeciesIdentity(previousRarity, dittoLine.Rarity);
        _state.Active = active;
        _state.ReconcileRepresentativeSelection();
        AppLog.Write($"ditto reveal: disguise={previousBaseID} → ditto " +
                     $"rarity={Rarities.Raw(dittoLine.Rarity)} shiny={active.IsShiny}");
        AddEvent(EventText.DittoReveal, disguiseName, active.IsShiny);
        ApplyGrowth(0);
    }

    private void Graduate()
    {
        if (_state.Active is not { } active) return;
        active.Profile?.AdvanceGrowth(PokemonBalance.GraduationTotal(active.Rarity), active.Rarity);
        var finalID = active.CurrentID;
        _state.CollectedFinals.Add($"{active.BaseID}:{finalID}");
        Dictionary<int, Dictionary<string, string>>? names = null;
        if (_options.Lines.Line(active.BaseID) is { } line)
        {
            names = [];
            foreach (var id in active.PathIDs)
                if (line.Names.TryGetValue(id, out var byLang))
                    names[id] = byLang;
        }
        _state.Dex.Add(new DexEntry(
            active.BaseID, finalID, new List<int>(active.PathIDs), active.Rarity, _options.Clock(),
            active.IsShiny, active.Nature, active.Profile, names, unownForm: active.UnownForm,
            id: active.Profile?.InstanceID ?? Guid.NewGuid().ToString()));
        var name = DisplayName(_options.Lines.Line(active.BaseID), finalID, active.UnownForm);
        AppLog.Write($"graduate: base={active.BaseID} final={finalID} shiny={active.IsShiny} " +
                     $"dex={_state.Dex.Count}");
        AddEvent(EventText.Graduate, name, active.IsShiny);
        _state.Active = null;
        _state.ReconcileRepresentativeSelection();
        _state.EggUsage = 0;
    }

    private EvoNode PickPlannedChild(EvoNode node, int baseID)
    {
        var fresh = node.Children
            .Where(child => child.FinalIDs.Any(finalID =>
                !_state.CollectedFinals.Contains($"{baseID}:{finalID}")))
            .ToList();
        var pool = fresh.Count == 0 ? node.Children : fresh;
        return pool[(int)(_options.NextRoll() % (ulong)pool.Count)];
    }

    private List<int> MakeEvolutionPlan(EvoNode root, int baseID)
    {
        var plan = new List<int> { root.SpeciesID };
        var node = root;
        while (node.Children.Count > 0)
        {
            var next = PickPlannedChild(node, baseID);
            plan.Add(next.SpeciesID);
            node = next;
        }
        return plan;
    }

    internal static List<int> RepairedPlan(List<int> realizedPath, int stageIndex, List<int> fallbackRoute)
    {
        if (realizedPath.Count == 0) return fallbackRoute;
        var currentIndex = Math.Min(stageIndex, realizedPath.Count - 1);
        var prefix = realizedPath.Take(currentIndex + 1).ToList();
        if (fallbackRoute.Count == 0 || fallbackRoute[0] != prefix[^1]) return prefix;
        prefix.AddRange(fallbackRoute.Skip(1));
        return prefix;
    }

    private (List<int> Path, EvoNode LastNode) LongestValidPath(List<int> ids, EvoNode root)
    {
        var path = new List<int> { root.SpeciesID };
        var node = root;
        if (ids.Count == 0 || ids[0] != root.SpeciesID) return (path, node);
        foreach (var id in ids.Skip(1))
        {
            EvoNode? child = null;
            foreach (var candidate in node.Children)
                if (candidate.SpeciesID == id)
                {
                    child = candidate;
                    break;
                }
            if (child is null) break;
            path.Add(id);
            node = child;
        }
        return (path, node);
    }

    private bool NormalizeActive()
    {
        if (_state.Active is not { } saved) return false;
        if (_options.Lines.Line(saved.BaseID) is not { } line) return false;
        var realized = LongestValidPath(saved.PathIDs, line.Tree);
        var candidate = LongestValidPath(saved.PlannedPathIDs, line.Tree);
        var canReuse = candidate.Path.SequenceEqual(saved.PlannedPathIDs)
            && candidate.Path.Take(realized.Path.Count).SequenceEqual(realized.Path)
            && candidate.LastNode.Children.Count == 0;
        List<int> plan;
        if (canReuse)
        {
            plan = candidate.Path;
        }
        else
        {
            var suffix = MakeEvolutionPlan(realized.LastNode, saved.BaseID);
            plan = new List<int>(realized.Path);
            plan.AddRange(suffix.Skip(1));
        }
        if (saved.PathIDs.SequenceEqual(realized.Path)
            && saved.PlannedPathIDs.SequenceEqual(plan)
            && saved.StageIndex == realized.Path.Count - 1
            && saved.TotalForms == plan.Count)
            return false;
        saved.PathIDs = realized.Path;
        saved.PlannedPathIDs = plan;
        saved.StageIndex = realized.Path.Count - 1;
        saved.TotalForms = plan.Count;
        _state.Active = saved;
        return true;
    }

    private bool MigrateProfilesIfNeeded()
    {
        var changed = false;
        if (_state.Active is { } active && active.Profile is null)
        {
            var key = $"active:{active.BaseID}:{string.Join(",", active.PathIDs)}:{_state.LastDate}";
            active.Profile = PokemonProfile.Generate(PokemonProfileMigration.Seed(key));
            _state.Active = active;
            changed = true;
        }
        foreach (var entry in _state.Dex)
        {
            if (entry.Profile is not null) continue;
            var graduated = !entry.IsReleased;
            var growth = graduated
                ? PokemonBalance.GraduationTotal(entry.Rarity)
                : ReconstructedGrowthTokens(entry.Rarity, Math.Max(1, entry.ChainOrder.Count),
                    Math.Max(0, entry.ChainOrder.Count - 1), 0);
            var profile = PokemonProfile.Generate(
                PokemonProfileMigration.Seed($"dex:{entry.ID}:{entry.FinalID}"),
                growth, entry.ID);
            profile.ApplyGrowth(0, entry.Rarity);
            entry.Profile = profile;
            changed = true;
        }
        return changed;
    }

    private void ReconcileActiveProfileGrowth()
    {
        if (_state.Active is not { } active || active.Profile is not { } profile) return;
        var completed = ReconstructedGrowthTokens(active.Rarity, active.TotalForms, active.StageIndex, 0);
        var standardPhase = PokemonBalance.PhaseThreshold(
            active.Rarity, active.TotalForms, active.StageIndex);
        var threshold = Math.Max(1, StageThreshold(active));
        var fraction = Math.Min(1, Math.Max(0, active.UsedAtStage / (double)threshold));
        var candidate = Math.Min(PokemonBalance.GraduationTotal(active.Rarity),
            completed + (long)Math.Floor(standardPhase * fraction));
        profile.AdvanceGrowth(candidate, active.Rarity);
        active.Profile = profile;
    }

    internal static long ReconstructedGrowthTokens(Rarity rarity, int totalForms,
        int completedStages, long currentStageUsage)
    {
        var forms = Math.Max(1, totalForms);
        var completed = Math.Min(Math.Max(0, completedStages), forms);
        long completedGrowth = 0;
        for (var stage = 0; stage < completed; stage++)
            completedGrowth += PokemonBalance.PhaseThreshold(rarity, forms, stage);
        return Math.Min(SaveTransfer.MaxTokenValue,
            completedGrowth + Math.Min(SaveTransfer.MaxTokenValue, Math.Max(0, currentStageUsage)));
    }

    private static long StageThreshold(MonState mon) => mon.PhaseThreshold;

    private bool OwnsShinyCharm =>
        _state.Inventory.TryGetValue(ItemKinds.Raw(ItemKind.ShinyCharm), out var count) && count > 0;

    private string DisplayName(EvoLine? line, int speciesID, UnownForm? unownForm)
    {
        var raw = line is not null
            ? line.LocalizedName(speciesID, _state.Language)
            : $"#{speciesID}";
        return UnownForms.DisplayName(raw, speciesID, unownForm);
    }

    private void AddEvent(string kind, string name, bool shiny = false)
    {
        var text = kind switch
        {
            EventText.Hatch => shiny ? $"Shiny {name} hatched!" : $"{name} hatched!",
            EventText.Evolve => $"{name} — evolved!",
            EventText.Graduate => shiny ? $"Shiny {name} graduated into the dex!" : $"{name} graduated into the dex!",
            EventText.DittoReveal => shiny
                ? $"{name} was a shiny Ditto in disguise!"
                : $"{name} was a Ditto in disguise!",
            _ => name
        };
        _events.Add(new CompanionEvent(_options.Clock(), text));
        if (_events.Count > MaxEvents) _events.RemoveRange(0, _events.Count - MaxEvents);
    }

    private static class EventText
    {
        public const string Hatch = "hatch";
        public const string Evolve = "evolve";
        public const string Graduate = "graduate";
        public const string DittoReveal = "ditto";
    }

    public byte[] ExportSave()
    {
        lock (_gate) return SaveTransfer.Encode(_state, _options.AppVersion, _options.DeviceName, _options.Clock());
    }

    public string SuggestedExportFileName() =>
        SaveTransfer.SuggestedFileName(_options.Clock());

    public void ImportSave(byte[] data)
    {
        lock (_gate)
        {
            var envelope = SaveTransfer.Decode(data);
            var directory = Path.GetDirectoryName(_options.StateFilePath);
            if (string.IsNullOrEmpty(directory))
                throw new SaveTransferException(SaveTransferError.BackupFailed);
            var currentJson = CompanionStateCodec.Write(_state, SaveDateMode.Iso8601)
                .ToJsonString(CompanionStateFile.IndentedJson);
            var backupPath = Path.Combine(directory, SaveTransfer.BackupFileName(_options.Clock()));
            try
            {
                File.WriteAllText(backupPath, currentJson);
            }
            catch (Exception ex)
            {
                AppLog.Write($"save import aborted — backup write failed: {ex.Message}");
                throw new SaveTransferException(SaveTransferError.BackupFailed);
            }
            PruneImportBackups(directory);
            _state = SaveTransfer.RebasedForThisDevice(
                envelope.State, _state,
                _lastMap ?? new Dictionary<string, long>(), _lastDate, _lastHasUsage);
            NormalizeActive();
            MigrateProfilesIfNeeded();
            _events.Clear();
            SaveCore();
            AppLog.Write($"save imported from {envelope.SourceDevice}: " +
                         $"dex={_state.Dex.Count} lifetime={_state.UsedSinceInstall}");
        }
        Changed?.Invoke();
    }

    private static void PruneImportBackups(string directory)
    {
        try
        {
            var backups = Directory.GetFiles(directory, SaveTransfer.BackupFilePrefix + "*")
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            if (backups.Count <= SaveTransfer.BackupsToKeep) return;
            foreach (var stale in backups.Take(backups.Count - SaveTransfer.BackupsToKeep))
                File.Delete(stale);
        }
        catch (Exception ex)
        {
            AppLog.Write($"import backup prune failed: {ex.Message}");
        }
    }

    public CompanionGameView View()
    {
        lock (_gate) return BuildView();
    }

    private CompanionGameView BuildView()
    {
        var active = _state.Active;
        var line = active is not null ? _options.Lines.Line(active.BaseID) : null;
        var hasActive = active is not null;
        var stageThreshold = active is not null ? Math.Max(1, StageThreshold(active)) : 1;
        var stageUsed = active?.UsedAtStage ?? 0;
        var stageProgress = hasActive ? Math.Min(1, stageUsed / (double)stageThreshold) : 0;
        var eggUsed = _state.EggUsage;
        var eggProgress = Math.Min(1, eggUsed / (double)PokemonBalance.EggHatchThreshold);
        return new CompanionGameView(
            hasActive,
            hasActive ? DisplayName(line, active!.CurrentID, active.UnownForm) : "Token Egg",
            active?.Rarity,
            hasActive && (active!.DittoDisguise is null || active.DittoRevealed) && active.IsShiny,
            active?.HasGrowthBoost ?? false,
            active?.StageIndex ?? 0,
            active?.TotalForms ?? 0,
            stageUsed,
            stageThreshold,
            stageProgress,
            !hasActive,
            eggUsed,
            PokemonBalance.EggHatchThreshold,
            eggProgress,
            _state.Dex.Count,
            _state.UsedSinceInstall,
            Math.Max(0, _state.UsedSinceInstall - _state.SpentTokens),
            hasActive ? BuildStageItems(active!, line) : [],
            BuildDexRows(),
            _events.ToList());
    }

    private IReadOnlyList<CompanionStageItem> BuildStageItems(MonState active, EvoLine? line)
    {
        var items = new List<CompanionStageItem>();
        for (var i = 0; i <= active.StageIndex && i < active.PathIDs.Count; i++)
            items.Add(new CompanionStageItem(
                DisplayName(line, active.PathIDs[i], active.UnownForm),
                i < active.StageIndex, i == active.StageIndex, false));
        if (line is not null && line.Tree.NodeWithID(active.CurrentID) is { } current)
        {
            var node = current;
            var guaranteed = new List<EvoNode>();
            while (node.Children.Count == 1 && node.Children[0] is { } child)
            {
                guaranteed.Add(child);
                node = child;
            }
            items.AddRange(guaranteed.Select(next => new CompanionStageItem(
                DisplayName(line, next.SpeciesID, active.UnownForm), false, false, false)));
            if (node.Children.Count > 1)
                items.Add(new CompanionStageItem("???", false, false, true));
        }
        return items;
    }

    private IReadOnlyList<CompanionDexRow> BuildDexRows()
    {
        var rows = new Dictionary<int, (string Name, Rarity Rarity, bool IsShiny)>();
        foreach (var entry in _state.Dex)
            foreach (var id in entry.ChainOrder)
            {
                var name = entry.Names is not null && entry.Names.TryGetValue(id, out var byLang)
                    ? _state.Language.ResolveName(byLang) ?? $"#{id}"
                    : $"#{id}";
                var shiny = entry.IsShiny;
                if (rows.TryGetValue(id, out var existing))
                    rows[id] = (existing.Name, existing.Rarity, existing.IsShiny || shiny);
                else
                    rows[id] = (name, entry.Rarity, shiny);
            }
        var currentID = _state.Active?.CurrentID;
        if (_state.Active is { } active)
            for (var i = 0; i <= active.StageIndex && i < active.PathIDs.Count; i++)
            {
                var id = active.PathIDs[i];
                var name = DisplayName(_options.Lines.Line(active.BaseID), id, active.UnownForm);
                var shiny = (active.DittoDisguise is null || active.DittoRevealed) && active.IsShiny;
                if (rows.TryGetValue(id, out var existing))
                    rows[id] = (existing.Name, existing.Rarity, existing.IsShiny || shiny);
                else
                    rows[id] = (name, active.Rarity, shiny);
            }
        return rows.Keys.OrderBy(id => id)
            .Select(id =>
            {
                var (name, rarity, shiny) = rows[id];
                return new CompanionDexRow(id, name, rarity, shiny, id == currentID);
            })
            .ToList();
    }

    private void SaveCore()
    {
        try
        {
            CompanionStateFile.Save(_options.StateFilePath, _state);
        }
        catch (Exception ex)
        {
            AppLog.Write($"companion state save failed: {ex.Message}");
        }
    }
}
