namespace PokeTokenBar.Core;

public enum CompanionCelebrationKind
{
    Hatch,
    Evolve,
    DittoReveal,
    CandyXp,
    MintSparkle
}

/// <summary>
/// One playback-ready celebration event. Mirrors the macOS store's
/// celebration/candyFeedback/mintFeedback trio (CompanionStore.swift 20-38):
/// hatch/ditto carry the shiny flag (disguise hides shiny until reveal),
/// candy carries the applied XP amount, mint is a plain sparkle.
/// </summary>
public sealed record CompanionCelebration(
    CompanionCelebrationKind Kind,
    bool Shiny = false,
    long Amount = 0);

/// <summary>
/// Pending one-shot celebrations drained by the UI after playback. macOS keeps
/// a single slot per feedback type (last write wins); the Windows port queues
/// so a hatch followed by carry-over evolutions plays each cue, capped so an
/// unattended dashboard cannot grow the list without bound.
/// </summary>
public sealed class CelebrationQueue
{
    public const int Capacity = 8;

    private readonly List<CompanionCelebration> _pending = [];

    public void Enqueue(CompanionCelebration celebration)
    {
        if (_pending.Count >= Capacity) _pending.RemoveAt(0);
        _pending.Add(celebration);
    }

    public IReadOnlyList<CompanionCelebration> Drain()
    {
        if (_pending.Count == 0) return [];
        var drained = _pending.ToList();
        _pending.Clear();
        return drained;
    }

    public void Clear() => _pending.Clear();
}
