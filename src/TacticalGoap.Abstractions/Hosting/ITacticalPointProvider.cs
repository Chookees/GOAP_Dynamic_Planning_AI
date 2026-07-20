using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host provider of scenario tactical points.
/// </summary>
/// <remarks>
/// <para>
/// Buffer ownership: the caller owns destination spans. Implementations copy at
/// most <c>destination.Length</c> identifiers and never retain the span.
/// </para>
/// <para>
/// Determinism: point sets are fixed after freeze unless the host documents
/// dynamic invalidation. Ordering must be stable.
/// </para>
/// <para>
/// Allocation: implementations must not allocate on the frozen runtime path.
/// </para>
/// </remarks>
public interface ITacticalPointProvider
{
    /// <summary>
    /// Copies tactical point identifiers for a category.
    /// </summary>
    /// <param name="category">Category filter.</param>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of points written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus CopyPoints(
        TacticalPointCategory category,
        Span<TacticalPointId> destination,
        out int writtenCount);

    /// <summary>
    /// Attempts to read a tactical point cell position.
    /// </summary>
    /// <param name="point">Point to locate.</param>
    /// <param name="position">Receives the position when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetPosition(TacticalPointId point, out Int2 position);

    /// <summary>
    /// Attempts to read the category of a tactical point.
    /// </summary>
    /// <param name="point">Point to query.</param>
    /// <param name="category">Receives the category when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetCategory(TacticalPointId point, out TacticalPointCategory category);

    /// <summary>
    /// Returns whether the tactical point is currently valid for use.
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns><see langword="true"/> when valid.</returns>
    public bool IsValid(TacticalPointId point);
}
