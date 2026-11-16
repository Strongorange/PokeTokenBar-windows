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

    public static string OpenCodeScratchDirectory(string cacheDirectory) =>
        Path.Combine(cacheDirectory, "opencode");

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
            OpenCode(fs, OpenCodeScratchDirectory(cacheDirectory)),
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
                    return ProviderRefreshOutcome.From(false, EmptySnapshot(provider));
                var floor = UsageAggregation.HeatmapScanStart(now, timeZone);
                var files = scanner.Scan(roots.Select(root => root.Path), floor, ClaudeParser(timeZone));
                var snapshot = provider.BuildSnapshot(files.Select(file => file.Payload ?? []));
                return ProviderRefreshOutcome.From(true, snapshot);
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
                    return ProviderRefreshOutcome.From(false, EmptySnapshot(provider));
                var rootPaths = roots.Select(root => root.Path).ToList();
                var floor = UsageAggregation.HeatmapScanStart(now, timeZone);
                var allFiles = CodexRolloutEnumeration.EnumerateAll(fileSystem, rootPaths);
                var windowFiles = allFiles.Where(file => file.MtimeUtc >= floor).ToList();
                var parser = CodexParser(timeZone);
                scanner.Scan(rootPaths, floor, parser);
                var (rollouts, includedPaths) = CodexParentClosure.Expand(
                    windowFiles,
                    allFiles,
                    file => LoadRollout(scanner, parser, file, timeZone),
                    _ => CodexSessionIdKnowledge.Unknown,
                    probe);
                var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
                cache.Save();
                return new ProviderRefreshOutcome(true, provider.ProviderId, provider.DisplayName, DailyUsageTable.From(entries));
            });
    }

    public static ProviderRegistration OpenCode(IFileSystemSource fileSystem, string scratchDirectory)
    {
        var provider = new OpenCodeUsageProvider();
        var reader = new OpenCodeDbReader();
        return new ProviderRegistration(
            provider.ProviderId,
            provider.DisplayName,
            new HashSet<UsageRootKind> { UsageRootKind.OpenCodeData },
            (now, timeZone, roots) =>
            {
                var available = roots.Count > 0;
                if (!available)
                    return ProviderRefreshOutcome.From(false, EmptySnapshot(provider));
                var floorMillis = UsageAggregation.HeatmapScanStart(now, timeZone).ToUnixTimeMilliseconds();
                var payloads = roots
                    .Select(root => reader.ReadEntries(root.Path, fileSystem, floorMillis, timeZone, scratchDirectory))
                    .ToList();
                return ProviderRefreshOutcome.From(true, provider.BuildSnapshot(payloads));
            });
    }

    private static UsageProviderSnapshot EmptySnapshot<TPayload>(IUsageProvider<TPayload> provider) =>
        new(provider.ProviderId, provider.DisplayName, []);

    public static LogParser<List<UsageEntry>> ClaudeParser(TimeZoneInfo timeZone) => new(
        ClaudeLogParser.MightMatter,
        (_, lines, resumeFrom) => ClaudeLogParser.Parse(lines, timeZone, resumeFrom));

    public static LogParser<CodexParsedRollout> CodexParser(TimeZoneInfo timeZone) => new(
        CodexLogParser.MightMatter,
        (path, lines, resumeFrom) => CodexLogParser.Parse(path, lines, timeZone, resumeFrom));

    private static CodexParsedRollout LoadRollout(
        IncrementalLogScanner<CodexParsedRollout> scanner,
        LogParser<CodexParsedRollout> parser,
        CodexRolloutFile file,
        TimeZoneInfo timeZone) =>
        scanner.Load(file.Path, new FileFingerprint(file.MtimeUtc, file.Size), parser)?.Payload
        ?? CodexLogParser.Parse(file.Path, [], timeZone);
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
