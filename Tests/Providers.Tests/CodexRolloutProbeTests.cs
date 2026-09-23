using System.Text;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Providers.Tests;

public class CodexRolloutProbeTests : IDisposable
{
    private readonly string _dir;

    public CodexRolloutProbeTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-providers-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private static string PaddedSessionMeta(string id, int totalBytes, string tail = "")
    {
        var head = "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-29T01:00:00.000Z\",\"payload\":{\"id\":\"" +
                   id + "\",\"session_id\":\"" + id + "\",\"base_instructions\":\"";
        var close = "\"}}";
        var pad = totalBytes - Encoding.UTF8.GetByteCount(head) - Encoding.UTF8.GetByteCount(close) -
                  Encoding.UTF8.GetByteCount(tail);
        Assert.True(pad > 0);
        return head + new string('a', pad) + tail + close;
    }

    private string WriteProbeFile(params string[] lines)
    {
        var path = Path.Combine(_dir, "rollout-probe.jsonl");
        File.WriteAllText(path, string.Join("\n", lines));
        return path;
    }

    private static MemoryStream StreamOf(params string[] lines)
    {
        var memory = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return memory;
    }

    [Fact]
    public void ReadsSessionIdWhenMetadataLineExceedsChunkSize()
    {
        var path = WriteProbeFile(
            PaddedSessionMeta("parent-xl", 200_000),
            CodexLogParserTests.TokenLine("2026-07-29T01:00:01.000Z"));
        var prefix = new byte[64 * 1024];
        using (var stream = File.OpenRead(path))
        {
            var read = stream.Read(prefix, 0, prefix.Length);
            Assert.Equal(prefix.Length, read);
        }
        Assert.DoesNotContain((byte)'\n', prefix);

        Assert.Equal("parent-xl", CodexRolloutProbe.ProbeFile(path));
    }

    [Fact]
    public void DecodesMultibyteStraddlingChunkBoundary()
    {
        var boundary = 64 * 1024;
        var meta = PaddedSessionMeta("parent-utf8", boundary + 4, "가");
        var bytes = Encoding.UTF8.GetBytes(meta);
        Assert.True((byte)0x80 <= bytes[boundary] && bytes[boundary] <= (byte)0xBF,
            "boundary byte must be a UTF-8 continuation byte");

        Assert.Equal("parent-utf8", CodexRolloutProbe.Probe(StreamOf(
            meta, CodexLogParserTests.TokenLine("2026-07-29T01:00:01.000Z"))));
    }

    [Fact]
    public void ScansManyLinesAcrossChunks()
    {
        var lines = Enumerable.Range(0, 2_000)
            .Select(i => "{\"type\":\"response_item\",\"seq\":" + i + ",\"payload\":{\"text\":\"filler-filler-filler\"}}")
            .ToList();
        lines.Add(CodexLogParserTests.SessionMetaLine("parent-after-many-lines", "2026-07-29T01:00:00.000Z"));

        Assert.Equal("parent-after-many-lines", CodexRolloutProbe.Probe(StreamOf([.. lines])));
    }

    [Fact]
    public void StopsAtByteLimit()
    {
        var path = WriteProbeFile(PaddedSessionMeta("parent-capped", 4_096));

        Assert.Null(CodexRolloutProbe.ProbeFile(path, byteLimit: 1_024));
        Assert.Equal("parent-capped", CodexRolloutProbe.ProbeFile(path, byteLimit: 4_096));
        Assert.Equal("parent-capped", CodexRolloutProbe.ProbeFile(path));
    }

    [Fact]
    public void StopsAtInvalidUtf8BeforeSessionMeta()
    {
        var path = Path.Combine(_dir, "rollout-invalid-utf8.jsonl");
        var data = new List<byte> { 0xFF, 0x0A };
        data.AddRange(Encoding.UTF8.GetBytes(
            CodexLogParserTests.SessionMetaLine("wrong-parent", "2026-07-29T01:00:00.001Z")));
        File.WriteAllBytes(path, [.. data]);

        Assert.Null(CodexRolloutProbe.ProbeFile(path));
    }

    [Fact]
    public void StopsAtTokenCountBeforeSessionMeta()
    {
        Assert.Null(CodexRolloutProbe.Probe(StreamOf(
            CodexLogParserTests.TokenLine("2026-07-29T01:00:00.000Z"),
            CodexLogParserTests.SessionMetaLine("too-late", "2026-07-29T01:00:01.000Z"))));
    }

    [Fact]
    public void FindsMetadataAfterLeadingNonTokenRecord()
    {
        Assert.Equal("parent-late", CodexRolloutProbe.Probe(StreamOf(
            "{\"type\":\"turn_context\",\"timestamp\":\"2026-07-29T01:00:00.000Z\",\"payload\":{}}",
            CodexLogParserTests.SessionMetaLine("parent-late", "2026-07-29T01:00:00.001Z"))));
    }

    [Fact]
    public void SessionMetaWithoutIdIsDefinitiveNull()
    {
        Assert.Null(CodexRolloutProbe.Probe(StreamOf(
            "{\"type\":\"session_meta\",\"timestamp\":\"2026-07-29T01:00:00.000Z\",\"payload\":{}}",
            CodexLogParserTests.SessionMetaLine("never-reached", "2026-07-29T01:00:01.000Z"))));
    }

    [Fact]
    public void MissingFileThrowsInsteadOfReturningNull()
    {
        Assert.ThrowsAny<IOException>(() =>
            CodexRolloutProbe.ProbeFile(Path.Combine(_dir, "does-not-exist.jsonl")));
    }

    [Fact]
    public void FileWithoutMetadataReturnsNull()
    {
        var path = WriteProbeFile(
            "{\"type\":\"response_item\",\"payload\":{\"text\":\"filler\"}}",
            CodexLogParserTests.TokenLine("2026-07-29T01:00:01.000Z"));
        Assert.Null(CodexRolloutProbe.ProbeFile(path));
    }
}
