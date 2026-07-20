using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Stable well-known action definition identifiers used by the standard catalog.
/// </summary>
public static class WellKnownActionIds
{
    /// <summary>Move to a navigation node.</summary>
    public static readonly ActionId MoveToNavigationNode = ActionId.FromInt32(1);

    /// <summary>Move to a tactical point.</summary>
    public static readonly ActionId MoveToTacticalPoint = ActionId.FromInt32(2);

    /// <summary>Move into cover.</summary>
    public static readonly ActionId MoveToCover = ActionId.FromInt32(3);

    /// <summary>Advance toward cover.</summary>
    public static readonly ActionId AdvanceToCover = ActionId.FromInt32(4);

    /// <summary>Retreat to cover.</summary>
    public static readonly ActionId RetreatToCover = ActionId.FromInt32(5);

    /// <summary>Move to a search sector.</summary>
    public static readonly ActionId MoveToSearchSector = ActionId.FromInt32(6);

    /// <summary>Follow a formation slot.</summary>
    public static readonly ActionId FollowFormationSlot = ActionId.FromInt32(7);

    /// <summary>Open a door.</summary>
    public static readonly ActionId OpenDoor = ActionId.FromInt32(8);

    /// <summary>Breach a door.</summary>
    public static readonly ActionId BreachDoor = ActionId.FromInt32(9);

    /// <summary>Vault an obstacle.</summary>
    public static readonly ActionId VaultObstacle = ActionId.FromInt32(10);

    /// <summary>Traverse a window.</summary>
    public static readonly ActionId TraverseWindow = ActionId.FromInt32(11);

    /// <summary>Use a smart object.</summary>
    public static readonly ActionId UseSmartObject = ActionId.FromInt32(12);

    /// <summary>Attack a target.</summary>
    public static readonly ActionId AttackTarget = ActionId.FromInt32(13);

    /// <summary>Attack from cover.</summary>
    public static readonly ActionId AttackFromCover = ActionId.FromInt32(14);

    /// <summary>Blind-fire from cover.</summary>
    public static readonly ActionId BlindFireFromCover = ActionId.FromInt32(15);

    /// <summary>Suppress a target area.</summary>
    public static readonly ActionId SuppressTargetArea = ActionId.FromInt32(16);

    /// <summary>Reload the selected weapon.</summary>
    public static readonly ActionId ReloadWeapon = ActionId.FromInt32(17);

    /// <summary>Switch weapon.</summary>
    public static readonly ActionId SwitchWeapon = ActionId.FromInt32(18);

    /// <summary>Melee a target.</summary>
    public static readonly ActionId MeleeTarget = ActionId.FromInt32(19);

    /// <summary>Throw a grenade.</summary>
    public static readonly ActionId ThrowGrenade = ActionId.FromInt32(20);

    /// <summary>Hold aim.</summary>
    public static readonly ActionId HoldAim = ActionId.FromInt32(21);

    /// <summary>Dodge shuffle.</summary>
    public static readonly ActionId DodgeShuffle = ActionId.FromInt32(22);

    /// <summary>Dodge roll.</summary>
    public static readonly ActionId DodgeRoll = ActionId.FromInt32(23);

    /// <summary>Escape grenade.</summary>
    public static readonly ActionId EscapeGrenade = ActionId.FromInt32(24);

    /// <summary>Flee danger.</summary>
    public static readonly ActionId FleeDanger = ActionId.FromInt32(25);

    /// <summary>Crouch in cover.</summary>
    public static readonly ActionId CrouchInCover = ActionId.FromInt32(26);

    /// <summary>Abandon invalid cover.</summary>
    public static readonly ActionId AbandonInvalidCover = ActionId.FromInt32(27);

    /// <summary>Investigate a sound.</summary>
    public static readonly ActionId InvestigateSound = ActionId.FromInt32(28);

    /// <summary>Investigate damage direction.</summary>
    public static readonly ActionId InvestigateDamageDirection = ActionId.FromInt32(29);

    /// <summary>Move to last known position.</summary>
    public static readonly ActionId MoveToLastKnownPosition = ActionId.FromInt32(30);

    /// <summary>Inspect a search sector.</summary>
    public static readonly ActionId InspectSearchSector = ActionId.FromInt32(31);

    /// <summary>Look around.</summary>
    public static readonly ActionId LookAround = ActionId.FromInt32(32);

    /// <summary>Hold a search position.</summary>
    public static readonly ActionId HoldSearchPosition = ActionId.FromInt32(33);

    /// <summary>Accept a squad order.</summary>
    public static readonly ActionId AcceptSquadOrder = ActionId.FromInt32(34);

    /// <summary>Move to assigned cover.</summary>
    public static readonly ActionId MoveToAssignedCover = ActionId.FromInt32(35);

    /// <summary>Provide suppression.</summary>
    public static readonly ActionId ProvideSuppression = ActionId.FromInt32(36);

    /// <summary>Advance under suppression.</summary>
    public static readonly ActionId AdvanceUnderSuppression = ActionId.FromInt32(37);

    /// <summary>Search an assigned sector.</summary>
    public static readonly ActionId SearchAssignedSector = ActionId.FromInt32(38);

    /// <summary>Hold formation.</summary>
    public static readonly ActionId HoldFormation = ActionId.FromInt32(39);

    /// <summary>Complete a squad order.</summary>
    public static readonly ActionId CompleteSquadOrder = ActionId.FromInt32(40);

    /// <summary>Patrol.</summary>
    public static readonly ActionId Patrol = ActionId.FromInt32(41);

    /// <summary>Wait.</summary>
    public static readonly ActionId Wait = ActionId.FromInt32(42);

    /// <summary>Idle.</summary>
    public static readonly ActionId Idle = ActionId.FromInt32(43);
}
