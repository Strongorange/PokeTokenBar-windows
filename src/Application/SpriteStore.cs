using System.Net.Http;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public sealed record SpriteBytes(byte[] Data, bool Animated);

public sealed class SpriteStore
{
    public const int MemoryLimit = 64;

    private static readonly HttpClient Http = new();

    private readonly string _directory;
    private readonly Func<Uri, byte[]?> _fetch;
    private readonly object _gate = new();
    private readonly Dictionary<string, byte[]> _mem = [];
    private readonly List<string> _memOrder = [];

    public SpriteStore(string? directory = null, Func<Uri, byte[]?>? fetch = null)
    {
        _directory = directory ?? DefaultDirectory();
        _fetch = fetch ?? HttpFetch;
        try { System.IO.Directory.CreateDirectory(_directory); } catch { }
    }

    public string Directory => _directory;

    public static string DefaultDirectory()
    {
        var stateDirectory = Path.GetDirectoryName(CompanionStateFile.DefaultPath());
        return Path.Combine(string.IsNullOrEmpty(stateDirectory) ? "." : stateDirectory!, "sprites");
    }

    private static byte[]? HttpFetch(Uri url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = Http.Send(request);
            if (!response.IsSuccessStatusCode) return null;
            using var stream = response.Content.ReadAsStream();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();
            return bytes.Length == 0 ? null : bytes;
        }
        catch
        {
            return null;
        }
    }

    public byte[]? CachedData(int speciesID, bool animated, bool shiny = false, UnownForm? unownForm = null)
    {
        if (animated && !PokemonAssets.HasAnimatedSprite(speciesID)) return null;
        var key = SpriteCatalog.CacheKey(speciesID, animated, shiny, unownForm);
        lock (_gate)
        {
            if (_mem.TryGetValue(key, out var hit))
            {
                Touch(key);
                return hit;
            }
        }
        var data = ReadDisk(key, SpriteCatalog.FileExtension(animated));
        if (data is not null) Remember(key, data);
        return data;
    }

    public byte[]? Data(int speciesID, bool animated, bool shiny = false, UnownForm? unownForm = null)
    {
        if (animated && !PokemonAssets.HasAnimatedSprite(speciesID)) return null;
        var key = SpriteCatalog.CacheKey(speciesID, animated, shiny, unownForm);
        lock (_gate)
        {
            if (_mem.TryGetValue(key, out var hit))
            {
                Touch(key);
                return hit;
            }
        }
        var data = ReadDisk(key, SpriteCatalog.FileExtension(animated));
        if (data is not null)
        {
            Remember(key, data);
            return data;
        }
        var fetched = _fetch(new Uri(SpriteCatalog.SpriteUrl(speciesID, animated, shiny, unownForm)));
        if (fetched is null || fetched.Length == 0) return null;
        WriteDisk(key, SpriteCatalog.FileExtension(animated), fetched);
        Remember(key, fetched);
        return fetched;
    }

    public SpriteBytes? Subject(int speciesID, bool animated, bool shiny, UnownForm? unownForm = null)
    {
        foreach (var variant in Variants(animated, shiny))
        {
            var data = Data(speciesID, variant.Animated, variant.Shiny, unownForm);
            if (data is not null) return new SpriteBytes(data, variant.Animated);
        }
        return null;
    }

    public SpriteBytes? CachedSubject(int speciesID, bool animated, bool shiny, UnownForm? unownForm = null)
    {
        foreach (var variant in Variants(animated, shiny))
        {
            var data = CachedData(speciesID, variant.Animated, variant.Shiny, unownForm);
            if (data is not null) return new SpriteBytes(data, variant.Animated);
        }
        return null;
    }

    private static IEnumerable<(bool Animated, bool Shiny)> Variants(bool animated, bool shiny)
    {
        foreach (var moving in animated ? (bool[])[true, false] : [false])
            yield return (moving, shiny);
        if (shiny)
            foreach (var moving in animated ? (bool[])[true, false] : [false])
                yield return (moving, false);
    }

    public byte[]? CachedEgg() => CachedFile(SpriteCatalog.EggCacheKey, "png");

    public byte[]? Egg()
    {
        var cached = CachedEgg();
        if (cached is not null) return cached;
        var fetched = _fetch(new Uri(SpriteCatalog.EggUrl));
        if (fetched is null || fetched.Length == 0) return null;
        WriteDisk(SpriteCatalog.EggCacheKey, "png", fetched);
        Remember(SpriteCatalog.EggCacheKey, fetched);
        return fetched;
    }

    private byte[]? CachedFile(string key, string ext)
    {
        lock (_gate)
        {
            if (_mem.TryGetValue(key, out var hit))
            {
                Touch(key);
                return hit;
            }
        }
        var data = ReadDisk(key, ext);
        if (data is not null) Remember(key, data);
        return data;
    }

    private byte[]? ReadDisk(string key, string ext)
    {
        try
        {
            var path = Path.Combine(_directory, $"{key}.{ext}");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private void WriteDisk(string key, string ext, byte[] data)
    {
        try
        {
            var path = Path.Combine(_directory, $"{key}.{ext}");
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
        }
    }

    private void Remember(string key, byte[] data)
    {
        lock (_gate)
        {
            _mem[key] = data;
            Touch(key);
            while (_memOrder.Count > MemoryLimit)
            {
                var old = _memOrder[0];
                _memOrder.RemoveAt(0);
                _mem.Remove(old);
            }
        }
    }

    private void Touch(string key)
    {
        _memOrder.Remove(key);
        _memOrder.Add(key);
    }
}
