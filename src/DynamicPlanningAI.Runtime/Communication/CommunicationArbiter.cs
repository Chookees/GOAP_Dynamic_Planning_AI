using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;

namespace DynamicPlanningAI.Runtime.Communication;

/// <summary>
/// Bounded arbitrator for semantic communication intents.
/// </summary>
/// <remarks>
/// Applies priority, recency, duplicate suppression, speaker cooldown, squad
/// channel cooldown, context validity, and max-concurrent emission caps.
/// Emission order is deterministic: score descending, then intent id ascending.
/// </remarks>
public sealed partial class CommunicationArbiter
{
    private readonly CommunicationArbiterOptions _options;
    private readonly CommunicationRequest[] _pending;
    private readonly bool[] _pendingLive;
    private readonly long[] _speakerLastEmit;
    private readonly long[] _squadLastEmit;
    private readonly int[] _scoreScratch;
    private readonly int[] _orderScratch;
    private readonly CommunicationEmission[] _recent;
    private readonly long[] _recentTick;
    private int _pendingCount;
    private int _nextIntentRawId;
    private int _recentCount;
    private long _currentTick;

    /// <summary>
    /// Initializes the arbiter with validated options.
    /// </summary>
    /// <param name="options">Arbitration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when options fail validation.</exception>
    public CommunicationArbiter(CommunicationArbiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Validate())
        {
            throw new ArgumentException("CommunicationArbiterOptions failed validation.", nameof(options));
        }

        _options = options;
        _pending = new CommunicationRequest[options.MaxPendingRequests];
        _pendingLive = new bool[options.MaxPendingRequests];
        _speakerLastEmit = new long[AiHardLimits.MaximumAgents];
        _squadLastEmit = new long[AiHardLimits.MaximumSquads];
        _scoreScratch = new int[options.MaxPendingRequests];
        _orderScratch = new int[options.MaxPendingRequests];
        _recent = new CommunicationEmission[options.MaxPendingRequests];
        _recentTick = new long[options.MaxPendingRequests];
        _pendingCount = 0;
        _nextIntentRawId = 0;
        _recentCount = 0;
        _currentTick = 0L;

        for (int i = 0; i < AiHardLimits.MaximumAgents; i++)
        {
            _speakerLastEmit[i] = long.MinValue / 4;
        }

        for (int i = 0; i < AiHardLimits.MaximumSquads; i++)
        {
            _squadLastEmit[i] = long.MinValue / 4;
        }
    }

    /// <summary>Gets the number of live pending requests.</summary>
    public int PendingCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _pendingCount; i++)
            {
                if (_pendingLive[i])
                {
                    count = checked(count + 1);
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Synchronizes the arbiter clock before request submission mid-tick.
    /// </summary>
    /// <param name="tickSequence">Current tick sequence.</param>
    [FrozenRuntimePath]
    public void SynchronizeTick(long tickSequence)
    {
        if (tickSequence >= 0)
        {
            _currentTick = tickSequence;
        }
    }

    /// <summary>
    /// Submits a semantic intent request (ICommunicationSink-compatible entry).
    /// </summary>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="intent">Semantic intent type.</param>
    /// <param name="relatedEntity">Optional related entity.</param>
    /// <param name="squad">Optional squad channel.</param>
    /// <param name="priority">Priority boost; zero uses type default only.</param>
    /// <param name="expiryTick">Expiry tick; non-positive uses default TTL from current tick.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Request(
        AgentId speaker,
        CommunicationIntentType intent,
        EntityId relatedEntity,
        SquadId squad,
        int priority,
        long expiryTick)
    {
        if (!speaker.IsValid || speaker.Value >= AiHardLimits.MaximumAgents)
        {
            return OperationStatus.InvalidArgument;
        }

        if (intent > CommunicationIntentType.OrderFailed)
        {
            return OperationStatus.InvalidArgument;
        }

        int slot = FindFreePendingSlot();
        if (slot < 0)
        {
            return OperationStatus.CapacityExceeded;
        }

        long submitted = _currentTick;
        long expiry = expiryTick > submitted
            ? expiryTick
            : checked(submitted + _options.DefaultTimeToLiveTicks);

        CommunicationIntentId intentId = CommunicationIntentId.FromInt32(_nextIntentRawId);
        _nextIntentRawId = checked(_nextIntentRawId + 1);

        _pending[slot] = new CommunicationRequest(
            intentId,
            speaker,
            squad,
            intent,
            relatedEntity,
            priority,
            submitted,
            expiry);
        _pendingLive[slot] = true;
        if (slot >= _pendingCount)
        {
            _pendingCount = checked(slot + 1);
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// ICommunicationSink-shaped convenience overload without squad metadata.
    /// </summary>
    /// <param name="speaker">Speaking agent.</param>
    /// <param name="intent">Semantic intent type.</param>
    /// <param name="relatedEntity">Optional related entity.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Request(AgentId speaker, CommunicationIntentType intent, EntityId relatedEntity)
    {
        return Request(speaker, intent, relatedEntity, SquadId.Invalid, priority: 0, expiryTick: 0L);
    }

    /// <summary>
    /// Advances arbitration for one tick and writes emissions into a caller buffer.
    /// </summary>
    /// <param name="tick">Host tick stamp.</param>
    /// <param name="destination">Caller-owned emission buffer.</param>
    /// <param name="emittedCount">Receives the number of emissions written.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(AiTick tick, Span<CommunicationEmission> destination, out int emittedCount)
    {
        emittedCount = 0;
        if (tick.Sequence < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        _currentTick = tick.Sequence;
        ExpirePending(tick.Sequence);

        int candidateCount = BuildCandidateOrder(tick.Sequence);
        SortCandidatesDeterministic(candidateCount);

        int maxEmit = _options.MaxConcurrentEmissions;
        if (destination.Length < maxEmit)
        {
            maxEmit = destination.Length;
        }

        for (int i = 0; i < candidateCount && emittedCount < maxEmit; i++)
        {
            int pendingIndex = _orderScratch[i];
            if (!_pendingLive[pendingIndex])
            {
                continue;
            }

            CommunicationRequest request = _pending[pendingIndex];
            if (!PassesRuntimeGates(in request, tick.Sequence))
            {
                continue;
            }

            int score = _scoreScratch[pendingIndex];
            CommunicationEmission emission = new(
                request.IntentId,
                request.Speaker,
                request.Squad,
                request.Intent,
                request.RelatedEntity,
                score,
                tick.Sequence);

            destination[emittedCount] = emission;
            emittedCount = checked(emittedCount + 1);
            _pendingLive[pendingIndex] = false;
            RecordEmission(in emission);
        }

        CompactPending();
        return OperationStatus.Success;
    }

    /// <summary>
    /// Returns the default base priority for an intent type.
    /// </summary>
    /// <param name="intent">Intent type.</param>
    /// <returns>Non-negative base priority.</returns>
    public static int GetBasePriority(CommunicationIntentType intent)
    {
        return intent switch
        {
            CommunicationIntentType.GrenadeWarning => 100,
            CommunicationIntentType.TakingFire => 90,
            CommunicationIntentType.CoverCompromised => 85,
            CommunicationIntentType.NeedAssistance => 80,
            CommunicationIntentType.ContactSpotted => 70,
            CommunicationIntentType.ThrowingGrenade => 65,
            CommunicationIntentType.ContactLost => 60,
            CommunicationIntentType.OrderFailed => 55,
            CommunicationIntentType.MovingToCover => 50,
            CommunicationIntentType.Suppressing => 45,
            CommunicationIntentType.Advancing => 40,
            CommunicationIntentType.Reloading => 35,
            CommunicationIntentType.DoorBlocked => 30,
            CommunicationIntentType.BreachingDoor => 30,
            CommunicationIntentType.NoValidRoute => 30,
            CommunicationIntentType.SearchingSector => 25,
            CommunicationIntentType.SectorClear => 25,
            CommunicationIntentType.HoldingPosition => 20,
            CommunicationIntentType.OrderAcknowledged => 15,
            _ => 10,
        };
    }
}
