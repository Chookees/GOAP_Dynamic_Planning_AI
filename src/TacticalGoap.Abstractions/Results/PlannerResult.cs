using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Abstractions.Results;

/// <summary>
/// Explicit planner step result carrying status and diagnostic identifiers.
/// </summary>
/// <remarks>
/// Callers must inspect <see cref="Status"/> before consuming plan data.
/// Capacity failures leave the workspace reusable after a deterministic reset.
/// </remarks>
public readonly struct PlannerResult
{
    /// <summary>
    /// Initializes a new planner result.
    /// </summary>
    /// <param name="status">Planner outcome status.</param>
    /// <param name="expansionsPerformed">Node expansions performed in this step.</param>
    /// <param name="nodesAllocated">Planner nodes allocated in the session.</param>
    /// <param name="planLength">Reconstructed plan length when successful.</param>
    /// <param name="goalId">Goal that motivated planning.</param>
    /// <param name="totalCost">Total estimated plan cost when successful.</param>
    public PlannerResult(
        PlannerStatus status,
        int expansionsPerformed,
        int nodesAllocated,
        int planLength,
        GoalId goalId,
        int totalCost)
    {
        Status = status;
        ExpansionsPerformed = expansionsPerformed;
        NodesAllocated = nodesAllocated;
        PlanLength = planLength;
        GoalId = goalId;
        TotalCost = totalCost;
    }

    /// <summary>
    /// Gets the planner outcome status.
    /// </summary>
    public PlannerStatus Status { get; }

    /// <summary>
    /// Gets the number of expansions performed during the step.
    /// </summary>
    public int ExpansionsPerformed { get; }

    /// <summary>
    /// Gets the number of planner nodes allocated in the session.
    /// </summary>
    public int NodesAllocated { get; }

    /// <summary>
    /// Gets the reconstructed plan length when <see cref="Status"/> is Succeeded.
    /// </summary>
    public int PlanLength { get; }

    /// <summary>
    /// Gets the goal identifier associated with the session.
    /// </summary>
    public GoalId GoalId { get; }

    /// <summary>
    /// Gets the total estimated plan cost when successful; otherwise zero.
    /// </summary>
    public int TotalCost { get; }

    /// <summary>
    /// Gets a value indicating whether planning succeeded.
    /// </summary>
    public bool IsSuccess => Status == PlannerStatus.Succeeded;

    /// <summary>
    /// Gets a value indicating whether planning may continue.
    /// </summary>
    public bool IsInProgress => Status == PlannerStatus.InProgress;
}
