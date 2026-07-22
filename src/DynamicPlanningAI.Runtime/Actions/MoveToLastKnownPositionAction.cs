using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Moves to the last known target position.
/// </summary>
public sealed class MoveToLastKnownPositionAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 5;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public MoveToLastKnownPositionAction()
        : base(WellKnownActionIds.MoveToLastKnownPosition, 3)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.MoveToLastKnownPosition,
            BaseCost,
            WorldFactId.TargetKnown,
            1,
            WorldFactId.AtMovementDestination,
            1,
            null);
    }

    /// <summary>
    /// Generates a grounded candidate for this action when focus bindings allow.
    /// </summary>
    /// <inheritdoc />
    public int Generate(
        in CandidateGenerationContext context,
        ActionDefinition definition,
        BoundedCandidateWriter writer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(writer);
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            context.FocusPoint,
            context.FocusNode,
            WeaponId.Invalid,
            context.FocusOrder,
            context.FocusSector);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Commands is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus move = context.Commands.SubmitMove(context.AgentId, context.ScratchDestination);
        return MapHostStatus(move, ActionFailureReason.NoPath);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (context.Movement is null)
        {
            // Null-safe stub path: progress without host position reads.
            AdvanceProgress(ref context);
            return ActionFailureReason.None;
        }

        OperationStatus status = context.Movement.TryGetPosition(context.AgentId, out _);
        ActionFailureReason mapped = MapHostStatus(status, ActionFailureReason.HostServiceFailure);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
