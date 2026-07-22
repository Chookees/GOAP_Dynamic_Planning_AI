using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Navigation;

/// <summary>
/// Optional hook invoked when pathfinding fails so working memory can record evidence.
/// </summary>
public interface INavigationFailureMemoryHook
{
    /// <summary>
    /// Records a path-failure memory entry for an agent.
    /// </summary>
    /// <param name="agent">Agent that failed to path.</param>
    /// <param name="status">Host or orchestrator query status.</param>
    /// <param name="tickSequence">Tick sequence when the failure occurred.</param>
    /// <returns>Memory write status from the memory subsystem.</returns>
    public MemoryWriteResult RecordPathFailure(AgentId agent, NavigationQueryStatus status, long tickSequence);
}

/// <summary>
/// Optional classifier that maps consecutive path nodes to required traversal flags.
/// </summary>
public interface INavigationTraversalClassifier
{
    /// <summary>
    /// Returns the traversal flags required to move between two consecutive nodes.
    /// </summary>
    /// <param name="from">Origin node.</param>
    /// <param name="destination">Destination node.</param>
    /// <returns>Required flags; <see cref="NavigationQueryFlags.None"/> for ordinary edges.</returns>
    public NavigationQueryFlags GetRequiredFlags(NavigationNodeId from, NavigationNodeId destination);
}
