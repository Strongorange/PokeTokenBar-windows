using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows;

public static class UsageRootDiscovery
{
    private static readonly string[] NonUserDistroNames = ["docker-desktop", "docker-desktop-data"];

    public static IReadOnlyList<UsageRoot> Discover(UsageRootOptions? options = null)
    {
        var o = options ?? new UsageRootOptions();
        var candidates = new List<UsageRoot>();
        candidates.AddRange(ExistingProfileRoots(o));
        candidates.AddRange(ExistingExtraRoots(o));
        if (o.IncludeWsl) candidates.AddRange(WslRoots(o));
        return Collapse(candidates);
    }

    private static IEnumerable<UsageRoot> ExistingProfileRoots(UsageRootOptions o)
    {
        foreach (var root in ProfileCandidates(o))
            if (RootExists(o.FileSystem, root))
                yield return root;
    }

    private static IEnumerable<UsageRoot> ProfileCandidates(UsageRootOptions o)
    {
        yield return new UsageRoot(UsageRootKind.ClaudeProjects,
            Path.Combine(o.UserProfile, ".claude", "projects"));
        yield return new UsageRoot(UsageRootKind.CodexSessions,
            Path.Combine(o.UserProfile, ".codex", "sessions"));
        yield return new UsageRoot(UsageRootKind.CodexArchivedSessions,
            Path.Combine(o.UserProfile, ".codex", "archived_sessions"), Optional: true);
    }

    private static IEnumerable<UsageRoot> ExistingExtraRoots(UsageRootOptions o)
    {
        foreach (var raw in o.ExtraClaudeRoots)
        {
            var root = ExtraCandidate(o, raw, UsageRootKind.ClaudeProjects);
            if (root is not null) yield return root.Value;
        }
        foreach (var raw in o.ExtraCodexRoots)
        {
            var root = ExtraCandidate(o, raw, UsageRootKind.CodexSessions);
            if (root is not null) yield return root.Value;
        }
    }

    private static UsageRoot? ExtraCandidate(UsageRootOptions o, string raw, UsageRootKind kind)
    {
        var normalized = PathNormalizer.Normalize(raw);
        if (normalized is null)
        {
            AppLog.Write($"scan root dropped, invalid path: {raw}");
            return null;
        }
        var root = new UsageRoot(kind, normalized);
        return RootExists(o.FileSystem, root) ? root : null;
    }

    private static bool RootExists(IFileSystemSource fileSystem, UsageRoot root)
    {
        try
        {
            if (fileSystem.Exists(root.Path)) return true;
            if (!root.Optional) AppLog.Write($"scan root missing: {root.Path}");
            return false;
        }
        catch (Exception ex)
        {
            AppLog.Write($"scan root check failed: {root.Path}: {ex.Message}");
            return false;
        }
    }

    private static IEnumerable<UsageRoot> WslRoots(UsageRootOptions o)
    {
        var source = o.WslDistroSource ?? new RegistryWslDistroSource();
        IReadOnlyList<string> distros;
        try
        {
            distros = source.DistributionNames();
        }
        catch (Exception ex)
        {
            AppLog.Write($"wsl distro enumeration failed: {ex.Message}");
            distros = [];
        }
        foreach (var distro in distros)
        {
            if (NonUserDistroNames.Contains(distro, StringComparer.OrdinalIgnoreCase)) continue;
            foreach (var root in ProbeDistro(o, distro))
                yield return root;
        }
    }

    private static IReadOnlyList<UsageRoot> ProbeDistro(UsageRootOptions o, string distro)
    {
        var task = Task.Run(() => DistroRoots(o.FileSystem, distro));
        task.ContinueWith(t => { if (t.IsFaulted) _ = t.Exception; }, TaskScheduler.Default);
        try
        {
            if (!task.Wait(o.WslProbeTimeout))
            {
                AppLog.Write($"wsl distro probe timed out after {o.WslProbeTimeout.TotalSeconds:0.##}s: {distro}");
                return [];
            }
            return task.Result;
        }
        catch (AggregateException ex)
        {
            AppLog.Write($"wsl distro probe failed: {distro}: {ex.InnerException?.Message ?? ex.Message}");
            return [];
        }
    }

    private static IReadOnlyList<UsageRoot> DistroRoots(IFileSystemSource fileSystem, string distro)
    {
        var home = $@"\\wsl.localhost\{distro}\home";
        if (!fileSystem.Exists(home))
        {
            AppLog.Write($"wsl distro home not reachable: {home}");
            return [];
        }
        var roots = new List<UsageRoot>();
        foreach (var userDirectory in fileSystem.GetDirectories(home))
        {
            AddWslRoot(roots, fileSystem, UsageRootKind.ClaudeProjects,
                Path.Combine(userDirectory, ".claude", "projects"));
            AddWslRoot(roots, fileSystem, UsageRootKind.CodexSessions,
                Path.Combine(userDirectory, ".codex", "sessions"));
            AddWslRoot(roots, fileSystem, UsageRootKind.CodexArchivedSessions,
                Path.Combine(userDirectory, ".codex", "archived_sessions"));
        }
        return roots;
    }

    private static void AddWslRoot(List<UsageRoot> roots, IFileSystemSource fileSystem, UsageRootKind kind, string path)
    {
        try
        {
            if (!fileSystem.Exists(path)) return;
        }
        catch (Exception ex)
        {
            AppLog.Write($"wsl root check failed: {path}: {ex.Message}");
            return;
        }
        roots.Add(new UsageRoot(kind, path));
    }

    private static IReadOnlyList<UsageRoot> Collapse(List<UsageRoot> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<UsageRoot>();
        foreach (var root in candidates)
        {
            var normalized = PathNormalizer.Normalize(root.Path);
            if (normalized is null)
            {
                AppLog.Write($"scan root dropped, unnormalizable path: {root.Path}");
                continue;
            }
            if (!seen.Add(RootKey(root.Kind, normalized))) continue;
            result.Add(root with { Path = normalized });
        }
        return result;
    }

    private static string RootKey(UsageRootKind kind, string path) => $"{kind}|{path}";
}
