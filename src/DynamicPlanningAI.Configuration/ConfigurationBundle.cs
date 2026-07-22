using System;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Configuration.Groups;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration;

/// <summary>
/// Frozen bundle of validated immutable configuration groups.
/// </summary>
/// <remarks>
/// After construction, no group may be replaced. JSON is loaded only during
/// initialization; runtime code must not deserialize configuration mid-combat.
/// </remarks>
public sealed class ConfigurationBundle
{
    /// <summary>
    /// Creates a validated bundle from explicit groups.
    /// </summary>
    public static ConfigurationBundle Create(
        RuntimeConfiguration runtime,
        PlannerConfiguration planner,
        PerceptionConfiguration perception,
        MemoryConfiguration memory,
        GoalConfiguration goals,
        ActionConfiguration actions,
        CoverConfiguration cover,
        NavigationConfiguration navigation,
        SquadConfiguration squad,
        CommunicationConfiguration communication,
        DiagnosticsConfiguration diagnostics,
        SampleConfiguration sample)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(perception);
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(cover);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(squad);
        ArgumentNullException.ThrowIfNull(communication);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(sample);

        if (runtime.MaxAgents < squad.MaxAgentsPerSquad)
        {
            throw new ConfigValidationException(
                nameof(ConfigurationBundle),
                "Runtime.MaxAgents must be >= Squad.MaxAgentsPerSquad.",
                3001);
        }

        return new ConfigurationBundle(
            runtime,
            planner,
            perception,
            memory,
            goals,
            actions,
            cover,
            navigation,
            squad,
            communication,
            diagnostics,
            sample);
    }

    private ConfigurationBundle(
        RuntimeConfiguration runtime,
        PlannerConfiguration planner,
        PerceptionConfiguration perception,
        MemoryConfiguration memory,
        GoalConfiguration goals,
        ActionConfiguration actions,
        CoverConfiguration cover,
        NavigationConfiguration navigation,
        SquadConfiguration squad,
        CommunicationConfiguration communication,
        DiagnosticsConfiguration diagnostics,
        SampleConfiguration sample)
    {
        Runtime = runtime;
        Planner = planner;
        Perception = perception;
        Memory = memory;
        Goals = goals;
        Actions = actions;
        Cover = cover;
        Navigation = navigation;
        Squad = squad;
        Communication = communication;
        Diagnostics = diagnostics;
        Sample = sample;
        IsFrozen = true;
    }

    /// <summary>Gets the runtime configuration.</summary>
    public RuntimeConfiguration Runtime { get; }

    /// <summary>Gets the planner configuration.</summary>
    public PlannerConfiguration Planner { get; }

    /// <summary>Gets the perception configuration.</summary>
    public PerceptionConfiguration Perception { get; }

    /// <summary>Gets the memory configuration.</summary>
    public MemoryConfiguration Memory { get; }

    /// <summary>Gets the goal configuration.</summary>
    public GoalConfiguration Goals { get; }

    /// <summary>Gets the action configuration.</summary>
    public ActionConfiguration Actions { get; }

    /// <summary>Gets the cover configuration.</summary>
    public CoverConfiguration Cover { get; }

    /// <summary>Gets the navigation configuration.</summary>
    public NavigationConfiguration Navigation { get; }

    /// <summary>Gets the squad configuration.</summary>
    public SquadConfiguration Squad { get; }

    /// <summary>Gets the communication configuration.</summary>
    public CommunicationConfiguration Communication { get; }

    /// <summary>Gets the diagnostics configuration.</summary>
    public DiagnosticsConfiguration Diagnostics { get; }

    /// <summary>Gets the sample configuration.</summary>
    public SampleConfiguration Sample { get; }

    /// <summary>Gets a value indicating whether the bundle is frozen.</summary>
    public bool IsFrozen { get; }

    /// <summary>Creates a fully default sample bundle.</summary>
    public static ConfigurationBundle CreateDefault()
    {
        return Create(
            RuntimeConfiguration.CreateDefault(),
            PlannerConfiguration.CreateDefault(),
            PerceptionConfiguration.CreateDefault(),
            MemoryConfiguration.CreateDefault(),
            GoalConfiguration.CreateDefault(),
            ActionConfiguration.CreateDefault(),
            CoverConfiguration.CreateDefault(),
            NavigationConfiguration.CreateDefault(),
            SquadConfiguration.CreateDefault(),
            CommunicationConfiguration.CreateDefault(),
            DiagnosticsConfiguration.CreateDefault(),
            SampleConfiguration.CreateDefault());
    }

    /// <summary>
    /// Attempts to create a default bundle, returning a structured result on failure.
    /// </summary>
    public static ConfigurationValidationResult TryCreateDefault(out ConfigurationBundle? bundle)
    {
        try
        {
            bundle = CreateDefault();
            return ConfigurationValidationResult.Success();
        }
        catch (ConfigValidationException ex)
        {
            bundle = null;
            return ConfigGuard.ToResult(ex);
        }
    }
}
