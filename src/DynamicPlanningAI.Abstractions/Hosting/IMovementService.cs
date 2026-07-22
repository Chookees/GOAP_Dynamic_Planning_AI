using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Host movement queries and desired-destination control.
/// </summary>
/// <remarks>
/// Implementations must be deterministic for identical inputs within a tick.
/// Methods must not allocate on the frozen runtime path. Destination writes are
/// requests; the host retains authority over actual locomotion.
/// </remarks>
public interface IMovementService
{
    /// <summary>
    /// Attempts to read the current agent position.
    /// </summary>
    /// <param name="agent">Agent to query.</param>
    /// <param name="position">Receives the position when successful.</param>
    /// <returns>Success or a failure status.</returns>
    public OperationStatus TryGetPosition(AgentId agent, out Int2 position);

    /// <summary>
    /// Attempts to read the current agent facing.
    /// </summary>
    /// <param name="agent">Agent to query.</param>
    /// <param name="facing">Receives the facing when successful.</param>
    /// <returns>Success or a failure status.</returns>
    public OperationStatus TryGetFacing(AgentId agent, out Direction8 facing);

    /// <summary>
    /// Requests a desired destination for host locomotion.
    /// </summary>
    /// <param name="agent">Agent to move.</param>
    /// <param name="destination">Desired destination cell.</param>
    /// <returns>Success, host rejection, or validation failure.</returns>
    public OperationStatus TrySetDesiredDestination(AgentId agent, Int2 destination);

    /// <summary>
    /// Requests that the agent stop locomotion.
    /// </summary>
    /// <param name="agent">Agent to stop.</param>
    /// <returns>Success or host rejection.</returns>
    public OperationStatus TryStop(AgentId agent);

    /// <summary>
    /// Classifies the quantized distance between two cells.
    /// </summary>
    /// <param name="from">Origin cell.</param>
    /// <param name="destination">Destination cell.</param>
    /// <returns>Configured distance category.</returns>
    public DistanceCategory ClassifyDistance(Int2 from, Int2 destination);
}
