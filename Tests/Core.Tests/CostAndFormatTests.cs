using System.Text.Json;
using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class ModelPricingTests
{
    private static readonly double Epsilon = 1e-12;

    [Fact]
    public void KnownModelCostMatchesHandComputation()
    {
        var cost = ModelPricing.EstimatedCost("claude-sonnet-5", 1000, 500, 2000, 3000);
        Assert.NotNull(cost);
        Assert.Equal((1000 * 2.0 + 2000 * 2.5 + 3000 * 0.2 + 500 * 10.0) / 1e6, cost.Value, Epsilon);
    }

    [Fact]
    public void UnknownModelReturnsNullNotZero()
    {
        Assert.Null(ModelPricing.EstimatedCost("glm-5.2", 1000, 1000, 0, 0));
        Assert.Null(ModelPricing.EstimatedCost("mystery", 1, 1, 1, 1));
    }

    [Fact]
    public void UnknownNamesNeverBorrowFamilyPrices()
    {
        Assert.Null(ModelPricing.EstimatedCost("gpt-5.99", 1000, 1000, 0, 0));
        Assert.Null(ModelPricing.EstimatedCost("claude-opus-6", 1000, 1000, 0, 0));
        Assert.Null(ModelPricing.EstimatedCost("gemini-3-pro", 1000, 1000, 0, 0));
    }

    [Fact]
    public void NamespacePrefixesAndAliasesNormalize()
    {
        Assert.Equal("gpt-5.6-sol", ModelPricing.ModelKey("openai/GPT-5.6"));
        Assert.Equal("claude-sonnet-4-20250514", ModelPricing.ModelKey("anthropic/claude-sonnet-4"));
        Assert.Equal("gpt-5", ModelPricing.ModelKey("gpt-5-2025-08-07"));
        Assert.Equal("claude-haiku-4-5-20251001", ModelPricing.ModelKey("claude-haiku-4-5"));
    }

    [Fact]
    public void LongContextMultipliesInputAndOutput()
    {
        var terra = ModelPricing.EstimatedCost("gpt-5.6-terra", 300_000, 1000, 0, 0);
        Assert.NotNull(terra);
        var expected = (300_000 * 2.0 / 1e6) * 2.0 + (1000 * 12.0 / 1e6) * 1.5;
        Assert.Equal(expected, terra.Value, Epsilon);

        var below = ModelPricing.EstimatedCost("gpt-5.6-terra", 272_000, 1000, 0, 0);
        Assert.NotNull(below);
        var expectedBelow = 272_000 * 2.0 / 1e6 + 1000 * 12.0 / 1e6;
        Assert.Equal(expectedBelow, below.Value, Epsilon);
    }

    [Fact]
    public void GeminiProLongContextThresholdIsLower()
    {
        var over = ModelPricing.EstimatedCost("gemini-2.5-pro", 250_000, 0, 0, 0);
        Assert.NotNull(over);
        Assert.Equal(250_000 * 1.25 / 1e6 * 2.0, over.Value, Epsilon);

        var under = ModelPricing.EstimatedCost("gemini-2.5-pro", 200_000, 0, 0, 0);
        Assert.NotNull(under);
        Assert.Equal(200_000 * 1.25 / 1e6, under.Value, Epsilon);
    }

    [Fact]
    public void ZeroWriteRateWithNonzeroWriteBucketIsUnavailable()
    {
        Assert.Null(ModelPricing.EstimatedCost("gpt-5", 1000, 1000, 500, 0));
        Assert.NotNull(ModelPricing.EstimatedCost("gpt-5", 1000, 1000, 0, 0));
    }

    [Fact]
    public void FlashLiteIsDistinctFromFlash()
    {
        var lite = ModelPricing.EstimatedCost("gemini-2.5-flash-lite", 1_000_000, 0, 0, 0);
        var flash = ModelPricing.EstimatedCost("gemini-2.5-flash", 1_000_000, 0, 0, 0);
        Assert.NotNull(lite);
        Assert.NotNull(flash);
        Assert.Equal(0.10, lite.Value, Epsilon);
        Assert.Equal(0.30, flash.Value, Epsilon);
    }

    [Fact]
    public void NegativeCountsReturnNull()
    {
        Assert.Null(ModelPricing.EstimatedCost("claude-sonnet-5", -1, 0, 0, 0));
    }
}

public class TokenFormatterTests
{
    [Theory]
    [InlineData(987, "987")]
    [InlineData(12_345, "12.3K")]
    [InlineData(10_000, "10K")]
    [InlineData(190_612_940, "190.6M")]
    [InlineData(1_240_000_000, "1.24B")]
    [InlineData(-4500, "-4.5K")]
    public void CompactMatchesReference(long value, string expected)
    {
        Assert.Equal(expected, TokenFormatter.Compact(value));
    }

    [Theory]
    [InlineData(0.0, "$0.00")]
    [InlineData(1.5, "$1.50")]
    [InlineData(123.456, "$123.46")]
    public void CostFormatsTwoDecimals(double value, string expected)
    {
        Assert.Equal(expected, TokenFormatter.Cost(value));
    }

    [Theory]
    [InlineData(9.51, "$9.5")]
    [InlineData(311.4, "$311")]
    [InlineData(1234.0, "$1234")]
    [InlineData(9999.0, "$9999")]
    [InlineData(12_345.0, "$12.3K")]
    public void CostCompactTiers(double value, string expected)
    {
        Assert.Equal(expected, TokenFormatter.CostCompact(value));
    }

    [Theory]
    [InlineData(50.0, "50%")]
    [InlineData(50.4, "50.4%")]
    [InlineData(99.9, "99.9%")]
    public void PercentIntegralVsOneDecimal(double value, string expected)
    {
        Assert.Equal(expected, TokenFormatter.Percent(value));
    }
}

public class IsoDatesTests
{
    [Fact]
    public void ParsesMilliseconds()
    {
        var d = IsoDates.Date("2026-09-22T07:46:31.693Z");
        Assert.NotNull(d);
        Assert.Equal(693, d.Value.Millisecond);
    }

    [Fact]
    public void ParsesMicrosecondsByTruncating()
    {
        var d = IsoDates.Date("2026-09-22T07:46:31.034464+00:00");
        Assert.NotNull(d);
        Assert.Equal(34, d.Value.Millisecond);
    }

    [Fact]
    public void ParsesTwoDigitFractionsSeenInCodexRollouts()
    {
        var d = IsoDates.Date("2026-09-06T23:18:29.74Z");
        Assert.NotNull(d);
        Assert.Equal(740, d.Value.Millisecond);
    }

    [Fact]
    public void ParsesPlainSecondsAndOffsets()
    {
        Assert.NotNull(IsoDates.Date("2026-09-22T07:46:31Z"));
        Assert.NotNull(IsoDates.Date("2026-09-22T16:46:31+09:00"));
    }

    [Fact]
    public void RejectsGarbage()
    {
        Assert.Null(IsoDates.Date("not a date"));
        Assert.Null(IsoDates.Date(""));
    }
}

public class DailyUsageJsonTests
{
    [Fact]
    public void DecodesCcusageCompatKeys()
    {
        var json = """
        {
          "period": "2026-09-22",
          "inputTokens": 100,
          "outputTokens": 50,
          "cacheCreationTokens": 10,
          "cachedInputTokens": 5,
          "costUSD": 0.25
        }
        """;
        var daily = DailyUsage.FromJson(JsonDocument.Parse(json).RootElement);
        Assert.NotNull(daily);
        Assert.Equal("2026-09-22", daily!.Date);
        Assert.Equal(165, daily.TotalTokens);
        Assert.Equal(5, daily.CacheReadTokens);
        Assert.Equal(0.25, daily.TotalCost);
        Assert.True(daily.CostCoverage.Estimated);
    }

    [Fact]
    public void ZeroTotalDecodesEmptyCoverage()
    {
        var json = """{"date": "2026-09-22"}""";
        var daily = DailyUsage.FromJson(JsonDocument.Parse(json).RootElement);
        Assert.NotNull(daily);
        Assert.Equal(CostCoverage.Empty, daily!.CostCoverage);
    }
}
