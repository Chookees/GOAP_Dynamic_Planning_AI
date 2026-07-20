using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Memory;

namespace TacticalGoap.Runtime.Perception;

/// <summary>
/// Bounded squad-message snapshot consumed by <see cref="SquadMessageSensor"/>.
/// </summary>
public struct SquadMessageSnapshot
{
    /// <summary>
    /// Gets or sets the sending agent entity.
    /// </summary>
    public EntityId SourceEntity { get; set; }

    /// <summary>
    /// Gets or sets the related contact entity, when known.
    /// </summary>
    public EntityId RelatedEntity { get; set; }

    /// <summary>
    /// Gets or sets the communication intent type as an integer payload.
    /// </summary>
    public CommunicationIntentType Intent { get; set; }

    /// <summary>
    /// Gets or sets a believed position; may be uncertain.
    /// </summary>
    public Int2 Position { get; set; }

    /// <summary>
    /// Gets or sets flags describing certainty and designation.
    /// </summary>
    public MemoryRecordFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets an optional order identifier value, or -1 when unused.
    /// </summary>
    public int OrderIdValue { get; set; }
}

/// <summary>
/// Sensor that ingests bounded squad messages into working memory.
/// </summary>
public sealed class SquadMessageSensor
{
    private int _tickCount;

    /// <summary>
    /// Gets the number of ticks processed by this sensor instance.
    /// </summary>
    public int TickCount => _tickCount;

    /// <summary>
    /// Writes communication and order memory from a caller-owned message span.
    /// </summary>
    /// <param name="messages">Bounded message snapshots for this tick.</param>
    /// <param name="context">Shared perception context.</param>
    /// <returns>Success or invalid-argument when the span exceeds hard limits.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(ReadOnlySpan<SquadMessageSnapshot> messages, PerceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _tickCount = checked(_tickCount + 1);

        int count = messages.Length;
        if (count > AiHardLimits.MaximumCommunicationRequests)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < count; i++)
        {
            WriteMessage(messages[i], context);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static void WriteMessage(in SquadMessageSnapshot message, PerceptionContext context)
    {
        long tick = context.Tick.Sequence;
        long ttl = context.DefaultTtlTicks;
        MemoryRecordFlags flags = message.Flags | MemoryRecordFlags.UncertainPosition;
        if (message.RelatedEntity.IsValid
            && (message.Flags & MemoryRecordFlags.SquadDesignated) != 0)
        {
            flags |= MemoryRecordFlags.SquadDesignated;
        }

        MemoryType type = message.OrderIdValue >= 0
            ? MemoryType.SquadOrderReceived
            : MemoryType.CommunicationReceived;

        MemoryRecord record = new MemoryRecord
        {
            Type = type,
            SourceEntity = message.SourceEntity,
            RelatedEntity = message.RelatedEntity,
            Position = message.Position,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = ttl > 0L ? checked(tick + ttl) : -1L,
            Confidence = context.DefaultConfidence,
            Flags = flags,
            Payload0 = (int)message.Intent,
            Payload1 = message.OrderIdValue,
        };

        _ = context.Memory.InsertOrUpdate(record);
    }
}
