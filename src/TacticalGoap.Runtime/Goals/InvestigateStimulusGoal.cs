using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Investigate perceived stimulus evidence.
/// </summary>
public sealed class InvestigateStimulusGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public InvestigateStimulusGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.HigherPriorityOnly, GoalPriorityBands.Tactical + 30)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetKnown)
            && !GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetVisible);
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.SearchLocationInspected, 1);
    }
}
