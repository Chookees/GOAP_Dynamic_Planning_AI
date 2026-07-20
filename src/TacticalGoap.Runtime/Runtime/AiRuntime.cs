using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Lifecycle;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Actions;
using TacticalGoap.Runtime.Execution;
using TacticalGoap.Runtime.Goals;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime;

/// <summary>
/// Deterministic multi-agent GOAP runtime lifecycle and tick orchestrator.
/// </summary>
/// <remarks>
/// Lifecycle: Created → Configuring → Initialized → Frozen → Running → Stopped → Disposed.
/// After <see cref="Freeze"/>, the tick path must not deliberately allocate.
/// </remarks>
public sealed class AiRuntime : IDisposable
{
    private readonly AgentId[] _agents;
    private readonly GoalArbiter[] _arbiters;
    private readonly SymbolicWorldState[] _worldStates;
    private readonly SymbolicWorldState[] _desireStates;
    private readonly PlanExecutor?[] _executors;
    private readonly PlanningSession?[] _sessions;
    private readonly int _agentCapacity;
    private readonly ActionDefinitionRegistry _actionRegistry;
    private readonly GoapPlanner _planner;
    private readonly ReplanPolicy _replanPolicyTemplate;
    private readonly FailureKnowledgeWriter _sharedFailureKnowledge;

    private RuntimeLifecycleState _state;
    private int _agentCount;
    private long _lastTickSequence;
    private IPerceptionTickHook? _perceptionHook;
    private ISquadTickHook? _squadHook;
    private IRuntimeDiagnosticsSink? _diagnostics;
    private ActionHostServices _hostServices;
    private int _tacticalDataRegistered;

    /// <summary>
    /// Initializes a runtime in the <see cref="RuntimeLifecycleState.Created"/> state.
    /// </summary>
    public AiRuntime()
        : this(AiHardLimits.MaximumAgents)
    {
    }

    /// <summary>
    /// Initializes a runtime with an explicit agent capacity.
    /// </summary>
    /// <param name="agentCapacity">Maximum registered agents.</param>
    public AiRuntime(int agentCapacity)
    {
        if (agentCapacity < 1 || agentCapacity > AiHardLimits.MaximumAgents)
        {
            throw new ArgumentOutOfRangeException(nameof(agentCapacity));
        }

        _agentCapacity = agentCapacity;
        _agents = new AgentId[agentCapacity];
        _arbiters = new GoalArbiter[agentCapacity];
        _worldStates = new SymbolicWorldState[agentCapacity];
        _desireStates = new SymbolicWorldState[agentCapacity];
        _executors = new PlanExecutor?[agentCapacity];
        _sessions = new PlanningSession?[agentCapacity];
        _actionRegistry = new ActionDefinitionRegistry();
        _planner = new GoapPlanner();
        _replanPolicyTemplate = new ReplanPolicy();
        _sharedFailureKnowledge = new FailureKnowledgeWriter();
        _state = RuntimeLifecycleState.Created;
        _agentCount = 0;
        _lastTickSequence = -1L;
        _tacticalDataRegistered = 0;
    }

    /// <summary>Gets the current lifecycle state.</summary>
    public RuntimeLifecycleState State => _state;

    /// <summary>Gets the action registry.</summary>
    public ActionDefinitionRegistry ActionRegistry => _actionRegistry;

    /// <summary>Gets the registered agent count.</summary>
    public int AgentCount => _agentCount;

    /// <summary>
    /// Transitions from Created into Configuring.
    /// </summary>
    /// <returns>Success or invalid lifecycle state.</returns>
    public OperationStatus BeginConfiguration()
    {
        if (_state != RuntimeLifecycleState.Created)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        _state = RuntimeLifecycleState.Configuring;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Registers an agent during configuration.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <returns>Success, conflict, or capacity failure.</returns>
    public OperationStatus RegisterAgent(AgentId agentId)
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        if (!agentId.IsValid)
        {
            return OperationStatus.InvalidArgument;
        }

        if (_agentCount >= _agentCapacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < _agentCount; i++)
        {
            if (_agents[i] == agentId)
            {
                return OperationStatus.Conflict;
            }
        }

        int index = _agentCount;
        _agents[index] = agentId;
        _arbiters[index] = new GoalArbiter();
        _worldStates[index] = new SymbolicWorldState();
        _desireStates[index] = new SymbolicWorldState();
        _executors[index] = null;
        _sessions[index] = null;
        _agentCount = checked(_agentCount + 1);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Registers a goal into an agent's arbiter during configuration.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="goal">Goal instance.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus RegisterGoalSet(AgentId agentId, IGoapGoal goal)
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        int index = FindAgentIndex(agentId);
        if (index < 0)
        {
            return OperationStatus.NotFound;
        }

        return _arbiters[index].Register(goal);
    }

    /// <summary>
    /// Registers the standard action catalog during configuration.
    /// </summary>
    /// <returns>Success or registration failure.</returns>
    public OperationStatus RegisterActionSet()
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        return StandardActionCatalog.RegisterAll(_actionRegistry);
    }

    /// <summary>
    /// Registers an explicit action definition and executor.
    /// </summary>
    /// <param name="definition">Action definition.</param>
    /// <param name="executor">Action executor.</param>
    /// <returns>Success or registration failure.</returns>
    public OperationStatus RegisterActionSet(ActionDefinition definition, IGoapActionExecutor executor)
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        return _actionRegistry.Register(definition, executor);
    }

    /// <summary>
    /// Marks tactical data as registered during configuration.
    /// </summary>
    /// <param name="pointCount">Number of tactical points acknowledged.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus RegisterTacticalData(int pointCount)
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        if (pointCount < 0 || pointCount > AiHardLimits.MaximumTacticalPoints)
        {
            return OperationStatus.InvalidArgument;
        }

        _tacticalDataRegistered = pointCount;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Allocates per-agent executor and planner workspaces.
    /// </summary>
    /// <returns>Success or invalid lifecycle state.</returns>
    public OperationStatus Initialize()
    {
        if (_state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        if (_agentCount < 1 || _actionRegistry.Count < 1)
        {
            return OperationStatus.ContractViolation;
        }

        for (int i = 0; i < _agentCount; i++)
        {
            FailureKnowledgeWriter knowledge = new();
            ReplanPolicy policy = new(
                _replanPolicyTemplate.MaxAttempts,
                _replanPolicyTemplate.MinDelayTicks,
                _replanPolicyTemplate.AllowEmergencyInterrupt);
            _executors[i] = new PlanExecutor(_actionRegistry, policy, knowledge);
            PlannerWorkspace workspace = PlannerWorkspace.CreateDefault();
            _sessions[i] = new PlanningSession(workspace);
        }

        _state = RuntimeLifecycleState.Initialized;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Validates configuration invariants before freeze.
    /// </summary>
    /// <returns>Success or contract violation.</returns>
    public OperationStatus Validate()
    {
        if (_state != RuntimeLifecycleState.Initialized && _state != RuntimeLifecycleState.Configuring)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        if (_agentCount < 1 || _actionRegistry.Count < 1)
        {
            return OperationStatus.ContractViolation;
        }

        for (int i = 0; i < _agentCount; i++)
        {
            if (!_agents[i].IsValid || _arbiters[i].GoalCount < 1)
            {
                return OperationStatus.ContractViolation;
            }
        }

        if (_tacticalDataRegistered < 0)
        {
            return OperationStatus.ContractViolation;
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Seals configuration for deterministic ticks.
    /// </summary>
    /// <returns>Success or invalid lifecycle state.</returns>
    public OperationStatus Freeze()
    {
        if (_state != RuntimeLifecycleState.Initialized)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        OperationStatus validation = Validate();
        if (validation != OperationStatus.Success)
        {
            return validation;
        }

        _state = RuntimeLifecycleState.Frozen;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Sets optional runtime hooks. Allowed before Running.
    /// </summary>
    public OperationStatus SetHooks(
        IPerceptionTickHook? perception,
        ISquadTickHook? squad,
        IRuntimeDiagnosticsSink? diagnostics)
    {
        if (_state == RuntimeLifecycleState.Running || _state == RuntimeLifecycleState.Disposed)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        _perceptionHook = perception;
        _squadHook = squad;
        _diagnostics = diagnostics;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Sets host services used by action executors.
    /// </summary>
    public OperationStatus SetHostServices(in ActionHostServices hostServices)
    {
        if (_state == RuntimeLifecycleState.Disposed)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        _hostServices = hostServices;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Exposes an agent world state for host/test writes before or during running.
    /// </summary>
    public bool TryGetWorldState(AgentId agentId, out SymbolicWorldState? worldState)
    {
        int index = FindAgentIndex(agentId);
        if (index < 0)
        {
            worldState = null;
            return false;
        }

        worldState = _worldStates[index];
        return true;
    }

    /// <summary>
    /// Exposes an agent plan executor for tests.
    /// </summary>
    public bool TryGetExecutor(AgentId agentId, out PlanExecutor? executor)
    {
        int index = FindAgentIndex(agentId);
        if (index < 0)
        {
            executor = null;
            return false;
        }

        executor = _executors[index];
        return executor is not null;
    }

    /// <summary>
    /// Exposes an agent goal arbiter for tests.
    /// </summary>
    public bool TryGetArbiter(AgentId agentId, out GoalArbiter? arbiter)
    {
        int index = FindAgentIndex(agentId);
        if (index < 0)
        {
            arbiter = null;
            return false;
        }

        arbiter = _arbiters[index];
        return true;
    }

    /// <summary>
    /// Processes one deterministic host tick for all agents.
    /// </summary>
    /// <param name="tick">Host tick stamp.</param>
    /// <returns>Success or validation/lifecycle failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(in AiTick tick)
    {
        if (_state == RuntimeLifecycleState.Frozen)
        {
            _state = RuntimeLifecycleState.Running;
        }

        if (_state != RuntimeLifecycleState.Running)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        OperationStatus validation = ValidateTick(in tick);
        if (validation != OperationStatus.Success)
        {
            return validation;
        }

        _lastTickSequence = tick.Sequence;
        for (int i = 0; i < _agentCount; i++)
        {
            OperationStatus agentStatus = TickAgent(i, in tick);
            WriteDiagnostic(agentStatus, DiagnosticSubsystem.Execution, _agents[i].Value);
            if (agentStatus != OperationStatus.Success && agentStatus != OperationStatus.NoOp && agentStatus != OperationStatus.InProgress)
            {
                return agentStatus;
            }
        }

        if (_squadHook is not null)
        {
            OperationStatus squadStatus = _squadHook.Tick(in tick);
            WriteDiagnostic(squadStatus, DiagnosticSubsystem.Squad, 0);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private OperationStatus TickAgent(int index, in AiTick tick)
    {
        AgentId agentId = _agents[index];
        if (_perceptionHook is not null)
        {
            OperationStatus perception = _perceptionHook.Tick(in tick, agentId);
            if (perception != OperationStatus.Success && perception != OperationStatus.NoOp)
            {
                WriteDiagnostic(perception, DiagnosticSubsystem.Perception, agentId.Value);
            }
        }

        OperationStatus arbitration = ArbitrateAndPlan(index, agentId, in tick);
        if (arbitration != OperationStatus.Success && arbitration != OperationStatus.NoOp && arbitration != OperationStatus.InProgress)
        {
            return arbitration;
        }

        PlanExecutor? executor = _executors[index];
        if (executor is null)
        {
            return OperationStatus.ContractViolation;
        }

        _ = executor.Tick(agentId, _worldStates[index], tick.Sequence, tick.DeltaMilliseconds, in _hostServices);
        if (executor.ReplanRequested)
        {
            _ = BeginPlanning(index, agentId, tick.Sequence);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private OperationStatus ArbitrateAndPlan(int index, AgentId agentId, in AiTick tick)
    {
        OperationStatus status = _arbiters[index].Arbitrate(agentId, _worldStates[index], tick.Sequence, out IGoapGoal? selected);
        WriteDiagnostic(status, DiagnosticSubsystem.GoalArbitration, agentId.Value);
        if (status == OperationStatus.NoOp || selected is null)
        {
            return OperationStatus.NoOp;
        }

        if (_arbiters[index].PlanRequested || NeedsPlan(index))
        {
            return BeginPlanning(index, agentId, tick.Sequence);
        }

        PlanningSession? session = _sessions[index];
        if (session is not null && session.IsActive)
        {
            return ContinuePlanning(index);
        }

        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private bool NeedsPlan(int index)
    {
        PlanExecutor? executor = _executors[index];
        if (executor is null)
        {
            return true;
        }

        Plan? plan = executor.ActivePlan;
        return plan is null || plan.IsComplete || plan.Status != PlannerStatus.Succeeded;
    }

    [FrozenRuntimePath]
    private OperationStatus BeginPlanning(int index, AgentId agentId, long tickSequence)
    {
        PlanningSession? session = _sessions[index];
        PlanExecutor? executor = _executors[index];
        if (session is null || executor is null)
        {
            return OperationStatus.ContractViolation;
        }

        GoalArbitrationContext context = new(
            agentId,
            _worldStates[index],
            _arbiters[index].ActiveGoalId,
            _arbiters[index].ActiveGoalPriority,
            tickSequence,
            GoalPriorityBands.DefaultHysteresisMargin);

        OperationStatus desire = _arbiters[index].BuildActiveDesiredState(in context, _desireStates[index]);
        if (desire != OperationStatus.Success)
        {
            return desire;
        }

        // Candidate generation is deferred to registered definitions with empty binding for now.
        ActionCandidate[] empty = Array.Empty<ActionCandidate>();
        // Array.Empty is a cached singleton; acceptable and does not allocate per call.
        OperationStatus began = session.Begin(
            _arbiters[index].ActiveGoalId,
            tickSequence,
            _desireStates[index],
            _worldStates[index],
            empty,
            0);
        if (began != OperationStatus.Success)
        {
            return began;
        }

        return ContinuePlanning(index);
    }

    [FrozenRuntimePath]
    private OperationStatus ContinuePlanning(int index)
    {
        PlanningSession? session = _sessions[index];
        PlanExecutor? executor = _executors[index];
        if (session is null || executor is null)
        {
            return OperationStatus.ContractViolation;
        }

        int steps = 0;
        while (steps < AiHardLimits.MaximumPlannerStepsPerTick)
        {
            PlannerResult result = _planner.Step(session, AiHardLimits.MaximumPlannerExpansionsPerStep);
            steps = checked(steps + 1);
            WriteDiagnostic(
                result.IsSuccess ? OperationStatus.Success : OperationStatus.Failed,
                DiagnosticSubsystem.Planning,
                session.GoalId.Value);

            if (result.Status == PlannerStatus.Succeeded)
            {
                session.End();
                return executor.Install(session.OutputPlan);
            }

            if (result.Status == PlannerStatus.ExpansionBudgetExceeded || result.Status == PlannerStatus.InProgress)
            {
                return OperationStatus.InProgress;
            }

            session.End();
            return OperationStatus.NoOp;
        }

        return OperationStatus.InProgress;
    }

    [FrozenRuntimePath]
    private OperationStatus ValidateTick(in AiTick tick)
    {
        if (tick.Sequence < 0L)
        {
            return OperationStatus.InvalidArgument;
        }

        if (_lastTickSequence >= 0L && tick.Sequence < _lastTickSequence)
        {
            return OperationStatus.InvalidArgument;
        }

        if (tick.DeltaMilliseconds <= 0 || tick.DeltaMilliseconds > AiHardLimits.MaximumTickDeltaMilliseconds)
        {
            return OperationStatus.InvalidArgument;
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Stops the runtime after running.
    /// </summary>
    public OperationStatus Stop()
    {
        if (_state != RuntimeLifecycleState.Running && _state != RuntimeLifecycleState.Frozen)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        _state = RuntimeLifecycleState.Stopped;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Resets mutable runtime state for a new scenario while retaining registrations.
    /// </summary>
    public OperationStatus ResetForNewScenario()
    {
        if (_state != RuntimeLifecycleState.Stopped && _state != RuntimeLifecycleState.Frozen)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        _lastTickSequence = -1L;
        _sharedFailureKnowledge.Reset();
        for (int i = 0; i < _agentCount; i++)
        {
            _arbiters[i].Reset();
            _worldStates[i].Reset();
            _desireStates[i].Reset();
            _executors[i]?.Reset();
            _sessions[i]?.End();
        }

        _state = RuntimeLifecycleState.Frozen;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_state == RuntimeLifecycleState.Disposed)
        {
            return;
        }

        for (int i = 0; i < _agentCount; i++)
        {
            _executors[i]?.Reset();
            _sessions[i]?.End();
        }

        _state = RuntimeLifecycleState.Disposed;
    }

    private int FindAgentIndex(AgentId agentId)
    {
        for (int i = 0; i < _agentCount; i++)
        {
            if (_agents[i] == agentId)
            {
                return i;
            }
        }

        return -1;
    }

    [FrozenRuntimePath]
    private void WriteDiagnostic(OperationStatus status, DiagnosticSubsystem subsystem, int primaryId)
    {
        if (_diagnostics is null)
        {
            return;
        }

        if (status == OperationStatus.Success || status == OperationStatus.NoOp || status == OperationStatus.InProgress)
        {
            _diagnostics.Write(OperationResult.Success(subsystem, primaryId));
            return;
        }

        _diagnostics.Write(OperationResult.Failure(status, subsystem, primaryId: primaryId));
    }
}
