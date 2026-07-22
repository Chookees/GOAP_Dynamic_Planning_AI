namespace DynamicPlanningAI.Abstractions.Results;

/// <summary>
/// Outcome of an incremental GOAP planner step.
/// </summary>
public enum PlannerStatus : byte
{
    /// <summary>
    /// Planning remains incomplete and may continue on a later step.
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// A valid plan was reconstructed.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// The search space was exhausted without a valid plan.
    /// </summary>
    NoPlan = 2,

    /// <summary>
    /// The expansion budget for this step was exhausted.
    /// </summary>
    ExpansionBudgetExceeded = 3,

    /// <summary>
    /// The planner node table reached capacity.
    /// </summary>
    NodeCapacityExceeded = 4,

    /// <summary>
    /// The open-set heap reached capacity.
    /// </summary>
    OpenSetCapacityExceeded = 5,

    /// <summary>
    /// The closed/duplicate table reached capacity.
    /// </summary>
    ClosedSetCapacityExceeded = 6,

    /// <summary>
    /// The reconstructed plan would exceed maximum plan length.
    /// </summary>
    PlanLengthExceeded = 7,

    /// <summary>
    /// The provided goal is invalid or empty.
    /// </summary>
    InvalidGoal = 8,

    /// <summary>
    /// The provided world state is invalid.
    /// </summary>
    InvalidWorldState = 9,

    /// <summary>
    /// An action candidate used during expansion is invalid.
    /// </summary>
    InvalidActionCandidate = 10,

    /// <summary>
    /// The planning session was cancelled.
    /// </summary>
    Cancelled = 11,

    /// <summary>
    /// A cost calculation overflowed.
    /// </summary>
    CostOverflow = 12,
}
