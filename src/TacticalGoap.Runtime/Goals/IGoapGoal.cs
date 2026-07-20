using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// GOAP goal that declares relevance, priority, desire facts, and interrupt policy.
/// </summary>
public interface IGoapGoal
{
    /// <summary>
    /// Gets the stable goal identifier.
    /// </summary>
    public GoalId Id { get; }

    /// <summary>
    /// Gets the interruption policy for this goal when it is active.
    /// </summary>
    public GoalInterruptionPolicy InterruptionPolicy { get; }

    /// <summary>
    /// Returns whether the goal is relevant under the supplied arbitration context.
    /// </summary>
    /// <param name="context">Bounded arbitration inputs.</param>
    /// <returns><see langword="true"/> when the goal may compete for selection.</returns>
    public bool IsRelevant(in GoalArbitrationContext context);

    /// <summary>
    /// Evaluates a non-negative integer priority for arbitration.
    /// </summary>
    /// <param name="context">Bounded arbitration inputs.</param>
    /// <returns>Bounded integer priority; higher values win.</returns>
    public int EvaluatePriority(in GoalArbitrationContext context);

    /// <summary>
    /// Writes the desired world-state facts that constitute goal success.
    /// </summary>
    /// <param name="context">Bounded arbitration inputs.</param>
    /// <param name="destination">Preallocated destination desire state.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus BuildDesiredState(in GoalArbitrationContext context, SymbolicWorldState destination);
}
