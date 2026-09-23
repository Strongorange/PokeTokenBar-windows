using PokeTokenBar.Core;

namespace PokeTokenBar.Providers;

public sealed record CodexUsageVector(
    long Input,
    long CachedInput,
    long CacheWriteInput,
    long Output,
    long ReasoningOutput,
    long Total)
{
    public string Fingerprint() =>
        $"{Input},{CachedInput},{CacheWriteInput},{Output},{ReasoningOutput},{Total}";

    public long BillableComponents() => Math.Max(0, Input - CachedInput) + CachedInput + Output;

    public bool IsLowerThan(CodexUsageVector previous) =>
        Input < previous.Input
        || CachedInput < previous.CachedInput
        || CacheWriteInput < previous.CacheWriteInput
        || Output < previous.Output
        || ReasoningOutput < previous.ReasoningOutput
        || Total < previous.Total;
}

public sealed record CodexUsageState(CodexUsageVector Cumulative, CodexUsageVector Last)
{
    public string Fingerprint() => $"{Cumulative.Fingerprint()}|{Last.Fingerprint()}";
}

public sealed record CodexUsageEvent(
    UsageEntry Entry,
    CodexUsageState? UsageState,
    string? SessionID);

public sealed record CodexParsedRollout(
    string Path,
    string? SessionID,
    string? ParentSessionID,
    DateTimeOffset? ForkedAt,
    bool IsSubagent,
    List<CodexUsageEvent> Events);
