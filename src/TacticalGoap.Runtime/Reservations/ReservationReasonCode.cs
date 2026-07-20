namespace TacticalGoap.Runtime.Reservations;

/// <summary>
/// Diagnostic reason codes for reservation table operations.
/// </summary>
public enum ReservationReasonCode : byte
{
    /// <summary>
    /// No diagnostic reason recorded.
    /// </summary>
    None = 0,

    /// <summary>
    /// Reservation acquired successfully.
    /// </summary>
    Acquired = 1,

    /// <summary>
    /// Requesting agent already owns the resource.
    /// </summary>
    AlreadyOwned = 2,

    /// <summary>
    /// Another agent holds a conflicting live reservation.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// No matching reservation was found.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// The reservation table has no free slots.
    /// </summary>
    CapacityExceeded = 5,

    /// <summary>
    /// Request arguments failed validation.
    /// </summary>
    InvalidRequest = 6,

    /// <summary>
    /// Reservation released successfully.
    /// </summary>
    Released = 7,

    /// <summary>
    /// Caller does not hold the reservation.
    /// </summary>
    NotHeld = 8,

    /// <summary>
    /// Reservation lifetime was extended.
    /// </summary>
    Renewed = 9,

    /// <summary>
    /// Ownership transferred to another agent.
    /// </summary>
    Transferred = 10,

    /// <summary>
    /// Reservation expired by tick lifetime.
    /// </summary>
    Expired = 11,

    /// <summary>
    /// Reservation released because the owner cancelled.
    /// </summary>
    ReleasedOnCancel = 12,

    /// <summary>
    /// Reservation released because the owner died.
    /// </summary>
    ReleasedOnDeath = 13,
}
