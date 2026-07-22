using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;
using DynamicPlanningAI.Configuration;
using DynamicPlanningAI.Runtime.Cover;
using DynamicPlanningAI.Runtime.Memory;
using DynamicPlanningAI.Runtime.Squad;
using DynamicPlanningAI.Sample.Hosting;
using DynamicPlanningAI.Sample.Navigation;
using DynamicPlanningAI.Sample.World;

namespace DynamicPlanningAI.Sample.Simulation;

/// <summary>
/// Outcome of a deterministic scenario run.
/// </summary>
public sealed class ScenarioResult
{
    /// <summary>
    /// Initializes a scenario result.
    /// </summary>
    public ScenarioResult(
        string scenarioName,
        int seed,
        int ticksExecuted,
        int maxTicks,
        bool success,
        string summary,
        string digest,
        string tracePath,
        IReadOnlyList<string> goalActionLog)
    {
        ScenarioName = scenarioName;
        Seed = seed;
        TicksExecuted = ticksExecuted;
        MaxTicks = maxTicks;
        Success = success;
        Summary = summary;
        Digest = digest;
        TracePath = tracePath;
        GoalActionLog = goalActionLog;
    }

    /// <summary>Gets the scenario name.</summary>
    public string ScenarioName { get; }

    /// <summary>Gets the seed.</summary>
    public int Seed { get; }

    /// <summary>Gets ticks executed.</summary>
    public int TicksExecuted { get; }

    /// <summary>Gets the tick budget.</summary>
    public int MaxTicks { get; }

    /// <summary>Gets whether success criteria were met.</summary>
    public bool Success { get; }

    /// <summary>Gets a concise summary.</summary>
    public string Summary { get; }

    /// <summary>Gets the deterministic digest.</summary>
    public string Digest { get; }

    /// <summary>Gets the trace file path.</summary>
    public string TracePath { get; }

    /// <summary>Gets goal/action log lines for replay comparison.</summary>
    public IReadOnlyList<string> GoalActionLog { get; }
}

/// <summary>
/// Shared simulation context available to scenarios.
/// </summary>
public sealed class SimulationContext
{
    /// <summary>
    /// Initializes a simulation context.
    /// </summary>
    public SimulationContext(
        ConfigurationBundle config,
        SimWorld world,
        GridNavigationService navigation,
        SampleHostServices host,
        RingTraceSink trace,
        SquadCoordinator squad,
        WorkingMemoryStore[] memories,
        int seed)
    {
        Config = config;
        World = world;
        Navigation = navigation;
        Host = host;
        Trace = trace;
        Squad = squad;
        Memories = memories;
        Seed = seed;
        PathBuffer = new NavigationNodeId[AiHardLimits.MaximumNavigationPathNodes];
        GoalActionLog = new List<string>(256);
        TickSequence = 0;
    }

    /// <summary>Gets configuration.</summary>
    public ConfigurationBundle Config { get; }

    /// <summary>Gets the world.</summary>
    public SimWorld World { get; }

    /// <summary>Gets navigation.</summary>
    public GridNavigationService Navigation { get; }

    /// <summary>Gets host services.</summary>
    public SampleHostServices Host { get; }

    /// <summary>Gets the trace sink.</summary>
    public RingTraceSink Trace { get; }

    /// <summary>Gets the squad coordinator.</summary>
    public SquadCoordinator Squad { get; }

    /// <summary>Gets per-agent memories.</summary>
    public WorkingMemoryStore[] Memories { get; }

    /// <summary>Gets the seed.</summary>
    public int Seed { get; }

    /// <summary>Gets the reusable path buffer.</summary>
    public NavigationNodeId[] PathBuffer { get; }

    /// <summary>Gets the goal/action log.</summary>
    public List<string> GoalActionLog { get; }

    /// <summary>Gets or sets the current tick sequence.</summary>
    public long TickSequence { get; set; }

    /// <summary>
    /// Writes a structured trace record.
    /// </summary>
    public void WriteTrace(
        AgentId agent,
        DiagnosticSubsystem subsystem,
        TraceEventCode code,
        int primaryId,
        int secondaryId,
        int valueA,
        int valueB,
        int statusCode)
    {
        AiTick tick = new(TickSequence, Config.Runtime.TickDeltaMilliseconds);
        Trace.Write(new TraceRecord(
            tick,
            agent,
            SquadId.Invalid,
            subsystem,
            (int)code,
            primaryId,
            secondaryId,
            valueA,
            valueB,
            statusCode));
    }

    /// <summary>
    /// Logs a goal/action transition for digest comparison.
    /// </summary>
    public void LogGoalAction(AgentId agent, string goal, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (World.TryGetAgent(agent, out SimAgentState? state) && state is not null)
        {
            state.ActiveGoal = goal;
            state.ActiveAction = action;
        }

        GoalActionLog.Add(
            string.Concat(
                TickSequence.ToString(CultureInfo.InvariantCulture),
                "|",
                agent.Value.ToString(CultureInfo.InvariantCulture),
                "|",
                goal,
                "|",
                action));
        WriteTrace(
            agent,
            DiagnosticSubsystem.GoalArbitration,
            TraceEventCode.GoalSelected,
            agent.Value,
            goal.GetHashCode(StringComparison.Ordinal),
            action.GetHashCode(StringComparison.Ordinal),
            0,
            0);
    }

    /// <summary>
    /// Moves an agent one step along a path toward a destination.
    /// </summary>
    public bool StepToward(AgentId agent, Int2 destination)
    {
        if (!World.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return false;
        }

        NavigationPathQuery query = new(
            agent,
            NavigationNodeId.Invalid,
            NavigationNodeId.Invalid,
            state.Position,
            destination,
            0,
            allowPartial: true);
        NavigationQueryResult result = Navigation.TryFindPath(in query, PathBuffer, out int written);
        if (!result.HasPath || written < 2)
        {
            return state.Position == destination;
        }

        Int2 next = World.Map.ToCell(PathBuffer[1].Value);
        OperationStatus move = Host.SubmitMove(agent, next);
        return move == OperationStatus.Success;
    }

    /// <summary>
    /// Selects best defensive cover using the runtime evaluator.
    /// </summary>
    public bool TrySelectCover(AgentId agent, Int2 threat, out TacticalPointId pointId, out Int2 pointPos)
    {
        pointId = TacticalPointId.Invalid;
        pointPos = Int2.Zero;
        if (!World.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return false;
        }

        CoverCandidateSample[] samples = new CoverCandidateSample[AiHardLimits.MaximumCoverCandidates];
        int count = 0;
        for (int i = 0; i < World.TacticalPointCount && count < samples.Length; i++)
        {
            if (World.GetTacticalCategory(i) != TacticalPointCategory.Cover)
            {
                continue;
            }

            Int2 pos = World.GetTacticalPosition(i);
            TacticalPointRecord record = new(
                TacticalPointId.FromInt32(i),
                NavigationNodeId.FromInt32(World.Map.ToIndex(pos)),
                pos,
                Direction8.East,
                TacticalPointCategory.Cover,
                CoverHeight.High,
                AgentStance.Crouching,
                exposure: 2,
                quality: 8,
                NavigationAreaId.FromInt32(0),
                maxOccupants: 1);
            int pathCost = Int2.ManhattanDistance(state.Position, pos);
            bool lof = Host.HasLineOfSight(pos, threat);
            bool inDanger = Host.IsPositionDangerous(pos);
            samples[count] = new CoverCandidateSample(
                in record,
                pathCost,
                occupantCount: 0,
                isReservedByOther: false,
                reservationHeld: false,
                hasLineOfFire: lof,
                inGrenadeDanger: inDanger,
                isDestroyed: false,
                isNavigationAvailable: true,
                allySeparation: 4,
                orderCompatible: true,
                CoverInvalidationReason.None);
            count = checked(count + 1);
        }

        CoverEvaluationContext context = new(
            state.Position,
            threat,
            Direction8.West,
            state.Stance,
            SquadOrderType.MoveToCover);
        CoverQueryResult query = CoverEvaluator.BestDefensiveCover(samples.AsSpan(0, count), in context);
        if (query.Status != OperationStatus.Success)
        {
            return false;
        }

        pointId = query.PointId;
        _ = Host.TryGetPosition(pointId, out pointPos);
        WriteTrace(
            agent,
            DiagnosticSubsystem.Cover,
            TraceEventCode.CoverSelected,
            pointId.Value,
            query.Score,
            pointPos.X,
            pointPos.Y,
            (int)query.Status);
        return true;
    }
}

/// <summary>
/// Scenario contract.
/// </summary>
public interface IScenario
{
    /// <summary>Gets the scenario name.</summary>
    public string Name { get; }

    /// <summary>Gets the fixed default seed.</summary>
    public int DefaultSeed { get; }

    /// <summary>Gets the maximum ticks.</summary>
    public int MaxTicks { get; }

    /// <summary>
    /// Runs the scenario on a prepared context.
    /// </summary>
    public ScenarioRunOutcome Run(SimulationContext context);
}

/// <summary>
/// Internal run outcome before file IO.
/// </summary>
public readonly struct ScenarioRunOutcome
{
    /// <summary>
    /// Initializes a run outcome.
    /// </summary>
    public ScenarioRunOutcome(bool success, string summary, int ticksExecuted)
    {
        Success = success;
        Summary = summary;
        TicksExecuted = ticksExecuted;
    }

    /// <summary>Gets success.</summary>
    public bool Success { get; }

    /// <summary>Gets summary.</summary>
    public string Summary { get; }

    /// <summary>Gets ticks executed.</summary>
    public int TicksExecuted { get; }
}
