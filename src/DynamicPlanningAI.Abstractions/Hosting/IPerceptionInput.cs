using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Bounded perception snapshot accessors for the current tick.
/// </summary>
/// <remarks>
/// <para>
/// Buffer ownership: the caller owns destination spans. Implementations copy at
/// most <c>destination.Length</c> entries and never retain the span.
/// </para>
/// <para>
/// Determinism: snapshot contents are frozen for the tick that produced them.
/// Ordering must be stable for identical host perception state.
/// </para>
/// <para>
/// Allocation: implementations must not allocate on the frozen runtime path.
/// Results exceeding buffer capacity return
/// <see cref="OperationStatus.CapacityExceeded"/>.
/// </para>
/// </remarks>
public interface IPerceptionInput
{
    /// <summary>
    /// Copies currently visible entity identifiers for an agent.
    /// </summary>
    /// <param name="agent">Perceiving agent.</param>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of entities written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus CopyVisibleEntities(
        AgentId agent,
        Span<EntityId> destination,
        out int writtenCount);

    /// <summary>
    /// Copies currently heard entity identifiers for an agent.
    /// </summary>
    /// <param name="agent">Perceiving agent.</param>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of entities written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus CopyHeardEntities(
        AgentId agent,
        Span<EntityId> destination,
        out int writtenCount);

    /// <summary>
    /// Copies damage source entity identifiers received this tick.
    /// </summary>
    /// <param name="agent">Damaged agent.</param>
    /// <param name="destination">Caller-owned destination buffer.</param>
    /// <param name="writtenCount">Receives the number of entities written.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus CopyDamageSources(
        AgentId agent,
        Span<EntityId> destination,
        out int writtenCount);
}
