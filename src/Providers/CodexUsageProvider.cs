using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed class CodexUsageProvider : IUsageProvider<CodexParsedRollout>
{
    public string ProviderId => "codex";
    public string DisplayName => "Codex";

    public CodexParsedRollout ParseFile(string path, IReadOnlyList<string> lines) =>
        CodexLogParser.Parse(path, lines);

    public UsageProviderSnapshot BuildSnapshot(IEnumerable<CodexParsedRollout> filePayloads) =>
        new(ProviderId, DisplayName, CodexRolloutResolver.Resolve(filePayloads));
}
