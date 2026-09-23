namespace PokeTokenBar.Platform.Windows.Tests;

internal sealed class FakeFileSystemSource : IFileSystemSource
{
    public readonly HashSet<string> Directories = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, long> FileSizes = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, DateTimeOffset> FileMtimes = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, DateTimeOffset> DirectoryMtimes = new(StringComparer.OrdinalIgnoreCase);
    public int BlockMillis;
    public string? BlockPathPrefix;

    public void AddDirectory(string path, DateTimeOffset? mtime = null)
    {
        Directories.Add(path);
        DirectoryMtimes[path] = mtime ?? DateTimeOffset.UtcNow;
    }

    public void AddFile(string path, long size, DateTimeOffset? mtime = null)
    {
        FileSizes[path] = size;
        FileMtimes[path] = mtime ?? DateTimeOffset.UtcNow;
    }

    private void SimulateBlocking(string path)
    {
        if (BlockMillis > 0 && BlockPathPrefix is not null &&
            path.StartsWith(BlockPathPrefix, StringComparison.OrdinalIgnoreCase))
            Thread.Sleep(BlockMillis);
    }

    public bool Exists(string path)
    {
        SimulateBlocking(path);
        return Directories.Contains(path) || FileSizes.ContainsKey(path);
    }

    public DateTimeOffset? LastWriteUtc(string directoryPath)
    {
        SimulateBlocking(directoryPath);
        return DirectoryMtimes.TryGetValue(directoryPath, out var mtime) ? mtime : null;
    }

    public IReadOnlyList<string> GetDirectories(string directoryPath)
    {
        SimulateBlocking(directoryPath);
        if (!Directories.Contains(directoryPath))
            throw new IOException($"directory not found: {directoryPath}");
        return Directories
            .Where(d => string.Equals(Path.GetDirectoryName(d), directoryPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<string> GetFiles(string directoryPath)
    {
        SimulateBlocking(directoryPath);
        if (!Directories.Contains(directoryPath))
            throw new IOException($"directory not found: {directoryPath}");
        return FileSizes.Keys
            .Where(f => string.Equals(Path.GetDirectoryName(f), directoryPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public FileFingerprint? GetFingerprint(string filePath) =>
        FileSizes.TryGetValue(filePath, out var size) && FileMtimes.TryGetValue(filePath, out var mtime)
            ? new FileFingerprint(mtime, size)
            : null;
}

internal sealed class FakeWslDistroSource : IWslDistroSource
{
    public IReadOnlyList<string> Names { get; set; } = [];

    public IReadOnlyList<string> DistributionNames() => Names;
}
