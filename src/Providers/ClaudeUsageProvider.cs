using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed class ClaudeUsageProvider : IUsageProvider
{
    public string ProviderId => "claude_code";
    public string DisplayName => "Claude Code";

    public List<UsageEntry> ParseFile(string path, IReadOnlyList<string> lines) =>
        ClaudeLogParser.Parse(path, lines);

    public UsageProviderSnapshot BuildSnapshot(IEnumerable<UsageEntry> fileEntries) =>
        new(ProviderId, DisplayName, UsageAggregation.DedupKeepMax(fileEntries));
}
