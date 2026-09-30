using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PokeTokenBar.Ui;

/// <summary>
/// Vector stand-ins for the macOS celebration emoji (✨/🎭). WPF renders emoji
/// glyphs monochrome (no color-font support), so the bursts are drawn as
/// paths: a curved four-point sparkle and a comedy/tragedy mask pair.
/// </summary>
internal static class CelebrationGlyphs
{
    private const string SparklePath =
        "M 0,-0.5 C 0.06,-0.16 0.16,-0.06 0.5,0 C 0.16,0.06 0.06,0.16 0,0.5 " +
        "C -0.06,0.16 -0.16,0.06 -0.5,0 C -0.16,-0.06 -0.06,-0.16 0,-0.5 Z";

    public static FrameworkElement Sparkle(double size)
    {
        var host = new Grid();
        var star = new Path
        {
            Data = Geometry.Parse(SparklePath),
            Stretch = Stretch.Fill,
            Width = size,
            Height = size
        };
        star.SetResourceReference(Shape.FillProperty, "SparkleBrush");
        host.Children.Add(star);
        if (size >= 15)
            host.Children.Add(new Ellipse
            {
                Fill = Brushes.White,
                Width = size * 0.12,
                Height = size * 0.12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, size * 0.1, size * 0.14, 0)
            });
        return host;
    }

    public static FrameworkElement TheaterMasks()
    {
        var host = new Grid();
        var tragedy = Mask("RarityRareBrush", smile: false);
        tragedy.RenderTransform = new TranslateTransform(-5, -5);
        host.Children.Add(tragedy);
        var comedy = Mask("RarityLegendaryBrush", smile: true);
        comedy.RenderTransform = new TranslateTransform(5, 5);
        host.Children.Add(comedy);
        return host;
    }

    public static void FillMintCluster(Grid host)
    {
        host.Children.Clear();
        foreach (var (size, x, y) in new[]
                 { (22.0, -9.0, -8.0), (15.0, 11.0, 4.0), (12.0, 1.0, 11.0) })
        {
            var sparkle = Sparkle(size);
            sparkle.HorizontalAlignment = HorizontalAlignment.Center;
            sparkle.VerticalAlignment = VerticalAlignment.Center;
            sparkle.Margin = new Thickness(x, y, 0, 0);
            host.Children.Add(sparkle);
        }
    }

    private static Canvas Mask(string fillKey, bool smile)
    {
        const double w = 30;
        const double h = 32;
        var mask = new Canvas { Width = w, Height = h };
        var face = new Path
        {
            Data = Geometry.Parse(
                "M 0,3 C 0,0 30,0 30,3 L 29,25 C 28.6,31 1.4,31 1,25 Z")
        };
        face.SetResourceReference(Shape.FillProperty, fillKey);
        mask.Children.Add(face);
        foreach (var eyeX in new[] { 7.0, 18.0 })
        {
            var eye = new Ellipse { Width = 5, Height = 6, Fill = Brushes.White };
            Canvas.SetLeft(eye, eyeX);
            Canvas.SetTop(eye, 9);
            mask.Children.Add(eye);
        }
        var mouth = new Path
        {
            Data = Geometry.Parse(smile ? "M 7,19 Q 15,27 23,19" : "M 7,25 Q 15,17 23,25"),
            Stroke = Brushes.White,
            StrokeThickness = 2.5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        mask.Children.Add(mouth);
        return mask;
    }
}
