using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Bag + shop card renderer extracted from DashboardWindow code-behind (M25).
/// Visual builders only — the confirm-state machinery, candy stepper, and
/// feedback live in ShopFlow (unit-tested). macOS parity: item cards use the
/// same inline two-step confirm as eggs (idle price row → confirm row), and
/// egg cards escalate to the shiny-discard warning only on the second click.
/// </summary>
internal sealed class ShopCards
{
    private readonly FrameworkElement _theme;
    private readonly SpriteStore _sprites;
    private readonly CompanionEngine _engine;
    private readonly ShopFlow _flow;
    private readonly Action _requestRender;
    private readonly StackPanel _bagCards;
    private readonly StackPanel _shopCards;
    private readonly TextBlock _spendableAmount;

    public ShopCards(FrameworkElement theme, SpriteStore sprites, CompanionEngine engine,
        ShopFlow flow, Action requestRender,
        StackPanel bagCards, StackPanel shopCards, TextBlock spendableAmount)
    {
        _theme = theme;
        _sprites = sprites;
        _engine = engine;
        _flow = flow;
        _requestRender = requestRender;
        _bagCards = bagCards;
        _shopCards = shopCards;
        _spendableAmount = spendableAmount;
    }

    public void RenderBag(CompanionGameView view)
    {
        var lang = view.Language;
        _bagCards.Children.Clear();
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
            _bagCards.Children.Add(empty);
            return;
        }
        foreach (var item in view.Bag)
            _bagCards.Children.Add(CreateBagCard(item, view, lang));
    }

    private Border CreateBagCard(CompanionBagItem item, CompanionGameView view, AppLanguage lang)
    {
        var card = new StackPanel();

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(CreateItemIcon(item.Kind));
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
            _flow.ClampCandy();
            header.Children.Add(new Border
            {
                Width = 1, Background = Token("DividerBrush"), Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
            });
            var minus = new Button { Content = "–", MinWidth = 26, MinHeight = 26, Padding = new Thickness(0) };
            minus.Click += (_, _) => { _flow.StepCandy(-1); _requestRender(); };
            header.Children.Add(minus);
            header.Children.Add(new TextBlock
            {
                Text = "×" + _flow.CandyCount, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 30, TextAlignment = TextAlignment.Center,
            });
            var plus = new Button { Content = "+", MinWidth = 26, MinHeight = 26, Padding = new Thickness(0) };
            plus.Click += (_, _) => { _flow.StepCandy(1); _requestRender(); };
            header.Children.Add(plus);
        }
        card.Children.Add(header);

        if (item.Kind == ItemKind.RareCandy && item.CanUse)
            foreach (var line in ShopFlow.CandyPreviewLines(
                         _engine.PlanRareCandyUse(_flow.CandyCount), lang))
                card.Children.Add(PreviewLine(line.Text, line.Secondary));

        FrameworkElement controls;
        if (item.Kind.IsPassive())
        {
            controls = BagHintText("✓ " + DashboardText.ShinyCharmEffectHint(lang),
                Token("RarityUncommonBrush"), FontWeights.SemiBold);
        }
        else if (item.Kind == ItemKind.Mint && item.CanUse)
        {
            controls = UseRow(BagHintText(DashboardText.MintEffectHint(lang),
                Token("TextTertiaryBrush"), null), item, view, lang,
                DashboardText.UseItemLabel(lang));
        }
        else if (item.Kind == ItemKind.RareCandy && item.CanUse)
        {
            controls = UseRow(BagHintText(_flow.CandyXpHint(lang),
                Token("TextTertiaryBrush"), null), item, view, lang,
                DashboardText.UseLabel(lang) + " ×" + _flow.CandyCount);
        }
        else
        {
            controls = BagHintText(
                view.IsEgg ? DashboardText.UseAfterHatch(lang) : DashboardText.UseNeedsPokemon(lang),
                Token("TextTertiaryBrush"), null);
        }
        card.Children.Add(controls);

        return new Border
        {
            Background = Paint.HexBrush("#0F000000"),
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

    private TextBlock BagHintText(string text, Brush foreground, FontWeight? weight)
    {
        return new TextBlock
        {
            Text = text, FontSize = 11, Foreground = foreground,
            FontWeight = weight ?? FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 0),
        };
    }

    private FrameworkElement UseRow(TextBlock hint, CompanionBagItem item, CompanionGameView view,
        AppLanguage lang, string buttonLabel)
    {
        if (_flow.ConfirmingBagItem == item.Kind)
        {
            var use = new Button
            {
                Content = buttonLabel, MinHeight = 26,
                Style = (Style) _theme.FindResource("AccentButton"),
            };
            use.Click += (_, _) =>
            {
                _flow.CommitBagConfirm();
                _requestRender();
            };
            var cancel = new Button
            {
                Content = DashboardText.CancelLabel(lang), MinWidth = 60, MinHeight = 26,
                Margin = new Thickness(6, 0, 0, 0),
            };
            cancel.Click += (_, _) =>
            {
                _flow.CancelBagConfirm();
                _requestRender();
            };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(use);
            buttons.Children.Add(cancel);
            var message = new TextBlock
            {
                Text = DashboardText.UseOnCurrent(lang, view.ActiveName), FontSize = 11,
                Foreground = Token("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            var row = ShopActionRow.Create(message, buttons);
            row.Margin = new Thickness(0, 7, 0, 0);
            return row;
        }
        var button = new Button { Content = buttonLabel, MinWidth = 70, MinHeight = 26 };
        button.Click += (_, _) =>
        {
            _flow.BeginBagConfirm(item.Kind);
            _requestRender();
        };
        var idle = ShopActionRow.Create(hint, button);
        idle.Margin = new Thickness(0, 7, 0, 0);
        return idle;
    }

    public void RenderShop(CompanionGameView view)
    {
        var lang = view.Language;
        _spendableAmount.Text = TokenFormatter.Compact(view.AvailableTokens);
        _shopCards.Children.Clear();
        foreach (var row in view.ShopRows)
            _shopCards.Children.Add(row.EggTier is not null || row.Item is null
                ? CreateEggCard(row, row.EggTier, view, lang)
                : CreateItemCard(row, view, lang));
    }

    private Border CreateItemCard(CompanionShopRow row, CompanionGameView view, AppLanguage lang)
    {
        var kind = row.Item ?? ItemKind.RareCandy;
        var owned = (int) (view.Bag.FirstOrDefault(item => item.Kind == kind)?.Count ?? 0);
        var passiveOwned = kind.IsPassive() && owned > 0;

        var card = new StackPanel();
        card.Children.Add(CreateShopCardHeader(
            CreateItemIcon(kind, 30),
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
            if (row.CanBuy && _flow.ConfirmingShopItem == kind)
            {
                var buttons = new StackPanel { Orientation = Orientation.Horizontal };
                var buy = new Button
                {
                    Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                    Style = (Style) _theme.FindResource("AccentButton"),
                };
                buy.Click += (_, _) =>
                {
                    _flow.CommitItemBuy(row.Label);
                    _requestRender();
                };
                var cancel = new Button
                {
                    Content = DashboardText.CancelLabel(lang), MinWidth = 70, MinHeight = 26,
                    Margin = new Thickness(6, 0, 0, 0),
                };
                cancel.Click += (_, _) =>
                {
                    _flow.CancelItemConfirm();
                    _requestRender();
                };
                buttons.Children.Add(buy);
                buttons.Children.Add(cancel);
                var message = new TextBlock
                {
                    Text = DashboardText.BuyConfirm(lang, row.Label),
                    FontSize = 11, Foreground = Token("TextSecondaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
                };
                var confirm = ShopActionRow.Create(message, buttons);
                confirm.Margin = new Thickness(0, 8, 0, 0);
                card.Children.Add(confirm);
            }
            else
            {
                var price = new TextBlock
                {
                    Text = $"{DashboardText.ShopPriceLabel(lang)} {TokenFormatter.Compact(row.Price)}",
                    FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                    FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center,
                };
                FrameworkElement trailing;
                if (row.CanBuy)
                {
                    var buy = new Button
                    {
                        Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                    };
                    buy.Click += (_, _) =>
                    {
                        _flow.BeginItemConfirm(kind);
                        _requestRender();
                    };
                    trailing = buy;
                }
                else
                {
                    trailing = new TextBlock
                    {
                        Text = DashboardText.NotEnoughTokens(lang), FontSize = 11,
                        Foreground = Token("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                    };
                }
                var idle = ShopActionRow.Create(price, trailing);
                idle.Margin = new Thickness(0, 8, 0, 0);
                card.Children.Add(idle);
            }
        }

        return WrapShopCard(card);
    }

    private Border CreateEggCard(CompanionShopRow row, Rarity? tier, CompanionGameView view,
        AppLanguage lang)
    {
        var card = new StackPanel();
        card.Children.Add(CreateShopCardHeader(
            CreateEggIcon(),
            row.Label, "", DashboardText.EggDescription(lang, tier), tier, lang));

        if (!view.HasActive)
        {
            var buyDisabled = new Button
            {
                Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                IsEnabled = false,
            };
            var lockedHint = new TextBlock
            {
                Text = DashboardText.EggShopLockedHint(lang), FontSize = 11,
                Foreground = Token("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            var lockedRow = ShopActionRow.Create(lockedHint, buyDisabled);
            lockedRow.Margin = new Thickness(0, 8, 0, 0);
            card.Children.Add(lockedRow);
        }
        else
        {
            var price = new TextBlock
            {
                Text = $"{DashboardText.ShopPriceLabel(lang)} {TokenFormatter.Compact(row.Price)}",
                FontSize = 11, Foreground = Token("TextTertiaryBrush"),
                FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center,
            };

            FrameworkElement controls;
            if (!row.CanBuy)
            {
                controls = ShopActionRow.Create(price, new TextBlock
                {
                    Text = DashboardText.NotEnoughTokens(lang), FontSize = 11,
                    Foreground = Token("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center,
                });
            }
            else if (_flow.EggConfirm.Tier == tier && _flow.EggConfirm.Stage == EggConfirmStage.Confirm)
            {
                controls = EggConfirmRow(lang,
                    DashboardText.EggConfirm(lang, view.ActiveName, row.Label),
                    DashboardText.BuyLabel(lang), FontWeights.Normal, Token("TextSecondaryBrush"),
                    () => { _flow.AdvanceEggConfirm(row.Label, view.IsShiny); _requestRender(); },
                    () => { _flow.CancelEggConfirm(); _requestRender(); });
            }
            else if (_flow.EggConfirm.Tier == tier && _flow.EggConfirm.Stage == EggConfirmStage.ShinyWarning)
            {
                controls = EggConfirmRow(lang,
                    DashboardText.FreshEggShinyWarning(lang),
                    DashboardText.FreshEggDiscardShiny(lang), FontWeights.SemiBold, Token("WarningBrush"),
                    () => { _flow.AdvanceEggConfirm(row.Label, false); _requestRender(); },
                    () => { _flow.CancelEggConfirm(); _requestRender(); });
            }
            else
            {
                var buy = new Button
                {
                    Content = DashboardText.BuyLabel(lang), MinWidth = 70, MinHeight = 26,
                };
                buy.Click += (_, _) =>
                {
                    _flow.BeginEggConfirm(tier);
                    _requestRender();
                };
                controls = ShopActionRow.Create(price, buy);
            }
            controls.Margin = new Thickness(0, 8, 0, 0);
            card.Children.Add(controls);
        }

        return WrapShopCard(card);
    }

    private DockPanel EggConfirmRow(AppLanguage lang, string message,
        string buyLabel, FontWeight messageWeight, Brush messageBrush,
        Action buyAction, Action cancelAction)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var buy = new Button
        {
            Content = buyLabel, MinHeight = 26,
            Style = (Style) _theme.FindResource("AccentButton"),
        };
        buy.Click += (_, _) => buyAction();
        var cancel = new Button
        {
            Content = DashboardText.CancelLabel(lang), MinWidth = 70, MinHeight = 26,
            Margin = new Thickness(6, 0, 0, 0),
        };
        cancel.Click += (_, _) => cancelAction();
        buttons.Children.Add(buy);
        buttons.Children.Add(cancel);
        return ShopActionRow.Create(new TextBlock
        {
            Text = message,
            FontSize = 11,
            FontWeight = messageWeight,
            Foreground = messageBrush,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
        }, buttons);
    }

    /// <summary>macOS parity: the real egg sprite (SpriteView speciesID nil,
    /// 26px in a 30px cell) — cached-first via SpriteSlot, 🥚 only as the
    /// pre-load fallback glyph (WPF renders emoji monochrome).</summary>
    private FrameworkElement CreateEggIcon()
    {
        var grid = new Grid { Width = 26, Height = 26 };
        var image = new Image { Width = 26, Height = 26, Stretch = Stretch.Uniform };
        var placeholder = new TextBlock
        {
            Text = "🥚", FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        grid.Children.Add(placeholder);
        grid.Children.Add(image);
        new SpriteSlot(image, placeholder).UpdateEgg(_sprites, "🥚");
        return grid;
    }

    private FrameworkElement CreateItemIcon(ItemKind kind, double size = 24)
    {
        var grid = new Grid { Width = size, Height = size };
        var image = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
        var placeholder = new TextBlock
        {
            FontSize = Math.Max(11, size * 0.5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        grid.Children.Add(placeholder);
        grid.Children.Add(image);
        new ItemIconSlot(image, placeholder).Update(_sprites, kind);
        return grid;
    }

    private FrameworkElement CreateShopCardHeader(FrameworkElement icon, string title, string ownedSuffix,
        string description, Rarity? tier, AppLanguage lang)
    {
        var header = new DockPanel();

        var iconBox = new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(6),
            Background = Token("AccentSoftBrush"),
            Child = icon,
        };
        DockPanel.SetDock(iconBox, Dock.Left);
        header.Children.Add(iconBox);

        var titleRow = new DockPanel();
        if (tier is { } rarityTier)
        {
            var capsule = new Border
            {
                Background = Paint.RarityBrush(_theme, rarityTier), CornerRadius = new CornerRadius(7),
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

    private Brush Token(string key) => Paint.Token(_theme, key);
}
