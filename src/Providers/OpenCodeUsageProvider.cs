using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed class OpenCodeUsageProvider : IUsageProvider<List<UsageEntry>>
{
    public string ProviderId => "opencode";
    public string DisplayName => "OpenCode";

    public List<UsageEntry> ParseFile(string path, IReadOnlyList<string> lines) =>
        OpenCodeLogParser.Parse(path, lines);

    public UsageProviderSnapshot BuildSnapshot(IEnumerable<List<UsageEntry>> filePayloads) =>
        new(ProviderId, DisplayName, UsageAggregation.DedupKeepMax(filePayloads.SelectMany(e => e)));
}
