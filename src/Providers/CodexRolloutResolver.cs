using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public static class CodexRolloutResolver
{
    public static readonly TimeSpan MaximumReplayGap = TimeSpan.FromSeconds(1);

    private sealed record ResolvedEvent(UsageEntry Entry, CodexUsageState? UsageState);

    private sealed record ResolvedRollout(List<ResolvedEvent> History, List<UsageEntry> OwnedEntries);

    public static List<UsageEntry> Resolve(IEnumerable<CodexParsedRollout> rollouts) =>
        Resolve(rollouts, includedPaths: null);

    public static List<UsageEntry> Resolve(
        IEnumerable<CodexParsedRollout> rollouts, IEnumerable<string>? includedPaths)
    {
        var list = rollouts.ToList();
        var bySession = new Dictionary<string, List<CodexParsedRollout>>();
        foreach (var rollout in list)
        {
            if (rollout.SessionID is not { } sessionID) continue;
            if (!bySession.TryGetValue(sessionID, out var group))
            {
                group = [];
                bySession[sessionID] = group;
            }
            group.Add(rollout);
        }
        foreach (var group in bySession.Values)
            group.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        var byPath = new Dictionary<string, CodexParsedRollout>();
        foreach (var rollout in list)
            if (!byPath.ContainsKey(rollout.Path))
                byPath[rollout.Path] = rollout;
        var memo = new Dictionary<string, ResolvedRollout>();
        var paths = includedPaths ?? byPath.Keys;
        var result = new List<UsageEntry>();
        foreach (var path in paths.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!byPath.TryGetValue(path, out var rollout)) continue;
            var visiting = new HashSet<string>();
            result.AddRange(ResolveRollout(rollout, bySession, byPath, memo, visiting).OwnedEntries);
        }
        return DedupKeepEarliest(result);
    }

    private static ResolvedRollout ResolveRollout(
        CodexParsedRollout rollout,
        Dictionary<string, List<CodexParsedRollout>> bySession,
        Dictionary<string, CodexParsedRollout> byPath,
        Dictionary<string, ResolvedRollout> memo,
        HashSet<string> visiting)
    {
        if (memo.TryGetValue(rollout.Path, out var cached)) return cached;
        if (!visiting.Add(rollout.Path))
            return ResolveOwnedEvents(rollout, FallbackReplayCount(rollout));
        try
        {
            (int ReplayCount, List<ResolvedEvent> History)? bestParentMatch = null;
            if (rollout.ParentSessionID is { } parentID &&
                bySession.TryGetValue(parentID, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Path == rollout.Path) continue;
                    var resolvedParent = ResolveRollout(candidate, bySession, byPath, memo, visiting);
                    if (ComparableUsagePrefixCount(rollout.Events, resolvedParent.History) is not { } replayCount ||
                        replayCount == 0)
                        continue;
                    if (bestParentMatch is null || replayCount > bestParentMatch.Value.ReplayCount)
                        bestParentMatch = (replayCount, resolvedParent.History);
                }
            }
            ResolvedRollout resolved;
            if (bestParentMatch is { } match)
            {
                var inherited = match.History.Take(match.ReplayCount).ToList();
                resolved = ResolveOwnedEvents(rollout, match.ReplayCount, inherited);
            }
            else if (rollout.ParentSessionID is not null)
            {
                resolved = ResolveOwnedEvents(rollout, FallbackReplayCount(rollout));
            }
            else
            {
                resolved = ResolveOwnedEvents(rollout, 0);
            }
            memo[rollout.Path] = resolved;
            return resolved;
        }
        finally
        {
            visiting.Remove(rollout.Path);
        }
    }

    private static ResolvedRollout ResolveOwnedEvents(
        CodexParsedRollout rollout, int replayCount, List<ResolvedEvent>? inheritedHistory = null)
    {
        List<ResolvedEvent> history = inheritedHistory is null ? [] : [.. inheritedHistory];
        var ownedEntries = new List<UsageEntry>();
        var epoch = 0;
        CodexUsageVector? previousCumulative = null;
        string? previousOwner = null;
        for (var i = replayCount; i < rollout.Events.Count; i++)
        {
            var evt = rollout.Events[i];
            var owner = rollout.ParentSessionID is null
                ? evt.SessionID ?? rollout.SessionID
                : rollout.SessionID;
            if (owner != previousOwner)
            {
                epoch = 0;
                previousCumulative = null;
                previousOwner = owner;
            }
            var priorCumulative = previousCumulative;
            if (evt.UsageState is { } state)
            {
                if (priorCumulative is { } prior && state.Cumulative.IsLowerThan(prior)) epoch++;
                previousCumulative = state.Cumulative;
            }
            else
            {
                previousCumulative = null;
            }
            UsageEntry entry;
            if (owner is not null && evt.UsageState is { } ownedState)
            {
                var filled = EntryTrustingTotalOnlyLast(evt.Entry, ownedState, priorCumulative);
                entry = filled with { Id = $"codex|{owner}|{epoch}|{ownedState.Fingerprint()}" };
            }
            else
            {
                entry = evt.Entry;
            }
            ownedEntries.Add(entry);
            history.Add(new ResolvedEvent(entry, evt.UsageState));
        }
        return new ResolvedRollout(history, ownedEntries);
    }

    private static UsageEntry EntryTrustingTotalOnlyLast(
        UsageEntry entry, CodexUsageState state, CodexUsageVector? previousCumulative)
    {
        if (entry.Total != 0) return entry;
        if (state.Last.BillableComponents() != 0) return entry;
        if (state.Last.Total <= 0) return entry;
        if (previousCumulative is not { } previous) return entry;
        if (state.Cumulative.Total <= previous.Total) return entry;
        return entry with
        {
            Input = state.Last.Total,
            Output = 0,
            CacheWrite = 0,
            CacheRead = 0,
            CostUnavailable = true,
        };
    }

    private static int? ComparableUsagePrefixCount(
        IReadOnlyList<CodexUsageEvent> child, List<ResolvedEvent> parent)
    {
        if (child.Count == 0) return 0;
        if (parent.Count == 0) return null;
        var count = 0;
        while (count < child.Count && count < parent.Count)
        {
            if (child[count].UsageState is not { } childState ||
                parent[count].UsageState is not { } parentState)
                return null;
            if (childState != parentState) break;
            count++;
        }
        return count;
    }

    private static int FallbackReplayCount(CodexParsedRollout rollout)
    {
        if (rollout.IsSubagent) return 0;
        var events = rollout.Events;
        if (events.Count <= 1) return events.Count == 0 ? 0 : 1;
        var count = 1;
        while (count < events.Count)
        {
            var gap = events[count].Entry.Date - events[count - 1].Entry.Date;
            if (gap >= MaximumReplayGap) break;
            count++;
        }
        return count;
    }

    private static List<UsageEntry> DedupKeepEarliest(List<UsageEntry> entries)
    {
        var byId = new Dictionary<string, UsageEntry>();
        var order = new List<string>();
        foreach (var entry in entries)
        {
            if (byId.TryGetValue(entry.Id, out var existing))
            {
                if (entry.Date < existing.Date) byId[entry.Id] = entry;
            }
            else
            {
                byId[entry.Id] = entry;
                order.Add(entry.Id);
            }
        }
        var result = new List<UsageEntry>(order.Count);
        foreach (var id in order) result.Add(byId[id]);
        return result;
    }
}
