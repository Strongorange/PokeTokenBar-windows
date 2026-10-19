using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

/// <summary>
/// Persists the usage ledger next to the usage caches. The ledger is a rebuildable cache, not
/// user state: a corrupt file is moved to `.corrupt` and recording starts fresh (the raw-log
/// scan window refills it on the next refresh, though days before the window become unknown
/// again — the year-over-year comparison recovers as coverage rebuilds).
/// </summary>
public static class UsageLedgerFile
{
    public const string FileName = "usage-ledger.json";

    public static string PathFor(string cacheDirectory) =>
        System.IO.Path.Combine(cacheDirectory, FileName);

    public static UsageLedger Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new UsageLedger();
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return UsageLedger.Read(document.RootElement);
        }
        catch (Exception ex)
        {
            AppLog.Write($"usage ledger load failed — recording starts fresh: {path}: {ex.Message}");
            TryBackupCorrupt(path);
            return new UsageLedger();
        }
    }

    private static void TryBackupCorrupt(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var backup = path + ".corrupt";
            File.Delete(backup);
            File.Move(path, backup);
            AppLog.Write($"usage ledger backed up to {Path.GetFileName(backup)}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"usage ledger corrupt backup failed: {ex.Message}");
        }
    }

    public static void Save(string path, UsageLedger ledger)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, ledger.Write().ToJsonString(CompanionStateFile.IndentedJson));
        File.Move(temp, path, overwrite: true);
    }
}
