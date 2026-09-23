using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed record UsageProviderSnapshot(
    string ProviderId,
    string DisplayName,
    IReadOnlyList<UsageEntry> Entries);

public interface IUsageProvider
{
    string ProviderId { get; }
    string DisplayName { get; }

    List<UsageEntry> ParseFile(string path, IReadOnlyList<string> lines);

    UsageProviderSnapshot BuildSnapshot(IEnumerable<UsageEntry> fileEntries);
}
