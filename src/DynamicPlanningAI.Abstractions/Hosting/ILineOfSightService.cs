using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Host line-of-sight queries between discrete cells.
/// </summary>
/// <remarks>
/// Queries must be deterministic for identical geometry within a tick.
/// Implementations must not allocate on the frozen runtime path. Occlusion is
/// defined by the host; the runtime never inspects raw collision meshes.
/// </remarks>
public interface ILineOfSightService
{
    /// <summary>
    /// Returns whether clear line of sight exists between two cells.
    /// </summary>
    /// <param name="from">Origin cell.</param>
    /// <param name="destination">Destination cell.</param>
    /// <returns><see langword="true"/> when the line is unobstructed.</returns>
    public bool HasLineOfSight(Int2 from, Int2 destination);

    /// <summary>
    /// Attempts a line-of-sight query with an explicit operation status.
    /// </summary>
    /// <param name="from">Origin cell.</param>
    /// <param name="destination">Destination cell.</param>
    /// <param name="clear">Receives whether the line is unobstructed.</param>
    /// <returns>Success or host rejection.</returns>
    public OperationStatus TryQueryLineOfSight(Int2 from, Int2 destination, out bool clear);
}
