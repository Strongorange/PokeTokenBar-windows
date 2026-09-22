using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokeTokenBar.Core;

public class DailyUsage
{
    public string Date { get; set; } = "";
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheCreationTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public CostCoverage CostCoverage { get; set; } = CostCoverage.Source;
    public Dictionary<string, long>? Models { get; set; }

    [JsonIgnore]
    public UsageCost UsageCost => new(TotalCost, CostCoverage);

    public DailyUsage() { }

    public DailyUsage(string date, long inputTokens, long outputTokens, long cacheCreationTokens,
        long cacheReadTokens, long totalTokens, double totalCost,
        Dictionary<string, long>? models = null, CostCoverage? costCoverage = null)
    {
        Date = date;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheCreationTokens = cacheCreationTokens;
        CacheReadTokens = cacheReadTokens;
        TotalTokens = totalTokens;
        TotalCost = totalCost;
        Models = models;
        CostCoverage = costCoverage ?? CostCoverage.Source;
    }

    public static DailyUsage? FromJson(JsonElement e)
    {
        long GetLong(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
        double GetDouble(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

        var date = e.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString()!
            : e.TryGetProperty("period", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()! : "";
        var input = GetLong("inputTokens");
        var output = GetLong("outputTokens");
        var cacheW = GetLong("cacheCreationTokens");
        var cacheR = e.TryGetProperty("cacheReadTokens", out var cr) && cr.ValueKind == JsonValueKind.Number
            ? cr.GetInt64()
            : GetLong("cachedInputTokens");
        var total = e.TryGetProperty("totalTokens", out var t) && t.ValueKind == JsonValueKind.Number
            ? t.GetInt64()
            : input + output + cacheW + cacheR;
        var cost = e.TryGetProperty("totalCost", out var tc) && tc.ValueKind == JsonValueKind.Number
            ? tc.GetDouble()
            : GetDouble("costUSD");
        var coverage = e.TryGetProperty("costCoverage", out var cc)
            ? JsonSerializer.Deserialize<CostCoverage>(cc.GetRawText())
            : (total == 0 ? CostCoverage.Empty : cost > 0 ? CostCoverage.Estimate : CostCoverage.Unavailable);
        Dictionary<string, long>? models = null;
        if (e.TryGetProperty("models", out var m) && m.ValueKind == JsonValueKind.Object)
        {
            models = [];
            foreach (var prop in m.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    models[prop.Name] = prop.Value.GetInt64();
        }
        return new DailyUsage(date, input, output, cacheW, cacheR, total, cost, models, coverage);
    }
}

public class BlockUsage
{
    public string Id { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public bool IsActive { get; set; }
    public long TotalTokens { get; set; }
    public double CostUSD { get; set; }
    public CostCoverage CostCoverage { get; set; } = CostCoverage.Source;
    public double? TokensPerMinute { get; set; }

    [JsonIgnore]
    public UsageCost UsageCost => new(CostUSD, CostCoverage);

    [JsonIgnore]
    public DateTimeOffset? EndDate => IsoDates.Date(EndTime);

    public BlockUsage() { }

    public BlockUsage(string id, string startTime, string endTime, bool isActive,
        long totalTokens, double costUSD, double? tokensPerMinute,
        CostCoverage? costCoverage = null)
    {
        Id = id;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
        TotalTokens = totalTokens;
        CostUSD = costUSD;
        CostCoverage = costCoverage ?? CostCoverage.Source;
        TokensPerMinute = tokensPerMinute;
    }
}

public class PeriodUsage
{
    public string Period { get; set; } = "";
    public long TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public CostCoverage CostCoverage { get; set; } = CostCoverage.Source;

    [JsonIgnore]
    public UsageCost UsageCost => new(TotalCost, CostCoverage);

    public PeriodUsage() { }

    public PeriodUsage(string period, long totalTokens, double totalCost,
        CostCoverage? costCoverage = null)
    {
        Period = period;
        TotalTokens = totalTokens;
        TotalCost = totalCost;
        CostCoverage = costCoverage ?? CostCoverage.Source;
    }

    public PeriodUsage(string period, IEnumerable<DailyUsage> daily)
    {
        Period = period;
        TotalTokens = 0;
        TotalCost = 0;
        CostCoverage = CostCoverage.Empty;
        foreach (var day in daily)
        {
            TotalTokens += day.TotalTokens;
            TotalCost += day.TotalCost;
            CostCoverage = CostCoverage.Merge(day.CostCoverage);
        }
    }

    public static PeriodUsage? FromJson(JsonElement e)
    {
        long GetLong(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;

        var period = e.TryGetProperty("week", out var w) && w.ValueKind == JsonValueKind.String ? w.GetString()
            : e.TryGetProperty("month", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()
            : e.TryGetProperty("period", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()
            : null;
        if (period is null) return null;
        var input = GetLong("inputTokens");
        var output = GetLong("outputTokens");
        var cacheW = GetLong("cacheCreationTokens");
        var cacheR = e.TryGetProperty("cacheReadTokens", out var cr) && cr.ValueKind == JsonValueKind.Number
            ? cr.GetInt64()
            : GetLong("cachedInputTokens");
        var total = e.TryGetProperty("totalTokens", out var t) && t.ValueKind == JsonValueKind.Number
            ? t.GetInt64()
            : input + output + cacheW + cacheR;
        var cost = e.TryGetProperty("totalCost", out var tc) && tc.ValueKind == JsonValueKind.Number
            ? tc.GetDouble()
            : e.TryGetProperty("costUSD", out var cu) && cu.ValueKind == JsonValueKind.Number ? cu.GetDouble() : 0;
        var coverage = e.TryGetProperty("costCoverage", out var cc)
            ? JsonSerializer.Deserialize<CostCoverage>(cc.GetRawText())
            : (total == 0 ? CostCoverage.Empty : cost > 0 ? CostCoverage.Estimate : CostCoverage.Unavailable);
        return new PeriodUsage(period, total, cost, coverage);
    }
}

public static class LimitWindowSpan
{
    public const double FiveHour = 5 * 3600;
    public const double SevenDay = 7 * 24 * 3600;

    public static double? FromMinutes(int? minutes) =>
        minutes is > 0 ? minutes.Value * 60.0 : null;

    public static double? FromKind(string? kind) => kind switch
    {
        "session" => FiveHour,
        "weekly_all" or "weekly_scoped" => SevenDay,
        _ => null,
    };
}

public class ProviderSnapshot
{
    public string ProviderId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DailyUsage? Today { get; set; }
    public BlockUsage? ActiveBlock { get; set; }
    public PeriodUsage? WeekTotal { get; set; }
    public PeriodUsage? MonthTotal { get; set; }
    public List<DailyUsage>? MonthDaily { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public bool ReportsCost { get; set; } = true;

    public long TodayTotalTokens => Today?.TotalTokens ?? 0;
}
