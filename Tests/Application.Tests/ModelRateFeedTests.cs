using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

[Collection("model-pricing")]
public class ModelRateFeedTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private const string CatalogBody = """
    { "openai": { "models": {
        "gpt-6-luna": { "cost": { "input": 0.1, "output": 0.5, "cache_read": 0.01, "cache_write": 0.125 } },
        "gpt-6-sol": { "cost": { "input": 2, "output": 10, "cache_read": 0.2, "cache_write": 2.5 } } } } }
    """;

    private static readonly IReadOnlyDictionary<string, ModelRate> FixtureRates =
        new Dictionary<string, ModelRate>
        {
            ["gpt-6-luna"] = ModelRate.PerMillion(0.1, 0.5, 0.125, 0.01),
            ["gpt-6-sol"] = ModelRate.PerMillion(2, 10, 2.5, 0.2),
        };

    public void Dispose() => ModelPricing.ClearRemoteRates();

    private sealed class Harness
    {
        public string? Cached;
        public string? LastWritten;
        public string? SeenEtag;
        public int FetchCalls;
        public RateCatalogResponse Next = new(0, null, null);

        public ModelRateFeed Feed(TimeSpan? interval = null) => new(new ModelRateFeedOptions
        {
            Fetch = (_, etag) =>
            {
                FetchCalls++;
                SeenEtag = etag;
                return Next;
            },
            ReadCache = () => Cached,
            WriteCache = text =>
            {
                LastWritten = text;
                Cached = text;
            },
            Clock = () => Now,
            RefreshInterval = interval ?? TimeSpan.FromHours(24),
        });

        public void SeedCache(DateTimeOffset fetchedAt, string? etag = null) =>
            Cached = ModelRateCacheCodec.Encode(
                new ModelRateCacheCodec.Payload(fetchedAt, etag, FixtureRates));
    }

    [Fact]
    public void CodecRoundTripsPayload()
    {
        var encoded = ModelRateCacheCodec.Encode(
            new ModelRateCacheCodec.Payload(Now, "\"etag-1\"", FixtureRates));

        var decoded = ModelRateCacheCodec.Decode(encoded);

        Assert.NotNull(decoded);
        Assert.Equal(Now, decoded!.FetchedAt);
        Assert.Equal("\"etag-1\"", decoded.ETag);
        Assert.Equal(2, decoded.Rates.Count);
        Assert.Equal(FixtureRates["gpt-6-luna"], decoded.Rates["gpt-6-luna"]);
        Assert.Equal(FixtureRates["gpt-6-sol"], decoded.Rates["gpt-6-sol"]);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"schemaVersion": 99, "fetchedAtMs": 1, "rates": {}}""")]
    [InlineData("""{"schemaVersion": 1, "fetchedAtMs": 0, "rates": {"m": {"input": 1, "output": 1}}}""")]
    [InlineData("""{"schemaVersion": 1, "fetchedAtMs": 100, "rates": {"m": {"input": -1, "output": 1}}}""")]
    public void CodecRejectsInvalidPayloads(string json)
    {
        Assert.Null(ModelRateCacheCodec.Decode(json));
    }

    [Fact]
    public void LoadCachedImportsRatesWithoutNetwork()
    {
        var harness = new Harness();
        harness.SeedCache(Now - TimeSpan.FromHours(1), "\"etag-1\"");
        var feed = harness.Feed();

        feed.LoadCached();

        Assert.True(feed.HasRates);
        Assert.Equal(0, harness.FetchCalls);
        Assert.Equal(FixtureRates["gpt-6-luna"], ModelPricing.Rate("gpt-6-luna"));
    }

    [Fact]
    public async Task RefreshFetchesImportsAndCachesRates()
    {
        var harness = new Harness();
        harness.Next = new RateCatalogResponse(200, "\"etag-2\"", CatalogBody);
        var feed = harness.Feed();

        await feed.RefreshIfDueAsync();

        Assert.True(feed.HasRates);
        Assert.Equal("\"etag-2\"", feed.ETag);
        Assert.Equal(1, harness.FetchCalls);
        Assert.Null(harness.SeenEtag);
        Assert.NotNull(harness.LastWritten);
        var decoded = ModelRateCacheCodec.Decode(harness.LastWritten!);
        Assert.NotNull(decoded);
        Assert.Equal(Now, decoded!.FetchedAt);
        Assert.Equal("\"etag-2\"", decoded.ETag);
        Assert.Equal(FixtureRates["gpt-6-luna"], decoded.Rates["gpt-6-luna"]);
        Assert.Equal(FixtureRates["gpt-6-sol"], ModelPricing.Rate("gpt-6-sol"));
    }

    [Fact]
    public async Task RefreshSendsCachedEtagAndHandlesNotModified()
    {
        var harness = new Harness();
        harness.SeedCache(Now - TimeSpan.FromHours(48), "\"etag-1\"");
        harness.Next = new RateCatalogResponse(304, "\"etag-1\"", null);
        var feed = harness.Feed();
        feed.LoadCached();

        await feed.RefreshIfDueAsync();

        Assert.Equal(1, harness.FetchCalls);
        Assert.Equal("\"etag-1\"", harness.SeenEtag);
        Assert.Equal(Now, feed.FetchedAt);
        Assert.NotNull(harness.LastWritten);
        var decoded = ModelRateCacheCodec.Decode(harness.LastWritten!);
        Assert.NotNull(decoded);
        Assert.Equal(Now, decoded!.FetchedAt);
        Assert.Equal(FixtureRates["gpt-6-luna"], decoded.Rates["gpt-6-luna"]);
    }

    [Fact]
    public async Task EmptyCatalogResultIsIgnored()
    {
        var harness = new Harness();
        harness.Next = new RateCatalogResponse(200, null, "{}");
        var feed = harness.Feed();

        await feed.RefreshIfDueAsync();

        Assert.False(feed.HasRates);
        Assert.Null(harness.LastWritten);
        Assert.Equal(ModelRate.Zero, ModelPricing.Rate("gpt-6-luna"));
    }

    [Fact]
    public async Task IntervalGuardSkipsRefetchWithinWindow()
    {
        var harness = new Harness();
        harness.SeedCache(Now - TimeSpan.FromHours(1));
        var feed = harness.Feed();
        feed.LoadCached();

        await feed.RefreshIfDueAsync();

        Assert.Equal(0, harness.FetchCalls);
    }

    [Fact]
    public async Task FailedFetchKeepsCachedRates()
    {
        var harness = new Harness();
        harness.SeedCache(Now - TimeSpan.FromHours(48), "\"etag-1\"");
        harness.Next = new RateCatalogResponse(0, null, null);
        var feed = harness.Feed();
        feed.LoadCached();

        await feed.RefreshIfDueAsync();

        Assert.True(feed.HasRates);
        Assert.Equal(1, harness.FetchCalls);
        Assert.Null(harness.LastWritten);
        Assert.Equal(FixtureRates["gpt-6-luna"], ModelPricing.Rate("gpt-6-luna"));
    }

    [Fact]
    public void BuildRequestCarriesUserAgentAndConditionalHeader()
    {
        var url = new Uri("https://models.dev/api.json");

        var plain = ModelRateFeed.BuildRequest(url, null);
        Assert.False(string.IsNullOrEmpty(plain.Headers.UserAgent.ToString()));
        Assert.True(plain.Headers.IfNoneMatch.Count == 0);

        var conditional = ModelRateFeed.BuildRequest(url, "\"etag-1\"");
        Assert.Equal("\"etag-1\"", conditional.Headers.IfNoneMatch.ToString());
    }
}
