using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    private readonly CompanionEngine _engine;
    private readonly SpriteStore _sprites;
    private readonly SpriteSlot _companionSprite;
    private readonly CompanionCelebrationPlayer _celebrations;
    private readonly ShopFlow _flow;
    private readonly UsageHomeRenderer _usageHome;
    private readonly ShopCards _shopCards;
    private CompanionGameView? _lastView;
    private Rarity? _dexRarityFilter;
    private Rarity? _catchRarityFilter;
    private bool _dexShowLog;
    private IReadOnlyList<CompanionDexRow> _visibleDexRows = [];
    private SpriteSlot? _dexEmptySprite;

    public DashboardWindow(CompanionEngine engine, SpriteStore sprites)
    {
        InitializeComponent();
        _engine = engine;
        _sprites = sprites;
        _companionSprite = new SpriteSlot(CompanionSprite, CompanionSpritePlaceholder);
        CelebrationShiny.Children.Add(CelebrationGlyphs.Sparkle(24));
        CelebrationDitto.Children.Add(CelebrationGlyphs.TheaterMasks());
        CelebrationGlyphs.FillMintCluster(CelebrationMint);
        _celebrations = new CompanionCelebrationPlayer(
            CompanionTile, CompanionScale, CompanionRotation, CelebrationFlash,
            CelebrationShiny, CelebrationShinyScale,
            CelebrationDitto, CelebrationDittoScale,
            CelebrationCandy, CelebrationCandyText, CelebrationCandySlide,
            CelebrationMint, CelebrationMintScale);
        _flow = new ShopFlow(engine);
        _usageHome = new UsageHomeRenderer(
            this, TodayCaptionLabel, TodayBigNumber, TodayGroupedLabel, TodayCostLabel,
            WeekMonthRow, ProviderChips, ProviderDetail, ProvidersUnavailableText,
            ProvidersSection, TrendSection, TrendCaptionLabel, TrendPeakLabel,
            TrendPeakValue, TrendReadout, TrendModelsHost, TrendBarsHost, TrendTicksHost,
            TrendAxisHost);
        _shopCards = new ShopCards(this, sprites, engine, _flow,
            () => UpdateGame(_engine.View()), BagCards, ShopCardsHost, SpendableAmount);
        LocalizeStaticText(engine.State.Language);
    }

    public void Relocalize(AppLanguage lang)
    {
        LocalizeStaticText(lang);
    }

    public void SelectDexTab()
    {
        MainTabs.SelectedIndex = 2;
    }

    private void LocalizeStaticText(AppLanguage lang)
    {
        var usageTab = (TabItem) MainTabs.Items[0];
        usageTab.Header = "_" + DashboardText.UsageTab(lang);
        var gameTab = (TabItem) MainTabs.Items[1];
        gameTab.Header = "_" + DashboardText.GameTab(lang);
        var dexTab = (TabItem) MainTabs.Items[2];
        dexTab.Header = "_" + DashboardText.DexTitle(lang);
        var shopTab = (TabItem) MainTabs.Items[3];
        shopTab.Header = "_" + DashboardText.ShopTitle(lang);
        DexModeDexRadio.Content = DashboardText.DexTitle(lang);
        DexModeLogRadio.Content = DashboardText.CatchLogTitle(lang);
        EventsHeader.Text = DashboardText.CompanionEventsLabel(lang);
        ShopHeader.Text = DashboardText.BagTitle(lang);
        SpendableLabel.Text = DashboardText.SpendableTokens(lang);
        ShopHintText.Text = DashboardText.ShopHint(lang);
        ProvidersHeader.Text = DashboardText.ProvidersTitle(lang);
        ExportButton.Content = DashboardText.ExportSave(lang);
        ImportButton.Content = DashboardText.ImportSave(lang);
        RefreshButton.Content = "_" + DashboardText.RefreshButton(lang);
        SettingsButton.Content = "_" + DashboardText.SettingsTitle(lang) + "…";
        UpdateSkipButton.Content = "_" + DashboardText.SkipThisVersion(lang);
        UpdateInstallButton.Content = "_" + DashboardText.UpdateButton(lang);
        RefreshUpdateBanner();
    }

    public void RefreshUpdateBanner()
    {
        App? app = System.Windows.Application.Current as App;
        var release = app?.UpdateChecker.Available;
        if (release is null || app is null || !app.UpdateNotificationsEnabled)
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
            return;
        }
        UpdateBannerText.Text = DashboardText.UpdateAvailable(
            _engine.State.Language, release.Version, app.UpdateChecker.CurrentVersion);
        UpdateBanner.Visibility = Visibility.Visible;
    }

    private void OnUpdateSkipClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.SkipCurrentUpdate();
    }

    private void OnUpdateInstallClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.OpenReleasePage();
    }

    public void Update(UsageDisplayState state)
    {
        var lang = _engine.State.Language;
        _usageHome.Render(state, lang);
        RefreshedText.Text = DashboardText.RefreshedAt(lang,
            state.AsOfUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private Brush Token(string key) =>
        Paint.Token(this, key);

    public void ShowRefreshing()
    {
        RefreshedText.Text = DashboardText.Refreshing(_engine.State.Language);
    }

    public void UpdateGame(CompanionGameView view)
    {
        _celebrations.Play(_engine.DrainCelebrations());
        _celebrations.SetEggImminent(!view.HasActive && view.EggProgress >= 0.9);
        var lang = view.Language;
        var shiny = view.HasActive && view.IsShiny ? " ✨" : "";
        CompanionName.Text = (view.HasActive ? view.ActiveName : DashboardText.TokenEgg(lang)) + shiny;
        CompanionName.ToolTip = view.HasActive && view.IsShiny
            ? "✨ " + DashboardText.ShinyLabel(lang)
            : null;

        if (view.Rarity is { } rarityValue)
        {
            RarityCapsule.Visibility = Visibility.Visible;
            RarityCapsule.Background = RarityBrush(rarityValue);
            RarityCapsuleText.Text = DashboardText.RarityLabel(lang, rarityValue).ToUpperInvariant();
        }
        else
        {
            RarityCapsule.Visibility = Visibility.Collapsed;
        }

        if (view.HasActive)
        {
            var stageCaption = view.StageIndex + 1 >= view.TotalForms && view.TotalForms > 0
                ? DashboardText.FinalForm(lang)
                : DashboardText.StageLabel(lang, view.StageIndex + 1, view.TotalForms);
            var nature = RaisingNature(view);
            CompanionDetail.Text = nature.Length > 0 ? $"{stageCaption} · {nature}" : stageCaption;
            if (view.HasGrowthBoost)
            {
                StatusCapsule.Visibility = Visibility.Visible;
                StatusCapsule.Background = HexBrush("#26F7630C");
                StatusCapsuleText.Text = DashboardText.GrowthBoost(
                    lang, (int) Math.Round((double) PokemonBalance.RepeatGrowthMultiplier));
                StatusCapsuleText.Foreground = Token("RarityLegendaryBrush");
            }
            else
            {
                StatusCapsule.Visibility = Visibility.Collapsed;
            }

            var remaining = Math.Max(0, view.StageThreshold - view.StageUsed);
            var isFinal = view.StageIndex + 1 >= view.TotalForms && view.TotalForms > 0;
            ProgressLabel.Text = isFinal
                ? DashboardText.ToGraduation(lang, TokenFormatter.Grouped(remaining))
                : DashboardText.ToNextEvolution(lang, TokenFormatter.Grouped(remaining));
            ProgressBar.Value = view.StageProgress;
            RenderEvolutionLine(view);
            _companionSprite.Update(_sprites, view.ActiveSpeciesID, true, view.IsShiny,
                view.ActiveUnownForm, "❔");
        }
        else
        {
            var imminent = view.EggProgress >= 0.9;
            CompanionDetail.Text = imminent
                ? DashboardText.EggImminent(lang)
                : DashboardText.EggIncubating(lang);
            CompanionDetail.Foreground = imminent
                ? Token("RarityLegendaryBrush")
                : Token("TextSecondaryBrush");
            if (view.EggGuarantee is { } guarantee)
            {
                StatusCapsule.Visibility = Visibility.Visible;
                StatusCapsule.Background = RarityBrush(guarantee);
                StatusCapsuleText.Text = DashboardText.EggGuaranteeHint(lang, guarantee);
                StatusCapsuleText.Foreground = Brushes.White;
            }
            else
            {
                StatusCapsule.Visibility = Visibility.Collapsed;
            }

            var toHatch = Math.Max(0, view.EggThreshold - view.EggUsed);
            ProgressLabel.Text = DashboardText.EggToHatch(lang, TokenFormatter.Grouped(toHatch));
            ProgressBar.Value = view.EggProgress;
            RenderEvolutionLine(view);
            _companionSprite.UpdateEgg(_sprites, "🥚");
        }
        if (view.HasActive)
            CompanionDetail.Foreground = Token("TextSecondaryBrush");
        UpdateCombatText(view);
        CompanionStatusText.Text = DashboardText.StatusLine(lang, view.Status, view.StatusEvolvedName);

        RenderDexTab(view, lang);
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        UpdateShop(view);
        var feedback = _flow.Feedback is { } feedbackOf ? feedbackOf(lang) : "";
        GameFooter.Text = feedback.Length > 0
            ? $"{feedback} · {DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}"
            : $"{DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}";
    }

    private string RaisingNature(CompanionGameView view)
    {
        if (!view.HasActive || _engine.Detail(view.ActiveSpeciesID) is not { } detail) return "";
        var raising = detail.Individuals.FirstOrDefault(individual => individual.IsRaising);
        return raising is not null && raising.Nature.Length > 0 ? raising.Nature : "";
    }

    private void RenderDexTab(CompanionGameView view, AppLanguage lang)
    {
        DexModeToggle.Visibility = view.DexRows.Count == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        CatchLogPanel.Visibility = Visibility.Collapsed;
        DexList.Items.Clear();
        CatchList.Items.Clear();
        if (view.DexRows.Count == 0)
        {
            DexHeader.Text = "";
            DexList.Visibility = Visibility.Collapsed;
            DexRarityFilter.Visibility = Visibility.Collapsed;
            DexEmpty.Visibility = Visibility.Visible;
            DexEmptyTitle.Text = DashboardText.DexEmptyTitle(lang);
            DexEmptyHint.Text = DashboardText.DexEmptyHint(lang);
            if (_dexEmptySprite is null)
                _dexEmptySprite = new SpriteSlot(DexEmptySprite, DexEmptyPlaceholder);
            _dexEmptySprite.Update(_sprites, 25, true, false, null, "❔");
            return;
        }
        DexEmpty.Visibility = Visibility.Collapsed;
        if (_dexShowLog)
        {
            DexHeader.Text = "";
            DexList.Visibility = Visibility.Collapsed;
            DexRarityFilter.Visibility = Visibility.Collapsed;
            CatchLogPanel.Visibility = Visibility.Visible;
            CatchHeader.Text = $"{DashboardText.CatchLogTitle(lang)} · " +
                               DashboardText.DexTotalCount(lang, view.CatchRows.Count);
            RenderCatchFilter(view, lang);
            foreach (var row in CompanionPresentation.VisibleCatchRows(view.CatchRows, _catchRarityFilter))
                CatchList.Items.Add(CreateCatchCard(row, lang));
            return;
        }
        DexHeader.Text = $"{DashboardText.DexTitle(lang)}: {DashboardText.DexSpeciesCount(lang, view.DexCount)} · " +
                         $"{DashboardText.WalletLabel(lang)} {TokenFormatter.Grouped(view.AvailableTokens)} · " +
                         DashboardText.DetailHint(lang);
        RenderDexFilter(view, lang);
        DexList.Visibility = Visibility.Visible;
        var showFilter = DexRarityFilter.Children.Count > 0;
        DexRarityFilter.Visibility = showFilter ? Visibility.Visible : Visibility.Collapsed;
        _visibleDexRows = CompanionPresentation.VisibleDexRows(view.DexRows, _dexRarityFilter);
        foreach (var row in _visibleDexRows)
            DexList.Items.Add(CreateDexTile(row, lang,
                row.SpeciesID == UnownForms.SpeciesID ? view.UnownForms.Count : 0,
                row.SpeciesID == view.RepresentativeSpeciesID));
    }

    private void OnDexModeChanged(object sender, RoutedEventArgs e)
    {
        _dexShowLog = ReferenceEquals(sender, DexModeLogRadio);
        if (_lastView is { } view)
            UpdateGame(view);
    }

    private void RenderDexFilter(CompanionGameView view, AppLanguage lang)
    {
        RenderRarityTally(DexRarityFilter,
            rarity => view.DexRows.Count(row => row.Rarity == rarity),
            _dexRarityFilter, lang,
            rarity => _dexRarityFilter = _dexRarityFilter == rarity ? null : rarity);
    }

    private void RenderCatchFilter(CompanionGameView view, AppLanguage lang)
    {
        RenderRarityTally(CatchRarityFilter,
            rarity => view.CatchRows.Count(row => row.Rarity == rarity),
            _catchRarityFilter, lang,
            rarity => _catchRarityFilter = _catchRarityFilter == rarity ? null : rarity);
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
        var brush = RarityBrush(rarity);
        var capsule = new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 5, 0),
            Background = selected ? brush : HexBrush("#14" + CompanionPresentation.RarityHex(rarity)),
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
            UpdateGame(_engine.View());
        };
        return capsule;
    }

    private static SolidColorBrush HexBrush(string hex) => Paint.HexBrush(hex);

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        _shopCards.RenderBag(view);
        _shopCards.RenderShop(view);
    }

    private Brush RarityBrush(Rarity rarity) => Paint.RarityBrush(this, rarity);

    private void RenderEvolutionLine(CompanionGameView view)
    {
        EvolutionLine.Children.Clear();
        var visible = view.HasActive && view.EvoLine.Count > 0;
        EvolutionScroll.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) return;
        for (var i = 0; i < view.EvoLine.Count; i++)
        {
            if (i > 0) EvolutionLine.Children.Add(CreateEvolutionArrow());
            EvolutionLine.Children.Add(CreateEvolutionCell(view.EvoLine[i], view));
        }
    }

    private TextBlock CreateEvolutionArrow() => new()
    {
        Text = "→",
        FontSize = 11,
        Foreground = Token("TextTertiaryBrush"),
        Width = 14,
        Height = 40,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private StackPanel CreateEvolutionCell(EvoLineItem item, CompanionGameView view)
    {
        var cell = new StackPanel { Width = 40 };
        var slot = new Grid { Width = 40, Height = 40 };
        if (item.Content.Kind == EvoLineItemContentKind.Mystery)
        {
            slot.Children.Add(new TextBlock
            {
                Text = "?",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Token("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = DashboardText.UnknownNextEvolution(view.Language),
            });
        }
        else
        {
            var image = new Image { Width = 40, Height = 40, Stretch = Stretch.Uniform };
            var placeholder = new TextBlock
            {
                Text = "❔",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slot.Children.Add(placeholder);
            slot.Children.Add(image);
            new SpriteSlot(image, placeholder).Update(_sprites, item.Content.SpeciesID,
                false, view.IsShiny, view.ActiveUnownForm, "❔");
        }
        cell.Children.Add(slot);
        cell.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 4,
            Height = 4,
            Fill = Token("AccentBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
            Visibility = item.State == EvoLineItemState.Current ? Visibility.Visible : Visibility.Hidden,
        });
        if (item.State == EvoLineItemState.Future)
            cell.Opacity = 0.32;
        return cell;
    }

    private void UpdateCombatText(CompanionGameView view)
    {
        var lang = view.Language;
        if (!view.HasActive || _engine.Detail(view.ActiveSpeciesID) is not { } detail)
        {
            CombatText.Text = "";
            return;
        }
        var raising = detail.Individuals.FirstOrDefault(individual => individual.IsRaising);
        var parts = new List<string>();
        if (raising is not null)
        {
            parts.Add($"Lv. {raising.Level}");
            if (raising.Gender.Length > 0) parts.Add(raising.Gender);
            if (raising.Nature.Length > 0) parts.Add(raising.Nature);
            if (raising.Ability.Length > 0)
                parts.Add(raising.Ability + (raising.AbilityIsHidden
                    ? $" ({DashboardText.HiddenMark(lang)})" : ""));
        }
        if (detail.Types.Count > 0) parts.Add(string.Join("/", detail.Types));
        parts.Add($"{DashboardText.BaseTotalLabel(lang)} {detail.BaseStatTotal}");
        CombatText.Text = string.Join(" · ", parts);
    }

    private UIElement CreateDexTile(CompanionDexRow row, AppLanguage lang, int unownCollected,
        bool isRepresentative)
    {
        var star = row.IsShiny ? " ✨" : "";
        var raising = row.IsRaising ? $"  ← {DashboardText.RaisingLabel(lang)}" : "";
        var unownForms = unownCollected > 0
            ? $" · {DashboardText.UnownFormsCollected(lang, unownCollected)}" : "";
        var representative = isRepresentative ? $" · {DashboardText.RepresentativeBadge(lang)}" : "";
        var tile = new Grid
        {
            Width = 76,
            Margin = new Thickness(1),
            ToolTip = $"#{row.SpeciesID} {row.Name}{star} · {DashboardText.RarityLabel(lang, row.Rarity)}{raising}{unownForms}{representative}",
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

        var nameSuffix = unownCollected > 0 ? $" {unownCollected}/{UnownForms.All.Length}" : "";
        var name = new TextBlock
        {
            Text = (row.IsRaising ? "← " : "") + row.Name + nameSuffix,
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
    /// Catch log card — port of the macOS DexEntryRow: rarity capsule +
    /// raising/released badge + ✨ header with the nature on the right, the
    /// reached evolution chain as sprites with names, caught-at relative line.
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
            Background = RarityBrush(row.Rarity),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(5, 1, 5, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = DashboardText.RarityLabel(lang, row.Rarity).ToUpperInvariant(),
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
                Background = HexBrush("#148A8A8A"),
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

        var ago = CaughtAgoText(row.CaughtAt, lang);
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
            Background = HexBrush("#0F000000"),
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

    private static string CaughtAgoText(DateTimeOffset? caughtAt, AppLanguage lang)
    {
        if (caughtAt is not { } at) return "";
        var now = DateTimeOffset.Now;
        return DashboardText.CaughtAgo(lang, RelativeTimes.Bucket(at, now),
            RelativeTimes.BucketValue(at, now));
    }

    private void OnDexDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastView is not { } view) return;
        var index = DexList.SelectedIndex;
        if (index < 0 || index >= _visibleDexRows.Count) return;
        var row = _visibleDexRows[index];
        try
        {
            if (_engine.Detail(row.SpeciesID) is { } detail)
            {
                var window = new SpeciesDetailWindow(detail, _sprites, _engine) { Owner = this };
                window.Show();
            }
            else
            {
                var speciesID = row.SpeciesID;
                _flow.SetFeedback(
                    language => DashboardText.NoCombatDetails(language, speciesID));
                UpdateGame(_engine.View());
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"species detail window failed: {ex.Message}");
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            await app.RefreshFromUiAsync();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.ShowSettings();
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = _engine.SuggestedExportFileName(),
            Filter = "PokeTokenBar save (*.json)|*.json",
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllBytes(dialog.FileName, _engine.ExportSave());
        }
        catch (Exception ex)
        {
            AppLog.Write($"save export failed: {ex.Message}");
            MessageBox.Show(this, $"Export failed: {ex.Message}", "PokeTokenBar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        var dialog = OpenFileDialog();
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var data = File.ReadAllBytes(dialog.FileName);
            var current = _engine.View();
            var confirm = MessageBox.Show(this,
                $"Import save from '{Path.GetFileName(dialog.FileName)}'?\n" +
                $"Current progress (dex {current.DexCount}, lifetime {current.LifetimeTokens}) " +
                "will be backed up and replaced.",
                "PokeTokenBar", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK) return;
            _engine.ImportSave(data);
            _flow.Reset();
            UpdateGame(_engine.View());
        }
        catch (Exception ex)
        {
            AppLog.Write($"save import failed: {ex.Message}");
            MessageBox.Show(this, $"Import failed: {ex.Message}", "PokeTokenBar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static OpenFileDialog OpenFileDialog() => new()
    {
        Filter = "PokeTokenBar save (*.json)|*.json|All files (*.*)|*.*",
    };
}
