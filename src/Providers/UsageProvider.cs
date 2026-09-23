using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed record UsageProviderSnapshot(
    string ProviderId,
    string DisplayName,
    IReadOnlyList<UsageEntry> Entries);

public interface IUsageProvider<TPayload>
{
    string ProviderId { get; }
    string DisplayName { get; }

    TPayload ParseFile(string path, IReadOnlyList<string> lines);

    UsageProviderSnapshot BuildSnapshot(IEnumerable<TPayload> filePayloads);
}
