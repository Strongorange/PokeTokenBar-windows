using PokeTokenBar.Core;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class CodexRolloutResolverTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    private static IReadOnlyList<string> FixtureLines(params string[] parts) =>
        File.ReadAllLines(Path.Combine([AppContext.BaseDirectory, "fixtures", .. parts]));

    private static CodexParsedRollout Parse(string fileName, params string[] parts) =>
        CodexLogParser.Parse(Path.Combine([AppContext.BaseDirectory, "fixtures", .. parts, fileName]),
            FixtureLines([.. parts, fileName]), Tz);

    private static CodexParsedRollout ParseLines(string fileName, IReadOnlyList<string> lines) =>
        CodexLogParser.Parse(fileName, lines, Tz);

    internal static string ForkedMetaLine(string ts) =>
        "{\"type\":\"session_meta\",\"timestamp\":\"" + ts +
        "\",\"payload\":{\"id\":\"child\",\"forked_from_id\":\"parent\",\"parent_thread_id\":\"parent\",\"thread_source\":\"user\"}}";

    internal static string ForkedMetaLine(string id, string parentId, string ts) =>
        "{\"type\":\"session_meta\",\"timestamp\":\"" + ts + "\",\"payload\":{\"id\":\"" + id +
        "\",\"session_id\":\"" + parentId + "\",\"forked_from_id\":\"" + parentId +
        "\",\"parent_thread_id\":\"" + parentId + "\",\"thread_source\":\"subagent\"}}";

    private static List<UsageEntry> Resolve(params CodexParsedRollout[] rollouts) =>
        CodexRolloutResolver.Resolve(rollouts);

    [Fact]
    public void ForkFixturesTrimParentReplayAndKeepOwnUsageOnEachDay()
    {
        var parent = Parse("parent.jsonl", "CodexFork");
        var child = Parse("child.jsonl", "CodexFork");

        var entries = Resolve(parent, child);

        var parentEntries = entries.Where(e => e.LocalDay == "2026-07-13").ToList();
        Assert.Equal(312_814L, parentEntries.Sum(e => e.Total));
        Assert.Equal(8, parentEntries.Count);
        var childEntries = entries.Where(e => e.LocalDay == "2026-07-28").ToList();
        Assert.Equal([0L, 28_138L], childEntries.Select(e => e.Total).OrderBy(t => t).ToList());
        Assert.Equal(340_952L, entries.Sum(e => e.Total));
        Assert.Equal(entries.Count, entries.Select(e => e.Id).Distinct().Count());
        Assert.All(childEntries, e => Assert.StartsWith(
            "codex|00000000-0000-7000-8000-000000000002|", e.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void ForkFixtureWithoutParentFallsBackToTimingTrim()
    {
        var child = Parse("child.jsonl", "CodexFork");

        var entries = Resolve(child);

        Assert.Equal([0L, 28_138L], entries.Select(e => e.Total).ToList());
    }

    [Fact]
    public void SiblingForkFixturesKeepIndependentPostReplayUsage()
    {
        var entries = Resolve(
            Parse("parent.jsonl", "CodexFork"),
            Parse("child.jsonl", "CodexFork"),
            Parse("sibling.jsonl", "CodexFork"));

        Assert.Equal([28_138L, 28_263L],
            entries.Select(e => e.Total).Where(t => t is 28_138 or 28_263).OrderBy(t => t).ToList());
        Assert.Equal(369_215L, entries.Sum(e => e.Total));
    }

    [Fact]
    public void SubagentFixturesKeepAllOwnUsageWithoutReplayTrim()
    {
        var fixtures = new (string Parent, string Child, long[] Totals, long Combined, string ChildId)[]
        {
            ("parent.jsonl", "child.jsonl",
                [22_992, 23_043, 23_062, 23_219, 23_291], 115_607,
                "00000000-0000-7000-8000-000000000002"),
            ("parent-v145.jsonl", "child-v145.jsonl",
                [20_863, 21_175, 21_365, 21_458, 21_722], 106_583,
                "00000000-0000-7000-8000-000000000146"),
        };

        foreach (var fixture in fixtures)
        {
            var entries = Resolve(
                Parse(fixture.Parent, "CodexSubagent"),
                Parse(fixture.Child, "CodexSubagent"));

            Assert.Equal(fixture.Totals, entries.Select(e => e.Total).OrderBy(t => t).ToArray());
            Assert.Equal(fixture.Combined, entries.Sum(e => e.Total));
            Assert.Equal(2, entries.Count(e => e.Id.StartsWith(
                "codex|" + fixture.ChildId + "|", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void SubagentChildAloneKeepsFirstTurnWhenParentIsMissing()
    {
        var fixtures = new (string Child, string ChildId, string ParentId, long[] Totals)[]
        {
            ("child.jsonl", "00000000-0000-7000-8000-000000000002",
                "00000000-0000-7000-8000-000000000001", [23_062, 23_291]),
            ("child-v145.jsonl", "00000000-0000-7000-8000-000000000146",
                "00000000-0000-7000-8000-000000000145", [21_458, 21_722]),
        };

        foreach (var fixture in fixtures)
        {
            var rollout = Parse(fixture.Child, "CodexSubagent");

            Assert.Equal(fixture.ChildId, rollout.SessionID);
            Assert.Equal(fixture.ParentId, rollout.ParentSessionID);
            Assert.True(rollout.IsSubagent);
            var entries = Resolve(rollout);
            Assert.Equal(fixture.Totals, entries.Select(e => e.Total).ToArray());
            Assert.All(entries, e => Assert.StartsWith(
                "codex|" + fixture.ChildId + "|", e.Id, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ForkOfForkResolvesThroughAncestorHistory()
    {
        var first = CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10);
        var second = CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 200, 20, 100, 10);
        var third = CodexLogParserTests.StateLine("2026-07-30T01:00:03.000Z", 300, 30, 100, 10);
        var fourth = CodexLogParserTests.StateLine("2026-07-30T01:00:04.000Z", 400, 40, 100, 10);

        var entries = Resolve(
            ParseLines("root.jsonl", [CodexLogParserTests.SessionMetaLine("root", "2026-07-30T01:00:00.000Z"), first, second]),
            ParseLines("child.jsonl", [ForkedMetaLine("child", "root", "2026-07-30T02:00:00.000Z"), CodexLogParserTests.SessionMetaLine("root", "2026-07-30T02:00:00.001Z"), first, second, third]),
            ParseLines("grandchild.jsonl", [ForkedMetaLine("grandchild", "child", "2026-07-30T03:00:00.000Z"), CodexLogParserTests.SessionMetaLine("child", "2026-07-30T03:00:00.001Z"), first, second, third, fourth]));

        Assert.Equal([110L, 110L, 110L, 110L], entries.Select(e => e.Total).ToList());
        Assert.Equal(2, entries.Count(e => e.Id.StartsWith("codex|root|", StringComparison.Ordinal)));
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|child|", StringComparison.Ordinal)));
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|grandchild|", StringComparison.Ordinal)));
    }

    [Fact]
    public void SiblingForksWithIdenticalOwnUsageKeepDistinctIds()
    {
        var replay = CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10);
        var own = CodexLogParserTests.StateLine("2026-07-30T02:00:01.000Z", 200, 20, 100, 10);

        var entries = Resolve(
            ParseLines("root.jsonl", [CodexLogParserTests.SessionMetaLine("root", "2026-07-30T01:00:00.000Z"), replay]),
            ParseLines("left.jsonl", [ForkedMetaLine("left", "root", "2026-07-30T02:00:00.000Z"), CodexLogParserTests.SessionMetaLine("root", "2026-07-30T02:00:00.001Z"), replay, own]),
            ParseLines("right.jsonl", [ForkedMetaLine("right", "root", "2026-07-30T02:00:00.000Z"), CodexLogParserTests.SessionMetaLine("root", "2026-07-30T02:00:00.001Z"), replay, own]));

        Assert.Equal(3, entries.Count);
        Assert.Equal(3, entries.Select(e => e.Id).Distinct().Count());
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|left|", StringComparison.Ordinal)));
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|right|", StringComparison.Ordinal)));
    }

    [Fact]
    public void CumulativeResetStartsNewCanonicalEpoch()
    {
        var entries = Resolve(ParseLines("rollout-a.jsonl",
        [
            CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 10, 1, 10, 1),
        ]));

        Assert.Equal(2, entries.Count);
        Assert.StartsWith("codex|session-a|0|", entries[0].Id, StringComparison.Ordinal);
        Assert.StartsWith("codex|session-a|1|", entries[1].Id, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalIdCollapsesSameStateAcrossFilesKeepingEarliestDate()
    {
        var entries = Resolve(
            ParseLines("later.jsonl",
            [
                CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-30T02:00:00.000Z", 100, 10, 100, 10),
            ]),
            ParseLines("earlier.jsonl",
            [
                CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-30T01:00:00.000Z", 100, 10, 100, 10),
            ]));

        var entry = Assert.Single(entries);
        Assert.Equal(new DateTimeOffset(2026, 7, 30, 1, 0, 0, TimeSpan.Zero), entry.Date);
    }

    [Fact]
    public void MidSessionTotalOnlyLastIsFilledAtResolution()
    {
        var entries = Resolve(ParseLines("rollout-a.jsonl",
        [
            CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 600, 50, 0, 0, lastTotal: 500),
        ]));

        Assert.Equal(2, entries.Count);
        Assert.Equal(110, entries[0].Total);
        Assert.Equal(500, entries[1].Total);
        Assert.Equal(500, entries[1].Input);
        Assert.True(entries[1].CostUnavailable);
    }

    [Fact]
    public void MidSessionTotalOnlyLastStaysZeroWhenCumulativeDidNotGrow()
    {
        var entries = Resolve(ParseLines("rollout-a.jsonl",
        [
            CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 100, 10, 0, 0, lastTotal: 90),
        ]));

        Assert.Equal(2, entries.Count);
        Assert.Equal(0, entries[1].Total);
    }

    [Fact]
    public void SameStateRerecordCollapsesBeforeReplayTrim()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            ForkedMetaLine("2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-29T01:00:03.000Z", 300, 30, 200, 20),
            CodexLogParserTests.StateLine("2026-07-29T01:00:04.000Z", 300, 30, 200, 20),
        ]));

        Assert.Equal([220L], entries.Select(e => e.Total).ToList());
    }

    [Fact]
    public void LeadingReplayBurstIsDroppedByTimingFallback()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            ForkedMetaLine("2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.010Z", output: 50),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.020Z", output: 51),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.000Z", output: 52),
        ]));

        var entry = Assert.Single(entries);
        Assert.Equal(52, entry.Output);
    }

    [Fact]
    public void ReplayBurstAfterMetadataDelayIsDropped()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            ForkedMetaLine("2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.000Z", output: 1),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.010Z", output: 2),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.020Z", output: 3),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:43.000Z", output: 99),
        ]));

        Assert.Equal([99L], entries.Select(e => e.Output).ToList());
    }

    [Fact]
    public void RealTurnsAfterReplayBurstAreKeptWhenSpacedApart()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            ForkedMetaLine("2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.010Z", output: 1),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.020Z", output: 2),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.030Z", output: 3),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:01.530Z", output: 11),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.030Z", output: 22),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:04.530Z", output: 33),
            CodexLogParserTests.TokenLine("2026-07-29T01:01:00.000Z", output: 44),
        ]));

        Assert.Equal([11L, 22L, 33L, 44L], entries.Select(e => e.Output).ToList());
    }

    [Fact]
    public void MetadataAfterLeadingNonTokenRecordIsDetected()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            "{\"type\":\"turn_context\",\"timestamp\":\"2026-07-29T01:00:00.000Z\",\"payload\":{}}",
            ForkedMetaLine("2026-07-29T01:00:00.001Z"),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.010Z", output: 1),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:03.000Z", output: 99),
        ]));

        Assert.Equal([99L], entries.Select(e => e.Output).ToList());
    }

    [Fact]
    public void ReplayIsTrimmedAgainstTheParentRollout()
    {
        var entries = Resolve(
            ParseLines("rollout-parent.jsonl",
            [
                CodexLogParserTests.SessionMetaLine("parent", "2026-07-29T01:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            ]),
            ParseLines("rollout-child.jsonl",
            [
                ForkedMetaLine("2026-07-29T02:00:00.000Z"),
                CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
                CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
            ]));

        Assert.Equal([110L, 220L], entries.Select(e => e.Total).OrderBy(t => t).ToList());
    }

    [Fact]
    public void DegenerateParentIdStillResolvesViaTimingFallback()
    {
        var entries = Resolve(ParseLines("rollout-child.jsonl",
        [
            "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-29T01:00:00.000Z\",\"payload\":{\"id\":\"child\",\"forked_from_id\":\"-\",\"thread_source\":\"user\"}}",
            CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-29T01:00:05.000Z", 300, 30, 200, 20),
        ]));

        Assert.Equal([220L], entries.Select(e => e.Total).ToList());
    }

    [Fact]
    public void IncludedPathsFilterExcludesDependencyOnlyRollouts()
    {
        var parent = ParseLines("rollout-parent.jsonl",
        [
            CodexLogParserTests.SessionMetaLine("parent", "2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-29T01:00:00.010Z", 100, 10, 100, 10),
        ]);
        var child = ParseLines("rollout-child.jsonl",
        [
            ForkedMetaLine("2026-07-29T02:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-29T02:00:00.010Z", 100, 10, 100, 10),
            CodexLogParserTests.StateLine("2026-07-29T02:00:05.000Z", 300, 30, 200, 20),
        ]);

        var entries = CodexRolloutResolver.Resolve([child, parent], [child.Path]);

        var entry = Assert.Single(entries);
        Assert.Equal(220, entry.Total);
    }

    [Fact]
    public void SessionSwitchInsideNonForkFileAttributesEventsToEachOwner()
    {
        var entries = Resolve(ParseLines("rollout-a.jsonl",
        [
            CodexLogParserTests.SessionMetaLine("session-a", "2026-07-30T01:00:00.000Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:01.000Z", 100, 10, 100, 10),
            CodexLogParserTests.SessionMetaLine("session-b", "2026-07-30T01:00:01.500Z"),
            CodexLogParserTests.StateLine("2026-07-30T01:00:02.000Z", 100, 10, 100, 10),
        ]));

        Assert.Equal(2, entries.Count);
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|session-a|", StringComparison.Ordinal)));
        Assert.Equal(1, entries.Count(e => e.Id.StartsWith("codex|session-b|", StringComparison.Ordinal)));
    }

    [Fact]
    public void EmptyRolloutListYieldsNoEntries() =>
        Assert.Empty(CodexRolloutResolver.Resolve([]));
}
