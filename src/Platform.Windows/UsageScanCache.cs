using System.Text.Json;
using System.Text.Json.Serialization;
using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows;

public sealed class UsageScanCache<TPayload>
{
    public const int CurrentFormatVersion = 1;
    public static readonly TimeSpan BlobRetention = TimeSpan.FromDays(40);

    private sealed class CacheEntry
    {
        public long MtimeUtcTicks { get; set; }
        public long Size { get; set; }
        public TPayload? Payload { get; set; }
    }

    private sealed class CacheFile
    {
        public int FormatVersion { get; set; }
        public int ParserVersion { get; set; }
        public Dictionary<string, CacheEntry>? Files { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;
    private readonly int _parserVersion;
    private readonly object _gate = new();
    private Dictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private bool _dirty;

    public UsageScanCache(string filePath, int parserVersion)
    {
        _filePath = filePath;
        _parserVersion = parserVersion;
        Load();
    }

    public int EntryCount
    {
        get
        {
            lock (_gate) return _entries.Count;
        }
    }

    public bool TryGet(string path, FileFingerprint fingerprint, out TPayload? payload)
    {
        lock (_gate)
        {
            payload = default;
            if (!_entries.TryGetValue(path, out var entry)) return false;
            if (entry.MtimeUtcTicks != fingerprint.MtimeUtc.UtcTicks || entry.Size != fingerprint.Size) return false;
            payload = entry.Payload;
            return true;
        }
    }

    public bool TryGetAny(string path, out TPayload? payload)
    {
        lock (_gate)
        {
            payload = default;
            if (!_entries.TryGetValue(path, out var entry)) return false;
            payload = entry.Payload;
            return true;
        }
    }

    public void Put(string path, FileFingerprint fingerprint, TPayload? payload)
    {
        lock (_gate)
        {
            _entries[path] = new CacheEntry
            {
                MtimeUtcTicks = fingerprint.MtimeUtc.UtcTicks,
                Size = fingerprint.Size,
                Payload = payload,
            };
            _dirty = true;
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var dto = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(_filePath), JsonOptions);
            if (dto is null) return;
            if (dto.FormatVersion != CurrentFormatVersion || dto.ParserVersion != _parserVersion)
            {
                _dirty = true;
                return;
            }
            var loaded = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
            if (dto.Files is not null)
                foreach (var pair in dto.Files)
                    loaded[pair.Key] = pair.Value;
            _entries = loaded;
        }
        catch (Exception ex)
        {
            AppLog.Write($"usage cache load failed, starting empty: {_filePath}: {ex.Message}");
            _entries = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
            _dirty = true;
        }
    }

    public void Save(DateTimeOffset? now = null)
    {
        lock (_gate)
        {
            if (!_dirty) return;
            PruneOldBlobs(now ?? DateTimeOffset.UtcNow);
            var dto = new CacheFile
            {
                FormatVersion = CurrentFormatVersion,
                ParserVersion = _parserVersion,
                Files = _entries,
            };
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var temp = _filePath + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(dto, JsonOptions));
                File.Move(temp, _filePath, overwrite: true);
                _dirty = false;
            }
            catch (Exception ex)
            {
                AppLog.Write($"usage cache save failed: {_filePath}: {ex.Message}");
            }
        }
    }

    private void PruneOldBlobs(DateTimeOffset now)
    {
        var cutoffTicks = (now - BlobRetention).UtcTicks;
        var survivors = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _entries)
            if (pair.Value.MtimeUtcTicks >= cutoffTicks)
                survivors[pair.Key] = pair.Value;
        if (survivors.Count != _entries.Count) _entries = survivors;
    }
}
