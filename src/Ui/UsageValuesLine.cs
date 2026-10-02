using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PokeTokenBar.Ui;

/// <summary>
/// One baseline-aligned "cost + tokens" value line for usage cards. Both
/// values are Runs inside a single TextBlock, so WPF aligns their baselines
/// and the cost run carries the inter-value gap — two bottom-aligned
/// TextBlocks of different font sizes sit on box bottoms (not baselines) and
/// a margin on the first child adds no gap between the values (the
/// 2026-10-02 provider header defect). Layout-only module: callers pass the
/// text and the cost brush.
/// </summary>
internal static class UsageValuesLine
{
    private const string Gap = "   ";

    public static TextBlock Create(string? costText, string tokensText, Brush costBrush)
    {
        var line = new TextBlock();
        if (!string.IsNullOrEmpty(costText))
            line.Inlines.Add(new Run(costText + Gap)
            {
                FontSize = 10,
                Foreground = costBrush,
            });
        line.Inlines.Add(new Run(tokensText)
        {
            FontSize = 13,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
        });
        return line;
    }
}
