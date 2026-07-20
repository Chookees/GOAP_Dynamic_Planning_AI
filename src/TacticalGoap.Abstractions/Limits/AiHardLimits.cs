namespace TacticalGoap.Abstractions.Limits;

/// <summary>
/// Compile-time absolute capacity ceilings for every bounded runtime subsystem.
/// </summary>
/// <remarks>
/// Runtime configuration may select lower values, but never values above these
/// hard limits. Every production runtime loop must refer to one of these limits
/// or to a value previously validated against one. Changing a hard limit is an
/// architecture decision and requires updating documentation, audit rules, and
/// capacity tests.
/// </remarks>
public static class AiHardLimits
{
    /// <summary>
    /// Maximum number of agents that may be registered in a single runtime.
    /// </summary>
    public const int MaximumAgents = 64;

    /// <summary>
    /// Maximum number of concurrent squads.
    /// </summary>
    public const int MaximumSquads = 16;

    /// <summary>
    /// Maximum agents that may belong to one squad.
    /// </summary>
    public const int MaximumAgentsPerSquad = 8;

    /// <summary>
    /// Maximum goals registered per agent.
    /// </summary>
    public const int MaximumGoalsPerAgent = 32;

    /// <summary>
    /// Maximum immutable action definitions registered in the runtime.
    /// </summary>
    public const int MaximumActionDefinitions = 128;

    /// <summary>
    /// Maximum action candidates generated for one planning request.
    /// </summary>
    public const int MaximumActionCandidates = 256;

    /// <summary>
    /// Maximum actions in a reconstructed plan.
    /// </summary>
    public const int MaximumPlanLength = 16;

    /// <summary>
    /// Maximum planner search nodes in a workspace.
    /// </summary>
    public const int MaximumPlannerNodes = 512;

    /// <summary>
    /// Maximum node expansions allowed in a single planner step call.
    /// </summary>
    public const int MaximumPlannerExpansionsPerStep = 64;

    /// <summary>
    /// Maximum planner step invocations permitted for one agent in one tick.
    /// </summary>
    public const int MaximumPlannerStepsPerTick = 8;

    /// <summary>
    /// Maximum symbolic world facts (bit-mask width).
    /// </summary>
    public const int MaximumWorldFacts = 64;

    /// <summary>
    /// Maximum working-memory records per agent.
    /// </summary>
    public const int MaximumMemoryRecords = 64;

    /// <summary>
    /// Maximum perception candidates processed per sensor tick.
    /// </summary>
    public const int MaximumPerceptionCandidates = 32;

    /// <summary>
    /// Maximum sound events buffered per tick.
    /// </summary>
    public const int MaximumSoundEvents = 32;

    /// <summary>
    /// Maximum damage events buffered per tick.
    /// </summary>
    public const int MaximumDamageEvents = 16;

    /// <summary>
    /// Maximum danger events buffered per tick.
    /// </summary>
    public const int MaximumDangerEvents = 16;

    /// <summary>
    /// Maximum tactical points registered for a scenario.
    /// </summary>
    public const int MaximumTacticalPoints = 256;

    /// <summary>
    /// Maximum cover candidates scored per evaluation.
    /// </summary>
    public const int MaximumCoverCandidates = 32;

    /// <summary>
    /// Maximum concurrent reservations.
    /// </summary>
    public const int MaximumReservations = 128;

    /// <summary>
    /// Maximum navigation path nodes written into a caller buffer.
    /// </summary>
    public const int MaximumNavigationPathNodes = 128;

    /// <summary>
    /// Maximum outstanding squad orders.
    /// </summary>
    public const int MaximumSquadOrders = 64;

    /// <summary>
    /// Maximum search sectors in a scenario.
    /// </summary>
    public const int MaximumSearchSectors = 32;

    /// <summary>
    /// Maximum diagnostic ring-buffer records.
    /// </summary>
    public const int MaximumDiagnosticRecords = 4096;

    /// <summary>
    /// Maximum pending communication requests.
    /// </summary>
    public const int MaximumCommunicationRequests = 32;

    /// <summary>
    /// Maximum replanning attempts for one goal activation.
    /// </summary>
    public const int MaximumReplanningAttempts = 8;

    /// <summary>
    /// Maximum ticks an individual runtime action may remain Running.
    /// </summary>
    public const int MaximumActionTicks = 1024;

    /// <summary>
    /// Maximum tick delta in milliseconds accepted by <c>AiTick</c> validation.
    /// </summary>
    public const int MaximumTickDeltaMilliseconds = 250;

    /// <summary>
    /// Maximum non-negative integer action cost before overflow rejection.
    /// </summary>
    public const int MaximumActionCost = 1_000_000;

    /// <summary>
    /// Maximum method logical-line count enforced by the audit tool.
    /// </summary>
    public const int MaximumMethodLogicalLines = 60;
}
