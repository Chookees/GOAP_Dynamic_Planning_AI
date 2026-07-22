using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.WorldState;
using Xunit;

namespace DynamicPlanningAI.UnitTests.WorldState;

public sealed class SymbolicWorldStateTests
{
    [Fact]
    public void Set_And_TryGet_RoundTrip()
    {
        SymbolicWorldState state = new();
        Assert.Equal(OperationStatus.Success, state.Set(WorldFactId.InCover, 1));
        Assert.True(state.TryGet(WorldFactId.InCover, out int value));
        Assert.Equal(1, value);
        Assert.True(state.Contains(WorldFactId.InCover));
    }

    [Fact]
    public void Clear_RemovesFact()
    {
        SymbolicWorldState state = new();
        state.Set(WorldFactId.TargetSelected, 7);
        Assert.Equal(OperationStatus.Success, state.Clear(WorldFactId.TargetSelected));
        Assert.False(state.Contains(WorldFactId.TargetSelected));
        Assert.Equal(OperationStatus.NoOp, state.Clear(WorldFactId.TargetSelected));
    }

    [Fact]
    public void Satisfies_RequiresMatchingFacts()
    {
        SymbolicWorldState world = new();
        SymbolicWorldState requirements = new();
        world.Set(WorldFactId.InCover, 1);
        world.Set(WorldFactId.WeaponLoaded, 1);
        requirements.Set(WorldFactId.InCover, 1);

        Assert.True(world.Satisfies(requirements));

        requirements.Set(WorldFactId.WeaponLoaded, 2);
        Assert.False(world.Satisfies(requirements));
    }

    [Fact]
    public void ConflictsWith_DetectsUnequalSharedFacts()
    {
        SymbolicWorldState left = new();
        SymbolicWorldState right = new();
        left.Set(WorldFactId.AtTacticalPoint, 3);
        right.Set(WorldFactId.AtTacticalPoint, 4);
        Assert.True(left.ConflictsWith(right));

        right.Set(WorldFactId.AtTacticalPoint, 3);
        Assert.False(left.ConflictsWith(right));
    }

    [Fact]
    public void RegressThrough_RemovesSatisfiedAndMergesPreconditions()
    {
        SymbolicWorldState requirements = new();
        SymbolicWorldState effects = new();
        SymbolicWorldState preconditions = new();
        SymbolicWorldState destination = new();

        requirements.Set(WorldFactId.InCover, 1);
        requirements.Set(WorldFactId.WeaponLoaded, 1);
        effects.Set(WorldFactId.InCover, 1);
        preconditions.Set(WorldFactId.CoverValid, 1);

        Assert.Equal(
            OperationStatus.Success,
            requirements.RegressThrough(effects, 0UL, preconditions, destination));

        Assert.False(destination.Contains(WorldFactId.InCover));
        Assert.True(destination.TryGet(WorldFactId.WeaponLoaded, out int loaded));
        Assert.Equal(1, loaded);
        Assert.True(destination.TryGet(WorldFactId.CoverValid, out int cover));
        Assert.Equal(1, cover);
    }

    [Fact]
    public void RegressThrough_RejectsConflictingEffects()
    {
        SymbolicWorldState requirements = new();
        SymbolicWorldState effects = new();
        SymbolicWorldState preconditions = new();
        SymbolicWorldState destination = new();

        requirements.Set(WorldFactId.InCover, 1);
        effects.Set(WorldFactId.InCover, 2);

        Assert.Equal(
            OperationStatus.Conflict,
            requirements.RegressThrough(effects, 0UL, preconditions, destination));
    }

    [Fact]
    public void ComputeHash_IsDeterministicAndSensitive()
    {
        SymbolicWorldState a = new();
        SymbolicWorldState b = new();
        a.Set(WorldFactId.TargetKnown, 1);
        a.Set(WorldFactId.PathValid, 1);
        a.CopyTo(b);

        Assert.Equal(a.ComputeHash(), b.ComputeHash());

        b.Set(WorldFactId.PathValid, 2);
        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
    }

    [Fact]
    public void CopyTo_And_Reset_WorkWithoutAllocationSemantics()
    {
        SymbolicWorldState source = new();
        SymbolicWorldState destination = new();
        source.Set(WorldFactId.HasAmmunition, 5);

        Assert.Equal(OperationStatus.Success, source.CopyTo(destination));
        Assert.True(destination.TryGet(WorldFactId.HasAmmunition, out int ammo));
        Assert.Equal(5, ammo);

        destination.Reset();
        Assert.Equal(0UL, destination.SpecifiedMask);
        Assert.False(destination.Contains(WorldFactId.HasAmmunition));
    }

    [Fact]
    public void CountUnsatisfied_CountsMissingAndMismatched()
    {
        SymbolicWorldState world = new();
        SymbolicWorldState requirements = new();
        world.Set(WorldFactId.InCover, 1);
        requirements.Set(WorldFactId.InCover, 1);
        requirements.Set(WorldFactId.WeaponLoaded, 1);
        requirements.Set(WorldFactId.HasAmmunition, 1);

        Assert.Equal(2, world.CountUnsatisfied(requirements));
    }

    [Fact]
    public void ApplyEffects_SetsAndClears()
    {
        SymbolicWorldState state = new();
        state.Set(WorldFactId.UnderDirectFire, 1);

        WorldStateEffect[] effects =
        [
            WorldStateEffect.Set(WorldFactId.InCover, 1),
            WorldStateEffect.Clear(WorldFactId.UnderDirectFire),
        ];

        Assert.Equal(OperationStatus.Success, state.ApplyEffects(effects, effects.Length));
        Assert.True(state.Contains(WorldFactId.InCover));
        Assert.False(state.Contains(WorldFactId.UnderDirectFire));
    }

    [Fact]
    public void WorldStateBuilder_MergesBoundedChannels()
    {
        SymbolicWorldState destination = new();

        int[] bodyValues = [1];
        int[] memoryValues = [1];
        int[] orderValues = [1];
        int[] dangerValues = [1];

        ulong bodyFlags = 1UL << (int)WorldFactId.InCover;
        ulong memoryFlags = 1UL << (int)WorldFactId.TargetKnown;
        ulong orderFlags = 1UL << (int)WorldFactId.SquadOrderAvailable;
        ulong dangerFlags = 1UL << (int)WorldFactId.ImmediateDangerPresent;

        Assert.Equal(
            OperationStatus.Success,
            WorldStateBuilder.Build(
                bodyFlags,
                bodyValues,
                memoryFlags,
                memoryValues,
                orderFlags,
                orderValues,
                dangerFlags,
                dangerValues,
                destination));

        Assert.True(destination.Contains(WorldFactId.InCover));
        Assert.True(destination.Contains(WorldFactId.TargetKnown));
        Assert.True(destination.Contains(WorldFactId.SquadOrderAvailable));
        Assert.True(destination.Contains(WorldFactId.ImmediateDangerPresent));
    }
}
