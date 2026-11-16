using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows;

public enum ScannedFileStatus
{
    Parsed,
    Cached,
    ReadFailed,
}

public sealed record ScannedFile<TPayload>(
    string Path,
    DateTimeOffset MtimeUtc,
    long Size,
    ScannedFileStatus Status,
    TPayload? Payload);

/// <summary>
/// Parses the lines of one log. <paramref name="resumeFrom"/> is the payload of the bytes already read when
/// the scanner continues an appended file, and null when it starts from the beginning.
/// </summary>
public delegate TPayload ParseLogLines<TPayload>(string path, IEnumerable<string> lines, TPayload? resumeFrom);

/// <param name="Filter">Byte-level prefilter; lines it rejects are never decoded or handed to the parser.</param>
public sealed record LogParser<TPayload>(LogLineFilter Filter, ParseLogLines<TPayload> Parse);

public sealed class IncrementalLogScanner<TPayload>
{
    private readonly IFileSystemSource _fileSystem;
    private readonly UsageScanCache<TPayload> _cache;

    public IncrementalLogScanner(IFileSystemSource fileSystem, UsageScanCache<TPayload> cache)
    {
        _fileSystem = fileSystem;
        _cache = cache;
    }

    public IReadOnlyList<ScannedFile<TPayload>> Scan(
        IEnumerable<string> roots, DateTimeOffset modifiedSince, LogParser<TPayload> parser)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ScannedFile<TPayload>>();
        foreach (var root in roots)
        {
            foreach (var file in EnumerateLogFiles(root))
            {
                var normalized = PathNormalizer.Normalize(file);
                if (normalized is null) continue;
                if (!seen.Add(normalized)) continue;
                if (Fingerprint(normalized) is not { } fingerprint) continue;
                if (fingerprint.MtimeUtc < modifiedSince) continue;
                if (Load(normalized, fingerprint, parser) is { } scanned) results.Add(scanned);
            }
        }
        _cache.Save();
        return results;
    }

    /// <summary>
    /// Brings one file's cached payload up to date, reading only the bytes appended since the last read.
    /// Returns null when the file cannot be read and nothing was cached for it.
    /// </summary>
    public ScannedFile<TPayload>? Load(string path, FileFingerprint fingerprint, LogParser<TPayload> parser)
    {
        if (_cache.TryGet(path, fingerprint, out var cached))
            return new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.Cached, cached);

        _cache.TryGetResume(path, out var previous, out var resume);
        var canResume = previous is not null && IsAppendOf(path, fingerprint, resume);
        var reader = new LogLineReader(path, canResume ? resume.Offset : 0, parser.Filter);
        TPayload payload;
        try
        {
            payload = parser.Parse(path, reader.Lines(), canResume ? previous : default);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Write($"log read failed, keeping previous data: {path}: {ex.Message}");
            return Stale(path, fingerprint);
        }
        catch (Exception ex)
        {
            AppLog.Write($"log parse failed, keeping previous data: {path}: {ex.Message}");
            return Stale(path, fingerprint);
        }
        if (reader.DroppedPartialFinalLine)
            AppLog.Write($"partial final line skipped: {path}");
        _cache.Put(path, fingerprint, payload, new LogResumePoint(reader.EndOffset, AnchorOrZero(path, reader.EndOffset)));
        return new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.Parsed, payload);
    }

    private FileFingerprint? Fingerprint(string path)
    {
        try
        {
            return _fileSystem.GetFingerprint(path);
        }
        catch (Exception ex)
        {
            AppLog.Write($"file stat failed: {path}: {ex.Message}");
            return null;
        }
    }

    private static bool IsAppendOf(string path, FileFingerprint fingerprint, LogResumePoint resume) =>
        resume.Offset > 0 &&
        fingerprint.Size >= resume.Offset &&
        AnchorOrZero(path, resume.Offset) == resume.Anchor;

    private static ulong AnchorOrZero(string path, long offset)
    {
        try
        {
            return LogLineReader.AnchorAt(path, offset);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private ScannedFile<TPayload>? Stale(string path, FileFingerprint fingerprint) =>
        _cache.TryGetAny(path, out var stale)
            ? new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.ReadFailed, stale)
            : null;

    private IEnumerable<string> EnumerateLogFiles(string root)
    {
        try
        {
            if (!_fileSystem.Exists(root))
            {
                AppLog.Write($"scan root missing: {root}");
                yield break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"scan root check failed: {root}: {ex.Message}");
            yield break;
        }
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            IReadOnlyList<string> files;
            IReadOnlyList<string> subdirectories;
            try
            {
                files = _fileSystem.GetFiles(dir);
                subdirectories = _fileSystem.GetDirectories(dir);
            }
            catch (Exception ex)
            {
                AppLog.Write($"directory listing failed: {dir}: {ex.Message}");
                continue;
            }
            foreach (var file in files)
                if (IsLogFile(file))
                    yield return file;
            foreach (var subdirectory in subdirectories)
                if (!IsHiddenName(subdirectory))
                    pending.Push(subdirectory);
        }
    }

    public static bool IsLogFile(string path) =>
        path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase) && !IsHiddenName(path);

    private static bool IsHiddenName(string path)
    {
        var name = Path.GetFileName(path);
        return name.Length == 0 || name[0] == '.';
    }
}
