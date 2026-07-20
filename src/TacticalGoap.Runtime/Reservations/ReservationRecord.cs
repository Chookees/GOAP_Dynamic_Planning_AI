using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Reservations;

/// <summary>
/// Fixed-layout reservation entry stored in <see cref="ReservationTable"/>.
/// </summary>
public struct ReservationRecord
{
    /// <summary>
    /// Gets or sets a value indicating whether this slot holds a live reservation.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the reserved resource kind.
    /// </summary>
    public ReservationResourceType ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the raw resource identifier (point, sector, object, etc.).
    /// </summary>
    public int ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the owning agent.
    /// </summary>
    public AgentId Owner { get; set; }

    /// <summary>
    /// Gets or sets the inclusive tick sequence when the reservation expires.
    /// </summary>
    public long ExpiresAtSequence { get; set; }

    /// <summary>
    /// Gets or sets the last diagnostic reason applied to this slot.
    /// </summary>
    public ReservationReasonCode LastReason { get; set; }

    /// <summary>
    /// Clears the record to an inactive default state.
    /// </summary>
    public void Clear()
    {
        IsActive = false;
        ResourceType = ReservationResourceType.TacticalPoint;
        ResourceId = -1;
        Owner = AgentId.Invalid;
        ExpiresAtSequence = 0L;
        LastReason = ReservationReasonCode.None;
    }
}
