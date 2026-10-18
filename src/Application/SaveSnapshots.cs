using System.Globalization;
using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

/// <summary>
/// A point-in-time local snapshot of CompanionState, with summary metadata for listing and
/// rollback. Port of macOS SaveSnapshotManager (#330): snapshots live next to the state file
/// under `<stateDir>/.snapshots/<stateFileName>/`, reuse the save-transfer envelope format,
/// keep the newest ten, and gate automatic snapshots to one per 12 hours.
/// </summary>
public sealed record SaveSnapshot(
    string FileName,
    string FilePath,
    DateTimeOffset Date,
    int DexCount,
    long LifetimeTokens,
    int? CurrentSpeciesID,
    bool CurrentIsShiny,
    PokemonForm? CurrentForm);

public static class SaveSnapshotManager
{
    public const string SnapshotPrefix = "companion-snapshot-";
    public const int MaxSnapshotsToKeep = 10;
    public static readonly TimeSpan MinAutoSnapshotInterval = TimeSpan.FromHours(12);

    private const string StampFormat = "yyyy-MM-dd-HHmmss";

    /// <summary>Directory where local snapshots are stored: `<stateDir>/.snapshots/<stateFileName>/`.</summary>
    public static string SnapshotsDirectory(string stateFilePath)
    {
        var dir = Path.Combine(
            Path.GetDirectoryName(stateFilePath) ?? ".",
            ".snapshots",
            Path.GetFileName(stateFilePath));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Generates a snapshot filename for the given timestamp.</summary>
    public static string SnapshotFileName(DateTimeOffset date) =>
        $"{SnapshotPrefix}{date.ToString(StampFormat, CultureInfo.InvariantCulture)}.json";

    /// <summary>Parses the timestamp from a snapshot filename, ignoring the collision counter.
    /// The full stamp is tried first: its own HHmmss segment would otherwise look like a counter.</summary>
    public static DateTimeOffset? ParseStamp(string fileName)
    {
        if (!fileName.StartsWith(SnapshotPrefix, StringComparison.Ordinal)
            || !fileName.EndsWith(".json", StringComparison.Ordinal))
            return null;
        var raw = fileName[SnapshotPrefix.Length..^".json".Length];
        if (TryParseStamp(raw) is { } direct) return direct;
        var dash = raw.LastIndexOf('-');
        // The collision counter ("-1", "-2", …) is short; HHmmss is six digits.
        if (dash >= 0 && raw.Length - dash - 1 is <= 3
            && int.TryParse(raw[(dash + 1)..], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out _))
            return TryParseStamp(raw[..dash]);
        return null;
    }

    private static DateTimeOffset? TryParseStamp(string raw) =>
        DateTimeOffset.TryParseExact(raw, StampFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;

    /// <summary>Lists all available snapshots, newest first. Undecodable files are skipped.</summary>
    public static List<SaveSnapshot> ListSnapshots(string stateFilePath)
    {
        var dir = SnapshotsDirectory(stateFilePath);
        var results = new List<SaveSnapshot>();
        if (!Directory.Exists(dir)) return results;

        foreach (var file in Directory.EnumerateFiles(dir, SnapshotPrefix + "*.json"))
        {
            if (TryDecodeEnvelope(file) is { } envelope)
            {
                results.Add(Summary(Path.GetFileName(file), file, envelope.State,
                    envelope.ExportedAt));
                continue;
            }
            // Legacy/raw state files (no envelope) still count, dated by their filename stamp.
            if (TryDecodeRawState(file) is not { } state) continue;
            var date = ParseStamp(Path.GetFileName(file)) ?? File.GetCreationTimeUtc(file);
            results.Add(Summary(Path.GetFileName(file), file, state, date));
        }

        return results
            .OrderByDescending(snapshot => snapshot.Date)
            .ThenByDescending(snapshot => snapshot.FileName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Loads and validates the SaveEnvelope from a snapshot file.</summary>
    public static SaveEnvelope LoadEnvelope(string path)
    {
        if (TryDecodeEnvelope(path) is { } envelope) return envelope;
        if (TryDecodeRawState(path) is { } state)
            return new SaveEnvelope
            {
                ExportedAt = ParseStamp(Path.GetFileName(path)) ?? File.GetCreationTimeUtc(path),
                State = SaveTransfer.Sanitized(state)
            };
        throw new SaveTransferException(SaveTransferError.NotASaveFile);
    }

    /// <summary>Loads sanitized CompanionState from a snapshot file, or null when unreadable.</summary>
    public static CompanionState? LoadState(string path)
    {
        try
        {
            return LoadEnvelope(path).State;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Creates a snapshot from the current state and prunes the oldest beyond the limit.</summary>
    public static SaveSnapshot CreateSnapshot(CompanionState state, string stateFilePath,
        DateTimeOffset date, string appVersion, string deviceName)
    {
        var dir = SnapshotsDirectory(stateFilePath);
        var fileName = SnapshotFileName(date);
        var target = Path.Combine(dir, fileName);
        if (File.Exists(target))
        {
            // Same-second snapshot: append a counter instead of overwriting history.
            var counter = 1;
            while (File.Exists(Path.Combine(dir,
                       $"{fileName[..^".json".Length]}-{counter}.json")))
                counter++;
            fileName = $"{fileName[..^".json".Length]}-{counter}.json";
            target = Path.Combine(dir, fileName);
        }
        var data = SaveTransfer.Encode(state, appVersion, deviceName, date);
        var temp = target + ".tmp";
        File.WriteAllBytes(temp, data);
        File.Move(temp, target, overwrite: true);
        PruneOldSnapshots(stateFilePath);
        return Summary(fileName, target, state, date);
    }

    /// <summary>Prunes snapshots keeping only the newest <see cref="MaxSnapshotsToKeep"/>.</summary>
    public static void PruneOldSnapshots(string stateFilePath)
    {
        var snapshots = ListSnapshots(stateFilePath);
        if (snapshots.Count <= MaxSnapshotsToKeep) return;
        foreach (var old in snapshots.Skip(MaxSnapshotsToKeep))
        {
            try
            {
                File.Delete(old.FilePath);
            }
            catch (Exception ex)
            {
                AppLog.Write($"snapshot prune failed for {old.FileName}: {ex.Message}");
            }
        }
    }

    /// <summary>Returns the most recent valid snapshot, if any.</summary>
    public static SaveSnapshot? LatestSnapshot(string stateFilePath) =>
        ListSnapshots(stateFilePath).FirstOrDefault();

    /// <summary>Loads the latest valid snapshot as CompanionState, if available.</summary>
    public static CompanionState? LoadLatestValidSnapshot(string stateFilePath) =>
        LatestSnapshot(stateFilePath) is { } latest ? LoadState(latest.FilePath) : null;

    private static SaveEnvelope? TryDecodeEnvelope(string path)
    {
        try
        {
            if (new FileInfo(path) is { Length: > SaveTransfer.MaxFileBytes }) return null;
            return SaveTransfer.Decode(File.ReadAllBytes(path));
        }
        catch
        {
            return null;
        }
    }

    private static CompanionState? TryDecodeRawState(string path)
    {
        try
        {
            if (new FileInfo(path) is { Length: > SaveTransfer.MaxFileBytes }) return null;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return CompanionStateCodec.Read(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static SaveSnapshot Summary(string fileName, string filePath, CompanionState state,
        DateTimeOffset date) => new(
        fileName,
        filePath,
        date,
        state.Dex.Count,
        state.UsedSinceInstall,
        state.Active?.CurrentID,
        state.Active is { } active
            && (active.DittoDisguise is null || active.DittoRevealed)
            && active.IsShiny,
        state.Active is { } mon
            ? PokemonForms.Resolved(mon.CurrentID, mon.Form)
            : null);
}
