namespace PokeTokenBar.Core;

/// <summary>
/// Burn-rate pacing tiers for the companion status line. Thresholds mirror the
/// macOS original (UsageStore.burnTier, tokens per minute combined across all
/// active provider blocks).
/// </summary>
public enum CompanionBurnTier
{
    Idle,
    Normal,
    Fast,
    Blazing
}

public enum CompanionStatusKind
{
    Egg,
    Idle,
    Working,
    Focus,
    Tired,
    Sleep,
    LevelUp
}

/// <summary>
/// Port of the macOS CompanionStore.computeState machine that drives the
/// one-line companion mood ("지금은 집중 모드예요." etc). Pure so the tier
/// boundaries stay unit-tested. macOS source: CompanionStore.swift 1478-1488.
/// </summary>
public static class CompanionStatus
{
    public const double IdleThreshold = 1_000;
    public const double WorkingThreshold = 100_000;
    public const double FocusThreshold = 400_000;

    public static CompanionBurnTier BurnTier(double burnPerMinute)
    {
        if (burnPerMinute <= IdleThreshold) return CompanionBurnTier.Idle;
        if (burnPerMinute < WorkingThreshold) return CompanionBurnTier.Normal;
        if (burnPerMinute < FocusThreshold) return CompanionBurnTier.Fast;
        return CompanionBurnTier.Blazing;
    }

    public static CompanionStatusKind Compute(bool hasActive, bool levelUpWindow,
        bool limitWarning, bool hasUsageData, long todayTokens, double burnPerMinute)
    {
        if (!hasActive) return CompanionStatusKind.Egg;
        if (levelUpWindow) return CompanionStatusKind.LevelUp;
        if (limitWarning) return CompanionStatusKind.Tired;
        if (!hasUsageData || todayTokens == 0) return CompanionStatusKind.Sleep;
        return BurnTier(burnPerMinute) switch
        {
            CompanionBurnTier.Idle => CompanionStatusKind.Idle,
            CompanionBurnTier.Normal => CompanionStatusKind.Working,
            _ => CompanionStatusKind.Focus
        };
    }
}
