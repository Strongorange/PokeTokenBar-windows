using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application;

/// <param name="Days">Per-day totals the usage views read; providers never hand over individual entries.</param>
public sealed record ProviderRefreshOutcome(bool Available, string ProviderId, string DisplayName, DailyUsageTable Days)
{
    public static ProviderRefreshOutcome From(bool available, UsageProviderSnapshot snapshot) =>
        new(available, snapshot.ProviderId, snapshot.DisplayName, DailyUsageTable.From(snapshot.Entries));
}

public delegate ProviderRefreshOutcome ProviderRefreshDelegate(
    DateTimeOffset now,
    TimeZoneInfo timeZone,
    IReadOnlyList<UsageRoot> roots);

public sealed record ProviderRegistration(
    string ProviderId,
    string DisplayName,
    IReadOnlySet<UsageRootKind> RootKinds,
    ProviderRefreshDelegate Refresh);
