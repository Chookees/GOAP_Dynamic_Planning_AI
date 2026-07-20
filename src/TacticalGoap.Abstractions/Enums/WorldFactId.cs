namespace TacticalGoap.Abstractions.Enums;

/// <summary>
/// Symbolic world-fact identifiers occupying fixed bit positions.
/// </summary>
/// <remarks>
/// Continuous quantities are quantized into these categories before planning.
/// Fact ownership is documented in WORLD_STATE_MODEL.md. Do not store object
/// references or floating-point values in symbolic facts.
/// </remarks>
public enum WorldFactId : byte
{
    /// <summary>A focused target entity is selected.</summary>
    TargetSelected = 0,

    /// <summary>The focused target has known evidence in memory.</summary>
    TargetKnown = 1,

    /// <summary>The focused target is currently visible.</summary>
    TargetVisible = 2,

    /// <summary>The focused target is believed alive.</summary>
    TargetAlive = 3,

    /// <summary>The agent occupies a tactical point.</summary>
    AtTacticalPoint = 4,

    /// <summary>The agent is in a cover posture at a valid point.</summary>
    InCover = 5,

    /// <summary>The current cover point remains valid.</summary>
    CoverValid = 6,

    /// <summary>A weapon is selected for the current focus.</summary>
    WeaponSelected = 7,

    /// <summary>The selected weapon is loaded.</summary>
    WeaponLoaded = 8,

    /// <summary>Ammunition remains for the selected weapon.</summary>
    HasAmmunition = 9,

    /// <summary>A grenade is available.</summary>
    HasGrenade = 10,

    /// <summary>The agent is under direct fire.</summary>
    UnderDirectFire = 11,

    /// <summary>A grenade danger region affects the agent.</summary>
    GrenadeDangerPresent = 12,

    /// <summary>A movement destination has been set.</summary>
    MovementDestinationSet = 13,

    /// <summary>The agent has reached its movement destination.</summary>
    AtMovementDestination = 14,

    /// <summary>A relevant door is known.</summary>
    DoorKnown = 15,

    /// <summary>The relevant door is open.</summary>
    DoorOpen = 16,

    /// <summary>The relevant door is blocked.</summary>
    DoorBlocked = 17,

    /// <summary>A traversal link is available.</summary>
    TraversalAvailable = 18,

    /// <summary>A squad order is available for the agent.</summary>
    SquadOrderAvailable = 19,

    /// <summary>The active squad order is satisfied.</summary>
    SquadOrderSatisfied = 20,

    /// <summary>A search location is available.</summary>
    SearchLocationAvailable = 21,

    /// <summary>The current search location has been inspected.</summary>
    SearchLocationInspected = 22,

    /// <summary>Immediate danger has been resolved.</summary>
    ImmediateDangerResolved = 23,

    /// <summary>The agent is at a window traversal point.</summary>
    AtWindowTraversal = 24,

    /// <summary>Window traversal has completed.</summary>
    WindowTraversed = 25,

    /// <summary>The agent is suppressing an area.</summary>
    Suppressing = 26,

    /// <summary>Blind fire is appropriate from current cover.</summary>
    BlindFireAppropriate = 27,

    /// <summary>Melee range against the focused target is available.</summary>
    MeleeRangeAvailable = 28,

    /// <summary>Immediate lethal danger is present.</summary>
    ImmediateDangerPresent = 29,

    /// <summary>A navigation path is currently valid.</summary>
    PathValid = 30,

    /// <summary>The agent is ready for routine patrol or idle behavior.</summary>
    ReadinessMaintained = 31,
}
