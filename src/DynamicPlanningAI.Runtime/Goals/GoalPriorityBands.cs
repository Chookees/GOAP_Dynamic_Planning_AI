namespace DynamicPlanningAI.Runtime.Goals;

/// <summary>
/// Recommended integer priority bands for registered goals.
/// </summary>
public static class GoalPriorityBands
{
    /// <summary>Critical survival priorities (grenade, lethal danger).</summary>
    public const int Critical = 900;

    /// <summary>Immediate combat survival (dodge, take cover under fire).</summary>
    public const int CombatSurvival = 750;

    /// <summary>Active combat priorities (eliminate, reload).</summary>
    public const int Combat = 600;

    /// <summary>Squad order priorities.</summary>
    public const int Squad = 500;

    /// <summary>Investigation and search priorities.</summary>
    public const int Tactical = 350;

    /// <summary>Readiness maintenance.</summary>
    public const int Readiness = 100;

    /// <summary>Patrol priority.</summary>
    public const int Patrol = 50;

    /// <summary>Idle fallback priority.</summary>
    public const int Idle = 10;

    /// <summary>Default hysteresis margin preventing priority thrashing.</summary>
    public const int DefaultHysteresisMargin = 40;

    /// <summary>Default cooldown ticks after a goal is deselected.</summary>
    public const int DefaultCooldownTicks = 4;
}
