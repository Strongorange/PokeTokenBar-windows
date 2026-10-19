using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Usage recap (macOS #332): one calendar week, month or year at a time. The chrome follows the
/// app theme; the card is a fixed-palette Pokédex LCD readout so the recap stays a device, not
/// a third chart. Offset 0 is the running period and never goes positive — the future has no
/// usage. Re-renders from a fresh ledger copy on every refresh so "today" stays live.
/// </summary>
public partial class RecapWindow : Window
{
    private const int MeterBlocks = 8;

    private static readonly Color Lcd = Color.FromRgb(0x82, 0xFA, 0x9E);
    private static readonly Color Amber = Color.FromRgb(0xFF, 0xBA, 0x47);

    private readonly UsageRefreshService _service;
    private readonly CompanionEngine _engine;
    private readonly SpriteStore _sprites;
    private readonly TimeZoneInfo _timeZone;
    private RecapScope _scope = RecapScope.Week;
    private int _offset;

    public RecapWindow(UsageRefreshService service, CompanionEngine engine, SpriteStore sprites,
        TimeZoneInfo? timeZone = null)
    {
        InitializeComponent();
        _service = service;
        _engine = engine;
        _sprites = sprites;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
        _service.StateChanged += OnUsageChanged;
        _engine.Changed += OnEngineChanged;
        Closed += (_, _) =>
        {
            _service.StateChanged -= OnUsageChanged;
            _engine.Changed -= OnEngineChanged;
        };
        Render();
    }

    private void OnUsageChanged(UsageDisplayState _) => Dispatcher.BeginInvoke(Render);

    private void OnEngineChanged() => Dispatcher.BeginInvoke(Render);

    private void Render()
    {
        var lang = _engine.State.Language;
        var view = RecapQuery.Build(_service.LedgerCopy(), _engine.State.Dex, _scope, _offset,
            DateTimeOffset.UtcNow, _timeZone, lang);

        Title = DashboardText.RecapOpen(lang);
        CloseButton.Content = "_" + DashboardText.CloseButton(lang);
        CopyButton.Content = "_" + DashboardText.CopyToClipboard(lang);
        BuildScopeButtons(lang);
        PreviousButton.Content = "‹ " + DashboardText.RecapPrevious(lang);
        NextButton.Content = DashboardText.RecapNext(lang) + " ›";
        CurrentButton.Content = view.CurrentPeriodLabel;
        PreviousButton.IsEnabled = true;
        NextButton.IsEnabled = _offset < 0;
        CurrentButton.IsEnabled = _offset != 0;

        PeriodLabel.Text = view.Title.ToUpperInvariant();
        TotalText.Text = TokenFormatter.Compact(view.TotalTokens);
        TotalUnit.Text = DashboardText.TokensUnit(lang).ToUpperInvariant();
        if (view.DeltaText is { } delta)
        {
            DeltaChip.Visibility = Visibility.Visible;
            var up = delta.StartsWith('+');
            DeltaText.Text = (up ? "▲" : "▼") + delta.TrimStart('+');
            DeltaText.Foreground = new SolidColorBrush(up ? Lcd : Amber);
        }
        else
        {
            DeltaChip.Visibility = Visibility.Collapsed;
        }
        CompareNote.Text = view.CompareNote ?? "";
        CompareNote.Visibility =
            view.CompareNote is null ? Visibility.Collapsed : Visibility.Visible;
        RenderMeters(view);
        RenderStats(view, lang);
        RenderGraduates(view, lang);
        BestDayLine.Text = view.BestDayText;
        CoverageNote.Text = DashboardText.RecapCompareSoFar(lang, view.Scope);
    }

    private void BuildScopeButtons(AppLanguage lang)
    {
        ScopeButtons.Children.Clear();
        foreach (var scope in Enum.GetValues<RecapScope>())
        {
            var button = new RadioButton
            {
                Content = DashboardText.RecapScopeName(lang, scope),
                GroupName = "RecapScope",
                IsChecked = scope == _scope,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                Tag = scope,
            };
            button.Checked += (_, _) => SelectScope(scope);
            ScopeButtons.Children.Add(button);
        }
    }

    private void SelectScope(RecapScope scope)
    {
        if (scope == _scope) return;
        _scope = scope;
        _offset = 0;
        Render();
    }

    private void RenderMeters(RecapView view)
    {
        Meters.Columns = Math.Max(1, view.Buckets.Count);
        Meters.Children.Clear();
        var peak = Math.Max(1, view.Buckets.Max(bucket => bucket.Tokens));
        // macOS meter spacing: week 6 / month 1.5 / year 4 — split into side margins.
        var horizontal = view.Scope switch
        {
            RecapScope.Week => 3.0,
            RecapScope.Year => 2.0,
            _ => 0.75
        };
        foreach (var bucket in view.Buckets)
        {
            var lit = bucket.Tokens > 0
                ? Math.Max(1, (int)Math.Round(bucket.Tokens / (double)peak * MeterBlocks))
                : 0;
            var column = new StackPanel();
            for (var block = MeterBlocks - 1; block >= 0; block--)
            {
                var isLit = block < lit;
                column.Children.Add(new Rectangle
                {
                    Height = 4,
                    RadiusX = 1,
                    RadiusY = 1,
                    Margin = new Thickness(horizontal, 0, horizontal, 2),
                    Fill = new SolidColorBrush(isLit
                        ? bucket.IsCurrent ? Lcd : Dim(Lcd, 0.55)
                        : Dim(Lcd, bucket.HasData ? 0.09 : 0.035)),
                });
            }
            var label = new TextBlock
            {
                Text = bucket.Label,
                FontFamily = new FontFamily("Consolas"),
                FontSize = view.Scope == RecapScope.Week ? 8 : 7,
                FontWeight = bucket.IsCurrent ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(bucket.IsCurrent ? Lcd : Dim(Lcd, 0.45)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            var cell = new StackPanel { ToolTip = bucket.ToolTip };
            cell.Children.Add(column);
            cell.Children.Add(label);
            Meters.Children.Add(cell);
        }
    }

    private static Color Dim(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * opacity), color.R, color.G, color.B);

    private void RenderStats(RecapView view, AppLanguage lang)
    {
        StatsLeft.Children.Clear();
        StatsRight.Children.Clear();
        StatsLeft.Children.Add(Readout(DashboardText.RecapBestDay(lang), view.BestDayTokensText));
        StatsLeft.Children.Add(Readout(DashboardText.RecapBestStreak(lang), view.StreakText));
        StatsRight.Children.Add(Readout(DashboardText.RecapActiveDays(lang), view.ActiveDaysText));
        StatsRight.Children.Add(Readout(DashboardText.RecapGraduates(lang),
            view.Graduates.Count.ToString()));
    }

    private static TextBlock Readout(string label, string value) => new()
    {
        Text = label.ToUpperInvariant() + "  " + value,
        Foreground = new SolidColorBrush(Dim(Lcd, 0.5)),
        FontFamily = new FontFamily("Consolas"),
        FontSize = 7.5,
        FontWeight = FontWeights.Medium,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new Thickness(0, 0, 0, 3),
    };

    private void RenderGraduates(RecapView view, AppLanguage lang)
    {
        GraduatesCaption.Text = DashboardText.RecapGraduates(lang).ToUpperInvariant();
        Graduates.Children.Clear();
        if (view.Graduates.Count == 0)
        {
            Graduates.Children.Add(new TextBlock
            {
                Text = view.GraduatesEmptyText,
                FontSize = 8,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
            });
            return;
        }
        foreach (var graduate in view.Graduates)
        {
            var spriteHost = new Grid
            {
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center
            };
            var image = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            var placeholder = new TextBlock { Text = "❔", FontSize = 7 };
            spriteHost.Children.Add(image);
            spriteHost.Children.Add(placeholder);
            new SpriteSlot(image, placeholder)
                .Update(_sprites, graduate.SpeciesID, false, graduate.IsShiny, graduate.Form, "❔");
            if (graduate.IsShiny)
                spriteHost.Children.Add(new TextBlock
                {
                    Text = "✨",
                    FontSize = 7,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                });
            var chip = new StackPanel { Orientation = Orientation.Horizontal };
            spriteHost.SetValue(MarginProperty, new Thickness(0, 0, 2, 0));
            chip.Children.Add(spriteHost);
            chip.Children.Add(new TextBlock
            {
                Text = graduate.Name,
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 110,
            });
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(56, 0, 0, 0)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 5, 0),
                Child = chip,
                ToolTip = graduate.Name,
            };
            Graduates.Children.Add(border);
        }
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        _offset--;
        Render();
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_offset >= 0) return;
        _offset++;
        Render();
    }

    private void OnCurrentClick(object sender, RoutedEventArgs e)
    {
        _offset = 0;
        Render();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        if (e.Key == Key.Left) OnPreviousClick(sender, e);
        if (e.Key == Key.Right) OnNextClick(sender, e);
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        var lang = _engine.State.Language;
        var view = RecapQuery.Build(_service.LedgerCopy(), _engine.State.Dex, _scope, _offset,
            DateTimeOffset.UtcNow, _timeZone, lang);
        var lines = new List<string>
        {
            $"{DashboardText.RecapOpen(lang)} — {view.Title}",
            $"{view.TotalText} {DashboardText.TokensUnit(lang)}" +
            (view.DeltaText is { } delta ? $" ({delta.TrimStart('+')})" : ""),
            $"{DashboardText.RecapBestDay(lang)}: {view.BestDayDateText ?? "--"} · {view.BestDayTokensText}",
            $"{DashboardText.RecapActiveDays(lang)}: {view.ActiveDaysText}",
            $"{DashboardText.RecapBestStreak(lang)}: {view.StreakText}",
            $"{DashboardText.RecapGraduates(lang)}: {view.Graduates.Count}",
        };
        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
        }
        catch (Exception ex)
        {
            // The clipboard is a shared resource another process may hold.
            AppLog.Write($"recap copy to clipboard failed: {ex.Message}");
        }
    }
}
