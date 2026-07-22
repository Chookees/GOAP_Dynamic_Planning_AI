using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Reservations;

/// <summary>
/// Fixed-capacity reservation table with deterministic conflict resolution.
/// </summary>
/// <remarks>
/// Conflict resolution prefers the lowest free slot index. Existing owners win
/// against later claimants until release, transfer, or expiry. No allocations
/// occur after construction.
/// </remarks>
public sealed class ReservationTable
{
    private readonly ReservationRecord[] _slots;
    private readonly int _capacity;
    private int _activeCount;
    private ReservationReasonCode _lastReason;

    /// <summary>
    /// Initializes a reservation table sized to the hard limit.
    /// </summary>
    public ReservationTable()
        : this(AiHardLimits.MaximumReservations)
    {
    }

    /// <summary>
    /// Initializes a reservation table with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Slot capacity in 1..<see cref="AiHardLimits.MaximumReservations"/>.</param>
    public ReservationTable(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumReservations)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be in 1..MaximumReservations.");
        }

        _capacity = capacity;
        _slots = new ReservationRecord[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _slots[i].Clear();
        }

        _activeCount = 0;
        _lastReason = ReservationReasonCode.None;
    }

    /// <summary>
    /// Gets the fixed slot capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of active reservations.
    /// </summary>
    public int ActiveCount => _activeCount;

    /// <summary>
    /// Gets the most recent diagnostic reason code.
    /// </summary>
    public ReservationReasonCode LastReason => _lastReason;

    /// <summary>
    /// Attempts to acquire a reservation for a resource.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="owner">Requesting agent.</param>
    /// <param name="expiresAtSequence">Inclusive expiry tick sequence.</param>
    /// <returns>Reservation result with ownership metadata.</returns>
    [FrozenRuntimePath]
    public ReservationResult TryReserve(
        ReservationResourceType resourceType,
        int resourceId,
        AgentId owner,
        long expiresAtSequence)
    {
        if (!owner.IsValid || resourceId < 0 || expiresAtSequence < 0L)
        {
            return Fail(ReservationStatus.InvalidRequest, ReservationReasonCode.InvalidRequest, AgentId.Invalid, resourceId);
        }

        int existing = FindActiveIndex(resourceType, resourceId);
        if (existing >= 0)
        {
            ref ReservationRecord held = ref _slots[existing];
            if (held.Owner == owner)
            {
                held.ExpiresAtSequence = expiresAtSequence;
                held.LastReason = ReservationReasonCode.AlreadyOwned;
                _lastReason = ReservationReasonCode.AlreadyOwned;
                return new ReservationResult(ReservationStatus.AlreadyOwned, existing, owner, resourceId);
            }

            return Fail(ReservationStatus.Conflict, ReservationReasonCode.Conflict, held.Owner, resourceId);
        }

        int free = FindFreeSlot();
        if (free < 0)
        {
            return Fail(ReservationStatus.CapacityExceeded, ReservationReasonCode.CapacityExceeded, AgentId.Invalid, resourceId);
        }

        ref ReservationRecord slot = ref _slots[free];
        slot.IsActive = true;
        slot.ResourceType = resourceType;
        slot.ResourceId = resourceId;
        slot.Owner = owner;
        slot.ExpiresAtSequence = expiresAtSequence;
        slot.LastReason = ReservationReasonCode.Acquired;
        _activeCount = checked(_activeCount + 1);
        _lastReason = ReservationReasonCode.Acquired;
        return ReservationResult.Acquired(free, owner, resourceId);
    }

    /// <summary>
    /// Extends the lifetime of an owned reservation.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="owner">Owning agent.</param>
    /// <param name="expiresAtSequence">New inclusive expiry tick sequence.</param>
    /// <returns>Reservation result.</returns>
    [FrozenRuntimePath]
    public ReservationResult TryRenew(
        ReservationResourceType resourceType,
        int resourceId,
        AgentId owner,
        long expiresAtSequence)
    {
        if (!owner.IsValid || resourceId < 0 || expiresAtSequence < 0L)
        {
            return Fail(ReservationStatus.InvalidRequest, ReservationReasonCode.InvalidRequest, AgentId.Invalid, resourceId);
        }

        int index = FindActiveIndex(resourceType, resourceId);
        if (index < 0)
        {
            return Fail(ReservationStatus.NotFound, ReservationReasonCode.NotFound, AgentId.Invalid, resourceId);
        }

        ref ReservationRecord slot = ref _slots[index];
        if (slot.Owner != owner)
        {
            return Fail(ReservationStatus.NotHeld, ReservationReasonCode.NotHeld, slot.Owner, resourceId);
        }

        slot.ExpiresAtSequence = expiresAtSequence;
        slot.LastReason = ReservationReasonCode.Renewed;
        _lastReason = ReservationReasonCode.Renewed;
        return new ReservationResult(ReservationStatus.AlreadyOwned, index, owner, resourceId);
    }

    /// <summary>
    /// Releases an owned reservation.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="owner">Owning agent.</param>
    /// <returns>Reservation result.</returns>
    [FrozenRuntimePath]
    public ReservationResult TryRelease(
        ReservationResourceType resourceType,
        int resourceId,
        AgentId owner)
    {
        if (!owner.IsValid || resourceId < 0)
        {
            return Fail(ReservationStatus.InvalidRequest, ReservationReasonCode.InvalidRequest, AgentId.Invalid, resourceId);
        }

        int index = FindActiveIndex(resourceType, resourceId);
        if (index < 0)
        {
            return Fail(ReservationStatus.NotHeld, ReservationReasonCode.NotHeld, AgentId.Invalid, resourceId);
        }

        ref ReservationRecord slot = ref _slots[index];
        if (slot.Owner != owner)
        {
            return Fail(ReservationStatus.NotHeld, ReservationReasonCode.NotHeld, slot.Owner, resourceId);
        }

        Deactivate(index, ReservationReasonCode.Released);
        return new ReservationResult(ReservationStatus.Released, index, owner, resourceId);
    }

    /// <summary>
    /// Transfers ownership of an active reservation to another agent.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="fromOwner">Current owner.</param>
    /// <param name="toOwner">New owner.</param>
    /// <param name="expiresAtSequence">New inclusive expiry tick sequence.</param>
    /// <returns>Reservation result for the new owner.</returns>
    [FrozenRuntimePath]
    public ReservationResult TryTransfer(
        ReservationResourceType resourceType,
        int resourceId,
        AgentId fromOwner,
        AgentId toOwner,
        long expiresAtSequence)
    {
        if (!fromOwner.IsValid || !toOwner.IsValid || resourceId < 0 || expiresAtSequence < 0L)
        {
            return Fail(ReservationStatus.InvalidRequest, ReservationReasonCode.InvalidRequest, AgentId.Invalid, resourceId);
        }

        int index = FindActiveIndex(resourceType, resourceId);
        if (index < 0)
        {
            return Fail(ReservationStatus.NotFound, ReservationReasonCode.NotFound, AgentId.Invalid, resourceId);
        }

        ref ReservationRecord slot = ref _slots[index];
        if (slot.Owner != fromOwner)
        {
            return Fail(ReservationStatus.NotHeld, ReservationReasonCode.NotHeld, slot.Owner, resourceId);
        }

        slot.Owner = toOwner;
        slot.ExpiresAtSequence = expiresAtSequence;
        slot.LastReason = ReservationReasonCode.Transferred;
        _lastReason = ReservationReasonCode.Transferred;
        return ReservationResult.Acquired(index, toOwner, resourceId);
    }

    /// <summary>
    /// Returns whether a resource is free or already owned by <paramref name="agent"/>.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="agent">Agent that would claim the resource.</param>
    /// <returns><see langword="true"/> when available to the agent.</returns>
    [FrozenRuntimePath]
    public bool IsAvailable(ReservationResourceType resourceType, int resourceId, AgentId agent)
    {
        if (resourceId < 0 || !agent.IsValid)
        {
            return false;
        }

        int index = FindActiveIndex(resourceType, resourceId);
        if (index < 0)
        {
            return true;
        }

        return _slots[index].Owner == agent;
    }

    /// <summary>
    /// Attempts to read the owner of an active reservation.
    /// </summary>
    /// <param name="resourceType">Resource kind.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <param name="owner">Receives the owner when found.</param>
    /// <returns>Success or not-found.</returns>
    [FrozenRuntimePath]
    public OperationStatus TryGetOwner(
        ReservationResourceType resourceType,
        int resourceId,
        out AgentId owner)
    {
        owner = AgentId.Invalid;
        if (resourceId < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        int index = FindActiveIndex(resourceType, resourceId);
        if (index < 0)
        {
            return OperationStatus.NotFound;
        }

        owner = _slots[index].Owner;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Expires all reservations whose lifetime is at or before <paramref name="sequence"/>.
    /// </summary>
    /// <param name="sequence">Current tick sequence.</param>
    /// <returns>Number of reservations expired.</returns>
    [FrozenRuntimePath]
    public int ExpireReservations(long sequence)
    {
        int expired = 0;
        for (int i = 0; i < _capacity; i++)
        {
            ref ReservationRecord slot = ref _slots[i];
            if (!slot.IsActive)
            {
                continue;
            }

            if (slot.ExpiresAtSequence > sequence)
            {
                continue;
            }

            Deactivate(i, ReservationReasonCode.Expired);
            expired = checked(expired + 1);
        }

        if (expired > 0)
        {
            _lastReason = ReservationReasonCode.Expired;
        }

        return expired;
    }

    /// <summary>
    /// Releases every reservation owned by <paramref name="owner"/> after cancel.
    /// </summary>
    /// <param name="owner">Agent being cancelled.</param>
    /// <returns>Number of reservations released.</returns>
    [FrozenRuntimePath]
    public int ReleaseAllOnCancel(AgentId owner)
    {
        return ReleaseAllForOwner(owner, ReservationReasonCode.ReleasedOnCancel);
    }

    /// <summary>
    /// Releases every reservation owned by <paramref name="owner"/> after death.
    /// </summary>
    /// <param name="owner">Dead agent.</param>
    /// <returns>Number of reservations released.</returns>
    [FrozenRuntimePath]
    public int ReleaseAllOnDeath(AgentId owner)
    {
        return ReleaseAllForOwner(owner, ReservationReasonCode.ReleasedOnDeath);
    }

    /// <summary>
    /// Attempts to copy an active reservation record by slot index.
    /// </summary>
    /// <param name="slotIndex">Zero-based slot index.</param>
    /// <param name="record">Receives the record when active.</param>
    /// <returns>Success or not-found.</returns>
    [FrozenRuntimePath]
    public OperationStatus TryGetRecord(int slotIndex, out ReservationRecord record)
    {
        record = default;
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        if (!_slots[slotIndex].IsActive)
        {
            return OperationStatus.NotFound;
        }

        record = _slots[slotIndex];
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private int ReleaseAllForOwner(AgentId owner, ReservationReasonCode reason)
    {
        if (!owner.IsValid)
        {
            _lastReason = ReservationReasonCode.InvalidRequest;
            return 0;
        }

        int released = 0;
        for (int i = 0; i < _capacity; i++)
        {
            ref ReservationRecord slot = ref _slots[i];
            if (!slot.IsActive || slot.Owner != owner)
            {
                continue;
            }

            Deactivate(i, reason);
            released = checked(released + 1);
        }

        if (released > 0)
        {
            _lastReason = reason;
        }

        return released;
    }

    [FrozenRuntimePath]
    private int FindActiveIndex(ReservationResourceType resourceType, int resourceId)
    {
        for (int i = 0; i < _capacity; i++)
        {
            ref ReservationRecord slot = ref _slots[i];
            if (slot.IsActive && slot.ResourceType == resourceType && slot.ResourceId == resourceId)
            {
                return i;
            }
        }

        return -1;
    }

    [FrozenRuntimePath]
    private int FindFreeSlot()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (!_slots[i].IsActive)
            {
                return i;
            }
        }

        return -1;
    }

    [FrozenRuntimePath]
    private void Deactivate(int index, ReservationReasonCode reason)
    {
        ref ReservationRecord slot = ref _slots[index];
        slot.Clear();
        slot.LastReason = reason;
        _activeCount = checked(_activeCount - 1);
        _lastReason = reason;
    }

    [FrozenRuntimePath]
    private ReservationResult Fail(
        ReservationStatus status,
        ReservationReasonCode reason,
        AgentId owner,
        int resourceId)
    {
        _lastReason = reason;
        return ReservationResult.Failure(status, owner, resourceId);
    }
}
