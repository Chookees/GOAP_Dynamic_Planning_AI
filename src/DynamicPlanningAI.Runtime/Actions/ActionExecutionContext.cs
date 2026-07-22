using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Runtime.Execution;
using DynamicPlanningAI.Runtime.Planning;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Mutable per-action execution context shared with host services.
/// </summary>
/// <remarks>
/// Host service references may be null; executors must treat null hosts as
/// <see cref="ActionFailureReason.HostServiceFailure"/> rather than throwing.
/// The struct never allocates.
/// </remarks>
public struct ActionExecutionContext
{
    /// <summary>
    /// Gets or sets the executing agent.
    /// </summary>
    public AgentId AgentId { get; set; }

    /// <summary>
    /// Gets or sets the grounded action candidate.
    /// </summary>
    public ActionCandidate Candidate { get; set; }

    /// <summary>
    /// Gets or sets the agent-local world state mutated on success.
    /// </summary>
    public SymbolicWorldState? WorldState { get; set; }

    /// <summary>
    /// Gets or sets the current tick sequence.
    /// </summary>
    public long TickSequence { get; set; }

    /// <summary>
    /// Gets or sets the tick delta in milliseconds.
    /// </summary>
    public int DeltaMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets ticks spent in the current action.
    /// </summary>
    public int TicksElapsed { get; set; }

    /// <summary>
    /// Gets or sets the current action lifecycle status.
    /// </summary>
    public ActionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the failure reason when terminal failure occurs.
    /// </summary>
    public ActionFailureReason FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the maximum ticks before timeout.
    /// </summary>
    public int MaximumTicks { get; set; }

    /// <summary>
    /// Gets or sets an optional failure-knowledge sink.
    /// </summary>
    public IFailureKnowledgeSink? FailureKnowledge { get; set; }

    /// <summary>
    /// Gets or sets the optional movement service.
    /// </summary>
    public IMovementService? Movement { get; set; }

    /// <summary>
    /// Gets or sets the optional navigation service.
    /// </summary>
    public INavigationService? Navigation { get; set; }

    /// <summary>
    /// Gets or sets the optional weapon service.
    /// </summary>
    public IWeaponService? Weapons { get; set; }

    /// <summary>
    /// Gets or sets the optional command sink.
    /// </summary>
    public IAgentCommandSink? Commands { get; set; }

    /// <summary>
    /// Gets or sets the optional animation service.
    /// </summary>
    public IAnimationService? Animation { get; set; }

    /// <summary>
    /// Gets or sets the optional interaction service.
    /// </summary>
    public IInteractionService? Interaction { get; set; }

    /// <summary>
    /// Gets or sets the optional smart-object service.
    /// </summary>
    public ISmartObjectService? SmartObjects { get; set; }

    /// <summary>
    /// Gets or sets the optional tactical-point provider.
    /// </summary>
    public ITacticalPointProvider? TacticalPoints { get; set; }

    /// <summary>
    /// Gets or sets the optional danger provider.
    /// </summary>
    public IDangerProvider? Danger { get; set; }

    /// <summary>
    /// Gets or sets a scratch destination used by movement actions.
    /// </summary>
    public Int2 ScratchDestination { get; set; }

    /// <summary>
    /// Gets or sets an internal progress counter for multi-tick actions.
    /// </summary>
    public int ProgressCounter { get; set; }

    /// <summary>
    /// Gets or sets the required progress ticks before success.
    /// </summary>
    public int RequiredProgressTicks { get; set; }
}
