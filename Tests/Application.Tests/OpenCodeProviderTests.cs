using Microsoft.Data.Sqlite;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;
using PokeTokenBar.Providers;

namespace PokeTokenBar.Application.Tests;

[Collection("Application diagnostics")]
public class OpenCodeProviderTests : IDisposable
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir;
    private readonly string _profile;
    private readonly string _cacheDir;

    public OpenCodeProviderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-opencode-tests", Guid.NewGuid().ToString("N"));
        _profile = Path.Combine(_dir, "profile");
        _cacheDir = Path.Combine(_dir, "cache");
        Directory.CreateDirectory(_profile);
        Directory.CreateDirectory(_cacheDir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string OpenCodeRoot => Path.Combine(_profile, ".local", "share", "opencode");

    private UsageRefreshService BuildService() =>
        new(new UsageRefreshOptions
        {
            CacheDirectory = _cacheDir,
            TimeZone = Tz,
            Clock = () => Now,
            RootSource = new DiscoveryUsageRootSource(
                new UsageRootOptions { UserProfile = _profile, IncludeWsl = false }),
            FileSystem = new CachedFileSystemSource(PhysicalFileSystemSource.Instance),
        });

    private void WriteOpenCodeDb(params (string Id, long Created, string Data)[] rows)
    {
        WriteOpenCodeDb(withSessionMessageTable: false, rows);
    }

    private void WriteOpenCodeDb(bool withSessionMessageTable,
        params (string Id, long Created, string Data)[] rows)
    {
        Directory.CreateDirectory(OpenCodeRoot);
        var dbPath = Path.Combine(OpenCodeRoot, "opencode.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = dbPath };
        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE message (
                id text PRIMARY KEY,
                session_id text NOT NULL,
                time_created integer NOT NULL,
                time_updated integer NOT NULL,
                data text NOT NULL
            )
            """;
        create.ExecuteNonQuery();
        if (withSessionMessageTable)
        {
            var createV2 = connection.CreateCommand();
            createV2.CommandText = """
                CREATE TABLE session_message (
                    id text PRIMARY KEY,
                    session_id text NOT NULL,
                    type text NOT NULL,
                    seq integer NOT NULL,
                    time_created integer NOT NULL,
                    time_updated integer NOT NULL,
                    data text NOT NULL
                )
                """;
            createV2.ExecuteNonQuery();
        }
        foreach (var row in rows)
        {
            var insert = connection.CreateCommand();
            insert.CommandText =
                "INSERT INTO message (id, session_id, time_created, time_updated, data) VALUES ($id, $session, $created, $updated, $data)";
            insert.Parameters.AddWithValue("$id", row.Id);
            insert.Parameters.AddWithValue("$session", "ses_fixture");
            insert.Parameters.AddWithValue("$created", row.Created);
            insert.Parameters.AddWithValue("$updated", row.Created);
            insert.Parameters.AddWithValue("$data", row.Data);
            insert.ExecuteNonQuery();
        }
    }

    private void WriteSessionMessages(params (string Id, string Type, long Created, string Data)[] rows)
    {
        var dbPath = Path.Combine(OpenCodeRoot, "opencode.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = dbPath };
        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        foreach (var row in rows)
        {
            var insert = connection.CreateCommand();
            insert.CommandText =
                "INSERT INTO session_message (id, session_id, type, seq, time_created, time_updated, data) " +
                "VALUES ($id, $session, $type, $seq, $created, $updated, $data)";
            insert.Parameters.AddWithValue("$id", row.Id);
            insert.Parameters.AddWithValue("$session", "ses_fixture");
            insert.Parameters.AddWithValue("$type", row.Type);
            insert.Parameters.AddWithValue("$seq", 0);
            insert.Parameters.AddWithValue("$created", row.Created);
            insert.Parameters.AddWithValue("$updated", row.Created);
            insert.Parameters.AddWithValue("$data", row.Data);
            insert.ExecuteNonQuery();
        }
    }

    private static string AssistantData(
        string model, long input, long output, long reasoning, long cacheRead, long created, long? completed) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["role"] = "assistant",
            ["modelID"] = model,
            ["providerID"] = "opencode",
            ["cost"] = 0,
            ["tokens"] = new Dictionary<string, object?>
            {
                ["input"] = input,
                ["output"] = output,
                ["reasoning"] = reasoning,
                ["cache"] = new Dictionary<string, object?> { ["write"] = 0L, ["read"] = cacheRead },
            },
            ["time"] = new Dictionary<string, object?> { ["created"] = created, ["completed"] = completed },
            ["finish"] = "stop",
        });

    [Fact]
    public async Task OpenCodeRootsFeedCombinedTotals()
    {
        WriteOpenCodeDb(
            ("msg_today", 1790164800000,
                AssistantData("gpt-5.3-codex", 1000, 200, 100, 50, 1790164800000, 1790164850000)),
            ("msg_earlier", 1788830400000,
                AssistantData("gpt-5.3-codex", 500, 100, 0, 0, 1788830400000, 1788830500000)));

        var state = await BuildService().RefreshAsync();

        var opencode = state.Provider("opencode")!;
        Assert.True(opencode.Available);
        Assert.Equal(1350, opencode.TodayTokens);
        Assert.Equal(1950, opencode.MonthTokens);
        Assert.Equal(1350, state.TodayTokens);
        Assert.Equal(1950, state.MonthTokens);
    }

    [Fact]
    public async Task MissingOpenCodeRootMarksProviderUnavailable()
    {
        var state = await BuildService().RefreshAsync();

        var opencode = state.Provider("opencode")!;
        Assert.False(opencode.Available);
        Assert.Equal(0, opencode.TodayTokens);
        Assert.Equal(0, state.TodayTokens);
    }

    [Fact]
    public async Task RootWithoutDatabaseIsAvailableWithZeroUsage()
    {
        Directory.CreateDirectory(OpenCodeRoot);

        var state = await BuildService().RefreshAsync();

        var opencode = state.Provider("opencode")!;
        Assert.True(opencode.Available);
        Assert.Equal(0, opencode.TodayTokens);
    }

    [Fact]
    public async Task EntriesOlderThanTheScanFloorAreIgnored()
    {
        WriteOpenCodeDb(
            ("msg_old", 1650000000000,
                AssistantData("gpt-5.3-codex", 1000, 1000, 1000, 1000, 1650000000000, 1650000001000)));

        var state = await BuildService().RefreshAsync();

        var opencode = state.Provider("opencode")!;
        Assert.True(opencode.Available);
        Assert.Equal(0, opencode.TodayTokens);
        Assert.Equal(0, opencode.MonthTokens);
    }

    [Fact]
    public async Task RepeatRefreshDoesNotDoubleCount()
    {
        WriteOpenCodeDb(
            ("msg_today", 1790164800000,
                AssistantData("gpt-5.3-codex", 1000, 200, 100, 50, 1790164800000, 1790164850000)));
        var service = BuildService();

        var first = await service.RefreshAsync();
        var second = await service.RefreshAsync();

        Assert.Equal(1350, first.Provider("opencode")!.TodayTokens);
        Assert.Equal(1350, second.Provider("opencode")!.TodayTokens);
    }

    [Fact]
    public void CorruptDatabaseYieldsNoEntriesWithoutThrowing()
    {
        Directory.CreateDirectory(OpenCodeRoot);
        File.WriteAllText(Path.Combine(OpenCodeRoot, "opencode.db"), "this is not a sqlite database");

        var reader = new OpenCodeDbReader();
        var entries = reader.ReadEntries(
            OpenCodeRoot,
            PhysicalFileSystemSource.Instance,
            DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds(),
            Tz,
            Path.Combine(_cacheDir, "opencode"));

        Assert.Empty(entries);
        Assert.Contains("opencode db read failed", Diagnostics.Read(_dir));
    }

    [Fact]
    public void ReaderFiltersByFloorAndPrefixesEntryIdsWithTheRoot()
    {
        WriteOpenCodeDb(
            ("msg_a", 1790164800000,
                AssistantData("gpt-5.3-codex", 1000, 200, 100, 50, 1790164800000, 1790164850000)),
            ("msg_zero", 1790164900000,
                AssistantData("gpt-5.3-codex", 0, 0, 0, 0, 1790164900000, 1790164950000)));

        var reader = new OpenCodeDbReader();
        var entries = reader.ReadEntries(
            OpenCodeRoot,
            PhysicalFileSystemSource.Instance,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            Tz,
            Path.Combine(_cacheDir, "opencode"));

        Assert.Equal(2, entries.Count);
        var entry = entries.Single(e => e.Id.EndsWith("|msg_a", StringComparison.Ordinal));
        Assert.Equal($"opencode|{OpenCodeRoot}|msg_a", entry.Id);
        Assert.Equal(1350, entry.Total);
        Assert.Equal("2026-09-23", entry.LocalDay);
        Assert.Contains(entries, e => e.Id.EndsWith("|msg_zero", StringComparison.Ordinal));
    }

    /// <summary>
    /// Port of testOpenCodeReadsV2SessionMessagesAndMergesLegacyUsage (upstream #363): v2
    /// session_message rows join the v1 message history, user rows stay out, and reasoning
    /// folds into output.
    /// </summary>
    [Fact]
    public void V2SessionMessagesMergeWithLegacyUsage()
    {
        WriteOpenCodeDb(withSessionMessageTable: true,
            ("msg-v1", 1767312000000,
                """
                {"id":"msg-v1","sessionID":"session-v1","providerID":"anthropic",
                 "modelID":"claude-sonnet-4-20250514","time":{"created":1767312000000},
                 "tokens":{"input":100,"output":50,"total":180,"cache":{"read":10,"write":20}},
                 "cost":0.25}
                """));
        WriteSessionMessages(
            ("msg-v2", "assistant", 1767312000000,
                """
                {"model":{"id":"gpt-5","providerID":"openai"},"time":{"created":1767312000000},
                 "tokens":{"input":100,"output":50,"reasoning":30,"cache":{"read":10,"write":20}},
                 "cost":0.5}
                """),
            ("msg-user", "user", 1767312000000,
                """
                {"time":{"created":1767312000000},"text":"not usage"}
                """));

        var reader = new OpenCodeDbReader();
        var entries = reader.ReadEntries(
            OpenCodeRoot,
            PhysicalFileSystemSource.Instance,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            Tz,
            Path.Combine(_cacheDir, "opencode"));

        Assert.Equal(2, entries.Count);
        var v1 = entries.Single(e => e.Id.EndsWith("|msg-v1", StringComparison.Ordinal));
        Assert.Equal(180, v1.Total);
        Assert.Equal(0.25, v1.ExplicitCost);

        var v2 = entries.Single(e => e.Id.EndsWith("|msg-v2", StringComparison.Ordinal));
        Assert.Equal("gpt-5", v2.Model);
        Assert.Equal(100, v2.Input);
        Assert.Equal(80, v2.Output);
        Assert.Equal(10, v2.CacheRead);
        Assert.Equal(20, v2.CacheWrite);
        Assert.Equal(210, v2.Total);
        Assert.Equal(0.5, v2.ExplicitCost);
    }

    /// <summary>A v1 database has no session_message table — the missing-table read stays quiet.</summary>
    [Fact]
    public void V1OnlyDatabaseSkipsSessionMessageQueryQuietly()
    {
        WriteOpenCodeDb(
            ("msg_a", 1790164800000,
                AssistantData("gpt-5.3-codex", 1000, 200, 100, 50, 1790164800000, 1790164850000)));

        var reader = new OpenCodeDbReader();
        var entries = reader.ReadEntries(
            OpenCodeRoot,
            PhysicalFileSystemSource.Instance,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            Tz,
            Path.Combine(_cacheDir, "opencode"));

        Assert.Single(entries);
        Assert.DoesNotContain("opencode db read failed", Diagnostics.Read(_dir));
    }

    /// <summary>The same id in both tables counts once, keeping the larger totals (keep-max).</summary>
    [Fact]
    public void DuplicateIdsAcrossTablesCountOnceWithMaxTotals()
    {
        WriteOpenCodeDb(withSessionMessageTable: true,
            ("msg-both", 1790164800000,
                AssistantData("gpt-5", 100, 100, 0, 0, 1790164800000, 1790164850000)));
        WriteSessionMessages(
            ("msg-both", "assistant", 1790164800000,
                """
                {"model":{"id":"gpt-5","providerID":"openai"},"time":{"created":1790164800000},
                 "tokens":{"input":500,"output":500,"cache":{"read":0,"write":0}}}
                """));

        var reader = new OpenCodeDbReader();
        var entries = reader.ReadEntries(
            OpenCodeRoot,
            PhysicalFileSystemSource.Instance,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            Tz,
            Path.Combine(_cacheDir, "opencode"));

        var entry = Assert.Single(entries);
        Assert.Equal(1000, entry.Total);
    }
}
