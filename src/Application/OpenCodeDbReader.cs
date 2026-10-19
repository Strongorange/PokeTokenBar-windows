using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application;

public sealed class OpenCodeDbReader
{
    private const string MessageQuery =
        "SELECT id, time_created, data FROM message WHERE time_created >= $floor";

    /// <summary>
    /// OpenCode v2 writes assistant usage to the event-sourced session_message projection;
    /// the older message table remains useful for v1 history. The global time index keeps
    /// this query bounded to the active usage window (upstream #363).
    /// </summary>
    private const string SessionMessageQuery =
        "SELECT id, time_created, data FROM session_message WHERE type = 'assistant' AND time_created >= $floor";

    private const int CommandTimeoutSeconds = 15;

    private sealed record RemoteKey(FileFingerprint Db, FileFingerprint? Wal);
    private sealed record RemoteCache(RemoteKey Key, List<UsageEntry> Entries);

    private readonly Dictionary<string, RemoteCache> _remoteCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public List<UsageEntry> ReadEntries(
        string rootPath,
        IFileSystemSource fileSystem,
        long floorMillis,
        TimeZoneInfo timeZone,
        string scratchDirectory)
    {
        var dbPath = Path.Combine(rootPath, "opencode.db");
        FileFingerprint? dbFingerprint;
        try
        {
            dbFingerprint = fileSystem.GetFingerprint(dbPath);
        }
        catch (Exception ex)
        {
            AppLog.Write($"opencode db stat failed: {dbPath}: {ex.Message}");
            return [];
        }
        if (dbFingerprint is null) return [];

        if (!IsRemote(dbPath))
            return QueryEntries(dbPath, rootPath, floorMillis, timeZone) ?? [];

        FileFingerprint? walFingerprint = null;
        try
        {
            walFingerprint = fileSystem.GetFingerprint(dbPath + "-wal");
        }
        catch (Exception ex)
        {
            AppLog.Write($"opencode wal stat failed: {dbPath}: {ex.Message}");
        }
        var key = new RemoteKey(dbFingerprint.Value, walFingerprint);
        lock (_gate)
        {
            if (_remoteCache.TryGetValue(dbPath, out var hit) && hit.Key == key)
                return hit.Entries;
        }
        var entries = ReadRemoteCopy(dbPath, rootPath, floorMillis, timeZone, scratchDirectory, walFingerprint is not null);
        if (entries is null) return [];
        lock (_gate)
        {
            _remoteCache[dbPath] = new RemoteCache(key, entries);
        }
        return entries;
    }

    internal static bool IsRemote(string path) =>
        path.StartsWith(@"\\", StringComparison.Ordinal);

    private List<UsageEntry>? ReadRemoteCopy(
        string dbPath, string rootPath, long floorMillis, TimeZoneInfo timeZone,
        string scratchDirectory, bool sourceHasWal)
    {
        string copyPath;
        try
        {
            var directory = Path.Combine(scratchDirectory, ScratchName(dbPath));
            Directory.CreateDirectory(directory);
            copyPath = Path.Combine(directory, "opencode.db");
            File.Copy(dbPath, copyPath, true);
            var walCopy = copyPath + "-wal";
            if (sourceHasWal)
                File.Copy(dbPath + "-wal", walCopy, true);
            else if (File.Exists(walCopy))
                File.Delete(walCopy);
        }
        catch (Exception ex)
        {
            AppLog.Write($"opencode remote db copy failed: {dbPath}: {ex.Message}");
            return null;
        }
        return QueryEntries(copyPath, rootPath, floorMillis, timeZone);
    }

    private List<UsageEntry>? QueryEntries(string dbPath, string sourceKey, long floorMillis, TimeZoneInfo timeZone)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
                DefaultTimeout = CommandTimeoutSeconds,
            };
            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            var entries = ReadRows(connection, MessageQuery, floorMillis, sourceKey, timeZone,
                (id, json, fallback) => OpenCodeLogParser.ParseMessage(id, json, sourceKey, fallback, timeZone));
            try
            {
                entries.AddRange(ReadRows(connection, SessionMessageQuery, floorMillis, sourceKey, timeZone,
                    (id, json, fallback) =>
                        OpenCodeLogParser.ParseSessionMessage(id, json, sourceKey, fallback, timeZone)));
            }
            catch (SqliteException)
            {
                // A v1 database has no session_message table — its history lives in `message`.
            }
            return UsageAggregation.DedupKeepMax(entries);
        }
        catch (Exception ex)
        {
            AppLog.Write($"opencode db read failed: {dbPath}: {ex.Message}");
            return null;
        }
    }

    private static List<UsageEntry> ReadRows(
        SqliteConnection connection, string sql, long floorMillis, string sourceKey,
        TimeZoneInfo timeZone, Func<string, string, long?, UsageEntry?> parse)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$floor", floorMillis);
        using var reader = command.ExecuteReader();
        List<UsageEntry> entries = [];
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(2)) continue;
            var id = reader.GetString(0);
            var dataJson = reader.GetString(2);
            long? fallbackMillis = reader.IsDBNull(1) ? null : reader.GetInt64(1);
            if (parse(id, dataJson, fallbackMillis) is not { } entry)
                continue;
            entries.Add(entry);
        }
        return entries;
    }

    private static string ScratchName(string dbPath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dbPath.ToUpperInvariant()));
        return Convert.ToHexString(hash)[..16];
    }
}
