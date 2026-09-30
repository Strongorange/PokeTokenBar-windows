using PokeTokenBar.Core;

namespace Core.Tests;

public class CelebrationQueueTests
{
    [Fact]
    public void DrainReturnsEnqueuedInOrderAndEmpties()
    {
        var queue = new CelebrationQueue();
        queue.Enqueue(new CompanionCelebration(CompanionCelebrationKind.Evolve));
        queue.Enqueue(new CompanionCelebration(CompanionCelebrationKind.CandyXp, Amount: 30_000));

        var drained = queue.Drain();

        Assert.Equal(2, drained.Count);
        Assert.Equal(CompanionCelebrationKind.Evolve, drained[0].Kind);
        Assert.Equal(CompanionCelebrationKind.CandyXp, drained[1].Kind);
        Assert.Equal(30_000, drained[1].Amount);
        Assert.Empty(queue.Drain());
    }

    [Fact]
    public void DrainOnEmptyQueueReturnsNoItems()
    {
        Assert.Empty(new CelebrationQueue().Drain());
    }

    [Fact]
    public void CapacityDropsOldestAndKeepsNewest()
    {
        var queue = new CelebrationQueue();
        for (var i = 0; i < CelebrationQueue.Capacity + 3; i++)
            queue.Enqueue(new CompanionCelebration(CompanionCelebrationKind.Evolve, Amount: i));

        var drained = queue.Drain();

        Assert.Equal(CelebrationQueue.Capacity, drained.Count);
        Assert.Equal(3, drained[0].Amount);
        Assert.Equal(CelebrationQueue.Capacity + 2, drained[^1].Amount);
    }

    [Fact]
    public void ClearDiscardsPendingWithoutDraining()
    {
        var queue = new CelebrationQueue();
        queue.Enqueue(new CompanionCelebration(CompanionCelebrationKind.Hatch, Shiny: true));
        queue.Enqueue(new CompanionCelebration(CompanionCelebrationKind.MintSparkle));

        queue.Clear();

        Assert.Empty(queue.Drain());
    }

    [Fact]
    public void CelebrationsCarryKindFlagsAndAmount()
    {
        var hatch = new CompanionCelebration(CompanionCelebrationKind.Hatch, Shiny: true);
        var ditto = new CompanionCelebration(CompanionCelebrationKind.DittoReveal, Shiny: false);
        var mint = new CompanionCelebration(CompanionCelebrationKind.MintSparkle);

        Assert.True(hatch.Shiny);
        Assert.Equal(0, hatch.Amount);
        Assert.False(ditto.Shiny);
        Assert.Equal(0, mint.Amount);
        Assert.False(mint.Shiny);
    }
}
