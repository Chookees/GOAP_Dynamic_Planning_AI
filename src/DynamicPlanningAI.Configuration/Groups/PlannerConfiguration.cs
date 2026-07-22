using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable planner budget configuration.
/// </summary>
public sealed class PlannerConfiguration
{
    /// <summary>
    /// Creates a validated planner configuration.
    /// </summary>
    public static PlannerConfiguration Create(
        int maxNodes,
        int maxExpansionsPerStep,
        int maxStepsPerTick,
        int maxPlanLength,
        int maxReplanningAttempts)
    {
        return new PlannerConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(PlannerConfiguration), nameof(MaxNodes), maxNodes, AiHardLimits.MaximumPlannerNodes, 1101),
            ConfigGuard.RequirePositiveAtMost(nameof(PlannerConfiguration), nameof(MaxExpansionsPerStep), maxExpansionsPerStep, AiHardLimits.MaximumPlannerExpansionsPerStep, 1102),
            ConfigGuard.RequirePositiveAtMost(nameof(PlannerConfiguration), nameof(MaxStepsPerTick), maxStepsPerTick, AiHardLimits.MaximumPlannerStepsPerTick, 1103),
            ConfigGuard.RequirePositiveAtMost(nameof(PlannerConfiguration), nameof(MaxPlanLength), maxPlanLength, AiHardLimits.MaximumPlanLength, 1104),
            ConfigGuard.RequirePositiveAtMost(nameof(PlannerConfiguration), nameof(MaxReplanningAttempts), maxReplanningAttempts, AiHardLimits.MaximumReplanningAttempts, 1105));
    }

    private PlannerConfiguration(
        int maxNodes,
        int maxExpansionsPerStep,
        int maxStepsPerTick,
        int maxPlanLength,
        int maxReplanningAttempts)
    {
        MaxNodes = maxNodes;
        MaxExpansionsPerStep = maxExpansionsPerStep;
        MaxStepsPerTick = maxStepsPerTick;
        MaxPlanLength = maxPlanLength;
        MaxReplanningAttempts = maxReplanningAttempts;
    }

    /// <summary>Gets the planner workspace node capacity.</summary>
    public int MaxNodes { get; }

    /// <summary>Gets expansions allowed per planner step.</summary>
    public int MaxExpansionsPerStep { get; }

    /// <summary>Gets planner steps allowed per tick.</summary>
    public int MaxStepsPerTick { get; }

    /// <summary>Gets the maximum reconstructed plan length.</summary>
    public int MaxPlanLength { get; }

    /// <summary>Gets the maximum replanning attempts per goal activation.</summary>
    public int MaxReplanningAttempts { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static PlannerConfiguration CreateDefault() =>
        Create(
            AiHardLimits.MaximumPlannerNodes,
            AiHardLimits.MaximumPlannerExpansionsPerStep,
            AiHardLimits.MaximumPlannerStepsPerTick,
            AiHardLimits.MaximumPlanLength,
            AiHardLimits.MaximumReplanningAttempts);
}
