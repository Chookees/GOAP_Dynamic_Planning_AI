using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Goals;

/// <summary>
/// Critical survival when lethal danger is present.
/// </summary>
public sealed class SurviveImmediateDangerGoal : GoapGoalBase
{
    /// <summary>
    /// Initializes the goal with a stable identifier.
    /// </summary>
    /// <param name="id">Stable goal identifier.</param>
    public SurviveImmediateDangerGoal(GoalId id)
        : base(id, GoalInterruptionPolicy.Always, GoalPriorityBands.Critical + 50)
    {
    }

    /// <inheritdoc />
    public override bool IsRelevant(in GoalArbitrationContext context)
    {
        return GoalHelpers.IsTruthy(context.WorldState, WorldFactId.ImmediateDangerPresent);
    }

    /// <inheritdoc />
    public override OperationStatus BuildDesiredState(
        in GoalArbitrationContext context,
        SymbolicWorldState destination)
    {
        return GoalHelpers.SetSingleDesire(destination, WorldFactId.ImmediateDangerResolved, 1);
    }
}
