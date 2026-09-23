using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed class ClaudeUsageProvider : IUsageProvider<List<UsageEntry>>
{
    public string ProviderId => "claude_code";
    public string DisplayName => "Claude Code";

    public List<UsageEntry> ParseFile(string path, IReadOnlyList<string> lines) =>
        ClaudeLogParser.Parse(path, lines);

    public UsageProviderSnapshot BuildSnapshot(IEnumerable<List<UsageEntry>> filePayloads) =>
        new(ProviderId, DisplayName, UsageAggregation.DedupKeepMax(filePayloads.SelectMany(e => e)));
}
