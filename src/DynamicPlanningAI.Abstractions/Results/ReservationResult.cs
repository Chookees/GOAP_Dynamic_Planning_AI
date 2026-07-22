using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Abstractions.Results;

/// <summary>
/// Outcome of a reservation table acquire or release operation.
/// </summary>
public enum ReservationStatus : byte
{
    /// <summary>
    /// The reservation was acquired successfully.
    /// </summary>
    Acquired = 0,

    /// <summary>
    /// The requesting agent already owns the reservation.
    /// </summary>
    AlreadyOwned = 1,

    /// <summary>
    /// Another agent holds a conflicting reservation.
    /// </summary>
    Conflict = 2,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// The reservation table reached capacity.
    /// </summary>
    CapacityExceeded = 4,

    /// <summary>
    /// The reservation request arguments were invalid.
    /// </summary>
    InvalidRequest = 5,

    /// <summary>
    /// The reservation was released successfully.
    /// </summary>
    Released = 6,

    /// <summary>
    /// No reservation existed to release.
    /// </summary>
    NotHeld = 7,
}

/// <summary>
/// Explicit reservation operation result carrying status and ownership metadata.
/// </summary>
/// <remarks>
/// Callers must inspect <see cref="Status"/> before treating a resource as owned.
/// <see cref="ReservationStatus.AlreadyOwned"/> is treated as a successful ownership
/// outcome for acquire retries.
/// </remarks>
public readonly struct ReservationResult
{
    /// <summary>
    /// Initializes a new reservation result.
    /// </summary>
    /// <param name="status">Reservation outcome status.</param>
    /// <param name="slotIndex">Zero-based reservation table slot; negative when unused.</param>
    /// <param name="owner">Owning agent when known; otherwise <see cref="AgentId.Invalid"/>.</param>
    /// <param name="resourceId">Raw resource identifier associated with the reservation.</param>
    public ReservationResult(ReservationStatus status, int slotIndex, AgentId owner, int resourceId)
    {
        Status = status;
        SlotIndex = slotIndex;
        Owner = owner;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Gets the reservation outcome status.
    /// </summary>
    public ReservationStatus Status { get; }

    /// <summary>
    /// Gets the zero-based reservation table slot; negative when unused.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Gets the owning agent when known.
    /// </summary>
    public AgentId Owner { get; }

    /// <summary>
    /// Gets the raw resource identifier associated with the reservation.
    /// </summary>
    public int ResourceId { get; }

    /// <summary>
    /// Gets a value indicating whether ownership is held after the operation.
    /// </summary>
    public bool IsOwned =>
        Status == ReservationStatus.Acquired || Status == ReservationStatus.AlreadyOwned;

    /// <summary>
    /// Creates an acquired reservation result.
    /// </summary>
    /// <param name="slotIndex">Reservation table slot.</param>
    /// <param name="owner">Owning agent.</param>
    /// <param name="resourceId">Reserved resource identifier.</param>
    /// <returns>An acquired result.</returns>
    public static ReservationResult Acquired(int slotIndex, AgentId owner, int resourceId)
    {
        return new ReservationResult(ReservationStatus.Acquired, slotIndex, owner, resourceId);
    }

    /// <summary>
    /// Creates a failed reservation result.
    /// </summary>
    /// <param name="status">Failure or non-owned status.</param>
    /// <param name="owner">Conflicting or previous owner when known.</param>
    /// <param name="resourceId">Requested resource identifier.</param>
    /// <returns>A failure result with unused slot index.</returns>
    public static ReservationResult Failure(ReservationStatus status, AgentId owner, int resourceId)
    {
        return new ReservationResult(status, -1, owner, resourceId);
    }
}
