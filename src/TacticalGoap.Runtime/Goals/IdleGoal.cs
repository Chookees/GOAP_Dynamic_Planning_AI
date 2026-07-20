using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Fallback idle when no higher goal is relevant.
/// </summary>
public sealed class IdleGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public IdleGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.HigherPriorityOnly, GoalPriorityBands.Idle)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return true;
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.ReadinessMaintained, 1);
    }
}
