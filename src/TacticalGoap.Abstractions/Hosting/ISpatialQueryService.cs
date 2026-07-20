using System;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host spatial queries over entities and cells.
/// </summary>
/// <remarks>
/// <para>
/// Buffer ownership: the caller owns destination spans. The service writes at
/// most <c>destination.Length</c> identifiers and never retains the span.
/// </para>
/// <para>
/// Determinism: identical queries against unchanged world state must return the
/// same ordered results.
/// </para>
/// <para>
/// Allocation: implementations must not allocate on the frozen runtime path.
/// Oversized results return <see cref="OperationStatus.CapacityExceeded"/>.
/// </para>
/// </remarks>
public interface ISpatialQueryService
{
    /// <summary>
    /// Copies entity identifiers within a Chebyshev or host-defined radius.
    /// </summary>
    /// <param name="origin">Query origin cell.</param>
    /// <param name="radius">Inclusive radius in cells.</param>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of entities written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus QueryEntitiesInRadius(
        Int2 origin,
        int radius,
        Span<EntityId> destination,
        out int writtenCount);

    /// <summary>
    /// Attempts to read an entity cell position.
    /// </summary>
    /// <param name="entity">Entity to locate.</param>
    /// <param name="position">Receives the position when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetEntityPosition(EntityId entity, out Int2 position);

    /// <summary>
    /// Classifies the quantized distance between two cells.
    /// </summary>
    /// <param name="from">Origin cell.</param>
    /// <param name="destination">Destination cell.</param>
    /// <returns>Configured distance category.</returns>
    public DistanceCategory ClassifyDistance(Int2 from, Int2 destination);
}
