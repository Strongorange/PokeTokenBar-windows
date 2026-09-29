using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// One item icon (shop/bag cards): cached sprite first, emoji fallback before
/// load / when the item has no sprite (mint) / on fetch failure. Mirrors the
/// macOS ItemIconView contract and SpriteSlot's cached-first pattern.
/// </summary>
internal sealed class ItemIconSlot
{
    private readonly Image _image;
    private readonly TextBlock _placeholder;
    private string? _name;
    private bool _set;
    private int _version;

    public ItemIconSlot(Image image, TextBlock placeholder)
    {
        _image = image;
        _placeholder = placeholder;
    }

    public void Update(SpriteStore store, ItemKind kind)
    {
        var name = kind.SpriteName();
        var emoji = kind.FallbackEmoji();
        if (_set && _name == name) return;
        _set = true;
        _name = name;
        var version = ++_version;
        if (name is not null)
        {
            var cached = store.CachedItem(name);
            if (cached is not null)
            {
                Render(cached);
                return;
            }
        }
        ShowPlaceholder(emoji);
        if (name is null) return;
        Task.Run(() =>
        {
            var fetched = store.Item(name);
            if (fetched is null) return;
            _image.Dispatcher.BeginInvoke(() =>
            {
                if (version == _version) Render(fetched);
            });
        });
    }

    private void Render(byte[] data)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(data);
        image.EndInit();
        image.Freeze();
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        _image.Source = image;
        _image.Visibility = Visibility.Visible;
        _placeholder.Visibility = Visibility.Collapsed;
    }

    private void ShowPlaceholder(string emoji)
    {
        _image.Source = null;
        _image.Visibility = Visibility.Collapsed;
        _placeholder.Text = emoji;
        _placeholder.Visibility = Visibility.Visible;
    }
}
