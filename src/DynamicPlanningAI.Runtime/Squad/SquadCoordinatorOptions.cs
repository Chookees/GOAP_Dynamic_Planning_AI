using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Configuration knobs for <see cref="SquadCoordinator"/> validated against hard limits.
/// </summary>
public sealed class SquadCoordinatorOptions
{
    /// <summary>
    /// Initializes options with defaults.
    /// </summary>
    public SquadCoordinatorOptions()
    {
        MaxAgentsPerSquad = AiHardLimits.MaximumAgentsPerSquad;
        FormationProximity = 12;
        ReclusterIntervalTicks = 30;
        DefaultBehaviorDurationTicks = 60;
        DefaultOrderDurationTicks = 45;
        MaxSquads = AiHardLimits.MaximumSquads;
    }

    /// <summary>Gets or sets the maximum agents per squad (≤ hard limit).</summary>
    public int MaxAgentsPerSquad { get; set; }

    /// <summary>Gets or sets Manhattan proximity for formation clustering.</summary>
    public int FormationProximity { get; set; }

    /// <summary>Gets or sets ticks between reclustering passes.</summary>
    public int ReclusterIntervalTicks { get; set; }

    /// <summary>Gets or sets default behavior lifetime in ticks.</summary>
    public int DefaultBehaviorDurationTicks { get; set; }

    /// <summary>Gets or sets default order lifetime in ticks.</summary>
    public int DefaultOrderDurationTicks { get; set; }

    /// <summary>Gets or sets maximum concurrent squads (≤ hard limit).</summary>
    public int MaxSquads { get; set; }

    /// <summary>
    /// Validates options against <see cref="AiHardLimits"/>.
    /// </summary>
    /// <returns><see langword="true"/> when valid.</returns>
    public bool Validate()
    {
        if (MaxAgentsPerSquad < 1 || MaxAgentsPerSquad > AiHardLimits.MaximumAgentsPerSquad)
        {
            return false;
        }

        if (MaxSquads < 1 || MaxSquads > AiHardLimits.MaximumSquads)
        {
            return false;
        }

        if (FormationProximity < 0)
        {
            return false;
        }

        if (ReclusterIntervalTicks < 1)
        {
            return false;
        }

        if (DefaultBehaviorDurationTicks < 1 || DefaultOrderDurationTicks < 1)
        {
            return false;
        }

        return true;
    }
}
