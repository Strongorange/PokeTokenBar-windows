using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

public class SpriteStoreTests : IDisposable
{
    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A];

    private readonly string _dir;

    public SpriteStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-sprite-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void FetchCachesToDiskAndSurvivesRestartWithoutRefetch()
    {
        var fetched = new List<Uri>();
        var store = new SpriteStore(_dir, url => FetchRecorder(fetched, url, Gif));

        Assert.Equal(Gif, store.Data(25, animated: true));
        Assert.Equal(Gif, store.Data(25, animated: true));
        Assert.Single(fetched);
        Assert.True(File.Exists(Path.Combine(_dir, "25-a.gif")));

        var restarted = new SpriteStore(_dir, url => FetchRecorder(fetched, url, Gif));
        Assert.Equal(Gif, restarted.Data(25, animated: true));
        Assert.Single(fetched);
        Assert.Equal(Gif, restarted.CachedSubject(25, animated: true, shiny: false)!.Data);
        Assert.Single(fetched);
    }

    [Fact]
    public void StaticFallbackWhenAnimatedFetchFails()
    {
        var store = new SpriteStore(_dir, url =>
            url.AbsoluteUri.EndsWith(".gif") ? null : Png);

        var subject = store.Subject(25, animated: true, shiny: false);

        Assert.NotNull(subject);
        Assert.False(subject!.Animated);
        Assert.Equal(Png, subject.Data);
        Assert.True(File.Exists(Path.Combine(_dir, "25-s.png")));
    }

    [Fact]
    public void ShinyFallsBackToNormalWhenUnavailable()
    {
        var fetched = new List<Uri>();
        var store = new SpriteStore(_dir, url =>
        {
            fetched.Add(url);
            return url.AbsoluteUri.Contains("/shiny/") ? null : Gif;
        });

        var subject = store.Subject(25, animated: true, shiny: true);

        Assert.NotNull(subject);
        Assert.True(subject!.Animated);
        Assert.Equal(Gif, subject.Data);
        Assert.Equal(3, fetched.Count);
        Assert.Contains("/shiny/", fetched[0].AbsoluteUri);
        Assert.Contains("/shiny/", fetched[1].AbsoluteUri);
        Assert.EndsWith("/versions/generation-v/black-white/animated/25.gif", fetched[2].AbsoluteUri);
    }

    [Fact]
    public void AnimatedRequestBeyond649GoesStraightToStatic()
    {
        var fetched = new List<Uri>();
        var store = new SpriteStore(_dir, url => FetchRecorder(fetched, url, Png));

        var subject = store.Subject(700, animated: true, shiny: false);

        Assert.NotNull(subject);
        Assert.False(subject!.Animated);
        Assert.Single(fetched);
        Assert.EndsWith("/sprites/pokemon/700.png", fetched[0].AbsoluteUri);
    }

    [Fact]
    public void OfflineWhenNeverFetchedYieldsNullWithoutThrowing()
    {
        var store = new SpriteStore(_dir, _ => null);

        Assert.Null(store.Subject(25, animated: true, shiny: false));
        Assert.Null(store.CachedSubject(25, animated: true, shiny: false));
        Assert.Null(store.Egg());
    }

    [Fact]
    public void EggCachesUnderFlatKeyAndRoundTrips()
    {
        var fetched = new List<Uri>();
        var store = new SpriteStore(_dir, url => FetchRecorder(fetched, url, Png));

        Assert.Equal(Png, store.Egg());
        Assert.True(File.Exists(Path.Combine(_dir, "egg.png")));
        var restarted = new SpriteStore(_dir, url => FetchRecorder(fetched, url, Png));
        Assert.Equal(Png, restarted.CachedEgg());
        Assert.Single(fetched);
    }

    [Fact]
    public void MemoryEvictionKeepsRetrievableFromDisk()
    {
        var store = new SpriteStore(_dir, _ => Png);
        for (var id = 1; id <= SpriteStore.MemoryLimit + 6; id++)
            Assert.NotNull(store.Data(id, animated: false));

        Assert.Equal(Png, store.Data(1, animated: false));
        Assert.Equal(Png, store.Data(SpriteStore.MemoryLimit + 6, animated: false));
    }

    private static byte[]? FetchRecorder(List<Uri> fetched, Uri url, byte[] data)
    {
        fetched.Add(url);
        return data;
    }
}
