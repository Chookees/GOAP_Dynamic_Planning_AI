using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Uses a bound smart object.
/// </summary>
public sealed class UseSmartObjectAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 3;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public UseSmartObjectAction()
        : base(WellKnownActionIds.UseSmartObject, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.UseSmartObject,
            BaseCost,
            null,
            0,
            WorldFactId.AtTacticalPoint,
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
        if (!context.FocusEntity.IsValid)
        {
            return 0;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            context.FocusEntity,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (context.Interaction is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        SmartObjectId smartObject = SmartObjectId.FromInt32(context.Candidate.BoundEntityId.Value);
        OperationStatus can = context.Interaction.CanInteract(context.AgentId, smartObject);
        return MapHostStatus(can, ActionFailureReason.InteractionRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (context.Interaction is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        SmartObjectId smartObject = SmartObjectId.FromInt32(context.Candidate.BoundEntityId.Value);
        OperationStatus beginInteract = context.Interaction.BeginInteract(context.AgentId, smartObject);
        ActionFailureReason mapped = MapHostStatus(beginInteract, ActionFailureReason.InteractionRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        if (context.ProgressCounter >= context.RequiredProgressTicks && context.Interaction is not null)
        {
            _ = context.Interaction.EndInteract(context.AgentId, smartObject);
        }

        return ActionFailureReason.None;
    }
}
