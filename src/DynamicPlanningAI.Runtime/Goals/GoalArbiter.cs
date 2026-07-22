using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Goals;

/// <summary>
/// Evaluates registered goals and selects at most one active goal per tick.
/// </summary>
/// <remarks>
/// Selection applies relevance, cooldowns, hysteresis against the active goal,
/// interruption policy, and deterministic <see cref="GoalId"/> tie-breaking.
/// Capacity is fixed at construction; no post-init growth occurs.
/// </remarks>
public sealed class GoalArbiter
{
    private readonly IGoapGoal[] _goals;
    private readonly long[] _cooldownUntilTick;
    private readonly int _capacity;
    private int _goalCount;
    private GoalId _activeGoalId;
    private int _activeGoalPriority;
    private int _hysteresisMargin;
    private int _cooldownTicks;
    private bool _planRequested;

    /// <summary>
    /// Initializes an arbiter with hard-limit capacity.
    /// </summary>
    public GoalArbiter()
        : this(AiHardLimits.MaximumGoalsPerAgent)
    {
    }

    /// <summary>
    /// Initializes an arbiter with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Maximum registered goals.</param>
    public GoalArbiter(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumGoalsPerAgent)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Goal capacity is out of range.");
        }

        _capacity = capacity;
        _goals = new IGoapGoal[capacity];
        _cooldownUntilTick = new long[capacity];
        _goalCount = 0;
        _activeGoalId = GoalId.Invalid;
        _activeGoalPriority = 0;
        _hysteresisMargin = GoalPriorityBands.DefaultHysteresisMargin;
        _cooldownTicks = GoalPriorityBands.DefaultCooldownTicks;
        _planRequested = false;
    }

    /// <summary>
    /// Gets the currently selected goal identifier.
    /// </summary>
    public GoalId ActiveGoalId => _activeGoalId;

    /// <summary>
    /// Gets the priority of the active goal.
    /// </summary>
    public int ActiveGoalPriority => _activeGoalPriority;

    /// <summary>
    /// Gets the number of registered goals.
    /// </summary>
    public int GoalCount => _goalCount;

    /// <summary>
    /// Gets a value indicating whether arbitration requested a new plan.
    /// </summary>
    public bool PlanRequested => _planRequested;

    /// <summary>
    /// Configures hysteresis and cooldown parameters.
    /// </summary>
    /// <param name="hysteresisMargin">Priority margin required to displace the active goal.</param>
    /// <param name="cooldownTicks">Ticks a deselected goal remains ineligible.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus Configure(int hysteresisMargin, int cooldownTicks)
    {
        if (hysteresisMargin < 0 || cooldownTicks < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        _hysteresisMargin = hysteresisMargin;
        _cooldownTicks = cooldownTicks;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Registers a goal during configuration.
    /// </summary>
    /// <param name="goal">Goal instance.</param>
    /// <returns>Success, capacity failure, or validation failure.</returns>
    public OperationStatus Register(IGoapGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        if (!goal.Id.IsValid)
        {
            return OperationStatus.InvalidArgument;
        }

        if (_goalCount >= _capacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < _goalCount; i++)
        {
            if (_goals[i].Id == goal.Id)
            {
                return OperationStatus.Conflict;
            }
        }

        _goals[_goalCount] = goal;
        _cooldownUntilTick[_goalCount] = 0L;
        _goalCount = checked(_goalCount + 1);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Selects the active goal for the supplied world state and tick.
    /// </summary>
    /// <param name="agentId">Agent under arbitration.</param>
    /// <param name="worldState">Current symbolic world state.</param>
    /// <param name="tickSequence">Current tick sequence.</param>
    /// <param name="selectedGoal">Receives the selected goal when any is relevant.</param>
    /// <returns>Success when a goal is selected; <see cref="OperationStatus.NoOp"/> when none.</returns>
    [FrozenRuntimePath]
    public OperationStatus Arbitrate(
        AgentId agentId,
        SymbolicWorldState worldState,
        long tickSequence,
        out IGoapGoal? selectedGoal)
    {
        ArgumentNullException.ThrowIfNull(worldState);
        _planRequested = false;
        selectedGoal = null;

        GoalArbitrationContext context = new(
            agentId,
            worldState,
            _activeGoalId,
            _activeGoalPriority,
            tickSequence,
            _hysteresisMargin);

        bool activeStillRelevant = IsActiveStillRelevant(in context);

        int bestIndex = -1;
        int bestPriority = -1;
        GoalId bestId = GoalId.Invalid;

        for (int i = 0; i < _goalCount; i++)
        {
            if (!TryEvaluateCandidate(i, in context, tickSequence, ref bestIndex, ref bestPriority, ref bestId))
            {
                continue;
            }
        }

        if (bestIndex < 0)
        {
            return ClearActiveGoal(tickSequence);
        }

        return ApplySelection(bestIndex, bestPriority, tickSequence, activeStillRelevant, out selectedGoal);
    }

    [FrozenRuntimePath]
    private bool TryEvaluateCandidate(
        int index,
        in GoalArbitrationContext context,
        long tickSequence,
        ref int bestIndex,
        ref int bestPriority,
        ref GoalId bestId)
    {
        IGoapGoal goal = _goals[index];
        if (tickSequence < _cooldownUntilTick[index] && goal.Id != _activeGoalId)
        {
            return false;
        }

        if (!goal.IsRelevant(in context))
        {
            return false;
        }

        int priority = goal.EvaluatePriority(in context);
        if (priority < 0)
        {
            return false;
        }

        if (!BeatsCurrentBest(goal.Id, priority, bestPriority, bestId))
        {
            return false;
        }

        bestIndex = index;
        bestPriority = priority;
        bestId = goal.Id;
        return true;
    }

    [FrozenRuntimePath]
    private static bool BeatsCurrentBest(GoalId candidateId, int candidatePriority, int bestPriority, GoalId bestId)
    {
        if (bestPriority < 0)
        {
            return true;
        }

        if (candidatePriority > bestPriority)
        {
            return true;
        }

        if (candidatePriority < bestPriority)
        {
            return false;
        }

        return candidateId.CompareTo(bestId) < 0;
    }

    [FrozenRuntimePath]
    private OperationStatus ApplySelection(
        int bestIndex,
        int bestPriority,
        long tickSequence,
        bool activeStillRelevant,
        out IGoapGoal? selectedGoal)
    {
        IGoapGoal best = _goals[bestIndex];
        selectedGoal = best;

        if (_activeGoalId == best.Id)
        {
            _activeGoalPriority = bestPriority;
            return OperationStatus.Success;
        }

        if (activeStillRelevant && !MayInterrupt(best, bestPriority))
        {
            selectedGoal = FindActiveGoal();
            return selectedGoal is null ? OperationStatus.NoOp : OperationStatus.Success;
        }

        if (_activeGoalId.IsValid)
        {
            ApplyCooldown(_activeGoalId, tickSequence);
        }

        _activeGoalId = best.Id;
        _activeGoalPriority = bestPriority;
        _planRequested = true;
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private bool IsActiveStillRelevant(in GoalArbitrationContext context)
    {
        if (!_activeGoalId.IsValid)
        {
            return false;
        }

        for (int i = 0; i < _goalCount; i++)
        {
            if (_goals[i].Id != _activeGoalId)
            {
                continue;
            }

            return _goals[i].IsRelevant(in context);
        }

        return false;
    }

    [FrozenRuntimePath]
    private bool MayInterrupt(IGoapGoal challenger, int challengerPriority)
    {
        if (!_activeGoalId.IsValid)
        {
            return true;
        }

        IGoapGoal? active = FindActiveGoal();
        if (active is null)
        {
            return true;
        }

        GoalInterruptionPolicy policy = active.InterruptionPolicy;
        if (policy == GoalInterruptionPolicy.Never)
        {
            return false;
        }

        int threshold = checked(_activeGoalPriority + _hysteresisMargin);
        if (challengerPriority < threshold)
        {
            return false;
        }

        if (policy == GoalInterruptionPolicy.HigherPriorityOnly && challengerPriority <= _activeGoalPriority)
        {
            return false;
        }

        return true;
    }

    [FrozenRuntimePath]
    private OperationStatus ClearActiveGoal(long tickSequence)
    {
        if (_activeGoalId.IsValid)
        {
            ApplyCooldown(_activeGoalId, tickSequence);
        }

        _activeGoalId = GoalId.Invalid;
        _activeGoalPriority = 0;
        return OperationStatus.NoOp;
    }

    [FrozenRuntimePath]
    private void ApplyCooldown(GoalId goalId, long tickSequence)
    {
        for (int i = 0; i < _goalCount; i++)
        {
            if (_goals[i].Id != goalId)
            {
                continue;
            }

            _cooldownUntilTick[i] = checked(tickSequence + _cooldownTicks);
            return;
        }
    }

    [FrozenRuntimePath]
    private IGoapGoal? FindActiveGoal()
    {
        for (int i = 0; i < _goalCount; i++)
        {
            if (_goals[i].Id == _activeGoalId)
            {
                return _goals[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Clears active selection and cooldowns for a new scenario.
    /// </summary>
    public void Reset()
    {
        _activeGoalId = GoalId.Invalid;
        _activeGoalPriority = 0;
        _planRequested = false;
        for (int i = 0; i < _goalCount; i++)
        {
            _cooldownUntilTick[i] = 0L;
        }
    }

    /// <summary>
    /// Builds the desire state for the currently selected goal.
    /// </summary>
    /// <param name="context">Arbitration context.</param>
    /// <param name="destination">Destination desire state.</param>
    /// <returns>Success, not-found, or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus BuildActiveDesiredState(in GoalArbitrationContext context, SymbolicWorldState destination)
    {
        IGoapGoal? active = FindActiveGoal();
        if (active is null)
        {
            return OperationStatus.NotFound;
        }

        return active.BuildDesiredState(in context, destination);
    }
}
