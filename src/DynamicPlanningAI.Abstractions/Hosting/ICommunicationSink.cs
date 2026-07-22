using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Bounded sink for semantic communication requests.
/// </summary>
/// <remarks>
/// Requests are capacity-bounded by
/// <see cref="Limits.AiHardLimits.MaximumCommunicationRequests"/>.
/// Implementations must not allocate on the frozen runtime path and must return
/// <see cref="OperationStatus.CapacityExceeded"/> when the pending queue is full.
/// The sink does not guarantee delivery; arbitration occurs in the runtime.
/// </remarks>
public interface ICommunicationSink
{
    /// <summary>
    /// Submits a communication intent for later arbitration.
    /// </summary>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="intent">Semantic intent type.</param>
    /// <param name="relatedEntity">Optional related entity; may be invalid.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus Request(
        AgentId speaker,
        CommunicationIntentType intent,
        EntityId relatedEntity);

    /// <summary>
    /// Submits a communication intent with an explicit intent identifier.
    /// </summary>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="intentId">Registered intent identifier.</param>
    /// <param name="relatedEntity">Optional related entity; may be invalid.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus Request(
        AgentId speaker,
        CommunicationIntentId intentId,
        EntityId relatedEntity);
}
