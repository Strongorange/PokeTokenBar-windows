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
    private CompanionGameView? _lastView;
    private UsageDisplayState? _lastUsageState;
    private CompanionShopRow? _confirmingShopRow;
    private Func<AppLanguage, string>? _feedback;
    private Rarity? _dexRarityFilter;
    private string? _selectedProviderId;
    private int _candyCount = 1;
    private ItemKind? _confirmingBagItem;
    private SpriteSlot? _dexEmptySprite;

    public DashboardWindow(CompanionEngine engine, SpriteStore sprites)
    {
        InitializeComponent();
        _engine = engine;
        _sprites = sprites;
        _companionSprite = new SpriteSlot(CompanionSprite, CompanionSpritePlaceholder);
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
        _lastUsageState = state;
        var lang = _engine.State.Language;
        RenderTodayCard(state, lang);
        RenderProviders(state, lang);
        RefreshedText.Text = DashboardText.RefreshedAt(lang,
            state.AsOfUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        UpdateTrend(state);
    }

    private void RenderTodayCard(UsageDisplayState state, AppLanguage lang)
    {
        TodayCaptionLabel.Text = DashboardText.TodayTokensHeader(lang);
        TodayBigNumber.Text = TokenFormatter.Compact(state.TodayTokens);
        TodayGroupedLabel.Text = TokenFormatter.Grouped(state.TodayTokens);
        var todayCost = new UsageCost(state.TodayCost, state.TodayCostCoverage);
        TodayCostLabel.Text = todayCost.Coverage.HasKnown ? todayCost.Text("", compact: true) : "";

        WeekMonthRow.Children.Clear();
        if (state.WeekTokens > 0 || state.MonthTokens > 0)
        {
            WeekMonthRow.Children.Add(CreatePeriodLabel(
                DashboardText.ThisWeekLabel(lang), state.WeekTokens,
                new UsageCost(state.WeekCost, state.WeekCostCoverage)));
            var monthLabel = CreatePeriodLabel(
                DashboardText.ThisMonthLabel(lang), state.MonthTokens,
                new UsageCost(state.MonthCost, state.MonthCostCoverage));
            monthLabel.Margin = new Thickness(14, 0, 0, 0);
            WeekMonthRow.Children.Add(monthLabel);
        }
    }

    private TextBlock CreatePeriodLabel(string name, long tokens, UsageCost cost)
    {
        var text = $"{name} {TokenFormatter.Compact(tokens)}";
        if (cost.Coverage.HasKnown) text += " " + cost.Text("", compact: true);
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = Token("TextSecondaryBrush"),
        };
    }

    private void RenderProviders(UsageDisplayState state, AppLanguage lang)
    {
        var available = state.Providers.Where(provider => provider.Available).ToList();
        ProviderChips.Children.Clear();
        ProviderDetail.Children.Clear();
        ProvidersUnavailableText.Text = "";

        var selectable = available.Where(provider => provider.TodayTokens > 0
            || provider.MonthTokens > 0 || provider.WeekTokens > 0).ToList();
        if (_selectedProviderId is null || selectable.All(provider => provider.ProviderId != _selectedProviderId))
            _selectedProviderId = selectable.FirstOrDefault()?.ProviderId;
        var selected = selectable.FirstOrDefault(provider => provider.ProviderId == _selectedProviderId);

        if (selectable.Count > 1)
        {
            foreach (var provider in selectable)
            {
                var isSelected = provider.ProviderId == _selectedProviderId;
                var capsule = new Border
                {
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(10, 2, 10, 2),
                    Margin = new Thickness(0, 0, 6, 0),
                    Background = isSelected
                        ? Token("AccentSoftBrush")
                        : HexBrush("#14000000"),
                    BorderBrush = isSelected ? Token("AccentBrush") : null,
                    BorderThickness = new Thickness(isSelected ? 1.5 : 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                };
                var picked = provider;
                capsule.MouseLeftButtonUp += (_, _) =>
                {
                    _selectedProviderId = picked.ProviderId;
                    if (_lastUsageState is { } usage) Update(usage);
                };
                capsule.Child = new TextBlock
                {
                    Text = provider.DisplayName,
                    FontSize = 10,
                    FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isSelected ? Token("AccentBrush") : Token("TextSecondaryBrush"),
                };
                ProviderChips.Children.Add(capsule);
            }
        }

        if (selected is not null)
            ProviderDetail.Children.Add(CreateProviderDetail(selected, lang));

        var unavailable = state.Providers.Where(provider => !provider.Available).ToList();
        if (unavailable.Count > 0)
            ProvidersUnavailableText.Text = string.Join(" · ", unavailable.Select(provider =>
                $"{provider.DisplayName} {DashboardText.ProviderNotFound(lang)}"));

        var empty = ProviderChips.Children.Count == 0 && ProviderDetail.Children.Count == 0
            && unavailable.Count == 0;
        ProvidersSection.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private StackPanel CreateProviderDetail(ProviderUsageSummary provider, AppLanguage lang)
    {
        var panel = new StackPanel();

        var header = new DockPanel();
        var totals = new StackPanel { Orientation = Orientation.Horizontal };
        var cost = new UsageCost(provider.TodayCost, provider.TodayCostCoverage);
        if (cost.Coverage.HasKnown)
            totals.Children.Add(new TextBlock
            {
                Text = cost.Text("", compact: true),
                FontSize = 10,
                Foreground = Token("TextSecondaryBrush"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
            });
        totals.Children.Add(new TextBlock
        {
            Text = TokenFormatter.Compact(provider.TodayTokens),
            FontSize = 13,
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Bottom,
        });
        DockPanel.SetDock(totals, Dock.Right);
        header.Children.Add(totals);
        header.Children.Add(new TextBlock
        {
            Text = provider.DisplayName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
        });
        panel.Children.Add(header);

        var breakdown = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };
        breakdown.Children.Add(CreateTokenTypeRow(lang,
            (DashboardText.TokenInputLabel(lang), provider.TodayInputTokens),
            (DashboardText.TokenOutputLabel(lang), provider.TodayOutputTokens)));
        breakdown.Children.Add(CreateTokenTypeRow(lang,
            (DashboardText.TokenCacheWriteLabel(lang), provider.TodayCacheWriteTokens),
            (DashboardText.TokenCacheReadLabel(lang), provider.TodayCacheReadTokens)));
        panel.Children.Add(breakdown);

        if (provider.TodayModels is { Count: > 1 })
        {
            var models = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };
            foreach (var pair in provider.TodayModels.OrderByDescending(model => model.Value))
                models.Children.Add(CreateModelRow(pair.Key, pair.Value));
            panel.Children.Add(models);
        }
        return panel;
    }

    private StackPanel CreateTokenTypeRow(AppLanguage lang,
        (string Label, long Value) left, (string Label, long Value) right)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0) };
        row.Children.Add(CreateTokenTypeLabel(left.Label, left.Value));
        var rightLabel = CreateTokenTypeLabel(right.Label, right.Value);
        rightLabel.Margin = new Thickness(14, 0, 0, 0);
        row.Children.Add(rightLabel);
        return row;
    }

    private TextBlock CreateTokenTypeLabel(string label, long value) => new()
    {
        Text = $"{label} {TokenFormatter.Compact(value)}",
        FontSize = 10,
        Foreground = Token("TextSecondaryBrush"),
    };

    private DockPanel CreateModelRow(string model, long tokens)
    {
        var row = new DockPanel { Margin = new Thickness(0, 1, 0, 0) };
        var value = new TextBlock
        {
            Text = TokenFormatter.Compact(tokens),
            FontSize = 10,
            Foreground = Token("TextTertiaryBrush"),
        };
        DockPanel.SetDock(value, Dock.Right);
        row.Children.Add(value);
        row.Children.Add(new TextBlock
        {
            Text = model.Contains('/') ? model[(model.LastIndexOf('/') + 1)..] : model,
            FontSize = 10,
            Foreground = Token("TextSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        return row;
    }

    private Brush Token(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Gray;

    private void UpdateTrend(UsageDisplayState state)
    {
        var lang = _engine.State.Language;
        var series = state.MonthDaily;
        long peak = 0;
        if (series is not null)
            foreach (var day in series)
                if (day.TotalTokens > peak) peak = day.TotalTokens;
        TrendSection.Visibility = peak > 0 ? Visibility.Visible : Visibility.Collapsed;
        TrendModelsHost.Children.Clear();
        TrendBarsHost.ColumnDefinitions.Clear();
        TrendBarsHost.Children.Clear();
        TrendTicksHost.ColumnDefinitions.Clear();
        TrendTicksHost.Children.Clear();
        TrendAxisHost.ColumnDefinitions.Clear();
        TrendAxisHost.Children.Clear();
        if (peak <= 0 || series is null) return;

        TrendCaptionLabel.Text = DashboardText.DailyTrend(lang);
        TrendPeakLabel.Text = DashboardText.PeakDay(lang);
        TrendPeakValue.Text = TokenFormatter.Compact(peak);

        var showsCost = false;
        foreach (var day in series)
            if (day.UsageCost.Coverage.HasKnown) { showsCost = true; break; }
        var todayKey = UsageAggregation.LocalDay(state.AsOfUtc);
        DailyUsage? today = null;
        foreach (var day in series)
            if (day.Date == todayKey) { today = day; break; }

        var index = 0;
        foreach (var day in series)
        {
            TrendBarsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TrendTicksHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TrendAxisHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var isToday = day.Date == todayKey;
            var bar = new Border
            {
                Height = DailyTrendMetrics.BarHeight(day.TotalTokens, peak),
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(1),
                Background = isToday ? Token("AccentBrush") : Token("TextTertiaryBrush"),
                Opacity = isToday ? 1 : day.TotalTokens == 0 ? 0.18 : 0.45,
            };
            Grid.SetColumn(bar, index);
            TrendBarsHost.Children.Add(bar);

            var hoveredDay = day;
            var hit = new Border { Background = Brushes.Transparent };
            Grid.SetColumn(hit, index);
            hit.MouseEnter += (_, _) => SetTrendReadout(hoveredDay, showsCost, lang);
            hit.MouseLeave += (_, _) => SetTrendReadout(today, showsCost, lang);
            TrendBarsHost.Children.Add(hit);

            if (DailyTrendMetrics.IsWeekend(day.Date))
            {
                var tick = new Border { Background = Token("TextTertiaryBrush"), Opacity = 0.5 };
                Grid.SetColumn(tick, index);
                TrendTicksHost.Children.Add(tick);
            }

            var label = new TextBlock
            {
                Text = DailyTrendMetrics.AxisLabel(day.Date, todayKey)?.ToString() ?? "",
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isToday ? Token("AccentBrush") : Token("TextSecondaryBrush"),
            };
            Grid.SetColumn(label, index);
            TrendAxisHost.Children.Add(label);
            index++;
        }
        SetTrendReadout(today, showsCost, lang);

        if (state.TodayModels is { Count: > 1 })
        {
            foreach (var pair in state.TodayModels.OrderByDescending(model => model.Value))
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 1) };
                var tokens = new TextBlock
                {
                    Text = TokenFormatter.Compact(pair.Value),
                    FontSize = 10,
                    Foreground = Token("TextTertiaryBrush"),
                };
                DockPanel.SetDock(tokens, Dock.Right);
                row.Children.Add(tokens);
                row.Children.Add(new TextBlock
                {
                    Text = pair.Key.Contains('/') ? pair.Key[(pair.Key.LastIndexOf('/') + 1)..] : pair.Key,
                    FontSize = 10,
                    Foreground = Token("TextSecondaryBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });
                TrendModelsHost.Children.Add(row);
            }
        }
    }

    private void SetTrendReadout(DailyUsage? day, bool showsCost, AppLanguage lang)
    {
        if (day is null)
        {
            TrendReadout.Text = "";
            return;
        }
        var text = $"{DailyTrendMetrics.DayStamp(day.Date, lang)} {TokenFormatter.Compact(day.TotalTokens)}";
        if (showsCost) text += " " + day.UsageCost.Text("", compact: true);
        TrendReadout.Text = text;
    }

    public void ShowRefreshing()
    {
        RefreshedText.Text = DashboardText.Refreshing(_engine.State.Language);
    }

    public void UpdateGame(CompanionGameView view)
    {
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

        DexHeader.Text = $"{DashboardText.DexTitle(lang)}: {DashboardText.DexSpeciesCount(lang, view.DexCount)} · " +
                         $"{DashboardText.WalletLabel(lang)} {TokenFormatter.Grouped(view.AvailableTokens)} · " +
                         DashboardText.DetailHint(lang);
        RenderDexFilter(view, lang);
        DexList.Items.Clear();
        if (view.DexRows.Count == 0)
        {
            DexList.Visibility = Visibility.Collapsed;
            DexRarityFilter.Visibility = Visibility.Collapsed;
            DexEmpty.Visibility = Visibility.Visible;
            DexEmptyTitle.Text = DashboardText.DexEmptyTitle(lang);
            DexEmptyHint.Text = DashboardText.DexEmptyHint(lang);
            if (_dexEmptySprite is null)
                _dexEmptySprite = new SpriteSlot(DexEmptySprite, DexEmptyPlaceholder);
            _dexEmptySprite.Update(_sprites, 25, true, false, null, "❔");
        }
        else
        {
            DexList.Visibility = Visibility.Visible;
            DexEmpty.Visibility = Visibility.Collapsed;
            var visibleRows = _dexRarityFilter is { } filter
                ? view.DexRows.Where(row => row.Rarity == filter).ToList()
                : view.DexRows;
            var showFilter = DexRarityFilter.Children.Count > 0;
            DexRarityFilter.Visibility = showFilter ? Visibility.Visible : Visibility.Collapsed;
            foreach (var row in visibleRows)
                DexList.Items.Add(CreateDexTile(row, lang,
                    row.SpeciesID == UnownForms.SpeciesID ? view.UnownForms.Count : 0,
                    row.SpeciesID == view.RepresentativeSpeciesID));
        }
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        UpdateShop(view);
        var feedback = _feedback is { } ? _feedback(lang) : "";
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

    private void RenderDexFilter(CompanionGameView view, AppLanguage lang)
    {
        DexRarityFilter.Children.Clear();
        if (view.DexRows.Count == 0) return;
        foreach (var rarity in new[] { Rarity.Legendary, Rarity.Rare, Rarity.Uncommon, Rarity.Common })
        {
            var count = view.DexRows.Count(row => row.Rarity == rarity);
            if (count == 0) continue;
            var selected = _dexRarityFilter == rarity;
            var brush = RarityBrush(rarity);
            var capsule = new Border
            {
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 0, 5, 0),
                Background = selected ? brush : HexBrush("#14" + ColorOf(rarity)),
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
            var picked = rarity;
            capsule.MouseLeftButtonUp += (_, _) =>
            {
                _dexRarityFilter = _dexRarityFilter == picked ? null : picked;
                UpdateGame(_engine.View());
            };
            DexRarityFilter.Children.Add(capsule);
        }
    }

    private static string ColorOf(Rarity rarity) => rarity switch
    {
        Rarity.Legendary => "F7630C",
        Rarity.Rare => "0078D4",
        Rarity.Uncommon => "107C10",
        _ => "8A8A8A",
    };

    private static SolidColorBrush HexBrush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        RenderBagCards(view);
        RenderShopCards(view);
    }

    private void RenderBagCards(CompanionGameView view)
    {
        var lang = view.Language;
        BagCards.Children.Clear();
        if (view.Bag.Count == 0)
        {
            var empty = new StackPanel { Orientation = Orientation.Horizontal };
            var host = new Grid { Width = 48, Height = 48, VerticalAlignment = VerticalAlignment.Center };
            var image = new Image { Width = 48, Height = 48, Stretch = Stretch.Uniform };
            var placeholder = new TextBlock
            {
                Text = "❔", FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            host.Children.Add(image);
            host.Children.Add(placeholder);
            new SpriteSlot(image, placeholder).Update(_sprites, 143, true, false, null, "❔");
            empty.Children.Add(host);
            empty.Children.Add(new TextBlock
            {
                Text = DashboardText.BagEmptyTitle(lang), FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = Token("TextSecondaryBrush"), Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            BagCards.Children.Add(empty);
            return;
        }
        foreach (var item in view.Bag)
            BagCards.Children.Add(CreateBagCard(item, view, lang));
    }

    private Border CreateBagCard(CompanionBagItem item, CompanionGameView view, AppLanguage lang)
    {
        var card = new StackPanel();

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new TextBlock
        {
            Text = item.Kind.FallbackEmoji(), FontSize = 15, VerticalAlignment = VerticalAlignment.Center,
        });
        header.Children.Add(new TextBlock
        {
            Text = item.Label, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
        });
        if (!item.Kind.IsPassive())
            header.Children.Add(new TextBlock
            {
                Text = "×" + item.Count, FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = Token("TextSecondaryBrush"), Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });

        if (item.Kind == ItemKind.RareCandy && item.CanUse)
        {
            var max = Math.Max(1, _engine.MaxRareCandyUseCount());
            _candyCount = Math.Clamp(_candyCount, 1, max);
            header.Children.Add(new Border
            {
                Width = 1, Background = Token("DividerBrush"), Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
            });
            var minus = new Button { Content = "–", MinWidth = 26, MinHeight = 26, Padding = new Thickness(0) };
            minus.Click += (_, _) => { _candyCount = Math.Max(1, _candyCount - 1); UpdateGame(_engine.View()); };
            header.Children.Add(minus);
            header.Children.Add(new TextBlock
            {
                Text = "×" + _candyCount, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 30, TextAlignment = TextAlignment.Center,
            });
            var plus = new Button { Content = "+", MinWidth = 26, MinHeight = 26, Padding = new Thickness(0) };
            plus.Click += (_, _) => { _candyCount = Math.Min(max, _candyCount + 1); UpdateGame(_engine.View()); };
            header.Children.Add(plus);
        }
        card.Children.Add(header);

        if (item.Kind == ItemKind.RareCandy && item.CanUse &&
            _engine.PlanRareCandyUse(_candyCount) is { } plan)
        {
            if (plan.Graduates)
                card.Children.Add(PreviewLine(DashboardText.CandyGraduatesHint(lang), secondary: true));
            if (plan.DiscardedXP > 0)
                card.Children.Add(PreviewLine(
                    DashboardText.CandyDiscardedXP(lang, TokenFormatter.Compact(plan.DiscardedXP)),
                    secondary: false));
            else if (plan.Evolves && !plan.Graduates)
                card.Children.Add(PreviewLine(
                    DashboardText.CandyCarryoverXP(lang, TokenFormatter.Compact(plan.CarryoverXP)),
                    secondary: true));
        }

        var controls = new DockPanel { Margin = new Thickness(0, 7, 0, 0) };
        if (item.Kind.IsPassive())
        {
            controls.Children.Add(new TextBlock
            {
                Text = "✓ " + DashboardText.ShinyCharmEffectHint(lang), FontSize = 11,
                FontWeight = FontWeights.SemiBold, Foreground = Token("RarityUncommonBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        else if (item.Kind == ItemKind.Mint && item.CanUse)
        {
            controls.Children.Add(new TextBlock
            {
                Text = DashboardText.MintEffectHint(lang), FontSize = 11,
                Foreground = Token("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center,
            });
            AddBagUseButton(controls, item, view, lang, DashboardText.UseItemLabel(lang));
        }
        else if (item.Kind == ItemKind.RareCandy && item.CanUse)
        {
            var hint = "+(" + TokenFormatter.Compact((long) _candyCount * RareCandies.Xp) + " XP)";
            controls.Children.Add(new TextBlock
            {
                Text = hint, FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            });
            AddBagUseButton(controls, item, view, lang,
                DashboardText.UseLabel(lang) + " ×" + _candyCount);
        }
        else
        {
            controls.Children.Add(new TextBlock
            {
                Text = view.IsEgg
                    ? DashboardText.UseAfterHatch(lang)
                    : DashboardText.UseNeedsPokemon(lang),
                FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        card.Children.Add(controls);

        return new Border
        {
            Background = HexBrush("#F8F8F8"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
            Child = card,
        };
    }

    private TextBlock PreviewLine(string text, bool secondary) => new()
    {
        Text = text, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0),
        Foreground = secondary ? Token("TextSecondaryBrush") : Token("RarityLegendaryBrush"),
    };

    private void AddBagUseButton(DockPanel controls, CompanionBagItem item, CompanionGameView view,
        AppLanguage lang, string buttonLabel)
    {
        if (_confirmingBagItem == item.Kind)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var use = new Button
            {
                Content = buttonLabel, MinHeight = 26,
                Style = (Style) FindResource("AccentButton"),
            };
            use.Click += (_, _) =>
            {
                _confirmingBagItem = null;
                UseItem(item.Kind);
            };
            var cancel = new Button
            {
                Content = DashboardText.CancelLabel(lang), MinWidth = 60, MinHeight = 26,
                Margin = new Thickness(6, 0, 0, 0),
            };
            cancel.Click += (_, _) =>
            {
                _confirmingBagItem = null;
                UpdateGame(_engine.View());
            };
            row.Children.Add(use);
            row.Children.Add(cancel);
            DockPanel.SetDock(row, Dock.Right);
            controls.Children.Add(row);
            controls.Children.Add(new TextBlock
            {
                Text = DashboardText.UseOnCurrent(lang, view.ActiveName), FontSize = 11,
                Foreground = Token("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }
        var button = new Button { Content = buttonLabel, MinWidth = 70, MinHeight = 26 };
        button.Click += (_, _) =>
        {
            _confirmingBagItem = item.Kind;
            UpdateGame(_engine.View());
        };
        DockPanel.SetDock(button, Dock.Right);
        controls.Children.Add(button);
    }

    private void UseItem(ItemKind kind)
    {
        if (kind == ItemKind.Mint)
        {
            var nature = _engine.UseMint();
            _feedback = nature is { } picked
                ? language => DashboardText.MintUsed(language, picked.ToString())
                : DashboardText.NoMint;
        }
        else
        {
            var result = _engine.UseRareCandy(_candyCount);
            _feedback = result switch
            {
                CandyUseResult.Graduated => DashboardText.CandyGraduated,
                CandyUseResult.Evolved => DashboardText.CandyEvolved,
                CandyUseResult.Progressed => DashboardText.CandyProgressed,
                _ => DashboardText.NoCandy,
            };
        }
        UpdateGame(_engine.View());
    }

    private void RenderShopCards(CompanionGameView view)
    {
        var lang = view.Language;
        SpendableAmount.Text = TokenFormatter.Compact(view.AvailableTokens);
        ShopCards.Children.Clear();
        foreach (var row in view.ShopRows)
            ShopCards.Children.Add(row.EggTier is { } tier
                ? CreateEggCard(row, tier, view, lang)
                : CreateItemCard(row, view, lang));
    }

    private Border CreateItemCard(CompanionShopRow row, CompanionGameView view, AppLanguage lang)
    {
        var kind = row.Item ?? ItemKind.RareCandy;
        var owned = (int) (view.Bag.FirstOrDefault(item => item.Kind == kind)?.Count ?? 0);
        var passiveOwned = kind.IsPassive() && owned > 0;

        var card = new StackPanel();
        card.Children.Add(CreateShopCardHeader(
            kind.FallbackEmoji(),
            row.Label,
            owned > 0 && !kind.IsPassive() ? DashboardText.OwnedCount(lang, owned) : "",
            DashboardText.ItemDescription(lang, kind),
            null, lang));

        if (passiveOwned)
        {
            var ownedRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            ownedRow.Children.Add(new TextBlock
            {
                Text = "✓ ", FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = Token("RarityUncommonBrush"), VerticalAlignment = VerticalAlignment.Center,
            });
            ownedRow.Children.Add(new TextBlock
            {
                Text = DashboardText.OwnedAlready(lang), FontSize = 11,
                FontWeight = FontWeights.SemiBold, Foreground = Token("RarityUncommonBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            });
            card.Children.Add(ownedRow);
        }
        else
        {
            var controls = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var price = new TextBlock
            {
                Text = $"{DashboardText.ShopPriceLabel(lang)} {TokenFormatter.Compact(row.Price)}",
                FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(price, Dock.Left);
            controls.Children.Add(price);

            if (row.CanBuy)
            {
                var confirm = new DockPanel();
                var buttons = new StackPanel { Orientation = Orientation.Horizontal };
                var buy = new Button
                {
                    Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                    Style = (Style) FindResource("AccentButton"),
                };
                buy.Click += (_, _) =>
                {
                    var bought = _engine.Buy(kind);
                    _feedback = language => bought
                        ? DashboardText.BoughtItem(language, row.Label)
                        : DashboardText.CannotBuyItem(language, row.Label);
                    UpdateGame(_engine.View());
                };
                var cancel = new Button
                {
                    Content = DashboardText.CancelLabel(lang), MinWidth = 70, MinHeight = 26,
                    Margin = new Thickness(6, 0, 0, 0),
                };
                cancel.Click += (_, _) =>
                {
                    _confirmingShopRow = null;
                    UpdateGame(_engine.View());
                };
                buttons.Children.Add(buy);
                buttons.Children.Add(cancel);
                DockPanel.SetDock(buttons, Dock.Right);
                confirm.Children.Add(buttons);
                confirm.Children.Add(new TextBlock
                {
                    Text = DashboardText.BuyConfirm(lang, row.Label),
                    FontSize = 11, Foreground = Token("TextSecondaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
                });
                controls.Children.Add(confirm);
            }
            else
            {
                var locked = new TextBlock
                {
                    Text = DashboardText.NotEnoughTokens(lang), FontSize = 11,
                    Foreground = Token("TextTertiaryBrush"),
                };
                DockPanel.SetDock(locked, Dock.Right);
                controls.Children.Add(locked);
            }
            card.Children.Add(controls);
        }

        return WrapShopCard(card);
    }

    private Border CreateEggCard(CompanionShopRow row, Rarity tier, CompanionGameView view,
        AppLanguage lang)
    {
        var card = new StackPanel();
        card.Children.Add(CreateShopCardHeader(
            "🥚", row.Label, "", DashboardText.EggDescription(lang, tier), tier, lang));

        if (!view.HasActive)
        {
            var lockedRow = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var buyDisabled = new Button
            {
                Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                IsEnabled = false,
            };
            DockPanel.SetDock(buyDisabled, Dock.Right);
            lockedRow.Children.Add(buyDisabled);
            lockedRow.Children.Add(new TextBlock
            {
                Text = DashboardText.EggShopLockedHint(lang), FontSize = 11,
                Foreground = Token("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            });
            card.Children.Add(lockedRow);
        }
        else
        {
            var controls = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var price = new TextBlock
            {
                Text = $"{DashboardText.ShopPriceLabel(lang)} {TokenFormatter.Compact(row.Price)}",
                FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(price, Dock.Left);
            controls.Children.Add(price);

            if (row.CanBuy)
            {
                var stage = _confirmingShopRow == row;
                var shinyStage = stage && view.IsShiny;
                if (stage)
                {
                    var confirm = new DockPanel();
                    var buttons = new StackPanel { Orientation = Orientation.Horizontal };
                    var buy = new Button
                    {
                        Content = shinyStage
                            ? DashboardText.FreshEggDiscardShiny(lang)
                            : DashboardText.BuyLabel(lang),
                        MinHeight = 26,
                        Style = (Style) FindResource("AccentButton"),
                    };
                    buy.Click += (_, _) => CommitEggPurchase(row);
                    var cancel = new Button
                    {
                        Content = DashboardText.CancelLabel(lang), MinWidth = 70, MinHeight = 26,
                        Margin = new Thickness(6, 0, 0, 0),
                    };
                    cancel.Click += (_, _) =>
                    {
                        _confirmingShopRow = null;
                        UpdateGame(_engine.View());
                    };
                    buttons.Children.Add(buy);
                    buttons.Children.Add(cancel);
                    DockPanel.SetDock(buttons, Dock.Right);
                    confirm.Children.Add(buttons);
                    confirm.Children.Add(new TextBlock
                    {
                        Text = shinyStage
                            ? DashboardText.FreshEggShinyWarning(lang)
                            : DashboardText.EggConfirm(lang, view.ActiveName, row.Label),
                        FontSize = 11,
                        FontWeight = shinyStage ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = shinyStage ? Token("WarningBrush") : Token("TextSecondaryBrush"),
                        VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
                    });
                    controls.Children.Add(confirm);
                }
                else
                {
                    var buy = new Button
                    {
                        Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                    };
                    buy.Click += (_, _) =>
                    {
                        _confirmingShopRow = row;
                        UpdateGame(_engine.View());
                    };
                    DockPanel.SetDock(buy, Dock.Right);
                    controls.Children.Add(buy);
                }
            }
            else
            {
                var locked = new TextBlock
                {
                    Text = DashboardText.NotEnoughTokens(lang), FontSize = 11,
                    Foreground = Token("TextTertiaryBrush"),
                };
                DockPanel.SetDock(locked, Dock.Right);
                controls.Children.Add(locked);
            }
            card.Children.Add(controls);
        }

        return WrapShopCard(card);
    }

    private void CommitEggPurchase(CompanionShopRow row)
    {
        _confirmingShopRow = null;
        var bought = _engine.BuyEgg(row.EggTier!.Value);
        _feedback = language => bought
            ? DashboardText.BoughtItem(language, row.Label)
            : DashboardText.CannotBuyItem(language, row.Label);
        UpdateGame(_engine.View());
    }

    private FrameworkElement CreateShopCardHeader(string icon, string title, string ownedSuffix,
        string description, Rarity? tier, AppLanguage lang)
    {
        var header = new DockPanel();

        var iconBox = new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(6),
            Background = Token("AccentSoftBrush"),
            Child = new TextBlock
            {
                Text = icon, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        DockPanel.SetDock(iconBox, Dock.Left);
        header.Children.Add(iconBox);

        var titleRow = new DockPanel();
        if (tier is { } rarityTier)
        {
            var capsule = new Border
            {
                Background = RarityBrush(rarityTier), CornerRadius = new CornerRadius(7),
                Padding = new Thickness(5, 1, 5, 1), VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = DashboardText.RarityLabel(lang, rarityTier).ToUpperInvariant(),
                    FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                },
            };
            DockPanel.SetDock(capsule, Dock.Right);
            titleRow.Children.Add(capsule);
        }
        var titleText = new TextBlock
        {
            Text = title + (ownedSuffix.Length > 0 ? "  " + ownedSuffix : ""),
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = Token("TextPrimaryBrush"), VerticalAlignment = VerticalAlignment.Center,
        };
        titleRow.Children.Add(titleText);

        var text = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(titleRow);
        text.Children.Add(new TextBlock
        {
            Text = description, FontSize = 11, Foreground = Token("TextSecondaryBrush"),
            Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap,
        });
        header.Children.Add(text);
        return header;
    }

    private Brush RarityBrush(Rarity rarity) => rarity switch
    {
        Rarity.Legendary => Token("RarityLegendaryBrush"),
        Rarity.Rare => Token("RarityRareBrush"),
        Rarity.Uncommon => Token("RarityUncommonBrush"),
        _ => Token("RarityCommonBrush"),
    };

    private Border WrapShopCard(StackPanel content) => new()
    {
        Background = Token("CardBgBrush"),
        BorderBrush = Token("CardBorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 10),
        Child = content,
    };

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

    private void OnDexDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastView is not { } view) return;
        var index = DexList.SelectedIndex;
        if (index < 0 || index >= view.DexRows.Count) return;
        var row = view.DexRows[index];
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
                _feedback = language => DashboardText.NoCombatDetails(language, speciesID);
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
