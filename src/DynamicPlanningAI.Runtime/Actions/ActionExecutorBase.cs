using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Shared Begin/Tick/Cancel state machine for GOAP action executors.
/// </summary>
public abstract class ActionExecutorBase : IGoapActionExecutor
{
    /// <summary>
    /// Initializes the executor identity and default duration.
    /// </summary>
    /// <param name="definitionId">Definition identifier.</param>
    /// <param name="requiredProgressTicks">Ticks of successful progress required.</param>
    protected ActionExecutorBase(ActionId definitionId, int requiredProgressTicks)
    {
        if (!definitionId.IsValid)
        {
            throw new ArgumentException("Definition id must be valid.", nameof(definitionId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(requiredProgressTicks, 1);

        DefinitionId = definitionId;
        DefaultRequiredProgressTicks = requiredProgressTicks;
    }

    /// <inheritdoc />
    public ActionId DefinitionId { get; }

    /// <summary>
    /// Gets the default required progress ticks.
    /// </summary>
    protected int DefaultRequiredProgressTicks { get; }

    /// <inheritdoc />
    [FrozenRuntimePath]
    public ActionTickResult Begin(ref ActionExecutionContext context)
    {
        context.Status = ActionStatus.Starting;
        context.FailureReason = ActionFailureReason.None;
        context.TicksElapsed = 0;
        context.ProgressCounter = 0;
        if (context.MaximumTicks <= 0)
        {
            context.MaximumTicks = AiHardLimits.MaximumActionTicks;
        }

        if (context.RequiredProgressTicks <= 0)
        {
            context.RequiredProgressTicks = DefaultRequiredProgressTicks;
        }

        ActionFailureReason beginFailure = OnBegin(ref context);
        if (beginFailure != ActionFailureReason.None)
        {
            return Fail(ref context, beginFailure);
        }

        context.Status = ActionStatus.Running;
        return ActionTickResult.Running(context.TicksElapsed);
    }

    /// <inheritdoc />
    [FrozenRuntimePath]
    public ActionTickResult Tick(ref ActionExecutionContext context)
    {
        if (context.Status == ActionStatus.NotStarted)
        {
            return Begin(ref context);
        }

        if (context.Status != ActionStatus.Running && context.Status != ActionStatus.Starting)
        {
            return new ActionTickResult(context.Status, context.FailureReason, context.TicksElapsed);
        }

        context.TicksElapsed = checked(context.TicksElapsed + 1);
        if (context.TicksElapsed > context.MaximumTicks)
        {
            context.Status = ActionStatus.TimedOut;
            context.FailureReason = ActionFailureReason.MaximumTickCountReached;
            OnTimeout(ref context);
            return new ActionTickResult(ActionStatus.TimedOut, context.FailureReason, context.TicksElapsed);
        }

        ActionFailureReason volatileFailure = ValidateVolatilePreconditions(ref context);
        if (volatileFailure != ActionFailureReason.None)
        {
            return Fail(ref context, volatileFailure);
        }

        ActionFailureReason tickFailure = OnTick(ref context);
        if (tickFailure != ActionFailureReason.None)
        {
            return Fail(ref context, tickFailure);
        }

        if (context.ProgressCounter >= context.RequiredProgressTicks)
        {
            ApplyEffects(ref context);
            context.Status = ActionStatus.Succeeded;
            context.FailureReason = ActionFailureReason.None;
            return ActionTickResult.Succeeded(context.TicksElapsed);
        }

        context.Status = ActionStatus.Running;
        return ActionTickResult.Running(context.TicksElapsed);
    }

    /// <inheritdoc />
    [FrozenRuntimePath]
    public ActionTickResult Cancel(ref ActionExecutionContext context)
    {
        OnCancel(ref context);
        context.Status = ActionStatus.Cancelled;
        context.FailureReason = ActionFailureReason.None;
        return new ActionTickResult(ActionStatus.Cancelled, ActionFailureReason.None, context.TicksElapsed);
    }

    /// <summary>
    /// Performs start validation and host resource acquisition.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <returns>Failure reason, or <see cref="ActionFailureReason.None"/>.</returns>
    protected abstract ActionFailureReason OnBegin(ref ActionExecutionContext context);

    /// <summary>
    /// Advances one tick of execution progress.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <returns>Failure reason, or <see cref="ActionFailureReason.None"/>.</returns>
    protected abstract ActionFailureReason OnTick(ref ActionExecutionContext context);

    /// <summary>
    /// Releases host resources on cancel.
    /// </summary>
    /// <param name="context">Execution context.</param>
    protected virtual void OnCancel(ref ActionExecutionContext context)
    {
        if (context.Movement is not null)
        {
            _ = context.Movement.TryStop(context.AgentId);
        }

        if (context.Animation is not null)
        {
            _ = context.Animation.StopAnimation(context.AgentId);
        }
    }

    /// <summary>
    /// Invoked when the action times out.
    /// </summary>
    /// <param name="context">Execution context.</param>
    protected virtual void OnTimeout(ref ActionExecutionContext context)
    {
        OnCancel(ref context);
    }

    /// <summary>
    /// Validates volatile preconditions each tick.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <returns>Failure reason, or none.</returns>
    protected virtual ActionFailureReason ValidateVolatilePreconditions(ref ActionExecutionContext context)
    {
        ActionCandidate candidate = context.Candidate;
        SymbolicWorldState? world = context.WorldState;
        if (world is null || candidate.Preconditions is null)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (!world.Satisfies(candidate.Preconditions))
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        return ActionFailureReason.None;
    }

    /// <summary>
    /// Applies symbolic effects to the world state on success.
    /// </summary>
    /// <param name="context">Execution context.</param>
    protected virtual void ApplyEffects(ref ActionExecutionContext context)
    {
        SymbolicWorldState? world = context.WorldState;
        ActionCandidate candidate = context.Candidate;
        if (world is null || candidate.EffectSets is null)
        {
            return;
        }

        MergeSetEffects(world, candidate.EffectSets);
        ClearEffectBits(world, candidate.EffectClearMask);
    }

    [FrozenRuntimePath]
    private static void MergeSetEffects(SymbolicWorldState world, SymbolicWorldState effectSets)
    {
        ulong remaining = effectSets.SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = System.Numerics.BitOperations.TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;
            if (!effectSets.TryGet((WorldFactId)index, out int value))
            {
                continue;
            }

            _ = world.Set((WorldFactId)index, value);
        }
    }

    [FrozenRuntimePath]
    private static void ClearEffectBits(SymbolicWorldState world, ulong clearMask)
    {
        ulong remaining = clearMask;
        while (remaining != 0UL)
        {
            int index = System.Numerics.BitOperations.TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;
            if (index < world.Capacity)
            {
                _ = world.Clear((WorldFactId)index);
            }
        }
    }

    [FrozenRuntimePath]
    private static ActionTickResult Fail(ref ActionExecutionContext context, ActionFailureReason reason)
    {
        context.Status = ActionStatus.Failed;
        context.FailureReason = reason;
        return ActionTickResult.Failed(reason, context.TicksElapsed);
    }

    /// <summary>
    /// Maps a host operation status to an action failure reason.
    /// </summary>
    /// <param name="status">Host status.</param>
    /// <param name="rejectedReason">Reason used for host rejection.</param>
    /// <returns>Mapped failure reason, or none on success/in-progress.</returns>
    protected static ActionFailureReason MapHostStatus(OperationStatus status, ActionFailureReason rejectedReason)
    {
        if (status == OperationStatus.Success || status == OperationStatus.InProgress || status == OperationStatus.NoOp)
        {
            return ActionFailureReason.None;
        }

        if (status == OperationStatus.HostRejected)
        {
            return rejectedReason;
        }

        if (status == OperationStatus.NotFound)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (status == OperationStatus.TimedOut)
        {
            return ActionFailureReason.MaximumTickCountReached;
        }

        return ActionFailureReason.HostServiceFailure;
    }

    /// <summary>
    /// Increments the progress counter using checked arithmetic.
    /// </summary>
    /// <param name="context">Execution context.</param>
    protected static void AdvanceProgress(ref ActionExecutionContext context)
    {
        context.ProgressCounter = checked(context.ProgressCounter + 1);
    }
}
