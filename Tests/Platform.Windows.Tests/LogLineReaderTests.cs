using System.Text;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Platform.Windows.Tests;

public class LogLineReaderTests : IDisposable
{
    private readonly string _dir;
    private static readonly LogLineFilter KeepAll = static _ => true;

    public LogLineReaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-line-reader-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string Write(byte[] bytes)
    {
        var path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".jsonl");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string Write(string text) => Write(Encoding.UTF8.GetBytes(text));

    private static (List<string> Lines, LogLineReader Reader) ReadAll(
        string path, long offset = 0, LogLineFilter? filter = null, int chunkSize = LogLineReader.DefaultChunkSize)
    {
        var reader = new LogLineReader(path, offset, filter ?? KeepAll, chunkSize);
        return (reader.Lines().ToList(), reader);
    }

    [Fact]
    public void EmptyFileYieldsNoLines()
    {
        var (lines, reader) = ReadAll(Write(""));
        Assert.Empty(lines);
        Assert.False(reader.DroppedPartialFinalLine);
        Assert.Equal(0, reader.EndOffset);
    }

    [Fact]
    public void TrailingNewlineMeansNoPartialLine()
    {
        var (lines, reader) = ReadAll(Write("a\nb\n"));
        Assert.Equal(["a", "b"], lines);
        Assert.False(reader.DroppedPartialFinalLine);
        Assert.Equal(4, reader.EndOffset);
    }

    [Fact]
    public void MissingTrailingNewlineDropsPartialFinalLineAndStopsBeforeIt()
    {
        var (lines, reader) = ReadAll(Write("a\nbc"));
        Assert.Equal(["a"], lines);
        Assert.True(reader.DroppedPartialFinalLine);
        Assert.Equal(2, reader.EndOffset);
    }

    [Fact]
    public void CarriageReturnsAreTrimmed()
    {
        var (lines, _) = ReadAll(Write("a\r\nb\r\n"));
        Assert.Equal(["a", "b"], lines);
    }

    [Fact]
    public void BlankLinesArePreserved()
    {
        var (lines, _) = ReadAll(Write("a\n\nb\n"));
        Assert.Equal(["a", "", "b"], lines);
    }

    [Fact]
    public void LeadingByteOrderMarkIsStrippedOnlyAtFileStart()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var path = Write([.. bom, .. Encoding.UTF8.GetBytes("{\"a\":1}\n"), .. bom, .. Encoding.UTF8.GetBytes("x\n")]);
        var (lines, _) = ReadAll(path);
        Assert.Equal("{\"a\":1}", lines[0]);
        Assert.Equal("﻿x", lines[1]);
    }

    [Fact]
    public void MultibyteCharactersSurviveChunkBoundaries()
    {
        var text = "가나다라마바사\n이모지 😀 줄\n";
        for (var chunk = 1; chunk <= 8; chunk++)
        {
            var (lines, reader) = ReadAll(Write(text), chunkSize: chunk);
            Assert.Equal(["가나다라마바사", "이모지 😀 줄"], lines);
            Assert.Equal(Encoding.UTF8.GetByteCount(text), reader.EndOffset);
        }
    }

    [Fact]
    public void LineLongerThanTheChunkIsReadWhole()
    {
        var longLine = new string('x', 10_000);
        var (lines, _) = ReadAll(Write(longLine + "\nshort\n"), chunkSize: 16);
        Assert.Equal([longLine, "short"], lines);
    }

    [Fact]
    public void FilteredOutLinesAreSkippedButStillAdvanceTheOffset()
    {
        var path = Write("skip\nkeep me\nskip\n");
        var (lines, reader) = ReadAll(path, filter: line => line.IndexOf("keep"u8) >= 0);
        Assert.Equal(["keep me"], lines);
        Assert.Equal(new FileInfo(path).Length, reader.EndOffset);
    }

    [Fact]
    public void ReadingFromAnOffsetYieldsOnlyLaterLines()
    {
        var path = Write("first\nsecond\nthird\n");
        var (_, firstPass) = ReadAll(path);
        File.AppendAllText(path, "fourth\n");
        var (lines, secondPass) = ReadAll(path, firstPass.EndOffset);
        Assert.Equal(["fourth"], lines);
        Assert.Equal(new FileInfo(path).Length, secondPass.EndOffset);
    }

    [Fact]
    public void AnchorMatchesAfterAppendAndChangesAfterRewrite()
    {
        var path = Write("{\"n\":1}\n{\"n\":2}\n");
        var offset = new FileInfo(path).Length;
        var anchor = LogLineReader.AnchorAt(path, offset);

        File.AppendAllText(path, "{\"n\":3}\n");
        Assert.Equal(anchor, LogLineReader.AnchorAt(path, offset));

        File.WriteAllText(path, "{\"n\":9}\n{\"n\":8}\n{\"n\":3}\n");
        Assert.NotEqual(anchor, LogLineReader.AnchorAt(path, offset));
    }
}
