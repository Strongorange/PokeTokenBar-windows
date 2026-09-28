using System.Windows;
using System.Windows.Controls;
using PokeTokenBar.Application;

namespace PokeTokenBar.Ui;

public partial class SpeciesDetailWindow : Window
{
    public SpeciesDetailWindow(CompanionDetailSnapshot detail)
    {
        InitializeComponent();
        Title = $"#{detail.SpeciesID} {detail.Name}";
        Build(detail);
    }

    private void Build(CompanionDetailSnapshot detail)
    {
        AddTitle($"{detail.Name}  ·  #{detail.SpeciesID}");
        AddSecondary(string.Join(" · ", new[]
        {
            detail.Rarity is { } rarity ? RarityText(rarity) : "",
            detail.Types.Count > 0 ? string.Join("/", detail.Types) : "",
            $"BST {detail.BaseStatTotal}",
            $"Height {detail.Height / 10.0:0.0} m",
            $"Weight {detail.Weight / 10.0:0.0} kg"
        }.Where(part => part.Length > 0)));

        if (detail.Individuals.Count > 0)
        {
            AddSection("Individuals");
            foreach (var individual in detail.Individuals)
            {
                var header = new List<string> { individual.Label };
                if (individual.IsShiny) header.Add("★");
                if (individual.IsRaising) header.Add("raising");
                header.Add($"Lv. {individual.Level}");
                if (individual.Gender.Length > 0) header.Add(individual.Gender);
                if (individual.Nature.Length > 0) header.Add(individual.Nature);
                AddBody(string.Join(" · ", header));
                AddSecondary((individual.Ability.Length > 0
                        ? $"Ability: {individual.Ability}{(individual.AbilityIsHidden ? " (hidden)" : "")}"
                        : "Ability: —")
                    + (individual.Moves.Count > 0
                        ? $" · Moves: {string.Join(", ", individual.Moves.Select(move => $"{move.Name} (Lv. {move.LearnedAtLevel})"))}"
                        : " · Moves: —"));
                foreach (var stat in individual.Stats)
                    AddStatRow(StatLabel(stat.Name), stat.Value, $"base {stat.Base} · IV {stat.Iv}");
            }
        }

        if (detail.BaseStats.Count > 0)
        {
            AddSection("Base stats");
            foreach (var stat in detail.BaseStats)
                AddStatRow(StatLabel(stat.Name), stat.Value, "");
        }

        if (detail.Abilities.Count > 0)
        {
            AddSection("Abilities");
            AddBody(string.Join(" · ", detail.Abilities
                .Select(ability => ability.IsHidden ? $"{ability.Name} (hidden)" : ability.Name)));
        }

        if (detail.Moves.Count > 0)
        {
            AddSection($"Move list ({detail.Moves.Count})");
            foreach (var move in detail.Moves)
                AddSecondary($"{move.Name} — {string.Join(" · ", move.Methods)}");
        }
    }

    private static string StatLabel(string stat) => stat switch
    {
        "hp" => "HP",
        "attack" => "Atk",
        "defense" => "Def",
        "special-attack" => "SpA",
        "special-defense" => "SpD",
        "speed" => "Spe",
        _ => stat
    };

    private static string RarityText(Core.Rarity rarity) => rarity switch
    {
        Core.Rarity.Common => "common",
        Core.Rarity.Uncommon => "uncommon",
        Core.Rarity.Rare => "rare",
        Core.Rarity.Legendary => "legendary",
        _ => ""
    };

    private void AddTitle(string text) =>
        ContentRoot.Children.Add(new TextBlock { Text = text, FontSize = 16, FontWeight = FontWeights.Bold });

    private void AddSection(string text) =>
        ContentRoot.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 14, 0, 4)
        });

    private void AddBody(string text) =>
        ContentRoot.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });

    private void AddSecondary(string text) =>
        ContentRoot.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = System.Windows.Media.Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });

    private void AddStatRow(string label, int value, string suffix)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Width = 36,
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray
        });
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 400,
            Value = value,
            Width = 220,
            Height = 10
        };
        panel.Children.Add(bar);
        panel.Children.Add(new TextBlock
        {
            Text = $" {value}{(suffix.Length > 0 ? $"  ({suffix})" : "")}",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11
        });
        ContentRoot.Children.Add(panel);
    }
}
