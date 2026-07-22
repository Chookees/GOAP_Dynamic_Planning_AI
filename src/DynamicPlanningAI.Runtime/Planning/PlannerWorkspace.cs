using System;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Preallocated planner scratch owned by a single planning session at a time.
/// </summary>
/// <remarks>
/// <para>
/// Ownership: the workspace owns node storage, world-state pool buffers, open heap,
/// closed table, candidate slot table, and plan index buffer. Callers must not
/// share one workspace across concurrent sessions. <see cref="Reset"/> clears
/// used counters without reallocating.
/// </para>
/// <para>
/// All buffers are sized at construction from validated capacities at or below
/// <see cref="AiHardLimits"/> ceilings.
/// </para>
/// </remarks>
public sealed class PlannerWorkspace
{
    /// <summary>
    /// Initializes a workspace with explicit capacities.
    /// </summary>
    /// <param name="nodeCapacity">Planner node capacity.</param>
    /// <param name="openCapacity">Open-set capacity.</param>
    /// <param name="closedCapacity">Closed-set capacity.</param>
    /// <param name="candidateCapacity">Candidate slot capacity.</param>
    /// <param name="planCapacity">Plan length capacity.</param>
    public PlannerWorkspace(
        int nodeCapacity,
        int openCapacity,
        int closedCapacity,
        int candidateCapacity,
        int planCapacity)
    {
        ValidateCapacity(nodeCapacity, AiHardLimits.MaximumPlannerNodes, nameof(nodeCapacity));
        ValidateCapacity(openCapacity, AiHardLimits.MaximumPlannerNodes, nameof(openCapacity));
        ValidateCapacity(closedCapacity, AiHardLimits.MaximumPlannerNodes, nameof(closedCapacity));
        ValidateCapacity(candidateCapacity, AiHardLimits.MaximumActionCandidates, nameof(candidateCapacity));
        ValidateCapacity(planCapacity, AiHardLimits.MaximumPlanLength, nameof(planCapacity));

        NodeCapacity = nodeCapacity;
        CandidateCapacity = candidateCapacity;
        PlanCapacity = planCapacity;

        Nodes = new PlannerNode[nodeCapacity];
        Candidates = new ActionCandidate[candidateCapacity];
        PlanCandidateIndices = new int[planCapacity];
        StatePool = new SymbolicWorldState[nodeCapacity];
        for (int i = 0; i < nodeCapacity; i++)
        {
            StatePool[i] = new SymbolicWorldState();
        }

        ScratchRequirements = new SymbolicWorldState();
        ScratchRegressed = new SymbolicWorldState();
        Open = new PlannerOpenHeap(openCapacity, Nodes);
        Closed = new PlannerClosedTable(closedCapacity);
        Reset();
    }

    /// <summary>
    /// Creates a workspace using hard-limit capacities.
    /// </summary>
    /// <returns>A fully allocated workspace.</returns>
    public static PlannerWorkspace CreateDefault()
    {
        return new PlannerWorkspace(
            AiHardLimits.MaximumPlannerNodes,
            AiHardLimits.MaximumPlannerNodes,
            AiHardLimits.MaximumPlannerNodes,
            AiHardLimits.MaximumActionCandidates,
            AiHardLimits.MaximumPlanLength);
    }

    /// <summary>
    /// Gets the node table capacity.
    /// </summary>
    public int NodeCapacity { get; }

    /// <summary>
    /// Gets the candidate table capacity.
    /// </summary>
    public int CandidateCapacity { get; }

    /// <summary>
    /// Gets the plan buffer capacity.
    /// </summary>
    public int PlanCapacity { get; }

    /// <summary>
    /// Gets the planner node storage.
    /// </summary>
    public PlannerNode[] Nodes { get; }

    /// <summary>
    /// Gets the candidate slot table.
    /// </summary>
    public ActionCandidate[] Candidates { get; }

    /// <summary>
    /// Gets the reconstructed plan candidate index buffer.
    /// </summary>
    public int[] PlanCandidateIndices { get; }

    /// <summary>
    /// Gets the world-state pool keyed by node state index.
    /// </summary>
    public SymbolicWorldState[] StatePool { get; }

    /// <summary>
    /// Gets scratch storage for the active requirement state.
    /// </summary>
    public SymbolicWorldState ScratchRequirements { get; }

    /// <summary>
    /// Gets scratch storage for regression results.
    /// </summary>
    public SymbolicWorldState ScratchRegressed { get; }

    /// <summary>
    /// Gets the open-set heap.
    /// </summary>
    public PlannerOpenHeap Open { get; }

    /// <summary>
    /// Gets the closed/duplicate table.
    /// </summary>
    public PlannerClosedTable Closed { get; }

    /// <summary>
    /// Gets or sets the number of allocated nodes in the current session.
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of candidates loaded for the current session.
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// Gets or sets the reconstructed plan length.
    /// </summary>
    public int PlanLength { get; set; }

    /// <summary>
    /// Gets or sets the next insertion ordinal for open-set tie-breaking.
    /// </summary>
    public int NextInsertionOrder { get; set; }

    /// <summary>
    /// Resets used counters and clears open/closed structures.
    /// </summary>
    public void Reset()
    {
        NodeCount = 0;
        CandidateCount = 0;
        PlanLength = 0;
        NextInsertionOrder = 0;
        Open.Clear();
        Closed.Clear();
        ScratchRequirements.Reset();
        ScratchRegressed.Reset();
    }

    private static void ValidateCapacity(int value, int maximum, string name)
    {
        if (value < 1 || value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, value, "Capacity is out of hard-limit range.");
        }
    }
}
