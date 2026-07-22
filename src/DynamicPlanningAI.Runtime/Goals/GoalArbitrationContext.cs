using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Goals;

/// <summary>
/// Bounded inputs available to goal relevance and priority evaluation.
/// </summary>
/// <remarks>
/// All fields are value types or preallocated references. The struct never allocates.
/// </remarks>
public readonly struct GoalArbitrationContext
{
    /// <summary>
    /// Initializes a new arbitration context.
    /// </summary>
    /// <param name="agentId">Agent under arbitration.</param>
    /// <param name="worldState">Current agent-local symbolic world state.</param>
    /// <param name="activeGoalId">Currently executing goal, or invalid.</param>
    /// <param name="activeGoalPriority">Priority of the active goal when valid.</param>
    /// <param name="tickSequence">Current tick sequence.</param>
    /// <param name="hysteresisMargin">Priority margin required to displace the active goal.</param>
    public GoalArbitrationContext(
        AgentId agentId,
        SymbolicWorldState worldState,
        GoalId activeGoalId,
        int activeGoalPriority,
        long tickSequence,
        int hysteresisMargin)
    {
        ArgumentNullException.ThrowIfNull(worldState);
        ArgumentOutOfRangeException.ThrowIfNegative(activeGoalPriority);
        ArgumentOutOfRangeException.ThrowIfNegative(hysteresisMargin);

        AgentId = agentId;
        WorldState = worldState;
        ActiveGoalId = activeGoalId;
        ActiveGoalPriority = activeGoalPriority;
        TickSequence = tickSequence;
        HysteresisMargin = hysteresisMargin;
    }

    /// <summary>
    /// Gets the agent under arbitration.
    /// </summary>
    public AgentId AgentId { get; }

    /// <summary>
    /// Gets the current agent-local symbolic world state.
    /// </summary>
    public SymbolicWorldState WorldState { get; }

    /// <summary>
    /// Gets the currently executing goal identifier.
    /// </summary>
    public GoalId ActiveGoalId { get; }

    /// <summary>
    /// Gets the priority of the active goal when valid.
    /// </summary>
    public int ActiveGoalPriority { get; }

    /// <summary>
    /// Gets the current tick sequence.
    /// </summary>
    public long TickSequence { get; }

    /// <summary>
    /// Gets the priority margin required to displace the active goal.
    /// </summary>
    public int HysteresisMargin { get; }

    /// <summary>
    /// Returns whether a world fact is specified and non-zero.
    /// </summary>
    /// <param name="factId">Fact to test.</param>
    /// <returns><see langword="true"/> when the fact is present and truthy.</returns>
    public bool HasFact(Abstractions.Enums.WorldFactId factId)
    {
        return WorldState.TryGet(factId, out int value) && value != 0;
    }
}
