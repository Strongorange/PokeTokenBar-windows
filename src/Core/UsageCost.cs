namespace PokeTokenBar.Core;

public readonly record struct CostCoverage(bool Reported, bool Estimated, bool Unknown)
{
    public static readonly CostCoverage Empty = new(false, false, false);
    public static readonly CostCoverage Source = new(true, false, false);
    public static readonly CostCoverage Estimate = new(false, true, false);
    public static readonly CostCoverage Unavailable = new(false, false, true);

    public bool HasKnown => Reported || Estimated;

    public CostCoverage Merge(CostCoverage other) =>
        new(Reported || other.Reported, Estimated || other.Estimated, Unknown || other.Unknown);
}

public readonly record struct UsageCost(double Amount, CostCoverage Coverage)
{
    public static readonly UsageCost Zero = new(0, CostCoverage.Empty);

    public UsageCost Add(UsageCost other) =>
        new(Amount + other.Amount, Coverage.Merge(other.Coverage));

    public string Text(string unavailableLabel, bool compact = false)
    {
        if (Coverage.Unknown && !Coverage.HasKnown) return compact ? "$—" : unavailableLabel;
        return compact ? TokenFormatter.CostCompact(Amount) : TokenFormatter.Cost(Amount);
    }
}
