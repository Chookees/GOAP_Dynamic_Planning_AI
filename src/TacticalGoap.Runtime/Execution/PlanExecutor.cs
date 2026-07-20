using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Actions;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Execution;

/// <summary>
/// Advances a reconstructed plan one action at a time under host ticks.
/// </summary>
public sealed class PlanExecutor
{
    private readonly ActionDefinitionRegistry _registry;
    private readonly ReplanPolicy _replanPolicy;
    private readonly FailureKnowledgeWriter _failureKnowledge;
    private Plan? _plan;
    private ActionExecutionContext _context;
    private IGoapActionExecutor? _currentExecutor;
    private bool _actionStarted;
    private bool _replanRequested;
    private bool _emergencyReplan;

    /// <summary>
    /// Initializes a plan executor.
    /// </summary>
    /// <param name="registry">Action definition and executor registry.</param>
    /// <param name="replanPolicy">Replanning policy.</param>
    /// <param name="failureKnowledge">Failure-knowledge sink.</param>
    public PlanExecutor(
        ActionDefinitionRegistry registry,
        ReplanPolicy replanPolicy,
        FailureKnowledgeWriter failureKnowledge)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(replanPolicy);
        ArgumentNullException.ThrowIfNull(failureKnowledge);
        _registry = registry;
        _replanPolicy = replanPolicy;
        _failureKnowledge = failureKnowledge;
        _plan = null;
        _currentExecutor = null;
        _actionStarted = false;
        _replanRequested = false;
        _emergencyReplan = false;
    }

    /// <summary>Gets a value indicating whether a replan was requested.</summary>
    public bool ReplanRequested => _replanRequested;

    /// <summary>Gets a value indicating whether the replan is emergency.</summary>
    public bool EmergencyReplan => _emergencyReplan;

    /// <summary>Gets the active plan, if any.</summary>
    public Plan? ActivePlan => _plan;

    /// <summary>Gets the failure-knowledge writer.</summary>
    public FailureKnowledgeWriter FailureKnowledge => _failureKnowledge;

    /// <summary>
    /// Installs a plan for execution.
    /// </summary>
    /// <param name="plan">Plan to execute.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus Install(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Validate() != OperationStatus.Success)
        {
            return OperationStatus.ContractViolation;
        }

        CancelCurrentAction();
        _plan = plan;
        _actionStarted = false;
        _currentExecutor = null;
        _replanRequested = false;
        _emergencyReplan = false;
        _replanPolicy.Reset();
        return OperationStatus.Success;
    }

    /// <summary>
    /// Invalidates the active plan and requests a replan when policy allows.
    /// </summary>
    /// <param name="tickSequence">Current tick.</param>
    /// <param name="emergency">Whether the invalidate is an emergency.</param>
    /// <returns>Success or rejection.</returns>
    [FrozenRuntimePath]
    public OperationStatus Invalidate(long tickSequence, bool emergency)
    {
        CancelCurrentAction();
        if (_plan is not null)
        {
            _plan.Invalidate();
        }

        _actionStarted = false;
        _currentExecutor = null;
        return RequestReplan(tickSequence, emergency);
    }

    /// <summary>
    /// Peeks at the current plan action without advancing.
    /// </summary>
    /// <param name="candidate">Receives the current candidate.</param>
    /// <returns><see langword="true"/> when an action remains.</returns>
    [FrozenRuntimePath]
    public bool TryPeek(out ActionCandidate candidate)
    {
        if (_plan is null)
        {
            candidate = default;
            return false;
        }

        return _plan.TryPeek(out candidate);
    }

    /// <summary>
    /// Ticks the current action, handling success, failure, and timeout.
    /// </summary>
    /// <param name="agentId">Executing agent.</param>
    /// <param name="worldState">Agent world state.</param>
    /// <param name="tickSequence">Current tick sequence.</param>
    /// <param name="deltaMilliseconds">Tick delta.</param>
    /// <param name="hostServices">Optional host services bundle.</param>
    /// <returns>Latest action tick result.</returns>
    [FrozenRuntimePath]
    public ActionTickResult Tick(
        AgentId agentId,
        SymbolicWorldState worldState,
        long tickSequence,
        int deltaMilliseconds,
        in ActionHostServices hostServices)
    {
        ArgumentNullException.ThrowIfNull(worldState);
        _replanRequested = false;
        _emergencyReplan = false;

        if (_plan is null || !_plan.TryPeek(out ActionCandidate candidate))
        {
            return new ActionTickResult(ActionStatus.NotStarted, ActionFailureReason.None, 0);
        }

        if (!_actionStarted)
        {
            OperationStatus beginStatus = BeginCurrent(agentId, worldState, tickSequence, deltaMilliseconds, candidate, in hostServices);
            if (beginStatus != OperationStatus.Success)
            {
                if (_plan is not null)
                {
                    _plan.Invalidate();
                }

                _actionStarted = false;
                _currentExecutor = null;
                _ = RequestReplan(tickSequence, emergency: true);
                return ActionTickResult.Failed(ActionFailureReason.HostServiceFailure, 0);
            }
        }

        if (_currentExecutor is null)
        {
            _ = RequestReplan(tickSequence, emergency: true);
            return ActionTickResult.Failed(ActionFailureReason.HostServiceFailure, _context.TicksElapsed);
        }

        ActionTickResult result = _currentExecutor.Tick(ref _context);
        return HandleTickResult(result, tickSequence);
    }

    [FrozenRuntimePath]
    private OperationStatus BeginCurrent(
        AgentId agentId,
        SymbolicWorldState worldState,
        long tickSequence,
        int deltaMilliseconds,
        ActionCandidate candidate,
        in ActionHostServices hostServices)
    {
        if (!_registry.TryGetExecutor(candidate.DefinitionId, out IGoapActionExecutor? executor) || executor is null)
        {
            return OperationStatus.NotFound;
        }

        _currentExecutor = executor;
        _context = CreateContext(agentId, worldState, tickSequence, deltaMilliseconds, candidate, in hostServices);
        ActionTickResult beginResult = executor.Begin(ref _context);
        _actionStarted = true;
        if (beginResult.IsFailed)
        {
            WriteFailureKnowledge(beginResult.FailureReason);
            return OperationStatus.Failed;
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private ActionTickResult HandleTickResult(ActionTickResult result, long tickSequence)
    {
        if (result.IsSucceeded)
        {
            _ = _plan!.Advance();
            _actionStarted = false;
            _currentExecutor = null;
            return result;
        }

        if (result.Status == ActionStatus.TimedOut || result.IsFailed)
        {
            WriteFailureKnowledge(result.FailureReason);
            if (_plan is not null)
            {
                _plan.Invalidate();
            }

            _actionStarted = false;
            _currentExecutor = null;
            bool emergency = result.FailureReason == ActionFailureReason.DangerChanged
                || result.FailureReason == ActionFailureReason.CoverInvalidated;
            _ = RequestReplan(tickSequence, emergency);
            return result;
        }

        return result;
    }

    [FrozenRuntimePath]
    private OperationStatus RequestReplan(long tickSequence, bool emergency)
    {
        if (!_replanPolicy.CanReplan(tickSequence, emergency))
        {
            return OperationStatus.Failed;
        }

        _replanPolicy.RecordReplan(tickSequence);
        _replanRequested = true;
        _emergencyReplan = emergency;
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private void CancelCurrentAction()
    {
        if (_actionStarted && _currentExecutor is not null)
        {
            _ = _currentExecutor.Cancel(ref _context);
        }
    }

    [FrozenRuntimePath]
    private void WriteFailureKnowledge(ActionFailureReason reason)
    {
        MemoryType? type = MapFailureToMemory(reason);
        if (!type.HasValue)
        {
            return;
        }

        _ = _failureKnowledge.TryWrite(
            _context.AgentId,
            type.Value,
            _context.Candidate.BoundEntityId.IsValid ? _context.Candidate.BoundEntityId.Value : 0,
            _context.Candidate.DefinitionId.IsValid ? _context.Candidate.DefinitionId.Value : 0,
            _context.TickSequence);
    }

    [FrozenRuntimePath]
    private static MemoryType? MapFailureToMemory(ActionFailureReason reason)
    {
        if (reason == ActionFailureReason.DoorBlocked)
        {
            return MemoryType.DoorBlocked;
        }

        if (reason == ActionFailureReason.NoPath || reason == ActionFailureReason.PathInvalidated)
        {
            return MemoryType.NavigationFailed;
        }

        if (reason == ActionFailureReason.TraversalUnavailable)
        {
            return MemoryType.TraversalFailed;
        }

        if (reason == ActionFailureReason.CoverInvalidated || reason == ActionFailureReason.ReservationLost)
        {
            return MemoryType.CoverInvalid;
        }

        if (reason == ActionFailureReason.TargetLost)
        {
            return MemoryType.TargetLost;
        }

        return null;
    }

    [FrozenRuntimePath]
    private ActionExecutionContext CreateContext(
        AgentId agentId,
        SymbolicWorldState worldState,
        long tickSequence,
        int deltaMilliseconds,
        ActionCandidate candidate,
        in ActionHostServices hostServices)
    {
        return new ActionExecutionContext
        {
            AgentId = agentId,
            Candidate = candidate,
            WorldState = worldState,
            TickSequence = tickSequence,
            DeltaMilliseconds = deltaMilliseconds,
            TicksElapsed = 0,
            Status = ActionStatus.NotStarted,
            FailureReason = ActionFailureReason.None,
            MaximumTicks = AiHardLimits.MaximumActionTicks,
            FailureKnowledge = _failureKnowledge,
            Movement = hostServices.Movement,
            Navigation = hostServices.Navigation,
            Weapons = hostServices.Weapons,
            Commands = hostServices.Commands,
            Animation = hostServices.Animation,
            Interaction = hostServices.Interaction,
            SmartObjects = hostServices.SmartObjects,
            TacticalPoints = hostServices.TacticalPoints,
            Danger = hostServices.Danger,
            ScratchDestination = default,
            ProgressCounter = 0,
            RequiredProgressTicks = 0,
        };
    }

    /// <summary>
    /// Clears executor state for a new scenario.
    /// </summary>
    public void Reset()
    {
        CancelCurrentAction();
        _plan = null;
        _currentExecutor = null;
        _actionStarted = false;
        _replanRequested = false;
        _emergencyReplan = false;
        _replanPolicy.Reset();
        _failureKnowledge.Reset();
    }
}

/// <summary>
/// Optional host service bundle passed into plan execution.
/// </summary>
public readonly struct ActionHostServices
{
    /// <summary>Gets or sets movement.</summary>
    public Abstractions.Hosting.IMovementService? Movement { get; init; }

    /// <summary>Gets or sets navigation.</summary>
    public Abstractions.Hosting.INavigationService? Navigation { get; init; }

    /// <summary>Gets or sets weapons.</summary>
    public Abstractions.Hosting.IWeaponService? Weapons { get; init; }

    /// <summary>Gets or sets commands.</summary>
    public Abstractions.Hosting.IAgentCommandSink? Commands { get; init; }

    /// <summary>Gets or sets animation.</summary>
    public Abstractions.Hosting.IAnimationService? Animation { get; init; }

    /// <summary>Gets or sets interaction.</summary>
    public Abstractions.Hosting.IInteractionService? Interaction { get; init; }

    /// <summary>Gets or sets smart objects.</summary>
    public Abstractions.Hosting.ISmartObjectService? SmartObjects { get; init; }

    /// <summary>Gets or sets tactical points.</summary>
    public Abstractions.Hosting.ITacticalPointProvider? TacticalPoints { get; init; }

    /// <summary>Gets or sets danger.</summary>
    public Abstractions.Hosting.IDangerProvider? Danger { get; init; }
}
