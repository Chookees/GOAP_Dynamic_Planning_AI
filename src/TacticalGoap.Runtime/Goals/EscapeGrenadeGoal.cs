using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Flee an active grenade danger region.
/// </summary>
public sealed class EscapeGrenadeGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public EscapeGrenadeGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.Always, GoalPriorityBands.Critical + 40)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return GoalHelpers.IsTruthy(context.WorldState, WorldFactId.GrenadeDangerPresent);
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.ImmediateDangerResolved, 1);
    }
}
