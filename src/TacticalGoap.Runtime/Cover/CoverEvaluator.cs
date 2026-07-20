using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Cover;

/// <summary>
/// Result of a bounded cover evaluation query.
/// </summary>
public readonly struct CoverQueryResult
{
    /// <summary>
    /// Initializes a cover query result.
    /// </summary>
    /// <param name="status">Operation status.</param>
    /// <param name="pointId">Selected point when successful.</param>
    /// <param name="score">Integer score of the selected point.</param>
    /// <param name="candidatesScored">Number of candidates considered.</param>
    public CoverQueryResult(OperationStatus status, TacticalPointId pointId, int score, int candidatesScored)
    {
        Status = status;
        PointId = pointId;
        Score = score;
        CandidatesScored = candidatesScored;
    }

    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public OperationStatus Status { get; }

    /// <summary>
    /// Gets the selected point when successful.
    /// </summary>
    public TacticalPointId PointId { get; }

    /// <summary>
    /// Gets the integer score of the selected point.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Gets the number of candidates considered.
    /// </summary>
    public int CandidatesScored { get; }
}

/// <summary>
/// Bounded integer cover scorer with deterministic tie-breaking by point id.
/// </summary>
/// <remarks>
/// Callers supply a nearby candidate span of at most
/// <see cref="AiHardLimits.MaximumCoverCandidates"/> entries. The evaluator never
/// scans an entire map. All arithmetic uses checked operators.
/// </remarks>
public static class CoverEvaluator
{
    /// <summary>
    /// Selects the best defensive cover from a bounded nearby candidate span.
    /// </summary>
    /// <param name="candidates">Nearby candidates (already spatially filtered).</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <returns>Best point or not-found when none are valid.</returns>
    [FrozenRuntimePath]
    public static CoverQueryResult BestDefensiveCover(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context)
    {
        return SelectBest(candidates, context, CoverQueryKind.BestDefensiveCover);
    }

    /// <summary>
    /// Selects the best advancing cover from a bounded nearby candidate span.
    /// </summary>
    /// <param name="candidates">Nearby candidates (already spatially filtered).</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <returns>Best point or not-found when none are valid.</returns>
    [FrozenRuntimePath]
    public static CoverQueryResult BestAdvancingCover(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context)
    {
        return SelectBest(candidates, context, CoverQueryKind.BestAdvancingCover);
    }

    /// <summary>
    /// Selects the best retreat cover from a bounded nearby candidate span.
    /// </summary>
    /// <param name="candidates">Nearby candidates (already spatially filtered).</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <returns>Best point or not-found when none are valid.</returns>
    [FrozenRuntimePath]
    public static CoverQueryResult BestRetreatCover(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context)
    {
        return SelectBest(candidates, context, CoverQueryKind.BestRetreatCover);
    }

    /// <summary>
    /// Selects the best suppression point from a bounded nearby candidate span.
    /// </summary>
    /// <param name="candidates">Nearby candidates (already spatially filtered).</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <returns>Best point or not-found when none are valid.</returns>
    [FrozenRuntimePath]
    public static CoverQueryResult BestSuppressionPoint(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context)
    {
        return SelectBest(candidates, context, CoverQueryKind.BestSuppressionPoint);
    }

    /// <summary>
    /// Selects the best search/observation point from a bounded nearby candidate span.
    /// </summary>
    /// <param name="candidates">Nearby candidates (already spatially filtered).</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <returns>Best point or not-found when none are valid.</returns>
    [FrozenRuntimePath]
    public static CoverQueryResult BestSearchObservationPoint(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context)
    {
        return SelectBest(candidates, context, CoverQueryKind.BestSearchObservationPoint);
    }

    /// <summary>
    /// Scores a single candidate for diagnostics and tests.
    /// </summary>
    /// <param name="sample">Candidate sample.</param>
    /// <param name="context">Shared evaluation context.</param>
    /// <param name="kind">Query intent.</param>
    /// <param name="score">Receives the integer score when valid.</param>
    /// <returns>Success when the candidate is scorable; otherwise Failed.</returns>
    [FrozenRuntimePath]
    public static OperationStatus TryScore(
        in CoverCandidateSample sample,
        in CoverEvaluationContext context,
        CoverQueryKind kind,
        out int score)
    {
        score = 0;
        if (sample.InvalidationReason != CoverInvalidationReason.None)
        {
            return OperationStatus.Failed;
        }

        score = ComputeScore(sample, context, kind);
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private static CoverQueryResult SelectBest(
        ReadOnlySpan<CoverCandidateSample> candidates,
        in CoverEvaluationContext context,
        CoverQueryKind kind)
    {
        if (candidates.Length > AiHardLimits.MaximumCoverCandidates)
        {
            return new CoverQueryResult(OperationStatus.CapacityExceeded, TacticalPointId.Invalid, 0, 0);
        }

        int bestScore = int.MinValue;
        TacticalPointId bestId = TacticalPointId.Invalid;
        int scored = 0;

        for (int i = 0; i < candidates.Length; i++)
        {
            ref readonly CoverCandidateSample sample = ref candidates[i];
            if (!sample.Point.Id.IsValid)
            {
                continue;
            }

            if (sample.InvalidationReason != CoverInvalidationReason.None)
            {
                continue;
            }

            int score = ComputeScore(sample, context, kind);
            scored = checked(scored + 1);

            if (IsBetter(score, sample.Point.Id, bestScore, bestId))
            {
                bestScore = score;
                bestId = sample.Point.Id;
            }
        }

        if (!bestId.IsValid)
        {
            return new CoverQueryResult(OperationStatus.NotFound, TacticalPointId.Invalid, 0, scored);
        }

        return new CoverQueryResult(OperationStatus.Success, bestId, bestScore, scored);
    }

    [FrozenRuntimePath]
    private static bool IsBetter(int score, TacticalPointId id, int bestScore, TacticalPointId bestId)
    {
        if (!bestId.IsValid)
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

        return id.CompareTo(bestId) < 0;
    }

    [FrozenRuntimePath]
    private static int ComputeScore(
        in CoverCandidateSample sample,
        in CoverEvaluationContext context,
        CoverQueryKind kind)
    {
        TacticalPointRecord point = sample.Point;
        int distanceToThreat = Int2.ManhattanDistance(point.Position, context.ThreatPosition);
        int distanceToAgent = Int2.ManhattanDistance(point.Position, context.AgentPosition);
        int flankBonus = ComputeFlankBonus(point.Position, context.ThreatPosition, context.ThreatFacing);

        int score = checked(point.Quality * 10);
        score = checked(score - (sample.PathCost * 2));
        score = checked(score - (distanceToAgent * 1));
        score = checked(score - (point.Exposure * 3));
        score = checked(score - (sample.OccupantCount * 25));
        score = checked(score + (sample.AllySeparation * 2));
        score = checked(score + flankBonus);

        if (sample.IsReservedByOther)
        {
            score = checked(score - 200);
        }

        if (sample.ReservationHeld)
        {
            score = checked(score + 15);
        }

        if (sample.InGrenadeDanger)
        {
            score = checked(score - 500);
        }

        if (sample.OrderCompatible)
        {
            score = checked(score + 20);
        }

        score = checked(score + ApplyQueryBias(kind, distanceToThreat, sample.HasLineOfFire, point.Quality));
        return score;
    }

    [FrozenRuntimePath]
    private static int ApplyQueryBias(CoverQueryKind kind, int distanceToThreat, bool hasLineOfFire, int quality)
    {
        switch (kind)
        {
            case CoverQueryKind.BestDefensiveCover:
                return checked((distanceToThreat * 2) + (hasLineOfFire ? 5 : 0));
            case CoverQueryKind.BestAdvancingCover:
                return checked((-distanceToThreat * 3) + (hasLineOfFire ? 30 : -10) + quality);
            case CoverQueryKind.BestRetreatCover:
                return checked(distanceToThreat * 4);
            case CoverQueryKind.BestSuppressionPoint:
                return checked((hasLineOfFire ? 80 : -100) + (-distanceToThreat));
            case CoverQueryKind.BestSearchObservationPoint:
                return checked((hasLineOfFire ? 40 : 10) + (distanceToThreat / 2));
            default:
                return 0;
        }
    }

    [FrozenRuntimePath]
    private static int ComputeFlankBonus(Int2 point, Int2 threat, Direction8 threatFacing)
    {
        Int2 delta = Int2.Subtract(point, threat);
        Int2 facing = DirectionToDelta(threatFacing);
        int dot = checked((delta.X * facing.X) + (delta.Y * facing.Y));

        // Prefer side approaches (near-orthogonal) over frontal approaches.
        if (dot > 0)
        {
            return -20;
        }

        if (dot < 0)
        {
            return 25;
        }

        return 40;
    }

    [FrozenRuntimePath]
    private static Int2 DirectionToDelta(Direction8 direction)
    {
        switch (direction)
        {
            case Direction8.North:
                return new Int2(0, -1);
            case Direction8.NorthEast:
                return new Int2(1, -1);
            case Direction8.East:
                return new Int2(1, 0);
            case Direction8.SouthEast:
                return new Int2(1, 1);
            case Direction8.South:
                return new Int2(0, 1);
            case Direction8.SouthWest:
                return new Int2(-1, 1);
            case Direction8.West:
                return new Int2(-1, 0);
            case Direction8.NorthWest:
                return new Int2(-1, -1);
            default:
                return Int2.Zero;
        }
    }
}
