using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Companion header + progress renderer extracted from DashboardWindow
/// code-behind (M26). Visual builders only — the name/stage/progress/
/// badge/combat-text decisions live in GameTabPresentation (unit-tested).
/// </summary>
internal sealed class CompanionHeader
{
    private readonly FrameworkElement _theme;
    private readonly SpriteStore _sprites;
    private readonly TextBlock _name;
    private readonly Border _rarityCapsule;
    private readonly TextBlock _rarityCapsuleText;
    private readonly TextBlock _detail;
    private readonly Border _statusCapsule;
    private readonly TextBlock _statusCapsuleText;
    private readonly TextBlock _combatText;
    private readonly TextBlock _progressLabel;
    private readonly ProgressBar _progressBar;
    private readonly TextBlock _statusText;
    private readonly FrameworkElement _evolutionScroll;
    private readonly StackPanel _evolutionLine;
    private readonly SpriteSlot _companionSprite;

    public CompanionHeader(
        FrameworkElement theme, SpriteStore sprites,
        TextBlock name, Border rarityCapsule, TextBlock rarityCapsuleText,
        TextBlock detail, Border statusCapsule, TextBlock statusCapsuleText,
        TextBlock combatText, TextBlock progressLabel, ProgressBar progressBar,
        TextBlock statusText, FrameworkElement evolutionScroll, StackPanel evolutionLine,
        Image companionSprite, TextBlock companionSpritePlaceholder)
    {
        _theme = theme;
        _sprites = sprites;
        _name = name;
        _rarityCapsule = rarityCapsule;
        _rarityCapsuleText = rarityCapsuleText;
        _detail = detail;
        _statusCapsule = statusCapsule;
        _statusCapsuleText = statusCapsuleText;
        _combatText = combatText;
        _progressLabel = progressLabel;
        _progressBar = progressBar;
        _statusText = statusText;
        _evolutionScroll = evolutionScroll;
        _evolutionLine = evolutionLine;
        _companionSprite = new SpriteSlot(companionSprite, companionSpritePlaceholder);
    }

    public void Render(CompanionGameView view, CompanionDetailSnapshot? detail)
    {
        var lang = view.Language;
        _name.Text = GameTabPresentation.HeaderName(view);
        _name.ToolTip = GameTabPresentation.ShinyTooltip(view);

        if (view.Rarity is { } rarityValue)
        {
            _rarityCapsule.Visibility = Visibility.Visible;
            _rarityCapsule.Background = Paint.RarityBrush(_theme, rarityValue);
            _rarityCapsuleText.Text = GameTabPresentation.RarityCapsuleLabel(rarityValue, lang);
        }
        else
        {
            _rarityCapsule.Visibility = Visibility.Collapsed;
        }

        _detail.Text = GameTabPresentation.DetailLine(
            view, GameTabPresentation.RaisingNature(detail));
        var badge = GameTabPresentation.StatusBadge(view);
        if (badge is null)
        {
            _statusCapsule.Visibility = Visibility.Collapsed;
        }
        else if (badge.Kind == GameBadgeKind.GrowthBoost)
        {
            _statusCapsule.Visibility = Visibility.Visible;
            _statusCapsule.Background = Paint.HexBrush("#26F7630C");
            _statusCapsuleText.Text = badge.Text;
            _statusCapsuleText.Foreground = Token("RarityLegendaryBrush");
        }
        else
        {
            _statusCapsule.Visibility = Visibility.Visible;
            _statusCapsule.Background = Paint.RarityBrush(_theme, badge.Rarity!.Value);
            _statusCapsuleText.Text = badge.Text;
            _statusCapsuleText.Foreground = Brushes.White;
        }

        _progressLabel.Text = GameTabPresentation.ProgressCaption(view);
        _progressBar.Value = GameTabPresentation.ProgressValue(view);
        _detail.Foreground = !view.HasActive && GameTabPresentation.EggImminent(view)
            ? Token("RarityLegendaryBrush")
            : Token("TextSecondaryBrush");

        RenderEvolutionLine(view);
        if (view.HasActive)
            _companionSprite.Update(_sprites, view.ActiveSpeciesID, true, view.IsShiny,
                view.ActiveUnownForm, "❔");
        else
            _companionSprite.UpdateEgg(_sprites, "🥚");

        _combatText.Text = GameTabPresentation.CombatLine(view, detail);
        _statusText.Text = DashboardText.StatusLine(lang, view.Status, view.StatusEvolvedName);
    }

    private void RenderEvolutionLine(CompanionGameView view)
    {
        _evolutionLine.Children.Clear();
        var visible = view.HasActive && view.EvoLine.Count > 0;
        _evolutionScroll.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) return;
        for (var i = 0; i < view.EvoLine.Count; i++)
        {
            if (i > 0) _evolutionLine.Children.Add(CreateEvolutionArrow());
            _evolutionLine.Children.Add(CreateEvolutionCell(view.EvoLine[i], view));
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

    private Brush Token(string key) => Paint.Token(_theme, key);
}
