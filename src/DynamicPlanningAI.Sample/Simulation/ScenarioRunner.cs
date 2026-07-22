using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration;
using DynamicPlanningAI.Runtime.Memory;
using DynamicPlanningAI.Runtime.Squad;
using DynamicPlanningAI.Sample.Hosting;
using DynamicPlanningAI.Sample.Navigation;
using DynamicPlanningAI.Sample.Scenarios;
using DynamicPlanningAI.Sample.World;

namespace DynamicPlanningAI.Sample.Simulation;

/// <summary>
/// Builds a standard combat arena used by most scenarios.
/// </summary>
public static class ArenaFactory
{
    /// <summary>
    /// Creates a bordered arena with optional center wall and openings.
    /// </summary>
    public static SimWorld CreateStandardArena(ConfigurationBundle config, bool centerWall)
    {
        ArgumentNullException.ThrowIfNull(config);
        GridMap map = new(config.Navigation.GridWidth, config.Navigation.GridHeight);
        for (int x = 0; x < map.Width; x++)
        {
            map.SetKind(new Int2(x, 0), CellKind.Wall);
            map.SetKind(new Int2(x, map.Height - 1), CellKind.Wall);
        }

        for (int y = 0; y < map.Height; y++)
        {
            map.SetKind(new Int2(0, y), CellKind.Wall);
            map.SetKind(new Int2(map.Width - 1, y), CellKind.Wall);
        }

        if (centerWall)
        {
            int mid = map.Width / 2;
            for (int y = 1; y < map.Height - 1; y++)
            {
                map.SetKind(new Int2(mid, y), CellKind.Wall);
            }
        }

        return new SimWorld(map, config.Perception.VisionRangeCells);
    }
}

/// <summary>
/// Runs named scenarios deterministically and writes trace files.
/// </summary>
public sealed class ScenarioRunner
{
    private readonly ConfigurationBundle _config;
    private readonly Dictionary<string, IScenario> _scenarios;

    /// <summary>
    /// Initializes a runner with the scenario catalog.
    /// </summary>
    public ScenarioRunner(ConfigurationBundle config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _scenarios = new Dictionary<string, IScenario>(StringComparer.OrdinalIgnoreCase);
        Register(new BasicAttackScenario());
        Register(new TakeCoverScenario());
        Register(new AdvanceUnderSuppressionScenario());
        Register(new EmergentSideAttackScenario());
        Register(new GrenadeEscapeScenario());
        Register(new BlockedDoorReplanningScenario());
        Register(new WindowTraversalFallbackScenario());
        Register(new LostTargetSearchScenario());
        Register(new BlindFireUnderPressureScenario());
        Register(new SquadSearchScenario());
        Register(new OrderOverriddenByDangerScenario());
        Register(new ZeroAllocationSteadyStateScenario());
    }

    /// <summary>Gets registered scenario names.</summary>
    public IReadOnlyCollection<string> ScenarioNames => _scenarios.Keys;

    /// <summary>
    /// Runs a scenario by name.
    /// </summary>
    public ScenarioResult Run(string scenarioName, int? seedOverride, string? traceDirectory)
    {
        if (!_scenarios.TryGetValue(scenarioName, out IScenario? scenario) || scenario is null)
        {
            throw new ArgumentException("Unknown scenario: " + scenarioName, nameof(scenarioName));
        }

        int seed = seedOverride ?? scenario.DefaultSeed;
        string traceDir = string.IsNullOrWhiteSpace(traceDirectory)
            ? _config.Sample.TraceDirectory
            : traceDirectory;
        Directory.CreateDirectory(traceDir);

        SimulationContext context = CreateContext(seed);
        ScenarioRunOutcome outcome = scenario.Run(context);
        string digest = context.Trace.BuildDigest();
        string tracePath = Path.Combine(
            traceDir,
            scenario.Name + "-" + seed.ToString(CultureInfo.InvariantCulture) + ".trace");
        WriteTraceFile(tracePath, scenario.Name, seed, outcome, digest, context);

        return new ScenarioResult(
            scenario.Name,
            seed,
            outcome.TicksExecuted,
            scenario.MaxTicks,
            outcome.Success,
            outcome.Summary,
            digest,
            tracePath,
            context.GoalActionLog);
    }

    private void Register(IScenario scenario) => _scenarios[scenario.Name] = scenario;

    private SimulationContext CreateContext(int seed)
    {
        SimWorld world = ArenaFactory.CreateStandardArena(_config, centerWall: false);
        GridNavigationService navigation = new(
            world.Map,
            _config.Navigation.MaxExpansions,
            _config.Navigation.MaxPathNodes);
        SampleHostServices host = new(world);
        RingTraceSink trace = new(_config.Diagnostics.RingBufferCapacity);
        SquadCoordinatorOptions options = new()
        {
            MaxSquads = _config.Squad.MaxSquads,
            MaxAgentsPerSquad = _config.Squad.MaxAgentsPerSquad,
            ReclusterIntervalTicks = _config.Squad.ReclusterIntervalTicks,
            DefaultOrderDurationTicks = _config.Squad.DefaultOrderDurationTicks,
        };
        SquadCoordinator squad = new(options);
        WorkingMemoryStore[] memories = new WorkingMemoryStore[AiHardLimits.MaximumAgents];
        for (int i = 0; i < memories.Length; i++)
        {
            memories[i] = new WorkingMemoryStore(_config.Memory.MaxRecords);
        }

        return new SimulationContext(_config, world, navigation, host, trace, squad, memories, seed);
    }

    private static void WriteTraceFile(
        string path,
        string name,
        int seed,
        ScenarioRunOutcome outcome,
        string digest,
        SimulationContext context)
    {
        StringBuilder sb = new(4096);
        sb.AppendLine("# DynamicPlanningAI Sample Trace");
        sb.Append("scenario=").AppendLine(name);
        sb.Append("seed=").AppendLine(seed.ToString(CultureInfo.InvariantCulture));
        sb.Append("success=").AppendLine(outcome.Success ? "1" : "0");
        sb.Append("ticks=").AppendLine(outcome.TicksExecuted.ToString(CultureInfo.InvariantCulture));
        sb.Append("summary=").AppendLine(outcome.Summary);
        sb.Append("digest=").AppendLine(digest);
        sb.AppendLine("# goal|action log");
        for (int i = 0; i < context.GoalActionLog.Count; i++)
        {
            sb.AppendLine(context.GoalActionLog[i]);
        }

        File.WriteAllText(path, sb.ToString());
    }
}
