using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Navigation;

/// <summary>
/// Orchestrates host path queries and path-follow state without embedding a navmesh.
/// </summary>
/// <remarks>
/// The orchestrator writes into a caller-owned <see cref="NavigationPathBuffer"/>,
/// validates walkability against an optional snapshot, and records path failures
/// through an optional memory hook. Door, window, and vault awareness is expressed
/// via <see cref="NavigationQueryFlags"/> and an optional traversal classifier.
/// </remarks>
public sealed class NavigationOrchestrator
{
    private readonly INavigationFailureMemoryHook? _failureHook;
    private readonly INavigationTraversalClassifier? _traversalClassifier;

    /// <summary>
    /// Initializes an orchestrator with optional failure and traversal hooks.
    /// </summary>
    /// <param name="failureHook">Optional path-failure memory hook.</param>
    /// <param name="traversalClassifier">Optional door/window/vault classifier.</param>
    public NavigationOrchestrator(
        INavigationFailureMemoryHook? failureHook = null,
        INavigationTraversalClassifier? traversalClassifier = null)
    {
        _failureHook = failureHook;
        _traversalClassifier = traversalClassifier;
    }

    /// <summary>
    /// Requests a path from the host service into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="service">Host navigation service.</param>
    /// <param name="query">Path query parameters.</param>
    /// <param name="flags">Allowed traversal link flags.</param>
    /// <param name="buffer">Caller-owned path buffer.</param>
    /// <param name="followState">Receives updated follow state.</param>
    /// <param name="tickSequence">Current tick sequence for memory recording.</param>
    /// <returns>Host query result mirrored after buffer commit and validation.</returns>
    [FrozenRuntimePath]
    public NavigationQueryResult RequestPath(
        INavigationService service,
        in NavigationPathQuery query,
        NavigationQueryFlags flags,
        NavigationPathBuffer buffer,
        ref PathFollowState followState,
        long tickSequence)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(buffer);
        PrepareRequest(query.Agent, flags, buffer, ref followState);

        if (!query.Agent.IsValid)
        {
            return FailRequest(ref followState, NavigationQueryStatus.InvalidQuery, tickSequence);
        }

        NavigationQueryResult result = service.TryFindPath(query, buffer.AsDestinationSpan(), out int written);
        followState.LastQueryStatus = result.Status;

        if (!TryAcceptPath(result, written, buffer, flags, ref followState, tickSequence, out NavigationQueryResult accepted))
        {
            return accepted;
        }

        return accepted;
    }

    /// <summary>
    /// Validates that every stored path node is walkable in the snapshot.
    /// </summary>
    /// <param name="snapshot">Current navigation snapshot.</param>
    /// <param name="buffer">Path buffer to validate.</param>
    /// <returns>Success when all nodes are walkable; otherwise Failed.</returns>
    [FrozenRuntimePath]
    public static OperationStatus ValidateAgainstSnapshot(INavigationSnapshot snapshot, NavigationPathBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(buffer);

        ReadOnlySpan<NavigationNodeId> nodes = buffer.AsReadOnlySpan();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].IsValid || !snapshot.IsNodeWalkable(nodes[i]))
            {
                return OperationStatus.Failed;
            }
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Advances follow state when the agent reaches the current path node.
    /// </summary>
    /// <param name="followState">Follow state to update.</param>
    /// <param name="reachedCurrent">Whether the current cursor node was reached.</param>
    /// <returns>Updated follow status.</returns>
    [FrozenRuntimePath]
    public static PathFollowStatus AdvanceFollow(ref PathFollowState followState, bool reachedCurrent)
    {
        if (followState.Status != PathFollowStatus.Following || !reachedCurrent)
        {
            return followState.Status;
        }

        int next = checked(followState.Cursor + 1);
        if (next >= followState.PathLength)
        {
            followState.Cursor = followState.PathLength;
            followState.Status = PathFollowStatus.Arrived;
            return PathFollowStatus.Arrived;
        }

        followState.Cursor = next;
        return PathFollowStatus.Following;
    }

    /// <summary>
    /// Marks the active path as invalidated and clears follow progress.
    /// </summary>
    /// <param name="followState">Follow state to invalidate.</param>
    /// <param name="buffer">Optional buffer to clear.</param>
    [FrozenRuntimePath]
    public static void Invalidate(ref PathFollowState followState, NavigationPathBuffer? buffer)
    {
        followState.Status = PathFollowStatus.Invalidated;
        followState.Cursor = 0;
        followState.PathLength = 0;
        buffer?.Clear();
    }

    [FrozenRuntimePath]
    private static void PrepareRequest(
        AgentId agent,
        NavigationQueryFlags flags,
        NavigationPathBuffer buffer,
        ref PathFollowState followState)
    {
        followState.Reset();
        followState.Agent = agent;
        followState.Flags = flags;
        buffer.Clear();
    }

    [FrozenRuntimePath]
    private bool TryAcceptPath(
        NavigationQueryResult result,
        int written,
        NavigationPathBuffer buffer,
        NavigationQueryFlags flags,
        ref PathFollowState followState,
        long tickSequence,
        out NavigationQueryResult accepted)
    {
        if (result.Status == NavigationQueryStatus.CapacityExceeded || written > buffer.Capacity)
        {
            buffer.Clear();
            followState.Status = PathFollowStatus.CapacityExceeded;
            followState.LastQueryStatus = NavigationQueryStatus.CapacityExceeded;
            RecordFailure(followState.Agent, NavigationQueryStatus.CapacityExceeded, tickSequence);
            accepted = NavigationQueryResult.Failure(NavigationQueryStatus.CapacityExceeded);
            return false;
        }

        if (result.Status == NavigationQueryStatus.NoPath)
        {
            buffer.Clear();
            accepted = FailRequest(ref followState, NavigationQueryStatus.NoPath, tickSequence);
            return false;
        }

        if (result.Status != NavigationQueryStatus.Success && result.Status != NavigationQueryStatus.PartialPath)
        {
            buffer.Clear();
            accepted = FailRequest(ref followState, result.Status, tickSequence);
            return false;
        }

        if (written <= 0 || buffer.CommitWrittenCount(written) != OperationStatus.Success || !ValidatePathNodes(buffer))
        {
            buffer.Clear();
            accepted = FailRequest(ref followState, NavigationQueryStatus.NoPath, tickSequence);
            return false;
        }

        followState.PathLength = written;
        followState.Cursor = 0;
        if (!ApplyTraversalAwareness(buffer, flags, ref followState))
        {
            buffer.Clear();
            followState.Status = PathFollowStatus.Failed;
            followState.LastQueryStatus = NavigationQueryStatus.HostRejected;
            RecordFailure(followState.Agent, NavigationQueryStatus.HostRejected, tickSequence);
            accepted = NavigationQueryResult.Failure(NavigationQueryStatus.HostRejected);
            return false;
        }

        followState.Status = PathFollowStatus.Following;
        accepted = new NavigationQueryResult(result.Status, written, result.EstimatedRemainingCost);
        return true;
    }

    [FrozenRuntimePath]
    private bool ApplyTraversalAwareness(
        NavigationPathBuffer buffer,
        NavigationQueryFlags flags,
        ref PathFollowState followState)
    {
        followState.RequiresDoorLink = false;
        followState.RequiresWindowLink = false;
        followState.RequiresVaultLink = false;

        if (_traversalClassifier is null)
        {
            return true;
        }

        ReadOnlySpan<NavigationNodeId> nodes = buffer.AsReadOnlySpan();
        for (int i = 1; i < nodes.Length; i++)
        {
            NavigationQueryFlags required = _traversalClassifier.GetRequiredFlags(nodes[i - 1], nodes[i]);
            MarkRequiredLinks(required, ref followState);
            if ((required & flags) != required)
            {
                return false;
            }
        }

        return true;
    }

    [FrozenRuntimePath]
    private static void MarkRequiredLinks(NavigationQueryFlags required, ref PathFollowState followState)
    {
        if ((required & NavigationQueryFlags.AllowDoorLinks) != 0)
        {
            followState.RequiresDoorLink = true;
        }

        if ((required & NavigationQueryFlags.AllowWindowLinks) != 0)
        {
            followState.RequiresWindowLink = true;
        }

        if ((required & NavigationQueryFlags.AllowVaultLinks) != 0)
        {
            followState.RequiresVaultLink = true;
        }
    }

    [FrozenRuntimePath]
    private static bool ValidatePathNodes(NavigationPathBuffer buffer)
    {
        ReadOnlySpan<NavigationNodeId> nodes = buffer.AsReadOnlySpan();
        if (nodes.Length <= 0)
        {
            return false;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].IsValid)
            {
                return false;
            }
        }

        return true;
    }

    [FrozenRuntimePath]
    private NavigationQueryResult FailRequest(
        ref PathFollowState followState,
        NavigationQueryStatus status,
        long tickSequence)
    {
        followState.LastQueryStatus = status;
        followState.Status = status switch
        {
            NavigationQueryStatus.NoPath => PathFollowStatus.NoPath,
            NavigationQueryStatus.CapacityExceeded => PathFollowStatus.CapacityExceeded,
            _ => PathFollowStatus.Failed,
        };

        RecordFailure(followState.Agent, status, tickSequence);
        return NavigationQueryResult.Failure(status);
    }

    [FrozenRuntimePath]
    private void RecordFailure(AgentId agent, NavigationQueryStatus status, long tickSequence)
    {
        _failureHook?.RecordPathFailure(agent, status, tickSequence);
    }
}
