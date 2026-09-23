using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using XamlAnimatedGif;

namespace PokeTokenBar.Ui;

public partial class FloatingPetWindow : Window
{
    private readonly CompanionEngine _engine;
    private readonly SpriteStore _store;
    private readonly Action<double, double> _onMoved;
    private MemoryStream? _gifStream;
    private int _renderVersion;
    private (bool Egg, int Species, bool Shiny, UnownForm? Form)? _lastSubject;
    private Point _downScreen;
    private Point _downOrigin;
    private bool _pressed;
    private bool _dragged;
    private long _lastClickTick;
    private Point _lastClickScreen;

    public FloatingPetWindow(CompanionEngine engine, SpriteStore store, double size,
        Action<double, double> onMoved)
    {
        InitializeComponent();
        _engine = engine;
        _store = store;
        _onMoved = onMoved;
        ApplySize(size);
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (_, _) => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    public void ApplySize(double size)
    {
        Width = size;
        Height = size;
        PlaceholderText.FontSize = size * 0.6;
    }

    public void Place(double? x, double? y)
    {
        if (x is { } px && y is { } py) SetLocation(px, py);
        if (!IntersectsVirtualScreen()) DefaultLocation();
    }

    public void Update(CompanionGameView view)
    {
        var subject = (view.IsEgg, view.ActiveSpeciesID, view.IsShiny, view.ActiveUnownForm);
        if (_lastSubject == subject) return;
        _lastSubject = subject;
        var version = ++_renderVersion;
        var egg = view.IsEgg;
        var speciesID = view.ActiveSpeciesID;
        var shiny = view.IsShiny;
        var form = view.ActiveUnownForm;

        var cached = egg
            ? WrapEgg(_store.CachedEgg())
            : _store.CachedSubject(speciesID, PokemonAssets.HasAnimatedSprite(speciesID), shiny, form);
        if (cached is not null)
        {
            Render(cached);
            return;
        }
        ShowPlaceholder(egg ? "🥚" : "❔");
        Task.Run(() =>
        {
            var fetched = egg
                ? WrapEgg(_store.Egg())
                : _store.Subject(speciesID, PokemonAssets.HasAnimatedSprite(speciesID), shiny, form);
            if (fetched is null) return;
            Dispatcher.BeginInvoke(() =>
            {
                if (version == _renderVersion) Render(fetched);
            });
        });
    }

    private static SpriteBytes? WrapEgg(byte[]? data) =>
        data is null ? null : new SpriteBytes(data, Animated: false);

    private void Render(SpriteBytes sprite)
    {
        DetachSprite();
        if (sprite.Animated)
        {
            var stream = new MemoryStream(sprite.Data);
            _gifStream = stream;
            AnimationBehavior.SetSourceStream(SpriteImage, stream);
        }
        else
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(sprite.Data);
            image.EndInit();
            image.Freeze();
            SpriteImage.Source = image;
        }
        PlaceholderText.Visibility = Visibility.Collapsed;
        SpriteImage.Visibility = Visibility.Visible;
    }

    private void ShowPlaceholder(string glyph)
    {
        DetachSprite();
        PlaceholderText.Text = glyph;
        PlaceholderText.Visibility = Visibility.Visible;
        SpriteImage.Visibility = Visibility.Collapsed;
    }

    private void DetachSprite()
    {
        AnimationBehavior.SetSourceStream(SpriteImage, null);
        SpriteImage.Source = null;
        _gifStream = null;
    }

    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        if (_engine is null) return;
        _pressed = true;
        _dragged = false;
        _downScreen = Root.PointToScreen(e.GetPosition(Root));
        _downOrigin = new Point(Left, Top);
        Root.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed) return;
        var screen = Root.PointToScreen(e.GetPosition(Root));
        var dx = screen.X - _downScreen.X;
        var dy = screen.Y - _downScreen.Y;
        if (!FloatingPetGeometry.IsClick(dx, dy)) _dragged = true;
        if (_dragged) SetLocation(_downOrigin.X + dx, _downOrigin.Y + dy);
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        Root.ReleaseMouseCapture();
        if (_dragged)
        {
            _onMoved(Left, Top);
            return;
        }
        var screen = Root.PointToScreen(e.GetPosition(Root));
        var elapsed = Environment.TickCount64 - _lastClickTick;
        var distanceX = screen.X - _lastClickScreen.X;
        var distanceY = screen.Y - _lastClickScreen.Y;
        if (elapsed <= GetDoubleClickTime()
            && FloatingPetGeometry.IsClick(distanceX, distanceY))
        {
            _lastClickTick = 0;
            if (System.Windows.Application.Current is App app) app.ShowDashboard();
        }
        else
        {
            _lastClickTick = Environment.TickCount64;
            _lastClickScreen = screen;
        }
    }

    private void OnOpenDashboardClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app) app.ShowDashboard();
    }

    private void OnHidePetClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app) app.SetPetEnabled(false);
    }

    private void SetLocation(double x, double y)
    {
        Left = x;
        Top = y;
    }

    private void DefaultLocation()
    {
        var work = SystemParameters.WorkArea;
        SetLocation(work.Right - Width - 24, work.Bottom - Height - 24);
    }

    private bool IntersectsVirtualScreen()
    {
        var left = SystemParameters.VirtualScreenLeft;
        var top = SystemParameters.VirtualScreenTop;
        var bounds = new Rect(left, top,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        return bounds.IntersectsWith(new Rect(Left, Top, Width, Height));
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!IntersectsVirtualScreen()) DefaultLocation();
            });
        }
        catch (Exception ex)
        {
            AppLog.Write($"pet display-change handler failed: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetDoubleClickTime();
}
