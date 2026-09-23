using System.Text;

namespace PokeTokenBar.Providers;

public static class CodexRolloutProbe
{
    public const int DefaultByteLimit = 1 << 20;
    private const int ChunkSize = 64 * 1024;
    private const string SessionMetaMarker = "session_meta";
    private const string TokenCountMarker = "token_count";
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private enum LineDecision
    {
        KeepScanning,
        ReturnNull,
        ReturnId,
    }

    public static string? ProbeFile(string path, int byteLimit = DefaultByteLimit)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Probe(stream, byteLimit);
    }

    public static string? Probe(Stream stream, int byteLimit = DefaultByteLimit)
    {
        var residual = new MemoryStream();
        var chunk = new byte[ChunkSize];
        long read = 0;
        while (read < byteLimit)
        {
            var want = (int)Math.Min(ChunkSize, byteLimit - read);
            var count = stream.Read(chunk, 0, want);
            if (count == 0) return FinalOutcome(residual);
            read += count;
            residual.Write(chunk, 0, count);
            var buffer = residual.GetBuffer();
            var length = (int)residual.Length;
            var lineStart = 0;
            while (lineStart < length)
            {
                var newline = Array.IndexOf(buffer, (byte)'\n', lineStart, length - lineStart);
                if (newline < 0) break;
                var (decision, id) = DecideLine(buffer, lineStart, newline - lineStart);
                if (decision == LineDecision.ReturnId) return id;
                if (decision == LineDecision.ReturnNull) return null;
                lineStart = newline + 1;
            }
            if (lineStart > 0)
            {
                var remaining = length - lineStart;
                var kept = new byte[remaining];
                Array.Copy(buffer, lineStart, kept, 0, remaining);
                residual.SetLength(0);
                if (remaining > 0) residual.Write(kept, 0, remaining);
            }
        }
        return FinalOutcome(residual);
    }

    private static string? FinalOutcome(MemoryStream residual)
    {
        var (decision, id) = DecideLine(residual.GetBuffer(), 0, (int)residual.Length);
        return decision == LineDecision.ReturnId ? id : null;
    }

    private static (LineDecision Decision, string? Id) DecideLine(byte[] buffer, int start, int length)
    {
        if (length == 0) return (LineDecision.KeepScanning, null);
        string line;
        try
        {
            line = StrictUtf8.GetString(buffer, start, length);
        }
        catch (DecoderFallbackException)
        {
            return (LineDecision.ReturnNull, null);
        }
        if (line.Contains(SessionMetaMarker, StringComparison.Ordinal) &&
            CodexLogParser.TryParseSessionMeta(line) is { } meta)
            return meta.Id is { } id ? (LineDecision.ReturnId, id) : (LineDecision.ReturnNull, null);
        if (line.Contains(TokenCountMarker, StringComparison.Ordinal))
            return (LineDecision.ReturnNull, null);
        return (LineDecision.KeepScanning, null);
    }
}
