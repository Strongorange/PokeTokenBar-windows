using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

/// <summary>
/// Usage home renderer — today card, provider chips/detail, and the month
/// trend chart, extracted from DashboardWindow code-behind (M25). Owns the
/// provider chip selection state; chip clicks re-render internally. Pure
/// decisions live in UsagePresentation/DailyTrendMetrics (unit-tested).
/// </summary>
internal sealed class UsageHomeRenderer
{
    private readonly FrameworkElement _theme;
    private readonly TextBlock _todayCaptionLabel;
    private readonly TextBlock _todayBigNumber;
    private readonly TextBlock _todayGroupedLabel;
    private readonly TextBlock _todayCostLabel;
    private readonly StackPanel _weekMonthRow;
    private readonly StackPanel _providerChips;
    private readonly StackPanel _providerDetail;
    private readonly TextBlock _providersUnavailableText;
    private readonly FrameworkElement _providersSection;
    private readonly FrameworkElement _trendSection;
    private readonly TextBlock _trendCaptionLabel;
    private readonly TextBlock _trendPeakLabel;
    private readonly TextBlock _trendPeakValue;
    private readonly TextBlock _trendReadout;
    private readonly StackPanel _trendModelsHost;
    private readonly Grid _trendBarsHost;
    private readonly Grid _trendTicksHost;
    private readonly Grid _trendAxisHost;
    private string? _selectedProviderId;
    private UsageDisplayState? _lastState;

    public UsageHomeRenderer(
        FrameworkElement theme,
        TextBlock todayCaptionLabel, TextBlock todayBigNumber, TextBlock todayGroupedLabel,
        TextBlock todayCostLabel, StackPanel weekMonthRow,
        StackPanel providerChips, StackPanel providerDetail,
        TextBlock providersUnavailableText, FrameworkElement providersSection,
        FrameworkElement trendSection, TextBlock trendCaptionLabel, TextBlock trendPeakLabel,
        TextBlock trendPeakValue, TextBlock trendReadout, StackPanel trendModelsHost,
        Grid trendBarsHost, Grid trendTicksHost, Grid trendAxisHost)
    {
        _theme = theme;
        _todayCaptionLabel = todayCaptionLabel;
        _todayBigNumber = todayBigNumber;
        _todayGroupedLabel = todayGroupedLabel;
        _todayCostLabel = todayCostLabel;
        _weekMonthRow = weekMonthRow;
        _providerChips = providerChips;
        _providerDetail = providerDetail;
        _providersUnavailableText = providersUnavailableText;
        _providersSection = providersSection;
        _trendSection = trendSection;
        _trendCaptionLabel = trendCaptionLabel;
        _trendPeakLabel = trendPeakLabel;
        _trendPeakValue = trendPeakValue;
        _trendReadout = trendReadout;
        _trendModelsHost = trendModelsHost;
        _trendBarsHost = trendBarsHost;
        _trendTicksHost = trendTicksHost;
        _trendAxisHost = trendAxisHost;
    }

    public void Render(UsageDisplayState state, AppLanguage lang)
    {
        _lastState = state;
        RenderTodayCard(state, lang);
        RenderProviders(state, lang);
        RenderTrend(state, lang);
    }

    private void RenderTodayCard(UsageDisplayState state, AppLanguage lang)
    {
        _todayCaptionLabel.Text = DashboardText.TodayTokensHeader(lang);
        _todayBigNumber.Text = TokenFormatter.Compact(state.TodayTokens);
        _todayGroupedLabel.Text = TokenFormatter.Grouped(state.TodayTokens);
        var todayCost = new UsageCost(state.TodayCost, state.TodayCostCoverage);
        _todayCostLabel.Text = todayCost.Coverage.HasKnown ? todayCost.Text("", compact: true) : "";

        _weekMonthRow.Children.Clear();
        if (state.WeekTokens > 0 || state.MonthTokens > 0)
        {
            _weekMonthRow.Children.Add(CreatePeriodLabel(
                DashboardText.ThisWeekLabel(lang), state.WeekTokens,
                new UsageCost(state.WeekCost, state.WeekCostCoverage)));
            var monthLabel = CreatePeriodLabel(
                DashboardText.ThisMonthLabel(lang), state.MonthTokens,
                new UsageCost(state.MonthCost, state.MonthCostCoverage));
            monthLabel.Margin = new Thickness(14, 0, 0, 0);
            _weekMonthRow.Children.Add(monthLabel);
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
        var selectable = UsagePresentation.SelectableProviders(state.Providers);
        _providerChips.Children.Clear();
        _providerDetail.Children.Clear();
        _providersUnavailableText.Text =
            UsagePresentation.UnavailableNote(state.Providers, lang);

        var selectedId = CompanionPresentation.ResolveSelectedProvider(selectable, _selectedProviderId);
        _selectedProviderId = selectedId;
        var selected = selectable.FirstOrDefault(provider => provider.ProviderId == selectedId);

        if (selectable.Count > 1)
        {
            foreach (var provider in selectable)
            {
                var isSelected = provider.ProviderId == selectedId;
                var capsule = new Border
                {
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(10, 2, 10, 2),
                    Margin = new Thickness(0, 0, 6, 0),
                    Background = isSelected
                        ? Token("AccentSoftBrush")
                        : Paint.HexBrush("#14000000"),
                    BorderBrush = isSelected ? Token("AccentBrush") : null,
                    BorderThickness = new Thickness(isSelected ? 1.5 : 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                };
                var picked = provider;
                capsule.MouseLeftButtonUp += (_, _) =>
                {
                    _selectedProviderId = picked.ProviderId;
                    if (_lastState is { } usage) Render(usage, lang);
                };
                capsule.Child = new TextBlock
                {
                    Text = provider.DisplayName,
                    FontSize = 10,
                    FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isSelected ? Token("AccentBrush") : Token("TextSecondaryBrush"),
                };
                _providerChips.Children.Add(capsule);
            }
        }

        if (selected is not null)
            _providerDetail.Children.Add(CreateProviderDetail(selected, lang));

        var unavailable = state.Providers.Where(provider => !provider.Available).ToList();
        var empty = _providerChips.Children.Count == 0 && _providerDetail.Children.Count == 0
            && unavailable.Count == 0;
        _providersSection.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
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
            foreach (var pair in UsagePresentation.OrderedModels(provider.TodayModels))
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
            Text = UsagePresentation.ModelShortName(model),
            FontSize = 10,
            Foreground = Token("TextSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        return row;
    }

    private void RenderTrend(UsageDisplayState state, AppLanguage lang)
    {
        var series = state.MonthDaily;
        var peak = DailyTrendMetrics.Peak(series);
        _trendSection.Visibility = peak > 0 ? Visibility.Visible : Visibility.Collapsed;
        _trendModelsHost.Children.Clear();
        _trendBarsHost.ColumnDefinitions.Clear();
        _trendBarsHost.Children.Clear();
        _trendTicksHost.ColumnDefinitions.Clear();
        _trendTicksHost.Children.Clear();
        _trendAxisHost.ColumnDefinitions.Clear();
        _trendAxisHost.Children.Clear();
        if (peak <= 0 || series is null) return;

        _trendCaptionLabel.Text = DashboardText.DailyTrend(lang);
        _trendPeakLabel.Text = DashboardText.PeakDay(lang);
        _trendPeakValue.Text = TokenFormatter.Compact(peak);

        var showsCost = DailyTrendMetrics.ShowsCost(series);
        var todayKey = UsageAggregation.LocalDay(state.AsOfUtc);
        var today = DailyTrendMetrics.TodayUsage(series, todayKey);

        var index = 0;
        foreach (var day in series)
        {
            _trendBarsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _trendTicksHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _trendAxisHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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
            _trendBarsHost.Children.Add(bar);

            var hoveredDay = day;
            var hit = new Border { Background = Brushes.Transparent };
            Grid.SetColumn(hit, index);
            hit.MouseEnter += (_, _) => SetTrendReadout(hoveredDay, showsCost, lang);
            hit.MouseLeave += (_, _) => SetTrendReadout(today, showsCost, lang);
            _trendBarsHost.Children.Add(hit);

            if (DailyTrendMetrics.IsWeekend(day.Date))
            {
                var tick = new Border { Background = Token("TextTertiaryBrush"), Opacity = 0.5 };
                Grid.SetColumn(tick, index);
                _trendTicksHost.Children.Add(tick);
            }

            var label = new TextBlock
            {
                Text = DailyTrendMetrics.AxisLabel(day.Date, todayKey)?.ToString() ?? "",
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isToday ? Token("AccentBrush") : Token("TextSecondaryBrush"),
            };
            Grid.SetColumn(label, index);
            _trendAxisHost.Children.Add(label);
            index++;
        }
        SetTrendReadout(today, showsCost, lang);

        if (state.TodayModels is { Count: > 1 })
        {
            foreach (var pair in UsagePresentation.OrderedModels(state.TodayModels))
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
                    Text = UsagePresentation.ModelShortName(pair.Key),
                    FontSize = 10,
                    Foreground = Token("TextSecondaryBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });
                _trendModelsHost.Children.Add(row);
            }
        }
    }

    private void SetTrendReadout(DailyUsage? day, bool showsCost, AppLanguage lang)
    {
        if (day is null)
        {
            _trendReadout.Text = "";
            return;
        }
        var text = $"{DailyTrendMetrics.DayStamp(day.Date, lang)} {TokenFormatter.Compact(day.TotalTokens)}";
        if (showsCost) text += " " + day.UsageCost.Text("", compact: true);
        _trendReadout.Text = text;
    }

    private Brush Token(string key) =>
        _theme.TryFindResource(key) as Brush ?? Brushes.Gray;
}
