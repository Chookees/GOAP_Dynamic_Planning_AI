using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable action catalog capacity configuration.
/// </summary>
public sealed class ActionConfiguration
{
    /// <summary>
    /// Creates a validated action configuration.
    /// </summary>
    public static ActionConfiguration Create(int maxDefinitions, int maxCandidates, int maxActionTicks)
    {
        return new ActionConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(ActionConfiguration), nameof(MaxDefinitions), maxDefinitions, AiHardLimits.MaximumActionDefinitions, 1501),
            ConfigGuard.RequirePositiveAtMost(nameof(ActionConfiguration), nameof(MaxCandidates), maxCandidates, AiHardLimits.MaximumActionCandidates, 1502),
            ConfigGuard.RequirePositiveAtMost(nameof(ActionConfiguration), nameof(MaxActionTicks), maxActionTicks, AiHardLimits.MaximumActionTicks, 1503));
    }

    private ActionConfiguration(int maxDefinitions, int maxCandidates, int maxActionTicks)
    {
        MaxDefinitions = maxDefinitions;
        MaxCandidates = maxCandidates;
        MaxActionTicks = maxActionTicks;
    }

    /// <summary>Gets the maximum action definitions.</summary>
    public int MaxDefinitions { get; }

    /// <summary>Gets the maximum action candidates per planning request.</summary>
    public int MaxCandidates { get; }

    /// <summary>Gets the maximum ticks an action may remain running.</summary>
    public int MaxActionTicks { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static ActionConfiguration CreateDefault() =>
        Create(
            AiHardLimits.MaximumActionDefinitions,
            AiHardLimits.MaximumActionCandidates,
            AiHardLimits.MaximumActionTicks);
}
