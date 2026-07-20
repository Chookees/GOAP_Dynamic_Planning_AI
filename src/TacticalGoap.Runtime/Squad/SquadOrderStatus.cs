namespace TacticalGoap.Runtime.Squad;

/// <summary>
/// Lifecycle status of a squad order.
/// </summary>
public enum SquadOrderStatus : byte
{
    /// <summary>Slot unused.</summary>
    None = 0,

    /// <summary>Order issued; awaiting agent acknowledgement.</summary>
    Issued = 1,

    /// <summary>Agent acknowledged the order (FollowSquadOrderGoal activated).</summary>
    Acknowledged = 2,

    /// <summary>Agent is executing toward the order desire.</summary>
    InProgress = 3,

    /// <summary>Order desire satisfied.</summary>
    Succeeded = 4,

    /// <summary>Order failed (planning, survival, or other).</summary>
    Failed = 5,

    /// <summary>Order exceeded its expiry tick.</summary>
    TimedOut = 6,

    /// <summary>Order cancelled by behavior change or cleanup.</summary>
    Cancelled = 7,
}
