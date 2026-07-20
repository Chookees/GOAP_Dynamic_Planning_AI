using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Engage and eliminate a selected living target.
/// </summary>
public sealed class EliminateTargetGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public EliminateTargetGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.HigherPriorityOnly, GoalPriorityBands.Combat)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetSelected)
            && GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetAlive);
    }

    /// <inheritdoc />
    public override int EvaluatePriority(in GoalArbitrationContext context)
    {
        int priority = BasePriority;
        if (GoalHelpers.IsTruthy(context.WorldState, WorldFactId.TargetVisible))
        {
            priority = checked(priority + 20);
        }

        return GoalHelpers.ClampPriority(priority);
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.TargetAlive, 0);
    }
}
