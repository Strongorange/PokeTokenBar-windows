using System.Windows;
using System.Windows.Media;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Shared brush helpers for code-built UI: theme lookup scoped to a
/// FrameworkElement (works in smoke harnesses with a bare Application),
/// rarity brushes, and the sanctioned alpha-tint hex brushes.
/// </summary>
internal static class Paint
{
    public static Brush Token(FrameworkElement source, string key) =>
        source.TryFindResource(key) as Brush ?? Brushes.Gray;

    public static Brush RarityBrush(FrameworkElement source, Rarity rarity) => rarity switch
    {
        Rarity.Legendary => Token(source, "RarityLegendaryBrush"),
        Rarity.Rare => Token(source, "RarityRareBrush"),
        Rarity.Uncommon => Token(source, "RarityUncommonBrush"),
        _ => Token(source, "RarityCommonBrush"),
    };

    public static SolidColorBrush HexBrush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
