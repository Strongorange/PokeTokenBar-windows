using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokeTokenBar.Core;

public class SaveEnvelope
{
    public const string FormatID = "poketokenbar.save";
    public const int SchemaVersion = 2;

    public string Format { get; set; } = FormatID;
    public int Schema { get; set; }
    public string AppVersion { get; set; } = "";
    public DateTimeOffset ExportedAt { get; set; }
    public string SourceDevice { get; set; } = "";
    public CompanionState State { get; set; } = new();
}

public readonly record struct SaveSummary(int DexCount, long LifetimeTokens)
{
    public static SaveSummary From(CompanionState state) =>
        new(state.Dex.Count, state.UsedSinceInstall);
}

public enum SaveTransferError
{
    NotASaveFile,
    NewerSchema,
    FileTooLarge,
    BackupFailed
}

public class SaveTransferException(SaveTransferError error, int foundSchema = 0, int supportedSchema = 0,
    long bytes = 0, long limit = 0) : Exception(error.ToString())
{
    public SaveTransferError Error { get; } = error;
    public int FoundSchema { get; } = foundSchema;
    public int SupportedSchema { get; } = supportedSchema;
    public long Bytes { get; } = bytes;
    public long Limit { get; } = limit;
}

public static class SaveTransfer
{
    public const long MaxFileBytes = 8 * 1024 * 1024;
    public const long MaxTokenValue = 1_000_000_000_000_000;
    public const int BackupsToKeep = 5;
    public const string BackupFilePrefix = "companion-state.pre-import-";

    public static string SuggestedFileName(DateTimeOffset date) =>
        $"PokeTokenBar-Save-{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.json";

    public static string BackupFileName(DateTimeOffset date) =>
        $"companion-state.pre-import-{date.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture)}.json";

    public static byte[] Encode(CompanionState state, string appVersion, string deviceName, DateTimeOffset now)
    {
        var envelope = new JsonObject
        {
            ["appVersion"] = appVersion,
            ["exportedAt"] = CompanionStateCodec.WriteDate(now, SaveDateMode.Iso8601),
            ["format"] = SaveEnvelope.FormatID,
            ["schema"] = SaveEnvelope.SchemaVersion,
            ["sourceDevice"] = deviceName,
            ["state"] = SortDeep(CompanionStateCodec.Write(state, SaveDateMode.Iso8601))
        };
        var sorted = SortDeep(envelope);
        return System.Text.Encoding.UTF8.GetBytes(sorted.ToJsonString(JsonOptions.Indented));
    }

    private static JsonObject SortDeep(JsonObject o)
    {
        var sorted = new JsonObject();
        foreach (var prop in o.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            sorted[prop.Key] = prop.Value switch
            {
                JsonObject nested => SortDeep(nested),
                _ => prop.Value?.DeepClone()
            };
        return sorted;
    }

    internal static class JsonOptions
    {
        public static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
    }

    public static SaveEnvelope Decode(byte[] data)
    {
        if (data.Length > MaxFileBytes)
            throw new SaveTransferException(SaveTransferError.FileTooLarge, bytes: data.Length, limit: MaxFileBytes);
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("format", out var format)
            || format.ValueKind != JsonValueKind.String
            || format.GetString() != SaveEnvelope.FormatID)
            throw new SaveTransferException(SaveTransferError.NotASaveFile);
        var schema = root.TryGetProperty("schema", out var s) && s.ValueKind == JsonValueKind.Number
            ? s.GetInt32() : 0;
        if (schema > SaveEnvelope.SchemaVersion)
            throw new SaveTransferException(SaveTransferError.NewerSchema, schema, SaveEnvelope.SchemaVersion);
        var state = CompanionStateCodec.Read(root.TryGetProperty("state", out var stateElement)
            ? stateElement
            : throw new SaveTransferException(SaveTransferError.NotASaveFile));
        return new SaveEnvelope
        {
            Format = SaveEnvelope.FormatID,
            Schema = schema,
            AppVersion = root.TryGetProperty("appVersion", out var av) && av.ValueKind == JsonValueKind.String
                ? av.GetString()! : "",
            ExportedAt = root.TryGetProperty("exportedAt", out var ea)
                ? CompanionStateCodec.ReadDate(ea) ?? DateTimeOffset.UtcNow
                : DateTimeOffset.UtcNow,
            SourceDevice = root.TryGetProperty("sourceDevice", out var sd) && sd.ValueKind == JsonValueKind.String
                ? sd.GetString()! : "",
            State = Sanitized(state)
        };
    }

    public static CompanionState Sanitized(CompanionState state)
    {
        long ClampToken(long v) => Math.Clamp(v, 0, MaxTokenValue);

        var s = state;
        s.UsedSinceInstall = ClampToken(s.UsedSinceInstall);
        s.SpentTokens = ClampToken(s.SpentTokens);
        s.EggUsage = ClampToken(s.EggUsage);
        if (s.ClaimedTodayTokensByProvider is { } claimed)
        {
            s.ClaimedTodayTokensByProvider = claimed.ToDictionary(
                kv => kv.Key, kv => ClampToken(kv.Value));
        }
        if (s.Active is not null)
        {
            s.EggTier = null;
            s.PendingHatchID = null;
        }
        s.PendingUnownForm = UnownForms.Resolved(s.PendingHatchID ?? 0, s.PendingUnownForm);
        if (s.EggTier?.CaptureRateCeiling() is null) s.EggTier = null;
        if (s.Active is { } active)
        {
            active.UsedAtStage = ClampToken(active.UsedAtStage);
            active.TotalForms = Math.Clamp(active.TotalForms, 1, 12);
            active.StageIndex = Math.Clamp(active.StageIndex, 0, Math.Max(0, active.PathIDs.Count - 1));
            active.Profile?.Sanitize();
            s.Active = active;
        }
        foreach (var entry in s.Dex) entry.Profile?.Sanitize();
        s.ReconcileRepresentativeSelection();
        return s;
    }

    public static CompanionState RebasedForThisDevice(CompanionState imported, CompanionState current,
        IReadOnlyDictionary<string, long> todayTokensByProvider, string todayDate, bool hasUsageData)
    {
        var state = imported;
        state.Language = current.Language;
        state.CandyGrantTier = MergedGrantTier(imported.CandyGrantTier, current.CandyGrantTier);
        state.CandyFeatureSeeded = imported.CandyFeatureSeeded || current.CandyFeatureSeeded;
        var hasCurrentProviderData = hasUsageData && todayTokensByProvider.Count > 0;
        if (hasCurrentProviderData)
        {
            state.InstallBaselineSet = true;
            state.ClaimedTodayTokensByProvider = new Dictionary<string, long>(todayTokensByProvider);
            state.LastDate = todayDate;
        }
        else
        {
            state.InstallBaselineSet = false;
            state.ClaimedTodayTokensByProvider = null;
            state.LastDate = "";
        }
        return state;
    }

    public static Dictionary<string, int> MergedGrantTier(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        var merged = new Dictionary<string, int>(a);
        foreach (var (key, tier) in b)
            merged[key] = merged.TryGetValue(key, out var existing) ? Math.Max(existing, tier) : tier;
        return merged;
    }
}
