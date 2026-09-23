using System.Text;
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

public sealed class IncrementalLogScanner<TPayload>
{
    public delegate TPayload ParseFile(string path, IReadOnlyList<string> lines);

    private readonly IFileSystemSource _fileSystem;
    private readonly UsageScanCache<TPayload> _cache;

    public IncrementalLogScanner(IFileSystemSource fileSystem, UsageScanCache<TPayload> cache)
    {
        _fileSystem = fileSystem;
        _cache = cache;
    }

    public IReadOnlyList<ScannedFile<TPayload>> Scan(IEnumerable<string> roots, DateTimeOffset modifiedSince, ParseFile parse)
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
                ScanOne(normalized, modifiedSince, parse, results);
            }
        }
        _cache.Save();
        return results;
    }

    private void ScanOne(string path, DateTimeOffset modifiedSince, ParseFile parse, List<ScannedFile<TPayload>> results)
    {
        FileFingerprint fingerprint;
        try
        {
            var probe = _fileSystem.GetFingerprint(path);
            if (probe is null) return;
            fingerprint = probe.Value;
        }
        catch (Exception ex)
        {
            AppLog.Write($"file stat failed: {path}: {ex.Message}");
            return;
        }
        if (fingerprint.MtimeUtc < modifiedSince) return;
        if (_cache.TryGet(path, fingerprint, out var cached))
        {
            results.Add(new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.Cached, cached));
            return;
        }
        List<string>? lines = null;
        try
        {
            lines = ReadAllLines(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Write($"log read failed, keeping previous data: {path}: {ex.Message}");
        }
        if (lines is null)
        {
            AddStale(path, fingerprint, results);
            return;
        }
        TPayload? payload;
        try
        {
            payload = parse(path, lines);
        }
        catch (Exception ex)
        {
            AppLog.Write($"log parse failed, keeping previous data: {path}: {ex.Message}");
            AddStale(path, fingerprint, results);
            return;
        }
        _cache.Put(path, fingerprint, payload);
        results.Add(new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.Parsed, payload));
    }

    private void AddStale(string path, FileFingerprint fingerprint, List<ScannedFile<TPayload>> results)
    {
        if (!_cache.TryGetAny(path, out var stale)) return;
        results.Add(new ScannedFile<TPayload>(path, fingerprint.MtimeUtc, fingerprint.Size, ScannedFileStatus.ReadFailed, stale));
    }

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

    private static List<string> ReadAllLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var lines = SplitLines(text, out var droppedPartialFinalLine);
        if (droppedPartialFinalLine)
            AppLog.Write($"partial final line skipped: {path}");
        return lines;
    }

    public static List<string> SplitLines(string text, out bool droppedPartialFinalLine)
    {
        droppedPartialFinalLine = false;
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            var end = i;
            if (end > start && text[end - 1] == '\r') end--;
            lines.Add(text[start..end]);
            start = i + 1;
        }
        if (start < text.Length) droppedPartialFinalLine = true;
        return lines;
    }
}
