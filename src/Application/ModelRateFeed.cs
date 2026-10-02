using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public sealed record RateCatalogResponse(int StatusCode, string? ETag, string? Body);

public static class ModelRateCacheCodec
{
    public const int SchemaVersion = 1;

    public sealed record Payload(
        DateTimeOffset FetchedAt,
        string? ETag,
        IReadOnlyDictionary<string, ModelRate> Rates);

    private sealed record CacheDto(
        int SchemaVersion,
        long FetchedAtMs,
        string? ETag,
        Dictionary<string, ModelRate> Rates);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Encode(Payload payload)
    {
        var dto = new CacheDto(
            SchemaVersion,
            payload.FetchedAt.ToUnixTimeMilliseconds(),
            payload.ETag,
            new Dictionary<string, ModelRate>(payload.Rates));
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static Payload? Decode(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<CacheDto>(json, JsonOptions);
            if (dto is null || dto.SchemaVersion != SchemaVersion) return null;
            if (dto.Rates is not { Count: > 0 }) return null;
            if (dto.FetchedAtMs <= 0) return null;
            var fetchedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.FetchedAtMs);
            foreach (var rate in dto.Rates.Values)
                if (!IsUsable(rate)) return null;
            return new Payload(fetchedAt, dto.ETag, dto.Rates);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsUsable(ModelRate rate) =>
        double.IsFinite(rate.Input) && rate.Input >= 0 &&
        double.IsFinite(rate.Output) && rate.Output >= 0 &&
        double.IsFinite(rate.CacheWrite) && rate.CacheWrite >= 0 &&
        double.IsFinite(rate.CacheRead) && rate.CacheRead >= 0 &&
        (rate.Input > 0 || rate.Output > 0);
}

public sealed class ModelRateFeedOptions
{
    public string CatalogUrl { get; init; } = "https://models.dev/api.json";

    public Func<Uri, string?, RateCatalogResponse>? Fetch { get; init; }

    public Func<string?>? ReadCache { get; init; }

    public Action<string>? WriteCache { get; init; }

    public Func<DateTimeOffset>? Clock { get; init; }

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromHours(24);

    public static string DefaultCacheFilePath() =>
        Path.Combine(UsageRefreshOptions.DefaultCacheDirectory(), "model-rates.json");
}

/// <summary>
/// Keeps ModelPricing's remote overlay in sync with the models.dev community
/// catalog. Startup loads the disk cache so estimates survive offline starts;
/// a background conditional fetch (If-None-Match) refreshes it at most once
/// per interval. Every failure path is silent and falls back to the cache,
/// then to the bundled table inside ModelPricing.
/// </summary>
public sealed class ModelRateFeed
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly Uri _catalogUrl;
    private readonly Func<Uri, string?, RateCatalogResponse> _fetch;
    private readonly Func<string?> _readCache;
    private readonly Action<string> _writeCache;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _refreshInterval;
    private readonly object _gate = new();
    private DateTimeOffset? _fetchedAt;
    private string? _etag;
    private IReadOnlyDictionary<string, ModelRate>? _rates;

    public ModelRateFeed(ModelRateFeedOptions? options = null)
    {
        options ??= new ModelRateFeedOptions();
        _catalogUrl = new Uri(options.CatalogUrl);
        _fetch = options.Fetch ?? HttpFetch;
        _clock = options.Clock ?? (static () => DateTimeOffset.UtcNow);
        _refreshInterval = options.RefreshInterval;
        if (options.ReadCache is { } readCache && options.WriteCache is { } writeCache)
        {
            _readCache = readCache;
            _writeCache = writeCache;
        }
        else
        {
            var path = ModelRateFeedOptions.DefaultCacheFilePath();
            _readCache = () => ReadFile(path);
            _writeCache = text => WriteFile(path, text);
        }
    }

    public bool HasRates
    {
        get { lock (_gate) return _rates is { Count: > 0 }; }
    }

    public DateTimeOffset? FetchedAt
    {
        get { lock (_gate) return _fetchedAt; }
    }

    public string? ETag
    {
        get { lock (_gate) return _etag; }
    }

    public void LoadCached()
    {
        string? json;
        try
        {
            json = _readCache();
        }
        catch
        {
            return;
        }
        if (string.IsNullOrEmpty(json)) return;
        if (ModelRateCacheCodec.Decode(json) is not { } payload) return;
        lock (_gate)
        {
            _fetchedAt = payload.FetchedAt;
            _etag = payload.ETag;
            _rates = payload.Rates;
        }
        ModelPricing.ImportRemoteRates(payload.Rates);
    }

    public Task RefreshIfDueAsync() => Task.Run(RefreshIfDue);

    private void RefreshIfDue()
    {
        lock (_gate)
        {
            if (_fetchedAt is { } last && _clock() - last < _refreshInterval) return;
        }
        try
        {
            var response = _fetch(_catalogUrl, VolatileETag());
            if (response.StatusCode == 304)
            {
                MarkRefreshed();
                return;
            }
            if (response.StatusCode != 200 || string.IsNullOrEmpty(response.Body)) return;
            var rates = ModelsDevCatalog.Parse(response.Body);
            if (rates.Count == 0) return;
            DateTimeOffset fetchedAt;
            lock (_gate)
            {
                fetchedAt = _clock();
                _fetchedAt = fetchedAt;
                _etag = response.ETag;
                _rates = rates;
            }
            ModelPricing.ImportRemoteRates(rates);
            _writeCache(ModelRateCacheCodec.Encode(
                new ModelRateCacheCodec.Payload(fetchedAt, response.ETag, rates)));
        }
        catch
        {
        }
    }

    private void MarkRefreshed()
    {
        string text;
        lock (_gate)
        {
            _fetchedAt = _clock();
            if (_rates is not { Count: > 0 }) return;
            text = ModelRateCacheCodec.Encode(
                new ModelRateCacheCodec.Payload(_fetchedAt.Value, _etag, _rates));
        }
        try
        {
            _writeCache(text);
        }
        catch
        {
        }
    }

    private string? VolatileETag()
    {
        lock (_gate) return _etag;
    }

    public static HttpRequestMessage BuildRequest(Uri url, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("PokeTokenBar-Windows");
        if (!string.IsNullOrEmpty(etag))
            request.Headers.IfNoneMatch.ParseAdd(etag);
        return request;
    }

    private static RateCatalogResponse HttpFetch(Uri url, string? etag)
    {
        try
        {
            using var request = BuildRequest(url, etag);
            using var response = Http.Send(request);
            var body = response.IsSuccessStatusCode
                ? response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                : null;
            var header = response.Headers.ETag;
            return new RateCatalogResponse((int)response.StatusCode, header?.Tag, body);
        }
        catch
        {
            return new RateCatalogResponse(0, null, null);
        }
    }

    private static string? ReadFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static void WriteFile(string path, string text)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, text);
        }
        catch
        {
        }
    }
}
