using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public sealed record UpdateRelease(string Version, string Url);

public enum UpdateNoticeKind
{
    Offer,
    Skipped,
    Current,
}

public sealed record UpdateNotice(UpdateNoticeKind Kind, string Version)
{
    public static readonly UpdateNotice CurrentNotice = new(UpdateNoticeKind.Current, "");
}

public sealed class UpdateCheckerOptions
{
    public string CurrentVersion { get; init; } = "0";

    public string Repository { get; init; } = "Strongorange/PokeTokenBar-windows";

    public Func<Uri, string?>? Fetch { get; init; }

    public Func<DateTimeOffset>? Clock { get; init; }

    public Func<string?>? ReadSkippedVersion { get; init; }

    public Action<string?>? WriteSkippedVersion { get; init; }
}

/// <summary>
/// Checks the GitHub releases/latest endpoint for a newer version, ported from
/// the macOS UpdateChecker. The banner target (`Available`) hides a skipped
/// release, which stays reachable in Settings (`Skipped`). Any fetch/parse
/// failure is silent. Applying an update is out of scope here — the URL is
/// validated so the UI can open the release page safely.
/// </summary>
public sealed class UpdateChecker
{
    public const int DefaultMinIntervalSeconds = 1800;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly string _currentVersion;
    private readonly Uri _apiUrl;
    private readonly Func<Uri, string?> _fetch;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<string?> _readSkipped;
    private readonly Action<string?> _writeSkipped;
    private readonly object _gate = new();
    private DateTimeOffset? _lastChecked;
    /// <summary>Overlapping checks share one fetch once the early cooldown stamp is gone
    /// (a failed check no longer blocks the next attempt).</summary>
    private bool _checkInFlight;
    private UpdateRelease? _available;
    private UpdateRelease? _skipped;

    public UpdateChecker(UpdateCheckerOptions? options = null)
    {
        options ??= new UpdateCheckerOptions();
        _currentVersion = VersionText.Normalize(options.CurrentVersion);
        _apiUrl = new Uri($"https://api.github.com/repos/{options.Repository}/releases/latest");
        _fetch = options.Fetch ?? HttpFetch;
        _clock = options.Clock ?? (static () => DateTimeOffset.UtcNow);
        _readSkipped = options.ReadSkippedVersion ?? (static () => null);
        _writeSkipped = options.WriteSkippedVersion ?? (static _ => { });
    }

    public event Action? Changed;

    public string CurrentVersion => _currentVersion;

    public UpdateRelease? Available
    {
        get { lock (_gate) return _available; }
    }

    public UpdateRelease? Skipped
    {
        get { lock (_gate) return _skipped; }
    }

    /// <summary>Release Settings can install; a skip hides the banner, not the URL.</summary>
    public UpdateRelease? UpdateTarget
    {
        get { lock (_gate) return _available ?? _skipped; }
    }

    public UpdateNotice SettingsNotice
    {
        get
        {
            lock (_gate)
            {
                if (_available is { } available) return new UpdateNotice(UpdateNoticeKind.Offer, available.Version);
                if (_skipped is { } skipped) return new UpdateNotice(UpdateNoticeKind.Skipped, skipped.Version);
                return UpdateNotice.CurrentNotice;
            }
        }
    }

    /// <summary>
    /// Fetch the latest release. The cooldown applies to <em>successful</em> checks only
    /// (rate-limit protection): a failed fetch, unsafe URL, malformed or prerelease tag does
    /// not start the cooldown, so the next popover open retries right away. Manual checks
    /// pass 0 to bypass the interval.
    /// </summary>
    public Task CheckAsync(int minIntervalSeconds = DefaultMinIntervalSeconds)
    {
        bool startFetch;
        lock (_gate)
        {
            if (_checkInFlight) return Task.CompletedTask;
            var now = _clock();
            if (_lastChecked is { } last && (now - last).TotalSeconds < minIntervalSeconds)
                return Task.CompletedTask;
            _checkInFlight = true;
            startFetch = true;
        }
        if (!startFetch) return Task.CompletedTask;
        return Task.Run(() =>
        {
            try
            {
                var body = _fetch(_apiUrl);
                if (string.IsNullOrEmpty(body)) return;
                if (ParseRelease(body) is not { } release) return;
                // The URL is handed to the browser, so only https github.com is accepted.
                if (!IsSafeReleaseUrl(release.Html)) return;
                // A prerelease tag such as v2.6.0-beta.1 would compare equal to 2.6.0 and
                // garbage as 0.0.0; neither may be applied nor counted as a checked release.
                if (NormalizedReleaseVersion(release.Tag) is null) return;
                lock (_gate)
                {
                    _lastChecked = _clock();
                }
                Consider(release.Tag, release.Html);
            }
            catch
            {
            }
            finally
            {
                lock (_gate) _checkInFlight = false;
            }
        });
    }

    /// <summary>
    /// Release tag as a comparable version, or null when the tag is not a plain
    /// MAJOR.MINOR.PATCH (optionally v-prefixed). IsNewer reads non-numeric parts as 0,
    /// so a prerelease such as v2.6.0-beta.1 would compare equal to 2.6.0.
    /// </summary>
    public static string? NormalizedReleaseVersion(string tag)
    {
        var version = tag.Trim();
        if (version.StartsWith('v') || version.StartsWith('V')) version = version[1..];
        var parts = version.Split('.');
        if (parts.Length != 3) return null;
        var normalized = new string[3];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length is 0 or > 9) return null;
            if (!part.All(char.IsAsciiDigit)) return null;
            normalized[i] = int.Parse(part, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture);
        }
        return string.Join('.', normalized);
    }

    /// <summary>Apply one fetched release. The tag may carry a "v" prefix.</summary>
    public void Consider(string tag, string url)
    {
        Action? handler;
        lock (_gate)
        {
            var version = VersionText.Normalize(tag);
            if (!VersionText.IsNewer(version, _currentVersion))
            {
                _available = null;
                _skipped = null;
            }
            else
            {
                var release = new UpdateRelease(version, url);
                var skippedVersion = VersionText.Normalize(_readSkipped() ?? "");
                if (version == skippedVersion)
                {
                    _available = null;
                    _skipped = release;
                }
                else
                {
                    _available = release;
                    _skipped = null;
                }
            }
            handler = Changed;
        }
        handler?.Invoke();
    }

    /// <summary>Hide the banner for this version; Settings can still see and install it.</summary>
    public void SkipCurrent()
    {
        Action? handler;
        lock (_gate)
        {
            if (_available is not { } release)
            {
                handler = Changed;
            }
            else
            {
                _writeSkipped(release.Version);
                _skipped = release;
                _available = null;
                handler = Changed;
            }
        }
        handler?.Invoke();
    }

    /// <summary>Undo a skip so the banner can show the same release again.</summary>
    public void ShowSkippedAgain()
    {
        Action? handler;
        lock (_gate)
        {
            _writeSkipped(null);
            if (_skipped is { } release)
            {
                _available = release;
                _skipped = null;
            }
            handler = Changed;
        }
        handler?.Invoke();
    }

    public static bool IsSafeReleaseUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host == "github.com";

    private static (string Tag, string Html)? ParseRelease(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String)
                return null;
            if (!root.TryGetProperty("html_url", out var html) || html.ValueKind != JsonValueKind.String)
                return null;
            return (tag.GetString() ?? "", html.GetString() ?? "");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The request for the releases/latest endpoint. GitHub's API rejects
    /// requests without a User-Agent with 403 — which the silent-failure
    /// policy then masqueraded as "up to date" for every real client since
    /// M17 (tests injected a fake fetch, so the real path was never live).
    /// </summary>
    public static HttpRequestMessage BuildRequest(Uri url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.UserAgent.ParseAdd("PokeTokenBar-Windows");
        return request;
    }

    private static string? HttpFetch(Uri url)
    {
        try
        {
            using var request = BuildRequest(url);
            using var response = Http.Send(request);
            if (!response.IsSuccessStatusCode) return null;
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}
