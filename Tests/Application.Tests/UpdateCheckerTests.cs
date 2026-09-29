using PokeTokenBar.Application;

namespace PokeTokenBar.Application.Tests;

public class UpdateCheckerTests
{
    private const string ReleaseUrl = "https://github.com/Strongorange/PokeTokenBar-windows/releases/tag/v1.2.0";

    private static string ReleaseJson(string tag, string html) =>
        $$"""{"tag_name": "{{tag}}", "html_url": "{{html}}"}""";

    private sealed class Harness
    {
        public readonly Dictionary<string, string?> Store = [];

        public UpdateChecker Checker { get; }

        public Harness(string currentVersion = "1.0.0", Func<Uri, string?>? fetch = null,
            DateTimeOffset? clock = null)
        {
            Checker = new UpdateChecker(new UpdateCheckerOptions
            {
                CurrentVersion = currentVersion,
                Fetch = fetch,
                Clock = clock is { } time ? () => time : null,
                ReadSkippedVersion = () => Store.TryGetValue("skipped", out var value) ? value : null,
                WriteSkippedVersion = version => Store["skipped"] = version,
            });
        }
    }

    [Fact]
    public void ConsiderWithNewerTagOffersRelease()
    {
        var harness = new Harness();

        harness.Checker.Consider("v1.1.0", ReleaseUrl);

        var release = harness.Checker.Available;
        Assert.NotNull(release);
        Assert.Equal("1.1.0", release.Version);
        Assert.Equal(ReleaseUrl, release.Url);
        Assert.Null(harness.Checker.Skipped);
    }

    [Fact]
    public void ConsiderWithOlderOrEqualTagClearsState()
    {
        var harness = new Harness();

        harness.Checker.Consider("v1.1.0", ReleaseUrl);
        harness.Checker.Consider("0.9.9", ReleaseUrl);
        Assert.Null(harness.Checker.Available);

        harness.Checker.Consider("1.0.0", ReleaseUrl);
        Assert.Null(harness.Checker.Available);
        Assert.Null(harness.Checker.Skipped);
    }

    [Fact]
    public void ConsiderWithPersistedSkipHidesFromBannerOnly()
    {
        var harness = new Harness();
        harness.Store["skipped"] = "1.1.0";

        harness.Checker.Consider("v1.1.0", ReleaseUrl);

        Assert.Null(harness.Checker.Available);
        Assert.NotNull(harness.Checker.Skipped);
        Assert.Equal(UpdateNoticeKind.Skipped, harness.Checker.SettingsNotice.Kind);
        Assert.NotNull(harness.Checker.UpdateTarget);
    }

    [Fact]
    public void SkipCurrentMovesOfferToSkippedAndPersists()
    {
        var harness = new Harness();
        harness.Checker.Consider("v1.1.0", ReleaseUrl);

        harness.Checker.SkipCurrent();

        Assert.Null(harness.Checker.Available);
        Assert.NotNull(harness.Checker.Skipped);
        Assert.Equal("1.1.0", harness.Store["skipped"]);
    }

    [Fact]
    public void SkipCurrentWithoutOfferIsNoop()
    {
        var harness = new Harness();

        harness.Checker.SkipCurrent();

        Assert.Null(harness.Checker.Available);
        Assert.Null(harness.Checker.Skipped);
        Assert.False(harness.Store.ContainsKey("skipped"));
    }

    [Fact]
    public void ShowSkippedAgainRestoresBannerAndClearsPersistedSkip()
    {
        var harness = new Harness();
        harness.Checker.Consider("v1.1.0", ReleaseUrl);
        harness.Checker.SkipCurrent();

        harness.Checker.ShowSkippedAgain();

        Assert.NotNull(harness.Checker.Available);
        Assert.Null(harness.Checker.Skipped);
        Assert.Null(harness.Store["skipped"]);
    }

    [Fact]
    public void SettingsNoticeMapsOfferSkippedAndCurrent()
    {
        var harness = new Harness();

        Assert.Equal(UpdateNoticeKind.Current, harness.Checker.SettingsNotice.Kind);

        harness.Checker.Consider("v1.1.0", ReleaseUrl);
        var offer = harness.Checker.SettingsNotice;
        Assert.Equal(UpdateNoticeKind.Offer, offer.Kind);
        Assert.Equal("1.1.0", offer.Version);

        harness.Checker.SkipCurrent();
        var skipped = harness.Checker.SettingsNotice;
        Assert.Equal(UpdateNoticeKind.Skipped, skipped.Kind);
        Assert.Equal("1.1.0", skipped.Version);
    }

    [Fact]
    public void UpdateTargetPrefersAvailableAndKeepsSkipped()
    {
        var harness = new Harness();
        Assert.Null(harness.Checker.UpdateTarget);

        harness.Checker.Consider("v1.1.0", ReleaseUrl);
        Assert.Equal("1.1.0", harness.Checker.UpdateTarget?.Version);

        harness.Checker.SkipCurrent();
        Assert.Equal("1.1.0", harness.Checker.UpdateTarget?.Version);
    }

    [Fact]
    public void ChangedFiresOnStateTransitions()
    {
        var harness = new Harness();
        var fired = 0;
        harness.Checker.Changed += () => fired++;

        harness.Checker.Consider("v1.1.0", ReleaseUrl);
        harness.Checker.SkipCurrent();
        harness.Checker.ShowSkippedAgain();
        harness.Checker.SkipCurrent();

        Assert.Equal(4, fired);
    }

    [Fact]
    public async Task CheckAsyncParsesReleaseAndOffersUpdate()
    {
        var harness = new Harness(fetch: _ => ReleaseJson("v1.2.0", ReleaseUrl));

        await harness.Checker.CheckAsync(0);

        var release = harness.Checker.Available;
        Assert.NotNull(release);
        Assert.Equal("1.2.0", release.Version);
        Assert.Equal(ReleaseUrl, release.Url);
    }

    [Fact]
    public async Task CheckAsyncDebouncesWithinIntervalAndManualBypasses()
    {
        var time = DateTimeOffset.UtcNow;
        var fetches = 0;
        string? Fetch(Uri url)
        {
            fetches++;
            return ReleaseJson("v1.2.0", ReleaseUrl);
        }
        var harness = new Harness(fetch: Fetch);

        await harness.Checker.CheckAsync(0);
        await harness.Checker.CheckAsync();
        await harness.Checker.CheckAsync(1800);
        Assert.Equal(1, fetches);

        await harness.Checker.CheckAsync(0);
        Assert.Equal(2, fetches);
    }

    [Fact]
    public async Task CheckAsyncFailuresAreSilent()
    {
        var harness = new Harness(fetch: _ => null);
        await harness.Checker.CheckAsync(0);
        Assert.Null(harness.Checker.Available);

        var garbage = new Harness(fetch: _ => "not json at all");
        await garbage.Checker.CheckAsync(0);
        Assert.Null(garbage.Checker.Available);

        var missingFields = new Harness(fetch: _ => """{"message": "API rate limit exceeded"}""");
        await missingFields.Checker.CheckAsync(0);
        Assert.Null(missingFields.Checker.Available);

        var throwing = new Harness(fetch: _ => throw new InvalidOperationException("offline"));
        await throwing.Checker.CheckAsync(0);
        Assert.Null(throwing.Checker.Available);
    }

    [Theory]
    [InlineData("http://github.com/Strongorange/PokeTokenBar-windows/releases")]
    [InlineData("https://evil.example.com/releases")]
    [InlineData("https://api.github.com/repos/x")]
    [InlineData("not a url")]
    public async Task CheckAsyncRejectsUnsafeReleaseUrls(string url)
    {
        var harness = new Harness(fetch: _ => ReleaseJson("v1.2.0", url));

        await harness.Checker.CheckAsync(0);

        Assert.Null(harness.Checker.Available);
        Assert.Null(harness.Checker.Skipped);
    }

    [Fact]
    public async Task CheckAsyncHonorsPersistedSkip()
    {
        var harness = new Harness(fetch: _ => ReleaseJson("v1.2.0", ReleaseUrl));
        harness.Store["skipped"] = "1.2.0";

        await harness.Checker.CheckAsync(0);

        Assert.Null(harness.Checker.Available);
        Assert.NotNull(harness.Checker.Skipped);
    }

    [Fact]
    public void SafeUrlValidationAcceptsOnlyHttpsGithub()
    {
        Assert.True(UpdateChecker.IsSafeReleaseUrl(ReleaseUrl));
        Assert.False(UpdateChecker.IsSafeReleaseUrl("http://github.com/x"));
        Assert.False(UpdateChecker.IsSafeReleaseUrl("https://github.evil.com/x"));
        Assert.False(UpdateChecker.IsSafeReleaseUrl(""));
    }

    [Fact]
    public void CurrentVersionIsNormalized()
    {
        var harness = new Harness(currentVersion: "v1.0.0");
        Assert.Equal("1.0.0", harness.Checker.CurrentVersion);
    }
}
