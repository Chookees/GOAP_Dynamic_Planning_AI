using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Flees immediate lethal danger.
/// </summary>
public sealed class FleeDangerAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 5;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public FleeDangerAction()
        : base(WellKnownActionIds.FleeDanger, 3)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.FleeDanger,
            BaseCost,
            WorldFactId.ImmediateDangerPresent,
            1,
            WorldFactId.ImmediateDangerResolved,
            1,
            WorldFactId.ImmediateDangerPresent);
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
            context.FocusEntity,
            context.FocusPoint,
            context.FocusNode,
            context.FocusWeapon,
            context.FocusOrder,
            context.FocusSector);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Commands is null || context.Animation is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.Dodge);
        ActionFailureReason mapped = MapHostStatus(animate, ActionFailureReason.AnimationRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        OperationStatus move = context.Commands.SubmitMove(context.AgentId, context.ScratchDestination);
        return MapHostStatus(move, ActionFailureReason.NoPath);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (context.Danger is not null && context.Movement is not null)
        {
            OperationStatus posStatus = context.Movement.TryGetPosition(context.AgentId, out var position);
            if (posStatus == OperationStatus.Success && context.Danger.IsPositionDangerous(position))
            {
                if (context.Commands is not null)
                {
                    _ = context.Commands.SubmitMove(context.AgentId, context.ScratchDestination);
                }
            }
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
