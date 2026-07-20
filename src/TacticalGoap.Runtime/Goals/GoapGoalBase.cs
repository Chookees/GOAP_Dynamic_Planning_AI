using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Shared sealed-goal helpers for relevance, priority, and desire writing.
/// </summary>
internal static class GoalHelpers
{
    /// <summary>
    /// Returns whether a fact is specified and non-zero.
    /// </summary>
    public static bool IsTruthy(SymbolicWorldState state, WorldFactId factId)
    {
        return state.TryGet(factId, out int value) && value != 0;
    }

    /// <summary>
    /// Clears <paramref name="destination"/> and sets a single desire fact.
    /// </summary>
    public static OperationStatus SetSingleDesire(
        SymbolicWorldState destination,
        WorldFactId factId,
        int value)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Reset();
        return destination.Set(factId, value);
    }

    /// <summary>
    /// Clears <paramref name="destination"/> and sets two desire facts.
    /// </summary>
    public static OperationStatus SetDualDesire(
        SymbolicWorldState destination,
        WorldFactId firstFact,
        int firstValue,
        WorldFactId secondFact,
        int secondValue)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Reset();
        OperationStatus status = destination.Set(firstFact, firstValue);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return destination.Set(secondFact, secondValue);
    }

    /// <summary>
    /// Clamps a priority into the non-negative integer domain.
    /// </summary>
    public static int ClampPriority(int priority)
    {
        return priority < 0 ? 0 : priority;
    }
}

/// <summary>
/// Base helpers shared by concrete goal types via composition of fields.
/// </summary>
public abstract class GoapGoalBase : IGoapGoal
{
    /// <summary>
    /// Initializes goal identity and interrupt policy.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    /// <param name="interruptionPolicy">Interruption policy.</param>
    /// <param name="basePriority">Non-negative base priority.</param>
    protected GoapGoalBase(GoalId id, GoalInterruptionPolicy interruptionPolicy, int basePriority)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException("Goal id must be valid.", nameof(id));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(basePriority);

        Id = id;
        InterruptionPolicy = interruptionPolicy;
        BasePriority = basePriority;
    }

    /// <inheritdoc />
    public GoalId Id { get; }

    /// <inheritdoc />
    public GoalInterruptionPolicy InterruptionPolicy { get; }

    /// <summary>
    /// Gets the configured base priority.
    /// </summary>
    protected int BasePriority { get; }

    /// <inheritdoc />
    public abstract bool IsRelevant(in GoalArbitrationContext context);

    /// <inheritdoc />
    public virtual int EvaluatePriority(in GoalArbitrationContext context)
    {
        return GoalHelpers.ClampPriority(BasePriority);
    }

    /// <inheritdoc />
    public abstract OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination);
}
