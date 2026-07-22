namespace DynamicPlanningAI.Runtime.Reservations;

/// <summary>
/// Resource kinds that may be reserved through the fixed-capacity reservation table.
/// </summary>
public enum ReservationResourceType : byte
{
    /// <summary>
    /// A tactical point (cover, ambush, observation, etc.).
    /// </summary>
    TacticalPoint = 0,

    /// <summary>
    /// A squad formation slot.
    /// </summary>
    FormationSlot = 1,

    /// <summary>
    /// A search sector assignment.
    /// </summary>
    SearchSector = 2,

    /// <summary>
    /// A smart-object interaction claim.
    /// </summary>
    SmartObject = 3,

    /// <summary>
    /// A door interaction pose or lock.
    /// </summary>
    DoorInteraction = 4,

    /// <summary>
    /// A suppression firing role within a squad behavior.
    /// </summary>
    SuppressionRole = 5,
}
