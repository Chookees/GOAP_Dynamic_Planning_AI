using System;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host provider of danger regions affecting agents.
/// </summary>
/// <remarks>
/// <para>
/// Buffer ownership: the caller owns destination spans. Implementations copy at
/// most <c>destination.Length</c> positions and never retain the span.
/// </para>
/// <para>
/// Determinism: danger sets are frozen for the current tick. Ordering must be
/// stable for identical host danger state.
/// </para>
/// <para>
/// Allocation: implementations must not allocate on the frozen runtime path.
/// </para>
/// </remarks>
public interface IDangerProvider
{
    /// <summary>
    /// Copies danger cell positions active for the current tick.
    /// </summary>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of positions written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus CopyDangerPositions(Span<Int2> destination, out int writtenCount);

    /// <summary>
    /// Returns whether a cell is currently considered dangerous.
    /// </summary>
    /// <param name="position">Cell to test.</param>
    /// <returns><see langword="true"/> when the cell is dangerous.</returns>
    public bool IsPositionDangerous(Int2 position);

    /// <summary>
    /// Attempts to read a host-defined danger intensity for a cell.
    /// </summary>
    /// <param name="position">Cell to query.</param>
    /// <param name="intensity">Receives non-negative intensity when successful.</param>
    /// <returns>Success or not-found status when no danger applies.</returns>
    public OperationStatus TryGetDangerIntensity(Int2 position, out int intensity);
}
