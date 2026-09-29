using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

public class CompanionPresentationTests
{
    [Fact]
    public void RarityDisplayOrderRunsFromMostPrecious()
    {
        Assert.Equal(
        [
            Rarity.Legendary, Rarity.Rare, Rarity.Uncommon, Rarity.Common
        ], CompanionPresentation.RarityDisplayOrder);
    }

    [Fact]
    public void RarityHexMatchesThemeBrushesWithoutHashPrefix()
    {
        Assert.Equal("F7630C", CompanionPresentation.RarityHex(Rarity.Legendary));
        Assert.Equal("0078D4", CompanionPresentation.RarityHex(Rarity.Rare));
        Assert.Equal("107C10", CompanionPresentation.RarityHex(Rarity.Uncommon));
        Assert.Equal("8A8A8A", CompanionPresentation.RarityHex(Rarity.Common));
        Assert.All(Enum.GetValues<Rarity>(),
            rarity => Assert.False(CompanionPresentation.RarityHex(rarity).StartsWith('#')));
    }

    [Fact]
    public void VisibleDexRowsFilterByRarity()
    {
        var rows = new List<CompanionDexRow>
        {
            new(1, "One", Rarity.Common, false, false),
            new(50, "Raremon", Rarity.Rare, false, false),
            new(10, "Solo", Rarity.Common, false, false),
        };

        Assert.Equal(3, CompanionPresentation.VisibleDexRows(rows, null).Count);
        var filtered = CompanionPresentation.VisibleDexRows(rows, Rarity.Common);
        Assert.Equal([1, 10], filtered.Select(row => row.SpeciesID));
        Assert.Empty(CompanionPresentation.VisibleDexRows(rows, Rarity.Legendary));
    }

    [Fact]
    public void VisibleCatchRowsFilterByRarity()
    {
        var rows = new List<CompanionCatchRow>
        {
            new("a", 1, Rarity.Common, false, true, false, "", null, null, []),
            new("b", 50, Rarity.Rare, true, false, true, "", null, Now, []),
        };

        Assert.Equal(2, CompanionPresentation.VisibleCatchRows(rows, null).Count);
        var filtered = CompanionPresentation.VisibleCatchRows(rows, Rarity.Rare);
        var row = Assert.Single(filtered);
        Assert.Equal("b", row.Key);
        Assert.True(row.IsReleased);
    }

    [Fact]
    public void ResolveSelectedProviderKeepsValidPick()
    {
        var selectable = new List<ProviderUsageSummary>
        {
            new("claude_code", "Claude Code", true, 100, 0, 200, 0),
            new("codex", "Codex", true, 50, 0, 0, 0),
        };

        Assert.Equal("codex",
            CompanionPresentation.ResolveSelectedProvider(selectable, "codex"));
    }

    [Fact]
    public void ResolveSelectedProviderFallsBackWhenMissingOrNull()
    {
        var selectable = new List<ProviderUsageSummary>
        {
            new("claude_code", "Claude Code", true, 100, 0, 200, 0),
            new("codex", "Codex", true, 50, 0, 0, 0),
        };

        Assert.Equal("claude_code",
            CompanionPresentation.ResolveSelectedProvider(selectable, null));
        Assert.Equal("claude_code",
            CompanionPresentation.ResolveSelectedProvider(selectable, "gone"));
        Assert.Null(CompanionPresentation.ResolveSelectedProvider([], null));
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
}
