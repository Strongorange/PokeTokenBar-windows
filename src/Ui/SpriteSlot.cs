using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using XamlAnimatedGif;

namespace PokeTokenBar.Ui;

/// <summary>
/// One sprite target (dashboard active mon, dex row, detail header): renders
/// from cache synchronously, falls back to a glyph, then swaps in the fetched
/// sprite when it arrives. Mirrors the pet window's cached-first pattern.
/// </summary>
internal sealed class SpriteSlot
{
    private readonly Image _image;
    private readonly TextBlock? _placeholder;
    private MemoryStream? _stream;
    private SpriteStore? _store;
    private (int Species, bool Animated, bool Shiny, PokemonForm? Form)? _subject;
    private bool _egg;
    private int _version;

    public SpriteSlot(Image image, TextBlock? placeholder = null)
    {
        _image = image;
        _placeholder = placeholder;
        _image.AddHandler(AnimationBehavior.ErrorEvent, new RoutedEventHandler(OnAnimatedSpriteError));
    }

    public void Update(SpriteStore store, int speciesID, bool animated, bool shiny,
        PokemonForm? form, string glyph)
    {
        _store = store;
        var subject = (speciesID, animated, shiny, form);
        if (_subject == subject && !_egg) return;
        _subject = subject;
        _egg = false;
        var version = ++_version;
        var cached = store.CachedSubject(speciesID, animated, shiny, form);
        if (cached is not null)
        {
            Render(cached);
            if (animated && !cached.Animated && PokemonAssets.HasAnimatedSprite(speciesID))
                UpgradeToAnimated(store, version, speciesID, shiny, form);
            return;
        }
        ShowPlaceholder(glyph);
        Task.Run(() =>
        {
            var fetched = store.Subject(speciesID, animated, shiny, form);
            if (fetched is null) return;
            _image.Dispatcher.BeginInvoke(() =>
            {
                if (version == _version) Render(fetched);
            });
        });
    }

    public void UpdateEgg(SpriteStore store, string glyph)
    {
        _store = store;
        if (_egg) return;
        _egg = true;
        _subject = null;
        var version = ++_version;
        var cached = store.CachedEgg();
        if (cached is not null)
        {
            Render(new SpriteBytes(cached, Animated: false));
            return;
        }
        ShowPlaceholder(glyph);
        Task.Run(() =>
        {
            var fetched = store.Egg();
            if (fetched is null) return;
            _image.Dispatcher.BeginInvoke(() =>
            {
                if (version == _version) Render(new SpriteBytes(fetched, Animated: false));
            });
        });
    }

    private void UpgradeToAnimated(SpriteStore store, int version, int speciesID,
        bool shiny, PokemonForm? form)
    {
        Task.Run(() =>
        {
            var fetched = store.Subject(speciesID, animated: true, shiny, form);
            if (fetched is not { Animated: true }) return;
            _image.Dispatcher.BeginInvoke(() =>
            {
                if (version == _version) Render(fetched);
            });
        });
    }

    private void Render(SpriteBytes sprite)
    {
        Detach();
        if (sprite.Animated)
        {
            var stream = new MemoryStream(sprite.Data);
            _stream = stream;
            AnimationBehavior.SetSourceStream(_image, stream);
        }
        else
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(sprite.Data);
            image.EndInit();
            image.Freeze();
            _image.Source = image;
        }
        _image.Visibility = Visibility.Visible;
        if (_placeholder is not null) _placeholder.Visibility = Visibility.Collapsed;
    }

    private void ShowPlaceholder(string glyph)
    {
        Detach();
        _image.Source = null;
        _image.Visibility = _placeholder is null ? Visibility.Collapsed : Visibility.Visible;
        if (_placeholder is not null)
        {
            _placeholder.Text = glyph;
            _placeholder.Visibility = Visibility.Visible;
        }
    }

    private void Detach()
    {
        AnimationBehavior.SetSourceStream(_image, null);
        _image.Source = null;
        _stream = null;
    }

    /// <summary>
    /// Mirrors the pet window's heal: a failed animated load leaves the image
    /// blank with no retry, so fall back to the cached static sprite.
    /// </summary>
    private void OnAnimatedSpriteError(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_stream is null || _store is null || _subject is not { } s) return;
            _version++;
            var detail = e is AnimationErrorEventArgs args && args.Exception is { } ex ? $": {ex.Message}" : "";
            AppLog.Write($"sprite slot animated render failed{detail} (species {s.Species}); falling back to static");
            var fallback = _store.CachedSubject(s.Species, animated: false, s.Shiny, s.Form);
            if (fallback is not null) Render(fallback);
        }
        catch (Exception ex)
        {
            AppLog.Write($"sprite slot animated heal failed: {ex.Message}");
        }
    }
}
