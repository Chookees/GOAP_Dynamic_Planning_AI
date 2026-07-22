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
/// Damage sensor that attributes incoming damage sources into working memory.
/// </summary>
public sealed class DamageSensor
{
    private int _tickCount;

    /// <summary>
    /// Gets the number of ticks processed by this sensor instance.
    /// </summary>
    public int TickCount => _tickCount;

    /// <summary>
    /// Consumes damage sources and writes <see cref="MemoryType.DamageReceived"/> records.
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

        Span<EntityId> buffer = context.DamageScratch.AsSpan(0, AiHardLimits.MaximumDamageEvents);
        OperationStatus copyStatus = input.CopyDamageSources(context.Agent, buffer, out int count);
        if (copyStatus != OperationStatus.Success && copyStatus != OperationStatus.CapacityExceeded)
        {
            return copyStatus;
        }

        if (count > AiHardLimits.MaximumDamageEvents)
        {
            count = AiHardLimits.MaximumDamageEvents;
        }

        for (int i = 0; i < count; i++)
        {
            TryWriteDamage(buffer[i], context);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static void TryWriteDamage(EntityId source, PerceptionContext context)
    {
        if (!source.IsValid)
        {
            return;
        }

        Int2 position = Int2.Zero;
        MemoryRecordFlags flags = MemoryRecordFlags.DamageAttributed | MemoryRecordFlags.UncertainPosition;
        if (context.Spatial is not null
            && context.Spatial.TryGetEntityPosition(source, out Int2 known) == OperationStatus.Success)
        {
            // Damage attribution may learn a position only when the host exposes it
            // for the damaging entity; this is not a general world query.
            position = known;
            flags = MemoryRecordFlags.DamageAttributed;
        }

        long tick = context.Tick.Sequence;
        long ttl = context.DefaultTtlTicks;
        MemoryRecord record = new MemoryRecord
        {
            Type = MemoryType.DamageReceived,
            SourceEntity = source,
            RelatedEntity = source,
            Position = position,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = ttl > 0L ? checked(tick + ttl) : -1L,
            Confidence = context.DefaultConfidence,
            Flags = flags,
        };

        _ = context.Memory.InsertOrUpdate(record);
    }
}
