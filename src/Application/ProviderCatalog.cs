using System.Text;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application;

public static class ProviderCatalog
{
    public const string ClaudeCacheFileName = "usage-cache-claude.json";
    public const string CodexCacheFileName = "usage-cache-codex.json";

    public static string ClaudeCachePath(string cacheDirectory) =>
        Path.Combine(cacheDirectory, ClaudeCacheFileName);

    public static string CodexCachePath(string cacheDirectory) =>
        Path.Combine(cacheDirectory, CodexCacheFileName);

    public static IReadOnlyList<ProviderRegistration> Default(
        string cacheDirectory,
        IFileSystemSource? fileSystem = null,
        Func<CodexRolloutFile, string?>? probeSessionId = null)
    {
        var fs = fileSystem ?? new CachedFileSystemSource(PhysicalFileSystemSource.Instance);
        return
        [
            Claude(fs, new UsageScanCache<List<UsageEntry>>(
                ClaudeCachePath(cacheDirectory), ClaudeLogParser.ParserVersion)),
            Codex(fs, new UsageScanCache<CodexParsedRollout>(
                CodexCachePath(cacheDirectory), CodexLogParser.ParserVersion), probeSessionId),
        ];
    }

    public static ProviderRegistration Claude(
        IFileSystemSource fileSystem, UsageScanCache<List<UsageEntry>> cache)
    {
        var provider = new ClaudeUsageProvider();
        var scanner = new IncrementalLogScanner<List<UsageEntry>>(fileSystem, cache);
        return new ProviderRegistration(
            provider.ProviderId,
            provider.DisplayName,
            new HashSet<UsageRootKind> { UsageRootKind.ClaudeProjects },
            (now, timeZone, roots) =>
            {
                var available = roots.Count > 0;
                if (!available)
                    return new ProviderRefreshOutcome(false, EmptySnapshot(provider));
                var floor = UsageAggregation.EnrichmentScanStart(now, timeZone);
                var files = scanner.Scan(
                    roots.Select(root => root.Path),
                    floor,
                    (_, lines) => ClaudeLogParser.Parse(lines, timeZone));
                var snapshot = provider.BuildSnapshot(files.Select(file => file.Payload ?? []));
                return new ProviderRefreshOutcome(true, snapshot);
            });
    }

    public static ProviderRegistration Codex(
        IFileSystemSource fileSystem,
        UsageScanCache<CodexParsedRollout> cache,
        Func<CodexRolloutFile, string?>? probeSessionId = null)
    {
        var provider = new CodexUsageProvider();
        var scanner = new IncrementalLogScanner<CodexParsedRollout>(fileSystem, cache);
        var probe = probeSessionId ?? (file => CodexRolloutProbe.ProbeFile(file.Path));
        return new ProviderRegistration(
            provider.ProviderId,
            provider.DisplayName,
            new HashSet<UsageRootKind> { UsageRootKind.CodexSessions, UsageRootKind.CodexArchivedSessions },
            (now, timeZone, roots) =>
            {
                var available = roots.Count > 0;
                if (!available)
                    return new ProviderRefreshOutcome(false, EmptySnapshot(provider));
                var rootPaths = roots.Select(root => root.Path).ToList();
                var floor = UsageAggregation.EnrichmentScanStart(now, timeZone);
                var allFiles = CodexRolloutEnumeration.EnumerateAll(fileSystem, rootPaths);
                var windowFiles = allFiles.Where(file => file.MtimeUtc >= floor).ToList();
                scanner.Scan(rootPaths, floor, (path, lines) => CodexLogParser.Parse(path, lines, timeZone));
                var (rollouts, includedPaths) = CodexParentClosure.Expand(
                    windowFiles,
                    allFiles,
                    file => LoadThroughCache(cache, file, timeZone),
                    _ => CodexSessionIdKnowledge.Unknown,
                    probe);
                var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
                cache.Save();
                return new ProviderRefreshOutcome(true,
                    new UsageProviderSnapshot(provider.ProviderId, provider.DisplayName, entries));
            });
    }

    private static UsageProviderSnapshot EmptySnapshot<TPayload>(IUsageProvider<TPayload> provider) =>
        new(provider.ProviderId, provider.DisplayName, []);

    private static CodexParsedRollout LoadThroughCache(
        UsageScanCache<CodexParsedRollout> cache, CodexRolloutFile file, TimeZoneInfo timeZone)
    {
        var fingerprint = new FileFingerprint(file.MtimeUtc, file.Size);
        if (cache.TryGet(file.Path, fingerprint, out var cached) && cached is not null)
            return cached;
        var rollout = CodexLogParser.Parse(file.Path, ReadLogLines(file.Path), timeZone);
        cache.Put(file.Path, fingerprint, rollout);
        return rollout;
    }

    private static List<string> ReadLogLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        return IncrementalLogScanner<CodexParsedRollout>.SplitLines(text, out _);
    }
}

internal static class CodexRolloutEnumeration
{
    public static List<CodexRolloutFile> EnumerateAll(IFileSystemSource fileSystem, IEnumerable<string> roots)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<CodexRolloutFile>();
        foreach (var root in roots)
        {
            if (!Exists(fileSystem, root)) continue;
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                if (!TryList(fileSystem, directory, out var subdirectories, out var fileNames)) continue;
                foreach (var file in fileNames)
                {
                    if (!IncrementalLogScanner<CodexParsedRollout>.IsLogFile(file)) continue;
                    var normalized = PathNormalizer.Normalize(file);
                    if (normalized is null || !seen.Add(normalized)) continue;
                    var fingerprint = Fingerprint(fileSystem, normalized);
                    if (fingerprint is null) continue;
                    files.Add(new CodexRolloutFile(normalized, fingerprint.Value.MtimeUtc, fingerprint.Value.Size));
                }
                foreach (var subdirectory in subdirectories)
                    if (!IsHiddenName(subdirectory))
                        pending.Push(subdirectory);
            }
        }
        return files;
    }

    private static bool Exists(IFileSystemSource fileSystem, string root)
    {
        try
        {
            return fileSystem.Exists(root);
        }
        catch (Exception ex)
        {
            AppLog.Write($"codex root check failed: {root}: {ex.Message}");
            return false;
        }
    }

    private static bool TryList(
        IFileSystemSource fileSystem,
        string directory,
        out IReadOnlyList<string> subdirectories,
        out IReadOnlyList<string> files)
    {
        try
        {
            subdirectories = fileSystem.GetDirectories(directory);
            files = fileSystem.GetFiles(directory);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"directory listing failed: {directory}: {ex.Message}");
            subdirectories = [];
            files = [];
            return false;
        }
    }

    private static FileFingerprint? Fingerprint(IFileSystemSource fileSystem, string path)
    {
        try
        {
            return fileSystem.GetFingerprint(path);
        }
        catch (Exception ex)
        {
            AppLog.Write($"file stat failed: {path}: {ex.Message}");
            return null;
        }
    }

    private static bool IsHiddenName(string path)
    {
        var name = Path.GetFileName(path);
        return name.Length == 0 || name[0] == '.';
    }
}
