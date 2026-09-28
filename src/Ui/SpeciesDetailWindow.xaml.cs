using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class SpeciesDetailWindow : Window
{
    private readonly SpriteSlot _sprite;

    public SpeciesDetailWindow(CompanionDetailSnapshot detail, SpriteStore sprites)
    {
        InitializeComponent();
        Title = DashboardText.PokemonDetailsTitle(detail.Language);
        _sprite = new SpriteSlot(SpriteImage, SpritePlaceholder);
        Build(detail);
        var shiny = detail.Individuals.Any(individual => individual.IsShiny);
        _sprite.Update(sprites, detail.SpeciesID, true, shiny, null, "❔");
    }

    private void Build(CompanionDetailSnapshot detail)
    {
        var lang = detail.Language;
        AddTitle($"{detail.Name}  ·  #{detail.SpeciesID}");
        AddSecondary(string.Join(" · ", new[]
        {
            detail.Rarity is { } rarity ? DashboardText.RarityLabel(lang, rarity) : "",
            detail.Types.Count > 0 ? string.Join("/", detail.Types) : "",
            $"{DashboardText.BaseTotalLabel(lang)} {detail.BaseStatTotal}",
            $"{DashboardText.HeightLabel(lang)} {detail.Height / 10.0:0.0} m",
            $"{DashboardText.WeightLabel(lang)} {detail.Weight / 10.0:0.0} kg"
        }.Where(part => part.Length > 0)), header: true);

        if (detail.Individuals.Count > 0)
        {
            AddSection(DashboardText.IndividualsTitle(lang));
            foreach (var individual in detail.Individuals)
            {
                var header = new List<string> { individual.Label };
                if (individual.IsShiny) header.Add("★");
                if (individual.IsRaising) header.Add(DashboardText.RaisingLabel(lang));
                header.Add($"Lv. {individual.Level}");
                if (individual.Gender.Length > 0) header.Add(individual.Gender);
                if (individual.Nature.Length > 0) header.Add(individual.Nature);
                AddBody(string.Join(" · ", header));
                AddSecondary((individual.Ability.Length > 0
                        ? $"{DashboardText.AbilityLabel(lang)}: {individual.Ability}" +
                          (individual.AbilityIsHidden ? $" ({DashboardText.HiddenMark(lang)})" : "")
                        : $"{DashboardText.AbilityLabel(lang)}: —")
                    + $" · {DashboardText.KnownMovesTitle(lang)}: " +
                    (individual.Moves.Count > 0
                        ? string.Join(", ", individual.Moves.Select(move => $"{move.Name} (Lv. {move.LearnedAtLevel})"))
                        : "—"));
                foreach (var stat in individual.Stats)
                    AddStatRow(DashboardText.StatLabel(lang, stat.Name), stat.Value,
                        $"base {stat.Base} · IV {stat.Iv}");
            }
        }

        if (detail.BaseStats.Count > 0)
        {
            AddSection(DashboardText.BaseStatsTitle(lang));
            foreach (var stat in detail.BaseStats)
                AddStatRow(DashboardText.StatLabel(lang, stat.Name), stat.Value, "");
        }

        if (detail.Abilities.Count > 0)
        {
            AddSection(DashboardText.PossibleAbilitiesTitle(lang));
            AddBody(string.Join(" · ", detail.Abilities
                .Select(ability => ability.IsHidden
                    ? $"{ability.Name} ({DashboardText.HiddenMark(lang)})" : ability.Name)));
        }

        if (detail.Moves.Count > 0)
        {
            AddSection(DashboardText.MoveListCount(lang, detail.Moves.Count));
            foreach (var move in detail.Moves)
                AddSecondary($"{move.Name} — {string.Join(" · ", move.Methods)}");
        }
    }

    private void AddTitle(string text) =>
        HeaderRoot.Children.Add(new TextBlock { Text = text, FontSize = 16, FontWeight = FontWeights.Bold });

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

    private void AddSecondary(string text, bool header = false)
    {
        if (header)
        {
            HeaderRoot.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
            return;
        }
        ContentRoot.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
    }

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
            Width = 48,
            FontSize = 11,
            Foreground = Brushes.Gray
        });
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 400,
            Value = value,
            Width = 210,
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
