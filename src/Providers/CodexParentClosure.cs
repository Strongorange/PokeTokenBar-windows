namespace PokeTokenBar.Providers;

public sealed record CodexRolloutFile(string Path, DateTimeOffset MtimeUtc, long Size);

public readonly record struct CodexSessionIdKnowledge(bool Known, string? SessionId)
{
    public static CodexSessionIdKnowledge Unknown { get; } = new(false, null);

    public static CodexSessionIdKnowledge Of(string? sessionId) => new(true, sessionId);
}

public static class CodexParentClosure
{
    public static (List<CodexParsedRollout> Rollouts, HashSet<string> IncludedPaths) Expand(
        IReadOnlyList<CodexRolloutFile> windowFiles,
        IReadOnlyList<CodexRolloutFile> allFiles,
        Func<CodexRolloutFile, CodexParsedRollout> load,
        Func<CodexRolloutFile, CodexSessionIdKnowledge> sessionIdKnowledge,
        Func<CodexRolloutFile, string?> probeSessionId)
    {
        var rolloutsByPath = new Dictionary<string, CodexParsedRollout>();
        foreach (var file in windowFiles)
        {
            var rollout = load(file);
            rolloutsByPath[rollout.Path] = rollout;
        }
        var includedPaths = windowFiles.Select(f => f.Path).ToHashSet(StringComparer.Ordinal);

        var pendingParentIds = rolloutsByPath.Values
            .Where(r => r.ParentSessionID is not null)
            .Select(r => r.ParentSessionID!)
            .ToHashSet(StringComparer.Ordinal);
        var searchedParentIds = new HashSet<string>(StringComparer.Ordinal);
        while (pendingParentIds.Except(searchedParentIds).FirstOrDefault() is { } parentId)
        {
            searchedParentIds.Add(parentId);
            if (rolloutsByPath.Values.Any(r => r.SessionID == parentId)) continue;

            bool Adopt(IEnumerable<CodexRolloutFile> candidates)
            {
                var resolvedAny = false;
                foreach (var candidate in candidates)
                {
                    var parent = load(candidate);
                    if (parent.SessionID != parentId) continue;
                    rolloutsByPath[parent.Path] = parent;
                    resolvedAny = true;
                    if (parent.ParentSessionID is { } ancestorId) pendingParentIds.Add(ancestorId);
                }
                return resolvedAny;
            }

            var unresolved = allFiles.Where(f => !rolloutsByPath.ContainsKey(f.Path)).ToList();
            var hinted = new List<CodexRolloutFile>();
            foreach (var file in unresolved)
            {
                var knowledge = sessionIdKnowledge(file);
                if (knowledge.Known)
                {
                    if (knowledge.SessionId == parentId) hinted.Add(file);
                }
                else if (IsUsableFilenameHint(parentId) &&
                         Path.GetFileName(file.Path).Contains(parentId, StringComparison.Ordinal))
                {
                    hinted.Add(file);
                }
            }
            if (Adopt(hinted)) continue;

            var hintedPaths = hinted.Select(f => f.Path).ToHashSet(StringComparer.Ordinal);
            _ = Adopt(unresolved
                .Where(f => !hintedPaths.Contains(f.Path) &&
                            !sessionIdKnowledge(f).Known &&
                            probeSessionId(f) == parentId)
                .ToList());
        }
        return ([.. rolloutsByPath.Values], includedPaths);
    }

    public static bool IsUsableFilenameHint(string id) =>
        id.Length >= 4 && id.Any(char.IsLetterOrDigit);
}
