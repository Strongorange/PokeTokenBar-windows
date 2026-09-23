using System.Xml.Linq;

namespace PokeTokenBar.Platform.Windows.Tests;

public sealed class PackagingTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PokeTokenBar.Windows.slnx")))
            dir = dir.Parent!;
        Assert.True(dir is not null, "repository root with PokeTokenBar.Windows.slnx not found");
        return dir.FullName;
    }

    [Fact]
    public void UiProjectDeclaresParseableVersion()
    {
        var csproj = XDocument.Load(Path.Combine(RepoRoot(), "src", "Ui", "PokeTokenBar.Ui.csproj"));
        var versionText = csproj.Descendants("Version").Single().Value.Trim();
        var version = Version.Parse(versionText);
        Assert.True(version > new Version(0, 0, 0), $"version {versionText} must be non-zero");
    }

    [Fact]
    public void UiProjectEmbedsWin32Icon()
    {
        var csproj = XDocument.Load(Path.Combine(RepoRoot(), "src", "Ui", "PokeTokenBar.Ui.csproj"));
        Assert.Equal(
            @"..\..\assets\pokeball.ico",
            csproj.Descendants("ApplicationIcon").Single().Value.Trim());
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "assets", "pokeball.ico")));
    }

    [Fact]
    public void PublishProfileDeclaresSelfContainedSingleFile()
    {
        var profile = XDocument.Load(Path.Combine(
            RepoRoot(), "src", "Ui", "Properties", "PublishProfiles", "win-x64.pubxml"));
        var properties = profile.Descendants("PropertyGroup").Single()
            .Elements().ToDictionary(e => e.Name.LocalName, e => e.Value.Trim());
        Assert.Equal("win-x64", properties["RuntimeIdentifier"]);
        Assert.Equal("true", properties["SelfContained"]);
        Assert.Equal("true", properties["PublishSingleFile"]);
        Assert.Equal("true", properties["IncludeNativeLibrariesForSelfExtract"]);
        Assert.Equal("true", properties["EnableCompressionInSingleFile"]);
    }
}
