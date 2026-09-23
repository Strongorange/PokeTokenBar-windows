namespace PokeTokenBar.Platform.Windows.Tests;

public class PathNormalizerTests
{
    [Fact]
    public void NormalizesForwardSlashesAndTrailingSeparators()
    {
        Assert.Equal(@"C:\Users\Foo\.claude\projects",
            PathNormalizer.Normalize(@"C:/Users/Foo/.claude/projects/"));
    }

    [Fact]
    public void CollapsesRelativeSegments()
    {
        Assert.Equal(@"C:\Users\Foo\.claude",
            PathNormalizer.Normalize(@"C:\Users\Foo\.claude\projects\.."));
    }

    [Fact]
    public void CanonicalizesWslDollarServerToWslLocalhost()
    {
        Assert.Equal(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu",
            PathNormalizer.Normalize(@"\\wsl$\Ubuntu-24.04\home\ubuntu"));
    }

    [Fact]
    public void CanonicalizesWslServerIgnoringCase()
    {
        Assert.Equal(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu",
            PathNormalizer.Normalize(@"\\WSL$\Ubuntu-24.04\home\ubuntu\"));
    }

    [Fact]
    public void NormalizesForwardSlashesOnUncPaths()
    {
        Assert.Equal(@"\\wsl.localhost\Ubuntu-24.04\home\ubuntu",
            PathNormalizer.Normalize(@"//wsl.localhost/Ubuntu-24.04/home/ubuntu"));
    }

    [Fact]
    public void ExpandsEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("PTB_TEST_ROOT", @"C:\ptb-test-root");
        try
        {
            Assert.Equal(@"C:\ptb-test-root\logs",
                PathNormalizer.Normalize(@"%PTB_TEST_ROOT%\logs"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PTB_TEST_ROOT", null);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyInput(string? raw)
    {
        Assert.Null(PathNormalizer.Normalize(raw));
    }
}
