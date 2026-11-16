using System.Globalization;
using System.Text.Json;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Application;

public sealed class UsageRefreshOptions
{
    public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromMinutes(5);

    public TimeSpan RefreshInterval { get; init; } = DefaultRefreshInterval;

    public string CacheDirectory { get; init; } = DefaultCacheDirectory();

    public TimeZoneInfo? TimeZone { get; init; }

    public UsageRootOptions? RootOptions { get; init; }

    public Func<DateTimeOffset>? Clock { get; init; }

    public Func<TimeSpan, CancellationToken, Task>? Delay { get; init; }

    public IFileSystemSource? FileSystem { get; init; }

    public IUsageRootSource? RootSource { get; init; }

    public IReadOnlyList<ProviderRegistration>? Providers { get; init; }

    /// <summary>Runs on the refresh thread after each successful refresh, e.g. to give memory back.</summary>
    public Action? AfterRefresh { get; init; }

    /// <summary>
    /// Computes the display state somewhere else, e.g. in a short-lived scan process. When it fails the
    /// service computes in process for that round. Providers and their caches load only on that fallback.
    /// </summary>
    public Func<CancellationToken, UsageDisplayState>? Compute { get; init; }

    public static string DefaultCacheDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokeTokenBar");
}

public sealed class UsageRefreshService
{
    private readonly UsageRefreshOptions _options;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly IUsageRootSource _rootSource;
    private readonly Lazy<IReadOnlyList<ProviderRegistration>> _providers;
    private readonly object _gate = new();
    private Task<UsageDisplayState>? _inFlight;
    private UsageDisplayState? _current;
    private UsageLedger? _ledger;

    public UsageRefreshService(UsageRefreshOptions? options = null)
    {
        _options = options ?? new UsageRefreshOptions();
        _clock = _options.Clock ?? (static () => DateTimeOffset.UtcNow);
        _delay = _options.Delay ?? Task.Delay;
        _rootSource = _options.RootSource ?? new DiscoveryUsageRootSource(_options.RootOptions);
        _providers = new Lazy<IReadOnlyList<ProviderRegistration>>(() => _options.Providers ??
            ProviderCatalog.Default(_options.CacheDirectory, _options.FileSystem));
    }

    public UsageDisplayState? Current
    {
        get
        {
            lock (_gate) return _current;
        }
    }

    /// <summary>
    /// Day-by-day history behind the usage recap. Copy-on-read: the refresh path mutates the
    /// ledger on a background thread, and the recap reads a snapshot on the UI thread.
    /// </summary>
    public UsageLedger LedgerCopy()
    {
        lock (_gate)
        {
            // 바깥 프로세스가 계산하면 usage-ledger.json 도 그쪽이 고쳐서, 메모리에 둔 사본은 낡음
            if (_options.Compute is not null) _ledger = null;
            _ledger ??= UsageLedgerFile.Load(UsageLedgerFile.PathFor(_options.CacheDirectory));
            return UsageLedger.Read(JsonDocument.Parse(_ledger.Write().ToJsonString()).RootElement);
        }
    }

    public event Action<UsageDisplayState>? StateChanged;

    public Task<UsageDisplayState> RefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_inFlight is not null) return _inFlight;
            var completion = new TaskCompletionSource<UsageDisplayState>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _inFlight = completion.Task;
            _ = RefreshOnceAsync(completion, cancellationToken);
            return completion.Task;
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            await _delay(_options.RefreshInterval, cancellationToken);
            try
            {
                await RefreshAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Write($"periodic refresh round skipped: {ex.Message}");
            }
        }
    }

    private async Task RefreshOnceAsync(
        TaskCompletionSource<UsageDisplayState> completion, CancellationToken cancellationToken)
    {
        UsageDisplayState state;
        try
        {
            state = await Task.Run(() =>
            {
                var refreshed = Compute(cancellationToken);
                _options.AfterRefresh?.Invoke();
                return refreshed;
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppLog.Write($"refresh failed: {ex.GetType().Name}: {ex.Message}");
            lock (_gate) _inFlight = null;
            completion.SetException(ex);
            return;
        }
        // 완료를 알리기 전에 진행 중 표시를 지워야, 기다리던 쪽이 바로 다시 부를 때 새 갱신이 돎
        lock (_gate)
        {
            _current = state;
            _inFlight = null;
        }
        completion.SetResult(state);
        StateChanged?.Invoke(state);
    }

    private UsageDisplayState Compute(CancellationToken cancellationToken)
    {
        if (_options.Compute is { } external)
        {
            try
            {
                return external(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Write($"out-of-process refresh failed, computing in process: {ex.GetType().Name}: {ex.Message}");
            }
        }
        return RefreshAll(cancellationToken);
    }

    /// <summary>Scans every provider and builds the display state in this process.</summary>
    public UsageDisplayState RefreshAll(CancellationToken cancellationToken = default)
    {
        var now = _clock();
        var timeZone = _options.TimeZone ?? TimeZoneInfo.Local;
        var roots = _rootSource.Discover();
        var todayKey = UsageAggregation.LocalDay(now, timeZone);
        var monthKey = UsageAggregation.MonthKey(now, timeZone);
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone).DateTime;
        var monthFrom = UsageAggregation.StartOfMonth(localNow)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var weekKey = UsageAggregation.StartOfWeek(localNow, CultureInfo.InvariantCulture)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var summaries = new List<ProviderUsageSummary>();
        var combined = new DailyUsageTable();
        foreach (var provider in _providers.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerRoots = roots.Where(root => provider.RootKinds.Contains(root.Kind)).ToList();
            var outcome = provider.Refresh(now, timeZone, providerRoots);
            summaries.Add(Summarize(outcome, todayKey, monthKey, monthFrom, weekKey));
            combined.AddRange(outcome.Days);
        }

        var combinedToday = combined.Daily(todayKey);
        var combinedMonth = combined.Period(monthKey, monthFrom, todayKey);
        var combinedWeek = combined.Period(weekKey, weekKey, todayKey);
        var combinedMonthDaily = combined.MonthDailySeries(now, timeZone);
        var combinedYearDaily = combined.YearDailySeries(now, timeZone);
        var combinedTodayModels = combined.Daily(todayKey, includeModels: true);
        RecordDailyLedger(combinedYearDaily, now, timeZone);
        return new UsageDisplayState(
            now,
            summaries,
            combinedToday?.TotalTokens ?? 0,
            combinedToday?.TotalCost ?? 0,
            combinedMonth.TotalTokens,
            combinedMonth.TotalCost,
            combinedMonthDaily,
            combinedTodayModels?.Models,
            combinedWeek.TotalTokens,
            combinedWeek.TotalCost,
            combinedToday?.CostCoverage ?? CostCoverage.Empty,
            combinedWeek.CostCoverage,
            combinedMonth.CostCoverage,
            combinedYearDaily);
    }

    /// <summary>
    /// Feeds the day-by-day ledger on the refresh background path (serialized by the
    /// single-flight gate). The year series enumerates every day of the scan window, empty ones
    /// included, so its merge vouches for real zeros; pruning keeps this year and last year —
    /// the year view compares against last year and nothing reaches further back. The file is
    /// written only when the merge actually changed anything (macOS UsageStore parity).
    /// </summary>
    private void RecordDailyLedger(List<DailyUsage> yearDaily, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var changed = false;
        lock (_gate)
        {
            _ledger ??= UsageLedgerFile.Load(UsageLedgerFile.PathFor(_options.CacheDirectory));
            var before = _ledger.Write().ToJsonString(CompanionStateFile.IndentedJson);
            _ledger.Merge(yearDaily);
            var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
            _ledger.Prune(new DateTime(localNow.Year - 1, 1, 1)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            changed = _ledger.Write().ToJsonString(CompanionStateFile.IndentedJson) != before;
        }
        if (!changed) return;
        try
        {
            UsageLedgerFile.Save(UsageLedgerFile.PathFor(_options.CacheDirectory), _ledger);
        }
        catch (Exception ex)
        {
            AppLog.Write($"usage ledger save failed: {ex.Message}");
        }
    }

    private static ProviderUsageSummary Summarize(
        ProviderRefreshOutcome outcome,
        string todayKey,
        string monthKey,
        string monthFrom,
        string weekFrom)
    {
        var today = outcome.Days.Daily(todayKey, includeModels: true);
        var month = outcome.Days.Period(monthKey, monthFrom, todayKey);
        var week = outcome.Days.Period(monthKey, weekFrom, todayKey);
        return new ProviderUsageSummary(
            outcome.ProviderId,
            outcome.DisplayName,
            outcome.Available,
            today?.TotalTokens ?? 0,
            today?.TotalCost ?? 0,
            month.TotalTokens,
            month.TotalCost,
            today?.InputTokens ?? 0,
            today?.OutputTokens ?? 0,
            today?.CacheCreationTokens ?? 0,
            today?.CacheReadTokens ?? 0,
            week.TotalTokens,
            week.TotalCost,
            today?.CostCoverage ?? CostCoverage.Empty,
            week.CostCoverage,
            today?.Models);
    }
}
