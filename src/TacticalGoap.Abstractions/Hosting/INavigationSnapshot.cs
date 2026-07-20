using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Read-only navigation graph snapshot for the current tick.
/// </summary>
/// <remarks>
/// Snapshots are immutable for the duration of a tick. Implementations must not
/// allocate when answering queries. The runtime must not retain the snapshot
/// beyond the tick that produced it unless the host guarantees stability.
/// </remarks>
public interface INavigationSnapshot
{
    /// <summary>
    /// Gets the number of navigation nodes in the snapshot.
    /// </summary>
    public int NodeCount { get; }

    /// <summary>
    /// Returns whether the specified node is currently walkable.
    /// </summary>
    /// <param name="node">Node to test.</param>
    /// <returns><see langword="true"/> when the node is walkable.</returns>
    public bool IsNodeWalkable(NavigationNodeId node);

    /// <summary>
    /// Attempts to resolve a node to a discrete cell position.
    /// </summary>
    /// <param name="node">Node to resolve.</param>
    /// <param name="position">Receives the cell position when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetNodePosition(NavigationNodeId node, out Int2 position);

    /// <summary>
    /// Attempts to resolve a cell position to a navigation node.
    /// </summary>
    /// <param name="position">Cell position.</param>
    /// <param name="node">Receives the node when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetNodeAt(Int2 position, out NavigationNodeId node);
}
