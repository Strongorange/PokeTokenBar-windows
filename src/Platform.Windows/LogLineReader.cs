using System.Text;

namespace PokeTokenBar.Platform.Windows;

public delegate bool LogLineFilter(ReadOnlySpan<byte> line);

/// <summary>
/// Streams the complete lines of an append-only log from a byte offset without holding the file in memory.
/// </summary>
public sealed class LogLineReader
{
    public const int DefaultChunkSize = 64 * 1024;
    private const int AnchorLength = 64;
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private readonly string _path;
    private readonly long _startOffset;
    private readonly LogLineFilter _filter;
    private readonly int _chunkSize;

    public LogLineReader(string path, long startOffset, LogLineFilter filter, int chunkSize = DefaultChunkSize)
    {
        _path = path;
        _startOffset = startOffset;
        _filter = filter;
        _chunkSize = chunkSize;
        EndOffset = startOffset;
    }

    /// <summary>Byte offset just past the last complete line; valid once <see cref="Lines"/> is fully enumerated.</summary>
    public long EndOffset { get; private set; }

    public bool DroppedPartialFinalLine { get; private set; }

    public IEnumerable<string> Lines()
    {
        using var stream = Open(_path);
        stream.Seek(_startOffset, SeekOrigin.Begin);
        var buffer = new byte[_chunkSize];
        var filled = 0;
        var position = _startOffset;
        var atFileStart = _startOffset == 0;
        while (true)
        {
            if (filled == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
            var read = stream.Read(buffer, filled, buffer.Length - filled);
            if (read == 0) break;
            filled += read;
            var lineStart = 0;
            while (true)
            {
                // UTF-8 연속 바이트는 0x80 이상이라 0x0A 로 자르면 청크 경계에서 문자가 깨지지 않음
                var newline = Array.IndexOf(buffer, (byte)'\n', lineStart, filled - lineStart);
                if (newline < 0) break;
                var line = new ReadOnlySpan<byte>(buffer, lineStart, newline - lineStart);
                if (atFileStart && line.StartsWith(Utf8Bom)) line = line[Utf8Bom.Length..];
                atFileStart = false;
                if (line.Length > 0 && line[^1] == (byte)'\r') line = line[..^1];
                var keep = _filter(line);
                var text = keep ? Utf8.GetString(line) : null;
                position += newline + 1 - lineStart;
                lineStart = newline + 1;
                EndOffset = position;
                if (text is not null) yield return text;
            }
            if (lineStart > 0)
            {
                Buffer.BlockCopy(buffer, lineStart, buffer, 0, filled - lineStart);
                filled -= lineStart;
            }
        }
        DroppedPartialFinalLine = filled > 0;
    }

    /// <summary>
    /// Fingerprint of the bytes just before <paramref name="offset"/>, so a resume can tell an appended file
    /// from one that was rewritten in place.
    /// </summary>
    public static ulong AnchorAt(string path, long offset)
    {
        if (offset <= 0) return 0;
        using var stream = Open(path);
        var length = (int)Math.Min(AnchorLength, offset);
        if (stream.Length < offset) return 0;
        stream.Seek(offset - length, SeekOrigin.Begin);
        Span<byte> bytes = stackalloc byte[AnchorLength];
        var slice = bytes[..length];
        stream.ReadExactly(slice);
        return Fnv1a(slice);
    }

    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    private static ulong Fnv1a(ReadOnlySpan<byte> bytes)
    {
        var hash = 14695981039346656037UL;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }
}
