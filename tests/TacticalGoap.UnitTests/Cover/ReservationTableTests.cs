using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Reservations;
using Xunit;

namespace TacticalGoap.UnitTests.Cover;

public sealed class ReservationTableTests
{
    [Fact]
    public void TryReserve_ConflictWhenOwnedByOtherAgent()
    {
        ReservationTable table = new(capacity: 8);
        AgentId first = AgentId.FromInt32(0);
        AgentId second = AgentId.FromInt32(1);

        ReservationResult acquired = table.TryReserve(
            ReservationResourceType.TacticalPoint,
            resourceId: 10,
            first,
            expiresAtSequence: 100);

        ReservationResult conflict = table.TryReserve(
            ReservationResourceType.TacticalPoint,
            resourceId: 10,
            second,
            expiresAtSequence: 100);

        Assert.True(acquired.IsOwned);
        Assert.Equal(ReservationStatus.Conflict, conflict.Status);
        Assert.Equal(ReservationReasonCode.Conflict, table.LastReason);
        Assert.Equal(first, conflict.Owner);
    }

    [Fact]
    public void TryRenew_ExtendsOwnedReservation()
    {
        ReservationTable table = new(capacity: 4);
        AgentId owner = AgentId.FromInt32(2);

        Assert.True(
            table.TryReserve(ReservationResourceType.FormationSlot, 1, owner, expiresAtSequence: 10).IsOwned);

        ReservationResult renewed = table.TryRenew(
            ReservationResourceType.FormationSlot,
            1,
            owner,
            expiresAtSequence: 50);

        Assert.Equal(ReservationStatus.AlreadyOwned, renewed.Status);
        Assert.Equal(ReservationReasonCode.Renewed, table.LastReason);
        Assert.Equal(0, table.ExpireReservations(40));
        Assert.Equal(1, table.ActiveCount);
    }

    [Fact]
    public void ExpireReservations_ReleasesExpiredSlotsDeterministically()
    {
        ReservationTable table = new(capacity: 4);
        AgentId owner = AgentId.FromInt32(0);

        table.TryReserve(ReservationResourceType.SearchSector, 1, owner, expiresAtSequence: 5);
        table.TryReserve(ReservationResourceType.SmartObject, 2, owner, expiresAtSequence: 15);

        int expired = table.ExpireReservations(5);

        Assert.Equal(1, expired);
        Assert.Equal(1, table.ActiveCount);
        Assert.True(table.IsAvailable(ReservationResourceType.SearchSector, 1, owner));
        Assert.False(table.IsAvailable(ReservationResourceType.SmartObject, 2, AgentId.FromInt32(1)));
        Assert.Equal(ReservationReasonCode.Expired, table.LastReason);
    }

    [Fact]
    public void TryRelease_AndReleaseOnDeath_ClearOwnership()
    {
        ReservationTable table = new(capacity: 4);
        AgentId owner = AgentId.FromInt32(3);

        table.TryReserve(ReservationResourceType.DoorInteraction, 7, owner, expiresAtSequence: 100);
        table.TryReserve(ReservationResourceType.SuppressionRole, 8, owner, expiresAtSequence: 100);

        ReservationResult released = table.TryRelease(ReservationResourceType.DoorInteraction, 7, owner);
        int deathReleased = table.ReleaseAllOnDeath(owner);

        Assert.Equal(ReservationStatus.Released, released.Status);
        Assert.Equal(1, deathReleased);
        Assert.Equal(0, table.ActiveCount);
        Assert.Equal(ReservationReasonCode.ReleasedOnDeath, table.LastReason);
    }

    [Fact]
    public void TryTransfer_MovesOwnershipDeterministically()
    {
        ReservationTable table = new(capacity: 2);
        AgentId from = AgentId.FromInt32(0);
        AgentId to = AgentId.FromInt32(1);

        table.TryReserve(ReservationResourceType.TacticalPoint, 4, from, expiresAtSequence: 20);
        ReservationResult transferred = table.TryTransfer(
            ReservationResourceType.TacticalPoint,
            4,
            from,
            to,
            expiresAtSequence: 40);

        Assert.True(transferred.IsOwned);
        Assert.Equal(to, transferred.Owner);
        Assert.Equal(ReservationReasonCode.Transferred, table.LastReason);
        Assert.True(table.TryGetOwner(ReservationResourceType.TacticalPoint, 4, out AgentId owner) == OperationStatus.Success);
        Assert.Equal(to, owner);
    }

    [Fact]
    public void CapacityExceeded_WhenTableFull()
    {
        ReservationTable table = new(capacity: 1);
        AgentId a = AgentId.FromInt32(0);
        AgentId b = AgentId.FromInt32(1);

        Assert.True(table.TryReserve(ReservationResourceType.TacticalPoint, 1, a, 10).IsOwned);
        ReservationResult full = table.TryReserve(ReservationResourceType.TacticalPoint, 2, b, 10);

        Assert.Equal(ReservationStatus.CapacityExceeded, full.Status);
        Assert.Equal(ReservationReasonCode.CapacityExceeded, table.LastReason);
    }
}
