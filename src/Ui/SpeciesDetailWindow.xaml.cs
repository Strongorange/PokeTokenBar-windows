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
    private readonly CompanionEngine _engine;
    private readonly IReadOnlyList<CompanionUnownFormStatus> _unownForms;
    private readonly StackPanel _individualsHost = new();
    private readonly Dictionary<UnownForm, Border> _formTiles = [];
    private readonly SpriteSlot _sprite;
    private Button? _representativeButton;
    private TextBlock? _shinyLine;
    private TextBlock? _raisingLine;
    private UnownForm? _selectedForm;
    private int _selectedIndividualIndex;

    public SpeciesDetailWindow(CompanionDetailSnapshot detail, SpriteStore sprites,
        CompanionEngine engine)
    {
        InitializeComponent();
        Title = DashboardText.PokemonDetailsTitle(detail.Language);
        _sprites = sprites;
        _detail = detail;
        _engine = engine;
        _unownForms = detail.SpeciesID == UnownForms.SpeciesID ? detail.UnownForms : [];
        _sprite = new SpriteSlot(SpriteImage, SpritePlaceholder);
        _selectedForm = _unownForms.Count > 0 ? _unownForms[0].Form : null;
        Build(detail);
        UpdateHeroSprite();
        RenderIdentityLines();
        HighlightSelectedForm();
    }

    private bool IsRepresentative =>
        _engine.State.RepresentativeSpeciesID == _detail.SpeciesID
        && (_detail.SpeciesID != UnownForms.SpeciesID
            || UnownForms.Resolved(_detail.SpeciesID, _selectedForm)
                == _engine.State.RepresentativeUnownForm);

    private void ToggleRepresentative()
    {
        if (IsRepresentative)
            _engine.SetRepresentative(null);
        else
            _engine.SetRepresentative(_detail.SpeciesID, _selectedForm);
        UpdateRepresentativeButton();
    }

    private void UpdateRepresentativeButton()
    {
        if (_representativeButton is not { } button) return;
        var lang = _detail.Language;
        var isRepresentative = IsRepresentative;
        button.Content = isRepresentative
            ? "★ " + DashboardText.RepresentativeFollowCurrent(lang)
            : "☆ " + DashboardText.RepresentativeSet(lang);
        button.ToolTip = button.Content;
        if (isRepresentative)
        {
            button.ClearValue(Button.StyleProperty);
            button.Foreground = Token("AccentBrush");
        }
        else
        {
            button.Style = (Style) FindResource("AccentButton");
            button.ClearValue(Button.ForegroundProperty);
        }
    }

    private List<CompanionDetailIndividual> FilteredIndividuals() =>
        _selectedForm is { } form
            ? _detail.Individuals.Where(individual => individual.UnownForm == form).ToList()
            : _detail.Individuals.ToList();

    private void Build(CompanionDetailSnapshot detail)
    {
        var lang = detail.Language;
        AddTitle($"{detail.Name}  ·  #{detail.SpeciesID}");
        if (detail.Rarity is { } rarity)
            AddSecondary(ContentRoot, DashboardText.RarityLabel(lang, rarity), header: true);
        _shinyLine = new TextBlock { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
        _raisingLine = new TextBlock
        {
            FontSize = 11,
            Foreground = Token("AccentBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        };
        HeaderRoot.Children.Add(_shinyLine);
        HeaderRoot.Children.Add(_raisingLine);

        var representativeButton = new Button
        {
            MinHeight = 26,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0),
        };
        representativeButton.Click += (_, _) => ToggleRepresentative();
        _representativeButton = representativeButton;
        HeaderRoot.Children.Add(representativeButton);
        UpdateRepresentativeButton();

        if (_unownForms.Count > 0)
            BuildUnownPicker(lang);

        ContentRoot.Children.Add(_individualsHost);
        RenderIndividuals();
        BuildSpeciesData(detail, lang);
        BuildMoveList(detail, lang);
    }

    private void BuildSpeciesData(CompanionDetailSnapshot detail, AppLanguage lang)
    {
        AddSection(ContentRoot, DashboardText.SpeciesDataTitle(lang));
        if (detail.Types.Count > 0)
            AddTypeCapsules(detail.Types);
        AddValuePairs(ContentRoot,
            (DashboardText.HeightLabel(lang), $"{detail.Height / 10.0:0.0} m"),
            (DashboardText.WeightLabel(lang), $"{detail.Weight / 10.0:0.0} kg"),
            (DashboardText.BaseTotalLabel(lang), $"{detail.BaseStatTotal}"));
        if (detail.Abilities.Count > 0)
        {
            AddSubSection(ContentRoot, DashboardText.PossibleAbilitiesTitle(lang));
            AddSecondary(ContentRoot, string.Join(" · ", detail.Abilities
                .Select(ability => ability.IsHidden
                    ? $"{ability.Name} ({DashboardText.HiddenMark(lang)})" : ability.Name)));
        }
    }

    private void BuildMoveList(CompanionDetailSnapshot detail, AppLanguage lang)
    {
        if (detail.Moves.Count == 0) return;
        AddSection(ContentRoot, DashboardText.MoveListCount(lang, detail.Moves.Count));
        foreach (var move in detail.Moves)
            AddSecondary(ContentRoot, $"{move.Name} — {string.Join(" · ", move.Methods)}");
    }

    private void BuildUnownPicker(AppLanguage lang)
    {
        AddSection(ContentRoot, DashboardText.UnownFormsCollected(lang, _unownForms.Count));
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
                Text = "✨",
                FontSize = 8,
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
                : $"{symbol}{(status.IsShiny ? " ✨" : "")}",
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
        _selectedIndividualIndex = 0;
        HighlightSelectedForm();
        UpdateHeroSprite();
        RenderIdentityLines();
        RenderIndividuals();
        UpdateRepresentativeButton();
    }

    private void RenderIdentityLines()
    {
        var lang = _detail.Language;
        var shiny = _selectedForm is { } form
            ? _unownForms.FirstOrDefault(entry => entry.Form == form)?.IsShiny == true
            : _detail.Individuals.Any(individual => individual.IsShiny);
        var raising = FilteredIndividuals().Any(individual => individual.IsRaising);
        if (_shinyLine is not null)
        {
            _shinyLine.Text = $"✨ {DashboardText.ShinyLabel(lang)}";
            _shinyLine.Visibility = shiny ? Visibility.Visible : Visibility.Collapsed;
        }
        if (_raisingLine is not null)
        {
            _raisingLine.Text = DashboardText.RaisingLabel(lang);
            _raisingLine.Visibility = raising ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private Brush Token(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Gray;

    private void HighlightSelectedForm()
    {
        foreach (var (form, tile) in _formTiles)
        {
            var selected = form == _selectedForm;
            tile.BorderBrush = selected ? Token("AccentBrush") : null;
            tile.Background = selected
                ? Token("AccentSoftBrush")
                : Token("HoverBrush");
        }
    }

    private void UpdateHeroSprite()
    {
        var individuals = FilteredIndividuals();
        bool shiny;
        if (_selectedForm is { } form)
            shiny = _unownForms.FirstOrDefault(entry => entry.Form == form)?.IsShiny == true;
        else if (individuals.Count > 0)
        {
            var index = Math.Clamp(_selectedIndividualIndex, 0, individuals.Count - 1);
            shiny = individuals[index].IsShiny;
        }
        else
            shiny = _detail.Individuals.Any(individual => individual.IsShiny);
        _sprite.Update(_sprites, _detail.SpeciesID, true, shiny, _selectedForm, "❔");
    }

    private void RenderIndividuals()
    {
        var lang = _detail.Language;
        var individuals = FilteredIndividuals();
        _individualsHost.Children.Clear();
        if (individuals.Count == 0)
        {
            RenderBaseStats(lang);
            return;
        }
        _selectedIndividualIndex = Math.Clamp(_selectedIndividualIndex, 0, individuals.Count - 1);

        if (individuals.Count > 1)
        {
            var picker = new ComboBox
            {
                Width = 170,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 2)
            };
            for (var i = 0; i < individuals.Count; i++)
                picker.Items.Add($"#{i + 1} · Lv. {individuals[i].Level}");
            picker.SelectedIndex = _selectedIndividualIndex;
            picker.SelectionChanged += (_, _) =>
            {
                if (picker.SelectedIndex < 0) return;
                _selectedIndividualIndex = picker.SelectedIndex;
                RenderIndividuals();
                UpdateHeroSprite();
            };
            _individualsHost.Children.Add(picker);
        }

        var individual = individuals[_selectedIndividualIndex];
        AddSection(_individualsHost, DashboardText.IndividualTitle(lang));
        AddValuePairs(_individualsHost,
            (DashboardText.LevelTitle(lang), $"{individual.Level}"),
            (DashboardText.GenderTitle(lang),
                individual.Gender.Length > 0 ? individual.Gender : "—"),
            (DashboardText.NatureTitle(lang),
                individual.Nature.Length > 0 ? individual.Nature : "—"));

        AddSubSection(_individualsHost, DashboardText.AbilityLabel(lang));
        var ability = individual.Ability.Length > 0
            ? individual.Ability + (individual.AbilityIsHidden
                ? $" · {DashboardText.HiddenAbilityTitle(lang)}" : "")
            : "—";
        AddBody(_individualsHost, ability);

        AddSubSection(_individualsHost, DashboardText.ActualStatsTitle(lang));
        var scale = PokemonStatCalculator.DisplayScaleMaximum(
            individuals[_selectedIndividualIndex].Stats.Select(stat => stat.Value).ToArray());
        foreach (var stat in individual.Stats)
            AddStatRow(_individualsHost, DashboardText.StatLabel(lang, stat.Name),
                stat.Value, stat.Iv, scale);

        AddSubSection(_individualsHost, DashboardText.KnownMovesTitle(lang));
        if (individual.Moves.Count == 0)
            AddSecondary(_individualsHost, DashboardText.NoLevelMoves(lang));
        else
            foreach (var move in individual.Moves)
                AddKnownMoveRow(_individualsHost, move);
    }

    private void RenderBaseStats(AppLanguage lang)
    {
        if (_detail.BaseStats.Count == 0) return;
        AddSection(_individualsHost, DashboardText.BaseStatsTitle(lang));
        foreach (var stat in _detail.BaseStats)
            AddStatRow(_individualsHost, DashboardText.StatLabel(lang, stat.Name),
                stat.Value, null, 300);
    }

    private void AddTitle(string text) =>
        HeaderRoot.Children.Add(new TextBlock { Text = text, FontSize = 16, FontWeight = FontWeights.Bold });

    private void AddSection(StackPanel target, string text) =>
        target.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Margin = new Thickness(0, 14, 0, 4)
        });

    private void AddSubSection(StackPanel target, string text) =>
        target.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Foreground = Token("TextSecondaryBrush"),
            Margin = new Thickness(0, 10, 0, 2)
        });

    private void AddTypeCapsules(IReadOnlyList<string> types)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 0)
        };
        foreach (var type in types)
        {
            row.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 5, 0),
                Background = Token("AccentSoftBrush"),
                Child = new TextBlock
                {
                    Text = type.ToUpperInvariant(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                },
            });
        }
        ContentRoot.Children.Add(row);
    }

    private void AddValuePairs(StackPanel target, params (string Label, string Value)[] pairs)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 0)
        };
        foreach (var (label, value) in pairs)
        {
            row.Children.Add(new StackPanel
            {
                Margin = new Thickness(0, 0, 16, 0),
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 9,
                        Foreground = Token("TextSecondaryBrush")
                    },
                    new TextBlock
                    {
                        Text = value,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 1, 0, 0)
                    }
                }
            });
        }
        target.Children.Add(row);
    }

    private void AddKnownMoveRow(StackPanel target, CompanionDetailKnownMove move)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var name = new TextBlock
        {
            Text = move.Name,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(name, 0);
        var level = new TextBlock
        {
            Text = $"Lv. {move.LearnedAtLevel}",
            FontSize = 11,
            Foreground = Token("TextSecondaryBrush"),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(level, 1);
        grid.Children.Add(name);
        grid.Children.Add(level);
        target.Children.Add(grid);
    }

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
                Foreground = Token("TextSecondaryBrush"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
            return;
        }
        target.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = Token("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
    }

    private void AddStatRow(StackPanel target, string label, int value, int? iv, int scaleMaximum)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });
        grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var name = new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = Token("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(name, 0);
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = scaleMaximum,
            Value = Math.Min(value, scaleMaximum),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0)
        };
        Grid.SetColumn(bar, 1);
        var valueText = new TextBlock
        {
            Text = value.ToString(),
            FontSize = 10,
            MinWidth = 28,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(name);
        grid.Children.Add(bar);
        grid.Children.Add(valueText);
        if (iv is { } ivValue)
        {
            var ivText = new TextBlock
            {
                Text = $"IV {ivValue}",
                FontSize = 10,
                Foreground = Token("TextSecondaryBrush"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(ivText, 3);
            grid.Children.Add(ivText);
        }
        target.Children.Add(grid);
    }
}
