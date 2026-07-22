using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Runtime.Cover;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Cover;

public sealed class CoverInvalidatorTests
{
    [Fact]
    public void GrenadeRegion_InvalidatesCover()
    {
        CoverCandidateSample sample = CreateSample(inGrenade: true);

        CoverInvalidationReason reason = CoverInvalidator.Evaluate(
            sample,
            AgentStance.Crouching,
            maxExposure: 50,
            reservationLost: false);

        Assert.Equal(CoverInvalidationReason.GrenadeRegion, reason);
    }

    [Fact]
    public void Apply_StampsGrenadeReasonOntoSample()
    {
        CoverCandidateSample sample = CreateSample(inGrenade: true);

        CoverCandidateSample stamped = CoverInvalidator.Apply(
            sample,
            AgentStance.Crouching,
            maxExposure: 50,
            reservationLost: false);

        Assert.Equal(CoverInvalidationReason.GrenadeRegion, stamped.InvalidationReason);
    }

    [Fact]
    public void ReservationLost_InvalidatesCover()
    {
        CoverCandidateSample sample = CreateSample(inGrenade: false);

        CoverInvalidationReason reason = CoverInvalidator.Evaluate(
            sample,
            AgentStance.Crouching,
            maxExposure: 50,
            reservationLost: true);

        Assert.Equal(CoverInvalidationReason.ReservationLost, reason);
    }

    [Fact]
    public void OverCapacity_InvalidatesCover()
    {
        CoverCandidateSample sample = CreateSample(inGrenade: false, occupants: 2, maxOccupants: 1);

        CoverInvalidationReason reason = CoverInvalidator.Evaluate(
            sample,
            AgentStance.Crouching,
            maxExposure: 50,
            reservationLost: false);

        Assert.Equal(CoverInvalidationReason.OverCapacity, reason);
    }

    private static CoverCandidateSample CreateSample(
        bool inGrenade,
        int occupants = 0,
        int maxOccupants = 1)
    {
        TacticalPointRecord point = new(
            TacticalPointId.FromInt32(1),
            NavigationNodeId.FromInt32(1),
            new Int2(3, 3),
            Direction8.North,
            TacticalPointCategory.Cover,
            CoverHeight.High,
            AgentStance.Crouching,
            exposure: 5,
            quality: 20,
            NavigationAreaId.FromInt32(0),
            maxOccupants);

        return new CoverCandidateSample(
            point,
            pathCost: 2,
            occupantCount: occupants,
            isReservedByOther: false,
            reservationHeld: true,
            hasLineOfFire: true,
            inGrenadeDanger: inGrenade,
            isDestroyed: false,
            isNavigationAvailable: true,
            allySeparation: 2,
            orderCompatible: true,
            invalidationReason: CoverInvalidationReason.None);
    }
}
