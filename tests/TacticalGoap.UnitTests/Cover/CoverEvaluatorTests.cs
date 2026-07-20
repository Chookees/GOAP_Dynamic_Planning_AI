using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Cover;
using Xunit;

namespace TacticalGoap.UnitTests.Cover;

public sealed class CoverEvaluatorTests
{
    [Fact]
    public void BestDefensiveCover_SelectsHigherQualityPoint()
    {
        CoverEvaluationContext context = CreateContext();
        CoverCandidateSample[] candidates =
        [
            CreateSample(id: 2, quality: 10, exposure: 5, pathCost: 4),
            CreateSample(id: 5, quality: 40, exposure: 2, pathCost: 6),
        ];

        CoverQueryResult result = CoverEvaluator.BestDefensiveCover(candidates, context);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TacticalPointId.FromInt32(5), result.PointId);
        Assert.Equal(2, result.CandidatesScored);
    }

    [Fact]
    public void BestDefensiveCover_TieBreaksByLowerTacticalPointId()
    {
        CoverEvaluationContext context = CreateContext();

        // Identical scoring inputs; only ids differ.
        CoverCandidateSample[] candidates =
        [
            CreateSample(id: 9, quality: 20, exposure: 1, pathCost: 3, position: new Int2(4, 0)),
            CreateSample(id: 3, quality: 20, exposure: 1, pathCost: 3, position: new Int2(4, 0)),
            CreateSample(id: 7, quality: 20, exposure: 1, pathCost: 3, position: new Int2(4, 0)),
        ];

        CoverQueryResult first = CoverEvaluator.BestDefensiveCover(candidates, context);
        CoverQueryResult second = CoverEvaluator.BestDefensiveCover(candidates, context);

        Assert.Equal(OperationStatus.Success, first.Status);
        Assert.Equal(TacticalPointId.FromInt32(3), first.PointId);
        Assert.Equal(first.PointId, second.PointId);
        Assert.Equal(first.Score, second.Score);
    }

    [Fact]
    public void BestAdvancingCover_PrefersCloserThreatWithLineOfFire()
    {
        CoverEvaluationContext context = CreateContext(agent: new Int2(0, 0), threat: new Int2(10, 0));
        CoverCandidateSample[] candidates =
        [
            CreateSample(id: 1, quality: 10, exposure: 1, pathCost: 2, position: new Int2(2, 0), hasLof: false),
            CreateSample(id: 2, quality: 10, exposure: 1, pathCost: 3, position: new Int2(8, 0), hasLof: true),
        ];

        CoverQueryResult result = CoverEvaluator.BestAdvancingCover(candidates, context);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TacticalPointId.FromInt32(2), result.PointId);
    }

    [Fact]
    public void SkipsInvalidatedCandidates_ReturnsNotFoundWhenNoneRemain()
    {
        CoverEvaluationContext context = CreateContext();
        CoverCandidateSample[] candidates =
        [
            CreateSample(id: 1, quality: 50, exposure: 0, pathCost: 1, invalidation: CoverInvalidationReason.GrenadeRegion),
        ];

        CoverQueryResult result = CoverEvaluator.BestDefensiveCover(candidates, context);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.False(result.PointId.IsValid);
    }

    [Fact]
    public void RejectsCandidateSpanAboveHardLimit()
    {
        CoverEvaluationContext context = CreateContext();
        CoverCandidateSample[] candidates = new CoverCandidateSample[33];
        for (int i = 0; i < candidates.Length; i++)
        {
            candidates[i] = CreateSample(id: i, quality: 1, exposure: 0, pathCost: 1);
        }

        CoverQueryResult result = CoverEvaluator.BestDefensiveCover(candidates, context);

        Assert.Equal(OperationStatus.CapacityExceeded, result.Status);
    }

    private static CoverEvaluationContext CreateContext(
        Int2? agent = null,
        Int2? threat = null)
    {
        return new CoverEvaluationContext(
            agent ?? new Int2(0, 0),
            threat ?? new Int2(10, 0),
            Direction8.East,
            AgentStance.Crouching,
            SquadOrderType.MoveToCover);
    }

    private static CoverCandidateSample CreateSample(
        int id,
        int quality,
        int exposure,
        int pathCost,
        Int2? position = null,
        bool hasLof = true,
        CoverInvalidationReason invalidation = CoverInvalidationReason.None)
    {
        TacticalPointRecord point = new(
            TacticalPointId.FromInt32(id),
            NavigationNodeId.FromInt32(id),
            position ?? new Int2(id, 0),
            Direction8.West,
            TacticalPointCategory.Cover,
            CoverHeight.High,
            AgentStance.Crouching,
            exposure,
            quality,
            NavigationAreaId.FromInt32(0),
            maxOccupants: 1);

        return new CoverCandidateSample(
            point,
            pathCost,
            occupantCount: 0,
            isReservedByOther: false,
            reservationHeld: false,
            hasLineOfFire: hasLof,
            inGrenadeDanger: invalidation == CoverInvalidationReason.GrenadeRegion,
            isDestroyed: false,
            isNavigationAvailable: true,
            allySeparation: 3,
            orderCompatible: true,
            invalidationReason: invalidation);
    }
}
