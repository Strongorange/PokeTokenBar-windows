using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Ui;

namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// The dex reveal must center the floating subject's tile in the viewport —
/// ScrollIntoView would only drag it to an edge. The layout test runs a real
/// WrapPanel grid headless (manual Measure/Arrange, like the M13 tile-grid
/// smoke checks) so the offset math is verified on actual tile positions.
/// </summary>
public class DexScrollTests
{
    private const double TileWidth = 76;
    private const double TileHeight = 100;

    [Fact]
    public void CenteredOffsetCentersAndClamps()
    {
        // A tile in the middle: top 300, height 100, viewport 200 → 350 - 100.
        Assert.Equal(250, DexScroll.CenteredOffset(300, 100, 200));
        // The first tile clamps to the top instead of scrolling above the grid.
        Assert.Equal(0, DexScroll.CenteredOffset(0, 100, 200));
        // A tile taller than the viewport still centers (top 40 + 150 - 100).
        Assert.Equal(90, DexScroll.CenteredOffset(40, 300, 200));
    }

    [Fact]
    public void CenterOnPlacesTheSubjectTileAtTheViewportMiddle()
    {
        Sta.Run(() =>
        {
            var list = Grid(tiles: 24, viewport: new Size(320, 200));
            var viewer = NotNull(DexScroll.ScrollViewerOf(list));

            DexScroll.CenterOn(list, 14);

            Assert.True(viewer.VerticalOffset > 0);
            AssertCentered(list, viewer, 14);
        });
    }

    [Fact]
    public void CenterOnKeepsTheTopRowAtTheTop()
    {
        Sta.Run(() =>
        {
            var list = Grid(tiles: 24, viewport: new Size(320, 200));
            var viewer = NotNull(DexScroll.ScrollViewerOf(list));

            DexScroll.CenterOn(list, 0);

            Assert.Equal(0, viewer.VerticalOffset);
        });
    }

    [Fact]
    public void CenterOnIgnoresOutOfRangeIndexes()
    {
        Sta.Run(() =>
        {
            var list = Grid(tiles: 4, viewport: new Size(320, 200));
            var viewer = NotNull(DexScroll.ScrollViewerOf(list));
            var before = viewer.VerticalOffset;

            DexScroll.CenterOn(list, -1);
            DexScroll.CenterOn(list, list.Items.Count);

            Assert.Equal(before, viewer.VerticalOffset);
        });
    }

    /// <summary>The user-visible contract: the tile's center sits at the
    /// viewport's middle, measured in viewport coordinates (a different frame
    /// from the content coordinates CenterOn computes with).</summary>
    private static void AssertCentered(ListBox list, ScrollViewer viewer, int index)
    {
        var tile = (FrameworkElement) list.ItemContainerGenerator.ContainerFromIndex(index)!;
        var topInViewport = tile.TranslatePoint(new Point(), viewer).Y;
        var tileCenter = topInViewport + tile.RenderSize.Height / 2;
        Assert.InRange(tileCenter, viewer.ViewportHeight / 2 - 1, viewer.ViewportHeight / 2 + 1);
    }

    private static ListBox Grid(int tiles, Size viewport)
    {
        var list = new ListBox { Width = viewport.Width, Height = viewport.Height };
        // WrapPanel trap from M13: without Disabled the ScrollViewer hands the
        // panel infinite width, nothing wraps and the grid is not scrollable
        // (Theme.xaml's implicit style does this in the real app).
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        list.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
        for (var index = 0; index < tiles; index++)
            list.Items.Add(new Border
            {
                Width = TileWidth,
                Height = TileHeight,
                Margin = new Thickness(1),
                Background = Brushes.Transparent,
            });
        list.Measure(viewport);
        list.Arrange(new Rect(new Size(viewport.Width, viewport.Height)));
        list.UpdateLayout();
        return list;
    }

    private static ScrollViewer NotNull(ScrollViewer? viewer) =>
        viewer ?? throw new InvalidOperationException("ListBox template has no ScrollViewer");
}
