using System.Globalization;
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
        var monthFrom = UsageAggregation.StartOfMonth(
            TimeZoneInfo.ConvertTime(now, timeZone).DateTime)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var summaries = new List<ProviderUsageSummary>();
        var combined = new List<UsageEntry>();
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerRoots = roots.Where(root => provider.RootKinds.Contains(root.Kind)).ToList();
            var outcome = provider.Refresh(now, timeZone, providerRoots);
            summaries.Add(Summarize(outcome, todayKey, monthKey, monthFrom, todayKey));
            combined.AddRange(outcome.Snapshot.Entries);
        }

        var combinedToday = UsageAggregation.Daily(combined, todayKey);
        var combinedMonth = UsageAggregation.Period(combined, monthKey, monthFrom, todayKey);
        return new UsageDisplayState(
            now,
            summaries,
            combinedToday?.TotalTokens ?? 0,
            combinedToday?.TotalCost ?? 0,
            combinedMonth.TotalTokens,
            combinedMonth.TotalCost);
    }

    private static ProviderUsageSummary Summarize(
        ProviderRefreshOutcome outcome,
        string todayKey,
        string monthKey,
        string monthFrom,
        string monthTo)
    {
        var today = UsageAggregation.Daily(outcome.Snapshot.Entries, todayKey);
        var month = UsageAggregation.Period(outcome.Snapshot.Entries, monthKey, monthFrom, monthTo);
        return new ProviderUsageSummary(
            outcome.Snapshot.ProviderId,
            outcome.Snapshot.DisplayName,
            outcome.Available,
            today?.TotalTokens ?? 0,
            today?.TotalCost ?? 0,
            month.TotalTokens,
            month.TotalCost);
    }
}
