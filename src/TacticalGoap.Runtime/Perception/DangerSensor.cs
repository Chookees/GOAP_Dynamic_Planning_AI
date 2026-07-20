using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Memory;

namespace TacticalGoap.Runtime.Perception;

/// <summary>
/// Danger sensor that records danger cells and may request an immediate interrupt.
/// </summary>
public sealed class DangerSensor
{
    private int _tickCount;

    /// <summary>
    /// Gets the number of ticks processed by this sensor instance.
    /// </summary>
    public int TickCount => _tickCount;

    /// <summary>
    /// Consumes danger positions and writes immediate-danger memory records.
    /// </summary>
    /// <param name="provider">Host danger provider.</param>
    /// <param name="context">Shared perception context.</param>
    /// <returns>Success or host/capacity failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(IDangerProvider provider, PerceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(context);
        _tickCount = checked(_tickCount + 1);

        Span<Int2> buffer = context.DangerScratch.AsSpan(0, AiHardLimits.MaximumDangerEvents);
        OperationStatus copyStatus = provider.CopyDangerPositions(buffer, out int count);
        if (copyStatus != OperationStatus.Success && copyStatus != OperationStatus.CapacityExceeded)
        {
            return copyStatus;
        }

        if (count > AiHardLimits.MaximumDangerEvents)
        {
            count = AiHardLimits.MaximumDangerEvents;
        }

        for (int i = 0; i < count; i++)
        {
            ProcessDanger(buffer[i], provider, context);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static void ProcessDanger(Int2 position, IDangerProvider provider, PerceptionContext context)
    {
        int intensity = 1;
        if (provider.TryGetDangerIntensity(position, out int reported) == OperationStatus.Success
            && reported > 0)
        {
            intensity = reported;
        }

        long tick = context.Tick.Sequence;
        long ttl = context.DefaultTtlTicks;
        MemoryType type = intensity >= 5 ? MemoryType.GrenadeDetected : MemoryType.DangerDetected;
        MemoryRecord record = new MemoryRecord
        {
            Type = type,
            SourceEntity = EntityId.Invalid,
            RelatedEntity = EntityId.Invalid,
            Position = position,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = ttl > 0L ? checked(tick + ttl) : -1L,
            Confidence = context.DefaultConfidence,
            Flags = MemoryRecordFlags.ImmediateDanger,
            Payload0 = intensity,
        };

        _ = context.Memory.InsertOrUpdate(record);

        int distance = Int2.ManhattanDistance(context.AgentPosition, position);
        if (distance <= context.DangerInterruptRangeCells)
        {
            context.ImmediateInterruptRequested = true;
        }
    }
}
