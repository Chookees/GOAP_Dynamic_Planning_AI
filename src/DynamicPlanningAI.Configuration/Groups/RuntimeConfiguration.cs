using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable runtime capacity configuration validated against hard limits.
/// </summary>
public sealed class RuntimeConfiguration
{
    /// <summary>
    /// Creates a validated runtime configuration.
    /// </summary>
    public static RuntimeConfiguration Create(
        int maxAgents,
        int maxSquads,
        int tickDeltaMilliseconds)
    {
        return new RuntimeConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(RuntimeConfiguration), nameof(MaxAgents), maxAgents, AiHardLimits.MaximumAgents, 1001),
            ConfigGuard.RequirePositiveAtMost(nameof(RuntimeConfiguration), nameof(MaxSquads), maxSquads, AiHardLimits.MaximumSquads, 1002),
            ConfigGuard.RequireTickDelta(nameof(RuntimeConfiguration), tickDeltaMilliseconds, 1003));
    }

    private RuntimeConfiguration(int maxAgents, int maxSquads, int tickDeltaMilliseconds)
    {
        MaxAgents = maxAgents;
        MaxSquads = maxSquads;
        TickDeltaMilliseconds = tickDeltaMilliseconds;
    }

    /// <summary>Gets the maximum registered agents.</summary>
    public int MaxAgents { get; }

    /// <summary>Gets the maximum concurrent squads.</summary>
    public int MaxSquads { get; }

    /// <summary>Gets the default tick delta in milliseconds.</summary>
    public int TickDeltaMilliseconds { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static RuntimeConfiguration CreateDefault() => Create(8, 4, 16);
}
