using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class ModelsDevCatalogTests
{
    internal const string Fixture = """
    {
      "nano-gpt": { "id": "nano-gpt", "models": {
        "openai/gpt-5.6-sol": { "cost": { "input": 2, "output": 10, "cache_read": 0.2, "cache_write": 2.5 } },
        "z-ai/glm-5.3": { "cost": { "input": 0.5, "output": 2, "cache_read": 0.1 } }
      }},
      "garbage-co": { "models": {
        "weird-model": { "cost": { "input": 999999, "output": 1 } }
      }},
      "302ai": { "models": {
        "gemini-3-pro-preview": { "cost": { "input": 2, "output": 12 } }
      }},
      "aws-mantle": { "models": {
        "gpt-5.6-terra": { "cost": { "input": 2.2, "output": 13.2, "cache_read": 0.22, "cache_write": 2.75 } }
      }},
      "deepinfra": { "models": {
        "zai-org/GLM-5.3": { "cost": { "input": 0.9, "output": 4, "cache_read": 0.2 } }
      }},
      "openai": { "id": "openai", "name": "OpenAI", "models": {
        "gpt-6-luna": { "cost": { "input": 0.1, "output": 0.5, "cache_read": 0.01, "cache_write": 0.125 } },
        "gpt-6-sol": { "cost": { "input": 2, "output": 10, "cache_read": 0.2, "cache_write": 2.5 } },
        "gpt-5.6-sol": { "cost": { "input": 4, "output": 20, "cache_read": 0.4, "cache_write": 5 } },
        "gpt-5.5": { "cost": { "input": 5, "output": 30, "cache_read": 0.5 } },
        "gpt-5.3-codex-spark": { "cost": { "input": 1.75, "output": 14, "cache_read": 0.175 } },
        "gpt-5.2-codex": {}
      }},
      "azure": { "models": {
        "openai/gpt-6-luna": { "cost": { "input": 0.2, "output": 1, "cache_read": 0.02, "cache_write": 0.25 } }
      }},
      "opencode": { "models": {
        "gpt-5.1-codex-max": { "cost": { "input": 1.25, "output": 10, "cache_read": 0.125 } },
        "glm-5.3": { "cost": { "input": 1.4, "output": 4.4, "cache_read": 0.26, "cache_write": 0 } },
        "glm-4.7-free": { "cost": { "input": 0, "output": 0, "cache_read": 0 } },
        "grok-code": { "cost": { "input": 0, "output": 0 } }
      }},
      "zai": { "models": {
        "glm-5.3": { "cost": { "input": 1.4, "output": 4.4, "cache_read": 0.26, "cache_write": 0 } }
      }},
      "anthropic": { "models": {
        "claude-opus-4-8": { "cost": { "input": 5, "output": 25, "cache_read": 0.5, "cache_write": 6.25 } },
        "claude-opus-4-5": { "cost": { "input": 5, "output": 25, "cache_read": 0.5, "cache_write": 6.25 } },
        "claude-opus-4-5-thinking": {}
      }},
      "google": { "models": {
        "gemini-2.5-pro": { "cost": { "input": 1.25, "output": 10, "cache_read": 0.125 } },
        "gemini-3-flash-preview": { "cost": { "input": 0.5, "output": 3, "cache_read": 0.05 } },
        "gemini-3-pro": {}
      }}
    }
    """;

    private static IReadOnlyDictionary<string, ModelRate> ParseFixture() =>
        ModelsDevCatalog.Parse(Fixture);

    [Fact]
    public void ParsePrefersOfficialProvidersRegardlessOfEncounterOrder()
    {
        var rates = ParseFixture();
        Assert.Equal(ModelRate.PerMillion(4, 20, 5, 0.4), rates["gpt-5.6-sol"]);
        Assert.Equal(ModelRate.PerMillion(0.1, 0.5, 0.125, 0.01), rates["gpt-6-luna"]);
        Assert.Equal(ModelRate.PerMillion(1.4, 4.4, 0, 0.26), rates["glm-5.3"]);
        Assert.Equal(ModelRate.PerMillion(1.75, 14, 0, 0.175), rates["gpt-5.3-codex-spark"]);
        Assert.Equal(ModelRate.PerMillion(1.25, 10, 0, 0.125), rates["gpt-5.1-codex-max"]);
    }

    [Fact]
    public void ParseFallsBackToUnpreferredProviderWhenOfficialLacksCost()
    {
        var rates = ParseFixture();
        Assert.Equal(ModelRate.PerMillion(2, 12, 0, 0), rates["gemini-3-pro-preview"]);
        Assert.Equal(ModelRate.PerMillion(2.2, 13.2, 2.75, 0.22), rates["gpt-5.6-terra"]);
    }

    [Fact]
    public void ParseSkipsFreeEmptyAndAbsurdEntries()
    {
        var rates = ParseFixture();
        Assert.False(rates.ContainsKey("glm-4.7-free"));
        Assert.False(rates.ContainsKey("grok-code"));
        Assert.False(rates.ContainsKey("gpt-5.2-codex"));
        Assert.False(rates.ContainsKey("claude-opus-4-5-thinking"));
        Assert.False(rates.ContainsKey("gemini-3-pro"));
        Assert.False(rates.ContainsKey("weird-model"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"openai\": []}")]
    public void ParseRejectsInvalidShapesWithEmptyResult(string json)
    {
        Assert.Empty(ModelsDevCatalog.Parse(json));
    }

    [Fact]
    public void ParseIgnoresUnknownFieldsInsideModelEntries()
    {
        var json = """
        { "openai": { "models": {
            "gpt-6-luna": { "reasoning": true, "limit": { "context": 258400 },
              "cost": { "input": 0.1, "output": 0.5, "cache_read": 0.01, "cache_write": 0.125, "tier": "standard" } } } }
        }
        """;
        var rates = ModelsDevCatalog.Parse(json);
        Assert.Equal(ModelRate.PerMillion(0.1, 0.5, 0.125, 0.01), rates["gpt-6-luna"]);
    }
}

[Collection("model-pricing")]
public class ModelPricingOverlayTests : IDisposable
{
    private static readonly double Epsilon = 1e-12;

    public ModelPricingOverlayTests()
    {
        ModelPricing.ImportRemoteRates(ModelsDevCatalog.Parse(ModelsDevCatalogTests.Fixture));
    }

    public void Dispose() => ModelPricing.ClearRemoteRates();

    [Fact]
    public void RemoteRatesAddUnknownModelsWithHandComputedCost()
    {
        var cost = ModelPricing.EstimatedCost("gpt-6-luna", 9762, 148, 0, 11008);
        Assert.NotNull(cost);
        Assert.Equal((9762 * 0.1 + 11008 * 0.01 + 148 * 0.5) / 1e6, cost.Value, Epsilon);
    }

    [Fact]
    public void RemoteRatesReproduceBundledVendorPrices()
    {
        var sol = ModelPricing.EstimatedCost("gpt-5.6-sol", 1000, 1000, 1000, 1000);
        Assert.NotNull(sol);
        Assert.Equal((1000 * 4.0 + 1000 * 5.0 + 1000 * 0.4 + 1000 * 20.0) / 1e6, sol.Value, Epsilon);
        var opus = ModelPricing.EstimatedCost("claude-opus-4-8", 1000, 1000, 1000, 1000);
        Assert.NotNull(opus);
        Assert.Equal((1000 * 5.0 + 1000 * 25.0 + 1000 * 6.25 + 1000 * 0.5) / 1e6, opus.Value, Epsilon);
        var gemini = ModelPricing.EstimatedCost("gemini-2.5-pro", 1000, 1000, 0, 1000);
        Assert.NotNull(gemini);
        Assert.Equal((1000 * 1.25 + 1000 * 10.0 + 1000 * 0.125) / 1e6, gemini.Value, Epsilon);
    }

    [Fact]
    public void RemoteRatesOverrideBundledRatesOnConflict()
    {
        ModelPricing.ImportRemoteRates(new Dictionary<string, ModelRate>
        {
            ["claude-sonnet-5"] = ModelRate.PerMillion(9, 9, 9, 9),
        });
        var cost = ModelPricing.EstimatedCost("claude-sonnet-5", 1_000_000, 0, 0, 0);
        Assert.NotNull(cost);
        Assert.Equal(9.0, cost.Value, Epsilon);
    }

    [Fact]
    public void ClearRestoresBundledOnlyBehavior()
    {
        ModelPricing.ClearRemoteRates();
        Assert.Null(ModelPricing.EstimatedCost("gpt-6-luna", 1000, 1000, 0, 0));
        Assert.NotNull(ModelPricing.EstimatedCost("claude-sonnet-5", 1000, 500, 2000, 3000));
    }

    [Fact]
    public void AntigravityAndThinkingVariantsResolveThroughRemoteRates()
    {
        var cost = ModelPricing.EstimatedCost("antigravity-claude-opus-4-5-thinking", 1_000_000, 0, 0, 0);
        Assert.NotNull(cost);
        Assert.Equal(5.0, cost.Value, Epsilon);
        Assert.Null(ModelPricing.EstimatedCost("antigravity-gemini-3-pro", 1000, 1000, 0, 0));
    }

    [Fact]
    public void RemoteRateLookupIsCaseInsensitive()
    {
        Assert.Equal(ModelRate.PerMillion(0.1, 0.5, 0.125, 0.01), ModelPricing.Rate("GPT-6-Luna"));
    }
}
