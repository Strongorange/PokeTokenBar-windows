using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Plays hatch/evolve/ditto celebrations next to the companion sprite, mirroring
/// the macOS CompanionHeader cues (CompanionView.swift 642-703): white flash +
/// spring pop for the moment itself, delayed ✨/🎭 bursts, an orange "+XP"
/// capsule for candy, a three-spark ✨ shimmer for mint, and a repeating egg
/// wiggle once incubation passes 90%.
/// </summary>
internal sealed class CompanionCelebrationPlayer
{
    private static readonly TimeSpan FlashDuration = TimeSpan.FromSeconds(0.8);
    private static readonly TimeSpan PopDuration = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan PlayGap = TimeSpan.FromSeconds(0.9);
    private static readonly TimeSpan BurstDelay = TimeSpan.FromSeconds(0.3);
    private static readonly TimeSpan BurstLinger = TimeSpan.FromSeconds(2.6);
    private static readonly TimeSpan CandyLinger = TimeSpan.FromSeconds(1.3);
    private static readonly TimeSpan MintLinger = TimeSpan.FromSeconds(0.9);
    private static readonly TimeSpan WiggleDuration = TimeSpan.FromSeconds(0.35);

    private readonly Border _tile;
    private readonly ScaleTransform _scale;
    private readonly RotateTransform _rotation;
    private readonly Border _flash;
    private readonly FrameworkElement _shinyBurst;
    private readonly ScaleTransform _shinyBurstScale;
    private readonly FrameworkElement _dittoBurst;
    private readonly ScaleTransform _dittoBurstScale;
    private readonly Border _candyCapsule;
    private readonly TextBlock _candyText;
    private readonly TranslateTransform _candySlide;
    private readonly Grid _mintSparkle;
    private readonly ScaleTransform _mintScale;
    private readonly Queue<CompanionCelebration> _pending = new();
    private bool _playing;
    private bool _eggImminent;

    public CompanionCelebrationPlayer(
        Border tile, ScaleTransform scale, RotateTransform rotation, Border flash,
        FrameworkElement shinyBurst, ScaleTransform shinyBurstScale,
        FrameworkElement dittoBurst, ScaleTransform dittoBurstScale,
        Border candyCapsule, TextBlock candyText, TranslateTransform candySlide,
        Grid mintSparkle, ScaleTransform mintScale)
    {
        _tile = tile;
        _scale = scale;
        _rotation = rotation;
        _flash = flash;
        _shinyBurst = shinyBurst;
        _shinyBurstScale = shinyBurstScale;
        _dittoBurst = dittoBurst;
        _dittoBurstScale = dittoBurstScale;
        _candyCapsule = candyCapsule;
        _candyText = candyText;
        _candySlide = candySlide;
        _mintSparkle = mintSparkle;
        _mintScale = mintScale;
        _candyCapsule.Background = HexBrush("#26" + "F7630C");
        _candyText.Foreground = (Brush) tile.FindResource("RarityLegendaryBrush");
    }

    public void Play(IReadOnlyList<CompanionCelebration> celebrations)
    {
        if (celebrations.Count == 0) return;
        foreach (var celebration in celebrations) _pending.Enqueue(celebration);
        if (!_playing) PlayQueue();
    }

    public void SetEggImminent(bool imminent)
    {
        if (imminent == _eggImminent) return;
        _eggImminent = imminent;
        if (imminent)
        {
            var wiggle = new DoubleAnimation(-5, 5, WiggleDuration)
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            _rotation.BeginAnimation(RotateTransform.AngleProperty, wiggle);
        }
        else
        {
            _rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }

    private async void PlayQueue()
    {
        _playing = true;
        try
        {
            while (_pending.Count > 0)
            {
                var celebration = _pending.Dequeue();
                switch (celebration.Kind)
                {
                    case CompanionCelebrationKind.CandyXp:
                        ShowCandy(celebration.Amount);
                        break;
                    case CompanionCelebrationKind.MintSparkle:
                        ShowMint();
                        break;
                    case CompanionCelebrationKind.Hatch:
                        PlayFlashAndPop();
                        if (celebration.Shiny) Burst(_shinyBurst, _shinyBurstScale, BurstDelay);
                        await Task.Delay(PlayGap);
                        break;
                    case CompanionCelebrationKind.DittoReveal:
                        PlayFlashAndPop();
                        Burst(_dittoBurst, _dittoBurstScale, TimeSpan.FromSeconds(0.25));
                        if (celebration.Shiny)
                            Burst(_shinyBurst, _shinyBurstScale, TimeSpan.FromSeconds(0.45));
                        await Task.Delay(PlayGap);
                        break;
                    default:
                        PlayFlashAndPop();
                        await Task.Delay(PlayGap);
                        break;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        _playing = false;
    }

    private void PlayFlashAndPop()
    {
        var flash = new DoubleAnimation(0.85, 0, FlashDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        _flash.BeginAnimation(UIElement.OpacityProperty, flash);
        var pop = new DoubleAnimation(0.6, 1, PopDuration)
        {
            EasingFunction = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 3,
                Springiness = 7
            }
        };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private async void Burst(FrameworkElement burst, ScaleTransform scale, TimeSpan delay)
    {
        await Task.Delay(delay);
        burst.BeginAnimation(UIElement.OpacityProperty, Fade(0, 1, TimeSpan.FromSeconds(0.25), spring: true));
        AnimateScale(scale, 0, 1, TimeSpan.FromSeconds(0.25), spring: true);
        await Task.Delay(BurstLinger - delay);
        burst.BeginAnimation(UIElement.OpacityProperty, Fade(1, 0, TimeSpan.FromSeconds(0.5)));
    }

    private async void ShowCandy(long amount)
    {
        _candyText.Text = "+" + TokenFormatter.Compact(amount) + " XP";
        _candyCapsule.BeginAnimation(UIElement.OpacityProperty,
            Fade(0, 1, TimeSpan.FromSeconds(0.3), spring: true));
        var slide = new DoubleAnimation(10, 0, TimeSpan.FromSeconds(0.3))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        _candySlide.BeginAnimation(TranslateTransform.YProperty, slide);
        await Task.Delay(CandyLinger);
        _candyCapsule.BeginAnimation(UIElement.OpacityProperty,
            Fade(1, 0, TimeSpan.FromSeconds(0.4)));
    }

    private async void ShowMint()
    {
        _mintSparkle.BeginAnimation(UIElement.OpacityProperty,
            Fade(0, 1, TimeSpan.FromSeconds(0.25), spring: true));
        AnimateScale(_mintScale, 0, 1, TimeSpan.FromSeconds(0.25), spring: true);
        await Task.Delay(MintLinger);
        _mintSparkle.BeginAnimation(UIElement.OpacityProperty,
            Fade(1, 0, TimeSpan.FromSeconds(0.4)));
    }

    private static void AnimateScale(ScaleTransform scale, double from, double to,
        TimeSpan duration, bool spring = false)
    {
        var animation = new DoubleAnimation(from, to, duration);
        if (spring)
            animation.EasingFunction = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 2,
                Springiness = 6
            };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static DoubleAnimation Fade(double from, double to, TimeSpan duration, bool spring = false)
    {
        var animation = new DoubleAnimation(from, to, duration);
        if (spring)
            animation.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        return animation;
    }

    private static SolidColorBrush HexBrush(string hex)
    {
        var brush = new SolidColorBrush((Color) ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
