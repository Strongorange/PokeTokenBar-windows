using PokeTokenBar.Core;

namespace PokeTokenBar.Platform.Windows.Tests;

[Collection("Platform diagnostics")]
public class UsageRootDiscoveryTests : IDisposable
{
    private readonly string _dir;
    private const string Profile = @"C:\Users\ptb-fake";

    public UsageRootDiscoveryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-discovery-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private static void AddStandardTree(FakeFileSystemSource fs)
    {
        fs.AddDirectory(Profile + @"\.claude\projects");
        fs.AddDirectory(Profile + @"\.codex\sessions");
        fs.AddDirectory(@"\\wsl.localhost\Ubuntu-24.04\home");
        fs.AddDirectory(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu");
        fs.AddDirectory(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.claude\projects");
        fs.AddDirectory(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.codex\sessions");
        fs.AddDirectory(@"\\wsl.localhost\docker-desktop\home");
        fs.AddDirectory(@"\\wsl.localhost\docker-desktop\home\docker");
        fs.AddDirectory(@"\\wsl.localhost\docker-desktop\home\docker\.claude\projects");
        fs.AddDirectory(@"\\wsl.localhost\docker-desktop\home\docker\.codex\sessions");
    }

    private UsageRootOptions OptionsWith(FakeFileSystemSource fs, FakeWslDistroSource distros) => new()
    {
        UserProfile = Profile,
        FileSystem = fs,
        WslDistroSource = distros,
    };

    [Fact]
    public void FindsProfileAndWslRootsExcludingNonUserDistros()
    {
        var fs = new FakeFileSystemSource();
        AddStandardTree(fs);
        var distros = new FakeWslDistroSource
        {
            Names = ["Ubuntu-24.04", "docker-desktop", "docker-desktop-data"],
        };
        var roots = UsageRootDiscovery.Discover(OptionsWith(fs, distros));
        Assert.Equal(
        [
            new UsageRoot(UsageRootKind.ClaudeProjects, Profile + @"\.claude\projects"),
            new UsageRoot(UsageRootKind.CodexSessions, Profile + @"\.codex\sessions"),
            new UsageRoot(UsageRootKind.ClaudeProjects, @"\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.claude\projects"),
            new UsageRoot(UsageRootKind.CodexSessions, @"\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.codex\sessions"),
        ], roots);
        Assert.DoesNotContain(roots, r => r.Path.Contains("docker-desktop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IncludesArchivedSessionsOnlyWhenPresent()
    {
        var fs = new FakeFileSystemSource();
        fs.AddDirectory(Profile + @"\.claude\projects");
        fs.AddDirectory(Profile + @"\.codex\sessions");
        fs.AddDirectory(Profile + @"\.codex\archived_sessions");
        var roots = UsageRootDiscovery.Discover(new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            IncludeWsl = false,
        });
        Assert.Equal(3, roots.Count);
        Assert.Contains(roots, r => r.Kind == UsageRootKind.CodexArchivedSessions);
    }

    [Fact]
    public void OmitsMissingProfileRootsWithoutThrowing()
    {
        var fs = new FakeFileSystemSource();
        var roots = UsageRootDiscovery.Discover(new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            IncludeWsl = false,
        });
        Assert.Empty(roots);
        var log = Diagnostics.Read(_dir);
        Assert.Contains("scan root missing", log);
        Assert.DoesNotContain("archived_sessions", log);
    }

    [Fact]
    public void IncludesExistingExtraRootsAndLogsMissingOnes()
    {
        var fs = new FakeFileSystemSource();
        fs.AddDirectory(Profile + @"\.claude\projects");
        fs.AddDirectory(Profile + @"\.codex\sessions");
        fs.AddDirectory(@"D:\custom\claude-logs");
        var roots = UsageRootDiscovery.Discover(new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            IncludeWsl = false,
            ExtraClaudeRoots = [@"D:\custom\claude-logs", @"D:\gone\claude-logs"],
        });
        Assert.Contains(new UsageRoot(UsageRootKind.ClaudeProjects, @"D:\custom\claude-logs"), roots);
        Assert.DoesNotContain(roots, r => r.Path.Contains("gone", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("scan root missing", Diagnostics.Read(_dir));
    }

    [Fact]
    public void CollapsesExtraRootDuplicatingAutoRootWithDifferentCaseAndSlashes()
    {
        var fs = new FakeFileSystemSource();
        AddStandardTree(fs);
        var distros = new FakeWslDistroSource { Names = ["Ubuntu-24.04"] };
        var roots = UsageRootDiscovery.Discover(new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            WslDistroSource = distros,
            ExtraClaudeRoots = [@$"c:/users/PTB-FAKE/.claude/projects/"],
        });
        Assert.Single(roots, r => r.Kind == UsageRootKind.ClaudeProjects && !r.Path.StartsWith(@"\\"));
        Assert.Equal(4, roots.Count);
    }

    [Fact]
    public void CollapsesWslDollarExtraRootAgainstDiscoveredUncRoot()
    {
        var fs = new FakeFileSystemSource();
        AddStandardTree(fs);
        var distros = new FakeWslDistroSource { Names = ["Ubuntu-24.04"] };
        var roots = UsageRootDiscovery.Discover(new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            WslDistroSource = distros,
            ExtraClaudeRoots = [@"\\wsl$\Ubuntu-24.04\home\ubuntu\.claude\projects"],
        });
        var wslClaude = roots.Where(r => r.Kind == UsageRootKind.ClaudeProjects && r.Path.StartsWith(@"\\")).ToList();
        Assert.Single(wslClaude);
        Assert.Equal(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu\.claude\projects", wslClaude[0].Path);
    }

    [Fact]
    public void TimesOutGracefullyOnUnreachableWslDistro()
    {
        var fs = new FakeFileSystemSource
        {
            BlockMillis = 1500,
            BlockPathPrefix = @"\\wsl.localhost\Stopped",
        };
        var distros = new FakeWslDistroSource { Names = ["Stopped"] };
        var options = new UsageRootOptions
        {
            UserProfile = Profile,
            FileSystem = fs,
            WslDistroSource = distros,
            WslProbeTimeout = TimeSpan.FromMilliseconds(200),
        };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var roots = UsageRootDiscovery.Discover(options);
        stopwatch.Stop();
        Assert.Empty(roots);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1200),
            $"probe should time out quickly, took {stopwatch.Elapsed}");
        Assert.Contains("timed out", Diagnostics.Read(_dir));
    }

    [Fact]
    public void ReportsUnreachableWslHomeWithoutThrowing()
    {
        var fs = new FakeFileSystemSource();
        fs.AddDirectory(Profile + @"\.claude\projects");
        fs.AddDirectory(Profile + @"\.codex\sessions");
        var distros = new FakeWslDistroSource { Names = ["Ubuntu-24.04"] };
        var roots = UsageRootDiscovery.Discover(OptionsWith(fs, distros));
        Assert.Equal(
        [
            new UsageRoot(UsageRootKind.ClaudeProjects, Profile + @"\.claude\projects"),
            new UsageRoot(UsageRootKind.CodexSessions, Profile + @"\.codex\sessions"),
        ], roots);
        Assert.Contains("not reachable", Diagnostics.Read(_dir));
    }
}
