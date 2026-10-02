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
    private readonly FrameworkElement _yearSection;
    private readonly Grid _yearHeatHost;
    private string? _selectedProviderId;
    private UsageDisplayState? _lastState;
    private AppLanguage _lastLang;
    private double _lastYearCell;

    public UsageHomeRenderer(
        FrameworkElement theme,
        TextBlock todayCaptionLabel, TextBlock todayBigNumber, TextBlock todayGroupedLabel,
        TextBlock todayCostLabel, StackPanel weekMonthRow,
        StackPanel providerChips, StackPanel providerDetail,
        TextBlock providersUnavailableText, FrameworkElement providersSection,
        FrameworkElement trendSection, TextBlock trendCaptionLabel, TextBlock trendPeakLabel,
        TextBlock trendPeakValue, TextBlock trendReadout, StackPanel trendModelsHost,
        Grid trendBarsHost, Grid trendTicksHost, Grid trendAxisHost,
        FrameworkElement yearSection, Grid yearHeatHost)
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
        _yearSection = yearSection;
        _yearHeatHost = yearHeatHost;
    }

    public void Render(UsageDisplayState state, AppLanguage lang)
    {
        _lastState = state;
        _lastLang = lang;
        RenderTodayCard(state, lang);
        RenderProviders(state, lang);
        RenderTrend(state, lang);
        RenderYearHeatmap(state, lang);
    }

    /// 창 폭 변경 시 ScrollViewer.SizeChanged 에서 호출 — 잔디만 다시 그린다.
    /// 셀 크기가 실제로 바뀐 경우에만 재구축(드래그 중 불필요한 리렌더 방지).
    public void RerenderYear()
    {
        if (_lastState is not { YearDaily: { Count: > 0 } } state) return;
        var viewport = (_yearHeatHost.Parent as ScrollViewer)?.ActualWidth ?? 0;
        if (viewport <= 0) return;
        var firstDate = state.YearDaily[0].Date;
        var columns = 0;
        foreach (var day in state.YearDaily)
            columns = Math.Max(columns, DailyTrendMetrics.HeatColumn(day.Date, firstDate) + 1);
        var candidate = Math.Clamp((viewport - 14 - 14) / Math.Max(columns, 1) - 2, 4, 14);
        if (Math.Abs(candidate - _lastYearCell) < 0.25) return;
        RenderYearHeatmap(state, _lastLang);
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
        var cost = new UsageCost(provider.TodayCost, provider.TodayCostCoverage);
        var totals = UsageValuesLine.Create(
            cost.Coverage.HasKnown ? cost.Text("", compact: true) : null,
            TokenFormatter.Compact(provider.TodayTokens),
            Token("TextSecondaryBrush"));
        totals.VerticalAlignment = VerticalAlignment.Bottom;
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

    /// 연간 잔디 히트맵 — 깃허브 기여 그래프 결(53주 x 7행 요일). 별도 카드
    /// ("최근 1년")로 제공하며 월간 막대 카드와 분리. 행/열/레벨/월 라벨 계산은
    /// Core DailyTrendMetrics(단위 테스트), 셀 크기·간격은 순수 시각 값.
    /// 시리즈(YearDaily)는 그리드 시작 일요일부터 오늘까지 0으로 채워 온다.
    private void RenderYearHeatmap(UsageDisplayState state, AppLanguage lang)
    {
        _yearHeatHost.RowDefinitions.Clear();
        _yearHeatHost.ColumnDefinitions.Clear();
        _yearHeatHost.Children.Clear();
        var series = state.YearDaily;
        var peak = DailyTrendMetrics.Peak(series);
        if (series is not { Count: > 0 } || peak <= 0)
        {
            _yearSection.Visibility = Visibility.Collapsed;
            return;
        }

        // 셀 크기는 뷰포트 폭에 반응형 — 53열이 정확히 들어가도록 **소수 픽셀**로
        // 계산한다(floor 하면 열마다 버리는 폭이 우측 여백으로 몰린다). 상한 14px
        // 도달 후에도 남는 폭은 그리드 중앙 정렬로 좌우 대칭이 된다.
        const double gap = 2;
        const double monthLabelOverhang = 14;
        const double weekdayLabelWidth = 14;
        var firstDate = series[0].Date;
        var columns = 0;
        foreach (var day in series)
            columns = Math.Max(columns, DailyTrendMetrics.HeatColumn(day.Date, firstDate) + 1);
        if (columns <= 0)
        {
            _yearSection.Visibility = Visibility.Collapsed;
            return;
        }
        _yearSection.Visibility = Visibility.Visible;

        var viewport = (_yearHeatHost.Parent as ScrollViewer)?.ActualWidth ?? 0;
        var cell = viewport > 0
            ? Math.Clamp((viewport - weekdayLabelWidth - monthLabelOverhang) / columns - gap, 4, 14)
            : 8;
        _lastYearCell = cell;

        var todayKey = UsageAggregation.LocalDay(state.AsOfUtc);

        // 0열 = 요일 라벨, 1..N열 = 주. 0행 = 월 라벨, 1..7행 = 요일.
        // 마지막 열 뒤 여유 칸(monthLabelOverhang) — "10월"처럼 칸 폭보다 넓은
        // 라벨이 그리드 폭을 넘어 ScrollViewer 경계에서 잘리는 것을 막는다.
        _yearHeatHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (var week = 0; week < columns; week++)
            _yearHeatHost.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(cell + gap) });
        _yearHeatHost.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(monthLabelOverhang) });
        _yearHeatHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var row = 0; row < 7; row++)
            _yearHeatHost.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(cell + gap) });

        var initials = DashboardText.WeekdayInitials(lang);
        for (var row = 0; row < 7 && row < initials.Length; row++)
        {
            var label = new TextBlock
            {
                Text = initials[row].ToString(),
                FontSize = 8,
                Foreground = Token("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
            };
            Grid.SetRow(label, row + 1);
            _yearHeatHost.Children.Add(label);
        }

        string? lastMonth = null;
        // int.MinValue 로 두면 'column - lastLabeledColumn'이 int 오버플로로
        // 음수가 되어 첫 라벨부터 전부 스킵된다(실제로 겪은 결함). -2 = 첫 라벨 허용.
        var lastLabeledColumn = -2;
        foreach (var day in series)
        {
            var column = DailyTrendMetrics.HeatColumn(day.Date, firstDate);
            if (column < 0 || day.Date.Length < 7) continue;
            var monthKey = day.Date[..7];
            if (monthKey == lastMonth) continue;
            lastMonth = monthKey;
            // 라벨(~18px)은 칸 폭(10px)보다 넓다 — 직전 라벨과 2열 미만이면
            // 겹쳐서 지저분해지므로(그리드 시작 직후 9월/10월 케이스) 건너뛴다.
            if (column - lastLabeledColumn < 2) continue;
            lastLabeledColumn = column;
            var monthLabel = new TextBlock
            {
                Text = DailyTrendMetrics.MonthLabel(day.Date, lang),
                FontSize = 8,
                Foreground = Token("TextTertiaryBrush"),
                Margin = new Thickness(0, 0, 0, 2),
            };
            Grid.SetRow(monthLabel, 0);
            Grid.SetColumn(monthLabel, column + 1);
            _yearHeatHost.Children.Add(monthLabel);
        }

        foreach (var day in series)
        {
            var row = DailyTrendMetrics.HeatRow(day.Date);
            var column = DailyTrendMetrics.HeatColumn(day.Date, firstDate);
            if (row < 0 || column < 0) continue;
            var isToday = day.Date == todayKey;
            var level = DailyTrendMetrics.HeatLevel(day.TotalTokens, peak);
            // 툴팁: 날짜(첫 줄) + 토큰·비용(둘째 줄). 작은 칸이라 즉시 뜨게.
            var usage = $"{TokenFormatter.Compact(day.TotalTokens)} {DashboardText.TokensUnit(lang)}";
            if (day.UsageCost.Coverage.HasKnown)
                usage += $" · {day.UsageCost.Text("", compact: true)}";
            var cellBox = new Border
            {
                Width = cell,
                Height = cell,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(1.5),
                Background = Token($"Heat{level}Brush"),
                BorderBrush = isToday ? Token("AccentBrush") : null,
                BorderThickness = new Thickness(isToday ? 1.5 : 0),
                ToolTip = $"{DailyTrendMetrics.DayStamp(day.Date, lang)}\n{usage}",
            };
            ToolTipService.SetInitialShowDelay(cellBox, 150);
            ToolTipService.SetBetweenShowDelay(cellBox, 0);
            ToolTipService.SetShowDuration(cellBox, 8000);
            Grid.SetRow(cellBox, row + 1);
            Grid.SetColumn(cellBox, column + 1);
            _yearHeatHost.Children.Add(cellBox);
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
