using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PokeTokenBar.Ui;

namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// Layout contract of the baseline-aligned cost+tokens value line.
/// Regression guard for the 2026-10-02 provider header defect: the cost sat
/// flush against the token count (margin was on the wrong side of the first
/// child) and the two bottom-aligned TextBlocks of different font sizes sat
/// on box bottoms, not on a shared baseline.
/// </summary>
public class UsageValuesLineTests
{
    private static TextBlock Arrange(string? cost, string tokens)
    {
        var line = UsageValuesLine.Create(cost, tokens, Brushes.Gray);
        line.Measure(new Size(400, 40));
        line.Arrange(new Rect(0, 0, 400, 40));
        return line;
    }

    private static List<Run> RunsOf(TextBlock line) =>
        line.Inlines.OfType<Run>().ToList();

    [Fact]
    public void BothValuesAreRunsOfOneTextBlockOnASingleLine()
    {
        Sta.Run(() =>
        {
            var line = Arrange("$132", "214.4M");
            var runs = RunsOf(line);
            Assert.Equal(2, runs.Count);
            Assert.Equal("$132", runs[0].Text.TrimEnd());
            Assert.Equal("214.4M", runs[1].Text);
            Assert.Empty(line.Inlines.OfType<LineBreak>());
            Assert.True(line.ActualHeight > 0, "line should be arranged");
        });
    }

    [Fact]
    public void CostRunCarriesTheGapBetweenValues()
    {
        Sta.Run(() =>
        {
            var line = Arrange("$132", "214.4M");
            var runs = RunsOf(line);
            Assert.EndsWith("   ", runs[0].Text);
            Assert.StartsWith("2", runs[1].Text);
        });
    }

    [Fact]
    public void MissingCostLeavesOnlyTheTokenRun()
    {
        Sta.Run(() =>
        {
            var line = Arrange(null, "190.6M");
            var runs = RunsOf(line);
            Assert.Single(runs);
            Assert.Equal("190.6M", runs[0].Text);
        });
    }
}
