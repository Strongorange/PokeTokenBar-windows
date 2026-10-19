using System.IO;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;
using PokeTokenBar.Ui;

namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// Recap window smoke: construction runs the full render path (ledger load, meters with #368
/// tooltips, stats, graduate strip) on the STA thread without throwing.
/// </summary>
public class RecapWindowTests
{
    [Fact]
    public void WindowConstructsAndClosesCleanly()
    {
        Sta.Run(() =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "ptb-recap-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var service = new UsageRefreshService(new UsageRefreshOptions
            {
                CacheDirectory = dir,
                TimeZone = TimeZoneInfo.Utc,
                RootSource = new EmptyRootSource(),
                Providers =
                [
                    new ProviderRegistration("fixture", "Fixture", new HashSet<UsageRootKind>(),
                        (now, timeZone, roots) => new ProviderRefreshOutcome(true,
                            new UsageProviderSnapshot("fixture", "Fixture", []))),
                ],
            });
            var engine = new CompanionEngine(new CompanionEngineOptions
            {
                StateFilePath = Path.Combine(dir, "companion-state.json"),
                Clock = static () => new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero),
                NextRoll = static () => 1,
                Lines = PokemonLineSource.Load(new MemoryStream(
                    System.Text.Encoding.UTF8.GetBytes(
                        """{"format":"poketokenbar.pokemon-snapshot","schema":1,"bases":[],"lines":[],"names":{}}"""))),
            });
            var window = new RecapWindow(service, engine, new SpriteStore(), TimeZoneInfo.Utc);
            window.Close();
            try { Directory.Delete(dir, true); } catch { }
        });
    }

    private sealed class EmptyRootSource : IUsageRootSource
    {
        public IReadOnlyList<UsageRoot> Discover() => [];
    }
}
