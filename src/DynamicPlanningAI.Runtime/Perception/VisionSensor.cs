using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Memory;

namespace DynamicPlanningAI.Runtime.Perception;

/// <summary>
/// Vision sensor that confirms host candidates with range, FOV, and optional LOS.
/// </summary>
public sealed class VisionSensor
{
    private int _tickCount;

    /// <summary>
    /// Gets the number of ticks processed by this sensor instance.
    /// </summary>
    public int TickCount => _tickCount;

    /// <summary>
    /// Consumes visible candidates and writes <see cref="MemoryType.TargetSeen"/> records.
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

        Span<EntityId> buffer = context.EntityScratch.AsSpan(
            0,
            AiHardLimits.MaximumPerceptionCandidates);
        OperationStatus copyStatus = input.CopyVisibleEntities(context.Agent, buffer, out int count);
        if (copyStatus != OperationStatus.Success && copyStatus != OperationStatus.CapacityExceeded)
        {
            return copyStatus;
        }

        if (count > AiHardLimits.MaximumPerceptionCandidates)
        {
            count = AiHardLimits.MaximumPerceptionCandidates;
        }

        int written = 0;
        for (int i = 0; i < count; i++)
        {
            if (TryWriteVisible(buffer[i], context))
            {
                written = checked(written + 1);
            }
        }

        WriteTrace(context, written, count);
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static bool TryWriteVisible(EntityId entity, PerceptionContext context)
    {
        if (!entity.IsValid || context.Spatial is null)
        {
            return false;
        }

        if (context.Spatial.TryGetEntityPosition(entity, out Int2 position) != OperationStatus.Success)
        {
            return false;
        }

        int distance = Int2.ManhattanDistance(context.AgentPosition, position);
        if (distance > context.VisionRangeCells)
        {
            return false;
        }

        if (!FieldOfView.IsInForwardCone(context.AgentPosition, context.AgentFacing, position))
        {
            return false;
        }

        if (context.LineOfSight is not null
            && !context.LineOfSight.HasLineOfSight(context.AgentPosition, position))
        {
            return false;
        }

        MemoryRecord record = CreateSeenRecord(entity, position, context);
        MemoryWriteResult write = context.Memory.InsertOrUpdate(record);
        return write.IsSuccess;
    }

    [FrozenRuntimePath]
    private static MemoryRecord CreateSeenRecord(EntityId entity, Int2 position, PerceptionContext context)
    {
        long tick = context.Tick.Sequence;
        long ttl = context.DefaultTtlTicks;
        return new MemoryRecord
        {
            Type = MemoryType.TargetSeen,
            SourceEntity = EntityId.FromInt32(context.Agent.Value),
            RelatedEntity = entity,
            Position = position,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = ttl > 0L ? checked(tick + ttl) : -1L,
            Confidence = context.DefaultConfidence,
            Flags = MemoryRecordFlags.Visible,
        };
    }

    [FrozenRuntimePath]
    private static void WriteTrace(PerceptionContext context, int written, int candidateCount)
    {
        if (context.Trace is null)
        {
            return;
        }

        context.Trace.Write(
            new TraceRecord(
                context.Tick,
                context.Agent,
                context.Squad,
                DiagnosticSubsystem.Perception,
                (int)TraceEventCode.PerceptionUpdated,
                written,
                candidateCount,
                0,
                0,
                (int)OperationStatus.Success));
    }
}
