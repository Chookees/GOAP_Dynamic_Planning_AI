using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Fixed-capacity reconstructed GOAP plan.
/// </summary>
public sealed class Plan
{
    private readonly ActionCandidate[] _candidates;
    private readonly int _capacity;
    private int _cursor;

    /// <summary>
    /// Initializes an empty plan with hard-limit capacity.
    /// </summary>
    public Plan()
        : this(AiHardLimits.MaximumPlanLength)
    {
    }

    /// <summary>
    /// Initializes an empty plan with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Maximum plan length.</param>
    public Plan(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumPlanLength)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Plan capacity is out of range.");
        }

        _capacity = capacity;
        _candidates = new ActionCandidate[capacity];
        Reset();
    }

    /// <summary>
    /// Gets the motivating goal identifier.
    /// </summary>
    public GoalId GoalId { get; private set; }

    /// <summary>
    /// Gets the creation tick sequence.
    /// </summary>
    public long CreationTick { get; private set; }

    /// <summary>
    /// Gets the source world-state hash used when the plan was built.
    /// </summary>
    public ulong SourceHash { get; private set; }

    /// <summary>
    /// Gets the number of actions in the plan.
    /// </summary>
    public int ActionCount { get; private set; }

    /// <summary>
    /// Gets the total estimated plan cost.
    /// </summary>
    public int TotalCost { get; private set; }

    /// <summary>
    /// Gets the plan status.
    /// </summary>
    public PlannerStatus Status { get; private set; }

    /// <summary>
    /// Gets the fixed capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the current cursor index.
    /// </summary>
    public int Cursor => _cursor;

    /// <summary>
    /// Gets a value indicating whether the plan has no remaining actions.
    /// </summary>
    public bool IsComplete => Status != PlannerStatus.Succeeded || _cursor >= ActionCount;

    /// <summary>
    /// Assigns plan contents from a successful reconstruction.
    /// </summary>
    /// <param name="goalId">Goal identifier.</param>
    /// <param name="creationTick">Creation tick sequence.</param>
    /// <param name="sourceHash">Source world hash.</param>
    /// <param name="candidates">Candidate buffer in forward execution order.</param>
    /// <param name="count">Action count.</param>
    /// <param name="totalCost">Total cost.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus Assign(
        GoalId goalId,
        long creationTick,
        ulong sourceHash,
        ReadOnlySpan<ActionCandidate> candidates,
        int count,
        int totalCost)
    {
        if (!goalId.IsValid || count < 0 || count > candidates.Length || totalCost < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        if (count > _capacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        GoalId = goalId;
        CreationTick = creationTick;
        SourceHash = sourceHash;
        ActionCount = count;
        TotalCost = totalCost;
        Status = PlannerStatus.Succeeded;
        _cursor = 0;

        for (int i = 0; i < count; i++)
        {
            _candidates[i] = candidates[i];
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Peeks at the current action without advancing.
    /// </summary>
    /// <param name="candidate">Receives the current candidate.</param>
    /// <returns><see langword="true"/> when an action remains.</returns>
    public bool TryPeek(out ActionCandidate candidate)
    {
        if (Status != PlannerStatus.Succeeded || _cursor >= ActionCount)
        {
            candidate = default;
            return false;
        }

        candidate = _candidates[_cursor];
        return true;
    }

    /// <summary>
    /// Advances the cursor past the current action.
    /// </summary>
    /// <returns>Success, no-op when complete, or invalid state.</returns>
    public OperationStatus Advance()
    {
        if (Status != PlannerStatus.Succeeded)
        {
            return OperationStatus.InvalidLifecycleState;
        }

        if (_cursor >= ActionCount)
        {
            return OperationStatus.NoOp;
        }

        _cursor = checked(_cursor + 1);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Marks the plan invalid without releasing capacity.
    /// </summary>
    public void Invalidate()
    {
        Status = PlannerStatus.NoPlan;
        _cursor = ActionCount;
    }

    /// <summary>
    /// Marks the plan cancelled.
    /// </summary>
    public void Cancel()
    {
        Status = PlannerStatus.Cancelled;
        _cursor = ActionCount;
    }

    /// <summary>
    /// Clears plan contents for reuse.
    /// </summary>
    public void Reset()
    {
        GoalId = GoalId.Invalid;
        CreationTick = 0L;
        SourceHash = 0UL;
        ActionCount = 0;
        TotalCost = 0;
        Status = PlannerStatus.NoPlan;
        _cursor = 0;
    }

    /// <summary>
    /// Validates structural integrity of the current plan.
    /// </summary>
    /// <returns>Success or contract violation.</returns>
    public OperationStatus Validate()
    {
        if (Status != PlannerStatus.Succeeded)
        {
            return OperationStatus.Success;
        }

        if (!GoalId.IsValid || ActionCount < 0 || ActionCount > _capacity || TotalCost < 0)
        {
            return OperationStatus.ContractViolation;
        }

        if (_cursor < 0 || _cursor > ActionCount)
        {
            return OperationStatus.ContractViolation;
        }

        for (int i = 0; i < ActionCount; i++)
        {
            if (!_candidates[i].IsValid)
            {
                return OperationStatus.ContractViolation;
            }
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Copies a candidate at an absolute plan index.
    /// </summary>
    /// <param name="index">Zero-based plan index.</param>
    /// <param name="candidate">Receives the candidate.</param>
    /// <returns><see langword="true"/> when the index is in range.</returns>
    public bool TryGet(int index, out ActionCandidate candidate)
    {
        if (index < 0 || index >= ActionCount)
        {
            candidate = default;
            return false;
        }

        candidate = _candidates[index];
        return true;
    }
}
