namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Role assigned to a squad member slot for the active behavior.
/// </summary>
public enum SquadSlotRole : byte
{
    /// <summary>No role assigned.</summary>
    None = 0,

    /// <summary>Primary mover toward cover or advance point.</summary>
    Mover = 1,

    /// <summary>Provides suppression while others move.</summary>
    Suppressor = 2,

    /// <summary>Searches an assigned sector.</summary>
    Searcher = 3,

    /// <summary>Holds a formation slot.</summary>
    Formation = 4,

    /// <summary>Observes an area.</summary>
    Observer = 5,

    /// <summary>Regroups to a rally point.</summary>
    Rally = 6,

    /// <summary>Holds current position.</summary>
    Holder = 7,
}
