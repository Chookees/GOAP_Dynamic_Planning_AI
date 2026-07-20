using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Diagnostics;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Memory;

namespace TacticalGoap.Runtime.Selection;

/// <summary>
/// Deterministic focus-target selector over working-memory evidence.
/// </summary>
/// <remarks>
/// Unseen entities (no memory evidence) are never selected. Ties break on
/// ascending <see cref="EntityId"/>. Scoring uses integer weights only.
/// </remarks>
public sealed class TargetSelector
{
    private readonly MemoryRecord[] _scratch;

    /// <summary>
    /// Initializes the selector with a memory scratch buffer.
    /// </summary>
    public TargetSelector()
    {
        _scratch = new MemoryRecord[AiHardLimits.MaximumMemoryRecords];
    }

    /// <summary>
    /// Selects a focus target from memory evidence.
    /// </summary>
    /// <param name="memory">Agent working memory.</param>
    /// <param name="request">Selection inputs including hysteresis.</param>
    /// <param name="spatial">Optional spatial service for distance bands.</param>
    /// <param name="trace">Optional trace sink.</param>
    /// <returns>Selection result.</returns>
    [FrozenRuntimePath]
    public TargetSelectionResult Select(
        WorkingMemoryStore memory,
        in TargetSelectionRequest request,
        ISpatialQueryService? spatial,
        IRuntimeTraceSink? trace)
    {
        ArgumentNullException.ThrowIfNull(memory);

        Span<MemoryRecord> scratch = _scratch.AsSpan();
        OperationStatus copy = memory.CopyTo(scratch, out int count);
        if (copy != OperationStatus.Success)
        {
            return new TargetSelectionResult(EntityId.Invalid, 0, request.CurrentFocus.IsValid);
        }

        int limit = request.MaxCandidates;
        if (limit <= 0 || limit > AiHardLimits.MaximumPerceptionCandidates)
        {
            limit = AiHardLimits.MaximumPerceptionCandidates;
        }

        EntityId bestEntity = EntityId.Invalid;
        int bestScore = int.MinValue;
        int considered = 0;

        for (int i = 0; i < count && considered < limit; i++)
        {
            ref MemoryRecord record = ref scratch[i];
            if (!IsTargetEvidence(record) || !record.RelatedEntity.IsValid)
            {
                continue;
            }

            considered = checked(considered + 1);
            int score = ScoreCandidate(record, request, spatial);
            if (IsBetter(score, record.RelatedEntity, bestScore, bestEntity))
            {
                bestScore = score;
                bestEntity = record.RelatedEntity;
            }
        }

        bool changed = bestEntity != request.CurrentFocus;
        if (changed && trace is not null)
        {
            WriteTrace(request, bestEntity, bestScore, trace);
        }

        return new TargetSelectionResult(
            bestEntity,
            bestEntity.IsValid ? bestScore : 0,
            changed);
    }

    [FrozenRuntimePath]
    private static bool IsTargetEvidence(in MemoryRecord record)
    {
        return record.Type == MemoryType.TargetSeen
            || record.Type == MemoryType.TargetHeard
            || record.Type == MemoryType.DamageReceived
            || (record.Type == MemoryType.CommunicationReceived
                && record.RelatedEntity.IsValid);
    }

    [FrozenRuntimePath]
    private static int ScoreCandidate(
        in MemoryRecord record,
        in TargetSelectionRequest request,
        ISpatialQueryService? spatial)
    {
        int score = record.Confidence;
        score = checked(score + ScoreVisibility(record));
        score = checked(score + ScoreRecency(record, request.CurrentTick));
        score = checked(score + ScoreDistance(record, request, spatial));
        score = checked(score + ScoreThreat(record));
        score = checked(score + ScoreHysteresis(record, request));
        score = checked(score + ScoreSquad(record));
        return score;
    }

    [FrozenRuntimePath]
    private static int ScoreVisibility(in MemoryRecord record)
    {
        if (record.Type == MemoryType.TargetSeen
            || (record.Flags & MemoryRecordFlags.Visible) != 0)
        {
            return 1000;
        }

        if (record.Type == MemoryType.TargetHeard)
        {
            return 200;
        }

        return 0;
    }

    [FrozenRuntimePath]
    private static int ScoreRecency(in MemoryRecord record, long currentTick)
    {
        long age = currentTick - record.UpdateTick;
        if (age < 0L)
        {
            age = 0L;
        }

        if (age > 100L)
        {
            age = 100L;
        }

        return 100 - (int)age;
    }

    [FrozenRuntimePath]
    private static int ScoreDistance(
        in MemoryRecord record,
        in TargetSelectionRequest request,
        ISpatialQueryService? spatial)
    {
        if ((record.Flags & MemoryRecordFlags.UncertainPosition) != 0)
        {
            return 50;
        }

        DistanceCategory category;
        if (spatial is not null)
        {
            category = spatial.ClassifyDistance(request.AgentPosition, record.Position);
        }
        else
        {
            category = ClassifyManhattan(request.AgentPosition, record.Position);
        }

        return category switch
        {
            DistanceCategory.Near => 300,
            DistanceCategory.Medium => 200,
            DistanceCategory.Far => 100,
            _ => 0,
        };
    }

    [FrozenRuntimePath]
    private static DistanceCategory ClassifyManhattan(Int2 from, Int2 to)
    {
        int d = Int2.ManhattanDistance(from, to);
        if (d <= 4)
        {
            return DistanceCategory.Near;
        }

        if (d <= 10)
        {
            return DistanceCategory.Medium;
        }

        if (d <= 20)
        {
            return DistanceCategory.Far;
        }

        return DistanceCategory.OutOfRange;
    }

    [FrozenRuntimePath]
    private static int ScoreThreat(in MemoryRecord record)
    {
        int score = 0;
        if (record.Type == MemoryType.DamageReceived
            || (record.Flags & MemoryRecordFlags.DamageAttributed) != 0)
        {
            score = checked(score + 400);
        }

        return score;
    }

    [FrozenRuntimePath]
    private static int ScoreHysteresis(in MemoryRecord record, in TargetSelectionRequest request)
    {
        if (!request.CurrentFocus.IsValid || record.RelatedEntity != request.CurrentFocus)
        {
            return 0;
        }

        int bonus = request.HysteresisBonus;
        return bonus > 0 ? bonus : 150;
    }

    [FrozenRuntimePath]
    private static int ScoreSquad(in MemoryRecord record)
    {
        return (record.Flags & MemoryRecordFlags.SquadDesignated) != 0 ? 250 : 0;
    }

    [FrozenRuntimePath]
    private static bool IsBetter(int score, EntityId entity, int bestScore, EntityId bestEntity)
    {
        if (!bestEntity.IsValid)
        {
            return true;
        }

        if (score > bestScore)
        {
            return true;
        }

        if (score < bestScore)
        {
            return false;
        }

        return entity.CompareTo(bestEntity) < 0;
    }

    [FrozenRuntimePath]
    private static void WriteTrace(
        in TargetSelectionRequest request,
        EntityId selected,
        int score,
        IRuntimeTraceSink trace)
    {
        AiTick tick = new AiTick(request.CurrentTick, 1);
        trace.Write(
            new TraceRecord(
                tick,
                request.Agent,
                request.Squad,
                DiagnosticSubsystem.TargetSelection,
                (int)TraceEventCode.TargetSelected,
                selected.IsValid ? selected.Value : -1,
                request.CurrentFocus.IsValid ? request.CurrentFocus.Value : -1,
                score,
                0,
                (int)OperationStatus.Success));
    }
}
