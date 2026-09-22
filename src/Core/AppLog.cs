using System.Globalization;

namespace PokeTokenBar.Core;

public sealed class AsyncLineLog : IDisposable
{
    private readonly object _gate = new();
    private readonly Queue<string> _queue = new();
    private readonly Action<string> _sink;
    private readonly Thread _worker;
    private bool _disposed;
    private bool _busy;

    public AsyncLineLog(Action<string> sink)
    {
        _sink = sink;
        _worker = new Thread(DrainLoop) { IsBackground = true, Name = "PokeTokenBar.Log" };
        _worker.Start();
    }

    public void Write(string line)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _queue.Enqueue(line);
            Monitor.Pulse(_gate);
        }
    }

    public void Flush()
    {
        lock (_gate)
        {
            while (_queue.Count > 0 || _busy) Monitor.Wait(_gate);
        }
    }

    public void WriteAndFlush(string line)
    {
        Write(line);
        Flush();
    }

    private void DrainLoop()
    {
        while (true)
        {
            string line;
            lock (_gate)
            {
                while (_queue.Count == 0 && !_disposed) Monitor.Wait(_gate);
                if (_disposed && _queue.Count == 0) return;
                line = _queue.Dequeue();
                _busy = true;
            }
            try
            {
                _sink(line);
            }
            catch
            {
                // diagnostics must never take the process down
            }
            lock (_gate)
            {
                _busy = false;
                Monitor.PulseAll(_gate);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            Monitor.Pulse(_gate);
        }
    }
}

public static class AppLog
{
    private static readonly object Gate = new();
    private static string? _logFilePath;
    private static long _maxBytes = 2 * 1024 * 1024;
    private static AsyncLineLog? _backend;

    public static string LogFilePath
    {
        get
        {
            lock (Gate)
            {
                EnsureConfigured();
                return _logFilePath!;
            }
        }
    }

    public static void Configure(string logFilePath, long maxBytes = 2 * 1024 * 1024)
    {
        lock (Gate)
        {
            _backend?.Dispose();
            _backend = null;
            _logFilePath = logFilePath;
            _maxBytes = maxBytes;
        }
    }

    public static void ResetToDefault()
    {
        lock (Gate)
        {
            _backend?.Dispose();
            _backend = null;
            _logFilePath = null;
        }
    }

    private static void EnsureConfigured()
    {
        if (_backend is not null) return;
        _logFilePath ??= DefaultLogFilePath();
        var path = _logFilePath;
        var maxBytes = _maxBytes;
        _backend = new AsyncLineLog(line => Sink(path, maxBytes, line));
    }

    private static string DefaultLogFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokeTokenBar", "Logs");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "PokeTokenBar.log");
    }

    private static void Sink(string path, long maxBytes, string line)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists && fileInfo.Length > maxBytes)
            {
                var old = Path.Combine(
                    Path.GetDirectoryName(path) ?? ".",
                    Path.GetFileNameWithoutExtension(path) + ".old.log");
                File.Delete(old);
                File.Move(path, old);
            }
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(path, line);
        }
        catch
        {
            // diagnostics must never take the process down
        }
    }

    public static void Flush()
    {
        AsyncLineLog? backend;
        lock (Gate)
        {
            EnsureConfigured();
            backend = _backend;
        }
        backend?.Flush();
    }

    public static void Write(string message)
    {
        AsyncLineLog? backend;
        lock (Gate)
        {
            EnsureConfigured();
            backend = _backend;
        }
        backend?.Write(Formatted(message));
    }

    public static void WriteAndFlush(string message)
    {
        AsyncLineLog? backend;
        lock (Gate)
        {
            EnsureConfigured();
            backend = _backend;
        }
        backend?.WriteAndFlush(Formatted(message));
    }

    private static string Formatted(string message) =>
        $"[{DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}] {message}\n";
}
