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
    private readonly IReadOnlyList<ProviderRegistration> _providers;
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
        _providers = _options.Providers ??
            ProviderCatalog.Default(_options.CacheDirectory, _options.FileSystem);
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
        UsageDisplayState? state = null;
        try
        {
            state = await Task.Run(() => RefreshAll(cancellationToken), CancellationToken.None);
            lock (_gate) _current = state;
            completion.SetResult(state);
        }
        catch (Exception ex)
        {
            AppLog.Write($"refresh failed: {ex.GetType().Name}: {ex.Message}");
            completion.SetException(ex);
            return;
        }
        finally
        {
            lock (_gate) _inFlight = null;
        }
        StateChanged?.Invoke(state);
    }

    private UsageDisplayState RefreshAll(CancellationToken cancellationToken)
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
        var combined = new List<UsageEntry>();
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerRoots = roots.Where(root => provider.RootKinds.Contains(root.Kind)).ToList();
            var outcome = provider.Refresh(now, timeZone, providerRoots);
            summaries.Add(Summarize(outcome, todayKey, monthKey, monthFrom, weekKey));
            combined.AddRange(outcome.Snapshot.Entries);
        }

        var combinedToday = UsageAggregation.Daily(combined, todayKey);
        var combinedMonth = UsageAggregation.Period(combined, monthKey, monthFrom, todayKey);
        var combinedWeek = UsageAggregation.Period(combined, weekKey, weekKey, todayKey);
        var combinedMonthDaily = UsageAggregation.MonthDailySeries(combined, now, timeZone);
        var combinedYearDaily = UsageAggregation.YearDailySeries(combined, now, timeZone);
        var combinedTodayModels = UsageAggregation.Daily(combined, todayKey, includeModels: true);
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
        var today = UsageAggregation.Daily(outcome.Snapshot.Entries, todayKey, includeModels: true);
        var month = UsageAggregation.Period(outcome.Snapshot.Entries, monthKey, monthFrom, todayKey);
        var week = UsageAggregation.Period(outcome.Snapshot.Entries, monthKey, weekFrom, todayKey);
        return new ProviderUsageSummary(
            outcome.Snapshot.ProviderId,
            outcome.Snapshot.DisplayName,
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
