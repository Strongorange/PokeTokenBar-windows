using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class SpeciesDetailWindow : Window
{
    private const double FormThumbSize = 28;

    private readonly SpriteStore _sprites;
    private readonly CompanionDetailSnapshot _detail;
    private readonly IReadOnlyList<CompanionUnownFormStatus> _unownForms;
    private readonly StackPanel _individualsHost = new();
    private readonly Dictionary<UnownForm, Border> _formTiles = [];
    private readonly SpriteSlot _sprite;
    private UnownForm? _selectedForm;

    public SpeciesDetailWindow(CompanionDetailSnapshot detail, SpriteStore sprites)
    {
        InitializeComponent();
        Title = DashboardText.PokemonDetailsTitle(detail.Language);
        _sprites = sprites;
        _detail = detail;
        _unownForms = detail.SpeciesID == UnownForms.SpeciesID ? detail.UnownForms : [];
        _sprite = new SpriteSlot(SpriteImage, SpritePlaceholder);
        _selectedForm = _unownForms.Count > 0 ? _unownForms[0].Form : null;
        Build(detail);
        UpdateHeroSprite();
        HighlightSelectedForm();
    }

    private void Build(CompanionDetailSnapshot detail)
    {
        var lang = detail.Language;
        AddTitle($"{detail.Name}  ·  #{detail.SpeciesID}");
        AddSecondary(ContentRoot, string.Join(" · ", new[]
        {
            detail.Rarity is { } rarity ? DashboardText.RarityLabel(lang, rarity) : "",
            detail.Types.Count > 0 ? string.Join("/", detail.Types) : "",
            $"{DashboardText.BaseTotalLabel(lang)} {detail.BaseStatTotal}",
            $"{DashboardText.HeightLabel(lang)} {detail.Height / 10.0:0.0} m",
            $"{DashboardText.WeightLabel(lang)} {detail.Weight / 10.0:0.0} kg"
        }.Where(part => part.Length > 0)), header: true);

        if (_unownForms.Count > 0)
            BuildUnownPicker(lang);

        if (detail.Individuals.Count > 0)
        {
            AddSection(DashboardText.IndividualsTitle(lang));
            ContentRoot.Children.Add(_individualsHost);
            RenderIndividuals();
        }

        if (detail.BaseStats.Count > 0)
        {
            AddSection(DashboardText.BaseStatsTitle(lang));
            foreach (var stat in detail.BaseStats)
                AddStatRow(ContentRoot, DashboardText.StatLabel(lang, stat.Name), stat.Value, "");
        }

        if (detail.Abilities.Count > 0)
        {
            AddSection(DashboardText.PossibleAbilitiesTitle(lang));
            AddBody(ContentRoot, string.Join(" · ", detail.Abilities
                .Select(ability => ability.IsHidden
                    ? $"{ability.Name} ({DashboardText.HiddenMark(lang)})" : ability.Name)));
        }

        if (detail.Moves.Count > 0)
        {
            AddSection(DashboardText.MoveListCount(lang, detail.Moves.Count));
            foreach (var move in detail.Moves)
                AddSecondary(ContentRoot, $"{move.Name} — {string.Join(" · ", move.Methods)}");
        }
    }

    private void BuildUnownPicker(AppLanguage lang)
    {
        AddSection(DashboardText.UnownFormsCollected(lang, _unownForms.Count));
        var grid = new UniformGrid { Columns = 7 };
        foreach (var form in UnownForms.All)
        {
            var status = _unownForms.FirstOrDefault(entry => entry.Form == form);
            var tile = BuildFormTile(form, status, lang);
            _formTiles[form] = tile;
            grid.Children.Add(tile);
        }
        ContentRoot.Children.Add(grid);
    }

    private Border BuildFormTile(UnownForm form, CompanionUnownFormStatus? status, AppLanguage lang)
    {
        var image = new Image
        {
            Width = FormThumbSize,
            Height = FormThumbSize,
            Stretch = Stretch.Uniform,
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        var placeholder = new TextBlock
        {
            Text = "❔",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var spriteHost = new Grid { Width = FormThumbSize, Height = FormThumbSize };
        spriteHost.Children.Add(image);
        spriteHost.Children.Add(placeholder);
        if (status?.IsShiny == true)
            spriteHost.Children.Add(new TextBlock
            {
                Text = "★",
                FontSize = 8,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            });
        new SpriteSlot(image, placeholder)
            .Update(_sprites, _detail.SpeciesID, false, status?.IsShiny == true, form, "❔");

        var symbol = UnownForms.Symbol(form);
        var tile = new Border
        {
            Padding = new Thickness(0, 3, 0, 2),
            Margin = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1.5),
            Opacity = status is null ? 0.25 : 1,
            Cursor = status is null ? null : Cursors.Hand,
            ToolTip = status is null
                ? $"{symbol} · {DashboardText.UnownNotCollected(lang)}"
                : $"{symbol}{(status.IsShiny ? " ★" : "")}",
            Child = new StackPanel
            {
                Children =
                {
                    spriteHost,
                    new TextBlock
                    {
                        Text = symbol,
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                },
            },
        };
        if (status is not null)
            tile.MouseLeftButtonUp += (_, _) => SelectForm(form);
        return tile;
    }

    private void SelectForm(UnownForm form)
    {
        if (_formTiles.GetValueOrDefault(form) is null) return;
        _selectedForm = form;
        HighlightSelectedForm();
        UpdateHeroSprite();
        RenderIndividuals();
    }

    private void HighlightSelectedForm()
    {
        foreach (var (form, tile) in _formTiles)
        {
            var selected = form == _selectedForm;
            tile.BorderBrush = selected ? SystemColors.HighlightBrush : null;
            tile.Background = selected
                ? new SolidColorBrush(Color.FromArgb(0x29, 0x00, 0x78, 0xD7))
                : new SolidColorBrush(Color.FromArgb(0x14, 0x80, 0x80, 0x80));
        }
    }

    private void UpdateHeroSprite()
    {
        var shiny = _selectedForm is { } form
            ? _unownForms.FirstOrDefault(entry => entry.Form == form)?.IsShiny == true
            : _detail.Individuals.Any(individual => individual.IsShiny);
        _sprite.Update(_sprites, _detail.SpeciesID, true, shiny, _selectedForm, "❔");
    }

    private void RenderIndividuals()
    {
        var lang = _detail.Language;
        var individuals = _selectedForm is { } form
            ? _detail.Individuals.Where(individual => individual.UnownForm == form)
            : _detail.Individuals;
        _individualsHost.Children.Clear();
        foreach (var individual in individuals)
        {
            var header = new List<string> { individual.Label };
            if (individual.IsShiny) header.Add("★");
            if (individual.IsRaising) header.Add(DashboardText.RaisingLabel(lang));
            header.Add($"Lv. {individual.Level}");
            if (individual.Gender.Length > 0) header.Add(individual.Gender);
            if (individual.Nature.Length > 0) header.Add(individual.Nature);
            AddBody(_individualsHost, string.Join(" · ", header));
            AddSecondary(_individualsHost, (individual.Ability.Length > 0
                    ? $"{DashboardText.AbilityLabel(lang)}: {individual.Ability}" +
                      (individual.AbilityIsHidden ? $" ({DashboardText.HiddenMark(lang)})" : "")
                    : $"{DashboardText.AbilityLabel(lang)}: —")
                + $" · {DashboardText.KnownMovesTitle(lang)}: " +
                (individual.Moves.Count > 0
                    ? string.Join(", ", individual.Moves.Select(move => $"{move.Name} (Lv. {move.LearnedAtLevel})"))
                    : "—"));
            foreach (var stat in individual.Stats)
                AddStatRow(_individualsHost, DashboardText.StatLabel(lang, stat.Name), stat.Value,
                    $"base {stat.Base} · IV {stat.Iv}");
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

    private void AddBody(StackPanel target, string text) =>
        target.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });

    private void AddSecondary(StackPanel target, string text, bool header = false)
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
        target.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
    }

    private void AddStatRow(StackPanel target, string label, int value, string suffix)
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
        target.Children.Add(panel);
    }
}
