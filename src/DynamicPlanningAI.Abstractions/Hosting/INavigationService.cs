using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Host navigation pathfinding service.
/// </summary>
/// <remarks>
/// <para>
/// Buffer ownership: the caller owns <c>destination</c>. The service writes at
/// most <c>destination.Length</c> nodes and never retains a reference to the span
/// after the call returns.
/// </para>
/// <para>
/// Determinism: identical queries against an unchanged navigation snapshot must
/// produce identical status, written counts, and node sequences.
/// </para>
/// <para>
/// Allocation: implementations must not allocate on the frozen runtime path.
/// Capacity failures return <see cref="NavigationQueryStatus.CapacityExceeded"/>.
/// </para>
/// </remarks>
public interface INavigationService
{
    /// <summary>
    /// Attempts to find a path and write nodes into a caller-owned buffer.
    /// </summary>
    /// <param name="query">Path query parameters.</param>
    /// <param name="destination">Caller-owned destination buffer for path nodes.</param>
    /// <param name="writtenCount">Receives the number of nodes written.</param>
    /// <returns>Query result describing success, partial path, or failure.</returns>
    public NavigationQueryResult TryFindPath(
        in NavigationPathQuery query,
        Span<NavigationNodeId> destination,
        out int writtenCount);
}
