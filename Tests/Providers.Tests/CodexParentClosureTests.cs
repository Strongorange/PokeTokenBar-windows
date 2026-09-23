using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class CodexParentClosureTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    private static CodexRolloutFile FileOf(string name) =>
        new(@"C:\codex\sessions\" + name, DateTimeOffset.UtcNow, 1_000);

    private static CodexParsedRollout Load(
        Dictionary<string, IReadOnlyList<string>> contents, CodexRolloutFile file) =>
        CodexLogParser.Parse(file.Path, contents[Path.GetFileName(file.Path)], Tz);

    [Fact]
    public void DegenerateFilenameHintsAreNotUsable()
    {
        Assert.False(CodexParentClosure.IsUsableFilenameHint("-"));
        Assert.False(CodexParentClosure.IsUsableFilenameHint(""));
        Assert.False(CodexParentClosure.IsUsableFilenameHint("----"));
        Assert.True(CodexParentClosure.IsUsableFilenameHint("parent"));
        Assert.True(CodexParentClosure.IsUsableFilenameHint("00000000-0000-7000-8000-000000000001"));
    }

    [Fact]
    public void HintedParentIsAdoptedOnlyWhenPayloadSessionIdMatches()
    {
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-child.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("2026-07-29T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
                CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
            ],
            ["rollout-parent.jsonl"] =
            [
                CodexLogParserTests.SessionMetaLine("parent", "2026-07-29T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            ],
            ["rollout-wrong-id.jsonl"] =
            [
                CodexLogParserTests.SessionMetaLine("someone-else", "2026-07-29T00:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T00:00:00.010Z", 500, 50, 500, 50),
            ],
        };
        var window = new[] { FileOf("rollout-child.jsonl") };
        var all = window.Concat([FileOf("rollout-parent.jsonl"), FileOf("rollout-wrong-id.jsonl")]).ToList();
        var probeCalls = 0;

        var (rollouts, includedPaths) = CodexParentClosure.Expand(
            window, all,
            file => Load(contents, file),
            _ => CodexSessionIdKnowledge.Unknown,
            _ =>
            {
                probeCalls++;
                return null;
            });

        Assert.Equal(2, rollouts.Count);
        Assert.Contains("parent", rollouts.Select(r => r.SessionID));
        Assert.Equal(["C:\\codex\\sessions\\rollout-child.jsonl"], includedPaths);
        Assert.Equal(0, probeCalls);
        var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
        Assert.Equal(220L, entries.Sum(e => e.Total));
    }

    [Fact]
    public void ParentIsAdoptedByProbeWhenFilenameHintDoesNotMatch()
    {
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-child.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("2026-07-29T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
                CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
            ],
            ["rollout-unrelated-name.jsonl"] =
            [
                CodexLogParserTests.SessionMetaLine("parent", "2026-07-29T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            ],
        };
        var window = new[] { FileOf("rollout-child.jsonl") };
        var all = window.Append(FileOf("rollout-unrelated-name.jsonl")).ToList();

        var (rollouts, includedPaths) = CodexParentClosure.Expand(
            window, all,
            file => Load(contents, file),
            _ => CodexSessionIdKnowledge.Unknown,
            file => Path.GetFileName(file.Path) == "rollout-unrelated-name.jsonl" ? "parent" : null);

        Assert.Equal(2, rollouts.Count);
        Assert.Contains("parent", rollouts.Select(r => r.SessionID));
        var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
        Assert.Equal(220L, entries.Sum(e => e.Total));
    }

    [Fact]
    public void KnownSessionIdsAreUsedWithoutOpeningOrProbingFiles()
    {
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-child.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("2026-07-29T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
                CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
            ],
            ["rollout-parent.jsonl"] =
            [
                CodexLogParserTests.SessionMetaLine("other-session", "2026-07-29T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            ],
        };
        var window = new[] { FileOf("rollout-child.jsonl") };
        var all = window.Append(FileOf("rollout-parent.jsonl")).ToList();
        var loaded = new List<string>();
        var probeCalls = 0;

        var (rollouts, includedPaths) = CodexParentClosure.Expand(
            window, all,
            file =>
            {
                loaded.Add(Path.GetFileName(file.Path));
                return Load(contents, file);
            },
            file => Path.GetFileName(file.Path) == "rollout-parent.jsonl"
                ? CodexSessionIdKnowledge.Of("other-session")
                : CodexSessionIdKnowledge.Unknown,
            _ =>
            {
                probeCalls++;
                return null;
            });

        Assert.Equal(["rollout-child.jsonl"], loaded);
        Assert.Equal(0, probeCalls);
        Assert.Single(rollouts);
        var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
        Assert.Equal(220L, entries.Sum(e => e.Total));
    }

    [Fact]
    public void KnownParentSessionIdIsAdoptedEvenWithoutFilenameHint()
    {
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-child.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("2026-07-29T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
                CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
            ],
            ["rollout-anywhere.jsonl"] =
            [
                CodexLogParserTests.SessionMetaLine("parent", "2026-07-29T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            ],
        };
        var window = new[] { FileOf("rollout-child.jsonl") };
        var all = window.Append(FileOf("rollout-anywhere.jsonl")).ToList();
        var probeCalls = 0;

        var (rollouts, _) = CodexParentClosure.Expand(
            window, all,
            file => Load(contents, file),
            file => Path.GetFileName(file.Path) == "rollout-anywhere.jsonl"
                ? CodexSessionIdKnowledge.Of("parent")
                : CodexSessionIdKnowledge.Unknown,
            _ =>
            {
                probeCalls++;
                return null;
            });

        Assert.Equal(0, probeCalls);
        Assert.Equal(2, rollouts.Count);
    }

    [Fact]
    public void AncestorDependenciesArePulledRecursively()
    {
        var first = CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10);
        var second = CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 200, 20, 100, 10);
        var third = CodexLogParserTests.StateLine("2026-07-30T01:00:03.000Z", 300, 30, 100, 10);
        var fourth = CodexLogParserTests.StateLine("2026-07-30T01:00:04.000Z", 400, 40, 100, 10);
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-root.jsonl"] =
                [CodexLogParserTests.SessionMetaLine("root", "2026-07-30T01:00:00.000Z"), first, second],
            ["rollout-child.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("child", "root", "2026-07-30T02:00:00.000Z"),
                CodexLogParserTests.SessionMetaLine("root", "2026-07-30T02:00:00.001Z"),
                first, second, third,
            ],
            ["rollout-grandchild.jsonl"] =
            [
                CodexRolloutResolverTests.ForkedMetaLine("grandchild", "child", "2026-07-30T03:00:00.000Z"),
                CodexLogParserTests.SessionMetaLine("child", "2026-07-30T03:00:00.001Z"),
                first, second, third, fourth,
            ],
        };
        var window = new[] { FileOf("rollout-grandchild.jsonl") };
        var all = new[]
        {
            FileOf("rollout-root.jsonl"),
            FileOf("rollout-child.jsonl"),
            FileOf("rollout-grandchild.jsonl"),
        };

        var (rollouts, includedPaths) = CodexParentClosure.Expand(
            window, all,
            file => Load(contents, file),
            _ => CodexSessionIdKnowledge.Unknown,
            file =>
            {
                var name = Path.GetFileName(file.Path);
                if (name == "rollout-root.jsonl") return "root";
                if (name == "rollout-child.jsonl") return "child";
                return null;
            });

        Assert.Equal(3, rollouts.Count);
        var entries = CodexRolloutResolver.Resolve(rollouts, includedPaths);
        var entry = Assert.Single(entries);
        Assert.StartsWith("codex|grandchild|", entry.Id, StringComparison.Ordinal);
        Assert.Equal(110, entry.Total);
    }

    [Fact]
    public void ProbeFailurePropagatesInsteadOfBecomingNoMetadata()
    {
        var contents = new Dictionary<string, IReadOnlyList<string>>
        {
            ["rollout-child.jsonl"] =
                [CodexRolloutResolverTests.ForkedMetaLine("2026-07-29T02:00:00.000Z")],
        };
        var window = new[] { FileOf("rollout-child.jsonl") };
        var all = window.Append(FileOf("rollout-x.jsonl")).ToList();

        Assert.Throws<IOException>(() => CodexParentClosure.Expand(
            window, all,
            file => Load(contents, file),
            _ => CodexSessionIdKnowledge.Unknown,
            _ => throw new IOException("transient")));
    }
}
