using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class UsageCostTests
{
    [Fact]
    public void MergeIsUnionPerFlag()
    {
        var a = CostCoverage.Source;
        var b = CostCoverage.Unavailable;
        var merged = a.Merge(b);
        Assert.True(merged.Reported && merged.Unknown && !merged.Estimated);
        Assert.True(merged.HasKnown);
    }

    [Fact]
    public void ExplicitZeroIsAValidSourceAmount()
    {
        var bucket = new UsageBucket();
        bucket.Add(new UsageEntry("id", DateTimeOffset.UtcNow, "2026-09-22", "claude-sonnet-5",
            1000, 100, 0, 0, ExplicitCost: 0));
        Assert.Equal(0, bucket.Cost);
        Assert.True(bucket.CostCoverage.Reported);
        Assert.False(bucket.CostCoverage.Estimated);
    }

    [Fact]
    public void ZeroTotalEntryNeverTouchesCostCoverage()
    {
        var bucket = new UsageBucket();
        bucket.Add(new UsageEntry("id", DateTimeOffset.UtcNow, "2026-09-22", "some-unknown-model",
            0, 0, 0, 0));
        Assert.Equal(CostCoverage.Empty, bucket.CostCoverage);
    }

    [Fact]
    public void UnknownModelCountsTokensButMarksCostUnavailable()
    {
        var bucket = new UsageBucket();
        bucket.Add(new UsageEntry("id", DateTimeOffset.UtcNow, "2026-09-22", "glm-5.2",
            1000, 1000, 0, 0));
        Assert.Equal(2000, bucket.Total);
        Assert.Equal(0, bucket.Cost);
        Assert.True(bucket.CostCoverage.Unknown);
    }

    [Fact]
    public void CostUnavailableFlagSkipsEstimation()
    {
        var bucket = new UsageBucket();
        bucket.Add(new UsageEntry("id", DateTimeOffset.UtcNow, "2026-09-22", "claude-sonnet-5",
            1000, 1000, 0, 0, CostUnavailable: true));
        Assert.Equal(0, bucket.Cost);
        Assert.True(bucket.CostCoverage.Unknown);
    }

    [Fact]
    public void TextShowsUnavailableOnlyWhenUnknownWithoutKnown()
    {
        var unknown = new UsageCost(0, CostCoverage.Unavailable);
        Assert.Equal("$—", unknown.Text("Unavailable", compact: true));
        Assert.Equal("Unavailable", unknown.Text("Unavailable"));
        var partial = new UsageCost(1, CostCoverage.Source.Merge(CostCoverage.Unavailable));
        Assert.Equal("$1.00", partial.Text("Unavailable"));
    }
}
