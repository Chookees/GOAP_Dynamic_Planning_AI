using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Communication;

/// <summary>
/// Semantic intent selected for emission by <see cref="CommunicationArbiter"/>.
/// </summary>
/// <remarks>
/// Core never produces spoken sentences. Hosts map emissions through
/// <see cref="Abstractions.Hosting.ICommunicationSink"/> adapters later.
/// </remarks>
public readonly struct CommunicationEmission
{
    /// <summary>
    /// Initializes an emission record.
    /// </summary>
    /// <param name="intentId">Stable intent identifier.</param>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="squad">Speaker squad; may be invalid.</param>
    /// <param name="intent">Semantic intent type.</param>
    /// <param name="relatedEntity">Optional related entity.</param>
    /// <param name="score">Arbitration score used for ordering.</param>
    /// <param name="emittedTick">Tick of emission.</param>
    public CommunicationEmission(
        CommunicationIntentId intentId,
        AgentId speaker,
        SquadId squad,
        CommunicationIntentType intent,
        EntityId relatedEntity,
        int score,
        long emittedTick)
    {
        IntentId = intentId;
        Speaker = speaker;
        Squad = squad;
        Intent = intent;
        RelatedEntity = relatedEntity;
        Score = score;
        EmittedTick = emittedTick;
    }

    /// <summary>Gets the stable intent identifier.</summary>
    public CommunicationIntentId IntentId { get; }

    /// <summary>Gets the speaking agent.</summary>
    public AgentId Speaker { get; }

    /// <summary>Gets the speaker squad.</summary>
    public SquadId Squad { get; }

    /// <summary>Gets the semantic intent type.</summary>
    public CommunicationIntentType Intent { get; }

    /// <summary>Gets the optional related entity.</summary>
    public EntityId RelatedEntity { get; }

    /// <summary>Gets the arbitration score.</summary>
    public int Score { get; }

    /// <summary>Gets the emission tick.</summary>
    public long EmittedTick { get; }
}
