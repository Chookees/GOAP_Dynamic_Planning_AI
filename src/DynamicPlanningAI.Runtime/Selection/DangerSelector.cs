using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Memory;

namespace DynamicPlanningAI.Runtime.Selection;

/// <summary>
/// Result of danger focus selection.
/// </summary>
public readonly struct DangerSelectionResult
{
    /// <summary>
    /// Initializes a danger selection result.
    /// </summary>
    /// <param name="position">Selected danger cell.</param>
    /// <param name="intensity">Selected intensity.</param>
    /// <param name="found">Whether a danger record was selected.</param>
    public DangerSelectionResult(Int2 position, int intensity, bool found)
    {
        Position = position;
        Intensity = intensity;
        Found = found;
    }

    /// <summary>
    /// Gets the selected danger cell.
    /// </summary>
    public Int2 Position { get; }

    /// <summary>
    /// Gets the selected intensity.
    /// </summary>
    public int Intensity { get; }

    /// <summary>
    /// Gets whether a danger was found.
    /// </summary>
    public bool Found { get; }
}

/// <summary>
/// Bounded danger selector over immediate-danger memory records.
/// </summary>
public sealed class DangerSelector
{
    private readonly MemoryRecord[] _scratch;

    /// <summary>
    /// Initializes the selector with a memory scratch buffer.
    /// </summary>
    public DangerSelector()
    {
        _scratch = new MemoryRecord[AiHardLimits.MaximumMemoryRecords];
    }

    /// <summary>
    /// Selects the highest-intensity nearby danger record.
    /// </summary>
    /// <param name="memory">Agent working memory.</param>
    /// <param name="agentPosition">Agent cell.</param>
    /// <param name="maxCandidates">Maximum danger records considered.</param>
    /// <returns>Selection result.</returns>
    [FrozenRuntimePath]
    public DangerSelectionResult Select(
        WorkingMemoryStore memory,
        Int2 agentPosition,
        int maxCandidates)
    {
        ArgumentNullException.ThrowIfNull(memory);

        Span<MemoryRecord> scratch = _scratch.AsSpan();
        if (memory.CopyTo(scratch, out int count) != OperationStatus.Success)
        {
            return new DangerSelectionResult(Int2.Zero, 0, false);
        }

        int limit = maxCandidates;
        if (limit <= 0 || limit > AiHardLimits.MaximumDangerEvents)
        {
            limit = AiHardLimits.MaximumDangerEvents;
        }

        Int2 bestPosition = Int2.Zero;
        int bestIntensity = -1;
        int bestDistance = int.MaxValue;
        int considered = 0;
        bool found = false;

        for (int i = 0; i < count && considered < limit; i++)
        {
            ref MemoryRecord record = ref scratch[i];
            if (!IsDanger(record))
            {
                continue;
            }

            considered = checked(considered + 1);
            int intensity = record.Payload0 > 0 ? record.Payload0 : 1;
            int distance = Int2.ManhattanDistance(agentPosition, record.Position);
            if (!found
                || intensity > bestIntensity
                || (intensity == bestIntensity && distance < bestDistance)
                || (intensity == bestIntensity && distance == bestDistance
                    && ComparePosition(record.Position, bestPosition) < 0))
            {
                found = true;
                bestIntensity = intensity;
                bestDistance = distance;
                bestPosition = record.Position;
            }
        }

        return new DangerSelectionResult(bestPosition, found ? bestIntensity : 0, found);
    }

    [FrozenRuntimePath]
    private static bool IsDanger(in MemoryRecord record)
    {
        return record.Type == MemoryType.DangerDetected
            || record.Type == MemoryType.GrenadeDetected
            || (record.Flags & MemoryRecordFlags.ImmediateDanger) != 0;
    }

    [FrozenRuntimePath]
    private static int ComparePosition(Int2 left, Int2 right)
    {
        int cmp = left.X.CompareTo(right.X);
        return cmp != 0 ? cmp : left.Y.CompareTo(right.Y);
    }
}
