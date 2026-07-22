using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Memory;

namespace DynamicPlanningAI.Runtime.Perception;

/// <summary>
/// Hearing sensor that writes uncertain auditory evidence without omniscient positions.
/// </summary>
public sealed class HearingSensor
{
    private int _tickCount;

    /// <summary>
    /// Gets the number of ticks processed by this sensor instance.
    /// </summary>
    public int TickCount => _tickCount;

    /// <summary>
    /// Consumes heard candidates and writes <see cref="MemoryType.TargetHeard"/> records.
    /// </summary>
    /// <param name="input">Host perception snapshot.</param>
    /// <param name="context">Shared perception context.</param>
    /// <returns>Success or host/capacity failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(IPerceptionInput input, PerceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        _tickCount = checked(_tickCount + 1);

        Span<EntityId> buffer = context.SoundScratch.AsSpan(0, AiHardLimits.MaximumSoundEvents);
        OperationStatus copyStatus = input.CopyHeardEntities(context.Agent, buffer, out int count);
        if (copyStatus != OperationStatus.Success && copyStatus != OperationStatus.CapacityExceeded)
        {
            return copyStatus;
        }

        if (count > AiHardLimits.MaximumSoundEvents)
        {
            count = AiHardLimits.MaximumSoundEvents;
        }

        for (int i = 0; i < count; i++)
        {
            TryWriteHeard(buffer[i], context);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static void TryWriteHeard(EntityId entity, PerceptionContext context)
    {
        // Hearing never queries host entity positions: that would be omniscient.
        EntityId related = entity.IsValid ? entity : EntityId.Invalid;
        MemoryRecordFlags flags = MemoryRecordFlags.UncertainPosition;
        if (!related.IsValid)
        {
            flags |= MemoryRecordFlags.UncertainSource;
        }

        long tick = context.Tick.Sequence;
        long ttl = context.DefaultTtlTicks;
        int confidence = context.DefaultConfidence / 2;
        if (confidence < 1)
        {
            confidence = 1;
        }

        MemoryRecord record = new MemoryRecord
        {
            Type = MemoryType.TargetHeard,
            SourceEntity = EntityId.FromInt32(context.Agent.Value),
            RelatedEntity = related,
            Position = Int2.Zero,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = ttl > 0L ? checked(tick + ttl) : -1L,
            Confidence = confidence,
            Flags = flags,
        };

        _ = context.Memory.InsertOrUpdate(record);
    }
}
