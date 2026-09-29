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
    private CompanionShopRow? _confirmingShopRow;
    private Func<AppLanguage, string>? _feedback;

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
        CombinedHeader.Text = DashboardText.CombinedTitle(lang);
        ExportButton.Content = DashboardText.ExportSave(lang);
        ImportButton.Content = DashboardText.ImportSave(lang);
        UseCandyButton.Content = DashboardText.UseItem(lang, DashboardText.ItemName(lang, ItemKind.RareCandy));
        UseMintButton.Content = DashboardText.UseItem(lang, DashboardText.ItemName(lang, ItemKind.Mint));
        UseAllButton.Content = DashboardText.UseAll(lang);
        ShopHeader.Text = DashboardText.ShopTitle(lang);
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
        ProvidersList.Items.Clear();
        foreach (var provider in state.Providers)
        {
            var availability = provider.Available ? "" : "  " + DashboardText.ProviderNotFound(lang);
            ProvidersList.Items.Add(
                $"{provider.DisplayName}: {DashboardText.TodayLabel(lang)} {TokenFormatter.Grouped(provider.TodayTokens)} · " +
                $"{DashboardText.MonthLabel(lang)} {TokenFormatter.Grouped(provider.MonthTokens)}{availability}");
        }
        CombinedText.Text =
            $"{DashboardText.TodayLabel(lang)} {TokenFormatter.Grouped(state.TodayTokens)} · " +
            $"{DashboardText.MonthLabel(lang)} {TokenFormatter.Grouped(state.MonthTokens)}";
        RefreshedText.Text = DashboardText.RefreshedAt(lang,
            state.AsOfUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        UpdateTrend(state);
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
        var boost = view.HasGrowthBoost
            ? "  (" + DashboardText.GrowthBoostMark(lang, PokemonBalance.RepeatGrowthMultiplier) + ")" : "";
        var rarity = view.Rarity is { } value ? DashboardText.RarityLabel(lang, value) : "";
        CompanionName.Text = (view.HasActive ? view.ActiveName : DashboardText.TokenEgg(lang)) + shiny;
        CompanionName.ToolTip = view.HasActive && view.IsShiny
            ? "✨ " + DashboardText.ShinyLabel(lang)
            : null;
        CompanionDetail.Text = view.HasActive
            ? $"{rarity}{boost}"
            : DashboardText.EggHint(lang);

        if (view.HasActive)
        {
            var stageCaption = view.StageIndex + 1 >= view.TotalForms && view.TotalForms > 0
                ? DashboardText.FinalForm(lang)
                : DashboardText.StageLabel(lang, view.StageIndex + 1, view.TotalForms);
            ProgressLabel.Text =
                $"{stageCaption} · " +
                $"{TokenFormatter.Grouped(view.StageUsed)} / {TokenFormatter.Grouped(view.StageThreshold)} " +
                DashboardText.TokensUnit(lang);
            ProgressBar.Value = view.StageProgress;
            RenderEvolutionLine(view);
            _companionSprite.Update(_sprites, view.ActiveSpeciesID, true, view.IsShiny,
                view.ActiveUnownForm, "❔");
        }
        else
        {
            ProgressLabel.Text =
                $"{DashboardText.TokenEgg(lang)} · {TokenFormatter.Grouped(view.EggUsed)} / " +
                $"{TokenFormatter.Grouped(view.EggThreshold)} {DashboardText.TokensUnit(lang)}";
            ProgressBar.Value = view.EggProgress;
            RenderEvolutionLine(view);
            _companionSprite.UpdateEgg(_sprites, "🥚");
        }
        UpdateCombatText(view);

        DexHeader.Text = $"{DashboardText.DexTitle(lang)}: {DashboardText.DexSpeciesCount(lang, view.DexCount)} · " +
                         $"{DashboardText.WalletLabel(lang)} {TokenFormatter.Grouped(view.AvailableTokens)} · " +
                         DashboardText.DetailHint(lang);
        DexList.Items.Clear();
        foreach (var row in view.DexRows)
            DexList.Items.Add(CreateDexTile(row, lang, row.SpeciesID == UnownForms.SpeciesID
                ? view.UnownForms.Count : 0));
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        UpdateShop(view);
        var feedback = _feedback is { } ? _feedback(lang) : "";
        GameFooter.Text = feedback.Length > 0
            ? $"{feedback} · {DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}"
            : $"{DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}";
    }

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        var lang = view.Language;
        BagText.Text = view.Bag.Count == 0
            ? DashboardText.BagEmpty(lang)
            : string.Join(" · ", view.Bag.Select(item =>
                $"{item.Label} ×{item.Count}"));
        RenderShopCards(view);
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

    private void OnUseCandyClick(object sender, RoutedEventArgs e)
    {
        var result = _engine.UseRareCandy(1);
        _feedback = result switch
        {
            CandyUseResult.Graduated => DashboardText.CandyGraduated,
            CandyUseResult.Evolved => DashboardText.CandyEvolved,
            CandyUseResult.Progressed => DashboardText.CandyProgressed,
            _ => DashboardText.NoCandy,
        };
        UpdateGame(_engine.View());
    }

    private void OnUseAllCandyClick(object sender, RoutedEventArgs e)
    {
        var count = _engine.MaxRareCandyUseCount();
        if (count <= 0)
        {
            _feedback = DashboardText.NoCandy;
        }
        else
        {
            _engine.UseRareCandy(count);
            _feedback = language => DashboardText.UsedCandies(language, count);
        }
        UpdateGame(_engine.View());
    }

    private void OnUseMintClick(object sender, RoutedEventArgs e)
    {
        var nature = _engine.UseMint();
        _feedback = nature is { } picked
            ? language => DashboardText.MintUsed(language, picked.ToString())
            : DashboardText.NoMint;
        UpdateGame(_engine.View());
    }

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

    private UIElement CreateDexTile(CompanionDexRow row, AppLanguage lang, int unownCollected)
    {
        var star = row.IsShiny ? " ✨" : "";
        var raising = row.IsRaising ? $"  ← {DashboardText.RaisingLabel(lang)}" : "";
        var unownForms = unownCollected > 0
            ? $" · {DashboardText.UnownFormsCollected(lang, unownCollected)}" : "";
        var tile = new Grid
        {
            Width = 76,
            Margin = new Thickness(1),
            ToolTip = $"#{row.SpeciesID} {row.Name}{star} · {DashboardText.RarityLabel(lang, row.Rarity)}{raising}{unownForms}",
        };
        for (var i = 0; i < 3; i++) tile.RowDefinitions.Add(new RowDefinition());

        var caption = new Grid();
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caption.Children.Add(new TextBlock
        {
            Text = "#" + row.SpeciesID,
            FontSize = 10,
            Foreground = Token("TextSecondaryBrush"),
        });
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
        return tile;
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
                var window = new SpeciesDetailWindow(detail, _sprites) { Owner = this };
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
