namespace PokeTokenBar.Platform.Windows;

public enum UsageRootKind
{
    ClaudeProjects,
    CodexSessions,
    CodexArchivedSessions,
}

public readonly record struct UsageRoot(UsageRootKind Kind, string Path, bool Optional = false);

public sealed class UsageRootOptions
{
    public string UserProfile { get; init; } =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public IReadOnlyList<string> ExtraClaudeRoots { get; init; } = [];

    public IReadOnlyList<string> ExtraCodexRoots { get; init; } = [];

    public IWslDistroSource? WslDistroSource { get; init; }

    public IFileSystemSource FileSystem { get; init; } =
        new CachedFileSystemSource(PhysicalFileSystemSource.Instance);

    public bool IncludeWsl { get; init; } = true;

    public TimeSpan WslProbeTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
