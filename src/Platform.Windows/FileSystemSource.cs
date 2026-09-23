namespace PokeTokenBar.Platform.Windows;

public readonly record struct FileFingerprint(DateTimeOffset MtimeUtc, long Size);

public interface IFileSystemSource
{
    bool Exists(string path);
    DateTimeOffset? LastWriteUtc(string directoryPath);
    IReadOnlyList<string> GetDirectories(string directoryPath);
    IReadOnlyList<string> GetFiles(string directoryPath);
    FileFingerprint? GetFingerprint(string filePath);
}

public sealed class PhysicalFileSystemSource : IFileSystemSource
{
    public static readonly PhysicalFileSystemSource Instance = new();

    private PhysicalFileSystemSource()
    {
    }

    public bool Exists(string path) => Directory.Exists(path) || File.Exists(path);

    public DateTimeOffset? LastWriteUtc(string directoryPath)
    {
        var t = Directory.GetLastWriteTimeUtc(directoryPath);
        return t == DateTime.MinValue ? null : new DateTimeOffset(t, TimeSpan.Zero);
    }

    public IReadOnlyList<string> GetDirectories(string directoryPath) => Directory.GetDirectories(directoryPath);

    public IReadOnlyList<string> GetFiles(string directoryPath) => Directory.GetFiles(directoryPath);

    public FileFingerprint? GetFingerprint(string filePath)
    {
        var info = new FileInfo(filePath);
        return info.Exists ? new FileFingerprint(new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero), info.Length) : null;
    }
}

public sealed class CachedFileSystemSource : IFileSystemSource
{
    private sealed record Listing(DateTimeOffset Stamp, IReadOnlyList<string> Directories, IReadOnlyList<string> Files);

    private readonly IFileSystemSource _inner;
    private readonly object _gate = new();
    private readonly Dictionary<string, Listing> _listings = new(StringComparer.OrdinalIgnoreCase);

    public CachedFileSystemSource(IFileSystemSource inner)
    {
        _inner = inner;
    }

    public bool Exists(string path) => _inner.Exists(path);

    public FileFingerprint? GetFingerprint(string filePath) => _inner.GetFingerprint(filePath);

    public DateTimeOffset? LastWriteUtc(string directoryPath) => _inner.LastWriteUtc(directoryPath);

    public IReadOnlyList<string> GetDirectories(string directoryPath)
    {
        var listing = ListingFor(directoryPath);
        return listing?.Directories ?? [];
    }

    public IReadOnlyList<string> GetFiles(string directoryPath)
    {
        var listing = ListingFor(directoryPath);
        return listing?.Files ?? [];
    }

    private Listing? ListingFor(string path)
    {
        lock (_gate)
        {
            var stamp = _inner.LastWriteUtc(path);
            if (stamp is null) return null;
            if (_listings.TryGetValue(path, out var hit) && hit.Stamp == stamp) return hit;
            var listing = new Listing(stamp.Value, _inner.GetDirectories(path), _inner.GetFiles(path));
            _listings[path] = listing;
            return listing;
        }
    }
}
