using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Immutable navigation path query parameters.
/// </summary>
/// <remarks>
/// Query values are copied by value into host services. Prefer valid navigation
/// node identifiers when available. When nodes are
/// <see cref="NavigationNodeId.Invalid"/>, hosts may resolve
/// <see cref="OriginCell"/> and <see cref="DestinationCell"/> instead.
/// </remarks>
public readonly struct NavigationPathQuery
{
    /// <summary>
    /// Initializes a new navigation path query.
    /// </summary>
    /// <param name="agent">Agent requesting the path.</param>
    /// <param name="origin">Origin navigation node.</param>
    /// <param name="destination">Destination navigation node.</param>
    /// <param name="originCell">Origin cell used when nodes are unresolved.</param>
    /// <param name="destinationCell">Destination cell used when nodes are unresolved.</param>
    /// <param name="maximumCost">Maximum acceptable path cost; zero means unlimited within hard limits.</param>
    /// <param name="allowPartial">Whether a truncated path may be returned.</param>
    public NavigationPathQuery(
        AgentId agent,
        NavigationNodeId origin,
        NavigationNodeId destination,
        Int2 originCell,
        Int2 destinationCell,
        int maximumCost,
        bool allowPartial)
    {
        Agent = agent;
        Origin = origin;
        Destination = destination;
        OriginCell = originCell;
        DestinationCell = destinationCell;
        MaximumCost = maximumCost;
        AllowPartial = allowPartial;
    }

    /// <summary>
    /// Gets the agent requesting the path.
    /// </summary>
    public AgentId Agent { get; }

    /// <summary>
    /// Gets the origin navigation node.
    /// </summary>
    public NavigationNodeId Origin { get; }

    /// <summary>
    /// Gets the destination navigation node.
    /// </summary>
    public NavigationNodeId Destination { get; }

    /// <summary>
    /// Gets the origin cell used when nodes are unresolved.
    /// </summary>
    public Int2 OriginCell { get; }

    /// <summary>
    /// Gets the destination cell used when nodes are unresolved.
    /// </summary>
    public Int2 DestinationCell { get; }

    /// <summary>
    /// Gets the maximum acceptable path cost; zero means unlimited within hard limits.
    /// </summary>
    public int MaximumCost { get; }

    /// <summary>
    /// Gets a value indicating whether a truncated path may be returned.
    /// </summary>
    public bool AllowPartial { get; }
}
