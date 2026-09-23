using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Application;

public interface IUsageRootSource
{
    IReadOnlyList<UsageRoot> Discover();
}

public sealed class DiscoveryUsageRootSource(UsageRootOptions? options = null) : IUsageRootSource
{
    public IReadOnlyList<UsageRoot> Discover() => UsageRootDiscovery.Discover(options);
}
