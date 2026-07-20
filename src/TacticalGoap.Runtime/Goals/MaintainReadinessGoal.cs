using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Keep readiness maintained when not in combat.
/// </summary>
public sealed class MaintainReadinessGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public MaintainReadinessGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.HigherPriorityOnly, GoalPriorityBands.Readiness)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return !GoalHelpers.IsTruthy(context.WorldState, WorldFactId.ImmediateDangerPresent)
            && !GoalHelpers.IsTruthy(context.WorldState, WorldFactId.GrenadeDangerPresent)
            && !GoalHelpers.IsTruthy(context.WorldState, WorldFactId.UnderDirectFire)
            && !GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetVisible);
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.ReadinessMaintained, 1);
    }
}
