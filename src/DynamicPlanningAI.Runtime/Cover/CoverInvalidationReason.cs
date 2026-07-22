namespace DynamicPlanningAI.Runtime.Cover;

/// <summary>
/// Reasons a tactical cover point may be invalidated for an agent.
/// </summary>
public enum CoverInvalidationReason : byte
{
    /// <summary>
    /// Cover remains valid.
    /// </summary>
    None = 0,

    /// <summary>
    /// Threat exposure exceeds the configured threshold.
    /// </summary>
    ThreatExposure = 1,

    /// <summary>
    /// Point lies inside an active grenade danger region.
    /// </summary>
    GrenadeRegion = 2,

    /// <summary>
    /// Linked navigation node is unavailable.
    /// </summary>
    NavigationUnavailable = 3,

    /// <summary>
    /// Point was destroyed or permanently disabled.
    /// </summary>
    Destroyed = 4,

    /// <summary>
    /// Occupancy exceeds maximum capacity.
    /// </summary>
    OverCapacity = 5,

    /// <summary>
    /// Required reservation is no longer held.
    /// </summary>
    ReservationLost = 6,

    /// <summary>
    /// Agent stance is unsupported by the point.
    /// </summary>
    StanceUnsupported = 7,
}
