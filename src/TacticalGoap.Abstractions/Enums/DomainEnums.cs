namespace TacticalGoap.Abstractions.Enums;

/// <summary>
/// Runtime action executor lifecycle statuses.
/// </summary>
public enum ActionStatus : byte
{
    /// <summary>The action has not started.</summary>
    NotStarted = 0,

    /// <summary>The action is performing start validation and resource acquisition.</summary>
    Starting = 1,

    /// <summary>The action is actively executing.</summary>
    Running = 2,

    /// <summary>The action completed successfully.</summary>
    Succeeded = 3,

    /// <summary>The action failed.</summary>
    Failed = 4,

    /// <summary>The action was cancelled.</summary>
    Cancelled = 5,

    /// <summary>The action exceeded its maximum tick budget.</summary>
    TimedOut = 6,
}

/// <summary>
/// Explicit runtime action failure reasons.
/// </summary>
public enum ActionFailureReason : byte
{
    /// <summary>No failure.</summary>
    None = 0,

    /// <summary>A volatile precondition became false.</summary>
    InvalidPrecondition = 1,

    /// <summary>The focused target was lost.</summary>
    TargetLost = 2,

    /// <summary>The focused target is dead.</summary>
    TargetDead = 3,

    /// <summary>No ammunition remains.</summary>
    NoAmmunition = 4,

    /// <summary>No navigation path exists.</summary>
    NoPath = 5,

    /// <summary>The active path was invalidated.</summary>
    PathInvalidated = 6,

    /// <summary>The destination is occupied beyond capacity.</summary>
    DestinationOccupied = 7,

    /// <summary>Cover became invalid.</summary>
    CoverInvalidated = 8,

    /// <summary>A required reservation was lost.</summary>
    ReservationLost = 9,

    /// <summary>A door is blocked.</summary>
    DoorBlocked = 10,

    /// <summary>A door was destroyed.</summary>
    DoorDestroyed = 11,

    /// <summary>Traversal is unavailable.</summary>
    TraversalUnavailable = 12,

    /// <summary>The host rejected an animation request.</summary>
    AnimationRejected = 13,

    /// <summary>The host rejected a weapon request.</summary>
    WeaponRejected = 14,

    /// <summary>The host rejected an interaction request.</summary>
    InteractionRejected = 15,

    /// <summary>Danger state changed materially.</summary>
    DangerChanged = 16,

    /// <summary>A squad order expired.</summary>
    OrderExpired = 17,

    /// <summary>Maximum action ticks were reached.</summary>
    MaximumTickCountReached = 18,

    /// <summary>A host service reported failure.</summary>
    HostServiceFailure = 19,
}

/// <summary>
/// Working-memory record type identifiers.
/// </summary>
public enum MemoryType : byte
{
    /// <summary>Visual confirmation of a target.</summary>
    TargetSeen = 0,

    /// <summary>Auditory evidence of a target.</summary>
    TargetHeard = 1,

    /// <summary>Damage received by the agent.</summary>
    DamageReceived = 2,

    /// <summary>Generic danger detection.</summary>
    DangerDetected = 3,

    /// <summary>Grenade danger detection.</summary>
    GrenadeDetected = 4,

    /// <summary>Cover point invalidated.</summary>
    CoverInvalid = 5,

    /// <summary>Cover successfully reserved.</summary>
    CoverReserved = 6,

    /// <summary>Cover reservation lost.</summary>
    CoverReservationLost = 7,

    /// <summary>Door open attempt failed because the door is blocked.</summary>
    DoorBlocked = 8,

    /// <summary>Door was breached.</summary>
    DoorBreached = 9,

    /// <summary>Traversal attempt failed.</summary>
    TraversalFailed = 10,

    /// <summary>Navigation query or follow failed.</summary>
    NavigationFailed = 11,

    /// <summary>Search sector inspected.</summary>
    SearchSectorInspected = 12,

    /// <summary>Squad order received.</summary>
    SquadOrderReceived = 13,

    /// <summary>Squad order completed.</summary>
    SquadOrderCompleted = 14,

    /// <summary>Squad order failed.</summary>
    SquadOrderFailed = 15,

    /// <summary>Previously known target lost.</summary>
    TargetLost = 16,

    /// <summary>Communication received from another agent.</summary>
    CommunicationReceived = 17,
}

/// <summary>
/// Tactical point category.
/// </summary>
public enum TacticalPointCategory : byte
{
    /// <summary>Defensive cover.</summary>
    Cover = 0,

    /// <summary>Ambush position.</summary>
    Ambush = 1,

    /// <summary>Search observation.</summary>
    Search = 2,

    /// <summary>Observation post.</summary>
    Observation = 3,

    /// <summary>Suppression firing position.</summary>
    Suppression = 4,

    /// <summary>Formation slot.</summary>
    Formation = 5,

    /// <summary>Door interaction point.</summary>
    DoorInteraction = 6,

    /// <summary>Window traversal point.</summary>
    WindowTraversal = 7,

    /// <summary>Vault traversal point.</summary>
    VaultTraversal = 8,

    /// <summary>Fallback/retreat point.</summary>
    Fallback = 9,
}

/// <summary>
/// Squad order types issued by the coordinator.
/// </summary>
public enum SquadOrderType : byte
{
    /// <summary>Move to an assigned cover point.</summary>
    MoveToCover = 0,

    /// <summary>Provide suppression on an area.</summary>
    ProvideSuppression = 1,

    /// <summary>Advance toward the threat.</summary>
    Advance = 2,

    /// <summary>Hold current position.</summary>
    Hold = 3,

    /// <summary>Search an assigned sector.</summary>
    SearchSector = 4,

    /// <summary>Follow a formation slot.</summary>
    FollowFormation = 5,

    /// <summary>Regroup to a rally point.</summary>
    Regroup = 6,

    /// <summary>Observe an area.</summary>
    Observe = 7,
}

/// <summary>
/// Squad behavior kinds. At most one is active per squad.
/// </summary>
public enum SquadBehaviorType : byte
{
    /// <summary>No active squad behavior.</summary>
    None = 0,

    /// <summary>Get agents into cover, optionally with suppression.</summary>
    GetToCover = 1,

    /// <summary>Advance using cover with suppression support.</summary>
    AdvanceCover = 2,

    /// <summary>Orderly formation advance.</summary>
    OrderlyAdvance = 3,

    /// <summary>Coordinated search.</summary>
    Search = 4,

    /// <summary>Regroup separated agents.</summary>
    Regroup = 5,

    /// <summary>Hold current positions.</summary>
    HoldPosition = 6,
}

/// <summary>
/// Semantic communication intent types.
/// </summary>
public enum CommunicationIntentType : byte
{
    /// <summary>Contact spotted.</summary>
    ContactSpotted = 0,

    /// <summary>Contact lost.</summary>
    ContactLost = 1,

    /// <summary>Taking fire.</summary>
    TakingFire = 2,

    /// <summary>Cover compromised.</summary>
    CoverCompromised = 3,

    /// <summary>Moving to cover.</summary>
    MovingToCover = 4,

    /// <summary>Advancing.</summary>
    Advancing = 5,

    /// <summary>Suppressing.</summary>
    Suppressing = 6,

    /// <summary>Reloading.</summary>
    Reloading = 7,

    /// <summary>Throwing grenade.</summary>
    ThrowingGrenade = 8,

    /// <summary>Grenade warning.</summary>
    GrenadeWarning = 9,

    /// <summary>Searching a sector.</summary>
    SearchingSector = 10,

    /// <summary>Sector clear.</summary>
    SectorClear = 11,

    /// <summary>Door blocked.</summary>
    DoorBlocked = 12,

    /// <summary>Breaching door.</summary>
    BreachingDoor = 13,

    /// <summary>No valid route.</summary>
    NoValidRoute = 14,

    /// <summary>Holding position.</summary>
    HoldingPosition = 15,

    /// <summary>Need assistance.</summary>
    NeedAssistance = 16,

    /// <summary>Order acknowledged.</summary>
    OrderAcknowledged = 17,

    /// <summary>Order failed.</summary>
    OrderFailed = 18,
}

/// <summary>
/// Diagnostic subsystem identifiers.
/// </summary>
public enum DiagnosticSubsystem : byte
{
    /// <summary>Perception subsystem.</summary>
    Perception = 0,

    /// <summary>Working memory.</summary>
    Memory = 1,

    /// <summary>Target selection.</summary>
    TargetSelection = 2,

    /// <summary>Weapon selection.</summary>
    WeaponSelection = 3,

    /// <summary>Goal arbitration.</summary>
    GoalArbitration = 4,

    /// <summary>Action candidate generation.</summary>
    CandidateGeneration = 5,

    /// <summary>GOAP planning.</summary>
    Planning = 6,

    /// <summary>Runtime action execution.</summary>
    Execution = 7,

    /// <summary>Navigation orchestration.</summary>
    Navigation = 8,

    /// <summary>Cover evaluation.</summary>
    Cover = 9,

    /// <summary>Reservation table.</summary>
    Reservation = 10,

    /// <summary>Squad coordination.</summary>
    Squad = 11,

    /// <summary>Communication arbitration.</summary>
    Communication = 12,

    /// <summary>Contract system.</summary>
    Contract = 13,
}
