using System.Windows;
using System.Windows.Controls;
using PokeTokenBar.Ui;

namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// Layout contract of the shop/bag action-row module. Regression guard for
/// the DockPanel.LastChildFill trap that stretched idle buy buttons across
/// the whole card width (2026-10-02 screenshot defect).
/// </summary>
public class ShopActionRowTests
{
    private const double RowWidth = 400;

    private static (DockPanel Row, FrameworkElement Leading, Button Trailing) ArrangeRow(
        FrameworkElement? leading)
    {
        var buy = new Button { Content = "구매", MinWidth = 70, MinHeight = 26 };
        var row = ShopActionRow.Create(leading, buy);
        row.Measure(new Size(RowWidth, 60));
        row.Arrange(new Rect(0, 0, RowWidth, 60));
        return (row, leading!, buy);
    }

    [Fact]
    public void Trailing_button_keeps_natural_width_next_to_leading_price()
    {
        Sta.Run(() =>
        {
            var price = new TextBlock { Text = "가격 250M" };
            var (_, _, buy) = ArrangeRow(price);

            Assert.True(buy.ActualWidth > 0, "button should be arranged");
            // Pre-fix the last docked child filled the row (~340px at 400px).
            Assert.InRange(buy.ActualWidth, 70, 150);
        });
    }

    [Fact]
    public void Leading_content_fills_the_space_the_button_does_not_use()
    {
        Sta.Run(() =>
        {
            var price = new TextBlock { Text = "가격 250M" };
            var (_, leading, buy) = ArrangeRow(price);

            Assert.InRange(leading.ActualWidth, RowWidth - 150, RowWidth - 70);
            Assert.InRange(leading.ActualWidth + buy.ActualWidth, RowWidth - 0.5, RowWidth + 0.5);
        });
    }

    [Fact]
    public void Trailing_button_keeps_natural_width_without_leading_content()
    {
        Sta.Run(() =>
        {
            var (_, _, buy) = ArrangeRow(null);

            Assert.True(buy.ActualWidth > 0, "button should be arranged");
            Assert.InRange(buy.ActualWidth, 70, 150);
        });
    }

    [Fact]
    public void Trailing_button_is_pinned_to_the_right_edge()
    {
        Sta.Run(() =>
        {
            var price = new TextBlock { Text = "가격 250M" };
            var (row, _, buy) = ArrangeRow(price);

            var left = buy.TranslatePoint(new Point(0, 0), row).X;
            Assert.InRange(left, RowWidth - 150, RowWidth - 70);
        });
    }
}
