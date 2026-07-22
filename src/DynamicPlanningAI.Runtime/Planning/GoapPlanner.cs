using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Bounded deterministic backward-regression GOAP planner.
/// </summary>
/// <remarks>
/// <para>
/// Search proceeds from goal requirements toward the current world by expanding
/// candidates whose effects advance unresolved requirements. Conflicting effects
/// are rejected. Regression removes satisfied requirements and merges
/// preconditions. Search stops when the current world satisfies the regressed
/// requirements.
/// </para>
/// <para>
/// Heuristic: zero (Dijkstra). An unsatisfied-fact-count heuristic is not used
/// because multi-effect actions would overestimate remaining cost. Costs must be
/// non-negative; additions use checked arithmetic.
/// </para>
/// <para>
/// No recursion, no post-init allocation, and no LINQ.
/// </para>
/// </remarks>
public sealed class GoapPlanner
{
    private readonly ActionCandidate[] _rebuildScratch;

    /// <summary>
    /// Initializes the planner with a rebuild scratch buffer.
    /// </summary>
    public GoapPlanner()
    {
        _rebuildScratch = new ActionCandidate[AiHardLimits.MaximumPlanLength];
    }

    /// <summary>
    /// Performs up to <paramref name="maxExpansions"/> planner expansions.
    /// </summary>
    /// <param name="session">Active planning session.</param>
    /// <param name="maxExpansions">Expansion budget for this call.</param>
    /// <returns>Planner result carrying status and diagnostics.</returns>
    [FrozenRuntimePath]
    public PlannerResult Step(PlanningSession session, int maxExpansions)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsActive)
        {
            return CreateResult(session, PlannerStatus.InvalidWorldState, 0);
        }

        if (session.IsCancelled)
        {
            return CreateResult(session, PlannerStatus.Cancelled, 0);
        }

        if (maxExpansions < 1 || maxExpansions > AiHardLimits.MaximumPlannerExpansionsPerStep)
        {
            return CreateResult(session, PlannerStatus.InvalidActionCandidate, 0);
        }

        PlannerStatus bootstrap = EnsureBootstrapped(session);
        if (bootstrap != PlannerStatus.InProgress)
        {
            return CreateResult(session, bootstrap, 0);
        }

        return ExpandLoop(session, maxExpansions);
    }

    [FrozenRuntimePath]
    private PlannerResult ExpandLoop(PlanningSession session, int maxExpansions)
    {
        PlannerWorkspace workspace = session.Workspace;
        int expansions = 0;

        while (expansions < maxExpansions)
        {
            if (session.IsCancelled)
            {
                return CreateResult(session, PlannerStatus.Cancelled, expansions);
            }

            if (!workspace.Open.TryPop(out int nodeIndex))
            {
                return CreateResult(session, PlannerStatus.NoPlan, expansions);
            }

            expansions = checked(expansions + 1);
            ref PlannerNode node = ref workspace.Nodes[nodeIndex];
            SymbolicWorldState requirements = workspace.StatePool[node.StateIndex];

            if (session.WorldState.Satisfies(requirements))
            {
                PlannerStatus rebuilt = ReconstructPlan(session, nodeIndex);
                return CreateResult(session, rebuilt, expansions, node.GCost);
            }

            PlannerStatus expandStatus = ExpandNode(session, nodeIndex);
            if (expandStatus != PlannerStatus.InProgress)
            {
                return CreateResult(session, expandStatus, expansions);
            }
        }

        if (workspace.Open.IsEmpty)
        {
            return CreateResult(session, PlannerStatus.NoPlan, expansions);
        }

        return CreateResult(session, PlannerStatus.ExpansionBudgetExceeded, expansions);
    }

    [FrozenRuntimePath]
    private static PlannerStatus ExpandNode(PlanningSession session, int parentIndex)
    {
        PlannerWorkspace workspace = session.Workspace;
        ref PlannerNode parent = ref workspace.Nodes[parentIndex];
        SymbolicWorldState requirements = workspace.StatePool[parent.StateIndex];

        for (int candidateIndex = 0; candidateIndex < workspace.CandidateCount; candidateIndex++)
        {
            ActionCandidate candidate = workspace.Candidates[candidateIndex];
            if (!candidate.IsValid)
            {
                return PlannerStatus.InvalidActionCandidate;
            }

            if (candidate.Cost < 0)
            {
                return PlannerStatus.InvalidActionCandidate;
            }

            SymbolicWorldState effectSets = candidate.EffectSets!;
            if (!requirements.IsAdvancedBy(effectSets, candidate.EffectClearMask))
            {
                continue;
            }

            OperationStatus regress = requirements.RegressThrough(
                effectSets,
                candidate.EffectClearMask,
                candidate.Preconditions!,
                workspace.ScratchRegressed);
            if (regress == OperationStatus.Conflict)
            {
                continue;
            }

            if (regress != OperationStatus.Success)
            {
                return PlannerStatus.InvalidActionCandidate;
            }

            PlannerStatus childStatus = TryEnqueueChild(session, parentIndex, candidateIndex, candidate.Cost);
            if (childStatus != PlannerStatus.InProgress)
            {
                return childStatus;
            }
        }

        return PlannerStatus.InProgress;
    }

    [FrozenRuntimePath]
    private static PlannerStatus TryEnqueueChild(
        PlanningSession session,
        int parentIndex,
        int candidateIndex,
        int actionCost)
    {
        PlannerWorkspace workspace = session.Workspace;
        ref PlannerNode parent = ref workspace.Nodes[parentIndex];

        int childG;
        try
        {
            childG = checked(parent.GCost + actionCost);
        }
        catch (OverflowException)
        {
            return PlannerStatus.CostOverflow;
        }

        if (childG > AiHardLimits.MaximumActionCost)
        {
            return PlannerStatus.CostOverflow;
        }

        ulong hash = workspace.ScratchRegressed.ComputeHash();
        OperationStatus closed = workspace.Closed.TryInsert(hash, childG, out bool duplicate);
        if (closed == OperationStatus.CapacityExceeded)
        {
            return PlannerStatus.ClosedSetCapacityExceeded;
        }

        if (closed != OperationStatus.Success)
        {
            return PlannerStatus.InvalidWorldState;
        }

        if (duplicate)
        {
            return PlannerStatus.InProgress;
        }

        if (workspace.NodeCount >= workspace.NodeCapacity)
        {
            return PlannerStatus.NodeCapacityExceeded;
        }

        int nodeIndex = workspace.NodeCount;
        workspace.NodeCount = checked(workspace.NodeCount + 1);

        OperationStatus copy = workspace.ScratchRegressed.CopyTo(workspace.StatePool[nodeIndex]);
        if (copy != OperationStatus.Success)
        {
            return PlannerStatus.InvalidWorldState;
        }

        workspace.Nodes[nodeIndex] = new PlannerNode
        {
            ParentIndex = parentIndex,
            CandidateIndex = candidateIndex,
            GCost = childG,
            HCost = 0,
            StateHash = hash,
            StateIndex = nodeIndex,
            InsertionOrder = workspace.NextInsertionOrder,
        };
        workspace.NextInsertionOrder = checked(workspace.NextInsertionOrder + 1);

        OperationStatus inserted = workspace.Open.Insert(nodeIndex);
        if (inserted == OperationStatus.CapacityExceeded)
        {
            return PlannerStatus.OpenSetCapacityExceeded;
        }

        return inserted == OperationStatus.Success
            ? PlannerStatus.InProgress
            : PlannerStatus.InvalidWorldState;
    }

    private static PlannerStatus EnsureBootstrapped(PlanningSession session)
    {
        if (session.GoalRequirements.SpecifiedMask == 0UL)
        {
            return PlannerStatus.InvalidGoal;
        }

        PlannerWorkspace workspace = session.Workspace;
        if (workspace.NodeCount > 0 || !workspace.Open.IsEmpty)
        {
            return PlannerStatus.InProgress;
        }

        if (session.WorldState.Satisfies(session.GoalRequirements))
        {
            OperationStatus assigned = session.OutputPlan.Assign(
                session.GoalId,
                session.CreationTick,
                session.WorldState.ComputeHash(),
                ReadOnlySpan<ActionCandidate>.Empty,
                0,
                0);
            return assigned == OperationStatus.Success
                ? PlannerStatus.Succeeded
                : PlannerStatus.InvalidWorldState;
        }

        return SeedRoot(session);
    }

    private static PlannerStatus SeedRoot(PlanningSession session)
    {
        PlannerWorkspace workspace = session.Workspace;
        if (workspace.NodeCount >= workspace.NodeCapacity)
        {
            return PlannerStatus.NodeCapacityExceeded;
        }

        OperationStatus copy = session.GoalRequirements.CopyTo(workspace.StatePool[0]);
        if (copy != OperationStatus.Success)
        {
            return PlannerStatus.InvalidWorldState;
        }

        ulong hash = workspace.StatePool[0].ComputeHash();
        OperationStatus closed = workspace.Closed.TryInsert(hash, 0, out bool duplicate);
        if (closed == OperationStatus.CapacityExceeded)
        {
            return PlannerStatus.ClosedSetCapacityExceeded;
        }

        if (closed != OperationStatus.Success || duplicate)
        {
            return PlannerStatus.InvalidWorldState;
        }

        workspace.Nodes[0] = new PlannerNode
        {
            ParentIndex = -1,
            CandidateIndex = -1,
            GCost = 0,
            HCost = 0,
            StateHash = hash,
            StateIndex = 0,
            InsertionOrder = 0,
        };
        workspace.NodeCount = 1;
        workspace.NextInsertionOrder = 1;

        OperationStatus inserted = workspace.Open.Insert(0);
        if (inserted == OperationStatus.CapacityExceeded)
        {
            return PlannerStatus.OpenSetCapacityExceeded;
        }

        return inserted == OperationStatus.Success
            ? PlannerStatus.InProgress
            : PlannerStatus.InvalidWorldState;
    }

    private PlannerStatus ReconstructPlan(PlanningSession session, int leafIndex)
    {
        PlannerWorkspace workspace = session.Workspace;
        int length = 0;
        int cursor = leafIndex;

        while (cursor >= 0)
        {
            ref PlannerNode node = ref workspace.Nodes[cursor];
            if (node.CandidateIndex >= 0)
            {
                if (length >= workspace.PlanCapacity)
                {
                    return PlannerStatus.PlanLengthExceeded;
                }

                workspace.PlanCandidateIndices[length] = node.CandidateIndex;
                length = checked(length + 1);
            }

            cursor = node.ParentIndex;
        }

        // Leaf-to-root follows backward search from the satisfied world toward the
        // goal, which is already forward execution order.
        for (int i = 0; i < length; i++)
        {
            int candidateIndex = workspace.PlanCandidateIndices[i];
            _rebuildScratch[i] = workspace.Candidates[candidateIndex];
        }

        workspace.PlanLength = length;
        OperationStatus assigned = session.OutputPlan.Assign(
            session.GoalId,
            session.CreationTick,
            session.WorldState.ComputeHash(),
            _rebuildScratch.AsSpan(0, length),
            length,
            workspace.Nodes[leafIndex].GCost);

        return assigned == OperationStatus.Success
            ? PlannerStatus.Succeeded
            : assigned == OperationStatus.CapacityExceeded
                ? PlannerStatus.PlanLengthExceeded
                : PlannerStatus.InvalidWorldState;
    }

    private static PlannerResult CreateResult(
        PlanningSession session,
        PlannerStatus status,
        int expansions,
        int totalCost = 0)
    {
        int planLength = status == PlannerStatus.Succeeded ? session.OutputPlan.ActionCount : 0;
        int cost = status == PlannerStatus.Succeeded ? totalCost : 0;
        return new PlannerResult(
            status,
            expansions,
            session.Workspace.NodeCount,
            planLength,
            session.GoalId,
            cost);
    }
}
