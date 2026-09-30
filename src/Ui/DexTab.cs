using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Dex grid + catch log renderer. Owns the rarity-filter and mode state;
/// pure decisions live in GameTabPresentation and CompanionPresentation
/// (unit-tested).
/// </summary>
internal sealed class DexTab
{
    private readonly FrameworkElement _theme;
    private readonly SpriteStore _sprites;
    private readonly Action _requestRender;
    private readonly FrameworkElement _modeToggle;
    private readonly TextBlock _header;
    private readonly StackPanel _rarityFilter;
    private readonly ListBox _list;
    private readonly FrameworkElement _catchLogPanel;
    private readonly TextBlock _catchHeader;
    private readonly StackPanel _catchRarityFilterHost;
    private readonly ListBox _catchList;
    private readonly FrameworkElement _empty;
    private readonly TextBlock _emptyTitle;
    private readonly TextBlock _emptyHint;
    private readonly SpriteSlot _emptySprite;
    private Rarity? _dexRarityFilter;
    private Rarity? _catchRarityFilter;
    private bool _showLog;
    private IReadOnlyList<CompanionDexRow> _visibleRows = [];

    public DexTab(
        FrameworkElement theme, SpriteStore sprites, Action requestRender,
        FrameworkElement modeToggle, TextBlock header, StackPanel rarityFilter,
        ListBox list, FrameworkElement catchLogPanel, TextBlock catchHeader,
        StackPanel catchRarityFilter, ListBox catchList,
        FrameworkElement empty, TextBlock emptyTitle, TextBlock emptyHint,
        Image emptySprite, TextBlock emptySpritePlaceholder)
    {
        _theme = theme;
        _sprites = sprites;
        _requestRender = requestRender;
        _modeToggle = modeToggle;
        _header = header;
        _rarityFilter = rarityFilter;
        _list = list;
        _catchLogPanel = catchLogPanel;
        _catchHeader = catchHeader;
        _catchRarityFilterHost = catchRarityFilter;
        _catchList = catchList;
        _empty = empty;
        _emptyTitle = emptyTitle;
        _emptyHint = emptyHint;
        _emptySprite = new SpriteSlot(emptySprite, emptySpritePlaceholder);
    }

    public void ShowLog(bool showLog) => _showLog = showLog;

    public CompanionDexRow? RowAt(int index) =>
        index >= 0 && index < _visibleRows.Count ? _visibleRows[index] : null;

    public void Render(CompanionGameView view)
    {
        var lang = view.Language;
        _modeToggle.Visibility = view.DexRows.Count == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        _catchLogPanel.Visibility = Visibility.Collapsed;
        _list.Items.Clear();
        _catchList.Items.Clear();
        if (view.DexRows.Count == 0)
        {
            _header.Text = "";
            _list.Visibility = Visibility.Collapsed;
            _rarityFilter.Visibility = Visibility.Collapsed;
            _empty.Visibility = Visibility.Visible;
            _emptyTitle.Text = DashboardText.DexEmptyTitle(lang);
            _emptyHint.Text = DashboardText.DexEmptyHint(lang);
            _emptySprite.Update(_sprites, 25, true, false, null, "❔");
            return;
        }
        _empty.Visibility = Visibility.Collapsed;
        if (_showLog)
        {
            _header.Text = "";
            _list.Visibility = Visibility.Collapsed;
            _rarityFilter.Visibility = Visibility.Collapsed;
            _catchLogPanel.Visibility = Visibility.Visible;
            _catchHeader.Text = GameTabPresentation.CatchHeader(view);
            RenderRarityTally(_catchRarityFilterHost,
                rarity => CompanionPresentation.RarityTally(view.CatchRows, rarity, row => row.Rarity),
                _catchRarityFilter, lang,
                rarity => _catchRarityFilter = _catchRarityFilter == rarity ? null : rarity);
            foreach (var row in CompanionPresentation.VisibleCatchRows(view.CatchRows, _catchRarityFilter))
                _catchList.Items.Add(CreateCatchCard(row, lang));
            return;
        }
        _header.Text = GameTabPresentation.DexHeader(view);
        RenderRarityTally(_rarityFilter,
            rarity => CompanionPresentation.RarityTally(view.DexRows, rarity, row => row.Rarity),
            _dexRarityFilter, lang,
            rarity => _dexRarityFilter = _dexRarityFilter == rarity ? null : rarity);
        _list.Visibility = Visibility.Visible;
        _rarityFilter.Visibility = _rarityFilter.Children.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _visibleRows = CompanionPresentation.VisibleDexRows(view.DexRows, _dexRarityFilter);
        foreach (var row in _visibleRows)
            _list.Items.Add(CreateDexTile(row, lang,
                row.SpeciesID == UnownForms.SpeciesID ? view.UnownForms.Count : 0,
                row.SpeciesID == view.RepresentativeSpeciesID));
    }

    private void RenderRarityTally(StackPanel host, Func<Rarity, int> countOf, Rarity? selected,
        AppLanguage lang, Action<Rarity> toggle)
    {
        host.Children.Clear();
        foreach (var rarity in CompanionPresentation.RarityDisplayOrder)
        {
            var count = countOf(rarity);
            if (count == 0) continue;
            host.Children.Add(CreateRarityTallyCapsule(rarity, count, selected == rarity, lang, toggle));
        }
    }

    private Border CreateRarityTallyCapsule(Rarity rarity, int count, bool selected,
        AppLanguage lang, Action<Rarity> toggle)
    {
        var brush = Paint.RarityBrush(_theme, rarity);
        var capsule = new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 5, 0),
            Background = selected ? brush : Paint.HexBrush("#14" + CompanionPresentation.RarityHex(rarity)),
            BorderBrush = brush,
            BorderThickness = new Thickness(selected ? 1.5 : 0.5),
            Opacity = 1,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = DashboardText.DexFilterHint(lang),
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 6, Height = 6, Fill = brush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        row.Children.Add(new TextBlock
        {
            Text = DashboardText.RarityLabel(lang, rarity),
            FontSize = 9, FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Token("TextPrimaryBrush"), Margin = new Thickness(4, 0, 3, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        row.Children.Add(new TextBlock
        {
            Text = count.ToString(), FontSize = 9, FontWeight = FontWeights.Bold,
            Foreground = Token("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center,
        });
        capsule.Child = row;
        capsule.MouseLeftButtonUp += (_, _) =>
        {
            toggle(rarity);
            _requestRender();
        };
        return capsule;
    }

    private UIElement CreateDexTile(CompanionDexRow row, AppLanguage lang, int unownCollected,
        bool isRepresentative)
    {
        var tile = new Grid
        {
            Width = 76,
            Margin = new Thickness(1),
            ToolTip = GameTabPresentation.TileTooltip(row, unownCollected, isRepresentative, lang),
        };
        for (var i = 0; i < 3; i++) tile.RowDefinitions.Add(new RowDefinition());

        var number = new StackPanel { Orientation = Orientation.Horizontal };
        number.Children.Add(new TextBlock
        {
            Text = "#" + row.SpeciesID,
            FontSize = 10,
            Foreground = Token("TextSecondaryBrush"),
        });
        if (isRepresentative)
        {
            var representativeStar = new TextBlock
            {
                Text = "★",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Token("AccentBrush"),
                Margin = new Thickness(3, 0, 0, 0),
                ToolTip = DashboardText.RepresentativeBadge(lang),
            };
            number.Children.Add(representativeStar);
        }
        var caption = new Grid();
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caption.Children.Add(number);
        if (row.IsShiny)
        {
            var shiny = new TextBlock
            {
                Text = "✨",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                ToolTip = "✨ " + DashboardText.ShinyLabel(lang),
            };
            Grid.SetColumn(shiny, 1);
            caption.Children.Add(shiny);
        }
        Grid.SetRow(caption, 0);
        tile.Children.Add(caption);

        var sprite = new Grid { Height = 64 };
        var image = new Image { Width = 64, Height = 64, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        var placeholder = new TextBlock
        {
            Text = "❔",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sprite.Children.Add(image);
        sprite.Children.Add(placeholder);
        Grid.SetRow(sprite, 1);
        tile.Children.Add(sprite);
        new SpriteSlot(image, placeholder).Update(_sprites, row.SpeciesID, false, row.IsShiny, null, "❔");

        var name = new TextBlock
        {
            Text = GameTabPresentation.TileName(row, unownCollected),
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetRow(name, 2);
        tile.Children.Add(name);
        if (!isRepresentative) return tile;
        return new Border
        {
            Background = Token("AccentSoftBrush"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(2, 0, 2, 2),
            Child = tile,
        };
    }

    /// <summary>
    /// Catch log card — port of the macOS DexEntryRow.
    /// </summary>
    private Border CreateCatchCard(CompanionCatchRow row, AppLanguage lang)
    {
        var card = new StackPanel();

        var header = new DockPanel();
        if (row.Nature.Length > 0)
        {
            var nature = new TextBlock
            {
                Text = row.Nature,
                FontSize = 9,
                Foreground = Token("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(nature, Dock.Right);
            header.Children.Add(nature);
        }
        var badges = new StackPanel { Orientation = Orientation.Horizontal };
        badges.Children.Add(new Border
        {
            Background = Paint.RarityBrush(_theme, row.Rarity),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(5, 1, 5, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = GameTabPresentation.RarityCapsuleLabel(row.Rarity, lang),
                FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
            },
        });
        if (row.IsRaising)
        {
            badges.Children.Add(new Border
            {
                Background = Token("AccentSoftBrush"),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = DashboardText.RaisingLabel(lang).ToUpperInvariant(),
                    FontSize = 8, FontWeight = FontWeights.Bold,
                    Foreground = Token("AccentBrush"),
                },
            });
        }
        else if (row.IsReleased)
        {
            badges.Children.Add(new Border
            {
                Background = Paint.HexBrush("#148A8A8A"),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = DashboardText.DexReleasedBadge(lang).ToUpperInvariant(),
                    FontSize = 8, FontWeight = FontWeights.Bold,
                    Foreground = Token("TextSecondaryBrush"),
                },
            });
        }
        if (row.IsShiny)
        {
            badges.Children.Add(new TextBlock
            {
                Text = "✨", FontSize = 10, Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "✨ " + DashboardText.ShinyLabel(lang),
            });
        }
        header.Children.Add(badges);
        card.Children.Add(header);

        var chain = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        for (var i = 0; i < row.Chain.Count; i++)
        {
            if (i > 0)
                chain.Children.Add(new TextBlock
                {
                    Text = "→",
                    FontSize = 10,
                    Foreground = Token("TextTertiaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 12),
                });
            chain.Children.Add(CreateChainNode(row.Chain[i], row));
        }
        card.Children.Add(chain);

        var ago = GameTabPresentation.CaughtAgo(row.CaughtAt, DateTimeOffset.Now, lang);
        if (ago.Length > 0)
            card.Children.Add(new TextBlock
            {
                Text = ago,
                FontSize = 9,
                Foreground = Token("TextTertiaryBrush"),
                Margin = new Thickness(0, 3, 0, 0),
            });

        return new Border
        {
            Background = Paint.HexBrush("#0F000000"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 8),
            Child = card,
        };
    }

    private StackPanel CreateChainNode(CompanionChainNode node, CompanionCatchRow row)
    {
        var cell = new StackPanel { Width = 48 };
        var slot = new Grid { Width = 40, Height = 40, HorizontalAlignment = HorizontalAlignment.Center };
        var image = new Image { Width = 40, Height = 40, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        var placeholder = new TextBlock
        {
            Text = "❔",
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        slot.Children.Add(image);
        slot.Children.Add(placeholder);
        cell.Children.Add(slot);
        cell.Children.Add(new TextBlock
        {
            Text = node.Name,
            FontSize = 9,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0),
        });
        var form = node.SpeciesID == UnownForms.SpeciesID ? row.UnownForm : null;
        new SpriteSlot(image, placeholder).Update(_sprites, node.SpeciesID, false, row.IsShiny, form, "❔");
        return cell;
    }

    private Brush Token(string key) => Paint.Token(_theme, key);
}
