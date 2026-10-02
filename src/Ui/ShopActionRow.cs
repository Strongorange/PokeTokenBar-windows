using System.Windows;
using System.Windows.Controls;

namespace PokeTokenBar.Ui;

/// <summary>
/// One "leading fill content + trailing action" row for shop/bag cards.
/// The trailing control (buy button, locked hint) is ALWAYS added first with
/// Dock.Right so <see cref="DockPanel.LastChildFill"/> (default true) can
/// never stretch it — the idle-row bug that made shop buy buttons fill the
/// whole card width. The optional leading element is added last and fills the
/// remaining space (left-aligned price / hint text). macOS parity: the shop
/// rows are `HStack { price ... Spacer() ... small bordered button }`.
/// Layout-only module: callers style their own buttons and text.
/// </summary>
internal static class ShopActionRow
{
    public static DockPanel Create(FrameworkElement? leading, FrameworkElement trailing)
    {
        var row = new DockPanel();
        DockPanel.SetDock(trailing, Dock.Right);
        row.Children.Add(trailing);
        if (leading is null)
        {
            row.LastChildFill = false;
        }
        else
        {
            row.Children.Add(leading);
        }
        return row;
    }
}
