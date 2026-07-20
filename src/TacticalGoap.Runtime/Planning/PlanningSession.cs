using System;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Planning;

/// <summary>
/// Binds a planner workspace to a goal, world snapshot, and candidate set for one session.
/// </summary>
public sealed class PlanningSession
{
    /// <summary>
    /// Initializes a session bound to a workspace.
    /// </summary>
    /// <param name="workspace">Preallocated planner workspace.</param>
    public PlanningSession(PlannerWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        Workspace = workspace;
        GoalId = GoalId.Invalid;
        GoalRequirements = new SymbolicWorldState();
        WorldState = new SymbolicWorldState();
        OutputPlan = new Plan(workspace.PlanCapacity);
        IsActive = false;
        IsCancelled = false;
        CreationTick = 0L;
    }

    /// <summary>
    /// Gets the bound workspace.
    /// </summary>
    public PlannerWorkspace Workspace { get; }

    /// <summary>
    /// Gets the session goal identifier.
    /// </summary>
    public GoalId GoalId { get; private set; }

    /// <summary>
    /// Gets the goal requirement state.
    /// </summary>
    public SymbolicWorldState GoalRequirements { get; }

    /// <summary>
    /// Gets the agent-local world snapshot used for satisfaction checks.
    /// </summary>
    public SymbolicWorldState WorldState { get; }

    /// <summary>
    /// Gets the output plan buffer.
    /// </summary>
    public Plan OutputPlan { get; }

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether cancellation was requested.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Gets the creation tick sequence.
    /// </summary>
    public long CreationTick { get; private set; }

    /// <summary>
    /// Begins a planning session.
    /// </summary>
    /// <param name="goalId">Goal identifier.</param>
    /// <param name="creationTick">Creation tick sequence.</param>
    /// <param name="goalRequirements">Goal requirement facts.</param>
    /// <param name="worldState">Current agent-local world state.</param>
    /// <param name="candidates">Bound action candidates.</param>
    /// <param name="candidateCount">Number of valid candidates.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus Begin(
        GoalId goalId,
        long creationTick,
        SymbolicWorldState goalRequirements,
        SymbolicWorldState worldState,
        ReadOnlySpan<ActionCandidate> candidates,
        int candidateCount)
    {
        ArgumentNullException.ThrowIfNull(goalRequirements);
        ArgumentNullException.ThrowIfNull(worldState);

        if (!goalId.IsValid)
        {
            return OperationStatus.InvalidArgument;
        }

        if (candidateCount < 0 || candidateCount > candidates.Length)
        {
            return OperationStatus.InvalidArgument;
        }

        if (candidateCount > Workspace.CandidateCapacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        Workspace.Reset();
        OutputPlan.Reset();
        GoalId = goalId;
        CreationTick = creationTick;
        IsCancelled = false;
        IsActive = true;

        OperationStatus status = goalRequirements.CopyTo(GoalRequirements);
        if (status != OperationStatus.Success)
        {
            IsActive = false;
            return status;
        }

        status = worldState.CopyTo(WorldState);
        if (status != OperationStatus.Success)
        {
            IsActive = false;
            return status;
        }

        for (int i = 0; i < candidateCount; i++)
        {
            Workspace.Candidates[i] = candidates[i];
        }

        Workspace.CandidateCount = candidateCount;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Requests cancellation of the active session.
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        OutputPlan.Cancel();
    }

    /// <summary>
    /// Ends the session and marks it inactive.
    /// </summary>
    public void End()
    {
        IsActive = false;
    }
}
