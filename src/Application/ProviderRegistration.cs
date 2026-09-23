using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application;

public sealed record ProviderRefreshOutcome(bool Available, UsageProviderSnapshot Snapshot);

public delegate ProviderRefreshOutcome ProviderRefreshDelegate(
    DateTimeOffset now,
    TimeZoneInfo timeZone,
    IReadOnlyList<UsageRoot> roots);

public sealed record ProviderRegistration(
    string ProviderId,
    string DisplayName,
    IReadOnlySet<UsageRootKind> RootKinds,
    ProviderRefreshDelegate Refresh);
