using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

public class VersionTextTests
{
    [Theory]
    [InlineData("2.0.10", "2.0.9", true)]
    [InlineData("2.0.9", "2.0.10", false)]
    [InlineData("0.16.0", "0.16.0", false)]
    [InlineData("1.0", "1.0.0", false)]
    [InlineData("0.15.1", "0.16.0", false)]
    [InlineData("0.16.1", "0.16", true)]
    [InlineData("1.0.0.1", "1.0.0", true)]
    public void ComparesNumericSegmentsPerDot(string candidate, string baseline, bool expected)
    {
        Assert.Equal(expected, VersionText.IsNewer(candidate, baseline));
    }

    [Theory]
    [InlineData("v0.16.0", "0.15.0", true)]
    [InlineData("V0.16.0", "0.15.0", true)]
    [InlineData("v0.15.0", "0.15.0", false)]
    [InlineData(" 0.16.0 ", "0.15.0", true)]
    public void StripsTagPrefixAndWhitespace(string candidate, string baseline, bool expected)
    {
        Assert.Equal(expected, VersionText.IsNewer(candidate, baseline));
    }

    [Theory]
    [InlineData("1.x.2", "1.0.1", true)]
    [InlineData("a.9", "1", false)]
    [InlineData("", "0.0.1", false)]
    [InlineData("  ", "0", false)]
    public void NonNumericSegmentsCountAsZero(string candidate, string baseline, bool expected)
    {
        Assert.Equal(expected, VersionText.IsNewer(candidate, baseline));
    }

    [Fact]
    public void NormalizeRemovesOneLeadingV()
    {
        Assert.Equal("0.16.0", VersionText.Normalize("v0.16.0"));
        Assert.Equal("2", VersionText.Normalize("V2"));
        Assert.Equal("0.16.0", VersionText.Normalize("0.16.0"));
        Assert.Equal("v1", VersionText.Normalize("vv1"));
        Assert.Equal("", VersionText.Normalize(""));
    }
}
