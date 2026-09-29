using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

public class CompanionStatusTests
{
    [Fact]
    public void EggWhenNoActiveMon()
    {
        Assert.Equal(CompanionStatusKind.Egg,
            CompanionStatus.Compute(false, false, false, true, 500_000, 50_000));
    }

    [Fact]
    public void LevelUpWindowBeatsEverything()
    {
        Assert.Equal(CompanionStatusKind.LevelUp,
            CompanionStatus.Compute(true, true, true, false, 0, 0));
    }

    [Fact]
    public void LimitWarningIsTired()
    {
        Assert.Equal(CompanionStatusKind.Tired,
            CompanionStatus.Compute(true, false, true, true, 900_000, 50_000));
    }

    [Theory]
    [InlineData(false, 900_000)]
    [InlineData(true, 0)]
    public void SleepsWhenNoUsageDataOrNoTokensToday(bool hasUsageData, long todayTokens)
    {
        Assert.Equal(CompanionStatusKind.Sleep,
            CompanionStatus.Compute(true, false, false, hasUsageData, todayTokens, 50_000));
    }

    [Theory]
    [InlineData(0, CompanionBurnTier.Idle)]
    [InlineData(1_000, CompanionBurnTier.Idle)]
    [InlineData(1_001, CompanionBurnTier.Normal)]
    [InlineData(99_999, CompanionBurnTier.Normal)]
    [InlineData(100_000, CompanionBurnTier.Fast)]
    [InlineData(399_999, CompanionBurnTier.Fast)]
    [InlineData(400_000, CompanionBurnTier.Blazing)]
    public void BurnTierThresholdsMatchMacOS(double burn, CompanionBurnTier expected)
    {
        Assert.Equal(expected, CompanionStatus.BurnTier(burn));
    }

    [Fact]
    public void BurnTiersMapToDisplayedStates()
    {
        Assert.Equal(CompanionStatusKind.Idle,
            CompanionStatus.Compute(true, false, false, true, 1, 1_000));
        Assert.Equal(CompanionStatusKind.Working,
            CompanionStatus.Compute(true, false, false, true, 1, 1_001));
        Assert.Equal(CompanionStatusKind.Focus,
            CompanionStatus.Compute(true, false, false, true, 1, 100_000));
    }
}

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MissingTimestampBucketsToNone()
    {
        Assert.Equal(RelativeTimeBucket.None, RelativeTimes.Bucket(null, Now));
    }

    [Fact]
    public void SubHourBucketsToMinutes()
    {
        var caught = Now - TimeSpan.FromMinutes(42);
        Assert.Equal(RelativeTimeBucket.Minutes, RelativeTimes.Bucket(caught, Now));
        Assert.Equal(42, RelativeTimes.BucketValue(caught, Now));
    }

    [Fact]
    public void SubDayBucketsToHours()
    {
        var caught = Now - TimeSpan.FromHours(5);
        Assert.Equal(RelativeTimeBucket.Hours, RelativeTimes.Bucket(caught, Now));
        Assert.Equal(5, RelativeTimes.BucketValue(caught, Now));
    }

    [Fact]
    public void BeyondADayBucketsToDays()
    {
        var caught = Now - TimeSpan.FromDays(30);
        Assert.Equal(RelativeTimeBucket.Days, RelativeTimes.Bucket(caught, Now));
        Assert.Equal(30, RelativeTimes.BucketValue(caught, Now));
    }

    [Fact]
    public void FutureTimestampsClampToZeroMinutes()
    {
        var caught = Now + TimeSpan.FromMinutes(3);
        Assert.Equal(RelativeTimeBucket.Minutes, RelativeTimes.Bucket(caught, Now));
        Assert.Equal(0, RelativeTimes.BucketValue(caught, Now));
    }
}
