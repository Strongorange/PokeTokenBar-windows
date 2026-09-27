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
}
