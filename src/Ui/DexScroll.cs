using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokeTokenBar.Ui;

/// <summary>
/// Centers one dex tile in the grid's viewport. `ScrollIntoView` only drags
/// the tile to the viewport edge, which still hides the neighborhood — the
/// reveal wants the subject in the middle so its context is visible.
/// </summary>
internal static class DexScroll
{
    /// <summary>
    /// Vertical offset that puts the tile's center at the viewport's middle.
    /// Clamped at the top; the upper end is coerced by ScrollViewer itself.
    /// A tile taller than the viewport centers too (both ends stay cropped),
    /// which never occurs with the fixed-height dex tiles.
    /// </summary>
    internal static double CenteredOffset(double tileTop, double tileHeight, double viewportHeight) =>
        Math.Max(0, tileTop + tileHeight / 2 - viewportHeight / 2);

    /// <summary>
    /// Scrolls so the tile at <paramref name="index"/> sits centered. Requires
    /// the list to be attached and laid out — callers queue this behind the
    /// layout pass (tab content realizes lazily). The final UpdateLayout
    /// commits the requested offset: ScrollViewer applies it during the next
    /// arrange, so without it the scroll only lands whenever something else
    /// triggers layout.
    /// </summary>
    internal static void CenterOn(ListBox list, int index)
    {
        if (index < 0 || index >= list.Items.Count) return;
        list.UpdateLayout();
        if (list.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement tile)
            return;
        if (ScrollViewerOf(list) is not { } viewer || viewer.Content is not UIElement content)
            return;
        viewer.ScrollToVerticalOffset(CenteredOffset(
            tile.TranslatePoint(new Point(), content).Y,
            tile.RenderSize.Height,
            viewer.ViewportHeight));
        list.UpdateLayout();
    }

    /// <summary>Finds the ListBox template's ScrollViewer (Theme.xaml hosts the
    /// items in one) by walking the visual tree. Internal for tests asserting
    /// viewport-frame positions.</summary>
    internal static ScrollViewer? ScrollViewerOf(DependencyObject root)
    {
        var children = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < children; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer viewer) return viewer;
            if (ScrollViewerOf(child) is { } nested) return nested;
        }
        return null;
    }
}
