using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Communication;

/// <summary>
/// Pending semantic communication request awaiting arbitration.
/// </summary>
public readonly struct CommunicationRequest
{
    /// <summary>
    /// Initializes a communication request.
    /// </summary>
    /// <param name="intentId">Stable intent identifier.</param>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="squad">Speaker squad; may be invalid.</param>
    /// <param name="intent">Semantic intent type.</param>
    /// <param name="relatedEntity">Optional related entity.</param>
    /// <param name="priority">Caller priority; higher wins. Zero uses type default.</param>
    /// <param name="submittedTick">Tick when submitted.</param>
    /// <param name="expiryTick">Tick after which the request is invalid.</param>
    public CommunicationRequest(
        CommunicationIntentId intentId,
        AgentId speaker,
        SquadId squad,
        CommunicationIntentType intent,
        EntityId relatedEntity,
        int priority,
        long submittedTick,
        long expiryTick)
    {
        IntentId = intentId;
        Speaker = speaker;
        Squad = squad;
        Intent = intent;
        RelatedEntity = relatedEntity;
        Priority = priority;
        SubmittedTick = submittedTick;
        ExpiryTick = expiryTick;
    }

    /// <summary>Gets the stable intent identifier.</summary>
    public CommunicationIntentId IntentId { get; }

    /// <summary>Gets the speaking agent.</summary>
    public AgentId Speaker { get; }

    /// <summary>Gets the speaker squad; may be invalid.</summary>
    public SquadId Squad { get; }

    /// <summary>Gets the semantic intent type.</summary>
    public CommunicationIntentType Intent { get; }

    /// <summary>Gets the optional related entity.</summary>
    public EntityId RelatedEntity { get; }

    /// <summary>Gets the caller priority boost.</summary>
    public int Priority { get; }

    /// <summary>Gets the submission tick.</summary>
    public long SubmittedTick { get; }

    /// <summary>Gets the expiry tick.</summary>
    public long ExpiryTick { get; }

    /// <summary>Gets a value indicating structural validity.</summary>
    public bool IsStructurallyValid =>
        IntentId.IsValid && Speaker.IsValid && Intent <= CommunicationIntentType.OrderFailed;
}
