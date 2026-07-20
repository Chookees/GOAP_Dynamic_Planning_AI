namespace TacticalGoap.Abstractions.Diagnostics;

/// <summary>
/// Stable diagnostic event codes written into the runtime trace ring buffer.
/// </summary>
public enum TraceEventCode : byte
{
    /// <summary>
    /// A goal was evaluated during arbitration.
    /// </summary>
    GoalEvaluated = 0,

    /// <summary>
    /// A goal was selected as the active objective.
    /// </summary>
    GoalSelected = 1,

    /// <summary>
    /// Planning started for the active goal.
    /// </summary>
    PlanStarted = 2,

    /// <summary>
    /// Planning produced a valid plan.
    /// </summary>
    PlanSucceeded = 3,

    /// <summary>
    /// Planning failed to produce a valid plan.
    /// </summary>
    PlanFailed = 4,

    /// <summary>
    /// An action began execution.
    /// </summary>
    ActionStarted = 5,

    /// <summary>
    /// An action completed successfully.
    /// </summary>
    ActionSucceeded = 6,

    /// <summary>
    /// An action failed during execution.
    /// </summary>
    ActionFailed = 7,

    /// <summary>
    /// A replan was requested for the active goal.
    /// </summary>
    ReplanRequested = 8,

    /// <summary>
    /// A focused target was selected.
    /// </summary>
    TargetSelected = 9,

    /// <summary>
    /// A cover or tactical point was selected.
    /// </summary>
    CoverSelected = 10,

    /// <summary>
    /// A reservation was acquired.
    /// </summary>
    ReservationAcquired = 11,

    /// <summary>
    /// A reservation was released or lost.
    /// </summary>
    ReservationReleased = 12,

    /// <summary>
    /// A squad order was issued.
    /// </summary>
    OrderIssued = 13,

    /// <summary>
    /// A squad order completed.
    /// </summary>
    OrderCompleted = 14,

    /// <summary>
    /// A communication request was submitted.
    /// </summary>
    CommunicationRequested = 15,

    /// <summary>
    /// A contract or invariant check failed.
    /// </summary>
    ContractViolation = 16,

    /// <summary>
    /// A navigation query completed.
    /// </summary>
    NavigationQueried = 17,

    /// <summary>
    /// Perception candidates were ingested.
    /// </summary>
    PerceptionUpdated = 18,

    /// <summary>
    /// Working memory was written.
    /// </summary>
    MemoryWritten = 19,

    /// <summary>
    /// Weapon selection completed.
    /// </summary>
    WeaponSelected = 20,

    /// <summary>
    /// Host service rejected a command.
    /// </summary>
    HostRejected = 21,

    /// <summary>
    /// Runtime lifecycle transition occurred.
    /// </summary>
    LifecycleTransition = 22,
}
