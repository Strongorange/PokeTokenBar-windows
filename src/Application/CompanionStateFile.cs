using System.Text.Json;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public static class CompanionStateFile
{
    public const string FileName = "companion-state.json";

    internal static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static string DefaultPath()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("PTB_STATE_DIR");
        var directory = string.IsNullOrWhiteSpace(overrideDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PokeTokenBar")
            : overrideDirectory;
        return Path.Combine(directory, FileName);
    }

    public static CompanionState Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new CompanionState();
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return SaveTransfer.Sanitized(CompanionStateCodec.Read(document.RootElement));
        }
        catch (Exception ex)
        {
            AppLog.Write($"companion state decode failed — starting fresh: {path}: {ex.Message}");
            TryBackupCorrupt(path);
            return new CompanionState();
        }
    }

    private static void TryBackupCorrupt(string path)
    {
        try
        {
            var backup = path + ".corrupt";
            File.Delete(backup);
            File.Move(path, backup);
            AppLog.Write($"companion state backed up to {Path.GetFileName(backup)}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"companion state corrupt backup failed: {ex.Message}");
        }
    }

    public static void Save(string path, CompanionState state)
    {
        var json = CompanionStateCodec.Write(state, SaveDateMode.Iso8601).ToJsonString(IndentedJson);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }
}
