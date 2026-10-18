using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Ui;

namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// Regression guard for the invisible floating pet (2026-10-13 Ledyba report):
/// a cached animated GIF whose load fails inside XamlAnimatedGif leaves the
/// Image blank with no retry, so the transparent pet window composites
/// nothing forever. The heal must fall back to the cached static sprite.
/// </summary>
public class AnimatedSpriteHealTests
{
    private const int Ledyba = 165;

    // "GIF89a" header followed by truncated data — raises the XamlAnimatedGif
    // load error synchronously (EndOfStreamException), like a corrupted cache
    // entry or a failed mid-swap load.
    private static readonly byte[] BrokenGif =
        [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0xFF, 0xFF, 0xFF, 0x00, 0x11, 0x22, 0x33];

    // 1x1 transparent PNG.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    private static string FixtureDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ptb-heal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, $"{Ledyba}-a.gif"), BrokenGif);
        File.WriteAllBytes(Path.Combine(directory, $"{Ledyba}-s.png"),
            Convert.FromBase64String(OnePixelPngBase64));
        return directory;
    }

    private static void Cleanup(string directory)
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
    }

    /// <summary>
    /// XamlAnimatedGif starts loading only once the Image is actually loaded
    /// (checking IsLoaded, so a manual Loaded event does not count). Host the
    /// element in a real but invisible offscreen window to reach the same
    /// state a shown floating pet is in; the load error then fires
    /// synchronously, exactly as in the 2026-10-13 incident.
    /// </summary>
    private static Window ShowInvisible(FrameworkElement content)
    {
        var host = new Window
        {
            Content = content,
            Width = 96,
            Height = 96,
            Opacity = 0,
            Left = -32000,
            Top = -32000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        host.Show();
        return host;
    }

    private static CompanionGameView SubjectView(int speciesID) => new(
        HasActive: true,
        ActiveName: "test",
        Rarity: null,
        IsShiny: false,
        HasGrowthBoost: false,
        StageIndex: 0,
        TotalForms: 1,
        StageUsed: 0,
        StageThreshold: 100,
        StageProgress: 0,
        IsEgg: false,
        EggUsed: 0,
        EggThreshold: 1,
        EggProgress: 0,
        DexCount: 1,
        LifetimeTokens: 0,
        AvailableTokens: 0,
        GrowthDifficulty: 1,
        ShopDifficulty: 1,
        StageItems: [],
        EvoLine: [],
        DexRows: [],
        RecentEvents: [],
        ShopRows: [],
        Bag: [],
        ActiveSpeciesID: speciesID,
        ActiveForm: null,
        EggGuarantee: null,
        RepresentativeSpeciesID: null,
        RepresentativeIsShiny: false,
        RepresentativeForm: null,
        Status: CompanionStatusKind.Idle,
        StatusEvolvedName: null,
        CatchRows: [],
        Language: AppLanguage.Ko);

    [Fact]
    public void Broken_animated_stream_heals_pet_window_to_static_sprite()
    {
        var directory = FixtureDirectory();
        try
        {
            Sta.Run(() =>
            {
                AppLog.Configure(Path.Combine(directory, "diag.log"));
                var engine = new CompanionEngine(new CompanionEngineOptions
                {
                    StateFilePath = Path.Combine(directory, "state.json"),
                });
                var store = new SpriteStore(directory, fetch: _ => null);
                var window = new FloatingPetWindow(engine, store, 96, (_, _) => { });
                window.Opacity = 0;
                window.Left = -32000;
                window.Top = -32000;
                try
                {
                    window.Show();
                    window.Update(SubjectView(Ledyba));

                    var source = Assert.IsType<BitmapImage>(window.SpriteImage.Source);
                    Assert.True(source.IsFrozen);
                }
                finally
                {
                    window.Close();
                }
            });
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public void Broken_animated_stream_heals_sprite_slot_to_static_sprite()
    {
        var directory = FixtureDirectory();
        try
        {
            Sta.Run(() =>
            {
                AppLog.Configure(Path.Combine(directory, "diag.log"));
                var image = new Image();
                var placeholder = new TextBlock();
                var slot = new SpriteSlot(image, placeholder);
                var store = new SpriteStore(directory, fetch: _ => null);
                var host = ShowInvisible(new StackPanel { Children = { image, placeholder } });
                try
                {
                    slot.Update(store, Ledyba, animated: true, shiny: false, form: null, glyph: "❔");

                    Assert.IsType<BitmapImage>(image.Source);
                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                }
                finally
                {
                    host.Close();
                }
            });
        }
        finally
        {
            Cleanup(directory);
        }
    }
}
